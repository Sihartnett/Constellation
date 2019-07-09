using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class GameController : MonoBehaviour {
    public enum EGameState {
        ReadyToShoot,
        WaitingToShoot,
        TransitioningToReady,
        GameOver,
        Repositioning,
    }

    [Header("Necessary Transforms")]
    public Transform cueBall;
    public Transform cueStick;
    public Transform[] starBalls;
    private List<Transform> m_starBallsList;

    [Header("Cue Stick Lines")]
    public Transform cueStickLine;
    private LineRenderer m_cueStickLineRenderer;

    [Header("User Interface")]
    public GameObject youWinScreen;

    [Header("Game Rules")]
    public GameRules gameRulesAsset;
    private float m_timeWaiting = 0f;

    private Rigidbody m_cueBallRigidbody;
    private Vector3 m_cueStickOriginalPosition;
    private float m_currentSliderValue = 0f;

    // Rotation around cue ball
    private const float rotationSpeed = 300f;
    private const float cueStickDistanceMultiplier = 2.5f;

    // Mouse Related Stuff
    private const int MOUSE_PRIMARY_BUTTON = 0;

    // State Related
    private EGameState m_currentGameState = EGameState.ReadyToShoot;

    private void Start() {
        // Caching Components
        m_cueBallRigidbody = cueBall.GetComponent<Rigidbody>();
        m_cueStickLineRenderer = cueStickLine.GetComponent<LineRenderer>();
        m_starBallsList = starBalls.ToList();

        // Deactivating UI stuff
        youWinScreen.SetActive(false);

        // We keep it kinematic until we want to shoot
        m_cueBallRigidbody.isKinematic = true;
        m_cueStickOriginalPosition = cueStick.position;

        // Preparing the Triggers
        DetectBallTrigger[] detectBallTriggers = FindObjectsOfType<DetectBallTrigger>();
        foreach(DetectBallTrigger ballTrigger in detectBallTriggers) {
            ballTrigger.CueBallOnTrigger += ResetCueBall;
            ballTrigger.StarBallOnTrigger += StartBallDestroyed;
        }
    }

    private void OnValidate() {
        if(cueBall) {
            Debug.Log("Validating Cue Ball...");
            Rigidbody t_cueBallRigidbody = cueBall.GetComponent<Rigidbody>();
            t_cueBallRigidbody.interpolation = RigidbodyInterpolation.Extrapolate;
            t_cueBallRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        }

        if(starBalls.Length > 0) {
            Debug.Log("Validating Star Balls...");
            foreach(Transform t_starBall in starBalls) {
                Rigidbody t_starBallRigidbody = t_starBall.GetComponent<Rigidbody>();
                t_starBallRigidbody.interpolation = RigidbodyInterpolation.Extrapolate;
                t_starBallRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            }
        }
    }

    private void Update() {
        switch(m_currentGameState) {
            case EGameState.ReadyToShoot:
                // Known Issue: There should be UI elements everywhere the player shouldn't be clicking...
                if (Input.GetMouseButton(MOUSE_PRIMARY_BUTTON) && !EventSystem.current.IsPointerOverGameObject()) {
                    ProcessCueStickRotation();
                }
                Debug.DrawRay(cueStick.transform.position, -cueStick.up * 10f, Color.blue, 0.25f);
                break;
            case EGameState.WaitingToShoot:
                // [TO DO]
                // Maybe this could be a Coroutine ?!
                m_timeWaiting += Time.deltaTime;
                if(m_timeWaiting > gameRulesAsset.maximumWaitTime) {
                    m_currentGameState = EGameState.TransitioningToReady;
                    m_timeWaiting = 0f;
                    DecelerateAllBalls();
                }
                break;
            case EGameState.TransitioningToReady:
                // [TO DO]
                // Maybe this could be a Coroutine ?!
                m_timeWaiting += Time.deltaTime;
                if(m_timeWaiting > gameRulesAsset.timeToStopAllBalls) {
                    ResetCueStickPosition();
                    m_currentGameState = EGameState.ReadyToShoot;
                    cueStick.gameObject.SetActive(true);
                    cueStickLine.gameObject.SetActive(true);
                }
                break;
            case EGameState.Repositioning:
                RaycastHit mouseHitInfo;

                // [TO DO]: Make it collide only with playable area
                // I can add a LayerMask thing.
                if(Physics.Raycast(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector3.down, out mouseHitInfo, 100f)) {
                    cueBall.position = mouseHitInfo.point;

                    if (Input.GetMouseButtonDown(MOUSE_PRIMARY_BUTTON)) {
                        cueBall.Translate(Vector3.up);
                        cueBall.GetComponent<SphereCollider>().enabled = true;
                        m_cueBallRigidbody.velocity = Vector3.zero;
                        m_cueBallRigidbody.rotation = Quaternion.Euler(Vector3.zero);

                        m_timeWaiting = 0f;
                        m_currentGameState = EGameState.TransitioningToReady;
                    }
                }
                break;
        }

        // Line Renderer to show where the ball will goes to
        RaycastHit hitInfo;
        bool hitAPoint = Physics.Raycast(cueBall.transform.position, -cueStick.up, out hitInfo);

        if(hitAPoint) {
            Debug.DrawLine(cueBall.transform.position, hitInfo.point, Color.black, 0.25f);

            m_cueStickLineRenderer.SetPosition(0, cueBall.transform.localPosition);
            m_cueStickLineRenderer.SetPosition(1, hitInfo.point - cueBall.transform.parent.position);
        }
    }

    private void ProcessCueStickRotation() {
        Vector3 mousePositionInWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePositionInWorld.y = cueBall.transform.position.y;
        Debug.DrawLine(mousePositionInWorld, cueBall.transform.position, Color.green, 0.25f);

        // Rotate so the stick is parallel with the mouse position
        Vector3 fromBallToStick = cueStick.transform.position - cueBall.transform.position;
        Vector3 fromBallToMouse = mousePositionInWorld - cueBall.transform.position;
        float t_rotationValue = Vector3.SignedAngle(fromBallToStick, fromBallToMouse, Vector3.up) * Mathf.Deg2Rad;

        cueStick.RotateAround(cueBall.position, Vector3.up, t_rotationValue * Time.deltaTime * rotationSpeed);

        // The Original Position should be when slider value = 0
        m_cueStickOriginalPosition = cueStick.position - ((cueStick.transform.up * cueStickDistanceMultiplier) * m_currentSliderValue);
    }

    private void DecelerateAllBalls() {
        StartCoroutine(DecelerateBallRoutine(m_cueBallRigidbody));
        foreach(Transform t_starBall in starBalls) {
            if(t_starBall == null) {
                // Ball was already destroyed!
                continue;
            }

            StartCoroutine(DecelerateBallRoutine(t_starBall.GetComponent<Rigidbody>()));
        }
    }

    private IEnumerator DecelerateBallRoutine(Rigidbody _ballRigidbody) {
        Vector3 t_currentBallVelocity = _ballRigidbody.velocity;
        Vector3 t_currentBallRotationVelocity = _ballRigidbody.angularVelocity;
        Vector3 t_futureBallVelocity = Vector3.zero;

        for(float t_timeElapsed = 0f; t_timeElapsed < gameRulesAsset.timeToStopAllBalls; t_timeElapsed += Time.deltaTime) {
            float t = Mathf.Clamp01(t_timeElapsed / gameRulesAsset.timeToStopAllBalls);
            _ballRigidbody.velocity = Vector3.Lerp(t_currentBallVelocity, t_futureBallVelocity, t);
            _ballRigidbody.angularVelocity = Vector3.Lerp(t_currentBallRotationVelocity, t_futureBallVelocity, t);
            yield return null;
        }

        _ballRigidbody.velocity = t_futureBallVelocity;
        _ballRigidbody.angularVelocity = t_futureBallVelocity;
    }

    private void ResetCueStickPosition() {
        cueStick.position = cueBall.position;
        cueStick.Translate(cueStick.up * 2.5f, Space.World);
        m_cueStickOriginalPosition = cueStick.position;
        OffsetCueStickWithSliderValue();
    }

    // CueBehavior interacting with UI elements is not good
    public void SliderValueChanged(Slider _slider) {
        m_currentSliderValue = _slider.value;
        OffsetCueStickWithSliderValue();
    }

    private void OffsetCueStickWithSliderValue() {
        Vector3 t_cueStickMaximumDistance = m_cueStickOriginalPosition + (cueStick.transform.up * cueStickDistanceMultiplier);
        cueStick.transform.position = Vector3.Lerp(m_cueStickOriginalPosition, t_cueStickMaximumDistance, m_currentSliderValue);
    } 

    public void Shoot() {
        if(m_currentGameState != EGameState.ReadyToShoot) {
            return;
        }

        m_timeWaiting = 0f;
        m_currentGameState = EGameState.WaitingToShoot;
        cueStickLine.gameObject.SetActive(false);
        cueStick.gameObject.SetActive(false);
        m_cueBallRigidbody.isKinematic = false;

        float t_shootForce = Mathf.Lerp(gameRulesAsset.minimumShootForce, gameRulesAsset.maximumShootForce, m_currentSliderValue);
        m_cueBallRigidbody.AddForce(-cueStick.transform.up * t_shootForce);
    }

    public void ResetCueBall() {
        // When the cue ball hits a hole, we choose where to put the ball back...
        m_currentGameState = EGameState.Repositioning;
        cueBall.GetComponent<SphereCollider>().enabled = false;
    }

    public void StartBallDestroyed(Transform _ball) {
        m_starBallsList.Remove(_ball);
        Destroy(_ball.gameObject);

        if(m_starBallsList.Count == 0) {
            m_currentGameState = EGameState.GameOver;
            youWinScreen.SetActive(true);
        }
    }
}

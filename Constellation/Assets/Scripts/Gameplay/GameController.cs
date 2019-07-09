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
    public IndividualBallBehavior cueBall;
    private Transform m_cueBallTransform;
    public Transform cueStick;
    public IndividualBallBehavior[] starBalls;
    // The purpose of this list is to (1) Decelerate all balls on the playing and area and (2) check for win condition
    private List<IndividualBallBehavior> m_starBallsList;

    [Header("Cue Stick Line")]
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

    // Mouse Related Stuff
    private const int MOUSE_PRIMARY_BUTTON = 0;

    // State Related
    private EGameState m_currentGameState = EGameState.ReadyToShoot;

    // Constant Values
    // Trying to avoid Magic Numbers (but they are magic numbers)
    private const float k_rotationSpeed = 300f;
    private const float k_cueStickDistanceMultiplier = 2.5f;
    private const float k_cueStickMultiplier = 2.5f;
    private const float k_repositioningMaxRaycastDistance = 100f;

    private void Start() {
        // Caching Components
        m_cueBallTransform = cueBall.transform;
        m_cueBallRigidbody = m_cueBallTransform.GetComponent<Rigidbody>();
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

    private void Update() {
        switch(m_currentGameState) {
            case EGameState.ReadyToShoot:
                // Known Issue: There should be UI elements everywhere the player shouldn't be clicking...
                if(Input.GetMouseButton(MOUSE_PRIMARY_BUTTON) && !EventSystem.current.IsPointerOverGameObject()) {
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
                if(Physics.Raycast(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector3.down, out mouseHitInfo, k_repositioningMaxRaycastDistance)) {
                    m_cueBallTransform.position = mouseHitInfo.point;

                    if (Input.GetMouseButtonDown(MOUSE_PRIMARY_BUTTON)) {
                        m_cueBallTransform.Translate(Vector3.up);
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
        bool hitAPoint = Physics.Raycast(cueBall.transform.position, -cueStick.up, out RaycastHit hitInfo);

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

        cueStick.RotateAround(m_cueBallTransform.position, Vector3.up, t_rotationValue * Time.deltaTime * k_rotationSpeed);

        // The Original Position should be when slider value = 0
        m_cueStickOriginalPosition = cueStick.position - ((cueStick.transform.up * k_cueStickDistanceMultiplier) * m_currentSliderValue);
    }

    private void DecelerateAllBalls() {
        cueBall.DecelerateBall(gameRulesAsset.timeToStopAllBalls);

        foreach(IndividualBallBehavior starBall in m_starBallsList) {
            starBall.DecelerateBall(gameRulesAsset.timeToStopAllBalls);
        }
    }

    private void ResetCueStickPosition() {
        cueStick.position = m_cueBallTransform.position;
        cueStick.Translate(cueStick.up * k_cueStickMultiplier, Space.World);
        m_cueStickOriginalPosition = cueStick.position;
        OffsetCueStickWithSliderValue();
    }

    // CueBehavior interacting with UI elements is not good
    public void SliderValueChanged(Slider _slider) {
        m_currentSliderValue = _slider.value;
        OffsetCueStickWithSliderValue();
    }

    private void OffsetCueStickWithSliderValue() {
        Vector3 t_cueStickMaximumDistance = m_cueStickOriginalPosition + (cueStick.transform.up * k_cueStickDistanceMultiplier);
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
        m_starBallsList.Remove(_ball.GetComponent<IndividualBallBehavior>());
        Destroy(_ball.gameObject);

        if(m_starBallsList.Count == 0) {
            m_currentGameState = EGameState.GameOver;
            youWinScreen.SetActive(true);
        }
    }
}

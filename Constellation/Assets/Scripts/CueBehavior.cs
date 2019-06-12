using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// NEXT STEPS
// => Holes like billiards with trigger detectors... (make a billiards scene)

public class CueBehavior : MonoBehaviour {
    public enum EGameState {
        ReadyToShoot,
        WaitingToShoot,
        TransitioningToReady,
    }

    [Header("Necessary Transforms")]
    public Transform cueBall;
    public Transform cueStick;
    public Transform[] starBalls;

    [Header("Shoot Configuration")]
    public float minimumShootForce = 100f;
    public float maximumShootForce = 15000;

    [Header("Transition Times Related")]
    public float maximumWaitTime = 5f;
    public float timeToStopAllBalls = 2f;
    private float m_timeWaiting = 0f;

    private Rigidbody m_cueBallRigidbody;
    private Vector3 m_cueStickOriginalPosition;
    private float m_currentSliderValue = 0f;

    // Rotation around cue ball
    private const float rotationSpeed = 300f;
    private const float cueStickDistanceMultiplier = 2.5f;

    // Mouse Related Stuff
    private const int MOUSE_PRIMARY_BUTTON = 0;
    private Vector2 m_lastMousePosition;

    // State Related
    private EGameState m_currentGameState = EGameState.ReadyToShoot;

    private void Start() {
        m_cueBallRigidbody = cueBall.GetComponent<Rigidbody>();

        // We keep it kinematic until we want to shoot
        m_cueBallRigidbody.isKinematic = true;

        m_cueStickOriginalPosition = cueStick.position;
    }

    private void OnValidate() {
        // As the game heavily relies on Physics, we want to have everything to interpolate and have continuous collision detection mode.
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
                // Idea 1: Create specific areas the player can click to move...
                // Idea 2: This area has to be near the cue ball...
                if (Input.GetMouseButton(MOUSE_PRIMARY_BUTTON) && !EventSystem.current.IsPointerOverGameObject()) {
                    ProcessCueStickRotation();
                }

                Debug.DrawRay(cueStick.transform.position, -cueStick.up * 10f, Color.blue, 5f);
                break;
            case EGameState.WaitingToShoot:
                // Wait until we can shoot again...
                m_timeWaiting += Time.deltaTime;
                if(m_timeWaiting > maximumWaitTime) {
                    m_currentGameState = EGameState.TransitioningToReady;
                    m_timeWaiting = 0f;
                    DecelerateAllBalls();
                }
                break;
            case EGameState.TransitioningToReady:
                m_timeWaiting += Time.deltaTime;
                if(m_timeWaiting > timeToStopAllBalls) {
                    ResetCueStickPosition();
                    m_currentGameState = EGameState.ReadyToShoot;
                    cueStick.gameObject.SetActive(true);
                }
                break;
        }
    }

    private void ProcessCueStickRotation() {
        Vector3 mousePositionInWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePositionInWorld.y = cueBall.transform.position.y;
        Debug.DrawLine(mousePositionInWorld, cueBall.transform.position, Color.green, 1.0f);

        // Rotate so the stick is parallel with the mouse position
        Vector3 fromBallToStick = cueStick.transform.position - cueBall.transform.position;
        Vector3 fromBallToMouse = mousePositionInWorld - cueBall.transform.position;
        float t_rotationValue = Vector3.SignedAngle(fromBallToStick, fromBallToMouse, Vector3.up) * Mathf.Deg2Rad;

        cueStick.RotateAround(cueBall.position, Vector3.up, t_rotationValue * Time.deltaTime * rotationSpeed);

        // The Original Position should be when slider value = 0
        m_cueStickOriginalPosition = cueStick.position - ((cueStick.transform.up * cueStickDistanceMultiplier) * m_currentSliderValue);

        m_lastMousePosition = Input.mousePosition;
    }

    private Vector2 SnapToNearestCardinal(Vector2 vectorToSnap) {
        if(vectorToSnap == Vector2.zero) {
            return Vector2.zero;
        }

        Vector2 t_returnVector = Vector2.zero;

        if(Mathf.Abs(vectorToSnap.x) > Mathf.Abs(vectorToSnap.y)) {
            t_returnVector.x = Mathf.Sign(vectorToSnap.x);
        } else {
            t_returnVector.y = Mathf.Sign(vectorToSnap.y);
        }

        return t_returnVector;
    }

    private void DecelerateAllBalls() {
        StartCoroutine(DecelerateBallRoutine(m_cueBallRigidbody));
        foreach(Transform t_starBall in starBalls) {
            StartCoroutine(DecelerateBallRoutine(t_starBall.GetComponent<Rigidbody>()));
        }
    }

    private IEnumerator DecelerateBallRoutine(Rigidbody ballRigidbody) {
        Vector3 t_currentBallVelocity = ballRigidbody.velocity;
        Vector3 t_futureBallPosition = Vector3.zero;

        for(float t_timeElapsed = 0f; t_timeElapsed < timeToStopAllBalls; t_timeElapsed += Time.deltaTime) {
            float t = Mathf.Clamp01(t_timeElapsed / timeToStopAllBalls);
            ballRigidbody.velocity = Vector3.Lerp(t_currentBallVelocity, t_futureBallPosition, t);
            yield return null;
        }

        ballRigidbody.velocity = t_futureBallPosition;

        Debug.LogWarning("Ball stopped!");
    }

    private void ResetCueStickPosition() {
        cueStick.position = cueBall.position;
        cueStick.Translate(cueStick.up * 2.5f, Space.World);
        m_cueStickOriginalPosition = cueStick.position;
        OffsetCueStickWithSliderValue();
    }

    // CueBehavior interacting with UI elements is not good
    public void SliderValueChanged(Slider slider) {
        m_currentSliderValue = slider.value;
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
        cueStick.gameObject.SetActive(false);
        m_cueBallRigidbody.isKinematic = false;

        float t_shootForce = Mathf.Lerp(minimumShootForce, maximumShootForce, m_currentSliderValue);
        m_cueBallRigidbody.AddForce(-cueStick.transform.up * t_shootForce);
    }
}

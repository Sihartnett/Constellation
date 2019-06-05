using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CueBehavior : MonoBehaviour {
    public Transform cueBall;
    public Transform cueStick;
    private Rigidbody m_cueBallRigidbody;
    private Vector3 m_cueStickOriginalPosition;
    private float m_currentSliderValue = 0f;

    private const float rotationSpeed = 200f;
    private const float cueStickDistanceMultiplier = 5f;

    private const int MOUSE_PRIMARY_BUTTON = 0;
    private Vector2 m_lastMousePosition;

    private void Start() {
        m_cueBallRigidbody = cueBall.GetComponent<Rigidbody>();

        // We keep it kinematic until we want to shoot
        m_cueBallRigidbody.isKinematic = true;

        m_cueStickOriginalPosition = cueStick.position;
    }

    private void Update() {
        // Known Issue: There should be UI elements everywhere the player shouldn't be clicking...
        // Idea 1: Create specific areas the player can click to move...
        // Idea 2: This area has to be near the cue ball...
        if(Input.GetMouseButton(MOUSE_PRIMARY_BUTTON) && !EventSystem.current.IsPointerOverGameObject()) {
            ProcessCueStickRotation();
        }

        Debug.DrawRay(cueStick.transform.position, cueStick.position + (-cueStick.up * 5f), Color.blue);
    }

    private void ProcessCueStickRotation() {
        Vector2 t_currentMousePosition = Input.mousePosition;
        Vector2 mousePositionDifference = t_currentMousePosition - m_lastMousePosition;
        Vector2 snappedMousePositionDifference = SnapToNearestCardinal(mousePositionDifference);
        float t_rotationValue = (snappedMousePositionDifference.x + snappedMousePositionDifference.y);

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

    // CueBehavior interacting with UI elements is not good
    public void SliderValueChanged(Slider slider) {
        m_currentSliderValue = slider.value;

        Vector3 t_cueStickMaximumDistance = m_cueStickOriginalPosition + (cueStick.transform.up * cueStickDistanceMultiplier);
        cueStick.transform.position = Vector3.Lerp(m_cueStickOriginalPosition, t_cueStickMaximumDistance, m_currentSliderValue);
    }

    public void Shoot() {
        // TO DO
        // Stick should hit the ball with the slider force...
        // Physics? Just add velocity in that direction...
        // Math? Calculate force and direction, play an animation and apply that force to the ball...
    }
}

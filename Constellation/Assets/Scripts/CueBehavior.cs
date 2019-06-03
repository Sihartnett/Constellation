using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CueBehavior : MonoBehaviour {
    public Transform cueBall;
    public Transform cueStick;
    private Vector3 cueStickOriginalPosition;

    private const float rotationSpeed = 200f;
    private const float cueStickDistanceMultiplier = 10f;

    private const int MOUSE_PRIMARY_BUTTON = 0;
    private Vector2 m_lastMousePosition;

    private void Start() {
        cueStickOriginalPosition = cueStick.transform.position;
    }

    private void Update() {
        // Known Issue: This is being activated when UI slider is used
        if(Input.GetMouseButton(MOUSE_PRIMARY_BUTTON)) {
            ProcessCueStickRotation();
        }
    }

    private void ProcessCueStickRotation() {
        Vector2 t_currentMousePosition = Input.mousePosition;
        Vector2 mousePositionDifference = t_currentMousePosition - m_lastMousePosition;
        Vector2 snappedMousePositionDifference = SnapToNearestCardinal(mousePositionDifference);
        float t_rotationValue = (snappedMousePositionDifference.x + snappedMousePositionDifference.y);
        cueBall.Rotate(new Vector3(0, t_rotationValue * Time.deltaTime * rotationSpeed, 0), Space.Self);

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

    // CueBehavior interacting with UI is not good
    public void SliderValueChanged(Slider slider) {
        // The Rotation is weird... fix the thing where cue still rotates when mouse is messing with UI
        Vector3 t_cueStickMaximumDistance = cueStickOriginalPosition + (cueStick.transform.up * cueStickDistanceMultiplier);
        cueStick.transform.position = Vector3.Lerp(cueStickOriginalPosition, t_cueStickMaximumDistance, slider.value);
    }
}

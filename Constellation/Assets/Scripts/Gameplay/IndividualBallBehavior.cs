using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This script is to handle individual ball behavior
// 1. Not letting the ball go up due to collision
public class IndividualBallBehavior : MonoBehaviour {
    private Rigidbody m_rigidbody;

    private void Start() {
        m_rigidbody = GetComponent<Rigidbody>();
    }

    private void OnValidate() {
        Debug.Log($"Validating {this.name}");
        Rigidbody t_rigidbody = GetComponent<Rigidbody>();
        t_rigidbody.interpolation = RigidbodyInterpolation.Extrapolate;
        t_rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
    }

    private void Update() {
        if(m_rigidbody.velocity.y > 0) {
            m_rigidbody.velocity = new Vector3(m_rigidbody.velocity.x, 0f, m_rigidbody.velocity.z);
        }
    }

    public void DecelerateBall(float _timeToStop) {
        StartCoroutine(DecelerateBallRoutine(_timeToStop));
    }

    private IEnumerator DecelerateBallRoutine(float _timeToStop) {
        Vector3 t_currentBallVelocity = m_rigidbody.velocity;
        Vector3 t_currentBallRotationVelocity = m_rigidbody.angularVelocity;
        Vector3 t_futureBallVelocity = Vector3.zero;

        for (float t_timeElapsed = 0f; t_timeElapsed < _timeToStop; t_timeElapsed += Time.deltaTime) {
            float t = Mathf.Clamp01(t_timeElapsed / _timeToStop);
            m_rigidbody.velocity = Vector3.Lerp(t_currentBallVelocity, t_futureBallVelocity, t);
            m_rigidbody.angularVelocity = Vector3.Lerp(t_currentBallRotationVelocity, t_futureBallVelocity, t);
            yield return null;
        }

        m_rigidbody.velocity = t_futureBallVelocity;
        m_rigidbody.angularVelocity = t_futureBallVelocity;
    }
}

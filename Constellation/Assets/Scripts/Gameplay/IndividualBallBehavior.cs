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

    private void Update() {
        if(m_rigidbody.velocity.y > 0) {
            m_rigidbody.velocity = new Vector3(m_rigidbody.velocity.x, 0f, m_rigidbody.velocity.z);
        }
    }
}

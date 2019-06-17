using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DetectBallTrigger : MonoBehaviour {
    private CueBehavior m_cueBehaviorReference;

    private void Start() {
        m_cueBehaviorReference = FindObjectOfType<CueBehavior>();
    }

    private void OnTriggerEnter(Collider other) {
        if(other.tag == "CueBall") {
            m_cueBehaviorReference.ResetCueBall();
        } else if(other.tag == "StarBall") {
            // What happens when a Star Ball hits the hole?
            Destroy(other.gameObject);
        }
    }
}

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
            m_cueBehaviorReference.StartBallDestroyed(other.transform);
        }
    }
}

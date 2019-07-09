using System;
using UnityEngine;

public class DetectBallTrigger : MonoBehaviour {

    public delegate void StartBallDestroyed(Transform _transform);
    public event StartBallDestroyed StarBallOnTrigger;
    public event Action CueBallOnTrigger;

    private void OnTriggerEnter(Collider other) {
        if(other.tag == "CueBall") {
            CueBallOnTrigger?.Invoke();
        } else if(other.tag == "StarBall") {
            StarBallOnTrigger?.Invoke(other.transform);
        }
    }
}

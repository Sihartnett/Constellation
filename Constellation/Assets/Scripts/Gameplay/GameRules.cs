using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GameRules", menuName = "Constellation/Game Rules")]
public class GameRules : ScriptableObject {
    public float minimumShootForce;
    public float maximumShootForce;
    public float maximumWaitTime;
    public float timeToStopAllBalls;
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

[System.Serializable]
public class EmitterMoveBehaviour : EmitterTrackBehaviour
{
    //Put data you want passed to emitter here
    public AnimationCurve moveCurve = AnimationCurve.Linear(0, 0, 1, 1);
    public int curveIndex = -1;

}
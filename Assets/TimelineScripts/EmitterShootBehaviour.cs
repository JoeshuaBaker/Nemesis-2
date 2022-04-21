using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

[System.Serializable]
public class EmitterShootBehaviour : EmitterTrackBehaviour
{
    //Put data you want passed to emitter here
    public int numShots = int.MaxValue;
}
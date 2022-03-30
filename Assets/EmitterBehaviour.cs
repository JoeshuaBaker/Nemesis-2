using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

[System.Serializable]
public class EmitterBehaviour : PlayableBehaviour
{
    [System.NonSerialized] public double start;
    [System.NonSerialized] public double end;

    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        PlayableEmitter emitter = playerData as PlayableEmitter;
        if(emitter != null)
        {
            emitter.PlayableUpdate(playable.GetTime(), start, end);
        }
        base.ProcessFrame(playable, info, playerData);
    }
}

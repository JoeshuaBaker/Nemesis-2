using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

[System.Serializable]
public class EmitterControllerBehaviour : EmitterTrackBehaviour
{
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

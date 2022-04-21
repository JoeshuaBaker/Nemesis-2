using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public class EmitterController : EmitterTrackAsset
{
    public EmitterControllerBehaviour _behaviour;
    public override EmitterTrackBehaviour behaviour {
        get
        {
            return _behaviour;
        }
    }
    
    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        return ScriptPlayable<EmitterControllerBehaviour>.Create(graph, _behaviour);
    }
}

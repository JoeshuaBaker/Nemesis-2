using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public class EmitterMove : EmitterTrackAsset
{

    public EmitterMoveBehaviour _behaviour;
    public override EmitterTrackBehaviour behaviour {
        get
        {
            return _behaviour;
        }
    }

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        return ScriptPlayable<EmitterMoveBehaviour>.Create(graph, _behaviour);
    }
}
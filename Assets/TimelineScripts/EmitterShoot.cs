using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public class EmitterShoot : EmitterTrackAsset
{

    public EmitterShootBehaviour _behaviour;
    public override EmitterTrackBehaviour behaviour {
        get
        {
            return _behaviour;
        }
    }

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        return ScriptPlayable<EmitterShootBehaviour>.Create(graph, _behaviour);
    }
}
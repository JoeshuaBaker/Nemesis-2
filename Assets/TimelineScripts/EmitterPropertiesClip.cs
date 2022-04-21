using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public class EmitterPropertiesClip : EmitterTrackAsset
{

    public EmitterProperties _behaviour;
    public override EmitterTrackBehaviour behaviour {
        get
        {
            return _behaviour;
        }
    }

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        return ScriptPlayable<EmitterProperties>.Create(graph, _behaviour);
    }
}
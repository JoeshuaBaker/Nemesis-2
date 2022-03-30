using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public class EmitterAsset : PlayableAsset
{
    public EmitterBehaviour behaviour;
    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        return ScriptPlayable<EmitterBehaviour>.Create(graph, behaviour);
    }
}

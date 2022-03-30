using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[TrackBindingType(typeof(PlayableEmitter))]
[TrackClipType(typeof(EmitterAsset))]
public class EmitterTrack : TrackAsset
{
    public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
    {       
        foreach (var c in GetClips()) {
            ((EmitterAsset)(c.asset)).behaviour.start = c.start;
            ((EmitterAsset)(c.asset)).behaviour.end = c.end;
        }
        
        return base.CreateTrackMixer(graph, go, inputCount);
    }
}

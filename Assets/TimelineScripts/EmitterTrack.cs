using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEditor;
using BulletHell;

[TrackColor(177f/255f,253f/255f, 89f/255f)]
[TrackBindingType(typeof(PlayableEmitter))]
[TrackClipType(typeof(EmitterTrackAsset))]
public class EmitterTrack : TrackAsset, ILayerable
{
    const string controllerName = "Controller";
    private PlayableDirector director;
    private static bool timelineNeedsRefresh = false;

    public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
    {
        Debug.Log("CREATE TRACK MIXER");
        director = go.GetComponent<PlayableDirector>();
        IEnumerable<TimelineClip> clips = GetClips();

        SetupTrack(clips);

        return base.CreateTrackMixer(graph, go, inputCount);
    }

    private void ProcessTrack(PlayableEmitter emitter, TrackAsset track)
    {
        if(track == null)
            return;

        int i = 0;
        foreach(var clip in track.GetClips())
        {
            Debug.Log($"Processing clip {clip.displayName}");
            //Set clip base properties
            ((EmitterTrackAsset)(clip.asset)).behaviour.start = clip.start;
            ((EmitterTrackAsset)(clip.asset)).behaviour.end = clip.end;
            ((EmitterTrackAsset)(clip.asset)).behaviour.id = i++;

            //Set clip subtype properties
            var clipType = clip.asset.GetType();
            if (clipType == typeof(EmitterPropertiesClip))
            {
                PlayableEmitter.TimelineProperties timelineProps = new PlayableEmitter.TimelineProperties();
                timelineProps.clipProperties = ((EmitterPropertiesClip)(clip.asset))._behaviour;
#if UNITY_EDITOR
                if(clip.curves != null)
                {
                    timelineProps.propertiesCurves = AnimationUtility.GetCurveBindings(clip.curves);
                    timelineProps.animationClip = clip.curves;
                }
#endif
                emitter.timelineProperties.Add(timelineProps);
            }
            else if(clipType == typeof(EmitterShoot))
            {
                emitter.shootBehaviours.Add(((EmitterShoot)(clip.asset))._behaviour);
            }
        }
        foreach(var childTrack in track.GetChildTracks())
        {
            ProcessTrack(emitter, childTrack);
        }
    }
    
    
    protected override void OnBeforeTrackSerialize()
    {
        base.OnBeforeTrackSerialize();
        Debug.Log("ONBEFORETRACKSERIALIZE");
        UnityEditor.Timeline.TimelineEditor.Refresh(UnityEditor.Timeline.RefreshReason.ContentsModified);
        
    }
    

    private void SetupTrack(IEnumerable<TimelineClip> clips)
    {
        if (!isSubTrack)
        {
            //Find controller clip
            TimelineClip controller = null;
            foreach (var clip in clips)
            {
                if (clip.displayName == controllerName)
                {
                    controller = clip;
                }
            }

            //Create controller clip
            if (controller != null)
            {
                controller.start = 0;
                controller.duration = director.duration;
            }
            else
            {
                controller = CreateClip<EmitterController>();
                controller.displayName = controllerName;
                controller.start = 0;
                controller.duration = director.duration;
            }

            //Set emitter lists
            PlayableEmitter emitter = director.GetGenericBinding(this) as PlayableEmitter;
            if (emitter.timelineProperties != null)
                emitter.timelineProperties.Clear();
            else
                emitter.timelineProperties = new List<PlayableEmitter.TimelineProperties>();
            if (emitter.shootBehaviours != null)
                emitter.shootBehaviours.Clear();
            else
                emitter.shootBehaviours = new List<EmitterShootBehaviour>();

            //Setup emitter lists
            ProcessTrack(emitter, this);

            emitter.Build();
            ProjectileManager.Instance.RefreshEmitters();
        }
    }

    Playable ILayerable.CreateLayerMixer(PlayableGraph graph, GameObject go, int inputCount)
    {
        return Playable.Null;
    }
}

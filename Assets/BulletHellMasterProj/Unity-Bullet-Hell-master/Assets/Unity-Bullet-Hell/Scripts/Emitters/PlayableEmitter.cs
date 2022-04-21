using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BulletHell;
using UnityEditor;

public class PlayableEmitter : ProjectileEmitterAdvanced
{   
    public struct TimelineProperties
    {
        public EmitterProperties clipProperties;
        public EditorCurveBinding[] propertiesCurves;
        public AnimationClip animationClip;
    }

    public List<TimelineProperties> timelineProperties;
    public List<EmitterShootBehaviour> shootBehaviours;
    private EmitterProperties baseProps = null;
    private List<(double, EmitterProperties)> shootEvents;

    public void Build()
    {
        //only build state in edit mode because unity's timeline curves are a fucking nightmare i guess
        if(EditorApplication.isPlaying)
            return;

        if(baseProps == null)
            baseProps = props;

        if(shootEvents == null)
            shootEvents = new List<(double, EmitterProperties)>();
        else
            shootEvents.Clear();
        
        foreach(var shoot in shootBehaviours)
        {
            int shots = shoot.numShots;
            double nextShot = shoot.start;

            //Process all shots this clip will make into ShootEvents
            while(nextShot < shoot.end && shots > 0)
            {
                int propIndex = GetPropertiesIndex(nextShot);
                if(propIndex == -1)
                {
                    shootEvents.Add((nextShot, baseProps));
                    nextShot += baseProps.CoolOffTime;
                }
                else
                {
                    TimelineProperties timelineProp = timelineProperties[propIndex];
                    EmitterProperties snapshotProps = timelineProp.clipProperties;

                    if(timelineProp.propertiesCurves != null && timelineProp.animationClip != null)
                    {
                        snapshotProps = snapshotProps.Copy();
                        EditorCurveBinding[] curveBindings = timelineProp.propertiesCurves;
                        AnimationClip animClip = timelineProp.animationClip;

                        foreach(EditorCurveBinding curve in curveBindings)
                        {
                            AnimationCurve animCurve = AnimationUtility.GetEditorCurve(animClip, curve);
                            float f = animCurve.Evaluate(Mathf.Max((float)(nextShot - snapshotProps.start), 0));
                            FieldInfo field = snapshotProps.GetType().GetField(curve.propertyName);
                            if(field != null)
                            {
                                SetFieldSafe(field, snapshotProps, f);
                            }
                        }
                    }
                    

                    shootEvents.Add((nextShot, snapshotProps));
                    nextShot += snapshotProps.CoolOffTime;
                    shots--;
                }
            }
        }
    }

    public void PlayableUpdate(double t, double start, double end)
    {
        if(baseProps == null)
        {
            baseProps = props;
        }

        int propsIndex = -1;
        if((propsIndex = GetPropertiesIndex(t)) != -1)
        {
            props = timelineProperties[propsIndex].clipProperties;
        }
        else 
        {
            props = baseProps;
        }
    }

    private int GetPropertiesIndex(double time)
    {
        time += 0.000000001;
        for(int i = 0; i < timelineProperties.Count; i++)
        {
            if(time >= timelineProperties[i].clipProperties.start && time < timelineProperties[i].clipProperties.end)
            {
                return i;
            }
        }
        return -1;
    }

    private void SetFieldSafe(FieldInfo field, EmitterProperties snapshotProps, float f)
    {
        Type type = field.FieldType;
        if(type == typeof(float))
        {
            field.SetValue(snapshotProps, f);
        }
        else if(type == typeof(double))
        {
            field.SetValue(snapshotProps, (double)f);
        }
        else if(type == typeof(int))
        {
            field.SetValue(snapshotProps, (int)f);
        }
        else if(type == typeof(bool))
        {
            field.SetValue(snapshotProps, (f != 0f));
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;

[Serializable]
public class BezierCurve1D
{
    public List<Keyframe1D> keys;

    public static BezierCurve1D WebExample = new BezierCurve1D(.5f, .25f, .75f, .5f);
    public static BezierCurve1D SmoothStep = new BezierCurve1D(0f, 0f, 1f, 1f);

    public BezierCurve1D() : this(1f, 1f, 1f, 1f)
    {

    }

    public BezierCurve1D(float a, float b, float c, float d)
    {
        keys = new List<Keyframe1D>();
        keys.Add(new Keyframe1D(a, b, c, d, new Vector2(0, 1)));
    }

    public BezierCurve1D(List<Keyframe1D> keys)
    {
        this.keys = keys;
    }

    public void DivideCurve(float t)
    {
        if (t < 0 || t > 1)
            return;

        Keyframe1D key = null;
        int i;
        for(i = 0; i < keys.Count; i++)
        {
            key = keys[i];
            if(t >= key.range.x && t < key.range.y)
            {
                break;
            }
        }

        if (i == keys.Count)
        {
            Debug.Log($"Could not find point {t} in keylist.");
            return;
        }

        float[] abcd = new float[] { key.a, key.b, key.c, key.d };
        float percent = (t - key.range.x) / (key.range.y - key.range.x);
        List<float> left = new List<float>();
        List<float> right = new List<float>();
        Split(abcd, percent, ref left, ref right);
        Keyframe1D newSegment = new Keyframe1D(left[0], left[1], left[2], left[3], new Vector2(key.range.x, t));
        key.range.x = t;
        key.a = right[3];
        key.b = right[2];
        key.c = right[1];
        key.d = right[0];
        keys.Insert(i, newSegment);
    }

    public float GetValue(float t)
    {
        if (t < 0 || t > 1)
            return -1;

        Keyframe1D key = null;
        for (int i = 0; i < keys.Count; i++)
        {
            key = keys[i];
            if (t >= key.range.x && t < key.range.y)
            {
                break;
            }
            else if (i == keys.Count - 1)
            {
                Debug.Log($"Could not find point {t} in keylist.");
                return -1;
            }
        }

        float percent = (t - key.range.x) / (key.range.y - key.range.x);
        return GetValue(key, percent);
    }

    public float GetValue(Keyframe1D segment, float t)
    {
        return GetValueDeCasteljau(new float[] { segment.a, segment.b, segment.c, segment.d }, t);
    }

    private float GetValueDeCasteljau(float[] points, float t)
    {
        if (points.Length == 1)
        {
            return points[0];
        }
        else
        {
            float[] newpoints = new float[points.Length - 1];
            for (int i = 0; i < newpoints.Length; i++)
            {
                newpoints[i] = (1 - t) * points[i] + t * points[i + 1];
            }
            return GetValueDeCasteljau(newpoints, t);
        }
    }

    private void Split(float[] points, float t, ref List<float> left, ref List<float> right)
    {

        if (points.Length == 1)
        {
            left.Add(points[0]);
            right.Add(points[0]);
        }
        else
        {
            float[] newpoints = new float[points.Length - 1];
            for (int i = 0; i < newpoints.Length; i++)
            {
                if (i == 0)
                    left.Add(points[i]);
                if (i == newpoints.Length - 1)
                    right.Add(points[i + 1]);
                newpoints[i] = (1 - t) * points[i] + t * points[i + 1];
            }
            Split(newpoints, t, ref left, ref right);
        }
    }

    public void Remove(Keyframe1D key)
    {
        for(int i = 1; i < keys.Count; i++)
        {
            if (keys[i] == key)
            {
                keys[i - 1].range.y = key.range.y;
                if (i < keys.Count - 1)
                    keys[i - 1].d = keys[i + 1].a;
            }
        }
        keys.Remove(key);
    }

    public float Integrate()
    {
        float sum = 0f;
        foreach(Keyframe1D key in keys)
        {
            sum += IntegrateSegment(key);
        }
        return sum;
    }

    public float Integrate(float t)
    {
        float sum = 0f;
        foreach(Keyframe1D key in keys)
        {
            if(t > key.range.y)
            {
                sum += IntegrateSegment(key);
            }
            else if(t > key.range.x)
            {
                sum += IntegrateSegment(key, t);
            }
            else
            {
                break;
            }
        }
        return sum;
    }

    private float IntegrateSegment(Keyframe1D segment)
    {
        float two = 2f;
        float three = 3f;
        float oneOverFour = .25f;
        float threeOverTwo = 1.5f;
        float a = segment.a;
        float b = segment.b;
        float c = segment.c;
        float d = segment.d;
        float aN = -1 * a;

        //function of 1d bezier curve: A*(1-x)^3+3*B*(1-x)^2*x+3*C*(1-x)*x^2+D*x^3
        //indefinite integral of 1d bezier curve:
        //-(a x^4)/4 + a x^3 - (3 a x^2)/2 + a x + (3 b x^4)/4 - 2 b x^3 + (3 b x^2)/2 - (3 c x^4)/4 + c x^3 + (d x^4)/4
        //Expanded polynomial form: ((-a + 3 b - 3 c + d) x^4)/4 + (a - 2 b + c) x^3 - (3 (a - b) x^2)/2 + a x
        //Can drop x coeffecient, as x=1 for a normalized curve
        //(-a + 3 b - 3 c + d)/4 + (a - 2 b + c) - (3 (a - b))/2 + a
        return segment.width * 
            (oneOverFour * (aN + three * b - three * c + d)
            + (a - two * b + c)
            - threeOverTwo * (a - b)
            + a);
    }

    private float IntegrateSegment(Keyframe1D key, float t)
    {
        float[] abcd = new float[] { key.a, key.b, key.c, key.d };
        float percent = (t - key.range.x) / (key.range.y - key.range.x);
        List<float> left = new List<float>();
        List<float> right = new List<float>();
        Split(abcd, percent, ref left, ref right);
        Keyframe1D newSegment = new Keyframe1D(left[0], left[1], left[2], left[3], new Vector2(key.range.x, t));
        return IntegrateSegment(newSegment);
    }

    private float IntegrateSin(Keyframe1D key, float maxAngle = 180f)
    {

        float A = key.a * maxAngle;
        float B = key.b * maxAngle;
        float C = key.c * maxAngle;
        float D = key.d * maxAngle;

        return IntegrateTrig(A, B, C, D) * key.width;

    }

    private float IntegrateCosin(Keyframe1D key, float maxAngle = 180f)
    {
        float half = 0.5f;
        float A = (half - key.a) * maxAngle;
        float B = (half - key.b) * maxAngle;
        float C = (half - key.c) * maxAngle;
        float D = (half - key.d) * maxAngle;

        return IntegrateTrig(A, B, C, D) * key.width;
    }

    private float IntegrateTrig(float A, float B, float C, float D)
    {
        float two = 2f;
        float four = 4f;
        float oneOverFour = 0.25f;
        float threeOverTwo = 1.5f;
        float threeOverFour = 0.75f;
        float inside = (-A * oneOverFour + A - A * threeOverTwo + A + B * threeOverFour - two * B + B * threeOverTwo - C * threeOverFour + C + D / four);
        
        return
            - 0.139233f * Mathf.Pow(inside, 3)
            + 0.656118f * Mathf.Pow(inside, 2)
            - 0.050465f * inside;
    }
}

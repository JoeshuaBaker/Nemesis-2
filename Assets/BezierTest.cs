using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[ExecuteAlways]
public class BezierTest : MonoBehaviour
{
    public AnimationCurve testCurve;
    public BezierCurve1D testCurve1D = new BezierCurve1D(0, 1f / 3f, 2f / 3f, 1f);
    public BezierCurve1D rotation = new BezierCurve1D(0.5f, 0.75f, 0.25f, 0.5f);
    public BezierCurve1D speed = new BezierCurve1D(0f, 0f, 1f, 1f);
    public bool evaluate = false;
    public float time = 0.5f;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    void Update()
    {
        if(evaluate)
        {
            Debug.Log($"Total area under test curve: {testCurve1D.Integrate()}");
            Debug.Log($"Area under test curve to t: {testCurve1D.Integrate(time)}");
            /*
            int i = 0;
            foreach (Keyframe kf in testCurve.keys)
            {
                //Debug.Log($"Keyframe {i++}: Time - {kf.time}, Value - {kf.value}, inTangent - {kf.inTangent}, outTangent - {kf.outTangent}, inWeight - {kf.inWeight}, outWeight - {kf.outWeight}");
            }
            Keyframe K1 = testCurve.keys[0];
            Keyframe K2 = testCurve.keys[testCurve.keys.Length - 1];
            Vector2 A = new Vector2(K1.time, K1.value);
            Vector2 D = new Vector2(K2.time, K2.value);
            float e = (D.x - A.x) / 3.0f;
            float f = 1.0f;
            Vector2 B = A + new Vector2(e, e * f * K1.outTangent);
            Vector2 C = D + new Vector2(-e, -e * f * K2.inTangent);

            float a, b, c, d;
            a = -A.y + 3.0f * B.y - 3.0f * C.y + D.y;
            b = 3.0f * A.y - 6.0f * B.y + 3.0f * C.y;
            c = -3.0f * A.y + 3.0f * B.y;
            d = A.y;

            float slope = (3.0f * a * time * time) + (2.0f * b * time) + c;

            int index = testCurve.AddKey(new Keyframe(time, testCurve.Evaluate(time), slope, slope, 1f/3f, 1f/3f));
            
            Debug.Log("New key index is: " + index);
            foreach(Keyframe kf in testCurve.keys)
            {
                //Debug.Log($"Keyframe {i++}: Time - {kf.time}, Value - {kf.value}, inTangent - {kf.inTangent}, outTangent - {kf.outTangent}, inWeight - {kf.inWeight}, outWeight - {kf.outWeight}");
            }
            Debug.Log("Total area under curve: " + AreaUnderCurve(testCurve, 1.0f, 1.0f));
            Debug.Log("Area under curve to new key: " + AreaUnderCurve(testCurve, 1.0f, 1.0f, index));
            AreaUnderCurve(testCurve, 1.0f, 1.0f, index);
            */
            evaluate = false;
        }
    }

    public float AreaUnderCurve(AnimationCurve curve, float w, float h, int toKeyIndex = int.MaxValue)
    {
        float areaUnderCurve = 0f;
        var keys = curve.keys;

        for (int i = 0; i < keys.Length - 1 && i < toKeyIndex; i++)
        {
            // Calculate the 4 cubic Bezier control points from Unity AnimationCurve (a hermite cubic spline) 
            Keyframe K1 = keys[i];
            Keyframe K2 = keys[i + 1];
            Vector2 A = new Vector2(K1.time * w, K1.value * h);
            Vector2 D = new Vector2(K2.time * w, K2.value * h);
            float e = (D.x - A.x) / 3.0f;
            float f = h / w;
            Vector2 B = A + new Vector2(e, e * f * K1.outTangent);
            Vector2 C = D + new Vector2(-e, -e * f * K2.inTangent);

            /*
             * The cubic Bezier curve function looks like this:
             * 
             * f(x) = A(1 - x)^3 + 3B(1 - x)^2 x + 3C(1 - x) x^2 + Dx^3
             * 
             * Where A, B, C and D are the control points and, 
             * for the purpose of evaluating an instance of the Bezier curve, 
             * are constants. 
             * 
             * Multiplying everything out and collecting terms yields the expanded polynomial form:
             * f(x) = (-A + 3B -3C + D)x^3 + (3A - 6B + 3C)x^2 + (-3A + 3B)x + A
             * 
             * If we say:
             * a = -A + 3B - 3C + D
             * b = 3A - 6B + 3C
             * c = -3A + 3B
             * d = A
             * 
             * Then we have the expanded polynomal:
             * f(x) = ax^3 + bx^2 + cx + d
             * 
             * Whos indefinite integral is:
             * a/4 x^4 + b/3 x^3 + c/2 x^2 + dx + E
             * Where E is a new constant introduced by integration.
             * 
             * The indefinite integral of the quadratic Bezier curve is:
             * (-A + 3B - 3C + D)/4 x^4 + (A - 2B + C) x^3 + 3/2 (B - A) x^2 + Ax + E
             */

            float a, b, c, d;
            a = -A.y + 3.0f * B.y - 3.0f * C.y + D.y;
            b = 3.0f * A.y - 6.0f * B.y + 3.0f * C.y;
            c = -3.0f * A.y + 3.0f * B.y;
            d = A.y;

            /* 
             * a, b, c, d, now contain the y component from the Bezier control points.
             * In other words - the AnimationCurve Keyframe value * h data!
             * 
             * What about the x component for the Bezier control points - the AnimationCurve
             * time data?  We will need to evaluate the x component when time = 1.
             * 
             * x^4, x^3, X^2, X all equal 1, so we can conveniently drop this coeffiecient.
             * 
             * Lastly, for each segment on the AnimationCurve we get the time difference of the 
             * Keyframes and multiply by w.
             * 
             * Iterate through the segments and add up all the areas for 
             * the total area under the AnimationCurve!
             */

            float t = (K2.time - K1.time) * w;

            // Define A and D...

            float area;  // just renamed area since it also works for negative values

            // If this portion of the curve is constant (i.e. either this key has a right tangent constant,
            // or the next key has a left tangent constant), compute the integral directly as the signed area of a rectangle
            if (float.IsInfinity(K1.outTangent) || float.IsInfinity(K2.inTangent))
            {
                area = A.y * (D.x - A.x);
            }
            else
            {
                // computation for non-constant tangents...
                area = ((a / 4.0f) + (b / 3.0f) + (c / 2.0f) + d) * t;
            }
            areaUnderCurve += area;

        }
        return areaUnderCurve;
    }
}

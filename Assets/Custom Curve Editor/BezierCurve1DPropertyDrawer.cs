using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Reflection;
using UnityEditor.ShaderGraph.Drawing.Inspector;
using UnityEditor.UIElements;
using UnityEditor.PackageManager.UI;

[CustomPropertyDrawer(typeof(BezierCurve1D))]

public class BezierCurve1DPropertyDrawer : PropertyDrawer
{
    Material mat;
    BezierCurve1D curve;
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        curve = PropertyDrawerUtility.GetValue(property) as BezierCurve1D;
        
        if(mat == null)
        {
            var shader = Shader.Find("Hidden/Internal-Colored");
            mat = new Material(shader);
        }
        label = EditorGUI.BeginProperty(position, label, property);
        Rect contentPosition = EditorGUI.PrefixLabel(position, label);
        EditorGUI.indentLevel = 0;

        if (curve != null)
        {
            if(curve.keys == null)
            {
                Debug.Log("Curve keys are null.");
                GUI.EndClip();
                EditorGUI.EndProperty();
                return;
            }
            Rect rect = contentPosition;
            if (Event.current.type == EventType.Repaint)
            {
                GUI.BeginClip(rect);
                GL.PushMatrix();
                GL.Clear(true, false, Color.black);
                mat.SetPass(0);

                GL.Begin(GL.QUADS);
                GL.Color(Color.black);
                GL.Vertex3(0, 0, 0);
                GL.Vertex3(0, rect.height, 0);
                GL.Vertex3(rect.width, rect.height, 0);
                GL.Vertex3(rect.width, 0, 0);
                GL.End();

                GL.Begin(GL.LINES);
                GL.Color(Color.green);

                
                Vector3 prevPos = new Vector3(0, curve.GetValue(0), 0);
                float step = 1f / rect.width;
                for (float t = step; t < 1; t += step)
                {
                    Vector3 pos = new Vector3(t, curve.GetValue(t), 0);
                    GL.Vertex3(prevPos.x * rect.width, (1f - prevPos.y) * rect.height, prevPos.z);
                    GL.Vertex3(pos.x * rect.width, (1f - pos.y) * rect.height, pos.z);

                    prevPos = pos;
                }

                GL.End();
                GL.PopMatrix();
                GUI.EndClip();
                
            }
            if(Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
            {
                Bezier1DEditor.Init();
                Bezier1DEditor.window.curve = curve;
                Bezier1DEditor.window.ShowNodes();
            }
        }
        else
        {
            Debug.Log("curve is null.");
        }

        EditorGUI.EndProperty();
    }
}

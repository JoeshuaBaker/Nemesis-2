using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Haze;

public class MakeCurveMesh : MonoBehaviour
{
    public int resolution = 25;
    private int lastResolution = -1;
    BezierCurve path;
    private Vector2 center;
    // Start is called before the first frame update
    void Start()
    {
        
    }  

    // Update is called once per frame
    void Update()
    {
        if(resolution != lastResolution)
        {
            path = GetComponentInChildren<BezierCurve>();
            List<Vector3> vertices = new List<Vector3>();
            List<int> indices = new List<int>();
            List<Vector2> points = new List<Vector2>();
            for(int i = 0; i < resolution; i++)
            {
                Vector3 threeDpoint = path.GetPointAt((float)i/(float)resolution);
                points.Add(new Vector2(threeDpoint.x, threeDpoint.y));
            }

            foreach(var point in path.GetAnchorPoints())
            {
                points.Add(new Vector2(point.position.x, point.position.y));
            }

            center = new Vector2();
            foreach(var point in points)
            {
                center += point;
            }
            center /= points.Count;
            points.Sort(ClockwiseInt);

            Triangulator.AddTrianglesToMesh(ref vertices, ref indices, Triangulator.Triangulate(points), 0.0f, true);

            Mesh mesh = new Mesh();
            MeshFilter mf = GetComponent<MeshFilter>();
            mesh.vertices = vertices.ToArray();
            mesh.triangles = indices.ToArray();
            mf.mesh = mesh;
            lastResolution = resolution;
        }
    }

    private int ClockwiseInt(Vector2 a, Vector2 b) 
    {
        if (a == b) return 0;
        return (Clockwise(a, b)) ? -1 : 1;
    }

    private bool Clockwise(Vector2 a, Vector2 b)
    {
        if (a.x - center.x >= 0 && b.x - center.x < 0)
            return true;
        if (a.x - center.x < 0 && b.x - center.x >= 0)
            return false;
        if (a.x - center.x == 0 && b.x - center.x == 0) {
            if (a.y - center.y >= 0 || b.y - center.y >= 0)
                return a.y > b.y;
            return b.y > a.y;
        }

        // compute the cross product of vectors (center -> a) x (center -> b)
        float det = (a.x - center.x) * (b.y - center.y) - (b.x - center.x) * (a.y - center.y);
        if (det < 0)
            return true;
        if (det > 0)
            return false;

        // points a and b are on the same line from the center
        // check which point is closer to the center
        float d1 = (a.x - center.x) * (a.x - center.x) + (a.y - center.y) * (a.y - center.y);
        float d2 = (b.x - center.x) * (b.x - center.x) + (b.y - center.y) * (b.y - center.y);
        return d1 > d2;

    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Haze;

public class MakeCurveMesh : MonoBehaviour
{
    public int resolution = 25;
    BezierCurve path;
    // Start is called before the first frame update
    void Start()
    {
        
    }  

    // Update is called once per frame
    void Update()
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
        Triangulator.AddTrianglesToMesh(ref vertices, ref indices, Triangulator.Triangulate(points), 0.0f, true);

        Mesh mesh = new Mesh();
        MeshFilter mf = GetComponent<MeshFilter>();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = indices.ToArray();
        mf.mesh = mesh;
    }
}

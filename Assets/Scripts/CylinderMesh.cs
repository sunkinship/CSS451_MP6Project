using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class GeneralCylinderMesh : MonoBehaviour
{
    [Header("Profile (radius, height) from bottom to top)")]
    public List<Vector2> profile = new List<Vector2>() {
        new Vector2(0.5f, -0.5f),
        new Vector2(0.5f,  0.5f)
    };


    [Range(4, 20)]
    public int resolution = 4;          
    [Range(10f, 360f)]
    public float sweepDegrees = 360f;   

    [Header("Editor")]
    public bool generateOnValidate = true;

    MeshFilter mf;
    Mesh mesh;
    //more to ignore
    //bool initialized;
    //Vector3[] generatedVerticesLocal = null;

    void Awake()
    {
        //initialization
        mf = GetComponent<MeshFilter>();
        mesh = mf.sharedMesh ?? new Mesh() { name = "GeneralCylinderMesh" };
        mf.sharedMesh = mesh;
        GenerateMesh();
    }

    public void SetResolution(int r)
    {
        resolution = r;
        GenerateMesh();
    }

    public void SetSweepDegrees(float deg)
    {
        sweepDegrees = Mathf.Clamp(deg, 10f, 360f);
        GenerateMesh();
    }

    void GenerateMesh()
    {
        mesh.Clear();
        
        List<Vector2> ringsProfile = ResampleProfileToRows(profile, resolution);

        float sweepRad = Mathf.Deg2Rad * sweepDegrees;

        int vertCount = resolution * resolution;
        Vector3[] verts = new Vector3[vertCount];
        Vector2[] uvs = new Vector2[vertCount];
        List<int> tris = new List<int>((resolution - 1) * (resolution - 1) * 6);
        float angleStep = (resolution == 1) ? 0f : sweepRad / (resolution - 1);

        for (int i = 0; i < resolution; ++i)
        {
            float angle = i * angleStep;
            float cos = Mathf.Cos(angle);
            float sin = Mathf.Sin(angle);

            for (int j = 0; j < resolution; ++j)
            {
                
                Vector2 rp = ringsProfile[j];
                float r = rp.x;
                float y = rp.y;

                int idx = i * resolution + j; 
                float x = cos * r;
                float z = sin * r;
                verts[idx] = new Vector3(x, y, z);

                float u = (resolution == 1) ? 0f : (float)i / (resolution - 1);
                float v = (resolution == 1) ? 0f : (float)j / (resolution - 1);
                uvs[idx] = new Vector2(u, v);
            }
        }

        for (int i = 0; i < resolution - 1; ++i)
        {
            for (int j = 0; j < resolution - 1; ++j)
            {
                int a = i * resolution + j;
                int b = (i + 1) * resolution + j;
                int c = (i + 1) * resolution + (j + 1);
                int d = i * resolution + (j + 1);

                tris.Add(a); tris.Add(b); tris.Add(c);
                tris.Add(a); tris.Add(c); tris.Add(d);
            }
        }

        mesh.vertices = verts;
        mesh.uv = uvs;
        mesh.triangles = tris.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        //generatedVerticesLocal = mesh.vertices;

        mf.sharedMesh = mesh;

        var vsm = GetComponent<CylinderVertexSelectionManager>();
        if (vsm != null) vsm.Rebuild();
    }

    List<Vector2> ResampleProfileToRows(List<Vector2> baseProfile, int rows)
    {
        List<Vector2> outp = new List<Vector2>();
        if (baseProfile == null || baseProfile.Count == 0) return outp;
        if (rows <= 1) { outp.Add(baseProfile[0]); return outp; }
        if (baseProfile.Count == 1)
        {
            for (int i = 0; i < rows; ++i) outp.Add(baseProfile[0]);
            return outp;
        }

        int segCount = baseProfile.Count - 1;
        float[] segLen = new float[segCount];
        float total = 0f;
        for (int i = 0; i < segCount; ++i)
        {
            segLen[i] = Vector2.Distance(baseProfile[i], baseProfile[i + 1]);
            total += segLen[i];
        }
        if (total <= 0f)
        {
            for (int i = 0; i < rows; ++i) outp.Add(baseProfile[0]);
            return outp;
        }

        for (int r = 0; r < rows; ++r)
        {
            float t = (float)r / (rows - 1);
            float target = t * total;
            float accum = 0f;
            int seg = 0;
            while (seg < segCount - 1 && accum + segLen[seg] < target)
            {
                accum += segLen[seg];
                seg++;
            }
            float localT = (segLen[seg] == 0f) ? 0f : (target - accum) / segLen[seg];
            Vector2 p = Vector2.Lerp(baseProfile[seg], baseProfile[seg + 1], localT);
            outp.Add(p);
        }
        return outp;
    }

    //messing around here
    /*    public Vector3 GetVertexLocal(int index)
        {
            if (generatedVerticesLocal == null || index < 0 || index >= generatedVerticesLocal.Length) return Vector3.zero;
            return generatedVerticesLocal[index];
        }
        public Vector3 GetVertexWorld(int index) => transform.TransformPoint(GetVertexLocal(index));

        public int VertexIndex(int col, int row)
        {
            col = Mathf.Clamp(col, 0, resolution - 1);
            row = Mathf.Clamp(row, 0, resolution - 1);
            return col * resolution + row;
        }
        void OnValidate()
        {
            resolution = Mathf.Max(2, resolution);
            sweepDegrees = Mathf.Clamp(sweepDegrees, 10f, 360f);

            if (generateOnValidate)
            {
                if (mf == null) mf = GetComponent<MeshFilter>();
                if (mf.sharedMesh == null) mf.sharedMesh = new Mesh() { name = "GeneralCylinderMesh" };
                mesh = mf.sharedMesh;
                GenerateMesh();
            }
            else if (Application.isPlaying && initialized)
            {
                GenerateMesh();
            }
        }
    */
}

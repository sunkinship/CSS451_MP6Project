using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class CylinderController : MonoBehaviour
{

    //Had a lot of help from ChatGPT and GitHub Copilot to build this class as you will see
    //I was trying to make the cylindercontroller a subclass of meshcontroller but it got too messy so I made it its own class
    //please take a look at all of it and see if you can subclass it easier.
    //Only big bug remaining is the z-axis not working properly at all.
    //To test the bug, run program, switch to cylinder mode, show controllers, move a controller on the z-axis, see that the mesh deforms incorrectly.
    public static CylinderController Instance { get; private set; }
    public static Action onMeshUpdated;

    [Header("Profile (radius,height) from bottom to top)")]
    public List<Vector2> profile = new List<Vector2>()
    {
        new Vector2(0.5f, -0.5f),
        new Vector2(0.5f,  0.5f)
    };

    [Header("Sweep settings")]
    [Range(4, 20)]
    [SerializeField] private int resolution = 4;
    [Range(10f, 360f)]
    [SerializeField] private float sweepDegrees = 360f;  

    [Header("Controller visuals")]
    public GameObject controllerPrefab;
    public float controllerVisualScale = 0.25f;
    public int selectableColumn = 0;
    public Color selectableColor = Color.white;
    public Color unselectableColor = Color.black;

    MeshFilter mf;
    Mesh mesh;
    GameObject[] wrappers;     // wrapper per vertex (parented to this transform)
    GameObject[] normalPivots; // pivot for normal visuals
    float[] originalAngle;     // angle around Y (radians)
    float[] originalRadius;    // radial distance in XZ
    float[] originalY;         // original Y (height)
    float[] rowRadiusOffset;
    float[] rowYOffset;

    public bool ControllersVisible { get; private set; } = false;
    public void SetResolution(float f) => SetResolution(Mathf.RoundToInt(f));
    public void SetResolution(int r)
    {
        resolution = Mathf.Clamp(r, 4, 20);
        GenerateMesh();
    }

    public void SetSweepDegrees(float deg)
    {
        sweepDegrees = Mathf.Clamp(deg, 10f, 360f);
        GenerateMesh();
    }

    public void Regenerate() => GenerateMesh();
    public int GetResolution() => resolution;
    public float GetSweepDegrees() => sweepDegrees;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        mf = GetComponent<MeshFilter>();
        mesh = mf.sharedMesh ?? new Mesh() { name = "CylinderMesh" };
        mf.sharedMesh = mesh;
        GenerateMesh();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void GenerateMesh()
    {
        if (wrappers != null && originalRadius != null && originalY != null)
        {
            int colsPrev = resolution; 
            int rowsPrev = Mathf.Max(1, originalRadius.Length / colsPrev);
            float[] tempRowR = new float[rowsPrev];
            float[] tempRowY = new float[rowsPrev];
            for (int r = 0; r < rowsPrev; ++r)
            {
                int idxFirstCol = r;
                if (idxFirstCol < wrappers.Length && wrappers[idxFirstCol] != null)
                {
                    var pos = wrappers[idxFirstCol].transform.localPosition;
                    float currRadius = new Vector2(pos.x, pos.z).magnitude;
                    tempRowR[r] = currRadius - originalRadius[idxFirstCol];
                    tempRowY[r] = pos.y - originalY[idxFirstCol];
                }
                else
                {
                    tempRowR[r] = 0f;
                    tempRowY[r] = 0f;
                }
            }
            rowRadiusOffset = tempRowR;
            rowYOffset = tempRowY;
        }

        if (mesh == null) mesh = new Mesh() { name = "CylinderMesh" };
        mesh.Clear();

        // Resample profile into 'resolution' rows
        List<Vector2> ringsProfile = ResampleProfileToRows(profile, resolution);

        float sweepRad = Mathf.Deg2Rad * sweepDegrees;

        int cols = resolution; // radial subdivisions
        int rows = resolution; // vertical (resampled profile)
        int vertCount = cols * rows;

        Vector3[] verts = new Vector3[vertCount];
        Vector2[] uvs = new Vector2[vertCount];
        List<int> tris = new List<int>((rows - 1) * (cols - 1) * 6);
        float angleStep = (cols == 1) ? 0f : sweepRad / (cols - 1);

        for (int i = 0; i < cols; ++i)
        {
            float angle = i * angleStep;
            float cos = Mathf.Cos(angle);
            float sin = Mathf.Sin(angle);

            for (int j = 0; j < rows; ++j)
            {
                Vector2 rp = ringsProfile[j];
                float r = rp.x;
                float y = rp.y;

                int idx = i * rows + j;
                float x = cos * r;
                float z = sin * r;
                verts[idx] = new Vector3(x, y, z);

                float u = (cols == 1) ? 0f : (float)i / (cols - 1);
                float v = (rows == 1) ? 0f : (float)j / (rows - 1);
                uvs[idx] = new Vector2(u, v);
            }
        }

        for (int i = 0; i < cols - 1; ++i)
        {
            for (int j = 0; j < rows - 1; ++j)
            {
                int a = i * rows + j;
                int b = (i + 1) * rows + j;
                int c = (i + 1) * rows + (j + 1);
                int d = i * rows + (j + 1);

                tris.Add(a); tris.Add(b); tris.Add(c);
                tris.Add(a); tris.Add(c); tris.Add(d);
            }
        }

        mesh.vertices = verts;
        mesh.uv = uvs;
        mesh.triangles = tris.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        mf.sharedMesh = mesh;
        RebuildControllers();
        onMeshUpdated?.Invoke();
    }
    List<Vector2> ResampleProfileToRows(List<Vector2> baseProfile, int rows)
    {
        var outp = new List<Vector2>();
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

    void RebuildControllers()
    {
        if (wrappers != null)
        {
            for (int i = 0; i < wrappers.Length; ++i) if (wrappers[i] != null) Destroy(wrappers[i]);
        }

        mesh = mf.sharedMesh;
        Vector3[] verts = mesh.vertices;
        Vector3[] normals = mesh.normals;
        int cols = resolution;
        int rows = Mathf.Max(1, verts.Length / cols); 
        int selectableCol = Mathf.Clamp(selectableColumn, 0, Mathf.Max(0, cols - 1));

        wrappers = new GameObject[verts.Length];
        normalPivots = new GameObject[verts.Length];
        originalAngle = new float[verts.Length];
        originalRadius = new float[verts.Length];
        originalY = new float[verts.Length];

        float sweepRad = Mathf.Deg2Rad * sweepDegrees;
        float angleStep = (cols == 1) ? 0f : sweepRad / (cols - 1);

        for (int i = 0; i < verts.Length; ++i)
        {
            Vector3 localPos = verts[i];

            GameObject wrapper = new GameObject($"CylWrap_{i}");
            wrapper.transform.SetParent(transform, false);
            wrapper.transform.localPosition = localPos;
            wrappers[i] = wrapper;

            float angle, radius;
            int radialIndex = rows > 0 ? (i / rows) : 0;
            angle = radialIndex * angleStep;
            radius = new Vector2(localPos.x, localPos.z).magnitude;
            originalAngle[i] = angle;
            originalRadius[i] = radius;
            originalY[i] = localPos.y;

            GameObject visual;
            if (controllerPrefab != null)
            {
                visual = Instantiate(controllerPrefab, wrapper.transform, false);
                visual.transform.localPosition = Vector3.zero;
            }
            else
            {
                visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                visual.transform.SetParent(wrapper.transform, false);
                visual.transform.localPosition = Vector3.zero;
            }

            visual.transform.localScale = Vector3.one * controllerVisualScale;

            int column = rows > 0 ? (i / rows) : 0;
            bool selectable = (column == selectableCol);

            Collider[] childColliders = visual.GetComponentsInChildren<Collider>(true);
            if (childColliders == null || childColliders.Length == 0)
            {
                var newCol = visual.AddComponent<SphereCollider>();
                newCol.isTrigger = false;
                childColliders = new Collider[] { newCol };
            }
            foreach (var c in childColliders)
            {
                c.isTrigger = false;
                c.enabled = selectable;
            }

            Rigidbody[] rbs = visual.GetComponentsInChildren<Rigidbody>(true);
            if (rbs == null || rbs.Length == 0)
            {
                var rbTop = visual.AddComponent<Rigidbody>();
                rbTop.isKinematic = true;
                rbTop.useGravity = false;
            }
            else
            {
                foreach (var rb in rbs)
                {
                    rb.isKinematic = true;
                    rb.useGravity = false;
                }
            }

            Renderer[] rends = visual.GetComponentsInChildren<Renderer>(true);
            foreach (var r in rends)
            {
                Material m = new Material(Shader.Find("Standard"));
                m.color = selectable ? selectableColor : unselectableColor;
                r.material = m;
            }

            if (normals != null && i < normals.Length)
            {
                Vector3 n = normals[i];
                Vector3 radial = new Vector3(localPos.x, 0f, localPos.z);
                if (radial.sqrMagnitude > 1e-6f)
                {
                    radial.Normalize();
                    if (Vector3.Dot(n, radial) < 0f) n = -n;
                }
                else n = Vector3.up;

                GameObject pivot = new GameObject("NormalPivot");
                pivot.transform.SetParent(visual.transform, false);
                pivot.transform.localPosition = Vector3.zero;
                pivot.transform.localRotation = Quaternion.FromToRotation(Vector3.up, n);

                GameObject normalLine = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                normalLine.transform.SetParent(pivot.transform, false);
                normalLine.transform.localPosition = new Vector3(0f, 1f, 0f);
                normalLine.transform.localScale = new Vector3(0.04f, 1.15f, 0.04f);
                var ncol = normalLine.GetComponent<Collider>();
                if (ncol != null) Destroy(ncol);

                normalPivots[i] = pivot;
            }

            visual.SetActive(false);
        }

        if (rowRadiusOffset != null && rowYOffset != null && rowRadiusOffset.Length == rows)
        {
            Vector3[] vertsModified = new Vector3[verts.Length];
            for (int idx = 0; idx < verts.Length; ++idx)
            {
                int row = idx % rows;
                float angle = originalAngle[idx];
                float targetRadius = originalRadius[idx] + rowRadiusOffset[row];
                float ny = originalY[idx] + rowYOffset[row];
                float nx = targetRadius * Mathf.Cos(angle);
                float nz = targetRadius * Mathf.Sin(angle);
                Vector3 newLocal = new Vector3(nx, ny, nz);

                if (wrappers[idx] != null)
                    wrappers[idx].transform.localPosition = newLocal;
                vertsModified[idx] = newLocal;
            }

            mesh.vertices = vertsModified;
            RecalculateNormals(mesh);
            mesh.RecalculateBounds();
            mf.sharedMesh = mesh;
        }
        ControllersVisible = false;
        VertController.onMovedAxis -= MeshModified;
        VertController.onMovedAxis += MeshModified;
    }

    public void ShowAllControllers()
    {
        if (wrappers == null) return;
        ControllersVisible = true;
        foreach (var w in wrappers)
        {
            if (w == null) continue;
            if (w.transform.childCount > 0)
                w.transform.GetChild(0).gameObject.SetActive(true);
        }
    }

    public void HideAllControllers()
    {
        if (wrappers == null) return;
        ControllersVisible = false;
        foreach (var w in wrappers)
        {
            if (w == null) continue;
            if (w.transform.childCount > 0)
                w.transform.GetChild(0).gameObject.SetActive(false);
        }
    }
    public void MeshModified()
    {
        if (mesh == null) mesh = mf.sharedMesh;
        if (mesh == null || wrappers == null) return;

        Vector3[] verts = mesh.vertices;
        int cols = resolution;
        int rows = Mathf.Max(1, verts.Length / cols);

        //ChatGPT (wasn't sure how to do this so asked AI, will add proper note later if we keep it)
        for (int i = 0; i < wrappers.Length && i < verts.Length; ++i)
        {
            if (wrappers[i] == null) continue;
            verts[i] = wrappers[i].transform.localPosition;
        }

        for (int movedIndex = 0; movedIndex < wrappers.Length; ++movedIndex)
        {
            if (wrappers[movedIndex] == null) continue;
            Vector3 movedPos = wrappers[movedIndex].transform.localPosition;
            int row = movedIndex % rows;
            float newRadius = new Vector2(movedPos.x, movedPos.z).magnitude;
            float deltaRadius = newRadius - originalRadius[movedIndex];
            float deltaY = movedPos.y - originalY[movedIndex];

            for (int c = 0; c < cols; ++c)
            {
                int idx = row + c * rows;
                if (idx < 0 || idx >= verts.Length) continue;
                if (wrappers[idx] == null) continue;
                float targetRadius = originalRadius[idx] + deltaRadius;
                float angle = originalAngle[idx];
                float nx = targetRadius * Mathf.Cos(angle);
                float nz = targetRadius * Mathf.Sin(angle);
                float ny = originalY[idx] + deltaY;

                Vector3 newLocal = new Vector3(nx, ny, nz);
                wrappers[idx].transform.localPosition = newLocal;
                verts[idx] = newLocal;
            }
        }
        mesh.vertices = verts;
        RecalculateNormals(mesh);
        mesh.RecalculateBounds();
        if (normalPivots != null)
        {
            Vector3[] normals = mesh.normals;
            for (int i = 0; i < normalPivots.Length && i < normals.Length; ++i)
            {
                if (normalPivots[i] == null) continue;
                Vector3 localPos = verts[i];
                Vector3 radial = new Vector3(localPos.x, 0f, localPos.z);
                Vector3 n = normals[i];
                if (radial.sqrMagnitude > 1e-6f)
                {
                    radial.Normalize();
                    if (Vector3.Dot(n, radial) < 0f) n = -n;
                }
                else n = Vector3.up;

                normalPivots[i].transform.localRotation = Quaternion.FromToRotation(Vector3.up, n);
            }
        }
        if (rowRadiusOffset == null || rowRadiusOffset.Length != rows)
            rowRadiusOffset = new float[rows];
        if (rowYOffset == null || rowYOffset.Length != rows)
            rowYOffset = new float[rows];

        for (int r = 0; r < rows; ++r)
        {
            int idxFirstCol = r; // column 0 index for row r
            if (idxFirstCol < wrappers.Length && wrappers[idxFirstCol] != null)
            {
                Vector3 pos = wrappers[idxFirstCol].transform.localPosition;
                float currRadius = new Vector2(pos.x, pos.z).magnitude;
                rowRadiusOffset[r] = currRadius - originalRadius[idxFirstCol];
                rowYOffset[r] = pos.y - originalY[idxFirstCol];
            }
            else
            {
                rowRadiusOffset[r] = 0f;
                rowYOffset[r] = 0f;
            }
        }
        MeshController.onMeshUpdated?.Invoke();
        onMeshUpdated?.Invoke();
    }

    void RecalculateNormals(Mesh m)
    {
        if (m == null) return;
        int[] tri = m.triangles;
        Vector3[] v = m.vertices;
        Vector3[] n = new Vector3[v.Length];
        for (int i = 0; i < n.Length; ++i) n[i] = Vector3.zero;
        for (int i = 0; i < tri.Length; i += 3)
        {
            int p1 = tri[i], p2 = tri[i + 1], p3 = tri[i + 2];
            Vector3 fn = Vector3.Cross(v[p2] - v[p1], v[p3] - v[p1]);
            n[p1] += fn; n[p2] += fn; n[p3] += fn;
        }
        for (int i = 0; i < n.Length; ++i) n[i] = n[i].normalized;
        m.normals = n;
    }
}

using System;
using UnityEngine;

public class NewCylinderController : MonoBehaviour
{
    public static NewCylinderController Instance;

    public static Action onMeshUpdated;

    private Mesh mesh;
    private readonly float meshSize = 10;

    [SerializeField] private float radius = 3;
    [SerializeField] private float height = 10;
    [SerializeField][Range(4, 20)] private int resolution = 10;
    [Range(10f, 360f)] [SerializeField] private float sweepDegrees = 275f;
    [SerializeField] private GameObject controller;

    [Header("Controller visuals")]
    public int selectableColumn = 0;
    public Color selectableColor = Color.white;
    public Color unselectableColor = Color.black;

    private GameObject[] controllers;
    private GameObject[] normalLines;
    private Vector2[] originalUVs;
    private Vector2[] currentUVs;
    public bool ControllersVisible { get; private set; } = false;

    public void Awake()
    {
        Instance = this;
        mesh = GetComponent<MeshFilter>().mesh;
        originalUVs = mesh.uv.Clone() as Vector2[];
        currentUVs = mesh.uv.Clone() as Vector2[];
    }

    private void Start()
    {
        mesh = GetComponent<MeshFilter>().mesh;

        SetMesh();
    }

    //set mesh verticies based on resolution
    public void SetMesh()
    {
        mesh.Clear();

        //calculate number of vertices and triangles based on resolution
        int vertexCount = resolution * resolution;
        int triangleCount = (resolution - 1) * (resolution - 1) * 2;

        Vector3[] vertices = new Vector3[vertexCount];
        int[] triangles = new int[triangleCount * 3]; //3 indices per triangle

        //set vertex positions
        Vector2 currentPos = new Vector2(-meshSize / 2, -meshSize / 2); //start at bottom left corner
        float distanceToNextVertex = meshSize / ((float)resolution - 1);

        for (int row = 0; row < resolution; row++) //resolution is the number of verticies per row/column
        {
            for (int col = 0; col < resolution; col++)
            {
                int index = (row * resolution) + col;
                vertices[index] = GetVertexPos(col, row);
            }
        }

        mesh.vertices = vertices;
        Vector2[] uvs = new Vector2[vertexCount];
        float inv = (resolution > 1) ? 1f / (resolution - 1) : 0f;
        for (int row = 0; row < resolution; ++row)
        {
            for (int col = 0; col < resolution; ++col)
            {
                int idx = row * resolution + col;
                float u = col * inv;
                float v = row * inv;
                uvs[idx] = new Vector2(u, v);
            }
        }
        originalUVs = (Vector2[])uvs.Clone();
        currentUVs = (Vector2[])uvs.Clone();

        mesh.uv = currentUVs;

        //set triangles
        int currentTriangleVertIndex = 0;
        for (int row = 0; row < resolution - 1; row++) //loop through each quad and set its 2 triangles
        {
            for (int col = 0; col < resolution - 1; col++)
            {
                //find vertex indices for the corners of the current quad
                int bottomLeft = row * resolution + col;
                int bottomRight = bottomLeft + 1;
                int topLeft = bottomLeft + resolution;
                int topRight = topLeft + 1;

                triangles[currentTriangleVertIndex++] = bottomLeft;
                triangles[currentTriangleVertIndex++] = topLeft;
                triangles[currentTriangleVertIndex++] = topRight;

                triangles[currentTriangleVertIndex++] = bottomLeft;
                triangles[currentTriangleVertIndex++] = topRight;
                triangles[currentTriangleVertIndex++] = bottomRight;
            }
        }

        mesh.triangles = triangles;

        //calculate vertex normals
        RecalculateNormals();
        for (int i = 0; i < mesh.vertices.Length; i++)
        {
            Debug.DrawRay(transform.TransformPoint(mesh.vertices[i]), transform.TransformDirection(mesh.normals[i]) * 0.5f, Color.red, 5f);
        }

        onMeshUpdated?.Invoke();
        InitControllers(vertices, mesh.normals);
    }

    private void RecalculateNormals()
    {
        if (mesh == null)
            return;

        int[] triangles = mesh.triangles;
        Vector3[] vertices = mesh.vertices;
        Vector3[] normals = new Vector3[vertices.Length];

        // initialize all normals to Vector3.up
        for (int i = 0; i < mesh.vertices.Length; i++)
        {
            normals[i] = Vector3.zero;
        }

        //find normals of each triangle and add to that value to each vertex touching it
        for (int i = 0; i < triangles.Length; i += 3)
        {
            int p1 = triangles[i];
            int p2 = triangles[i + 1];
            int p3 = triangles[i + 2];

            Vector3 normal = GetNormalOfTriangle(vertices[p1], vertices[p2], vertices[p3]);

            normals[p1] += normal;
            normals[p2] += normal;
            normals[p3] += normal;
        }

        //nornmalize each vertex normal
        for (int i = 0; i < normals.Length; i++)
        {
            normals[i] = normals[i].normalized;
        }

        mesh.normals = normals;
    }

    //create sphere primitives at mesh vertices
    private void InitControllers(Vector3[] vertices, Vector3[] normals)
    {
        if (controllers != null)
        {
            for (int i = 0; i < controllers.Length; i++)
            {
                Destroy(controllers[i]);
            }
        }

        controllers = new GameObject[vertices.Length];
        normalLines = new GameObject[controllers.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            controllers[i] = Instantiate(controller); //controllers
            controllers[i].transform.parent = transform;
            controllers[i].transform.localPosition = vertices[i];

            GameObject pivot = new GameObject("Normal Line"); //normal visuals
            pivot.transform.parent = controllers[i].transform;
            pivot.transform.localPosition = Vector3.zero;

            GameObject normalLine = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            normalLine.transform.parent = pivot.transform;

            pivot.transform.localRotation = Quaternion.FromToRotation(Vector3.up, normals[i]);     

            normalLine.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            normalLine.transform.localScale = new Vector3(0.05f, 0.5f, 0.05f);
            normalLines[i] = pivot;
        }

        HideAllControllers();
    }

    //move mesh vertices to controller positions when user moves controllers
    private void MeshModified()
    {
        //update vertices
        Vector3[] vertices = mesh.vertices;
        for (int i = 0; i < controllers.Length; i++)
        {
            vertices[i] = controllers[i].transform.localPosition;
        }
        mesh.vertices = vertices;

        //update normals and visual lines
        RecalculateNormals();
        for (int i = 0; i < normalLines.Length; i++)
        {
            normalLines[i].transform.localRotation = Quaternion.FromToRotation(Vector3.up, mesh.normals[i]);
        }
    }

    //added UVTRS application function for mesh UV manipulation
    public void ApplyUVTRS(Vector2 translation, float rotationDeg, Vector2 scale)
    {
        if (originalUVs == null || originalUVs.Length == 0)
            return;

        Matrix3x3 m = Matrix3x3Helpers.CreateTRS(translation, rotationDeg, scale);

        if (currentUVs == null || currentUVs.Length != originalUVs.Length)
            currentUVs = new Vector2[originalUVs.Length];

        for (int i = 0; i < originalUVs.Length; ++i)
            currentUVs[i] = m * originalUVs[i];

        mesh.uv = currentUVs;
    }

    #region UTIL
    private int GetVertIndexFromRowCol(int row, int col)
    {
        return row * resolution + col;
    }

    //get vertex position on cylinder
    private Vector3 GetVertexPos(int col, int row)
    {
        int index = (row * resolution) + col;
        float t = col / (resolution - 1f);
        float angleDeg = t * sweepDegrees;
        float angleRad = angleDeg * Mathf.Deg2Rad;

        float x = Mathf.Cos(angleRad) * radius;
        float z = Mathf.Sin(angleRad) * radius;
        float y = (row / (resolution - 1f)) * height;

        return new Vector3(x, y, z);
    }

    private Vector3 GetNormalOfTriangle(Vector3 p1, Vector3 p2, Vector3 p3)
    {
        Vector3 side1 = p2 - p1;
        Vector3 side2 = p3 - p1;
        return Vector3.Cross(side1, side2);
    }

    public void HideAllControllers()
    {
        ControllersVisible = false;
        foreach (var controller in controllers)
        {
            controller.SetActive(false);
        }
    }

    public void ShowAllControllers()
    {
        ControllersVisible = true;
        foreach (var controller in controllers)
        {
            controller.SetActive(true);
        }
    }
    #endregion

    public void SetResolution(int resolution)
    {
        this.resolution = resolution;
        SetMesh();
    }

    public void SetSweepAngle(int angle)
    {
        sweepDegrees = angle;
        SetMesh();
    }

    private void OnEnable()
    {
        VertController.onMovedAxis += MeshModified;
    }

    private void OnDisable()
    {
        VertController.onMovedAxis -= MeshModified;
    }
}

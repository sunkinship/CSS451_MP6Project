using System;
using Unity.VisualScripting;
using UnityEngine;

public class MeshController : MonoBehaviour
{
    public static MeshController Instance;

    public static Action onMeshUpdated;

    private Mesh mesh;
    private readonly float meshSize = 10;

    [SerializeField] [Range(2, 20)] private int resolution = 3;
    [SerializeField] private GameObject controller;

    private GameObject[] controllers;
    private GameObject[] normalLines;

    public bool ControllersVisible { get; private set; } = false;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        mesh = GetComponent<MeshFilter>().mesh;

        SetMesh(resolution);
    }

    //set mesh verticies based on resolution
    public void SetMesh(int resolution)
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
                vertices[index] = new Vector3(currentPos.x + (col * distanceToNextVertex), 0, currentPos.y + (row * distanceToNextVertex));
            }
        }

        mesh.vertices = vertices;

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
            pivot.transform.localRotation = Quaternion.FromToRotation(Vector3.up, normals[i]);

            GameObject normalLine = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            normalLine.transform.parent = pivot.transform;

            normalLine.transform.localPosition = new Vector3(0f, 1f, 0f);
            normalLine.transform.localScale = new Vector3(0.05f, 1.15f, 0.05f);
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

    #region UTIL
    private int GetVertIndexFromRowCol(int row, int col)
    {
        return row * resolution + col;
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

    private void OnEnable()
    {
        VertController.onMovedAxis += MeshModified;
    }

    private void OnDisable()
    {
        VertController.onMovedAxis -= MeshModified;
    }
}
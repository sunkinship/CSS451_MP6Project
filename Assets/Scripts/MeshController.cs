using UnityEngine;

public class MeshController : MonoBehaviour
{
    public static MeshController Instance;

    private Mesh mesh;
    private readonly float meshSize = 10;

    [SerializeField] [Range(2, 20)] private int resolution = 3;

    private GameObject[] controllers;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        mesh = GetComponent<MeshFilter>().mesh;

        SetMesh(resolution);
        UpdateMeshVerts();
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
        Vector3[] normals = new Vector3[vertexCount];

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

        //set normals
        for (int i = 0; i < normals.Length; i++) //all normals facing up
        {
            normals[i] = new Vector3(0, 1, 0);
        }

        //set triangles
        int currentTriangleVertIndex = 0;
        for (int row = 0; row < resolution - 1; row++)
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

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.normals = normals;

        InitControllers(vertices, normals);
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
        for (int i = 0; i < vertices.Length; i++)
        {
            controllers[i] = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            controllers[i].transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);

            GameObject normalLine = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            normalLine.transform.localPosition = new Vector3(0f, 1f, 0f);
            normalLine.transform.localScale = new Vector3(0.05f, 1f, 0.05f);
            normalLine.transform.localRotation = Quaternion.Euler(normals[i]);
            normalLine.transform.parent = controllers[i].transform;

            controllers[i].transform.localPosition = vertices[i];
            controllers[i].transform.parent = this.transform;
        }
    }

    //move mesh vertices to controller positions
    private void UpdateMeshVerts()
    {
        Vector3[] vertices = mesh.vertices;
        for (int i = 0; i < controllers.Length; i++)
        {
            vertices[i] = controllers[i].transform.localPosition;
        }
        mesh.vertices = vertices;
    }
}
using System.Collections.Generic;
using UnityEngine;


[RequireComponent(typeof(GeneralCylinderMesh))]
public class CylinderVertexSelectionManager : MonoBehaviour
{
    public GameObject spherePrefab;
    public float sphereScale = 0.06f;
    public Color normalColor = Color.cyan;
    public Color selectedColor = Color.yellow;

    List<GameObject> spheres = new List<GameObject>();
    int selectedIndex = -1;

    GeneralCylinderMesh cylinder;
    MeshFilter mf;

    void Awake()
    {
        cylinder = GetComponent<GeneralCylinderMesh>();
        mf = GetComponent<MeshFilter>();
    }

    void Start()
    {
        BuildFromMesh();
    }

    void Update()
    {
        bool show = Input.GetKey(KeyCode.LeftControl);
        for (int i = 0; i < spheres.Count; ++i)
        {
            if (spheres[i] != null && spheres[i].activeSelf != show) spheres[i].SetActive(show);
        }

        if (show && Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
                var go = hit.collider.gameObject;
                int idx = spheres.IndexOf(go);
                if (idx >= 0)
                {
                    SelectVertex(idx);
                }
            }
        }
    }

    void ClearOld()
    {
        foreach (var s in spheres) if (s != null) Destroy(s);
        spheres.Clear();
    }

    public void BuildFromMesh()
    {
        ClearOld();

        if (mf == null || mf.sharedMesh == null)
        {
            Debug.LogWarning("[CylinderVertexSelectionManager] mesh not found");
            return;
        }

        Vector3[] meshVerts = mf.sharedMesh.vertices;
        for (int i = 0; i < meshVerts.Length; ++i)
        {
            Vector3 world = transform.TransformPoint(meshVerts[i]);
            GameObject s;
            if (spherePrefab != null) s = Instantiate(spherePrefab, world, Quaternion.identity, transform);
            else
            {
                s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                s.transform.SetParent(transform, true);
                s.transform.position = world;
            }
            s.transform.localScale = Vector3.one * sphereScale;
            if (s.GetComponent<Rigidbody>() == null) { var rb = s.AddComponent<Rigidbody>(); rb.isKinematic = true; }
            SetSphereColor(s, normalColor);
            s.SetActive(false);
            spheres.Add(s);
        }
    }

    void SetSphereColor(GameObject s, Color c)
    {
        var r = s.GetComponent<Renderer>();
        if (r != null) r.material = new Material(Shader.Find("Standard")) { color = c };
    }

    void SelectVertex(int idx)
    {
        if (selectedIndex >= 0 && selectedIndex < spheres.Count)
        {
            SetSphereColor(spheres[selectedIndex], normalColor);
        }

        selectedIndex = idx;
        SetSphereColor(spheres[selectedIndex], selectedColor);
        ObjectManager.Instance.ObjectSelected(spheres[selectedIndex].transform);
    }

    public void Rebuild()
    {
        BuildFromMesh();
    }
}

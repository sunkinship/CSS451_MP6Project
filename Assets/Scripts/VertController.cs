using System;
using UnityEngine;

public class VertController : MonoBehaviour
{
    //modified to work for both MeshController and CylinderController
    //if we end up subclassing, this can be reverted to just work with MeshController
    public static VertController Instance;

    private const string CONTROL_AXIS_X_TAG = "ControlAxisX";
    private const string CONTROL_AXIS_Y_TAG = "ControlAxisY";
    private const string CONTROL_AXIS_Z_TAG = "ControlAxisZ";

    public static Action onMovedAxis;

    [Header("Layers / Prefab")]
    [SerializeField] private LayerMask controllerMask;   
    [SerializeField] private LayerMask controlAxisMask;  
    [SerializeField] private GameObject controlAxis;     

    [Header("Movement")]
    [SerializeField] private float axisMoveSpeed = 15f;

    [HideInInspector] public Transform controllerSelected; 
    [HideInInspector] public Transform axisSelected;

    private GameObject controlAxisInstance;

    private readonly Color axisSelectedColor = new Color(Color.yellow.r, Color.yellow.g, Color.yellow.b, 0.5f);
    private readonly Color controllerSelectedColor = Color.red;
    private Color controllerOriginalColor;
    private Color axisOriginalColor;

    private enum VertControlMode { None, X, Y, Z };
    private VertControlMode vertControlMode = VertControlMode.None;

    private Vector3 lastMousePosition;
    private bool leftMouseHeld => Input.GetMouseButton(0);

    private void Awake()
    {
        if (Instance == null) Instance = this;

        if (controlAxis != null)
        {
            controlAxisInstance = Instantiate(controlAxis);
            controlAxisInstance.SetActive(false);
            controlAxisInstance.transform.parent = null;
        }
    }

    private void Start()
    {
        if (controlAxisInstance != null) controlAxisInstance.SetActive(false);
    }

    private void OnEnable()
    {
        MeshController.onMeshUpdated += OnMeshUpdated;
        CylinderController.onMeshUpdated += OnMeshUpdated;
    }

    private void OnDisable()
    {
        MeshController.onMeshUpdated -= OnMeshUpdated;
        CylinderController.onMeshUpdated -= OnMeshUpdated;
    }

    private void Update()
    {
        SetMode();

        if (!leftMouseHeld)
            return;

        switch (vertControlMode)
        {
            case VertControlMode.None: break;
            case VertControlMode.X: MoveControllerX(); break;
            case VertControlMode.Y: MoveControllerY(); break;
            case VertControlMode.Z: MoveControllerZ(); break;
        }
    }
    private bool TryGetActiveController(out IActiveController active)
    {
        active = null;
        if (CylinderController.Instance != null && CylinderController.Instance.gameObject.activeInHierarchy)
        {
            active = new CylinderActiveWrapper(CylinderController.Instance);
            return true;
        }
        if (MeshController.Instance != null)
        {
            active = new MeshActiveWrapper(MeshController.Instance);
            return true;
        }
        return false;
    }

    #region MODE
    private void SetMode()
    {
        if (!Input.GetKey(KeyCode.LeftControl))
        {
            vertControlMode = VertControlMode.None;
            if (controllerSelected == null)
            {
                if (TryGetActiveController(out var active))
                {
                    if (active.ControllersVisible) active.HideAllControllers();
                }
            }
            return;
        }

        if (CameraController.Instance != null && CameraController.Instance.InCamControlMode)
            return;

        if (!TryGetActiveController(out var controller))
            return;

        if (!controller.ControllersVisible)
            controller.ShowAllControllers();

        if (Input.GetMouseButtonUp(0))
        {
            ClearAxisSelection();
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            

            if (Physics.Raycast(ray, out hit, 100f, controlAxisMask))
            {
                lastMousePosition = Input.mousePosition;
                ControlAxisSelected(hit.transform);
            }
            else if (Physics.Raycast(ray, out hit, 100f, controllerMask))
            {
                ControllerSelected(hit.transform);
            }
            else
            {
                ClearSelection();
            }
        }
    }

    private void ControllerSelected(Transform selected)
    {
        ClearSelection();
        if (selected == null) return;

        // guard against selecting a destroyed UnityEngine.Object (MissingReference)
        if (selected.gameObject == null) return;

        if (selected.TryGetComponent<Renderer>(out var renderer))
        {
            controllerOriginalColor = renderer.material.color;
            renderer.material.color = controllerSelectedColor;
        }

        controllerSelected = selected;
        EnableControlAxis();
    }


    private void ControlAxisSelected(Transform selected)
    {
        if (selected == null) return;
        Transform axisTransform = FindAxisTaggedTransform(selected);
        if (axisTransform.TryGetComponent<Renderer>(out var renderer))
        {
            axisOriginalColor = renderer.material.color;
            renderer.material.color = axisSelectedColor;
        }

        axisSelected = axisTransform;

        if (axisTransform.CompareTag(CONTROL_AXIS_X_TAG))
        {
            vertControlMode = VertControlMode.X;
            MoveControllerX();
        }
        else if (axisTransform.CompareTag(CONTROL_AXIS_Y_TAG))
        {
            vertControlMode = VertControlMode.Y;
            MoveControllerY();
        }
        else if (axisTransform.CompareTag(CONTROL_AXIS_Z_TAG))
        {
            vertControlMode = VertControlMode.Z;
            MoveControllerZ();
        }
    }
    private Transform FindAxisTaggedTransform(Transform t)
    {
        if (t == null) return null;
        Transform cur = t;
        while (cur != null)
        {
            if (cur.CompareTag(CONTROL_AXIS_X_TAG) || cur.CompareTag(CONTROL_AXIS_Y_TAG) || cur.CompareTag(CONTROL_AXIS_Z_TAG))
                return cur;
            cur = cur.parent;
        }
        return null;
    }

    private void ClearSelection()
    {
        vertControlMode = VertControlMode.None;
        if (controllerSelected != null)
        {
            if (controllerSelected.TryGetComponent<Renderer>(out var renderer))
            {
                renderer.material.color = controllerOriginalColor;
            }
            controllerSelected = null;
        }
        HideControlAxis();
    }

    private void ClearAxisSelection()
    {
        vertControlMode = VertControlMode.None;
        if (axisSelected != null)
        {
            if (axisSelected.TryGetComponent<Renderer>(out var renderer))
                renderer.material.color = axisOriginalColor;
            axisSelected = null;
        }
    }

    private void EnableControlAxis()
    {
        if (controllerSelected == null)
            return;
        if (controlAxisInstance == null)
        {
            if (controlAxis == null)
            {
                Debug.LogWarning("[VertController] EnableControlAxis: controlAxis prefab is missing.");
                return;
            }
            controlAxisInstance = Instantiate(controlAxis);
            controlAxisInstance.SetActive(false);
            controlAxisInstance.transform.parent = null;
        }
        Transform parentTransform = controllerSelected.transform.parent;
        if (parentTransform == null)
        {
            Debug.LogWarning("[VertController] EnableControlAxis: controllerSelected parent is null/destroyed.");
            return;
        }

        controlAxisInstance.transform.SetParent(parentTransform, false);
        controlAxisInstance.transform.localPosition = Vector3.zero;
        controlAxisInstance.transform.localScale = Vector3.one;
        controlAxisInstance.SetActive(true);
    }



    private void HideControlAxis()
    {
        if (controlAxisInstance == null) return;

        controlAxisInstance.SetActive(false);
        controlAxisInstance.transform.SetParent(null, true);
        controlAxisInstance.transform.localScale = Vector3.one;
    }

    #endregion

    #region MOVE
    private void MoveControllerX()
    {
        Debug.Log(controllerSelected);
        if (controllerSelected == null) return;
        Vector3 delta = Input.mousePosition - lastMousePosition;
        Vector3 moveOffset = (transform.right * delta.x) * (axisMoveSpeed * Time.deltaTime);
        controllerSelected.parent.position += moveOffset;
        onMovedAxis?.Invoke();
        lastMousePosition = Input.mousePosition;
    }

    private void MoveControllerY()
    {
        if (controllerSelected == null) return;
        Vector3 delta = Input.mousePosition - lastMousePosition;
        Vector3 moveOffset = (transform.up * delta.y) * (axisMoveSpeed * Time.deltaTime);
        controllerSelected.parent.position += moveOffset;
        onMovedAxis?.Invoke();
        lastMousePosition = Input.mousePosition;
    }

    private void MoveControllerZ()
    {
        if (controllerSelected == null) return;
        Vector3 delta = Input.mousePosition - lastMousePosition;
        Vector3 moveOffset = (transform.forward * delta.y) * (axisMoveSpeed * Time.deltaTime);
        controllerSelected.parent.position += moveOffset;
        onMovedAxis?.Invoke();
        lastMousePosition = Input.mousePosition;
    }
    #endregion

    private void OnMeshUpdated()
    {
        if (leftMouseHeld || vertControlMode != VertControlMode.None)
        {
            return;
        }
        vertControlMode = VertControlMode.None;
        controllerSelected = null;
        if (controlAxisInstance != null)
        {
            controlAxisInstance.transform.SetParent(null, true);
            HideControlAxis();
            controlAxisInstance.transform.localScale = Vector3.one;
        }
    }

    private interface IActiveController
    {
        bool ControllersVisible { get; }
        void ShowAllControllers();
        void HideAllControllers();
    }

    private class MeshActiveWrapper : IActiveController
    {
        MeshController mc;
        public MeshActiveWrapper(MeshController m) { mc = m; }
        public bool ControllersVisible => mc.ControllersVisible;
        public void ShowAllControllers() => mc.ShowAllControllers();
        public void HideAllControllers() => mc.HideAllControllers();
    }

    private class CylinderActiveWrapper : IActiveController
    {
        CylinderController cc;
        public CylinderActiveWrapper(CylinderController c) { cc = c; }
        public bool ControllersVisible => cc.ControllersVisible;
        public void ShowAllControllers() => cc.ShowAllControllers();
        public void HideAllControllers() => cc.HideAllControllers();
    }
}

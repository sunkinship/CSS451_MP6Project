using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//manage sphere controllers that control verticies
public class VertController : MonoBehaviour
{
    public static VertController Instance;

    private const string CONTROL_AXIS_X_TAG = "ControlAxisX";
    private const string CONTROL_AXIS_Y_TAG = "ControlAxisY";
    private const string CONTROL_AXIS_Z_TAG = "ControlAxisZ";

    private MeshController meshController => MeshController.Instance;

    public static Action onMovedAxis;

    [SerializeField] private LayerMask controllerMask; //sphere controllers
    [SerializeField] private LayerMask controlAxisMask; //axis frame to move controllers
    [SerializeField] private GameObject controlAxis;

    [SerializeField] private float axisMoveSpeed = 15;

    [HideInInspector] public Transform controllerSelected;
    [HideInInspector] public Transform axisSelected;

    private readonly Color axisSelectedColor = new Color(Color.yellow.r, Color.yellow.g, Color.yellow.b, 0.5f);
    private readonly Color controllerSelectedColor = Color.red;
    private Color controllerOriginalColor;
    private Color axisOriginalColor;
    
    //control modes
    private enum VertControlMode { None, X, Y, Z };
    private VertControlMode vertControlMode = VertControlMode.None;

    private Vector3 lastMousePosition;
    private bool leftMouseHeld => Input.GetMouseButton(0);

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
    }

    private void Start()
    {
        controlAxis = Instantiate(controlAxis);
        controlAxis.SetActive(false);
    }

    private void Update()
    {
        SetMode();

        if (!leftMouseHeld)
            return;

        switch (vertControlMode)
        {
            case VertControlMode.None:
                break;
            case VertControlMode.X:
                MoveControllerX();
                break;
            case VertControlMode.Y:
                MoveControllerY();
                break;
            case VertControlMode.Z:
                MoveControllerZ();
                break;
        }
    }

    #region MODE
    private void SetMode()
    {
        //only enter vert control mode if left control is held and camera is not being controlled already
        if (!Input.GetKey(KeyCode.LeftControl))
        {
            vertControlMode = VertControlMode.None;
            if (controllerSelected == null)
            {
                if (meshController.ControllersVisible)
                    meshController.HideAllControllers();
            }
            return;
        }

        if (CameraController.Instance.InCamControlMode)
            return;


        if (meshController.ControllersVisible == false)
            meshController.ShowAllControllers();

        if (Input.GetMouseButtonUp(0))
        {
            ClearAxisSelection();
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 50, controlAxisMask))
            {
                lastMousePosition = Input.mousePosition;
                ControlAxisSelected(hit.transform);
            }
            else if (Physics.Raycast(ray, out hit, 50, controllerMask))
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
        if (selected.TryGetComponent<Renderer>(out var renderer))
        {
            axisOriginalColor = renderer.material.color;
            renderer.material.color = axisSelectedColor;
        }

        axisSelected = selected;

        if (selected.CompareTag(CONTROL_AXIS_X_TAG))
        {
            vertControlMode = VertControlMode.X;
            MoveControllerX();
        }
        else if (selected.CompareTag(CONTROL_AXIS_Y_TAG))
        {
            vertControlMode = VertControlMode.Y;
            MoveControllerY();
        }
        else if (selected.CompareTag(CONTROL_AXIS_Z_TAG))
        {
            vertControlMode = VertControlMode.Z;
            MoveControllerZ();
        }
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
            {
                renderer.material.color = axisOriginalColor;
            }
            axisSelected = null;
        }
    }

    private void EnableControlAxis()
    {
        if (controllerSelected == null)
            return;

        controlAxis.transform.SetParent(controllerSelected.transform.parent, false);
        controlAxis.transform.localPosition = Vector3.zero;
        controlAxis.SetActive(true);
    }

    private void HideControlAxis()
    {
        controlAxis.SetActive(false);
    }
    #endregion

    #region MOVE WITH AXIS
    private void MoveControllerX()
    {
        if (controllerSelected == null)
            return;

        Vector3 delta = Input.mousePosition - lastMousePosition;
        Vector3 moveOffset = (transform.right * delta.x) * (axisMoveSpeed * Time.deltaTime);
        controllerSelected.parent.position += moveOffset;

        onMovedAxis?.Invoke();

        lastMousePosition = Input.mousePosition;
    }

    private void MoveControllerY()
    {
        if (controllerSelected == null)
            return;

        Vector3 delta = Input.mousePosition - lastMousePosition;

        Vector3 moveOffset = (transform.up * delta.y) * (axisMoveSpeed * Time.deltaTime);
        controllerSelected.parent.position += moveOffset;

        onMovedAxis?.Invoke();

        lastMousePosition = Input.mousePosition;
    }

    private void MoveControllerZ()
    {
        if (controllerSelected == null)
            return;

        Vector3 delta = Input.mousePosition - lastMousePosition;

        Vector3 moveOffset = (transform.forward * delta.x) * (axisMoveSpeed * Time.deltaTime);
        controllerSelected.parent.position += moveOffset;

        onMovedAxis?.Invoke();

        lastMousePosition = Input.mousePosition;
    }
    #endregion

    private void OnMeshUpdated()
    {
        vertControlMode = VertControlMode.None;
        controllerSelected = null;
        controlAxis.transform.parent = null;
        HideControlAxis();
    }

    private void OnEnable()
    {
        MeshController.onMeshUpdated += OnMeshUpdated;
    }

    private void OnDisable()
    {
        MeshController.onMeshUpdated -= OnMeshUpdated;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform LookAtPosition;

    //control modes
    private enum ControlMode { None, Tumble, Pan };
    private ControlMode controlMode = ControlMode.None;
    private bool Zooming = false;

    private Vector3 lastMousePositioTumble;

    //tumble
    
    private bool leftMouseHeld => Input.GetMouseButton(0);
    private float orbitDirection = 1f;
    private float currentPitch = 0f;
    [Header("Tumble")]
    [SerializeField] private float rotateSpeed = 15;
    [SerializeField] private float minVerticalAngle = 10f;
    [SerializeField] private float maxVerticalAngle = 80f;

    //pan
    private bool rightMouseHeld => Input.GetMouseButton(1);
    [Header("Pan")]
    [SerializeField] private float panSpeed = 4;

    //zoom
    private float scrollDeltaY => Input.mouseScrollDelta.y;
    [Header("Zoom")]
    [SerializeField] private float zoomSpeed = 200;
    [SerializeField] private float minZoomDistance = 2f;
    [SerializeField] private float maxZoomDistance = 20f;

    private Vector3 startPos;
    private Quaternion startRot;

    private void Start()
    {
        startPos = transform.position;
        startRot = transform.rotation;

        InitializePitch();
        LookAtTarget();
    }

    void Update()
    {
        SetModes();

        if (Zooming)
            Zoom();

        switch (controlMode)
        {
            case ControlMode.None:
                break;
            case ControlMode.Tumble:
                if (leftMouseHeld)
                    Tumble();
                break;
            case ControlMode.Pan:
                if (rightMouseHeld)
                    Pan();
                break;
        }

        //SetLine();
    }
    
    private void SetModes()
    {
        if (!Input.GetKey(KeyCode.LeftAlt))
        {
            controlMode = ControlMode.None;
            Zooming = false;
            return;
        }

        Zooming = Input.mouseScrollDelta.y != 0;

        if (Input.GetMouseButtonDown(0))
        {
            lastMousePositioTumble = Input.mousePosition;
            controlMode = ControlMode.Tumble;
            return;
        }

        if (Input.GetMouseButtonDown(1))
        {
            lastMousePositioTumble = Input.mousePosition;
            controlMode = ControlMode.Pan;
            return;
        }
    }

    private void Tumble()
    {
        Vector3 delta = Input.mousePosition - lastMousePositioTumble;
        ComputeOrbit(delta);
        lastMousePositioTumble = Input.mousePosition;
    }

    private void ComputeOrbit(Vector3 delta)
    {
        //find target rotation
        float amount = orbitDirection * rotateSpeed * Time.deltaTime;
        float rotateAmountX = amount * delta.x;
        float rotateAmountY = amount * -delta.y;

        Quaternion targetQHorizontal = Quaternion.AngleAxis(rotateAmountX, Vector3.up);

        //clamp vertical rotation
        float newPitch = Mathf.Clamp(currentPitch + rotateAmountY, minVerticalAngle, maxVerticalAngle);
        float clampedAmountY = newPitch - currentPitch; //actual change allowed
        currentPitch = newPitch;
        Quaternion targetQVertical = Quaternion.AngleAxis(clampedAmountY, transform.right);

        Quaternion targetQTotal = targetQHorizontal * targetQVertical;
        
        //find final position and rotation based on target rotation to maintain orbit
        Matrix4x4 rot = Matrix4x4.Rotate(targetQTotal);
        Matrix4x4 invP = Matrix4x4.TRS(-LookAtPosition.localPosition, Quaternion.identity, Vector3.one);
        rot = invP.inverse * rot * invP;

        //set target position and rotation
        Vector3 newCameraPos = rot.MultiplyPoint(transform.localPosition);
        transform.localPosition = newCameraPos;
        transform.localRotation = targetQTotal * transform.localRotation;
    }

    private void InitializePitch()
    {
        Vector3 dir = (transform.position - LookAtPosition.position).normalized;
        currentPitch = Vector3.Angle(Vector3.ProjectOnPlane(dir, Vector3.up), dir);
    }

    private void Pan()
    {
        Vector3 delta = Input.mousePosition - lastMousePositioTumble;
        Vector3 moveDirection = new Vector3(-delta.x, -delta.y, 0);

        Vector3 panOffset = (transform.right * moveDirection.x + transform.up * moveDirection.y) * (panSpeed * Time.deltaTime);

        transform.position += panOffset;
        LookAtPosition.position += panOffset;

        //transform.Translate(panSpeed * Time.deltaTime * moveDirection);
        //LookAtPosition.Translate(panSpeed * Time.deltaTime * moveDirection);

        lastMousePositioTumble = Input.mousePosition;
    }

    private void Zoom()
    {
        Vector3 directionToTarget = (LookAtPosition.position - transform.position).normalized;

        float currentDistance = Vector3.Distance(transform.position, LookAtPosition.position);
        float zoomAmount = scrollDeltaY * zoomSpeed * Time.deltaTime;
        float targetDistance = currentDistance - zoomAmount;

        targetDistance = Mathf.Clamp(targetDistance, minZoomDistance, maxZoomDistance); //clamp distance
        transform.position = LookAtPosition.position - directionToTarget * targetDistance;
    }

    public void LookAtTarget()
    {
        Vector3 V = LookAtPosition.localPosition - transform.localPosition; //target forward
        Vector3 W = Vector3.Cross(-V, Vector3.up);
        Vector3 U = Vector3.Cross(W, -V); //target up

        transform.localRotation = Quaternion.FromToRotation(Vector3.up, U); //align up
        Quaternion alignU = Quaternion.FromToRotation(transform.forward, V); //align forward
        transform.localRotation = alignU * transform.localRotation;
    }

    public void ResetCamera()
    {
        transform.position = startPos;
        transform.rotation = startRot;
        LookAtTarget();
    }
}

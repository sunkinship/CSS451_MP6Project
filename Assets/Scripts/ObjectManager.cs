using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectManager : MonoBehaviour
{
    public static ObjectManager Instance { get; private set; }

    [SerializeField] private CameraController cam;

    [HideInInspector] public GameObject mSelected;
    private Quaternion originalRotation;


    private void Awake()
    {
        Instance = this;
    }

    public void ObjectSelected(Transform selected)
    {
        mSelected = selected.gameObject;
        originalRotation = mSelected.transform.localRotation;

        UIManager.Instance.StateChanged();
    }

    #region MODIFY
    public void TranslateSelected(float pos, UIManager.Axis axis)
    {
        if (mSelected == null)
            return;

        switch (axis)
        {
            case UIManager.Axis.X:
                mSelected.transform.localPosition = new Vector3(pos, mSelected.transform.localPosition.y, mSelected.transform.localPosition.z);
                break;
            case UIManager.Axis.Y:
                mSelected.transform.localPosition = new Vector3(mSelected.transform.localPosition.x, pos, mSelected.transform.localPosition.z);
                break;
            case UIManager.Axis.Z:
                mSelected.transform.localPosition = new Vector3(mSelected.transform.localPosition.x, mSelected.transform.localPosition.y, pos);
                break;
        }
    }

    public void RotateSelected(float rot, UIManager.Axis axis)
    {
        if (mSelected == null) 
            return;

        Quaternion q = Quaternion.identity;

        switch (axis)
        {
            case UIManager.Axis.X:
                q = Quaternion.AngleAxis(rot, Vector3.right);
                break;
            case UIManager.Axis.Y:
                q = Quaternion.AngleAxis(rot, Vector3.up);
                break;
            case UIManager.Axis.Z:
                q = Quaternion.AngleAxis(rot, Vector3.forward);
                break;
        }

        mSelected.transform.localRotation = originalRotation * q;
    }

    public void ScaleSelected(float scale, UIManager.Axis axis)
    {
        if (mSelected == null) 
            return;

        switch (axis)
        {
            case UIManager.Axis.X:
                mSelected.transform.localScale = new Vector3(scale, mSelected.transform.localScale.y, mSelected.transform.localScale.z);
                break;
            case UIManager.Axis.Y:
                mSelected.transform.localScale = new Vector3(mSelected.transform.localScale.x, scale, mSelected.transform.localScale.z);
                break;
            case UIManager.Axis.Z:
                mSelected.transform.localScale = new Vector3(mSelected.transform.localScale.x, mSelected.transform.localScale.y, scale);
                break;
        }
    }
    #endregion

    public void ResetScene()
    {
        cam.ResetCamera();
    }

    public void UpdateOriginalRotation()
    {
        if (mSelected == null) 
            return;

        originalRotation = mSelected.transform.localRotation;
    }
}

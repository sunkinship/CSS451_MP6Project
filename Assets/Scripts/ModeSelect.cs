using UnityEngine;
using TMPro;

public class ModeSelect : MonoBehaviour
{
    public GameObject planarMeshObject;   
    public GameObject cylinderObject;     
    public TMP_Dropdown tmpDropdown;      

    void Start()
    {
        SetActivePlanar();
        tmpDropdown.onValueChanged.RemoveAllListeners();
        tmpDropdown.onValueChanged.AddListener(SelectMode);

    }
    public void SelectMode(int index)
    {

        if (index == 0) SetActivePlanar();
        else SetActiveCylinder();
    }
    void SetActivePlanar()
    {
        planarMeshObject.SetActive(true);
        cylinderObject.SetActive(false);

    }

    void SetActiveCylinder()
    {
        planarMeshObject.SetActive(false);
        cylinderObject.SetActive(true);
    }
}

using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
//using UnityEngine.UIElements;


public class UIManager : MonoBehaviour
{
    //edited to include UV manipulation functionality and cylinder controls
    public static UIManager Instance { get; private set; }

    public CylinderController cylinderTarget;

    private readonly string sliderValueStringFormat = "F4";

    [Header("Transform Slider Values")]
    [SerializeField] private TextMeshProUGUI xText;
    [SerializeField] private TextMeshProUGUI yText;
    [SerializeField] private TextMeshProUGUI zText;

    [Header("Transform Sliders (UV Sliders)")]
    [SerializeField] private Slider xSlider;
    [SerializeField] private Slider ySlider;
    [SerializeField] private Slider zSlider;

    private readonly Vector2 translateRange = new Vector2(-10, 10);
    private readonly Vector2 rotateRange = new Vector2(-180, 180);
    private readonly Vector2 scaleRange = new Vector2(0.1f, 5f);

    [Header("Toggles (UV Modes)")]
    [SerializeField] private Toggle translateToggle;
    [SerializeField] private Toggle rotateToggle;
    [SerializeField] private Toggle scaleToggle;

    private enum Mode { None, Translate, Rotate, Scale }
    private Mode currentMode = Mode.None;
    private bool suppressUVUpdate = false;
    // Persistent UV TRS values
    private Vector2 savedTranslation = Vector2.zero;
    private Vector2 savedScale = Vector2.one;
    private float savedRotation = 0f;


    public enum Axis { None, X, Y, Z }
    private Axis lastChangedAxis = Axis.None;

    public event Action OnStateChanged;

    [Header("Resolution Settings")]
    [SerializeField] private Slider resSlider;
    [SerializeField] private TextMeshProUGUI resText;

    [Header("Cylinder Resolution Settings")]
    [SerializeField] private Slider CylResSlider;
    [SerializeField] private TextMeshProUGUI CylResText;

    [Header("Cylinder Rotation Settings")]
    [SerializeField] private Slider CylRotSlider;
    [SerializeField] private TextMeshProUGUI CylRotText;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        translateToggle.onValueChanged.AddListener(val => { if (val) SetTranslateMode(); });
        rotateToggle.onValueChanged.AddListener(val => { if (val) SetRotateMode(); });
        scaleToggle.onValueChanged.AddListener(val => { if (val) SetScaleMode(); });
        SetRotateMode();
    }

    public void StateChanged() => OnStateChanged?.Invoke();




    #region MODE TOGGLES
    public void SetTranslateMode()
    {
        if (currentMode == Mode.Translate)
        {
            translateToggle.SetIsOnWithoutNotify(true);
            return;
        }

        currentMode = Mode.Translate;
        xSlider.minValue = translateRange.x; xSlider.maxValue = translateRange.y;
        ySlider.minValue = translateRange.x; ySlider.maxValue = translateRange.y;
        zSlider.minValue = translateRange.x; zSlider.maxValue = translateRange.y;

        translateToggle.SetIsOnWithoutNotify(true);
        rotateToggle.SetIsOnWithoutNotify(false);
        scaleToggle.SetIsOnWithoutNotify(false);

        suppressUVUpdate = true;
        SliderXChangeWithoutNotif(savedTranslation.x);
        SliderYChangeWithoutNotif(savedTranslation.y);
        SliderZChangeWithoutNotif(0f);
        suppressUVUpdate = false;

        UpdateSliderInteractivity();

    }




    public void SetRotateMode()
    {
        if (currentMode == Mode.Rotate)
        {
            rotateToggle.SetIsOnWithoutNotify(true);
            return;
        }

        currentMode = Mode.Rotate;

        // Restore min/max
        xSlider.minValue = rotateRange.x; xSlider.maxValue = rotateRange.y;
        ySlider.minValue = rotateRange.x; ySlider.maxValue = rotateRange.y;
        zSlider.minValue = rotateRange.x; zSlider.maxValue = rotateRange.y;

        translateToggle.SetIsOnWithoutNotify(false);
        rotateToggle.SetIsOnWithoutNotify(true);
        scaleToggle.SetIsOnWithoutNotify(false);

        suppressUVUpdate = true;
        SliderXChangeWithoutNotif(0f);
        SliderYChangeWithoutNotif(0f);
        SliderZChangeWithoutNotif(0f);
        suppressUVUpdate = false;

        UpdateSliderInteractivity();

    }




    public void SetScaleMode()
    {
        if (currentMode == Mode.Scale)
        {
            scaleToggle.SetIsOnWithoutNotify(true);
            return;
        }

        currentMode = Mode.Scale;

        suppressUVUpdate = true;
        SliderXChangeWithoutNotif(savedScale.x);
        SliderYChangeWithoutNotif(savedScale.y);
        SliderZChangeWithoutNotif(1f);
        suppressUVUpdate = false;
        // Restore min/max
        xSlider.minValue = scaleRange.x; xSlider.maxValue = scaleRange.y;
        ySlider.minValue = scaleRange.x; ySlider.maxValue = scaleRange.y;
        zSlider.minValue = scaleRange.x; zSlider.maxValue = scaleRange.y;

        translateToggle.SetIsOnWithoutNotify(false);
        rotateToggle.SetIsOnWithoutNotify(false);
        scaleToggle.SetIsOnWithoutNotify(true);
        UpdateSliderInteractivity();

    }

    private void UpdateSliderInteractivity()
    {
        switch (currentMode)
        {
            case Mode.Translate:
                xSlider.interactable = true;
                ySlider.interactable = true;
                zSlider.interactable = false; // Z not used for translation
                break;

            case Mode.Rotate:
                xSlider.interactable = false;
                ySlider.interactable = false;
                zSlider.interactable = true;  // Only Z is used for rotation
                break;

            case Mode.Scale:
                xSlider.interactable = true;
                ySlider.interactable = true;
                zSlider.interactable = false; // Z not used for scaling
                break;

            case Mode.None:
            default:
                xSlider.interactable = true;
                ySlider.interactable = true;
                zSlider.interactable = true;
                break;
        }
    }


    #endregion

    #region SLIDER EVENTS (UV TRS)
    public void XSliderChanged()
    {
        CheckIfAxisChanged(Axis.X);
        xText.text = xSlider.value.ToString(sliderValueStringFormat);
        UpdateUVTRS();
    }

    public void YSliderChanged()
    {
        CheckIfAxisChanged(Axis.Y);
        yText.text = ySlider.value.ToString(sliderValueStringFormat);
        UpdateUVTRS();
    }

    public void ZSliderChanged()
    {
        CheckIfAxisChanged(Axis.Z);
        zText.text = zSlider.value.ToString(sliderValueStringFormat);
        UpdateUVTRS();
    }

    private void SliderXChangeWithoutNotif(float value)
    {
        xText.text = value.ToString(sliderValueStringFormat);
        xSlider.SetValueWithoutNotify(value);
    }

    private void SliderYChangeWithoutNotif(float value)
    {
        yText.text = value.ToString(sliderValueStringFormat);
        ySlider.SetValueWithoutNotify(value);
    }

    private void SliderZChangeWithoutNotif(float value)
    {
        zText.text = value.ToString(sliderValueStringFormat);
        zSlider.SetValueWithoutNotify(value);
    }

    private void CheckIfAxisChanged(Axis axis)
    {
        if (lastChangedAxis != axis)
        {
            lastChangedAxis = axis;
            ObjectManager.Instance.UpdateOriginalRotation();
        }
    }

    private void UpdateUVTRS()
    {
        if (suppressUVUpdate)
            return;

        switch (currentMode)
        {
            case Mode.Translate:
                savedTranslation = new Vector2(xSlider.value, ySlider.value);
                break;

            case Mode.Scale:
                savedScale = new Vector2(xSlider.value, ySlider.value);
                break;

            case Mode.Rotate:
                savedRotation = zSlider.value;
                break;
        }

        MeshController.Instance.ApplyUVTRS(savedTranslation, savedRotation, savedScale);
    }



    #endregion

    #region RESOLUTION SLIDER
    public void ResolutionSliderChanged(float value)
    {
        resText.text = ((int)value).ToString();
        MeshController.Instance.SetMesh((int)value);
    }
    #endregion

    #region CYLINDER RESOLUTION
    public void CylResSliderChanged()
    {
        CylResText.text = ((int)CylResSlider.value).ToString();
        CylResolutionSliderChanged(CylResSlider.value);
    }

    public void CylResolutionSliderChanged(float value)
    {
        int newRes = Mathf.Max(4, Mathf.RoundToInt(value));
        cylinderTarget.SetResolution(newRes);
    }
    #endregion

    #region CYLINDER ROTATION
    public void CylRotSliderChanged()
    {
        CylRotText.text = ((int)CylRotSlider.value).ToString();
        CylRotationSliderChanged((int)CylRotSlider.value);
    }

    public void CylRotationSliderChanged(int value)
    {
        cylinderTarget.SetSweepDegrees(value);
    }
    #endregion
}

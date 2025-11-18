using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
//using UnityEngine.UIElements;


public class UIManager : MonoBehaviour
{
    //edited to include UV manipulation functionality
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
        OnStateChanged += SelectionUpdated;
    }

    private void Start()
    {
        translateToggle.onValueChanged.AddListener(val => { if (val) SetTranslateMode(); });
        rotateToggle.onValueChanged.AddListener(val => { if (val) SetRotateMode(); });
        scaleToggle.onValueChanged.AddListener(val => { if (val) SetScaleMode(); });
        SetRotateMode();
    }

    private void OnDisable()
    {
        OnStateChanged -= SelectionUpdated;
    }

    public void StateChanged() => OnStateChanged?.Invoke();

    public void SelectionUpdated()
    {
        if (ObjectManager.Instance.mSelected == null)
        {
            switch (currentMode)
            {
                case Mode.Rotate:
                    SliderXChangeWithoutNotif(0);
                    SliderYChangeWithoutNotif(0);
                    SliderZChangeWithoutNotif(0);
                    break;
                case Mode.Scale:
                    SliderXChangeWithoutNotif(1);
                    SliderYChangeWithoutNotif(1);
                    SliderZChangeWithoutNotif(1);
                    break;
                case Mode.Translate:
                    SliderXChangeWithoutNotif(0);
                    SliderYChangeWithoutNotif(0);
                    SliderZChangeWithoutNotif(0);
                    break;
            }
            return;
        }

        Transform selected = ObjectManager.Instance.mSelected.transform;

        switch (currentMode)
        {
            case Mode.Translate:
                SliderXChangeWithoutNotif(selected.localPosition.x);
                SliderYChangeWithoutNotif(selected.localPosition.y);
                SliderZChangeWithoutNotif(selected.localPosition.z);
                break;
            case Mode.Rotate:
                SliderXChangeWithoutNotif(0);
                SliderYChangeWithoutNotif(0);
                SliderZChangeWithoutNotif(0);
                break;
            case Mode.Scale:
                SliderXChangeWithoutNotif(selected.localScale.x);
                SliderYChangeWithoutNotif(selected.localScale.y);
                SliderZChangeWithoutNotif(selected.localScale.z);
                break;
        }
    }

    #region MODE TOGGLES
    public void SetTranslateMode()
    {
        if (currentMode == Mode.Translate)
        {
            translateToggle.SetIsOnWithoutNotify(true);
            return;
        }

        translateToggle.SetIsOnWithoutNotify(true);
        rotateToggle.SetIsOnWithoutNotify(false);
        scaleToggle.SetIsOnWithoutNotify(false);

        xSlider.minValue = translateRange.x;
        xSlider.maxValue = translateRange.y;
        ySlider.minValue = translateRange.x;
        ySlider.maxValue = translateRange.y;
        zSlider.minValue = translateRange.x;
        zSlider.maxValue = translateRange.y;

        currentMode = Mode.Translate;
        StateChanged();
    }

    public void SetRotateMode()
    {
        if (currentMode == Mode.Rotate)
        {
            rotateToggle.SetIsOnWithoutNotify(true);
            return;
        }

        translateToggle.SetIsOnWithoutNotify(false);
        rotateToggle.SetIsOnWithoutNotify(true);
        scaleToggle.SetIsOnWithoutNotify(false);

        xSlider.minValue = rotateRange.x;
        xSlider.maxValue = rotateRange.y;
        ySlider.minValue = rotateRange.x;
        ySlider.maxValue = rotateRange.y;
        zSlider.minValue = rotateRange.x;
        zSlider.maxValue = rotateRange.y;

        currentMode = Mode.Rotate;
        StateChanged();
    }

    public void SetScaleMode()
    {
        if (currentMode == Mode.Scale)
        {
            scaleToggle.SetIsOnWithoutNotify(true);
            return;
        }

        translateToggle.SetIsOnWithoutNotify(false);
        rotateToggle.SetIsOnWithoutNotify(false);
        scaleToggle.SetIsOnWithoutNotify(true);

        xSlider.minValue = scaleRange.x;
        xSlider.maxValue = scaleRange.y;
        ySlider.minValue = scaleRange.x;
        ySlider.maxValue = scaleRange.y;
        zSlider.minValue = scaleRange.x;
        zSlider.maxValue = scaleRange.y;

        currentMode = Mode.Scale;
        StateChanged();
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
        Vector2 translation = Vector2.zero;
        Vector2 scale = Vector2.one;
        float rotation = 0f;

        switch (currentMode)
        {
            case Mode.Translate:
                translation = new Vector2(xSlider.value, ySlider.value);
                break;

            case Mode.Scale:
                scale = new Vector2(xSlider.value, ySlider.value);
                break;

            case Mode.Rotate:
                rotation = zSlider.value;
                break;
        }

        MeshController.Instance.ApplyUVTRS(translation, rotation, scale);

        xText.text = xSlider.value.ToString("F2");
        yText.text = ySlider.value.ToString("F2");
        zText.text = zSlider.value.ToString("F2");
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

using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    private readonly string sliderValueStringFormat = "F4";

    [Header("Transform Slider Values")]
    [SerializeField] private TextMeshProUGUI xText;
    [SerializeField] private TextMeshProUGUI yText;
    [SerializeField] private TextMeshProUGUI zText;

    [Header("Transform Sliders")]
    [SerializeField] private Slider xSlider;
    [SerializeField] private Slider ySlider;
    [SerializeField] private Slider zSlider;

    private readonly Vector2 translateRange = new Vector2(-10, 10);
    private readonly Vector2 rotateRange = new Vector2(-180, 180);
    private readonly Vector2 scaleRange = new Vector2(0.1f, 5);

    [Header("Toggles")]
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

    private void Awake()
    {
        Instance = this;
        OnStateChanged += SelectionUpdated;
    }

    private void Start()
    {
        rotateToggle.isOn = true;
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
                //case Mode.Translate:
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

    #region TOGGLE MODE
    public void SetTranslateMode()
    {
        if (currentMode == Mode.Translate)
        {
            translateToggle.isOn = true;
            return;
        }

        currentMode = Mode.None;

        rotateToggle.isOn = false;
        scaleToggle.isOn = false;

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
            rotateToggle.isOn = true;
            return;
        }

        currentMode = Mode.None;

        translateToggle.isOn = false;
        scaleToggle.isOn = false;
        
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
            scaleToggle.isOn = true;
            return;
        }

        currentMode = Mode.None;

        translateToggle.isOn = false;
        rotateToggle.isOn = false;

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

    #region TRASNFORM SLIDER
    public void XSliderChanged()
    {
        CheckIfAxisChanged(Axis.X);

        xText.text = xSlider.value.ToString(sliderValueStringFormat);
        UpdateObject(xSlider.value, Axis.X);
    }

    public void YSliderChanged()
    {
        CheckIfAxisChanged(Axis.Y);

        yText.text = ySlider.value.ToString(sliderValueStringFormat);
        UpdateObject(ySlider.value, Axis.Y);
    }

    public void ZSliderChanged()
    {
        CheckIfAxisChanged(Axis.Z);

        zText.text = zSlider.value.ToString(sliderValueStringFormat);
        UpdateObject(zSlider.value, Axis.Z);
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

    private void UpdateObject(float value, Axis axis)
    {
        switch (currentMode)
        {
            case Mode.Translate:
                ObjectManager.Instance.TranslateSelected(value, axis);
                break;
            case Mode.Rotate:
                ObjectManager.Instance.RotateSelected(value, axis);
                break;
            case Mode.Scale:
                ObjectManager.Instance.ScaleSelected(value, axis);
                break;
        }
    }
    #endregion

    #region RESOLUTION SLIDER
    public void ResolutionSliderChanged(float value)
    {
        resText.text = resSlider.value.ToString();
        MeshController.Instance.SetMesh((int)value);
    }
    #endregion
}
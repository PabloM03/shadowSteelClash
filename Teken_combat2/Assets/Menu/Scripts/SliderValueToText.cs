using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SliderValueToText : MonoBehaviour
{
    public Slider slider;
    public TextMeshProUGUI valueTextTMP;

    void Start()
    {
        if (slider != null)
        {
            slider.onValueChanged.AddListener(UpdateText);
            UpdateText(slider.value);
        }
    }

    void UpdateText(float value)
    {
        if (valueTextTMP != null)
        {
            valueTextTMP.text = value.ToString("0.00");
        }
    }

    private void OnDestroy()
    {
        if (slider != null)
        {
            slider.onValueChanged.RemoveListener(UpdateText);
        }
    }
}

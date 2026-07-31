using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SliderValuePercentText : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _sliderValuePercentText;
    [SerializeField] private Slider _slider;

    private void Awake()
    {
        _slider.onValueChanged.AddListener(ChangeValueText);
        ChangeValueText(0);
    }

    private void ChangeValueText(float temp)
    {
        int value = (int)(_slider.value * 100);
        _sliderValuePercentText.text = "%" + value;
    }

}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public class PauseMenu : MonoBehaviour
{
    [SerializeField] private GameObject _baseMenu;
    [SerializeField] private GameObject _exitMenu;
    [SerializeField] private GameObject _restartMenu;

    [SerializeField] private Slider _brightnessSlider;
    [SerializeField] private Slider _soundSlider;
    [SerializeField] private Image _brightnessImage;
    private static bool isSoundGot = false;
    private static float brightnessValue = 1;
    private float a;
    private void Start()
    {
        _brightnessSlider.onValueChanged.AddListener(ChangeBrightness);
        _soundSlider.onValueChanged.AddListener(ChangeSound);
        _brightnessSlider.value = brightnessValue;

        float configuredVolume = Mathf.Clamp01(Legacy2D.PlayBoxLauncherDataManager.GetSound);
        _soundSlider.SetValueWithoutNotify(configuredVolume);
        AudioListener.volume = configuredVolume;

        isSoundGot = true;
    }

    public void OpenPanel(int dir)
    {
        transform.eulerAngles = dir == 1 ? 180 * Vector3.forward : 0 * Vector3.forward;
        ToggleMenu(true);
    }

    public void ToggleMenu(bool state)
    {
        ToggleExitMenu(false);
        _baseMenu.SetActive(state);

        if (state)
            Time.timeScale = 0;
        else
            Time.timeScale = 1;
    }

    public void ToggleExitMenu(bool state)
    {
        _exitMenu.SetActive(state);
        _baseMenu.SetActive(!state);
    }

    public void ToggleRestartMenu(bool state)
    {
        _restartMenu.SetActive(state);
        _baseMenu.SetActive(!state);
    }

    private void ChangeBrightness(float value)
    {
        brightnessValue = value;
        value -= 1;
        value = Mathf.Abs(value);
        Color color = _brightnessImage.color;
        color.a = (value * 200f) / 255f;
        _brightnessImage.color = color;
    }

    private void ChangeSound(float value)
    {
        Legacy2D.PlayBoxLauncherDataManager.SetSound(value);
        AudioListener.volume = value;
    }

    public void Restart()
    {
        Time.timeScale = 1;

        SceneManager.LoadScene(0);
    }

    public void ExitGame()
    {
        Time.timeScale = 1;

        Application.Quit();
    }

    private void OnApplicationQuit()
    {
        brightnessValue = 1;
        isSoundGot = false;
    }
}

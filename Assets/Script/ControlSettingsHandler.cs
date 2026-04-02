using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ControlSettingsHandler : MonoBehaviour
{
    [Header("UI Components")]
    public Slider sensitivitySlider;
    public TMP_Text sensitivityValueText;
    public Toggle invertYToggle;

    private const string PREF_SENSITIVITY = "MouseSensitivity";
    private const string PREF_INVERT_Y = "InvertY";
    private bool isWired = false;

    void OnEnable()
    {
        float savedSens = PlayerPrefs.GetFloat(PREF_SENSITIVITY, 2.0f);
        bool savedInvert = PlayerPrefs.GetInt(PREF_INVERT_Y, 0) == 1;

        sensitivitySlider.value = savedSens;
        if (sensitivityValueText != null)
            sensitivityValueText.text = savedSens.ToString("F1");
        invertYToggle.isOn = savedInvert;

        // Wire once only
        if (!isWired)
        {
            sensitivitySlider.onValueChanged.AddListener(SetSensitivity);
            invertYToggle.onValueChanged.AddListener(SetInvertY);
            isWired = true;
        }
    }

    public void SetSensitivity(float value)
    {
        if (sensitivityValueText != null)
            sensitivityValueText.text = value.ToString("F1");

        PlayerPrefs.SetFloat(PREF_SENSITIVITY, value);
        PlayerPrefs.Save();
        SyncToCloud();
    }

    public void SetInvertY(bool isInverted)
    {
        PlayerPrefs.SetInt(PREF_INVERT_Y, isInverted ? 1 : 0);
        PlayerPrefs.Save();
        SyncToCloud();
    }

    void SyncToCloud()
    {
        if (PlayerSettingsCloud.Instance != null)
            PlayerSettingsCloud.Instance.SaveSettings();
    }
}
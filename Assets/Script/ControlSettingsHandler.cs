using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ControlSettingsHandler : MonoBehaviour
{
    [Header("UI Components")]
    public Slider sensitivitySlider;
    public TMP_Text sensitivityValueText;
    public Toggle invertYToggle;

    // We use constants to make sure we don't misspell the save keys later
    private const string PREF_SENSITIVITY = "MouseSensitivity";
    private const string PREF_INVERT_Y = "InvertY";

    void Start()
    {
        // --- 1. LOAD DATA ---
        // Get the saved float, or default to 2.0 if no save exists
        float savedSens = PlayerPrefs.GetFloat(PREF_SENSITIVITY, 2.0f);
        // Get the saved int (0=False, 1=True), default to 0
        bool savedInvert = PlayerPrefs.GetInt(PREF_INVERT_Y, 0) == 1;

        // --- 2. UPDATE UI VISUALS ---
        // Set the slider handle to the correct spot
        sensitivitySlider.value = savedSens;
        // Update the text number (e.g. "2.5")
        sensitivityValueText.text = savedSens.ToString("F1");
        // Set the checkbox checkmark
        invertYToggle.isOn = savedInvert;

        // --- 3. ADD LISTENERS (The Auto-Wiring) ---
        // This tells the script: "When slider moves, run SetSensitivity()"
        sensitivitySlider.onValueChanged.AddListener(SetSensitivity);
        invertYToggle.onValueChanged.AddListener(SetInvertY);
    }

    // Called automatically when Slider moves
    public void SetSensitivity(float value)
    {
        // Update the text number instantly
        sensitivityValueText.text = value.ToString("F1");
        
        // Save to hard drive
        PlayerPrefs.SetFloat(PREF_SENSITIVITY, value);
        PlayerPrefs.Save();
    }

    // Called automatically when Toggle is clicked
    public void SetInvertY(bool isInverted)
    {
        // Save 1 for True, 0 for False
        PlayerPrefs.SetInt(PREF_INVERT_Y, isInverted ? 1 : 0);
        PlayerPrefs.Save();
    }
}
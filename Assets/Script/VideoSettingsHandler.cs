using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class VideoSettingsHandler : MonoBehaviour
{
    [Header("UI Text Displays")]
    public TMP_Text resolutionText;
    public TMP_Text fullscreenText;
    public TMP_Text vsyncText;
    public TMP_Text qualityText;

    [Header("Brightness Components")]
    public Slider brightnessSlider;
    public Volume globalVolume; 

    private Resolution[] resolutions;
    private int resIndex;
    private string[] qualityLevels;
    private int qualityIndex;
    private ColorAdjustments colorAdjustments; 

    // Key to save data on disk
    private const string PREF_BRIGHTNESS = "MasterBrightness";

    void Start()
    {
        // 1. SETUP RESOLUTIONS
        resolutions = Screen.resolutions;
        for (int i = 0; i < resolutions.Length; i++)
        {
            if (resolutions[i].width == Screen.currentResolution.width && 
                resolutions[i].height == Screen.currentResolution.height)
                resIndex = i;
        }

        // 2. SETUP QUALITY
        qualityLevels = QualitySettings.names;
        qualityIndex = QualitySettings.GetQualityLevel();

        // 3. SETUP BRIGHTNESS & LOAD SAVE
        if (globalVolume != null && globalVolume.profile.TryGet(out colorAdjustments))
        {
            // Load saved brightness (Default to 0.5 if nothing saved)
            float savedValue = PlayerPrefs.GetFloat(PREF_BRIGHTNESS, 0.5f);
            
            // Set the slider visual to match saved value
            if(brightnessSlider != null)
                brightnessSlider.value = savedValue;

            // Apply the actual brightness to the screen immediately
            SetBrightness(savedValue);
        }
        else
        {
            Debug.LogWarning("Color Adjustments not found in Global Volume!");
        }

        // 4. INITIALIZE UI
        UpdateUI();
    }

    // --- RESOLUTION ---
    public void NextResolution() { resIndex = (resIndex + 1) % resolutions.Length; ApplyAndSave(); }
    public void PrevResolution() { resIndex = (resIndex - 1 + resolutions.Length) % resolutions.Length; ApplyAndSave(); }

    // --- FULLSCREEN ---
    public void ToggleFullscreen() { Screen.fullScreen = !Screen.fullScreen; UpdateUI(); }

    // --- VSYNC ---
    public void ToggleVSync() { QualitySettings.vSyncCount = (QualitySettings.vSyncCount == 0) ? 1 : 0; UpdateUI(); }

    // --- QUALITY ---
    public void NextQuality() 
    { 
        qualityIndex = (qualityIndex + 1) % qualityLevels.Length; 
        ApplyQuality(); 
    }

    public void PrevQuality() 
    { 
        qualityIndex = (qualityIndex - 1 + qualityLevels.Length) % qualityLevels.Length; 
        ApplyQuality(); 
    }

    void ApplyQuality()
    {
        QualitySettings.SetQualityLevel(qualityIndex);
        qualityText.text = qualityLevels[qualityIndex].ToUpper();
    }

    // --- BRIGHTNESS (Updated with Saving) ---
    public void SetBrightness(float value)
    {
        if (colorAdjustments != null)
        {
            // Map 0-1 slider to -5 to +5 Exposure
            float intensity = Mathf.Lerp(-5f, 5f, value);
            colorAdjustments.postExposure.value = intensity;

            // SAVE to disk automatically whenever slider moves
            PlayerPrefs.SetFloat(PREF_BRIGHTNESS, value);
            PlayerPrefs.Save();
        }
    }

    void ApplyAndSave()
    {
        Screen.SetResolution(resolutions[resIndex].width, resolutions[resIndex].height, Screen.fullScreen);
        UpdateUI();
    }

    void UpdateUI()
    {
        resolutionText.text = resolutions[resIndex].width + " x " + resolutions[resIndex].height;
        fullscreenText.text = Screen.fullScreen ? "ON" : "OFF";
        vsyncText.text = (QualitySettings.vSyncCount > 0) ? "ON" : "OFF";
        qualityText.text = qualityLevels[qualityIndex].ToUpper();
    }
}
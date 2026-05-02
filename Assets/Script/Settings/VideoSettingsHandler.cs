using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class VideoSettingsHandler : MonoBehaviour
{
    [Header("Resolution")]
    public TMP_Text resolutionText;
    public Button resolutionLeftBtn;
    public Button resolutionRightBtn;

    [Header("Full Screen")]
    public TMP_Text fullscreenText;
    public Button fullscreenLeftBtn;
    public Button fullscreenRightBtn;

    [Header("VSync")]
    public TMP_Text vsyncText;
    public Button vsyncLeftBtn;
    public Button vsyncRightBtn;

    [Header("Brightness")]
    public Slider brightnessSlider;
    public TMP_Text brightnessPercentText;
    public Volume globalVolume;

    private Resolution[] resolutions;
    private int resIndex;
    private ColorAdjustments colorAdjustments;
    private bool isWired = false;

    private const string PREF_BRIGHTNESS = "MasterBrightness";

    void OnEnable()
    {
        resolutions = Screen.resolutions;
        for (int i = 0; i < resolutions.Length; i++)
        {
            if (resolutions[i].width == Screen.currentResolution.width &&
                resolutions[i].height == Screen.currentResolution.height)
                resIndex = i;
        }

        if (globalVolume != null && globalVolume.profile.TryGet(out colorAdjustments))
        {
            float savedValue = PlayerPrefs.GetFloat(PREF_BRIGHTNESS, 0.5f);
            if (brightnessSlider != null)
                brightnessSlider.value = savedValue;
            SetBrightness(savedValue);
        }

        // Wire buttons once only
        if (!isWired)
        {
            if (resolutionLeftBtn != null) resolutionLeftBtn.onClick.AddListener(PrevResolution);
            if (resolutionRightBtn != null) resolutionRightBtn.onClick.AddListener(NextResolution);
            if (fullscreenLeftBtn != null) fullscreenLeftBtn.onClick.AddListener(ToggleFullscreen);
            if (fullscreenRightBtn != null) fullscreenRightBtn.onClick.AddListener(ToggleFullscreen);
            if (vsyncLeftBtn != null) vsyncLeftBtn.onClick.AddListener(ToggleVSync);
            if (vsyncRightBtn != null) vsyncRightBtn.onClick.AddListener(ToggleVSync);
            if (brightnessSlider != null) brightnessSlider.onValueChanged.AddListener(SetBrightness);
            isWired = true;
        }

        UpdateUI();
    }

    public void NextResolution()
    {
        resIndex = (resIndex + 1) % resolutions.Length;
        Screen.SetResolution(resolutions[resIndex].width, resolutions[resIndex].height, Screen.fullScreen);
        UpdateUI();
        SyncToCloud();
    }

    public void PrevResolution()
    {
        resIndex = (resIndex - 1 + resolutions.Length) % resolutions.Length;
        Screen.SetResolution(resolutions[resIndex].width, resolutions[resIndex].height, Screen.fullScreen);
        UpdateUI();
        SyncToCloud();
    }

    public void ToggleFullscreen()
    {
        Screen.fullScreen = !Screen.fullScreen;
        UpdateUI();
        SyncToCloud();
    }

    public void ToggleVSync()
    {
        QualitySettings.vSyncCount = (QualitySettings.vSyncCount == 0) ? 1 : 0;
        UpdateUI();
        SyncToCloud();
    }

    public void SetBrightness(float value)
    {
        if (colorAdjustments != null)
        {
            float intensity = Mathf.Lerp(-5f, 5f, value);
            colorAdjustments.postExposure.value = intensity;
            PlayerPrefs.SetFloat(PREF_BRIGHTNESS, value);
            PlayerPrefs.Save();
        }

        if (brightnessPercentText != null)
            brightnessPercentText.text = Mathf.RoundToInt(value * 100) + "%";

        SyncToCloud();
    }

    void UpdateUI()
    {
        if (resolutionText != null && resolutions != null && resolutions.Length > 0)
            resolutionText.text = resolutions[resIndex].width + " x " + resolutions[resIndex].height;

        if (fullscreenText != null)
            fullscreenText.text = Screen.fullScreen ? "ON" : "OFF";

        if (vsyncText != null)
            vsyncText.text = (QualitySettings.vSyncCount > 0) ? "ON" : "OFF";
    }

    void SyncToCloud()
    {
        if (PlayerSettingsCloud.Instance != null)
            PlayerSettingsCloud.Instance.SaveSettings();
    }
}
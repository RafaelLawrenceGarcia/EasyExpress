using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using TMPro;

public class AudioSettingsHandler : MonoBehaviour
{
    [Header("Components")]
    public AudioMixer mainMixer;

    [Header("Sliders")]
    public Slider masterSlider;
    public Slider musicSlider;
    public Slider sfxSlider;

    [Header("Percentage Text (Optional)")]
    public TMP_Text masterPercentText;
    public TMP_Text musicPercentText;
    public TMP_Text sfxPercentText;

    private const string MIXER_MASTER = "MasterVol";
    private const string MIXER_MUSIC = "MusicVol";
    private const string MIXER_SFX = "SFXVol";

    void OnEnable()
    {
        float savedMaster = PlayerPrefs.GetFloat("AudioMaster", 1f);
        float savedMusic = PlayerPrefs.GetFloat("AudioMusic", 1f);
        float savedSfx = PlayerPrefs.GetFloat("AudioSFX", 1f);

        // Remove old listeners to prevent stacking
        masterSlider.onValueChanged.RemoveListener(SetMasterVolume);
        musicSlider.onValueChanged.RemoveListener(SetMusicVolume);
        sfxSlider.onValueChanged.RemoveListener(SetSFXVolume);

        masterSlider.value = savedMaster;
        musicSlider.value = savedMusic;
        sfxSlider.value = savedSfx;

        SetMasterVolume(savedMaster);
        SetMusicVolume(savedMusic);
        SetSFXVolume(savedSfx);

        masterSlider.onValueChanged.AddListener(SetMasterVolume);
        musicSlider.onValueChanged.AddListener(SetMusicVolume);
        sfxSlider.onValueChanged.AddListener(SetSFXVolume);
    }

    public void SetMasterVolume(float value)
    {
        float dB = Mathf.Log10(Mathf.Clamp(value, 0.0001f, 1f)) * 20;
        if (mainMixer != null) mainMixer.SetFloat(MIXER_MASTER, dB);
        if (masterPercentText != null) masterPercentText.text = Mathf.RoundToInt(value * 100) + "%";
        PlayerPrefs.SetFloat("AudioMaster", value);
        SyncToCloud();
    }

    public void SetMusicVolume(float value)
    {
        float dB = Mathf.Log10(Mathf.Clamp(value, 0.0001f, 1f)) * 20;
        if (mainMixer != null) mainMixer.SetFloat(MIXER_MUSIC, dB);
        if (musicPercentText != null) musicPercentText.text = Mathf.RoundToInt(value * 100) + "%";
        PlayerPrefs.SetFloat("AudioMusic", value);
        SyncToCloud();
    }

    public void SetSFXVolume(float value)
    {
        float dB = Mathf.Log10(Mathf.Clamp(value, 0.0001f, 1f)) * 20;
        if (mainMixer != null) mainMixer.SetFloat(MIXER_SFX, dB);
        if (sfxPercentText != null) sfxPercentText.text = Mathf.RoundToInt(value * 100) + "%";
        PlayerPrefs.SetFloat("AudioSFX", value);
        SyncToCloud();
    }

    void SyncToCloud()
    {
        if (PlayerSettingsCloud.Instance != null)
            PlayerSettingsCloud.Instance.SaveSettings();
    }
}
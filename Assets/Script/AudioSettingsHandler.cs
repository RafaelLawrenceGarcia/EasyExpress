using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class AudioSettingsHandler : MonoBehaviour
{
    [Header("Components")]
    public AudioMixer mainMixer; // Drag your MainMixer asset here

    [Header("Sliders")]
    public Slider masterSlider;
    public Slider musicSlider;
    public Slider sfxSlider;

    // These must match EXACTLY what you named them in the Mixer window!
    private const string MIXER_MASTER = "MasterVol";
    private const string MIXER_MUSIC = "MusicVol";
    private const string MIXER_SFX = "SFXVol";

    void Start()
    {
        // Set sliders to max (1) by default
        // In a real game, you would load these from PlayerPrefs
        masterSlider.value = 1f;
        musicSlider.value = 1f;
        sfxSlider.value = 1f;
    }

    public void SetMasterVolume(float value)
    {
        // Logarithmic conversion for natural volume fading
        float dB = Mathf.Log10(Mathf.Clamp(value, 0.0001f, 1f)) * 20;
        mainMixer.SetFloat(MIXER_MASTER, dB);
    }

    public void SetMusicVolume(float value)
    {
        float dB = Mathf.Log10(Mathf.Clamp(value, 0.0001f, 1f)) * 20;
        mainMixer.SetFloat(MIXER_MUSIC, dB);
    }

    public void SetSFXVolume(float value)
    {
        float dB = Mathf.Log10(Mathf.Clamp(value, 0.0001f, 1f)) * 20;
        mainMixer.SetFloat(MIXER_SFX, dB);
    }
}
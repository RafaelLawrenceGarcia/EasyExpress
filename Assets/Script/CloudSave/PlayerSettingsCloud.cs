// ============================================================
//  PlayerSettingsCloud.cs — REFACTORED
//  Easy Express — Per-Account Settings Sync
// ============================================================
//  Location: Assets/Script/CloudSave/PlayerSettingsCloud.cs
//
//  DEPENDENCIES: Core/GameEventBus, Core/AuthEvents, Core/GameSession
//  DEPENDS ON:   Auth/ → NOTHING (subscribes to LoginSuccessEvent)
//
//  CHANGE: Added OnEnable/OnDisable subscription to LoginSuccessEvent.
//  AuthManager no longer calls LoadSettings() directly.
// ============================================================

using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using System.Collections.Generic;

[System.Serializable]
public class PlayerSettingsData
{
    public float masterVolume = 1f;
    public float musicVolume = 1f;
    public float sfxVolume = 1f;
    public int resolutionWidth = 1920;
    public int resolutionHeight = 1080;
    public bool fullscreen = true;
    public int vsyncCount = 1;
    public float brightness = 0.5f;
    public float sensitivity = 2f;
    public bool invertY = false;
}

public class PlayerSettingsCloud : MonoBehaviour
{
    public static PlayerSettingsCloud Instance;

    private const string PLAYFAB_KEY = "PlayerSettings";
    private float saveTimer = -1f;
    private const float SAVE_DELAY = 2f;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // ── EVENT SUBSCRIPTION (replaces AuthManager direct call) ──
    void OnEnable()
    {
        GameEventBus.Subscribe<LoginSuccessEvent>(OnLoginSuccess);
    }

    void OnDisable()
    {
        GameEventBus.Unsubscribe<LoginSuccessEvent>(OnLoginSuccess);
    }

    void OnLoginSuccess(LoginSuccessEvent evt)
    {
        LoadSettings();
        Debug.Log("[PlayerSettingsCloud] Login detected — loading cloud settings.");
    }
    // ───────────────────────────────────────────────────────────

    void Update()
    {
        if (saveTimer > 0f)
        {
            saveTimer -= Time.unscaledDeltaTime;
            if (saveTimer <= 0f)
            {
                saveTimer = -1f;
                UploadToCloud();
            }
        }
    }

    public void SaveSettings()
    {
        saveTimer = SAVE_DELAY;
    }

    void UploadToCloud()
    {
        if (!GameSession.IsLoggedIn) return;

        PlayerSettingsData data = new PlayerSettingsData();
        data.masterVolume = PlayerPrefs.GetFloat("AudioMaster", 1f);
        data.musicVolume = PlayerPrefs.GetFloat("AudioMusic", 1f);
        data.sfxVolume = PlayerPrefs.GetFloat("AudioSFX", 1f);
        data.resolutionWidth = Screen.currentResolution.width;
        data.resolutionHeight = Screen.currentResolution.height;
        data.fullscreen = Screen.fullScreen;
        data.vsyncCount = QualitySettings.vSyncCount;
        data.brightness = PlayerPrefs.GetFloat("MasterBrightness", 0.5f);
        data.sensitivity = PlayerPrefs.GetFloat("MouseSensitivity", 2f);
        data.invertY = PlayerPrefs.GetInt("InvertY", 0) == 1;

        string json = JsonUtility.ToJson(data);
        var request = new UpdateUserDataRequest
        {
            Data = new Dictionary<string, string> { { PLAYFAB_KEY, json } }
        };

        PlayFabClientAPI.UpdateUserData(request,
            result => Debug.Log("[Settings] Saved to cloud."),
            error => Debug.LogWarning("[Settings] Cloud save failed: " + error.ErrorMessage));
    }

    public void LoadSettings()
    {
        if (!GameSession.IsLoggedIn)
        {
            Debug.Log("[Settings] Guest mode — using local PlayerPrefs.");
            return;
        }

        ApplySettings(new PlayerSettingsData());

        PlayFabClientAPI.GetUserData(new GetUserDataRequest(),
            result =>
            {
                if (result.Data != null && result.Data.ContainsKey(PLAYFAB_KEY))
                {
                    string json = result.Data[PLAYFAB_KEY].Value;
                    PlayerSettingsData data = JsonUtility.FromJson<PlayerSettingsData>(json);
                    ApplySettings(data);
                    Debug.Log("[Settings] Loaded from cloud and applied.");
                }
                else
                {
                    Debug.Log("[Settings] No cloud settings — defaults applied.");
                }
            },
            error => Debug.LogWarning("[Settings] Cloud load failed: " + error.ErrorMessage));
    }

    void ApplySettings(PlayerSettingsData data)
    {
        PlayerPrefs.SetFloat("AudioMaster", data.masterVolume);
        PlayerPrefs.SetFloat("AudioMusic", data.musicVolume);
        PlayerPrefs.SetFloat("AudioSFX", data.sfxVolume);
        PlayerPrefs.SetFloat("MasterBrightness", data.brightness);
        Screen.SetResolution(data.resolutionWidth, data.resolutionHeight, data.fullscreen);
        QualitySettings.vSyncCount = data.vsyncCount;
        PlayerPrefs.SetFloat("MouseSensitivity", data.sensitivity);
        PlayerPrefs.SetInt("InvertY", data.invertY ? 1 : 0);
        PlayerPrefs.Save();

        RefreshAudioHandler(data);
        RefreshVideoHandler(data);
        RefreshControlsHandler(data);
    }

    void RefreshAudioHandler(PlayerSettingsData data)
    {
        AudioSettingsHandler audio = FindFirstObjectByType<AudioSettingsHandler>();
        if (audio == null) return;
        if (audio.masterSlider != null) audio.masterSlider.value = data.masterVolume;
        if (audio.musicSlider != null) audio.musicSlider.value = data.musicVolume;
        if (audio.sfxSlider != null) audio.sfxSlider.value = data.sfxVolume;
    }

    void RefreshVideoHandler(PlayerSettingsData data)
    {
        VideoSettingsHandler video = FindFirstObjectByType<VideoSettingsHandler>();
        if (video == null) return;
        if (video.brightnessSlider != null) video.brightnessSlider.value = data.brightness;
    }

    void RefreshControlsHandler(PlayerSettingsData data)
    {
        ControlSettingsHandler controls = FindFirstObjectByType<ControlSettingsHandler>();
        if (controls == null) return;
        if (controls.sensitivitySlider != null) controls.sensitivitySlider.value = data.sensitivity;
        if (controls.invertYToggle != null) controls.invertYToggle.isOn = data.invertY;
    }
}
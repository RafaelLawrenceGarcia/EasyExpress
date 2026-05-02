// ============================================================
//  MainMenuController.cs — REFACTORED (Self-Contained)
//  Easy Express — Main Menu System
// ============================================================
//  Location: Assets/Script/MainMenu/MainMenuController.cs
//
//  Manages main menu panels, save data checking, scene loading.
//  Reacts to login/logout via events from Core/AuthEvents.
//
//  DEPENDENCIES: Core/GameEventBus, Core/AuthEvents, Core/GameSession
//                CloudSave/CloudDataHandler (for PreloadAndContinue)
//                DayCycle/DayTransitionManager (for ResetDayFlag)
//  DEPENDS ON:   Auth/ → NOTHING (fully decoupled)
//
//  SCENE SETUP:
//    MainMenu root GameObject must be ACTIVE in the scene.
//    All child panels start hidden (Start does this).
//    Panels appear when LoginSuccessEvent or GuestLoginEvent arrives.
// ============================================================

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using PlayFab;
using PlayFab.ClientModels;

public class MainMenu : MonoBehaviour
{
    private bool cloudHasSaveData = false;

    [Header("Main Buttons")]
    public Button playButton;
    public Button creditsButton;
    public Button optionsButton;
    public Button logoutButton;

    [Header("Mode Selection Buttons")]
    public Button singleplayerButton;
    public Button multiplayerButton;
    public Button backToMainButton;

    [Header("Save Selection (Optional)")]
    public GameObject saveSelectionPanel;
    public Button continueButton;
    public Button newGameButton;
    public Button backToModeButton;

    [Header("Save Info Display")]
    public Text saveInfoText;

    [Header("Panels")]
    public GameObject mainPanel;
    public GameObject modeSelectionPanel;
    public GameObject optionsPanel;
    public GameObject creditsPanel;
    public Text statusText;

    [Header("Options Back Button")]
    public Button optionsBackButton;

    [Header("Settings")]
    public string gameplaySceneName = "Gameplay";

    // ═══════════════════════════════════════════════════════════
    //  EVENT SUBSCRIPTIONS
    // ═══════════════════════════════════════════════════════════

    void OnEnable()
    {
        GameEventBus.Subscribe<LoginSuccessEvent>(OnLoginSuccess);
        GameEventBus.Subscribe<GuestLoginEvent>(OnGuestLogin);
    }

    void OnDisable()
    {
        GameEventBus.Unsubscribe<LoginSuccessEvent>(OnLoginSuccess);
        GameEventBus.Unsubscribe<GuestLoginEvent>(OnGuestLogin);
    }

    // ═══════════════════════════════════════════════════════════
    //  LIFECYCLE
    // ═══════════════════════════════════════════════════════════

    void Start()
    {
        // All panels hidden until auth event arrives
        if (mainPanel != null) mainPanel.SetActive(false);
        if (modeSelectionPanel != null) modeSelectionPanel.SetActive(false);
        if (saveSelectionPanel != null) saveSelectionPanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(false);
        if (creditsPanel != null) creditsPanel.SetActive(false);

        // Wire buttons
        if (playButton != null) playButton.onClick.AddListener(OpenModeSelection);
        if (optionsButton != null) optionsButton.onClick.AddListener(OpenOptions);
        if (creditsButton != null) creditsButton.onClick.AddListener(OpenCredits);
        if (logoutButton != null) logoutButton.onClick.AddListener(Logout);

        if (singleplayerButton != null) singleplayerButton.onClick.AddListener(StartSingleplayer);
        if (multiplayerButton != null) multiplayerButton.onClick.AddListener(() => Debug.Log("Multiplayer TBD!"));
        if (backToMainButton != null) backToMainButton.onClick.AddListener(ShowMainPanel);

        if (newGameButton != null) newGameButton.onClick.AddListener(NewGame);
        if (continueButton != null) continueButton.onClick.AddListener(ContinueGame);
        if (backToModeButton != null) backToModeButton.onClick.AddListener(OpenModeSelection);

        if (optionsBackButton != null) optionsBackButton.onClick.AddListener(ShowMainPanel);

        // Fallback: if auth event already fired before we subscribed
        if (GameSession.IsLoggedIn)
        {
            CheckForSaveData();
            ShowMainPanel();
        }
        else if (GameSession.IsGuest)
        {
            ShowMainPanel();
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  AUTH EVENT HANDLERS
    // ═══════════════════════════════════════════════════════════

    void OnLoginSuccess(LoginSuccessEvent evt)
    {
        Debug.Log("[MainMenu] LoginSuccessEvent received — showing main panel.");
        CheckForSaveData();
        ShowMainPanel();
    }

    void OnGuestLogin(GuestLoginEvent evt)
    {
        Debug.Log("[MainMenu] GuestLoginEvent received — showing main panel (offline).");
        ShowMainPanel();
    }

    // ═══════════════════════════════════════════════════════════
    //  PANEL NAVIGATION
    // ═══════════════════════════════════════════════════════════

    #region Panel Navigation
    public void ShowMainPanel()
    {
        SwitchPanel(mainPanel);

        if (logoutButton != null)
        {
            Text btnText = logoutButton.GetComponentInChildren<Text>();
            if (btnText != null)
                btnText.text = GameSession.IsGuest ? "BACK TO LOGIN" : "LOGOUT";
        }

        if (GameSession.IsGuest && statusText != null)
            statusText.text = "Playing Offline (Local Save)";
    }

    public void OpenModeSelection() => SwitchPanel(modeSelectionPanel);
    public void OpenSaveSelection() => SwitchPanel(saveSelectionPanel);
    public void OpenOptions() => SwitchPanel(optionsPanel);
    public void OpenCredits() => SwitchPanel(creditsPanel);

    private void SwitchPanel(GameObject activePanel)
    {
        if (mainPanel != null) mainPanel.SetActive(activePanel == mainPanel);
        if (modeSelectionPanel != null) modeSelectionPanel.SetActive(activePanel == modeSelectionPanel);
        if (saveSelectionPanel != null) saveSelectionPanel.SetActive(activePanel == saveSelectionPanel);
        if (optionsPanel != null) optionsPanel.SetActive(activePanel == optionsPanel);
        if (creditsPanel != null) creditsPanel.SetActive(activePanel == creditsPanel);
    }
    #endregion

    // ═══════════════════════════════════════════════════════════
    //  GAME LOGIC
    // ═══════════════════════════════════════════════════════════

    #region Game Logic
    void StartSingleplayer()
    {
        DayTransitionManager.ResetDayFlag();
        if (GameSession.IsLoggedIn && cloudHasSaveData)
        {
            if (CloudDataHandler.Instance != null)
            {
                CloudDataHandler.Instance.PreloadAndContinue(gameplaySceneName);
                return;
            }
            PlayerPrefs.SetInt("IsLoadingGame", 1);
        }
        else
        {
            PlayerPrefs.SetInt("IsLoadingGame", 0);
        }

        SceneManager.LoadScene(gameplaySceneName);
    }

    void CheckForSaveData()
    {
        if (!GameSession.IsLoggedIn) return;

        if (statusText) statusText.text = "Checking Save Data...";
        cloudHasSaveData = false;

        PlayFabClientAPI.GetUserData(new GetUserDataRequest(),
        result =>
        {
            if (newGameButton != null) newGameButton.interactable = true;

            if (result.Data != null && result.Data.ContainsKey("GameData"))
            {
                cloudHasSaveData = true;
                if (continueButton != null) continueButton.interactable = true;
                if (statusText) statusText.text = "Welcome!";

                string json = result.Data["GameData"].Value;
                if (!string.IsNullOrEmpty(json) && saveInfoText != null)
                {
                    try
                    {
                        GamePersistData d = JsonUtility.FromJson<GamePersistData>(json);

                        int jobCount = d.acceptedJobs != null ? d.acceptedJobs.Count : 0;
                        int invCount = d.inventoryParts != null ? d.inventoryParts.Count : 0;
                        int delCount = d.pendingDeliveries != null ? d.pendingDeliveries.Count : 0;

                        saveInfoText.text =
                            $"Day {d.currentDay}   |   \u20B1{d.gold:F2}\n" +
                            $"Jobs: {jobCount}   |   " +
                            $"Inventory: {invCount}   |   " +
                            $"Deliveries: {delCount}";
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning("[MainMenu] Failed to parse save info: " + e.Message);
                        saveInfoText.text = "";
                    }
                }
            }
            else
            {
                if (continueButton != null) continueButton.interactable = false;
                if (statusText) statusText.text = "Ready.";
                if (saveInfoText != null) saveInfoText.text = "No save data found.";
            }
        },
        error => Debug.LogWarning("Failed to get save data: " + error.ErrorMessage));
    }

    void NewGame()
    {
        PlayerPrefs.SetInt("IsLoadingGame", 0);
        PlayerPrefs.SetInt("TutorialDone", 0);
        PlayerPrefs.SetInt("CurrentDay", 1);
        if (saveInfoText != null) saveInfoText.text = "";
        SceneManager.LoadScene(gameplaySceneName);
    }

    void ContinueGame()
    {
        DayTransitionManager.ResetDayFlag();
        if (GameSession.IsLoggedIn)
        {
            if (CloudDataHandler.Instance != null)
            {
                CloudDataHandler.Instance.PreloadAndContinue(gameplaySceneName);
                return;
            }
            PlayerPrefs.SetInt("IsLoadingGame", 1);
        }
        else
        {
            PlayerPrefs.SetInt("IsLoadingGame", 0);
        }

        SceneManager.LoadScene(gameplaySceneName);
    }

    public void Logout()
    {
        // Hide OUR panels
        if (mainPanel != null) mainPanel.SetActive(false);
        if (modeSelectionPanel != null) modeSelectionPanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(false);
        if (creditsPanel != null) creditsPanel.SetActive(false);

        // Tell the auth system (and anyone else) that we want to log out
        GameEventBus.Publish(new LogoutRequestedEvent());
        Debug.Log("[MainMenu] Logout — LogoutRequestedEvent published.");
    }
    #endregion
}
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
    [Tooltip("The BackBtn inside the OptionUI — wired here so it returns to main panel.")]
    public Button optionsBackButton;

    [Header("Settings")]
    public string gameplaySceneName = "Gameplay";

    [Header("Auth")]
    public AuthManager authManager;

    void Start()
    {
        // Hide all panels on startup
        if (mainPanel != null) mainPanel.SetActive(false);
        if (modeSelectionPanel != null) modeSelectionPanel.SetActive(false);
        if (saveSelectionPanel != null) saveSelectionPanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(false);
        if (creditsPanel != null) creditsPanel.SetActive(false);

        // Main Panel
        if (playButton != null) playButton.onClick.AddListener(OpenModeSelection);
        if (optionsButton != null) optionsButton.onClick.AddListener(OpenOptions);
        if (creditsButton != null) creditsButton.onClick.AddListener(OpenCredits);
        if (logoutButton != null) logoutButton.onClick.AddListener(Logout);

        // Mode Selection
        if (singleplayerButton != null) singleplayerButton.onClick.AddListener(StartSingleplayer);
        if (multiplayerButton != null) multiplayerButton.onClick.AddListener(() => Debug.Log("Multiplayer TBD!"));
        if (backToMainButton != null) backToMainButton.onClick.AddListener(ShowMainPanel);

        // Save Selection
        if (newGameButton != null) newGameButton.onClick.AddListener(NewGame);
        if (continueButton != null) continueButton.onClick.AddListener(ContinueGame);
        if (backToModeButton != null) backToModeButton.onClick.AddListener(OpenModeSelection);

        // Options back button — returns to main panel
        if (optionsBackButton != null) optionsBackButton.onClick.AddListener(ShowMainPanel);

        if (GameSession.IsLoggedIn)
        {
            AuthManager.OnLoginSuccessEvent += CheckForSaveData;
        }
    }

    void OnDestroy() => AuthManager.OnLoginSuccessEvent -= CheckForSaveData;

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

    #region Game Logic
    void StartSingleplayer()
    {
        // Only load cloud save if we CONFIRMED save data exists
        if (GameSession.IsLoggedIn)
            PlayerPrefs.SetInt("IsLoadingGame", cloudHasSaveData ? 1 : 0);
        else
            PlayerPrefs.SetInt("IsLoadingGame", 0);

        SceneManager.LoadScene(gameplaySceneName);
    }
    void CheckForSaveData()
    {
        if (!GameSession.IsLoggedIn) return;

        if (statusText) statusText.text = "Checking Save Data...";
        cloudHasSaveData = false; // ← ADD THIS LINE
        PlayFabClientAPI.GetUserData(new GetUserDataRequest(),
        result =>
        {
            if (newGameButton != null) newGameButton.interactable = true;

            if (result.Data != null && result.Data.ContainsKey("GameData"))
            {
                cloudHasSaveData = true; // ← ADD THIS LINE
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
                            $"Day {d.currentDay}   |   ₱{d.gold:F2}\n" +
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
        if (GameSession.IsLoggedIn)
            PlayerPrefs.SetInt("IsLoadingGame", 1);
        else
            PlayerPrefs.SetInt("IsLoadingGame", 0);

        SceneManager.LoadScene(gameplaySceneName);
    }

    public void Logout()
    {
        if (GameSession.IsLoggedIn)
            PlayFabClientAPI.ForgetAllCredentials();

        GameSession.Logout();

        if (mainPanel != null) mainPanel.SetActive(false);
        if (modeSelectionPanel != null) modeSelectionPanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(false);
        if (creditsPanel != null) creditsPanel.SetActive(false);

        if (authManager != null)
        {
            if (authManager.loginOverlay != null)
                authManager.loginOverlay.SetActive(true);

            authManager.ResetToLogin();
        }
    }
    #endregion
}
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using PlayFab;
using PlayFab.ClientModels;

public class MainMenu : MonoBehaviour
{
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

    [Header("Panels")]
    public GameObject mainPanel;
    public GameObject modeSelectionPanel;
    public GameObject optionsPanel;
    public GameObject creditsPanel;
    public Text statusText; 

    [Header("Settings")]
    public string gameplaySceneName = "Gameplay"; // Make sure this matches your exact scene name!

    void Start()
    {
        // Main Panel
        if (playButton != null) playButton.onClick.AddListener(OpenModeSelection);
        if (optionsButton != null) optionsButton.onClick.AddListener(OpenOptions);
        if (creditsButton != null) creditsButton.onClick.AddListener(OpenCredits);
        if (logoutButton != null) logoutButton.onClick.AddListener(Logout);

        // Mode Selection
        // ---> CHANGED: Now skips save selection and loads the scene directly
        if (singleplayerButton != null) singleplayerButton.onClick.AddListener(StartSingleplayer);
        if (multiplayerButton != null) multiplayerButton.onClick.AddListener(() => Debug.Log("Multiplayer TBD!"));
        if (backToMainButton != null) backToMainButton.onClick.AddListener(ShowMainPanel);

        // Save Selection (Safe to leave empty in Inspector for now)
        if (newGameButton != null) newGameButton.onClick.AddListener(NewGame);
        if (continueButton != null) continueButton.onClick.AddListener(ContinueGame);
        if (backToModeButton != null) backToModeButton.onClick.AddListener(OpenModeSelection);

        AuthManager.OnLoginSuccessEvent += CheckForSaveData;
    }

    void OnDestroy() => AuthManager.OnLoginSuccessEvent -= CheckForSaveData;

    #region Panel Navigation
    public void ShowMainPanel() => SwitchPanel(mainPanel);
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
        // Loads the gameplay scene immediately
        SceneManager.LoadScene(gameplaySceneName);
    }

    void CheckForSaveData()
    {
        if(statusText) statusText.text = "Checking Save Data...";

        PlayFabClientAPI.GetUserData(new GetUserDataRequest(), 
        result => 
        {
            if (newGameButton != null) newGameButton.interactable = true; 
            if (result.Data != null && result.Data.ContainsKey("Gold"))
            {
                if (continueButton != null) continueButton.interactable = true; 
                if(statusText) statusText.text = "Welcome Back!";
            }
            else
            {
                if (continueButton != null) continueButton.interactable = false;
                if(statusText) statusText.text = "Ready.";
            }
        }, 
        error => Debug.LogError("Failed to get data"));
    }

    void NewGame()
    {
        PlayerPrefs.SetInt("IsLoadingGame", 0);
        PlayerPrefs.SetInt("TutorialDone", 0);
        PlayerPrefs.SetInt("CurrentDay", 1);
        SceneManager.LoadScene(gameplaySceneName);
    }

    void ContinueGame()
    {
        PlayerPrefs.SetInt("IsLoadingGame", 1);
        SceneManager.LoadScene(gameplaySceneName);
    }

    public void Logout()
    {
        PlayFabClientAPI.ForgetAllCredentials();
        AuthManager auth = Object.FindFirstObjectByType<AuthManager>();
        if (auth != null) auth.ResetToLogin();
    }
    #endregion
}
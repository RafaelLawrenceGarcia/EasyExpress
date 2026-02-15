using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using PlayFab;
using PlayFab.ClientModels;
using System.Collections.Generic;

public class MainMenu : MonoBehaviour
{
    [Header("Buttons")]
    public Button continueButton;
    public Button newGameButton;
    public Button optionsButton;
    public Button quitButton;

    [Header("Panels")]
    public GameObject mainPanel;
    public GameObject optionsPanel;
    public Text statusText; 

    [Header("Settings")]
    public string firstLevelName = "City";

    void Start()
    {
        continueButton.interactable = false;
        newGameButton.interactable = false; 

        newGameButton.onClick.AddListener(NewGame);
        optionsButton.onClick.AddListener(OpenOptions);
        quitButton.onClick.AddListener(QuitGame);
        continueButton.onClick.AddListener(ContinueGame);

        // --- FIXED: LISTEN TO THE NEW NAME ---
        AuthManager.OnLoginSuccessEvent += CheckForSaveData;
    }

    void OnDestroy()
    {
        // --- FIXED: UNSUBSCRIBE FROM THE NEW NAME ---
        AuthManager.OnLoginSuccessEvent -= CheckForSaveData;
    }

    // --- PLAYFAB LOGIC ---

    void CheckForSaveData()
    {
        if(statusText) statusText.text = "Checking Save Data...";

        PlayFabClientAPI.GetUserData(new GetUserDataRequest(), 
        result => 
        {
            newGameButton.interactable = true; 

            if (result.Data != null && result.Data.ContainsKey("Gold"))
            {
                Debug.Log("Save Data Found!");
                continueButton.interactable = true; 
                if(statusText) statusText.text = "Welcome Back!";
            }
            else
            {
                Debug.Log("No Save Data Found (New Player)");
                continueButton.interactable = false;
                if(statusText) statusText.text = "Ready.";
            }
        }, 
        error => Debug.LogError("Failed to get data"));
    }

    void NewGame()
    {
        PlayerPrefs.SetInt("IsLoadingGame", 0);
        SceneManager.LoadScene(firstLevelName);
    }

    void ContinueGame()
    {
        PlayerPrefs.SetInt("IsLoadingGame", 1);
        SceneManager.LoadScene(firstLevelName);
    }

    void OpenOptions()
    {
        mainPanel.SetActive(false);
        optionsPanel.SetActive(true);
    }

    public void CloseOptions() 
    {
        optionsPanel.SetActive(false);
        mainPanel.SetActive(true);
    }

    void QuitGame() => Application.Quit();
}
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using PlayFab;
using PlayFab.ClientModels;

public class DemoLockManager : MonoBehaviour
{
    public static DemoLockManager Instance;

    [Header("UI References (auto-wired by editor script)")]
    public GameObject lockPanel;
    public Button buyButton;
    public Button mainMenuButton;

    [Header("Settings")]
    [Tooltip("The day the demo locks (player can finish day before this).")]
    public int lockAtDay = 4;

    [Tooltip("URL to your game's purchase page.")]
    public string purchaseURL = "https://easy-express-sites-rafcows-projects.vercel.app/#/";

    [Tooltip("Scene name of your main menu.")]
    public string mainMenuScene = "MainMenu";

    [Header("PlayFab Key")]
    [Tooltip("The PlayFab player data key your groupmate uses to track purchase.\n" +
             "e.g. 'HasPurchased', 'GameOwned', 'FullVersion', etc.")]
    public string purchaseDataKey = "HasPurchased";

    private bool hasPurchased = false;
    private bool checkComplete = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        // Don't deactivate the GameObject — just hide the visuals
        CanvasGroup cg = lockPanel.GetComponent<CanvasGroup>();
        if (cg == null) cg = lockPanel.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
        cg.blocksRaycasts = false;
        cg.interactable = false;
    }
    void Start()
    {
        if (PlayFabClientAPI.IsClientLoggedIn())
            CheckPurchaseStatus();
        else
            hasPurchased = false;
    }

    void CheckPurchaseStatus()
    {
        var request = new GetUserDataRequest
        {
            Keys = new System.Collections.Generic.List<string> { purchaseDataKey }
        };

        PlayFabClientAPI.GetUserData(request, OnDataReceived, OnDataError);
    }

    void OnDataReceived(GetUserDataResult result)
    {
        checkComplete = true;

        if (result.Data != null && result.Data.ContainsKey(purchaseDataKey))
        {
            string value = result.Data[purchaseDataKey].Value.ToLower();
            hasPurchased = (value == "true" || value == "1" || value == "yes");
        }
        else
        {
            hasPurchased = false;
        }

        Debug.Log($"[DemoLock] Purchase check complete. HasPurchased={hasPurchased}");

        // Check immediately in case we're already past the lock day
        int currentDay = PlayerPrefs.GetInt("CurrentDay", 1);
        if (currentDay >= lockAtDay && !hasPurchased)
            ShowLockScreen();
    }

    void OnDataError(PlayFabError error)
    {
        checkComplete = true;
        hasPurchased = false;
        Debug.LogWarning($"[DemoLock] PlayFab data check failed: {error.GenerateErrorReport()}");
    }

    /// <summary>
    /// Call this from DayTransitionManager at the START of a new day.
    /// If the new day >= lockAtDay and the player hasn't purchased, lock the game.
    /// Returns true if the demo is locked (so you can skip the day intro).
    /// </summary>
    public bool CheckDemoLock(int newDay)
    {
        if (hasPurchased) return false;
        if (newDay < lockAtDay) return false;

        // If PlayFab check hasn't completed yet, re-check
        if (!checkComplete && PlayFabClientAPI.IsClientLoggedIn())
        {
            CheckPurchaseStatus();
            // Assume locked until proven otherwise
        }

        ShowLockScreen();
        return true;
    }
    /// <summary>
    /// Parameterless check — reads CurrentDay from PlayerPrefs.
    /// Returns true if demo is locked.
    /// </summary>
    public bool CheckDemoStatus()
    {
        int currentDay = PlayerPrefs.GetInt("CurrentDay", 1);
        return CheckDemoLock(currentDay);
    }

    void ShowLockScreen()
    {
        if (lockPanel != null)
        {
            CanvasGroup cg = lockPanel.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.alpha = 1f;
                cg.blocksRaycasts = true;
                cg.interactable = true;
            }
            lockPanel.SetActive(true);

            if (buyButton != null)
            {
                buyButton.onClick.RemoveAllListeners();
                buyButton.onClick.AddListener(OnBuyClicked);
            }
            if (mainMenuButton != null)
            {
                mainMenuButton.onClick.RemoveAllListeners();
                mainMenuButton.onClick.AddListener(OnMainMenuClicked);
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Time.timeScale = 0f;
        }
    }
    void OnBuyClicked()
    {
        Application.OpenURL(purchaseURL);
    }

    void OnMainMenuClicked()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (DayTransitionManager.Instance != null)
            DayTransitionManager.ResetDayFlag();

        SceneManager.LoadScene(mainMenuScene);
    }
}
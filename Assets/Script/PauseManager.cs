using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseManager : MonoBehaviour
{
    [Header("Panels")]
    [Tooltip("The main pause menu with Resume / Options / Quit buttons.")]
    public GameObject pausePanel;

    [Tooltip("The existing options panel (video, audio, controls tabs).")]
    public GameObject optionsPanel;

    [Header("Buttons")]
    public Button resumeButton;
    public Button optionsButton;
    public Button quitButton;

    [Tooltip("Optional — a Back button inside the Options panel to return to the pause menu.")]
    public Button optionsBackButton;

    [Header("Settings")]
    [Tooltip("Scene name to load when Quit is pressed. Leave empty to use 'MainMenu'.")]
    public string mainMenuSceneName = "MainMenu";

    public static bool isPaused = false;

    // Other scripts can set this to TRUE to stop the pause menu from opening
    public static bool BlockPause = false;

    void Start()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(false);

        // Wire up buttons
        if (resumeButton != null) resumeButton.onClick.AddListener(ResumeGame);
        if (optionsButton != null) optionsButton.onClick.AddListener(OpenOptions);
        if (quitButton != null) quitButton.onClick.AddListener(QuitToMainMenu);
        if (optionsBackButton != null) optionsBackButton.onClick.AddListener(BackToPauseMenu);

        ResumeGame();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (BlockPause) return;

            if (isPaused)
            {
                // If options panel is open, go back to pause menu first
                if (optionsPanel != null && optionsPanel.activeSelf)
                {
                    BackToPauseMenu();
                }
                else
                {
                    ResumeGame();
                }
            }
            else
            {
                // Only pause if the cursor is HIDDEN (Gameplay Mode)
                if (Cursor.visible == false)
                {
                    PauseGame();
                }
            }
        }
    }

    void LateUpdate()
    {
        BlockPause = false;
    }

    // =============================================
    //  PAUSE / RESUME
    // =============================================

    public void PauseGame()
    {
        if (pausePanel != null) pausePanel.SetActive(true);
        if (optionsPanel != null) optionsPanel.SetActive(false);

        Time.timeScale = 0f;
        isPaused = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ResumeGame()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(false);

        Time.timeScale = 1f;
        isPaused = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // =============================================
    //  OPTIONS
    // =============================================

    public void OpenOptions()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(true);
    }

    public void BackToPauseMenu()
    {
        if (optionsPanel != null) optionsPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(true);
    }

    // =============================================
    //  QUIT TO MAIN MENU
    // =============================================

    public void QuitToMainMenu()
    {
        DayTransitionManager.ResetDayFlag();
        PlayerPrefs.SetFloat("SavedGameTime", 6f);  // ← add this
        PlayerPrefs.Save();                           // ← add this

        Time.timeScale = 1f;
        isPaused = false;
        SceneManager.LoadScene(mainMenuSceneName);
    }
}
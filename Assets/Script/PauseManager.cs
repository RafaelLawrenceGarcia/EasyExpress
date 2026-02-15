using UnityEngine;

public class PauseManager : MonoBehaviour
{
    [Header("UI Reference")]
    public GameObject optionsPanel; // Drag your entire Options Menu (the black box) here

    // Global state so other scripts can check if we are paused
    public static bool isPaused = false;

    void Start()
    {
        // Ensure the menu is hidden when the game starts
        if(optionsPanel != null)
            optionsPanel.SetActive(false);
            
        ResumeGame(); // Ensure time is running and cursor is locked
    }

    void Update()
    {
        // Toggle Pause when ESC is pressed
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }

    public void PauseGame()
    {
        optionsPanel.SetActive(true); // Show the menu
        Time.timeScale = 0f;          // FREEZE the game logic/physics
        isPaused = true;

        // Unlock the mouse so you can click the sliders
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ResumeGame()
    {
        optionsPanel.SetActive(false); // Hide the menu
        Time.timeScale = 1f;           // UNFREEZE the game
        isPaused = false;

        // Lock the mouse back to the center for gameplay
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
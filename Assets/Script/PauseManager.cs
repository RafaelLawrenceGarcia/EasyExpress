using UnityEngine;

public class PauseManager : MonoBehaviour
{
    [Header("UI Reference")]
    public GameObject optionsPanel; 

    public static bool isPaused = false;
    
    // --- 1. THE BLOCKER FLAG ---
    // Other scripts can set this to TRUE to stop the pause menu from opening
    public static bool BlockPause = false; 

    void Start()
    {
        if(optionsPanel != null) optionsPanel.SetActive(false);
        ResumeGame();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // --- 2. CHECK THE FLAG ---
            // If something else (like the Shop) blocked us, STOP immediately.
            if (BlockPause) return; 

            if (isPaused)
            {
                ResumeGame();
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

    // --- 3. AUTO-RESET ---
    // LateUpdate runs AFTER all Update functions are done.
    // This resets the flag so the pause menu works again in the next frame.
    void LateUpdate()
    {
        BlockPause = false;
    }

    public void PauseGame()
    {
        optionsPanel.SetActive(true); 
        Time.timeScale = 0f;          
        isPaused = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ResumeGame()
    {
        optionsPanel.SetActive(false); 
        Time.timeScale = 1f;           
        isPaused = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
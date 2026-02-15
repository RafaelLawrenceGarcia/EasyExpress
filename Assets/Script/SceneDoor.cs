using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.UI;

public class SceneDoor : MonoBehaviour
{
    [Header("Settings")]
    public string sceneName; 
    [Range(0, 10)] public float minLoadingTime = 3.0f;

    [Header("Drag Your Loading Screen Here")]
        public GameObject loadingScreenPanel; 
        public Slider progressBar; 

    // Prevent player from spamming the E button
    private bool isAlreadyLoading = false; 

    public void EnterDoor()
    {
        // 1. If we are already loading, IGNORE the click
        if (isAlreadyLoading) return;

        if (loadingScreenPanel != null)
        {
            StartCoroutine(LoadAsync());
        }
        else
        {
            Debug.LogError("You forgot to drag the Loading Screen into the Door script!");
        }
    }

    IEnumerator LoadAsync()
    {
        isAlreadyLoading = true;

        // 2. DISABLE PLAYER CONTROL (The Fix)
        // We stop the scripts so the player cannot Move or Press E again
        GTAMovement moveScript = FindObjectOfType<GTAMovement>();
        if (moveScript) moveScript.enabled = false;

        PlayerInteract interactScript = FindObjectOfType<PlayerInteract>();
        if (interactScript) interactScript.enabled = false; // Stops the "Press E" logic

        // 3. Save Data
        PlayerWallet wallet = FindObjectOfType<PlayerWallet>();
        if (wallet != null)
        {
            PlayerPrefs.SetFloat("SavedGold", wallet.currentGold);
            PlayerPrefs.Save();
        }

        // 4. Hide UI Elements (Cleanup)
        GameObject hud = GameObject.Find("Gold HUD");
        if (hud != null) hud.SetActive(false);

        // Safely hide the prompt if it exists
        // (We search for the specific interaction script to find its UI, rather than guessing names)
        if (interactScript != null && interactScript.pressEPrompt != null)
        {
            interactScript.pressEPrompt.SetActive(false);
        }

        // 5. Show Loading Screen
        loadingScreenPanel.SetActive(true);

        // 6. Start Loading
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        operation.allowSceneActivation = false; 

        float timer = 0f;

        while (!operation.isDone)
        {
            timer += Time.deltaTime;
            
            float progress = Mathf.Clamp01(operation.progress / 0.9f);
            if (progressBar != null) progressBar.value = progress;

            if (operation.progress >= 0.9f && timer >= minLoadingTime)
            {
                operation.allowSceneActivation = true; 
            }

            yield return null; 
        }
    }
}
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;

public class SceneDoor : MonoBehaviour
{
    [Header("Settings")]
    public string sceneName;

    [Header("UI - Loading Screen")]
    public GameObject loadingScreenPanel;
    public Slider progressBar;
    public TextMeshProUGUI progressText; 

    [Header("Cycling Hints")]
    [Tooltip("The text component that will change to show different tips.")]
    public TextMeshProUGUI hintText;
    
    [Tooltip("How many seconds to wait before showing the next hint.")]
    public float hintCycleTime = 3.5f; 

    [TextArea(2, 4)]
    public string[] tips = {
        "Remember to ground yourself before touching sensitive components!",
        "Dust is the enemy! Make cleaning dusty PCs a top priority.",
        "Use thermal paste sparingly; a pea-sized amount is enough.",
        "A simple reboot can solve strange software glitches.",
        "Check for loose connections before diagnosing complex issues."
    };

    private bool isAlreadyLoading = false;

    public void EnterDoor()
    {
        if (isAlreadyLoading) return;
        if (loadingScreenPanel != null) StartCoroutine(LoadAsync());
    }

    IEnumerator LoadAsync()
    {
        isAlreadyLoading = true;

        // --- PREPARE & SAVE DATA BEFORE LOAD ---
        GTAMovement moveScript = FindFirstObjectByType<GTAMovement>();
        if (moveScript) moveScript.enabled = false;

        PlayerInteract interactScript = FindFirstObjectByType<PlayerInteract>();
        if (interactScript) interactScript.enabled = false;

        DayTimeUI dayTimeUI = FindFirstObjectByType<DayTimeUI>();
        if (dayTimeUI != null) PlayerPrefs.SetFloat("SavedGameTime", dayTimeUI.GetCurrentTime());

        if (ShopCustomerSpawner.Instance != null) CustomerRetainer.SaveCustomers(ShopCustomerSpawner.Instance);

        if (DeliveryManager.Instance != null)
        {
            DeliveryManager.SavedOrders = new List<DeliveryOrder>(DeliveryManager.Instance.pendingOrders);
        }

        DeliveryBox[] allBoxes = FindObjectsByType<DeliveryBox>(FindObjectsSortMode.None);
        if (allBoxes.Length > 0)
        {
            DeliveryManager.SavedSpawnedBoxItems = new List<ItemData>();
            foreach (DeliveryBox box in allBoxes)
            {
                if (box.containedItem != null) DeliveryManager.SavedSpawnedBoxItems.Add(box.containedItem);
            }
        }

        GameObject hud = GameObject.Find("Gold HUD");
        if (hud != null) hud.SetActive(false);

        if (interactScript != null && interactScript.interactionPrompt != null)
            interactScript.interactionPrompt.Hide();

        PlayerPrefs.SetInt("ChangingRooms", 1);
        PlayerPrefs.Save();

        // --- SETUP UI ---
        loadingScreenPanel.SetActive(true);
        if (progressBar != null) progressBar.value = 0f;
        if (progressText != null) progressText.text = "0%";
        if (hintText != null) hintText.text = "";

        // Start cycling the hints
        if (hintText != null && tips.Length > 0) StartCoroutine(CycleHintsEffect());

        // --- ACTUAL ASYNC LOAD ---
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        operation.allowSceneActivation = false;

        float timer = 0f;
        float minimumDisplayTime = 4.0f; // Shows the screen for at least 4 seconds so players can read

        while (!operation.isDone)
        {
            timer += Time.deltaTime;

            float loadProgress = Mathf.Clamp01(operation.progress / 0.9f);
            float timeProgress = Mathf.Clamp01(timer / minimumDisplayTime);
            
            float currentVisualProgress = Mathf.Min(loadProgress, timeProgress);

            if (progressBar != null) progressBar.value = currentVisualProgress;
            if (progressText != null) progressText.text = Mathf.RoundToInt(currentVisualProgress * 100f) + "%"; 

            if (operation.progress >= 0.9f && timer >= minimumDisplayTime)
            {
                if (progressBar != null) progressBar.value = 1f; 
                if (progressText != null) progressText.text = "100%";
                operation.allowSceneActivation = true;
            }

            yield return null;
        }
    }

    IEnumerator CycleHintsEffect()
    {
        int index = Random.Range(0, tips.Length); // Start on a random hint
        while (true)
        {
            // Just puts the hint on the screen
            hintText.text = tips[index];
            yield return new WaitForSeconds(hintCycleTime);
            index = (index + 1) % tips.Length; // Move to next hint
        }
    }
}
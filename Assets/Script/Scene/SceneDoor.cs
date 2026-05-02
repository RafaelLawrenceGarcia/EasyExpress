using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class SceneDoor : MonoBehaviour
{
    [Header("Settings")]
    public string sceneName;
    [Range(0, 10)] public float minLoadingTime = 3.0f;

    [Header("Drag Your Loading Screen Here")]
    public GameObject loadingScreenPanel;
    public Slider progressBar;

    private bool isAlreadyLoading = false;

    public void EnterDoor()
    {
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

        // 1. DISABLE PLAYER CONTROL
        GTAMovement moveScript = FindObjectOfType<GTAMovement>();
        if (moveScript) moveScript.enabled = false;

        PlayerInteract interactScript = FindObjectOfType<PlayerInteract>();
        if (interactScript) interactScript.enabled = false;

        // 2. Save game time for room change
        DayTimeUI dayTimeUI = FindObjectOfType<DayTimeUI>();
        if (dayTimeUI != null)
        {
            PlayerPrefs.SetFloat("SavedGameTime", dayTimeUI.GetCurrentTime());
        }

        // 3. Save customers
        if (ShopCustomerSpawner.Instance != null)
        {
            CustomerRetainer.SaveCustomers(ShopCustomerSpawner.Instance);
        }

        // 4. Save pending delivery orders (not yet arrived)
        if (DeliveryManager.Instance != null)
        {
            DeliveryManager.SavedOrders = new List<DeliveryOrder>(DeliveryManager.Instance.pendingOrders);
            Debug.Log("[SceneDoor] Saved " + DeliveryManager.SavedOrders.Count + " pending delivery orders.");
        }

        // 5. Save already-spawned delivery boxes (arrived but not picked up)
        DeliveryBox[] allBoxes = FindObjectsOfType<DeliveryBox>();
        if (allBoxes.Length > 0)
        {
            DeliveryManager.SavedSpawnedBoxItems = new List<ItemData>();
            foreach (DeliveryBox box in allBoxes)
            {
                if (box.containedItem != null)
                {
                    DeliveryManager.SavedSpawnedBoxItems.Add(box.containedItem);
                }
            }
            Debug.Log("[SceneDoor] Saved " + DeliveryManager.SavedSpawnedBoxItems.Count + " spawned delivery boxes.");
        }

        // 6. Hide UI Elements
        GameObject hud = GameObject.Find("Gold HUD");
        if (hud != null) hud.SetActive(false);

        if (interactScript != null && interactScript.interactionPrompt != null)
        {
            interactScript.interactionPrompt.Hide();
        }

        // 7. Set room change flag
        PlayerPrefs.SetInt("ChangingRooms", 1);
        PlayerPrefs.Save();

        // 8. Show Loading Screen
        loadingScreenPanel.SetActive(true);

        // 9. Start Loading
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
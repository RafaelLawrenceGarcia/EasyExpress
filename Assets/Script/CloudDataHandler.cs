using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using System.Collections.Generic;

public class CloudDataHandler : MonoBehaviour
{
   public static CloudDataHandler Instance;

    void Awake()
    {
        if (Instance == null) 
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // <--- ADD THIS LINE
        }
        else 
        {
            Destroy(gameObject); // Prevents duplicates if you go back to menu
        }
    }
    // --- SAVE TO DATABASE ---
    public void SaveGameData()
    {
        // 1. Get current values
        float currentGold = FindObjectOfType<PlayerWallet>().currentGold;
        List<string> inventory = ShopSystem.Instance.GetInventoryIDs();
        
        // 2. Prepare the data package
        var request = new UpdateUserDataRequest
        {
            Data = new Dictionary<string, string>
            {
                { "Gold", currentGold.ToString() },
                { "Inventory", string.Join(",", inventory) } // Saves as "gpu_1080,case_black"
            }
        };

        // 3. Send to PlayFab
        PlayFabClientAPI.UpdateUserData(request, 
            result => Debug.Log("Cloud Save Successful!"), 
            error => Debug.LogError("Cloud Save Failed: " + error.GenerateErrorReport()));
    }

    // --- LOAD FROM DATABASE ---
    public void LoadGameData()
    {
        PlayFabClientAPI.GetUserData(new GetUserDataRequest(), OnDataReceived, OnError);
    }

    void OnDataReceived(GetUserDataResult result)
    {
        // 1. Load Gold
        if (result.Data != null && result.Data.ContainsKey("Gold"))
        {
            float loadedGold = float.Parse(result.Data["Gold"].Value);
            FindObjectOfType<PlayerWallet>().currentGold = loadedGold;
            FindObjectOfType<PlayerWallet>().UpdateUI(); // Refresh the text
        }

        // 2. Load Inventory
        if (result.Data != null && result.Data.ContainsKey("Inventory"))
        {
            string csv = result.Data["Inventory"].Value;
            ShopSystem.Instance.SetInventoryIDs(csv.Split(','));
        }
        
        Debug.Log("Cloud Load Complete.");
    }

    void OnError(PlayFabError error)
    {
        Debug.Log("Error loading data: " + error.ErrorMessage);
    }
}
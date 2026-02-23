using UnityEngine;
using System.Collections.Generic;

public class ShopSystem : MonoBehaviour
{
    public static ShopSystem Instance;

    [Header("Databases")]
    public List<ItemData> allAvailableItems; 

    private List<string> ownedItemIDs = new List<string>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        // We do NOT load PlayerPrefs here anymore. 
        // We wait for CloudDataHandler to load the real data.
    }

    public void BuyItem(ItemData item)
    {
        PlayerWallet wallet = FindObjectOfType<PlayerWallet>();

        if (wallet != null && wallet.currentGold >= item.price)
        {
            wallet.SpendGold(item.price);

            if (!ownedItemIDs.Contains(item.id))
            {
                ownedItemIDs.Add(item.id);
                
                // --- CRITICAL CHANGE: SAVE TO CLOUD ---
                CloudDataHandler.Instance.SaveGameData(); 
                // -------------------------------------
                
                Debug.Log($"Bought {item.itemName}!");
            }
        }
    }

    public bool HasItem(ItemData item)
    {
        return ownedItemIDs.Contains(item.id);
    }

    // Helper functions for the Cloud Handler to use
    public List<string> GetInventoryIDs()
    {
        return ownedItemIDs;
    }

    public void SetInventoryIDs(string[] loadedIDs)
    {
        ownedItemIDs = new List<string>(loadedIDs);
        // Refresh UI here if needed
    }
}
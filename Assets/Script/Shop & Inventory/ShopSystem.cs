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

            // FIX: Removed the duplicate check! Buy as many as you want!
            ownedItemIDs.Add(item.id);

            // --- CRITICAL CHANGE: SAVE TO CLOUD ---
            if (CloudDataHandler.Instance != null)
            {
                CloudDataHandler.Instance.SaveGameData();
            }
            // -------------------------------------

            Debug.Log($"Bought {item.itemName}!");
        }
    }

    /// Called by DeliveryBox when the player unpacks a delivered order.
    /// Adds the item to owned inventory without spending gold.
    public void AddItemDirectly(ItemData item)
    {
        // FIX: Removed the duplicate check! Unpack as many as you ordered!
        ownedItemIDs.Add(item.id);

        if (CloudDataHandler.Instance != null)
        {
            CloudDataHandler.Instance.SaveGameData();
        }

        Debug.Log($"[ShopSystem] '{item.itemName}' added to inventory via delivery!");
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
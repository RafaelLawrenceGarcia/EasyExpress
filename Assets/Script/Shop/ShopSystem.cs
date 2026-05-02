using UnityEngine;
using System.Collections.Generic;

public class ShopSystem : MonoBehaviour
{
    public static ShopSystem Instance;

    [Header("Databases")]
    public List<ItemData> allAvailableItems;

    private List<string> ownedItemIDs = new List<string>();
    [Header("Initial Stock (First Game Only)")]
    [Tooltip("How many of each PC part to pre-stock on first game start.")]
    public int initialStockPerCategory = 3;

    void Start()
    {
        // Only pre-stock on first game (no save data AND not loading)
        bool hasSave = PlayerPrefs.GetInt("HasSaveData", 0) == 1;
        bool isLoading = PlayerPrefs.GetInt("IsLoadingGame", 0) == 1;
        bool tutorialDone = PlayerPrefs.GetInt("TutorialDone", 0) == 1;

        if (!hasSave && !isLoading && !tutorialDone && ownedItemIDs.Count == 0)
        {
            PopulateInitialStock();
        }
    }

    /// <summary>
    /// Adds 'initialStockPerCategory' of each PC part category to the shop inventory.
    /// Groups items by category and picks one representative per category,
    /// then adds multiple copies of it.
    /// </summary>
    void PopulateInitialStock()
    {
        if (allAvailableItems == null || allAvailableItems.Count == 0) return;

        // Collect one item per PC part category
        Dictionary<string, ItemData> categoryRepresentatives = new Dictionary<string, ItemData>();
        string[] stockCategories = { "GPU", "RAM", "CPU", "Motherboard", "PSU", "Storage", "Cooler", "Fan" };

        foreach (string cat in stockCategories)
        {
            foreach (ItemData item in allAvailableItems)
            {
                if (item == null) continue;
                if (item.itemType != ItemCategory.PCPart) continue;

                // Match by item category string
                if (item.category != null && item.category.Trim().Equals(cat,
                    System.StringComparison.OrdinalIgnoreCase))
                {
                    if (!categoryRepresentatives.ContainsKey(cat))
                        categoryRepresentatives[cat] = item;
                }
            }
        }

        // Add stock
        int totalAdded = 0;
        foreach (var kvp in categoryRepresentatives)
        {
            for (int i = 0; i < initialStockPerCategory; i++)
            {
                ownedItemIDs.Add(kvp.Value.id);
                totalAdded++;
            }
            Debug.Log($"[ShopSystem] Pre-stocked {initialStockPerCategory}x {kvp.Key}: {kvp.Value.itemName}");
        }

        Debug.Log($"[ShopSystem] Initial stock: {totalAdded} items across {categoryRepresentatives.Count} categories.");
    }
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

            // Checkpoint system: no mid-day cloud saves.
            // State is saved at end-of-day by DayTransitionManager.

            Debug.Log($"Bought {item.itemName}!");
        }
    }

    /// Called by DeliveryBox when the player unpacks a delivered order.
    /// Adds the item to owned inventory without spending gold.
    public void AddItemDirectly(ItemData item)
    {
        // FIX: Removed the duplicate check! Unpack as many as you ordered!
        ownedItemIDs.Add(item.id);

        // Checkpoint system: no mid-day cloud saves.

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
using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using System.Collections.Generic;

public class CloudDataHandler : MonoBehaviour
{
    public static CloudDataHandler Instance;

    [Header("Database Reference")]
    [Tooltip("Drag your PC Part Database here so we can look up prefabs on load.")]
    public PCPartDatabase partDatabase;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // =============================================
    //  SAVE ALL GAME DATA
    // =============================================
    public void SaveGameData()
    {
        // ── GUARD: skip cloud save if not logged in ──
        if (!GameSession.IsLoggedIn)
        {
            Debug.Log("[CloudSave] Skipped — not logged in (guest session).");
            return;
        }

        PlayerWallet wallet = FindFirstObjectByType<PlayerWallet>();
        float currentGold = wallet != null ? wallet.currentGold : 0f;

        // Build persistent data
        GamePersistData persistData = new GamePersistData();
        persistData.currentDay = PlayerPrefs.GetInt("CurrentDay", 1);
        persistData.tutorialDone = PlayerPrefs.GetInt("TutorialDone", 0) == 1;

        // Save accepted jobs
        EmailManager email = EmailManager.Instance;
        if (email != null)
        {
            foreach (EmailData job in email.acceptedJobs)
            {
                persistData.acceptedJobs.Add(SerializeJob(job));
            }
        }

        // Save player inventory (parts removed from PCs)
        InspectionManager inspection = FindFirstObjectByType<InspectionManager>();
        if (inspection != null)
        {
            foreach (GameObject partObj in inspection.playerStorage)
            {
                if (partObj == null) continue;
                InspectableItem item = partObj.GetComponent<InspectableItem>();
                if (item == null) continue;
                persistData.inventoryParts.Add(SerializePart(item));
            }
        }

        // Save inventory system entries too
        InventorySystem invSys = InventorySystem.Instance;
        if (invSys != null)
        {
            foreach (var entry in invSys.entries)
            {
                if (entry.obj == null) continue;
                InspectableItem item = entry.obj.GetComponent<InspectableItem>();
                if (item == null) continue;
                persistData.inventoryParts.Add(SerializePart(item));
            }
        }

        // Save shop owned item IDs
        ShopSystem shop = ShopSystem.Instance;
        if (shop != null)
        {
            persistData.ownedItemIDs = new List<string>(shop.GetInventoryIDs());
        }

        // Save pending delivery orders
        DeliveryManager delivery = DeliveryManager.Instance;
        if (delivery != null)
        {
            foreach (DeliveryOrder order in delivery.pendingOrders)
            {
                if (order.item == null) continue;
                SavedDelivery saved = new SavedDelivery();
                saved.itemId = order.item.id;
                saved.quantity = order.quantity;
                saved.daysRemaining = order.daysRemaining;
                persistData.pendingDeliveries.Add(saved);
            }
        }

        string persistJson = JsonUtility.ToJson(persistData);

        // Prepare PlayFab data
        var request = new UpdateUserDataRequest
        {
            Data = new Dictionary<string, string>
            {
                { "Gold", currentGold.ToString() },
                { "GameData", persistJson }
            }
        };

        PlayFabClientAPI.UpdateUserData(request,
            result => Debug.Log("[CloudSave] Save successful! Day: " + persistData.currentDay +
                              ", Tutorial: " + persistData.tutorialDone +
                              ", Jobs: " + persistData.acceptedJobs.Count +
                              ", Inventory: " + persistData.inventoryParts.Count +
                              ", ShopItems: " + persistData.ownedItemIDs.Count +
                              ", Deliveries: " + persistData.pendingDeliveries.Count),
            error => Debug.LogWarning("[CloudSave] Save failed: " + error.GenerateErrorReport()));
    }

    // =============================================
    //  LOAD ALL GAME DATA
    // =============================================
    public void LoadGameData()
    {
        // ── GUARD: skip cloud load if not logged in ──
        if (!GameSession.IsLoggedIn)
        {
            Debug.Log("[CloudSave] Load skipped — not logged in (guest session).");
            return;
        }

        PlayFabClientAPI.GetUserData(new GetUserDataRequest(), OnDataReceived, OnError);
    }

    void OnDataReceived(GetUserDataResult result)
    {
        // ── NEW ACCOUNT: no cloud data at all → reset to fresh state ──
        if (result.Data == null || result.Data.Count == 0)
        {
            Debug.Log("[CloudSave] No cloud data found (new account). Resetting to Day 1.");
            ResetToFreshState();
            return;
        }

        // Load Gold
        if (result.Data.ContainsKey("Gold"))
        {
            PlayerWallet wallet = FindFirstObjectByType<PlayerWallet>();
            if (wallet != null)
            {
                wallet.currentGold = float.Parse(result.Data["Gold"].Value);
                wallet.UpdateUI();
            }
        }
        else
        {
            // No gold saved yet — reset wallet to 0
            PlayerWallet wallet = FindFirstObjectByType<PlayerWallet>();
            if (wallet != null)
            {
                wallet.currentGold = 0f;
                wallet.UpdateUI();
            }
        }

        // Load persistent game data
        if (result.Data.ContainsKey("GameData"))
        {
            string json = result.Data["GameData"].Value;
            if (!string.IsNullOrEmpty(json))
            {
                GamePersistData persistData = JsonUtility.FromJson<GamePersistData>(json);
                RestoreGameData(persistData);
            }
            else
            {
                // GameData key exists but is empty → treat as new
                Debug.Log("[CloudSave] GameData key is empty. Resetting to Day 1.");
                ResetToFreshState();
            }
        }
        else
        {
            // Has Gold but no GameData → still reset day/tutorial
            Debug.Log("[CloudSave] No GameData key. Resetting to Day 1.");
            ResetToFreshState();
        }

        Debug.Log("[CloudSave] Load complete.");
    }

    /// <summary>
    /// Resets local PlayerPrefs to a fresh Day 1 state.
    /// Called when a logged-in account has no cloud save data yet.
    /// </summary>
    void ResetToFreshState()
    {
        PlayerPrefs.SetInt("CurrentDay", 1);
        PlayerPrefs.SetInt("TutorialDone", 0);
        PlayerPrefs.SetInt("IsLoadingGame", 0);
        PlayerPrefs.SetFloat("SavedGold", 0f);
        PlayerPrefs.SetInt("HasSaveData", 0);
        PlayerPrefs.Save();

        // Reset wallet in-game if it exists
        PlayerWallet wallet = FindFirstObjectByType<PlayerWallet>();
        if (wallet != null)
        {
            wallet.currentGold = 0f;
            wallet.UpdateUI();
        }

        Debug.Log("[CloudSave] Local state reset to fresh Day 1.");

        // Update DayTransitionManager so the HUD shows the correct day
        SyncDayTransitionManager();

        // Restart the tutorial — it was skipped because IsLoadingGame was 1
        if (TutorialManager.Instance != null)
        {
            TutorialManager.Instance.RestartTutorial();
        }
    }

    /// <summary>
    /// Tells DayTransitionManager to re-read the current day from PlayerPrefs.
    /// Called after cloud data changes the day value.
    /// </summary>
    void SyncDayTransitionManager()
    {
        if (DayTransitionManager.Instance != null)
        {
            DayTransitionManager.Instance.SyncCurrentDay();
        }
    }

    /// <summary>
    /// Waits for scene singletons to be ready, then loads.
    /// Call this from GameManager.OnSceneLoaded instead of LoadGameData directly.
    /// </summary>
    public void LoadGameDataDelayed()
    {
        if (!GameSession.IsLoggedIn)
        {
            Debug.Log("[CloudSave] Delayed load skipped — not logged in.");
            return;
        }

        StartCoroutine(DelayedLoad());
    }

    private System.Collections.IEnumerator DelayedLoad()
    {
        // Wait a few frames for EmailManager, InventorySystem etc. to Awake/Start
        yield return null;
        yield return null;
        yield return null;
        LoadGameData();
    }

    void RestoreGameData(GamePersistData data)
    {
        if (data == null) return;

        // Restore current day and tutorial status from cloud
        PlayerPrefs.SetInt("CurrentDay", data.currentDay);
        PlayerPrefs.SetInt("TutorialDone", data.tutorialDone ? 1 : 0);
        PlayerPrefs.Save();
        Debug.Log("[CloudSave] Restored day: " + data.currentDay + ", tutorialDone: " + data.tutorialDone);

        // Update DayTransitionManager so the HUD shows the correct day
        SyncDayTransitionManager();

        // Restore accepted jobs
        EmailManager email = EmailManager.Instance;
        if (email != null && data.acceptedJobs.Count > 0)
        {
            foreach (SavedJob savedJob in data.acceptedJobs)
            {
                EmailData job = DeserializeJob(savedJob);
                if (job != null)
                {
                    bool spawned = email.SpawnBoxFromData(job.basePCCasePrefab, job.startingParts, job);
                    if (spawned)
                    {
                        email.acceptedJobs.Add(job);
                    }
                    else
                    {
                        Debug.LogWarning("[CloudSave] No shelf space for restored job: " + savedJob.senderName);
                        email.acceptedJobs.Add(job);
                    }
                }
            }
            email.RefreshInboxUI();
            Debug.Log("[CloudSave] Restored " + data.acceptedJobs.Count + " accepted jobs.");
        }

        // Restore inventory parts
        InspectionManager inspection = FindFirstObjectByType<InspectionManager>();
        if (inspection != null && data.inventoryParts.Count > 0)
        {
            foreach (SavedPart savedPart in data.inventoryParts)
            {
                GameObject partObj = RecreatePart(savedPart);
                if (partObj != null)
                {
                    partObj.SetActive(false);
                    inspection.playerStorage.Add(partObj);
                }
            }
            Debug.Log("[CloudSave] Restored " + data.inventoryParts.Count + " inventory parts.");
        }

        // Restore shop owned item IDs
        ShopSystem shop = ShopSystem.Instance;
        if (shop != null && data.ownedItemIDs != null && data.ownedItemIDs.Count > 0)
        {
            shop.SetInventoryIDs(data.ownedItemIDs.ToArray());
            Debug.Log("[CloudSave] Restored " + data.ownedItemIDs.Count + " shop owned items.");
        }

        // Restore pending delivery orders
        DeliveryManager delivery = DeliveryManager.Instance;
        if (delivery != null && shop != null && data.pendingDeliveries != null && data.pendingDeliveries.Count > 0)
        {
            delivery.pendingOrders.Clear();
            foreach (SavedDelivery saved in data.pendingDeliveries)
            {
                ItemData item = FindItemDataById(saved.itemId, shop);
                if (item != null)
                {
                    DeliveryOrder order = new DeliveryOrder(item, saved.quantity, saved.daysRemaining);
                    delivery.pendingOrders.Add(order);
                }
                else
                {
                    Debug.LogWarning("[CloudSave] Could not find ItemData for delivery: " + saved.itemId);
                }
            }
            Debug.Log("[CloudSave] Restored " + data.pendingDeliveries.Count + " pending deliveries.");
        }
    }

    // =============================================
    //  SERIALIZATION HELPERS
    // =============================================

    SavedJob SerializeJob(EmailData job)
    {
        SavedJob saved = new SavedJob();
        saved.senderName = job.senderName;
        saved.subjectLine = job.subjectLine;
        saved.bodyText = job.bodyText;
        saved.jobType = (int)job.jobType;
        saved.labourCost = job.labourCost;
        saved.partsBudget = job.partsBudget;
        saved.objectives = job.objectives;
        saved.pcProblems = job.pcProblems;
        saved.originalFaultCount = job.originalFaultCount;

        if (job.basePCCasePrefab != null)
            saved.casePrefabName = job.basePCCasePrefab.name;

        if (job.startingParts != null)
        {
            foreach (var part in job.startingParts)
                saved.startingParts.Add(SerializeStartingPart(part));
        }

        if (job.requestedParts != null)
        {
            foreach (var part in job.requestedParts)
                saved.requestedParts.Add(SerializeStartingPart(part));
        }

        return saved;
    }

    SavedPart SerializeStartingPart(StartingPCComponent part)
    {
        SavedPart saved = new SavedPart();
        saved.partCategory = part.partCategory;
        saved.partName = part.partName;
        saved.prefabName = part.partPrefab != null ? part.partPrefab.name : "";
        saved.compatTags = part.compatTags;
        saved.powerDraw = part.powerDraw;
        saved.maxWattage = part.maxWattage;
        saved.isDusty = part.isDusty;
        saved.fault = (int)part.fault;
        saved.faultDescription = part.faultDescription;
        return saved;
    }

    SavedPart SerializePart(InspectableItem item)
    {
        SavedPart saved = new SavedPart();
        saved.partCategory = item.partCategory;
        saved.partName = item.itemName;
        saved.prefabName = GetPrefabNameForPart(item);
        saved.compatTags = item.compatTags;
        saved.powerDraw = item.powerDraw;
        saved.maxWattage = item.maxWattage;
        saved.fault = (int)item.fault;
        saved.faultDescription = item.faultDescription;
        return saved;
    }

    // =============================================
    //  DESERIALIZATION HELPERS
    // =============================================

    EmailData DeserializeJob(SavedJob saved)
    {
        EmailData job = ScriptableObject.CreateInstance<EmailData>();
        job.senderName = saved.senderName;
        job.subjectLine = saved.subjectLine;
        job.bodyText = saved.bodyText;
        job.jobType = (JobType)saved.jobType;
        job.labourCost = saved.labourCost;
        job.partsBudget = saved.partsBudget;
        job.objectives = saved.objectives;
        job.pcProblems = saved.pcProblems;
        job.originalFaultCount = saved.originalFaultCount;

        job.basePCCasePrefab = FindCasePrefab(saved.casePrefabName);

        job.startingParts = new List<StartingPCComponent>();
        foreach (SavedPart sp in saved.startingParts)
            job.startingParts.Add(DeserializeStartingPart(sp));

        job.requestedParts = new List<StartingPCComponent>();
        foreach (SavedPart sp in saved.requestedParts)
            job.requestedParts.Add(DeserializeStartingPart(sp));

        return job;
    }

    StartingPCComponent DeserializeStartingPart(SavedPart saved)
    {
        StartingPCComponent part = new StartingPCComponent();
        part.partCategory = saved.partCategory;
        part.partName = saved.partName;
        part.partPrefab = FindPartPrefab(saved.prefabName, saved.partCategory);
        part.compatTags = saved.compatTags;
        part.powerDraw = saved.powerDraw;
        part.maxWattage = saved.maxWattage;
        part.isDusty = saved.isDusty;
        part.fault = (PartFault)saved.fault;
        part.faultDescription = saved.faultDescription;
        return part;
    }

    GameObject RecreatePart(SavedPart saved)
    {
        GameObject prefab = FindPartPrefab(saved.prefabName, saved.partCategory);
        if (prefab == null)
        {
            Debug.LogWarning($"[CloudSave] Could not find prefab for inventory part: {saved.partName} ({saved.partCategory})");
            return null;
        }

        GameObject obj = Instantiate(prefab);
        obj.name = saved.partName;

        InspectableItem item = obj.GetComponent<InspectableItem>();
        if (item != null)
        {
            item.itemName = saved.partName;
            item.partCategory = saved.partCategory;
            item.compatTags = saved.compatTags;
            item.powerDraw = saved.powerDraw;
            item.maxWattage = saved.maxWattage;
            item.fault = (PartFault)saved.fault;
            item.faultDescription = saved.faultDescription;
            item.isRemovable = true;
        }

        return obj;
    }

    // =============================================
    //  PREFAB LOOKUP
    // =============================================

    GameObject FindCasePrefab(string prefabName)
    {
        if (string.IsNullOrEmpty(prefabName) || partDatabase == null) return null;

        if (partDatabase.cases != null)
        {
            foreach (GameObject casePrefab in partDatabase.cases)
            {
                if (casePrefab != null && casePrefab.name == prefabName)
                    return casePrefab;
            }
        }
        return null;
    }

    GameObject FindPartPrefab(string prefabName, string category)
    {
        if (string.IsNullOrEmpty(prefabName) || partDatabase == null) return null;

        StartingPCComponent[][] allArrays = new StartingPCComponent[][]
        {
            partDatabase.motherboards,
            partDatabase.psus,
            partDatabase.cpus,
            partDatabase.gpus,
            partDatabase.rams,
            partDatabase.storage,
            partDatabase.coolers,
            partDatabase.fans
        };

        foreach (StartingPCComponent[] array in allArrays)
        {
            if (array == null) continue;
            foreach (StartingPCComponent comp in array)
            {
                if (comp.partPrefab != null && comp.partPrefab.name == prefabName)
                    return comp.partPrefab;
            }
        }

        foreach (StartingPCComponent[] array in allArrays)
        {
            if (array == null) continue;
            foreach (StartingPCComponent comp in array)
            {
                if (comp.partCategory == category && comp.partPrefab != null)
                    return comp.partPrefab;
            }
        }

        return null;
    }

    string GetPrefabNameForPart(InspectableItem item)
    {
        if (partDatabase == null) return item.gameObject.name.Replace("(Clone)", "").Trim();

        StartingPCComponent[][] allArrays = new StartingPCComponent[][]
        {
            partDatabase.motherboards, partDatabase.psus, partDatabase.cpus,
            partDatabase.gpus, partDatabase.rams, partDatabase.storage,
            partDatabase.coolers, partDatabase.fans
        };

        foreach (StartingPCComponent[] array in allArrays)
        {
            if (array == null) continue;
            foreach (StartingPCComponent comp in array)
            {
                if (comp.partCategory == item.partCategory && comp.partName == item.itemName && comp.partPrefab != null)
                    return comp.partPrefab.name;
            }
        }

        foreach (StartingPCComponent[] array in allArrays)
        {
            if (array == null) continue;
            foreach (StartingPCComponent comp in array)
            {
                if (comp.partCategory == item.partCategory && comp.partPrefab != null)
                    return comp.partPrefab.name;
            }
        }

        return item.gameObject.name.Replace("(Clone)", "").Trim();
    }

    /// <summary>
    /// Find an ItemData ScriptableObject by its ID from the ShopSystem's item list.
    /// </summary>
    ItemData FindItemDataById(string itemId, ShopSystem shop)
    {
        if (string.IsNullOrEmpty(itemId) || shop == null) return null;

        foreach (ItemData item in shop.allAvailableItems)
        {
            if (item != null && item.id == itemId)
                return item;
        }
        return null;
    }

    void OnError(PlayFabError error)
    {
        Debug.LogWarning("[CloudSave] Error: " + error.ErrorMessage);
    }
}
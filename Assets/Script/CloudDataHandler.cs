using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using System;
using System.Collections.Generic;

public class CloudDataHandler : MonoBehaviour
{
    public static bool IsRestoringCheckpoint = false;
    public static bool IsRoomChangeLoad = false;
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
    //  NO AUTO-SAVE ON QUIT / PAUSE
    //  The game uses an end-of-day checkpoint system.
    //  If the player quits mid-day, progress rolls back
    //  to the start of that day. Only EndDaySequence
    //  calls SaveGameData().
    // =============================================

    // =============================================
    //  SAVE ALL GAME DATA (End-of-Day Checkpoint)
    //  Called ONLY by DayTransitionManager.EndDaySequence
    // =============================================
    public void SaveGameData()
    {
        if (!GameSession.IsLoggedIn)
        {
            Debug.Log("[CloudSave] Skipped — not logged in (guest session).");
            return;
        }

        // ── READ GOLD DIRECTLY FROM THE WALLET ──
        // At end-of-day the game is stable, so we read straight from the wallet.
        // We also cross-check with PlayerPrefs as a fallback.
        float currentGold = PlayerPrefs.GetFloat("SavedGold", 0f);

        // ── BUILD PERSISTENT DATA ──
        GamePersistData persistData = new GamePersistData();
        persistData.currentDay = PlayerPrefs.GetInt("CurrentDay", 1);
        persistData.tutorialDone = PlayerPrefs.GetInt("TutorialDone", 0) == 1;
        persistData.gold = currentGold;
        persistData.lastSaveTime = DateTime.UtcNow.ToString("o");

        // Checkpoint = start of new day, so time is always 6:00 AM
        persistData.gameTime = 6f;

        // LOCAL BACKUP
        PlayerPrefs.SetFloat("SavedGameTime", 6f);
        PlayerPrefs.SetFloat("SavedGold", currentGold);
        PlayerPrefs.SetInt("HasSaveData", 1);
        PlayerPrefs.Save();

        // ── PENDING EMAILS (inbox offers not yet accepted) ──
        EmailManager email = EmailManager.Instance;
        if (email != null)
        {
            foreach (EmailData pending in email.activeEmails)
                persistData.activeEmails.Add(SerializeJob(pending));

            foreach (EmailData job in email.acceptedJobs)
                persistData.acceptedJobs.Add(SerializeJob(job));
        }

        // ── PLAYER INVENTORY ──
        // InventorySystem.AddPart() stores everything into playerStorage,
        // so ONE loop captures both parts removed from PCs AND loose world pickups.
        // The old separate invSys.entries block is no longer needed.
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

        // ── SHOP OWNED ITEMS ──
        ShopSystem shop = ShopSystem.Instance;
        if (shop != null)
        {
            persistData.ownedItemIDs = new List<string>(shop.GetInventoryIDs());
        }

        // ── PENDING DELIVERIES ──
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

        // ── UPLOAD TO PLAYFAB ──
        string persistJson = JsonUtility.ToJson(persistData);

        var request = new UpdateUserDataRequest
        {
            Data = new Dictionary<string, string>
            {
                { "Gold",     currentGold.ToString() },
                { "GameData", persistJson }
            }
        };

        PlayFabClientAPI.UpdateUserData(request,
            result =>
            {
                Debug.Log($"[CloudSave] End-of-day checkpoint saved! " +
                    $"Day: {persistData.currentDay}, " +
                    $"Gold: {persistData.gold:N0}, " +
                    $"PendingEmails: {persistData.activeEmails.Count}, " +
                    $"AcceptedJobs: {persistData.acceptedJobs.Count}, " +
                    $"Inventory: {persistData.inventoryParts.Count}, " +
                    $"Deliveries: {persistData.pendingDeliveries.Count}");

                // ── SYNC LEADERBOARD STATISTICS ──
                // Pushes Gold / PCsRepaired / OverallPoints to PlayFab Statistics
                // so the React website leaderboard reflects accurate live data.
                if (PlayFabStatSync.Instance != null)
                    PlayFabStatSync.Instance.SyncStats();
            },
            error => Debug.LogWarning("[CloudSave] Save failed: " + error.GenerateErrorReport()));
    }

    // =============================================
    //  LOAD ALL GAME DATA
    // =============================================
    public void LoadGameData()
    {
        if (!GameSession.IsLoggedIn)
        {
            Debug.Log("[CloudSave] Load skipped — not logged in (guest session).");
            return;
        }

        PlayFabClientAPI.GetUserData(new GetUserDataRequest(), OnDataReceived, OnError);
    }

    void OnDataReceived(GetUserDataResult result)
    {
        if (result.Data == null || result.Data.Count == 0)
        {
            Debug.Log("[CloudSave] No cloud data found (new account). Resetting to Day 1.");
            ResetToFreshState();
            return;
        }

        // Quick gold restore (skip on room change — PlayerPrefs has current value)
        if (!IsRoomChangeLoad)
        {
            if (result.Data.ContainsKey("Gold"))
            {
                float g = float.Parse(result.Data["Gold"].Value);
                SetAllWallets(g);
            }
            else
            {
                SetAllWallets(0f);
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
                Debug.Log("[CloudSave] GameData key is empty. Resetting to Day 1.");
                ResetToFreshState();
            }
        }
        else
        {
            Debug.Log("[CloudSave] No GameData key. Resetting to Day 1.");
            ResetToFreshState();
        }

        IsRoomChangeLoad = false;
        Debug.Log("[CloudSave] Load complete.");
    }

    void ResetToFreshState()
    {
        PlayerPrefs.SetInt("CurrentDay", 1);
        PlayerPrefs.SetInt("TutorialDone", 0);
        PlayerPrefs.SetInt("IsLoadingGame", 0);
        PlayerPrefs.SetFloat("SavedGold", 0f);
        PlayerPrefs.SetInt("HasSaveData", 0);
        PlayerPrefs.Save();

        SetAllWallets(0f);

        Debug.Log("[CloudSave] Local state reset to fresh Day 1.");
        SyncDayTransitionManager();
        WalkInLimiter.ResetDaily();
        if (TutorialManager.Instance != null)
            TutorialManager.Instance.RestartTutorial();
    }

    void SyncDayTransitionManager()
    {
        if (DayTransitionManager.Instance != null)
            DayTransitionManager.Instance.SyncCurrentDay();
    }

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
        yield return null;
        yield return null;
        yield return null;
        LoadGameData();
    }

    // =============================================
    //  RESTORE ALL GAME DATA
    // =============================================
    void RestoreGameData(GamePersistData data)
    {
        if (data == null) return;

        // ── DAY & TUTORIAL (skip on room change) ──
        if (!IsRoomChangeLoad)
        {
            PlayerPrefs.SetInt("CurrentDay", data.currentDay);
            PlayerPrefs.SetInt("TutorialDone", data.tutorialDone ? 1 : 0);
            PlayerPrefs.Save();
            Debug.Log("[CloudSave] Restored day: " + data.currentDay + ", tutorialDone: " + data.tutorialDone);
        }

        // ── GOLD (skip on room change — current session value stays) ──
        if (!IsRoomChangeLoad)
        {
            SetAllWallets(data.gold);
            PlayerPrefs.SetFloat("SavedGold", data.gold);
            PlayerPrefs.Save();
            Debug.Log("[CloudSave] Restored gold: " + data.gold.ToString("N0"));
        }

        // ── SAVE TIMESTAMP ──
        if (!string.IsNullOrEmpty(data.lastSaveTime))
            Debug.Log("[CloudSave] Last saved at: " + data.lastSaveTime);

        // ── GAME TIME (skip on room change — PlayerPrefs has current time) ──
        if (!IsRoomChangeLoad && data.gameTime > 0f)
        {
            PlayerPrefs.SetFloat("SavedGameTime", data.gameTime);
            PlayerPrefs.Save();
            Debug.Log("[CloudSave] Restored game time: " + data.gameTime.ToString("F2") + "h");

            DayTimeUI dayTimeUI = FindFirstObjectByType<DayTimeUI>();
            if (dayTimeUI != null)
                dayTimeUI.SetTime(data.gameTime);
        }

        if (!IsRoomChangeLoad)
        {
            IsRestoringCheckpoint = true;
            SyncDayTransitionManager();
            WalkInLimiter.ResetDaily();
        }

        // ── EMAILS & JOBS ──
        EmailManager email = EmailManager.Instance;
        if (email != null)
        {
            bool hasEmailData = (data.activeEmails != null && data.activeEmails.Count > 0)
                             || (data.acceptedJobs != null && data.acceptedJobs.Count > 0);

            if (hasEmailData)
            {
                email.activeEmails.Clear();
                email.acceptedJobs.Clear();
            }

            if (data.activeEmails != null && data.activeEmails.Count > 0)
            {
                foreach (SavedJob savedEmail in data.activeEmails)
                {
                    EmailData pending = DeserializeJob(savedEmail);
                    if (pending != null)
                        email.activeEmails.Add(pending);
                }
                Debug.Log("[CloudSave] Restored " + data.activeEmails.Count + " pending emails.");
            }

            if (data.acceptedJobs != null && data.acceptedJobs.Count > 0)
            {
                foreach (SavedJob savedJob in data.acceptedJobs)
                {
                    EmailData job = DeserializeJob(savedJob);
                    if (job != null)
                    {
                        bool spawned = email.SpawnBoxFromData(job.basePCCasePrefab, job.startingParts, job);
                        if (!spawned)
                            Debug.LogWarning("[CloudSave] No shelf space for restored job: " + savedJob.senderName);
                        email.acceptedJobs.Add(job);
                    }
                }
                Debug.Log("[CloudSave] Restored " + data.acceptedJobs.Count + " accepted jobs.");
            }

            email.RefreshInboxUI();
        }

        // ── INVENTORY PARTS ──
        // Recreates saved parts and adds them to playerStorage.
        // InventorySystem then syncs those into the StorageRoomShelf
        // so the physical storage room shows the correct items on its shelves.
        InspectionManager inspection = FindFirstObjectByType<InspectionManager>();
        if (inspection != null && data.inventoryParts != null && data.inventoryParts.Count > 0)
        {
            foreach (SavedPart savedPart in data.inventoryParts)
            {
                GameObject partObj = RecreatePart(savedPart);
                if (partObj == null) continue;

                // Keep hidden — InventorySystem.HideFromWorld already does this,
                // but SetActive(false) + position guard is the belt-and-braces approach.
                partObj.SetActive(false);
                partObj.transform.position = new Vector3(0f, -999f, 0f);

                inspection.playerStorage.Add(partObj);
            }

            Debug.Log("[CloudSave] Restored " + data.inventoryParts.Count + " inventory parts.");

            // ── SYNC STORAGE ROOM SHELF VISUALS ──
            // After all parts are in playerStorage, tell StorageRoomShelf to
            // place 3D props on the shelves so the room looks populated.
            if (InventorySystem.Instance != null && InventorySystem.Instance.storageShelf != null)
                InventorySystem.Instance.storageShelf.SyncWithStorage(inspection.playerStorage);
        }

        // ── SHOP OWNED ITEMS ──
        ShopSystem shop = ShopSystem.Instance;
        if (shop != null && data.ownedItemIDs != null && data.ownedItemIDs.Count > 0)
        {
            shop.SetInventoryIDs(data.ownedItemIDs.ToArray());
            Debug.Log("[CloudSave] Restored " + data.ownedItemIDs.Count + " shop owned items.");
        }

        // ── PENDING DELIVERIES (skip on room change — DeliveryManager uses static SavedOrders) ──
        if (!IsRoomChangeLoad)
        {
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
        saved.buildPurpose = (int)job.buildPurpose;
        saved.reward = job.reward;
        saved.objectives = job.objectives;
        saved.pcProblems = job.pcProblems;
        saved.originalFaultCount = job.originalFaultCount;

        if (job.basePCCasePrefab != null)
            saved.casePrefabName = job.basePCCasePrefab.name;

        if (job.startingParts != null)
            foreach (var part in job.startingParts)
                saved.startingParts.Add(SerializeStartingPart(part));

        if (job.requestedParts != null)
            foreach (var part in job.requestedParts)
                saved.requestedParts.Add(SerializeStartingPart(part));

        return saved;
    }

    SavedPart SerializeStartingPart(StartingPCComponent part)
    {
        SavedPart saved = new SavedPart();
        saved.partCategory = part.partCategory;
        saved.partName = part.partName;
        saved.prefabName = part.partPrefab != null ? part.partPrefab.name : "";
        saved.compatTags = part.compatTags;
        saved.partPrice = part.partPrice;
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
        job.buildPurpose = (BuildPurpose)saved.buildPurpose;
        job.reward = saved.reward;
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
        part.partPrice = saved.partPrice;
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
            item.sourceOwner = saved.sourceOwner;


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

        // Try exact match first
        foreach (GameObject casePrefab in partDatabase.cases)
            if (casePrefab != null && casePrefab.name == prefabName)
                return casePrefab;

        // Fallback: old case no longer exists — use the first available case
        if (partDatabase.cases != null && partDatabase.cases.Length > 0)
        {
            Debug.LogWarning($"[SaveRestore] Case '{prefabName}' not found — falling back to '{partDatabase.cases[0].name}'.");
            return partDatabase.cases[0];
        }

        return null;
    }

    GameObject FindPartPrefab(string prefabName, string category)
    {
        if (string.IsNullOrEmpty(prefabName) || partDatabase == null) return null;

        StartingPCComponent[][] allArrays =
        {
            partDatabase.motherboards, partDatabase.psus,  partDatabase.cpus,
            partDatabase.gpus,         partDatabase.rams,  partDatabase.storage,
            partDatabase.coolers,      partDatabase.fans
        };

        // Exact name match first
        foreach (StartingPCComponent[] array in allArrays)
        {
            if (array == null) continue;
            foreach (StartingPCComponent comp in array)
                if (comp.partPrefab != null && comp.partPrefab.name == prefabName)
                    return comp.partPrefab;
        }

        // Category fallback
        foreach (StartingPCComponent[] array in allArrays)
        {
            if (array == null) continue;
            foreach (StartingPCComponent comp in array)
                if (comp.partCategory == category && comp.partPrefab != null)
                    return comp.partPrefab;
        }

        return null;
    }

    string GetPrefabNameForPart(InspectableItem item)
    {
        if (partDatabase == null)
            return item.gameObject.name.Replace("(Clone)", "").Trim();

        StartingPCComponent[][] allArrays =
        {
            partDatabase.motherboards, partDatabase.psus,  partDatabase.cpus,
            partDatabase.gpus,         partDatabase.rams,  partDatabase.storage,
            partDatabase.coolers,      partDatabase.fans
        };

        // Exact name + category match
        foreach (StartingPCComponent[] array in allArrays)
        {
            if (array == null) continue;
            foreach (StartingPCComponent comp in array)
                if (comp.partCategory == item.partCategory
                    && comp.partName == item.itemName
                    && comp.partPrefab != null)
                    return comp.partPrefab.name;
        }

        // Category fallback
        foreach (StartingPCComponent[] array in allArrays)
        {
            if (array == null) continue;
            foreach (StartingPCComponent comp in array)
                if (comp.partCategory == item.partCategory && comp.partPrefab != null)
                    return comp.partPrefab.name;
        }

        return item.gameObject.name.Replace("(Clone)", "").Trim();
    }

    ItemData FindItemDataById(string itemId, ShopSystem shop)
    {
        if (string.IsNullOrEmpty(itemId) || shop == null) return null;
        foreach (ItemData item in shop.allAvailableItems)
            if (item != null && item.id == itemId)
                return item;
        return null;
    }

    // =============================================
    //  WALLET HELPER
    // =============================================

    /// <summary>
    /// Sets currentGold on EVERY PlayerWallet in the scene and updates their UI.
    /// </summary>
    void SetAllWallets(float gold)
    {
        PlayerWallet[] allWallets = FindObjectsByType<PlayerWallet>(FindObjectsSortMode.None);
        foreach (PlayerWallet w in allWallets)
        {
            w.currentGold = gold;
            w.UpdateUI();
        }
    }

    void OnError(PlayFabError error)
    {
        Debug.LogWarning("[CloudSave] Error: " + error.ErrorMessage);
    }
}
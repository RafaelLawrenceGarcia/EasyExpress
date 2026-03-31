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
        PlayerWallet wallet = FindFirstObjectByType<PlayerWallet>();
        float currentGold = wallet != null ? wallet.currentGold : 0f;

        // Build persistent data
        GamePersistData persistData = new GamePersistData();
        persistData.currentDay = PlayerPrefs.GetInt("CurrentDay", 1);

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
            result => Debug.Log("[CloudSave] Save successful! Jobs: " + persistData.acceptedJobs.Count +
                              ", Inventory: " + persistData.inventoryParts.Count),
            error => Debug.LogError("[CloudSave] Save failed: " + error.GenerateErrorReport()));
    }

    // =============================================
    //  LOAD ALL GAME DATA
    // =============================================
    public void LoadGameData()
    {
        PlayFabClientAPI.GetUserData(new GetUserDataRequest(), OnDataReceived, OnError);
    }

    void OnDataReceived(GetUserDataResult result)
    {
        if (result.Data == null) return;

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

        // Load persistent game data
        if (result.Data.ContainsKey("GameData"))
        {
            string json = result.Data["GameData"].Value;
            if (!string.IsNullOrEmpty(json))
            {
                GamePersistData persistData = JsonUtility.FromJson<GamePersistData>(json);
                RestoreGameData(persistData);
            }
        }

        Debug.Log("[CloudSave] Load complete.");
    }
    /// <summary>
    /// Waits for scene singletons to be ready, then loads.
    /// Call this from GameManager.OnSceneLoaded instead of LoadGameData directly.
    /// </summary>
    public void LoadGameDataDelayed()
    {
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

        // Restore accepted jobs
        EmailManager email = EmailManager.Instance;
        if (email != null && data.acceptedJobs.Count > 0)
        {
            foreach (SavedJob savedJob in data.acceptedJobs)
            {
                EmailData job = DeserializeJob(savedJob);
                if (job != null)
                {
                    // Add to accepted list and spawn the box on a shelf
                    bool spawned = email.SpawnBoxFromData(job.basePCCasePrefab, job.startingParts, job);
                    if (spawned)
                    {
                        email.acceptedJobs.Add(job);
                    }
                    else
                    {
                        Debug.LogWarning("[CloudSave] No shelf space for restored job: " + savedJob.senderName);
                        // Still add to accepted list so it shows in email
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

        // Find case prefab
        job.basePCCasePrefab = FindCasePrefab(saved.casePrefabName);

        // Reconstruct parts
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

    /// <summary>
    /// Recreate a part GameObject from saved data for inventory restoration.
    /// </summary>
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

        // Search all part arrays in the database
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

        // Fallback: match by category if exact name not found
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

    /// <summary>
    /// Try to determine the prefab name for an existing part instance.
    /// Falls back to searching the database by category.
    /// </summary>
    string GetPrefabNameForPart(InspectableItem item)
    {
        if (partDatabase == null) return item.gameObject.name.Replace("(Clone)", "").Trim();

        StartingPCComponent[][] allArrays = new StartingPCComponent[][]
        {
            partDatabase.motherboards, partDatabase.psus, partDatabase.cpus,
            partDatabase.gpus, partDatabase.rams, partDatabase.storage,
            partDatabase.coolers, partDatabase.fans
        };

        // Match by category and name
        foreach (StartingPCComponent[] array in allArrays)
        {
            if (array == null) continue;
            foreach (StartingPCComponent comp in array)
            {
                if (comp.partCategory == item.partCategory && comp.partName == item.itemName && comp.partPrefab != null)
                    return comp.partPrefab.name;
            }
        }

        // Fallback: just use the category match
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

    void OnError(PlayFabError error)
    {
        Debug.LogError("[CloudSave] Error: " + error.ErrorMessage);
    }
}
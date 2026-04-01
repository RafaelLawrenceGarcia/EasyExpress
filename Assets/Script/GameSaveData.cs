using System;
using System.Collections.Generic;

/// <summary>
/// Serializable wrapper for all persistent game data.
/// Converted to JSON and stored in PlayFab user data.
/// </summary>
[Serializable]
public class GamePersistData
{
    public List<SavedJob> acceptedJobs = new List<SavedJob>();
    public List<SavedPart> inventoryParts = new List<SavedPart>();
    public int currentDay = 1;
    public bool tutorialDone = false;

    // Shop purchases (item IDs the player owns)
    public List<string> ownedItemIDs = new List<string>();

    // Pending delivery orders that haven't arrived yet
    public List<SavedDelivery> pendingDeliveries = new List<SavedDelivery>();
}

/// <summary>
/// Serializable version of an accepted EmailData job.
/// </summary>
[Serializable]
public class SavedJob
{
    public string senderName;
    public string subjectLine;
    public string bodyText;
    public int jobType;
    public float labourCost;
    public float partsBudget;
    public string[] objectives;
    public string[] pcProblems;
    public int originalFaultCount;
    public List<SavedPart> startingParts = new List<SavedPart>();
    public List<SavedPart> requestedParts = new List<SavedPart>();
    public string casePrefabName;
}

/// <summary>
/// Serializable version of a PC component (used for both job parts and inventory).
/// </summary>
[Serializable]
public class SavedPart
{
    public string partCategory;
    public string partName;
    public string prefabName;
    public string[] compatTags;
    public float powerDraw;
    public float maxWattage;
    public bool isDusty;
    public int fault;
    public string faultDescription;
}

/// <summary>
/// Serializable version of a pending delivery order.
/// </summary>
[Serializable]
public class SavedDelivery
{
    public string itemId;       // ItemData.id to look up on restore
    public int quantity;
    public int daysRemaining;
}
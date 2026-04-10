using System;
using System.Collections.Generic;

[Serializable]
public class GamePersistData
{
    // Core progression
    public int currentDay = 1;
    public bool tutorialDone = false;
    public float gold = 0f;

    // In-game clock (0-24 float, e.g. 7.5 = 7:30 AM)
    public float gameTime = 6f;

    // UTC timestamp of last save (ISO 8601)
    public string lastSaveTime = "";

    // Email jobs: pending inbox AND accepted in-progress
    public List<SavedJob> activeEmails = new List<SavedJob>();
    public List<SavedJob> acceptedJobs = new List<SavedJob>();

    // Player inventory (loose parts the player picked up)
    public List<SavedPart> inventoryParts = new List<SavedPart>();

    // Shop purchases (item IDs the player owns)
    public List<string> ownedItemIDs = new List<string>();

    // Pending delivery orders that haven't arrived yet
    public List<SavedDelivery> pendingDeliveries = new List<SavedDelivery>();
}

[Serializable]
public class SavedJob
{
    public string senderName;
    public string subjectLine;
    public string bodyText;
    public int jobType;
    public int buildPurpose;
    public float reward;
    public string[] objectives;
    public string[] pcProblems;
    public int originalFaultCount;
    public List<SavedPart> startingParts = new List<SavedPart>();
    public List<SavedPart> requestedParts = new List<SavedPart>();
    public string casePrefabName;
}

[Serializable]
public class SavedPart
{
    public string sourceOwner = "";  // ← add here
    public string partCategory;
    public string partName;
    public string prefabName;
    public string[] compatTags;
    public float partPrice;
    public float powerDraw;
    public float maxWattage;
    public bool isDusty;
    public int fault;
    public string faultDescription;
}

[Serializable]
public class SavedDelivery
{
    public string itemId;
    public int quantity;
    public int daysRemaining;
}
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
}

/// <summary>
/// Serializable version of an accepted EmailData job.
/// </summary>
[Serializable]
public class SavedJob
{
    // Sender info
    public string senderName;
    public string subjectLine;
    public string bodyText;

    // Job type & stats
    public int jobType; // 0=Repair, 1=Build
    public float labourCost;
    public float partsBudget;

    // Requirements
    public string[] objectives;
    public string[] pcProblems;
    public int originalFaultCount;

    // Parts
    public List<SavedPart> startingParts = new List<SavedPart>();
    public List<SavedPart> requestedParts = new List<SavedPart>();

    // Case prefab identifier (matched by name from database)
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
    public string prefabName;  // Name of the prefab to look up in the database
    public string[] compatTags;
    public float powerDraw;
    public float maxWattage;
    public bool isDusty;

    // Fault data
    public int fault; // PartFault enum as int
    public string faultDescription;
}
using UnityEngine;
using System.Collections.Generic;

// =============================================
//  JOB TYPE SYSTEM
// =============================================
public enum JobType
{
    Repair,     // Fix a broken PC (existing system)
    Build       // Build a PC from scratch (new system)
}

[CreateAssetMenu(fileName = "New Email", menuName = "EasyExpress/Email Job")]
public class EmailData : ScriptableObject
{
    [Header("Sender Info")]
    public string senderName;
    public Sprite profilePic;
    public string subjectLine;

    [TextArea(5, 10)]
    public string bodyText;

    [Header("Job Type")]
    public JobType jobType = JobType.Repair;

    [Header("Job Stats")]
    public float labourCost;
    public float partsBudget;

    [Header("Requirements")]
    public string[] objectives;

    [Header("PC Diagnostics (Repair Jobs)")]
    public string[] pcProblems;

    [Tooltip("How many faults were originally assigned (set automatically)")]
    [HideInInspector] public int originalFaultCount = 0;

    [Header("Build Request (Build Jobs)")]
    [Tooltip("The parts the customer wants installed in their new PC")]
    public List<StartingPCComponent> requestedParts;

    [Header("Auto-Build PC Setup")]
    [Tooltip("The empty PC case prefab to spawn on the shelf.")]
    public GameObject basePCCasePrefab;
    [Tooltip("The parts that will automatically be built inside the case.")]
    public List<StartingPCComponent> startingParts;
}

// =============================================
//  PART FAULT SYSTEM
// =============================================
public enum PartFault
{
    None,               // Part is fine
    NotSeated,          // Part not plugged in properly (e.g. RAM not clicked in)
    Broken,             // Part is dead / defective
    Dusty,              // Clogged with dust
    Incompatible,       // Wrong specs for this motherboard
    Overloaded,         // PSU can't handle the power draw
    Overheating,        // Thermal paste dried out / cooler not working
    Corrupted,          // Storage has corrupted data
    LooseConnection,    // Cable or part is loose
    WrongSlot,          // Installed in the wrong slot
    Outdated            // Part is too old / needs replacement
}

[System.Serializable]
public class StartingPCComponent
{
    public string partCategory;
    public string partName;
    public GameObject partPrefab;
    public Sprite partIcon;

    // Compatibility
    [Tooltip("e.g. DDR4, LGA1700, PCIe4")]
    public string[] compatTags;

    [Tooltip("Watts this part draws")]
    public float powerDraw = 0f;

    [Tooltip("For PSU: max wattage output")]
    public float maxWattage = 0f;

    [Tooltip("Is this PC dusty?")]
    public bool isDusty = false;

    // Fault system
    [Header("Diagnosis")]
    [Tooltip("The fault assigned to this part (None = healthy)")]
    public PartFault fault = PartFault.None;

    [Tooltip("Description of the fault shown to the player on hover")]
    public string faultDescription = "";
}
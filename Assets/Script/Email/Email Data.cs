using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Email", menuName = "EasyExpress/Email Job")]
public class EmailData : ScriptableObject
{
    [Header("Sender Info")]
    public string senderName;
    public Sprite profilePic;
    public string subjectLine;

    [TextArea(5, 10)]
    public string bodyText;

    [Header("Job Stats")]
    public float labourCost;
    public float partsBudget;

    [Header("Requirements")]
    public string[] objectives;

    [Header("PC Diagnostics")]
    public string[] pcProblems;

    [Header("Auto-Build PC Setup")]
    [Tooltip("The empty PC case prefab to spawn on the shelf.")]
    public GameObject basePCCasePrefab;
    [Tooltip("The parts that will automatically be built inside the case.")]
    public List<StartingPCComponent> startingParts;
}

// Defining the component class down here keeps Unity's asset system happy!
[System.Serializable]
public class StartingPCComponent
{
    public string partCategory;
    public string partName;
    public GameObject partPrefab;
    public Sprite partIcon;
    
    // NEW: Compatibility
    [Tooltip("e.g. DDR4, LGA1700, PCIe4")]
    public string[] compatTags;
    
    [Tooltip("Watts this part draws")]
    public float powerDraw = 0f;
    
    [Tooltip("For PSU: max wattage output")]
    public float maxWattage = 0f;
    
    [Tooltip("Is this PC dusty?")]
    public bool isDusty = false;
}
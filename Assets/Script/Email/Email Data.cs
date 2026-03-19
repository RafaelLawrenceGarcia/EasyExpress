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
    public string partCategory;   // e.g., "RAM", "GPU", "CaseFan"
    public string partName;       // e.g., "8GB DDR4 FuryX"
    public GameObject partPrefab; // The 3D model to spawn

    [Tooltip("The icon that will display in the PC Status UI menu.")]
    public Sprite partIcon;       // NEW: The image to show in the UI!
}
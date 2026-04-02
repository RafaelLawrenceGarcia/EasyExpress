using System.Collections.Generic;
using UnityEngine;

public class InspectableItem : MonoBehaviour
{
    [Header("Compatibility System")]
    [Tooltip("Tags like DDR4, DDR5, LGA1700, AM5, ATX, etc.")]
    public string[] compatTags;

    [Tooltip("For SLOTS: what tags must a part have to fit here?")]
    public string[] requiredTags;

    [Tooltip("Watts this component draws (GPU=250, CPU=125, RAM=5, etc.)")]
    public float powerDraw = 0f;

    [Tooltip("For PSU only: max watts it can supply")]
    public float maxWattage = 0f;

    [Header("Screw System")]
    [Tooltip("Does this part require a screwdriver to remove/install?")]
    public bool requiresScrewdriver = true;

    [Header("Item Info")]
    public string itemName = "Unknown Item";

    // NEW: Categorizes the part so the storage knows what slot it fits into!
    public string partCategory = "Generic";
    public Sprite inventoryIcon;
    [TextArea(3, 5)]
    public string itemDescription = "No description available.";
    public GameObject itemIconPrefab;
    [HideInInspector] public Sprite cachedShopIcon;

    [Header("Interaction Rules")]
    public bool isMainObject = false;

    [Header("Advanced Interactions")]
    public bool isRemovable = false;
    public bool isPowerButton = false;
    public bool isWirePort = false;

    [Header("Wiring System (For Ports Only)")]
    public InspectableItem parentComponent;

    public string connectorType = "";
    public bool isPSUPort = false;

    [HideInInspector] public bool isOccupied = false;
    [HideInInspector] public InspectableItem connectedTo = null;
    [HideInInspector] public GameObject attachedWire = null;

    [Header("Wiring - Ribbon Spread")]
    [Tooltip("World-space axis along which strands fan out.")]
    public Vector3 ribbonAxis = Vector3.up;

    [Header("Wire Head Placement")]
    public bool placeHeadHere = false;
    public GameObject wireHead = null;

    void Awake()
    {
        if (wireHead != null) wireHead.SetActive(false);
    }

    [Header("Removal Prerequisites")]
    public List<InspectableItem> blockingParts = new List<InspectableItem>();

    // =============================================
    //  NEW: CHILD DETACH SYSTEM
    //  Drag child GameObjects here that should NOT
    //  be removed when this part is removed.
    //  Example: On your CPU, drag the Cooler, Pipes,
    //  and Backplate here. When the CPU is removed,
    //  these children get re-parented to the case
    //  and stay behind as independent parts.
    // =============================================
    [Header("Child Detach (CPU Fix)")]
    [Tooltip("Child objects that should stay behind when this part is removed.\n" +
             "Example: On CPU, drag the Cooler, PipeLocker, and Backplate here.\n" +
             "They will become independent parts instead of being removed with the CPU.")]
    public List<GameObject> childPartsToDetach = new List<GameObject>();

    // Set at runtime when this object is a ghost placeholder for a removed part
    [HideInInspector] public bool isInventorySlot = false;
    [HideInInspector] public string inventoryEntryId = "";

    // --- FAULT DIAGNOSIS SYSTEM ---
    [Header("Diagnosis")]
    [HideInInspector] public PartFault fault = PartFault.None;
    [HideInInspector] public string faultDescription = "";

    public bool IsFaulty()
    {
        return fault != PartFault.None;
    }
}
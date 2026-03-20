using System.Collections.Generic;
using UnityEngine;

public class InspectableItem : MonoBehaviour
{
    [Header("Item Info")]
    public string itemName = "Unknown Item";
    
    // NEW: Categorizes the part so the storage knows what slot it fits into!
    public string partCategory = "Generic"; 
    
    [TextArea(3, 5)]
    public string itemDescription = "No description available.";
    public GameObject itemIconPrefab;
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
    [Tooltip("World-space axis along which strands fan out. Ignores the port GameObject's rotation.\n" +
             "• Motherboard / device ports → (0,1,0) = world UP   → vertical spread\n" +
             "• PSU ports                  → (1,0,0) = world RIGHT → horizontal spread")]
    public Vector3 ribbonAxis = Vector3.up;

    [Header("Wire Head Placement")]
    [Tooltip("Tick this on the port that should receive the connector head mesh " +
             "(e.g. the motherboard 24-pin socket).")]
    public bool placeHeadHere = false;

    [Tooltip("Drag the already-placed connector head GameObject here. " +
             "It will be hidden by default and shown when the wire is connected.")]
    public GameObject wireHead = null;

    void Awake()
    {
        if (wireHead != null) wireHead.SetActive(false);
    }

    [Header("Removal Prerequisites")]
    public List<InspectableItem> blockingParts = new List<InspectableItem>();

    // Set at runtime when this object is a ghost placeholder for a removed part
    [HideInInspector] public bool   isInventorySlot  = false;
    [HideInInspector] public string inventoryEntryId = "";
}
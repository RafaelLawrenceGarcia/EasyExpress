using System.Collections.Generic;
using UnityEngine;

public class InspectableItem : MonoBehaviour
{
    [Header("Item Info")]
    public string itemName = "Unknown Item";
    [TextArea(3, 5)]
    public string itemDescription = "No description available.";

    [Header("Interaction Rules")]
    public bool isMainObject = false;

    [Header("Advanced Interactions")]
    public bool isRemovable = false;
    public bool isPowerButton = false;
    public bool isWirePort = false;

    [Header("Wiring System (For Ports Only)")]
    public InspectableItem parentComponent;

    // The connector type tag, e.g. "24pin", "8pin_cpu", "sata", "pcie_6pin"
    // A port can only connect to a port with the SAME connectorType.
    public string connectorType = "";

    // True = this port lives on the PSU side (source), False = motherboard/device side (sink)
    // A cable goes from a SOURCE port to a SINK port of the same type.
    public bool isPSUPort = false;

    // Runtime — set by InspectionManager when a wire is connected here
    [HideInInspector] public bool isOccupied = false;
    [HideInInspector] public InspectableItem connectedTo = null;   // the other port
    [HideInInspector] public GameObject attachedWire = null;       // the wire GO

    [Header("Removal Prerequisites")]
    public List<InspectableItem> blockingParts = new List<InspectableItem>();
}
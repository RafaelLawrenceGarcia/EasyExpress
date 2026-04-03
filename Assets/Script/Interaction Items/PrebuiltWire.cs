using System.Collections.Generic;
using UnityEngine;

public class PrebuiltWire : MonoBehaviour, IPrebuiltWire
{
    [Header("Wire Mesh")]
    [Tooltip("Root of the visible wire mesh(es). Shown/hidden on connect/disconnect.\n" +
             "If empty, defaults to this GameObject.")]
    public GameObject wireMeshRoot;

    [Header("Connector Port")]
    [Tooltip("The InspectableItem port the player clicks. Must have isWirePort=true.\n" +
             "Auto-found at Awake if left empty.")]
    public InspectableItem connectorPort;

    [Header("Requirement")]
    [Tooltip("Part category that must be installed first (e.g. GPU, Motherboard, Storage).\n" +
             "Leave EMPTY for always-connectable wires (power cord, fan wires).")]
    public string requiredPartCategory = "";

    [Header("Power Cord")]
    [Tooltip("Check for the mains power cord. Connecting also sets PCPowerSystem.isPowerCordConnected.")]
    public bool isPowerCord = false;

    [Header("Display Name")]
    [Tooltip("Shown in tooltips (e.g. '8-Pin GPU Power', '24-Pin ATX', 'Power Cord').")]
    public string wireName = "Cable";

    [HideInInspector] public bool isConnected = false;
    [HideInInspector] public InspectableItem wireInspectableItem;

    // ── IPrebuiltWire interface properties ──
    public bool IsConnected => isConnected;
    public bool IsPowerCord => isPowerCord;
    public string WireName => wireName;
    public string RequiredPartCategory => requiredPartCategory;
    public InspectableItem ConnectorPort => connectorPort;
    public GameObject WireMeshRoot => wireMeshRoot;

    void Awake()
    {
        if (wireMeshRoot == null)
            wireMeshRoot = gameObject;

        if (connectorPort == null)
        {
            foreach (InspectableItem child in GetComponentsInChildren<InspectableItem>(true))
            {
                if (child.isWirePort)
                {
                    connectorPort = child;
                    break;
                }
            }
        }

        if (connectorPort != null)
            connectorPort.linkedPrebuiltWire = this;

        SetWireVisible(false);
    }

    public void Connect(Transform pcRoot)
    {
        isConnected = true;
        SetWireVisible(true);
        EnsureWireInspectableItem();

        InspectableItem parentComponent = FindParentComponent(pcRoot);
        if (parentComponent != null && wireInspectableItem != null)
        {
            if (parentComponent.blockingParts == null)
                parentComponent.blockingParts = new List<InspectableItem>();
            if (!parentComponent.blockingParts.Contains(wireInspectableItem))
            {
                parentComponent.blockingParts.Add(wireInspectableItem);
                Debug.Log($"[PrebuiltWire] '{wireName}' now blocks removal of '{parentComponent.itemName}'.");
            }
        }

        if (isPowerCord && pcRoot != null)
        {
            PCPowerSystem power = pcRoot.GetComponent<PCPowerSystem>();
            if (power != null)
            {
                power.isPowerCordConnected = true;
                power.connectedCord = wireMeshRoot;
                Debug.Log($"[PrebuiltWire] Power cord connected.");
            }
        }

        Debug.Log($"[PrebuiltWire] '{wireName}' connected.");
    }

    public void Disconnect(Transform pcRoot)
    {
        isConnected = false;
        SetWireVisible(false);

        InspectableItem parentComponent = FindParentComponent(pcRoot);
        if (parentComponent != null && wireInspectableItem != null)
        {
            parentComponent.blockingParts.Remove(wireInspectableItem);
            Debug.Log($"[PrebuiltWire] '{wireName}' no longer blocks '{parentComponent.itemName}'.");
        }

        if (isPowerCord && pcRoot != null)
        {
            PCPowerSystem power = pcRoot.GetComponent<PCPowerSystem>();
            if (power != null)
            {
                power.DisconnectPowerCord();
                Debug.Log($"[PrebuiltWire] Power cord disconnected.");
            }
        }

        Debug.Log($"[PrebuiltWire] '{wireName}' disconnected.");
    }

    public bool IsRequiredComponentInstalled(Transform pcRoot)
    {
        if (string.IsNullOrEmpty(requiredPartCategory)) return true;
        if (pcRoot == null) return false;

        foreach (InspectableItem part in pcRoot.GetComponentsInChildren<InspectableItem>(true))
        {
            if (part.isMainObject) continue;
            if (part.isInventorySlot) continue;
            if (part.partCategory == requiredPartCategory)
                return true;
        }
        return false;
    }

    void SetWireVisible(bool visible)
    {
        if (wireMeshRoot == null) return;
        foreach (Renderer r in wireMeshRoot.GetComponentsInChildren<Renderer>(true))
        {
            if (connectorPort != null && r.transform == connectorPort.transform) continue;
            if (connectorPort != null && r.transform.IsChildOf(connectorPort.transform)) continue;
            r.enabled = visible;
        }
        if (wireInspectableItem != null)
        {
            Collider wireCol = wireInspectableItem.GetComponent<Collider>();
            if (wireCol != null) wireCol.enabled = visible;
        }
    }

    void EnsureWireInspectableItem()
    {
        if (wireInspectableItem != null) return;
        wireInspectableItem = wireMeshRoot.GetComponent<InspectableItem>();
        if (wireInspectableItem == null)
            wireInspectableItem = wireMeshRoot.AddComponent<InspectableItem>();
        wireInspectableItem.itemName = wireName;
        wireInspectableItem.itemDescription = isPowerCord
            ? "Power cord. Disconnect to unplug."
            : $"{wireName}. Disconnect before removing the component.";
        wireInspectableItem.isRemovable = true;
        wireInspectableItem.linkedPrebuiltWire = this;
        if (wireMeshRoot.GetComponent<Collider>() == null)
        {
            if (wireMeshRoot.GetComponent<MeshRenderer>() != null)
                wireMeshRoot.AddComponent<BoxCollider>();
        }
    }

    InspectableItem FindParentComponent(Transform pcRoot)
    {
        if (pcRoot == null) return null;
        string category = isPowerCord ? "PSU" : requiredPartCategory;
        if (string.IsNullOrEmpty(category)) return null;
        foreach (InspectableItem part in pcRoot.GetComponentsInChildren<InspectableItem>(true))
        {
            if (part.isMainObject) continue;
            if (part.isInventorySlot) continue;
            if (part.partCategory == category) return part;
        }
        return null;
    }
}
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
    [Tooltip("Part category that must be installed first (e.g. GPU, Motherboard, Storage, Case Fan).\n" +
             "Leave EMPTY for always-connectable wires (power cord).")]
    public string requiredPartCategory = "";

    [Header("Proximity Matching")]
    [Tooltip("When multiple parts share the same category (e.g. Case Fan),\n" +
             "this wire only connects to the NEAREST one within this radius.\n" +
             "Set to 0 to disable proximity matching (connects to any matching part).")]
    public float proximityRadius = 2f;

    [Header("Power Cord")]
    [Tooltip("Check for the mains power cord. Connecting also sets PCPowerSystem.isPowerCordConnected.")]
    public bool isPowerCord = false;
    [Header("Blocking Behaviour")]
    [Tooltip("If true, this wire does NOT block removal of its parent component.\n" +
           "Enable for fan wires — fans can be removed without disconnecting their cable first.")]
    public bool allowRemoveWithoutDisconnect = false;

    [Header("Display Name")]
    [Tooltip("Shown in tooltips (e.g. '8-Pin GPU Power', '24-Pin ATX', 'Power Cord').")]
    public string wireName = "Cable";

    [HideInInspector] public bool isConnected = false;
    [HideInInspector] public InspectableItem wireInspectableItem;

    // ── Cached reference to the specific part this wire is linked to ──
    [HideInInspector] public InspectableItem linkedPart;

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
            // Fan wires (and wires flagged allowRemoveWithoutDisconnect) don't block removal
            if (!allowRemoveWithoutDisconnect)
            {
                if (parentComponent.blockingParts == null)
                    parentComponent.blockingParts = new List<InspectableItem>();
                if (!parentComponent.blockingParts.Contains(wireInspectableItem))
                {
                    parentComponent.blockingParts.Add(wireInspectableItem);
                    Debug.Log($"[PrebuiltWire] '{wireName}' now blocks removal of '{parentComponent.itemName}'.");
                }
            }
            else
            {
                Debug.Log($"[PrebuiltWire] '{wireName}' connected but does NOT block removal (fan wire).");
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

                if (TutorialManager.Instance != null)
                    TutorialManager.Instance.NotifyPowerCordConnected();
            }
        }
    }

    public void Disconnect(Transform pcRoot)
    {
        isConnected = false;
        SetWireVisible(false);
        linkedPart = null;

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

        // If proximity matching is enabled, use nearest-part logic
        if (proximityRadius > 0f)
        {
            InspectableItem nearest = FindNearestPart(pcRoot, requiredPartCategory);
            return nearest != null;
        }

        // No proximity — any matching part counts
        foreach (InspectableItem part in pcRoot.GetComponentsInChildren<InspectableItem>(true))
        {
            if (part.isMainObject) continue;
            if (part.isInventorySlot) continue;
            if (part.partCategory == requiredPartCategory)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Finds the nearest installed part matching the given category.
    /// Uses renderer bounds center as fallback when transforms are at origin.
    /// If proximityRadius > 0, only parts within that distance are considered.
    /// This allows each fan wire to connect only to its own nearby fan.
    /// </summary>
    InspectableItem FindNearestPart(Transform pcRoot, string category)
    {
        if (string.IsNullOrEmpty(category) || pcRoot == null) return null;

        Vector3 wirePos = GetWorldPosition(transform);
        InspectableItem nearest = null;
        float nearestDist = float.MaxValue;

        foreach (InspectableItem part in pcRoot.GetComponentsInChildren<InspectableItem>(true))
        {
            if (part.isMainObject) continue;
            if (part.isInventorySlot) continue;
            if (part.partCategory != category) continue;

            Vector3 partPos = GetWorldPosition(part.transform);
            float dist = Vector3.Distance(wirePos, partPos);

            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearest = part;
            }
        }

        // Enforce the distance limit
        if (proximityRadius > 0f && nearestDist > proximityRadius)
            return null;

        return nearest;
    }

    /// <summary>
    /// Gets the actual world position of a transform.
    /// Falls back to renderer bounds center if localPosition is at origin
    /// (handles Blender imports where origins weren't set).
    /// </summary>
    static Vector3 GetWorldPosition(Transform t)
    {
        if (t.localPosition.sqrMagnitude > 0.0001f)
            return t.position;

        Renderer rend = t.GetComponent<Renderer>();
        if (rend == null) rend = t.GetComponentInChildren<Renderer>();
        if (rend != null) return rend.bounds.center;

        return t.position;
    }

    InspectableItem FindParentComponent(Transform pcRoot)
    {
        if (pcRoot == null) return null;
        string category = isPowerCord ? "PSU" : requiredPartCategory;
        if (string.IsNullOrEmpty(category)) return null;

        // If we have a cached linked part, use it
        if (linkedPart != null) return linkedPart;

        // Use proximity matching if enabled
        if (proximityRadius > 0f)
        {
            InspectableItem nearest = FindNearestPart(pcRoot, category);
            if (nearest != null)
            {
                linkedPart = nearest;
                return nearest;
            }
        }

        // Fallback: return any matching part
        foreach (InspectableItem part in pcRoot.GetComponentsInChildren<InspectableItem>(true))
        {
            if (part.isMainObject) continue;
            if (part.isInventorySlot) continue;
            if (part.partCategory == category) return part;
        }
        return null;
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
}
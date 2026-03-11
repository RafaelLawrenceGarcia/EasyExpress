using UnityEngine;
using System.Collections.Generic;

public class InventorySystem : MonoBehaviour
{
    public static InventorySystem Instance;

    public class InventoryEntry
    {
        public string      id;
        public GameObject  obj;
        public string      itemName;
        public string      itemDescription;
        public Transform   originalParent;
        public Vector3     localPosition;
        public Quaternion  localRotation;
        public Vector3     localScale;
        public Material[]  originalMaterials; // per-renderer original materials
        public GameObject  ghostObj;          // the placeholder left in-scene
    }

    public List<InventoryEntry> entries = new List<InventoryEntry>();
    public System.Action onInventoryChanged;

    private int _nextId = 0;

    void Awake() { Instance = this; }

    public InventoryEntry AddPart(GameObject obj, InspectableItem item)
    {
        // Capture original materials before we do anything
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        List<Material> mats = new List<Material>();
        foreach (var r in renderers)
            foreach (var m in r.sharedMaterials)
                mats.Add(m);

        var entry = new InventoryEntry
        {
            id              = (_nextId++).ToString(),
            obj             = obj,
            itemName        = item.itemName,
            itemDescription = item.itemDescription,
            originalParent  = obj.transform.parent,
            localPosition   = obj.transform.localPosition,
            localRotation   = obj.transform.localRotation,
            localScale      = obj.transform.localScale,
            originalMaterials = mats.ToArray(),
        };

        obj.SetActive(false);
        entries.Add(entry);
        onInventoryChanged?.Invoke();
        return entry;
    }

    public InventoryEntry GetEntry(string id)
    {
        return entries.Find(e => e.id == id);
    }

    /// <summary>
    /// Called by the animation coroutine after the part is already restored.
    /// Just destroys the ghost and removes the entry.
    /// </summary>
    public void ReinstallPart(string id)
    {
        InventoryEntry entry = GetEntry(id);
        if (entry == null) return;
        if (entry.ghostObj != null) Object.Destroy(entry.ghostObj);
        entries.Remove(entry);
        onInventoryChanged?.Invoke();
    }

    public void Clear()
    {
        foreach (var e in entries)
            if (e.ghostObj != null) Object.Destroy(e.ghostObj);
        entries.Clear();
        onInventoryChanged?.Invoke();
    }
}
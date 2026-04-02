// ============================================================
//  InventorySystem.cs  (FULL REPLACEMENT — replaces yesterday's version)
//  Easy Express – Thesis Project
// ============================================================
//
//  WHAT THIS DOES
//  ──────────────
//  Bridges world-pickup into the systems you already have:
//
//  PlayerInteract already calls:
//    InventorySystem.Instance.AddPart(worldObj, looseItem);   ← line 8705
//
//  This class makes that call:
//    1.  Disable the world item's mesh + colliders (invisible, un-clickable)
//    2.  Move it to Y=-999 so physics ignores it
//    3.  Add it to InspectionManager.playerStorage
//        → InspectionInventoryUI already reads playerStorage, so the
//          Tab-panel inside inspection mode will show the new item
//    4.  Tell StorageRoomShelf to refresh its physical shelf props
//
//  WHAT THIS DOES NOT REPLACE
//  ───────────────────────────
//  InspectionInventoryUI — left completely unchanged.
//  InspectionManager.TryInstallPart — left completely unchanged.
//    (When a part is installed, playerStorage.Remove() runs there.
//     StorageRoomShelf detects the change on its next poll.)
//
//  SETUP IN INSPECTOR
//  ──────────────────
//  • Add to the same persistent GameObject as CloudDataHandler.
//  • inspectionManager — drag your InspectionManager here.
//  • inventoryUI       — drag your InspectionInventoryUI here.
//  • storageShelf is left empty; StorageRoomShelf fills it on Awake.
//
// ============================================================

using UnityEngine;
using System.Collections.Generic;

public class InventorySystem : MonoBehaviour
{
    public static InventorySystem Instance;

    [Header("Required References")]
    [Tooltip("Your InspectionManager in the scene.")]
    public InspectionManager inspectionManager;

    [Tooltip("Your InspectionInventoryUI in the scene.")]
    public InspectionInventoryUI inventoryUI;

    // Auto-filled by StorageRoomShelf.Awake()
    [HideInInspector] public StorageRoomShelf storageShelf;

    // ─────────────────────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); }
    }

    void Start()
    {
        if (inspectionManager == null)
            inspectionManager = FindObjectOfType<InspectionManager>();
        if (inventoryUI == null)
            inventoryUI = FindObjectOfType<InspectionInventoryUI>();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  MAIN PUBLIC API
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Called by PlayerInteract when the player presses E on a loose PickupBox
    /// or loose component in the world.
    ///
    ///   InventorySystem.Instance.AddPart(hit.collider.gameObject, looseItem);
    ///
    /// </summary>
    public bool AddPart(GameObject worldObj, InspectableItem itemData)
    {
        if (worldObj == null || itemData == null)
        {
            Debug.LogWarning("[Inventory] AddPart called with null args.");
            return false;
        }

        // Lazy-find InspectionManager if not set
        if (inspectionManager == null)
        {
            inspectionManager = FindObjectOfType<InspectionManager>();
            if (inspectionManager == null)
            {
                Debug.LogError("[Inventory] InspectionManager missing — cannot store item.");
                return false;
            }
        }

        // ── Step 1: Hide from world ──────────────────────────────────────────
        HideFromWorld(worldObj);

        // ── Step 2: Add to playerStorage (what InspectionInventoryUI reads) ──
        if (!inspectionManager.playerStorage.Contains(worldObj))
            inspectionManager.playerStorage.Add(worldObj);

        Debug.Log($"[Inventory] +'{itemData.itemName}' ({itemData.partCategory}) " +
                  $"→ playerStorage now has {inspectionManager.playerStorage.Count} item(s).");

        // ── Step 3: Refresh Tab-panel if it's currently open ─────────────────
        RefreshUIIfOpen();

        // ── Step 4: Sync the storage room shelf visuals ───────────────────────
        SyncShelf();

        return true;
    }

    /// <summary>
    /// Drop an item from playerStorage back into the world at a given position.
    /// If no position given, drops in front of the main camera.
    /// </summary>
    public void DropPart(GameObject itemObj, Vector3 dropPos = default)
    {
        if (inspectionManager == null || itemObj == null) return;

        inspectionManager.playerStorage.Remove(itemObj);

        if (dropPos == default)
        {
            Camera cam = Camera.main;
            dropPos = cam != null
                ? cam.transform.position + cam.transform.forward * 1.5f
                : Vector3.zero;
        }

        itemObj.transform.position = dropPos + Vector3.up * 0.15f;
        ShowInWorld(itemObj);

        RefreshUIIfOpen();
        SyncShelf();

        Debug.Log($"[Inventory] Dropped item back to world at {dropPos}.");
    }

    /// <summary>
    /// Call this after InspectionManager.TryInstallPart removes an item from
    /// playerStorage so the shelf visuals update immediately.
    /// (StorageRoomShelf also polls on its own timer as a fallback.)
    /// </summary>
    public void NotifyInstalled()
    {
        SyncShelf();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  INTERNAL HELPERS
    // ─────────────────────────────────────────────────────────────────────────

    void RefreshUIIfOpen()
    {
        if (inventoryUI == null) inventoryUI = FindObjectOfType<InspectionInventoryUI>();
        if (inventoryUI != null
            && inventoryUI.inventoryPanel != null
            && inventoryUI.inventoryPanel.activeSelf)
        {
            inventoryUI.RefreshInventory();
        }
    }

    void SyncShelf()
    {
        if (storageShelf != null && inspectionManager != null)
            storageShelf.SyncWithStorage(inspectionManager.playerStorage);
    }

    // ── World presence helpers ────────────────────────────────────────────────

    public static void HideFromWorld(GameObject obj)
    {
        foreach (Renderer r in obj.GetComponentsInChildren<Renderer>(true))
            r.enabled = false;
        foreach (Collider c in obj.GetComponentsInChildren<Collider>(true))
            c.enabled = false;

        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb != null) { rb.linearVelocity = Vector3.zero; rb.isKinematic = true; }

        obj.transform.position = new Vector3(0f, -999f, 0f);
    }

    public static void ShowInWorld(GameObject obj)
    {
        foreach (Renderer r in obj.GetComponentsInChildren<Renderer>(true))
            r.enabled = true;
        foreach (Collider c in obj.GetComponentsInChildren<Collider>(true))
            c.enabled = true;

        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = false;
    }
}
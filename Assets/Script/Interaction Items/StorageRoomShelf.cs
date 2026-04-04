// ============================================================
//  StorageRoomShelf.cs
//  Easy Express – Thesis Project
// ============================================================
//  FULL FLOW:
//
//  1. Player buys item → ShopSystem.ownedItemIDs gets it
//  2. End day → DeliveryBox spawns
//  3. Player presses E on DeliveryBox → DeliveryBox.InteractUnpack()
//       → ShopSystem.AddItemDirectly(item)   ← already happens
//       → StorageRoomShelf sees new item in ShopSystem → spawns shelf prop
//  4. Player walks to shelf → "Press E to open storage" prompt appears
//  5. Player presses E → InspectionInventoryUI opens
//       (the SAME panel as Tab during inspection — already shows ShopSystem items)
//  6. Player clicks item in panel → TryInstallShopItem() runs
//       → item moves to playerStorage (character inventory)
//       → ShopSystem removes item → shelf prop disappears
//  7. During inspection → Tab → item shows → install into PC
//
//  SETUP IN INSPECTOR:
//  • Add this component to your shelf GameObject
//  • Add a Box Collider to the shelf, check "Is Trigger", set it to
//    cover the area in front of the shelf where the player stands
//  • Assign slotPositions (9 empty child GameObjects, one per compartment)
//  • Assign inventoryUI (your InspectionInventoryUI)
//  • Assign promptUI (your InteractionPromptUI, optional)
//  • Assign genericPropPrefab (a small box/crate prefab, optional)
// ============================================================

using UnityEngine;
using TMPro;
using System.Collections.Generic;

[RequireComponent(typeof(Collider))]
public class StorageRoomShelf : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────
    public static StorageRoomShelf Instance;
    [Header("Shelf Slots (one per compartment)")]
    [Tooltip("Create 9 empty child GameObjects, one per shelf compartment. " +
             "Position each one in the center of its compartment.")]
    public Transform[] slotPositions;

    [Header("UI Reference")]
    [Tooltip("Drag your InspectionInventoryUI here. " +
             "This is the SAME panel the player uses during inspection (Tab key).")]
    public InspectionInventoryUI inventoryUI;

    [Tooltip("Optional — drag your InteractionPromptUI here to show 'Press E' hint.")]
    public InteractionPromptUI promptUI;

    [Header("Shelf Props")]
    [Tooltip("Fallback prop when an item has no itemIconPrefab. Can be a simple box/crate.")]
    public GameObject genericPropPrefab;

    [Tooltip("How big props appear on the shelf. 0.15 = nice miniature size.")]
    public Vector3 propScale = new Vector3(0.15f, 0.15f, 0.15f);

    [Header("Interaction")]
    [Tooltip("Key the player presses to open the shelf inventory.")]
    public KeyCode interactKey = KeyCode.E;

    [Tooltip("How often (seconds) the shelf checks ShopSystem for new/removed items.")]
    public float pollInterval = 1f;
    private PlacementManager _placementManager;
    // ── Runtime ───────────────────────────────────────────────────────────────

    // itemId → slot index it's shown on
    private Dictionary<string, int> _shownItemIds = new Dictionary<string, int>();

    // slot index → prop GameObject
    private Dictionary<int, GameObject> _slotProps = new Dictionary<int, GameObject>();

    private float   _pollTimer      = 0f;
    private bool    _playerNearby   = false;
    private bool    _panelOpen      = false;

    // ─────────────────────────────────────────────────────────────────────────
    //  UNITY LIFECYCLE
    // ─────────────────────────────────────────────────────────────────────────

    void Awake()
    {
        Instance = this;
        // Make sure the trigger collider is set correctly
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;

        // Auto-register with InventorySystem so it can call SyncShelf()
        StartCoroutine(RegisterWithInventorySystem());
    }

    System.Collections.IEnumerator RegisterWithInventorySystem()
    {
        yield return null; // wait one frame for InventorySystem to Awake
        if (InventorySystem.Instance != null)
            InventorySystem.Instance.storageShelf = this;
    }

    void Start()
    {
        if (inventoryUI == null)
            inventoryUI = FindObjectOfType<InspectionInventoryUI>();

        // Cache PlacementManager reference
        _placementManager = FindObjectOfType<PlacementManager>();

        SyncWithShopSystem();
    }

    void Update()
    {
        // ── Periodic sync with ShopSystem ────────────────────────────────────
        _pollTimer += Time.deltaTime;
        if (_pollTimer >= pollInterval)
        {
            _pollTimer = 0f;
            SyncWithShopSystem();
        }

        // ── E to open/close shelf UI ─────────────────────────────────────────
        if (_playerNearby && Input.GetKeyDown(interactKey))
        {
            if (_panelOpen)
                CloseShelfUI();
            else
                OpenShelfUI();
        }

        // ── Close on Escape ──────────────────────────────────────────────────
        if (_panelOpen && Input.GetKeyDown(KeyCode.Escape))
            CloseShelfUI();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  PLAYER PROXIMITY (Trigger)
    // ─────────────────────────────────────────────────────────────────────────

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        _playerNearby = true;

        if (promptUI != null)
            promptUI.Show("E", "Open Storage");
        else
            Debug.Log("[Shelf] Press E to open storage.");
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        _playerNearby = false;

        if (promptUI != null)
            promptUI.Hide();

        if (_panelOpen)
            CloseShelfUI();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  OPEN / CLOSE SHELF UI
    // ─────────────────────────────────────────────────────────────────────────

    void OpenShelfUI()
    {
        if (inventoryUI == null) return;

        _panelOpen = true;

        // ── SET MODE TO SHELF ── so it only shows shelf items
        inventoryUI.currentMode = InspectionInventoryUI.InventoryMode.ShelfStorage;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;

        if (inventoryUI.inventoryPanel != null)
            inventoryUI.inventoryPanel.SetActive(true);

        inventoryUI.RefreshInventory();

        if (promptUI != null) promptUI.Hide();
    }

    void CloseShelfUI()
    {
        if (inventoryUI == null) return;

        _panelOpen = false;

        if (inventoryUI.inventoryPanel != null)
            inventoryUI.inventoryPanel.SetActive(false);

        // Lock cursor again for gameplay
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;

        if (promptUI != null && _playerNearby)
            promptUI.Show("E", "Open Storage");

        Debug.Log("[Shelf] Storage shelf UI closed.");
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  SYNC WITH SHOPSYSTEM (shelf props mirror ShopSystem.ownedItemIDs)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Called by poll timer, by InventorySystem.NotifyInstalled(),
    /// and by CloudDataHandler after restore.
    /// Adds a prop for every new item in ShopSystem.
    /// Removes a prop when an item is taken from ShopSystem.
    /// </summary>
    public void SyncWithShopSystem()
    {
        if (ShopSystem.Instance == null) return;
        if (slotPositions == null || slotPositions.Length == 0) return;

        List<string> currentIDs = ShopSystem.Instance.GetInventoryIDs();

        // ── Remove props for items no longer in ShopSystem ────────────────────
        List<string> toRemove = new List<string>();
        foreach (string id in _shownItemIds.Keys)
        {
            if (!currentIDs.Contains(id))
                toRemove.Add(id);
        }
        foreach (string id in toRemove)
            RemoveProp(id);

        // ── Add props for new items in ShopSystem ─────────────────────────────
        // Track which IDs we've already shown (handle duplicates properly)
        Dictionary<string, int> shownCount = new Dictionary<string, int>();
        foreach (string id in _shownItemIds.Keys)
        {
            if (!shownCount.ContainsKey(id)) shownCount[id] = 0;
            shownCount[id]++;
        }

        foreach (string id in currentIDs)
        {
            // If this id is already shown, skip it
            if (_shownItemIds.ContainsKey(id + "_" + GetShownCountForId(id)))
                continue;

            // Find free slot
            int freeSlot = FindFreeSlot();
            if (freeSlot < 0) break; // shelf full

            // Find the ItemData for this id
            ItemData itemData = FindItemById(id);
            if (itemData == null) continue;

            SpawnProp(freeSlot, id, itemData);
        }
    }

    /// <summary>
    /// Called by StorageRoomShelf when player is in trigger and picks up from shelf via UI.
    /// Also used by CloudDataHandler to sync after load.
    /// </summary>
    public void SyncWithStorage(List<GameObject> playerStorage)
    {
        // This is called by InventorySystem for playerStorage sync.
        // On the shelf we care about ShopSystem items, so just do a full sync.
        SyncWithShopSystem();
    }

    /// <summary>
    /// Returns the Transform of the nearest free slot, or null if shelf is full.
    /// Called by DeliveryBox.InteractUnpack() to find where to place the box.
    /// </summary>
    public Transform GetFreeSlot()
    {
        for (int i = 0; i < slotPositions.Length; i++)
        {
            if (!_slotProps.ContainsKey(i))
                return slotPositions[i];
        }
        return null;
    }

    /// <summary>
    /// Called by DeliveryBox after it moves itself to a slot.
    /// Registers the box GameObject as the visual prop for that slot
    /// so SyncWithShopSystem() knows the slot is occupied.
    /// </summary>
   public void RegisterBoxOnSlot(Transform slot, GameObject retailBox, ItemData item)
    {
        for (int i = 0; i < slotPositions.Length; i++)
        {
            if (slotPositions[i] == slot)
            {
                _slotProps[i] = retailBox;
                string key = item.id + "_" + GetShownCountForId(item.id);
                _shownItemIds[key] = i;
                Debug.Log($"[Shelf] '{item.itemName}' retail box on slot {i}.");
                return;
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  PROP MANAGEMENT
    // ─────────────────────────────────────────────────────────────────────────

    void SpawnProp(int slotIndex, string itemId, ItemData itemData)
{
    Transform slot = slotPositions[slotIndex];

    // Pick prop source priority:
    // 1. deliveryBoxPrefab  ← the retail box (nvidia box, amd box, etc.)
    // 2. itemIconPrefab     ← custom icon prefab on InspectableItem
    // 3. prefabToPlace      ← the actual part model shrunk down
    // 4. genericPropPrefab  ← last resort fallback
    GameObject propSource = null;

    // Priority 1 — delivery box prefab (the physical retail box)
    if (itemData.deliveryBoxPrefab != null)
        propSource = itemData.deliveryBoxPrefab;

    // Priority 2 — item icon prefab on InspectableItem
    if (propSource == null && itemData.prefabToPlace != null)
    {
        InspectableItem ii = itemData.prefabToPlace.GetComponent<InspectableItem>();
        if (ii != null && ii.itemIconPrefab != null)
            propSource = ii.itemIconPrefab;
    }

    // Priority 3 — the actual part model
    if (propSource == null && itemData.prefabToPlace != null)
        propSource = itemData.prefabToPlace;

    // Priority 4 — generic fallback
    if (propSource == null)
        propSource = genericPropPrefab;

    if (propSource == null)
    {
        Debug.LogWarning($"[Shelf] No prop source for '{itemData.itemName}'. " +
                         "Assign a deliveryBoxPrefab on the ItemData asset.");
        // Still register the slot so UI works, just no visual
        string regKey = itemId + "_" + GetShownCountForId(itemId);
        _shownItemIds[regKey] = slotIndex;
        return;
    }

    // Spawn prop
    GameObject prop = Instantiate(propSource, slot.position, slot.rotation, slot);
    prop.name = $"Prop_{itemData.itemName}_{slotIndex}";
    prop.transform.localScale = propScale;

    // Disable colliders (visual only)
    foreach (Collider c in prop.GetComponentsInChildren<Collider>(true))
        c.enabled = false;

    // Disable fan spin etc.
    foreach (PCFanController fan in prop.GetComponentsInChildren<PCFanController>(true))
        fan.enabled = false;

    // Remove rigidbody so it doesn't fall
    Rigidbody rb = prop.GetComponent<Rigidbody>();
    if (rb != null) Destroy(rb);

    // Make sure it's visible
    foreach (Renderer r in prop.GetComponentsInChildren<Renderer>(true))
        r.enabled = true;

    // Register
    string key = itemId + "_" + GetShownCountForId(itemId);
    _shownItemIds[key] = slotIndex;
    _slotProps[slotIndex] = prop;

    Debug.Log($"[Shelf] Placed '{itemData.itemName}' on slot {slotIndex}.");
}

    void RemoveProp(string key)
    {
        if (!_shownItemIds.TryGetValue(key, out int slotIndex)) return;

        if (_slotProps.TryGetValue(slotIndex, out GameObject prop))
        {
            if (prop != null) Destroy(prop);
            _slotProps.Remove(slotIndex);
        }

        _shownItemIds.Remove(key);
        Debug.Log($"[Shelf] Removed prop from slot {slotIndex}.");
    }

    int FindFreeSlot()
    {
        for (int i = 0; i < slotPositions.Length; i++)
            if (!_slotProps.ContainsKey(i)) return i;
        return -1;
    }

    int GetShownCountForId(string id)
    {
        int count = 0;
        foreach (string key in _shownItemIds.Keys)
            if (key.StartsWith(id + "_")) count++;
        return count;
    }

    ItemData FindItemById(string id)
    {
        if (ShopSystem.Instance == null) return null;
        foreach (ItemData item in ShopSystem.Instance.allAvailableItems)
            if (item != null && item.id == id) return item;
        return null;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  GIZMOS
    // ─────────────────────────────────────────────────────────────────────────

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (slotPositions == null) return;
        Gizmos.color = new Color(0f, 1f, 1f, 0.8f);
        for (int i = 0; i < slotPositions.Length; i++)
        {
            if (slotPositions[i] == null) continue;
            Gizmos.DrawWireCube(slotPositions[i].position, new Vector3(0.3f, 0.1f, 0.3f));
            UnityEditor.Handles.Label(
                slotPositions[i].position + Vector3.up * 0.15f,
                $"Slot {i}");
        }
    }
#endif
}
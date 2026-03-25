using UnityEngine;
using System.Collections.Generic;

// =============================================
//  DATA: One pending delivery order
// =============================================
[System.Serializable]
public class DeliveryOrder
{
    public ItemData item;          // What was ordered
    public int quantity;           // How many
    public int daysRemaining;      // Countdown — when 0, it arrives!
    public GameObject boxPrefab;   // The specific box prefab for this item's size

    public DeliveryOrder(ItemData item, int quantity, int deliveryDays)
    {
        this.item = item;
        this.quantity = quantity;
        this.daysRemaining = deliveryDays;
        this.boxPrefab = item.deliveryBoxPrefab; // Grab from the item (can be null)
    }
}

// =============================================
//  MANAGER: Tracks all pending orders, spawns
//  fragile delivery boxes when they arrive.
// =============================================
public class DeliveryManager : MonoBehaviour
{
    public static DeliveryManager Instance;

    [Header("Delivery Spawn Settings")]
    [Tooltip("Default fallback box. Used when an ItemData has no deliveryBoxPrefab assigned.")]
    public GameObject fragileBoxPrefab;

    [Tooltip("Drag empty GameObjects where delivery boxes should appear (e.g. near your shop door or a delivery zone).")]
    public Transform[] deliverySpawnPoints;

    [Header("Layer Fix")]
    [Tooltip("Auto-detected at startup from existing PickupBox objects. If no boxes exist yet, set this manually " +
             "to the same layer your JobBox prefab uses (check any existing box in your scene).")]
    public int interactableLayer = -1;

    [Header("Active Orders (Debug View)")]
    public List<DeliveryOrder> pendingOrders = new List<DeliveryOrder>();

    // ---- Internal state ----
    private int nextSpawnIndex = 0; // Cycles through spawn points
    private int[] boxCountPerPoint; // tracks how many boxes are at each spawn point
    public int maxBoxesPerSpawnPoint = 3; // set this in Inspector — how many boxes fit per zone

    // =============================================
    //  SETUP
    // =============================================

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {
        // AUTO-DETECT: Find the layer from any existing PickupBox in the scene
        // so spawned delivery boxes match the same layer PlayerInteraction raycasts against.
        if (interactableLayer == -1)
        {
            GameObject existingBox = GameObject.FindWithTag("PickupBox");
            if (existingBox != null)
            {
                interactableLayer = existingBox.layer;
                Debug.Log($"[DeliveryManager] Auto-detected interactable layer: {LayerMask.LayerToName(interactableLayer)} ({interactableLayer})");
            }
            else
            {
                Debug.LogWarning("[DeliveryManager] Could not auto-detect layer — no PickupBox objects found in scene. " +
                                 "Set 'Interactable Layer' manually in the Inspector to match your PlayerInteraction's interactLayer.");
            }
        }
    }

    void OnEnable()
    {
        // Listen for the new-day event from your existing DayTransitionManager
        DayTransitionManager.OnNewDayStarted += OnNewDay;
    }

    void OnDisable()
    {
        DayTransitionManager.OnNewDayStarted -= OnNewDay;
    }

    // =============================================
    //  PUBLIC API — Called by ShopManager.Checkout()
    // =============================================

    /// <summary>
    /// Registers a new delivery order. Call this once per cart line item at checkout.
    /// </summary>
    public void PlaceOrder(ItemData item, int quantity, int deliveryDays)
    {
        DeliveryOrder newOrder = new DeliveryOrder(item, quantity, deliveryDays);
        pendingOrders.Add(newOrder);
        Debug.Log($"[DeliveryManager] Order placed: {quantity}x {item.itemName} — arrives in {deliveryDays} day(s).");
    }

    // =============================================
    //  DAY TICK — Counts down and spawns arrivals
    // =============================================

    void OnNewDay(int newDayNumber)
    {
        List<DeliveryOrder> arrivedOrders = new List<DeliveryOrder>();

        // 1. Count down every pending order
        foreach (DeliveryOrder order in pendingOrders)
        {
            order.daysRemaining--;

            if (order.daysRemaining <= 0)
            {
                arrivedOrders.Add(order);
            }
        }

        // 2. Spawn a fragile box for each arrived order
        foreach (DeliveryOrder arrived in arrivedOrders)
        {
            SpawnDeliveryBox(arrived);
            pendingOrders.Remove(arrived);
            Debug.Log($"[DeliveryManager] DELIVERED: {arrived.quantity}x {arrived.item.itemName}!");
        }
    }

    // =============================================
    //  SPAWNING
    // =============================================

    void SpawnDeliveryBox(DeliveryOrder order)
    {
        GameObject prefabToUse = order.boxPrefab != null ? order.boxPrefab : fragileBoxPrefab;

        if (order.boxPrefab != null)
            Debug.Log($"[DeliveryManager] Using ITEM-SPECIFIC box '{order.boxPrefab.name}' for '{order.item.itemName}'");
        else
            Debug.LogWarning($"[DeliveryManager] '{order.item.itemName}' has NO deliveryBoxPrefab assigned! Using default.");

        if (prefabToUse == null)
        {
            Debug.LogError($"[DeliveryManager] No box prefab for '{order.item.itemName}'!");
            return;
        }

        if (deliverySpawnPoints == null || deliverySpawnPoints.Length == 0)
        {
            Debug.LogError("[DeliveryManager] No delivery spawn points assigned!");
            return;
        }

        // Init the per-point counter array if needed
        if (boxCountPerPoint == null || boxCountPerPoint.Length != deliverySpawnPoints.Length)
            boxCountPerPoint = new int[deliverySpawnPoints.Length];

        // Spawn each box individually, picking the best available spawn point
        for (int i = 0; i < order.quantity; i++)
        {
            // Find the spawn point with the fewest boxes (spread the load)
            int chosenIndex = 0;
            for (int p = 0; p < deliverySpawnPoints.Length; p++)
            {
                if (boxCountPerPoint[p] < maxBoxesPerSpawnPoint)
                {
                    chosenIndex = p;
                    break; // use the FIRST point that still has room
                }

            }

            Transform spawnPoint = deliverySpawnPoints[chosenIndex];

            // Grid offset within that spawn point: 3 columns, rows going back in Z
            int localIndex = boxCountPerPoint[chosenIndex];
            int col = localIndex % 3;
            int row = localIndex / 3;
            Vector3 offset = new Vector3(0.55f * col, 0f, 0.55f * row);
            

            GameObject newBox = Instantiate(prefabToUse, spawnPoint.position + offset, spawnPoint.rotation);
            newBox.tag = "PickupBox";

            if (interactableLayer != -1)
                SetLayerRecursive(newBox, interactableLayer);

            DeliveryBox boxScript = newBox.GetComponent<DeliveryBox>();
            if (boxScript != null)
                boxScript.Setup(order.item);
            else
                Debug.LogWarning("[DeliveryManager] Box prefab missing DeliveryBox component!");

            boxCountPerPoint[chosenIndex]++;
            Debug.Log($"[DeliveryManager] Spawned box {i+1}/{order.quantity} at SpawnPoint[{chosenIndex}] slot {localIndex}");
        }
    }
    /// <summary>
    /// Recursively sets the layer on a GameObject and all its children.
    /// </summary>
    void SetLayerRecursive(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursive(child.gameObject, layer);
        }
    }

    // =============================================
    //  UTILITY — For UI or notification systems
    // =============================================

    /// <summary>
    /// Returns how many orders are still in transit.
    /// </summary>
    public int GetPendingOrderCount()
    {
        return pendingOrders.Count;
    }

    /// <summary>
    /// Returns a copy of all pending orders (safe to iterate).
    /// </summary>
    public List<DeliveryOrder> GetAllPendingOrders()
    {
        return new List<DeliveryOrder>(pendingOrders);
    }
}
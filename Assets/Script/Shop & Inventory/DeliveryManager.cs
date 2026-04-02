using UnityEngine;
using System.Collections.Generic;

// =============================================
//  DATA: One pending delivery order
// =============================================
[System.Serializable]
public class DeliveryOrder
{
    public ItemData item;
    public int quantity;
    public int daysRemaining;
    public GameObject boxPrefab;

    public DeliveryOrder(ItemData item, int quantity, int deliveryDays)
    {
        this.item = item;
        this.quantity = quantity;
        this.daysRemaining = deliveryDays;
        this.boxPrefab = item.deliveryBoxPrefab;
    }
}

// =============================================
//  MANAGER
// =============================================
public class DeliveryManager : MonoBehaviour
{
    public static DeliveryManager Instance;

    [Header("Delivery Spawn Settings")]
    public GameObject fragileBoxPrefab;
    public Transform[] deliverySpawnPoints;

    [Header("Layer Fix")]
    public int interactableLayer = -1;

    [Header("Active Orders (Debug View)")]
    public List<DeliveryOrder> pendingOrders = new List<DeliveryOrder>();

    // ── Static holders: survive scene loads for room changes ──
    public static List<DeliveryOrder> SavedOrders = null;
    public static List<ItemData> SavedSpawnedBoxItems = null;

    private int[] boxCountPerPoint;
    public int maxBoxesPerSpawnPoint = 3;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {
        // Auto-detect layer
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
                Debug.LogWarning("[DeliveryManager] Could not auto-detect layer — no PickupBox found. Set manually.");
            }
        }

        // Restore pending orders from room change
        if (SavedOrders != null)
        {
            pendingOrders = new List<DeliveryOrder>(SavedOrders);
            SavedOrders = null;
            Debug.Log("[DeliveryManager] Restored " + pendingOrders.Count + " pending orders from room change.");
        }

        // Re-spawn delivery boxes that were sitting in the scene before room change
        if (SavedSpawnedBoxItems != null && SavedSpawnedBoxItems.Count > 0)
        {
            foreach (ItemData item in SavedSpawnedBoxItems)
            {
                SpawnSingleBox(item);
            }
            Debug.Log("[DeliveryManager] Re-spawned " + SavedSpawnedBoxItems.Count + " delivery boxes from room change.");
            SavedSpawnedBoxItems = null;
        }
    }

    void OnEnable() { DayTransitionManager.OnNewDayStarted += OnNewDay; }
    void OnDisable() { DayTransitionManager.OnNewDayStarted -= OnNewDay; }

    // =============================================
    //  PUBLIC API
    // =============================================

    public void PlaceOrder(ItemData item, int quantity, int deliveryDays)
    {
        DeliveryOrder newOrder = new DeliveryOrder(item, quantity, deliveryDays);
        pendingOrders.Add(newOrder);
        Debug.Log($"[DeliveryManager] Order placed: {quantity}x {item.itemName} — arrives in {deliveryDays} day(s).");
    }

    // =============================================
    //  DAY TICK
    // =============================================

    void OnNewDay(int newDayNumber)
    {
        List<DeliveryOrder> arrivedOrders = new List<DeliveryOrder>();

        foreach (DeliveryOrder order in pendingOrders)
        {
            order.daysRemaining--;
            if (order.daysRemaining <= 0)
                arrivedOrders.Add(order);
        }

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

    /// <summary>
    /// Spawns a single box for one ItemData (used for room change re-spawn).
    /// </summary>
    void SpawnSingleBox(ItemData item)
    {
        GameObject prefabToUse = (item.deliveryBoxPrefab != null) ? item.deliveryBoxPrefab : fragileBoxPrefab;

        if (prefabToUse == null)
        {
            Debug.LogError($"[DeliveryManager] No box prefab for re-spawn: '{item.itemName}'!");
            return;
        }

        if (deliverySpawnPoints == null || deliverySpawnPoints.Length == 0)
        {
            Debug.LogError("[DeliveryManager] No delivery spawn points assigned!");
            return;
        }

        if (boxCountPerPoint == null || boxCountPerPoint.Length != deliverySpawnPoints.Length)
            boxCountPerPoint = new int[deliverySpawnPoints.Length];

        int chosenIndex = 0;
        for (int p = 0; p < deliverySpawnPoints.Length; p++)
        {
            if (boxCountPerPoint[p] < maxBoxesPerSpawnPoint)
            {
                chosenIndex = p;
                break;
            }
        }

        Transform spawnPoint = deliverySpawnPoints[chosenIndex];
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
            boxScript.Setup(item);

        boxCountPerPoint[chosenIndex]++;
        Debug.Log($"[DeliveryManager] Re-spawned box for '{item.itemName}' at SpawnPoint[{chosenIndex}]");
    }

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

        if (boxCountPerPoint == null || boxCountPerPoint.Length != deliverySpawnPoints.Length)
            boxCountPerPoint = new int[deliverySpawnPoints.Length];

        for (int i = 0; i < order.quantity; i++)
        {
            int chosenIndex = 0;
            for (int p = 0; p < deliverySpawnPoints.Length; p++)
            {
                if (boxCountPerPoint[p] < maxBoxesPerSpawnPoint)
                {
                    chosenIndex = p;
                    break;
                }
            }

            Transform spawnPoint = deliverySpawnPoints[chosenIndex];
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
            Debug.Log($"[DeliveryManager] Spawned box {i + 1}/{order.quantity} at SpawnPoint[{chosenIndex}] slot {localIndex}");
        }
    }

    void SetLayerRecursive(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
            SetLayerRecursive(child.gameObject, layer);
    }

    // =============================================
    //  UTILITY
    // =============================================

    public int GetPendingOrderCount() { return pendingOrders.Count; }

    public List<DeliveryOrder> GetAllPendingOrders()
    {
        return new List<DeliveryOrder>(pendingOrders);
    }
}
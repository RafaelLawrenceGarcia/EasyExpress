using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "EasyExpress/Shop Item")]
public class ItemData : ScriptableObject
{
    [Header("Item Info")]
    public string id; // Unique ID (e.g., "gpu_1080")
    public string itemName;
    public Sprite icon;

    // We will use this string for your dynamic Shop tabs (e.g., "GPU", "RAM")
    public string category;

    [TextArea] public string description;

    [Header("Technical Specs")]
    [TextArea] public string specs; // <-- New field for PC part specifications!

    [Header("Shop Settings")]
    public float price;

    // CHANGED: Renamed this to 'itemType' so it doesn't conflict with the string above!
    public ItemCategory itemType;

    [Header("Gameplay")]
    public GameObject prefabToPlace; // The 3D model spawned when placed
    public bool isTool = false;
    // If true, it's an upgrade, not a physical object

    [Header("Delivery")]
    [Tooltip("The specific-sized fragile box prefab for this item. Leave empty to use the default.")]
    public GameObject deliveryBoxPrefab;
}

public enum ItemCategory
{
    PCPart,
    PCCase,
    Decoration,
    Tool
}
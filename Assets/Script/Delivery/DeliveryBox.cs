using UnityEngine;

/// <summary>
/// DeliveryBox — Attach this to your "Fragile Box" prefab.
/// 
/// This is the physical delivery box that spawns in the shop when an order arrives.
/// The player can pick it up (tag: "PickupBox") and place it on a Storage/Workstation slot.
/// 
/// When placed, it adds the item to the player's ShopSystem inventory and optionally
/// spawns the 3D prefab (like furniture or a PC part) before destroying itself.
/// </summary>
public class DeliveryBox : MonoBehaviour
{
    [Header("Delivery Contents (Set by DeliveryManager)")]
    [Tooltip("The item this box contains. Set automatically when spawned.")]
    public ItemData containedItem;

    [Header("Settings")]
    [Tooltip("If true, the 3D prefab from ItemData will be spawned when placed on a slot. " +
             "If false, the item is just added to inventory silently.")]
    public bool spawnPrefabOnPlace = true;

    [Tooltip("If true, the item is added to ShopSystem's owned inventory on unpack.")]
    public bool addToInventoryOnUnpack = true;

    // =============================================
    //  SETUP — Called by DeliveryManager when spawning
    // =============================================

    public void Setup(ItemData item)
    {
        containedItem = item;
    }

    // =============================================
    //  UNPACK — Called when placed on a Workstation
    //  or Storage slot (via PlacementManager)
    // =============================================

    public GameObject Unpack(Transform slotTransform)
    {
        if (containedItem == null)
        {
            Debug.LogWarning("[DeliveryBox] This box is empty! No item data was assigned.");
            Destroy(gameObject);
            return null;
        }

        // 1. Add the item to the player's inventory
        if (addToInventoryOnUnpack && ShopSystem.Instance != null)
        {
            ShopSystem.Instance.AddItemDirectly(containedItem);
            Debug.Log($"[DeliveryBox] '{containedItem.itemName}' added to inventory!");
        }

        // 2. Optionally spawn the physical 3D object on the slot
        GameObject spawnedObject = null;

        if (spawnPrefabOnPlace && containedItem.prefabToPlace != null)
        {
            spawnedObject = Instantiate(
                containedItem.prefabToPlace,
                slotTransform.position,
                slotTransform.rotation
            );

            if (containedItem.itemType == ItemCategory.PCPart)
                spawnedObject.tag = "PickupPC";

            Debug.Log($"[DeliveryBox] Spawned '{containedItem.itemName}' on the slot.");
        }

        // 3. Destroy the cardboard box
        Destroy(gameObject);

        // 4. Return what should sit on the slot
        return spawnedObject;
    }

    // =============================================
    //  DIRECT UNPACK — Called when pressing E
    // =============================================
    public void InteractUnpack()
    {
        if (containedItem == null)
        {
            Debug.LogWarning("[DeliveryBox] This box is empty!");
            Destroy(gameObject);
            return;
        }

        // ── BUNDLE DELIVERY: deliver multiple individual parts ──
        if (containedItem.bundleItemData != null && containedItem.bundleQuantity > 1)
        {
            for (int i = 0; i < containedItem.bundleQuantity; i++)
            {
                if (ShopSystem.Instance != null)
                    ShopSystem.Instance.AddItemDirectly(containedItem.bundleItemData);
            }
            Debug.Log($"[DeliveryBox] Bundle unpacked: {containedItem.bundleQuantity}x '{containedItem.bundleItemData.itemName}'");

            StorageRoomShelf shelf = FindObjectOfType<StorageRoomShelf>();
            if (shelf != null)
            {
                for (int i = 0; i < containedItem.bundleQuantity; i++)
                {
                    Transform freeSlot = shelf.GetFreeSlot();
                    if (freeSlot == null) break;

                    if (containedItem.bundleItemData.retailBoxPrefab != null)
                    {
                        GameObject retailBox = Instantiate(
                            containedItem.bundleItemData.retailBoxPrefab,
                            freeSlot.position, freeSlot.rotation, freeSlot);
                        retailBox.name = $"RetailBox_{containedItem.bundleItemData.itemName}_{i}";

                        Rigidbody rb = retailBox.GetComponent<Rigidbody>();
                        if (rb != null) Destroy(rb);
                        foreach (Collider c in retailBox.GetComponentsInChildren<Collider>(true))
                            c.enabled = false;

                        shelf.RegisterBoxOnSlot(freeSlot, retailBox, containedItem.bundleItemData);
                    }
                }
            }

            Destroy(gameObject);
            return;
        }

        // ── NORMAL (non-bundle) DELIVERY ──
        if (ShopSystem.Instance != null)
        {
            ShopSystem.Instance.AddItemDirectly(containedItem);
            Debug.Log($"[DeliveryBox] '{containedItem.itemName}' added to inventory!");
        }

        StorageRoomShelf shelf2 = FindObjectOfType<StorageRoomShelf>();
        if (shelf2 != null)
        {
            Transform freeSlot = shelf2.GetFreeSlot();
            if (freeSlot != null)
            {
                if (containedItem.retailBoxPrefab != null)
                {
                    GameObject retailBox = Instantiate(
                        containedItem.retailBoxPrefab,
                        freeSlot.position, freeSlot.rotation, freeSlot);
                    retailBox.name = $"RetailBox_{containedItem.itemName}";

                    Rigidbody rb = retailBox.GetComponent<Rigidbody>();
                    if (rb != null) Destroy(rb);
                    foreach (Collider c in retailBox.GetComponentsInChildren<Collider>(true))
                        c.enabled = false;

                    shelf2.RegisterBoxOnSlot(freeSlot, retailBox, containedItem);
                }
            }
        }

        Destroy(gameObject);
    }
}
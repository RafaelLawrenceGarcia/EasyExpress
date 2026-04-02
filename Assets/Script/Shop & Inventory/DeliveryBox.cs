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

        // 1. Add item to ShopSystem inventory
        if (ShopSystem.Instance != null)
        {
            ShopSystem.Instance.AddItemDirectly(containedItem);
            Debug.Log($"[DeliveryBox] '{containedItem.itemName}' added to inventory!");
        }

        // 2. Find a free shelf slot
        StorageRoomShelf shelf = FindObjectOfType<StorageRoomShelf>();
        if (shelf != null)
        {
            Transform freeSlot = shelf.GetFreeSlot();
            if (freeSlot != null)
            {
                // 3. Spawn the RETAIL BOX (AMD/Nvidia/Intel box)
                //    on the shelf slot instead of the plain cardboard box
                if (containedItem.retailBoxPrefab != null)
                {
                    GameObject retailBox = Instantiate(
                        containedItem.retailBoxPrefab,
                        freeSlot.position,
                        freeSlot.rotation,
                        freeSlot
                    );
                    retailBox.name = $"RetailBox_{containedItem.itemName}";

                    // Disable physics on retail box
                    Rigidbody retailRb = retailBox.GetComponent<Rigidbody>();
                    if (retailRb != null) Destroy(retailRb);

                    // Disable colliders — visual only
                    foreach (Collider c in retailBox.GetComponentsInChildren<Collider>(true))
                        c.enabled = false;

                    // Register on shelf
                    shelf.RegisterBoxOnSlot(freeSlot, retailBox, containedItem);

                    Debug.Log($"[DeliveryBox] Retail box spawned on shelf for " +
                            $"'{containedItem.itemName}'.");
                }
                else
                {
                    Debug.LogWarning($"[DeliveryBox] '{containedItem.itemName}' has no " +
                                    "retailBoxPrefab assigned on its ItemData!");
                }
            }
            else
            {
                Debug.LogWarning("[DeliveryBox] Shelf is full!");
            }
        }

        // 4. Destroy the plain cardboard delivery box
        Destroy(gameObject);
    }
}
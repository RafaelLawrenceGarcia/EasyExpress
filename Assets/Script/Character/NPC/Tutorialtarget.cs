using UnityEngine;

/// <summary>
/// TutorialTarget — Attach this to prefabs and scene objects so the
/// Tutorial Arrow can find them automatically at runtime.
///
/// ─────────────────────────────────────────────────────────────────
///  SETUP GUIDE — add one component per object, set the Type:
/// ─────────────────────────────────────────────────────────────────
///
///  Customer NPC prefab          →  Type = Customer
///  Customer's PC Box prefab     →  Type = Box
///  Inspectable PC prefab        →  Type = PC
///  Cashier counter zone object  →  Type = Cashier
///  Cashier / main desk PC       →  Type = CashierPC
///  Shop / order PC              →  Type = ShopPC
///  Storage Shelf object         →  Type = StorageShelf
///  Delivery box prefab          →  Type = DeliveryBox
///  Email workstation monitor    →  Type = Email
///
/// ─────────────────────────────────────────────────────────────────
///  TIP: If a PC or Box is spawned at runtime, add this component
///  to the prefab — TutorialManager will find it automatically.
/// ─────────────────────────────────────────────────────────────────
/// </summary>
public class TutorialTarget : MonoBehaviour
{
    public enum TargetType
    {
        // ── Original types ──────────────────────
        Customer,       // Shop customer NPC
        Box,            // Customer's PC box (PickupBox tag)
        PC,             // Inspectable PC on the workstation

        // ── New types ───────────────────────────
        Cashier,        // The cashier counter zone where the customer walks to
        CashierPC,      // The cashier / main desk computer (accept/reject jobs)
        ShopPC,         // The shop order PC (buy components)
        StorageShelf,   // The storage shelf where ordered parts are retrieved
        DeliveryBox,    // Delivery boxes that arrive at the start of a new day
        Email           // The email workstation monitor
    }

    [Tooltip("What this object represents in the tutorial flow.")]
    public TargetType type;
}
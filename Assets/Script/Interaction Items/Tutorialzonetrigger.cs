using UnityEngine;

/// <summary>
/// TutorialZoneTrigger — Reusable proximity trigger for any tutorial step.
/// Attach to any GameObject with a trigger collider.
/// When the player walks in, it fires the matching Complete method on TutorialManager.
///
/// ─────────────────────────────────────────────────────────────────
///  SETUP (do this for EACH spot you want to detect):
///   1. Select the GameObject (Cashier Counter, Cashier PC, Door, etc.)
///   2. Add a Box Collider  →  tick "Is Trigger"
///   3. Add this script
///   4. In the Inspector, set "Zone Type" to match the spot
///   5. Make sure your Player has the tag "Player"
/// ─────────────────────────────────────────────────────────────────
/// </summary>
public class TutorialZoneTrigger : MonoBehaviour
{
    public enum ZoneType
    {
        CashierCounter,   // Step 3 Task 0 — "Head to the cashier counter"
        CashierPC,        // Step 5 Task 0 — "Walk to the cashier PC"
        Workstation,      // Step 9 Task 0 — "Walk to the workstation desk"
        StorageShelf,     // Step 22 Task 0 — "Go to the storage shelf"
        ShopPC,           // Step 17/24 Task 0 — "Go to the Shop PC"
        Door,             // Step 18 Task 0 — "Walk to the shop door"
        Email,            // Step 32 Task 0 — "Go to the workstation monitor"
    }

    [Tooltip("What spot this trigger represents.")]
    public ZoneType zoneType;

    private bool hasTriggered = false;

    void OnTriggerEnter(Collider other)
    {
        if (hasTriggered) return;
        if (!other.CompareTag("Player")) return;
        if (TutorialManager.Instance == null) return;
        if (!TutorialManager.Instance.IsTutorialActive()) return;

        int step = TutorialManager.Instance.GetCurrentStep();

        switch (zoneType)
        {
            case ZoneType.CashierCounter:
                if (step == 3)
                {
                    hasTriggered = true;
                    TutorialManager.Instance.CompleteGoToCashierTask();
                }
                break;

            case ZoneType.CashierPC:
                if (step == 5)
                {
                    hasTriggered = true;
                    TutorialManager.Instance.CompleteApproachCashierPCTask();
                }
                break;

            case ZoneType.Workstation:
                if (step == 9)
                {
                    hasTriggered = true;
                    TutorialManager.Instance.CompleteApproachWorkstationTask();
                }
                break;

            case ZoneType.StorageShelf:
                if (step == 22)
                {
                    hasTriggered = true;
                    TutorialManager.Instance.CompleteApproachStorageShelfTask();
                }
                break;

            case ZoneType.ShopPC:
                if (step == 17 || step == 24)
                {
                    hasTriggered = true;
                    TutorialManager.Instance.CompleteApproachShopPCTask();
                }
                break;

            case ZoneType.Door:
                if (step == 18)
                {
                    hasTriggered = true;
                    TutorialManager.Instance.CompleteApproachDoorTask();
                }
                break;

            case ZoneType.Email:
                if (step == 32)
                {
                    hasTriggered = true;
                    TutorialManager.Instance.CompleteApproachEmailTask();
                }
                break;
        }
    }

    /// <summary>
    /// Call this if the tutorial restarts so the trigger can fire again.
    /// </summary>
    public void ResetTrigger()
    {
        hasTriggered = false;
    }
}
using UnityEngine;

// ============================================================
//  CustomerDeskManager.cs
//  Easy Express – Thesis Project
// ============================================================
//  WHAT IT DOES:
//  When the customer at queue spot 0 reaches the counter,
//  their PC case spawns on your repair desk Transform.
//  The player can inspect it (already works — it's a normal
//  InspectableItem in the world), then Accept or Reject.
//
//  SETUP IN INSPECTOR:
//  1. Add this component to any persistent GameObject in the scene
//     (your shop manager object is fine).
//  2. deskSpawnPoint → drag the empty GameObject on top of your
//     repair desk where the PC should appear.
//  3. That's it. CustomerInside calls TrySpawnDeskPC() automatically
//     when the customer is at spot 0 and isAtSpot = true.
//
//  HOW ACCEPT/REJECT WORKS:
//  • Player talks to customer (existing dialogue system)
//  • Clicks Accept → CustomerInside.AcceptJob() → calls ClearDeskPC()
//  • Clicks Reject → CustomerInside.RejectJob() → calls ClearDeskPC()
// ============================================================

public class CustomerDeskManager : MonoBehaviour
{
    public static CustomerDeskManager Instance;

    [Header("Desk")]
    [Tooltip("The empty GameObject on top of your repair desk. " +
             "The customer's PC case will spawn here.")]
    public Transform deskSpawnPoint;

    [Tooltip("The ShopCustomerSpawner in your scene. " +
             "Used to check which customer is first in queue.")]
    public ShopCustomerSpawner spawner;

    // The PC currently sitting on the desk
    private GameObject _deskPC = null;

    // ─────────────────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (spawner == null)
            spawner = FindObjectOfType<ShopCustomerSpawner>();

        if (deskSpawnPoint == null)
            Debug.LogWarning("[DeskManager] deskSpawnPoint is not assigned! " +
                             "Drag your desk Transform into the Inspector.");
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  TRY SPAWN DESK PC
    //  Called by CustomerInside.Update() when isAtSpot becomes true.
    //  Only spawns if this customer is FIRST in the queue (slot 0).
    // ─────────────────────────────────────────────────────────────────────────

        public void TrySpawnDeskPC(CustomerInside customer)
    {
        if (deskSpawnPoint == null)
        {
            Debug.LogWarning("[DeskManager] deskSpawnPoint is not assigned in Inspector!");
            return;
        }

        if (!IsFirstInQueue(customer))
        {
            Debug.Log($"[DeskManager] {customer.npcName} is not first — no desk PC.");
            return;
        }

        if (customer.assignedJob.basePCCasePrefab == null)
        {
            Debug.Log($"[DeskManager] {customer.npcName}'s job has no basePCCasePrefab — " +
                      "skipping desk PC. Assign one to the EmailData asset.");
            return;
        }

        // Clear any previous PC on the desk
        ClearDeskPC();

        // Spawn the customer's PC on the desk
        Vector3 spawnPos     = deskSpawnPoint != null
            ? deskSpawnPoint.position
            : new Vector3(0f, 1f, 0f);
        Quaternion spawnRot  = deskSpawnPoint != null
            ? deskSpawnPoint.rotation
            : Quaternion.identity;

        _deskPC = Instantiate(
            customer.assignedJob.basePCCasePrefab,
            spawnPos,
            spawnRot);

        _deskPC.name = $"DeskPC_{customer.npcName}";

        // Tag it so the player can interact / inspect it
        // Use "PickupPC" so PlayerInteract shows the inspect prompt
        _deskPC.tag = "PickupPC";

        // Make sure the main InspectableItem is set correctly
        InspectableItem rootItem = _deskPC.GetComponent<InspectableItem>();
        if (rootItem != null)
        {
            rootItem.isMainObject = true;
            rootItem.itemName     = $"{customer.npcName}'s PC";
            rootItem.itemDescription = customer.jobRequest;
        }

        // Build the PC internals from the job's starting parts
        PCCaseBuilder builder = _deskPC.GetComponent<PCCaseBuilder>();
        if (builder != null && customer.assignedJob.startingParts != null)
            builder.BuildFromData(customer.assignedJob.startingParts);

        Debug.Log($"[DeskManager] Spawned {customer.npcName}'s PC on the desk.");
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  CLEAR DESK PC
    //  Called by CustomerInside.AcceptJob() and RejectJob()
    // ─────────────────────────────────────────────────────────────────────────

    public void ClearDeskPC()
    {
        if (_deskPC != null)
        {
            Destroy(_deskPC);
            _deskPC = null;
            Debug.Log("[DeskManager] Desk PC cleared.");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  HELPERS
    // ─────────────────────────────────────────────────────────────────────────

    bool IsFirstInQueue(CustomerInside customer)
    {
        if (spawner == null)
            spawner = FindObjectOfType<ShopCustomerSpawner>();

        if (spawner == null) return false;

        var customers = spawner.GetActiveCustomers();
        if (customers == null || customers.Count == 0) return false;

        // Customer is first in queue if they are at queue spot index 0
        // We check by position distance to queueSpots[0]
        if (spawner.queueSpots == null || spawner.queueSpots.Length == 0) return false;

        Transform firstSpot = spawner.queueSpots[0];
        if (firstSpot == null) return false;

        // This customer is first if they are the closest one to spot 0
        float myDist = Vector3.Distance(customer.transform.position, firstSpot.position);

        foreach (CustomerInside other in customers)
        {
            if (other == null || other == customer) continue;
            float otherDist = Vector3.Distance(other.transform.position, firstSpot.position);
            if (otherDist < myDist) return false; // someone else is closer to spot 0
        }

        return true;
    }
}
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ShopCustomerSpawner : MonoBehaviour
{
    public static ShopCustomerSpawner Instance;

    [Header("Settings")]
    public GameObject customerPrefab;
    public Transform spawnPoint;

    [Header("Queue System")]
    public Transform[] queueSpots;

    private bool isReady = false; // Always starts false — TutorialManager releases it
    private List<CustomerInside> activeCustomers = new List<CustomerInside>();

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // Always start paused. Wait one frame so TutorialManager.Instance is ready,
        // then check: if tutorial is already done → release immediately.
        // If tutorial is active → stay paused until TutorialManager calls AllowSpawn().
        StartCoroutine(WaitForTutorialDecision());
    }

    IEnumerator WaitForTutorialDecision()
    {
        // Wait one frame so all Awake/Start functions have run
        yield return null;

        // If no tutorial manager, or tutorial is already finished → spawn now
        if (TutorialManager.Instance == null || !TutorialManager.Instance.IsTutorialActive())
        {
            AllowSpawn();
        }
        // Otherwise stay paused — TutorialManager calls AllowSpawn() after WASD
    }

    // ── Called by TutorialManager after WASD is done ──
    // Also called immediately above if tutorial is already finished
    public void AllowSpawn()
    {
        if (isReady) return; // Safety: don't double-spawn
        isReady = true;
        Debug.Log("[ShopCustomerSpawner] AllowSpawn called. RetainedData=" + CustomerRetainer.hasRetainedData
        + " QueueSpots=" + (queueSpots != null ? queueSpots.Length : 0));
        Debug.Log("[ShopCustomerSpawner] Released! Spawning customers.");

        // ── CHECK FOR RETAINED CUSTOMERS (room change) ──
        // Only restore if this scene has queue spots (store scene, not outside)
        if (CustomerRetainer.hasRetainedData && queueSpots != null && queueSpots.Length > 0)
        {
            RestoreRetainedCustomers();
            return; // Don't spawn new ones — we restored the old ones
        }

        int customersWaiting = NPCWalker.incomingCustomers;

        // During the tutorial no street NPCs were allowed to queue up,
        // so force-spawn 1 customer so the tutorial can continue.
        // Only do this if the tutorial is actually active.
        if (customersWaiting == 0
            && TutorialManager.Instance != null
            && TutorialManager.Instance.IsTutorialActive())
        {
            customersWaiting = 1;
        }

        for (int i = 0; i < customersWaiting; i++)
        {
            if (activeCustomers.Count < queueSpots.Length)
                SpawnCustomer();
        }
    }

    /// <summary>
    /// Restores customers saved by CustomerRetainer during a scene transition.
    /// </summary>
    void RestoreRetainedCustomers()
    {
        Debug.Log("[ShopCustomerSpawner] Restoring " + CustomerRetainer.savedCustomers.Count + " retained customers.");

        foreach (var rc in CustomerRetainer.savedCustomers)
        {
            if (activeCustomers.Count >= queueSpots.Length) break;

            if (customerPrefab == null || spawnPoint == null) continue;

            int slotIndex = activeCustomers.Count;
            Transform assignedSpot = queueSpots[slotIndex];

            GameObject newCustomerObj = Instantiate(customerPrefab, spawnPoint.position, spawnPoint.rotation);
            CustomerInside customer = newCustomerObj.GetComponent<CustomerInside>();

            if (customer != null)
            {
                // Override the random name/job with the retained data
                customer.npcName = rc.npcName;
                customer.assignedJob = rc.assignedJob;
                customer.reward = rc.assignedJob != null ? (int)rc.assignedJob.reward : 0;
                customer.jobRequest = rc.assignedJob != null ? BuildRestoredDialogue(rc.assignedJob, rc.npcName) : "Can you help me?";
                newCustomerObj.name = "Customer_" + rc.npcName;

                // Skip browsing — they already browsed before the scene change
                customer.willBrowseFirst = false;

                customer.AssignQueueSpot(assignedSpot);
                customer.mySpawner = this;
                activeCustomers.Add(customer);
                customer.DisableCollisionUntilAtSpot();
                Debug.Log("[ShopCustomerSpawner] Restored customer: " + rc.npcName);
            }

            if (NPCWalker.incomingCustomers > 0)
                NPCWalker.incomingCustomers--;
        }

        // Clear retained data so the next scene entry doesn't re-restore
        CustomerRetainer.Clear();
    }

    /// <summary>
    /// Builds a simple dialogue line for a restored customer.
    /// </summary>
    string BuildRestoredDialogue(EmailData job, string name)
    {
        if (job.jobType == JobType.Build)
            return $"Hi! I'm still waiting for my PC build. Can you help?\n\nReward: {job.reward:N0}";
        else
            return $"Hey! I'm still here about my PC repair. Can you take a look?\n\nReward: {job.reward:N0}";
    }

    public void SpawnCustomer()
    {
        if (customerPrefab == null || spawnPoint == null)
        {
            Debug.LogError("MISSING PREFAB OR SPAWN POINT!");
            return;
        }

        if (activeCustomers.Count >= queueSpots.Length)
        {
            Debug.Log("Shop is full! Customer waits outside.");
            return;
        }

        int mySlotIndex = activeCustomers.Count;
        Transform assignedSpot = queueSpots[mySlotIndex];

        GameObject newCustomerObj = Instantiate(customerPrefab, spawnPoint.position, spawnPoint.rotation);
        CustomerInside newCustomerScript = newCustomerObj.GetComponent<CustomerInside>();

        if (newCustomerScript != null)
        {
            newCustomerScript.AssignQueueSpot(assignedSpot);
            newCustomerScript.mySpawner = this;
            activeCustomers.Add(newCustomerScript);
        }

        if (NPCWalker.incomingCustomers > 0)
            NPCWalker.incomingCustomers--;
    }

    public void CustomerLeft(CustomerInside customer)
    {
        if (activeCustomers.Contains(customer))
        {
            activeCustomers.Remove(customer);
            UpdateQueuePositions();
        }
    }

    /// <summary>
    /// Returns true if any customers are currently inside the shop.
    /// Used by DoorInteractionMenu to block ending the day.
    /// </summary>
    public bool HasCustomersInside()
    {
        // Clean up any destroyed references first
        activeCustomers.RemoveAll(c => c == null);
        return activeCustomers.Count > 0;
    }

    /// <summary>
    /// Returns how many customers are currently inside.
    /// </summary>
    public int GetCustomerCount()
    {
        activeCustomers.RemoveAll(c => c == null);
        return activeCustomers.Count;
    }

    /// <summary>
    /// Returns the active customers list (used by CustomerRetainer).
    /// </summary>
    public List<CustomerInside> GetActiveCustomers()
    {
        activeCustomers.RemoveAll(c => c == null);
        return activeCustomers;
    }

    void UpdateQueuePositions()
    {
        for (int i = 0; i < activeCustomers.Count; i++)
        {
            activeCustomers[i].AssignQueueSpot(queueSpots[i]);
        }
    }
}
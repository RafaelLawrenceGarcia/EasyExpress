using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ShopCustomerSpawner : MonoBehaviour
{
    public static ShopCustomerSpawner Instance;

    [Header("Settings")]
    public GameObject customerPrefab;
    public Transform  spawnPoint;

    [Header("Queue System")]
    public Transform[] queueSpots;

    [Header("Exit Point")]
    [Tooltip("Drag the empty GameObject at your shop exit door here. " +
             "Customers walk here before being destroyed. " +
             "THIS MUST BE SET or customers will vanish instantly.")]
    public Transform exitPoint;

    private bool isReady = false;
    private List<CustomerInside> activeCustomers = new List<CustomerInside>();

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        StartCoroutine(WaitForTutorialDecision());
    }

    IEnumerator WaitForTutorialDecision()
    {
        yield return null;

        if (TutorialManager.Instance == null || !TutorialManager.Instance.IsTutorialActive())
            AllowSpawn();
    }

    // ── Called by TutorialManager after WASD is done ──────────────────────
    public void AllowSpawn()
    {
        if (isReady) return;
        isReady = true;

        Debug.Log("[ShopCustomerSpawner] Released! RetainedData=" +
                  CustomerRetainer.hasRetainedData +
                  " QueueSpots=" + (queueSpots != null ? queueSpots.Length : 0));

        // Restore retained customers from a scene change
        if (CustomerRetainer.hasRetainedData && queueSpots != null && queueSpots.Length > 0)
        {
            RestoreRetainedCustomers();
            return;
        }

        int customersWaiting = NPCWalker.incomingCustomers;

        // Force at least 1 customer during tutorial so it can continue
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

    // =====================================================================
    //  SPAWN CUSTOMER
    //  KEY FIX: now calls Initialize(queueSpot, exitPoint) instead of
    //  AssignQueueSpot() — this ensures exitPos is always set so customers
    //  physically walk to the door before being destroyed.
    // =====================================================================
    public void SpawnCustomer()
    {
        if (customerPrefab == null || spawnPoint == null)
        {
            Debug.LogError("[ShopCustomerSpawner] Missing prefab or spawn point!");
            return;
        }

        if (activeCustomers.Count >= queueSpots.Length)
        {
            Debug.Log("[ShopCustomerSpawner] Shop is full! Customer waits outside.");
            return;
        }

        if (exitPoint == null)
        {
            Debug.LogWarning("[ShopCustomerSpawner] exitPoint is not assigned! " +
                             "Customers will vanish instantly when they leave. " +
                             "Please assign the exit door Transform in the Inspector.");
        }

        int mySlotIndex = activeCustomers.Count;
        Transform assignedSpot = queueSpots[mySlotIndex];

        GameObject newCustomerObj = Instantiate(
            customerPrefab, spawnPoint.position, spawnPoint.rotation);

        CustomerInside customer = newCustomerObj.GetComponent<CustomerInside>();

        if (customer != null)
        {
            customer.mySpawner = this;
            activeCustomers.Add(customer);
            customer.DisableCollisionUntilAtSpot();

            // ── THE FIX ──────────────────────────────────────────────────────
            // Call Initialize instead of AssignQueueSpot.
            // This sets exitPos AND starts the browse routine properly.
            customer.Initialize(assignedSpot, exitPoint);
        }

        if (NPCWalker.incomingCustomers > 0)
            NPCWalker.incomingCustomers--;
    }

    // =====================================================================
    //  CUSTOMER LEFT — remove from list and shuffle queue forward
    // =====================================================================
    public void CustomerLeft(CustomerInside customer)
    {
        if (activeCustomers.Contains(customer))
        {
            activeCustomers.Remove(customer);
            UpdateQueuePositions();
        }
    }

    void UpdateQueuePositions()
    {
        for (int i = 0; i < activeCustomers.Count; i++)
        {
            // AssignQueueSpot just repositions — Initialize was already called
            activeCustomers[i].AssignQueueSpot(queueSpots[i]);
        }
    }

    // =====================================================================
    //  RESTORE RETAINED CUSTOMERS (after scene change)
    // =====================================================================
    void RestoreRetainedCustomers()
    {
        Debug.Log("[ShopCustomerSpawner] Restoring " +
                  CustomerRetainer.savedCustomers.Count + " retained customers.");

        foreach (var rc in CustomerRetainer.savedCustomers)
        {
            if (activeCustomers.Count >= queueSpots.Length) break;
            if (customerPrefab == null || spawnPoint == null) continue;

            int slotIndex = activeCustomers.Count;
            Transform assignedSpot = queueSpots[slotIndex];

            GameObject newCustomerObj = Instantiate(
                customerPrefab, spawnPoint.position, spawnPoint.rotation);

            CustomerInside customer = newCustomerObj.GetComponent<CustomerInside>();

            if (customer != null)
            {
                customer.npcName     = rc.npcName;
                customer.assignedJob = rc.assignedJob;
                customer.reward      = rc.assignedJob != null ? (int)rc.assignedJob.reward : 0;
                customer.jobRequest  = rc.assignedJob != null
                    ? BuildRestoredDialogue(rc.assignedJob, rc.npcName)
                    : "Can you help me?";
                newCustomerObj.name  = "Customer_" + rc.npcName;

                customer.mySpawner = this;
                activeCustomers.Add(customer);
                customer.DisableCollisionUntilAtSpot();

                // Skip browsing for restored customers — they already browsed
                customer.Initialize(assignedSpot, exitPoint, skipBrowse: true);

                Debug.Log("[ShopCustomerSpawner] Restored: " + rc.npcName);
            }

            if (NPCWalker.incomingCustomers > 0)
                NPCWalker.incomingCustomers--;
        }

        CustomerRetainer.Clear();
    }

    string BuildRestoredDialogue(EmailData job, string name)
    {
        if (job.jobType == JobType.Build)
            return $"Hi! I'm still waiting for my PC build.\n\nReward: ₱{job.reward:N0}";
        else
            return $"Hey! I'm still here about my PC repair.\n\nReward: ₱{job.reward:N0}";
    }

    // =====================================================================
    //  HELPERS
    // =====================================================================
    public bool HasCustomersInside()
    {
        activeCustomers.RemoveAll(c => c == null);
        return activeCustomers.Count > 0;
    }

    public int GetCustomerCount()
    {
        activeCustomers.RemoveAll(c => c == null);
        return activeCustomers.Count;
    }

    public List<CustomerInside> GetActiveCustomers()
    {
        activeCustomers.RemoveAll(c => c == null);
        return activeCustomers;
    }
}
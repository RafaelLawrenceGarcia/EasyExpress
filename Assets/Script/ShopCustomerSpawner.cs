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

        Debug.Log("[ShopCustomerSpawner] Released! Spawning customers.");

        int customersWaiting = NPCWalker.incomingCustomers;

        // During the tutorial no street NPCs were allowed to queue up,
        // so force-spawn 1 customer so the tutorial can continue.
        if (customersWaiting == 0)
            customersWaiting = 1;

        for (int i = 0; i < customersWaiting; i++)
        {
            if (activeCustomers.Count < queueSpots.Length)
                SpawnCustomer();
        }
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

    void UpdateQueuePositions()
    {
        for (int i = 0; i < activeCustomers.Count; i++)
        {
            activeCustomers[i].AssignQueueSpot(queueSpots[i]);
        }
    }
}
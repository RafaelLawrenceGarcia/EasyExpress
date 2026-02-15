using UnityEngine;
using System.Collections.Generic;

public class ShopCustomerSpawner : MonoBehaviour
{
    [Header("Settings")]
    public GameObject customerPrefab; 
    public Transform spawnPoint;      
    
    [Header("Queue System")]
    public Transform[] queueSpots; // Drag 3 empty objects here (Slot 1, Slot 2, Slot 3)

    private List<CustomerInside> activeCustomers = new List<CustomerInside>();

    void Start()
    {
        // Check for waiting customers when scene loads
        int customersWaiting = NPCWalker.incomingCustomers;
        
        // Spawn up to the number of available slots
        for (int i = 0; i < customersWaiting; i++)
        {
            if (activeCustomers.Count < queueSpots.Length)
            {
                SpawnCustomer();
            }
        }
    }

    public void SpawnCustomer() 
    {
        // 1. Safety Checks
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

        // 2. Find the first empty slot index
        int mySlotIndex = activeCustomers.Count;
        Transform assignedSpot = queueSpots[mySlotIndex];

        // 3. Create the Customer
        GameObject newCustomerObj = Instantiate(customerPrefab, spawnPoint.position, spawnPoint.rotation);
        CustomerInside newCustomerScript = newCustomerObj.GetComponent<CustomerInside>();

        // 4. Assign the Spot and Add to List
        if (newCustomerScript != null)
        {
            newCustomerScript.AssignQueueSpot(assignedSpot);
            newCustomerScript.mySpawner = this; // Let them talk back to the spawner
            activeCustomers.Add(newCustomerScript);
        }

        // 5. Decrease the outside counter
        if (NPCWalker.incomingCustomers > 0) 
        {
            NPCWalker.incomingCustomers--;
        }
    }

    // Called by CustomerInside when they leave
    public void CustomerLeft(CustomerInside customer)
    {
        if (activeCustomers.Contains(customer))
        {
            activeCustomers.Remove(customer);
            
            // Optional: Shuffle everyone up the line? 
            // For now, let's just leave the spot open for the next guy.
            UpdateQueuePositions();
        }
    }

    void UpdateQueuePositions()
    {
        // Re-assign spots to fill gaps (Move everyone up)
        for (int i = 0; i < activeCustomers.Count; i++)
        {
            activeCustomers[i].AssignQueueSpot(queueSpots[i]);
        }
    }
}
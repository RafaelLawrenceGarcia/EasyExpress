using UnityEngine;

public class ShopTrafficSimulator : MonoBehaviour
{
    [Header("Foot Traffic Settings")]
    public float checkInterval = 5f;   // Roll the dice every 5 seconds
    [Range(0, 100)] public float walkInChance = 30f; // 30% chance someone enters
    
    [Header("Limits")]
    public int maxCustomersInside = 3; // Don't overcrowd the shop

    private float timer = 0f;
    private ShopCustomerSpawner mySpawner; // Reference to the spawner we made earlier

    void Start()
    {
        mySpawner = GetComponent<ShopCustomerSpawner>();
    }

    void Update()
    {
        // 1. Check how many people are already here
        int peopleInShop = FindObjectsOfType<CustomerInside>().Length;

        // 2. If we have room, run the timer
        if (peopleInShop < maxCustomersInside)
        {
            timer += Time.deltaTime;

            if (timer >= checkInterval)
            {
                TrySpawnWalkIn();
                timer = 0f;
            }
        }
    }

    void TrySpawnWalkIn()
    {
        // Roll the dice!
        float roll = Random.Range(0f, 100f);

        if (roll <= walkInChance)
        {
            Debug.Log("A random customer just walked in from the street!");
            
            // We tell the spawner to do its job
            if (mySpawner != null)
            {
                mySpawner.SpawnCustomer(); 
            }
        }
    }
}
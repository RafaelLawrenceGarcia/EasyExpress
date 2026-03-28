using UnityEngine;

public class ShopTrafficSimulator : MonoBehaviour
{
    [Header("Foot Traffic Settings")]
    public float checkInterval = 5f;
    [Range(0, 100)] public float walkInChance = 30f;

    [Header("Limits")]
    public int maxCustomersInside = 3;

    private float timer = 0f;
    private ShopCustomerSpawner mySpawner;

    void Start()
    {
        mySpawner = GetComponent<ShopCustomerSpawner>();
    }

    void Update()
    {
        // ── BLOCK DURING TUTORIAL ──────────────────────────────
        // Don't let random walk-ins happen while the tutorial is running.
        // Once tutorial is done this check costs almost nothing.
        if (TutorialManager.Instance != null && TutorialManager.Instance.IsTutorialActive())
        return;
        // ───────────────────────────────────────────────────────

        int peopleInShop = FindObjectsOfType<CustomerInside>().Length;

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
        float roll = Random.Range(0f, 100f);

        if (roll <= walkInChance)
        {
            Debug.Log("A random customer just walked in from the street!");

            if (mySpawner != null)
                mySpawner.SpawnCustomer();
        }
    }
}
using System.Collections.Generic;

/// <summary>
/// Holds customer data in memory between scene transitions.
/// Static so it survives scene loads without DontDestroyOnLoad.
/// </summary>
public static class CustomerRetainer
{
    public static List<RetainedCustomer> savedCustomers = new List<RetainedCustomer>();
    public static bool hasRetainedData = false;

    public class RetainedCustomer
    {
        public string npcName;
        public EmailData assignedJob;  // Runtime ScriptableObject — survives as static reference
        public bool isServed;
        public bool isBrowsing;
    }

    /// <summary>
    /// Call before leaving the store scene. Saves all active (non-served) customers.
    /// </summary>
    public static void SaveCustomers(ShopCustomerSpawner spawner)
    {
        savedCustomers.Clear();
        hasRetainedData = false;

        if (spawner == null) return;

        // Use GetAllActiveCustomers or iterate the internal list
        var customers = spawner.GetActiveCustomers();
        if (customers == null || customers.Count == 0) return;

        foreach (CustomerInside c in customers)
        {
            if (c == null || c.isServed) continue;

            RetainedCustomer rc = new RetainedCustomer();
            rc.npcName = c.npcName;
            rc.assignedJob = c.assignedJob;
            rc.isServed = c.isServed;
            rc.isBrowsing = c.isBrowsing;
            savedCustomers.Add(rc);
        }

        hasRetainedData = savedCustomers.Count > 0;
        UnityEngine.Debug.Log("[CustomerRetainer] Saved " + savedCustomers.Count + " customers for scene transition.");
    }

    /// <summary>
    /// Call this to clear retained data (e.g. on new day or new game).
    /// </summary>
    public static void Clear()
    {
        savedCustomers.Clear();
        hasRetainedData = false;
    }
}
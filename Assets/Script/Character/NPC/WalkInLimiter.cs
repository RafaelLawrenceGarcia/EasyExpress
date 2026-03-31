using UnityEngine;

/// <summary>
/// WalkInLimiter — Enforces a maximum number of walk-in customers per day.
///
/// SETUP:
///   1. No GameObject needed — this is a static utility class.
///   2. DayTransitionManager calls WalkInLimiter.ResetDaily() on each new day.
///   3. Your customer spawner calls WalkInLimiter.TrySpawn() before spawning.
///
/// USAGE IN YOUR SPAWNER:
///   if (WalkInLimiter.TrySpawn())
///   {
///       // spawn the customer
///   }
///   else
///   {
///       Debug.Log("Walk-in limit reached for today.");
///   }
/// </summary>
public static class WalkInLimiter
{
    // Hard limit: 3 walk-ins per day
    public static int maxWalkInsPerDay = 3;

    private static int walkInsToday = 0;

    /// <summary>
    /// Call this before spawning a walk-in customer.
    /// Returns true if under the limit (and increments the counter).
    /// Returns false if the limit has been reached.
    /// </summary>
    public static bool TrySpawn()
    {
        if (walkInsToday >= maxWalkInsPerDay)
        {
            Debug.Log($"[WalkInLimiter] Limit reached ({walkInsToday}/{maxWalkInsPerDay}). No more walk-ins today.");
            return false;
        }

        walkInsToday++;
        Debug.Log($"[WalkInLimiter] Walk-in #{walkInsToday}/{maxWalkInsPerDay} allowed.");
        return true;
    }

    /// <summary>
    /// Call this at the start of each new day.
    /// DayTransitionManager handles this automatically.
    /// </summary>
    public static void ResetDaily()
    {
        walkInsToday = 0;
        Debug.Log("[WalkInLimiter] Daily counter reset.");
    }

    /// <summary>
    /// How many walk-ins are left today.
    /// </summary>
    public static int RemainingToday()
    {
        return Mathf.Max(0, maxWalkInsPerDay - walkInsToday);
    }
}
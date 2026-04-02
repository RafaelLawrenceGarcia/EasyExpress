// ============================================================
//  PlayFabStatSync.cs
//  Easy Express – Thesis Project
// ============================================================
//  PURPOSE:
//    Pushes in-game stats (Gold, PCsRepaired, OverallPoints)
//    to PlayFab Leaderboard Statistics so the React website
//    Leaderboard shows live, accurate data.
//
//  SETUP:
//    1. Add this component to the same persistent GameObject
//       as CloudDataHandler (it's already DontDestroyOnLoad).
//    2. In your PlayFab dashboard, create three Statistics:
//         Name: "Gold"           AggregationMethod: Last
//         Name: "PCsRepaired"   AggregationMethod: Last
//         Name: "OverallPoints" AggregationMethod: Last
//    3. CloudDataHandler.SaveGameData() calls SyncStats()
//       automatically — no other wiring needed.
// ============================================================

using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using System.Collections.Generic;

public class PlayFabStatSync : MonoBehaviour
{
    public static PlayFabStatSync Instance;

    // ── Statistic names MUST match PlayFab dashboard exactly ─────────────────
    private const string STAT_GOLD          = "Gold";
    private const string STAT_PCS_REPAIRED  = "PCsRepaired";
    private const string STAT_OVERALL_PTS   = "OverallPoints";

    // ── PlayerPrefs keys (source of truth between scenes) ────────────────────
    private const string PREFS_PCS_REPAIRED  = "PCsRepaired";
    private const string PREFS_OVERALL_PTS   = "OverallPoints";

    private bool _syncPending = false;
    private float _syncDebounceTimer = 0f;
    private const float SYNC_DEBOUNCE = 2f; // Wait 2s after last change before uploading

    void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); return; }
    }

    void Update()
    {
        // Debounced sync — avoids hammering the API on rapid stat changes
        if (_syncPending)
        {
            _syncDebounceTimer -= Time.deltaTime;
            if (_syncDebounceTimer <= 0f)
            {
                _syncPending = false;
                UploadStats();
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  PUBLIC API
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Sync all stats immediately. Called by CloudDataHandler at end-of-day.
    /// </summary>
    public void SyncStats()
    {
        if (!GameSession.IsLoggedIn)
        {
            Debug.Log("[StatSync] Skipped — not logged in.");
            return;
        }
        _syncPending = false; // cancel debounce, we're doing it now
        UploadStats();
    }

    /// <summary>
    /// Queue a debounced sync. Call this any time a stat changes mid-game
    /// so you don't make a PlayFab call on every gold coin collected.
    /// </summary>
    public void QueueSync()
    {
        if (!GameSession.IsLoggedIn) return;
        _syncPending = true;
        _syncDebounceTimer = SYNC_DEBOUNCE;
    }

    /// <summary>
    /// Call this whenever a PC repair is completed.
    /// Increments the counter and queues a debounced stat upload.
    /// </summary>
    /// <param name="bonusPoints">Points awarded for this repair job.</param>
    public void OnPCRepaired(int bonusPoints = 100)
    {
        int repaired = PlayerPrefs.GetInt(PREFS_PCS_REPAIRED, 0) + 1;
        int points   = PlayerPrefs.GetInt(PREFS_OVERALL_PTS, 0) + bonusPoints;

        PlayerPrefs.SetInt(PREFS_PCS_REPAIRED, repaired);
        PlayerPrefs.SetInt(PREFS_OVERALL_PTS,  points);
        PlayerPrefs.Save();

        Debug.Log($"[StatSync] PC Repaired! Total: {repaired}, Points: {points}");
        QueueSync();
    }

    /// <summary>
    /// Directly set Overall Points (e.g., from a scoring formula).
    /// </summary>
    public void SetOverallPoints(int points)
    {
        PlayerPrefs.SetInt(PREFS_OVERALL_PTS, points);
        PlayerPrefs.Save();
        QueueSync();
    }

    /// <summary>
    /// Returns current PCsRepaired count.
    /// </summary>
    public int GetPCsRepaired() => PlayerPrefs.GetInt(PREFS_PCS_REPAIRED, 0);

    /// <summary>
    /// Returns current OverallPoints.
    /// </summary>
    public int GetOverallPoints() => PlayerPrefs.GetInt(PREFS_OVERALL_PTS, 0);

    // ─────────────────────────────────────────────────────────────────────────
    //  INTERNAL
    // ─────────────────────────────────────────────────────────────────────────

    void UploadStats()
    {
        // Gold is stored as a float in PlayerPrefs but PlayFab stats are int.
        // We store in cents (₱ * 100) to preserve two decimal places.
        int goldCents     = Mathf.RoundToInt(PlayerPrefs.GetFloat("SavedGold", 0f) * 100f);
        int pcsRepaired   = PlayerPrefs.GetInt(PREFS_PCS_REPAIRED, 0);
        int overallPoints = PlayerPrefs.GetInt(PREFS_OVERALL_PTS, 0);

        var request = new UpdatePlayerStatisticsRequest
        {
            Statistics = new List<StatisticUpdate>
            {
                new StatisticUpdate { StatisticName = STAT_GOLD,         Value = goldCents },
                new StatisticUpdate { StatisticName = STAT_PCS_REPAIRED, Value = pcsRepaired },
                new StatisticUpdate { StatisticName = STAT_OVERALL_PTS,  Value = overallPoints },
            }
        };

        PlayFabClientAPI.UpdatePlayerStatistics(request,
            result =>
            {
                Debug.Log($"[StatSync] ✓ Leaderboard updated — " +
                          $"Gold: ₱{goldCents / 100f:N0}, " +
                          $"PCsRepaired: {pcsRepaired}, " +
                          $"Points: {overallPoints}");
            },
            error =>
            {
                Debug.LogWarning("[StatSync] ✗ Upload failed: " + error.GenerateErrorReport());
                // Re-queue on failure so it retries next debounce window
                _syncPending = true;
                _syncDebounceTimer = 10f; // retry after 10s
            });
    }
}
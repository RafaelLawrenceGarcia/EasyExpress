// ============================================================
//  ScenePersistenceManager.cs
//  Easy Express – Thesis Project
// ============================================================
//  PURPOSE:
//    When the player moves between scenes (e.g., Shop → Outside),
//    Unity destroys non-persistent MonoBehaviours. This manager
//    snapshots critical runtime state BEFORE the scene unloads
//    and restores it AFTER the new scene's managers initialize.
//
//  ROOT CAUSE OF THE BUG:
//    CloudDataHandler.LoadGameData() skips delivery/shop restore
//    when IsRoomChangeLoad = true. But nothing was actually
//    snapshotting that data before the transition. This fills
//    that gap with a two-layer cache (static memory + PlayerPrefs).
//
//  SETUP:
//    1. Add ScenePersistenceBootstrap to your persistent
//       GameObject (same one as CloudDataHandler).
//    2. Replace ALL your SceneManager.LoadScene() calls with:
//       ScenePersistenceManager.LoadScene("SceneName");
//    3. In DeliveryManager.Start() and ShopSystem.Start(),
//       add one line (see comments below each class).
//
//  NO MonoBehaviour needed — pure static class for zero overhead.
// ============================================================

using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public static class ScenePersistenceManager
{
    // ── Two-layer cache ────────────────────────────────────────────────────────
    // Layer 1: Static fields  (fastest, survives scene loads in same process)
    // Layer 2: PlayerPrefs    (survives unexpected crashes / edge cases)

    private static List<string>        _ownedItemIDs  = null;
    private static List<SavedDelivery> _pendingOrders = null;
    private static float               _cachedGold    = -1f;
    private static bool                _hasPending    = false;

    private const string PREFS_OWNED_IDS     = "SPM_OwnedIDs";
    private const string PREFS_PENDING_ORDERS = "SPM_PendingOrders";
    private const string PREFS_GOLD          = "SPM_Gold";
    private const string PREFS_HAS_PENDING   = "SPM_HasPending";

    // ─────────────────────────────────────────────────────────────────────────
    //  PUBLIC: SCENE TRANSITION ENTRY POINT
    //  Replace SceneManager.LoadScene() calls with this throughout your project.
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Snapshots current game state, then loads the target scene.
    /// This is a DROP-IN replacement for SceneManager.LoadScene().
    /// </summary>
    public static void LoadScene(string sceneName)
    {
        Snapshot();
        CloudDataHandler.IsRoomChangeLoad = true;
        SceneManager.LoadScene(sceneName);
    }

    /// <summary>
    /// Async version — use when you need a loading screen coroutine.
    /// </summary>
    public static AsyncOperation LoadSceneAsync(string sceneName)
    {
        Snapshot();
        CloudDataHandler.IsRoomChangeLoad = true;
        return SceneManager.LoadSceneAsync(sceneName);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  SNAPSHOT — Call before scene unload
    // ─────────────────────────────────────────────────────────────────────────

    public static void Snapshot()
    {
        // ── GOLD (PlayerPrefs is already up-to-date from PlayerWallet.SaveData) ──
        _cachedGold = PlayerPrefs.GetFloat("SavedGold", 0f);

        // ── SHOP INVENTORY ──
        ShopSystem shop = ShopSystem.Instance;
        _ownedItemIDs = shop != null
            ? new List<string>(shop.GetInventoryIDs())
            : TryDeserializeOwnedIDs();

        // ── PENDING DELIVERIES ──
        DeliveryManager delivery = DeliveryManager.Instance;
        if (delivery != null)
        {
            _pendingOrders = new List<SavedDelivery>();
            foreach (DeliveryOrder order in delivery.pendingOrders)
            {
                if (order?.item == null) continue;
                _pendingOrders.Add(new SavedDelivery
                {
                    itemId        = order.item.id,
                    quantity      = order.quantity,
                    daysRemaining = order.daysRemaining
                });
            }
        }
        else
        {
            _pendingOrders = TryDeserializeOrders();
        }

        _hasPending = true;

        // ── PERSIST TO PLAYERPREFS (fallback layer) ──
        FlushToPrefs();

        Debug.Log($"[ScenePersist] Snapshot complete — " +
                  $"Items: {_ownedItemIDs?.Count ?? 0}, " +
                  $"Deliveries: {_pendingOrders?.Count ?? 0}, " +
                  $"Gold: ₱{_cachedGold:N0}");
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  RESTORE — Call from DeliveryManager.Start() and ShopSystem.Start()
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Called by DeliveryManager and ShopSystem after they initialize in the new scene.
    /// Returns true if data was successfully restored.
    ///
    ///  HOW TO USE IN DeliveryManager.Start():
    ///    void Start() {
    ///        if (ScenePersistenceManager.HasPendingRestore)
    ///            ScenePersistenceManager.RestoreDeliveries(this, ShopSystem.Instance);
    ///    }
    ///
    ///  HOW TO USE IN ShopSystem.Start():
    ///    void Start() {
    ///        if (ScenePersistenceManager.HasPendingRestore)
    ///            ScenePersistenceManager.RestoreShop(this);
    ///    }
    /// </summary>
    public static bool HasPendingRestore
    {
        get
        {
            if (_hasPending) return true;
            // Check PlayerPrefs fallback
            return PlayerPrefs.GetInt(PREFS_HAS_PENDING, 0) == 1;
        }
    }

    public static void RestoreShop(ShopSystem shop)
    {
        if (shop == null) return;
        EnsureLoaded();

        if (_ownedItemIDs != null && _ownedItemIDs.Count > 0)
        {
            shop.SetInventoryIDs(_ownedItemIDs.ToArray());
            Debug.Log($"[ScenePersist] ✓ Shop restored — {_ownedItemIDs.Count} items.");
        }
    }

    public static void RestoreDeliveries(DeliveryManager delivery, ShopSystem shop)
    {
        if (delivery == null) return;
        EnsureLoaded();

        if (_pendingOrders == null || _pendingOrders.Count == 0) return;

        delivery.pendingOrders.Clear();
        int restored = 0;

        foreach (SavedDelivery saved in _pendingOrders)
        {
            ItemData item = FindItemById(saved.itemId, shop);
            if (item != null)
            {
                delivery.pendingOrders.Add(new DeliveryOrder(item, saved.quantity, saved.daysRemaining));
                restored++;
            }
            else
            {
                Debug.LogWarning($"[ScenePersist] Could not find ItemData for: {saved.itemId}");
            }
        }

        Debug.Log($"[ScenePersist] ✓ Deliveries restored — {restored}/{_pendingOrders.Count} orders.");
        ClearAfterRestore();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  INTERNAL HELPERS
    // ─────────────────────────────────────────────────────────────────────────

    static void EnsureLoaded()
    {
        // If static cache is empty (e.g. after a crash-reload), try PlayerPrefs
        if (!_hasPending && PlayerPrefs.GetInt(PREFS_HAS_PENDING, 0) == 1)
        {
            _ownedItemIDs  = TryDeserializeOwnedIDs();
            _pendingOrders = TryDeserializeOrders();
            _cachedGold    = PlayerPrefs.GetFloat(PREFS_GOLD, -1f);
            _hasPending    = true;
            Debug.Log("[ScenePersist] Loaded from PlayerPrefs fallback.");
        }
    }

    static void FlushToPrefs()
    {
        if (_ownedItemIDs != null)
        {
            var wrapper = new OwnedIDsWrapper { ids = _ownedItemIDs };
            PlayerPrefs.SetString(PREFS_OWNED_IDS, JsonUtility.ToJson(wrapper));
        }

        if (_pendingOrders != null)
        {
            var wrapper = new OrdersWrapper { orders = _pendingOrders };
            PlayerPrefs.SetString(PREFS_PENDING_ORDERS, JsonUtility.ToJson(wrapper));
        }

        PlayerPrefs.SetFloat(PREFS_GOLD, _cachedGold);
        PlayerPrefs.SetInt(PREFS_HAS_PENDING, 1);
        PlayerPrefs.Save();
    }

    static void ClearAfterRestore()
    {
        _ownedItemIDs  = null;
        _pendingOrders = null;
        _cachedGold    = -1f;
        _hasPending    = false;
        PlayerPrefs.DeleteKey(PREFS_OWNED_IDS);
        PlayerPrefs.DeleteKey(PREFS_PENDING_ORDERS);
        PlayerPrefs.DeleteKey(PREFS_GOLD);
        PlayerPrefs.SetInt(PREFS_HAS_PENDING, 0);
        PlayerPrefs.Save();
        CloudDataHandler.IsRoomChangeLoad = false;
    }

    static ItemData FindItemById(string id, ShopSystem shop)
    {
        if (string.IsNullOrEmpty(id) || shop == null) return null;
        foreach (ItemData item in shop.allAvailableItems)
            if (item != null && item.id == id) return item;
        return null;
    }

    // ── JSON Wrappers (JsonUtility needs a class root) ────────────────────────

    [System.Serializable]
    private class OwnedIDsWrapper { public List<string> ids = new List<string>(); }

    [System.Serializable]
    private class OrdersWrapper { public List<SavedDelivery> orders = new List<SavedDelivery>(); }

    static List<string> TryDeserializeOwnedIDs()
    {
        string raw = PlayerPrefs.GetString(PREFS_OWNED_IDS, "");
        if (string.IsNullOrEmpty(raw)) return null;
        try { return JsonUtility.FromJson<OwnedIDsWrapper>(raw)?.ids; }
        catch { return null; }
    }

    static List<SavedDelivery> TryDeserializeOrders()
    {
        string raw = PlayerPrefs.GetString(PREFS_PENDING_ORDERS, "");
        if (string.IsNullOrEmpty(raw)) return null;
        try { return JsonUtility.FromJson<OrdersWrapper>(raw)?.orders; }
        catch { return null; }
    }
}

// ============================================================
//  ScenePersistenceBootstrap.cs (inner class, same file)
//  Add this as a component alongside CloudDataHandler to
//  auto-hook scene load events.
// ============================================================

using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    void Awake()
    {
        // Singleton Pattern: Make sure only one Brain exists
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;

            // ── FAILSAFE: Direct-to-Gameplay without auth ──
            // If we got here without going through login or "Play Offline",
            // force a guest session so we only use local saves and never
            // touch cloud data.
            if (!GameSession.IsLoggedIn && !GameSession.IsGuest)
            {
                Debug.Log("[GameManager] No session detected — forcing guest mode (local save only).");
                GameSession.StartGuestSession();
                PlayerPrefs.SetInt("IsLoadingGame", 0);
                PlayerPrefs.Save();
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Called automatically every time a scene finishes loading
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (DemoLockManager.Instance != null
            && DemoLockManager.Instance.CheckDemoStatus())
            return;

        if (PlayerPrefs.GetInt("IsLoadingGame") == 1)
        {
            // ── CLOUD SESSION: load from PlayFab (last end-of-day checkpoint) ──
            if (GameSession.IsLoggedIn && CloudDataHandler.Instance != null)
            {
                Debug.Log("[GameManager] Cloud session — loading last checkpoint from PlayFab.");
                CloudDataHandler.Instance.LoadGameDataDelayed();
            }
            else
            {
                // ── GUEST SESSION: load from local PlayerPrefs ──
                Debug.Log("[GameManager] Guest session — loading local save.");
                LoadPlayerData();
            }

            PlayerPrefs.SetInt("IsLoadingGame", 0);
        }
    }

    public void SaveGame()
    {
        // Save player position locally (for scene transitions)
        GameObject player = GameObject.FindWithTag("Player");

        if (player != null)
        {
            PlayerPrefs.SetFloat("SaveX", player.transform.position.x);
            PlayerPrefs.SetFloat("SaveY", player.transform.position.y);
            PlayerPrefs.SetFloat("SaveZ", player.transform.position.z);
            PlayerPrefs.SetFloat("RotY", player.transform.eulerAngles.y);
            PlayerPrefs.SetString("SavedScene", SceneManager.GetActiveScene().name);
        }

        // Checkpoint system: gold is only saved at end-of-day.
        // Player position is still saved locally for scene transitions.

        PlayerPrefs.SetInt("HasSaveData", 1);
        PlayerPrefs.Save();
        Debug.Log("[GameManager] Local save complete (position only).");
    }

    void LoadPlayerData()
    {
        GameObject player = GameObject.FindWithTag("Player");
        PlayerWallet wallet = FindObjectOfType<PlayerWallet>();

        if (player != null && PlayerPrefs.HasKey("SaveX"))
        {
            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc) cc.enabled = false;

            Vector3 pos;
            pos.x = PlayerPrefs.GetFloat("SaveX");
            pos.y = PlayerPrefs.GetFloat("SaveY");
            pos.z = PlayerPrefs.GetFloat("SaveZ");
            player.transform.position = pos;

            float rotY = PlayerPrefs.GetFloat("RotY", 0);
            player.transform.rotation = Quaternion.Euler(0, rotY, 0);

            if (cc) cc.enabled = true;
        }

        // Guest session: gold loaded from PlayerPrefs (last local checkpoint)
        if (wallet != null && PlayerPrefs.HasKey("SavedGold"))
        {
            wallet.currentGold = PlayerPrefs.GetFloat("SavedGold");
            wallet.UpdateUI();
        }
    }
}
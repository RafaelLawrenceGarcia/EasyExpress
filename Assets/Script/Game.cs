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
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Called automatically every time a scene finishes loading
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (PlayerPrefs.GetInt("IsLoadingGame") == 1)
        {
            // ── CLOUD SESSION: load from PlayFab ──
            if (GameSession.IsLoggedIn && CloudDataHandler.Instance != null)
            {
                Debug.Log("[GameManager] Cloud session — loading save from PlayFab.");
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
        // 1. Always save locally to PlayerPrefs (works for both modes)
        GameObject player = GameObject.FindWithTag("Player");
        PlayerWallet wallet = FindObjectOfType<PlayerWallet>();

        if (player != null)
        {
            PlayerPrefs.SetFloat("SaveX", player.transform.position.x);
            PlayerPrefs.SetFloat("SaveY", player.transform.position.y);
            PlayerPrefs.SetFloat("SaveZ", player.transform.position.z);
            PlayerPrefs.SetFloat("RotY", player.transform.eulerAngles.y);
            PlayerPrefs.SetString("SavedScene", SceneManager.GetActiveScene().name);
        }

        if (wallet != null)
        {
            PlayerPrefs.SetFloat("SavedGold", wallet.currentGold);
        }

        PlayerPrefs.SetInt("HasSaveData", 1);
        PlayerPrefs.Save();
        Debug.Log("[GameManager] Local save complete.");

        // 2. If logged in, ALSO push to cloud
        if (GameSession.IsLoggedIn && CloudDataHandler.Instance != null)
        {
            CloudDataHandler.Instance.SaveGameData();
            Debug.Log("[GameManager] Cloud save triggered.");
        }
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

        if (wallet != null && PlayerPrefs.HasKey("SavedGold"))
        {
            wallet.currentGold = PlayerPrefs.GetFloat("SavedGold");
            wallet.UpdateUI();
        }
    }
}
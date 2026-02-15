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
            DontDestroyOnLoad(gameObject); // Keep this alive when switching scenes
            SceneManager.sceneLoaded += OnSceneLoaded; // Listen for level changes
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Called automatically every time a scene finishes loading
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // If we flagged that we are "Continuing", load the data now
        if (PlayerPrefs.GetInt("IsLoadingGame") == 1)
        {
            LoadPlayerData();
            PlayerPrefs.SetInt("IsLoadingGame", 0); // Reset flag
        }
    }

    public void SaveGame()
    {
        // 1. Find the Player
        GameObject player = GameObject.FindWithTag("Player");
        PlayerWallet wallet = FindObjectOfType<PlayerWallet>();

        if (player != null)
        {
            // Save Position
            PlayerPrefs.SetFloat("SaveX", player.transform.position.x);
            PlayerPrefs.SetFloat("SaveY", player.transform.position.y);
            PlayerPrefs.SetFloat("SaveZ", player.transform.position.z);
            
            // Save Rotation
            PlayerPrefs.SetFloat("RotY", player.transform.eulerAngles.y);
            
            // Save Scene Name
            PlayerPrefs.SetString("SavedScene", SceneManager.GetActiveScene().name);
        }

        if (wallet != null)
        {
            PlayerPrefs.SetFloat("SavedGold", wallet.currentGold);
        }

        PlayerPrefs.SetInt("HasSaveData", 1); // Mark that a save file exists
        PlayerPrefs.Save();
        Debug.Log("Game Saved!");
    }

    void LoadPlayerData()
    {
        GameObject player = GameObject.FindWithTag("Player");
        PlayerWallet wallet = FindObjectOfType<PlayerWallet>();

        if (player != null && PlayerPrefs.HasKey("SaveX"))
        {
            // Disable CharacterController to prevent glitching while moving
            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc) cc.enabled = false;

            // Teleport
            Vector3 pos;
            pos.x = PlayerPrefs.GetFloat("SaveX");
            pos.y = PlayerPrefs.GetFloat("SaveY");
            pos.z = PlayerPrefs.GetFloat("SaveZ");
            player.transform.position = pos;

            // Rotate
            Vector3 rot = player.transform.eulerAngles;
            rot.y = PlayerPrefs.GetFloat("RotY");
            player.transform.rotation = Quaternion.Euler(rot);

            if (cc) cc.enabled = true;
        }

        if (wallet != null)
        {
            wallet.currentGold = PlayerPrefs.GetFloat("SavedGold");
            // Force UI update if you have a method for it, e.g., wallet.UpdateUI();
        }
    }
}
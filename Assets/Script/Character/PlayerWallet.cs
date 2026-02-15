using UnityEngine;
using TMPro;

public class PlayerWallet : MonoBehaviour
{
    [Header("Settings")]
    public float currentGold = 0.00f; // Changed 'int' to 'float' for decimals

    [Header("UI Reference")]
    public TextMeshProUGUI goldText; 

    void Start()
    {
        // Load the money we saved earlier. 
        // If there is no save, default to 0.
        currentGold = PlayerPrefs.GetFloat("SavedGold", 0);
        
        UpdateUI();
    }

    // Now accepts decimals (e.g., 50.25)
    public void AddGold(float amount)
    {
        currentGold += amount;
        UpdateUI();
    }

    public void SpendGold(float amount)
    {
        if(currentGold >= amount)
        {
            currentGold -= amount;
            UpdateUI();
        }
        else
        {
            Debug.Log("Not enough money!");
        }
    }

    void UpdateUI()
    {
        if (goldText != null)
        {
            // "F2" means "Fixed-point with 2 decimal places"
            // Example: 0 becomes "0.00", 5.1 becomes "5.10"
            goldText.text = currentGold.ToString("F2");
        }
    }

    // Cheat key for testing
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
        {
            AddGold(100.50f); // Adds 100 gold and 50 cents
        }
    }
}
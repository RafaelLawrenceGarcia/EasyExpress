using UnityEngine;
using TMPro;

public class PlayerWallet : MonoBehaviour
{
    [Header("Settings")]
    public float currentGold = 0.00f; 
    public float currentDebt = 0.00f;       // Tracks how much the player owes
    public float maxLoanLimit = 50000.00f;  // The maximum debt they are allowed to have

    [Header("UI Reference")]
    public TextMeshProUGUI goldText; 
    public TextMeshProUGUI debtText; // Optional: A UI text to show their current debt
    
    // NEW: Reference to an input field if you want to type an amount to add/borrow via UI
    public TMP_InputField customAmountInput; 

    void Start()
    {
        // Load the money and debt we saved earlier
        currentGold = PlayerPrefs.GetFloat("SavedGold", 0);
        currentDebt = PlayerPrefs.GetFloat("SavedDebt", 0);
        
        UpdateUI();
    }

    // Standard method for adding gold (from sales, etc.)
    public void AddGold(float amount)
    {
        currentGold += amount;
        UpdateUI();
        SaveData();
    }

    // NEW: This reads the number typed into your TMP_InputField on the screen!
    public void AddGoldFromUI()
    {
        if (customAmountInput != null)
        {
            // Try to convert the typed text into a float
            if (float.TryParse(customAmountInput.text, out float typedAmount))
            {
                AddGold(typedAmount);
                customAmountInput.text = ""; // Clear the input box after adding
            }
            else
            {
                Debug.LogWarning("Please enter a valid number!");
            }
        }
    }

    // NEW: Method to manually pay down the loan
    public void PayDebt(float amount)
    {
        if (currentGold >= amount && currentDebt > 0)
        {
            currentGold -= amount;
            currentDebt -= amount;
            
            // Safety check so debt doesn't go below 0
            if (currentDebt < 0) currentDebt = 0; 
            
            UpdateUI();
            SaveData();
        }
        else
        {
            Debug.Log("Not enough gold to make this loan payment.");
        }
    }

    // UPDATED: Now returns a true/false and handles automatic loans!
    public bool SpendGold(float amount)
    {
        // Scenario 1: Player has enough cash
        if (currentGold >= amount)
        {
            currentGold -= amount;
            UpdateUI();
            SaveData();
            return true; // Purchase successful!
        }
        // Scenario 2: Player doesn't have enough cash, but it fits in their loan limit
        else if ((currentGold + maxLoanLimit - currentDebt) >= amount)
        {
            float remainingCost = amount - currentGold; // Calculate what we are short
            currentGold = 0;                            // Drain whatever cash we had
            currentDebt += remainingCost;               // Put the rest on the tab
            
            Debug.Log("Bought with a loan! Added ₱" + remainingCost + " to debt.");
            UpdateUI();
            SaveData();
            return true; // Purchase successful!
        }
        // Scenario 3: Player is too broke and maxed out their credit
        else
        {
            Debug.Log("Purchase failed: Not enough money AND reached maximum loan limit!");
            return false; // Purchase failed!
        }
    }

    public void UpdateUI()
    {
        if (goldText != null)
        {
            goldText.text = "₱" + currentGold.ToString("F2");
        }
        if (debtText != null)
        {
            debtText.text = "Debt: ₱" + currentDebt.ToString("F2");
        }
    }

    // Helper method to keep saving clean
    private void SaveData()
    {
        PlayerPrefs.SetFloat("SavedGold", currentGold);
        PlayerPrefs.SetFloat("SavedDebt", currentDebt);
        if (CloudDataHandler.Instance) CloudDataHandler.Instance.SaveGameData();
    }

    // Cheat key for testing
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
        {
            AddGold(100.50f); 
        }
    }
}
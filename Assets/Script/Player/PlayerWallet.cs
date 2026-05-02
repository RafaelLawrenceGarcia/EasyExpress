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
        // Read current gold from PlayerPrefs (kept up-to-date by SaveData()).
        // For room changes: this is the correct current value.
        // For checkpoint loads: CloudDataHandler.RestoreGameData() will
        //   override this a few frames later via SetAllWallets().
        if (PlayerPrefs.HasKey("SavedGold"))
        {
            currentGold = PlayerPrefs.GetFloat("SavedGold");
            currentDebt = PlayerPrefs.GetFloat("SavedDebt", 0);
        }

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
        if (currentGold >= amount)
        {
            currentGold -= amount;
            UpdateUI();
            SaveData();
            return true;
        }

        Debug.Log("Not enough gold to purchase.");
        return false;
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

    // Keeps PlayerPrefs up-to-date so:
    // 1. CloudDataHandler.SaveGameData() reads the correct gold at end-of-day
    // 2. Room changes carry gold over via PlayerPrefs
    // No cloud save triggered here — checkpoint system only.
    private void SaveData()
    {
        PlayerPrefs.SetFloat("SavedGold", currentGold);
        PlayerPrefs.SetFloat("SavedDebt", currentDebt);
    }

    // Cheat key for testing
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
        {
            AddGold(10000f);
        }
    }
}
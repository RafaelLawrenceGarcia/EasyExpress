using UnityEngine;
using TMPro;

public class EconomyManager : MonoBehaviour
{
    // The Singleton Instance
    public static EconomyManager Instance { get; private set; }

    [Header("Settings")]
    public float currentGold = 0.00f;
    public TextMeshProUGUI goldText; 

    void Awake()
    {
        // Ensure there is only one EconomyManager
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start() => UpdateUI();

    public void AddGold(float amount)
    {
        currentGold += amount;
        UpdateUI();
    }

    public void SpendGold(float amount)
    {
        if (currentGold >= amount)
        {
            currentGold -= amount;
            UpdateUI();
        }
    }

    void UpdateUI()
    {
        if (goldText != null) goldText.text = currentGold.ToString("F2");
    }
}
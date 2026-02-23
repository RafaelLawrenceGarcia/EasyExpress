using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class ShopUI : MonoBehaviour
{
    [Header("References")]
    public Transform contentGrid;   // The "Content" object inside your Scroll View
    public GameObject buttonTemplate; // Your designed button prefab
    
    [Header("Categories")]
    public ItemCategory currentCategory = ItemCategory.PCPart;

    private List<GameObject> activeButtons = new List<GameObject>();

    void Start()
    {
        // Hide the template if it's in the scene
        buttonTemplate.SetActive(false);
        RefreshShop();
    }

    // Call this via UI Buttons (e.g., "Parts Tab" button clicks this with int 0)
    public void SetCategory(int categoryIndex)
    {
        currentCategory = (ItemCategory)categoryIndex;
        RefreshShop();
    }

    public void RefreshShop()
    {
        // 1. Clear old buttons
        foreach (GameObject btn in activeButtons) Destroy(btn);
        activeButtons.Clear();

        // 2. Loop through all items in the system
        foreach (ItemData item in ShopSystem.Instance.allAvailableItems)
        {
            // Only show items for the selected tab
            if (item.itemType == currentCategory)
            {
                CreateItemButton(item);
            }
        }
    }

    void CreateItemButton(ItemData item)
    {
        // Copy the template
        GameObject newBtn = Instantiate(buttonTemplate, contentGrid);
        newBtn.SetActive(true);
        activeButtons.Add(newBtn);

        // Fill in the Text/Icon (Assumes your button has these components)
        // You can change "Find" to direct references if you make a separate Button script
        TextMeshProUGUI nameTxt = newBtn.transform.Find("NameText").GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI priceTxt = newBtn.transform.Find("PriceText").GetComponent<TextMeshProUGUI>();
        Image iconImg = newBtn.transform.Find("Icon").GetComponent<Image>();
        Button btnComp = newBtn.GetComponent<Button>();

        if(nameTxt) nameTxt.text = item.itemName;
        if(priceTxt) priceTxt.text = "$" + item.price.ToString("F2");
        if(iconImg) iconImg.sprite = item.icon;

        // Add Click Listener
        btnComp.onClick.AddListener(() => OnItemClicked(item));
    }

    void OnItemClicked(ItemData item)
    {
        ShopSystem.Instance.BuyItem(item);
    }
}
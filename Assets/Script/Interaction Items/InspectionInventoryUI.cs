using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class InspectionInventoryUI : MonoBehaviour
{
    [Header("Panels")]
    public GameObject inventoryPanel; // The main parent panel containing everything
    public GameObject detailPanel;    // The middle panel

    [Header("Inventory Grid (Right Side)")]
    public Transform slotsContainer;  // The grid layout group
    public GameObject slotPrefab;     // Your square button prefab

    [Header("Detail View (Middle)")]
    public TextMeshProUGUI detailCategoryText; // e.g. "RAM"
    public TextMeshProUGUI detailNameText;     // e.g. "HyperX 8GB"
    public TextMeshProUGUI detailDescText;
    public Image detailIcon;
    public Button actionButton;                // The "Add to PC" / "Use" button
    public TextMeshProUGUI actionButtonText;

    private InspectionManager inspectManager;
    private GameObject currentlySelectedItemObj;
    private InspectableItem currentlySelectedItemData;

    void Start()
    {
        inspectManager = FindObjectOfType<InspectionManager>();
        inventoryPanel.SetActive(false);
        detailPanel.SetActive(false);
    }

    public void ToggleInventory()
    {
        // 1. Crash Prevention: If we don't have the manager yet, find it right now!
        if (inspectManager == null) 
        {
            inspectManager = FindObjectOfType<InspectionManager>();
        }

        bool isOpening = !inventoryPanel.activeSelf;
        inventoryPanel.SetActive(isOpening);

        if (isOpening)
        {
            RefreshInventory();
            if (detailPanel != null) detailPanel.SetActive(false); 
        }
    }

    public void RefreshInventory()
    {
        foreach (Transform child in slotsContainer) 
        {
            Destroy(child.gameObject);
        }

       foreach (GameObject itemObj in inspectManager.playerStorage) 
        {
            InspectableItem itemData = itemObj.GetComponent<InspectableItem>();
            if (itemData != null)
            {
               GameObject newSlot = Instantiate(slotPrefab, slotsContainer); 
                
                // 1. Find the parent where the icon should live
                Transform iconParent = newSlot.transform.Find("Icon"); 
                
                // 2. Spawn the icon prefab if it exists
                if (iconParent != null && itemData.itemIconPrefab != null)
                {
                    // Clean up any default placeholder image first
                    if (iconParent.GetComponent<Image>() != null) 
                        iconParent.GetComponent<Image>().enabled = false;

                    GameObject spawnedIcon = Instantiate(itemData.itemIconPrefab, iconParent);
                    
                    // Reset the prefab's UI transform so it fits the slot perfectly
                    RectTransform rect = spawnedIcon.GetComponent<RectTransform>();
                    if(rect != null)
                    {
                        rect.anchoredPosition = Vector2.zero;
                        rect.localScale = Vector3.one;
                    }
                }

                // Set the name text [cite: 1115]
               Transform nameTransform = newSlot.transform.Find("NameText"); 
                if (nameTransform != null)
                {
                    TextMeshProUGUI slotNameText = nameTransform.GetComponent<TextMeshProUGUI>(); 
                    if (slotNameText != null) slotNameText.text = itemData.itemName;
                }

                Button btn = newSlot.GetComponent<Button>(); 
                btn.onClick.AddListener(() => SelectItem(itemObj, itemData));
            }
        }
    }
    public void SelectItem(GameObject itemObj, InspectableItem itemData)
    {
        currentlySelectedItemObj = itemObj; 
        currentlySelectedItemData = itemData;

        detailPanel.SetActive(true); 
        detailCategoryText.text = itemData.partCategory;
        detailNameText.text = itemData.itemName;
        detailDescText.text = itemData.itemDescription;
        
        // Handle Detail View Icon Prefab
        if (detailIcon != null)
        {
            // Clear previous detailed icon
            foreach (Transform child in detailIcon.transform) Destroy(child.gameObject);

            if (itemData.itemIconPrefab != null)
            {
                detailIcon.enabled = false; // Hide the background box if you prefer
                GameObject detailIconObj = Instantiate(itemData.itemIconPrefab, detailIcon.transform);
                
                RectTransform rect = detailIconObj.GetComponent<RectTransform>();
                if(rect != null)
                {
                    rect.anchoredPosition = Vector2.zero;
                    rect.localScale = Vector3.one;
                }
            }
        }

        actionButton.onClick.RemoveAllListeners();
        actionButton.onClick.AddListener(TryInstallSelected);
    }

   void TryInstallSelected()
    {
        // Tell the manager to SHOW the blue ghost slots for this specific part
        inspectManager.PrepareInstallationFromUI(currentlySelectedItemObj, currentlySelectedItemData);
        
        // Close the UI so the player can manually click the blue slot!
        ToggleInventory(); 
    }
}
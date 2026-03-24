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

    // Memory for what the player just clicked on
    private GameObject currentlySelectedItemObj;
    private InspectableItem currentlySelectedItemData;
    private ItemData currentlySelectedShopItem; // NEW: Tracks if it came from the shop!

    void Start()
    {
        inspectManager = FindObjectOfType<InspectionManager>();
        inventoryPanel.SetActive(false);
        detailPanel.SetActive(false);
    }

    public void ToggleInventory()
    {
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
        // 0. Clear old slots
        foreach (Transform child in slotsContainer)
        {
            Destroy(child.gameObject);
        }

        // ==========================================
        // 1. LOAD REMOVED PARTS (From PC Inspection)
        // ==========================================
        foreach (GameObject itemObj in inspectManager.playerStorage)
        {
            InspectableItem itemData = itemObj.GetComponent<InspectableItem>();
            if (itemData != null)
            {
                GameObject newSlot = Instantiate(slotPrefab, slotsContainer);

                Transform iconParent = newSlot.transform.Find("Icon");
                if (iconParent != null && itemData.itemIconPrefab != null)
                {
                    if (iconParent.GetComponent<Image>() != null)
                        iconParent.GetComponent<Image>().enabled = false;

                    GameObject spawnedIcon = Instantiate(itemData.itemIconPrefab, iconParent);
                    RectTransform rect = spawnedIcon.GetComponent<RectTransform>();
                    if (rect != null)
                    {
                        rect.anchoredPosition = Vector2.zero;
                        rect.localScale = Vector3.one;
                    }
                }

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

        // ==========================================
        // 2. LOAD BOUGHT PARTS (From Shop System)
        // ==========================================
        if (ShopSystem.Instance != null)
        {
            List<string> shopItemIDs = ShopSystem.Instance.GetInventoryIDs();

            foreach (string id in shopItemIDs)
            {
                ItemData shopItem = ShopSystem.Instance.allAvailableItems.Find(x => x.id == id);

                // Make sure the shop item actually has a 3D physical counterpart we can install
                if (shopItem != null && shopItem.prefabToPlace != null)
                {
                    InspectableItem prefabData = shopItem.prefabToPlace.GetComponent<InspectableItem>();
                    if (prefabData == null) continue;

                    GameObject newSlot = Instantiate(slotPrefab, slotsContainer);

                    Transform iconParent = newSlot.transform.Find("Icon");
                    if (iconParent != null)
                    {
                        Image img = iconParent.GetComponent<Image>();

                        // Use the 2D Shop Sprite if it has one!
                        if (shopItem.icon != null && img != null)
                        {
                            img.sprite = shopItem.icon;
                            img.enabled = true;
                        }
                        // Fallback to the 3D icon prefab if no sprite exists
                        else if (prefabData.itemIconPrefab != null)
                        {
                            if (img != null) img.enabled = false;
                            GameObject spawnedIcon = Instantiate(prefabData.itemIconPrefab, iconParent);
                            RectTransform rect = spawnedIcon.GetComponent<RectTransform>();
                            if (rect != null) { rect.anchoredPosition = Vector2.zero; rect.localScale = Vector3.one; }
                        }
                    }

                    Transform nameTransform = newSlot.transform.Find("NameText");
                    if (nameTransform != null)
                    {
                        TextMeshProUGUI slotNameText = nameTransform.GetComponent<TextMeshProUGUI>();
                        if (slotNameText != null) slotNameText.text = shopItem.itemName;
                    }

                    Button btn = newSlot.GetComponent<Button>();
                    // Route this click to our new Shop Item setup!
                    btn.onClick.AddListener(() => SelectShopItem(shopItem, prefabData));
                }
            }
        }
    }

    // --- FOR PHYSICAL ITEMS ALREADY REMOVED FROM THE PC ---
    public void SelectItem(GameObject itemObj, InspectableItem itemData)
    {
        currentlySelectedItemObj = itemObj;
        currentlySelectedItemData = itemData;
        currentlySelectedShopItem = null; // Clear out the shop item memory!

        detailPanel.SetActive(true);
        detailCategoryText.text = itemData.partCategory;
        detailNameText.text = itemData.itemName;
        detailDescText.text = itemData.itemDescription;

        if (detailIcon != null)
        {
            foreach (Transform child in detailIcon.transform) Destroy(child.gameObject);

            if (itemData.itemIconPrefab != null)
            {
                detailIcon.enabled = false;
                GameObject detailIconObj = Instantiate(itemData.itemIconPrefab, detailIcon.transform);

                RectTransform rect = detailIconObj.GetComponent<RectTransform>();
                if (rect != null) { rect.anchoredPosition = Vector2.zero; rect.localScale = Vector3.one; }
            }
        }

        actionButton.onClick.RemoveAllListeners();
        actionButton.onClick.AddListener(TryInstallSelected);
    }

    // --- FOR BRAND NEW ITEMS FROM THE SHOP ---
    public void SelectShopItem(ItemData shopItem, InspectableItem prefabData)
    {
        currentlySelectedItemObj = null; // No physical object exists yet!
        currentlySelectedItemData = prefabData;
        currentlySelectedShopItem = shopItem;

        detailPanel.SetActive(true);
        detailCategoryText.text = shopItem.category;
        detailNameText.text = shopItem.itemName;
        detailDescText.text = shopItem.description;

        if (detailIcon != null)
        {
            foreach (Transform child in detailIcon.transform) Destroy(child.gameObject);

            if (shopItem.icon != null)
            {
                detailIcon.sprite = shopItem.icon;
                detailIcon.enabled = true;
            }
            else if (prefabData.itemIconPrefab != null)
            {
                detailIcon.enabled = false;
                GameObject detailIconObj = Instantiate(prefabData.itemIconPrefab, detailIcon.transform);

                RectTransform rect = detailIconObj.GetComponent<RectTransform>();
                if (rect != null) { rect.anchoredPosition = Vector2.zero; rect.localScale = Vector3.one; }
            }
        }

        actionButton.onClick.RemoveAllListeners();
        actionButton.onClick.AddListener(TryInstallShopItem);
    }

    // --- INSTALLING EXISTING PARTS ---
    void TryInstallSelected()
    {
        inspectManager.PrepareInstallationFromUI(currentlySelectedItemObj, currentlySelectedItemData);
        ToggleInventory();
    }

    // --- INSTALLING NEW SHOP PARTS ---
    void TryInstallShopItem()
    {
        GameObject newPartObj = Instantiate(currentlySelectedShopItem.prefabToPlace);
        newPartObj.SetActive(false);

        InspectableItem newPartData = newPartObj.GetComponent<InspectableItem>();

        // --- NEW FIX: Force the 3D part to inherit the Shop Category! ---
        if (newPartData.partCategory == "Generic" || string.IsNullOrEmpty(newPartData.partCategory))
        {
            newPartData.partCategory = currentlySelectedShopItem.category;
        }

        inspectManager.playerStorage.Add(newPartObj);
        ShopSystem.Instance.GetInventoryIDs().Remove(currentlySelectedShopItem.id);
        inspectManager.PrepareInstallationFromUI(newPartObj, newPartData);
        ToggleInventory();
    }
}
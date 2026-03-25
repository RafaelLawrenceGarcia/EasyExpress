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
            Destroy(child.gameObject);

        // ==========================================
        // STEP 1 — Group playerStorage by itemName
        // ==========================================
        // Key = itemName, Value = list of GameObjects with that name
        Dictionary<string, List<GameObject>> storageGroups = new Dictionary<string, List<GameObject>>();

        foreach (GameObject itemObj in inspectManager.playerStorage)
        {
            InspectableItem itemData = itemObj.GetComponent<InspectableItem>();
            if (itemData == null) continue;

            string key = itemData.itemName;
            if (!storageGroups.ContainsKey(key))
                storageGroups[key] = new List<GameObject>();

            storageGroups[key].Add(itemObj);
        }

        // Sort groups: GPU first, then RAM, CPU, etc. alphabetically
        List<string> storageKeys = new List<string>(storageGroups.Keys);
        storageKeys.Sort((a, b) =>
        {
            string catA = storageGroups[a][0].GetComponent<InspectableItem>().partCategory;
            string catB = storageGroups[b][0].GetComponent<InspectableItem>().partCategory;
            int catCompare = string.Compare(catA, catB, System.StringComparison.OrdinalIgnoreCase);
            return catCompare != 0 ? catCompare : string.Compare(a, b, System.StringComparison.OrdinalIgnoreCase);
        });

        foreach (string key in storageKeys)
        {
            List<GameObject> group = storageGroups[key];
            GameObject firstObj = group[0];
            InspectableItem itemData = firstObj.GetComponent<InspectableItem>();

            GameObject newSlot = Instantiate(slotPrefab, slotsContainer);

            // Icon
            Transform iconParent = newSlot.transform.Find("Icon");
            if (iconParent != null)
            {
                Image img = iconParent.GetComponent<Image>();

                if (itemData.cachedShopIcon != null && img != null)
                {
                    img.sprite = itemData.cachedShopIcon;
                    img.enabled = true;
                }
                else if (itemData.itemIconPrefab != null)
                {
                    if (img != null) img.enabled = false;
                    GameObject spawnedIcon = Instantiate(itemData.itemIconPrefab, iconParent);
                    RectTransform rect = spawnedIcon.GetComponent<RectTransform>();
                    if (rect != null) { rect.anchoredPosition = Vector2.zero; rect.localScale = Vector3.one; }
                }
                else if (img != null)
                {
                    img.enabled = false; // hide blank white box
                }
            }

            // Name
            Transform nameTransform = newSlot.transform.Find("NameText");
            if (nameTransform != null)
            {
                TextMeshProUGUI slotNameText = nameTransform.GetComponent<TextMeshProUGUI>();
                if (slotNameText != null) slotNameText.text = itemData.itemName;
            }

            // Stack count badge (add if > 1)
            AddCountBadge(newSlot, group.Count);

            // Click → select the first item in the group
            GameObject capturedObj = firstObj;
            InspectableItem capturedData = itemData;
            Button btn = newSlot.GetComponent<Button>();
            btn.onClick.AddListener(() => SelectItem(capturedObj, capturedData));
        }

        // ==========================================
        // STEP 2 — Group shop inventory by item ID
        // ==========================================
        if (ShopSystem.Instance != null)
        {
            List<string> shopItemIDs = ShopSystem.Instance.GetInventoryIDs();

            // Count duplicates
            Dictionary<string, int> idCounts = new Dictionary<string, int>();
            foreach (string id in shopItemIDs)
            {
                if (!idCounts.ContainsKey(id)) idCounts[id] = 0;
                idCounts[id]++;
            }

            // Sort by category then name
            List<string> uniqueIDs = new List<string>(idCounts.Keys);
            uniqueIDs.Sort((a, b) =>
            {
                ItemData ia = ShopSystem.Instance.allAvailableItems.Find(x => x.id == a);
                ItemData ib = ShopSystem.Instance.allAvailableItems.Find(x => x.id == b);
                string catA = ia != null ? ia.category : "";
                string catB = ib != null ? ib.category : "";
                int catCompare = string.Compare(catA, catB, System.StringComparison.OrdinalIgnoreCase);
                if (catCompare != 0) return catCompare;
                string nameA = ia != null ? ia.itemName : a;
                string nameB = ib != null ? ib.itemName : b;
                return string.Compare(nameA, nameB, System.StringComparison.OrdinalIgnoreCase);
            });

            foreach (string id in uniqueIDs)
            {
                ItemData shopItem = ShopSystem.Instance.allAvailableItems.Find(x => x.id == id);
                if (shopItem == null || shopItem.prefabToPlace == null) continue;

                InspectableItem prefabData = shopItem.prefabToPlace.GetComponent<InspectableItem>();
                if (prefabData == null) continue;

                GameObject newSlot = Instantiate(slotPrefab, slotsContainer);

                // Icon
                Transform iconParent = newSlot.transform.Find("Icon");
                if (iconParent != null)
                {
                    Image img = iconParent.GetComponent<Image>();

                    if (shopItem.icon != null && img != null)
                    {
                        img.sprite = shopItem.icon;
                        img.enabled = true;
                    }
                    else if (prefabData.itemIconPrefab != null)
                    {
                        if (img != null) img.enabled = false;
                        GameObject spawnedIcon = Instantiate(prefabData.itemIconPrefab, iconParent);
                        RectTransform rect = spawnedIcon.GetComponent<RectTransform>();
                        if (rect != null) { rect.anchoredPosition = Vector2.zero; rect.localScale = Vector3.one; }
                    }
                    else if (img != null)
                    {
                        img.enabled = false; // hide blank white box
                    }
                }

                // Name — use full shop item name so "6GB" is never lost
                Transform nameTransform = newSlot.transform.Find("NameText");
                if (nameTransform != null)
                {
                    TextMeshProUGUI slotNameText = nameTransform.GetComponent<TextMeshProUGUI>();
                    if (slotNameText != null) slotNameText.text = shopItem.itemName;
                }

                // Stack count badge
                AddCountBadge(newSlot, idCounts[id]);

                // Click — capture loop variables properly
                ItemData capturedShopItem = shopItem;
                InspectableItem capturedPrefabData = prefabData;
                Button btn = newSlot.GetComponent<Button>();
                btn.onClick.AddListener(() => SelectShopItem(capturedShopItem, capturedPrefabData));
            }
        }
    }

    // ==========================================
    //  HELPER — Adds a count badge dynamically
    // ==========================================
    void AddCountBadge(GameObject slot, int count)
    {
        if (count <= 1) return; // no badge needed for single items

        // Create the badge GO on the slot
        GameObject badge = new GameObject("CountBadge");
        badge.transform.SetParent(slot.transform, false);

        // Background circle image
        Image badgeBG = badge.AddComponent<Image>();
        badgeBG.color = new Color(0.1f, 0.1f, 0.1f, 0.85f);

        RectTransform badgeRect = badge.GetComponent<RectTransform>();
        badgeRect.sizeDelta = new Vector2(22, 22);
        badgeRect.anchorMin = new Vector2(1f, 0f); // bottom-right corner
        badgeRect.anchorMax = new Vector2(1f, 0f);
        badgeRect.pivot = new Vector2(1f, 0f);
        badgeRect.anchoredPosition = new Vector2(-2f, 2f);

        // Count text inside the badge
        GameObject textGO = new GameObject("BadgeText");
        textGO.transform.SetParent(badge.transform, false);

        TextMeshProUGUI badgeText = textGO.AddComponent<TextMeshProUGUI>();
        badgeText.text = "x" + count;
        badgeText.fontSize = 10;
        badgeText.fontStyle = FontStyles.Bold;
        badgeText.color = Color.white;
        badgeText.alignment = TextAlignmentOptions.Center;

        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
    }
    // --- FOR PHYSICAL ITEMS ALREADY REMOVED FROM THE PC ---
   public void SelectItem(GameObject itemObj, InspectableItem itemData)
    {
        currentlySelectedItemObj = itemObj;
        currentlySelectedItemData = itemData;
        currentlySelectedShopItem = null;

        detailPanel.SetActive(true);
        detailCategoryText.text = itemData.partCategory;
        detailNameText.text = itemData.itemName;
        detailDescText.text = itemData.itemDescription;

        if (detailIcon != null)
        {
            foreach (Transform child in detailIcon.transform) Destroy(child.gameObject);

            if (itemData.cachedShopIcon != null)
            {
                // Use the 2D shop sprite that was cached when it was bought
                detailIcon.sprite = itemData.cachedShopIcon;
                detailIcon.enabled = true;
            }
            else if (itemData.itemIconPrefab != null)
            {
                detailIcon.enabled = false;
                GameObject detailIconObj = Instantiate(itemData.itemIconPrefab, detailIcon.transform);
                RectTransform rect = detailIconObj.GetComponent<RectTransform>();
                if (rect != null) { rect.anchoredPosition = Vector2.zero; rect.localScale = Vector3.one; }
            }
            else
            {
                // No icon at all — hide the Image so it doesn't show as a white box
                detailIcon.enabled = false;
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

        if (newPartData.partCategory == "Generic" || string.IsNullOrEmpty(newPartData.partCategory))
            newPartData.partCategory = currentlySelectedShopItem.category;

        newPartData.itemName        = currentlySelectedShopItem.itemName;
        newPartData.itemDescription = currentlySelectedShopItem.description;
        newPartData.cachedShopIcon  = currentlySelectedShopItem.icon;

        inspectManager.playerStorage.Add(newPartObj);
        ShopSystem.Instance.GetInventoryIDs().Remove(currentlySelectedShopItem.id);
        inspectManager.PrepareInstallationFromUI(newPartObj, newPartData);
        ToggleInventory();
    }
}
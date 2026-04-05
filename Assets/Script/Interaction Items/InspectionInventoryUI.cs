using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class InspectionInventoryUI : MonoBehaviour
{
    // =============================================
    //  INVENTORY MODE
    //  PlayerStorage = Tab during inspection (parts player is carrying)
    //  ShelfStorage  = E at shelf (items sitting on the storage shelf)
    // =============================================
    public enum InventoryMode
    {
        PlayerStorage,
        ShelfStorage
    }

    [HideInInspector] public InventoryMode currentMode = InventoryMode.PlayerStorage;

    [Header("Panels")]
    public GameObject inventoryPanel;
    public GameObject detailPanel;

    [Header("Inventory Grid (Right Side)")]
    public Transform slotsContainer;
    public GameObject slotPrefab;

    [Header("Detail View (Middle)")]
    public TextMeshProUGUI detailCategoryText;
    public TextMeshProUGUI detailNameText;
    public TextMeshProUGUI detailDescText;
    public Image detailIcon;
    public Button actionButton;
    public TextMeshProUGUI actionButtonText;

    private InspectionManager inspectManager;

    private GameObject currentlySelectedItemObj;
    private InspectableItem currentlySelectedItemData;
    private ItemData currentlySelectedShopItem;

    void Start()
    {
        inspectManager = FindObjectOfType<InspectionManager>();
        inventoryPanel.SetActive(false);
        detailPanel.SetActive(false);
    }

    // =============================================
    //  TOGGLE — called by Tab key during inspection
    //  Always opens in PlayerStorage mode
    // =============================================
    public void ToggleInventory()
    {
        if (inspectManager == null)
            inspectManager = FindObjectOfType<InspectionManager>();

        // ── Always set to PlayerStorage when opened via Tab during inspection
        currentMode = InventoryMode.PlayerStorage;

        bool isOpening = !inventoryPanel.activeSelf;
        inventoryPanel.SetActive(isOpening);

        if (isOpening)
        {
            RefreshInventory();
            if (detailPanel != null) detailPanel.SetActive(false);
        }
    }

    // =============================================
    //  REFRESH — routes to correct method by mode
    // =============================================
    public void RefreshInventory()
    {
        foreach (Transform child in slotsContainer)
            Destroy(child.gameObject);

        if (currentMode == InventoryMode.PlayerStorage)
            RefreshPlayerStorage();
        else
            RefreshShelfStorage();
    }

    // =============================================
    //  PLAYER STORAGE MODE
    //  Shows ONLY parts the player is carrying
    //  (Tab key during PC inspection)
    // =============================================
    void RefreshPlayerStorage()
    {
        if (inspectManager == null) return;

        // Group playerStorage by itemName
        Dictionary<string, List<GameObject>> storageGroups
            = new Dictionary<string, List<GameObject>>();

        foreach (GameObject itemObj in inspectManager.playerStorage)
        {
            InspectableItem itemData = itemObj.GetComponent<InspectableItem>();
            if (itemData == null) continue;

            string key = itemData.itemName;
            if (!storageGroups.ContainsKey(key))
                storageGroups[key] = new List<GameObject>();
            storageGroups[key].Add(itemObj);
        }

        // Sort by category then name
        List<string> storageKeys = new List<string>(storageGroups.Keys);
        storageKeys.Sort((a, b) =>
        {
            string catA = storageGroups[a][0].GetComponent<InspectableItem>().partCategory;
            string catB = storageGroups[b][0].GetComponent<InspectableItem>().partCategory;
            int catCompare = string.Compare(catA, catB, System.StringComparison.OrdinalIgnoreCase);
            return catCompare != 0 ? catCompare
                 : string.Compare(a, b, System.StringComparison.OrdinalIgnoreCase);
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
                    if (rect != null)
                    { rect.anchoredPosition = Vector2.zero; rect.localScale = Vector3.one; }
                }
                else if (img != null) img.enabled = false;
            }

            // Name
            Transform nameTransform = newSlot.transform.Find("NameText");
            if (nameTransform != null)
            {
                TextMeshProUGUI slotNameText = nameTransform.GetComponent<TextMeshProUGUI>();
                if (slotNameText != null) slotNameText.text = itemData.itemName;
            }

            AddCountBadge(newSlot, group.Count);

            GameObject capturedObj = firstObj;
            InspectableItem capturedData = itemData;
            Button btn = newSlot.GetComponent<Button>();
            btn.onClick.AddListener(() => SelectItem(capturedObj, capturedData));
        }
    }

    // =============================================
    //  SHELF STORAGE MODE
    //  Shows ONLY items sitting on the storage shelf
    //  (E key at the storage shelf)
    // =============================================
    void RefreshShelfStorage()
    {
        if (ShopSystem.Instance == null) return;

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
                    if (rect != null)
                    { rect.anchoredPosition = Vector2.zero; rect.localScale = Vector3.one; }
                }
                else if (img != null) img.enabled = false;
            }

            // Name
            Transform nameTransform = newSlot.transform.Find("NameText");
            if (nameTransform != null)
            {
                TextMeshProUGUI slotNameText = nameTransform.GetComponent<TextMeshProUGUI>();
                if (slotNameText != null) slotNameText.text = shopItem.itemName;
            }

            AddCountBadge(newSlot, idCounts[id]);

            ItemData capturedShopItem = shopItem;
            InspectableItem capturedPrefabData = prefabData;
            Button btn = newSlot.GetComponent<Button>();
            btn.onClick.AddListener(() => SelectShopItem(capturedShopItem, capturedPrefabData));
        }
    }

    // =============================================
    //  HELPER — Count badge (x2, x3 etc.)
    // =============================================
    void AddCountBadge(GameObject slot, int count)
    {
        if (count <= 1) return;

        GameObject badge = new GameObject("CountBadge");
        badge.transform.SetParent(slot.transform, false);

        Image badgeBG = badge.AddComponent<Image>();
        badgeBG.color = new Color(0.1f, 0.1f, 0.1f, 0.85f);

        RectTransform badgeRect = badge.GetComponent<RectTransform>();
        badgeRect.sizeDelta = new Vector2(22, 22);
        badgeRect.anchorMin = new Vector2(1f, 0f);
        badgeRect.anchorMax = new Vector2(1f, 0f);
        badgeRect.pivot = new Vector2(1f, 0f);
        badgeRect.anchoredPosition = new Vector2(-2f, 2f);

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

    // =============================================
    //  SELECT ITEM — physical part player is carrying
    // =============================================
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
                detailIcon.enabled = false;
            }
        }

        // In PlayerStorage mode → button installs into PC
        if (actionButtonText != null) actionButtonText.text = "Add to PC";
        actionButton.onClick.RemoveAllListeners();
        actionButton.onClick.AddListener(TryInstallSelected);
    }

    // =============================================
    //  SELECT SHOP ITEM — item from the shelf
    // =============================================
    public void SelectShopItem(ItemData shopItem, InspectableItem prefabData)
    {
        currentlySelectedItemObj = null;
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

        // In ShelfStorage mode → button takes item to player inventory
        if (actionButtonText != null) actionButtonText.text = "Take Item";
        actionButton.onClick.RemoveAllListeners();
        actionButton.onClick.AddListener(TryInstallShopItem);
    }

    // =============================================
    //  INSTALL SELECTED — part player is carrying → into PC
    //  Only works during inspection
    // =============================================
    void TryInstallSelected()
    {
        if (inspectManager == null) return;
        inspectManager.PrepareInstallationFromUI(currentlySelectedItemObj, currentlySelectedItemData);
        ToggleInventory();
    }

    // =============================================
    //  TAKE SHOP ITEM — shelf item → player inventory
    //  Works from shelf (no inspection needed)
    //  Also works during inspection to install directly
    // =============================================
    void TryInstallShopItem()
    {
        if (currentlySelectedShopItem == null) return;
        if (inspectManager == null)
            inspectManager = FindObjectOfType<InspectionManager>();

        GameObject newPartObj = Instantiate(currentlySelectedShopItem.prefabToPlace);
        newPartObj.SetActive(false);

        InspectableItem newPartData = newPartObj.GetComponent<InspectableItem>();
        if (newPartData == null)
        {
            Destroy(newPartObj);
            return;
        }

        if (newPartData.partCategory == "Generic" || string.IsNullOrEmpty(newPartData.partCategory))
            newPartData.partCategory = currentlySelectedShopItem.category;

        newPartData.itemName = currentlySelectedShopItem.itemName;
        newPartData.itemDescription = currentlySelectedShopItem.description;
        newPartData.cachedShopIcon = currentlySelectedShopItem.icon;

        inspectManager.playerStorage.Add(newPartObj);

        ShopSystem.Instance.GetInventoryIDs().Remove(currentlySelectedShopItem.id);

        InventorySystem.HideFromWorld(newPartObj);

        // ── NEW — Notify tutorial ──
        if (TutorialManager.Instance != null)
            TutorialManager.Instance.NotifyStoragePartGrabbed(newPartData.partCategory);

        if (InventorySystem.Instance != null)
            InventorySystem.Instance.NotifyInstalled();

        if (inspectManager.isInspecting)
            inspectManager.PrepareInstallationFromUI(newPartObj, newPartData);

        if (inventoryPanel != null)
            inventoryPanel.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
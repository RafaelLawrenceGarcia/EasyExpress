// ============================================================
//  InventoryUIManager.cs
//  Easy Express – Unified Inventory UI
// ============================================================
//  Handles BOTH Storage mode (at the shelf) and Inspection mode
//  (Tab key during PC inspection). Features:
//    - Category tabs with counts (All, GPU, RAM, CPU, etc.)
//    - Owner tabs (tracks which customer's PC parts came from)
//    - Search bar filtering
//    - Detail panel with specs, fault status, owner info
//    - Install into PC (inspection) / Add to Inventory (storage)
//    - Delete item function
//
//  SETUP:
//    Run EasyExpress > Create Inventory UI in the Unity menu bar.
//    The editor script spawns the entire canvas and wires this up.
// ============================================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class InventoryUIManager : MonoBehaviour
{
    // ═══════════════════════════════════════════════════════════
    //  MODE
    // ═══════════════════════════════════════════════════════════
    public enum InventoryMode { Storage, Inspection }

    [HideInInspector] public InventoryMode currentMode = InventoryMode.Storage;

    // ═══════════════════════════════════════════════════════════
    //  UI REFERENCES (auto-wired by InventoryUIBuilder)
    // ═══════════════════════════════════════════════════════════

    [Header("Root")]
    public GameObject inventoryPanel;

    [Header("Header")]
    public Button storageModeBtn;
    public Button inspectionModeBtn;
    public Button closeBtn;

    [Header("Sidebar - Category Tabs")]
    public Transform categoryTabContainer;
    public GameObject categoryTabPrefab;

    [Header("Sidebar - Owner Tabs")]
    public Transform ownerTabContainer;
    public GameObject ownerTabPrefab;

    [Header("Main Grid")]
    public TMP_InputField searchInput;
    public Transform gridContainer;
    public GameObject slotPrefab;

    [Header("Detail Panel")]
    public GameObject detailPanel;
    public Image detailIcon;
    public TextMeshProUGUI detailCategoryText;
    public TextMeshProUGUI detailNameText;
    public TextMeshProUGUI detailDescText;
    public Transform detailSpecsContainer;
    public TextMeshProUGUI detailOwnerText;
    public Button actionBtn;
    public TextMeshProUGUI actionBtnText;
    public Button deleteBtn;

    [Header("Prefabs")]
    public GameObject specTagPrefab;

    [Header("Colors")]
    public Color tabActiveColor = new Color(0.29f, 0.56f, 0.85f, 0.15f);
    public Color tabActiveBorderColor = new Color(0.42f, 0.71f, 0.97f, 1f);
    public Color tabActiveTextColor = new Color(0.42f, 0.71f, 0.97f, 1f);
    public Color tabInactiveTextColor = new Color(1f, 1f, 1f, 0.35f);
    public Color slotSelectedBorder = new Color(0.42f, 0.71f, 0.97f, 1f);
    public Color slotNormalBorder = new Color(1f, 1f, 1f, 0.06f);
    public Color ownerTagColor = new Color(0.85f, 0.35f, 0.18f, 0.25f);
    public Color ownerTagTextColor = new Color(0.94f, 0.60f, 0.49f, 1f);
    public Color faultTagColor = new Color(0.88f, 0.29f, 0.29f, 0.12f);
    public Color faultTagTextColor = new Color(0.94f, 0.58f, 0.58f, 1f);

    // ═══════════════════════════════════════════════════════════
    //  RUNTIME STATE
    // ═══════════════════════════════════════════════════════════

    private string activeCategory = "All";
    private string activeOwner = "";
    private string searchTerm = "";

    private InspectionManager inspectionManager;
    private List<InventoryEntry> currentEntries = new List<InventoryEntry>();
    private InventoryEntry selectedEntry = null;

    // Category definitions with colors
    private static readonly string[] CATEGORIES = {
        "All", "GPU", "RAM", "CPU", "Motherboard", "PSU", "Storage", "Cooler", "Fan"
    };

    private static readonly Dictionary<string, Color> CAT_COLORS = new Dictionary<string, Color>
    {
        { "All",         new Color(0.42f, 0.71f, 0.97f) },
        { "GPU",         new Color(0.85f, 0.35f, 0.19f) },
        { "RAM",         new Color(0.11f, 0.62f, 0.46f) },
        { "CPU",         new Color(0.83f, 0.33f, 0.49f) },
        { "Motherboard", new Color(0.22f, 0.54f, 0.87f) },
        { "PSU",         new Color(0.53f, 0.53f, 0.50f) },
        { "Storage",     new Color(0.50f, 0.47f, 0.87f) },
        { "Cooler",      new Color(0.73f, 0.46f, 0.09f) },
        { "Fan",         new Color(0.36f, 0.79f, 0.65f) },
    };

    // ═══════════════════════════════════════════════════════════
    //  INVENTORY ENTRY — unified wrapper for both modes
    // ═══════════════════════════════════════════════════════════

    private class InventoryEntry
    {
        public string itemName;
        public string category;
        public string description;
        public string ownerName;
        public Sprite icon;
        public string[] compatTags;
        public float powerDraw;
        public float maxWattage;
        public PartFault fault;
        public string faultDescription;
        public int count;

        // Source references
        public List<GameObject> sourceObjects;  // for playerStorage
        public ItemData shopItemData;           // for ShopSystem
    }

    // ═══════════════════════════════════════════════════════════
    //  LIFECYCLE
    // ═══════════════════════════════════════════════════════════

    void Start()
    {
        inspectionManager = FindFirstObjectByType<InspectionManager>();

        if (inventoryPanel != null) inventoryPanel.SetActive(false);
        if (detailPanel != null) detailPanel.SetActive(false);

        if (closeBtn != null) closeBtn.onClick.AddListener(Close);
        if (storageModeBtn != null) storageModeBtn.onClick.AddListener(() => SetMode(InventoryMode.Storage));
        if (inspectionModeBtn != null) inspectionModeBtn.onClick.AddListener(() => SetMode(InventoryMode.Inspection));
        if (searchInput != null) searchInput.onValueChanged.AddListener(OnSearchChanged);
        if (deleteBtn != null) deleteBtn.onClick.AddListener(DeleteSelected);
        if (actionBtn != null) actionBtn.onClick.AddListener(OnActionButton);
    }

    // ═══════════════════════════════════════════════════════════
    //  PUBLIC API
    // ═══════════════════════════════════════════════════════════

    public void OpenStorage()
    {
        currentMode = InventoryMode.Storage;
        Open();
    }

    public void OpenInspection()
    {
        currentMode = InventoryMode.Inspection;
        Open();
    }

    public void Open()
    {
        if (inspectionManager == null)
            inspectionManager = FindFirstObjectByType<InspectionManager>();

        if (inventoryPanel != null) inventoryPanel.SetActive(true);
        if (detailPanel != null) detailPanel.SetActive(false);

        activeCategory = "All";
        activeOwner = "";
        searchTerm = "";
        selectedEntry = null;

        if (searchInput != null) searchInput.SetTextWithoutNotify("");

        UpdateModeButtons();
        Refresh();
    }

    public void Close()
    {
        if (inventoryPanel != null) inventoryPanel.SetActive(false);
        selectedEntry = null;
    }

    public bool IsOpen()
    {
        return inventoryPanel != null && inventoryPanel.activeSelf;
    }

    public void Refresh()
    {
        BuildEntries();
        RefreshCategoryTabs();
        RefreshOwnerTabs();
        RefreshGrid();

        if (selectedEntry != null)
            RefreshDetailPanel();
    }

    // ═══════════════════════════════════════════════════════════
    //  MODE SWITCHING
    // ═══════════════════════════════════════════════════════════

    public void SetMode(InventoryMode mode)
    {
        currentMode = mode;
        activeCategory = "All";
        activeOwner = "";
        selectedEntry = null;
        if (detailPanel != null) detailPanel.SetActive(false);
        UpdateModeButtons();
        Refresh();
    }

    void UpdateModeButtons()
    {
        if (storageModeBtn != null)
        {
            Image img = storageModeBtn.GetComponent<Image>();
            TextMeshProUGUI txt = storageModeBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (currentMode == InventoryMode.Storage)
            {
                if (img != null) img.color = tabActiveColor;
                if (txt != null) txt.color = tabActiveTextColor;
            }
            else
            {
                if (img != null) img.color = Color.clear;
                if (txt != null) txt.color = tabInactiveTextColor;
            }
        }

        if (inspectionModeBtn != null)
        {
            Image img = inspectionModeBtn.GetComponent<Image>();
            TextMeshProUGUI txt = inspectionModeBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (currentMode == InventoryMode.Inspection)
            {
                if (img != null) img.color = tabActiveColor;
                if (txt != null) txt.color = tabActiveTextColor;
            }
            else
            {
                if (img != null) img.color = Color.clear;
                if (txt != null) txt.color = tabInactiveTextColor;
            }
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  BUILD ENTRIES — collect items from the right source
    // ═══════════════════════════════════════════════════════════

    void BuildEntries()
    {
        currentEntries.Clear();

        if (currentMode == InventoryMode.Inspection)
            BuildFromPlayerStorage();
        else
            BuildFromShopSystem();
    }

    void BuildFromPlayerStorage()
    {
        if (inspectionManager == null) return;

        // Group by itemName + ownerName
        Dictionary<string, InventoryEntry> groups = new Dictionary<string, InventoryEntry>();

        foreach (GameObject obj in inspectionManager.playerStorage)
        {
            if (obj == null) continue;
            InspectableItem item = obj.GetComponent<InspectableItem>();
            if (item == null) continue;

            string owner = !string.IsNullOrEmpty(item.sourceOwner) ? item.sourceOwner : "";
            string key = item.itemName + "|" + owner + "|" + (int)item.fault;

            if (groups.ContainsKey(key))
            {
                groups[key].count++;
                groups[key].sourceObjects.Add(obj);
            }
            else
            {
                InventoryEntry entry = new InventoryEntry();
                entry.itemName = item.itemName;
                entry.category = item.partCategory;
                entry.description = item.itemDescription;
                entry.ownerName = owner;
                entry.icon = item.cachedShopIcon;
                entry.compatTags = item.compatTags;
                entry.powerDraw = item.powerDraw;
                entry.maxWattage = item.maxWattage;
                entry.fault = item.fault;
                entry.faultDescription = item.faultDescription;
                entry.count = 1;
                entry.sourceObjects = new List<GameObject> { obj };
                entry.shopItemData = null;
                groups[key] = entry;
            }
        }

        currentEntries = groups.Values.ToList();
        SortEntries();
    }

    void BuildFromShopSystem()
    {
        if (ShopSystem.Instance == null) return;

        List<string> ids = ShopSystem.Instance.GetInventoryIDs();
        Dictionary<string, int> idCounts = new Dictionary<string, int>();
        foreach (string id in ids)
        {
            if (!idCounts.ContainsKey(id)) idCounts[id] = 0;
            idCounts[id]++;
        }

        foreach (var kvp in idCounts)
        {
            ItemData shopItem = ShopSystem.Instance.allAvailableItems.Find(x => x != null && x.id == kvp.Key);
            if (shopItem == null) continue;

            InspectableItem prefabData = null;
            if (shopItem.prefabToPlace != null)
                prefabData = shopItem.prefabToPlace.GetComponent<InspectableItem>();

            InventoryEntry entry = new InventoryEntry();
            entry.itemName = shopItem.itemName;
            entry.category = shopItem.category ?? (prefabData != null ? prefabData.partCategory : "");
            entry.description = shopItem.description;
            entry.ownerName = "";
            entry.icon = shopItem.icon;
            entry.compatTags = prefabData != null ? prefabData.compatTags : null;
            entry.powerDraw = prefabData != null ? prefabData.powerDraw : 0f;
            entry.maxWattage = prefabData != null ? prefabData.maxWattage : 0f;
            entry.fault = PartFault.None;
            entry.faultDescription = "";
            entry.count = kvp.Value;
            entry.sourceObjects = null;
            entry.shopItemData = shopItem;
            currentEntries.Add(entry);
        }

        SortEntries();
    }

    void SortEntries()
    {
        currentEntries.Sort((a, b) =>
        {
            int catCompare = string.Compare(a.category, b.category, System.StringComparison.OrdinalIgnoreCase);
            return catCompare != 0 ? catCompare
                 : string.Compare(a.itemName, b.itemName, System.StringComparison.OrdinalIgnoreCase);
        });
    }

    // ═══════════════════════════════════════════════════════════
    //  FILTER
    // ═══════════════════════════════════════════════════════════

    List<InventoryEntry> GetFilteredEntries()
    {
        return currentEntries.Where(e =>
        {
            if (activeCategory != "All" && !string.Equals(e.category, activeCategory, System.StringComparison.OrdinalIgnoreCase))
                return false;

            if (!string.IsNullOrEmpty(activeOwner))
            {
                if (activeOwner == "__shop__" && !string.IsNullOrEmpty(e.ownerName))
                    return false;
                if (activeOwner != "__shop__" && e.ownerName != activeOwner)
                    return false;
            }

            if (!string.IsNullOrEmpty(searchTerm))
            {
                string lower = searchTerm.ToLower();
                if (!e.itemName.ToLower().Contains(lower)
                    && !e.category.ToLower().Contains(lower))
                    return false;
            }

            return true;
        }).ToList();
    }

    void OnSearchChanged(string value)
    {
        searchTerm = value;
        RefreshGrid();
    }

    // ═══════════════════════════════════════════════════════════
    //  CATEGORY TABS
    // ═══════════════════════════════════════════════════════════

    void RefreshCategoryTabs()
    {
        if (categoryTabContainer == null) return;
        foreach (Transform child in categoryTabContainer) Destroy(child.gameObject);

        foreach (string cat in CATEGORIES)
        {
            int count = cat == "All"
                ? currentEntries.Count
                : currentEntries.Count(e => string.Equals(e.category, cat, System.StringComparison.OrdinalIgnoreCase));

            if (cat != "All" && count == 0) continue;

            GameObject tab = categoryTabPrefab != null
                ? Instantiate(categoryTabPrefab, categoryTabContainer)
                : CreateTabObject(categoryTabContainer);

            bool isActive = (activeCategory == cat);
            SetupTab(tab, cat, count, isActive, CAT_COLORS.ContainsKey(cat) ? CAT_COLORS[cat] : Color.gray);

            string capturedCat = cat;
            Button btn = tab.GetComponent<Button>();
            if (btn == null) btn = tab.AddComponent<Button>();
            btn.onClick.AddListener(() =>
            {
                activeCategory = capturedCat;
                activeOwner = "";
                selectedEntry = null;
                if (detailPanel != null) detailPanel.SetActive(false);
                RefreshCategoryTabs();
                RefreshOwnerTabs();
                RefreshGrid();
            });
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  OWNER TABS
    // ═══════════════════════════════════════════════════════════

    void RefreshOwnerTabs()
    {
        if (ownerTabContainer == null) return;
        foreach (Transform child in ownerTabContainer) Destroy(child.gameObject);

        // Collect unique owners
        Dictionary<string, int> owners = new Dictionary<string, int>();
        int shopCount = 0;

        foreach (InventoryEntry e in currentEntries)
        {
            if (string.IsNullOrEmpty(e.ownerName))
            {
                shopCount += e.count;
            }
            else
            {
                if (!owners.ContainsKey(e.ownerName)) owners[e.ownerName] = 0;
                owners[e.ownerName] += e.count;
            }
        }

        // Owner entries
        foreach (var kvp in owners)
        {
            GameObject tab = ownerTabPrefab != null
                ? Instantiate(ownerTabPrefab, ownerTabContainer)
                : CreateTabObject(ownerTabContainer);

            bool isActive = (activeOwner == kvp.Key);
            SetupOwnerTab(tab, kvp.Key + "'s PC", kvp.Value, isActive);

            string capturedOwner = kvp.Key;
            Button btn = tab.GetComponent<Button>();
            if (btn == null) btn = tab.AddComponent<Button>();
            btn.onClick.AddListener(() =>
            {
                activeOwner = capturedOwner;
                activeCategory = "All";
                selectedEntry = null;
                if (detailPanel != null) detailPanel.SetActive(false);
                RefreshCategoryTabs();
                RefreshOwnerTabs();
                RefreshGrid();
            });
        }

        // Shop stock
        if (shopCount > 0)
        {
            GameObject tab = ownerTabPrefab != null
                ? Instantiate(ownerTabPrefab, ownerTabContainer)
                : CreateTabObject(ownerTabContainer);

            bool isActive = (activeOwner == "__shop__");
            SetupOwnerTab(tab, "Shop stock", shopCount, isActive);

            Button btn = tab.GetComponent<Button>();
            if (btn == null) btn = tab.AddComponent<Button>();
            btn.onClick.AddListener(() =>
            {
                activeOwner = "__shop__";
                activeCategory = "All";
                selectedEntry = null;
                if (detailPanel != null) detailPanel.SetActive(false);
                RefreshCategoryTabs();
                RefreshOwnerTabs();
                RefreshGrid();
            });
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  GRID
    // ═══════════════════════════════════════════════════════════

    void RefreshGrid()
    {
        if (gridContainer == null) return;
        foreach (Transform child in gridContainer) Destroy(child.gameObject);

        List<InventoryEntry> filtered = GetFilteredEntries();

        foreach (InventoryEntry entry in filtered)
        {
            GameObject slot = slotPrefab != null
                ? Instantiate(slotPrefab, gridContainer)
                : CreateSlotObject(gridContainer);

            SetupSlot(slot, entry);

            InventoryEntry capturedEntry = entry;
            Button btn = slot.GetComponent<Button>();
            if (btn == null) btn = slot.AddComponent<Button>();
            btn.onClick.AddListener(() => SelectEntry(capturedEntry, slot));
        }
    }

    void SelectEntry(InventoryEntry entry, GameObject slot)
    {
        selectedEntry = entry;
        RefreshDetailPanel();
        RefreshGrid(); // re-highlight
    }

    // ═══════════════════════════════════════════════════════════
    //  DETAIL PANEL
    // ═══════════════════════════════════════════════════════════

    void RefreshDetailPanel()
    {
        if (detailPanel == null || selectedEntry == null) return;
        detailPanel.SetActive(true);

        if (detailIcon != null)
        {
            if (selectedEntry.icon != null)
            {
                detailIcon.sprite = selectedEntry.icon;
                detailIcon.enabled = true;
                detailIcon.color = Color.white;
            }
            else
            {
                detailIcon.enabled = false;
            }
        }

        if (detailCategoryText != null)
            detailCategoryText.text = selectedEntry.category;

        if (detailNameText != null)
        {
            string nameStr = selectedEntry.itemName;
            if (selectedEntry.count > 1) nameStr += $" (x{selectedEntry.count})";
            detailNameText.text = nameStr;
        }

        if (detailDescText != null)
            detailDescText.text = selectedEntry.description;

        // Specs
        if (detailSpecsContainer != null)
        {
            foreach (Transform child in detailSpecsContainer) Destroy(child.gameObject);

            if (selectedEntry.compatTags != null)
            {
                foreach (string tag in selectedEntry.compatTags)
                {
                    if (string.IsNullOrEmpty(tag)) continue;
                    CreateSpecTag(tag, new Color(1f, 1f, 1f, 0.05f), new Color(1f, 1f, 1f, 0.5f));
                }
            }

            if (selectedEntry.powerDraw > 0)
                CreateSpecTag($"{selectedEntry.powerDraw}W", new Color(0.83f, 0.66f, 0.09f, 0.12f), new Color(0.83f, 0.66f, 0.09f, 1f));

            if (selectedEntry.maxWattage > 0)
                CreateSpecTag($"Max {selectedEntry.maxWattage}W", new Color(0.29f, 0.88f, 0.29f, 0.12f), new Color(0.29f, 0.88f, 0.29f, 1f));

            if (selectedEntry.fault != PartFault.None)
                CreateSpecTag(selectedEntry.fault.ToString(), faultTagColor, faultTagTextColor);
        }

        // Owner
        if (detailOwnerText != null)
        {
            if (!string.IsNullOrEmpty(selectedEntry.ownerName))
                detailOwnerText.text = "Owner: <b>" + selectedEntry.ownerName + "</b>";
            else
                detailOwnerText.text = "Owner: <b>Shop stock</b>";
        }

        // Action button text
        if (actionBtnText != null)
        {
            if (currentMode == InventoryMode.Inspection)
                actionBtnText.text = "Install into PC";
            else
                actionBtnText.text = "Add to Inventory";
        }
    }

    void CreateSpecTag(string text, Color bgColor, Color textColor)
    {
        if (detailSpecsContainer == null) return;

        GameObject tag = new GameObject("SpecTag", typeof(RectTransform), typeof(Image));
        tag.transform.SetParent(detailSpecsContainer, false);

        Image bg = tag.GetComponent<Image>();
        bg.color = bgColor;

        RectTransform rt = tag.GetComponent<RectTransform>();
        LayoutElement le = tag.AddComponent<LayoutElement>();
        le.preferredHeight = 22;

        // Auto-size with ContentSizeFitter
        HorizontalLayoutGroup hlg = tag.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(8, 8, 2, 2);
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;

        ContentSizeFitter csf = tag.AddComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

        GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(tag.transform, false);

        TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 11;
        tmp.color = textColor;
        tmp.enableWordWrapping = false;
    }

    // ═══════════════════════════════════════════════════════════
    //  ACTIONS
    // ═══════════════════════════════════════════════════════════

    void OnActionButton()
    {
        if (selectedEntry == null) return;

        if (currentMode == InventoryMode.Inspection)
            InstallSelected();
        else
            AddToInventory();
    }

    void InstallSelected()
    {
        if (selectedEntry == null || inspectionManager == null) return;

        if (selectedEntry.sourceObjects != null && selectedEntry.sourceObjects.Count > 0)
        {
            GameObject partObj = selectedEntry.sourceObjects[0];
            InspectableItem partData = partObj.GetComponent<InspectableItem>();
            if (partData != null)
            {
                inspectionManager.PrepareInstallationFromUI(partObj, partData);
                Close();
            }
        }
    }

    void AddToInventory()
    {
        if (selectedEntry == null || selectedEntry.shopItemData == null) return;
        if (inspectionManager == null)
            inspectionManager = FindFirstObjectByType<InspectionManager>();
        if (inspectionManager == null) return;

        ItemData shopItem = selectedEntry.shopItemData;
        GameObject newPartObj = Instantiate(shopItem.prefabToPlace);
        newPartObj.SetActive(false);

        InspectableItem newPartData = newPartObj.GetComponent<InspectableItem>();
        if (newPartData == null) { Destroy(newPartObj); return; }

        if (string.IsNullOrEmpty(newPartData.partCategory) || newPartData.partCategory == "Generic")
            newPartData.partCategory = shopItem.category;

        newPartData.itemName = shopItem.itemName;
        newPartData.itemDescription = shopItem.description;
        newPartData.cachedShopIcon = shopItem.icon;

        inspectionManager.playerStorage.Add(newPartObj);
        ShopSystem.Instance.GetInventoryIDs().Remove(shopItem.id);
        InventorySystem.HideFromWorld(newPartObj);

        if (TutorialManager.Instance != null)
            TutorialManager.Instance.NotifyStoragePartGrabbed(newPartData.partCategory);

        if (InventorySystem.Instance != null)
            InventorySystem.Instance.NotifyInstalled();

        // If in inspection, also prepare for installation
        if (inspectionManager.isInspecting)
        {
            inspectionManager.PrepareInstallationFromUI(newPartObj, newPartData);
            Close();
        }
        else
        {
            Refresh();
        }
    }

    void DeleteSelected()
    {
        if (selectedEntry == null) return;

        if (currentMode == InventoryMode.Inspection)
        {
            // Remove from playerStorage
            if (selectedEntry.sourceObjects != null && selectedEntry.sourceObjects.Count > 0)
            {
                GameObject obj = selectedEntry.sourceObjects[0];
                if (inspectionManager != null)
                    inspectionManager.playerStorage.Remove(obj);
                if (obj != null) Destroy(obj);
            }
        }
        else
        {
            // Remove from ShopSystem
            if (selectedEntry.shopItemData != null && ShopSystem.Instance != null)
                ShopSystem.Instance.GetInventoryIDs().Remove(selectedEntry.shopItemData.id);
        }

        selectedEntry = null;
        if (detailPanel != null) detailPanel.SetActive(false);

        if (InventorySystem.Instance != null)
            InventorySystem.Instance.NotifyInstalled();

        Refresh();
    }

    // ═══════════════════════════════════════════════════════════
    //  TAB / SLOT SETUP HELPERS
    // ═══════════════════════════════════════════════════════════

    void SetupTab(GameObject tab, string label, int count, bool isActive, Color dotColor)
    {
        Image bg = tab.GetComponent<Image>();
        if (bg != null)
        {
            bg.color = isActive ? tabActiveColor : Color.clear;
        }

        // Find or create children
        Transform dotT = tab.transform.Find("Dot");
        Transform labelT = tab.transform.Find("Label");
        Transform countT = tab.transform.Find("Count");

        if (dotT != null)
        {
            Image dotImg = dotT.GetComponent<Image>();
            if (dotImg != null) dotImg.color = dotColor;
        }

        if (labelT != null)
        {
            TextMeshProUGUI tmp = labelT.GetComponent<TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.text = label;
                tmp.color = isActive ? tabActiveTextColor : tabInactiveTextColor;
            }
        }

        if (countT != null)
        {
            TextMeshProUGUI tmp = countT.GetComponent<TextMeshProUGUI>();
            if (tmp != null) tmp.text = count.ToString();
        }

        // Border indicator
        Transform borderT = tab.transform.Find("ActiveBorder");
        if (borderT != null) borderT.gameObject.SetActive(isActive);
    }

    void SetupOwnerTab(GameObject tab, string label, int count, bool isActive)
    {
        SetupTab(tab, label, count, isActive, ownerTagTextColor);
    }

    void SetupSlot(GameObject slot, InventoryEntry entry)
    {
        bool isSelected = (selectedEntry == entry);

        // Border
        Image slotBg = slot.GetComponent<Image>();
        if (slotBg != null)
        {
            Outline outline = slot.GetComponent<Outline>();
            if (outline == null) outline = slot.AddComponent<Outline>();
            outline.effectColor = isSelected ? slotSelectedBorder : slotNormalBorder;
            outline.effectDistance = new Vector2(1, 1);
        }

        // Icon
        Transform iconT = slot.transform.Find("Icon");
        if (iconT != null)
        {
            Image img = iconT.GetComponent<Image>();
            if (img != null)
            {
                if (entry.icon != null)
                {
                    img.sprite = entry.icon;
                    img.enabled = true;
                    img.color = Color.white;
                }
                else
                {
                    img.enabled = false;
                }
            }
        }

        // Name
        Transform nameT = slot.transform.Find("Name");
        if (nameT != null)
        {
            TextMeshProUGUI tmp = nameT.GetComponent<TextMeshProUGUI>();
            if (tmp != null) tmp.text = entry.itemName;
        }

        // Count badge
        Transform badgeT = slot.transform.Find("CountBadge");
        if (badgeT != null)
        {
            badgeT.gameObject.SetActive(entry.count > 1);
            TextMeshProUGUI tmp = badgeT.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null) tmp.text = "x" + entry.count;
        }

        // Owner tag
        Transform ownerT = slot.transform.Find("OwnerTag");
        if (ownerT != null)
        {
            bool hasOwner = !string.IsNullOrEmpty(entry.ownerName);
            ownerT.gameObject.SetActive(hasOwner);
            if (hasOwner)
            {
                TextMeshProUGUI tmp = ownerT.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp != null) tmp.text = entry.ownerName;
            }
        }

        // Fault indicator
        Transform faultT = slot.transform.Find("FaultIndicator");
        if (faultT != null)
        {
            faultT.gameObject.SetActive(entry.fault != PartFault.None);
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  FALLBACK OBJECT CREATORS (if no prefab assigned)
    // ═══════════════════════════════════════════════════════════

    GameObject CreateTabObject(Transform parent)
    {
        GameObject tab = new GameObject("Tab", typeof(RectTransform), typeof(Image), typeof(Button));
        tab.transform.SetParent(parent, false);
        tab.GetComponent<Image>().color = Color.clear;

        LayoutElement le = tab.AddComponent<LayoutElement>();
        le.preferredHeight = 36;
        le.flexibleWidth = 1;

        HorizontalLayoutGroup hlg = tab.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(12, 12, 0, 0);
        hlg.spacing = 8;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = false;
        hlg.childControlHeight = true;

        // Dot
        GameObject dot = new GameObject("Dot", typeof(RectTransform), typeof(Image));
        dot.transform.SetParent(tab.transform, false);
        dot.GetComponent<Image>().color = Color.gray;
        LayoutElement dotLE = dot.AddComponent<LayoutElement>();
        dotLE.preferredWidth = 6;
        dotLE.preferredHeight = 6;

        // Label
        GameObject label = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        label.transform.SetParent(tab.transform, false);
        TextMeshProUGUI labelTMP = label.GetComponent<TextMeshProUGUI>();
        labelTMP.fontSize = 13;
        labelTMP.color = tabInactiveTextColor;
        LayoutElement labelLE = label.AddComponent<LayoutElement>();
        labelLE.flexibleWidth = 1;

        // Count
        GameObject count = new GameObject("Count", typeof(RectTransform), typeof(TextMeshProUGUI));
        count.transform.SetParent(tab.transform, false);
        TextMeshProUGUI countTMP = count.GetComponent<TextMeshProUGUI>();
        countTMP.fontSize = 11;
        countTMP.color = new Color(1f, 1f, 1f, 0.3f);
        countTMP.alignment = TextAlignmentOptions.MidlineRight;
        LayoutElement countLE = count.AddComponent<LayoutElement>();
        countLE.preferredWidth = 24;

        return tab;
    }

    GameObject CreateSlotObject(Transform parent)
    {
        GameObject slot = new GameObject("Slot", typeof(RectTransform), typeof(Image), typeof(Button));
        slot.transform.SetParent(parent, false);
        slot.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.03f);

        LayoutElement le = slot.AddComponent<LayoutElement>();
        le.preferredWidth = 90;
        le.preferredHeight = 90;

        // Icon
        GameObject icon = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        icon.transform.SetParent(slot.transform, false);
        RectTransform iconRT = icon.GetComponent<RectTransform>();
        iconRT.anchorMin = new Vector2(0.5f, 0.6f);
        iconRT.anchorMax = new Vector2(0.5f, 0.6f);
        iconRT.sizeDelta = new Vector2(36, 36);

        // Name
        GameObject name = new GameObject("Name", typeof(RectTransform), typeof(TextMeshProUGUI));
        name.transform.SetParent(slot.transform, false);
        TextMeshProUGUI nameTMP = name.GetComponent<TextMeshProUGUI>();
        nameTMP.fontSize = 10;
        nameTMP.color = new Color(1f, 1f, 1f, 0.6f);
        nameTMP.alignment = TextAlignmentOptions.Bottom;
        RectTransform nameRT = name.GetComponent<RectTransform>();
        nameRT.anchorMin = new Vector2(0, 0);
        nameRT.anchorMax = new Vector2(1, 0.35f);
        nameRT.offsetMin = new Vector2(4, 4);
        nameRT.offsetMax = new Vector2(-4, 0);

        // Count badge
        GameObject badge = new GameObject("CountBadge", typeof(RectTransform), typeof(Image));
        badge.transform.SetParent(slot.transform, false);
        badge.GetComponent<Image>().color = new Color(0.04f, 0.05f, 0.08f, 0.85f);
        RectTransform badgeRT = badge.GetComponent<RectTransform>();
        badgeRT.anchorMin = new Vector2(1, 1);
        badgeRT.anchorMax = new Vector2(1, 1);
        badgeRT.pivot = new Vector2(1, 1);
        badgeRT.sizeDelta = new Vector2(24, 16);
        badgeRT.anchoredPosition = new Vector2(-4, -4);

        GameObject badgeText = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        badgeText.transform.SetParent(badge.transform, false);
        TextMeshProUGUI badgeTMP = badgeText.GetComponent<TextMeshProUGUI>();
        badgeTMP.fontSize = 9;
        badgeTMP.color = Color.white;
        badgeTMP.alignment = TextAlignmentOptions.Center;
        RectTransform badgeTextRT = badgeText.GetComponent<RectTransform>();
        badgeTextRT.anchorMin = Vector2.zero;
        badgeTextRT.anchorMax = Vector2.one;
        badgeTextRT.offsetMin = Vector2.zero;
        badgeTextRT.offsetMax = Vector2.zero;
        badge.SetActive(false);

        // Owner tag
        GameObject ownerTag = new GameObject("OwnerTag", typeof(RectTransform), typeof(Image));
        ownerTag.transform.SetParent(slot.transform, false);
        ownerTag.GetComponent<Image>().color = ownerTagColor;
        RectTransform ownerRT = ownerTag.GetComponent<RectTransform>();
        ownerRT.anchorMin = new Vector2(0, 0);
        ownerRT.anchorMax = new Vector2(1, 0);
        ownerRT.pivot = new Vector2(0.5f, 0);
        ownerRT.sizeDelta = new Vector2(0, 14);
        ownerRT.anchoredPosition = new Vector2(0, 4);
        ownerRT.offsetMin = new Vector2(4, 4);
        ownerRT.offsetMax = new Vector2(-4, 18);

        GameObject ownerText = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        ownerText.transform.SetParent(ownerTag.transform, false);
        TextMeshProUGUI ownerTMP = ownerText.GetComponent<TextMeshProUGUI>();
        ownerTMP.fontSize = 8;
        ownerTMP.color = ownerTagTextColor;
        ownerTMP.alignment = TextAlignmentOptions.Center;
        ownerTMP.enableWordWrapping = false;
        ownerTMP.overflowMode = TextOverflowModes.Ellipsis;
        RectTransform ownerTextRT = ownerText.GetComponent<RectTransform>();
        ownerTextRT.anchorMin = Vector2.zero;
        ownerTextRT.anchorMax = Vector2.one;
        ownerTextRT.offsetMin = Vector2.zero;
        ownerTextRT.offsetMax = Vector2.zero;
        ownerTag.SetActive(false);

        // Fault indicator
        GameObject fault = new GameObject("FaultIndicator", typeof(RectTransform), typeof(Image));
        fault.transform.SetParent(slot.transform, false);
        fault.GetComponent<Image>().color = new Color(0.88f, 0.29f, 0.29f, 0.8f);
        RectTransform faultRT = fault.GetComponent<RectTransform>();
        faultRT.anchorMin = new Vector2(0, 1);
        faultRT.anchorMax = new Vector2(0, 1);
        faultRT.pivot = new Vector2(0, 1);
        faultRT.sizeDelta = new Vector2(6, 6);
        faultRT.anchoredPosition = new Vector2(4, -4);
        fault.SetActive(false);

        return slot;
    }
}

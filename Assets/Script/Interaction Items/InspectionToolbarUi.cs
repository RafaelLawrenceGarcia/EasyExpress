using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// InspectionToolbarUI — Bottom-center toolbar that appears ONLY during inspection.
/// Shows: [Screwdriver] [Air Can] | [USB 1] [USB 2] [USB 3]
/// 
/// Press 1 = toggle Screwdriver, Press 2 = toggle Air Can
/// USB slots are for diagnostic tools (future feature)
/// 
/// SETUP IN UNITY:
/// 1. Create Canvas "InspectionToolbarCanvas" (Sort Order 12)
///    - Canvas Scaler: Scale With Screen Size, 1920x1080
///    - START IT DISABLED (the script enables it during inspection)
///
/// 2. Inside, create Panel "ToolbarPanel"
///    - Anchor: bottom-center, Pivot (0.5, 0)
///    - Width: 380, Height: 70
///    - Pos Y: 16
///    - Image color: (10, 10, 10, 200)
///    - Add Outline: (255,255,255,15), distance (1,1)
///
/// 3. Inside ToolbarPanel, manually position (no layout groups):
///
///    "ToolsLabel" (TMP) — tiny vertical label
///    - Anchor: middle-left, Pos X: 6, Pos Y: 0
///    - Width: 14, Height: 50
///    - Text: "TOOLS", Size 7, White 25% alpha, rotation Z: 90
///
///    "ScrewdriverSlot" (Panel) — tool slot 1
///    - Anchor: middle-left, Pos X: 24, Pos Y: 0
///    - Width: 56, Height: 56
///    - Image color: (255,255,255,25) — changes when selected
///    - Add Outline: (255,255,255,25), distance (1,1)
///    - Inside: "ScrewdriverIcon" (Image) for the icon, 24x24, centered
///    - Inside: "ScrewdriverLabel" (TMP) "Screwdriver", size 7, bottom
///    - Inside: "Key1Badge" (Panel) top-right corner, 16x16, with TMP "1" size 9
///
///    "AirCanSlot" (Panel) — tool slot 2
///    - Same as above but Pos X: 86
///    - "AirCanIcon", "AirCanLabel" = "Air Can", "Key2Badge" with "2"
///
///    "Divider" (Image) — vertical line
///    - Anchor: middle-left, Pos X: 150
///    - Width: 1, Height: 40
///    - Color: (255,255,255,20)
///
///    "USBLabel" (TMP) — tiny vertical label
///    - Same style as ToolsLabel but Pos X: 158, text "USB"
///
///    "USBSlot1" (Panel) — USB slot
///    - Anchor: middle-left, Pos X: 176
///    - Width: 44, Height: 44
///    - Image color: (133,183,235,20)
///    - Add Outline: (133,183,235,60), distance (1,1)
///    - Inside: "USBIcon1" (Image) USB icon, "USBLabel1" (TMP) "Empty", size 7
///
///    "USBSlot2" — same, Pos X: 226
///    "USBSlot3" — same, Pos X: 276
///
/// 4. Drag references into this script on InspectionToolbarCanvas
/// </summary>
public class InspectionToolbarUI : MonoBehaviour
{
    public static InspectionToolbarUI Instance;

    [Header("Root")]
    public Canvas toolbarCanvas;          // The whole canvas — enable/disable this

    [Header("Tool Slots")]
    public Image screwdriverSlotBG;       // Background image of screwdriver slot
    public Image airCanSlotBG;            // Background image of air can slot
    public TextMeshProUGUI screwdriverLabel;
    public TextMeshProUGUI airCanLabel;

    [Header("USB Slots")]
    public Image usbSlot1BG;
    public Image usbSlot2BG;
    public Image usbSlot3BG;
    public TextMeshProUGUI usbLabel1;
    public TextMeshProUGUI usbLabel2;
    public TextMeshProUGUI usbLabel3;

    [Header("Colors")]
    public Color toolNormalBG = new Color(1f, 1f, 1f, 0.06f);
    public Color toolSelectedBG = new Color(0.94f, 0.62f, 0.15f, 0.15f); // Amber glow
    public Color toolNormalBorder = new Color(1f, 1f, 1f, 0.1f);
    public Color toolSelectedBorder = new Color(0.94f, 0.62f, 0.15f, 0.6f);
    public Color toolNormalText = new Color(1f, 1f, 1f, 0.35f);
    public Color toolSelectedText = new Color(0.94f, 0.62f, 0.15f, 0.9f);

    public Color usbEmptyBG = new Color(0.52f, 0.72f, 0.92f, 0.08f);
    public Color usbFilledBG = new Color(0.52f, 0.72f, 0.92f, 0.2f);
    public Color usbEmptyText = new Color(0.52f, 0.72f, 0.92f, 0.5f);
    public Color usbFilledText = new Color(0.52f, 0.72f, 0.92f, 0.9f);

    // --- STATE ---
    private int selectedTool = -1; // -1=none, 0=screwdriver, 1=aircan
    private string[] usbContents = new string[3]; // null = empty
    private Outline screwdriverOutline;
    private Outline airCanOutline;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // Cache outlines
        if (screwdriverSlotBG != null) screwdriverOutline = screwdriverSlotBG.GetComponent<Outline>();
        if (airCanSlotBG != null) airCanOutline = airCanSlotBG.GetComponent<Outline>();

        // Start hidden
        if (toolbarCanvas != null) toolbarCanvas.enabled = false;

        // Clear USB slots
        for (int i = 0; i < 3; i++) usbContents[i] = null;
        UpdateUSBVisuals();
    }

    // =============================================
    //  SHOW / HIDE (called by InspectionManager)
    // =============================================

    public void Show()
    {
        if (toolbarCanvas != null) toolbarCanvas.enabled = true;
        selectedTool = -1;
        UpdateToolVisuals();
    }

    public void Hide()
    {
        if (toolbarCanvas != null) toolbarCanvas.enabled = false;
        selectedTool = -1;
    }

    // =============================================
    //  TOOL SELECTION (called from Update or ToolBelt)
    // =============================================

    /// <summary>
    /// Call this every frame during inspection to handle 1/2/3 keys.
    /// </summary>
    public void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            selectedTool = (selectedTool == 0) ? -1 : 0;
            UpdateToolVisuals();
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            selectedTool = (selectedTool == 1) ? -1 : 1;
            UpdateToolVisuals();
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            selectedTool = (selectedTool == 2) ? -1 : 2; // 2 = USB
            UpdateToolVisuals();
        }
    }

    public int GetSelectedTool()
    {
        return selectedTool; // -1=none, 0=screwdriver, 1=aircan
    }

    public bool IsScrewdriverSelected()
    {
        return selectedTool == 0;
    }

    public bool IsAirCanSelected()
    {
        return selectedTool == 1;
    }

    // =============================================
    //  USB SLOTS
    // =============================================

    /// <summary>
    /// Plug something into a USB slot. Returns the slot index, or -1 if all full.
    /// </summary>
    public int PlugInUSB(string deviceName)
    {
        for (int i = 0; i < 3; i++)
        {
            if (usbContents[i] == null)
            {
                usbContents[i] = deviceName;
                UpdateUSBVisuals();
                return i;
            }
        }
        Debug.Log("All USB slots full!");
        return -1;
    }

    /// <summary>
    /// Remove a USB device by slot index.
    /// </summary>
    public void UnplugUSB(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < 3)
        {
            usbContents[slotIndex] = null;
            UpdateUSBVisuals();
        }
    }

    /// <summary>
    /// Check if any USB slot has a specific device.
    /// </summary>
    public bool HasUSBDevice(string deviceName)
    {
        foreach (string s in usbContents)
        {
            if (s != null && s == deviceName) return true;
        }
        return false;
    }

    // =============================================
    //  VISUALS
    // =============================================

    void UpdateToolVisuals()
    {
        // Screwdriver
        if (screwdriverSlotBG != null)
            screwdriverSlotBG.color = (selectedTool == 0) ? toolSelectedBG : toolNormalBG;
        if (screwdriverOutline != null)
            screwdriverOutline.effectColor = (selectedTool == 0) ? toolSelectedBorder : toolNormalBorder;
        if (screwdriverLabel != null)
            screwdriverLabel.color = (selectedTool == 0) ? toolSelectedText : toolNormalText;

        // Air Can
        if (airCanSlotBG != null)
            airCanSlotBG.color = (selectedTool == 1) ? toolSelectedBG : toolNormalBG;
        if (airCanOutline != null)
            airCanOutline.effectColor = (selectedTool == 1) ? toolSelectedBorder : toolNormalBorder;
        if (airCanLabel != null)
            airCanLabel.color = (selectedTool == 1) ? toolSelectedText : toolNormalText;

        // USB
        // USB
        if (usbSlot1BG != null)
            usbSlot1BG.color = (selectedTool == 2) ? usbFilledBG : usbEmptyBG;
        if (usbLabel1 != null)
            usbLabel1.color = (selectedTool == 2) ? usbFilledText : usbEmptyText;
    }
    public bool IsUSBSelected()
    {
        return selectedTool == 2;
    }
    void UpdateUSBVisuals()
    {
        UpdateSingleUSB(usbSlot1BG, usbLabel1, usbContents[0]);
        UpdateSingleUSB(usbSlot2BG, usbLabel2, usbContents[1]);
        UpdateSingleUSB(usbSlot3BG, usbLabel3, usbContents[2]);
    }

    void UpdateSingleUSB(Image bg, TextMeshProUGUI label, string content)
    {
        bool filled = !string.IsNullOrEmpty(content);

        if (bg != null)
            bg.color = filled ? usbFilledBG : usbEmptyBG;
        if (label != null)
        {
            label.text = filled ? content : "Empty";
            label.color = filled ? usbFilledText : usbEmptyText;
        }
    }
}
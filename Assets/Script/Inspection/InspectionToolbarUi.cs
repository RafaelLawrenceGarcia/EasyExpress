using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InspectionToolbarUI : MonoBehaviour
{
    public static InspectionToolbarUI Instance;

    [Header("Root — assign the PANEL inside the canvas, not the canvas itself")]
    public GameObject toolbarPanel;

    [Header("Tool Slot Backgrounds")]
    public Image screwdriverSlotBG;
    public Image usbSlotBG;
    public Image airDusterSlotBG;
    public Image handSlotBG;

    [Header("Tool Slot Labels")]
    public TextMeshProUGUI screwdriverLabel;
    public TextMeshProUGUI usbLabel;
    public TextMeshProUGUI airDusterLabel;
    public TextMeshProUGUI handLabel;

    [Header("Colors")]
    public Color toolNormalBG       = new Color(1f, 1f, 1f, 0.06f);
    public Color toolSelectedBG     = new Color(0.94f, 0.62f, 0.15f, 0.15f);
    public Color toolNormalBorder   = new Color(1f, 1f, 1f, 0.1f);
    public Color toolSelectedBorder = new Color(0.94f, 0.62f, 0.15f, 0.6f);
    public Color toolNormalText     = new Color(1f, 1f, 1f, 0.35f);
    public Color toolSelectedText   = new Color(0.94f, 0.62f, 0.15f, 0.9f);

    // -1=none, 0=screwdriver, 1=usb, 2=airDuster, 3=hand
    private int selectedTool = -1;

    private Outline screwdriverOutline;
    private Outline usbOutline;
    private Outline airDusterOutline;
    private Outline handOutline;

    void Awake() { Instance = this; }

    void Start()
    {
        if (screwdriverSlotBG != null) screwdriverOutline = screwdriverSlotBG.GetComponent<Outline>();
        if (usbSlotBG         != null) usbOutline         = usbSlotBG.GetComponent<Outline>();
        if (airDusterSlotBG   != null) airDusterOutline   = airDusterSlotBG.GetComponent<Outline>();
        if (handSlotBG        != null) handOutline        = handSlotBG.GetComponent<Outline>();

        if (toolbarPanel != null) toolbarPanel.SetActive(false);
    }

    public void Show()
    {
        if (toolbarPanel != null) toolbarPanel.SetActive(true);
        selectedTool = -1;
        UpdateToolVisuals();
    }

    public void Hide()
    {
        if (toolbarPanel != null) toolbarPanel.SetActive(false);
        selectedTool = -1;
    }

    public void HandleInput()
    {
        bool changed = false;
        if (Input.GetKeyDown(KeyCode.Alpha1)) { selectedTool = (selectedTool == 0) ? -1 : 0; changed = true; }
        if (Input.GetKeyDown(KeyCode.Alpha2)) { selectedTool = (selectedTool == 1) ? -1 : 1; changed = true; }
        if (Input.GetKeyDown(KeyCode.Alpha3)) { selectedTool = (selectedTool == 2) ? -1 : 2; changed = true; }
        if (Input.GetKeyDown(KeyCode.Alpha4)) { selectedTool = (selectedTool == 3) ? -1 : 3; changed = true; }

        if (changed) { UpdateToolVisuals(); SyncToolBelt(); }
    }

    void SyncToolBelt()
    {
        if (ToolBelt.Instance == null) return;
        switch (selectedTool)
        {
            case 0:  ToolBelt.Instance.EquipTool(ToolBelt.ToolType.Screwdriver);   break;
            case 2:  ToolBelt.Instance.EquipTool(ToolBelt.ToolType.CompressedAir); break;
            default: ToolBelt.Instance.EquipTool(ToolBelt.ToolType.None);          break;
        }
    }

    public int  GetSelectedTool()       => selectedTool;
    public bool IsScrewdriverSelected() => selectedTool == 0;
    public bool IsUSBSelected()         => selectedTool == 1;
    public bool IsAirCanSelected()      => selectedTool == 2;
    public bool IsHandSelected()        => selectedTool == 3 || selectedTool == -1;

    void UpdateToolVisuals()
    {
        SetSlot(screwdriverSlotBG, screwdriverOutline, screwdriverLabel, selectedTool == 0);
        SetSlot(usbSlotBG,         usbOutline,         usbLabel,         selectedTool == 1);
        SetSlot(airDusterSlotBG,   airDusterOutline,   airDusterLabel,   selectedTool == 2);
        SetSlot(handSlotBG,        handOutline,        handLabel,        selectedTool == 3);
    }

    void SetSlot(Image bg, Outline outline, TextMeshProUGUI label, bool selected)
    {
        if (bg      != null) bg.color            = selected ? toolSelectedBG     : toolNormalBG;
        if (outline != null) outline.effectColor = selected ? toolSelectedBorder : toolNormalBorder;
        if (label   != null) label.color         = selected ? toolSelectedText   : toolNormalText;
    }
}
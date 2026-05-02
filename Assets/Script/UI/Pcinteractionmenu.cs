using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// PCInteractionMenu — Scroll menu that appears when looking at a PC on the workstation.
/// Shows "Inspect PC" and "Grab PC" — scroll to pick, press E to confirm.
/// 
/// Unlike the door menu, this one is ALREADY VISIBLE when you're near the PC.
/// No need to press E first to open it.
/// 
/// SETUP IN UNITY:
/// 1. Create a new Canvas or use InteractionPromptCanvas
/// 
/// 2. Create a Panel called "PCMenuPanel"
///    - Anchor: bottom-center
///    - Pivot: (0.5, 0)
///    - Width: 200, Height: 120
///    - Pos Y: 80
///    - Image color: transparent (no background, options have their own bg)
///    - START DISABLED
///
/// 3. Inside PCMenuPanel, create "ScrollHint" (TextMeshPro)
///    - Anchor: top-center
///    - Text: "SCROLL ↑↓"
///    - Font Size: 10
///    - Color: white at 30% alpha
///    - Alignment: center
///
/// 4. Inside PCMenuPanel, create "Option1_Panel" (Panel)
///    - Width: 190, Height: 40
///    - Anchor: middle-center, Pos Y: 8
///    - Image color: (255,255,255,30)
///    - Add Outline: (255,255,255,50), distance (1,1)
///    - Inside it, create a HorizontalLayoutGroup child with:
///      - "Key1BG" panel (24x24) with "Key1Text" TMP inside ("E", size 11, bold, centered)
///      - "Action1Text" TMP ("Inspect PC", size 14)
///
/// 5. Inside PCMenuPanel, create "Option2_Panel" (Panel) 
///    - Same setup but Pos Y: -36
///    - Image color: (255,255,255,10)
///    - Key2Text = "E", Action2Text = "Grab PC"
///
/// 6. Drag all references into this script
/// </summary>
public class PCInteractionMenu : MonoBehaviour
{
    [Header("UI Panel")]
    public GameObject menuPanel;

    [Header("Option 1 - Inspect PC")]
    public Image option1Background;
    public TextMeshProUGUI option1Text;
    public Image option1KeyBG;

    [Header("Option 2 - Grab PC")]
    public Image option2Background;
    public TextMeshProUGUI option2Text;
    public Image option2KeyBG;

    [Header("Colors")]
    public Color selectedBG = new Color(1f, 1f, 1f, 0.12f);
    public Color unselectedBG = new Color(1f, 1f, 1f, 0.04f);
    public Color selectedTextColor = new Color(1f, 1f, 1f, 0.9f);
    public Color unselectedTextColor = new Color(1f, 1f, 1f, 0.4f);
    public Color selectedKeyBG = new Color(1f, 1f, 1f, 0.15f);
    public Color unselectedKeyBG = new Color(1f, 1f, 1f, 0.08f);

    // --- STATE ---
    private bool isOpen = false;
    private int selectedIndex = 0; // 0 = Inspect, 1 = Grab

    // Callback when user confirms
    private System.Action<int> onConfirmed;

    void Update()
    {
        if (!isOpen) return;

        // --- SCROLL WHEEL ---
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll > 0f) { selectedIndex = 0; UpdateVisuals(); }
        if (scroll < 0f) { selectedIndex = 1; UpdateVisuals(); }

        // --- KEYBOARD ---
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
        { selectedIndex = 0; UpdateVisuals(); }
        if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
        { selectedIndex = 1; UpdateVisuals(); }

        // --- CONFIRM ---
        if (Input.GetKeyDown(KeyCode.E))
        {
            onConfirmed?.Invoke(selectedIndex);
        }

        // Smooth scale
        SmoothScale(option1Background, selectedIndex == 0);
        SmoothScale(option2Background, selectedIndex == 1);
    }

    // =============================================
    //  PUBLIC API
    // =============================================

    /// <summary>
    /// Show the menu. It appears immediately (no press E to open).
    /// onSelected: 0 = Inspect, 1 = Grab
    /// </summary>
    // In PCInteractionMenu.cs — add these 2 lines inside Show()
    public void Show(System.Action<int> onSelected)
    {
        isOpen = true;
        selectedIndex = 0;
        onConfirmed = onSelected;

        // ADD THESE:
        if (option1Text != null) option1Text.text = "Inspect PC";
        if (option2Text != null) option2Text.text = "Grab PC";

        if (menuPanel != null) menuPanel.SetActive(true);
        UpdateVisuals();
    }

    public void Hide()
    {
        isOpen = false;
        onConfirmed = null;
        if (menuPanel != null) menuPanel.SetActive(false);
    }

    public bool IsOpen()
    {
        return isOpen;
    }

    public int GetSelectedIndex()
    {
        return selectedIndex;
    }

    // =============================================
    //  VISUALS
    // =============================================

    void UpdateVisuals()
    {
        // Option 1
        if (option1Background != null)
            option1Background.color = (selectedIndex == 0) ? selectedBG : unselectedBG;
        if (option1Text != null)
            option1Text.color = (selectedIndex == 0) ? selectedTextColor : unselectedTextColor;
        if (option1KeyBG != null)
            option1KeyBG.color = (selectedIndex == 0) ? selectedKeyBG : unselectedKeyBG;

        // Option 2
        if (option2Background != null)
            option2Background.color = (selectedIndex == 1) ? selectedBG : unselectedBG;
        if (option2Text != null)
            option2Text.color = (selectedIndex == 1) ? selectedTextColor : unselectedTextColor;
        if (option2KeyBG != null)
            option2KeyBG.color = (selectedIndex == 1) ? selectedKeyBG : unselectedKeyBG;
    }

    void SmoothScale(Image panel, bool isSelected)
    {
        if (panel == null) return;
        float target = isSelected ? 1.03f : 1.0f;
        panel.transform.localScale = Vector3.Lerp(
            panel.transform.localScale,
            Vector3.one * target,
            Time.unscaledDeltaTime * 10f
        );
    }
}
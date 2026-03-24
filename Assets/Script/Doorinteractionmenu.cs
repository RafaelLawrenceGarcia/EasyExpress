using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// DoorInteractionMenu — 3-option scroll menu at the door.
/// Options: Exit Store, End Day, Cancel
/// 
/// SETUP: Same as before but now with a third option panel.
/// </summary>
public class DoorInteractionMenu : MonoBehaviour
{
    [Header("UI Panel")]
    public GameObject menuPanel;

    [Header("Option 1 - Exit Store")]
    public Image option1Background;
    public TextMeshProUGUI option1Text;

    [Header("Option 2 - End Day")]
    public Image option2Background;
    public TextMeshProUGUI option2Text;

    [Header("Option 3 - Cancel")]
    public Image option3Background;
    public TextMeshProUGUI option3Text;

    [Header("Colors")]
    public Color selectedBG = new Color(1f, 1f, 1f, 0.12f);
    public Color unselectedBG = new Color(1f, 1f, 1f, 0.04f);
    public Color selectedTextColor = new Color(1f, 1f, 1f, 0.95f);
    public Color unselectedTextColor = new Color(1f, 1f, 1f, 0.45f);

    [Header("References")]
    public DayTransitionManager dayTransitionManager;
    public PlayerInteract playerInteract;

    // --- STATE ---
    private bool isOpen = false;
    private int selectedIndex = 0; // 0 = Exit Store, 1 = End Day, 2 = Cancel
    private const int OPTION_COUNT = 3;

    // NEW: Record when menu opened to prevent double-click
    private float openTime = 0f;

    // Store references in arrays for cleaner code
    private Image[] backgrounds;
    private TextMeshProUGUI[] texts;

    void Start()
    {
        if (menuPanel != null) menuPanel.SetActive(false);

        backgrounds = new Image[] { option1Background, option2Background, option3Background };
        texts = new TextMeshProUGUI[] { option1Text, option2Text, option3Text };
    }

    void Update()
    {
        if (!isOpen) return;

        // ADDED: Ignore input for a short time after opening to prevent
        // the same E press from immediately confirming
        if (Time.time - openTime < 0.2f) return;

        // --- SCROLL WHEEL ---
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll > 0f) { selectedIndex--; if (selectedIndex < 0) selectedIndex = OPTION_COUNT - 1; UpdateVisuals(); }
        if (scroll < 0f) { selectedIndex++; if (selectedIndex >= OPTION_COUNT) selectedIndex = 0; UpdateVisuals(); }

        // --- KEYBOARD BACKUP ---
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
        { selectedIndex--; if (selectedIndex < 0) selectedIndex = OPTION_COUNT - 1; UpdateVisuals(); }

        if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
        { selectedIndex++; if (selectedIndex >= OPTION_COUNT) selectedIndex = 0; UpdateVisuals(); }

        // --- CONFIRM ---
        if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Return))
        {
            ConfirmSelection();
        }

        // --- CANCEL SHORTCUT ---
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Q))
        {
            DoCancel();
        }

        // Smooth scale animation each frame
        for (int i = 0; i < OPTION_COUNT; i++)
        {
            SmoothScale(backgrounds[i], selectedIndex == i);
        }
    }

    // =============================================
    //  PUBLIC API
    // =============================================

    public void OpenMenu()
    {
        if (isOpen) return;

        isOpen = true;
        selectedIndex = 0;
        openTime = Time.time; // ADDED: Record when menu opened

        if (menuPanel != null) menuPanel.SetActive(true);

        UpdateVisuals();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void CloseMenu()
    {
        isOpen = false;
        if (menuPanel != null) menuPanel.SetActive(false);
    }

    public bool IsOpen()
    {
        return isOpen;
    }

    // =============================================
    //  VISUALS
    // =============================================

    void UpdateVisuals()
    {
        for (int i = 0; i < OPTION_COUNT; i++)
        {
            bool isSelected = (i == selectedIndex);

            if (backgrounds[i] != null)
                backgrounds[i].color = isSelected ? selectedBG : unselectedBG;

            if (texts[i] != null)
            {
                texts[i].color = isSelected ? selectedTextColor : unselectedTextColor;
                texts[i].fontStyle = isSelected ? FontStyles.Bold : FontStyles.Normal;
            }
        }
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

    // =============================================
    //  ACTIONS
    // =============================================

    void ConfirmSelection()
    {
        switch (selectedIndex)
        {
            case 0: DoExitStore(); break;
            case 1: DoEndDay(); break;
            case 2: DoCancel(); break;
        }
    }

    void DoExitStore()
    {
        CloseMenu();

        ShopDoor shopDoor = GetComponentInParent<ShopDoor>();
        SceneDoor sceneDoor = GetComponentInParent<SceneDoor>();

        if (playerInteract != null) playerInteract.ForceCloseAllInteraction();

        if (shopDoor != null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null) shopDoor.EnterShop(player);
        }
        else if (sceneDoor != null)
        {
            sceneDoor.EnterDoor();
        }
        else
        {
            Debug.LogWarning("DoorInteractionMenu: No ShopDoor or SceneDoor found!");
        }
    }

    void DoEndDay()
    {
        CloseMenu();

        if (playerInteract != null) playerInteract.ForceCloseAllInteraction();

        if (dayTransitionManager != null)
        {
            dayTransitionManager.EndDay(() =>
            {
                Debug.Log("New day started! Reset shop systems here.");
            });
        }
        else
        {
            Debug.LogWarning("DoorInteractionMenu: No DayTransitionManager assigned!");
        }
    }

    void DoCancel()
    {
        CloseMenu();
        if (playerInteract != null) playerInteract.ForceCloseAllInteraction();
    }
}
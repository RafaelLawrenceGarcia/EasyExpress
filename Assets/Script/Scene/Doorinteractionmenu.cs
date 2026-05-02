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

    // Record when menu opened to prevent double-click
    private float openTime = 0f;

    // For resetting the End Day text after showing a warning
    private string defaultEndDayLabel = "End Day";
    private Coroutine warningResetCoroutine;

    // Store references in arrays for cleaner code
    private Image[] backgrounds;
    private TextMeshProUGUI[] texts;

    void Start()
    {
        if (menuPanel != null) menuPanel.SetActive(false);

        backgrounds = new Image[] { option1Background, option2Background, option3Background };
        texts = new TextMeshProUGUI[] { option1Text, option2Text, option3Text };

        // Cache the default label so we can restore it after warnings
        if (option2Text != null)
            defaultEndDayLabel = option2Text.text;
    }

    void Update()
    {
        if (!isOpen) return;

        // Ignore input for a short time after opening to prevent
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
        openTime = Time.time;

        // Reset the End Day label in case it was showing a warning
        ResetEndDayLabel();

        if (menuPanel != null) menuPanel.SetActive(true);

        UpdateVisuals();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void CloseMenu()
    {
        isOpen = false;
        if (menuPanel != null) menuPanel.SetActive(false);

        // Clean up any pending warning reset
        if (warningResetCoroutine != null)
        {
            StopCoroutine(warningResetCoroutine);
            warningResetCoroutine = null;
        }
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
        // ── During tutorial: only allow End Day at step 18 ──
        bool tutActive  = TutorialManager.Instance != null && TutorialManager.Instance.IsTutorialActive();
        bool endDayStep = TutorialManager.Instance != null && TutorialManager.Instance.IsEndDayStep();

        if (tutActive && !endDayStep)
        {
            ShowEndDayWarning("Finish your tasks first!");
            return;
        }

        // ── BLOCK: Cannot end day while customers are still inside ──
        // Skip this check during tutorial so it doesn't get stuck
        if (!tutActive && ShopCustomerSpawner.Instance != null
            && ShopCustomerSpawner.Instance.HasCustomersInside())
        {
            int count = ShopCustomerSpawner.Instance.GetCustomerCount();
            ShowEndDayWarning("Customers still inside! (" + count + ")");
            return;
        }

        CloseMenu();

        if (playerInteract != null) playerInteract.ForceCloseAllInteraction();

        // ── Tutorial hook: mark End Day task complete ──
        if (TutorialManager.Instance != null)
            TutorialManager.Instance.CompleteEndDayTask();

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

    // =============================================
    //  END DAY WARNING HELPERS
    // =============================================

    /// <summary>
    /// Briefly flashes a red warning on the End Day option, then resets.
    /// </summary>
    void ShowEndDayWarning(string message)
    {
        if (option2Text != null)
        {
            option2Text.text = message;
            option2Text.color = new Color(1f, 0.3f, 0.3f, 1f); // Red
        }

        // Reset after 2 seconds
        if (warningResetCoroutine != null)
            StopCoroutine(warningResetCoroutine);

        warningResetCoroutine = StartCoroutine(ResetEndDayLabelAfterDelay(2f));
    }

    System.Collections.IEnumerator ResetEndDayLabelAfterDelay(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        ResetEndDayLabel();
        UpdateVisuals(); // Re-apply correct colors
        warningResetCoroutine = null;
    }

    void ResetEndDayLabel()
    {
        if (option2Text != null)
        {
            option2Text.text = defaultEndDayLabel;
            // Color will be set by UpdateVisuals
        }
    }
}
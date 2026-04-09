using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class DemoLockManager : MonoBehaviour
{
    public static DemoLockManager Instance;

    [Header("Demo Settings")]
    [Tooltip("The last day the player can play. Demo locks when day exceeds this.")]
    public int maxDemoDay = 3;

    [Header("Price")]
    public string priceText = "\u20B1299.00";
    public string priceSubtext = "One-time purchase \u2014 lifetime access";

    [Header("Payment URLs")]
    public string gcashURL = "https://payments.gcash.com";
    public string mayaURL = "https://www.maya.ph";
    public string paypalURL = "https://www.paypal.com";
    public string cardURL = "https://payments.example.com/card";

    [Header("Main Menu")]
    public string mainMenuSceneName = "MainMenu";

    [Header("Animation")]
    public float fadeInDuration = 1.2f;

    // ── Built UI references ──────────────────────────────────────
    private Canvas lockCanvas;
    private GameObject lockPanel;
    private CanvasGroup panelCG;
    private TextMeshProUGUI titleTMP;
    private TextMeshProUGUI messageTMP;
    private TextMeshProUGUI dayTMP;
    private TextMeshProUGUI priceTMP;
    private TextMeshProUGUI priceSubTMP;

    private bool isDemoLocked = false;
    private bool uiBuilt = false;

    // ── Colors ───────────────────────────────────────────────────
    static readonly Color COL_BG = new Color(0.02f, 0.02f, 0.06f, 0.96f);
    static readonly Color COL_GOLD = new Color(1f, 0.84f, 0f, 1f);
    static readonly Color COL_CYAN = new Color(0.29f, 0.88f, 1f, 1f);
    static readonly Color COL_WHITE = new Color(1f, 1f, 1f, 1f);
    static readonly Color COL_WHITE_DIM = new Color(1f, 1f, 1f, 0.5f);
    static readonly Color COL_WHITE_FADE = new Color(1f, 1f, 1f, 0.25f);
    static readonly Color COL_GCASH = new Color(0f, 0.44f, 0.88f, 1f);
    static readonly Color COL_MAYA = new Color(0f, 0.69f, 0.25f, 1f);
    static readonly Color COL_PAYPAL = new Color(0f, 0.19f, 0.53f, 1f);
    static readonly Color COL_CARD = new Color(0.18f, 0.18f, 0.18f, 1f);
    static readonly Color COL_MENU_BG = new Color(1f, 1f, 1f, 0.06f);
    static readonly Color COL_DIVIDER = new Color(1f, 1f, 1f, 0.08f);
    static readonly Color COL_CARD_BG = new Color(1f, 1f, 1f, 0.03f);

    // ─────────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(this); return; }

        BuildUI();
    }

    void OnEnable() { DayTransitionManager.OnNewDayStarted += OnNewDay; }
    void OnDisable() { DayTransitionManager.OnNewDayStarted -= OnNewDay; }

    void Start() { CheckDemoStatus(); }

    // ═════════════════════════════════════════════════════════════
    //  BUILD THE ENTIRE UI IN CODE
    // ═════════════════════════════════════════════════════════════

    void BuildUI()
    {
        if (uiBuilt) return;
        uiBuilt = true;

        // ── Canvas ───────────────────────────────────────────────
        GameObject canvasGO = new GameObject("DemoLockCanvas");
        canvasGO.transform.SetParent(transform, false);

        lockCanvas = canvasGO.AddComponent<Canvas>();
        lockCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        lockCanvas.sortingOrder = 200;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        // ── Full-screen dark panel ───────────────────────────────
        lockPanel = CreatePanel(canvasGO.transform, "LockPanel", COL_BG);
        Stretch(lockPanel);
        panelCG = lockPanel.AddComponent<CanvasGroup>();
        panelCG.alpha = 0f;

        // ── Center content container ─────────────────────────────
        GameObject content = CreateVerticalGroup(lockPanel.transform, "Content",
            spacing: 0, childAlignment: TextAnchor.UpperCenter,
            padding: new RectOffset(40, 40, 40, 40));
        RectTransform contentRT = content.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0.5f, 0.5f);
        contentRT.anchorMax = new Vector2(0.5f, 0.5f);
        contentRT.pivot = new Vector2(0.5f, 0.5f);
        contentRT.sizeDelta = new Vector2(720, 0);
        ContentSizeFitter contentCSF = content.AddComponent<ContentSizeFitter>();
        contentCSF.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // ── Decorative top line ──────────────────────────────────
        CreateDivider(content.transform, COL_GOLD, 2f, 120f);
        AddSpacer(content.transform, 24);

        // ── DEMO COMPLETE title ──────────────────────────────────
        titleTMP = CreateTMP(content.transform, "Title",
            "DEMO COMPLETE!", 52, COL_GOLD, FontStyles.Bold,
            TextAlignmentOptions.Center);
        AddLayoutElement(titleTMP.gameObject, prefHeight: 64);

        AddSpacer(content.transform, 8);

        // ── Day reached subtitle ─────────────────────────────────
        dayTMP = CreateTMP(content.transform, "DayText",
            "", 20, COL_CYAN, FontStyles.Normal,
            TextAlignmentOptions.Center);
        AddLayoutElement(dayTMP.gameObject, prefHeight: 30);

        AddSpacer(content.transform, 6);
        CreateDivider(content.transform, COL_DIVIDER, 1f);
        AddSpacer(content.transform, 20);

        // ── Message body ─────────────────────────────────────────
        messageTMP = CreateTMP(content.transform, "Message",
            "", 17, COL_WHITE, FontStyles.Normal,
            TextAlignmentOptions.Center);
        messageTMP.lineSpacing = 8;
        AddLayoutElement(messageTMP.gameObject, prefHeight: 200, flexWidth: 1);

        AddSpacer(content.transform, 16);
        CreateDivider(content.transform, COL_DIVIDER, 1f);
        AddSpacer(content.transform, 24);

        // ── Price card ───────────────────────────────────────────
        GameObject priceCard = CreatePanel(content.transform, "PriceCard", COL_CARD_BG);
        AddLayoutElement(priceCard, prefHeight: 110, flexWidth: 1);
        Outline priceOutline = priceCard.AddComponent<Outline>();
        priceOutline.effectColor = new Color(1f, 0.84f, 0f, 0.15f);
        priceOutline.effectDistance = new Vector2(1, 1);

        GameObject priceVert = CreateVerticalGroup(priceCard.transform, "PriceContent",
            spacing: 4, childAlignment: TextAnchor.MiddleCenter,
            padding: new RectOffset(0, 0, 14, 14));
        Stretch(priceVert);

        TextMeshProUGUI getLabel = CreateTMP(priceVert.transform, "GetFullGame",
            "GET THE FULL GAME", 13, COL_WHITE_DIM, FontStyles.Bold,
            TextAlignmentOptions.Center);
        getLabel.characterSpacing = 6;
        AddLayoutElement(getLabel.gameObject, prefHeight: 20);

        priceTMP = CreateTMP(priceVert.transform, "Price",
            priceText, 44, COL_GOLD, FontStyles.Bold,
            TextAlignmentOptions.Center);
        AddLayoutElement(priceTMP.gameObject, prefHeight: 50);

        priceSubTMP = CreateTMP(priceVert.transform, "PriceSub",
            priceSubtext, 13, COL_WHITE_FADE, FontStyles.Italic,
            TextAlignmentOptions.Center);
        AddLayoutElement(priceSubTMP.gameObject, prefHeight: 20);

        AddSpacer(content.transform, 20);

        // ── Payment section label ────────────────────────────────
        TextMeshProUGUI payLabel = CreateTMP(content.transform, "PayLabel",
            "CHOOSE PAYMENT METHOD", 12, COL_WHITE_FADE, FontStyles.Bold,
            TextAlignmentOptions.Center);
        payLabel.characterSpacing = 4;
        AddLayoutElement(payLabel.gameObject, prefHeight: 20);

        AddSpacer(content.transform, 12);

        // ── Payment buttons row ──────────────────────────────────
        GameObject payRow = CreateHorizontalGroup(content.transform, "PaymentButtons",
            spacing: 14, childAlignment: TextAnchor.MiddleCenter);
        AddLayoutElement(payRow, prefHeight: 56);

        CreatePaymentButton(payRow.transform, "GCash", "GCash", COL_GCASH, gcashURL);
        CreatePaymentButton(payRow.transform, "Maya", "Maya", COL_MAYA, mayaURL);
        CreatePaymentButton(payRow.transform, "PayPal", "PayPal", COL_PAYPAL, paypalURL);
        CreatePaymentButton(payRow.transform, "Card", "Mastercard / Visa", COL_CARD, cardURL);

        AddSpacer(content.transform, 28);
        CreateDivider(content.transform, COL_DIVIDER, 1f);
        AddSpacer(content.transform, 20);

        // ── Main Menu button ─────────────────────────────────────
        CreateMenuButton(content.transform);

        AddSpacer(content.transform, 16);
        CreateDivider(content.transform, COL_GOLD, 2f, 120f);

        // ── Version text (bottom-right) ──────────────────────────
        TextMeshProUGUI versionTMP = CreateTMP(lockPanel.transform, "Version",
            "EasyExpress Demo v1.0", 12, COL_WHITE_FADE, FontStyles.Normal,
            TextAlignmentOptions.BottomRight);
        RectTransform vRT = versionTMP.GetComponent<RectTransform>();
        vRT.anchorMin = new Vector2(1, 0);
        vRT.anchorMax = new Vector2(1, 0);
        vRT.pivot = new Vector2(1, 0);
        vRT.anchoredPosition = new Vector2(-20, 12);
        vRT.sizeDelta = new Vector2(250, 24);

        // ── Start hidden ─────────────────────────────────────────
        lockPanel.SetActive(false);
    }

    // ═════════════════════════════════════════════════════════════
    //  DEMO LOCK LOGIC
    // ═════════════════════════════════════════════════════════════

    void OnNewDay(int newDay)
    {
        if (TutorialManager.Instance != null && TutorialManager.Instance.IsTutorialActive())
            return;
        if (newDay > maxDemoDay)
            StartCoroutine(DelayedLock(newDay));
    }

    IEnumerator DelayedLock(int day)
    {
        yield return new WaitForSeconds(2.0f);
        ShowDemoLock(day);
    }

    public bool CheckDemoStatus()
    {
        // Skip if full game is unlocked
        if (PlayerPrefs.GetInt("FullGameUnlocked", 0) == 1) return false;

        int currentDay = PlayerPrefs.GetInt("CurrentDay", 1);
        bool tutorialDone = PlayerPrefs.GetInt("TutorialDone", 0) == 1;

        if (tutorialDone && currentDay > maxDemoDay)
        {
            ShowDemoLock(currentDay);
            return true;
        }
        return false;
    }

    void ShowDemoLock(int dayReached)
    {
        if (isDemoLocked) return;
        if (PlayerPrefs.GetInt("FullGameUnlocked", 0) == 1) return;
        isDemoLocked = true;

        Debug.Log($"[DemoLock] Demo locked at Day {dayReached}.");

        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        GTAMovement movement = FindFirstObjectByType<GTAMovement>();
        OrbitCamera camera = FindFirstObjectByType<OrbitCamera>();
        if (movement != null) movement.SetMovementState(false);
        if (camera != null) camera.SetCameraState(false);

        // Fill text
        int daysPlayed = Mathf.Min(dayReached - 1, maxDemoDay);
        if (dayTMP != null)
            dayTMP.text = $"You completed {daysPlayed} day{(daysPlayed != 1 ? "s" : "")} \u2014 great work!";

        if (messageTMP != null)
            messageTMP.text =
                "Thank you for playing the <b>EasyExpress Demo</b>!\n\n" +
                "You've experienced the basics of PC repair \u2014\n" +
                "diagnosing faults, swapping components, and managing your shop.\n\n" +
                "The <b>full version</b> unlocks:\n" +
                "  \u2022  Advanced diagnostics & more PC problems\n" +
                "  \u2022  Custom PC builds from scratch\n" +
                "  \u2022  Shop upgrades & furniture customization\n" +
                "  \u2022  Customer reputation & rating system\n" +
                "  \u2022  Unlimited gameplay with no day limit";

        if (priceTMP != null)
            priceTMP.text = priceText;

        // Show
        if (lockPanel != null)
        {
            lockPanel.SetActive(true);
            StartCoroutine(FadeIn());
        }
    }

    IEnumerator FadeIn()
    {
        if (panelCG == null) yield break;
        panelCG.alpha = 0f;
        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            panelCG.alpha = Mathf.SmoothStep(0f, 1f, elapsed / fadeInDuration);
            yield return null;
        }
        panelCG.alpha = 1f;
    }

    void OpenURL(string url, string label)
    {
        if (string.IsNullOrEmpty(url)) return;
        Debug.Log($"[DemoLock] Opening {label}: {url}");
        Application.OpenURL(url);
    }

    void ReturnToMainMenu()
    {
        isDemoLocked = false;
        Time.timeScale = 1f;
        DayTransitionManager.ResetDayFlag();
        PlayerPrefs.SetFloat("SavedGameTime", 6f);
        PlayerPrefs.Save();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    // ── Public helpers ───────────────────────────────────────────

    public bool IsDemoLocked() => isDemoLocked;
    public int GetMaxDemoDay() => maxDemoDay;

    /// <summary>
    /// Call this after verifying a purchase receipt.
    /// Removes the demo lock permanently.
    /// </summary>
    public void UnlockFullGame()
    {
        isDemoLocked = false;
        PlayerPrefs.SetInt("FullGameUnlocked", 1);
        PlayerPrefs.Save();
        Time.timeScale = 1f;
        if (lockPanel != null) lockPanel.SetActive(false);
        GTAMovement m = FindFirstObjectByType<GTAMovement>();
        OrbitCamera c = FindFirstObjectByType<OrbitCamera>();
        if (m != null) m.SetMovementState(true);
        if (c != null) c.SetCameraState(true);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // ═════════════════════════════════════════════════════════════
    //  UI BUILDER HELPERS
    // ═════════════════════════════════════════════════════════════

    GameObject CreatePanel(Transform parent, string name, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = color;
        return go;
    }

    void Stretch(GameObject go)
    {
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    TextMeshProUGUI CreateTMP(Transform parent, string name, string text,
        float fontSize, Color color, FontStyles style, TextAlignmentOptions align)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);

        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.fontStyle = style;
        tmp.alignment = align;
        tmp.enableWordWrapping = true;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.raycastTarget = false;

        return tmp;
    }

    GameObject CreateVerticalGroup(Transform parent, string name,
        float spacing, TextAnchor childAlignment, RectOffset padding = null)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(VerticalLayoutGroup));
        go.transform.SetParent(parent, false);

        VerticalLayoutGroup vlg = go.GetComponent<VerticalLayoutGroup>();
        vlg.spacing = spacing;
        vlg.childAlignment = childAlignment;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        if (padding != null) vlg.padding = padding;

        return go;
    }

    GameObject CreateHorizontalGroup(Transform parent, string name,
        float spacing, TextAnchor childAlignment)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(HorizontalLayoutGroup));
        go.transform.SetParent(parent, false);

        HorizontalLayoutGroup hlg = go.GetComponent<HorizontalLayoutGroup>();
        hlg.spacing = spacing;
        hlg.childAlignment = childAlignment;
        hlg.childControlWidth = false;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;

        return go;
    }

    void AddSpacer(Transform parent, float height)
    {
        GameObject go = new GameObject("Spacer", typeof(RectTransform), typeof(LayoutElement));
        go.transform.SetParent(parent, false);
        go.GetComponent<LayoutElement>().preferredHeight = height;
    }

    void AddLayoutElement(GameObject go, float prefHeight = -1, float prefWidth = -1, float flexWidth = -1)
    {
        LayoutElement le = go.GetComponent<LayoutElement>();
        if (le == null) le = go.AddComponent<LayoutElement>();
        if (prefHeight >= 0) le.preferredHeight = prefHeight;
        if (prefWidth >= 0) le.preferredWidth = prefWidth;
        if (flexWidth >= 0) le.flexibleWidth = flexWidth;
    }

    void CreateDivider(Transform parent, Color color, float height, float width = -1)
    {
        GameObject go = new GameObject("Divider", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = color;

        LayoutElement le = go.AddComponent<LayoutElement>();
        le.preferredHeight = height;
        if (width > 0)
            le.preferredWidth = width;
        else
            le.flexibleWidth = 1;
    }

    void CreatePaymentButton(Transform parent, string name, string label, Color bgColor, string url)
    {
        GameObject btnGO = new GameObject(name + "Btn", typeof(RectTransform),
            typeof(Image), typeof(Button), typeof(LayoutElement));
        btnGO.transform.SetParent(parent, false);

        Image btnImg = btnGO.GetComponent<Image>();
        btnImg.color = bgColor;

        Outline outline = btnGO.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 1f, 1f, 0.1f);
        outline.effectDistance = new Vector2(1, 1);

        LayoutElement le = btnGO.GetComponent<LayoutElement>();
        le.preferredWidth = 152;
        le.preferredHeight = 52;

        Button btn = btnGO.GetComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
        cb.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        btn.colors = cb;

        TextMeshProUGUI tmp = CreateTMP(btnGO.transform, "Label",
            label, 15, COL_WHITE, FontStyles.Bold, TextAlignmentOptions.Center);
        Stretch(tmp.gameObject);
        tmp.raycastTarget = false;

        string capturedURL = url;
        string capturedName = name;
        btn.onClick.AddListener(() => OpenURL(capturedURL, capturedName));
    }

    void CreateMenuButton(Transform parent)
    {
        GameObject btnGO = new GameObject("MainMenuBtn", typeof(RectTransform),
            typeof(Image), typeof(Button), typeof(LayoutElement));
        btnGO.transform.SetParent(parent, false);

        Image btnImg = btnGO.GetComponent<Image>();
        btnImg.color = COL_MENU_BG;

        Outline outline = btnGO.AddComponent<Outline>();
        outline.effectColor = COL_WHITE_FADE;
        outline.effectDistance = new Vector2(1, 1);

        LayoutElement le = btnGO.GetComponent<LayoutElement>();
        le.preferredHeight = 48;
        le.preferredWidth = 260;

        Button btn = btnGO.GetComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1.3f, 1.3f, 1.3f, 1f);
        cb.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        btn.colors = cb;

        TextMeshProUGUI tmp = CreateTMP(btnGO.transform, "Label",
            "Return to Main Menu", 16, COL_WHITE_DIM, FontStyles.Normal,
            TextAlignmentOptions.Center);
        Stretch(tmp.gameObject);
        tmp.raycastTarget = false;

        btn.onClick.AddListener(ReturnToMainMenu);
    }
}
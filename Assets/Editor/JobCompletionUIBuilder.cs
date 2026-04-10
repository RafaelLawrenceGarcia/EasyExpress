// ============================================================
//  JobCompletionUIBuilder.cs
//  Easy Express – Editor Tool
// ============================================================
//  Menu: EasyExpress > Build Job Completion UI
//
//  Creates the full Job Completion popup canvas hierarchy
//  matching the dark-themed mockup design. Wires all references
//  to the JobCompletionUI component automatically.
//
//  PLACE THIS FILE IN: Assets/Editor/JobCompletionUIBuilder.cs
// ============================================================

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

public class JobCompletionUIBuilder : Editor
{
    // ═══════════════════════════════════════════════════════════
    //  COLORS
    // ═══════════════════════════════════════════════════════════

    static readonly Color COL_PANEL_BG     = new Color(0.071f, 0.078f, 0.122f, 0.96f);
    static readonly Color COL_CARD_BG      = new Color(0.102f, 0.114f, 0.161f, 1f);
    static readonly Color COL_HEADER_BG    = new Color(0.290f, 0.878f, 1.000f, 0.08f);
    static readonly Color COL_HEADER_BORDER = new Color(0.290f, 0.878f, 1.000f, 0.15f);
    static readonly Color COL_DIVIDER      = new Color(1f, 1f, 1f, 0.06f);
    static readonly Color COL_CYAN         = new Color(0.290f, 0.878f, 1.000f, 1f);
    static readonly Color COL_GOLD         = new Color(1.000f, 0.843f, 0.000f, 1f);
    static readonly Color COL_GREEN        = new Color(0.290f, 1.000f, 0.290f, 1f);
    static readonly Color COL_GREEN_BG     = new Color(0.290f, 1.000f, 0.290f, 0.06f);
    static readonly Color COL_GREEN_BORDER = new Color(0.290f, 1.000f, 0.290f, 0.12f);
    static readonly Color COL_RED          = new Color(1.000f, 0.420f, 0.420f, 1f);
    static readonly Color COL_RED_BG       = new Color(1.000f, 0.420f, 0.420f, 0.06f);
    static readonly Color COL_RED_BORDER   = new Color(1.000f, 0.420f, 0.420f, 0.12f);
    static readonly Color COL_MUTED        = new Color(0.478f, 0.541f, 0.667f, 1f);
    static readonly Color COL_BODY         = new Color(0.753f, 0.784f, 0.867f, 1f);
    static readonly Color COL_WHITE        = new Color(1f, 1f, 1f, 1f);
    static readonly Color COL_BTN_BG       = new Color(0.290f, 0.878f, 1.000f, 0.12f);
    static readonly Color COL_BTN_BORDER   = new Color(0.290f, 0.878f, 1.000f, 0.30f);
    static readonly Color COL_INFO_BG      = new Color(1f, 1f, 1f, 0.03f);
    static readonly Color COL_INFO_BORDER  = new Color(1f, 1f, 1f, 0.06f);
    static readonly Color COL_STAR_OFF     = new Color(0.333f, 0.333f, 0.333f, 1f);
    static readonly Color COL_TRANSPARENT  = new Color(0, 0, 0, 0);

    // ═══════════════════════════════════════════════════════════
    //  MENU ENTRY
    // ═══════════════════════════════════════════════════════════

    [MenuItem("EasyExpress/Build Job Completion UI")]
    public static void Build()
    {
        // ── 1. Find or create a parent canvas ────────────────────
        // Look for an existing canvas named "JobCompletionCanvas"
        // or the monitor's canvas. If none, create a new one.
        Canvas parentCanvas = null;
        GameObject existing = GameObject.Find("JobCompletionCanvas");
        if (existing != null) parentCanvas = existing.GetComponent<Canvas>();

        if (parentCanvas == null)
        {
            GameObject canvasGO = new GameObject("JobCompletionCanvas");
            parentCanvas = canvasGO.AddComponent<Canvas>();
            parentCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            parentCanvas.sortingOrder = 100;

            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGO.AddComponent<GraphicRaycaster>();

            Undo.RegisterCreatedObjectUndo(canvasGO, "Create Job Completion UI");
        }

        // ── 2. Build the hierarchy ───────────────────────────────
        // Full-screen dark overlay
        GameObject overlay = CreateImage(parentCanvas.transform, "CompletionOverlay",
            new Color(0, 0, 0, 0.65f));
        Stretch(overlay);
        CanvasGroup overlayCG = overlay.AddComponent<CanvasGroup>();

        // Center card container (520px wide, auto height)
        GameObject card = CreatePanel(overlay.transform, "CompletionCard", COL_PANEL_BG, 16);
        RectTransform cardRT = card.GetComponent<RectTransform>();
        cardRT.anchorMin = new Vector2(0.5f, 0.5f);
        cardRT.anchorMax = new Vector2(0.5f, 0.5f);
        cardRT.pivot = new Vector2(0.5f, 0.5f);
        cardRT.sizeDelta = new Vector2(520, 0);

        // Outline
        Outline cardOutline = card.AddComponent<Outline>();
        cardOutline.effectColor = new Color(1f, 1f, 1f, 0.08f);
        cardOutline.effectDistance = new Vector2(1, 1);

        // Auto-size height
        ContentSizeFitter cardCSF = card.AddComponent<ContentSizeFitter>();
        cardCSF.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        VerticalLayoutGroup cardVLG = card.AddComponent<VerticalLayoutGroup>();
        cardVLG.spacing = 0;
        cardVLG.childAlignment = TextAnchor.UpperCenter;
        cardVLG.childControlWidth = true;
        cardVLG.childControlHeight = true;
        cardVLG.childForceExpandWidth = true;
        cardVLG.childForceExpandHeight = false;

        // ══════════════════════════════════════════════════════════
        //  HEADER STRIP
        // ══════════════════════════════════════════════════════════

        GameObject header = CreatePanel(card.transform, "HeaderStrip", COL_HEADER_BG, 0);
        AddLE(header, -1, 76);

        // Bottom border on header
        Outline headerBorder = header.AddComponent<Outline>();
        headerBorder.effectColor = COL_HEADER_BORDER;
        headerBorder.effectDistance = new Vector2(0, -1);

        VerticalLayoutGroup headerVLG = header.AddComponent<VerticalLayoutGroup>();
        headerVLG.padding = new RectOffset(28, 28, 18, 14);
        headerVLG.spacing = 6;
        headerVLG.childAlignment = TextAnchor.MiddleCenter;
        headerVLG.childControlWidth = true;
        headerVLG.childControlHeight = false;

        TextMeshProUGUI headerLabel = CreateTMP(header.transform, "HeaderLabel",
            "REPAIR COMPLETE", 13, COL_CYAN, FontStyles.Bold, TextAlignmentOptions.Center);
        headerLabel.characterSpacing = 3;
        AddLE(headerLabel.gameObject, -1, 18);

        TextMeshProUGUI titleTMP = CreateTMP(header.transform, "TitleText",
            "Perfect Repair!", 26, COL_WHITE, FontStyles.Bold, TextAlignmentOptions.Center);
        AddLE(titleTMP.gameObject, -1, 34);

        // ══════════════════════════════════════════════════════════
        //  STARS ROW
        // ══════════════════════════════════════════════════════════

        GameObject starsRow = CreatePanel(card.transform, "StarsRow", COL_TRANSPARENT, 0);
        AddLE(starsRow, -1, 56);

        HorizontalLayoutGroup starsHLG = starsRow.AddComponent<HorizontalLayoutGroup>();
        starsHLG.spacing = 8;
        starsHLG.childAlignment = TextAnchor.MiddleCenter;
        starsHLG.childControlWidth = false;
        starsHLG.childControlHeight = false;
        starsHLG.padding = new RectOffset(0, 0, 12, 4);

        TextMeshProUGUI[] starArray = new TextMeshProUGUI[5];
        for (int i = 0; i < 5; i++)
        {
            TextMeshProUGUI star = CreateTMP(starsRow.transform, $"Star_{i}",
                "\u2605", 36, COL_GOLD, FontStyles.Normal, TextAlignmentOptions.Center);
            AddLE(star.gameObject, 40, 40);
            starArray[i] = star;
        }

        // ══════════════════════════════════════════════════════════
        //  BODY CONTAINER
        // ══════════════════════════════════════════════════════════

        GameObject body = CreatePanel(card.transform, "Body", COL_TRANSPARENT, 0);

        VerticalLayoutGroup bodyVLG = body.AddComponent<VerticalLayoutGroup>();
        bodyVLG.padding = new RectOffset(28, 28, 12, 20);
        bodyVLG.spacing = 0;
        bodyVLG.childAlignment = TextAnchor.UpperCenter;
        bodyVLG.childControlWidth = true;
        bodyVLG.childControlHeight = true;
        bodyVLG.childForceExpandWidth = true;
        bodyVLG.childForceExpandHeight = false;

        // ── Customer Info Card ───────────────────────────────────
        GameObject infoCard = CreatePanel(body.transform, "CustomerInfoCard", COL_INFO_BG, 10);
        Outline infoBorder = infoCard.AddComponent<Outline>();
        infoBorder.effectColor = COL_INFO_BORDER;
        infoBorder.effectDistance = new Vector2(1, 1);
        AddLE(infoCard, -1, 76);

        VerticalLayoutGroup infoVLG = infoCard.AddComponent<VerticalLayoutGroup>();
        infoVLG.padding = new RectOffset(18, 18, 14, 14);
        infoVLG.spacing = 8;
        infoVLG.childControlWidth = true;
        infoVLG.childControlHeight = false;

        // Customer row
        GameObject custRow = CreateHRow(infoCard.transform, "CustomerRow");
        AddLE(custRow, -1, 20);
        TextMeshProUGUI custLabel = CreateTMP(custRow.transform, "Label",
            "Customer", 12, COL_MUTED, FontStyles.Normal, TextAlignmentOptions.Left);
        AddLE(custLabel.gameObject, -1, -1, 1);
        TextMeshProUGUI custName = CreateTMP(custRow.transform, "Value",
            "James Garcia", 14, COL_WHITE, FontStyles.Bold, TextAlignmentOptions.Right);
        AddLE(custName.gameObject, -1, -1, 1);

        // Problem row
        GameObject probRow = CreateHRow(infoCard.transform, "ProblemRow");
        AddLE(probRow, -1, 20);
        TextMeshProUGUI probLabel = CreateTMP(probRow.transform, "Label",
            "Problem", 12, COL_MUTED, FontStyles.Normal, TextAlignmentOptions.Left);
        AddLE(probLabel.gameObject, -1, -1, 1);
        TextMeshProUGUI probValue = CreateTMP(probRow.transform, "Value",
            "No Display on Monitor", 14, COL_BODY, FontStyles.Normal, TextAlignmentOptions.Right);
        AddLE(probValue.gameObject, -1, -1, 1);

        AddSpacer(body.transform, 16);

        // ── Status: Resolved ─────────────────────────────────────
        GameObject resolvedPanel = CreatePanel(body.transform, "StatusResolved", COL_GREEN_BG, 10);
        Outline resolvedBorder = resolvedPanel.AddComponent<Outline>();
        resolvedBorder.effectColor = COL_GREEN_BORDER;
        resolvedBorder.effectDistance = new Vector2(1, 1);
        AddLE(resolvedPanel, -1, 44);

        VerticalLayoutGroup resolvedVLG = resolvedPanel.AddComponent<VerticalLayoutGroup>();
        resolvedVLG.padding = new RectOffset(18, 18, 12, 12);
        resolvedVLG.childAlignment = TextAnchor.MiddleCenter;
        resolvedVLG.childControlWidth = true;
        resolvedVLG.childControlHeight = false;

        TextMeshProUGUI resolvedText = CreateTMP(resolvedPanel.transform, "ResolvedText",
            "All issues resolved!", 14, COL_GREEN, FontStyles.Normal, TextAlignmentOptions.Center);
        AddLE(resolvedText.gameObject, -1, 20);

        // ── Status: Issues ───────────────────────────────────────
        GameObject issuesPanel = CreatePanel(body.transform, "StatusIssues", COL_RED_BG, 10);
        Outline issuesBorder = issuesPanel.AddComponent<Outline>();
        issuesBorder.effectColor = COL_RED_BORDER;
        issuesBorder.effectDistance = new Vector2(1, 1);
        AddLE(issuesPanel, -1, -1);
        issuesPanel.SetActive(false); // hidden by default

        ContentSizeFitter issuesCSF = issuesPanel.AddComponent<ContentSizeFitter>();
        issuesCSF.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        VerticalLayoutGroup issuesVLG = issuesPanel.AddComponent<VerticalLayoutGroup>();
        issuesVLG.padding = new RectOffset(18, 18, 12, 12);
        issuesVLG.spacing = 6;
        issuesVLG.childAlignment = TextAnchor.UpperLeft;
        issuesVLG.childControlWidth = true;
        issuesVLG.childControlHeight = true;

        TextMeshProUGUI issuesLabel = CreateTMP(issuesPanel.transform, "IssuesLabel",
            "Unfixed issues:", 12, COL_RED, FontStyles.Bold, TextAlignmentOptions.Left);
        AddLE(issuesLabel.gameObject, -1, 16);

        TextMeshProUGUI issuesBody = CreateTMP(issuesPanel.transform, "IssuesBody",
            "• Fan: fan bearing worn out", 13, new Color(1f, 0.533f, 0.533f, 1f),
            FontStyles.Normal, TextAlignmentOptions.Left);
        AddLE(issuesBody.gameObject, -1, -1);

        // ── Divider 1 ────────────────────────────────────────────
        AddSpacer(body.transform, 16);
        CreateDivider(body.transform);
        AddSpacer(body.transform, 16);

        // ══════════════════════════════════════════════════════════
        //  PAYMENT BREAKDOWN
        // ══════════════════════════════════════════════════════════

        // Base reward row
        GameObject baseRow = CreateHRow(body.transform, "BaseRewardRow");
        AddLE(baseRow, -1, 22);
        CreateTMP(baseRow.transform, "Label", "Base reward", 13, COL_MUTED,
            FontStyles.Normal, TextAlignmentOptions.Left);
        TextMeshProUGUI baseVal = CreateTMP(baseRow.transform, "Value", "₱12,500", 14, COL_BODY,
            FontStyles.Normal, TextAlignmentOptions.Right);

        AddSpacer(body.transform, 6);

        // Score row
        GameObject scoreRow = CreateHRow(body.transform, "ScoreRow");
        AddLE(scoreRow, -1, 22);
        TextMeshProUGUI scoreLbl = CreateTMP(scoreRow.transform, "Label", "Score", 13, COL_MUTED,
            FontStyles.Normal, TextAlignmentOptions.Left);
        TextMeshProUGUI scoreVal = CreateTMP(scoreRow.transform, "Value", "100%", 14, COL_GREEN,
            FontStyles.Normal, TextAlignmentOptions.Right);

        AddSpacer(body.transform, 6);

        // Earned reward row
        GameObject earnedRow = CreateHRow(body.transform, "EarnedRewardRow");
        AddLE(earnedRow, -1, 22);
        CreateTMP(earnedRow.transform, "Label", "Earned reward", 13, COL_MUTED,
            FontStyles.Normal, TextAlignmentOptions.Left);
        TextMeshProUGUI earnedVal = CreateTMP(earnedRow.transform, "Value", "₱12,500", 14, COL_WHITE,
            FontStyles.Normal, TextAlignmentOptions.Right);

        AddSpacer(body.transform, 6);

        // Tip row (hidden if no tip)
        GameObject tipRowGO = CreateHRow(body.transform, "TipRow");
        AddLE(tipRowGO, -1, 22);
        TextMeshProUGUI tipLbl = CreateTMP(tipRowGO.transform, "Label",
            "Customer tip (5★ bonus)", 13, COL_GOLD, FontStyles.Normal, TextAlignmentOptions.Left);
        TextMeshProUGUI tipVal = CreateTMP(tipRowGO.transform, "Value",
            "+₱3,125", 14, COL_GOLD, FontStyles.Normal, TextAlignmentOptions.Right);

        // ── Divider 2 ────────────────────────────────────────────
        AddSpacer(body.transform, 12);
        CreateDivider(body.transform);
        AddSpacer(body.transform, 12);

        // ══════════════════════════════════════════════════════════
        //  TOTAL
        // ══════════════════════════════════════════════════════════

        GameObject totalRow = CreateHRow(body.transform, "TotalRow");
        AddLE(totalRow, -1, 38);
        CreateTMP(totalRow.transform, "Label", "Total earned", 15, COL_WHITE,
            FontStyles.Bold, TextAlignmentOptions.Left);
        TextMeshProUGUI totalVal = CreateTMP(totalRow.transform, "Value", "₱15,625", 28, COL_GREEN,
            FontStyles.Bold, TextAlignmentOptions.Right);

        AddSpacer(body.transform, 20);

        // ══════════════════════════════════════════════════════════
        //  COLLECT BUTTON
        // ══════════════════════════════════════════════════════════

        GameObject btnContainer = CreatePanel(body.transform, "ButtonContainer", COL_TRANSPARENT, 0);
        AddLE(btnContainer, -1, 52);

        HorizontalLayoutGroup btnHLG = btnContainer.AddComponent<HorizontalLayoutGroup>();
        btnHLG.childAlignment = TextAnchor.MiddleCenter;
        btnHLG.childControlWidth = false;
        btnHLG.childControlHeight = false;

        GameObject btnGO = CreatePanel(btnContainer.transform, "CollectButton", COL_BTN_BG, 10);
        AddLE(btnGO, 260, 48);

        Outline btnBorder = btnGO.AddComponent<Outline>();
        btnBorder.effectColor = COL_BTN_BORDER;
        btnBorder.effectDistance = new Vector2(1, 1);

        Button collectBtn = btnGO.AddComponent<Button>();
        ColorBlock cb = collectBtn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1.2f, 1.2f, 1.2f, 1f);
        cb.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        collectBtn.colors = cb;

        TextMeshProUGUI btnText = CreateTMP(btnGO.transform, "ButtonText",
            "COLLECT PAYMENT", 16, COL_CYAN, FontStyles.Bold, TextAlignmentOptions.Center);
        btnText.characterSpacing = 1;
        Stretch(btnText.gameObject);
        btnText.raycastTarget = false;

        // ══════════════════════════════════════════════════════════
        //  WIRE COMPONENT
        // ══════════════════════════════════════════════════════════

        JobCompletionUI comp = overlay.AddComponent<JobCompletionUI>();
        comp.rootPanel       = overlay;
        comp.rootCanvasGroup = overlayCG;

        comp.headerStrip     = header.GetComponent<Image>();
        comp.headerLabel     = headerLabel;
        comp.titleText       = titleTMP;

        comp.starTexts       = starArray;

        comp.customerNameText = custName;
        comp.problemText      = probValue;

        comp.statusResolvedPanel = resolvedPanel;
        comp.statusResolvedText  = resolvedText;
        comp.statusIssuesPanel   = issuesPanel;
        comp.statusIssuesLabel   = issuesLabel;
        comp.statusIssuesBody    = issuesBody;

        comp.baseRewardValue   = baseVal;
        comp.scoreLabel        = scoreLbl;
        comp.scoreValue        = scoreVal;
        comp.earnedRewardValue = earnedVal;
        comp.tipLabel          = tipLbl;
        comp.tipValue          = tipVal;
        comp.tipRow            = tipRowGO;

        comp.totalValue        = totalVal;

        comp.collectButton     = collectBtn;
        comp.collectButtonText = btnText;

        // Legacy fields (for EmailManager backward compat)
        comp.legacyTitle   = titleTMP;
        comp.legacyRating  = null; // stars are individual now
        comp.legacyDetails = null; // structured fields replace this
        comp.legacyPay     = null; // structured fields replace this

        // Start hidden
        overlay.SetActive(false);

        // Select so user can see it
        Selection.activeGameObject = overlay;

        Debug.Log("[JobCompletionUIBuilder] ✓ Job Completion UI created!\n" +
                  "Parent: " + parentCanvas.gameObject.name + "\n" +
                  "To use: JobCompletionUI.Instance.Show(result);\n" +
                  "Or wire completionPanel on EmailManager to the CompletionOverlay.");

        EditorUtility.DisplayDialog(
            "Job Completion UI — Built!",
            "The UI has been created under '" + parentCanvas.gameObject.name + "'.\n\n" +
            "NEXT STEPS:\n" +
            "1. In EmailManager, drag 'CompletionOverlay' into completionPanel\n" +
            "2. Drag 'CollectButton' into completionOKButton\n" +
            "3. If using MonitorShopBridge, also wire the bridge fields\n\n" +
            "OR: Replace the ShowCompletionPopup call with:\n" +
            "   JobCompletionUI.Instance.Show(result);\n\n" +
            "The old legacy fields (completionTitle, completionDetails, etc.)\n" +
            "still work as fallback.",
            "Got it!");
    }

    // ═══════════════════════════════════════════════════════════
    //  BUILDER HELPERS
    // ═══════════════════════════════════════════════════════════

    static GameObject CreateImage(Transform parent, string name, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = color;
        return go;
    }

    static GameObject CreatePanel(Transform parent, string name, Color color, int radius)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        Image img = go.GetComponent<Image>();
        img.color = color;
        return go;
    }

    static void Stretch(GameObject go)
    {
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static TextMeshProUGUI CreateTMP(Transform parent, string name, string text,
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

    static GameObject CreateHRow(Transform parent, string name)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        HorizontalLayoutGroup hlg = go.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = false;

        return go;
    }

    static void CreateDivider(Transform parent)
    {
        GameObject div = new GameObject("Divider", typeof(RectTransform), typeof(Image));
        div.transform.SetParent(parent, false);
        div.GetComponent<Image>().color = COL_DIVIDER;
        AddLE(div, -1, 1);
    }

    static void AddSpacer(Transform parent, float height)
    {
        GameObject go = new GameObject("Spacer", typeof(RectTransform), typeof(LayoutElement));
        go.transform.SetParent(parent, false);
        go.GetComponent<LayoutElement>().preferredHeight = height;
    }

    static void AddLE(GameObject go, float width, float height, float flexW = -1)
    {
        LayoutElement le = go.GetComponent<LayoutElement>();
        if (le == null) le = go.AddComponent<LayoutElement>();
        if (width >= 0) le.preferredWidth = width;
        if (height >= 0) le.preferredHeight = height;
        if (flexW >= 0) le.flexibleWidth = flexW;
    }
}
#endif

// ============================================================
//  EasyExpressCanvasBuilder.cs
//  
//  EDITOR SCRIPT — Place in Assets/Editor/
//
//  Menu: EasyExpress > Build Repair Manual Canvas
//  Menu: EasyExpress > Build Component Summary Panel
//
//  Creates the full UI hierarchy with all components, colors,
//  layout groups, scroll views, and wiring pre-configured.
//  After building, just drag references into the script fields.
// ============================================================
#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

public class EasyExpressCanvasBuilder : Editor
{
    // ─── Color palette (matches game dark UI) ────────────────────
    static readonly Color BG_DARK = new Color(0.063f, 0.078f, 0.165f, 0.94f);  // #101428
    static readonly Color BG_HEADER = new Color(0.078f, 0.094f, 0.196f, 1f);     // #141832
    static readonly Color BG_CARD = new Color(0.082f, 0.102f, 0.208f, 1f);     // #151A35
    static readonly Color BG_ENTRY = new Color(0.078f, 0.094f, 0.196f, 1f);     // #141832
    static readonly Color BG_METRICS = new Color(0.082f, 0.082f, 0.133f, 1f);     // #141422
    static readonly Color BG_FOOTER = new Color(0.055f, 0.071f, 0.145f, 1f);     // #0E1225
    static readonly Color BG_BUTTON = new Color(0.102f, 0.125f, 0.251f, 1f);     // #1A2040
    static readonly Color BG_BUTTON_HVR = new Color(0.118f, 0.157f, 0.314f, 1f);     // #1E2850
    static readonly Color BORDER = new Color(0.145f, 0.176f, 0.314f, 1f);     // #252D50
    static readonly Color TEXT_PRIMARY = new Color(0.91f, 0.93f, 0.96f, 1f);        // #E8ECF4
    static readonly Color TEXT_SECONDARY = new Color(0.478f, 0.541f, 0.667f, 1f);     // #7A8AAA
    static readonly Color TEXT_MUTED = new Color(0.353f, 0.412f, 0.565f, 1f);     // #5A6A90
    static readonly Color ACCENT_BLUE = new Color(0.357f, 0.616f, 0.961f, 1f);     // #5B9DF5
    static readonly Color ACCENT_GREEN = new Color(0.102f, 0.478f, 0.353f, 1f);     // #1A7A5A
    static readonly Color CLOSE_RED = new Color(0.753f, 0.224f, 0.169f, 1f);     // #C0392B
    static readonly Color SCROLLBAR_BG = new Color(0.071f, 0.086f, 0.165f, 1f);     // #12162A
    static readonly Color SCROLLBAR_HDL = new Color(0.165f, 0.196f, 0.345f, 1f);     // #2A3258

    // ═══════════════════════════════════════════════════════════
    //  REPAIR MANUAL CANVAS
    // ═══════════════════════════════════════════════════════════

    [MenuItem("EasyExpress/Build Repair Manual Canvas")]
    static void BuildRepairManualCanvas()
    {
        // ── Canvas ──
        GameObject canvasObj = CreateCanvas("ManualCanvas", 150);

        // ── ManualPanel (root panel — full screen overlay) ──
        GameObject panel = CreatePanel(canvasObj.transform, "ManualPanel", BG_DARK);
        Stretch(panel);
        SetPadding(panel, 80, 80, 60, 60); // L, R, T, B

        VerticalLayoutGroup vlg = panel.AddComponent<VerticalLayoutGroup>();
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.spacing = 0;
        vlg.padding = new RectOffset(0, 0, 0, 0);

        // ── Header bar ──
        GameObject header = CreatePanel(panel.transform, "Header", BG_HEADER);
        SetSize(header, 0, 56);
        LayoutElement headerLE = header.AddComponent<LayoutElement>();
        headerLE.minHeight = 56;
        headerLE.preferredHeight = 56;
        headerLE.flexibleWidth = 1;

        HorizontalLayoutGroup headerHLG = header.AddComponent<HorizontalLayoutGroup>();
        headerHLG.childForceExpandWidth = false;
        headerHLG.childForceExpandHeight = false;
        headerHLG.childControlWidth = false;
        headerHLG.childControlHeight = false;
        headerHLG.childAlignment = TextAnchor.MiddleLeft;
        headerHLG.padding = new RectOffset(18, 18, 0, 0);
        headerHLG.spacing = 10;

        // Icon
        GameObject iconBg = CreatePanel(header.transform, "IconBg", new Color(0.173f, 0.435f, 0.808f, 1f));
        SetSize(iconBg, 32, 32);
        AddRoundedCorners(iconBg, 6);

        // Title
        CreateTMPText(header.transform, "Title", "REPAIR MANUAL", 16, TEXT_PRIMARY, FontStyles.Bold);

        // Spacer
        GameObject spacer = new GameObject("Spacer", typeof(RectTransform), typeof(LayoutElement));
        spacer.transform.SetParent(header.transform, false);
        spacer.GetComponent<LayoutElement>().flexibleWidth = 1;

        // Close button
        GameObject closeBtn = CreateButton(header.transform, "CloseButton", "✕", 28, 28, CLOSE_RED, Color.white);

        // Add bottom border to header
        AddBorder(header, BORDER, RectTransform.Edge.Bottom, 1);

        // ── Category scroll area ──
        GameObject catScrollView = CreateScrollView(panel.transform, "CategoryScrollView", 0);
        LayoutElement catLE = catScrollView.AddComponent<LayoutElement>();
        catLE.flexibleHeight = 1;
        catLE.flexibleWidth = 1;

        Transform catContent = catScrollView.transform.Find("Viewport/Content");
        if (catContent != null)
        {
            VerticalLayoutGroup catVLG = catContent.gameObject.AddComponent<VerticalLayoutGroup>();
            catVLG.childForceExpandWidth = true;
            catVLG.childForceExpandHeight = false;
            catVLG.childControlWidth = true;
            catVLG.childControlHeight = false;
            catVLG.spacing = 6;
            catVLG.padding = new RectOffset(16, 16, 12, 12);

            ContentSizeFitter catCSF = catContent.gameObject.AddComponent<ContentSizeFitter>();
            catCSF.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        // ── Procedure panel (hidden by default) ──
        GameObject procPanel = CreatePanel(panel.transform, "ProcedurePanel", BG_DARK);
        procPanel.SetActive(false);
        LayoutElement procLE = procPanel.AddComponent<LayoutElement>();
        procLE.flexibleHeight = 1;
        procLE.flexibleWidth = 1;

        VerticalLayoutGroup procVLG = procPanel.AddComponent<VerticalLayoutGroup>();
        procVLG.childForceExpandWidth = true;
        procVLG.childForceExpandHeight = false;
        procVLG.childControlWidth = true;
        procVLG.childControlHeight = false;
        procVLG.spacing = 8;
        procVLG.padding = new RectOffset(20, 20, 14, 14);

        // Back button
        GameObject backBtn = CreateButton(procPanel.transform, "BackButton", "← Back to categories", 200, 36, BG_BUTTON, ACCENT_BLUE);
        LayoutElement backLE = backBtn.AddComponent<LayoutElement>();
        backLE.minHeight = 36;
        backLE.preferredHeight = 36;

        // Procedure title
        GameObject procTitle = CreateTMPText(procPanel.transform, "ProcedureTitle", "Problem Title", 18, TEXT_PRIMARY, FontStyles.Bold);
        LayoutElement ptLE = procTitle.AddComponent<LayoutElement>();
        ptLE.minHeight = 30;

        // Procedure body scroll
        GameObject procScroll = CreateScrollView(procPanel.transform, "ProcedureScrollView", 0);
        LayoutElement psLE = procScroll.AddComponent<LayoutElement>();
        psLE.flexibleHeight = 1;
        psLE.flexibleWidth = 1;

        Transform procBodyParent = procScroll.transform.Find("Viewport/Content");
        if (procBodyParent != null)
        {
            ContentSizeFitter pbCSF = procBodyParent.gameObject.AddComponent<ContentSizeFitter>();
            pbCSF.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            GameObject procBody = CreateTMPText(procBodyParent, "ProcedureBody", "Steps will appear here...", 14, new Color(0.66f, 0.69f, 0.78f), FontStyles.Normal);
            TextMeshProUGUI pbTMP = procBody.GetComponent<TextMeshProUGUI>();
            pbTMP.enableWordWrapping = true;
            pbTMP.richText = true;
            pbTMP.overflowMode = TextOverflowModes.Overflow;
            LayoutElement pbLE = procBody.AddComponent<LayoutElement>();
            pbLE.flexibleWidth = 1;
        }

        // ── Footer ──
        GameObject footer = CreatePanel(panel.transform, "Footer", BG_FOOTER);
        LayoutElement footLE = footer.AddComponent<LayoutElement>();
        footLE.minHeight = 32;
        footLE.preferredHeight = 32;
        footLE.flexibleWidth = 1;
        AddBorder(footer, BORDER, RectTransform.Edge.Top, 1);

        GameObject footerText = CreateTMPText(footer.transform, "FooterText", "Press [F] to close  •  Press [Esc] to exit", 11, TEXT_MUTED, FontStyles.Normal);
        RectTransform ftRT = footerText.GetComponent<RectTransform>();
        Stretch(footerText);
        footerText.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

        // ── Wire up RepairManual component ──
        RepairManual manual = panel.AddComponent<RepairManual>();
        manual.manualPanel = panel;
        manual.categoryContent = catContent;
        manual.procedurePanel = procPanel;
        manual.procedureTitle = procPanel.transform.Find("ProcedureTitle")?.GetComponent<TextMeshProUGUI>();
        // ProcedureBody is inside the scroll view
        Transform pbTransform = procScroll.transform.Find("Viewport/Content/ProcedureBody");
        if (pbTransform != null)
            manual.procedureBody = pbTransform.GetComponent<TextMeshProUGUI>();
        manual.backButton = backBtn.GetComponent<Button>();
        manual.closeButton = closeBtn.GetComponent<Button>();

        Selection.activeGameObject = canvasObj;
        Debug.Log("[EasyExpress] Repair Manual canvas created! Select 'ManualCanvas' in Hierarchy.");
        EditorUtility.DisplayDialog("Done", "Repair Manual canvas created.\n\nIt's been added to the scene root.\nThe RepairManual script is already attached and wired up.", "OK");
    }

    // ═══════════════════════════════════════════════════════════
    //  COMPONENT SUMMARY PANEL
    //  (Goes inside your Inspection UI canvas, not a new canvas)
    // ═══════════════════════════════════════════════════════════

    [MenuItem("EasyExpress/Build Component Summary Panel")]
    static void BuildComponentSummaryPanel()
    {
        // ── Canvas ──
        GameObject canvasObj = CreateCanvas("SummaryCanvas", 140);

        // ── SummaryPanel ──
        GameObject panel = CreatePanel(canvasObj.transform, "SummaryPanel", BG_DARK);
        Stretch(panel);
        SetPadding(panel, 100, 100, 50, 50);

        VerticalLayoutGroup vlg = panel.AddComponent<VerticalLayoutGroup>();
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.spacing = 0;

        // ── Header ──
        GameObject header = CreatePanel(panel.transform, "Header", BG_HEADER);
        LayoutElement hLE = header.AddComponent<LayoutElement>();
        hLE.minHeight = 50;
        hLE.preferredHeight = 50;
        hLE.flexibleWidth = 1;

        HorizontalLayoutGroup hHLG = header.AddComponent<HorizontalLayoutGroup>();
        hHLG.childForceExpandWidth = false;
        hHLG.childForceExpandHeight = false;
        hHLG.childControlWidth = false;
        hHLG.childControlHeight = false;
        hHLG.childAlignment = TextAnchor.MiddleLeft;
        hHLG.padding = new RectOffset(16, 16, 0, 0);
        hHLG.spacing = 10;

        GameObject iconBg = CreatePanel(header.transform, "IconBg", ACCENT_GREEN);
        SetSize(iconBg, 28, 28);
        AddRoundedCorners(iconBg, 6);

        CreateTMPText(header.transform, "SummaryTitle", "PC COMPONENT SUMMARY", 15, TEXT_PRIMARY, FontStyles.Bold);

        GameObject spacer1 = new GameObject("Spacer", typeof(RectTransform), typeof(LayoutElement));
        spacer1.transform.SetParent(header.transform, false);
        spacer1.GetComponent<LayoutElement>().flexibleWidth = 1;

        GameObject closeBtn = CreateButton(header.transform, "CloseBtn", "✕", 26, 26, CLOSE_RED, Color.white);
        AddBorder(header, BORDER, RectTransform.Edge.Bottom, 1);

        // ── Section label: Installed Components ──
        GameObject secLabel = CreateTMPText(panel.transform, "SectionLabel", "INSTALLED COMPONENTS", 11, TEXT_MUTED, FontStyles.Normal);
        LayoutElement slLE = secLabel.AddComponent<LayoutElement>();
        slLE.minHeight = 34;
        slLE.preferredHeight = 34;
        TextMeshProUGUI slTMP = secLabel.GetComponent<TextMeshProUGUI>();
        slTMP.margin = new Vector4(18, 18, 12, 0);
        slTMP.characterSpacing = 2;

        // ── Parts scroll area ──
        GameObject partsScroll = CreateScrollView(panel.transform, "PartsScrollView", 0);
        LayoutElement psLE = partsScroll.AddComponent<LayoutElement>();
        psLE.flexibleHeight = 1;
        psLE.flexibleWidth = 1;
        psLE.minHeight = 120;

        Transform partsContent = partsScroll.transform.Find("Viewport/Content");
        if (partsContent != null)
        {
            VerticalLayoutGroup pcVLG = partsContent.gameObject.AddComponent<VerticalLayoutGroup>();
            pcVLG.childForceExpandWidth = true;
            pcVLG.childForceExpandHeight = false;
            pcVLG.childControlWidth = true;
            pcVLG.childControlHeight = false;
            pcVLG.spacing = 4;
            pcVLG.padding = new RectOffset(16, 16, 4, 8);

            ContentSizeFitter pcCSF = partsContent.gameObject.AddComponent<ContentSizeFitter>();
            pcCSF.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        // ── Divider ──
        GameObject divider = CreatePanel(panel.transform, "Divider", BORDER);
        LayoutElement divLE = divider.AddComponent<LayoutElement>();
        divLE.minHeight = 1;
        divLE.preferredHeight = 1;
        divLE.flexibleWidth = 1;

        // ── Section label: Metrics ──
        GameObject metLabel = CreateTMPText(panel.transform, "MetricsLabel", "SYSTEM METRICS", 11, TEXT_MUTED, FontStyles.Normal);
        LayoutElement mlLE = metLabel.AddComponent<LayoutElement>();
        mlLE.minHeight = 34;
        mlLE.preferredHeight = 34;
        TextMeshProUGUI mlTMP = metLabel.GetComponent<TextMeshProUGUI>();
        mlTMP.margin = new Vector4(18, 18, 12, 0);
        mlTMP.characterSpacing = 2;

        // ── Metrics container ──
        GameObject metricsContainer = CreatePanel(panel.transform, "MetricsContainer", Color.clear);
        LayoutElement mcLE = metricsContainer.AddComponent<LayoutElement>();
        mcLE.minHeight = 160;
        mcLE.preferredHeight = 180;
        mcLE.flexibleWidth = 1;

        VerticalLayoutGroup mcVLG = metricsContainer.AddComponent<VerticalLayoutGroup>();
        mcVLG.childForceExpandWidth = true;
        mcVLG.childForceExpandHeight = false;
        mcVLG.childControlWidth = true;
        mcVLG.childControlHeight = false;
        mcVLG.spacing = 8;
        mcVLG.padding = new RectOffset(16, 16, 0, 12);

        // Power card
        GameObject powerCard = CreatePanel(metricsContainer.transform, "PowerCard", BG_METRICS);
        LayoutElement powLE = powerCard.AddComponent<LayoutElement>();
        powLE.minHeight = 50;
        powLE.preferredHeight = 55;
        powLE.flexibleWidth = 1;
        AddRoundedCorners(powerCard, 8);
        AddOutline(powerCard, BORDER);

        GameObject powerText = CreateTMPText(powerCard.transform, "PowerText", "<b>POWER:</b>  Loading...", 13, new Color(0.66f, 0.69f, 0.78f), FontStyles.Normal);
        Stretch(powerText);
        TextMeshProUGUI pwTMP = powerText.GetComponent<TextMeshProUGUI>();
        pwTMP.margin = new Vector4(14, 14, 10, 10);
        pwTMP.richText = true;
        pwTMP.enableWordWrapping = true;

        // Compat card
        GameObject compatCard = CreatePanel(metricsContainer.transform, "CompatCard", BG_METRICS);
        LayoutElement cmpLE = compatCard.AddComponent<LayoutElement>();
        cmpLE.minHeight = 50;
        cmpLE.preferredHeight = 55;
        cmpLE.flexibleWidth = 1;
        AddRoundedCorners(compatCard, 8);
        AddOutline(compatCard, BORDER);

        GameObject compatText = CreateTMPText(compatCard.transform, "CompatText", "<b>COMPATIBILITY:</b>  Loading...", 13, new Color(0.66f, 0.69f, 0.78f), FontStyles.Normal);
        Stretch(compatText);
        TextMeshProUGUI cmTMP = compatText.GetComponent<TextMeshProUGUI>();
        cmTMP.margin = new Vector4(14, 14, 10, 10);
        cmTMP.richText = true;
        cmTMP.enableWordWrapping = true;

        // Health card
        GameObject healthCard = CreatePanel(metricsContainer.transform, "HealthCard", BG_METRICS);
        LayoutElement hlthLE = healthCard.AddComponent<LayoutElement>();
        hlthLE.minHeight = 50;
        hlthLE.preferredHeight = 55;
        hlthLE.flexibleWidth = 1;
        AddRoundedCorners(healthCard, 8);
        AddOutline(healthCard, BORDER);

        GameObject healthText = CreateTMPText(healthCard.transform, "HealthText", "<b>SYSTEM:</b>  Loading...", 13, new Color(0.66f, 0.69f, 0.78f), FontStyles.Normal);
        Stretch(healthText);
        TextMeshProUGUI htTMP = healthText.GetComponent<TextMeshProUGUI>();
        htTMP.margin = new Vector4(14, 14, 10, 10);
        htTMP.richText = true;
        htTMP.enableWordWrapping = true;

        // ── Footer ──
        GameObject footer = CreatePanel(panel.transform, "Footer", BG_FOOTER);
        LayoutElement ftLE = footer.AddComponent<LayoutElement>();
        ftLE.minHeight = 28;
        ftLE.preferredHeight = 28;
        ftLE.flexibleWidth = 1;
        AddBorder(footer, BORDER, RectTransform.Edge.Top, 1);

        GameObject footerText = CreateTMPText(footer.transform, "FooterText", "Press [G] to close  •  [F] Repair manual", 11, TEXT_MUTED, FontStyles.Normal);
        Stretch(footerText);
        footerText.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

        // ── Wire up PCComponentSummary ──
        PCComponentSummary summary = panel.AddComponent<PCComponentSummary>();
        summary.summaryPanel = panel;
        summary.contentContainer = partsContent;
        summary.closeButton = closeBtn.GetComponent<Button>();

        Selection.activeGameObject = canvasObj;
        Debug.Log("[EasyExpress] Component Summary canvas created!");
        EditorUtility.DisplayDialog("Done", "Component Summary canvas created.\n\nPCComponentSummary script is attached and wired up.\nPanel starts hidden — press [G] during Inspect Mode.", "OK");
    }

    // ═══════════════════════════════════════════════════════════
    //  CATEGORY BUTTON PREFAB (for manual)
    // ═══════════════════════════════════════════════════════════

    [MenuItem("EasyExpress/Build Manual Category Button Prefab")]
    static void BuildCategoryButtonPrefab()
    {
        // Create button object
        GameObject btn = new GameObject("CategoryButton", typeof(RectTransform), typeof(Image), typeof(Button));

        RectTransform rt = btn.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 50);

        Image img = btn.GetComponent<Image>();
        img.color = BG_BUTTON;

        // Rounded corners via a basic approach
        AddRoundedCorners(btn, 8);

        LayoutElement le = btn.AddComponent<LayoutElement>();
        le.minHeight = 50;
        le.preferredHeight = 50;
        le.flexibleWidth = 1;

        HorizontalLayoutGroup hlg = btn.AddComponent<HorizontalLayoutGroup>();
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.padding = new RectOffset(14, 14, 0, 0);
        hlg.spacing = 12;

        // Icon area
        GameObject iconArea = CreatePanel(btn.transform, "IconArea", new Color(0.3f, 0.3f, 0.4f, 0.2f));
        SetSize(iconArea, 32, 32);
        AddRoundedCorners(iconArea, 6);

        // Label
        GameObject label = CreateTMPText(btn.transform, "Label", "Category Name", 14, new Color(0.78f, 0.82f, 0.91f), FontStyles.Normal);
        LayoutElement lblLE = label.AddComponent<LayoutElement>();
        lblLE.flexibleWidth = 1;

        // Arrow
        CreateTMPText(btn.transform, "Arrow", "›", 18, TEXT_MUTED, FontStyles.Normal);

        // Save as prefab
        string path = "Assets/Prefabs";
        if (!AssetDatabase.IsValidFolder(path))
            AssetDatabase.CreateFolder("Assets", "Prefabs");

        string prefabPath = path + "/ManualCategoryButton.prefab";
        PrefabUtility.SaveAsPrefabAsset(btn, prefabPath);
        DestroyImmediate(btn);

        Debug.Log($"[EasyExpress] Category button prefab saved to {prefabPath}");
        EditorUtility.DisplayDialog("Done", $"Prefab saved to:\n{prefabPath}\n\nDrag it into RepairManual's 'categoryButtonPrefab' field.", "OK");
    }

    // ═══════════════════════════════════════════════════════════
    //  BUILDER HELPERS
    // ═══════════════════════════════════════════════════════════

    static GameObject CreateCanvas(string name, int sortOrder)
    {
        GameObject canvasObj = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));

        Canvas canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortOrder;

        CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        Undo.RegisterCreatedObjectUndo(canvasObj, "Create Canvas");
        return canvasObj;
    }

    static GameObject CreatePanel(Transform parent, string name, Color bgColor)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(parent, false);

        Image img = panel.GetComponent<Image>();
        img.color = bgColor;

        return panel;
    }

    static GameObject CreateTMPText(Transform parent, string name, string text, int fontSize, Color color, FontStyles style)
    {
        GameObject textObj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(parent, false);

        TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.fontStyle = style;
        tmp.enableWordWrapping = true;
        tmp.richText = true;
        tmp.overflowMode = TextOverflowModes.Overflow;

        return textObj;
    }

    static GameObject CreateButton(Transform parent, string name, string label, float w, float h, Color bgColor, Color textColor)
    {
        GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(parent, false);

        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(w, h);

        Image img = btnObj.GetComponent<Image>();
        img.color = bgColor;

        AddRoundedCorners(btnObj, 6);

        // Button color transitions
        Button btn = btnObj.GetComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1, 1, 1, 0.9f);
        colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        btn.colors = colors;

        // Label text
        GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(btnObj.transform, false);
        Stretch(textObj);

        TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 13;
        tmp.color = textColor;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = false;

        return btnObj;
    }

    static GameObject CreateScrollView(Transform parent, string name, float minHeight)
    {
        // ScrollView root
        GameObject scrollObj = new GameObject(name, typeof(RectTransform), typeof(ScrollRect));
        scrollObj.transform.SetParent(parent, false);

        RectTransform scrollRT = scrollObj.GetComponent<RectTransform>();
        scrollRT.anchorMin = Vector2.zero;
        scrollRT.anchorMax = Vector2.one;
        scrollRT.sizeDelta = Vector2.zero;

        ScrollRect scrollRect = scrollObj.GetComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 20;

        // Viewport
        GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        viewport.transform.SetParent(scrollObj.transform, false);
        Stretch(viewport);

        Image vpImg = viewport.GetComponent<Image>();
        vpImg.color = Color.white;

        Mask mask = viewport.GetComponent<Mask>();
        mask.showMaskGraphic = false;

        // Content
        GameObject content = new GameObject("Content", typeof(RectTransform));
        content.transform.SetParent(viewport.transform, false);

        RectTransform contentRT = content.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1);
        contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot = new Vector2(0.5f, 1);
        contentRT.sizeDelta = new Vector2(0, 300);

        scrollRect.viewport = viewport.GetComponent<RectTransform>();
        scrollRect.content = contentRT;

        // Scrollbar
        GameObject scrollbar = new GameObject("Scrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
        scrollbar.transform.SetParent(scrollObj.transform, false);

        RectTransform sbRT = scrollbar.GetComponent<RectTransform>();
        sbRT.anchorMin = new Vector2(1, 0);
        sbRT.anchorMax = new Vector2(1, 1);
        sbRT.pivot = new Vector2(1, 0.5f);
        sbRT.sizeDelta = new Vector2(6, 0);

        Image sbImg = scrollbar.GetComponent<Image>();
        sbImg.color = SCROLLBAR_BG;

        // Scrollbar handle
        GameObject sbHandle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        sbHandle.transform.SetParent(scrollbar.transform, false);
        Stretch(sbHandle);

        Image handleImg = sbHandle.GetComponent<Image>();
        handleImg.color = SCROLLBAR_HDL;

        Scrollbar sb = scrollbar.GetComponent<Scrollbar>();
        sb.handleRect = sbHandle.GetComponent<RectTransform>();
        sb.direction = Scrollbar.Direction.BottomToTop;
        sb.targetGraphic = handleImg;

        scrollRect.verticalScrollbar = sb;
        scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
        scrollRect.verticalScrollbarSpacing = -2;

        return scrollObj;
    }

    // ─── Layout Helpers ──────────────────────────────────────────

    static void Stretch(GameObject obj)
    {
        RectTransform rt = obj.GetComponent<RectTransform>();
        if (rt == null) return;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
    }

    static void SetSize(GameObject obj, float w, float h)
    {
        RectTransform rt = obj.GetComponent<RectTransform>();
        if (rt != null) rt.sizeDelta = new Vector2(w, h);

        LayoutElement le = obj.GetComponent<LayoutElement>();
        if (le == null) le = obj.AddComponent<LayoutElement>();
        le.minWidth = w;
        le.minHeight = h;
        le.preferredWidth = w;
        le.preferredHeight = h;
    }

    static void SetPadding(GameObject obj, int left, int right, int top, int bottom)
    {
        RectTransform rt = obj.GetComponent<RectTransform>();
        if (rt == null) return;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(left, bottom);
        rt.offsetMax = new Vector2(-right, -top);
    }

    static void AddRoundedCorners(GameObject obj, float radius)
    {
        // Unity's default Image doesn't support border-radius natively.
        // This is a placeholder — for actual rounded corners, use a
        // sprite with rounded corners or a shader. The structure is
        // correct; just swap the Image sprite to a rounded rect.
        // For now we mark it so you know which ones should be rounded.
        obj.name = obj.name; // No-op, but the radius is documented
    }

    static void AddOutline(GameObject obj, Color color)
    {
        Outline outline = obj.AddComponent<Outline>();
        outline.effectColor = color;
        outline.effectDistance = new Vector2(1, -1);
    }

    static void AddBorder(GameObject obj, Color color, RectTransform.Edge edge, float thickness)
    {
        GameObject border = new GameObject("Border", typeof(RectTransform), typeof(Image));
        border.transform.SetParent(obj.transform, false);

        Image img = border.GetComponent<Image>();
        img.color = color;

        RectTransform rt = border.GetComponent<RectTransform>();

        switch (edge)
        {
            case RectTransform.Edge.Bottom:
                rt.anchorMin = new Vector2(0, 0);
                rt.anchorMax = new Vector2(1, 0);
                rt.pivot = new Vector2(0.5f, 0);
                rt.sizeDelta = new Vector2(0, thickness);
                break;
            case RectTransform.Edge.Top:
                rt.anchorMin = new Vector2(0, 1);
                rt.anchorMax = new Vector2(1, 1);
                rt.pivot = new Vector2(0.5f, 1);
                rt.sizeDelta = new Vector2(0, thickness);
                break;
        }
    }
}
#endif

// ============================================================
//  InventoryUIBuilder.cs — Place in Assets/Editor/
//  Menu: EasyExpress > Create Inventory UI
//  Spawns the complete inventory canvas with all elements wired.
// ============================================================

#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

public class InventoryUIBuilder : MonoBehaviour
{
    [MenuItem("EasyExpress/Create Inventory UI")]
    public static void CreateInventoryUI()
    {
        // ═══════════════════════════════════════════════════════
        //  CANVAS
        // ═══════════════════════════════════════════════════════
        GameObject canvasGO = new GameObject("InventoryCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        // ═══════════════════════════════════════════════════════
        //  MAIN PANEL (dark overlay)
        // ═══════════════════════════════════════════════════════
        GameObject mainPanel = CreatePanel(canvasGO.transform, "InventoryPanel",
            new Color(0.04f, 0.05f, 0.08f, 0.96f));
        Stretch(mainPanel);

        // ═══════════════════════════════════════════════════════
        //  FRAME (centered container)
        // ═══════════════════════════════════════════════════════
        GameObject frame = CreatePanel(mainPanel.transform, "Frame",
            new Color(0.04f, 0.05f, 0.08f, 1f));
        RectTransform frameRT = frame.GetComponent<RectTransform>();
        frameRT.anchorMin = new Vector2(0.08f, 0.08f);
        frameRT.anchorMax = new Vector2(0.92f, 0.92f);
        frameRT.offsetMin = Vector2.zero;
        frameRT.offsetMax = Vector2.zero;

        Outline frameOutline = frame.AddComponent<Outline>();
        frameOutline.effectColor = new Color(1f, 1f, 1f, 0.06f);
        frameOutline.effectDistance = new Vector2(1, 1);

        // ═══════════════════════════════════════════════════════
        //  HEADER BAR
        // ═══════════════════════════════════════════════════════
        GameObject header = CreatePanel(frame.transform, "Header",
            new Color(1f, 1f, 1f, 0.02f));
        RectTransform headerRT = header.GetComponent<RectTransform>();
        headerRT.anchorMin = new Vector2(0, 1);
        headerRT.anchorMax = new Vector2(1, 1);
        headerRT.pivot = new Vector2(0.5f, 1);
        headerRT.sizeDelta = new Vector2(0, 50);
        headerRT.anchoredPosition = Vector2.zero;

        TextMeshProUGUI titleTMP = CreateTMP(header.transform, "Title", "Inventory",
            16, new Color(0.88f, 0.89f, 0.93f), TextAlignmentOptions.MidlineLeft);
        RectTransform titleRT = titleTMP.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0, 0);
        titleRT.anchorMax = new Vector2(0.2f, 1);
        titleRT.offsetMin = new Vector2(20, 0);
        titleRT.offsetMax = Vector2.zero;

        GameObject modeContainer = new GameObject("ModeContainer", typeof(RectTransform));
        modeContainer.transform.SetParent(header.transform, false);
        RectTransform modeRT = modeContainer.GetComponent<RectTransform>();
        modeRT.anchorMin = new Vector2(0.35f, 0.15f);
        modeRT.anchorMax = new Vector2(0.65f, 0.85f);
        modeRT.offsetMin = Vector2.zero;
        modeRT.offsetMax = Vector2.zero;

        HorizontalLayoutGroup modeHLG = modeContainer.AddComponent<HorizontalLayoutGroup>();
        modeHLG.spacing = 4;
        modeHLG.childAlignment = TextAnchor.MiddleCenter;
        modeHLG.childControlWidth = true;
        modeHLG.childControlHeight = true;
        modeHLG.childForceExpandWidth = true;

        GameObject storageBtnGO = CreatePanel(modeContainer.transform, "StorageModeBtn",
            new Color(0.29f, 0.56f, 0.85f, 0.15f));
        Button storageBtn = storageBtnGO.AddComponent<Button>();
        TextMeshProUGUI storageTMP = CreateTMP(storageBtnGO.transform, "Text", "Storage",
            12, new Color(0.42f, 0.71f, 0.97f), TextAlignmentOptions.Center);
        Stretch(storageTMP.gameObject);

        GameObject inspBtnGO = CreatePanel(modeContainer.transform, "InspectionModeBtn",
            Color.clear);
        Button inspBtn = inspBtnGO.AddComponent<Button>();
        TextMeshProUGUI inspTMP = CreateTMP(inspBtnGO.transform, "Text", "Inspection",
            12, new Color(1f, 1f, 1f, 0.35f), TextAlignmentOptions.Center);
        Stretch(inspTMP.gameObject);

        GameObject closeBtnGO = CreatePanel(header.transform, "CloseBtn",
            new Color(1f, 1f, 1f, 0.06f));
        RectTransform closeRT = closeBtnGO.GetComponent<RectTransform>();
        closeRT.anchorMin = new Vector2(1, 0.5f);
        closeRT.anchorMax = new Vector2(1, 0.5f);
        closeRT.pivot = new Vector2(1, 0.5f);
        closeRT.sizeDelta = new Vector2(32, 32);
        closeRT.anchoredPosition = new Vector2(-10, 0);
        Button closeBtn = closeBtnGO.AddComponent<Button>();
        TextMeshProUGUI closeTMP = CreateTMP(closeBtnGO.transform, "X", "\u2715",
            14, new Color(1f, 1f, 1f, 0.5f), TextAlignmentOptions.Center);
        Stretch(closeTMP.gameObject);

        // ═══════════════════════════════════════════════════════
        //  BODY (below header)
        // ═══════════════════════════════════════════════════════
        GameObject body = new GameObject("Body", typeof(RectTransform));
        body.transform.SetParent(frame.transform, false);
        RectTransform bodyRT = body.GetComponent<RectTransform>();
        bodyRT.anchorMin = Vector2.zero;
        bodyRT.anchorMax = Vector2.one;
        bodyRT.offsetMin = Vector2.zero;
        bodyRT.offsetMax = new Vector2(0, -50);

        // ═══════════════════════════════════════════════════════
        //  SIDEBAR (left)
        // ═══════════════════════════════════════════════════════
        GameObject sidebar = CreatePanel(body.transform, "Sidebar", Color.clear);
        RectTransform sidebarRT = sidebar.GetComponent<RectTransform>();
        sidebarRT.anchorMin = Vector2.zero;
        sidebarRT.anchorMax = new Vector2(0, 1);
        sidebarRT.pivot = new Vector2(0, 0.5f);
        sidebarRT.sizeDelta = new Vector2(200, 0);

        GameObject sidebarBorder = CreatePanel(sidebar.transform, "Border",
            new Color(1f, 1f, 1f, 0.06f));
        RectTransform sBorderRT = sidebarBorder.GetComponent<RectTransform>();
        sBorderRT.anchorMin = new Vector2(1, 0);
        sBorderRT.anchorMax = new Vector2(1, 1);
        sBorderRT.pivot = new Vector2(1, 0.5f);
        sBorderRT.sizeDelta = new Vector2(1, 0);

        GameObject sidebarScroll = CreateScrollView(sidebar.transform, "SidebarScroll");
        Stretch(sidebarScroll);
        Transform sidebarContent = sidebarScroll.transform.Find("Viewport/Content");

        CreateSectionLabel(sidebarContent, "Categories");

        GameObject catContainer = new GameObject("CategoryTabContainer", typeof(RectTransform));
        catContainer.transform.SetParent(sidebarContent, false);
        VerticalLayoutGroup catVLG = catContainer.AddComponent<VerticalLayoutGroup>();
        catVLG.spacing = 2;
        catVLG.childControlWidth = true;
        catVLG.childControlHeight = false;
        catVLG.childForceExpandWidth = true;
        catContainer.AddComponent<LayoutElement>().flexibleWidth = 1;
        catContainer.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        CreateSectionLabel(sidebarContent, "By owner");

        GameObject ownerContainer = new GameObject("OwnerTabContainer", typeof(RectTransform));
        ownerContainer.transform.SetParent(sidebarContent, false);
        VerticalLayoutGroup ownerVLG = ownerContainer.AddComponent<VerticalLayoutGroup>();
        ownerVLG.spacing = 2;
        ownerVLG.childControlWidth = true;
        ownerVLG.childControlHeight = false;
        ownerVLG.childForceExpandWidth = true;
        ownerContainer.AddComponent<LayoutElement>().flexibleWidth = 1;
        ownerContainer.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // ═══════════════════════════════════════════════════════
        //  CENTER GRID AREA
        // ═══════════════════════════════════════════════════════
        GameObject center = new GameObject("Center", typeof(RectTransform));
        center.transform.SetParent(body.transform, false);
        RectTransform centerRT = center.GetComponent<RectTransform>();
        centerRT.anchorMin = Vector2.zero;
        centerRT.anchorMax = Vector2.one;
        centerRT.offsetMin = new Vector2(200, 0);
        centerRT.offsetMax = new Vector2(-240, 0);

        // Search bar
        GameObject searchGO = new GameObject("SearchInput", typeof(RectTransform));
        searchGO.transform.SetParent(center.transform, false);
        RectTransform searchRT = searchGO.GetComponent<RectTransform>();
        searchRT.anchorMin = new Vector2(0, 1);
        searchRT.anchorMax = new Vector2(1, 1);
        searchRT.pivot = new Vector2(0.5f, 1);
        searchRT.sizeDelta = new Vector2(-24, 36);
        searchRT.anchoredPosition = new Vector2(0, -12);
        searchGO.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.04f);

        TMP_InputField searchField = searchGO.AddComponent<TMP_InputField>();

        GameObject searchTextArea = new GameObject("Text Area", typeof(RectTransform));
        searchTextArea.transform.SetParent(searchGO.transform, false);
        Stretch(searchTextArea);
        RectTransform textAreaRT = searchTextArea.GetComponent<RectTransform>();
        textAreaRT.offsetMin = new Vector2(12, 0);
        textAreaRT.offsetMax = new Vector2(-12, 0);
        searchTextArea.AddComponent<RectMask2D>();

        TextMeshProUGUI placeholder = CreateTMP(searchTextArea.transform, "Placeholder",
            "Search parts...", 12, new Color(1f, 1f, 1f, 0.25f), TextAlignmentOptions.MidlineLeft);
        Stretch(placeholder.gameObject);

        TextMeshProUGUI inputText = CreateTMP(searchTextArea.transform, "Text",
            "", 12, new Color(0.8f, 0.84f, 0.92f), TextAlignmentOptions.MidlineLeft);
        Stretch(inputText.gameObject);

        searchField.textViewport = textAreaRT;
        searchField.textComponent = inputText;
        searchField.placeholder = placeholder;
        searchField.fontAsset = inputText.font;
        searchField.pointSize = 12;

        // Grid scroll
        GameObject gridScroll = CreateScrollView(center.transform, "GridScroll");
        RectTransform gridScrollRT = gridScroll.GetComponent<RectTransform>();
        gridScrollRT.anchorMin = Vector2.zero;
        gridScrollRT.anchorMax = Vector2.one;
        gridScrollRT.offsetMin = Vector2.zero;
        gridScrollRT.offsetMax = new Vector2(0, -56);

        Transform gridContent = gridScroll.transform.Find("Viewport/Content");

        // ── CRITICAL: Remove VLG + CSF BEFORE adding GridLayoutGroup ──
        VerticalLayoutGroup oldVLG = gridContent.GetComponent<VerticalLayoutGroup>();
        if (oldVLG != null) DestroyImmediate(oldVLG);
        ContentSizeFitter oldCSF = gridContent.GetComponent<ContentSizeFitter>();
        if (oldCSF != null) DestroyImmediate(oldCSF);

        GridLayoutGroup glg = gridContent.gameObject.AddComponent<GridLayoutGroup>();
        glg.cellSize = new Vector2(140, 140);
        glg.spacing = new Vector2(8, 8);
        glg.padding = new RectOffset(12, 12, 8, 8);
        glg.constraint = GridLayoutGroup.Constraint.Flexible;
        glg.childAlignment = TextAnchor.UpperLeft;

        gridContent.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // ═══════════════════════════════════════════════════════
        //  DETAIL PANEL (right)
        // ═══════════════════════════════════════════════════════
        GameObject detail = CreatePanel(body.transform, "DetailPanel", Color.clear);
        RectTransform detailRT = detail.GetComponent<RectTransform>();
        detailRT.anchorMin = new Vector2(1, 0);
        detailRT.anchorMax = new Vector2(1, 1);
        detailRT.pivot = new Vector2(1, 0.5f);
        detailRT.sizeDelta = new Vector2(240, 0);

        GameObject detailBorder = CreatePanel(detail.transform, "Border",
            new Color(1f, 1f, 1f, 0.06f));
        RectTransform dBorderRT = detailBorder.GetComponent<RectTransform>();
        dBorderRT.anchorMin = new Vector2(0, 0);
        dBorderRT.anchorMax = new Vector2(0, 1);
        dBorderRT.pivot = new Vector2(0, 0.5f);
        dBorderRT.sizeDelta = new Vector2(1, 0);

        GameObject detailContent = new GameObject("DetailContent", typeof(RectTransform));
        detailContent.transform.SetParent(detail.transform, false);
        RectTransform dcRT = detailContent.GetComponent<RectTransform>();
        dcRT.anchorMin = Vector2.zero;
        dcRT.anchorMax = Vector2.one;
        dcRT.offsetMin = new Vector2(16, 16);
        dcRT.offsetMax = new Vector2(-16, -16);

        VerticalLayoutGroup dcVLG = detailContent.AddComponent<VerticalLayoutGroup>();
        dcVLG.spacing = 10;
        dcVLG.childControlWidth = true;
        dcVLG.childControlHeight = false;
        dcVLG.childForceExpandWidth = true;
        dcVLG.childForceExpandHeight = false;

        // Icon preview
        GameObject iconPreview = CreatePanel(detailContent.transform, "IconPreview",
            new Color(1f, 1f, 1f, 0.03f));
        iconPreview.AddComponent<LayoutElement>().preferredHeight = 120;

        GameObject iconImg = new GameObject("DetailIcon", typeof(RectTransform), typeof(Image));
        iconImg.transform.SetParent(iconPreview.transform, false);
        Image detailIconImage = iconImg.GetComponent<Image>();
        detailIconImage.preserveAspect = true;
        RectTransform iconImgRT = iconImg.GetComponent<RectTransform>();
        iconImgRT.anchorMin = new Vector2(0.15f, 0.1f);
        iconImgRT.anchorMax = new Vector2(0.85f, 0.9f);
        iconImgRT.offsetMin = Vector2.zero;
        iconImgRT.offsetMax = Vector2.zero;

        TextMeshProUGUI catText = CreateTMP(detailContent.transform, "DetailCategory", "GPU",
            11, new Color(0.42f, 0.71f, 0.97f), TextAlignmentOptions.TopLeft);
        catText.gameObject.AddComponent<LayoutElement>().preferredHeight = 16;

        TextMeshProUGUI nameText = CreateTMP(detailContent.transform, "DetailName", "Part Name",
            15, new Color(0.88f, 0.89f, 0.93f), TextAlignmentOptions.TopLeft);
        nameText.fontStyle = FontStyles.Bold;
        nameText.gameObject.AddComponent<LayoutElement>().preferredHeight = 22;

        TextMeshProUGUI descText = CreateTMP(detailContent.transform, "DetailDesc", "Description",
            12, new Color(1f, 1f, 1f, 0.4f), TextAlignmentOptions.TopLeft);
        descText.enableWordWrapping = true;
        LayoutElement descLE = descText.gameObject.AddComponent<LayoutElement>();
        descLE.preferredHeight = 48;
        descLE.flexibleWidth = 1;

        GameObject specsContainer = new GameObject("SpecsContainer", typeof(RectTransform));
        specsContainer.transform.SetParent(detailContent.transform, false);
        HorizontalLayoutGroup specsHLG = specsContainer.AddComponent<HorizontalLayoutGroup>();
        specsHLG.spacing = 6;
        specsHLG.childAlignment = TextAnchor.MiddleLeft;
        specsHLG.childControlWidth = false;
        specsHLG.childControlHeight = true;
        specsHLG.childForceExpandWidth = false;
        LayoutElement specsLE = specsContainer.AddComponent<LayoutElement>();
        specsLE.preferredHeight = 24;
        specsLE.flexibleWidth = 1;

        TextMeshProUGUI ownerText = CreateTMP(detailContent.transform, "DetailOwner",
            "Owner: <b>Shop stock</b>", 11, new Color(1f, 1f, 1f, 0.35f), TextAlignmentOptions.TopLeft);
        ownerText.richText = true;
        ownerText.gameObject.AddComponent<LayoutElement>().preferredHeight = 18;

        GameObject spacer = new GameObject("Spacer", typeof(RectTransform), typeof(LayoutElement));
        spacer.transform.SetParent(detailContent.transform, false);
        spacer.GetComponent<LayoutElement>().flexibleHeight = 1;

        GameObject actionBtnGO = CreatePanel(detailContent.transform, "ActionBtn",
            new Color(0.29f, 0.56f, 0.85f, 0.2f));
        Button actionButton = actionBtnGO.AddComponent<Button>();
        actionBtnGO.AddComponent<LayoutElement>().preferredHeight = 36;
        TextMeshProUGUI actionText = CreateTMP(actionBtnGO.transform, "Text",
            "Add to Inventory", 12, new Color(0.42f, 0.71f, 0.97f), TextAlignmentOptions.Center);
        actionText.fontStyle = FontStyles.Bold;
        Stretch(actionText.gameObject);

        GameObject deleteBtnGO = CreatePanel(detailContent.transform, "DeleteBtn",
            new Color(0.88f, 0.29f, 0.29f, 0.15f));
        Button deleteButton = deleteBtnGO.AddComponent<Button>();
        deleteBtnGO.AddComponent<LayoutElement>().preferredHeight = 36;
        TextMeshProUGUI deleteText = CreateTMP(deleteBtnGO.transform, "Text",
            "Delete item", 12, new Color(0.94f, 0.58f, 0.58f), TextAlignmentOptions.Center);
        Stretch(deleteText.gameObject);

        // ═══════════════════════════════════════════════════════
        //  WIRE UP MANAGER COMPONENT
        // ═══════════════════════════════════════════════════════
        InventoryUIManager manager = canvasGO.AddComponent<InventoryUIManager>();
        manager.inventoryPanel = mainPanel;
        manager.storageModeBtn = storageBtn;
        manager.inspectionModeBtn = inspBtn;
        manager.closeBtn = closeBtn;
        manager.categoryTabContainer = catContainer.transform;
        manager.ownerTabContainer = ownerContainer.transform;
        manager.searchInput = searchField;
        manager.gridContainer = gridContent;
        manager.detailPanel = detail;
        manager.detailIcon = detailIconImage;
        manager.detailCategoryText = catText;
        manager.detailNameText = nameText;
        manager.detailDescText = descText;
        manager.detailSpecsContainer = specsContainer.transform;
        manager.detailOwnerText = ownerText;
        manager.actionBtn = actionButton;
        manager.actionBtnText = actionText;
        manager.deleteBtn = deleteButton;

        // Start hidden
        mainPanel.SetActive(false);
        detail.SetActive(false);

        Selection.activeGameObject = canvasGO;
        Undo.RegisterCreatedObjectUndo(canvasGO, "Create Inventory UI");
        Debug.Log("[InventoryUIBuilder] Inventory UI created successfully! All references wired.");
    }

    // ═══════════════════════════════════════════════════════════
    //  HELPERS
    // ═══════════════════════════════════════════════════════════

    static GameObject CreatePanel(Transform parent, string name, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = color;
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
        float fontSize, Color color, TextAlignmentOptions align)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = align;
        tmp.enableWordWrapping = false;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        tmp.raycastTarget = false;
        return tmp;
    }

    static void CreateSectionLabel(Transform parent, string text)
    {
        GameObject go = new GameObject("Section_" + text, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        go.AddComponent<LayoutElement>().preferredHeight = 28;

        TextMeshProUGUI tmp = CreateTMP(go.transform, "Label", text.ToUpper(),
            10, new Color(1f, 1f, 1f, 0.2f), TextAlignmentOptions.MidlineLeft);
        RectTransform tmpRT = tmp.GetComponent<RectTransform>();
        tmpRT.anchorMin = Vector2.zero;
        tmpRT.anchorMax = Vector2.one;
        tmpRT.offsetMin = new Vector2(16, 0);
        tmpRT.offsetMax = Vector2.zero;
        tmp.characterSpacing = 2;
    }

    static GameObject CreateScrollView(Transform parent, string name)
    {
        GameObject scrollGO = new GameObject(name, typeof(RectTransform), typeof(ScrollRect));
        scrollGO.transform.SetParent(parent, false);

        ScrollRect sr = scrollGO.GetComponent<ScrollRect>();
        sr.horizontal = false;
        sr.vertical = true;
        sr.movementType = ScrollRect.MovementType.Clamped;
        sr.scrollSensitivity = 20;

        GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        viewport.transform.SetParent(scrollGO.transform, false);
        Stretch(viewport);
        viewport.GetComponent<Image>().color = Color.white;
        viewport.GetComponent<Mask>().showMaskGraphic = false;

        GameObject content = new GameObject("Content", typeof(RectTransform));
        content.transform.SetParent(viewport.transform, false);
        RectTransform cRT = content.GetComponent<RectTransform>();
        cRT.anchorMin = new Vector2(0, 1);
        cRT.anchorMax = new Vector2(1, 1);
        cRT.pivot = new Vector2(0.5f, 1);
        cRT.sizeDelta = Vector2.zero;

        VerticalLayoutGroup vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 0;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        sr.viewport = viewport.GetComponent<RectTransform>();
        sr.content = cRT;

        return scrollGO;
    }
}
#endif
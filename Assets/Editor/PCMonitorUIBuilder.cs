// Assets/Editor/PCMonitorUIBuilder.cs
// ============================================================
//  PCMonitorUIBuilder — Unity Editor Tool
//  Window: EasyExpress → Build Monitor UI
//
//  Opens an editor window. Drag a PCController GameObject into
//  the slot and click Build to auto-generate all screen panels
//  (Boot, NoSignal, BSOD, BIOS) as children of its Canvas and
//  wire the references into the PCController component.
// ============================================================

using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

public class PCMonitorUIBuilder : EditorWindow
{
    private PCController targetController;
    private Canvas targetCanvas;

    // ── Color palette ─────────────────────────────────────────
    static readonly Color COL_BLACK = Color.black;
    static readonly Color COL_BIOS_BG = new Color(0f, 0f, 0.55f, 1f);  // dark navy
    static readonly Color COL_BIOS_TEXT = new Color(0.8f, 0.8f, 1f, 1f);
    static readonly Color COL_BSOD_BG = new Color(0f, 0.47f, 0.84f, 1f);  // Windows blue
    static readonly Color COL_BSOD_TEXT = Color.white;
    static readonly Color COL_BOOT_BG = Color.black;
    static readonly Color COL_BOOT_LOGO = Color.white;
    static readonly Color COL_BOOT_BAR_BG = new Color(0.2f, 0.2f, 0.2f, 1f);
    static readonly Color COL_BOOT_BAR_FG = Color.white;
    static readonly Color COL_BOOT_STATUS = new Color(0.6f, 0.6f, 0.6f, 1f);
    static readonly Color COL_NO_SIGNAL = Color.black;
    static readonly Color COL_NO_SIG_TEXT = new Color(0.5f, 0.5f, 0.5f, 1f);

    // ─────────────────────────────────────────────────────────
    [MenuItem("EasyExpress/Build Monitor UI")]
    static void OpenWindow()
    {
        GetWindow<PCMonitorUIBuilder>("Monitor UI Builder");
    }

    void OnGUI()
    {
        GUILayout.Space(10);
        GUILayout.Label("PC Monitor UI Builder", EditorStyles.boldLabel);
        GUILayout.Label("Generates Boot, NoSignal, BSOD and BIOS panels\n" +
                         "and wires them into the PCController.", EditorStyles.wordWrappedLabel);
        GUILayout.Space(10);

        targetController = (PCController)EditorGUILayout.ObjectField(
            "PCController", targetController, typeof(PCController), true);

        targetCanvas = (Canvas)EditorGUILayout.ObjectField(
            "Target Canvas (localCanvas)", targetCanvas, typeof(Canvas), true);

        GUILayout.Space(6);

        // Auto-fill canvas from controller if possible
        if (targetController != null && targetCanvas == null)
        {
            targetCanvas = targetController.GetComponentInChildren<Canvas>(true);
        }

        EditorGUILayout.HelpBox(
            "Drag the PCController and its localCanvas here, then click Build.\n" +
            "Existing panels with the same names will be skipped.",
            MessageType.Info);

        GUILayout.Space(10);

        GUI.enabled = targetController != null && targetCanvas != null;
        if (GUILayout.Button("Build Monitor UI", GUILayout.Height(36)))
            BuildUI();
        GUI.enabled = true;
    }

    // =========================================================
    //  MAIN BUILD
    // =========================================================

    void BuildUI()
    {
        Undo.RegisterFullObjectHierarchyUndo(targetCanvas.gameObject, "Build Monitor UI");

        // ── Force canvas to landscape 1920x1080 ──────────────────
        RectTransform canvasRT = targetCanvas.GetComponent<RectTransform>();
        canvasRT.sizeDelta = new Vector2(1920f, 1080f);
        canvasRT.anchorMin = new Vector2(0.5f, 0.5f);
        canvasRT.anchorMax = new Vector2(0.5f, 0.5f);
        canvasRT.pivot = new Vector2(0.5f, 0.5f);
        canvasRT.anchoredPosition = Vector2.zero;
        // ─────────────────────────────────────────────────────────
        // ── Remove orphaned empty panels left from previous builds ──
        foreach (Image img in targetCanvas.GetComponentsInChildren<Image>(true))
        {
            // Skip panels we are about to create or that have children
            if (img.transform.childCount > 0) continue;
            if (img.transform.parent != canvasRT) continue;
            string n = img.gameObject.name;
            if (n == "BootScreenPanel" || n == "NoSignalPanel"
             || n == "BSODPanel" || n == "BIOSPanel") continue;

            Debug.Log($"[PCMonitorUIBuilder] Removing orphaned panel: {n}");
            DestroyImmediate(img.gameObject);
        }
        // Also set CanvasScaler if present
        CanvasScaler scaler = targetCanvas.GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
        }

        // ... rest of BuildUI unchanged
        GameObject bootPanel = BuildBootScreen(canvasRT);
        GameObject noSignalPanel = BuildNoSignalScreen(canvasRT);
        GameObject bsodPanel = BuildBSODScreen(canvasRT);
        GameObject biosPanel = BuildBIOSScreen(canvasRT);

        SerializedObject so = new SerializedObject(targetController);
        SetIfNull(so, "bootScreenPanel", bootPanel);
        SetIfNull(so, "noSignalPanel", noSignalPanel);
        SetIfNull(so, "bsodPanel", bsodPanel);
        SetIfNull(so, "biosPanel", biosPanel);
        WireBootRefs(so, bootPanel);
        WireBSODRefs(so, bsodPanel);
        so.ApplyModifiedProperties();

        EditorUtility.SetDirty(targetController);
        Debug.Log("[PCMonitorUIBuilder] UI built and wired successfully.");
    }

    // =========================================================
    //  BOOT SCREEN
    //  Black background, large logo text, loading bar, status
    // =========================================================

    GameObject BuildBootScreen(RectTransform parent)
    {
        GameObject panel = GetOrCreatePanel(parent, "BootScreenPanel", COL_BOOT_BG);
        RectTransform rt = panel.GetComponent<RectTransform>();
        Stretch(rt);

        // Logo text — centered upper third
        GameObject logoGO = GetOrCreateChild(panel, "LogoText");
        TextMeshProUGUI logo = EnsureTMP(logoGO);
        logo.text = "EasyExpress";
        logo.fontSize = 72;
        logo.fontStyle = FontStyles.Bold;
        logo.color = COL_BOOT_LOGO;
        logo.alignment = TextAlignmentOptions.Center;
        RectTransform logoRT = logoGO.GetComponent<RectTransform>();
        logoRT.anchorMin = new Vector2(0.1f, 0.55f);
        logoRT.anchorMax = new Vector2(0.9f, 0.75f);
        logoRT.offsetMin = logoRT.offsetMax = Vector2.zero;

        // Tagline under logo
        GameObject tagGO = GetOrCreateChild(panel, "TaglineText");
        TextMeshProUGUI tag = EnsureTMP(tagGO);
        tag.text = "PC REPAIR SIMULATOR";
        tag.fontSize = 18;
        tag.color = new Color(0.45f, 0.45f, 0.45f, 1f);
        tag.alignment = TextAlignmentOptions.Center;
        tag.characterSpacing = 6;
        RectTransform tagRT = tagGO.GetComponent<RectTransform>();
        tagRT.anchorMin = new Vector2(0.1f, 0.47f);
        tagRT.anchorMax = new Vector2(0.9f, 0.54f);
        tagRT.offsetMin = tagRT.offsetMax = Vector2.zero;

        // Loading bar background
        GameObject barBGGO = GetOrCreateChild(panel, "LoadingBarBG");
        Image barBGImg = EnsureImage(barBGGO);
        barBGImg.color = COL_BOOT_BAR_BG;
        RectTransform barBGRT = barBGGO.GetComponent<RectTransform>();
        barBGRT.anchorMin = new Vector2(0.2f, 0.35f);
        barBGRT.anchorMax = new Vector2(0.8f, 0.38f);
        barBGRT.offsetMin = barBGRT.offsetMax = Vector2.zero;

        // Loading bar (Slider)
        GameObject sliderGO = GetOrCreateChild(panel, "LoadingBar");
        Slider slider = sliderGO.GetComponent<Slider>();
        if (slider == null) slider = sliderGO.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 0f;
        slider.transition = Selectable.Transition.None;
        RectTransform sliderRT = sliderGO.GetComponent<RectTransform>();
        sliderRT.anchorMin = new Vector2(0.2f, 0.35f);
        sliderRT.anchorMax = new Vector2(0.8f, 0.38f);
        sliderRT.offsetMin = sliderRT.offsetMax = Vector2.zero;

        // Slider fill area
        GameObject fillAreaGO = GetOrCreateChild(sliderGO, "Fill Area");
        RectTransform fillAreaRT = fillAreaGO.GetComponent<RectTransform>();
        fillAreaRT.anchorMin = Vector2.zero;
        fillAreaRT.anchorMax = Vector2.one;
        fillAreaRT.offsetMin = fillAreaRT.offsetMax = Vector2.zero;

        GameObject fillGO = GetOrCreateChild(fillAreaGO, "Fill");
        Image fillImg = EnsureImage(fillGO);
        fillImg.color = COL_BOOT_BAR_FG;
        RectTransform fillRT = fillGO.GetComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = Vector2.one;
        fillRT.offsetMin = fillRT.offsetMax = Vector2.zero;

        slider.fillRect = fillRT;
        slider.handleRect = null;

        // Status text
        GameObject statusGO = GetOrCreateChild(panel, "StatusText");
        TextMeshProUGUI status = EnsureTMP(statusGO);
        status.text = "Starting up...";
        status.fontSize = 14;
        status.color = COL_BOOT_STATUS;
        status.alignment = TextAlignmentOptions.Center;
        RectTransform statusRT = statusGO.GetComponent<RectTransform>();
        statusRT.anchorMin = new Vector2(0.1f, 0.29f);
        statusRT.anchorMax = new Vector2(0.9f, 0.34f);
        statusRT.offsetMin = statusRT.offsetMax = Vector2.zero;

        panel.SetActive(false);
        return panel;
    }

    // =========================================================
    //  NO SIGNAL SCREEN
    //  Full black with grey centered text
    // =========================================================

    GameObject BuildNoSignalScreen(RectTransform parent)
    {
        GameObject panel = GetOrCreatePanel(parent, "NoSignalPanel", COL_NO_SIGNAL);
        Stretch(panel.GetComponent<RectTransform>());

        GameObject textGO = GetOrCreateChild(panel, "NoSignalText");
        TextMeshProUGUI tmp = EnsureTMP(textGO);
        tmp.text = "NO SIGNAL";
        tmp.fontSize = 36;
        tmp.color = COL_NO_SIG_TEXT;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.characterSpacing = 8;
        RectTransform textRT = textGO.GetComponent<RectTransform>();
        textRT.anchorMin = new Vector2(0.1f, 0.44f);
        textRT.anchorMax = new Vector2(0.9f, 0.56f);
        textRT.offsetMin = textRT.offsetMax = Vector2.zero;

        // Subtext
        GameObject subGO = GetOrCreateChild(panel, "NoSignalSub");
        TextMeshProUGUI sub = EnsureTMP(subGO);
        sub.text = "Check cable connection or GPU output.";
        sub.fontSize = 14;
        sub.color = new Color(0.3f, 0.3f, 0.3f, 1f);
        sub.alignment = TextAlignmentOptions.Center;
        RectTransform subRT = subGO.GetComponent<RectTransform>();
        subRT.anchorMin = new Vector2(0.1f, 0.38f);
        subRT.anchorMax = new Vector2(0.9f, 0.43f);
        subRT.offsetMin = subRT.offsetMax = Vector2.zero;

        panel.SetActive(false);
        return panel;
    }

    // =========================================================
    //  BSOD SCREEN
    //  Windows-blue, sad face, error code, message, progress
    // =========================================================

    GameObject BuildBSODScreen(RectTransform parent)
    {
        GameObject panel = GetOrCreatePanel(parent, "BSODPanel", COL_BSOD_BG);
        Stretch(panel.GetComponent<RectTransform>());

        // Sad face
        GameObject faceGO = GetOrCreateChild(panel, "SadFaceText");
        TextMeshProUGUI face = EnsureTMP(faceGO);
        face.text = ":(";
        face.fontSize = 96;
        face.color = COL_BSOD_TEXT;
        face.alignment = TextAlignmentOptions.Left;
        RectTransform faceRT = faceGO.GetComponent<RectTransform>();
        faceRT.anchorMin = new Vector2(0.08f, 0.65f);
        faceRT.anchorMax = new Vector2(0.5f, 0.85f);
        faceRT.offsetMin = faceRT.offsetMax = Vector2.zero;

        // Title
        GameObject titleGO = GetOrCreateChild(panel, "BSODTitle");
        TextMeshProUGUI title = EnsureTMP(titleGO);
        title.text = "Your PC ran into a problem.";
        title.fontSize = 28;
        title.color = COL_BSOD_TEXT;
        title.alignment = TextAlignmentOptions.Left;
        RectTransform titleRT = titleGO.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0.08f, 0.54f);
        titleRT.anchorMax = new Vector2(0.92f, 0.64f);
        titleRT.offsetMin = titleRT.offsetMax = Vector2.zero;

        // Error code
        GameObject codeGO = GetOrCreateChild(panel, "ErrorCodeText");
        TextMeshProUGUI code = EnsureTMP(codeGO);
        code.text = "POST_FAILURE";
        code.fontSize = 22;
        code.fontStyle = FontStyles.Bold;
        code.color = COL_BSOD_TEXT;
        code.alignment = TextAlignmentOptions.Left;
        RectTransform codeRT = codeGO.GetComponent<RectTransform>();
        codeRT.anchorMin = new Vector2(0.08f, 0.45f);
        codeRT.anchorMax = new Vector2(0.92f, 0.53f);
        codeRT.offsetMin = codeRT.offsetMax = Vector2.zero;

        // Message
        GameObject msgGO = GetOrCreateChild(panel, "BSODMessageText");
        TextMeshProUGUI msg = EnsureTMP(msgGO);
        msg.text = "The system failed to complete POST.\n" +
                   "One or more components may be faulty or missing.";
        msg.fontSize = 14;
        msg.color = COL_BSOD_TEXT;
        msg.alignment = TextAlignmentOptions.Left;
        msg.enableWordWrapping = true;
        RectTransform msgRT = msgGO.GetComponent<RectTransform>();
        msgRT.anchorMin = new Vector2(0.08f, 0.32f);
        msgRT.anchorMax = new Vector2(0.92f, 0.44f);
        msgRT.offsetMin = msgRT.offsetMax = Vector2.zero;

        // Progress line
        GameObject progGO = GetOrCreateChild(panel, "BSODProgressText");
        TextMeshProUGUI prog = EnsureTMP(progGO);
        prog.text = "Collecting error information... 0%";
        prog.fontSize = 14;
        prog.color = COL_BSOD_TEXT;
        prog.alignment = TextAlignmentOptions.Left;
        RectTransform progRT = progGO.GetComponent<RectTransform>();
        progRT.anchorMin = new Vector2(0.08f, 0.22f);
        progRT.anchorMax = new Vector2(0.92f, 0.30f);
        progRT.offsetMin = progRT.offsetMax = Vector2.zero;

        panel.SetActive(false);
        return panel;
    }

    // =========================================================
    //  BIOS SCREEN
    //  Dark navy retro BIOS look with system info table
    // =========================================================

    GameObject BuildBIOSScreen(RectTransform parent)
    {
        GameObject panel = GetOrCreatePanel(parent, "BIOSPanel", COL_BIOS_BG);
        Stretch(panel.GetComponent<RectTransform>());

        // Header bar
        GameObject headerGO = GetOrCreateChild(panel, "BIOSHeader");
        Image headerImg = EnsureImage(headerGO);
        headerImg.color = new Color(0.6f, 0.6f, 1f, 0.2f);
        RectTransform headerRT = headerGO.GetComponent<RectTransform>();
        headerRT.anchorMin = new Vector2(0f, 0.9f);
        headerRT.anchorMax = new Vector2(1f, 1f);
        headerRT.offsetMin = headerRT.offsetMax = Vector2.zero;

        GameObject headerTextGO = GetOrCreateChild(headerGO, "HeaderText");
        TextMeshProUGUI headerText = EnsureTMP(headerTextGO);
        headerText.text = "EASY EXPRESS BIOS  v1.0.0";
        headerText.fontSize = 14;
        headerText.color = COL_BIOS_TEXT;
        headerText.alignment = TextAlignmentOptions.Center;
        Stretch(headerTextGO.GetComponent<RectTransform>());

        // Footer bar
        GameObject footerGO = GetOrCreateChild(panel, "BIOSFooter");
        Image footerImg = EnsureImage(footerGO);
        footerImg.color = new Color(0.6f, 0.6f, 1f, 0.2f);
        RectTransform footerRT = footerGO.GetComponent<RectTransform>();
        footerRT.anchorMin = new Vector2(0f, 0f);
        footerRT.anchorMax = new Vector2(1f, 0.07f);
        footerRT.offsetMin = footerRT.offsetMax = Vector2.zero;

        GameObject footerTextGO = GetOrCreateChild(footerGO, "FooterText");
        TextMeshProUGUI footerText = EnsureTMP(footerTextGO);
        footerText.text = "F1: Help    F2: Load Defaults    ESC: Exit BIOS";
        footerText.fontSize = 11;
        footerText.color = new Color(0.5f, 0.5f, 0.8f, 1f);
        footerText.alignment = TextAlignmentOptions.Center;
        Stretch(footerTextGO.GetComponent<RectTransform>());

        // System info block
        string biosBody =
            "CPU          : Detecting...\n" +
            "Memory       : Detecting...\n" +
            "Storage      : Detecting...\n" +
            "GPU          : Detecting...\n\n" +
            "POST Status  : Running...\n\n" +
            ">> POST failed. Check installed components.\n" +
            ">> Enter BIOS to review hardware status.";

        GameObject bodyGO = GetOrCreateChild(panel, "BIOSBodyText");
        TextMeshProUGUI body = EnsureTMP(bodyGO);
        body.text = biosBody;
        body.fontSize = 13;
        body.color = COL_BIOS_TEXT;
        body.alignment = TextAlignmentOptions.TopLeft;
        body.enableWordWrapping = false;
        // Use a monospace font if available — fallback is fine
        RectTransform bodyRT = bodyGO.GetComponent<RectTransform>();
        bodyRT.anchorMin = new Vector2(0.05f, 0.12f);
        bodyRT.anchorMax = new Vector2(0.95f, 0.88f);
        bodyRT.offsetMin = bodyRT.offsetMax = Vector2.zero;

        panel.SetActive(false);
        return panel;
    }

    // =========================================================
    //  WIRE REFERENCES
    // =========================================================

    void WireBootRefs(SerializedObject so, GameObject bootPanel)
    {
        if (bootPanel == null) return;

        TextMeshProUGUI logo = FindTMPInChildren(bootPanel, "LogoText");
        Slider bar = bootPanel.GetComponentInChildren<Slider>(true);
        TextMeshProUGUI status = FindTMPInChildren(bootPanel, "StatusText");

        SetIfNull(so, "bootLogoText", logo);
        SetIfNull(so, "bootLoadingBar", bar);
        SetIfNull(so, "bootStatusText", status);
    }

    void WireBSODRefs(SerializedObject so, GameObject bsodPanel)
    {
        if (bsodPanel == null) return;

        TextMeshProUGUI code = FindTMPInChildren(bsodPanel, "ErrorCodeText");
        TextMeshProUGUI msg = FindTMPInChildren(bsodPanel, "BSODMessageText");

        SetIfNull(so, "bsodErrorCodeText", code);
        SetIfNull(so, "bsodMessageText", msg);
    }

    // =========================================================
    //  HELPERS
    // =========================================================

    GameObject GetOrCreatePanel(RectTransform parent, string name, Color bgColor)
    {
        Transform existing = parent.Find(name);
        if (existing != null) return existing.gameObject;

        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = bgColor;
        return go;
    }

    GameObject GetOrCreateChild(GameObject parent, string name)
    {
        Transform existing = parent.transform.Find(name);
        if (existing != null) return existing.gameObject;

        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent.transform, false);
        return go;
    }

    void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    Image EnsureImage(GameObject go)
    {
        Image img = go.GetComponent<Image>();
        if (img == null) img = go.AddComponent<Image>();
        return img;
    }

    TextMeshProUGUI EnsureTMP(GameObject go)
    {
        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        if (tmp == null) tmp = go.AddComponent<TextMeshProUGUI>();
        return tmp;
    }

    TextMeshProUGUI FindTMPInChildren(GameObject parent, string childName)
    {
        Transform t = parent.transform.Find(childName);
        if (t == null) return null;
        return t.GetComponent<TextMeshProUGUI>();
    }

    void SetIfNull(SerializedObject so, string propName, Object value)
    {
        SerializedProperty prop = so.FindProperty(propName);
        if (prop == null) return;
        if (prop.objectReferenceValue == null)
            prop.objectReferenceValue = value;
    }
}
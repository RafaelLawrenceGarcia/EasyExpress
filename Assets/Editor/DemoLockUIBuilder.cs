#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

public class DemoLockUIBuilder : MonoBehaviour
{
    [MenuItem("EasyExpress/Create Demo Lock UI")]
    public static void CreateDemoLockUI()
    {
        // ── Find or create Canvas ──
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // ── Root Panel (full-screen overlay) ──
        GameObject root = new GameObject("DemoLockPanel");
        root.transform.SetParent(canvas.transform, false);

        RectTransform rootRT = root.AddComponent<RectTransform>();
        rootRT.anchorMin = Vector2.zero;
        rootRT.anchorMax = Vector2.one;
        rootRT.offsetMin = Vector2.zero;
        rootRT.offsetMax = Vector2.zero;

        Image rootBG = root.AddComponent<Image>();
        rootBG.color = new Color(0.039f, 0.102f, 0.180f, 0.97f);

        // Sort order — render on top of everything
        Canvas rootCanvas = root.AddComponent<Canvas>();
        rootCanvas.overrideSorting = true;
        rootCanvas.sortingOrder = 999;
        root.AddComponent<GraphicRaycaster>();

        // ── Gradient overlay for blue-to-green feel ──
        GameObject gradientOverlay = new GameObject("GradientOverlay");
        gradientOverlay.transform.SetParent(root.transform, false);

        RectTransform gradRT = gradientOverlay.AddComponent<RectTransform>();
        gradRT.anchorMin = new Vector2(0, 0);
        gradRT.anchorMax = new Vector2(1, 0.5f);
        gradRT.offsetMin = Vector2.zero;
        gradRT.offsetMax = Vector2.zero;

        Image gradImg = gradientOverlay.AddComponent<Image>();
        gradImg.color = new Color(0.05f, 0.157f, 0.094f, 0.6f);
        gradImg.raycastTarget = false;

        // ── Center Container ──
        GameObject center = new GameObject("CenterContainer");
        center.transform.SetParent(root.transform, false);

        RectTransform centerRT = center.AddComponent<RectTransform>();
        centerRT.anchorMin = new Vector2(0.5f, 0.5f);
        centerRT.anchorMax = new Vector2(0.5f, 0.5f);
        centerRT.sizeDelta = new Vector2(460, 400);
        centerRT.anchoredPosition = Vector2.zero;

        VerticalLayoutGroup centerVLG = center.AddComponent<VerticalLayoutGroup>();
        centerVLG.childAlignment = TextAnchor.MiddleCenter;
        centerVLG.childControlWidth = false;
        centerVLG.childControlHeight = false;
        centerVLG.childForceExpandWidth = false;
        centerVLG.childForceExpandHeight = false;
        centerVLG.spacing = 8;
        centerVLG.padding = new RectOffset(20, 20, 20, 20);

        // ── Lock Icon Circle ──
        GameObject iconCircle = new GameObject("IconCircle");
        iconCircle.transform.SetParent(center.transform, false);

        RectTransform iconCircleRT = iconCircle.AddComponent<RectTransform>();
        iconCircleRT.sizeDelta = new Vector2(72, 72);

        Image iconCircleBG = iconCircle.AddComponent<Image>();
        iconCircleBG.color = new Color(0.176f, 0.706f, 0.471f, 0.15f);
        iconCircleBG.raycastTarget = false;

        // Make it circular
        Mask iconMask = iconCircle.AddComponent<Mask>();
        iconMask.showMaskGraphic = true;

        LayoutElement iconCircleLE = iconCircle.AddComponent<LayoutElement>();
        iconCircleLE.preferredWidth = 72;
        iconCircleLE.preferredHeight = 72;

        // Lock icon text (🔒 fallback — replace with your own icon sprite if preferred)
        GameObject lockIcon = new GameObject("LockIcon");
        lockIcon.transform.SetParent(iconCircle.transform, false);

        RectTransform lockIconRT = lockIcon.AddComponent<RectTransform>();
        lockIconRT.anchorMin = Vector2.zero;
        lockIconRT.anchorMax = Vector2.one;
        lockIconRT.offsetMin = Vector2.zero;
        lockIconRT.offsetMax = Vector2.zero;

        TextMeshProUGUI lockIconTMP = lockIcon.AddComponent<TextMeshProUGUI>();
        lockIconTMP.text = "<size=36>\u2191</size>";
        lockIconTMP.fontSize = 36;
        lockIconTMP.color = new Color(0.176f, 0.722f, 0.471f, 1f);
        lockIconTMP.alignment = TextAlignmentOptions.Center;
        lockIconTMP.enableWordWrapping = false;
        lockIconTMP.raycastTarget = false;

        // ── Spacer ──
        CreateSpacer(center.transform, 16);

        // ── Title ──
        GameObject title = CreateText(center.transform, "TitleText",
            "Thanks for playing the demo!",
            24, new Color(0.91f, 0.957f, 0.973f, 1f), TextAlignmentOptions.Center, 500);
        title.GetComponent<LayoutElement>().preferredWidth = 420;

        // ── Spacer ──
        CreateSpacer(center.transform, 4);

        // ── Subtitle line 1 ──
        GameObject sub1 = CreateText(center.transform, "SubtitleText1",
            "You've completed <color=#2DB878>Day 3</color> \u2014 the demo ends here.",
            15, new Color(0.784f, 0.882f, 0.941f, 0.55f), TextAlignmentOptions.Center, 400);
        sub1.GetComponent<TextMeshProUGUI>().richText = true;
        sub1.GetComponent<LayoutElement>().preferredWidth = 420;

        // ── Spacer ──
        CreateSpacer(center.transform, 2);

        // ── Subtitle line 2 ──
        GameObject sub2 = CreateText(center.transform, "SubtitleText2",
            "Purchase the full game to unlock all days,\nparts, and workstations.",
            15, new Color(0.784f, 0.882f, 0.941f, 0.55f), TextAlignmentOptions.Center, 400);
        sub2.GetComponent<LayoutElement>().preferredWidth = 420;

        // ── Spacer ──
        CreateSpacer(center.transform, 24);

        // ── Buy Button ──
        GameObject buyBtn = CreateButton(center.transform, "BuyButton",
            "Buy full game",
            new Color(0.102f, 0.478f, 0.710f, 1f),  // Blue-green gradient start
            Color.white, 15, new Vector2(260, 46));

        // Add a gradient feel by using a slightly green-tinted color
        Image buyBtnImg = buyBtn.GetComponent<Image>();
        buyBtnImg.color = new Color(0.133f, 0.533f, 0.533f, 1f);

        // ── Spacer ──
        CreateSpacer(center.transform, 4);

        // ── Main Menu Button ──
        GameObject menuBtn = CreateButton(center.transform, "MainMenuButton",
            "Return to main menu",
            new Color(1f, 1f, 1f, 0.04f),
            new Color(0.784f, 0.882f, 0.941f, 0.5f), 13, new Vector2(260, 42));

        Image menuBtnImg = menuBtn.GetComponent<Image>();
        menuBtnImg.color = new Color(1f, 1f, 1f, 0.04f);

        // Add outline border
        Outline menuOutline = menuBtn.AddComponent<Outline>();
        menuOutline.effectColor = new Color(0.176f, 0.706f, 0.471f, 0.3f);
        menuOutline.effectDistance = new Vector2(1, 1);

        // ── Spacer ──
        CreateSpacer(center.transform, 20);

        // ── Footer ──
        CreateText(center.transform, "FooterText",
            "EasyExpress v1.0 \u2014 Thank you for playing!",
            12, new Color(0.784f, 0.882f, 0.941f, 0.2f), TextAlignmentOptions.Center, 400);

        // ── Add DemoLockManager component ──
        DemoLockManager manager = root.GetComponent<DemoLockManager>();
        if (manager == null) manager = root.AddComponent<DemoLockManager>();

        manager.lockPanel = root;
        manager.buyButton = buyBtn.GetComponent<Button>();
        manager.mainMenuButton = menuBtn.GetComponent<Button>();

        root.SetActive(false);

        // Select and ping
        Selection.activeGameObject = root;
        EditorGUIUtility.PingObject(root);

        Debug.Log("[DemoLockUIBuilder] Demo Lock UI created! " +
                  "Set your purchase URL and main menu scene on the DemoLockManager component.");
    }

    // ── HELPERS ──

    static GameObject CreateText(Transform parent, string name, string content,
        float fontSize, Color color, TextAlignmentOptions alignment, float fontWeight)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);

        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(420, 0);

        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = content;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.enableWordWrapping = true;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.raycastTarget = false;

        if (fontWeight >= 500)
            tmp.fontStyle = FontStyles.Bold;

        ContentSizeFitter csf = obj.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        LayoutElement le = obj.AddComponent<LayoutElement>();
        le.preferredWidth = 420;

        return obj;
    }

    static GameObject CreateButton(Transform parent, string name, string label,
        Color bgColor, Color textColor, float fontSize, Vector2 size)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);

        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.sizeDelta = size;

        Image img = obj.AddComponent<Image>();
        img.color = bgColor;
        img.type = Image.Type.Sliced;

        Button btn = obj.AddComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 1f, 1f, 0.9f);
        colors.pressedColor = new Color(1f, 1f, 1f, 0.7f);
        btn.colors = colors;

        LayoutElement le = obj.AddComponent<LayoutElement>();
        le.preferredWidth = size.x;
        le.preferredHeight = size.y;

        // Button text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(obj.transform, false);

        RectTransform textRT = textObj.AddComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = fontSize;
        tmp.color = textColor;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;
        tmp.raycastTarget = false;

        return obj;
    }

    static void CreateSpacer(Transform parent, float height)
    {
        GameObject spacer = new GameObject("Spacer");
        spacer.transform.SetParent(parent, false);
        spacer.AddComponent<RectTransform>();

        LayoutElement le = spacer.AddComponent<LayoutElement>();
        le.preferredHeight = height;
        le.flexibleWidth = 1;
    }
}
#endif

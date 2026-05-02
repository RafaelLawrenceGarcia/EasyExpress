// ============================================================
//  MonitorButtonHighlight.cs
//  Easy Express – Thesis Project
// ============================================================
//  Creates a pulsing ring outline around a target UI element
//  (button, icon, panel) on the monitor overlay to guide the
//  player during the tutorial.
//
//  Works at sort order 150 — above the monitor (50) and below
//  the JobCompletionUI (200).
//
//  USAGE:
//    MonitorButtonHighlight.Instance.HighlightButton(someRectTransform);
//    MonitorButtonHighlight.Instance.ClearHighlight();
//
//  Called automatically by TutorialManager.uiguide.cs via the
//  PointAt() helper whenever the tutorial needs to draw attention
//  to a specific monitor button.
//
//  SETUP:
//    Attach to any persistent GameObject (e.g. TutorialManager).
//    No inspector wiring needed — creates its own canvas at runtime.
// ============================================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MonitorButtonHighlight : MonoBehaviour
{
    public static MonitorButtonHighlight Instance;

    [Header("Ring Appearance")]
    public Color ringColor       = new Color(0.29f, 0.88f, 1f, 0.9f);
    public float ringThickness   = 3f;
    public float ringPadding     = 8f;
    public float cornerRadius    = 8f;

    [Header("Pulse Animation")]
    public float pulseSpeed      = 3f;
    public float pulseMinAlpha   = 0.3f;
    public float pulseMaxAlpha   = 1.0f;
    public float pulseScaleMin   = 1.0f;
    public float pulseScaleMax   = 1.08f;

    [Header("Label")]
    public Color labelBgColor    = new Color(0.29f, 0.88f, 1f, 0.15f);
    public Color labelTextColor  = new Color(0.29f, 0.88f, 1f, 1f);
    public float labelFontSize   = 12f;
    public Vector2 labelOffset   = new Vector2(0, -32f);

    // ── Runtime ──────────────────────────────────────────────────

    private Canvas _highlightCanvas;
    private GameObject _ringGO;
    private Outline _ringOutline;
    private Image _ringImage;
    private RectTransform _ringRT;

    private GameObject _labelGO;
    private TextMeshProUGUI _labelTMP;
    private Image _labelBG;
    private RectTransform _labelRT;

    private RectTransform _target;
    private Canvas _targetRootCanvas;
    private bool _isShowing;
    private float _pulseTimer;

    // ─────────────────────────────────────────────────────────────

    void Awake()
    {
        Instance = this;
        CreateHighlightCanvas();
    }

    void LateUpdate()
    {
        if (!_isShowing || _target == null)
        {
            if (_isShowing) ClearHighlight();
            return;
        }

        // ── Follow the target button ─────────────────────────────
        FollowTarget();

        // ── Pulse animation ──────────────────────────────────────
        _pulseTimer += Time.unscaledDeltaTime * pulseSpeed;

        float wave = (Mathf.Sin(_pulseTimer) + 1f) * 0.5f;

        // Alpha pulse
        float alpha = Mathf.Lerp(pulseMinAlpha, pulseMaxAlpha, wave);
        if (_ringOutline != null)
        {
            Color c = ringColor;
            c.a = alpha;
            _ringOutline.effectColor = c;
        }

        // Scale pulse
        float scale = Mathf.Lerp(pulseScaleMin, pulseScaleMax, wave);
        if (_ringRT != null)
            _ringRT.localScale = Vector3.one * scale;
    }

    // ═══════════════════════════════════════════════════════════
    //  PUBLIC API
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Show a pulsing ring around the given UI element.
    /// Optionally show a text label below it (e.g. "Click here").
    /// </summary>
    public void HighlightButton(RectTransform target, string label = null)
    {
        if (target == null) return;

        _target = target;
        _isShowing = true;
        _pulseTimer = 0f;

        // Find the root canvas of the target to convert coordinates
        _targetRootCanvas = target.GetComponentInParent<Canvas>();
        if (_targetRootCanvas != null)
            _targetRootCanvas = _targetRootCanvas.rootCanvas;

        // Show ring
        if (_ringGO != null) _ringGO.SetActive(true);

        // Label
        bool hasLabel = !string.IsNullOrEmpty(label);
        if (_labelGO != null)
        {
            _labelGO.SetActive(hasLabel);
            if (hasLabel && _labelTMP != null)
                _labelTMP.text = label;
        }

        // Immediately position
        FollowTarget();

        Debug.Log($"[ButtonHighlight] Highlighting '{target.name}'" +
                  (hasLabel ? $" — \"{label}\"" : ""));
    }

    /// <summary>
    /// Remove the highlight ring.
    /// </summary>
    public void ClearHighlight()
    {
        _isShowing = false;
        _target = null;
        _targetRootCanvas = null;

        if (_ringGO != null)
        {
            _ringGO.SetActive(false);
            _ringRT.localScale = Vector3.one;
        }
        if (_labelGO != null)
            _labelGO.SetActive(false);
    }

    /// <summary>Is the highlight currently visible?</summary>
    public bool IsHighlighting => _isShowing;

    // ═══════════════════════════════════════════════════════════
    //  FOLLOW TARGET
    // ═══════════════════════════════════════════════════════════

    void FollowTarget()
    {
        if (_target == null || _ringRT == null) return;

        // Get the target's screen-space rect
        Vector3[] corners = new Vector3[4];
        _target.GetWorldCorners(corners);

        // Convert to our canvas's local space
        Canvas rootCanvas = _highlightCanvas;
        RectTransform canvasRT = rootCanvas.GetComponent<RectTransform>();

        // Screen position of target center
        Vector3 center = (corners[0] + corners[2]) * 0.5f;
        Vector2 size = new Vector2(
            Vector3.Distance(corners[0], corners[3]),
            Vector3.Distance(corners[0], corners[1]));

        // Convert screen point to our canvas local point
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRT, center, null, out localPoint);

        // Size the ring to match target + padding
        _ringRT.anchoredPosition = localPoint;
        _ringRT.sizeDelta = size + new Vector2(ringPadding * 2, ringPadding * 2);

        // Position label below
        if (_labelRT != null && _labelGO.activeSelf)
        {
            _labelRT.anchoredPosition = localPoint + labelOffset +
                new Vector2(0, -(size.y * 0.5f + ringPadding));
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  CREATE CANVAS & RING AT RUNTIME
    // ═══════════════════════════════════════════════════════════

    void CreateHighlightCanvas()
    {
        // ── Canvas (sort order 150: above monitor 50, below completion 200) ──
        GameObject canvasGO = new GameObject("MonitorButtonHighlightCanvas");
        canvasGO.transform.SetParent(transform, false);

        _highlightCanvas = canvasGO.AddComponent<Canvas>();
        _highlightCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _highlightCanvas.sortingOrder = 150;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        // No GraphicRaycaster — this canvas should NOT block clicks
        // The ring is purely visual; clicks pass through to the monitor

        // ── Ring GameObject ──────────────────────────────────────
        _ringGO = new GameObject("HighlightRing", typeof(RectTransform), typeof(Image));
        _ringGO.transform.SetParent(canvasGO.transform, false);

        _ringRT = _ringGO.GetComponent<RectTransform>();
        _ringRT.anchorMin = new Vector2(0.5f, 0.5f);
        _ringRT.anchorMax = new Vector2(0.5f, 0.5f);
        _ringRT.pivot = new Vector2(0.5f, 0.5f);
        _ringRT.sizeDelta = new Vector2(100, 40);

        _ringImage = _ringGO.GetComponent<Image>();
        _ringImage.color = Color.clear; // transparent fill — outline only
        _ringImage.raycastTarget = false;

        _ringOutline = _ringGO.AddComponent<Outline>();
        _ringOutline.effectColor = ringColor;
        _ringOutline.effectDistance = new Vector2(ringThickness, ringThickness);

        // Add a second outline for extra glow
        Outline outerGlow = _ringGO.AddComponent<Outline>();
        outerGlow.effectColor = new Color(ringColor.r, ringColor.g, ringColor.b, 0.2f);
        outerGlow.effectDistance = new Vector2(ringThickness + 2, ringThickness + 2);

        _ringGO.SetActive(false);

        // ── Label GameObject ─────────────────────────────────────
        _labelGO = new GameObject("HighlightLabel", typeof(RectTransform), typeof(Image));
        _labelGO.transform.SetParent(canvasGO.transform, false);

        _labelRT = _labelGO.GetComponent<RectTransform>();
        _labelRT.anchorMin = new Vector2(0.5f, 0.5f);
        _labelRT.anchorMax = new Vector2(0.5f, 0.5f);
        _labelRT.pivot = new Vector2(0.5f, 1f);
        _labelRT.sizeDelta = new Vector2(160, 26);

        _labelBG = _labelGO.GetComponent<Image>();
        _labelBG.color = labelBgColor;
        _labelBG.raycastTarget = false;

        GameObject labelTextGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelTextGO.transform.SetParent(_labelGO.transform, false);

        RectTransform labelTextRT = labelTextGO.GetComponent<RectTransform>();
        labelTextRT.anchorMin = Vector2.zero;
        labelTextRT.anchorMax = Vector2.one;
        labelTextRT.offsetMin = new Vector2(8, 0);
        labelTextRT.offsetMax = new Vector2(-8, 0);

        _labelTMP = labelTextGO.GetComponent<TextMeshProUGUI>();
        _labelTMP.fontSize = labelFontSize;
        _labelTMP.color = labelTextColor;
        _labelTMP.alignment = TextAlignmentOptions.Center;
        _labelTMP.enableWordWrapping = false;
        _labelTMP.raycastTarget = false;

        // Auto-size label width
        ContentSizeFitter csf = _labelGO.AddComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

        HorizontalLayoutGroup hlg = _labelGO.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(12, 12, 4, 4);
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;

        _labelGO.SetActive(false);
    }
}

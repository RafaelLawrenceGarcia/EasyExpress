// ============================================================
//  TutorialUIPointer.cs
//  A canvas-space pointer arrow that can point at:
//    - World-space transforms (3D objects in the scene)
//    - UI RectTransforms (buttons, panels on screen)
//    - Fixed screen positions (arbitrary pixel coordinates)
//
//  SETUP:
//  1. Create a Canvas "TutorialPointerCanvas" (Sort Order 99)
//  2. Inside, create UI Image "Pointer" with an arrow sprite
//     - Pivot (0.5, 0) so it points downward from its tip
//  3. Attach this script to the Pointer Image
//  4. Drag the Image's RectTransform into 'pointerRect'
//  5. TutorialManager calls ShowAtWorld/ShowAtUI/ShowAtScreen/Hide
// ============================================================
using UnityEngine;
using UnityEngine.UI;

public class TutorialUIPointer : MonoBehaviour
{
    public static TutorialUIPointer Instance;

    [Header("References")]
    public RectTransform pointerRect;
    public Canvas parentCanvas;
    public Camera playerCamera;

    [Header("Appearance")]
    public Color pointerColor = new Color(1f, 0.85f, 0f, 1f);
    public float pointerSize = 50f;

    [Header("Animation")]
    public float bounceAmount = 12f;
    public float bounceSpeed = 4f;
    public float pulseMin = 0.9f;
    public float pulseMax = 1.1f;

    // ── State ──
    enum PointerMode { Hidden, World, UI, Screen }
    PointerMode mode = PointerMode.Hidden;
    Transform worldTarget;
    RectTransform uiTarget;
    Vector2 screenTarget;
    Vector2 screenOffset = Vector2.zero;
    float bounceTimer;
    Image pointerImage;

    void Awake()
    {
        Instance = this;
        if (pointerRect != null) pointerImage = pointerRect.GetComponent<Image>();
        if (playerCamera == null) playerCamera = Camera.main;
        if (parentCanvas == null && pointerRect != null)
            parentCanvas = pointerRect.GetComponentInParent<Canvas>();

        ApplyAppearance();
        gameObject.SetActive(false);
    }

    void ApplyAppearance()
    {
        if (pointerImage != null) pointerImage.color = pointerColor;
        if (pointerRect != null) pointerRect.sizeDelta = new Vector2(pointerSize, pointerSize);
    }

    void LateUpdate()
    {

        if (mode == PointerMode.Hidden || pointerRect == null) return;

        // Always use the current main camera (handles inspection camera swaps)
        playerCamera = Camera.main;
        if (playerCamera == null) return;

        bounceTimer += Time.deltaTime * bounceSpeed;
        float bounce = Mathf.Sin(bounceTimer) * bounceAmount;
        float pulse = Mathf.Lerp(pulseMin, pulseMax, (Mathf.Sin(bounceTimer * 1.5f) + 1f) * 0.5f);

        Vector2 targetScreenPos = Vector2.zero;

        switch (mode)
        {
            case PointerMode.World:
                if (worldTarget == null) { Hide(); return; }
                if (playerCamera == null) playerCamera = Camera.main;
                if (playerCamera == null) return;
                Vector3 sp = playerCamera.WorldToScreenPoint(worldTarget.position);
                if (sp.z < 0) { pointerRect.gameObject.SetActive(false); return; }
                pointerRect.gameObject.SetActive(true);
                targetScreenPos = new Vector2(sp.x, sp.y);
                break;

            case PointerMode.UI:
                if (uiTarget == null) { Hide(); return; }
                // Get the UI element's screen-space center
                Vector3[] corners = new Vector3[4];
                uiTarget.GetWorldCorners(corners);
                Vector3 center = (corners[0] + corners[2]) * 0.5f;
                // If overlay canvas, corners are already in screen space
                if (parentCanvas != null && parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
                    targetScreenPos = center;
                else
                    targetScreenPos = RectTransformUtility.WorldToScreenPoint(parentCanvas.worldCamera, center);
                break;

            case PointerMode.Screen:
                targetScreenPos = screenTarget;
                break;
        }

        // Apply offset (e.g., above the target)
        targetScreenPos += screenOffset;
        targetScreenPos.y += bounce;

        // Convert screen position to canvas local position
        Vector2 canvasPos;
        RectTransform canvasRect = parentCanvas.GetComponent<RectTransform>();
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, targetScreenPos, null, out canvasPos);

        pointerRect.localPosition = canvasPos;

        // Point downward (toward the target below)
        pointerRect.localRotation = Quaternion.Euler(0, 0, 180f);

        // Pulse scale
        pointerRect.localScale = Vector3.one * pulse;
    }

    // ─── Public API ──────────────────────────────────────────────

    /// <summary>Point at a 3D world-space transform.</summary>
    public void ShowAtWorld(Transform target, Vector2 offset = default)
    {
        worldTarget = target;
        screenOffset = offset == default ? new Vector2(0, pointerSize + 10) : offset;
        mode = PointerMode.World;
        bounceTimer = 0;
        gameObject.SetActive(true);
    }

    /// <summary>Point at a UI element (button, panel, etc.).</summary>
    public void ShowAtUI(RectTransform target, Vector2 offset = default)
    {
        uiTarget = target;
        screenOffset = offset == default ? new Vector2(0, 40) : offset;
        mode = PointerMode.UI;
        bounceTimer = 0;
        gameObject.SetActive(true);
    }

    /// <summary>Point at a fixed screen position (pixels from bottom-left).</summary>
    public void ShowAtScreen(Vector2 position, Vector2 offset = default)
    {
        screenTarget = position;
        screenOffset = offset == default ? new Vector2(0, 40) : offset;
        mode = PointerMode.Screen;
        bounceTimer = 0;
        gameObject.SetActive(true);
    }

    /// <summary>Hide the pointer.</summary>
    public void Hide()
    {
        mode = PointerMode.Hidden;
        worldTarget = null;
        uiTarget = null;
        gameObject.SetActive(false);
    }

    /// <summary>Is the pointer currently visible?</summary>
    public bool IsShowing => mode != PointerMode.Hidden;
}
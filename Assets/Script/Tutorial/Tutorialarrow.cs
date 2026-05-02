using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// TutorialArrow — A bouncing UI arrow that points the player toward a target.
///
/// HOW IT WORKS:
/// - Attach this script to a UI Image (your arrow graphic) inside a Screen-Space Canvas.
/// - Call SetTarget(transform) to make it point toward any world-space object.
/// - Call Show() / Hide() to toggle visibility per tutorial step.
/// - The arrow sits on the edge of the screen and rotates to face the target.
///
/// SETUP IN UNITY:
/// 1. In your Tutorial Canvas, create a UI Image named "TutorialArrow".
///    ── Use a simple arrow sprite (e.g. a white triangle pointing UP).
/// 2. Set its Pivot to (0.5, 0) so it rotates around its tail, not its center.
/// 3. Attach this script to that Image object.
/// 4. Drag the Image component into the 'arrowImage' field below.
/// 5. In TutorialManager, drag this GameObject into the 'tutorialArrow' field.
///
/// THAT'S IT. TutorialManager will call Show/Hide/SetTarget automatically.
/// </summary>
public class TutorialArrow : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  INSPECTOR FIELDS
    // ─────────────────────────────────────────────

    [Header("References")]
    [Tooltip("Drag the UI Image (arrow graphic) here.")]
    public RectTransform arrowRect;

    [Tooltip("The camera used to project world → screen positions. Leave empty to auto-find Main Camera.")]
    public Camera playerCamera;

    [Header("Arrow Appearance")]
    [Tooltip("How far from the screen center the arrow sits (in pixels).")]
    public float screenEdgeOffset = 150f;

    [Tooltip("Color of the arrow.")]
    public Color arrowColor = new Color(1f, 0.85f, 0f, 1f); // Gold yellow

    [Tooltip("Size of the arrow UI image in pixels.")]
    public float arrowSize = 80f;

    [Header("Bounce Animation")]
    [Tooltip("How far the arrow bounces back and forth (pixels).")]
    public float bounceAmount = 18f;

    [Tooltip("Speed of the bounce.")]
    public float bounceSpeed = 3.5f;

    // ─────────────────────────────────────────────
    //  PRIVATE STATE
    // ─────────────────────────────────────────────

    private Transform targetTransform;      // World-space object to point at
    private Image arrowImage;
    private bool isVisible = false;
    private float bounceTimer = 0f;

    // ─────────────────────────────────────────────
    //  UNITY LIFECYCLE
    // ─────────────────────────────────────────────

    void Awake()
    {
        // Auto-find image component if rect is set
        if (arrowRect != null)
            arrowImage = arrowRect.GetComponent<Image>();

        // Auto-find camera
        if (playerCamera == null)
            playerCamera = Camera.main;

        // Apply color and size
        if (arrowImage != null)
            arrowImage.color = arrowColor;

        if (arrowRect != null)
            arrowRect.sizeDelta = new Vector2(arrowSize, arrowSize);

        // Start hidden
        gameObject.SetActive(false);
    }

    void LateUpdate()
    {
        // Only run if we have a target and the arrow is visible
        if (!isVisible || targetTransform == null || playerCamera == null || arrowRect == null)
            return;

        // ── 1. Project the target's world position to screen space ──
        Vector3 screenPos = playerCamera.WorldToScreenPoint(targetTransform.position);

        // ── 2. Figure out if the target is behind the camera ──
        bool isBehind = screenPos.z < 0f;
        if (isBehind)
        {
            // Flip the position so the arrow still points correctly when behind us
            screenPos *= -1f;
        }

        // ── 3. Calculate the direction from screen center → target ──
        Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        Vector2 direction = new Vector2(screenPos.x - screenCenter.x, screenPos.y - screenCenter.y);

        // ── 4. Check if target is already ON screen ──
        bool isOnScreen = !isBehind
            && screenPos.x > 0 && screenPos.x < Screen.width
            && screenPos.y > 0 && screenPos.y < Screen.height;

        // ── 5. Bounce animation ──
        bounceTimer += Time.deltaTime * bounceSpeed;
        float bounce = Mathf.Sin(bounceTimer) * bounceAmount;

        if (isOnScreen)
        {
            // ── TARGET IS VISIBLE: Place arrow DIRECTLY on top of the target ──
            // Convert screen pos to canvas local pos
            RectTransform canvasRect = arrowRect.GetComponentInParent<Canvas>().GetComponent<RectTransform>();
            Vector2 canvasPos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, screenPos, null, out canvasPos);

            // Offset the arrow above the target so it doesn't overlap
            canvasPos.y += arrowSize + bounce;
            arrowRect.localPosition = canvasPos;

            // Point the arrow downward (toward the target below it)
            arrowRect.localRotation = Quaternion.Euler(0f, 0f, 180f);
        }
        else
        {
            // ── TARGET IS OFF SCREEN: Place arrow on screen edge pointing at target ──
            direction.Normalize();

            // Clamp to screen edge
            float angle = Mathf.Atan2(direction.y, direction.x);
            float cos = Mathf.Cos(angle);
            float sin = Mathf.Sin(angle);

            // Find which edge of the screen we hit first
            float halfW = (Screen.width * 0.5f) - screenEdgeOffset;
            float halfH = (Screen.height * 0.5f) - screenEdgeOffset;

            float tX = (cos != 0) ? halfW / Mathf.Abs(cos) : float.MaxValue;
            float tY = (sin != 0) ? halfH / Mathf.Abs(sin) : float.MaxValue;
            float t = Mathf.Min(tX, tY);

            Vector2 edgePos = new Vector2(cos * t, sin * t);

            // Add bounce along the pointing direction
            edgePos += direction * bounce;

            arrowRect.localPosition = edgePos;

            // Rotate arrow to point toward target (+90 because our arrow sprite points UP)
            float angleDeg = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
            arrowRect.localRotation = Quaternion.Euler(0f, 0f, angleDeg);
        }
    }

    // ─────────────────────────────────────────────
    //  PUBLIC API  (called by TutorialManager)
    // ─────────────────────────────────────────────

    /// <summary>
    /// Point the arrow at this world-space transform.
    /// </summary>
    public void SetTarget(Transform target)
    {
        targetTransform = target;
    }

    /// <summary>
    /// Show the arrow. Call SetTarget() first!
    /// </summary>
    public void Show()
    {
        isVisible = true;
        bounceTimer = 0f;
        gameObject.SetActive(true);
    }

    /// <summary>
    /// Hide the arrow.
    /// </summary>
    public void Hide()
    {
        isVisible = false;
        gameObject.SetActive(false);
        targetTransform = null;
    }

    /// <summary>
    /// Shortcut: set target AND show at the same time.
    /// </summary>
    public void ShowAt(Transform target)
    {
        SetTarget(target);
        Show();
    }
}
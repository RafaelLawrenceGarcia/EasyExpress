using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// TutorialHighlighter — Replaces TutorialArrow.
/// 1. Applies a pulsing outline to the target object.
/// 2. Draws animated NavMesh dots on the ground leading to the target.
///
/// SETUP:
///   1. Attach to any persistent GameObject in the scene.
///   2. Create an outline Material (Shader: Universal Render Pipeline/Lit,
///      enable Emission, set emission color to cyan/gold).
///   3. Create a small Quad or Sphere prefab for path dots (no collider,
///      assign a bright emissive material).
///   4. Drag both into the inspector fields.
///   5. In TutorialManager, replace the tutorialArrow field with this.
/// </summary>
public class TutorialHighlighter : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────

    [Header("Highlight Outline")]
    [Tooltip("Material added on top of the target object's renderers. " +
             "Use an emission or outline shader. Color is set at runtime.")]
    public Material outlineMaterial;

    [Tooltip("Base emission color of the pulsing highlight.")]
    public Color highlightColor = new Color(0.3f, 0.9f, 1f, 1f);

    [Tooltip("Pulse oscillation speed.")]
    public float pulseSpeed = 2.5f;

    [Tooltip("Emission intensity range during pulse (min / max).")]
    public float pulseMin = 0.3f;
    public float pulseMax = 1.8f;

    [Header("Ground Path")]
    [Tooltip("Small quad/sphere prefab used as a path dot. " +
             "Should have an emissive material and NO collider.")]
    public GameObject pathDotPrefab;

    [Tooltip("World-space distance between each dot.")]
    public float dotSpacing = 1.1f;
    [Tooltip("Maximum dots drawn (caps very long paths).")]
    public int maxDots = 18;
    [Tooltip("Height above ground the dots float.")]
    public float dotYOffset = 0.15f;

    [Tooltip("How much each dot bobs up and down.")]
    public float dotBobAmount = 0.1f;

    [Tooltip("Bob speed (staggered per dot for a wave effect).")]
    public float dotBobSpeed = 2.2f;

    [Tooltip("Dots animate from player toward target — wave travel speed.")]
    public float waveSpeed = 1.4f;

    [Tooltip("Color of path dots.")]
    public Color dotColor = new Color(0.3f, 0.9f, 1f, 0.85f);

    [Header("References")]
    public Transform playerTransform;

    // ── Runtime State ─────────────────────────────────────────────────

    private Transform _target;
    private bool _isActive;

    private readonly List<Renderer> _highlightedRenderers = new List<Renderer>();
    private readonly Dictionary<Renderer, Material[]> _originalMats
        = new Dictionary<Renderer, Material[]>();

    private readonly List<GameObject> _dots = new List<GameObject>();
    private readonly List<Vector3> _dotBasePositions = new List<Vector3>();

    private Material _outlineInstance;
    private Coroutine _pulseRoutine;
    private Coroutine _pathRoutine;
    private float _pathTimer;
    private const float PATH_REFRESH = 1.2f;   // seconds between NavMesh recalcs

    // ═════════════════════════════════════════════════════════════════
    //  UNITY LIFECYCLE
    // ═════════════════════════════════════════════════════════════════

    void Awake()
    {
        if (outlineMaterial != null)
            _outlineInstance = new Material(outlineMaterial);
    }

    void Start()
    {
        if (playerTransform == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) playerTransform = p.transform;
        }
    }

    void Update()
    {
        if (!_isActive || playerTransform == null || _target == null) return;

        // Periodic path refresh
        _pathTimer += Time.deltaTime;
        if (_pathTimer >= PATH_REFRESH)
        {
            _pathTimer = 0f;
            RefreshPath();
        }

        // Animate dot wave
        AnimateDots();
    }

    void OnDestroy()
    {
        if (_outlineInstance != null) Destroy(_outlineInstance);
    }

    // ═════════════════════════════════════════════════════════════════
    //  PUBLIC API  (mirrors TutorialArrow for easy swapping)
    // ═════════════════════════════════════════════════════════════════

    /// <summary>Point highlight + path at a world-space transform.</summary>
    public void ShowAt(Transform target)
    {
        if (target == _target && _isActive) return;
        Hide();

        _target = target;
        _isActive = true;
        _pathTimer = PATH_REFRESH; // force immediate refresh

        ApplyOutline(target);

        if (_pulseRoutine != null) StopCoroutine(_pulseRoutine);
        _pulseRoutine = StartCoroutine(PulseOutline());
    }

    /// <summary>Remove highlight and path.</summary>
    public void Hide()
    {
        _isActive = false;
        _target = null;

        if (_pulseRoutine != null) { StopCoroutine(_pulseRoutine); _pulseRoutine = null; }
        if (_pathRoutine != null) { StopCoroutine(_pathRoutine); _pathRoutine = null; }

        RemoveOutline();
        ClearDots();
    }

    public bool IsShowing => _isActive;

    // ═════════════════════════════════════════════════════════════════
    //  OUTLINE
    // ═════════════════════════════════════════════════════════════════

    void ApplyOutline(Transform target)
    {
        if (_outlineInstance == null || target == null) return;

        _highlightedRenderers.Clear();
        _originalMats.Clear();

        foreach (Renderer r in target.GetComponentsInChildren<Renderer>(true))
        {
            if (r == null) continue;
            _highlightedRenderers.Add(r);
            _originalMats[r] = r.sharedMaterials;

            Material[] newMats = new Material[r.sharedMaterials.Length + 1];
            for (int i = 0; i < r.sharedMaterials.Length; i++)
                newMats[i] = r.sharedMaterials[i];
            newMats[newMats.Length - 1] = _outlineInstance;
            r.sharedMaterials = newMats;
        }
    }

    void RemoveOutline()
    {
        foreach (var kv in _originalMats)
        {
            if (kv.Key == null) continue;
            // Restore to original shared materials regardless of 
            // what happened to the renderer during inspection
            kv.Key.sharedMaterials = kv.Value;
        }
        _highlightedRenderers.Clear();
        _originalMats.Clear();

        // Destroy all material instances we created to prevent leaks
        if (_outlineInstance != null)
        {
            Destroy(_outlineInstance);
            _outlineInstance = null;
        }
        // Recreate instance ready for next ShowAt() call
        if (outlineMaterial != null)
            _outlineInstance = new Material(outlineMaterial);
    }
    IEnumerator PulseOutline()
    {
        float t = 0f;
        while (_isActive)
        {
            t += Time.deltaTime * pulseSpeed;
            float intensity = Mathf.Lerp(pulseMin, pulseMax,
                (Mathf.Sin(t) + 1f) * 0.5f);

            if (_outlineInstance != null)
            {
                Color emissive = highlightColor * intensity;
                _outlineInstance.color = new Color(
                    highlightColor.r, highlightColor.g,
                    highlightColor.b, Mathf.Lerp(0.4f, 0.9f,
                        (Mathf.Sin(t) + 1f) * 0.5f));
                if (_outlineInstance.HasProperty("_EmissionColor"))
                {
                    _outlineInstance.EnableKeyword("_EMISSION");
                    _outlineInstance.SetColor("_EmissionColor", emissive);
                }
            }
            yield return null;
        }
    }

    // ═════════════════════════════════════════════════════════════════
    //  PATH DOTS
    // ═════════════════════════════════════════════════════════════════

    void RefreshPath()
    {
        if (playerTransform == null || _target == null || pathDotPrefab == null)
        {
            ClearDots();
            return;
        }

        // Calculate NavMesh path
        NavMeshPath navPath = new NavMeshPath();
        Vector3 startPos = playerTransform.position;
        Vector3 endPos = _target.position;

        bool hasPath = NavMesh.CalculatePath(startPos, endPos,
            NavMesh.AllAreas, navPath);

        if (!hasPath || navPath.corners.Length < 2)
        {
            // Fallback: straight line
            PlaceDotsAlongPoints(new Vector3[] { startPos, endPos });
            return;
        }

        PlaceDotsAlongPoints(navPath.corners);
    }

    void PlaceDotsAlongPoints(Vector3[] corners)
    {
        ClearDots();
        _dotBasePositions.Clear();

        // Build evenly-spaced positions along the polyline
        List<Vector3> positions = new List<Vector3>();
        float accumulated = dotSpacing * 0.5f; // offset first dot from player

        for (int i = 0; i < corners.Length - 1; i++)
        {
            Vector3 a = corners[i];
            Vector3 b = corners[i + 1];
            float segLen = Vector3.Distance(a, b);
            float walked = 0f;

            while (walked < segLen)
            {
                float t = walked / segLen;
                Vector3 pt = Vector3.Lerp(a, b, t);

                if (accumulated >= dotSpacing)
                {
                    accumulated = 0f;
                    // Sample NavMesh for accurate Y
                    if (NavMesh.SamplePosition(pt, out NavMeshHit hit, 1f, NavMesh.AllAreas))
                        pt.y = hit.position.y;
                    pt.y += dotYOffset;
                    positions.Add(pt);

                    if (positions.Count >= maxDots) goto donePlacing;
                }

                float step = dotSpacing - accumulated;
                walked += step;
                accumulated += step;
            }
            accumulated += segLen - (accumulated % segLen == 0 ? 0 : 0);
        }
    donePlacing:

        // Skip last few dots near target so it doesn't pile up on the object
        int skip = Mathf.Min(2, positions.Count);
        positions.RemoveRange(positions.Count - skip, skip);

        // Spawn dots
        foreach (Vector3 pos in positions)
        {
            GameObject dot = Instantiate(pathDotPrefab, pos, Quaternion.identity);
            dot.transform.SetParent(transform);

            Renderer r = dot.GetComponentInChildren<Renderer>();
            if (r != null)
            {
                Material m = new Material(r.sharedMaterial);
                m.color = dotColor;
                if (m.HasProperty("_EmissionColor"))
                {
                    m.EnableKeyword("_EMISSION");
                    m.SetColor("_EmissionColor", dotColor * 3.5f);
                }
                r.material = m;
            }

            // Scale dots smaller near the player, larger near the target
            float ratio = positions.Count > 1
                ? (float)positions.IndexOf(pos) / (positions.Count - 1)
                : 1f;
            float scale = Mathf.Lerp(0.25f, 0.45f, ratio);
            dot.transform.localScale = Vector3.one * scale;

            _dots.Add(dot);
            _dotBasePositions.Add(pos);
        }
    }

    void AnimateDots()
    {
        float time = Time.time;
        for (int i = 0; i < _dots.Count; i++)
        {
            if (_dots[i] == null) continue;

            // Wave offset per dot index traveling toward target
            float wave = Mathf.Sin(time * dotBobSpeed - i * 0.45f + time * waveSpeed);
            Vector3 pos = _dotBasePositions[i];
            pos.y += wave * dotBobAmount;
            _dots[i].transform.position = pos;

            // Opacity pulse staggered per dot
            Renderer r = _dots[i].GetComponentInChildren<Renderer>();
            if (r != null && r.material != null)
            {
                float alpha = Mathf.Lerp(0.4f, 1f,
                    (Mathf.Sin(time * dotBobSpeed - i * 0.6f) + 1f) * 0.5f);
                Color c = dotColor;
                c.a = alpha;
                r.material.color = c;
            }
        }
    }

    void ClearDots()
    {
        foreach (GameObject d in _dots)
            if (d != null) Destroy(d);
        _dots.Clear();
        _dotBasePositions.Clear();
    }
}
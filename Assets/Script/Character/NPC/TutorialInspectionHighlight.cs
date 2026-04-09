using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// TutorialInspectionHighlight — Highlights parts inside the inspection clone
/// by pulsing the EMISSION on the part's own materials.
///
/// KEY DESIGN:
///   - CacheAndApply stores material INSTANCES once (via rend.materials)
///   - PulseRoutine reuses those same instances (never calls rend.materials again)
///   - RestoreAll restores emission on those instances, then sets sharedMaterials
///     back to originals so all instances are discarded cleanly
///
/// SETUP:
///   1. Add this component to the same GameObject as TutorialManager.
///   2. Drag it into TutorialManager's 'tutorialInspectionHighlight' field.
///   3. tutorialHighlightMaterial is optional — emission-based glow works on its own.
/// </summary>
public class TutorialInspectionHighlight : MonoBehaviour
{
    [Header("Tutorial Highlight Material (Optional Overlay)")]
    [Tooltip("If assigned, appended as an extra material pass.\n" +
             "Leave empty to use emission-only highlighting (recommended).")]
    public Material tutorialHighlightMaterial;

    [Header("Pulse Settings")]
    public float pulseSpeed = 2.5f;
    public float pulseMin = 0.5f;
    public float pulseMax = 2.0f;
    public Color highlightColor = new Color(0.3f, 0.9f, 1f, 1f); // cyan

    // ── Runtime state ─────────────────────────────────────────────

    private Transform _currentTarget;
    private Coroutine _pulseRoutine;

    // Per-material: the INSTANCE we modified + its original emission state
    private struct MaterialState
    {
        public Material matInstance;
        public bool hadEmission;
        public Color originalEmissionColor;
    }

    // Per-renderer: original shared materials so we can fully revert
    private struct RendererState
    {
        public Renderer renderer;
        public Material[] originalSharedMats;
    }

    private readonly List<MaterialState> _matStates = new List<MaterialState>();
    private readonly List<RendererState> _rendStates = new List<RendererState>();

    // ── Lifecycle ────────────────────────────────────────────────

    void OnDestroy()
    {
        Hide();
    }

    // ── Public API ────────────────────────────────────────────────

    public void ShowAt(Transform target)
    {
        if (target == null) return;
        if (target == _currentTarget) return; // already on this target

        Hide(); // clean up previous highlight completely

        _currentTarget = target;
        CacheAndApply(target);

        if (_pulseRoutine != null) StopCoroutine(_pulseRoutine);
        _pulseRoutine = StartCoroutine(PulseRoutine());

        Debug.Log($"[TutorialHighlight] ShowAt: {target.name} " +
                  $"({_rendStates.Count} renderers, {_matStates.Count} materials)");
    }

    public void Hide()
    {
        if (_pulseRoutine != null) { StopCoroutine(_pulseRoutine); _pulseRoutine = null; }
        RestoreAll();
        _currentTarget = null;
    }

    public bool IsShowing => _currentTarget != null;

    // ── Cache originals and enable emission ───────────────────────

    void CacheAndApply(Transform target)
    {
        _matStates.Clear();
        _rendStates.Clear();

        foreach (Renderer rend in target.GetComponentsInChildren<Renderer>(true))
        {
            if (rend == null || !rend.enabled) continue;

            // 1. Cache the SHARED materials so we can fully revert later
            RendererState rs;
            rs.renderer = rend;
            rs.originalSharedMats = rend.sharedMaterials;
            _rendStates.Add(rs);

            // 2. Get material INSTANCES (one call, creates instances for all slots)
            //    We store these and reuse them in PulseRoutine — never call
            //    rend.materials again to avoid creating new instances each frame
            Material[] instances = rend.materials;

            foreach (Material mat in instances)
            {
                if (mat == null) continue;

                MaterialState ms;
                ms.matInstance = mat;
                ms.hadEmission = mat.IsKeywordEnabled("_EMISSION");
                ms.originalEmissionColor = mat.HasProperty("_EmissionColor")
                    ? mat.GetColor("_EmissionColor")
                    : Color.black;

                _matStates.Add(ms);

                // Enable emission ready for pulsing
                mat.EnableKeyword("_EMISSION");
            }
        }

        if (_matStates.Count == 0)
            Debug.LogWarning($"[TutorialHighlight] ShowAt '{target.name}': " +
                             "no enabled renderers found under target.");
    }

    // ── Restore everything ────────────────────────────────────────

    void RestoreAll()
    {
        // Step 1: Restore emission on the material instances we modified.
        // This handles the case where the instances are still in use.
        foreach (var ms in _matStates)
        {
            if (ms.matInstance == null) continue;

            if (!ms.hadEmission)
                ms.matInstance.DisableKeyword("_EMISSION");

            if (ms.matInstance.HasProperty("_EmissionColor"))
                ms.matInstance.SetColor("_EmissionColor", ms.originalEmissionColor);
        }
        _matStates.Clear();

        // Step 2: Restore shared materials on each renderer.
        // This discards all material instances and reverts to the original
        // shared materials, ensuring a completely clean state.
        foreach (var rs in _rendStates)
        {
            if (rs.renderer == null) continue;
            if (rs.originalSharedMats != null)
                rs.renderer.sharedMaterials = rs.originalSharedMats;
        }
        _rendStates.Clear();
    }

    // ── Pulse emission on stored instances ─────────────────────────

    IEnumerator PulseRoutine()
    {
        float t = 0f;
        while (_currentTarget != null)
        {
            t += Time.deltaTime * pulseSpeed;
            float intensity = Mathf.Lerp(pulseMin, pulseMax,
                                         (Mathf.Sin(t) + 1f) * 0.5f);

            Color emissive = highlightColor * intensity;

            // Pulse the STORED material instances — never call rend.materials
            // again, which would create new instances and leak emission state
            foreach (var ms in _matStates)
            {
                if (ms.matInstance == null) continue;
                if (ms.matInstance.HasProperty("_EmissionColor"))
                {
                    ms.matInstance.EnableKeyword("_EMISSION");
                    ms.matInstance.SetColor("_EmissionColor", emissive);
                }
            }

            yield return null;
        }
    }
}
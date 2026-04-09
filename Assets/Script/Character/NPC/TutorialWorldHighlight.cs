using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Highlights any world-space object by pulsing its emission.
/// Separate from TutorialHighlighter (which only works on the inspection clone).
/// Attach to the same GameObject as TutorialManager.
/// </summary>
public class TutorialWorldHighlight : MonoBehaviour
{
    public static TutorialWorldHighlight Instance;

    [Header("Pulse Settings")]
    public Color highlightColor = new Color(0.3f, 0.9f, 1f, 1f);
    public float pulseSpeed = 2.5f;
    public float pulseMin = 0.3f;
    public float pulseMax = 2.5f;

    private Coroutine _pulseRoutine;
    private Transform _currentTarget;

    // Store original emission state so we can restore it
    private readonly List<(Renderer rend, bool hadEmission, Color originalColor)> _originalStates
        = new List<(Renderer, bool, Color)>();

    void Awake() { Instance = this; }

    // ── Public API ────────────────────────────────────────────────

    public void ShowAt(Transform target)
    {
        if (target == null) return;
        Hide(); // clear any previous

        _currentTarget = target;
        CacheAndApply(target);

        if (_pulseRoutine != null) StopCoroutine(_pulseRoutine);
        _pulseRoutine = StartCoroutine(PulseRoutine());
    }

    public void Hide()
    {
        if (_pulseRoutine != null)
        {
            StopCoroutine(_pulseRoutine);
            _pulseRoutine = null;
        }
        RestoreOriginalEmission();
        _currentTarget = null;
    }

    // ── Internal ──────────────────────────────────────────────────

    void CacheAndApply(Transform target)
    {
        _originalStates.Clear();

        foreach (Renderer r in target.GetComponentsInChildren<Renderer>(true))
        {
            if (r == null) continue;

            // Work on a material instance (not shared) so we don't
            // permanently modify the project asset
            Material mat = r.material; // creates instance automatically

            bool hadEmission = mat.IsKeywordEnabled("_EMISSION");
            Color originalColor = mat.HasProperty("_EmissionColor")
                ? mat.GetColor("_EmissionColor")
                : Color.black;

            _originalStates.Add((r, hadEmission, originalColor));

            mat.EnableKeyword("_EMISSION");
        }
    }

    void RestoreOriginalEmission()
    {
        foreach (var entry in _originalStates)
        {
            if (entry.rend == null) continue;

            Material mat = entry.rend.material;
            if (!entry.hadEmission)
                mat.DisableKeyword("_EMISSION");

            if (mat.HasProperty("_EmissionColor"))
                mat.SetColor("_EmissionColor", entry.originalColor);
        }
        _originalStates.Clear();
    }

    IEnumerator PulseRoutine()
    {
        float t = 0f;
        while (true)
        {
            t += Time.deltaTime * pulseSpeed;
            float intensity = Mathf.Lerp(pulseMin, pulseMax,
                (Mathf.Sin(t) + 1f) * 0.5f);

            Color emissive = highlightColor * intensity;

            foreach (var entry in _originalStates)
            {
                if (entry.rend == null) continue;
                Material mat = entry.rend.material;
                if (mat.HasProperty("_EmissionColor"))
                    mat.SetColor("_EmissionColor", emissive);
            }

            yield return null;
        }
    }
}
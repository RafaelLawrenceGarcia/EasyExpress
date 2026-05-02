// Assets/Script/PC area/Dustsystem.cs  — full replacement
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// DustSystem — Attach to a PC case (same object as PCCaseBuilder).
///
/// CHANGED TO PER-COMPONENT CLEANING:
///   Each InspectableItem that has PartFault.Dusty gets its own dust
///   overlay material instance and its own clean-progress counter.
///   The player must hover the air duster over each dusty part and
///   hold left-click to clean it individually.
///   isDusty becomes false only once every dusty part has been cleaned.
///
/// SETUP (same as before):
///   1. Attach to the PC case prefab alongside PCCaseBuilder.
///   2. Assign dustOverlayMaterial (from Assets/Materials/PC Parts/DustOverlay).
///   3. Optionally assign dustParticlesPrefab.
/// </summary>
public class DustSystem : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────

    [Header("State")]
    public bool isDusty = false;

    [Header("Visuals")]
    public Material dustOverlayMaterial;
    public GameObject dustParticlesPrefab;
    public Color dustTint = new Color(0.6f, 0.5f, 0.35f, 0.3f);

    [Header("Cleaning")]
    [Tooltip("Seconds of holding the air duster on a part to fully clean it.")]
    public float cleanDuration = 2.0f;

    // Legacy field kept so external code that reads cleanProgress still compiles
    [HideInInspector] public float cleanProgress = 0f;

    // ── Per-component data ────────────────────────────────────────────

    private class PartDustData
    {
        public Renderer[] renderers;
        public Material[][] originalMats;
        public Material[] dustInstances;  // one unique Material per Renderer
        public float progress;       // 0-1 clean progress
    }

    private readonly Dictionary<InspectableItem, PartDustData> _parts
        = new Dictionary<InspectableItem, PartDustData>();

    private GameObject _activeParticles;
    private bool _dustApplied = false;

    // ═════════════════════════════════════════════════════════════════
    //  PUBLIC API
    // ═════════════════════════════════════════════════════════════════

    /// <summary>
    /// Apply dust overlays to every InspectableItem that has PartFault.Dusty.
    /// Called by InspectionManager.Inspect() when isDusty is true.
    /// </summary>
    public void ApplyDust()
    {
        if (!isDusty || dustOverlayMaterial == null) return;
        if (_dustApplied) return;

        foreach (InspectableItem item in GetComponentsInChildren<InspectableItem>(true))
        {
            if (item.isMainObject || item.isInventorySlot) continue;
            if (item.fault == PartFault.Dusty)
                ApplyDustToItem(item);
        }

        if (dustParticlesPrefab != null)
        {
            _activeParticles = Instantiate(dustParticlesPrefab, transform);
            _activeParticles.transform.localPosition = Vector3.zero;
        }

        _dustApplied = true;
    }

    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Advance the clean progress on ONE specific part for deltaTime seconds.
    /// Returns true the moment that part becomes fully clean.
    /// Called every frame by InspectionManager while the player holds
    /// the air duster over this particular part.
    /// </summary>
    public bool CleanPartTick(InspectableItem part, float deltaTime)
    {
        if (part == null || part.fault != PartFault.Dusty) return true;

        // Lazy-apply overlay if the part somehow missed ApplyDust
        if (!_parts.ContainsKey(part) && dustOverlayMaterial != null)
            ApplyDustToItem(part);

        if (!_parts.TryGetValue(part, out PartDustData data)) return true;

        data.progress += deltaTime / cleanDuration;
        data.progress = Mathf.Clamp01(data.progress);

        // Fade this part's own material instances — other parts are unaffected
        float alpha = dustTint.a * (1f - data.progress);
        foreach (Material m in data.dustInstances)
        {
            if (m == null) continue;
            Color c = dustTint;
            c.a = alpha;
            m.color = c;
        }

        if (data.progress >= 1f)
        {
            RemovePartDust(part);
            return true;
        }
        return false;
    }

    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the 0-1 clean progress for a specific part.
    /// Used by the tooltip to show a percentage.
    /// </summary>
    public float GetPartProgress(InspectableItem part)
    {
        if (part == null || !_parts.TryGetValue(part, out PartDustData d)) return 0f;
        return d.progress;
    }

    /// <summary>Returns true if this specific part still has dust.</summary>
    public bool IsPartDusty(InspectableItem part)
    {
        return part != null && part.fault == PartFault.Dusty;
    }

    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Instantly remove dust from ONE component.
    /// Restores its original materials, clears its fault,
    /// and checks if the whole PC is now dust-free.
    /// </summary>
    public void RemovePartDust(InspectableItem part)
    {
        if (part == null) return;

        // Restore this part's renderer materials
        if (_parts.TryGetValue(part, out PartDustData data))
        {
            for (int i = 0; i < data.renderers.Length; i++)
            {
                if (data.renderers[i] != null && data.originalMats[i] != null)
                    data.renderers[i].materials = data.originalMats[i];
            }
            // Destroy the material instances we created so we don't leak
            foreach (Material m in data.dustInstances)
                if (m != null) Destroy(m);

            _parts.Remove(part);
        }

        // Clear the Dusty fault on this part
        if (part.fault == PartFault.Dusty)
        {
            part.fault = PartFault.None;
            part.faultDescription = "";
        }

        // Fan overheating that was dust-caused is also resolved
        if (part.partCategory == "Fan" && part.fault == PartFault.Overheating)
        {
            part.fault = PartFault.None;
            part.faultDescription = "";
        }

        Debug.Log($"[DustSystem] '{part.itemName}' cleaned.");
        CheckAllPartsClean();
    }

    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Instantly clean the entire PC (legacy / admin use).
    /// Restores all parts and sets isDusty = false.
    /// </summary>
    public void RemoveDust()
    {
        // Restore all tracked parts
        foreach (var kvp in _parts)
        {
            for (int i = 0; i < kvp.Value.renderers.Length; i++)
            {
                if (kvp.Value.renderers[i] != null && kvp.Value.originalMats[i] != null)
                    kvp.Value.renderers[i].materials = kvp.Value.originalMats[i];
            }
            foreach (Material m in kvp.Value.dustInstances)
                if (m != null) Destroy(m);

            if (kvp.Key != null && kvp.Key.fault == PartFault.Dusty)
            {
                kvp.Key.fault = PartFault.None;
                kvp.Key.faultDescription = "";
            }
        }
        _parts.Clear();

        if (_activeParticles != null) { Destroy(_activeParticles); _activeParticles = null; }

        // Also clear dust-caused Overheating on fans/coolers
        foreach (InspectableItem part in GetComponentsInChildren<InspectableItem>(true))
        {
            if (part.isMainObject || part.isInventorySlot) continue;
            if (part.fault != PartFault.Overheating) continue;

            bool dustCaused = part.partCategory == "Fan"
                || (part.faultDescription != null
                    && part.faultDescription.ToLower().Contains("dust"));
            if (dustCaused) { part.fault = PartFault.None; part.faultDescription = ""; }
        }

        isDusty = false;
        cleanProgress = 0f;
        _dustApplied = false;
        Debug.Log("[DustSystem] PC fully cleaned — all dust removed.");
    }

    /// <summary>Returns true if the PC still needs cleaning.</summary>
    public bool NeedsCleaning() => isDusty;

    /// <summary>
    /// Legacy single-pass CleanTick — cleans every dusty part simultaneously.
    /// Kept so any older callers still compile.
    /// </summary>
    public bool CleanTick(float deltaTime)
    {
        if (!isDusty) return true;
        foreach (InspectableItem item in GetComponentsInChildren<InspectableItem>(true))
        {
            if (item.isMainObject || item.isInventorySlot) continue;
            if (item.fault == PartFault.Dusty) CleanPartTick(item, deltaTime);
        }
        return !isDusty;
    }

    // ═════════════════════════════════════════════════════════════════
    //  PRIVATE HELPERS
    // ═════════════════════════════════════════════════════════════════

    void ApplyDustToItem(InspectableItem item)
    {
        if (_parts.ContainsKey(item)) return;

        Renderer[] renderers = item.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return;

        Material[][] origMats = new Material[renderers.Length][];
        Material[] dustInstances = new Material[renderers.Length];

        for (int i = 0; i < renderers.Length; i++)
        {
            origMats[i] = renderers[i].sharedMaterials;

            // Create a UNIQUE dust material instance for this renderer so
            // fading one part doesn't affect the others
            Material instance = new Material(dustOverlayMaterial);
            Color c = dustTint;
            instance.color = c;
            dustInstances[i] = instance;

            // Append the dust layer on top of the part's existing materials
            Material[] newMats = new Material[origMats[i].Length + 1];
            for (int j = 0; j < origMats[i].Length; j++) newMats[j] = origMats[i][j];
            newMats[newMats.Length - 1] = instance;
            renderers[i].materials = newMats;
        }

        _parts[item] = new PartDustData
        {
            renderers = renderers,
            originalMats = origMats,
            dustInstances = dustInstances,
            progress = 0f
        };
    }

    void CheckAllPartsClean()
    {
        foreach (InspectableItem item in GetComponentsInChildren<InspectableItem>(true))
        {
            if (item.isMainObject || item.isInventorySlot) continue;
            if (item.fault == PartFault.Dusty) return; // still dusty
        }

        // Every part is clean
        if (_activeParticles != null) { Destroy(_activeParticles); _activeParticles = null; }
        isDusty = false;
        _dustApplied = false;
        Debug.Log("[DustSystem] All components are clean — PC is dust-free!");
    }
}
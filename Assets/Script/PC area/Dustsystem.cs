using UnityEngine;
using System.Collections;

/// <summary>
/// DustSystem — Attach this to a PC case (same object as PCCaseBuilder).
/// 
/// HOW IT WORKS:
/// - When isDusty = true, a dust overlay material is applied to all parts
/// - Player must use a compressed air can (DustFilterTool) to clean it
/// - Hold left-click with the tool active → dust particles blow away → clean
/// - Some customer PCs arrive dusty (set in EmailData or CustomerInside)
/// 
/// SETUP:
/// 1. Attach this script to your PC case prefab (alongside PCCaseBuilder)
/// 2. Create a "Dust" material — semi-transparent brownish overlay
/// 3. Assign it to dustOverlayMaterial
/// 4. Optionally assign a dust particle system prefab
/// </summary>
public class DustSystem : MonoBehaviour
{
    [Header("State")]
    public bool isDusty = false;

    [Header("Visuals")]
    public Material dustOverlayMaterial;     // Semi-transparent dust material
    public GameObject dustParticlesPrefab;   // Particle effect for dust clouds
    public Color dustTint = new Color(0.6f, 0.5f, 0.35f, 0.3f); // Brownish tint

    [Header("Cleaning")]
    public float cleanDuration = 2.0f;       // How long to hold to fully clean
    public float cleanProgress = 0f;

    // Runtime
    private GameObject activeParticles;
    private bool isCleaning = false;
    private Material[] originalMaterials;
    private Renderer[] cachedRenderers;
    private Material[][] savedMaterialArrays;
    private bool dustApplied = false;

    /// <summary>
    /// Call this after the PC is built to apply dust if needed.
    /// </summary>
    public void ApplyDust()
    {
        if (!isDusty || dustOverlayMaterial == null) return;
        if (dustApplied) return;

        cachedRenderers = GetComponentsInChildren<Renderer>(true);
        savedMaterialArrays = new Material[cachedRenderers.Length][];

        for (int i = 0; i < cachedRenderers.Length; i++)
        {
            Renderer rend = cachedRenderers[i];
            savedMaterialArrays[i] = rend.sharedMaterials;

            // Add dust material as an extra layer on top
            Material[] newMats = new Material[rend.sharedMaterials.Length + 1];
            for (int j = 0; j < rend.sharedMaterials.Length; j++)
                newMats[j] = rend.sharedMaterials[j];
            newMats[newMats.Length - 1] = dustOverlayMaterial;
            rend.materials = newMats;
        }

        // Spawn dust particles if we have a prefab
        if (dustParticlesPrefab != null)
        {
            activeParticles = Instantiate(dustParticlesPrefab, transform);
            activeParticles.transform.localPosition = Vector3.zero;
        }

        dustApplied = true;
    }

    /// <summary>
    /// Remove all dust visuals. Called when cleaning is complete.
    /// </summary>
    public void RemoveDust()
    {
        isDusty = false;
        cleanProgress = 0f;

        if (cachedRenderers != null && savedMaterialArrays != null)
        {
            for (int i = 0; i < cachedRenderers.Length; i++)
            {
                if (cachedRenderers[i] != null && savedMaterialArrays[i] != null)
                {
                    cachedRenderers[i].materials = savedMaterialArrays[i];
                }
            }
        }

        if (activeParticles != null)
        {
            Destroy(activeParticles);
            activeParticles = null;
        }

        dustApplied = false;
        Debug.Log("PC cleaned! Dust removed.");
    }

    /// <summary>
    /// Called each frame while the player holds the dust can on this PC.
    /// Returns true when fully cleaned.
    /// </summary>
    public bool CleanTick(float deltaTime)
    {
        if (!isDusty) return true;

        cleanProgress += deltaTime / cleanDuration;
        cleanProgress = Mathf.Clamp01(cleanProgress);

        // Visual feedback: reduce dust tint as cleaning progresses
        if (dustOverlayMaterial != null)
        {
            Color fadingDust = dustTint;
            fadingDust.a = dustTint.a * (1f - cleanProgress);
            dustOverlayMaterial.color = fadingDust;
        }

        // Finished cleaning
        if (cleanProgress >= 1f)
        {
            RemoveDust();
            return true;
        }

        return false;
    }

    /// <summary>
    /// Check if this PC needs cleaning before work can begin.
    /// </summary>
    public bool NeedsCleaning()
    {
        return isDusty;
    }
}
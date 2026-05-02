using UnityEngine;

/// <summary>
/// NPCShirtRandomizer — Attach to the Customer NPC prefab.
/// On Awake, picks a random shirt color from the list so
/// every customer looks slightly different.
///
/// SETUP:
///   1. Attach this to the NPC prefab root (same object as CustomerInside)
///   2. Drag the SkinnedMeshRenderer of the character model into "characterRenderer"
///   3. Set "shirtMaterialIndex" to the material slot index of the shirt
///      (check your model's Materials array — usually 0 or 1)
///   4. Drag your shirt Material variants into the "shirtVariants" array
///      (Shirt_Red, Shirt_Blue, Shirt_Green, etc.)
///
/// TIP: To find the correct material index, click on the character model
///      in the scene and look at the SkinnedMeshRenderer → Materials array.
///      The shirt material's position in that array (0-based) is your index.
/// </summary>
public class NPCShirtRandomizer : MonoBehaviour
{
    [Header("Renderer")]
    [Tooltip("The SkinnedMeshRenderer on the character model child.")]
    public SkinnedMeshRenderer characterRenderer;

    [Header("Shirt Setup")]
    [Tooltip("Index of the shirt material in the renderer's Materials array.\n" +
             "Check your model's material slots to find this (usually 0 or 1).")]
    public int shirtMaterialIndex = 1;

    [Tooltip("Drag all your shirt color Material variants here.\n" +
             "A random one is picked each time this NPC spawns.")]
    public Material[] shirtVariants;

    [Header("Fallback — Color Randomizer")]
    [Tooltip("If no material variants are assigned, pick a random color instead.\n" +
             "Useful for quick testing without creating separate materials.")]
    public bool useRandomColorFallback = true;

    [Tooltip("Colors to randomly pick from if no materials are assigned.")]
    public Color[] fallbackColors = new Color[]
    {
        new Color(0.8f, 0.2f, 0.2f), // Red
        new Color(0.2f, 0.4f, 0.8f), // Blue
        new Color(0.2f, 0.7f, 0.3f), // Green
        new Color(0.9f, 0.8f, 0.2f), // Yellow
        new Color(0.6f, 0.2f, 0.7f), // Purple
        new Color(0.2f, 0.2f, 0.2f), // Dark Grey
        new Color(0.9f, 0.9f, 0.9f), // White
        new Color(0.1f, 0.1f, 0.1f), // Black
        new Color(0.9f, 0.5f, 0.2f), // Orange
        new Color(0.4f, 0.7f, 0.7f), // Teal
    };

    void Awake()
    {
        // Auto-find renderer if not assigned
        if (characterRenderer == null)
            characterRenderer = GetComponentInChildren<SkinnedMeshRenderer>();

        if (characterRenderer == null)
        {
            Debug.LogWarning($"[ShirtRandomizer] No SkinnedMeshRenderer found on {name}!");
            return;
        }

        // Validate material index
        if (shirtMaterialIndex < 0 || shirtMaterialIndex >= characterRenderer.materials.Length)
        {
            Debug.LogWarning($"[ShirtRandomizer] Material index {shirtMaterialIndex} out of range " +
                             $"(model has {characterRenderer.materials.Length} materials) on {name}.");
            return;
        }

        // Method 1: Pick from pre-made material variants
        if (shirtVariants != null && shirtVariants.Length > 0)
        {
            Material[] mats = characterRenderer.materials;
            mats[shirtMaterialIndex] = shirtVariants[Random.Range(0, shirtVariants.Length)];
            characterRenderer.materials = mats;
            return;
        }

        // Method 2: Generate a random color on a copy of the existing material
        if (useRandomColorFallback && fallbackColors.Length > 0)
        {
            Material[] mats = characterRenderer.materials;
            // Create an instance so we don't modify the shared material
            mats[shirtMaterialIndex] = new Material(mats[shirtMaterialIndex]);
            Color picked = fallbackColors[Random.Range(0, fallbackColors.Length)];
            mats[shirtMaterialIndex].SetColor("_BaseColor", picked);
            characterRenderer.materials = mats;
        }
    }
}
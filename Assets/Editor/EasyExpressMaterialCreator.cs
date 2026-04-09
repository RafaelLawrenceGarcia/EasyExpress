// Place this file inside:  Assets/Editor/EasyExpressMaterialCreator.cs
// Access via the Unity menu:  EasyExpress ▸ Create PC Part Materials

using UnityEngine;
using UnityEditor;

public static class EasyExpressMaterialCreator
{
    private const string FOLDER_ROOT    = "Assets/Materials";
    private const string FOLDER_PCPARTS = "Assets/Materials/PC Parts";

    // ─────────────────────────────────────────────────────────────────
    //  CREATE ALL AT ONCE
    // ─────────────────────────────────────────────────────────────────

    [MenuItem("EasyExpress/Create PC Part Materials/All Three")]
    static void CreateAll()
    {
        EnsureFolders();
        CreateDustOverlay();
        CreateThermalPasteNew();
        CreateThermalPasteDry();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[EasyExpress] All three PC part materials created in " + FOLDER_PCPARTS);
    }

    // ─────────────────────────────────────────────────────────────────
    //  DUST OVERLAY
    //  Semi-transparent brownish layer that sits on top of all
    //  component meshes to show dust accumulation.
    //  Surface type: Transparent so it can be layered on top.
    // ─────────────────────────────────────────────────────────────────

    [MenuItem("EasyExpress/Create PC Part Materials/Dust Overlay")]
    static void CreateDustOverlay()
    {
        EnsureFolders();

        Material mat = new Material(GetURPLitShader());
        mat.name = "DustOverlay";

        // Brownish, semi-transparent
        mat.SetColor("_BaseColor", new Color(0.55f, 0.45f, 0.30f, 0.38f));
        mat.SetFloat("_Metallic",    0.0f);
        mat.SetFloat("_Smoothness",  0.08f);   // very rough / matte

        // Enable transparency
        mat.SetFloat("_Surface", 1);            // 0=Opaque, 1=Transparent
        mat.SetFloat("_Blend",   0);            // 0=Alpha, 1=Premultiply, 2=Additive, 3=Multiply
        mat.SetFloat("_AlphaClip", 0);
        mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetFloat("_ZWrite",   0);
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.DisableKeyword("_ALPHABLEND_ON");
        mat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

        SaveMaterial(mat, "DustOverlay");
    }

    // ─────────────────────────────────────────────────────────────────
    //  THERMAL PASTE — NEW / FRESH
    //  Silvery-grey, slightly metallic and smooth — looks like a
    //  freshly applied pea-sized dot of compound on the CPU die.
    //  Apply to the CPU or cooler base mesh after fixing the fault.
    // ─────────────────────────────────────────────────────────────────

    [MenuItem("EasyExpress/Create PC Part Materials/Thermal Paste (New)")]
    static void CreateThermalPasteNew()
    {
        EnsureFolders();

        Material mat = new Material(GetURPLitShader());
        mat.name = "ThermalPasteNew";

        // Fresh paste: cool silver-grey
        mat.SetColor("_BaseColor", new Color(0.72f, 0.73f, 0.74f, 1f));
        mat.SetFloat("_Metallic",   0.55f);
        mat.SetFloat("_Smoothness", 0.50f);

        // Opaque
        mat.SetFloat("_Surface",   0);
        mat.SetFloat("_AlphaClip", 0);
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry;

        SaveMaterial(mat, "ThermalPasteNew");
    }

    // ─────────────────────────────────────────────────────────────────
    //  THERMAL PASTE — DRY / DEGRADED
    //  Yellowed, crusty, very dull — indicates the compound has
    //  dried out and is no longer transferring heat properly.
    //  Apply to the CPU or cooler base when PartFault.Overheating
    //  with a "thermal paste" faultDescription is set.
    // ─────────────────────────────────────────────────────────────────

    [MenuItem("EasyExpress/Create PC Part Materials/Thermal Paste (Dry)")]
    static void CreateThermalPasteDry()
    {
        EnsureFolders();

        Material mat = new Material(GetURPLitShader());
        mat.name = "ThermalPasteDry";

        // Dried paste: yellowed brownish-grey, very matte / rough
        mat.SetColor("_BaseColor", new Color(0.66f, 0.58f, 0.40f, 1f));
        mat.SetFloat("_Metallic",   0.02f);
        mat.SetFloat("_Smoothness", 0.04f);   // almost zero — cracked and rough

        // Opaque
        mat.SetFloat("_Surface",   0);
        mat.SetFloat("_AlphaClip", 0);
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry;

        SaveMaterial(mat, "ThermalPasteDry");
    }

    // ─────────────────────────────────────────────────────────────────
    //  HELPERS
    // ─────────────────────────────────────────────────────────────────

    static Shader GetURPLitShader()
    {
        Shader s = Shader.Find("Universal Render Pipeline/Lit");
        if (s == null)
        {
            Debug.LogWarning("[EasyExpress] URP Lit shader not found — falling back to Standard.");
            s = Shader.Find("Standard");
        }
        if (s == null)
            Debug.LogError("[EasyExpress] Could not find any usable shader!");
        return s;
    }

    static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder(FOLDER_ROOT))
            AssetDatabase.CreateFolder("Assets", "Materials");

        if (!AssetDatabase.IsValidFolder(FOLDER_PCPARTS))
            AssetDatabase.CreateFolder(FOLDER_ROOT, "PC Parts");
    }

    static void SaveMaterial(Material mat, string assetName)
    {
        string path = $"{FOLDER_PCPARTS}/{assetName}.mat";

        Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null)
        {
            // Overwrite properties on the existing asset so any references stay valid
            EditorUtility.CopySerialized(mat, existing);
            EditorUtility.SetDirty(existing);
            Debug.Log($"[EasyExpress] Updated existing material: {path}");
        }
        else
        {
            AssetDatabase.CreateAsset(mat, path);
            Debug.Log($"[EasyExpress] Created material: {path}");
        }
    }
}

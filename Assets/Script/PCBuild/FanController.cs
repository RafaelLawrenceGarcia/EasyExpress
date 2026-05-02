using UnityEngine;

public class PCFanController : MonoBehaviour
{
    [Header("Rotation Settings")]
    public float spinSpeed = 1000f;
    public Vector3 spinAxis = new Vector3(0, 0, 1);

    [Header("RGB Lighting Settings")]
    public bool enableRainbowRGB = true;
    public float colorCycleSpeed = 0.5f;

    [Range(0f, 1f)]
    public float glowIntensity = 0.8f; // CLAMPED to 0-1 max

    private Material fanMaterial;
    private Renderer fanRenderer;

    void Start()
    {
        fanRenderer = GetComponentInChildren<Renderer>();
        if (fanRenderer != null)
        {
            fanMaterial = fanRenderer.material;
            fanMaterial.SetColor("_EmissionColor", Color.black);
        }
    }

    void Update()
    {
        // Check if we are currently in inspection mode
        // If yes — kill emission completely to prevent bloom
        if (IsInInspectionMode())
        {
            if (fanMaterial != null)
                fanMaterial.SetColor("_EmissionColor", Color.black);
            
            // Still spin so it looks alive
            Spin();
            return;
        }

        // Check if PC is powered on
        PCPowerSystem power = GetComponentInParent<PCPowerSystem>();
        if (power != null && !power.isPoweredOn)
        {
            if (fanMaterial != null)
                fanMaterial.SetColor("_EmissionColor", Color.black);
            return;
        }

        // Spin
        Spin();

        // RGB Glow — only in gameplay, not inspection
        if (enableRainbowRGB && fanMaterial != null)
        {
            fanMaterial.EnableKeyword("_EMISSION");

            float hue       = Mathf.Repeat(Time.time * colorCycleSpeed, 1f);
            Color neonColor = Color.HSVToRGB(hue, 1f, 1f);

            // glowIntensity is now clamped 0-1 in the inspector
            // Multiply by 2 max so it never goes above 2.0
            fanMaterial.SetColor("_EmissionColor", neonColor * (glowIntensity * 2f));
        }
    }

    void Spin()
    {
        if (fanRenderer == null) return;
        Vector3 worldAxis = transform.TransformDirection(spinAxis);
        transform.RotateAround(
            fanRenderer.bounds.center, worldAxis, spinSpeed * Time.deltaTime);
    }

    bool IsInInspectionMode()
    {
        // InspectionManager.isInspecting is the source of truth
        InspectionManager mgr = InspectionManager_Cache.Instance;
        return mgr != null && mgr.isInspecting;
    }
}

// Lightweight cache so we don't call FindObjectOfType every frame
public static class InspectionManager_Cache
{
    private static InspectionManager _instance;
    public static InspectionManager Instance
    {
        get
        {
            if (_instance == null)
                _instance = Object.FindObjectOfType<InspectionManager>();
            return _instance;
        }
    }
}
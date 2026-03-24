using UnityEngine;

public class PCFanController : MonoBehaviour
{
    [Header("Rotation Settings")]
    public float spinSpeed = 1000f;
    public Vector3 spinAxis = new Vector3(0, 0, 1);

    [Header("RGB Lighting Settings")]
    public bool enableRainbowRGB = true;
    public float colorCycleSpeed = 0.5f;
    public float glowIntensity = 2.5f;

    private Material fanMaterial;
    private Renderer fanRenderer;
    // REMOVED: private float currentHue; (We don't need personal stopwatches anymore!)

    void Start()
    {
        // Save the renderer so we can find the physical center of the model later
        fanRenderer = GetComponentInChildren<Renderer>();
        if (fanRenderer != null)
        {
            fanMaterial = fanRenderer.material;
            fanMaterial.EnableKeyword("_EMISSION");
        }
    }

    void Update()
    {
        // 1. THE AUTOMATIC PIVOT FIX
        // We find the 'bounds.center' (the exact middle of the 3D mesh) and spin around that!
        if (fanRenderer != null)
        {
            // Convert your local axis (0,0,1) into a world direction
            Vector3 worldAxis = transform.TransformDirection(spinAxis);

            // Spin around the visual center, completely ignoring the broken pivot point
            transform.RotateAround(fanRenderer.bounds.center, worldAxis, spinSpeed * Time.deltaTime);
        }

        // 2. Cycle the RGB Rainbow Glow (NOW SYNCED!)
        if (enableRainbowRGB && fanMaterial != null)
        {
            // THE FIX: Use Time.time so every single fan calculates the exact same color at the exact same moment!
            float syncedHue = Mathf.Repeat(Time.time * colorCycleSpeed, 1f);

            Color neonColor = Color.HSVToRGB(syncedHue, 1f, 1f);
            Color finalGlow = neonColor * Mathf.Pow(2, glowIntensity); // Keeps your awesome HDR glow math

            fanMaterial.SetColor("_EmissionColor", finalGlow);
        }
    }
}
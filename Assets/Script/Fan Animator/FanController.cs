// PCFanController.cs — FULL REPLACEMENT
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

    void Start()
    {
        fanRenderer = GetComponentInChildren<Renderer>();
        if (fanRenderer != null)
        {
            fanMaterial = fanRenderer.material;

            // DON'T enable emission here — start dark, let Update handle glow
            fanMaterial.SetColor("_EmissionColor", Color.black);
        }
    }

    void Update()
    {
        // 1. Spin
        if (fanRenderer != null)
        {
            Vector3 worldAxis = transform.TransformDirection(spinAxis);
            transform.RotateAround(fanRenderer.bounds.center, worldAxis, spinSpeed * Time.deltaTime);
        }

        // 2. RGB Glow — only runs when enabled (SetFansState controls this)
        if (enableRainbowRGB && fanMaterial != null)
        {
            fanMaterial.EnableKeyword("_EMISSION");

            float syncedHue = Mathf.Repeat(Time.time * colorCycleSpeed, 1f);
            Color neonColor = Color.HSVToRGB(syncedHue, 1f, 1f);
            Color finalGlow = neonColor * Mathf.Pow(2, glowIntensity);
            fanMaterial.SetColor("_EmissionColor", finalGlow);
        }
    }
}
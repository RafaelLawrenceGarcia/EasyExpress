    using UnityEngine;

public class PCFanController : MonoBehaviour
{
    [Header("Rotation Settings")]
    public float spinSpeed = 1000f;
    public Vector3 spinAxis = new Vector3(1, 0, 0); // Z-axis is usually forward/backward

    [Header("RGB Lighting Settings")]
    public bool enableRainbowRGB = true;
    public float colorCycleSpeed = 0.5f;
    public float glowIntensity = 2.5f;

    private Material fanMaterial;
    private float currentHue;

   void Start()
    {
        // Notice the added "InChildren" here! This tells the invisible pivot to grab the colors from the fan blade attached to it.
        Renderer rend = GetComponentInChildren<Renderer>(); 
        if (rend != null)
        {
            fanMaterial = rend.material; 
            fanMaterial.EnableKeyword("_EMISSION"); 
        }
    }

    void Update()
    {
        // 1. Spin the fan blade smoothly
        transform.Rotate(spinAxis * spinSpeed * Time.deltaTime);

        // 2. Cycle the RGB Rainbow Glow
        if (enableRainbowRGB && fanMaterial != null)
        {
            currentHue += colorCycleSpeed * Time.deltaTime;
            if (currentHue > 1f) currentHue -= 1f;

            Color neonColor = Color.HSVToRGB(currentHue, 1f, 1f);
            Color finalGlow = neonColor * Mathf.Pow(2, glowIntensity);
            
            fanMaterial.SetColor("_EmissionColor", finalGlow);
        }
    }
}
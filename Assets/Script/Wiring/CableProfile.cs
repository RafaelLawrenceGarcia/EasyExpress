using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class CableTypeProfile
{
    public string connectorType;        // must match InspectableItem.connectorType exactly
    public int    strandCount   = 8;
    public float  strandSpacing = 0.012f;
    public float  strandWidth   = 0.008f;

    [Tooltip("One color per strand — loops if fewer entries than strandCount")]
    public Color[] strandColors;
}

/// <summary>
/// Place ONE instance of this anywhere in the scene.
/// Add a CableTypeProfile entry for every connector type you use.
/// </summary>
public class CableProfile : MonoBehaviour
{
    public static CableProfile Instance;

    public List<CableTypeProfile> profiles = new List<CableTypeProfile>()
    {
        new CableTypeProfile
        {
            connectorType  = "24pin",
            strandCount    = 9,
            strandSpacing  = 0.013f,
            strandWidth    = 0.009f,
            strandColors   = new Color[]
            {
                new Color(1f,   1f,   1f),    // white
                new Color(1f,   1f,   0f),    // yellow
                new Color(1f,   0.5f, 0f),    // orange
                new Color(1f,   0f,   0f),    // red
                new Color(0.3f, 0.3f, 0.3f), // dark grey
                new Color(0f,   0f,   0f),    // black
                new Color(0f,   0f,   1f),    // blue
                new Color(0.5f, 0f,   0.5f), // purple
                new Color(0f,   1f,   0f),    // green
            }
        }
        // Add more profiles here for "8pin", "sata", "pcie6pin", etc.
    };

    void Awake() { Instance = this; }

    public CableTypeProfile Get(string connectorType)
    {
        foreach (var p in profiles)
            if (p.connectorType == connectorType) return p;
        return null;
    }
}
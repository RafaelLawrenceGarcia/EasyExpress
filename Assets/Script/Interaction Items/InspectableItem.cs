using UnityEngine;

public class InspectableItem : MonoBehaviour
{
    [Header("Item Info")]
    public string itemName = "Unknown Item";
    [TextArea(3, 5)] // Makes a nice big box in Inspector
    public string itemDescription = "No description available.";
}
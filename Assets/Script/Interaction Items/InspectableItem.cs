using UnityEngine;

public class InspectableItem : MonoBehaviour
{
    [Header("Item Info")]
    public string itemName = "Unknown Item";
    [TextArea(3, 5)]
    public string itemDescription = "No description available.";

    [Header("Interaction Rules")]
    [Tooltip("Check this box ONLY for the outer PC case so you can click it in the shop.")]
    public bool isMainObject = false; 
}
using UnityEngine;

public class SlotData : MonoBehaviour
{
    public bool isOccupied = false; 
    public GameObject currentItem;  

    private MeshRenderer myGraphics; 

    void Start()
    {
        // CHANGED: Look in Children too, just in case the mesh is nested!
        myGraphics = GetComponentInChildren<MeshRenderer>();
        
        if (myGraphics == null)
        {
            Debug.LogError("SlotData Error: I cannot find a MeshRenderer on " + gameObject.name);
        }
    }

    void Update()
    {
        // 1. AUTO-RESET: If the box is gone (picked up), show the green square again!
        if (isOccupied && currentItem == null)
        {
            isOccupied = false;
            if (myGraphics) myGraphics.enabled = true; // SHOW GREEN
        }
    }

    // This function forces the slot to hide
    public void PlaceItemHere(GameObject item)
    {
        isOccupied = true;
        currentItem = item;
        
        // Safety: If we lost the graphics reference, find it again
        if (myGraphics == null) myGraphics = GetComponentInChildren<MeshRenderer>();

        if (myGraphics != null) 
        {
            myGraphics.enabled = false; // HIDE GREEN
        }
    }
}
using UnityEngine;

public class PlacementManager : MonoBehaviour
{
    [Header("Shop System Integration")]
    public ItemData currentPlacementItem; 

    [Header("Settings")]
    public float reachDistance = 2.5f; 

    [Header("Visuals")]
    public GameObject heldItemModel; 
    public GameObject playerBodyModel; 

    [Header("References")]
    public Camera playerCamera;
    public LayerMask placementSurfaceLayer; 
    public OrbitCamera cameraScript; 

    [Header("Prefabs")]
    public GameObject realPCPrefab;      
    public GameObject realBoxPrefab;     

    [Header("State")]
    public bool isHoldingCardboardBox = false;

    private bool canPlace = false;
    private Transform currentSlotTransform; 
    private string currentSlotTag;
    private SlotData currentSlotData; // <--- NEW: Stores the slot's data

    void Update()
    {
        HandleVisuals();

        if (!isHoldingCardboardBox) return;

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, reachDistance, placementSurfaceLayer))
        {
            currentSlotTransform = hit.transform;
            currentSlotTag = hit.collider.tag;
            
            // --- NEW: CHECK IF SLOT IS FULL ---
            currentSlotData = hit.collider.GetComponent<SlotData>();

            // Valid Tag?
            bool isValidTag = (currentSlotTag == "Workstation" || currentSlotTag == "WorkstationSlot" || 
                               currentSlotTag == "Storage" || currentSlotTag == "StorageSlot");

            // Valid AND Not Full?
            if (isValidTag && currentSlotData != null && !currentSlotData.isOccupied)
            {
                canPlace = true;
            }
            else
            {
                // Either wrong tag, or the slot is already full!
                canPlace = false;
            }
        }
        else
        {
            canPlace = false;
        }

        if (canPlace && Input.GetMouseButtonDown(0))
        {
            PerformPlacement();
        }
    }

    void HandleVisuals()
    {
        if (isHoldingCardboardBox)
        {
            if (heldItemModel && !heldItemModel.activeSelf) heldItemModel.SetActive(true);
            if (playerBodyModel && playerBodyModel.activeSelf) playerBodyModel.SetActive(false);
            if (cameraScript) cameraScript.forceFPSMode = true;
        }
        else
        {
            if (heldItemModel && heldItemModel.activeSelf) heldItemModel.SetActive(false);
            if (playerBodyModel && !playerBodyModel.activeSelf) playerBodyModel.SetActive(true);
            if (cameraScript) cameraScript.forceFPSMode = false;
        }
    }

    // ... inside PlacementManager.cs ...

    // ... inside PlacementManager.cs ...

    void PerformPlacement()
    {
        GameObject newItem = null;

        // 1. Check if we are placing a specific Shop Item
        if (currentPlacementItem != null && currentPlacementItem.prefabToPlace != null)
        {
            newItem = Instantiate(currentPlacementItem.prefabToPlace, currentSlotTransform.position, currentSlotTransform.rotation);
            // Optional: tag it based on category
          if (currentPlacementItem.itemType == ItemCategory.PCPart) newItem.tag = "PickupPC";
            else newItem.tag = "Untagged"; 

            // Consumed? If you want items to be one-time use, remove it from inventory here.
        }
        // 2. Fallback to your old logic (The Boxes)
        else if (currentSlotTag == "Workstation" || currentSlotTag == "WorkstationSlot")
        {
            newItem = Instantiate(realPCPrefab, currentSlotTransform.position, currentSlotTransform.rotation);
            newItem.tag = "PickupPC"; 
        }
        else
        {
            newItem = Instantiate(realBoxPrefab, currentSlotTransform.position, currentSlotTransform.rotation);
            newItem.tag = "PickupBox";
        }

        // 3. Occupy Slot (Existing logic)
        if (currentSlotData != null)
        {
            currentSlotData.PlaceItemHere(newItem);
        }

        // 4. Reset
        isHoldingCardboardBox = false; 
        currentPlacementItem = null; // Clear the selection
    }
}
using UnityEngine;

public class PlacementManager : MonoBehaviour
{
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

        // 1. Spawn the Real Object
        if (currentSlotTag == "Workstation" || currentSlotTag == "WorkstationSlot")
        {
            newItem = Instantiate(realPCPrefab, currentSlotTransform.position, currentSlotTransform.rotation);
            newItem.tag = "PickupPC"; 
        }
        else
        {
            newItem = Instantiate(realBoxPrefab, currentSlotTransform.position, currentSlotTransform.rotation);
            newItem.tag = "PickupBox";
        }

        // --- CHECK THIS PART CAREFULLY ---
        // 2. TELL THE SLOT TO HIDE
        if (currentSlotData != null)
        {
            // This line sends the "Turn Off" signal
            currentSlotData.PlaceItemHere(newItem);
        }
        else
        {
            // If you see this in the console, you forgot to add the SlotData script to the cube!
            Debug.LogError("MISSING SCRIPT: The Green Cube does not have the SlotData script!");
        }
        // ---------------------------------

        // 3. Reset Player
        isHoldingCardboardBox = false; 
    }
}
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
    public bool isHoldingItem = false; 

    private bool canPlace = false;
    private Transform currentSlotTransform; 
    private string currentSlotTag;
    private SlotData currentSlotData; 

    void Update()
    {
        HandleVisuals();

        if (!isHoldingItem) return;

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, reachDistance, placementSurfaceLayer))
        {
            currentSlotTransform = hit.transform;
            currentSlotTag = hit.collider.tag;
            
            currentSlotData = hit.collider.GetComponent<SlotData>();

            bool isValidTag = (currentSlotTag == "Workstation" || currentSlotTag == "WorkstationSlot" || 
                               currentSlotTag == "Storage" || currentSlotTag == "StorageSlot");

            if (isValidTag && currentSlotData != null && !currentSlotData.isOccupied)
            {
                canPlace = true;
            }
            else
            {
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
        if (isHoldingItem) 
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

    public void PickUpObject(GameObject pickedUpObj)
    {
        if (isHoldingItem) return;
        
        isHoldingItem = true;
        Destroy(pickedUpObj); 
    }

    void PerformPlacement()
    {
        GameObject newItem = null;

        if (currentPlacementItem != null && currentPlacementItem.prefabToPlace != null)
        {
            newItem = Instantiate(currentPlacementItem.prefabToPlace, currentSlotTransform.position, currentSlotTransform.rotation);
          if (currentPlacementItem.itemType == ItemCategory.PCPart) newItem.tag = "PickupPC";
            else newItem.tag = "Untagged"; 
        }
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

        if (currentSlotData != null)
        {
            currentSlotData.PlaceItemHere(newItem);
        }

        isHoldingItem = false; 
        currentPlacementItem = null;
    }
}
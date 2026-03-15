using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections; 

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

    // Remembers the physical object in your hands
    private GameObject currentlyHeldObject;

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
            // 1. Is the CEO currently talking?
            bool ceoIsTalking = TutorialManager.Instance != null && TutorialManager.Instance.dialogueManager.isDialogueActive;
            
            // 2. Are we clicking on a UI button?
            bool clickingUI = UnityEngine.EventSystems.EventSystem.current != null && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();

            // Only place the box if BOTH of those are false!
            if (!ceoIsTalking && !clickingUI)
            {
                PerformPlacement();
            }
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
        if (TutorialManager.Instance != null && pickedUpObj.CompareTag("PickupBox")) 
            TutorialManager.Instance.CompletePickupBoxTask();
        isHoldingItem = true;
        
        // HIDE the object instead of destroying it so it keeps its data!
        currentlyHeldObject = pickedUpObj;
        currentlyHeldObject.SetActive(false);

        // SAFETY FIX: Detach the box from any weirdly-scaled parents so the stickers don't float!
        currentlyHeldObject.transform.SetParent(null);
        
        // FOOLPROOF SLOT CLEAR: Search all slots to find the one holding this box and empty it
        SlotData[] allSlots = FindObjectsOfType<SlotData>();
        foreach (SlotData slot in allSlots)
        {
            if (slot.currentItem == pickedUpObj)
            {
                slot.isOccupied = false;
                slot.currentItem = null;

                // THE FIX: Force the green shelf's Mesh Renderers to check themselves back on!
                MeshRenderer[] renderers = slot.GetComponentsInChildren<MeshRenderer>(true);
                foreach(MeshRenderer mr in renderers)
                {
                    mr.enabled = true;
                }
            }
        }
    }
    
    void PerformPlacement()
    {
        GameObject itemToPlaceInSlot = null;

        // 1. Placing a store-bought item
        if (currentPlacementItem != null && currentPlacementItem.prefabToPlace != null)
        {
            itemToPlaceInSlot = Instantiate(currentPlacementItem.prefabToPlace, currentSlotTransform.position, currentSlotTransform.rotation);
            if (currentPlacementItem.itemType == ItemCategory.PCPart) itemToPlaceInSlot.tag = "PickupPC";
            else itemToPlaceInSlot.tag = "Untagged"; 
        }
        // 2. Placing the physical object you are holding
        else if (currentlyHeldObject != null)
        {
            currentlyHeldObject.transform.position = currentSlotTransform.position;
            currentlyHeldObject.transform.rotation = currentSlotTransform.rotation;
            currentlyHeldObject.SetActive(true);

            itemToPlaceInSlot = currentlyHeldObject;

            // --- SCENARIO A: IS IT A BOX GOING ON THE WORKSTATION? UNPACK IT! ---
            if (currentlyHeldObject.CompareTag("PickupBox") && (currentSlotTag == "Workstation" || currentSlotTag == "WorkstationSlot"))
            {
                // TRIGGER ADDED HERE
                if (TutorialManager.Instance != null) TutorialManager.Instance.CompletePlaceBoxTask();

                JobBox boxScript = currentlyHeldObject.GetComponent<JobBox>();
                if (boxScript != null)
                {
                    itemToPlaceInSlot = boxScript.UnpackPC(currentSlotTransform);
                }
            }
            // --- SCENARIO B: IS IT A PC GOING ON THE STORAGE SHELF? PACK IT IN A BOX! ---
            else if ((currentlyHeldObject.CompareTag("PickupPC") || currentlyHeldObject.GetComponent<PCCaseBuilder>() != null) && 
                     (currentSlotTag == "Storage" || currentSlotTag == "StorageSlot"))
            {
                if (realBoxPrefab != null)
                {
                    // 1. Spawn a brand new cardboard box on the shelf
                    GameObject newBox = Instantiate(realBoxPrefab, currentSlotTransform.position, currentSlotTransform.rotation);
                    
                    // 2. Tell the box to swallow the PC and hide it!
                    JobBox boxScript = newBox.GetComponent<JobBox>();
                    if (boxScript != null)
                    {
                        boxScript.PackExistingPC(currentlyHeldObject);
                    }

                    // 3. Register the newly spawned cardboard box to the shelf slot, not the naked PC
                    itemToPlaceInSlot = newBox;
                }
                else
                {
                    Debug.LogWarning("You forgot to assign your Cardboard Box Prefab to the 'Real Box Prefab' slot in the PlacementManager!");
                }
            }

            currentlyHeldObject = null;
        }

        // Tell the slot it is full
        if (currentSlotData != null && itemToPlaceInSlot != null)
        {
            currentSlotData.PlaceItemHere(itemToPlaceInSlot);
        }

        isHoldingItem = false; 
        currentPlacementItem = null;
    }
}
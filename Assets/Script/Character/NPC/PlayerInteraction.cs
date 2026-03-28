using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class PlayerInteract : MonoBehaviour
{
    [Header("Settings")]
    public float interactRange = 4f;
    public LayerMask interactLayer;

    [Header("References")]
    public Camera mainCam;
    public PlacementManager placementManager;
    public GTAMovement movementScript;
    public OrbitCamera cameraScript;
    public InspectionManager inspectionManager;
    public DayTransitionManager dayTransitionManager;

    [Header("NEW UI")]
    public InteractionPromptUI interactionPrompt;
    public PCInteractionMenu pcMenu;

    [Header("Legacy UI (keep for dialogue)")]
    public GameObject goldHUD;
    public GameObject dialoguePanel;
    public PCController computerOS;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI dialogueText;

    public Button option1Button;
    public Button option2Button;
    public Button exitButton;

    [Header("Outline Highlight")]
    [Tooltip("Same outline material used by InspectionManager (Custom/OutlineEdge shader)")]
    public Material outlineMaterial;

    // Internal tracking for gameplay outline
    private GameObject currentOutlinedObject = null;
    private System.Collections.Generic.Dictionary<Renderer, Material[]> outlineCache
        = new System.Collections.Generic.Dictionary<Renderer, Material[]>();

    private NPCWalker currentCityNPC;
    private CustomerInside currentShopNPC;
    private bool isInteracting = false;
    private DoorInteractionMenu activeDoorMenu = null;
    private bool menuJustClosed = false;

    private GameObject currentLookTarget = null;
    private InspectableItem storedInspectItem = null;
    private GameObject storedPickupTarget = null;

    void Start()
    {
        if (mainCam == null) mainCam = Camera.main;
        if (placementManager == null) placementManager = FindObjectOfType<PlacementManager>();
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (computerOS != null) computerOS.gameObject.SetActive(false);
        if (goldHUD != null) goldHUD.SetActive(true);
        if (interactionPrompt != null) interactionPrompt.Hide();
        if (pcMenu != null) pcMenu.Hide();
    }

    void Update()
    {
        if (dayTransitionManager != null && dayTransitionManager.IsTransitioning())
        { HideAllPrompts(); return; }

        if (menuJustClosed) { menuJustClosed = false; return; }
        if (inspectionManager != null && inspectionManager.isInspecting) { HideAllPrompts(); return; }
        if (activeDoorMenu != null && activeDoorMenu.IsOpen()) { HideAllPrompts(); return; }

        if (pcMenu != null && pcMenu.IsOpen())
        {
            Ray checkRay = mainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            RaycastHit checkHit;
            bool stillLookingAtPC = false;

            if (Physics.Raycast(checkRay, out checkHit, interactRange, interactLayer))
            {
                InspectableItem checkItem = checkHit.collider.GetComponent<InspectableItem>();
                stillLookingAtPC = checkItem != null
                                && checkItem.isMainObject
                                && checkHit.collider.CompareTag("PickupPC");
            }

            if (!stillLookingAtPC)
                HideAllPrompts(); // ← player walked away, close the menu

            if (interactionPrompt != null) interactionPrompt.Hide();
            return;
        }

        if (computerOS != null && computerOS.gameObject.activeSelf)
        {
            HideAllPrompts();
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                PauseManager.BlockPause = true;
                if (computerOS.HandleEscapeInput())
                {
                    // Close whichever monitor opened it
                    if (activeWorkstationMonitor != null)
                        CloseWorkstationMonitor();
                    else
                        CloseShopComputer();
                }
            }
            return;
        }
        if (isInteracting)
        {
            HideAllPrompts();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }

        Ray ray = mainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactRange, interactLayer))
        {
            NPCWalker cityNPC = hit.collider.GetComponent<NPCWalker>();
            CustomerInside shopCustomer = hit.collider.GetComponent<CustomerInside>();
            ShopTrigger shopPC = hit.collider.GetComponent<ShopTrigger>();
            InspectableItem item = hit.collider.GetComponent<InspectableItem>();

            DoorInteractionMenu doorMenu = hit.collider.GetComponentInParent<DoorInteractionMenu>();
            ShopDoor shopDoor = hit.collider.GetComponentInParent<ShopDoor>();
            SceneDoor sceneDoor = hit.collider.GetComponentInParent<SceneDoor>();

            if (doorMenu == null && shopDoor != null)
                doorMenu = shopDoor.GetComponentInChildren<DoorInteractionMenu>();
            if (doorMenu == null && sceneDoor != null)
                doorMenu = sceneDoor.GetComponentInChildren<DoorInteractionMenu>();
            bool isPickupBox = hit.collider.CompareTag("PickupBox");
            bool isPickupPC = hit.collider.CompareTag("PickupPC");
            bool canInspectInWorld = (item != null && item.isMainObject);
            bool readyShopCustomer = (shopCustomer != null && shopCustomer.isAtSpot);

            // =============================================
            //  DOOR WITH SCROLL MENU (Inside Shop)
            // =============================================
            if (doorMenu != null)
            {
                if (TutorialManager.Instance != null && TutorialManager.Instance.IsTutorialActive())
                {
                    ShowPromptWithOutline("X", "Finish your tasks first!", hit.collider.gameObject);
                    return;
                }

                if (pcMenu != null) pcMenu.Hide();
                ShowPromptWithOutline("E", "Open Door", hit.collider.gameObject);

                if (Input.GetKeyDown(KeyCode.E)) OpenDoorMenu(doorMenu);
            }
            // =============================================
            //  RAW SCENE DOOR (Outside City)
            // =============================================
            else if (sceneDoor != null)
            {
                if (pcMenu != null) pcMenu.Hide();
                ShowPromptWithOutline("E", "Open Door", hit.collider.gameObject);

                if (Input.GetKeyDown(KeyCode.E))
                {
                    DoorInteractionMenu childMenu = sceneDoor.GetComponentInChildren<DoorInteractionMenu>();
                    if (childMenu != null)
                    {
                        OpenDoorMenu(childMenu);
                    }
                    else
                    {
                        sceneDoor.EnterDoor();
                    }
                }
            }
            // =============================================
            //  RAW SHOP DOOR (Alternative teleport)
            // =============================================
            else if (shopDoor != null)
            {
                if (pcMenu != null) pcMenu.Hide();
                ShowPromptWithOutline("E", "Enter Room", hit.collider.gameObject);

                if (Input.GetKeyDown(KeyCode.E)) shopDoor.EnterShop(transform.root.gameObject);
            }
            // =============================================
            //  PC ON WORKSTATION
            // =============================================
            else if (canInspectInWorld && isPickupPC)
            {
                // Block during tutorial until step 9 (the inspect PC step)
                if (TutorialManager.Instance != null && TutorialManager.Instance.IsTutorialActive()
                    && TutorialManager.Instance.GetCurrentStep() < 9)
                {
                    ShowPromptWithOutline("X", "Finish your tasks first!", hit.collider.gameObject);
                    return;
                }

                if (interactionPrompt != null) interactionPrompt.Hide();

                if (pcMenu != null && !pcMenu.IsOpen())
                {
                    storedInspectItem = item;
                    storedPickupTarget = hit.collider.gameObject;

                    ApplyOutline(hit.collider.gameObject);
                    pcMenu.Show((int choice) =>
                    {
                        if (choice == 0 && storedInspectItem != null)
                        {
                            pcMenu.Hide();
                            ClearOutline(); // Clear the green outline before entering Inspect Mode!
                            if (TutorialManager.Instance != null) TutorialManager.Instance.CompletePCTask();
                            if (inspectionManager != null) inspectionManager.Inspect(storedInspectItem);
                        }
                        else if (choice == 1 && storedPickupTarget != null)
                        {
                            pcMenu.Hide();
                            PickUpItem(storedPickupTarget);
                        }

                        storedInspectItem = null;
                        storedPickupTarget = null;
                    });
                }
            }
            // =============================================
            //  INSPECTABLE ITEM
            // =============================================
            else if (canInspectInWorld)
            {
                if (pcMenu != null) pcMenu.Hide();
                ShowPromptWithOutline("E", "Inspect PC", hit.collider.gameObject);

                if (Input.GetKeyDown(KeyCode.E))
                {
                    HideAllPrompts(); // Clear the green outline before entering Inspect Mode!
                    if (TutorialManager.Instance != null) TutorialManager.Instance.CompletePCTask();
                    if (inspectionManager != null) inspectionManager.Inspect(item);
                }
            }
            // =============================================
            //  CITY NPC
            // =============================================
            else if (cityNPC)
            {
                if (pcMenu != null) pcMenu.Hide();
                ShowPromptWithOutline("E", "Talk to Citizen", hit.collider.gameObject);

                if (Input.GetKeyDown(KeyCode.E)) StartCityInteraction(cityNPC);
            }
            // =============================================
            //  SHOP CUSTOMER
            // =============================================
            else if (readyShopCustomer)
            {
                if (pcMenu != null) pcMenu.Hide();
                ShowPromptWithOutline("E", "Talk to Customer", hit.collider.gameObject);

                if (Input.GetKeyDown(KeyCode.E)) StartShopInteraction(shopCustomer);
            }
            else if (hit.collider.CompareTag("WorkstationMonitor"))
            {
                // Block during tutorial
                if (TutorialManager.Instance != null && TutorialManager.Instance.IsTutorialActive())
                {
                    ShowPromptWithOutline("X", "Finish your tasks first!", hit.collider.gameObject);
                    return;
                }

                WorkstationMonitor monitor = hit.collider.GetComponent<WorkstationMonitor>();
                if (monitor == null) monitor = hit.collider.GetComponentInParent<WorkstationMonitor>();

                if (monitor != null)
                {
                    if (pcMenu != null) pcMenu.Hide();

                    if (monitor.CanUseMonitor())
                    {
                        ShowPromptWithOutline("E", "Use Monitor", hit.collider.gameObject);

                        if (Input.GetKeyDown(KeyCode.E))
                        {
                            HideAllPrompts();
                            OpenWorkstationMonitor(monitor);
                        }
                    }
                    else
                    {
                        // FIX 2: Changed GetBlockedReason() to GetMonitorStatus()
                        string reason = monitor.GetMonitorStatus();
                        ShowPromptWithOutline("X", reason, hit.collider.gameObject);
                    }
                }
            }
            // =============================================
            //  SHOP COMPUTER
            // =============================================
            else if (shopPC)
            {
                // Block during tutorial
                if (TutorialManager.Instance != null && TutorialManager.Instance.IsTutorialActive())
                {
                    ShowPromptWithOutline("X", "Finish your tasks first!", hit.collider.gameObject);
                    return;
                }

                if (pcMenu != null) pcMenu.Hide();
                ShowPromptWithOutline("E", "Use Computer", hit.collider.gameObject);

                if (Input.GetKeyDown(KeyCode.E)) OpenShopComputer();
            }
            // =============================================
            //  PICKUP BOX & LOOSE COMPONENTS
            // =============================================
            else if (isPickupBox)
            {
                if (pcMenu != null) pcMenu.Hide();
                ShowPromptWithOutline("E / Q", "E: Inventory | Q: Carry", hit.collider.gameObject);

                if (Input.GetKeyDown(KeyCode.E))
                {
                    DeliveryBox dBox = hit.collider.GetComponentInParent<DeliveryBox>();

                    if (dBox != null)
                    {
                        dBox.InteractUnpack();
                        HideAllPrompts();
                    }
                    else
                    {
                        InspectableItem looseItem = hit.collider.GetComponentInParent<InspectableItem>();
                        if (looseItem != null)
                        {
                            InventorySystem.Instance.AddPart(hit.collider.gameObject, looseItem);
                            HideAllPrompts();
                        }
                    }
                }
                else if (Input.GetKeyDown(KeyCode.Q))
                {
                    PickUpItem(hit.collider.gameObject);
                }
            }
            // =============================================
            //  PICKUP PC
            // =============================================
            else if (isPickupPC)
            {
                if (pcMenu != null) pcMenu.Hide();
                ShowPromptWithOutline("Q", "Pick Up PC", hit.collider.gameObject);

                if (Input.GetKeyDown(KeyCode.Q)) PickUpItem(hit.collider.gameObject);
            }
            else { HideAllPrompts(); }
        }
        else { HideAllPrompts(); currentLookTarget = null; }
    }

    void ShowPromptWithOutline(string key, string action, GameObject target)
    {
        if (interactionPrompt != null) interactionPrompt.Show(key, action);
        ApplyOutline(target);
    }

    void HideAllPrompts()
    {
        if (interactionPrompt != null) interactionPrompt.Hide();
        if (pcMenu != null) pcMenu.Hide();
        ClearOutline();
    }

    void OpenDoorMenu(DoorInteractionMenu doorMenu)
    {
        activeDoorMenu = doorMenu;
        FreezePlayer(true);
        HideAllPrompts();
        doorMenu.OpenMenu();
    }

    public void ForceCloseAllInteraction()
    {
        activeDoorMenu = null;
        menuJustClosed = true;
        FreezePlayer(false);
    }

    void PickUpItem(GameObject itemObj)
    {
        if (placementManager != null && !placementManager.isHoldingItem)
        {
            DeliveryBox deliveryScript = itemObj.GetComponentInParent<DeliveryBox>();
            if (deliveryScript != null) itemObj = deliveryScript.gameObject;

            JobBox boxScript = itemObj.GetComponentInParent<JobBox>();
            if (boxScript != null) itemObj = boxScript.gameObject;

            PCCaseBuilder pcScript = itemObj.GetComponentInParent<PCCaseBuilder>();
            if (pcScript != null) itemObj = pcScript.gameObject;

            placementManager.PickUpObject(itemObj);
            HideAllPrompts();
        }
    }

    void StartCityInteraction(NPCWalker npc)
    {
        isInteracting = true;
        currentCityNPC = npc;
        currentShopNPC = null;

        FreezePlayer(true);
        HideAllPrompts();
        dialoguePanel.SetActive(true);
        if (computerOS != null) computerOS.gameObject.SetActive(false);

        option1Button.interactable = true;
        option2Button.interactable = true;

        var btn1Label = option1Button.GetComponentInChildren<TextMeshProUGUI>();
        var btn2Label = option2Button.GetComponentInChildren<TextMeshProUGUI>();
        if (btn1Label != null) btn1Label.text = "Wanna buy a Pc?";
        if (btn2Label != null) btn2Label.text = npc.option2Response;

        nameText.text = npc.npcName;
        dialogueText.text = npc.greeting;
        npc.StartConversation();
    }

    void StartShopInteraction(CustomerInside npc)
    {
        isInteracting = true;
        currentShopNPC = npc;
        currentCityNPC = null;

        FreezePlayer(true);
        HideAllPrompts();
        dialoguePanel.SetActive(true);
        if (computerOS != null) computerOS.gameObject.SetActive(false);

        option1Button.interactable = true;
        option2Button.interactable = true;

        var btn1Label = option1Button.GetComponentInChildren<TextMeshProUGUI>();
        var btn2Label = option2Button.GetComponentInChildren<TextMeshProUGUI>();
        if (btn1Label != null) btn1Label.text = "Accept Job";
        if (btn2Label != null) btn2Label.text = "Decline";

        nameText.text = npc.npcName;
        dialogueText.text = npc.jobRequest;
        npc.StartShopConversation();
    }

    void FreezePlayer(bool freeze)
    {
        if (movementScript != null) movementScript.SetMovementState(!freeze);
        if (cameraScript != null) cameraScript.SetCameraState(!freeze);

        if (freeze) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
        else { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
    }

    public void OnOption1Click()
    {
        if (currentCityNPC != null) dialogueText.text = currentCityNPC.TryInviteToShop();
        else if (currentShopNPC != null)
        {
            dialogueText.text = "Deal! I'll take a look.";
            currentShopNPC.AcceptJob();
            option1Button.interactable = false;
            option2Button.interactable = false;
            StartCoroutine(CloseAfterDelay(true));
        }
    }

    public void OnOption2Click()
    {
        if (currentCityNPC != null) dialogueText.text = currentCityNPC.option2Response;
        else if (currentShopNPC != null)
        {
            dialogueText.text = "Sorry, I can't help you.";
            currentShopNPC.RejectJob();
            option1Button.interactable = false;
            option2Button.interactable = false;
            StartCoroutine(CloseAfterDelay(false));
        }
    }

    public void OnExitClick()
    {
        isInteracting = false;
        dialoguePanel.SetActive(false);
        FreezePlayer(false);

        if (currentCityNPC != null) currentCityNPC.EndConversation();
        if (currentShopNPC != null) currentShopNPC.EndShopConversation();
    }

    IEnumerator CloseAfterDelay(bool acceptedJob)
    {
        yield return new WaitForSeconds(1.5f);
        OnExitClick();

        if (acceptedJob && TutorialManager.Instance != null)
            TutorialManager.Instance.CompleteCustomerTask();
    }

    void OpenShopComputer()
    {
        isInteracting = true;
        if (computerOS != null) { computerOS.gameObject.SetActive(true); computerOS.ShowDesktop(); }
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        HideAllPrompts();
        if (goldHUD != null) goldHUD.SetActive(false);
        FreezePlayer(true);
        if (TutorialManager.Instance != null) TutorialManager.Instance.HideTaskTemporarily();
    }

    public void CloseShopComputer()
    {
        isInteracting = false;
        if (computerOS != null) computerOS.gameObject.SetActive(false);
        if (goldHUD != null) goldHUD.SetActive(true);
        FreezePlayer(false);
        if (TutorialManager.Instance != null) TutorialManager.Instance.RestoreTaskIfNeeded();
    }
    // =============================================
    //  WORKSTATION MONITOR (NEW)
    // =============================================
    private WorkstationMonitor activeWorkstationMonitor = null;

    void OpenWorkstationMonitor(WorkstationMonitor monitor)
    {
        activeWorkstationMonitor = monitor;
        isInteracting = true;

        if (computerOS != null)
        {
            computerOS.gameObject.SetActive(true);

            // FIX 3: Use a new GetLinkedPC() method (we will add this next)
            PCPowerSystem pc = monitor.GetLinkedPC();

            // FIX 4: Use GetCriticalFaultReason() instead of HasAnyFault()
            if (pc != null && !string.IsNullOrEmpty(pc.GetCriticalFaultReason()))
            {
                // PC has problems — boot to BIOS or show error
                computerOS.RestartToBIOS();
            }
            else
            {
                // PC is healthy — boot normally
                computerOS.BootToOS();
            }
        }

        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        HideAllPrompts();
        if (goldHUD != null) goldHUD.SetActive(false);
        FreezePlayer(true);
    }

    public void CloseWorkstationMonitor()
    {
        activeWorkstationMonitor = null;
        isInteracting = false;
        if (computerOS != null) computerOS.gameObject.SetActive(false);
        if (goldHUD != null) goldHUD.SetActive(true);
        FreezePlayer(false);
    }

    void ApplyOutline(GameObject obj)
    {
        if (obj == currentOutlinedObject) return;

        ClearOutline();

        if (outlineMaterial == null || obj == null) return;

        currentOutlinedObject = obj;

        GameObject outlineTarget = obj;

        Transform parentCheck = obj.transform.parent;
        if (parentCheck != null)
        {
            if (parentCheck.CompareTag("PickupBox") || parentCheck.CompareTag("PickupPC"))
                outlineTarget = parentCheck.gameObject;

            JobBox jb = obj.GetComponentInParent<JobBox>();
            DeliveryBox db = obj.GetComponentInParent<DeliveryBox>();
            PCCaseBuilder pcb = obj.GetComponentInParent<PCCaseBuilder>();

            if (jb != null) outlineTarget = jb.gameObject;
            else if (db != null) outlineTarget = db.gameObject;
            else if (pcb != null) outlineTarget = pcb.gameObject;
        }

        foreach (Renderer rend in outlineTarget.GetComponentsInChildren<Renderer>())
        {
            if (rend == null) continue;

            if (!outlineCache.ContainsKey(rend))
                outlineCache.Add(rend, rend.sharedMaterials);

            Material[] currentMats = rend.sharedMaterials;
            Material[] newMats = new Material[currentMats.Length + 1];
            for (int i = 0; i < currentMats.Length; i++)
                newMats[i] = currentMats[i];
            newMats[newMats.Length - 1] = outlineMaterial;
            rend.sharedMaterials = newMats;
        }
    }

    void ClearOutline()
    {
        foreach (var kv in outlineCache)
        {
            if (kv.Key != null)
                kv.Key.sharedMaterials = kv.Value;
        }
        outlineCache.Clear();
        currentOutlinedObject = null;
    }
}
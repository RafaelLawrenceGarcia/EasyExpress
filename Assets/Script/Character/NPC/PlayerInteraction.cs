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

    [Header("Computer Canvas Transition")]
    public Canvas computerCanvas;
    private RenderMode originalRenderMode;
    private Camera originalWorldCamera;

    public Button option1Button;
    public Button option2Button;
    public Button exitButton;

    [Header("Highlight Overlay")]
    [Tooltip("Material using Custom/HighlightOverlay shader (semi-transparent grey)")]
    public Material highlightMaterial;

    // Internal tracking for gameplay highlight
    private GameObject currentHighlightedObject = null;
    private System.Collections.Generic.Dictionary<Renderer, Material[]> highlightCache
        = new System.Collections.Generic.Dictionary<Renderer, Material[]>();

    private NPCWalker currentCityNPC;
    private CustomerInside currentShopNPC;
    private bool isInteracting = false;
    private bool isUsingComputer = false;
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

        // Save the main shop 3D monitor settings
        if (computerCanvas != null)
        {
            originalRenderMode = computerCanvas.renderMode;
            originalWorldCamera = computerCanvas.worldCamera;
        }
    }

    void Update()
    {
        if (dayTransitionManager != null && dayTransitionManager.IsTransitioning())
        { HideAllPrompts(); return; }

        if (menuJustClosed) { menuJustClosed = false; return; }
        PowerCordManager cordManager = GetComponent<PowerCordManager>();
        if (cordManager != null && cordManager.IsCarrying()) { HideAllPrompts(); return; }
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

        if (isUsingComputer)
        {
            HideAllPrompts();
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                PauseManager.BlockPause = true;

                // Dynamically find WHICH computer OS we are currently looking at
                PCController currentOS = computerOS; // Default to main shop PC
                if (activeWorkstationMonitor != null && activeWorkstationMonitor.localOS != null)
                    currentOS = activeWorkstationMonitor.localOS;

                if (currentOS != null && currentOS.HandleEscapeInput())
                {
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
                // ── Tutorial: block door EXCEPT during End Day step (step 18) ──
                bool tutActive  = TutorialManager.Instance != null && TutorialManager.Instance.IsTutorialActive();
                bool endDayStep = TutorialManager.Instance != null && TutorialManager.Instance.IsEndDayStep();

                if (tutActive && !endDayStep)
                {
                    ShowPromptWithHighlight("X", "Finish your tasks first!", hit.collider.gameObject);
                    return;
                }

                if (pcMenu != null) pcMenu.Hide();
                ShowPromptWithHighlight("E", "Open Door", hit.collider.gameObject);

                if (Input.GetKeyDown(KeyCode.E)) OpenDoorMenu(doorMenu);
            }
            // =============================================
            //  RAW SCENE DOOR (Outside City)
            // =============================================
            else if (sceneDoor != null)
            {
                if (pcMenu != null) pcMenu.Hide();
                ShowPromptWithHighlight("E", "Open Door", hit.collider.gameObject);

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
                ShowPromptWithHighlight("E", "Enter Room", hit.collider.gameObject);

                if (Input.GetKeyDown(KeyCode.E)) shopDoor.EnterShop(transform.root.gameObject);
            }
            // =============================================
            //  PC ON WORKSTATION
            // =============================================
            else if (canInspectInWorld && isPickupPC)
            {
                // Block during tutorial until the Inspect step (step 11)
                bool pcBlocked = TutorialManager.Instance != null
                && TutorialManager.Instance.IsTutorialActive()
                && !TutorialManager.Instance.IsInspectPCAllowed();

                if (pcBlocked)
                {
                    ShowPromptWithHighlight("X", "Finish your tasks first!", hit.collider.gameObject);
                    return;
                }

                if (interactionPrompt != null) interactionPrompt.Hide();

                if (pcMenu != null && !pcMenu.IsOpen())
                {
                    storedInspectItem = item;
                    storedPickupTarget = hit.collider.gameObject;

                    ApplyHighlight(hit.collider.gameObject);
                    pcMenu.Show((int choice) =>
                    {
                        if (choice == 0 && storedInspectItem != null)
                        {
                            pcMenu.Hide();
                            ClearHighlight();
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
                ShowPromptWithHighlight("E", "Inspect PC", hit.collider.gameObject);

                if (Input.GetKeyDown(KeyCode.E))
                {
                    HideAllPrompts();
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
                ShowPromptWithHighlight("E", "Talk to Citizen", hit.collider.gameObject);

                if (Input.GetKeyDown(KeyCode.E)) StartCityInteraction(cityNPC);
            }
            // =============================================
            //  SHOP CUSTOMER
            // =============================================
            else if (readyShopCustomer)
            {
                if (pcMenu != null) pcMenu.Hide();
                ShowPromptWithHighlight("E", "Talk to Customer", hit.collider.gameObject);

                if (Input.GetKeyDown(KeyCode.E)) StartShopInteraction(shopCustomer);
            }
            // =============================================
            //  WORKSTATION MONITOR  (Email / OS)
            // =============================================
            else if (hit.collider.CompareTag("WorkstationMonitor"))
            {
                // ── Tutorial: block monitor EXCEPT during Email step (step 32) ──
                bool tutActive = TutorialManager.Instance != null && TutorialManager.Instance.IsTutorialActive();
                bool emailStep = TutorialManager.Instance != null && TutorialManager.Instance.IsEmailStep();

                if (tutActive && !emailStep)
                {
                    ShowPromptWithHighlight("X", "Finish your tasks first!", hit.collider.gameObject);
                    return;
                }

                WorkstationMonitor monitor = hit.collider.GetComponent<WorkstationMonitor>();
                if (monitor == null) monitor = hit.collider.GetComponentInParent<WorkstationMonitor>();

                if (monitor != null)
                {
                    if (pcMenu != null) pcMenu.Hide();

                    if (monitor.CanInteract())
                    {
                        ShowPromptWithHighlight("E", "Use Monitor", hit.collider.gameObject);

                        if (Input.GetKeyDown(KeyCode.E))
                        {
                            HideAllPrompts();
                            // ── Tutorial hook: notify email step ──
                            if (TutorialManager.Instance != null) TutorialManager.Instance.CompleteEmailTask();
                            OpenWorkstationMonitor(monitor);
                        }
                    }
                    else
                    {
                        string reason = monitor.GetBlockedReason();
                        ShowPromptWithHighlight("X", reason, hit.collider.gameObject);
                    }
                }
            }
            // =============================================
            //  SHOP COMPUTER  (Main Desk / Cashier PC)
            // =============================================
            else if (shopPC)
            {
                // ── Tutorial: evaluate which steps are allowed ──
                bool tutActive    = TutorialManager.Instance != null && TutorialManager.Instance.IsTutorialActive();
                bool cashierStep  = TutorialManager.Instance != null && TutorialManager.Instance.IsCashierPCStep();
                bool installStep  = TutorialManager.Instance != null && TutorialManager.Instance.IsInstallComponentStep();

                // Block when tutorial is running but it's not one of the two allowed steps
                if (tutActive && !cashierStep && !installStep)
                {
                    ShowPromptWithHighlight("X", "Finish your tasks first!", hit.collider.gameObject);
                    return;
                }

                // ── Step 5: accept the customer job via the cashier PC ──
                if (cashierStep)
                {
                    if (pcMenu != null) pcMenu.Hide();
                    ShowPromptWithHighlight("E", "Check Customer Request", hit.collider.gameObject);

                    if (Input.GetKeyDown(KeyCode.E))
                        TutorialManager.Instance.ForceAcceptCurrentCustomer();

                    return; // Don't fall through to normal OpenShopComputer
                }

                // ── Normal flow (also used at step 24 — install component) ──
                if (pcMenu != null) pcMenu.Hide();
                ShowPromptWithHighlight("E", "Use Computer", hit.collider.gameObject);

                if (Input.GetKeyDown(KeyCode.E)) OpenShopComputer();
            }
            // =============================================
            //  PICKUP BOX & LOOSE COMPONENTS
            // =============================================
            else if (isPickupBox)
            {
                if (pcMenu != null) pcMenu.Hide();
                ShowPromptWithHighlight("E / Q", "E: Inventory | Q: Carry", hit.collider.gameObject);

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
                ShowPromptWithHighlight("Q", "Pick Up PC", hit.collider.gameObject);

                if (Input.GetKeyDown(KeyCode.Q)) PickUpItem(hit.collider.gameObject);
            }
            else { HideAllPrompts(); }
        }
        else { HideAllPrompts(); currentLookTarget = null; }
    }

    void ShowPromptWithHighlight(string key, string action, GameObject target)
    {
        if (interactionPrompt != null) interactionPrompt.Show(key, action);
        ApplyHighlight(target);
    }

    void HideAllPrompts()
    {
        if (interactionPrompt != null) interactionPrompt.Hide();
        if (pcMenu != null) pcMenu.Hide();
        ClearHighlight();
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
            // ── Check for delivery box BEFORE swapping the reference ──
            DeliveryBox deliveryScript = itemObj.GetComponentInParent<DeliveryBox>();
            if (deliveryScript != null)
            {
                itemObj = deliveryScript.gameObject;
                // Tutorial hook: step 20 — picking up the delivery box
                if (TutorialManager.Instance != null)
                    TutorialManager.Instance.CompletePickupDeliveryTask();
            }

            JobBox boxScript = itemObj.GetComponentInParent<JobBox>();
            if (boxScript != null) itemObj = boxScript.gameObject;

            PCCaseBuilder pcScript = itemObj.GetComponentInParent<PCCaseBuilder>();
            if (pcScript != null) itemObj = pcScript.gameObject;

            placementManager.PickUpObject(itemObj);
            HideAllPrompts();

            // Tutorial hook: step 7 — picking up the customer's job box (PickupBox tag)
            if (itemObj.CompareTag("PickupBox") && TutorialManager.Instance != null)
                TutorialManager.Instance.CompletePickupBoxTask();
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

        // Legacy path (outside tutorial): notify if job was accepted
        if (acceptedJob && TutorialManager.Instance != null)
            TutorialManager.Instance.CompleteCustomerTask();
    }

    // =============================================
    //  MAIN SHOP COMPUTER
    // =============================================
    void OpenShopComputer()
    {
        isInteracting = true;
        isUsingComputer = true;

        if (computerOS != null)
        {
            computerOS.gameObject.SetActive(true);
            computerOS.ShowDesktop();
        }

        // Added Full-Screen Snap for Main Desk Computer too!
        if (computerCanvas != null)
        {
            originalRenderMode = computerCanvas.renderMode;
            originalWorldCamera = computerCanvas.worldCamera;
            computerCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }

        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        HideAllPrompts();
        if (goldHUD != null) goldHUD.SetActive(false);
        FreezePlayer(true);
        if (TutorialManager.Instance != null) TutorialManager.Instance.HideTaskTemporarily();
    }

    public void CloseShopComputer()
    {
        isUsingComputer = false;
        isInteracting = false;

        // Put the Main Desk Computer Canvas back
        if (computerCanvas != null)
        {
            computerCanvas.renderMode = originalRenderMode;
            computerCanvas.worldCamera = originalWorldCamera;
        }

        if (computerOS != null) computerOS.gameObject.SetActive(false);
        if (goldHUD != null) goldHUD.SetActive(true);
        FreezePlayer(false);
        if (TutorialManager.Instance != null) TutorialManager.Instance.RestoreTaskIfNeeded();
    }

    // =============================================
    //  PREFAB WORKSTATION MONITOR 
    // =============================================
    private WorkstationMonitor activeWorkstationMonitor = null;

    void OpenWorkstationMonitor(WorkstationMonitor monitor)
    {
        activeWorkstationMonitor = monitor;
        isInteracting = true;
        isUsingComputer = true;

        Canvas activeCanvas = monitor.localCanvas;
        PCController activeOS = monitor.localOS;

        if (activeOS != null && activeCanvas != null)
        {
            originalRenderMode = activeCanvas.renderMode;
            originalWorldCamera = activeCanvas.worldCamera;

            activeOS.gameObject.SetActive(true);
            activeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

            // Enable raycaster so fullscreen UI is clickable
            GraphicRaycaster raycaster = activeCanvas.GetComponent<GraphicRaycaster>();
            if (raycaster != null) raycaster.enabled = true;

            if (monitor.uiBridge != null)
            {
                monitor.uiBridge.ActivateForShop();
                monitor.uiBridge.ActivateForEmail();
            }

            PCPowerSystem pc = monitor.GetLinkedPC();
            if (pc != null && pc.HasAnyFault())
                activeOS.RestartToBIOS();
            else
                activeOS.BootToOS();
        }

        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        HideAllPrompts();
        if (goldHUD != null) goldHUD.SetActive(false);
        FreezePlayer(true);
    }

    public void CloseWorkstationMonitor()
    {
        if (activeWorkstationMonitor != null)
        {
            Canvas activeCanvas = activeWorkstationMonitor.localCanvas;
            if (activeCanvas != null)
            {
                activeCanvas.renderMode = originalRenderMode;
                activeCanvas.worldCamera = originalWorldCamera;

                // Disable raycaster so world-space canvas doesn't steal clicks
                GraphicRaycaster raycaster = activeCanvas.GetComponent<GraphicRaycaster>();
                if (raycaster != null) raycaster.enabled = false;
            }
        }

        activeWorkstationMonitor = null;
        isInteracting = false;
        isUsingComputer = false;

        PauseManager.BlockPause = false;
        if (goldHUD != null) goldHUD.SetActive(true);
        FreezePlayer(false);
        if (TutorialManager.Instance != null) TutorialManager.Instance.RestoreTaskIfNeeded();
    }

    // =============================================
    //  HIGHLIGHT OVERLAY (semi-transparent fill)
    // =============================================

    void ApplyHighlight(GameObject obj)
    {
        if (obj == currentHighlightedObject) return;
        ClearHighlight();
        if (highlightMaterial == null || obj == null) return;

        currentHighlightedObject = obj;
        GameObject highlightTarget = obj;

        Transform parentCheck = obj.transform.parent;
        if (parentCheck != null)
        {
            if (parentCheck.CompareTag("PickupBox") || parentCheck.CompareTag("PickupPC"))
                highlightTarget = parentCheck.gameObject;

            JobBox jb = obj.GetComponentInParent<JobBox>();
            DeliveryBox db = obj.GetComponentInParent<DeliveryBox>();
            PCCaseBuilder pcb = obj.GetComponentInParent<PCCaseBuilder>();

            if (jb != null) highlightTarget = jb.gameObject;
            else if (db != null) highlightTarget = db.gameObject;
            else if (pcb != null) highlightTarget = pcb.gameObject;
        }

        foreach (Renderer rend in highlightTarget.GetComponentsInChildren<Renderer>())
        {
            if (rend == null) continue;

            if (!highlightCache.ContainsKey(rend))
                highlightCache.Add(rend, rend.sharedMaterials);

            Material[] currentMats = rend.sharedMaterials;
            Material[] newMats = new Material[currentMats.Length + 1];
            for (int i = 0; i < currentMats.Length; i++)
                newMats[i] = currentMats[i];
            newMats[newMats.Length - 1] = highlightMaterial;
            rend.sharedMaterials = newMats;
        }
    }

    void ClearHighlight()
    {
        foreach (var kv in highlightCache)
        {
            if (kv.Key != null)
                kv.Key.sharedMaterials = kv.Value;
        }
        highlightCache.Clear();
        currentHighlightedObject = null;
    }
}
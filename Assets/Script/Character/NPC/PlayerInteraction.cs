using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class PlayerInteract : MonoBehaviour
{
    [Header("Storage")]
    public float storageDetectRange = 3f;

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
    public InteractionPromptUI interactionPrompt; // optional, legacy
    [Header("Press E Prompt (assign a TMP Text in Player HUD)")]
    public TextMeshProUGUI pressEText;            // drag any TMP Text here
    public PCInteractionMenu pcMenu;

    [Header("Legacy UI (keep for dialogue)")]
    public GameObject goldHUD;
    public GameObject dialoguePanel;
    public PCController computerOS;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI dialogueText;
    public Image characterPicture;

    [Header("Computer Canvas Transition")]
    public Canvas computerCanvas;
    private RenderMode originalRenderMode;
    private Camera originalWorldCamera;

    public Button option1Button;
    public Button option2Button;
    public Button exitButton;

    [Header("Highlight Overlay")]
    public Material highlightMaterial;

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
        if (pressEText != null) pressEText.gameObject.SetActive(false);
        if (pcMenu != null) pcMenu.Hide();

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

        // =============================================
        //  STORAGE — proximity check, runs before raycast
        //  When holding a delivery box near the shelf, E stores it
        // =============================================
        if (placementManager != null && placementManager.isHoldingItem
            && StorageRoomShelf.Instance != null)
        {
            float dist = Vector3.Distance(transform.position,
                StorageRoomShelf.Instance.transform.position);

            if (dist <= storageDetectRange)
            {
                GameObject held = placementManager.GetHeldObject();
                bool hasDeliveryBox = held != null
                    && (held.GetComponent<DeliveryBox>() != null
                     || held.GetComponentInChildren<DeliveryBox>() != null);

                if (hasDeliveryBox)
                {
                    if (interactionPrompt != null) interactionPrompt.Show("E", "Store Item");
                    if (pressEText != null) { pressEText.text = "[E] Store Item"; pressEText.gameObject.SetActive(true); }



                    if (Input.GetKeyDown(KeyCode.E))
                    {
                        HideAllPrompts();
                        held.SetActive(true);
                        DeliveryBox dBox = held.GetComponent<DeliveryBox>();
                        if (dBox == null) dBox = held.GetComponentInChildren<DeliveryBox>();
                        if (dBox != null)
                        {
                            dBox.InteractUnpack();
                            placementManager.ForceRelease();
                            if (TutorialManager.Instance != null)
                                TutorialManager.Instance.CompleteStorageShelfTask();
                        }
                    }
                    return;
                }
            }
        }

        if (pcMenu != null && pcMenu.IsOpen())
        {
            Ray checkRay = mainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            RaycastHit checkHit;
            bool stillLookingAtPC = false;

            if (Physics.Raycast(checkRay, out checkHit, interactRange, interactLayer))
            {
                InspectableItem checkItem = checkHit.collider.GetComponent<InspectableItem>();
                stillLookingAtPC = checkItem != null && checkItem.isMainObject
                                && checkHit.collider.CompareTag("PickupPC");
            }

            if (!stillLookingAtPC) HideAllPrompts();
            if (interactionPrompt != null) interactionPrompt.Hide();
        if (pressEText != null) pressEText.gameObject.SetActive(false);
            return;
        }

        if (isUsingComputer)
        {
            HideAllPrompts();
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                PauseManager.BlockPause = true;
                PCController currentOS = computerOS;
                if (activeWorkstationMonitor != null && activeWorkstationMonitor.localOS != null)
                    currentOS = activeWorkstationMonitor.localOS;
                if (currentOS != null && currentOS.HandleEscapeInput())
                {
                    if (activeWorkstationMonitor != null) CloseWorkstationMonitor();
                    else CloseShopComputer();
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

        if (Physics.SphereCast(ray, 0.25f, out hit, interactRange, interactLayer))
        {
            Debug.Log("HIT: " + hit.collider.gameObject.name + " | Tag: " + hit.collider.tag + " | Layer: " + LayerMask.LayerToName(hit.collider.gameObject.layer));

            // FIX: Use GetComponentInParent so NPCWalker is found even if the
            // collider hit belongs to a child object of the NPC prefab.
            NPCWalker cityNPC = hit.collider.GetComponent<NPCWalker>()
                             ?? hit.collider.GetComponentInParent<NPCWalker>();

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
            //  DOOR WITH SCROLL MENU
            // =============================================
            if (doorMenu != null)
            {
                bool tutActive = TutorialManager.Instance != null && TutorialManager.Instance.IsTutorialActive();
                bool endDayStep = TutorialManager.Instance != null && TutorialManager.Instance.IsEndDayStep();

                if (tutActive && !endDayStep)
                { ShowPromptWithHighlight("X", "Finish your tasks first!", hit.collider.gameObject); return; }

                if (pcMenu != null) pcMenu.Hide();
                ShowPromptWithHighlight("E", "Open Door", hit.collider.gameObject);
                if (Input.GetKeyDown(KeyCode.E)) OpenDoorMenu(doorMenu);
            }
            // =============================================
            //  RAW SCENE DOOR
            // =============================================
            else if (sceneDoor != null)
            {
                if (pcMenu != null) pcMenu.Hide();
                ShowPromptWithHighlight("E", "Open Door", hit.collider.gameObject);
                if (Input.GetKeyDown(KeyCode.E))
                {
                    DoorInteractionMenu childMenu = sceneDoor.GetComponentInChildren<DoorInteractionMenu>();
                    if (childMenu != null) OpenDoorMenu(childMenu);
                    else sceneDoor.EnterDoor();
                }
            }
            // =============================================
            //  RAW SHOP DOOR
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
                // Check if this is the customer's desk PC (view-only)
                bool isDeskPC = CustomerDeskManager.Instance != null
                    && hit.collider.gameObject.name.StartsWith("DeskPC_");

                if (isDeskPC)
                {
                    // Counter PC — view only, no grabbing
                    if (pcMenu != null) pcMenu.Hide();
                    ShowPromptWithHighlight("E", "Preview PC", hit.collider.gameObject);

                    if (Input.GetKeyDown(KeyCode.E))
                    {
                        HideAllPrompts();
                        if (TutorialManager.Instance != null)
                            TutorialManager.Instance.CompleteCashierInspectTask();
                        if (inspectionManager != null) inspectionManager.InspectViewOnly(item);
                    }
                }
                else
                {
                    // Normal workstation PC — full inspect + grab
                    if (pcMenu != null) pcMenu.Hide();
                    ShowPromptWithHighlight("E / Q", "E: Inspect | Q: Grab", hit.collider.gameObject);

                    if (Input.GetKeyDown(KeyCode.E))
                    {
                        HideAllPrompts();
                        if (TutorialManager.Instance != null) TutorialManager.Instance.CompletePCTask();
                        if (inspectionManager != null) inspectionManager.Inspect(item);
                    }
                    else if (Input.GetKeyDown(KeyCode.Q))
                    {
                        HideAllPrompts();
                        PickUpItem(hit.collider.gameObject);
                    }
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
            else if (cityNPC != null)
            {
                if (cityNPC.isExhausted)
                {
                    // NPC is done talking — show a locked prompt, no interaction
                    ShowPromptWithHighlight("X", "Not interested anymore", hit.collider.gameObject);
                }
                else
                {
                    if (pcMenu != null) pcMenu.Hide();
                    ShowPromptWithHighlight("E", "Talk to Citizen", hit.collider.gameObject);
                    if (Input.GetKeyDown(KeyCode.E)) StartCityInteraction(cityNPC);
                }
            }
            // =============================================
            //  SHOP CUSTOMER
            //  Only allowed at step 5 (accept job) during tutorial
            // =============================================
            else if (readyShopCustomer)
            {
                bool tutActive = TutorialManager.Instance != null && TutorialManager.Instance.IsTutorialActive();
                bool cashierStep = TutorialManager.Instance != null && TutorialManager.Instance.IsCashierPCStep();
                bool mustInspectFirst = TutorialManager.Instance != null && TutorialManager.Instance.GetCurrentStep() == 5;

                if (tutActive && (!cashierStep || mustInspectFirst))
                {
                    string msg = mustInspectFirst ? "Inspect the PC first!" : "Finish your tasks first!";
                    ShowPromptWithHighlight("X", msg, hit.collider.gameObject); return;
                }

                if (pcMenu != null) pcMenu.Hide();
                ShowPromptWithHighlight("E", "Talk to Customer", hit.collider.gameObject);
                if (Input.GetKeyDown(KeyCode.E)) StartShopInteraction(shopCustomer);
            }
            // =============================================
            //  WORKSTATION MONITOR  (Email / Shop PC / Cashier)
            // =============================================
            else if (hit.collider.CompareTag("WorkstationMonitor"))
            {
                bool tutActive = TutorialManager.Instance != null && TutorialManager.Instance.IsTutorialActive();
                bool emailStep = TutorialManager.Instance != null && TutorialManager.Instance.IsEmailStep();
                bool cashierStep = TutorialManager.Instance != null && TutorialManager.Instance.IsCashierPCStep();
                bool shopPCStep = TutorialManager.Instance != null && TutorialManager.Instance.IsShopPCStep();

                if (tutActive && !emailStep && !cashierStep && !shopPCStep)
                {
                    bool repairStep = TutorialManager.Instance != null && TutorialManager.Instance.IsRepairStep();
                    if (repairStep) { HideAllPrompts(); return; }
                    if (cashierStep) { HideAllPrompts(); return; }
                    ShowPromptWithHighlight("X", "Finish your tasks first!", hit.collider.gameObject);
                    return;
                }

                WorkstationMonitor monitor = hit.collider.GetComponent<WorkstationMonitor>();
                if (monitor == null) monitor = hit.collider.GetComponentInParent<WorkstationMonitor>();

                if (monitor != null)
                {
                    if (pcMenu != null) pcMenu.Hide();

                    // Step 5 — player should talk to customer, not use monitor
                    if (cashierStep) { HideAllPrompts(); return; }

                    // Steps 17/24 — shop PC to order or install
                    if (shopPCStep)
                    {
                        ShowPromptWithHighlight("E", "Use Computer", hit.collider.gameObject);
                        if (Input.GetKeyDown(KeyCode.E))
                        {
                            HideAllPrompts();
                            if (TutorialManager.Instance != null)
                                TutorialManager.Instance.CompleteApproachShopPCTask();
                            OpenWorkstationMonitor(monitor);
                        }
                        return;
                    }

                    // Normal flow — step 32 email + post-tutorial
                    if (monitor.CanInteract())
                    {
                        ShowPromptWithHighlight("E", "Use Monitor", hit.collider.gameObject);
                        if (Input.GetKeyDown(KeyCode.E))
                        {
                            HideAllPrompts();
                            if (TutorialManager.Instance != null)
                                TutorialManager.Instance.CompleteApproachEmailTask();
                            OpenWorkstationMonitor(monitor);
                        }
                    }
                    else
                    {
                        ShowPromptWithHighlight("X", monitor.GetBlockedReason(), hit.collider.gameObject);
                    }
                }
            }
            // =============================================
            //  SHOP COMPUTER  (ShopTrigger)
            // =============================================
            else if (shopPC)
            {
                bool tutActive = TutorialManager.Instance != null && TutorialManager.Instance.IsTutorialActive();
                bool cashierStep = TutorialManager.Instance != null && TutorialManager.Instance.IsCashierPCStep();
                bool shopPCStep = TutorialManager.Instance != null && TutorialManager.Instance.IsShopPCStep();
                bool emailStep = TutorialManager.Instance != null && TutorialManager.Instance.IsEmailStep();

                if (tutActive && cashierStep) { HideAllPrompts(); return; }

                if (tutActive && !shopPCStep && !emailStep)
                { ShowPromptWithHighlight("X", "Finish your tasks first!", hit.collider.gameObject); return; }

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

                DeliveryBox dBox = hit.collider.GetComponentInParent<DeliveryBox>();

                if (dBox != null)
                {
                    // Delivery boxes — Q only, carry to storage
                    ShowPromptWithHighlight("Q", "Carry to Storage", hit.collider.gameObject);
                    if (Input.GetKeyDown(KeyCode.Q)) PickUpItem(hit.collider.gameObject);
                }
                else
                {
                    // Loose components — E to inventory, Q to carry
                    ShowPromptWithHighlight("E / Q", "E: Inventory | Q: Carry", hit.collider.gameObject);
                    if (Input.GetKeyDown(KeyCode.E))
                    {
                        InspectableItem looseItem = hit.collider.GetComponentInParent<InspectableItem>();
                        if (looseItem != null)
                        {
                            InventorySystem.Instance.AddPart(hit.collider.gameObject, looseItem);
                            HideAllPrompts();
                        }
                    }
                    else if (Input.GetKeyDown(KeyCode.Q)) PickUpItem(hit.collider.gameObject);
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
        if (pressEText != null) { pressEText.text = "[" + key + "] " + action; pressEText.gameObject.SetActive(true); }
        ApplyHighlight(target);
    }

    void HideAllPrompts()
    {
        if (interactionPrompt != null) interactionPrompt.Hide();
        if (pressEText != null) pressEText.gameObject.SetActive(false);
        if (pressEText != null) pressEText.gameObject.SetActive(false);
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
            DeliveryBox deliveryScript = itemObj.GetComponentInParent<DeliveryBox>();
            if (deliveryScript != null)
            {
                itemObj = deliveryScript.gameObject;
                if (TutorialManager.Instance != null)
                    TutorialManager.Instance.CompletePickupDeliveryTask();
            }

            JobBox boxScript = itemObj.GetComponentInParent<JobBox>();
            if (boxScript != null) itemObj = boxScript.gameObject;

            PCCaseBuilder pcScript = itemObj.GetComponentInParent<PCCaseBuilder>();
            if (pcScript != null) itemObj = pcScript.gameObject;

            placementManager.PickUpObject(itemObj);
            HideAllPrompts();

            if (itemObj.CompareTag("PickupBox") && TutorialManager.Instance != null)
                TutorialManager.Instance.CompletePickupBoxTask();
        }
    }

    void StartCityInteraction(NPCWalker npc)
    {
        // FIX: Guard against unassigned UI references so the interaction
        // doesn't silently crash and leave isInteracting stuck at true.
        if (dialoguePanel == null || nameText == null || dialogueText == null
            || option1Button == null || option2Button == null || exitButton == null)
        {
            Debug.LogWarning("[PlayerInteract] StartCityInteraction: one or more UI references " +
                             "are not assigned on this PlayerInteract instance. " +
                             "Please assign dialoguePanel, nameText, dialogueText, " +
                             "option1Button, option2Button, and exitButton in the Inspector.");
            return;
        }

        isInteracting = true;
        currentCityNPC = npc;
        currentShopNPC = null;
        FreezePlayer(true);
        HideAllPrompts();

        dialoguePanel.SetActive(true);
        option1Button.gameObject.SetActive(true);
        option2Button.gameObject.SetActive(true);
        exitButton.gameObject.SetActive(true);
        if (computerOS != null) computerOS.gameObject.SetActive(false);
        option1Button.interactable = true;
        option2Button.interactable = true;

        var btn1Label = option1Button.GetComponentInChildren<TextMeshProUGUI>();
        var btn2Label = option2Button.GetComponentInChildren<TextMeshProUGUI>();
        if (btn1Label != null)
        {
            int left = npc.InviteAttemptsLeft();
            btn1Label.text = left > 0 ? "Invite to Shop" : "Invite to Shop";
        }
        if (btn2Label != null) btn2Label.text = npc.option2Response;

        nameText.text = npc.npcName;
        dialogueText.text = npc.greeting;
        npc.StartConversation();
    }

    void StartShopInteraction(CustomerInside npc)
    {
        isInteracting = true; currentShopNPC = npc; currentCityNPC = null;
        FreezePlayer(true); HideAllPrompts();
        dialoguePanel.SetActive(true);
        option1Button.gameObject.SetActive(true);
        option2Button.gameObject.SetActive(true);
        exitButton.gameObject.SetActive(true);
        if (characterPicture != null) characterPicture.gameObject.SetActive(false);
        if (computerOS != null) computerOS.gameObject.SetActive(false);
        option1Button.interactable = true; option2Button.interactable = true;

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
        if (currentCityNPC != null)
        {
            dialogueText.text = currentCityNPC.TryInviteToShop();
            // Update button to show remaining attempts
            var btn1Label = option1Button.GetComponentInChildren<TextMeshProUGUI>();
            int left = currentCityNPC.InviteAttemptsLeft();
            if (currentCityNPC.isExhausted || currentCityNPC.isGoingToShop)
            {
                // No more attempts or already going — disable the invite button
                option1Button.interactable = false;
                if (btn1Label != null) btn1Label.text = "Invite to Shop";
            }
            else if (btn1Label != null)
            {
                btn1Label.text = "Invite to Shop";
            }
        }
        else if (currentShopNPC != null)
        {
            dialogueText.text = "Deal! I'll take a look.";
            currentShopNPC.AcceptJob();
            option1Button.interactable = false; option2Button.interactable = false;
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
            option1Button.interactable = false; option2Button.interactable = false;
            StartCoroutine(CloseAfterDelay(false));
        }
    }

    public void OnExitClick()
    {
        isInteracting = false;
        dialoguePanel.SetActive(false);
        if (characterPicture != null) characterPicture.gameObject.SetActive(true);
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

    // =============================================
    //  MAIN SHOP COMPUTER
    // =============================================
    void OpenShopComputer()
    {
        isInteracting = true; isUsingComputer = true;
        if (computerOS != null) { computerOS.gameObject.SetActive(true); computerOS.ShowDesktop(); }
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
        isUsingComputer = false; isInteracting = false;
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
        activeWorkstationMonitor = monitor; isInteracting = true; isUsingComputer = true;
        Canvas activeCanvas = monitor.localCanvas;
        PCController activeOS = monitor.localOS;

        if (activeOS != null && activeCanvas != null)
        {
            originalRenderMode = activeCanvas.renderMode;
            originalWorldCamera = activeCanvas.worldCamera;
            activeOS.gameObject.SetActive(true);
            activeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            GraphicRaycaster raycaster = activeCanvas.GetComponent<GraphicRaycaster>();
            if (raycaster != null) raycaster.enabled = true;
            if (monitor.uiBridge != null)
            {
                monitor.uiBridge.ActivateForShop();
                monitor.uiBridge.ActivateForEmail();
            }
            PCPowerSystem pc = monitor.GetLinkedPC();
            if (pc != null && pc.HasAnyFault()) activeOS.RestartToBIOS();
            else activeOS.BootToOS();
        }

        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        HideAllPrompts();
        if (goldHUD != null) goldHUD.SetActive(false);
        FreezePlayer(true);
        if (TutorialManager.Instance != null)
            TutorialManager.Instance.NotifyMonitorOpenedForTutorial(monitor);
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
                GraphicRaycaster raycaster = activeCanvas.GetComponent<GraphicRaycaster>();
                if (raycaster != null) raycaster.enabled = false;
            }
        }
        activeWorkstationMonitor = null; isInteracting = false; isUsingComputer = false;
        PauseManager.BlockPause = false;
        if (goldHUD != null) goldHUD.SetActive(true);
        FreezePlayer(false);
        if (TutorialManager.Instance != null) TutorialManager.Instance.RestoreTaskIfNeeded();
    }

    // =============================================
    //  HIGHLIGHT OVERLAY
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
            if (!highlightCache.ContainsKey(rend)) highlightCache.Add(rend, rend.sharedMaterials);
            Material[] currentMats = rend.sharedMaterials;
            Material[] newMats = new Material[currentMats.Length + 1];
            for (int i = 0; i < currentMats.Length; i++) newMats[i] = currentMats[i];
            newMats[newMats.Length - 1] = highlightMaterial;
            rend.sharedMaterials = newMats;
        }
    }

    void ClearHighlight()
    {
        foreach (var kv in highlightCache)
            if (kv.Key != null) kv.Key.sharedMaterials = kv.Value;
        highlightCache.Clear();
        currentHighlightedObject = null;
    }
}
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// UPDATED PlayerInteract — Now with:
/// 1. Styled [Key] + Action prompts for each interaction type
/// 2. PC scroll menu (Inspect / Grab) that shows automatically when looking at a PC
/// 3. Unique text: "Talk to Citizen", "Talk to Customer", "Use Computer", etc.
/// 4. Door still uses DoorInteractionMenu scroll system
/// 
/// NEW INSPECTOR SLOTS:
/// - interactionPrompt: The new styled prompt UI
/// - pcMenu: The PC scroll menu (Inspect/Grab)
/// - You can DELETE the old pressEPrompt and pressEText references
/// </summary>
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
    public InteractionPromptUI interactionPrompt;  // The styled [E] Action prompt
    public PCInteractionMenu pcMenu;               // Scroll menu for PCs

    [Header("Legacy UI (keep for dialogue)")]
    public GameObject goldHUD;
    public GameObject dialoguePanel;
    public PCController computerOS;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI dialogueText;

    // UI Buttons
    public Button option1Button;
    public Button option2Button;
    public Button exitButton;

    // State Variables
    private NPCWalker currentCityNPC;
    private CustomerInside currentShopNPC;
    private bool isInteracting = false;
    private DoorInteractionMenu activeDoorMenu = null;
    private bool menuJustClosed = false;

    // Track what we're currently looking at (for PC menu)
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
        // --- Block during day transition ---
        if (dayTransitionManager != null && dayTransitionManager.IsTransitioning())
        {
            HideAllPrompts();
            return;
        }

        // --- Eat one frame after menu closes ---
        if (menuJustClosed)
        {
            menuJustClosed = false;
            return;
        }

        // --- GLOBAL LOCK: Inspecting PC case ---
        if (inspectionManager != null && inspectionManager.isInspecting)
        {
            HideAllPrompts();
            return;
        }

        // --- DOOR MENU OPEN ---
        if (activeDoorMenu != null && activeDoorMenu.IsOpen())
        {
            HideAllPrompts();
            return;
        }

        // --- PC MENU: If open, let it handle E input, hide regular prompt ---
       // --- PC MENU: If open, let it handle ALL input ---
        if (pcMenu != null && pcMenu.IsOpen())
        {
            if (interactionPrompt != null) interactionPrompt.Hide();
            return; // BLOCK everything else while PC menu is open
        }

        // --- COMPUTER OS OPEN ---
        if (computerOS != null && computerOS.gameObject.activeSelf)
        {
            HideAllPrompts();
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                PauseManager.BlockPause = true;
                if (computerOS.HandleEscapeInput())
                {
                    CloseShopComputer();
                }
            }
            return;
        }

        // --- TALKING TO NPC ---
        if (isInteracting)
        {
            HideAllPrompts();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }

        // =============================================
        //  RAYCAST FOR INTERACTABLES
        // =============================================
        Ray ray = mainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactRange, interactLayer))
        {
            NPCWalker cityNPC = hit.collider.GetComponent<NPCWalker>();
            CustomerInside shopCustomer = hit.collider.GetComponent<CustomerInside>();
            ShopTrigger shopPC = hit.collider.GetComponent<ShopTrigger>();
            ShopDoor shopDoor = hit.collider.GetComponent<ShopDoor>();
            SceneDoor sceneDoor = hit.collider.GetComponent<SceneDoor>();
            InspectableItem item = hit.collider.GetComponent<InspectableItem>();
            DoorInteractionMenu doorMenu = hit.collider.GetComponent<DoorInteractionMenu>();

            bool isPickupBox = hit.collider.CompareTag("PickupBox");
            bool isPickupPC = hit.collider.CompareTag("PickupPC");
            bool canInspectInWorld = (item != null && item.isMainObject);
            bool readyShopCustomer = (shopCustomer != null && shopCustomer.isAtSpot);

            // =============================================
            //  DOOR WITH SCROLL MENU
            // =============================================
            if (doorMenu != null)
            {
                if (pcMenu != null) pcMenu.Hide();
                ShowPrompt("E", "Open Door");

                if (Input.GetKeyDown(KeyCode.E))
                {
                    OpenDoorMenu(doorMenu);
                }
            }
            // =============================================
            //  PC ON WORKSTATION — SCROLL MENU (auto-visible)
            // =============================================
           else if (canInspectInWorld && isPickupPC)
            {
                if (interactionPrompt != null) interactionPrompt.Hide();

                if (pcMenu != null && !pcMenu.IsOpen())
                {
                    // STORE references so the callback has them later
                    storedInspectItem = item;
                    storedPickupTarget = hit.collider.gameObject;

                    pcMenu.Show((int choice) =>
                    {
                        if (choice == 0 && storedInspectItem != null)
                        {
                            // Inspect
                            pcMenu.Hide();
                            if (TutorialManager.Instance != null) TutorialManager.Instance.CompletePCTask();
                            if (inspectionManager != null) inspectionManager.Inspect(storedInspectItem);
                        }
                        else if (choice == 1 && storedPickupTarget != null)
                        {
                            // Grab
                            pcMenu.Hide();
                            PickUpItem(storedPickupTarget);
                        }

                        storedInspectItem = null;
                        storedPickupTarget = null;
                    });
                }
            }
            // =============================================
            //  INSPECTABLE ITEM (PC that can only be inspected, not grabbed)
            // =============================================
            else if (canInspectInWorld)
            {
                if (pcMenu != null) pcMenu.Hide();
                ShowPrompt("E", "Inspect PC");

                if (Input.GetKeyDown(KeyCode.E))
                {
                    if (TutorialManager.Instance != null) TutorialManager.Instance.CompletePCTask();
                    if (inspectionManager != null) inspectionManager.Inspect(item);
                    HideAllPrompts();
                }
            }
            // =============================================
            //  CITY NPC
            // =============================================
            else if (cityNPC)
            {
                if (pcMenu != null) pcMenu.Hide();
                ShowPrompt("E", "Talk to Citizen");

                if (Input.GetKeyDown(KeyCode.E))
                    StartCityInteraction(cityNPC);
            }
            // =============================================
            //  SHOP CUSTOMER
            // =============================================
            else if (readyShopCustomer)
            {
                if (pcMenu != null) pcMenu.Hide();
                ShowPrompt("E", "Talk to Customer");

                if (Input.GetKeyDown(KeyCode.E))
                    StartShopInteraction(shopCustomer);
            }
            // =============================================
            //  SHOP COMPUTER (monitor)
            // =============================================
            else if (shopPC)
            {
                if (pcMenu != null) pcMenu.Hide();
                ShowPrompt("E", "Use Computer");

                if (Input.GetKeyDown(KeyCode.E))
                    OpenShopComputer();
            }
            // =============================================
            //  SHOP DOOR (without DoorInteractionMenu)
            // =============================================
            else if (shopDoor)
            {
                if (pcMenu != null) pcMenu.Hide();
                ShowPrompt("E", "Enter Shop");

                if (Input.GetKeyDown(KeyCode.E))
                    shopDoor.EnterShop(transform.root.gameObject);
            }
            // =============================================
            //  SCENE DOOR (without DoorInteractionMenu)
            // =============================================
            else if (sceneDoor)
            {
                if (pcMenu != null) pcMenu.Hide();
                ShowPrompt("E", "Go Outside");

                if (Input.GetKeyDown(KeyCode.E))
                    sceneDoor.EnterDoor();
            }
            // =============================================
            //  PICKUP BOX
            // =============================================
            else if (isPickupBox)
            {
                if (pcMenu != null) pcMenu.Hide();
                ShowPrompt("Q", "Pick Up Box");

                if (Input.GetKeyDown(KeyCode.Q))
                    PickUpItem(hit.collider.gameObject);
            }
            // =============================================
            //  PICKUP PC (not inspectable, just grab)
            // =============================================
            else if (isPickupPC)
            {
                if (pcMenu != null) pcMenu.Hide();
                ShowPrompt("Q", "Pick Up PC");

                if (Input.GetKeyDown(KeyCode.Q))
                    PickUpItem(hit.collider.gameObject);
            }
            // =============================================
            //  NOTHING INTERACTABLE
            // =============================================
            else
            {
                HideAllPrompts();
            }
        }
        else
        {
            // Not looking at anything
            HideAllPrompts();
            currentLookTarget = null;
        }
    }

    // =============================================
    //  PROMPT HELPERS
    // =============================================

    void ShowPrompt(string key, string action)
    {
        if (interactionPrompt != null)
            interactionPrompt.Show(key, action);
    }

    void HideAllPrompts()
    {
        if (interactionPrompt != null) interactionPrompt.Hide();
        if (pcMenu != null) pcMenu.Hide();
    }

    // =============================================
    //  DOOR SCROLL MENU
    // =============================================

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

    // =============================================
    //  PICKUP
    // =============================================

    void PickUpItem(GameObject itemObj)
    {
        if (placementManager != null && !placementManager.isHoldingItem)
        {
            JobBox boxScript = itemObj.GetComponentInParent<JobBox>();
            if (boxScript != null) itemObj = boxScript.gameObject;

            PCCaseBuilder pcScript = itemObj.GetComponentInParent<PCCaseBuilder>();
            if (pcScript != null) itemObj = pcScript.gameObject;

            placementManager.PickUpObject(itemObj);
            HideAllPrompts();
        }
    }

    // =============================================
    //  NPC INTERACTIONS
    // =============================================

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
        }
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
        if (currentCityNPC != null)
        {
            dialogueText.text = currentCityNPC.option2Response;
        }
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

    // =============================================
    //  SHOP COMPUTER
    // =============================================

    void OpenShopComputer()
    {
        isInteracting = true;

        if (computerOS != null)
        {
            computerOS.gameObject.SetActive(true);
            computerOS.ShowDesktop();
        }

        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        HideAllPrompts();
        if (goldHUD != null) goldHUD.SetActive(false);
        FreezePlayer(true);

        if (TutorialManager.Instance != null)
            TutorialManager.Instance.HideTaskTemporarily();
    }

    public void CloseShopComputer()
    {
        isInteracting = false;
        if (computerOS != null) computerOS.gameObject.SetActive(false);
        if (goldHUD != null) goldHUD.SetActive(true);
        FreezePlayer(false);

        if (TutorialManager.Instance != null)
            TutorialManager.Instance.RestoreTaskIfNeeded();
    }
}
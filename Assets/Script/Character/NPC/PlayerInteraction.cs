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

    [Header("UI References")]
    public GameObject goldHUD; 
    public GameObject pressEPrompt; 
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

    void Start()
    {
        if (mainCam == null) mainCam = Camera.main; 
        if (placementManager == null) placementManager = FindObjectOfType<PlacementManager>();

        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (computerOS != null) computerOS.gameObject.SetActive(false);
        if (pressEPrompt != null) pressEPrompt.SetActive(false);
        
        if (goldHUD != null) goldHUD.SetActive(true); 
    }

 void Update()
    {
        if (computerOS != null && computerOS.gameObject.activeSelf)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                PauseManager.BlockPause = true; 

                if (computerOS.HandleEscapeInput())
                {
                    CloseShopComputer();
                }
                return; 
            }
        }

        if (isInteracting) 
        {
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
            ShopDoor shopDoor = hit.collider.GetComponent<ShopDoor>();   
            SceneDoor sceneDoor = hit.collider.GetComponent<SceneDoor>(); 
            InspectableItem item = hit.collider.GetComponent<InspectableItem>(); 
            
            // --- USING ONLY YOUR EXISTING TAGS ---
            bool isPickupBox = hit.collider.CompareTag("PickupBox");
            bool isPickupPC = hit.collider.CompareTag("PickupPC"); 
            
            bool canInspectInWorld = (item != null && item.isMainObject);
            bool readyShopCustomer = (shopCustomer != null && shopCustomer.isAtSpot);

            if (cityNPC || readyShopCustomer || shopDoor || sceneDoor || canInspectInWorld || isPickupBox || isPickupPC || shopPC)
            {
                pressEPrompt.SetActive(true); 

                if (Input.GetKeyDown(KeyCode.E))
                {
                    
                    if (cityNPC) StartCityInteraction(cityNPC);
                    else if (shopPC) OpenShopComputer(); 
                    else if (readyShopCustomer) StartShopInteraction(shopCustomer);
                    else if (shopDoor) shopDoor.EnterShop(transform.root.gameObject);
                    else if (sceneDoor) sceneDoor.EnterDoor();
                    else if (canInspectInWorld && inspectionManager) 
                    {
                        if (TutorialManager.Instance != null) TutorialManager.Instance.CompletePCTask();
                        inspectionManager.Inspect(item);
                        pressEPrompt.SetActive(false);
                    }
                }

                if (Input.GetKeyDown(KeyCode.Q))
                {
                    if (isPickupBox || isPickupPC) PickUpItem(hit.collider.gameObject);
                }
            }
            else pressEPrompt.SetActive(false);
        }
        else pressEPrompt.SetActive(false);
    }

   void PickUpItem(GameObject itemObj)
    {
        if (placementManager != null && !placementManager.isHoldingItem)
        {
            // SAFETY CHECK: If we clicked a sticker or a piece of cardboard, grab the main parent!
            JobBox boxScript = itemObj.GetComponentInParent<JobBox>();
            if (boxScript != null)
            {
                itemObj = boxScript.gameObject; 
            }

            PCCaseBuilder pcScript = itemObj.GetComponentInParent<PCCaseBuilder>();
            if (pcScript != null)
            {
                itemObj = pcScript.gameObject;
            }

            placementManager.PickUpObject(itemObj);
            pressEPrompt.SetActive(false);
        }
    }
    
    void StartCityInteraction(NPCWalker npc)
    {
        isInteracting = true;
        currentCityNPC = npc;
        currentShopNPC = null; 
        
        FreezePlayer(true);
        pressEPrompt.SetActive(false);
        dialoguePanel.SetActive(true);

        if(computerOS != null) computerOS.gameObject.SetActive(false);
        
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
        pressEPrompt.SetActive(false);
        dialoguePanel.SetActive(true);
        if(computerOS != null) computerOS.gameObject.SetActive(false);
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
            StartCoroutine(CloseAfterDelay(true)); // Pass TRUE because we accepted
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
            StartCoroutine(CloseAfterDelay(false)); // Pass FALSE because we rejected
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

    // UPDATED COROUTINE: Now checks if we accepted the job to trigger tutorial
    IEnumerator CloseAfterDelay(bool acceptedJob)
    {
        yield return new WaitForSeconds(1.5f);
        OnExitClick();

        if (acceptedJob && TutorialManager.Instance != null)
        {
            TutorialManager.Instance.CompleteCustomerTask();
        }
    }

  void OpenShopComputer()
    {
        isInteracting = true;
        
        if(computerOS != null) 
        {
            computerOS.gameObject.SetActive(true);
            computerOS.ShowDesktop(); 
        }

        if(dialoguePanel != null) dialoguePanel.SetActive(false);
        pressEPrompt.SetActive(false);
        
        if(goldHUD != null) goldHUD.SetActive(false);

        FreezePlayer(true);
    }

    public void CloseShopComputer()
    {
        isInteracting = false;

        if(computerOS != null) computerOS.gameObject.SetActive(false);
        
        if(goldHUD != null) goldHUD.SetActive(true);

        FreezePlayer(false);
    }
}
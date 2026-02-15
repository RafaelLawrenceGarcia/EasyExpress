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
    public GameObject pressEPrompt; 
    public GameObject dialoguePanel; 
    public TextMeshProUGUI nameText; 
    public TextMeshProUGUI dialogueText;

    // UI Buttons
    public Button option1Button;
    public Button option2Button;
    public Button exitButton;

    // State Variables
    private NPCWalker currentCityNPC;       // The one walking outside
    private CustomerInside currentShopNPC;  // The one waiting at the counter
    private bool isInteracting = false; 

    void Start()
    {
        if (mainCam == null) mainCam = Camera.main; 
        if (placementManager == null) placementManager = FindObjectOfType<PlacementManager>();

        // IMPORTANT: I removed the "AddListener" lines here.
        // You MUST connect the buttons in the Inspector to avoid the "Double Click" bug.
    }

    void Update()
    {
        // 1. Mouse Cursor Logic
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
            // --- IDENTIFY WHAT WE HIT ---
            NPCWalker cityNPC = hit.collider.GetComponent<NPCWalker>();
            CustomerInside shopCustomer = hit.collider.GetComponent<CustomerInside>();
            ShopDoor shopDoor = hit.collider.GetComponent<ShopDoor>();   
            SceneDoor sceneDoor = hit.collider.GetComponent<SceneDoor>(); 
            InspectableItem item = hit.collider.GetComponent<InspectableItem>(); 
            
            bool isPickupBox = hit.collider.CompareTag("PickupBox");
            bool isPickupPC = hit.collider.CompareTag("PickupPC"); 
            
            // Only allow shop customer if they are at the counter
            bool readyShopCustomer = (shopCustomer != null && shopCustomer.isAtSpot);

            // --- SHOW PROMPT ---
            if (cityNPC || readyShopCustomer || shopDoor || sceneDoor || item || isPickupBox || isPickupPC)
            {
                pressEPrompt.SetActive(true); 

                // --- PRESS E TO INTERACT ---
                if (Input.GetKeyDown(KeyCode.E))
                {
                    if (cityNPC) StartCityInteraction(cityNPC); // <--- OLD LOGIC RESTORED
                    else if (readyShopCustomer) StartShopInteraction(shopCustomer);
                    else if (shopDoor) shopDoor.EnterShop(transform.root.gameObject);
                    else if (sceneDoor) sceneDoor.EnterDoor();
                    else if (item) 
                    {
                        if(inspectionManager) 
                        {
                            inspectionManager.Inspect(item);
                            pressEPrompt.SetActive(false);
                        }
                    }
                }

                // --- PRESS Q TO PICKUP ---
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
        if (placementManager != null && !placementManager.isHoldingCardboardBox)
        {
            Destroy(itemObj); 
            placementManager.isHoldingCardboardBox = true; 
            pressEPrompt.SetActive(false);
        }
    }
    
    // --- THIS IS THE "OLD" NPC INTERACTION RESTORED ---
    void StartCityInteraction(NPCWalker npc)
    {
        isInteracting = true;
        currentCityNPC = npc;
        currentShopNPC = null; // Clear shop customer
        
        FreezePlayer(true);
        pressEPrompt.SetActive(false);
        dialoguePanel.SetActive(true);
        
        // Reset Buttons
        option1Button.interactable = true;
        option2Button.interactable = true;

        // Set Text (Invite / Chat)
        nameText.text = npc.npcName;
        dialogueText.text = npc.greeting;

        // Update Button Text (Optional - if you have text components on buttons)
        // SetButtonText("Invite to Shop", "Chat");

        npc.StartConversation(); 
    }

    void StartShopInteraction(CustomerInside npc)
    {
        isInteracting = true;
        currentShopNPC = npc;
        currentCityNPC = null; // Clear city NPC

        FreezePlayer(true);
        pressEPrompt.SetActive(false);
        dialoguePanel.SetActive(true);

        // Reset Buttons
        option1Button.interactable = true;
        option2Button.interactable = true;

        nameText.text = npc.npcName;
        dialogueText.text = npc.jobRequest;

        // Update Button Text (Optional)
        // SetButtonText("Accept Job", "Refuse");

        npc.StartShopConversation();
    }

    void FreezePlayer(bool freeze)
    {
        if (movementScript != null) movementScript.SetMovementState(!freeze);
        if (cameraScript != null) cameraScript.SetCameraState(!freeze);

        if (freeze) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
        else { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
    }

    // --- BUTTON 1 LOGIC (INVITE / ACCEPT) ---
    public void OnOption1Click() 
    { 
        // 1. If talking to a City Walker (Inviting)
        if (currentCityNPC != null) 
        {
            // Run the Dice Roll from your old script
            dialogueText.text = currentCityNPC.TryInviteToShop(); 
            
            // We DON'T close the menu instantly here, so you can read the "Yes/No" result.
        } 
        // 2. If talking to a Shop Customer (Job)
        else if (currentShopNPC != null)
        {
            dialogueText.text = "Deal! I'll take a look.";
            currentShopNPC.AcceptJob();
            
            option1Button.interactable = false;
            option2Button.interactable = false;
            StartCoroutine(CloseAfterDelay()); 
        }
    }

    // --- BUTTON 2 LOGIC (CHAT / REJECT) ---
    public void OnOption2Click() 
    { 
        // 1. If talking to a City Walker (Chat)
        if (currentCityNPC != null) 
        {
            dialogueText.text = currentCityNPC.option2Response; 
        } 
        // 2. If talking to a Shop Customer (Refuse)
        else if (currentShopNPC != null) 
        {
            dialogueText.text = "Sorry, I can't help you.";
            currentShopNPC.RejectJob();
            
            option1Button.interactable = false;
            option2Button.interactable = false;
            StartCoroutine(CloseAfterDelay());
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

    IEnumerator CloseAfterDelay()
    {
        yield return new WaitForSeconds(1.5f);
        OnExitClick();
    }
}
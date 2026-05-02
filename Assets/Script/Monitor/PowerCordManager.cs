using UnityEngine;

/// <summary>
/// PowerCordManager — Manages picking up the power cord plug
/// and connecting it to a PC's PSU port.
///
/// Attach to the Player (same object as PlayerInteract).
///
/// SIMPLIFIED FLOW (matches your prefab structure):
///   1. Player looks at the PowerCord child (the plug end) → "E: Pick Up Power Cord"
///   2. Press E → plug is hidden, player is carrying it
///   3. Player looks at a PC on the workstation → "E: Plug In Power Cord"
///   4. Press E → plug snaps to PSU port, power flows
///   5. Player looks at a plugged-in cord → "E: Unplug Power Cord"
///   6. Press E → cord returns to its resting spot on the outlet
///
///   Press Q while carrying → cord returns to outlet (cancel)
///
/// SETUP:
///   1. Attach to the Player object
///   2. Drag: mainCam, interactLayer, interactionPrompt, placementManager
///   3. PowerCord children tagged "PowerCord" on NPC layer
///   4. PC cases have PCPowerSystem + child tagged "PowerCordSlot" on NPC layer
///   5. IMPORTANT: The outlet body / wire mesh siblings must be on
///      Default layer so they don't block the raycast!
/// </summary>
public class PowerCordManager : MonoBehaviour
{
    [Header("References")]
    public Camera mainCam;
    public float interactRange = 4f;
    public LayerMask interactLayer;
    public InteractionPromptUI interactionPrompt;
    public PlacementManager placementManager;

    [Header("State (Read-Only)")]
    public bool isCarryingCord = false;
    public bool isHandlingInteraction = false;

    private PowerCordInteraction carriedCord = null;
    private InspectionManager cachedInspection;

    void Start()
    {
        cachedInspection = FindObjectOfType<InspectionManager>();
    }

    // LateUpdate runs AFTER PlayerInteract.Update()
    // so our prompts don't get overridden by HideAllPrompts()
    void LateUpdate()
    {
        isHandlingInteraction = false;

        // Don't interfere if player is holding a box
        if (placementManager != null && placementManager.isHoldingItem) return;

        // Don't run during inspection mode
        if (cachedInspection != null && cachedInspection.isInspecting) return;

        if (isCarryingCord)
            HandleCarrying();
        else
            HandleLooking();
    }

    // =============================================
    //  NOT CARRYING — Look for cords to pick up
    //  or plugged-in cords to unplug
    // =============================================
    void HandleLooking()
    {
        Ray ray = mainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        if (!Physics.Raycast(ray, out hit, interactRange, interactLayer)) return;

        // --- Check if we hit a PowerCord (the plug end) ---
        PowerCordInteraction cord = hit.collider.GetComponent<PowerCordInteraction>();
        if (cord == null) cord = hit.collider.GetComponentInParent<PowerCordInteraction>();

        if (cord != null)
        {
            isHandlingInteraction = true;

            if (cord.isPluggedIntoPC)
            {
                // Cord is plugged into a PC → offer to unplug
                if (interactionPrompt != null)
                    interactionPrompt.Show("E", "Unplug Power Cord");

                if (Input.GetKeyDown(KeyCode.E))
                {
                    cord.UnplugFromPC();
                    if (interactionPrompt != null) interactionPrompt.Hide();
                }
            }
            else
            {
                // Cord is loose (resting on outlet) → pick it up
                if (interactionPrompt != null)
                    interactionPrompt.Show("E", "Pick Up Power Cord");

                if (Input.GetKeyDown(KeyCode.E))
                {
                    PickUpCord(cord);
                }
            }
            return;
        }
    }

    // =============================================
    //  CARRYING — Look for a PC to plug into
    // =============================================
    void HandleCarrying()
    {
        if (carriedCord == null) { isCarryingCord = false; return; }

        isHandlingInteraction = true;

        // CANCEL with Q → cord returns to outlet
        if (Input.GetKeyDown(KeyCode.Q))
        {
            CancelCord();
            return;
        }

        Ray ray = mainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        if (!Physics.Raycast(ray, out hit, interactRange, interactLayer))
        {
            if (interactionPrompt != null)
                interactionPrompt.Show("Q", "Cancel | Look at PC to plug in");
            return;
        }

        // --- Check if we hit a PC's PSU port or the PC itself ---
        PCPowerSystem targetPC = null;

        if (hit.collider.CompareTag("PowerCordSlot"))
        {
            targetPC = hit.collider.GetComponentInParent<PCPowerSystem>();
        }
        else if (hit.collider.CompareTag("PickupPC"))
        {
            targetPC = hit.collider.GetComponent<PCPowerSystem>();
            if (targetPC == null)
                targetPC = hit.collider.GetComponentInParent<PCPowerSystem>();
        }

        if (targetPC != null)
        {
            // PC already has a cord?
            if (targetPC.isPowerCordConnected)
            {
                if (interactionPrompt != null)
                    interactionPrompt.Show("X", "PC already has a power cord");
            }
            else
            {
                if (interactionPrompt != null)
                    interactionPrompt.Show("E", "Plug In Power Cord");

                if (Input.GetKeyDown(KeyCode.E))
                {
                    PlugCordInto(targetPC);
                }
            }
            return;
        }

        // Not looking at anything useful
        if (interactionPrompt != null)
            interactionPrompt.Show("Q", "Cancel | Look at PC to plug in");
    }

    // =============================================
    //  ACTIONS
    // =============================================

    void PickUpCord(PowerCordInteraction cord)
    {
        isCarryingCord = true;
        carriedCord = cord;

        // Hide the plug while carrying
        cord.gameObject.SetActive(false);

        if (interactionPrompt != null) interactionPrompt.Hide();
        Debug.Log($"[PowerCordManager] Picked up {cord.name}.");
    }

    void PlugCordInto(PCPowerSystem pc)
    {
        if (carriedCord == null) return;

        // Show the plug again and connect it
        carriedCord.gameObject.SetActive(true);
        carriedCord.PlugIntoPC(pc);

        isCarryingCord = false;
        carriedCord = null;

        if (interactionPrompt != null) interactionPrompt.Hide();
        Debug.Log($"[PowerCordManager] Cord plugged into {pc.name}.");
    }

    void CancelCord()
    {
        if (carriedCord == null) return;

        // Return cord to its resting spot on the outlet
        carriedCord.ReturnToOutlet();

        isCarryingCord = false;
        carriedCord = null;

        if (interactionPrompt != null) interactionPrompt.Hide();
        Debug.Log("[PowerCordManager] Cord returned to outlet.");
    }

    // =============================================
    //  PUBLIC HELPERS
    // =============================================

    public bool IsCarrying()
    {
        return isCarryingCord;
    }
}
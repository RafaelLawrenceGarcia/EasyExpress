using UnityEngine;

/// <summary>
/// PowerCordManager — Manages picking up, carrying, and plugging in power cords.
///
/// This script integrates with your existing PlayerInteract system.
/// It handles the "carrying a cord" state separately from PlacementManager
/// so it doesn't conflict with box carrying.
///
/// SETUP:
///   1. Attach this to the same GameObject as PlayerInteract (the player).
///   2. Drag references in the Inspector:
///      - mainCam: your player camera
///      - interactLayer: same layer mask as PlayerInteract
///      - interactionPrompt: same InteractionPromptUI as PlayerInteract
///   3. Make sure PSU cord objects are tagged "PowerCord" and on the interact layer.
///   4. Make sure PC cases have PCPowerSystem + a child tagged "PowerCordSlot".
///
/// HOW IT WORKS:
///   • When NOT carrying a cord:
///     - Raycast hits a "PowerCord" tag → shows "E: Pick Up Power Cord"
///     - Press E → cord is hidden, player enters carrying state
///     - Raycast hits a plugged-in cord → shows "E: Unplug Power Cord"
///     - Press E → cord unplugs, player picks it up immediately
///
///   • When CARRYING a cord:
///     - Raycast hits a "PickupPC" with PCPowerSystem → shows "E: Plug In Power Cord"
///     - Press E → cord snaps to PSU slot
///     - Press Q → drop the cord on the ground
///
///   • This script yields to PlacementManager — if the player is already
///     holding a box, power cord interactions are disabled.
/// </summary>
public class PowerCordManager : MonoBehaviour
{
    [Header("References")]
    public Camera mainCam;
    public float interactRange = 4f;
    public LayerMask interactLayer;
    public InteractionPromptUI interactionPrompt;
    public PlacementManager placementManager;

    [Header("State")]
    public bool isCarryingCord = false;

    private PowerCordInteraction carriedCord = null;

    void Update()
    {
        // Don't interfere if player is holding a box or in a menu
        if (placementManager != null && placementManager.isHoldingItem) return;

        // Don't run if InspectionManager is active
        InspectionManager inspection = FindObjectOfType<InspectionManager>();
        if (inspection != null && inspection.isInspecting) return;

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

        // --- LOOSE CORD ON THE GROUND ---
        PowerCordInteraction cord = hit.collider.GetComponent<PowerCordInteraction>();
        if (cord == null) cord = hit.collider.GetComponentInParent<PowerCordInteraction>();

        if (cord != null)
        {
            if (cord.isPluggedIn)
            {
                // Show unplug prompt
                if (interactionPrompt != null) interactionPrompt.Show("E", "Unplug Power Cord");

                if (Input.GetKeyDown(KeyCode.E))
                {
                    cord.Unplug();
                    PickUpCord(cord);
                }
            }
            else
            {
                // Show pickup prompt
                if (interactionPrompt != null) interactionPrompt.Show("E", "Pick Up Power Cord");

                if (Input.GetKeyDown(KeyCode.E))
                {
                    PickUpCord(cord);
                }
            }
        }
    }

    // =============================================
    //  CARRYING — Look for a PC to plug into
    // =============================================
    void HandleCarrying()
    {
        // DROP with Q
        if (Input.GetKeyDown(KeyCode.Q))
        {
            DropCord();
            return;
        }

        Ray ray = mainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        if (!Physics.Raycast(ray, out hit, interactRange, interactLayer))
        {
            if (interactionPrompt != null) interactionPrompt.Show("Q", "Drop Power Cord");
            return;
        }

        // --- LOOKING AT A PC ON THE WORKSTATION ---
        // Check if we hit a PowerCordSlot tag directly
        PCPowerSystem targetPC = null;

        if (hit.collider.CompareTag("PowerCordSlot"))
        {
            targetPC = hit.collider.GetComponentInParent<PCPowerSystem>();
        }
        // Or if we hit the PC case itself
        else if (hit.collider.CompareTag("PickupPC"))
        {
            targetPC = hit.collider.GetComponent<PCPowerSystem>();
            if (targetPC == null)
                targetPC = hit.collider.GetComponentInParent<PCPowerSystem>();
        }

        if (targetPC != null)
        {
            // Check if this PC already has a cord
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

        // Not looking at anything useful — show drop hint
        if (interactionPrompt != null) interactionPrompt.Show("Q", "Drop Power Cord");
    }

    // =============================================
    //  ACTIONS
    // =============================================

    void PickUpCord(PowerCordInteraction cord)
    {
        isCarryingCord = true;
        carriedCord = cord;

        // Hide the cord while carrying
        cord.gameObject.SetActive(false);
        cord.transform.SetParent(null); // Detach from any parent

        if (interactionPrompt != null) interactionPrompt.Hide();

        Debug.Log($"[PowerCordManager] Picked up {cord.gameObject.name}.");
    }

    void PlugCordInto(PCPowerSystem pc)
    {
        if (carriedCord == null) return;

        // Re-enable the cord and plug it in
        carriedCord.gameObject.SetActive(true);
        carriedCord.PlugInto(pc);

        isCarryingCord = false;
        carriedCord = null;

        if (interactionPrompt != null) interactionPrompt.Hide();

        Debug.Log($"[PowerCordManager] Cord plugged into {pc.gameObject.name}.");
    }

    void DropCord()
    {
        if (carriedCord == null) return;

        // Place the cord at the player's feet
        carriedCord.gameObject.SetActive(true);
        carriedCord.transform.position = transform.position + transform.forward * 1f;
        carriedCord.transform.rotation = Quaternion.identity;

        isCarryingCord = false;
        carriedCord = null;

        if (interactionPrompt != null) interactionPrompt.Hide();

        Debug.Log("[PowerCordManager] Cord dropped.");
    }

    // =============================================
    //  PUBLIC HELPERS
    // =============================================

    /// <summary>
    /// Other scripts can check this to know if the player is busy with a cord.
    /// </summary>
    public bool IsCarrying()
    {
        return isCarryingCord;
    }
}
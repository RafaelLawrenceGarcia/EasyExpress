using UnityEngine;

/// <summary>
/// PowerCordInteraction — Attach to the "PowerCord" child object
/// (the plug end that goes into the PC's PSU port).
///
/// AUTO-OUTLET LINK:
///   On Start(), this script automatically finds the WallOutlet
///   on its parent (PSU cord3) and links to it. Since the cord
///   physically comes from the outlet, the outlet connection is
///   always automatic — the player only needs to plug into the PC.
///
/// FLOW:
///   1. Player picks up the PowerCord (it detaches from the outlet assembly)
///   2. Player plugs it into the PC's PSU port
///   3. Power flows automatically (outlet is auto-linked)
///
/// SETUP:
///   1. Attach this to the "PowerCord" child (the plug end)
///   2. Tag it as "PowerCord"
///   3. Set it and ALL its children to the NPC layer
///   4. Make sure the parent PSU cord3 has a WallOutlet script
///   5. The sibling objects (outlet body, wire mesh) should be
///      on Default layer so they don't block the raycast
/// </summary>
public class PowerCordInteraction : MonoBehaviour
{
    [Header("Connection State")]
    [Tooltip("Is this cord plugged into a PC's PSU port?")]
    public bool isPluggedIntoPC = false;

    [Header("References (Auto-Linked at Runtime)")]
    [Tooltip("The wall outlet this cord belongs to (found automatically).")]
    public WallOutlet parentOutlet = null;

    [Tooltip("The PC this cord is plugged into (set when player connects it).")]
    public PCPowerSystem connectedPC = null;

    // Remember where we started so we can return there when unplugged
    private Vector3 originalLocalPosition;
    private Quaternion originalLocalRotation;
    private Transform originalParent;
    void Start()
    {
        // Save the original position (resting on the outlet)
        originalLocalPosition = transform.localPosition;
        originalLocalRotation = transform.localRotation;
        originalParent = transform.parent;

        // Auto-find the WallOutlet on the parent (PSU cord3)
        if (transform.parent != null)
        {
            parentOutlet = transform.parent.GetComponent<WallOutlet>();
            if (parentOutlet == null)
                parentOutlet = transform.parent.GetComponentInParent<WallOutlet>();
        }

        if (parentOutlet != null)
            Debug.Log($"[PowerCord] {name} auto-linked to outlet: {parentOutlet.name}");
        else
            Debug.LogWarning($"[PowerCord] {name} could not find a WallOutlet on parent!");
    }

    // =============================================
    //  PLUG INTO PC
    // =============================================

    /// <summary>
    /// Plug this cord into a PC's PSU port.
    /// Since the other end is always in the outlet, power flows immediately.
    /// </summary>
    public void PlugIntoPC(PCPowerSystem pc)
    {
        if (pc == null) return;

        isPluggedIntoPC = true;
        connectedPC = pc;

        // Snap the plug to the PC's PSU port
        if (pc.powerCordSnapPoint != null)
        {
            transform.SetParent(pc.powerCordSnapPoint);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }
        else
        {
            transform.SetParent(pc.transform);
            transform.localPosition = Vector3.zero;
        }

        gameObject.SetActive(true);

        // Tell the outlet it's in use
        if (parentOutlet != null)
            parentOutlet.PlugIn(this);

        // Power flows — both ends connected
        pc.ConnectPowerCord(gameObject);

        Debug.Log($"[PowerCord] {name} plugged into PC {pc.name} — power flowing!");
    }

    // =============================================
    //  UNPLUG FROM PC
    // =============================================

    /// <summary>
    /// Unplug from the PC. The cord returns to its resting position on the outlet.
    /// </summary>
    public void UnplugFromPC()
    {
        // Cut power
        if (connectedPC != null)
        {
            connectedPC.DisconnectPowerCord();
            connectedPC = null;
        }

        // Tell the outlet it's free
        if (parentOutlet != null)
            parentOutlet.Unplug();

        isPluggedIntoPC = false;

        // Return cord to its original resting spot on the outlet
        ReturnToOutlet();

        Debug.Log($"[PowerCord] {name} unplugged — returned to outlet.");
    }

    // =============================================
    //  HELPERS
    // =============================================

    /// <summary>
    /// Moves the cord back to its original resting position on the outlet.
    /// </summary>
    public void ReturnToOutlet()
    {
        if (originalParent != null)
        {
            transform.SetParent(originalParent);
            transform.localPosition = originalLocalPosition;
            transform.localRotation = originalLocalRotation;
        }
        else
        {
            transform.SetParent(null);
        }

        gameObject.SetActive(true);
    }
}
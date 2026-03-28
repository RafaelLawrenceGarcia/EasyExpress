using UnityEngine;

/// <summary>
/// PowerCordInteraction — Attach to each PSU cord object in the scene.
///
/// Lets the player:
///   1. Look at a cord → see "E: Pick Up Power Cord" prompt
///   2. Press E → cord disappears, player is now "carrying" it
///   3. Look at a PC on the workstation → see "E: Plug In Power Cord" prompt
///   4. Press E → cord snaps to the PSU slot on the back of the case
///   5. Look at a plugged-in cord → see "E: Unplug Power Cord" prompt
///
/// SETUP:
///   1. Attach this script to each PSU cord prefab/object (PSU cord3, etc.)
///   2. Tag the cord object as "PowerCord"
///   3. Put the cord on the same layer as your interactable objects
///      (the same layer PlayerInteract raycasts against)
///   4. Make sure the cord has a Collider so raycasts can hit it
///   5. On each PC Case prefab, add a child called "PowerCordSlot":
///      - Position it at the PSU power inlet on the back
///      - Add a Collider (does NOT need to be trigger)
///      - Tag it as "PowerCordSlot"
///      - Put it on the interactable layer
///   6. The PCPowerSystem on the case needs its powerCordSnapPoint
///      set to that same PowerCordSlot transform.
/// </summary>
public class PowerCordInteraction : MonoBehaviour
{
    [Header("State")]
    [Tooltip("Is this cord currently plugged into a PC?")]
    public bool isPluggedIn = false;

    [Header("References (Set At Runtime)")]
    [Tooltip("The PCPowerSystem this cord is plugged into (null if not plugged in).")]
    public PCPowerSystem connectedPC = null;

    /// <summary>
    /// Called when the player plugs this cord into a PC.
    /// Snaps the cord to the PSU slot and notifies the PCPowerSystem.
    /// </summary>
    public void PlugInto(PCPowerSystem pc)
    {
        if (pc == null) return;

        isPluggedIn = true;
        connectedPC = pc;

        // Snap cord to the PSU slot position
        if (pc.powerCordSnapPoint != null)
        {
            transform.SetParent(pc.powerCordSnapPoint);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }
        else
        {
            // Fallback: just parent to the PC
            transform.SetParent(pc.transform);
        }

        // Re-enable the cord visuals (it was hidden while carrying)
        gameObject.SetActive(true);

        // Tell the PC it has power
        pc.PlugInCord(gameObject);

        Debug.Log($"[PowerCord] {gameObject.name} plugged into {pc.gameObject.name}.");
    }

    /// <summary>
    /// Called when the player unplugs this cord.
    /// Returns the cord to the world and notifies the PCPowerSystem.
    /// </summary>
    public void Unplug()
    {
        if (connectedPC != null)
        {
            connectedPC.UnplugCord();
        }

        isPluggedIn = false;

        // Detach from the PC
        transform.SetParent(null);

        connectedPC = null;

        Debug.Log($"[PowerCord] {gameObject.name} unplugged.");
    }
}
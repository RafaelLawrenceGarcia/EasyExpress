using UnityEngine;
 
/// <summary>
/// WallOutlet — Attach to the PSU cord3 ROOT object (the desk outlet).
///
/// This is a simple marker script that tells the system
/// "this is a wall outlet with a power cord attached."
///
/// The PowerCordInteraction on the child "PowerCord" will
/// automatically find this script on Start() and link to it.
///
/// SETUP:
///   1. Attach this to the PSU cord3 root object
///   2. That's it! No tag needed since the cord auto-finds it.
///   3. Keep this object on Default layer (NOT NPC)
///      so it doesn't block raycasts to the PowerCord child.
/// </summary>
public class WallOutlet : MonoBehaviour
{
    [Header("State (Read-Only)")]
    [Tooltip("Is the cord currently plugged into a PC?")]
    public bool isOccupied = false;
 
    // Runtime reference to the cord plugged into a PC from this outlet
    [HideInInspector] public PowerCordInteraction pluggedCord = null;
 
    /// <summary>
    /// Called when the cord from this outlet is plugged into a PC.
    /// </summary>
    public void PlugIn(PowerCordInteraction cord)
    {
        isOccupied = true;
        pluggedCord = cord;
        Debug.Log($"[WallOutlet] {name} — cord is now in use.");
    }
 
    /// <summary>
    /// Called when the cord is unplugged from the PC.
    /// </summary>
    public void Unplug()
    {
        isOccupied = false;
        pluggedCord = null;
        Debug.Log($"[WallOutlet] {name} — cord is free.");
    }
 
    /// <summary>
    /// Needed by PowerCordInteraction for compatibility.
    /// Returns this transform as the snap point.
    /// </summary>
    public Transform GetSnapTransform()
    {
        return transform;
    }
}
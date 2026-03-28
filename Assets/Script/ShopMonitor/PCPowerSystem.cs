using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// PCPowerSystem — Attach to the SAME GameObject as PCCaseBuilder.
/// 
/// This script controls whether the PC can turn on or not.
/// It checks two things:
///   1. Is a power cord physically plugged in?
///   2. Does the PC have any CRITICAL faults that prevent booting?
///
/// SETUP:
///   1. Add this component to your PC Case prefab (same object that has PCCaseBuilder).
///   2. Create an empty child object on the BACK of the case called "PowerCordSnapPoint".
///      Position it where the PSU power inlet is (the 3-prong socket on the back).
///   3. Drag that empty object into the "powerCordSnapPoint" field.
///   4. Add a Box Collider (isTrigger = true) around the PSU inlet area.
///      Tag the collider object as "PSUInlet" so PowerCordSnap can detect it.
///   5. The system auto-detects faults from InspectableItem components inside the PC.
/// </summary>
public class PCPowerSystem : MonoBehaviour
{
    [Header("Power Cord")]
    [Tooltip("Empty transform on the back of the case where the cord snaps to.")]
    public Transform powerCordSnapPoint;

    [Tooltip("Is a power cord currently plugged in?")]
    public bool isPowerCordConnected = false;

    [Header("Power State")]
    [Tooltip("Is the PC currently turned on?")]
    public bool isPoweredOn = false;

    [Tooltip("Should this PC start with the power cord already connected?\n" +
             "Tip: Set to FALSE for repair jobs so the player must plug it in.")]
    public bool startWithCordConnected = false;

    [Header("Visual Feedback")]
    [Tooltip("Optional: A cord model child that appears when plugged in.\n" +
             "Leave empty if using PowerCordSnap (it handles visuals).")]
    public GameObject connectedCordVisual;

    [Header("Audio (Optional)")]
    public AudioClip plugInSound;
    public AudioClip powerOnSound;
    public AudioClip powerFailSound;
    private AudioSource audioSource;

    // Runtime: the cord object that's plugged in
    [HideInInspector] public GameObject attachedCord = null;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 1f;
        audioSource.playOnAwake = false;

        isPowerCordConnected = startWithCordConnected;
        isPoweredOn = false; // Always start OFF

        if (connectedCordVisual != null)
            connectedCordVisual.SetActive(isPowerCordConnected);
    }

    // =============================================
    //  POWER CORD CONNECTION
    // =============================================

    /// <summary>
    /// Called by PowerCordSnap when the player plugs a cord in.
    /// </summary>
    public void PlugInCord(GameObject cord)
    {
        isPowerCordConnected = true;
        attachedCord = cord;

        if (connectedCordVisual != null)
            connectedCordVisual.SetActive(true);

        if (plugInSound != null && audioSource != null)
            audioSource.PlayOneShot(plugInSound);

        Debug.Log($"[PCPower] Power cord plugged into {gameObject.name}!");
    }

    /// <summary>
    /// Called when the player unplugs the cord.
    /// </summary>
    public void UnplugCord()
    {
        if (isPoweredOn)
            ForcePowerOff();

        isPowerCordConnected = false;
        attachedCord = null;

        if (connectedCordVisual != null)
            connectedCordVisual.SetActive(false);

        Debug.Log($"[PCPower] Power cord unplugged from {gameObject.name}.");
    }

    // =============================================
    //  POWER TOGGLE
    // =============================================

    /// <summary>
    /// Attempts to toggle the PC power.
    /// Returns true if the action succeeded.
    /// Also outputs a reason string for UI tooltips.
    /// </summary>
    public bool TryTogglePower(out string failReason)
    {
        failReason = "";

        if (isPoweredOn)
        {
            ForcePowerOff();
            return true;
        }
        else
        {
            return TryPowerOn(out failReason);
        }
    }

    /// <summary>
    /// Tries to turn the PC on. Checks cord + faults.
    /// </summary>
    public bool TryPowerOn(out string failReason)
    {
        failReason = "";

        // CHECK 1: Power cord
        if (!isPowerCordConnected)
        {
            failReason = "No power cord connected!";
            Debug.Log("[PCPower] Cannot turn on — no power cord connected!");
            PlaySound(powerFailSound);
            return false;
        }

        // CHECK 2: Critical faults
        string faultReason = GetCriticalFaultReason();
        if (!string.IsNullOrEmpty(faultReason))
        {
            failReason = faultReason;
            Debug.Log($"[PCPower] Cannot turn on — {faultReason}");
            PlaySound(powerFailSound);
            return false;
        }

        // All clear — power on!
        isPoweredOn = true;
        PlaySound(powerOnSound);
        Debug.Log($"[PCPower] {gameObject.name} powered ON!");
        return true;
    }

    /// <summary>
    /// Force the PC off (always works).
    /// </summary>
    public void ForcePowerOff()
    {
        isPoweredOn = false;
        Debug.Log($"[PCPower] {gameObject.name} powered OFF.");
    }

    // =============================================
    //  FAULT CHECKING
    //  These faults BLOCK the PC from booting.
    //  Other faults (slow perf, noise) let it boot.
    // =============================================

    /// <summary>
    /// Scans all child parts for critical boot-blocking faults.
    /// Returns reason string if PC cannot boot, or "" if OK.
    /// </summary>
    public string GetCriticalFaultReason()
    {
        InspectableItem[] allParts = GetComponentsInChildren<InspectableItem>(true);

        // --- Check for BROKEN essential components ---
        foreach (InspectableItem part in allParts)
        {
            if (part.isInventorySlot || part.isMainObject) continue;

            // PSU faults that prevent boot
            if (part.partCategory == "PSU" && part.fault == PartFault.Broken)
                return "PSU is dead — no power output";

            if (part.partCategory == "PSU" && part.fault == PartFault.Overloaded)
                return "PSU overloaded — insufficient wattage";

            // Motherboard faults that prevent boot
            if (part.partCategory == "Motherboard" && part.fault == PartFault.Broken)
                return "Motherboard is dead — no POST";

            if (part.partCategory == "Motherboard" && part.fault == PartFault.LooseConnection)
                return "24-pin ATX cable loose — reseat the connection";

            if (part.partCategory == "Motherboard" && part.fault == PartFault.Corrupted)
                return "BIOS corrupted — system stuck in boot loop";

            // CPU faults that prevent boot
            if (part.partCategory == "CPU" && part.fault == PartFault.Broken)
                return "CPU is dead — system won't POST";

            // RAM not seated prevents POST
            if (part.partCategory == "RAM" && part.fault == PartFault.NotSeated)
                return "RAM not seated — system beeps, won't POST";
        }

        // --- Check for MISSING essential parts (ghost slots) ---
        foreach (InspectableItem part in allParts)
        {
            if (!part.isInventorySlot) continue;

            if (part.partCategory == "PSU")
                return "No PSU installed — cannot power on";
            if (part.partCategory == "Motherboard")
                return "No motherboard — cannot boot";
            if (part.partCategory == "CPU")
                return "No CPU installed — cannot POST";
        }

        return ""; // All clear!
    }

    // =============================================
    //  HELPERS
    // =============================================

    /// <summary>
    /// User-friendly status string for UI tooltips.
    /// </summary>
    public string GetPowerStatus()
    {
        if (!isPowerCordConnected) return "No power cord connected";
        if (!isPoweredOn)
        {
            string fault = GetCriticalFaultReason();
            if (!string.IsNullOrEmpty(fault)) return $"Won't turn on: {fault}";
            return "Ready to turn on";
        }
        return "PC is ON";
    }

    void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
            audioSource.PlayOneShot(clip);
    }
}
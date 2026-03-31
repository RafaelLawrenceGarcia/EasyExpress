using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// PCPowerSystem — Attach to the same GameObject as PCCaseBuilder.
///
/// Controls whether the PC can turn on. Checks:
///   1. Is a power cord FULLY connected? (both outlet + PC ends)
///   2. Does the PC have critical faults that prevent booting?
///
/// The PowerCordInteraction script calls ConnectPowerCord() only
/// when BOTH ends of the cord are plugged in (outlet + PSU port).
///
/// SETUP:
///   1. Add this to your PC Case prefab (same object as PCCaseBuilder).
///      It will also be auto-added at runtime by the PCCaseBuilder change.
///   2. Create an empty child called "PowerCordSlot":
///      - Position it at the PSU power inlet on the back of the case
///      - Add a small Box Collider
///      - Tag it as "PowerCordSlot"
///      - Put it on the interactable layer
///   3. Drag that child into the "powerCordSnapPoint" field.
/// </summary>
public class PCPowerSystem : MonoBehaviour
{
    [Header("State (Read-Only in Inspector)")]
    [Tooltip("Is a power cord FULLY connected (outlet + PC)?")]
    public bool isPowerCordConnected = false;

    [Tooltip("Is this PC currently powered on?")]
    public bool isPoweredOn = false;

    [Header("Connected Cord Reference")]
    [Tooltip("The power cord object currently delivering power (set at runtime).")]
    public GameObject connectedCord = null;

    [Header("Snap Point")]
    [Tooltip("Drag the 'PowerCordSlot' child transform here.")]
    public Transform powerCordSnapPoint;

    [HideInInspector] public bool skipStartReset = false;

    void Start()
    {

        // Auto-find PowerCordSlot if not assigned (works for ALL PCs)
        if (powerCordSnapPoint == null)
        {
            foreach (Transform child in GetComponentsInChildren<Transform>(true))
            {
                if (child.CompareTag("PowerCordSlot"))
                {
                    powerCordSnapPoint = child;
                    Debug.Log($"[PCPowerSystem] Auto-found PowerCordSlot: {child.name}");
                    break;
                }
            }
        }
        if (skipStartReset) return;

        isPoweredOn = false;
        isPowerCordConnected = false;
        SetFansState(false);

        // Auto-find PowerCordSlot if not assigned in Inspector
        if (powerCordSnapPoint == null)
        {
            foreach (Transform child in GetComponentsInChildren<Transform>(true))
            {
                if (child.CompareTag("PowerCordSlot"))
                {
                    powerCordSnapPoint = child;
                    Debug.Log($"[PCPowerSystem] Auto-found PowerCordSlot: {child.name}");
                    break;
                }
            }
        }
    }

    // =============================================
    //  POWER CORD CONNECTION
    //  (Called by PowerCordInteraction when BOTH
    //   ends are connected — outlet + PC)
    // =============================================

    /// <summary>
    /// Called by PowerCordInteraction when both ends are plugged in.
    /// </summary>
    public void ConnectPowerCord(GameObject cord)
    {
        isPowerCordConnected = true;
        connectedCord = cord;
        Debug.Log($"[PCPowerSystem] Power cord delivering power to {gameObject.name}.");
    }

    /// <summary>
    /// Called when the cord is disconnected from either end.
    /// Forces the PC off if it was running.
    /// </summary>
    public void DisconnectPowerCord()
    {
        bool wasOn = isPoweredOn;
        isPowerCordConnected = false;
        connectedCord = null;

        if (wasOn)
        {
            isPoweredOn = false;
            SetFansState(false);
            Debug.Log($"[PCPowerSystem] Power lost — {gameObject.name} forced OFF.");
        }

        Debug.Log($"[PCPowerSystem] Power cord disconnected from {gameObject.name}.");
    }

    // =============================================
    //  POWER TOGGLE (called by InspectionManager)
    // =============================================

    /// <summary>
    /// Attempts to toggle the PC on/off.
    /// Returns true if the action succeeded.
    /// 'reason' is filled with a user-friendly message.
    /// </summary>
    public bool TryTogglePower(out string reason)
    {
        // --- TURNING OFF (always allowed) ---
        if (isPoweredOn)
        {
            isPoweredOn = false;
            SetFansState(false);
            reason = "PC powered off.";
            Debug.Log($"[PCPowerSystem] {gameObject.name} turned OFF.");
            return true;
        }

        // --- TURNING ON: CHECK REQUIREMENTS ---

        // 1. Power cord must be fully connected (outlet + PC)
        if (!isPowerCordConnected)
        {
            reason = "No power!\nPlug the cord into both the outlet and the PC.";
            Debug.Log($"[PCPowerSystem] Cannot turn on — no power cord connected.");
            return false;
        }

        // 2. Check for critical faults that prevent booting
        string faultReason = CheckCriticalFaults();
        if (!string.IsNullOrEmpty(faultReason))
        {
            reason = faultReason;
            Debug.Log($"[PCPowerSystem] Cannot turn on — {faultReason}");
            return false;
        }

        // All checks passed — power on!
        isPoweredOn = true;
        SetFansState(true);
        reason = "PC powered on!";
        Debug.Log($"[PCPowerSystem] {gameObject.name} turned ON.");
        return true;
    }

    // =============================================
    //  FAULT CHECKING
    //  Critical faults = PC won't even POST
    //  Non-critical faults = PC boots but has issues
    // =============================================

    string CheckCriticalFaults()
    {
        InspectableItem[] allParts = GetComponentsInChildren<InspectableItem>(true);

        foreach (InspectableItem part in allParts)
        {
            if (part.isMainObject) continue;

            // --- Missing essential parts (ghost slots) ---
            if (part.isInventorySlot)
            {
                if (part.partCategory == "PSU")
                    return "PSU is missing!\nInstall a power supply first.";
                if (part.partCategory == "Motherboard")
                    return "Motherboard is missing!\nInstall a motherboard first.";
                if (part.partCategory == "CPU")
                    return "CPU is missing!\nInstall a processor first.";
                continue;
            }

            // --- Critical faults on installed parts ---

            // PSU
            if (part.partCategory == "PSU" && part.fault == PartFault.Broken)
                return "PSU is dead!\nReplace the power supply.";
            if (part.partCategory == "PSU" && part.fault == PartFault.Overloaded)
                return "PSU is overloaded!\nWattage too low for this build.";

            // Motherboard
            if (part.partCategory == "Motherboard" && part.fault == PartFault.Broken)
                return "Motherboard is dead!\nReplace the motherboard.";
            if (part.partCategory == "Motherboard" && part.fault == PartFault.LooseConnection)
                return "24-pin power cable loose!\nReseat the ATX connection.";
            if (part.partCategory == "Motherboard" && part.fault == PartFault.Corrupted)
                return "BIOS is corrupted!\nThe system can't POST.";

            // CPU
            if (part.partCategory == "CPU" && part.fault == PartFault.Broken)
                return "CPU is dead!\nReplace the processor.";

            // RAM (prevents POST)
            if (part.partCategory == "RAM" && part.fault == PartFault.Broken)
                return "RAM is faulty!\nSystem fails POST — replace the RAM.";
            if (part.partCategory == "RAM" && part.fault == PartFault.NotSeated)
                return "RAM not seated properly!\nReseat the memory sticks.";
        }

        return null; // No blocking faults — all clear!
    }

    // =============================================
    //  FAN CONTROL
    // =============================================

    void SetFansState(bool on)
    {
        foreach (PCFanController fan in GetComponentsInChildren<PCFanController>(true))
        {
            fan.enabled = on;

            // Always kill emission directly on the renderer
            Renderer r = fan.GetComponentInChildren<Renderer>();
            if (r != null)
            {
                if (!on)
                {
                    r.material.EnableKeyword("_EMISSION");
                    r.material.SetColor("_EmissionColor", Color.black);
                }
            }
        }
    }

    // =============================================
    //  HELPERS (used by WorkstationMonitor)
    // =============================================

    /// <summary>
    /// Returns true if any installed part has a fault.
    /// Used by WorkstationMonitor to show error screens.
    /// </summary>
    public bool HasAnyFault()
    {
        InspectableItem[] allParts = GetComponentsInChildren<InspectableItem>(true);
        foreach (InspectableItem part in allParts)
        {
            if (part.isMainObject || part.isInventorySlot) continue;
            if (part.IsFaulty()) return true;
        }
        return false;
    }

    /// <summary>
    /// Returns descriptions of all current faults.
    /// </summary>
    public List<string> GetAllFaultDescriptions()
    {
        List<string> faults = new List<string>();
        InspectableItem[] allParts = GetComponentsInChildren<InspectableItem>(true);
        foreach (InspectableItem part in allParts)
        {
            if (part.isMainObject || part.isInventorySlot) continue;
            if (part.IsFaulty())
                faults.Add($"{part.itemName}: {part.faultDescription}");
        }
        return faults;
    }
}
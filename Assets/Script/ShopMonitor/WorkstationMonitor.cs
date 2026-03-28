using UnityEngine;

/// <summary>
/// WorkstationMonitor — Attach to each Monitor object next to a workstation.
///
/// The monitor can ONLY be used when the PC on the adjacent workstation is powered ON.
///
/// SETUP:
///   1. Add this script to your Monitor1, Monitor1 (2), etc. objects.
///   2. Drag the corresponding workstation's SlotData into "workstationSlot".
///   3. Tag the monitor's collider as "WorkstationMonitor" (create this tag).
///   4. Optionally assign screenOnVisual / screenOffVisual for visual feedback.
///   5. Assign the monitorOS (PCController) if this monitor runs an OS.
/// </summary>
public class WorkstationMonitor : MonoBehaviour
{
    [Header("Workstation Link")]
    [Tooltip("Drag the SlotData from the workstation next to this monitor.\n" +
             "The monitor checks this slot for a powered-on PC.")]
    public SlotData workstationSlot;

    [Header("Monitor Visuals")]
    [Tooltip("The screen GameObject that shows the desktop/OS.\n" +
             "Gets enabled when PC is powered on.")]
    public GameObject screenOnVisual;

    [Tooltip("Optional: A 'No Signal' or blank screen visual.\n" +
             "Shown when PC is off or no PC is present.")]
    public GameObject screenOffVisual;

    [Header("Monitor Interaction")]
    [Tooltip("The PCController (OS) that this monitor runs.\n" +
             "Drag the monitor's Canvas/PCController here.")]
    public PCController monitorOS;

    [Tooltip("Can the player currently interact with this monitor?")]
    public bool canInteract = false;

    // Internal tracking
    private PCPowerSystem linkedPC = null;
    private bool wasOn = false;

    void Start()
    {
        // Start with monitor off
        UpdateMonitorState();
    }

    void Update()
    {
        PCPowerSystem currentPC = FindLinkedPC();

        // Did the linked PC change?
        if (currentPC != linkedPC)
        {
            linkedPC = currentPC;
            UpdateMonitorState();
        }

        // Did the power state change?
        bool isOn = (linkedPC != null && linkedPC.isPoweredOn);
        if (isOn != wasOn)
        {
            wasOn = isOn;
            UpdateMonitorState();
        }
    }

    // =============================================
    //  FIND THE PC ON THE WORKSTATION
    // =============================================

    PCPowerSystem FindLinkedPC()
    {
        if (workstationSlot == null) return null;
        if (!workstationSlot.isOccupied || workstationSlot.currentItem == null)
            return null;

        // Check the item in the slot for PCPowerSystem
        PCPowerSystem power = workstationSlot.currentItem.GetComponent<PCPowerSystem>();
        if (power != null) return power;

        // Also check children
        return workstationSlot.currentItem.GetComponentInChildren<PCPowerSystem>();
    }

    // =============================================
    //  UPDATE MONITOR VISUALS
    // =============================================

    void UpdateMonitorState()
    {
        bool pcIsOn = (linkedPC != null && linkedPC.isPoweredOn);
        canInteract = pcIsOn;

        if (screenOnVisual != null)
            screenOnVisual.SetActive(pcIsOn);

        if (screenOffVisual != null)
            screenOffVisual.SetActive(!pcIsOn);

        // If PC turned off while player is using the monitor, close the OS
        if (!pcIsOn && monitorOS != null && monitorOS.gameObject.activeSelf)
        {
            monitorOS.PowerOff();
        }
    }

    // =============================================
    //  PUBLIC API
    // =============================================

    /// <summary>
    /// Returns true if the monitor can be interacted with.
    /// </summary>
    public bool CanUseMonitor()
    {
        return canInteract && linkedPC != null && linkedPC.isPoweredOn;
    }

    /// <summary>
    /// Returns a tooltip string explaining the monitor status.
    /// </summary>
    public string GetMonitorStatus()
    {
        if (workstationSlot == null || !workstationSlot.isOccupied)
            return "No PC on workstation";

        if (linkedPC == null)
            return "PC has no power system";

        if (!linkedPC.isPowerCordConnected)
            return "Plug in the power cord first";

        if (!linkedPC.isPoweredOn)
        {
            string fault = linkedPC.GetCriticalFaultReason();
            if (!string.IsNullOrEmpty(fault))
                return $"PC won't turn on: {fault}";
            return "PC is off — turn it on first";
        }

        return "Monitor ready";
    }
    /// <summary>
    /// Returns the PC currently linked to this monitor.
    /// </summary>
    public PCPowerSystem GetLinkedPC()
    {
        return linkedPC;
    }
}
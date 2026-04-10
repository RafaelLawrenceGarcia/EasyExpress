using UnityEngine.Experimental.Rendering;
using UnityEngine;

/// <summary>
/// WorkstationMonitor — Attach to each Monitor object next to a workstation.
///
/// Makes the monitor interactive ONLY when the PC on its linked
/// workstation slot is powered on (via PCPowerSystem).
///
/// SETUP:
///   1. Attach this to each Monitor object (Monitor1, Monitor1 (2), etc.)
///   2. Tag the monitor's collider as "WorkstationMonitor"
///   3. Put it on the interactable layer
///   4. Drag the matching workstation's SlotData into "linkedWorkstationSlot"
///   5. (Optional) Drag screen GameObjects for visual on/off/error states
/// </summary>
public class WorkstationMonitor : MonoBehaviour
{
    [Header("Workstation Link")]
    [Tooltip("The SlotData of the workstation this monitor is paired with.\n" +
             "The monitor checks this slot's currentItem for a PCPowerSystem.")]
    public SlotData linkedWorkstationSlot;

    [Header("Monitor Visuals")]
    [Tooltip("The screen mesh/panel that lights up when the PC is on.")]
    public GameObject monitorScreen;

    [Tooltip("(Optional) An error/BSOD screen shown when PC has faults.")]
    public GameObject errorScreen;

    [Tooltip("(Optional) A 'No Signal' screen shown when PC is off.")]
    public GameObject noSignalScreen;
    [Header("Monitor Emission")]
    [Tooltip("The renderer whose emission should glow when on.")]
    public Renderer screenRenderer;
    public Color onEmissionColor = new Color(0.5f, 0.8f, 1f) * 2f;
    public Color offEmissionColor = Color.black;
    [Header("Independent Prefab Setup")]
    public Camera localUICamera;     // This monitor's personal UI Camera
    public Canvas localCanvas;       // This monitor's personal UI Canvas
    public PCController localOS;
    // =============================================
    //  RUNTIME STATE
    // =============================================
    private PCPowerSystem currentPC = null;
    private bool wasOn = false;
    // Add this field to WorkstationMonitor.cs
    [Header("UI Bridge")]
    public MonitorShopBridge uiBridge;

    void Update()
    {
        currentPC = FindLinkedPC();

        bool isOn = (currentPC != null && currentPC.isPoweredOn);
        bool hasFaults = (currentPC != null && currentPC.HasAnyFault());

        if (isOn != wasOn)
        {
            UpdateMonitorVisuals(isOn, hasFaults);
            wasOn = isOn;
        }
        else if (isOn && errorScreen != null)
        {
            // Dynamically update error screen if faults are fixed while PC is on
            errorScreen.SetActive(currentPC.HasAnyFault());
        }

        // ═══════════════════════════════════════════════════════════
        //  FIX: The old code had an emission kill here that ran
        //  EVERY FRAME, overriding UpdateMonitorVisuals and keeping
        //  the monitor permanently black even when the PC was on.
        //  Removed — Awake() already initializes emission to black,
        //  and UpdateMonitorVisuals handles on/off state changes.
        // ═══════════════════════════════════════════════════════════
    }

    void Awake()
    {
        RenderTexture uniqueRT = new RenderTexture(1920, 1080, 0);
        uniqueRT.depthStencilFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.D24_UNorm_S8_UInt;

        // Clear the texture to black so it doesn't glow white when empty
        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = uniqueRT;
        GL.Clear(true, true, Color.black);
        RenderTexture.active = prev;

        if (localUICamera != null)
            localUICamera.targetTexture = uniqueRT;

        if (screenRenderer != null)
        {
            Material uniqueMat = new Material(screenRenderer.sharedMaterial);
            uniqueMat.SetTexture("_BaseMap", uniqueRT);
            uniqueMat.SetTexture("_EmissionMap", uniqueRT);
            screenRenderer.material = uniqueMat;

            // Start with emission off — UpdateMonitorVisuals will enable it
            uniqueMat.SetColor("_EmissionColor", Color.black);
        }
    }

    // =============================================
    //  VISUALS
    // =============================================

    void UpdateMonitorVisuals(bool isOn, bool hasFaults)
    {
        if (isOn)
        {
            if (monitorScreen != null) monitorScreen.SetActive(true);
            if (noSignalScreen != null) noSignalScreen.SetActive(false);
            if (errorScreen != null) errorScreen.SetActive(hasFaults);

            if (screenRenderer != null)
            {
                Material mat = screenRenderer.material;
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", onEmissionColor);
            }

            // ═══════════════════════════════════════════════════════
            //  Activate the PCController canvas so the local UI camera
            //  actually has content to render into the RenderTexture.
            //  The canvas MUST be ScreenSpaceCamera (not Overlay) so
            //  localUICamera can capture it into the RT.
            // ═══════════════════════════════════════════════════════
            if (localOS != null)
            {
                if (localCanvas != null)
                {
                    localCanvas.gameObject.SetActive(true);
                    // Force ScreenSpaceCamera so the RT camera can capture it.
                    // OpenWorkstationMonitor switches to Overlay for full-screen;
                    // CloseWorkstationMonitor restores this mode.
                    localCanvas.renderMode = RenderMode.ScreenSpaceCamera;
                    localCanvas.worldCamera = localUICamera;
                }
                localOS.gameObject.SetActive(true);

                // Show the right screen based on how the PC booted
                PCPowerSystem pc = GetLinkedPC();
                if (pc != null)
                {
                    switch (pc.lastPowerResult)
                    {
                        case PowerResult.NoDisplay:
                            localOS.ShowNoSignal();
                            break;
                        case PowerResult.FailedPOST:
                            localOS.ShowBSOD("POST_FAILURE",
                                "The system failed to complete POST.\n" +
                                "A critical hardware component may be\n" +
                                "defective or missing.");
                            break;
                        case PowerResult.CrashToBSOD:
                            localOS.StartBootThenCrash("SYSTEM_CRASH",
                                "A fatal error occurred after boot.\n" +
                                "This may be caused by improperly\n" +
                                "seated RAM or overheating.");
                            break;
                        case PowerResult.BootWithIssues:
                        case PowerResult.Success:
                            if (localOS.HasBootedToDesktop)
                                localOS.BootToOS();
                            else
                                localOS.StartBoot();
                            break;
                    }
                }
                else
                {
                    localOS.StartBoot();
                }
            }
        }
        else
        {
            if (monitorScreen != null) monitorScreen.SetActive(false);
            if (errorScreen != null) errorScreen.SetActive(false);

            // "No Signal" only if a PC exists but is off
            if (noSignalScreen != null)
                noSignalScreen.SetActive(currentPC != null);

            if (screenRenderer != null)
            {
                Material mat = screenRenderer.material;
                mat.SetColor("_EmissionColor", offEmissionColor);
            }

            // Turn off OS content when PC powers down
            if (localOS != null)
                localOS.PowerOff();
        }
    }

    // =============================================
    //  PC LOOKUP
    // =============================================

    PCPowerSystem FindLinkedPC()
    {
        if (linkedWorkstationSlot == null) return null;
        if (!linkedWorkstationSlot.isOccupied) return null;
        if (linkedWorkstationSlot.currentItem == null) return null;

        return linkedWorkstationSlot.currentItem.GetComponent<PCPowerSystem>();
    }

    // =============================================
    //  PUBLIC API (called by PlayerInteract)
    // =============================================

    /// <summary>
    /// Returns true if the monitor can be used (PC is powered on).
    /// </summary>
    public bool CanInteract()
    {
        return currentPC != null && currentPC.isPoweredOn;
    }

    /// <summary>
    /// Returns why the monitor can't be used.
    /// </summary>
    public string GetBlockedReason()
    {
        if (linkedWorkstationSlot == null)
            return "Monitor not connected to a workstation.";
        if (!linkedWorkstationSlot.isOccupied || linkedWorkstationSlot.currentItem == null)
            return "No PC on the workstation.";
        if (currentPC == null)
            return "PC has no power system.";
        if (!currentPC.isPowerCordConnected)
            return "No power cord connected.\nPlug into outlet AND PC.";
        if (!currentPC.isPoweredOn)
            return "PC is turned off.\nPress the power button in Inspect Mode.";
        return "";
    }

    /// <summary>
    /// Returns the PCPowerSystem of the linked PC (or null).
    /// </summary>
    public PCPowerSystem GetLinkedPC()
    {
        return currentPC;
    }
}
using UnityEngine;
using System.Collections.Generic;

public enum PowerResult
{
    Success,          // 0 — PC boots fully (least severe)
    BootWithIssues,   // 1 — Boots but has non-critical faults
    CrashToBSOD,      // 2 — Boots fully to desktop, then crashes to BSOD
    NoDisplay,        // 3 — Fans spin, no picture (GPU issue)
    FailedPOST,       // 4 — PC stays on, shows BSOD immediately
    NoPower           // 5 — Won't respond at all (most severe)
}

public class PCPowerSystem : MonoBehaviour
{
    [HideInInspector] public PowerResult lastPowerResult = PowerResult.Success;
    [HideInInspector] public string lastBSODCode = "";
    [HideInInspector] public string lastBSODMessage = "";

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

    // ═══════════════════════════════════════════════════════
    //  MONITOR BACKLIGHT
    //  Drag the monitor screen Renderer here. The screen
    //  shows a faint backlight glow when the PC is on but
    //  has no display signal (NoDisplay result).
    // ═══════════════════════════════════════════════════════
    [Header("Monitor Screen (Backlight Effect)")]
    [Tooltip("The Renderer on the monitor screen mesh/quad. " +
             "Leave empty if this PC case has no attached monitor.")]
    public Renderer monitorScreen;

    [Tooltip("Emission colour when the monitor is on but has no signal.\n" +
             "A very dark blue-gray simulates LCD backlight bleed.")]
    public Color noSignalEmission = new Color(0.015f, 0.015f, 0.025f);

    [Tooltip("Emission colour when the monitor displays a working desktop.")]
    public Color desktopEmission = new Color(0.25f, 0.28f, 0.35f);

    [Tooltip("Base colour when the monitor is completely off.")]
    public Color screenOffColor = Color.black;

    [HideInInspector] public bool skipStartReset = false;

    void Start()
    {
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

        // Auto-find monitor screen if not assigned
        if (monitorScreen == null)
        {
            foreach (Transform child in GetComponentsInChildren<Transform>(true))
            {
                string n = child.name.ToLower();
                if (n.Contains("screen") || n.Contains("display") || n.Contains("monitor_screen"))
                {
                    Renderer r = child.GetComponent<Renderer>();
                    if (r != null)
                    {
                        monitorScreen = r;
                        Debug.Log($"[PCPowerSystem] Auto-found monitor screen: {child.name}");
                        break;
                    }
                }
            }
        }

        if (skipStartReset) return;

        isPoweredOn = false;
        isPowerCordConnected = false;
        SetFansState(false);
        SetMonitorState(PowerResult.NoPower);

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
    // =============================================

    public void ConnectPowerCord(GameObject cord)
    {
        isPowerCordConnected = true;
        connectedCord = cord;
        Debug.Log($"[PCPowerSystem] Power cord delivering power to {gameObject.name}.");
    }

    public void DisconnectPowerCord()
    {
        bool wasOn = isPoweredOn;
        isPowerCordConnected = false;
        connectedCord = null;

        if (wasOn)
        {
            isPoweredOn = false;
            SetFansState(false);
            SetMonitorState(PowerResult.NoPower);
            Debug.Log($"[PCPowerSystem] Power lost — {gameObject.name} forced OFF.");
        }

        Debug.Log($"[PCPowerSystem] Power cord disconnected from {gameObject.name}.");
    }

    // =============================================
    //  POWER TOGGLE
    // =============================================

    public bool TryTogglePower(out string reason)
    {
        // --- TURNING OFF ---
        if (isPoweredOn)
        {
            isPoweredOn = false;
            SetFansState(false);
            SetMonitorState(PowerResult.NoPower);
            lastPowerResult = PowerResult.Success;
            reason = "PC powered off.";
            Debug.Log($"[PCPowerSystem] {gameObject.name} turned OFF.");
            return true;
        }

        // --- TURNING ON ---
        if (!isPowerCordConnected)
        {
            lastPowerResult = PowerResult.NoPower;
            reason = "No power!\nPlug the cord into both the outlet and the PC.";
            Debug.Log($"[PCPowerSystem] Cannot turn on — no power cord connected.");
            return false;
        }

        PowerResult result = EvaluatePowerState();
        lastPowerResult = result;

        switch (result)
        {
            case PowerResult.NoPower:
                reason = GetNoPowerReason();
                Debug.Log($"[PCPowerSystem] Cannot turn on — {reason}");
                return false;

            case PowerResult.CrashToBSOD:
                isPoweredOn = true;
                SetFansState(true);
                SetMonitorState(result);
                reason = GetCrashToBSODReason();
                Debug.Log($"[PCPowerSystem] {gameObject.name} ON — will crash to BSOD.");
                return true;

            case PowerResult.FailedPOST:
                isPoweredOn = true;
                SetFansState(true);
                SetMonitorState(result);
                reason = GetFailedPOSTReason();
                Debug.Log($"[PCPowerSystem] {gameObject.name} ON — POST failed.");
                StartCoroutine(DelayedShutdown(3.5f, "POST failure — auto shutdown."));
                return true;

            case PowerResult.NoDisplay:
                isPoweredOn = true;
                SetFansState(true);
                SetMonitorState(result);  // ← backlit black screen
                reason = GetNoDisplayReason();
                Debug.Log($"[PCPowerSystem] {gameObject.name} ON but no display.");
                return true;

            case PowerResult.BootWithIssues:
                isPoweredOn = true;
                SetFansState(true);
                SetMonitorState(result);
                reason = "PC powered on.\n⚠ Warning: Non-critical issues detected.";
                Debug.Log($"[PCPowerSystem] {gameObject.name} ON with issues.");
                return true;

            default: // Success
                isPoweredOn = true;
                SetFansState(true);
                SetMonitorState(result);
                reason = "PC powered on successfully!";
                Debug.Log($"[PCPowerSystem] {gameObject.name} turned ON.");
                return true;
        }
    }

    System.Collections.IEnumerator DelayedShutdown(float delay, string shutdownReason)
    {
        yield return new WaitForSeconds(delay);

        if (isPoweredOn)
        {
            isPoweredOn = false;
            SetFansState(false);
            SetMonitorState(PowerResult.NoPower);
            Debug.Log($"[PCPowerSystem] Auto-shutdown: {shutdownReason}");
        }
    }

    // =============================================
    //  MONITOR BACKLIGHT CONTROL
    // =============================================

    /// <summary>
    /// Sets the monitor screen material to match the current power state.
    /// - NoPower: screen completely dark (off)
    /// - NoDisplay: faint backlight glow (on but no signal)
    /// - Success/BootWithIssues: bright desktop emission
    /// - FailedPOST/CrashToBSOD: handled by WorkstationMonitor (BSOD)
    /// </summary>
    void SetMonitorState(PowerResult result)
    {
        if (monitorScreen == null) return;

        Material mat = monitorScreen.material;

        switch (result)
        {
            case PowerResult.NoPower:
                // Screen completely off — no glow at all
                mat.SetColor("_BaseColor", screenOffColor);
                mat.SetColor("_Color", screenOffColor);           // fallback for Standard
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", Color.black);
                break;

            case PowerResult.NoDisplay:
                // Screen is ON but no signal — faint dark backlight
                mat.SetColor("_BaseColor", screenOffColor);
                mat.SetColor("_Color", screenOffColor);
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", noSignalEmission);
                break;

            case PowerResult.FailedPOST:
            case PowerResult.CrashToBSOD:
                // These show BSOD — handled by WorkstationMonitor.
                // We just make the screen lit so it doesn't look dead.
                mat.SetColor("_BaseColor", screenOffColor);
                mat.SetColor("_Color", screenOffColor);
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", desktopEmission);
                break;

            case PowerResult.Success:
            case PowerResult.BootWithIssues:
                // Working display — bright backlight
                mat.SetColor("_BaseColor", screenOffColor);
                mat.SetColor("_Color", screenOffColor);
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", desktopEmission);
                break;
        }
    }

    // =============================================
    //  FAULT EVALUATION
    // =============================================

    PowerResult EvaluatePowerState()
    {
        InspectableItem[] allParts = GetComponentsInChildren<InspectableItem>(true);
        PowerResult worst = PowerResult.Success;

        bool hasRAM = false;
        bool hasGPU = false;
        bool hasPSU = false;
        bool hasMotherboard = false;
        bool hasCPU = false;

        foreach (InspectableItem part in allParts)
        {
            if (part.isMainObject || part.isInventorySlot) continue;

            switch (part.partCategory)
            {
                case "RAM": hasRAM = true; break;
                case "GPU": hasGPU = true; break;
                case "PSU": hasPSU = true; break;
                case "Motherboard": hasMotherboard = true; break;
                case "CPU": hasCPU = true; break;
            }
        }

        if (!hasPSU || !hasMotherboard || !hasCPU)
            return PowerResult.NoPower;

        if (!hasRAM)
            worst = WorstOf(worst, PowerResult.FailedPOST);

        if (!hasGPU)
            worst = WorstOf(worst, PowerResult.NoDisplay);

        foreach (InspectableItem part in allParts)
        {
            if (part.isMainObject || part.isInventorySlot) continue;
            if (part.fault == PartFault.None) continue;

            PowerResult partResult = EvaluatePartFault(part);
            worst = WorstOf(worst, partResult);

            if (worst == PowerResult.NoPower) return worst;
        }

        return worst;
    }

    PowerResult EvaluatePartFault(InspectableItem part)
    {
        if (part.fault == PartFault.None) return PowerResult.Success;
        if (part.fault == PartFault.Dusty) return PowerResult.Success;

        string cat = part.partCategory;

        // TIER 1: NO POWER
        if (cat == "PSU" && (part.fault == PartFault.Broken || part.fault == PartFault.Overloaded
                          || part.fault == PartFault.NotSeated || part.fault == PartFault.LooseConnection))
            return PowerResult.NoPower;
        if (cat == "Motherboard" && (part.fault == PartFault.Broken || part.fault == PartFault.LooseConnection))
            return PowerResult.NoPower;
        if (cat == "CPU" && (part.fault == PartFault.Broken || part.fault == PartFault.NotSeated))
            return PowerResult.NoPower;

        // TIER 2: FAILED POST
        if (cat == "RAM" && (part.fault == PartFault.Broken || part.fault == PartFault.Incompatible
                          || part.fault == PartFault.LooseConnection))
            return PowerResult.FailedPOST;
        if (cat == "Motherboard" && part.fault == PartFault.Corrupted)
            return PowerResult.FailedPOST;

        // TIER 2b: CRASH TO BSOD
        if (cat == "RAM" && part.fault == PartFault.NotSeated)
            return PowerResult.CrashToBSOD;
        if ((cat == "CPU" || cat == "Cooler") && part.fault == PartFault.Overheating)
            return PowerResult.CrashToBSOD;
        if (cat == "GPU" && (part.fault == PartFault.Overheating || part.fault == PartFault.Corrupted))
            return PowerResult.CrashToBSOD;
        if (cat == "Storage" && (part.fault == PartFault.Broken || part.fault == PartFault.Corrupted))
            return PowerResult.CrashToBSOD;
        if ((cat == "Cooler" || cat == "Fan") && part.fault == PartFault.Broken)
            return PowerResult.CrashToBSOD; // thermal failure

        // TIER 3: NO DISPLAY
        if (cat == "GPU" && (part.fault == PartFault.Broken || part.fault == PartFault.NotSeated
                          || part.fault == PartFault.LooseConnection))
            return PowerResult.NoDisplay;

        // Anything else is cosmetic / non-critical
        return PowerResult.BootWithIssues;
    }
    PowerResult WorstOf(PowerResult a, PowerResult b)
    {
        return (int)a < (int)b ? b : a;
    }

    // ── REASON STRING HELPERS ──

    string GetNoPowerReason()
    {
        InspectableItem[] allParts = GetComponentsInChildren<InspectableItem>(true);
        foreach (InspectableItem part in allParts)
        {
            if (part.isMainObject) continue;
            if (part.isInventorySlot)
            {
                if (part.partCategory == "PSU") return "PSU is missing!\nInstall a power supply first.";
                if (part.partCategory == "Motherboard") return "Motherboard is missing!\nInstall a motherboard first.";
                if (part.partCategory == "CPU") return "CPU is missing!\nInstall a processor first.";
            }
            if (part.partCategory == "PSU" && part.fault == PartFault.Broken)
                return "PSU is dead!\nNo power output detected.";
            if (part.partCategory == "PSU" && part.fault == PartFault.Overloaded)
                return "PSU is overloaded!\nWattage too low for this build.";
            if (part.partCategory == "Motherboard" && part.fault == PartFault.Broken)
                return "Motherboard is dead!\nNo response from the board.";
            if (part.partCategory == "Motherboard" && part.fault == PartFault.LooseConnection)
                return "24-pin ATX cable loose!\nNo power reaching the board.";
            if (part.partCategory == "CPU" && part.fault == PartFault.Broken)
                return "CPU is dead!\nThe system won't respond.";
        }
        return "PC won't turn on — critical component failure.";
    }

    string GetFailedPOSTReason()
    {
        bool hasRAM = false;
        InspectableItem[] allParts = GetComponentsInChildren<InspectableItem>(true);
        foreach (InspectableItem part in allParts)
        {
            if (part.isMainObject || part.isInventorySlot) continue;
            if (part.partCategory == "RAM") { hasRAM = true; break; }
        }

        if (!hasRAM)
            return "No RAM installed!\nFans spin briefly, then PC shuts off.";

        foreach (InspectableItem part in allParts)
        {
            if (part.isMainObject || part.isInventorySlot) continue;
            if (part.partCategory == "RAM" && part.fault == PartFault.Broken)
                return "RAM is faulty!\nPC beeps and shuts off after a few seconds.";
            if (part.partCategory == "RAM" && part.fault == PartFault.NotSeated)
                return "RAM not seated properly!\nPOST fails — reseat the sticks.";
            if (part.partCategory == "RAM" && part.fault == PartFault.Incompatible)
                return "Incompatible RAM!\nWrong type for this motherboard — POST fails.";
            if (part.partCategory == "Motherboard" && part.fault == PartFault.Corrupted)
                return "BIOS corrupted!\nPC turns on but can't POST.";
        }
        return "POST failed — system powers on briefly then shuts off.";
    }

    string GetCrashToBSODReason()
    {
        InspectableItem[] allParts = GetComponentsInChildren<InspectableItem>(true);
        foreach (InspectableItem part in allParts)
        {
            if (part.isMainObject || part.isInventorySlot) continue;
            if (part.partCategory == "RAM" && part.fault == PartFault.NotSeated)
                return "RAM not fully seated!\nPC will boot then crash — reseat the sticks.";
            if (part.fault == PartFault.Overheating)
                return "Overheating detected!\nPC will boot then crash due to thermal failure.";
        }
        return "A critical fault will cause the PC to crash after booting.";
    }

    string GetNoDisplayReason()
    {
        bool hasGPU = false;
        InspectableItem[] allParts = GetComponentsInChildren<InspectableItem>(true);
        foreach (InspectableItem part in allParts)
        {
            if (part.isMainObject || part.isInventorySlot) continue;
            if (part.partCategory == "GPU") { hasGPU = true; break; }
        }

        if (!hasGPU)
            return "No GPU installed!\nFans spin, system runs, but no display output.";

        foreach (InspectableItem part in allParts)
        {
            if (part.isMainObject || part.isInventorySlot) continue;
            if (part.partCategory == "GPU" && part.fault == PartFault.Broken)
                return "GPU is dead!\nPC runs but no display — replace the graphics card.";
            if (part.partCategory == "GPU" && part.fault == PartFault.NotSeated)
                return "GPU not seated properly!\nNo display — reseat the card.";
        }
        return "No display output — check GPU.";
    }

    // =============================================
    //  FAN CONTROL
    // =============================================

    void SetFansState(bool on)
    {
        foreach (PCFanController fan in GetComponentsInChildren<PCFanController>(true))
        {
            fan.enabled = on;

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
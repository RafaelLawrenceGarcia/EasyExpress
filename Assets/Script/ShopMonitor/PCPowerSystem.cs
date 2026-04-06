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
public enum PowerResult
{
    Success,          // 0 — PC boots fully (least severe)
    BootWithIssues,   // 1 — Boots but has non-critical faults
    NoDisplay,        // 2 — Fans spin, no picture (GPU issue)
    FailedPOST,       // 3 — Fans spin briefly then shuts off (RAM/BIOS)
    NoPower           // 4 — Won't respond at all (most severe)
}
public class PCPowerSystem : MonoBehaviour
{
    [HideInInspector] public PowerResult lastPowerResult = PowerResult.Success;
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
    // ── NEW ──────────────────────────────────────────
    /// <summary>
    /// Attempts to toggle the PC on/off.
    /// Returns true if the button press resulted in a state change.
    /// 'reason' is filled with a user-friendly message.
    /// </summary>
    public bool TryTogglePower(out string reason)
    {
        // --- TURNING OFF (always allowed) ---
        if (isPoweredOn)
        {
            isPoweredOn = false;
            SetFansState(false);
            lastPowerResult = PowerResult.Success;
            reason = "PC powered off.";
            Debug.Log($"[PCPowerSystem] {gameObject.name} turned OFF.");
            return true;
        }

        // --- TURNING ON: CHECK REQUIREMENTS ---

        // 1. Power cord must be fully connected (outlet + PC)
        if (!isPowerCordConnected)
        {
            lastPowerResult = PowerResult.NoPower;
            reason = "No power!\nPlug the cord into both the outlet and the PC.";
            Debug.Log($"[PCPowerSystem] Cannot turn on — no power cord connected.");
            return false;
        }

        // 2. Evaluate all faults with the tiered system
        PowerResult result = EvaluatePowerState();
        lastPowerResult = result;

        switch (result)
        {
            case PowerResult.NoPower:
                reason = GetNoPowerReason();
                Debug.Log($"[PCPowerSystem] Cannot turn on — {reason}");
                return false;

            case PowerResult.FailedPOST:
                // PC turns on briefly (fans spin) then shuts off after a few seconds
                isPoweredOn = true;
                SetFansState(true);
                reason = GetFailedPOSTReason();
                StartCoroutine(DelayedShutdown(3f, "POST failed — system shut down automatically."));
                Debug.Log($"[PCPowerSystem] POST failed — auto-shutdown in 3s.");
                return true;

            case PowerResult.NoDisplay:
                // PC stays on (fans spin) but no display — GPU issue
                isPoweredOn = true;
                SetFansState(true);
                reason = GetNoDisplayReason();
                Debug.Log($"[PCPowerSystem] {gameObject.name} ON but no display.");
                return true;

            case PowerResult.BootWithIssues:
                // PC boots but has non-critical issues
                isPoweredOn = true;
                SetFansState(true);
                reason = "PC powered on.\n⚠ Warning: Non-critical issues detected.";
                Debug.Log($"[PCPowerSystem] {gameObject.name} ON with issues.");
                return true;

            default: // PowerResult.Success
                isPoweredOn = true;
                SetFansState(true);
                reason = "PC powered on successfully!";
                Debug.Log($"[PCPowerSystem] {gameObject.name} turned ON.");
                return true;
        }
    }

    /// <summary>
    /// Coroutine: Fans spin for a few seconds, then the PC shuts itself off.
    /// Simulates a failed POST — the motherboard detects bad RAM / corrupted BIOS.
    /// </summary>
    System.Collections.IEnumerator DelayedShutdown(float delay, string shutdownReason)
    {
        yield return new WaitForSeconds(delay);

        if (isPoweredOn)
        {
            isPoweredOn = false;
            SetFansState(false);
            Debug.Log($"[PCPowerSystem] Auto-shutdown: {shutdownReason}");
        }
    }

    /// <summary>
    /// Evaluates all parts and returns the worst power result tier.
    /// Priority: NoPower > FailedPOST > NoDisplay > BootWithIssues > Success
    /// </summary>
    PowerResult EvaluatePowerState()
    {
        InspectableItem[] allParts = GetComponentsInChildren<InspectableItem>(true);
        PowerResult worst = PowerResult.Success;

        // ── PASS 1: Count installed (non-ghost) parts per category ──
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

        // ── PASS 2: Check for missing essentials ──
        if (!hasPSU || !hasMotherboard || !hasCPU)
            return PowerResult.NoPower;

        if (!hasRAM)
            worst = WorstOf(worst, PowerResult.FailedPOST);

        if (!hasGPU)
            worst = WorstOf(worst, PowerResult.NoDisplay);

        // ── PASS 3: Check faults on installed parts ──
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

        string cat = part.partCategory;

        // ── TIER 1: NO POWER (won't respond at all) ──
        if (cat == "PSU" && (part.fault == PartFault.Broken || part.fault == PartFault.Overloaded))
            return PowerResult.NoPower;
        if (cat == "Motherboard" && (part.fault == PartFault.Broken || part.fault == PartFault.LooseConnection))
            return PowerResult.NoPower;
        if (cat == "CPU" && part.fault == PartFault.Broken)
            return PowerResult.NoPower;

        // ── TIER 2: FAILED POST (turns on, beeps, shuts off ~3s) ──
        if (cat == "RAM" && (part.fault == PartFault.Broken || part.fault == PartFault.NotSeated
                             || part.fault == PartFault.Incompatible))
            return PowerResult.FailedPOST;
        if (cat == "Motherboard" && part.fault == PartFault.Corrupted)
            return PowerResult.FailedPOST;

        // ── TIER 3: NO DISPLAY (fans spin, no picture) ──
        if (cat == "GPU" && (part.fault == PartFault.Broken || part.fault == PartFault.NotSeated))
            return PowerResult.NoDisplay;

        // ── TIER 4: BOOTS WITH ISSUES ──
        // Overheating, dusty, outdated, wrong slot — PC works but has symptoms
        return PowerResult.BootWithIssues;
    }

    PowerResult WorstOf(PowerResult a, PowerResult b)
    {
        // Lower enum value = more severe
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
        // Check if RAM is truly missing (no installed RAM at all)
        bool hasRAM = false;
        InspectableItem[] allParts = GetComponentsInChildren<InspectableItem>(true);
        foreach (InspectableItem part in allParts)
        {
            if (part.isMainObject || part.isInventorySlot) continue;
            if (part.partCategory == "RAM") { hasRAM = true; break; }
        }

        if (!hasRAM)
            return "No RAM installed!\nFans spin briefly, then PC shuts off.";

        // Check for faults on installed RAM
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
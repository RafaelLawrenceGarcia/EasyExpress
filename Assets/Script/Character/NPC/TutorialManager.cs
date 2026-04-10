using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum DiagState
{
    // RAM swap (first)
    RemoveOldRAM, InstallNewRAM, TestAfterRAMSwap, DisconnectGPUWire,
    // GPU swap (second, after checking manual for No Display)
    RemoveOldGPU, InstallNewGPU, TestAfterGPUSwap,
    // RAM verify (put old RAM back to confirm it was broken)
    RemoveNewRAM, InstallOldRAM, TestWithOldRAM,
    // Final fix (put new RAM back)
    RemoveOldRAMAgain, InstallNewRAMFinal, TestFinal,
    Done
}

public partial class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance;
    private const int STEP_DONE = 50;
    [HideInInspector] public bool waitingForCutscene = false;

    [Header("Managers")]
    public IntroDialogueManager dialogueManager;
    public DayTransitionManager dayTransitionManager;
    public PCPartDatabase partDatabase;

    // NEW
    [Header("Highlight System")]
    public TutorialHighlighter tutorialHighlighter;

    [Tooltip("Highlights parts inside inspection clone AND world objects (outlet, power button) with tutorial colour.")]
    public TutorialInspectionHighlight tutorialInspectionHighlight;

    [Header("Scene Targets (drag from Hierarchy)")]
    public Transform workstationTarget;
    public Transform cashierTarget;
    public Transform cashierPCTarget;
    public Transform shopPCTarget;
    public Transform storageShelfTarget;
    public Transform emailTarget;
    public Transform doorTarget;

    [Header("WASD Settings")]
    public float requiredHoldTime = 2.0f;

    [Header("Tutorial Stipend")]
    public float tutorialStipend = 15000f;

    [Header("Tutorial Stock")]
    [Tooltip("How many of each shop item to pre-stock for the tutorial.")]
    public int tutorialStockPerItem = 2;

    [Header("Debug")]
    [Tooltip("Hold this key to fast-forward through dialogues.")]
    public KeyCode debugSkipKey = KeyCode.F9;

    // ─── Dialogue Sequences ──────────────────────────────────────

    [Header("Dialogue — Phase 1: Movement & Customer")]
    public DialogueSequence dlg_Intro;
    public DialogueSequence dlg_GoCashier;
    public DialogueSequence dlg_TalkCustomer;

    [Header("Dialogue — Phase 2: PC Summary & Intake")]
    [Tooltip("Teaches player to press G for PC Summary while inspecting desk PC.")]
    public DialogueSequence dlg_PCSummaryIntro;
    public DialogueSequence dlg_PickupBox;
    public DialogueSequence dlg_PlaceBox;

    [Header("Dialogue — Phase 3: Diagnosis")]
    public DialogueSequence dlg_InspectPC;
    [Tooltip("Connect the power cord.")]
    public DialogueSequence dlg_ConnectCord;
    public DialogueSequence dlg_PowerTest;

    [Header("Dialogue — Phase 3b: Repair Manual")]
    [Tooltip("After first power test — tells player to check the Repair Manual (F key).")]
    public DialogueSequence dlg_CheckManual;
    [Tooltip("After player opens and closes the manual — follow the steps.")]
    public DialogueSequence dlg_ManualChecked;

    [Header("Dialogue — Phase 3c: Open Case")]
    [Tooltip("Open the case (screws + panel).")]
    public DialogueSequence dlg_OpenCase;

    [Header("Dialogue — Phase 3d: Swap-and-Test (RAM first)")]
    [Tooltip("Start with RAM based on manual guidance.")]
    public DialogueSequence dlg_SwapRAMFirst;
    [Tooltip("RAM swap result: boots past POST but no display (GPU still bad).")]
    public DialogueSequence dlg_RAMSwapNoDisplay;
    [Tooltip("Check manual for No Display problem.")]
    public DialogueSequence dlg_CheckManualNoDisplay;

    [Header("Dialogue — Phase 3e: Swap GPU")]
    [Tooltip("Swap GPU based on manual's No Display procedure.")]
    public DialogueSequence dlg_SwapGPUNext;
    [Tooltip("GPU swap success — both parts working!")]
    public DialogueSequence dlg_GPUSwapSuccess;

    [Header("Dialogue — Phase 3f: Verify RAM")]
    [Tooltip("Put old RAM back to confirm it was broken.")]
    public DialogueSequence dlg_VerifyOldRAM;
    [Tooltip("POST failed — old RAM confirmed broken.")]
    public DialogueSequence dlg_RAMVerifiedBroken;

    [Header("Dialogue — Phase 3g: Repair Complete")]
    [Tooltip("All fixed — exit inspection and submit via email.")]
    public DialogueSequence dlg_RepairComplete;

    [Header("Dialogue — Phase 4: Email & Shop")]
    public DialogueSequence dlg_EmailTutorial;
    [Tooltip("Played AFTER player exits the monitor, not immediately on email complete.")]
    public DialogueSequence dlg_AfterEmailSubmit;
    public DialogueSequence dlg_ShopTutorial;
    [Tooltip("Final welcome — mentions Manual, Summary, and key tools.")]
    public DialogueSequence dlg_FinalWelcome;

    // ─── Runtime State ───────────────────────────────────────────

    private int step = 0;
    private float wTimer, aTimer, sTimer, dTimer;
    private bool wDone, aDone, sDone, dDone;
    private bool playerReachedCashier, customerReachedCashier;
    private int screwsRemoved, totalScrewsOnPanel;
    private bool panelRemoved;
    private DiagState diagState = DiagState.RemoveOldRAM;
    private bool grabbedGPU, grabbedRAM;
    private bool shopBrowsed, shopAddedToCart;
    private bool boxPlacedEarly;

    // NEW: PC Summary teaching state
    private bool summaryShown = false;
    private bool summaryDialoguePlayed = false;

    // NEW: Manual teaching state
    private bool manualOpened = false;
    private bool manualNoDisplayOpened = false;
    private bool waitingForManualOpen = false;
    private bool waitingForManualNoDisplay = false;

    // NEW: Monitor-exit dialogue fix
    private bool pendingEmailCompleteDialogue = false;
    private bool pendingShopDialogue = false;

    // ─── Lifecycle ───────────────────────────────────────────────

    void Awake()
    {
        Instance = this;
        bool done = PlayerPrefs.GetInt("TutorialDone", 0) == 1;
        bool loading = PlayerPrefs.GetInt("IsLoadingGame", 0) == 1;
        if (!done && !loading) waitingForCutscene = true;
    }
    void OnEnable() { DayTransitionManager.OnNewDayStarted += OnDayStarted; }
    void OnDisable() { DayTransitionManager.OnNewDayStarted -= OnDayStarted; }

    void Start()
    {
        bool done = PlayerPrefs.GetInt("TutorialDone", 0) == 1;
        bool loading = PlayerPrefs.GetInt("IsLoadingGame", 0) == 1;
        if (done || loading)
        {
            step = STEP_DONE;
            HideArrow();
        }
        else step = 0;
    }
    void OnDayStarted(int day)
    {
        if (step == 0 && day == 1 && !waitingForCutscene)
            StartCoroutine(DelayedStart());
    }

    public void OnCutsceneComplete()
    {
        waitingForCutscene = false;
        if (step == 0) StartCoroutine(DelayedStart());
    }

    void Update()
    {
        if (step == 1) UpdateWASD();

        // Manual open detection (step 13b: waiting for F key)
        if (waitingForManualOpen && Input.GetKeyDown(KeyCode.F))
        {
            // RepairManual.ToggleManual() handles the actual open
            // We just detect the key press
        }

        // DEBUG: Hold F9 to fast-skip dialogues
        if (Input.GetKey(debugSkipKey) && dialogueManager != null && dialogueManager.isDialogueActive)
            dialogueManager.SendMessage("NextLine", SendMessageOptions.DontRequireReceiver);
    }

    // ─── Public Queries ──────────────────────────────────────────

    public int GetCurrentStep() => step;
    public bool IsTutorialActive() => PlayerPrefs.GetInt("TutorialDone", 0) != 1
                                         && PlayerPrefs.GetInt("IsLoadingGame", 0) != 1
                                         && step < STEP_DONE;
    public bool IsRepairStep() => step >= 11 && step <= 24;
    public bool IsShopPCStep() => step == 26;
    public bool IsCashierPCStep() => step == 5 || step == 6;
    public bool IsInstallComponentStep() => step == 16
        && (diagState == DiagState.InstallNewRAM
         || diagState == DiagState.InstallNewGPU
         || diagState == DiagState.InstallOldRAM
         || diagState == DiagState.InstallNewRAMFinal);
    public bool IsEndDayStep() => false;
    public bool IsEmailStep() => step == 25;
    public bool IsInspectPCAllowed() => true;
    public bool IsDiagnosingPC() => step == 16;
    public DiagState GetDiagState() => diagState;

    public void HideTaskTemporarily() { TaskListUI.Instance?.EnterMonitorMode(); }
    public void RestoreTaskIfNeeded() { TaskListUI.Instance?.ExitMonitorMode(); }

    // ═══════════════════════════════════════════════════════════
    //  PUBLIC COMPLETIONS
    // ═══════════════════════════════════════════════════════════

    public void CompleteGoToCashierTask()
    {
        if (step != 3 || playerReachedCashier) return;
        playerReachedCashier = true;
        TaskListUI.Instance?.CompleteTask(0);
        TryAdvanceCashier();
    }

    public void NotifyCustomerArrivedAtCashier()
    {
        if (step != 3 || customerReachedCashier) return;
        customerReachedCashier = true;
        TaskListUI.Instance?.CompleteTask(1);
        TryAdvanceCashier();
    }

    public void CompleteCashierInspectTask()
    {
        if (step != 5) return;

        TaskListUI.Instance?.CompleteTask(0);
        TaskListUI.Instance?.CompleteTask(1);

        HideArrow(); // remove outline before inspection takes over renderers
        // Play the PC Summary intro dialogue ONLY when the player
        // actually enters inspection, and only once
        if (!summaryDialoguePlayed)
        {
            summaryDialoguePlayed = true;
            Dialogue(0.5f, dlg_PCSummaryIntro, null);
        }
    }

    public void CompleteApproachCashierPCTask()
    {
        if (step == 5) TaskListUI.Instance?.CompleteTask(0);
    }

    // NEW: Called when player presses G during desk PC inspection (step 5)
    public void NotifyPCSummaryOpened()
    {
        if (step == 5 && !summaryShown)
        {
            summaryShown = true;
            TaskListUI.Instance?.CompleteTask(2);
        }
    }

    public void ForceAcceptCurrentCustomer()
    {
        if (step != 6) return;
        foreach (CustomerInside c in FindObjectsOfType<CustomerInside>())
            if (c.isAtSpot) { c.AcceptJob(); break; }
        CompleteCashierPCTask();
    }

    public void CompleteCashierPCTask()
    {
        if (step != 6) return;
        CompleteAllTasks(); HideArrow();
        step = 7;
        Dialogue(1.0f, dlg_PickupBox, StartStep_PickupBox);
    }

    public void CompleteCustomerTask() => CompleteCashierPCTask();

    public void CompletePickupBoxTask()
    {
        if (step != 7) return;
        CompleteAllTasks(); HideArrow();
        step = 8;
        Dialogue(1.0f, dlg_PlaceBox, StartStep_PlaceBox);
    }

    public void CompleteApproachWorkstationTask()
    {
        if (step == 9) TaskListUI.Instance?.CompleteTask(0);
        if (step == 15) TaskListUI.Instance?.CompleteTask(0);
    }

    public void CompletePlaceBoxTask()
    {
        if (step == 8)
        {
            boxPlacedEarly = true;
            return;
        }
        if (step != 9) return;
        CompleteAllTasks(); HideArrow();
        step = 10;
        Dialogue(1.0f, dlg_InspectPC, StartStep_InspectPC);
    }

    public void CompleteApproachShopPCTask()
    {
        if (step == 26) TaskListUI.Instance?.CompleteTask(0);
    }

    // ═══════════════════════════════════════════════════════════
    //  FIX 1: Guard OnReenteredInspect against null clone
    //  CompletePCTask is called by PlayerInteract BEFORE Inspect()
    //  runs, so im.currentClone may not exist yet.
    //  NotifyInspectionCloneReady (called at the end of Inspect)
    //  will handle initialization when the clone IS ready.
    // ═══════════════════════════════════════════════════════════
    public void CompletePCTask()
    {
        if (step == 11)
        {
            CompleteAllTasks(); HideArrow();
            step = 12;
            Dialogue(0.5f, dlg_ConnectCord, StartStep_ConnectPowerCord);
            return;
        }
        if (step == 15 && !panelRemoved)
        {
            // Only run OnReenteredInspect if the clone already exists.
            // If Inspect() hasn't created the clone yet, NotifyInspectionCloneReady
            // will handle step-15 setup once the clone is ready.
            InspectionManager im = FindFirstObjectByType<InspectionManager>();
            if (im != null && im.currentClone != null)
                OnReenteredInspect();
            return;
        }
    }

    public void NotifyPowerCordConnected()
    {
        if (step != 12) return;
        tutorialInspectionHighlight?.Hide(); // clear outlet highlight
        if (TutorialWorldHighlight.Instance != null)
            TutorialWorldHighlight.Instance.Hide();
        TaskListUI.Instance?.CompleteTask(0);
        TaskListUI.Instance?.CompleteTask(1);
        HideArrow();
        Dialogue(0.5f, dlg_PowerTest, StartStep_PowerOnTest);
    }

    public void CompleteHoverTask() { }

    public void CompleteRemoveTask(InspectableItem removedPart)
    {
        if (step != 15 && step != 16) return;
        HandlePartRemoval(removedPart);
    }

    public void OnPlayerExitedInspection()
    {
        HideArrow();
        tutorialInspectionHighlight?.Hide(); // clear inspection highlight on exit
        if (step == 5)
        {
            // Only advance if summary was shown during inspection
            if (summaryShown)
            {
                CompleteAllTasks();
                step = 6;
                // FIX: Go straight to the task — dlg_TalkCustomer already
                // played at step 4 and doesn't need to repeat.
                StartStep_TalkToCustomer();
            }
            else
            {
                // Player exited without pressing G — remind them
                SetTask("TASK",
                    "Press [E] on the PC to inspect it again",
                    "Press [G] to open the PC Summary Panel",
                    "Press [Esc] to exit when done");
                HideArrow();
                StartCoroutine(ShowArrowForType(TutorialTarget.TargetType.CashierPC));
                // FIX: Do NOT reset summaryDialoguePlayed here.
                // The PC Summary intro dialogue should only play once.
                // Resetting it caused the dialogue to replay every re-entry.
            }
            return;
        }
        if (step == 14) { OnExitInspectForStorage(); return; }
        if (step == 24)
        {
            TaskListUI.Instance?.CompleteTask(0);
            step = 25;
            Dialogue(0.5f, dlg_EmailTutorial, StartStep_EmailTutorial);
            return;
        }
    }

    // NEW: Called by PlayerInteract.CloseWorkstationMonitor()
    public void NotifyMonitorClosed()
    {
        // Exit monitor mode — restores task panel position & sort order
        TaskListUI.Instance?.ExitMonitorMode();

        // Clear any button highlight ring on the monitor
        if (MonitorButtonHighlight.Instance != null)
            MonitorButtonHighlight.Instance.ClearHighlight();

        // Stop any active UI guide arrow
        StopUIGuide();

        if (pendingEmailCompleteDialogue)
        {
            pendingEmailCompleteDialogue = false;
            step = 26;
            Dialogue(0.8f, dlg_AfterEmailSubmit, StartStep_ShopTutorial);
        }
    }

    public void OnPCPowerToggled(bool poweredOn)
    {
        if (step == 13 && poweredOn)
        {
            TaskListUI.Instance?.CompleteTask(0);
            StartCoroutine(DelayThenAdvanceFromFirstPower());
            return;
        }
        if (step == 16)
        {
            // Complete "Turn off the PC" task when player powers off during removal steps
            bool isRemovalState = diagState == DiagState.RemoveOldRAM
                               || diagState == DiagState.RemoveOldGPU
                               || diagState == DiagState.RemoveNewRAM
                               || diagState == DiagState.RemoveOldRAMAgain;
            if (!poweredOn && isRemovalState)
            {
                TaskListUI.Instance?.CompleteTask(0);
                // PC is now off — switch highlight from power button to the part
                RefreshInspectionHighlight();
                return;
            }

            bool isTestState = diagState == DiagState.TestAfterRAMSwap
                            || diagState == DiagState.TestAfterGPUSwap
                            || diagState == DiagState.TestWithOldRAM
                            || diagState == DiagState.TestFinal;
            if (poweredOn && isTestState) HandleDiagPowerTest();
            return;
        }
    }

    // NEW: Called by RepairManual when opened/closed
    public void NotifyManualOpened()
    {
        if (waitingForManualOpen && !manualOpened)
        {
            manualOpened = true;
        }
    }

    public void NotifyManualClosed()
    {
        // Step 13b: First time checking manual after failed POST
        if (waitingForManualOpen && manualOpened)
        {
            waitingForManualOpen = false;
            TaskListUI.Instance?.CompleteTask(0);
            TaskListUI.Instance?.CompleteTask(1);
            Dialogue(0.5f, dlg_ManualChecked, StartStep_ExitForStorage);
        }

        // Step 16: Checking manual for No Display after RAM swap
        if (waitingForManualNoDisplay && !manualNoDisplayOpened)
        {
            manualNoDisplayOpened = true;
            waitingForManualNoDisplay = false;
            TaskListUI.Instance?.CompleteTask(0);
            Dialogue(0.5f, dlg_CheckManualNoDisplay, () =>
            {
                diagState = DiagState.DisconnectGPUWire;  // ← was RemoveOldGPU
                ShowSwapTask();
            });
        }
    }

    // ─── Storage (Step 14) ───────────────────────────────────────

    public void NotifyStoragePartGrabbed(string partCategory)
    {
        if (step != 14) return;
        string cat = (partCategory ?? "").Trim().ToUpper();
        if (cat == "GPU" && !grabbedGPU) { grabbedGPU = true; TaskListUI.Instance?.CompleteTask(3); }
        else if (cat == "RAM" && !grabbedRAM) { grabbedRAM = true; TaskListUI.Instance?.CompleteTask(4); }
    }

    public void CompleteApproachStorageShelfTask()
    {
        if (step == 14) TaskListUI.Instance?.CompleteTask(1);
    }

    public void NotifyStorageShelfOpened()
    {
        if (step == 14) TaskListUI.Instance?.CompleteTask(2);
    }

    public void CompleteStorageShelfTask()
    {
        if (step != 14) return;
        if (!grabbedGPU || !grabbedRAM) { Debug.Log("[Tutorial] Storage closed but not all parts grabbed."); return; }
        OnStoragePartsGrabbed();
    }

    // ─── Install (Step 16) ───────────────────────────────────────

    public void CompleteInstallComponentTask()
    {
        if (step == 16) { HandlePartInstall_SwapTest(); return; }
    }

    public void CompleteAssemblyTask() => CompleteInstallComponentTask();

    // ─── Email (Step 25) ─────────────────────────────────────────

    public void CompleteApproachEmailTask()
    {
        if (step == 25)
        {
            TaskListUI.Instance?.CompleteTask(0);
            TaskListUI.Instance?.CompleteTask(1);
        }
    }

    public void CompleteEmailTask()
    {
        if (step != 25) return;
        Debug.Log("[Tutorial] Email job marked complete → will advance after monitor closes.");
        CompleteAllTasks(); HideArrow(); StopUIGuide();

        // DON'T start dialogue immediately — player is still in the monitor.
        // Set a flag so NotifyMonitorClosed() triggers it.
        pendingEmailCompleteDialogue = true;
    }

    // ─── Shop (Step 26) ──────────────────────────────────────────

    public void NotifyShopCategoryBrowsed()
    {
        if (step != 26) return;
        if (!shopBrowsed) { shopBrowsed = true; TaskListUI.Instance?.CompleteTask(1); }
    }

    public void NotifyShopItemAddedToCart()
    {
        if (step != 26) return;
        uiGuideItemAdded = true;
        if (!shopAddedToCart) { shopAddedToCart = true; TaskListUI.Instance?.CompleteTask(2); }
    }

    public void CompleteOrderPartsTask()
    {
        if (step == 26) CompleteShopTutorial();
    }

    public void CompleteApproachDoorTask() { }
    public void CompleteEndDayTask() { }
    public void CompletePickupDeliveryTask() { }

    // ─── Restart ─────────────────────────────────────────────────

    public void RestartTutorial()
    {
        tutorialInspectionHighlight?.Hide();
        if (TutorialWorldHighlight.Instance != null)
            TutorialWorldHighlight.Instance.Hide();
        if (MonitorButtonHighlight.Instance != null)
            MonitorButtonHighlight.Instance.ClearHighlight();
        TaskListUI.Instance?.ExitMonitorMode();
        step = 0;
        wTimer = aTimer = sTimer = dTimer = 0f;
        wDone = aDone = sDone = dDone = false;
        playerReachedCashier = customerReachedCashier = false;
        screwsRemoved = totalScrewsOnPanel = 0;
        panelRemoved = false;
        diagState = DiagState.RemoveOldRAM;
        grabbedGPU = grabbedRAM = false;
        shopBrowsed = shopAddedToCart = false;
        boxPlacedEarly = false;
        summaryShown = false;
        summaryDialoguePlayed = false;
        manualOpened = false;
        manualNoDisplayOpened = false;
        waitingForManualOpen = false;
        waitingForManualNoDisplay = false;
        pendingEmailCompleteDialogue = false;
        pendingShopDialogue = false;
        StopUIGuide();
        uiGuideItemAdded = false;
        HideArrow(); HidePointer();
        StartCoroutine(DelayedStart());
    }

    // ─── Utility ─────────────────────────────────────────────────

    void ShowArrow(Transform target) { tutorialHighlighter?.ShowAt(target); }
    void HideArrow() { tutorialHighlighter?.Hide(); }
    void HidePointer() { if (TutorialUIPointer.Instance != null) TutorialUIPointer.Instance.Hide(); }

    /// <summary>
    /// Highlights a button on the monitor with a pulsing ring.
    /// Called by TutorialManager.uiguide.cs PointAt() alongside
    /// the existing TutorialUIPointer arrow.
    /// </summary>
    void HighlightMonitorButton(UnityEngine.RectTransform target, string label = null)
    {
        if (MonitorButtonHighlight.Instance != null)
            MonitorButtonHighlight.Instance.HighlightButton(target, label);
    }

    void ClearMonitorHighlight()
    {
        if (MonitorButtonHighlight.Instance != null)
            MonitorButtonHighlight.Instance.ClearHighlight();
    }

    void CompleteAllTasks()
    {
        if (TaskListUI.Instance == null) return;
        for (int i = 0; i < 10; i++) TaskListUI.Instance.CompleteTask(i);
    }

    void SetTask(string title, params string[] tasks)
    {
        TaskListUI.Instance?.SetTitle(title);
        TaskListUI.Instance?.SetTasks(tasks);
    }

    void Dialogue(float delay, DialogueSequence seq, System.Action callback)
    { StartCoroutine(DialogueCoroutine(delay, seq, callback)); }

    IEnumerator DialogueCoroutine(float delay, DialogueSequence seq, System.Action callback)
    {
        yield return new WaitForSeconds(delay);
        TaskListUI.Instance?.HidePanel();
        InspectionManager im = FindFirstObjectByType<InspectionManager>();
        if (im != null && im.controlsUI != null) im.controlsUI.SetActive(false);
        yield return new WaitForSeconds(0.3f);

        if (seq != null && dialogueManager != null) dialogueManager.PlaySequence(seq, callback);
        else callback?.Invoke();
    }

    IEnumerator ShowArrowForType(TutorialTarget.TargetType type)
    {
        Transform found = null; float elapsed = 0f;
        while (found == null && elapsed < 5f)
        {
            foreach (TutorialTarget t in FindObjectsOfType<TutorialTarget>())
                if (t.type == type) { found = t.transform; break; }
            if (found == null) { elapsed += 0.2f; yield return new WaitForSeconds(0.2f); }
        }
        if (found != null) ShowArrow(found);
    }

    PCPartDatabase GetDatabase()
    {
        if (partDatabase != null) return partDatabase;
        if (CloudDataHandler.Instance != null && CloudDataHandler.Instance.partDatabase != null)
            return CloudDataHandler.Instance.partDatabase;
        CustomerInside c = FindObjectOfType<CustomerInside>();
        return c != null ? c.partDatabase : null;
    }

    /// <summary>
    /// Called by CustomerDeskManager after the desk PC is spawned.
    /// Re-applies the highlight to the actual spawned object.
    /// </summary>
    public void NotifyDeskPCSpawned(Transform deskPC)
    {
        if (step == 5 && deskPC != null)
        {
            HideArrow();
            ShowArrow(deskPC);
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  FIX 2: NotifyInspectionCloneReady
    //
    //  Two changes:
    //  A) Step 12: Highlight the power cord connector port INSIDE
    //     the inspection clone instead of the world-space WallOutlet
    //     (which is invisible during inspection because the camera
    //     only renders InspectLayer).
    //
    //  B) Step 15: If totalScrewsOnPanel is still 0 (because
    //     OnReenteredInspect ran before the clone existed), count
    //     the screws now and update the task UI.
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Called by InspectionManager once the inspection clone is fully built.
    /// Highlights the relevant part based on current tutorial step.
    /// </summary>
    public void NotifyInspectionCloneReady(GameObject clone)
    {
        if (clone == null) return;
        HideArrow();

        // Clear any previous highlights
        tutorialInspectionHighlight?.Hide();
        if (TutorialWorldHighlight.Instance != null)
            TutorialWorldHighlight.Instance.Hide();

        Debug.Log($"[Tutorial] NotifyInspectionCloneReady — step={step}, diagState={diagState}");

        // ── Step 12: Highlight the power cord connector port INSIDE the clone ──
        // The WallOutlet lives in world space and is NOT visible during inspection
        // (camera only renders InspectLayer). Instead, find the PrebuiltWire that
        // has isPowerCord and highlight its connector port on the inspection clone.
        if (step == 12)
        {
            foreach (MonoBehaviour mb in clone.GetComponentsInChildren<MonoBehaviour>(true))
            {
                IPrebuiltWire wire = mb as IPrebuiltWire;
                if (wire != null && wire.IsPowerCord && wire.ConnectorPort != null)
                {
                    tutorialInspectionHighlight?.ShowAt(wire.ConnectorPort.transform);
                    Debug.Log($"[Tutorial] Step 12: Highlighting power cord port '{wire.ConnectorPort.name}' inside clone.");
                    return;
                }
            }
            // Fallback: search by name if no IPrebuiltWire found
            Transform powerPort = FindChildByNameContains(clone.transform, "PowerCord", "power cord", "Power Cord");
            if (powerPort != null)
            {
                tutorialInspectionHighlight?.ShowAt(powerPort);
                Debug.Log($"[Tutorial] Step 12: Highlighting fallback '{powerPort.name}' inside clone.");
            }
            else
            {
                Debug.LogWarning("[Tutorial] Step 12: Could not find power cord port in clone!");
            }
            return;
        }

        // ── Step 15: Ensure screw count is initialized ──
        // OnReenteredInspect may have run before the clone existed (CompletePCTask
        // is called by PlayerInteract BEFORE Inspect). If so, totalScrewsOnPanel
        // is still 0. Initialize it now that the clone is ready.
        if (step == 15 && totalScrewsOnPanel == 0)
        {
            totalScrewsOnPanel = CountSidePanelScrews();
            TaskListUI.Instance?.CompleteTask(0);
            TaskListUI.Instance?.CompleteTask(1);
            TaskListUI.Instance?.UpdateTaskText(3, $"Unscrew the side panel screws (0/{totalScrewsOnPanel})");
            Debug.Log($"[Tutorial] Step 15: Late-initialized totalScrewsOnPanel={totalScrewsOnPanel}");
        }

        Transform target = FindInspectionTarget(clone, step);

        Debug.Log($"[Tutorial] FindInspectionTarget result: {(target != null ? target.name : "NULL")}");

        if (target != null)
            tutorialInspectionHighlight?.ShowAt(target);
    }
    public void NotifyPrebuiltWireDisconnected(IPrebuiltWire wire)
    {
        if (step != 16 || diagState != DiagState.DisconnectGPUWire) return;
        string cat = wire.RequiredPartCategory ?? "";
        if (cat == "GPU")
        {
            CompleteAllTasks();
            diagState = DiagState.RemoveOldGPU;
            ShowSwapTask();
        }
    }

    /// <summary>
    /// Returns the child transform to highlight inside the inspection clone
    /// for the given tutorial step.
    /// </summary>
    private Transform FindInspectionTarget(GameObject clone, int currentStep)
    {
        switch (currentStep)
        {
            case 12:
                return null; // handled directly in NotifyInspectionCloneReady

            case 13: // Power button — find by isPowerButton flag; name search as fallback
                foreach (InspectableItem item in clone.GetComponentsInChildren<InspectableItem>(true))
                {
                    if (item.isPowerButton) return item.transform;
                }
                return FindChildByNameContains(clone.transform, "Power", "Button", "power");

            case 15: // Open case — highlight panel screw first, then the panel itself
                if (screwsRemoved < totalScrewsOnPanel)
                {
                    // FIX: Find the first unremoved panel screw, checking BOTH
                    // itemName AND gameObject.name (e.g. "Front Panel Screw 1")
                    foreach (InspectableItem item in clone.GetComponentsInChildren<InspectableItem>(true))
                    {
                        if (item.isInventorySlot || item.isMainObject) continue;
                        string n = (item.itemName ?? "").ToLower();
                        string c = (item.partCategory ?? "").ToLower();
                        string gn = item.gameObject.name.ToLower();
                        // Combine so either field can satisfy the check
                        string combined = n + " " + gn;
                        bool isPanelScrew =
                            (combined.Contains("screw") || combined.Contains("bolt") || combined.Contains("fastener")
                             || c.Contains("screw") || c.Contains("bolt"))
                            && (combined.Contains("panel") || combined.Contains("front") || combined.Contains("side"))
                            && !combined.Contains("back");
                        if (isPanelScrew) return item.transform;
                    }
                }
                else if (!panelRemoved)
                {
                    // All screws done — highlight the panel itself (front/side only, not back)
                    foreach (InspectableItem item in clone.GetComponentsInChildren<InspectableItem>(true))
                    {
                        if (item.isInventorySlot || item.isMainObject) continue;
                        string n = (item.itemName ?? "").ToLower();
                        string c = (item.partCategory ?? "").ToLower();
                        string gn = item.gameObject.name.ToLower();
                        string combined = n + " " + gn;
                        if ((combined.Contains("panel") || c.Contains("panel"))
                            && !combined.Contains("back"))
                            return item.transform;
                    }
                }
                return null;
            case 16:
                // ── GPU WIRE DISCONNECT: highlight power button first, then wire port ──
                if (diagState == DiagState.DisconnectGPUWire)
                {
                    PCPowerSystem pw = clone.GetComponent<PCPowerSystem>();
                    if (pw != null && pw.isPoweredOn)
                    {
                        foreach (InspectableItem item in clone.GetComponentsInChildren<InspectableItem>(true))
                            if (item.isPowerButton) return item.transform;
                    }
                    // PC is off — highlight the GPU wire connector port
                    foreach (MonoBehaviour mb in clone.GetComponentsInChildren<MonoBehaviour>(true))
                    {
                        IPrebuiltWire w = mb as IPrebuiltWire;
                        if (w != null && w.IsConnected && w.RequiredPartCategory == "GPU" && w.ConnectorPort != null)
                            return w.ConnectorPort.transform;
                    }
                    return null;
                }

                // ── RAM-related states (swap, verify, final fix) ──
                if (diagState == DiagState.RemoveOldRAM
                 || diagState == DiagState.InstallNewRAM
                 || diagState == DiagState.RemoveNewRAM
                 || diagState == DiagState.InstallOldRAM
                 || diagState == DiagState.RemoveOldRAMAgain
                 || diagState == DiagState.InstallNewRAMFinal)
                    return FindPartByCategory(clone.transform, "RAM");

                // ── GPU-related states ──
                if (diagState == DiagState.RemoveOldGPU
                 || diagState == DiagState.InstallNewGPU)
                    return FindPartByCategory(clone.transform, "GPU");

                // ── TEST STATES: always highlight the power button ──
                if (diagState == DiagState.TestAfterRAMSwap
                 || diagState == DiagState.TestAfterGPUSwap
                 || diagState == DiagState.TestWithOldRAM
                 || diagState == DiagState.TestFinal)
                {
                    foreach (InspectableItem item in clone.GetComponentsInChildren<InspectableItem>(true))
                        if (item.isPowerButton) return item.transform;
                }

                return null;

            default:
                return null;
        }
    }

    private Transform FindPartByCategory(Transform root, string category)
    {
        // Pass 1: Find an installed (non-ghost) part with matching category,
        // then walk UP the hierarchy to find the component root.
        // PCCaseBuilder copies partCategory to all children, so a deep child
        // mesh may match first — walking up ensures we get the full component.
        foreach (InspectableItem item in root.GetComponentsInChildren<InspectableItem>(true))
        {
            if (item.isInventorySlot || item.isMainObject) continue;
            if (item.partCategory == category)
                return FindComponentRoot(item.transform, root, category);
        }

        // Pass 2: Find ghost slots (empty slots for installation).
        // During install states the part has been removed and the slot is
        // flagged isInventorySlot — highlight it so the player sees where to click.
        foreach (InspectableItem item in root.GetComponentsInChildren<InspectableItem>(true))
        {
            if (item.isMainObject) continue;
            if (item.partCategory == category && item.isInventorySlot)
                return item.transform;
        }

        return null;
    }

    /// <summary>
    /// Walks UP from a child InspectableItem to find the topmost ancestor
    /// that shares the same partCategory. This is the component ROOT placed
    /// by PCCaseBuilder — highlighting it will cover every child renderer
    /// (the full RAM stick, the full GPU, etc.) instead of just one sub-mesh.
    /// </summary>
    private Transform FindComponentRoot(Transform start, Transform cloneRoot, string category)
    {
        Transform best = start;
        Transform current = start.parent;

        while (current != null && current != cloneRoot)
        {
            InspectableItem ii = current.GetComponent<InspectableItem>();
            if (ii != null && !ii.isMainObject && !ii.isInventorySlot
                && ii.partCategory == category)
            {
                best = current; // found a higher ancestor with same category
            }
            current = current.parent;
        }

        return best;
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private Transform FindChildByNameContains(Transform root, params string[] names)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            foreach (string n in names)
            {
                if (child.name.IndexOf(n, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return child;
            }
        }
        return null;
    }

    private void RefreshInspectionHighlight()
    {
        // Clear first so ShowAt() same-target guard doesn't block the refresh
        tutorialInspectionHighlight?.Hide();
        InspectionManager im = FindFirstObjectByType<InspectionManager>();
        if (im != null && im.currentClone != null)
            NotifyInspectionCloneReady(im.currentClone);
    }

    /// <summary>
    /// FIX: Deferred highlight refresh — waits one frame so TryRemovePart
    /// finishes converting the just-removed part to a ghost slot before
    /// FindInspectionTarget searches for the next screw to highlight.
    /// Without this, the search finds the same (not-yet-ghosted) screw again.
    /// </summary>
    private IEnumerator RefreshHighlightNextFrame()
    {
        yield return null; // wait one frame for TryRemovePart to complete
        tutorialInspectionHighlight?.Hide();
        InspectionManager im = FindFirstObjectByType<InspectionManager>();
        if (im != null && im.currentClone != null)
            NotifyInspectionCloneReady(im.currentClone);
    }
}
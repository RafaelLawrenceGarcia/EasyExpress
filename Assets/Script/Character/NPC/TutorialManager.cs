using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum DiagState
{
    // GPU swap
    RemoveOldGPU, InstallNewGPU, TestAfterGPUSwap,
    // RAM swap
    RemoveOldRAM, InstallNewRAM, TestAfterRAMSwap,
    // RAM verify
    RemoveNewRAM, InstallOldRAM, TestWithOldRAM,
    RemoveOldRAMAgain, InstallNewRAMFinal, TestAfterRAMVerify,
    // GPU verify
    RemoveNewGPU, InstallOldGPU, TestWithOldGPU,
    RemoveOldGPUAgain, InstallNewGPUFinal, TestFinal,
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

    [Header("Arrow Systems")]
    public TutorialArrow tutorialArrow;

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

    [Header("Dialogue — Phase 2: Intake")]
    public DialogueSequence dlg_PickupBox;
    public DialogueSequence dlg_PlaceBox;

    [Header("Dialogue — Phase 3: Diagnosis")]
    public DialogueSequence dlg_InspectPC;
    [Tooltip("Connect the power cord.")]
    public DialogueSequence dlg_ConnectCord;
    public DialogueSequence dlg_PowerTest;
    [Tooltip("After first power test — grab parts from storage.")]
    public DialogueSequence dlg_GrabPartsFirst;
    [Tooltip("Open the case (screws + panel).")]
    public DialogueSequence dlg_OpenCase;

    [Header("Dialogue — Phase 3: Swap-and-Test")]
    [Tooltip("After panel removed — swap the GPU first.")]
    public DialogueSequence dlg_SwapGPU;
    [Tooltip("GPU swap still fails (RAM still bad).")]
    public DialogueSequence dlg_GPUNotEnough;
    [Tooltip("Swap the RAM next.")]
    public DialogueSequence dlg_SwapRAM;
    [Tooltip("RAM swap — it works!")]
    public DialogueSequence dlg_RAMFixed;
    [Tooltip("Boss: put old RAM back to confirm.")]
    public DialogueSequence dlg_VerifyOldRAM;
    [Tooltip("RAM confirmed bad. Put new RAM back.")]
    public DialogueSequence dlg_RAMConfirmed;
    [Tooltip("Put the new RAM back.")]
    public DialogueSequence dlg_PutNewRAMBack;
    [Tooltip("RAM fixed — now verify GPU too.")]
    public DialogueSequence dlg_NowCheckGPU;
    [Tooltip("GPU confirmed dead (no display).")]
    public DialogueSequence dlg_GPUConfirmed;
    [Tooltip("Put the new GPU back to finish.")]
    public DialogueSequence dlg_PutNewGPUBack;

    [Header("Dialogue — Phase 4: Email & Shop")]
    public DialogueSequence dlg_EmailTutorial;
    public DialogueSequence dlg_ShopTutorial;
    [Tooltip("Troubleshooting patterns.")]
    public DialogueSequence dlg_TroubleshootGuide;
    [Tooltip("Final welcome speech.")]
    public DialogueSequence dlg_FinalWelcome;

    // ─── Runtime State ───────────────────────────────────────────

    private int step = 0;
    private float wTimer, aTimer, sTimer, dTimer;
    private bool wDone, aDone, sDone, dDone;
    private bool playerReachedCashier, customerReachedCashier;
    private int screwsRemoved, totalScrewsOnPanel;
    private bool panelRemoved;
    private DiagState diagState = DiagState.RemoveOldGPU;
    private bool grabbedGPU, grabbedRAM;
    private bool shopBrowsed, shopAddedToCart;

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
            if (!done) { PlayerPrefs.SetInt("TutorialDone", 1); PlayerPrefs.Save(); }
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
        && (diagState == DiagState.InstallNewGPU
         || diagState == DiagState.InstallNewRAM
         || diagState == DiagState.InstallOldRAM
         || diagState == DiagState.InstallNewRAMFinal
         || diagState == DiagState.InstallOldGPU
         || diagState == DiagState.InstallNewGPUFinal);
    public bool IsEndDayStep() => false;
    public bool IsEmailStep() => step == 25;
    public bool IsInspectPCAllowed() => true;
    public bool IsDiagnosingPC() => step == 16;
    public DiagState GetDiagState() => diagState;

    public void HideTaskTemporarily() { TaskListUI.Instance?.HideTemporarily(); }
    public void RestoreTaskIfNeeded() { TaskListUI.Instance?.RestoreIfNeeded(); }

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
    }

    public void CompleteApproachCashierPCTask()
    {
        if (step == 5) TaskListUI.Instance?.CompleteTask(0);
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
        if (step != 9) return;
        CompleteAllTasks(); HideArrow();
        step = 10;
        Dialogue(1.0f, dlg_InspectPC, StartStep_InspectPC);
    }

    public void CompleteApproachShopPCTask()
    {
        if (step == 26) TaskListUI.Instance?.CompleteTask(0);
    }

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
            OnReenteredInspect();
            return;
        }
    }

    public void NotifyPowerCordConnected()
    {
        if (step != 12) return;
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
        if (step == 5) { CompleteCashierInspectTask(); step = 6; StartStep_TalkToCustomer(); return; }
        if (step == 14) { OnExitInspectForStorage(); return; }
        if (step == 24)
        {
            TaskListUI.Instance?.CompleteTask(0);
            step = 25;
            Debug.Log($"[Tutorial] Step 24→25. dlg_EmailTutorial is " +
                      $"{(dlg_EmailTutorial != null ? "ASSIGNED (" + dlg_EmailTutorial.lines.Length + " lines)" : "NULL — assign it in Inspector!")}");
            Dialogue(0.5f, dlg_EmailTutorial, StartStep_EmailTutorial);
            return;
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
            bool isTestState = diagState == DiagState.TestAfterGPUSwap
                            || diagState == DiagState.TestAfterRAMSwap
                            || diagState == DiagState.TestWithOldRAM
                            || diagState == DiagState.TestAfterRAMVerify
                            || diagState == DiagState.TestWithOldGPU
                            || diagState == DiagState.TestFinal;
            if (poweredOn && isTestState) HandleDiagPowerTest();
            return;
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
        Debug.Log("[Tutorial] Email job marked complete → advancing to shop tutorial.");
        CompleteAllTasks(); HideArrow();
        step = 26;
        Dialogue(1.0f, dlg_ShopTutorial, StartStep_ShopTutorial);
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
        step = 0;
        wTimer = aTimer = sTimer = dTimer = 0f;
        wDone = aDone = sDone = dDone = false;
        playerReachedCashier = customerReachedCashier = false;
        screwsRemoved = totalScrewsOnPanel = 0;
        panelRemoved = false;
        diagState = DiagState.RemoveOldGPU;
        grabbedGPU = grabbedRAM = false;
        shopBrowsed = shopAddedToCart = false;
        HideArrow(); HidePointer();
        StartCoroutine(DelayedStart());
    }

    // ─── Utility ─────────────────────────────────────────────────

    void ShowArrow(Transform target) { tutorialArrow?.ShowAt(target); }
    void HideArrow() { tutorialArrow?.Hide(); }
    void HidePointer() { if (TutorialUIPointer.Instance != null) TutorialUIPointer.Instance.Hide(); }

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
}
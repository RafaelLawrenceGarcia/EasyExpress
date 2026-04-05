using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public partial class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance;
    private const int STEP_DONE = 50;

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
    [Tooltip("Tells player to connect the power cord before testing.")]
    public DialogueSequence dlg_ConnectCord;
    public DialogueSequence dlg_PowerTest;
    public DialogueSequence dlg_OpenCase;
    [Tooltip("After panel removed. Explains process of elimination.")]
    public DialogueSequence dlg_IdentifyFaults;
    [Tooltip("After GPU removed. Tells player to press power to test.")]
    public DialogueSequence dlg_TestAfterFirstRemoval;
    [Tooltip("After mid-diagnosis power test still fails.")]
    public DialogueSequence dlg_StillFailing;
    [Tooltip("After all faulty parts removed.")]
    public DialogueSequence dlg_FoundAllFaults;

    [Header("Dialogue — Phase 4: Storage & Repair")]
    [Tooltip("Step-by-step storage shelf walkthrough.")]
    public DialogueSequence dlg_StorageTutorial;
    public DialogueSequence dlg_InstallParts;
    public DialogueSequence dlg_PowerSuccess;

    [Header("Dialogue — Phase 5: Email & Shop")]
    public DialogueSequence dlg_EmailTutorial;
    public DialogueSequence dlg_ShopTutorial;
    public DialogueSequence dlg_WelcomeSpeech;

    // ─── Runtime State ───────────────────────────────────────────

    private int step = 0;
    private float wTimer, aTimer, sTimer, dTimer;
    private bool wDone, aDone, sDone, dDone;
    private bool playerReachedCashier, customerReachedCashier;
    private bool powerOnDone, powerOffDone;
    private int screwsRemoved, totalScrewsOnPanel;
    private bool panelRemoved;
    private int diagPhase = 0;
    private int faultyPartsRemoved = 0;
    private int totalFaultyParts = 0;

    // ─── Lifecycle ───────────────────────────────────────────────

    void Awake() { Instance = this; }
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
        if (step == 0 && day == 1) StartCoroutine(DelayedStart());
    }

    void Update()
    {
        if (step == 1) UpdateWASD();
    }

    // ─── Public Queries ──────────────────────────────────────────

    public int GetCurrentStep() => step;
    public bool IsTutorialActive() => PlayerPrefs.GetInt("TutorialDone", 0) != 1
                                         && PlayerPrefs.GetInt("IsLoadingGame", 0) != 1
                                         && step < STEP_DONE;
    public bool IsRepairStep() => step >= 11 && step <= 24;
    public bool IsShopPCStep() => step == 26;
    public bool IsCashierPCStep() => step == 5 || step == 6;
    public bool IsInstallComponentStep() => step == 22;
    public bool IsEndDayStep() => false;
    public bool IsEmailStep() => step == 25;
    public bool IsInspectPCAllowed() => true;
    public bool IsDiagnosingPC() => step == 15;

    public void HideTaskTemporarily() { TaskListUI.Instance?.HideTemporarily(); }
    public void RestoreTaskIfNeeded() { TaskListUI.Instance?.RestoreIfNeeded(); }

    // ─── Public Completions ──────────────────────────────────────

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
        if (step == 22) TaskListUI.Instance?.CompleteTask(0);
        if (step == 26) TaskListUI.Instance?.CompleteTask(0);
    }

    public void CompletePCTask()
    {
        if (step != 11) return;
        CompleteAllTasks(); HideArrow();
        step = 12;
        Dialogue(0.5f, dlg_ConnectCord, StartStep_ConnectPowerCord);
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
        if (step != 14 && step != 15) return;
        HandlePartRemoval(removedPart);
    }

    public void OnPlayerExitedInspection()
    {
        if (step == 5)
        {
            CompleteCashierInspectTask();
            step = 6;
            StartStep_TalkToCustomer();
            return;
        }
        if (step == 17)
        {
            TaskListUI.Instance?.CompleteTask(0);
            step = 21;
            Dialogue(0.5f, dlg_StorageTutorial, StartStep_StorageShelf);
        }
        else if (step == 24)
        {
            TaskListUI.Instance?.CompleteTask(0);
            step = 25;
            Dialogue(0.5f, dlg_EmailTutorial, StartStep_EmailTutorial);
        }
    }

    public void OnPCPowerToggled(bool poweredOn)
    {
        if (step == 13)
        {
            if (poweredOn)
            {
                TaskListUI.Instance?.CompleteTask(0);
                StartCoroutine(DelayThenAdvanceFromFirstPower());
            }
            return;
        }

        if (step == 15 && diagPhase == 1)
        {
            if (poweredOn)
            {
                TaskListUI.Instance?.CompleteTask(0);
                StartCoroutine(DelayThenAdvanceFromDiagPowerTest());
            }
            return;
        }

        if (step == 23)
        {
            if (poweredOn && !powerOnDone)
            { powerOnDone = true; TaskListUI.Instance?.CompleteTask(1); }
            if (!poweredOn && powerOnDone && !powerOffDone)
            { powerOffDone = true; TaskListUI.Instance?.CompleteTask(2); }
            if (powerOnDone && powerOffDone)
            {
                TaskListUI.Instance?.CompleteTask(0);
                HideArrow();
                step = 24;
                StartStep_ExitInspectForEmail();
            }
        }
    }

    public void NotifyStoragePartGrabbed(string partCategory)
    {
        if (step != 21) return;
        string cat = (partCategory ?? "").Trim().ToUpper();
        if (cat == "GPU") { TaskListUI.Instance?.CompleteTask(2); }
        else if (cat == "RAM") { TaskListUI.Instance?.CompleteTask(3); }
    }

    public void CompleteApproachStorageShelfTask()
    {
        if (step == 21) TaskListUI.Instance?.CompleteTask(0);
    }

    public void CompleteStorageShelfTask()
    {
        if (step != 21) return;
        TaskListUI.Instance?.CompleteTask(0);
        TaskListUI.Instance?.CompleteTask(1);
        TaskListUI.Instance?.CompleteTask(4);
        HideArrow();
        step = 22;
        Dialogue(1.0f, dlg_InstallParts, StartStep_InstallComponent);
    }

    public void CompleteInstallComponentTask()
    {
        if (step != 22) return;
        TaskListUI.Instance?.CompleteTask(2);
        TaskListUI.Instance?.CompleteTask(3);
        HideArrow();
        step = 23;
        Dialogue(0.5f, dlg_PowerSuccess, StartStep_FinalPowerTest);
    }

    public void CompleteAssemblyTask() => CompleteInstallComponentTask();

    public void CompleteApproachEmailTask()
    {
        if (step == 25) TaskListUI.Instance?.CompleteTask(0);
    }

    public void CompleteEmailTask()
    {
        if (step != 25) return;
        CompleteAllTasks(); HideArrow();
        step = 26;
        Dialogue(1.0f, dlg_ShopTutorial, StartStep_ShopTutorial);
    }

    public void CompleteOrderPartsTask() { }
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
        powerOnDone = powerOffDone = false;
        screwsRemoved = totalScrewsOnPanel = 0;
        panelRemoved = false;
        diagPhase = 0;
        faultyPartsRemoved = totalFaultyParts = 0;
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
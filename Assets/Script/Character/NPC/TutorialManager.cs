using UnityEngine;
using System.Collections;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance;

    [Header("Managers")]
    public IntroDialogueManager dialogueManager;
    public DayTransitionManager dayTransitionManager;

    [Header("Dialogue Files")]
    public DialogueSequence part1_MoveDialogue;
    public DialogueSequence part2_CustomerDialogue;
    public DialogueSequence part3_PickupBoxDialogue;
    public DialogueSequence part4_PlaceBoxDialogue;
    public DialogueSequence part5_PCDialogue;
    public DialogueSequence part6_HoverDialogue;
    public DialogueSequence part7_RemoveDialogue;

    // ─────────────────────────────────────────────────────────────
    //  ARROW SYSTEM
    //
    //  SETUP:
    //  1. Drag the TutorialArrow UI into the slot below.
    //  2. Drag the Workstation (already in scene) into its slot.
    //  3. On your Customer/Box/PC prefabs, add the TutorialTarget.cs
    //     script and set the Type dropdown (Customer / Box / PC).
    //  Done! The script finds the spawned clones automatically.
    // ─────────────────────────────────────────────────────────────

    [Header("Arrow System")]
    public TutorialArrow tutorialArrow;

    [Tooltip("Drag the Workstation desk from the scene here.")]
    public Transform workstationTarget;

    [Header("Movement Task Settings")]
    public float requiredHoldTime = 2.0f;
    private float wTimer, aTimer, sTimer, dTimer;
    private bool wDone, aDone, sDone, dDone;

    private int tutorialStep = 0;

    // ─────────────────────────────────────────────────────────────
    //  FIND SPAWNED CLONE BY ITS TutorialTarget TYPE
    //  Retries every 0.2s so it doesn't matter when it spawns.
    // ─────────────────────────────────────────────────────────────

    IEnumerator ShowArrowForType(TutorialTarget.TargetType type)
    {
        Transform found = null;
        float elapsed = 0f;

        while (found == null && elapsed < 5f)
        {
            // Search all active TutorialTarget components in the scene
            TutorialTarget[] all = FindObjectsOfType<TutorialTarget>();
            foreach (TutorialTarget t in all)
            {
                if (t.type == type)
                {
                    found = t.transform;
                    break;
                }
            }

            if (found == null)
            {
                elapsed += 0.2f;
                yield return new WaitForSeconds(0.2f);
            }
        }

        if (found != null)
            ShowArrow(found);
        else
            Debug.LogWarning($"[TutorialArrow] No TutorialTarget of type '{type}' found. Did you add TutorialTarget.cs to the prefab?");
    }

    void ShowArrow(Transform target) { if (tutorialArrow != null && target != null) tutorialArrow.ShowAt(target); }
    void HideArrow() { if (tutorialArrow != null) tutorialArrow.Hide(); }

    // ─────────────────────────────────────────────────────────────
    //  UNITY LIFECYCLE
    // ─────────────────────────────────────────────────────────────

    void Awake() { Instance = this; }
    void OnEnable() { DayTransitionManager.OnNewDayStarted += OnDayStarted; }
    void OnDisable() { DayTransitionManager.OnNewDayStarted -= OnDayStarted; }

    void OnDayStarted(int day)
    {
        if (tutorialStep == 0 && day == 1)
            StartCoroutine(DelayedStart());
    }

    void Start()
    {
        if (PlayerPrefs.GetInt("TutorialDone", 0) == 1 || PlayerPrefs.GetInt("IsLoadingGame", 0) == 1)
        {
            tutorialStep = 14;
            if (PlayerPrefs.GetInt("TutorialDone", 0) != 1) { PlayerPrefs.SetInt("TutorialDone", 1); PlayerPrefs.Save(); }
            HideArrow();
        }
        else { tutorialStep = 0; }
    }

    IEnumerator DelayedStart()
    {
        yield return new WaitForSeconds(1.0f);
        if (dialogueManager == null || part1_MoveDialogue == null) { tutorialStep = 14; yield break; }
        dialogueManager.PlaySequence(part1_MoveDialogue, StartMovementTask);
    }

    // ─────────────────────────────────────────────────────────────
    //  RESTART TUTORIAL (called by CloudDataHandler for new accounts)
    //
    //  When a logged-in player has no cloud data, Start() already
    //  ran and skipped the tutorial (because IsLoadingGame was 1).
    //  This method resets everything and kicks off the tutorial
    //  without waiting for the day-started event again.
    // ─────────────────────────────────────────────────────────────

    public void RestartTutorial()
    {
        Debug.Log("[Tutorial] Restarting tutorial for new account.");

        // Reset step and WASD timers
        tutorialStep = 0;
        wTimer = aTimer = sTimer = dTimer = 0f;
        wDone = aDone = sDone = dDone = false;
        HideArrow();

        // Kick off the tutorial directly (day intro already played)
        StartCoroutine(DelayedStart());
    }

    public void HideTaskTemporarily() { if (TaskListUI.Instance != null) TaskListUI.Instance.HideTemporarily(); }
    public void RestoreTaskIfNeeded() { if (TaskListUI.Instance != null) TaskListUI.Instance.RestoreIfNeeded(); }

    // ─────────────────────────────────────────────────────────────
    //  STEP 1 — WASD (no arrow)
    // ─────────────────────────────────────────────────────────────

    void StartMovementTask()
    {
        wDone = aDone = sDone = dDone = false;
        if (TaskListUI.Instance != null)
        {
            TaskListUI.Instance.SetTitle("CALIBRATION");
            TaskListUI.Instance.SetTasks(new string[] {
                "Hold W to move forward",
                "Hold A to move left",
                "Hold S to move backward",
                "Hold D to move right"
            });
        }
        HideArrow();
        tutorialStep = 1;
    }

    void Update()
    {
        if (tutorialStep != 1) return;

        if (Input.GetKey(KeyCode.W)) wTimer += Time.deltaTime;
        if (Input.GetKey(KeyCode.A)) aTimer += Time.deltaTime;
        if (Input.GetKey(KeyCode.S)) sTimer += Time.deltaTime;
        if (Input.GetKey(KeyCode.D)) dTimer += Time.deltaTime;

        wTimer = Mathf.Clamp(wTimer, 0, requiredHoldTime);
        aTimer = Mathf.Clamp(aTimer, 0, requiredHoldTime);
        sTimer = Mathf.Clamp(sTimer, 0, requiredHoldTime);
        dTimer = Mathf.Clamp(dTimer, 0, requiredHoldTime);

        if (TaskListUI.Instance != null)
        {
            if (!wDone && wTimer >= requiredHoldTime) { wDone = true; TaskListUI.Instance.CompleteTask(0); }
            if (!aDone && aTimer >= requiredHoldTime) { aDone = true; TaskListUI.Instance.CompleteTask(1); }
            if (!sDone && sTimer >= requiredHoldTime) { sDone = true; TaskListUI.Instance.CompleteTask(2); }
            if (!dDone && dTimer >= requiredHoldTime) { dDone = true; TaskListUI.Instance.CompleteTask(3); }
        }

        if (wTimer >= requiredHoldTime && aTimer >= requiredHoldTime &&
            sTimer >= requiredHoldTime && dTimer >= requiredHoldTime)
        {
            tutorialStep = 2;

            // ► WASD done — tell the spawner to release the customer now!
            if (ShopCustomerSpawner.Instance != null)
                ShopCustomerSpawner.Instance.AllowSpawn();

            StartCoroutine(DelayedHideAndDialogue(1.0f, part2_CustomerDialogue, StartCustomerTask));
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  STEP 3 — Customer
    // ─────────────────────────────────────────────────────────────

    void StartCustomerTask()
    {
        if (TaskListUI.Instance != null)
        {
            TaskListUI.Instance.SetTitle("OBJECTIVES");
            TaskListUI.Instance.SetTasks(new string[] { "Approach the customer", "Press E to talk", "Accept the job" });
        }
        StartCoroutine(ShowArrowForType(TutorialTarget.TargetType.Customer));
        tutorialStep = 3;
    }

    public void CompleteCustomerTask()
    {
        if (tutorialStep != 3) return;
        if (TaskListUI.Instance != null) { TaskListUI.Instance.CompleteTask(0); TaskListUI.Instance.CompleteTask(1); TaskListUI.Instance.CompleteTask(2); }
        HideArrow();
        tutorialStep = 4;
        StartCoroutine(DelayedHideAndDialogue(1.0f, part3_PickupBoxDialogue, StartPickupBoxTask));
    }

    // ─────────────────────────────────────────────────────────────
    //  STEP 5 — Box
    // ─────────────────────────────────────────────────────────────

    void StartPickupBoxTask()
    {
        if (TaskListUI.Instance != null)
        {
            TaskListUI.Instance.SetTitle("OBJECTIVES");
            TaskListUI.Instance.SetTasks(new string[] { "Find the PC box on the shelf", "Press Q to pick it up" });
        }
        StartCoroutine(ShowArrowForType(TutorialTarget.TargetType.Box));
        tutorialStep = 5;
    }

    public void CompletePickupBoxTask()
    {
        if (tutorialStep != 5) return;
        if (TaskListUI.Instance != null) { TaskListUI.Instance.CompleteTask(0); TaskListUI.Instance.CompleteTask(1); }
        HideArrow();
        tutorialStep = 6;
        StartCoroutine(DelayedHideAndDialogue(1.0f, part4_PlaceBoxDialogue, StartPlaceBoxTask));
    }

    // ─────────────────────────────────────────────────────────────
    //  STEP 7 — Workstation (already in scene, just drag it)
    // ─────────────────────────────────────────────────────────────

    void StartPlaceBoxTask()
    {
        if (TaskListUI.Instance != null)
        {
            TaskListUI.Instance.SetTitle("OBJECTIVES");
            TaskListUI.Instance.SetTasks(new string[] { "Look at the workstation desk", "Left-click to place the box" });
        }
        ShowArrow(workstationTarget);
        tutorialStep = 7;
    }

    public void CompletePlaceBoxTask()
    {
        if (tutorialStep != 7) return;
        if (TaskListUI.Instance != null) { TaskListUI.Instance.CompleteTask(0); TaskListUI.Instance.CompleteTask(1); }
        HideArrow();
        tutorialStep = 8;
        StartCoroutine(DelayedHideAndDialogue(1.0f, part5_PCDialogue, StartPCTask));
    }

    // ─────────────────────────────────────────────────────────────
    //  STEP 9 — PC
    // ─────────────────────────────────────────────────────────────

    void StartPCTask()
    {
        if (TaskListUI.Instance != null)
        {
            TaskListUI.Instance.SetTitle("OBJECTIVES");
            TaskListUI.Instance.SetTasks(new string[] { "Inspect the unpacked PC" });
        }
        StartCoroutine(ShowArrowForType(TutorialTarget.TargetType.PC));
        tutorialStep = 9;
    }

    public void CompletePCTask()
    {
        if (tutorialStep != 9) return;
        if (TaskListUI.Instance != null) TaskListUI.Instance.CompleteTask(0);
        HideArrow();
        tutorialStep = 10;
        StartCoroutine(DelayedHideAndDialogue(1.0f, part6_HoverDialogue, StartHoverTask));
    }

    // ─────────────────────────────────────────────────────────────
    //  STEP 11 — Hover (inside PC view, no arrow)
    // ─────────────────────────────────────────────────────────────

    void StartHoverTask()
    {
        if (TaskListUI.Instance != null)
        {
            TaskListUI.Instance.SetTitle("OBJECTIVES");
            TaskListUI.Instance.SetTasks(new string[] { "Hover over a PC component" });
        }
        HideArrow();
        tutorialStep = 11;
    }

    public void CompleteHoverTask()
    {
        if (tutorialStep != 11) return;
        if (TaskListUI.Instance != null) TaskListUI.Instance.CompleteTask(0);
        tutorialStep = 12;
        StartCoroutine(DelayedHideAndDialogue(1.0f, part7_RemoveDialogue, StartRemoveTask));
    }

    // ─────────────────────────────────────────────────────────────
    //  STEP 13 — Remove (inside PC view, no arrow)
    // ─────────────────────────────────────────────────────────────

    void StartRemoveTask()
    {
        if (TaskListUI.Instance != null)
        {
            TaskListUI.Instance.SetTitle("OBJECTIVES");
            TaskListUI.Instance.SetTasks(new string[] { "Left-click to remove the part" });
        }
        HideArrow();
        tutorialStep = 13;
    }

    public void CompleteRemoveTask()
    {
        if (tutorialStep != 13) return;
        if (TaskListUI.Instance != null) TaskListUI.Instance.CompleteTask(0);
        HideArrow();
        tutorialStep = 14;
        StartCoroutine(FinalCleanup());
    }

    IEnumerator FinalCleanup()
    {
        yield return new WaitForSeconds(1.5f);
        if (TaskListUI.Instance != null) TaskListUI.Instance.HidePanel();
        HideArrow();
        PlayerPrefs.SetInt("TutorialDone", 1);
        PlayerPrefs.Save();
        Debug.Log("Tutorial Fully Complete!");
    }

    IEnumerator DelayedHideAndDialogue(float delay, DialogueSequence seq, System.Action callback)
    {
        yield return new WaitForSeconds(delay);
        if (TaskListUI.Instance != null) TaskListUI.Instance.HidePanel();
        yield return new WaitForSeconds(0.3f);
        dialogueManager.PlaySequence(seq, callback);
    }

    public int GetCurrentStep() { return tutorialStep; }

    public bool IsTutorialActive()
    {
        if (PlayerPrefs.GetInt("TutorialDone", 0) == 1) return false;
        if (PlayerPrefs.GetInt("IsLoadingGame", 0) == 1) return false;
        return tutorialStep < 14; // includes step 0 (waiting for day to start)
    }
}
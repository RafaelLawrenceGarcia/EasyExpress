using UnityEngine;
using System.Collections;

/// <summary>
/// ╔══════════════════════════════════════════════════════════════════════════╗
/// ║                    EASY EXPRESS — TUTORIAL MANAGER                      ║
/// ║                         Full 18-Step Tutorial                           ║
/// ╠══════════════════════════════════════════════════════════════════════════╣
/// ║  STEP MAP                                                                ║
/// ║  ─────────────────────────────────────────────────────────────────────  ║
/// ║  Step  1  WASD Movement Training                                         ║
/// ║  Step  3  Go to Cashier / Wait for Customer NPC                          ║
/// ║  Step  5  Interact Cashier PC → Accept the Job                           ║
/// ║  Step  7  Pick Up the Customer's Box                                     ║
/// ║  Step  9  Place Box on the Workstation                                   ║
/// ║  Step 11  Open PC Inspection                                             ║
/// ║  Step 13  Hover Over a Component                                         ║
/// ║  Step 15  Remove the Faulty Component                                    ║
/// ║  Step 16  Exit Inspection (auto-detected on close)                       ║
/// ║  Step 17  Buy Replacement Part  [wallet zeroed → stipend given]          ║
/// ║  Step 18  End the Day  (go to door)                                      ║
/// ║  Step 20  New Day — Pick Up the Delivery Box                             ║
/// ║  Step 22  Go to Storage Shelf / Retrieve Item                            ║
/// ║  Step 24  Interact Shop PC → Install Component to Job                   ║
/// ║  Step 26  Finish PC Assembly on the Workstation                          ║
/// ║  Step 28  Power-Test the PC  (Turn ON → Turn OFF)                        ║
/// ║  Step 30  Exit Inspection → Open Email Workstation                       ║
/// ║  Step 32  Mark Job Complete → Final Welcome Speech                       ║
/// ║  Step 50  TUTORIAL COMPLETE                                              ║
/// ╠══════════════════════════════════════════════════════════════════════════╣
/// ║  Even steps (2, 4, 6 …) = in-between states while a dialogue plays.     ║
/// ║  Odd steps              = active tasks waiting for player input.         ║
/// ╠══════════════════════════════════════════════════════════════════════════╣
/// ║  See TUTORIAL_INTEGRATION_NOTES.md for the small changes required       ║
/// ║  in PlayerInteract, CustomerInside, InspectionManager, etc.             ║
/// ╚══════════════════════════════════════════════════════════════════════════╝
/// </summary>
public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance;

    // ─────────────────────────────────────────────────────────────
    //  CONSTANTS
    // ─────────────────────────────────────────────────────────────

    /// <summary>tutorialStep is set to this value when the tutorial is fully complete.</summary>
    private const int STEP_DONE = 50;

    // ─────────────────────────────────────────────────────────────
    //  INSPECTOR — MANAGERS
    // ─────────────────────────────────────────────────────────────

    [Header("Managers")]
    public IntroDialogueManager dialogueManager;
    public DayTransitionManager dayTransitionManager;

    // ─────────────────────────────────────────────────────────────
    //  INSPECTOR — DIALOGUE FILES
    //  Create a DialogueSequence ScriptableObject for each step and
    //  drag it into the matching slot in the Inspector.
    // ─────────────────────────────────────────────────────────────

    [Header("Dialogue — Block 1: Movement → Customer")]

    [Tooltip("Plays at Day 1 start. Brief welcome + explains WASD.")]
    public DialogueSequence part01_IntroAndWASD;

    [Tooltip("Plays after WASD. Announces first customer is on the way, tells player to go to the cashier.")]
    public DialogueSequence part02_GoCashier;

    [Tooltip("Plays when customer reaches cashier. Tells player to use the Cashier PC to check the job.")]
    public DialogueSequence part03_UseCashierPC;

    [Header("Dialogue — Block 2: Box → Inspect")]

    [Tooltip("Plays after accepting the job. Tells player to find and carry the customer's PC box.")]
    public DialogueSequence part04_PickupBox;

    [Tooltip("Plays after picking up the box. Tells player to place it on the workstation.")]
    public DialogueSequence part05_PlaceBox;
    private bool playerReachedCashier  = false;
    private bool customerReachedCashier = false;
    
    [Tooltip("Plays after placing the box. Explains the inspect system, tells player to open it.")]
    public DialogueSequence part06_OpenInspect;

    [Tooltip("Plays after entering inspect. Explains component info tooltips, tells player to hover.")]
    public DialogueSequence part07_HoverComponent;

    [Tooltip("Plays after hovering. Tells player to left-click to remove the faulty part.")]
    public DialogueSequence part08_RemoveComponent;

    [Header("Dialogue — Block 3: Shop → End Day")]

    [Tooltip("Plays after removing the part (player has just exited inspect). " +
             "Explains they need to buy a replacement — wallet is reset and stipend is given here.")]
    public DialogueSequence part09_BuyParts;

    [Tooltip("Plays after player orders the part. Tells them to end the day so it can be delivered.")]
    public DialogueSequence part10_EndDay;

    [Header("Dialogue — Block 4: New Day → Final")]

    [Tooltip("Plays at the start of Day 2. Announces the delivery has arrived.")]
    public DialogueSequence part11_DeliveryArrived;

    [Tooltip("Plays after picking up the delivery. Explains the storage shelf system.")]
    public DialogueSequence part12_GoToStorage;

    [Tooltip("Plays after storage shelf task. Tells player to use the Shop PC to install the part.")]
    public DialogueSequence part13_InstallPart;

    [Tooltip("Plays after installing the component. Tells player to return to the workstation and finish the build.")]
    public DialogueSequence part14_FinishBuild;

    [Tooltip("Plays after the PC is built. Tells player to power-test it inside inspect.")]
    public DialogueSequence part15_PowerTest;

    [Tooltip("Plays after the power test. Tells player to exit inspect and open their email.")]
    public DialogueSequence part16_GoToEmail;

    [Tooltip("Final welcome speech + job completion celebration. Unlocks free play.")]
    public DialogueSequence part17_WelcomeSpeech;

    // ─────────────────────────────────────────────────────────────
    //  INSPECTOR — ARROW SYSTEM
    // ─────────────────────────────────────────────────────────────

    [Header("Arrow System")]
    [Tooltip("Drag the TutorialArrow UI object from your Tutorial Canvas here.")]
    public TutorialArrow tutorialArrow;

    [Header("Scene Transform Targets (drag from Hierarchy)")]
    [Tooltip("The Workstation desk (already in scene).")]
    public Transform workstationTarget;

    [Tooltip("The Cashier counter where the customer NPC walks to.")]
    public Transform cashierTarget;

    [Tooltip("The Cashier / main desk PC that the player uses to accept jobs.")]
    public Transform cashierPCTarget;

    [Tooltip("The Shop / Order PC (may be the same as cashierPCTarget).")]
    public Transform shopPCTarget;

    [Tooltip("The Storage Shelf where ordered components are stored.")]
    public Transform storageShelfTarget;

    [Tooltip("The Email workstation monitor.")]
    public Transform emailTarget;

    [Tooltip("The shop exit door (used for the End Day step).")]
    public Transform doorTarget;

    // ─────────────────────────────────────────────────────────────
    //  INSPECTOR — WASD SETTINGS
    // ─────────────────────────────────────────────────────────────

    [Header("WASD Task Settings")]
    [Tooltip("How long (seconds) the player must hold each directional key to pass.")]
    public float requiredHoldTime = 2.0f;

    private float wTimer, aTimer, sTimer, dTimer;
    private bool  wDone, aDone, sDone, dDone;

    // ─────────────────────────────────────────────────────────────
    //  INSPECTOR — MONEY STIPEND
    // ─────────────────────────────────────────────────────────────

    [Header("Tutorial Money Stipend")]
    [Tooltip("Amount given to the player at the 'Buy Parts' step. Their wallet is zeroed first.")]
    public float tutorialStipend = 15000f;

    // ─────────────────────────────────────────────────────────────
    //  PRIVATE STATE
    // ─────────────────────────────────────────────────────────────

    private int  tutorialStep  = 0;
    private bool powerOnDone   = false;
    private bool powerOffDone  = false;

    // ══════════════════════════════════════════════════════════════
    //  UNITY LIFECYCLE
    // ══════════════════════════════════════════════════════════════

    void Awake() { Instance = this; }

    void OnEnable()  { DayTransitionManager.OnNewDayStarted += OnDayStarted; }
    void OnDisable() { DayTransitionManager.OnNewDayStarted -= OnDayStarted; }

    void Start()
    {
        bool tutorialDone  = PlayerPrefs.GetInt("TutorialDone",  0) == 1;
        bool isLoadingGame = PlayerPrefs.GetInt("IsLoadingGame", 0) == 1;

        if (tutorialDone || isLoadingGame)
        {
            tutorialStep = STEP_DONE;
            if (!tutorialDone) { PlayerPrefs.SetInt("TutorialDone", 1); PlayerPrefs.Save(); }
            HideArrow();
        }
        else
        {
            tutorialStep = 0;
        }
    }

    void OnDayStarted(int day)
    {
        // Day 1 — kick off the tutorial
        if (tutorialStep == 0 && day == 1)
            StartCoroutine(DelayedStart());

        // Day 2+ (after End Day) — component delivery arrived
        if (tutorialStep == 19 && day >= 2)
            StartCoroutine(DelayedHideAndDialogue(1.5f, part11_DeliveryArrived, StartPickupDeliveryTask));
    }

    // ══════════════════════════════════════════════════════════════
    //  RESTART (called by CloudDataHandler for brand-new accounts)
    // ══════════════════════════════════════════════════════════════

    /// <summary>
    /// Resets all tutorial state and starts from the beginning.
    /// Called by CloudDataHandler when a fresh account has no cloud data.
    /// </summary>
    public void RestartTutorial()
    {
        Debug.Log("[Tutorial] Restarting tutorial for new account.");

        tutorialStep   = 0;
        wTimer = aTimer = sTimer = dTimer = 0f;
        wDone  = aDone  = sDone  = dDone  = false;
        powerOnDone    = false;
        powerOffDone   = false;
        HideArrow();

        StartCoroutine(DelayedStart());
    }

    // ══════════════════════════════════════════════════════════════
    //  TASK LIST PASSTHROUGH HELPERS
    // ══════════════════════════════════════════════════════════════

    public void HideTaskTemporarily() { TaskListUI.Instance?.HideTemporarily(); }
    public void RestoreTaskIfNeeded() { TaskListUI.Instance?.RestoreIfNeeded(); }

    // ══════════════════════════════════════════════════════════════
    //  ▌STEP 1 — WASD MOVEMENT
    // ══════════════════════════════════════════════════════════════

    IEnumerator DelayedStart()
    {
        yield return new WaitForSeconds(1.0f);

        if (dialogueManager == null || part01_IntroAndWASD == null)
        {
            Debug.LogWarning("[Tutorial] Missing dialogueManager or part01_IntroAndWASD. Skipping tutorial.");
            tutorialStep = STEP_DONE;
            yield break;
        }

        dialogueManager.PlaySequence(part01_IntroAndWASD, StartMovementTask);
    }

    void StartMovementTask()
    {
        wDone = aDone = sDone = dDone = false;

        TaskListUI.Instance?.SetTitle("CALIBRATION");
        TaskListUI.Instance?.SetTasks(new string[]
        {
            "Hold [W]  — Move Forward",
            "Hold [A]  — Move Left",
            "Hold [S]  — Move Backward",
            "Hold [D]  — Move Right"
        });

        HideArrow();
        tutorialStep = 1;
    }

    void Update()
    {
        // Only WASD polling runs in Update — everything else is event-driven
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

        if (wDone && aDone && sDone && dDone)
        {
            tutorialStep = 2; // transition — waiting for dialogue to finish

            // Release the held customer so they begin walking to the shop
            ShopCustomerSpawner.Instance?.AllowSpawn();

            StartCoroutine(DelayedHideAndDialogue(1.0f, part02_GoCashier, StartGoCashierTask));
        }
    }

    // ══════════════════════════════════════════════════════════════
    //  ▌STEP 3 — GO TO CASHIER / WAIT FOR CUSTOMER NPC
    // ══════════════════════════════════════════════════════════════

        void StartGoCashierTask()
    {
        playerReachedCashier   = false;
        customerReachedCashier = false;
 
        TaskListUI.Instance?.SetTitle("FIRST CUSTOMER");
        TaskListUI.Instance?.SetTasks(new string[]
        {
            "Head to the cashier counter",    // 0 — zone trigger
            "Wait for the customer to arrive" // 1 — NPC arrival
        });
 
        if (cashierTarget != null) ShowArrow(cashierTarget);
        else StartCoroutine(ShowArrowForType(TutorialTarget.TargetType.Cashier));
 
        tutorialStep = 3;
    }
 
    // Called by TutorialZoneTrigger (ZoneType.CashierCounter)
    public void CompleteGoToCashierTask()
    {
        if (tutorialStep != 3 || playerReachedCashier) return;
        playerReachedCashier = true;
        TaskListUI.Instance?.CompleteTask(0);
        TryAdvanceFromCashierStep();
    }
 
    // Called by CustomerInside.cs when isAtSpot = true
    public void NotifyCustomerArrivedAtCashier()
    {
        if (tutorialStep != 3 || customerReachedCashier) return;
        customerReachedCashier = true;
        TaskListUI.Instance?.CompleteTask(1);
        TryAdvanceFromCashierStep();
    }
 
    void TryAdvanceFromCashierStep()
    {
        if (!playerReachedCashier || !customerReachedCashier) return;
        HideArrow();
        tutorialStep = 4;
        StartCoroutine(DelayedHideAndDialogue(0.5f, part03_UseCashierPC, StartCashierPCTask));
    }

    // ══════════════════════════════════════════════════════════════
    //  ▌STEP 5 — INTERACT CASHIER PC → ACCEPT JOB
    //
    //  ── Required change in PlayerInteract.cs ──────────────────
    //  Find the 'else if (shopPC)' block and REPLACE the tutorial
    //  block check with this:
    //
    //    bool tutActive    = TutorialManager.Instance != null
    //                        && TutorialManager.Instance.IsTutorialActive();
    //    bool cashierStep  = TutorialManager.Instance != null
    //                        && TutorialManager.Instance.IsCashierPCStep();
    //    bool installStep  = TutorialManager.Instance != null
    //                        && TutorialManager.Instance.IsInstallComponentStep();
    //
    //    if (tutActive && !cashierStep && !installStep)
    //    {
    //        ShowPromptWithHighlight("X", "Finish your tasks first!", hit.collider.gameObject);
    //        return;
    //    }
    //
    //    // Tutorial cashier step: show accept prompt, force-accept on E
    //    if (cashierStep)
    //    {
    //        ShowPromptWithHighlight("E", "Check Customer Request", hit.collider.gameObject);
    //        if (Input.GetKeyDown(KeyCode.E))
    //            TutorialManager.Instance.ForceAcceptCurrentCustomer();
    //        return;
    //    }
    //
    //  (The normal OpenShopComputer() call handles isInstallStep automatically)
    // ─────────────────────────────────────────────────────────────

        void StartCashierPCTask()
    {
        TaskListUI.Instance?.SetTitle("NEW JOB REQUEST");
        TaskListUI.Instance?.SetTasks(new string[]
        {
            "Walk to the cashier PC",          // 0 — zone trigger
            "Press [E] to check the request",  // 1 — pressing E
            "Accept the job"                   // 2 — force accept
        });
 
        if (cashierPCTarget != null) ShowArrow(cashierPCTarget);
        else StartCoroutine(ShowArrowForType(TutorialTarget.TargetType.CashierPC));
 
        tutorialStep = 5;
    }

    /// <summary>
    /// Returns true when the player is allowed to interact with the cashier PC
    /// and should see the "accept / reject" customer popup.
    /// Queried by PlayerInteract.cs each frame.
    /// </summary>
    public bool IsCashierPCStep() => tutorialStep == 5;

    /// <summary>
    /// Called from PlayerInteract.cs when the player presses [E] on the
    /// cashier PC during step 5.  Finds the waiting CustomerInside and
    /// force-accepts their job, then advances the tutorial.
    ///
    /// NOTE: The Reject button should be hidden / disabled in your cashier
    ///       PC UI when IsCashierPCStep() returns true, so the player
    ///       cannot accidentally refuse the tutorial job.
    /// </summary>
        public void ForceAcceptCurrentCustomer()
    {
        if (tutorialStep != 5) return;
 
        TaskListUI.Instance?.CompleteTask(1); // "Press E" green
 
        CustomerInside[] customers = FindObjectsOfType<CustomerInside>();
        foreach (CustomerInside c in customers)
        {
            if (c.isAtSpot) { c.AcceptJob(); break; }
        }
 
        CompleteCashierPCTask();
    }

    /// <summary>
    /// Advances the tutorial after the job is accepted via the cashier PC.
    /// Also used as the legacy alias CompleteCustomerTask() for backwards compatibility.
    /// </summary>
    public void CompleteCashierPCTask()
    {
        if (tutorialStep != 5) return;
        TaskListUI.Instance?.CompleteTask(0);
        TaskListUI.Instance?.CompleteTask(1);
        TaskListUI.Instance?.CompleteTask(2); // "Accept the job" green
        HideArrow();
        tutorialStep = 6;
        StartCoroutine(DelayedHideAndDialogue(1.0f, part04_PickupBox, StartPickupBoxTask));
    }

    /// <summary>
    /// Legacy alias kept for backwards compatibility with existing
    /// PlayerInteract.cs CloseAfterDelay() hook.
    /// </summary>
    public void CompleteCustomerTask() => CompleteCashierPCTask();

    // ══════════════════════════════════════════════════════════════
    //  ▌STEP 7 — PICK UP CUSTOMER'S BOX
    // ══════════════════════════════════════════════════════════════

    void StartPickupBoxTask()
    {
        TaskListUI.Instance?.SetTitle("INTAKE — STEP 1 / 3");
        TaskListUI.Instance?.SetTasks(new string[]
        {
            "Find the customer's PC box",
            "Press [Q] to carry it"
        });

        StartCoroutine(ShowArrowForType(TutorialTarget.TargetType.Box));
        tutorialStep = 7;
    }
    /// <summary>
    /// Returns true when the player is allowed to inspect the PC.
    /// Allowed once the box has been placed on the workstation (step 10+).
    /// This covers the Inspect step (11), the Email step (32), and everything after.
    /// </summary>
    public bool IsInspectPCAllowed()
    {
        return tutorialStep >= 10;
    }
    /// <summary>
    /// Call from PlayerInteract.cs when the player picks up a PickupBox tagged object.
    ///
    /// Existing hook location (PlayerInteract.cs, end of PickUpItem or wherever
    /// TutorialManager.Instance.CompletePickupBoxTask() was previously called):
    ///   if (TutorialManager.Instance != null && pickedUpObj.CompareTag("PickupBox"))
    ///       TutorialManager.Instance.CompletePickupBoxTask();
    /// </summary>
    public void CompletePickupBoxTask()
    {
        if (tutorialStep != 7) return;

        TaskListUI.Instance?.CompleteTask(0);
        TaskListUI.Instance?.CompleteTask(1);
        HideArrow();
        tutorialStep = 8;

        StartCoroutine(DelayedHideAndDialogue(1.0f, part05_PlaceBox, StartPlaceBoxTask));
    }

    // ══════════════════════════════════════════════════════════════
    //  ▌STEP 9 — PLACE BOX ON WORKSTATION
    // ══════════════════════════════════════════════════════════════
    public void CompleteApproachWorkstationTask()
    {
        if (tutorialStep != 9) return;
        TaskListUI.Instance?.CompleteTask(0); // "Walk to workstation" green
    }
    public void CompleteApproachCashierPCTask()
    {
        if (tutorialStep != 5) return;
        TaskListUI.Instance?.CompleteTask(0);
    }
    void StartPlaceBoxTask()
    {
        TaskListUI.Instance?.SetTitle("INTAKE — STEP 1 / 3");
        TaskListUI.Instance?.SetTasks(new string[]
        {
            "Walk to the workstation desk", // 0 — zone trigger
            "Left-click to set the box down" // 1 — place action
        });
 
        ShowArrow(workstationTarget);
        tutorialStep = 9;
    }

    /// <summary>
    /// Call from your placement/interaction code when the box lands on the workstation.
    ///
    /// Existing hook location:
    ///   if (TutorialManager.Instance != null)
    ///       TutorialManager.Instance.CompletePlaceBoxTask();
    /// </summary>
    public void CompletePlaceBoxTask()
    {
        if (tutorialStep != 9) return;
        TaskListUI.Instance?.CompleteTask(0);
        TaskListUI.Instance?.CompleteTask(1);
        HideArrow();
        tutorialStep = 10;
        StartCoroutine(DelayedHideAndDialogue(1.0f, part06_OpenInspect, StartInspectTask));
    }
        public void CompleteApproachShopPCTask()
    {
        if (tutorialStep == 17)
            TaskListUI.Instance?.CompleteTask(1); // "Go to the Shop PC" green (task index 1)
 
        if (tutorialStep == 24)
            TaskListUI.Instance?.CompleteTask(0); // "Go to the shop PC" green (task index 0)
    }

    // ══════════════════════════════════════════════════════════════
    //  ▌STEP 11 — OPEN PC INSPECTION
    // ══════════════════════════════════════════════════════════════

    void StartInspectTask()
    {
        TaskListUI.Instance?.SetTitle("DIAGNOSE — STEP 2 / 3");
        TaskListUI.Instance?.SetTasks(new string[]
        {
            "Look at the unpacked PC on the desk",
            "Press [E] to enter Inspect Mode"
        });

        StartCoroutine(ShowArrowForType(TutorialTarget.TargetType.PC));
        tutorialStep = 11;
    }

    /// <summary>
    /// Call from PlayerInteract.cs when the player opens PC inspection.
    ///
    /// ⚠ Also update the step-number guard in PlayerInteract.cs:
    ///   OLD:  TutorialManager.Instance.GetCurrentStep() < 9
    ///   NEW:  TutorialManager.Instance.GetCurrentStep() < 11
    ///
    /// Existing hook location (PlayerInteract.cs, inspect confirmation path):
    ///   if (TutorialManager.Instance != null) TutorialManager.Instance.CompletePCTask();
    /// </summary>
    public void CompletePCTask()
    {
        if (tutorialStep != 11) return;

        TaskListUI.Instance?.CompleteTask(0);
        TaskListUI.Instance?.CompleteTask(1);
        HideArrow();
        tutorialStep = 12;

        StartCoroutine(DelayedHideAndDialogue(1.0f, part07_HoverComponent, StartHoverTask));
    }

    // ══════════════════════════════════════════════════════════════
    //  ▌STEP 13 — HOVER OVER A COMPONENT
    // ══════════════════════════════════════════════════════════════

    void StartHoverTask()
    {
        TaskListUI.Instance?.SetTitle("DIAGNOSE — STEP 2 / 3");
        TaskListUI.Instance?.SetTasks(new string[]
        {
            "Move your cursor over any component",
            "Read the component info tooltip"
        });

        HideArrow(); // player is inside the PC view — no world arrow
        tutorialStep = 13;
    }

    /// <summary>
    /// Call from your PC inspection hover system when a component is hovered.
    ///
    /// ⚠ Update step-number check in your hover script:
    ///   OLD:  TutorialManager.Instance.GetCurrentStep() == 11
    ///   NEW:  TutorialManager.Instance.GetCurrentStep() == 13
    ///
    ///   if (TutorialManager.Instance != null
    ///       && TutorialManager.Instance.GetCurrentStep() == 13)
    ///       TutorialManager.Instance.CompleteHoverTask();
    /// </summary>
    public void CompleteHoverTask()
    {
        if (tutorialStep != 13) return;

        TaskListUI.Instance?.CompleteTask(0);
        TaskListUI.Instance?.CompleteTask(1);
        tutorialStep = 14;

        StartCoroutine(DelayedHideAndDialogue(1.0f, part08_RemoveComponent, StartRemoveTask));
    }

    // ══════════════════════════════════════════════════════════════
    //  ▌STEP 15 — REMOVE FAULTY COMPONENT
    // ══════════════════════════════════════════════════════════════

    void StartRemoveTask()
    {
        TaskListUI.Instance?.SetTitle("DIAGNOSE — STEP 2 / 3");
        TaskListUI.Instance?.SetTasks(new string[]
        {
            "Left-click the faulty component to remove it"
        });

        HideArrow();
        tutorialStep = 15;
    }

    /// <summary>
    /// Call from your component-removal system when a part is clicked and removed.
    ///
    /// Existing hook location:
    ///   if (TutorialManager.Instance != null) TutorialManager.Instance.CompleteRemoveTask();
    /// </summary>
    public void CompleteRemoveTask()
    {
        if (tutorialStep != 15) return;

        TaskListUI.Instance?.CompleteTask(0);
        HideArrow();
        tutorialStep = 16; // auto-advance — waiting for player to exit inspection

        StartCoroutine(ShowExitInspectPrompt());
    }

    // ══════════════════════════════════════════════════════════════
    //  ▌STEP 16 — EXIT INSPECTION  (auto-detected)
    //
    //  ── Required change in InspectionManager.cs ───────────────
    //  At the END of your ExitInspect() / CloseInspection() method:
    //
    //    if (TutorialManager.Instance != null)
    //        TutorialManager.Instance.OnPlayerExitedInspection();
    //
    //  This same hook handles both Step 16 (exit after remove) and
    //  Step 30 (exit before email) automatically.
    // ─────────────────────────────────────────────────────────────

    IEnumerator ShowExitInspectPrompt()
    {
        yield return new WaitForSeconds(1.5f);
        TaskListUI.Instance?.SetTitle("DIAGNOSE — STEP 2 / 3");
        TaskListUI.Instance?.SetTasks(new string[]
        {
            "Press [Esc] or [Back] to exit Inspect Mode"
        });
    }

    /// <summary>
    /// Called by InspectionManager.cs whenever the player closes the inspect view.
    /// Handles both Step 16 (after remove → buy parts) and Step 30 (after power test → email).
    /// </summary>
    public void OnPlayerExitedInspection()
    {
        if (tutorialStep == 16)
        {
            TaskListUI.Instance?.CompleteTask(0);
            tutorialStep = 17; // transition → buy parts
            StartCoroutine(DelayedHideAndDialogue(0.5f, part09_BuyParts, StartBuyPartsTask));
        }
        else if (tutorialStep == 30)
        {
            TaskListUI.Instance?.CompleteTask(0);
            tutorialStep = 31; // transition → email
            StartCoroutine(DelayedHideAndDialogue(0.5f, part16_GoToEmail, StartEmailTask));
        }
    }

    // ══════════════════════════════════════════════════════════════
    //  ▌STEP 17 — BUY REPLACEMENT PARTS  (wallet zeroed → stipend)
    // ══════════════════════════════════════════════════════════════

    void StartBuyPartsTask()
    {
        // ── Money Setup ──────────────────────────────────────────
        // Zero every wallet in the scene, then give the tutorial stipend.
        PlayerWallet[] wallets = FindObjectsOfType<PlayerWallet>();
        foreach (PlayerWallet w in wallets)
        {
            w.currentGold = 0f;
            w.currentDebt = 0f;
            w.UpdateUI();
        }
        // Add stipend through the proper AddGold path so PlayerPrefs stays in sync.
        if (wallets.Length > 0)
            wallets[0].AddGold(tutorialStipend);

        Debug.Log($"[Tutorial] Wallet zeroed → stipend of ₱{tutorialStipend:F0} applied.");
        // ─────────────────────────────────────────────────────────

        TaskListUI.Instance?.SetTitle("REPAIR — STEP 3 / 3");
        TaskListUI.Instance?.SetTasks(new string[]
        {
            $"You received ₱{tutorialStipend:F0} to order parts",
            "Go to the Shop PC",
            "Order the replacement component"
        });

        // Point at the shop/order PC
        if (shopPCTarget != null)       ShowArrow(shopPCTarget);
        else if (cashierPCTarget != null) ShowArrow(cashierPCTarget);
        else StartCoroutine(ShowArrowForType(TutorialTarget.TargetType.ShopPC));

        tutorialStep = 17;
    }

    /// <summary>
    /// Call this after the player successfully submits a component order via the Shop PC.
    ///
    /// Hook into your component-ordering success callback:
    ///   if (TutorialManager.Instance != null)
    ///       TutorialManager.Instance.CompleteOrderPartsTask();
    /// </summary>
    public void CompleteOrderPartsTask()
    {
        if (tutorialStep != 17) return;

        TaskListUI.Instance?.CompleteTask(0);
        TaskListUI.Instance?.CompleteTask(1);
        TaskListUI.Instance?.CompleteTask(2);
        HideArrow();
        tutorialStep = 18; // transition

        StartCoroutine(DelayedHideAndDialogue(1.0f, part10_EndDay, StartEndDayTask));
    }

    // ══════════════════════════════════════════════════════════════
    //  ▌STEP 18 — END THE DAY
    //
    //  ── Required change in DoorInteractionMenu.cs ─────────────
    //  In DoEndDay(), AFTER calling dayTransitionManager.EndDay(...):
    //
    //    if (TutorialManager.Instance != null)
    //        TutorialManager.Instance.CompleteEndDayTask();
    //
    //  Also update the door-block check so End Day is ALLOWED at step 18:
    //
    //    bool tutActive  = TutorialManager.Instance != null
    //                      && TutorialManager.Instance.IsTutorialActive();
    //    bool endDayStep = TutorialManager.Instance != null
    //                      && TutorialManager.Instance.IsEndDayStep();
    //
    //    if (tutActive && !endDayStep)
    //    {
    //        ShowPromptWithHighlight("X", "Finish your tasks first!", ...);
    //        return;
    //    }
    // ─────────────────────────────────────────────────────────────

    void StartEndDayTask()
    {
        TaskListUI.Instance?.SetTitle("END OF DAY");
        TaskListUI.Instance?.SetTasks(new string[]
        {
            "Walk to the shop door",  // 0 — zone trigger
            "Open the door menu",     // 1 — E on door
            "Select  ›  End Day"      // 2 — confirm
        });
 
        if (doorTarget != null) ShowArrow(doorTarget);
        tutorialStep = 18;
    }
        public void CompleteApproachDoorTask()
    {
        if (tutorialStep != 18) return;
        TaskListUI.Instance?.CompleteTask(0); // "Walk to door" green
    }

    /// <summary>Returns true when the player is allowed to use End Day (step 18).</summary>
    public bool IsEndDayStep() => tutorialStep == 18;

    /// <summary>
    /// Call from DoorInteractionMenu.DoEndDay() when End Day is confirmed.
    /// </summary>
    public void CompleteEndDayTask()
    {
        if (tutorialStep != 18) return;
        TaskListUI.Instance?.CompleteTask(0);
        TaskListUI.Instance?.CompleteTask(1);
        TaskListUI.Instance?.CompleteTask(2);
        HideArrow();
        tutorialStep = 19;
    }

    // ══════════════════════════════════════════════════════════════
    //  ▌STEP 20 — NEW DAY: PICK UP DELIVERY BOX
    //  (Triggered automatically from OnDayStarted when step == 19)
    // ══════════════════════════════════════════════════════════════

    void StartPickupDeliveryTask()
    {
        TaskListUI.Instance?.SetTitle("DELIVERY ARRIVED!");
        TaskListUI.Instance?.SetTasks(new string[]
        {
            "A package has arrived at the shop!",
            "Walk over to the delivery box",
            "Press [Q] to pick it up"
        });

        StartCoroutine(ShowArrowForType(TutorialTarget.TargetType.DeliveryBox));
        tutorialStep = 20;
    }

    /// <summary>
    /// Call from PlayerInteract.cs (or DeliveryBox.cs) when a delivery box is picked up.
    ///
    /// Suggested hook in PlayerInteract.cs inside PickUpItem():
    ///   DeliveryBox dBox = itemObj.GetComponentInParent&lt;DeliveryBox&gt;();
    ///   if (dBox != null && TutorialManager.Instance != null)
    ///       TutorialManager.Instance.CompletePickupDeliveryTask();
    /// </summary>
    public void CompletePickupDeliveryTask()
    {
        if (tutorialStep != 20) return;

        TaskListUI.Instance?.CompleteTask(0);
        TaskListUI.Instance?.CompleteTask(1);
        TaskListUI.Instance?.CompleteTask(2);
        HideArrow();
        tutorialStep = 21;

        StartCoroutine(DelayedHideAndDialogue(1.0f, part12_GoToStorage, StartStorageShelfTask));
    }

    // ══════════════════════════════════════════════════════════════
    //  ▌STEP 22 — GO TO STORAGE SHELF / RETRIEVE COMPONENT
    //
    //  ── Required change in StorageRoomShelf.cs ────────────────
    //  When the player takes an item from the shelf:
    //
    //    if (TutorialManager.Instance != null)
    //        TutorialManager.Instance.CompleteStorageShelfTask();
    // ─────────────────────────────────────────────────────────────

    void StartStorageShelfTask()
    {
        TaskListUI.Instance?.SetTitle("REPAIR — STEP 3 / 3");
        TaskListUI.Instance?.SetTasks(new string[]
        {
            "Go to the storage shelf",       // 0 — zone trigger
            "Pick up the ordered component"  // 1 — shelf interaction
        });
 
        if (storageShelfTarget != null) ShowArrow(storageShelfTarget);
        else StartCoroutine(ShowArrowForType(TutorialTarget.TargetType.StorageShelf));
 
        tutorialStep = 22;
    }
        public void CompleteApproachStorageShelfTask()
    {
        if (tutorialStep != 22) return;
        TaskListUI.Instance?.CompleteTask(0); // "Go to storage shelf" green
    }

    /// <summary>
    /// Call from StorageRoomShelf when the player retrieves a component during step 22.
    /// </summary>
        public void CompleteStorageShelfTask()
    {
        if (tutorialStep != 22) return;
        TaskListUI.Instance?.CompleteTask(0);
        TaskListUI.Instance?.CompleteTask(1);
        HideArrow();
        tutorialStep = 23;
        StartCoroutine(DelayedHideAndDialogue(1.0f, part13_InstallPart, StartInstallComponentTask));
    }

    // ══════════════════════════════════════════════════════════════
    //  ▌STEP 24 — INTERACT SHOP PC → INSTALL COMPONENT
    //
    //  ── Required change in PlayerInteract.cs ──────────────────
    //  The shopPC block already allows this step when IsInstallComponentStep()
    //  returns true (see the IsCashierPCStep change above).
    //  Inside OpenShopComputer() or the OS, hook component installation:
    //
    //    if (TutorialManager.Instance != null)
    //        TutorialManager.Instance.CompleteInstallComponentTask();
    // ─────────────────────────────────────────────────────────────

    void StartInstallComponentTask()
    {
        TaskListUI.Instance?.SetTitle("REPAIR — STEP 3 / 3");
        TaskListUI.Instance?.SetTasks(new string[]
        {
            "Go to the shop PC",
            "Press [E] to open the computer",
            "Install the component into the customer's job"
        });

        if (shopPCTarget != null)         ShowArrow(shopPCTarget);
        else if (cashierPCTarget != null) ShowArrow(cashierPCTarget);
        else StartCoroutine(ShowArrowForType(TutorialTarget.TargetType.ShopPC));

        tutorialStep = 24;
    }

    /// <summary>
    /// Returns true when the player is allowed to use the Shop PC for component installation.
    /// Queried by PlayerInteract.cs — allows the shopPC to open at this step.
    /// </summary>
    public bool IsInstallComponentStep() => tutorialStep == 24;

    /// <summary>
    /// Call this after the player successfully assigns the component to the job.
    /// </summary>
    public void CompleteInstallComponentTask()
    {
        if (tutorialStep != 24) return;

        TaskListUI.Instance?.CompleteTask(0);
        TaskListUI.Instance?.CompleteTask(1);
        TaskListUI.Instance?.CompleteTask(2);
        HideArrow();
        tutorialStep = 25;

        StartCoroutine(DelayedHideAndDialogue(1.0f, part14_FinishBuild, StartBuildPCTask));
    }

    // ══════════════════════════════════════════════════════════════
    //  ▌STEP 26 — FINISH PC ASSEMBLY
    //
    //  ── Required hook in your PCCaseBuilder / AssemblyManager ─
    //    if (TutorialManager.Instance != null)
    //        TutorialManager.Instance.CompleteAssemblyTask();
    // ─────────────────────────────────────────────────────────────

    void StartBuildPCTask()
    {
        TaskListUI.Instance?.SetTitle("REPAIR — STEP 3 / 3");
        TaskListUI.Instance?.SetTasks(new string[]
        {
            "Return to the workstation",
            "Install all remaining components",
            "Complete the PC build"
        });

        ShowArrow(workstationTarget);
        tutorialStep = 26;
    }

    /// <summary>
    /// Call from PCCaseBuilder / AssemblyManager when all parts are installed and the build is complete.
    /// </summary>
    public void CompleteAssemblyTask()
    {
        if (tutorialStep != 26) return;

        TaskListUI.Instance?.CompleteTask(0);
        TaskListUI.Instance?.CompleteTask(1);
        TaskListUI.Instance?.CompleteTask(2);
        HideArrow();
        tutorialStep = 27;

        StartCoroutine(DelayedHideAndDialogue(1.0f, part15_PowerTest, StartPowerTestTask));
    }

    // ══════════════════════════════════════════════════════════════
    //  ▌STEP 28 — POWER-TEST THE PC  (Turn ON → Turn OFF)
    //
    //  ── Required hook in your PC power-button script ──────────
    //  When the power button is pressed inside the inspect view:
    //
    //    bool nowOn = /* your power-on state */;
    //    if (TutorialManager.Instance != null)
    //        TutorialManager.Instance.OnPCPowerToggled(nowOn);
    // ─────────────────────────────────────────────────────────────

    void StartPowerTestTask()
    {
        powerOnDone  = false;
        powerOffDone = false;

        TaskListUI.Instance?.SetTitle("QUALITY CHECK");
        TaskListUI.Instance?.SetTasks(new string[]
        {
            "Inspect the finished PC",
            "Press the power button  →  Turn it ON",
            "Press the power button again  →  Turn it OFF"
        });

        StartCoroutine(ShowArrowForType(TutorialTarget.TargetType.PC));
        tutorialStep = 28;
    }

    /// <summary>
    /// Call from your power-button script whenever the PC is toggled inside the inspect view.
    /// <param name="poweredOn">true = just turned ON;  false = just turned OFF.</param>
    /// </summary>
    public void OnPCPowerToggled(bool poweredOn)
    {
        if (tutorialStep != 28) return;

        if (poweredOn && !powerOnDone)
        {
            powerOnDone = true;
            TaskListUI.Instance?.CompleteTask(1); // "Turn it ON" ✓
        }

        if (!poweredOn && powerOnDone && !powerOffDone)
        {
            powerOffDone = true;
            TaskListUI.Instance?.CompleteTask(2); // "Turn it OFF" ✓
        }

        if (powerOnDone && powerOffDone)
        {
            TaskListUI.Instance?.CompleteTask(0); // "Inspect finished PC" ✓
            HideArrow();
            tutorialStep = 29; // transition — prompt to exit inspect

            // No dialogue between power-test and the exit prompt
            StartCoroutine(DelayedHideAndDialogue(0.8f, null, StartExitInspectForEmailTask));
        }
    }

    // ══════════════════════════════════════════════════════════════
    //  ▌STEP 30 — EXIT INSPECTION  →  OPEN EMAIL
    //  (OnPlayerExitedInspection handles the actual advance at step 30)
    // ══════════════════════════════════════════════════════════════

    void StartExitInspectForEmailTask()
    {
        TaskListUI.Instance?.SetTitle("QUALITY CHECK");
        TaskListUI.Instance?.SetTasks(new string[]
        {
            "Press [Esc] or [Back] to exit Inspect Mode"
        });
        HideArrow();
        tutorialStep = 30;
        // Advance is triggered by OnPlayerExitedInspection() when the player closes inspect.
    }

    // ══════════════════════════════════════════════════════════════
    //  ▌STEP 32 — OPEN EMAIL / MARK JOB COMPLETE
    //
    //  ── Required change in PlayerInteract.cs ──────────────────
    //  Update the WorkstationMonitor block-during-tutorial check:
    //
    //    bool tutActive  = TutorialManager.Instance != null
    //                      && TutorialManager.Instance.IsTutorialActive();
    //    bool emailStep  = TutorialManager.Instance != null
    //                      && TutorialManager.Instance.IsEmailStep();
    //
    //    if (tutActive && !emailStep)
    //    {
    //        ShowPromptWithHighlight("X", "Finish your tasks first!", ...);
    //        return;
    //    }
    //
    //  ── Required hook in EmailManager / WorkstationMonitor ────
    //  When the email panel opens:
    //    if (TutorialManager.Instance != null)
    //        TutorialManager.Instance.CompleteEmailTask();
    // ─────────────────────────────────────────────────────────────

    void StartEmailTask()
    {
        TaskListUI.Instance?.SetTitle("SUBMIT JOB");
        TaskListUI.Instance?.SetTasks(new string[]
        {
            "Go to the workstation monitor",  // 0 — zone trigger
            "Press [E] to open your emails",  // 1 — E on monitor
            "Mark the job as complete"        // 2 — email action
        });
 
        if (emailTarget != null) ShowArrow(emailTarget);
        else StartCoroutine(ShowArrowForType(TutorialTarget.TargetType.Email));
 
        tutorialStep = 32;
    }
    public void CompleteApproachEmailTask()
    {
        if (tutorialStep != 32) return;
        TaskListUI.Instance?.CompleteTask(0); // "Go to monitor" green
    }

    /// <summary>Returns true when the player is allowed to open the email workstation monitor.</summary>
    public bool IsEmailStep() => tutorialStep == 32;

    /// <summary>
    /// Call from WorkstationMonitor / EmailManager when the email UI is opened during step 32.
    /// </summary>
    public void CompleteEmailTask()
    {
        if (tutorialStep != 32) return;
        TaskListUI.Instance?.CompleteTask(0);
        TaskListUI.Instance?.CompleteTask(1);
        TaskListUI.Instance?.CompleteTask(2);
        HideArrow();
        tutorialStep = 33;
        StartCoroutine(DelayedHideAndDialogue(1.0f, part17_WelcomeSpeech, FinishTutorial));
    }

    // ══════════════════════════════════════════════════════════════
    //  FINAL CLEANUP
    // ══════════════════════════════════════════════════════════════

    void FinishTutorial() => StartCoroutine(FinalCleanup());

    IEnumerator FinalCleanup()
    {
        yield return new WaitForSeconds(1.5f);

        TaskListUI.Instance?.HidePanel();
        HideArrow();

        tutorialStep = STEP_DONE;
        PlayerPrefs.SetInt("TutorialDone", 1);
        PlayerPrefs.Save();

        Debug.Log("[Tutorial] Tutorial complete! Welcome to Easy Express — enjoy the game.");
    }

    // ══════════════════════════════════════════════════════════════
    //  SHARED UTILITIES
    // ══════════════════════════════════════════════════════════════

    /// <summary>
    /// Waits, hides the task panel, then plays a dialogue sequence (or skips
    /// directly to the callback if the sequence is null or dialogueManager is missing).
    /// </summary>
    IEnumerator DelayedHideAndDialogue(float delay, DialogueSequence seq, System.Action callback)
    {
        yield return new WaitForSeconds(delay);
        TaskListUI.Instance?.HidePanel();
        yield return new WaitForSeconds(0.3f);

        if (seq != null && dialogueManager != null)
            dialogueManager.PlaySequence(seq, callback);
        else
            callback?.Invoke(); // no dialogue assigned → go straight to next task
    }

    /// <summary>
    /// Polls the scene every 0.2 s (up to 5 s) for a TutorialTarget of the given type,
    /// then aims the arrow at it.  Safe to call before the prefab is spawned.
    /// </summary>
    IEnumerator ShowArrowForType(TutorialTarget.TargetType type)
    {
        Transform found  = null;
        float     elapsed = 0f;

        while (found == null && elapsed < 5f)
        {
            TutorialTarget[] all = FindObjectsOfType<TutorialTarget>();
            foreach (TutorialTarget t in all)
            {
                if (t.type == type) { found = t.transform; break; }
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
            Debug.LogWarning($"[TutorialArrow] No TutorialTarget of type '{type}' found. " +
                             "Did you add TutorialTarget.cs to the prefab?");
    }

    void ShowArrow(Transform target) { tutorialArrow?.ShowAt(target); }
    void HideArrow()                 { tutorialArrow?.Hide(); }

    // ══════════════════════════════════════════════════════════════
    //  PUBLIC QUERY API  (used by other scripts each frame)
    // ══════════════════════════════════════════════════════════════

    /// <summary>Returns the current tutorial step number.</summary>
    public int GetCurrentStep() => tutorialStep;

    /// <summary>
    /// Returns true while the tutorial is actively running.
    /// Scripts use this to block menus, prevent NPCs from spawning, etc.
    /// </summary>
    public bool IsTutorialActive()
    {
        if (PlayerPrefs.GetInt("TutorialDone",  0) == 1) return false;
        if (PlayerPrefs.GetInt("IsLoadingGame", 0) == 1) return false;
        return tutorialStep < STEP_DONE;
    }
}
using UnityEngine;
using System.Collections;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance;

    public bool IsRepairStep() => tutorialStep >= 11 && tutorialStep <= 29;
    public bool IsShopPCStep() => tutorialStep == 17 || tutorialStep == 24;

    private const int STEP_DONE = 50;

    [Header("Managers")]
    public IntroDialogueManager dialogueManager;
    public DayTransitionManager dayTransitionManager;

    [Header("Dialogue — Block 1: Movement → Customer")]
    public DialogueSequence part01_IntroAndWASD;
    public DialogueSequence part02_GoCashier;
    public DialogueSequence part03_UseCashierPC;

    [Header("Dialogue — Block 2: Box → Inspect")]
    public DialogueSequence part04_PickupBox;
    public DialogueSequence part05_PlaceBox;

    private bool playerReachedCashier  = false;
    private bool customerReachedCashier = false;

    public DialogueSequence part06_OpenInspect;
    public DialogueSequence part07_HoverComponent;
    public DialogueSequence part08_RemoveComponent;

    [Header("Dialogue — Block 3: Shop → End Day")]
    public DialogueSequence part09_BuyParts;
    public DialogueSequence part10_EndDay;

    [Header("Dialogue — Block 4: New Day → Final")]
    public DialogueSequence part11_DeliveryArrived;
    public DialogueSequence part12_GoToStorage;
    public DialogueSequence part13_InstallPart;
    public DialogueSequence part14_FinishBuild;
    public DialogueSequence part15_PowerTest;
    public DialogueSequence part16_GoToEmail;
    public DialogueSequence part17_WelcomeSpeech;

    [Header("Arrow System")]
    public TutorialArrow tutorialArrow;

    [Header("Scene Transform Targets (drag from Hierarchy)")]
    public Transform workstationTarget;
    public Transform cashierTarget;
    public Transform cashierPCTarget;
    public Transform shopPCTarget;
    public Transform storageShelfTarget;
    public Transform emailTarget;
    public Transform doorTarget;

    [Header("WASD Task Settings")]
    public float requiredHoldTime = 2.0f;

    private float wTimer, aTimer, sTimer, dTimer;
    private bool  wDone, aDone, sDone, dDone;

    [Header("Tutorial Money Stipend")]
    public float tutorialStipend = 15000f;

    private int  tutorialStep  = 0;
    private bool powerOnDone   = false;
    private bool powerOffDone  = false;

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
        else { tutorialStep = 0; }
    }

    void OnDayStarted(int day)
    {
        if (tutorialStep == 0 && day == 1)
            StartCoroutine(DelayedStart());

        if (tutorialStep == 19 && day >= 2)
            StartCoroutine(DelayedHideAndDialogue(1.5f, part11_DeliveryArrived, StartPickupDeliveryTask));
    }

    public void RestartTutorial()
    {
        tutorialStep   = 0;
        wTimer = aTimer = sTimer = dTimer = 0f;
        wDone  = aDone  = sDone  = dDone  = false;
        powerOnDone    = false;
        powerOffDone   = false;
        HideArrow();
        StartCoroutine(DelayedStart());
    }

    public void HideTaskTemporarily() { TaskListUI.Instance?.HideTemporarily(); }
    public void RestoreTaskIfNeeded() { TaskListUI.Instance?.RestoreIfNeeded(); }

    // ══════════════════════════════════════════════════════════════
    //  STEP 1 — WASD
    // ══════════════════════════════════════════════════════════════

    IEnumerator DelayedStart()
    {
        yield return new WaitForSeconds(1.0f);

        // Force tutorial PC part chances
        PCPartDatabase db = FindObjectOfType<PCPartDatabase>();
        if (db != null)
        {
            db.gpuChance    = 0f;   // GPU always missing
            db.ramChance    = 0f;   // RAM always missing
            db.cpuChance    = 100f;
            db.storageChance = 100f;
            db.coolerChance = 100f;
        }

        if (dialogueManager == null || part01_IntroAndWASD == null)
        { tutorialStep = STEP_DONE; yield break; }

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
            tutorialStep = 2;
            ShopCustomerSpawner.Instance?.AllowSpawn();
            StartCoroutine(DelayedHideAndDialogue(1.0f, part02_GoCashier, StartGoCashierTask));
            
            // After spawning, strip GPU and RAM from the customer's job parts
            StartCoroutine(ForceRemoveGPUAndRAMFromTutorialJob());
        }
    }
   IEnumerator ForceRemoveGPUAndRAMFromTutorialJob()
{
    yield return new WaitForSeconds(0.8f);

    CustomerInside[] customers = FindObjectsOfType<CustomerInside>();
    foreach (CustomerInside c in customers)
    {
        if (c.assignedJob == null || c.assignedJob.startingParts == null)
        {
            Debug.Log("[Tutorial] Customer has no job or no parts!");
            continue;
        }

        Debug.Log($"[Tutorial] Customer has {c.assignedJob.startingParts.Count} parts:");
        foreach (StartingPCComponent part in c.assignedJob.startingParts)
            Debug.Log($"  - {part.partName} | category: '{part.partCategory}'");

        foreach (StartingPCComponent part in c.assignedJob.startingParts)
        {
            string cat = (part.partCategory ?? "").ToLower().Trim();
            string name = (part.partName ?? "").ToLower().Trim();

            if (cat == "gpu" || cat == "graphics card" || name.Contains("gpu") || name.Contains("gtx") || name.Contains("rtx") || name.Contains("rx "))
            {
                part.fault = PartFault.Broken;
                part.faultDescription = "GPU has failed and needs replacement.";
                Debug.Log($"[Tutorial] Marked GPU as broken: {part.partName}");
            }
            else if (cat == "ram" || cat == "memory" || name.Contains("ram") || name.Contains("ddr"))
            {
                part.fault = PartFault.Broken;
                part.faultDescription = "RAM stick is dead and needs replacement.";
                Debug.Log($"[Tutorial] Marked RAM as broken: {part.partName}");
            }
        }

        c.jobRequest = "My PC won't boot. I think the GPU and RAM are dead — can you replace them?";
        break;
    }
}
    // ══════════════════════════════════════════════════════════════
    //  STEP 3 — GO TO CASHIER
    // ══════════════════════════════════════════════════════════════

    void StartGoCashierTask()
    {
        playerReachedCashier   = false;
        customerReachedCashier = false;

        TaskListUI.Instance?.SetTitle("FIRST CUSTOMER");
        TaskListUI.Instance?.SetTasks(new string[]
        {
            "Head to the cashier counter",
            "Wait for the customer to arrive"
        });

        if (cashierTarget != null) ShowArrow(cashierTarget);
        else StartCoroutine(ShowArrowForType(TutorialTarget.TargetType.Cashier));

        tutorialStep = 3;
    }

    public void CompleteGoToCashierTask()
    {
        if (tutorialStep != 3 || playerReachedCashier) return;
        playerReachedCashier = true;
        TaskListUI.Instance?.CompleteTask(0);
        TryAdvanceFromCashierStep();
    }

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

        // Override the tutorial customer's job request
        CustomerInside[] customers = FindObjectsOfType<CustomerInside>();
        foreach (CustomerInside c in customers)
        {
            if (c.isAtSpot)
            {
                c.jobRequest = "My PC won't boot. I think the GPU and RAM are faulty — can you check?";
                break;
            }
        }

        HideArrow();
        tutorialStep = 4;
        StartCoroutine(DelayedHideAndDialogue(0.5f, part03_UseCashierPC, StartCashierPCTask));
    }

    // ══════════════════════════════════════════════════════════════
    //  STEP 5 — TALK TO CUSTOMER & ACCEPT JOB
    //  (No longer requires interacting with NPC's PC — just talk directly)
    // ══════════════════════════════════════════════════════════════

    void StartCashierPCTask()
    {
        TaskListUI.Instance?.SetTitle("NEW JOB REQUEST");
        TaskListUI.Instance?.SetTasks(new string[]
        {
            "Walk to the customer's PC",      // 0
            "Press [E] to inspect it",        // 1
            "Press [E] to talk to the customer", // 2
            "Accept the job"                  // 3
        });

        StartCoroutine(ShowArrowForType(TutorialTarget.TargetType.CashierPC));
        tutorialStep = 5;
    }
    public void CompleteCashierInspectTask()
    {
        if (tutorialStep != 5) return;
        TaskListUI.Instance?.CompleteTask(0);
        TaskListUI.Instance?.CompleteTask(1);
        // Arrow now points to customer NPC
        StartCoroutine(ShowArrowForType(TutorialTarget.TargetType.Customer));
    }

    public bool IsCashierPCStep() => tutorialStep == 5;

    public void ForceAcceptCurrentCustomer()
    {
        if (tutorialStep != 5) return;
        TaskListUI.Instance?.CompleteTask(1);
        CustomerInside[] customers = FindObjectsOfType<CustomerInside>();
        foreach (CustomerInside c in customers)
            if (c.isAtSpot) { c.AcceptJob(); break; }
        CompleteCashierPCTask();
    }

    public void CompleteCashierPCTask()
    {
        if (tutorialStep != 5) return;
        TaskListUI.Instance?.CompleteTask(0);
        TaskListUI.Instance?.CompleteTask(1);
        TaskListUI.Instance?.CompleteTask(2);
        TaskListUI.Instance?.CompleteTask(3); // ADD THIS — "Accept the job"
        HideArrow();
        tutorialStep = 6;
        StartCoroutine(DelayedHideAndDialogue(1.0f, part04_PickupBox, StartPickupBoxTask));
    }

    public void CompleteCustomerTask() => CompleteCashierPCTask();

    public void CompleteApproachCashierPCTask()
    {
        if (tutorialStep != 5) return;
        TaskListUI.Instance?.CompleteTask(0);
    }

    // ══════════════════════════════════════════════════════════════
    //  STEP 7 — PICK UP BOX
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

    public bool IsInspectPCAllowed()
    {
         return true; 
    }

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
    //  STEP 9 — PLACE BOX ON WORKSTATION
    // ══════════════════════════════════════════════════════════════

    public void CompleteApproachWorkstationTask()
    {
        if (tutorialStep != 9) return;
        TaskListUI.Instance?.CompleteTask(0);
    }

    void StartPlaceBoxTask()
    {
        TaskListUI.Instance?.SetTitle("INTAKE — STEP 1 / 3");
        TaskListUI.Instance?.SetTasks(new string[]
        {
            "Walk to the workstation desk",
            "Left-click to set the box down"
        });
        ShowArrow(workstationTarget);
        tutorialStep = 9;
    }

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
        if (tutorialStep == 17) TaskListUI.Instance?.CompleteTask(1);
        if (tutorialStep == 24) TaskListUI.Instance?.CompleteTask(0);
    }

    // ══════════════════════════════════════════════════════════════
    //  STEP 11 — OPEN PC INSPECTION
    // ══════════════════════════════════════════════════════════════

    void StartInspectTask()
    {
        TaskListUI.Instance?.SetTitle("DIAGNOSE — STEP 2 / 3");
        TaskListUI.Instance?.SetTasks(new string[]
        {
            "Look at the unpacked PC on the desk",
            "Press [E] to enter Inspect Mode",
            "Press [Tab] inside inspect to see your parts"
        });
        StartCoroutine(ShowArrowForType(TutorialTarget.TargetType.PC));
        tutorialStep = 11;
    }

    public void CompletePCTask()
    {
        if (tutorialStep != 11) return;
        TaskListUI.Instance?.CompleteTask(0);
        TaskListUI.Instance?.CompleteTask(1);
        TaskListUI.Instance?.CompleteTask(2);
        HideArrow();
        tutorialStep = 13;
        StartHoverTask();
    }

    // ══════════════════════════════════════════════════════════════
    //  STEP 13 — HOVER COMPONENT
    // ══════════════════════════════════════════════════════════════

    void StartHoverTask()
    {
        TaskListUI.Instance?.SetTitle("DIAGNOSE — STEP 2 / 3");
        TaskListUI.Instance?.SetTasks(new string[]
        {
            "Move your cursor over any component",
            "Read the component info tooltip"
        });
        HideArrow();
        tutorialStep = 13;
    }

    public void CompleteHoverTask()
    {
        if (tutorialStep != 13) return;
        TaskListUI.Instance?.CompleteTask(0);
        TaskListUI.Instance?.CompleteTask(1);
        tutorialStep = 14;
        StartCoroutine(DelayedHideAndDialogue(1.0f, part08_RemoveComponent, StartRemoveTask));
    }

    // ══════════════════════════════════════════════════════════════
    //  STEP 15 — REMOVE COMPONENT
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

    public void CompleteRemoveTask()
    {
        if (tutorialStep != 15) return;
        TaskListUI.Instance?.CompleteTask(0);
        HideArrow();
        tutorialStep = 16;
        StartCoroutine(ShowExitInspectPrompt());
    }

    IEnumerator ShowExitInspectPrompt()
    {
        yield return new WaitForSeconds(1.5f);
        TaskListUI.Instance?.SetTitle("DIAGNOSE — STEP 2 / 3");
        TaskListUI.Instance?.SetTasks(new string[]
        {
            "Press [Esc] or [Back] to exit Inspect Mode"
        });
    }

   public void OnPlayerExitedInspection()
    {
        if (tutorialStep == 5)
        {
        CompleteCashierInspectTask();
        return; // stay at step 5, waiting for customer talk
        }
        if (tutorialStep == 16)
        {
            TaskListUI.Instance?.CompleteTask(0);
            tutorialStep = 17;
            StartCoroutine(DelayedHideAndDialogue(0.5f, part09_BuyParts, StartBuyPartsTask));
        }
        else if (tutorialStep == 30)
        {
            TaskListUI.Instance?.CompleteTask(0);
            tutorialStep = 31;
            StartCoroutine(DelayedHideAndDialogue(0.5f, part16_GoToEmail, StartEmailTask));
        }
    }

    // ══════════════════════════════════════════════════════════════
    //  STEP 17 — BUY PARTS
    // ══════════════════════════════════════════════════════════════

    void StartBuyPartsTask()
    {
        PlayerWallet[] wallets = FindObjectsOfType<PlayerWallet>();
        foreach (PlayerWallet w in wallets) { w.currentGold = 0f; w.currentDebt = 0f; w.UpdateUI(); }
        if (wallets.Length > 0) wallets[0].AddGold(tutorialStipend);

        TaskListUI.Instance?.SetTitle("REPAIR — STEP 3 / 3");
        TaskListUI.Instance?.SetTasks(new string[]
        {
            $"You received ₱{tutorialStipend:F0} to order parts",
            "Go to the Shop PC",
            "Order the replacement component"
        });

        if (shopPCTarget != null)         ShowArrow(shopPCTarget);
        else if (cashierPCTarget != null) ShowArrow(cashierPCTarget);
        else StartCoroutine(ShowArrowForType(TutorialTarget.TargetType.ShopPC));

        tutorialStep = 17;
    }
    // ══════════════════════════════════════════════════════════════
    //  STEP 18 — END DAY
    // ══════════════════════════════════════════════════════════════
    public void CompleteOrderPartsTask()
    {
        if (tutorialStep != 17) return;
        TaskListUI.Instance?.CompleteTask(0);
        TaskListUI.Instance?.CompleteTask(1);
        TaskListUI.Instance?.CompleteTask(2);
        HideArrow();
        tutorialStep = 18;
        StartCoroutine(DelayedHideAndDialogue(1.0f, part10_EndDay, StartEndDayTask));
    }
    void StartEndDayTask()
    {
        TaskListUI.Instance?.SetTitle("END OF DAY");
        TaskListUI.Instance?.SetTasks(new string[]
        {
            "Walk to the shop door",
            "Open the door menu",
            "Select  ›  End Day"
        });
        if (doorTarget != null) ShowArrow(doorTarget);
        tutorialStep = 18;
    }

    public void CompleteApproachDoorTask()
    {
        if (tutorialStep != 18) return;
        TaskListUI.Instance?.CompleteTask(0);
    }

    public bool IsEndDayStep() => tutorialStep == 18;

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
    //  STEP 20 — PICK UP DELIVERY BOX
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
    //  STEP 22 — STORAGE SHELF
    // ══════════════════════════════════════════════════════════════

    void StartStorageShelfTask()
    {
        TaskListUI.Instance?.SetTitle("REPAIR — STEP 3 / 3");
        TaskListUI.Instance?.SetTasks(new string[]
        {
            "Go to the storage shelf",
            "Press [E] to store the delivery box",
            "Press [Tab] to open inventory and grab the component"
        });

        if (storageShelfTarget != null) ShowArrow(storageShelfTarget);
        else StartCoroutine(ShowArrowForType(TutorialTarget.TargetType.StorageShelf));

        tutorialStep = 22;
    }

    public void CompleteApproachStorageShelfTask()
    {
        if (tutorialStep != 22) return;
        TaskListUI.Instance?.CompleteTask(0);
    }

    public void CompleteStorageShelfTask()
    {
        if (tutorialStep != 22) return;
        TaskListUI.Instance?.CompleteTask(0);
        TaskListUI.Instance?.CompleteTask(1);
        TaskListUI.Instance?.CompleteTask(2);
        HideArrow();
        tutorialStep = 23;
        StartCoroutine(DelayedHideAndDialogue(1.0f, part13_InstallPart, StartInstallComponentTask));
    }

    // ══════════════════════════════════════════════════════════════
    //  STEP 24 — INSTALL COMPONENT VIA SHOP PC
    // ══════════════════════════════════════════════════════════════

    void StartInstallComponentTask()
    {
        TaskListUI.Instance?.SetTitle("REPAIR — STEP 3 / 3");
        TaskListUI.Instance?.SetTasks(new string[]
        {
            "Go to the customer's PC on the workstation",
            "Press [E] to inspect it",
            "Press [Tab] to see your parts",
            "Install the new component"
        });

        // Arrow points at the workstation PC, not the shop PC
        ShowArrow(workstationTarget);

        tutorialStep = 24;
    }

    public bool IsInstallComponentStep() => tutorialStep == 24;

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
    //  STEP 26 — FINISH BUILD
    // ══════════════════════════════════════════════════════════════

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
    //  STEP 28 — POWER TEST
    // ══════════════════════════════════════════════════════════════

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

    public void OnPCPowerToggled(bool poweredOn)
    {
        if (tutorialStep != 28) return;

        if (poweredOn && !powerOnDone)
        { powerOnDone = true; TaskListUI.Instance?.CompleteTask(1); }

        if (!poweredOn && powerOnDone && !powerOffDone)
        { powerOffDone = true; TaskListUI.Instance?.CompleteTask(2); }

        if (powerOnDone && powerOffDone)
        {
            TaskListUI.Instance?.CompleteTask(0);
            HideArrow();
            tutorialStep = 29;
            StartCoroutine(DelayedHideAndDialogue(0.8f, null, StartExitInspectForEmailTask));
        }
    }

    void StartExitInspectForEmailTask()
    {
        TaskListUI.Instance?.SetTitle("QUALITY CHECK");
        TaskListUI.Instance?.SetTasks(new string[]
        {
            "Press [Esc] or [Back] to exit Inspect Mode"
        });
        HideArrow();
        tutorialStep = 30;
    }

    // ══════════════════════════════════════════════════════════════
    //  STEP 32 — EMAIL
    // ══════════════════════════════════════════════════════════════

    void StartEmailTask()
    {
        TaskListUI.Instance?.SetTitle("SUBMIT JOB");
        TaskListUI.Instance?.SetTasks(new string[]
        {
            "Go to the workstation monitor",
            "Press [E] to open your emails",
            "Mark the job as complete"
        });

        if (emailTarget != null) ShowArrow(emailTarget);
        else StartCoroutine(ShowArrowForType(TutorialTarget.TargetType.Email));

        tutorialStep = 32;
    }

    public void CompleteApproachEmailTask()
    {
        if (tutorialStep != 32) return;
        TaskListUI.Instance?.CompleteTask(0);
    }

    public bool IsEmailStep() => tutorialStep == 32;

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
    //  FINAL
    // ══════════════════════════════════════════════════════════════

    void FinishTutorial() => StartCoroutine(FinalCleanup());

    IEnumerator FinalCleanup()
    {
        yield return new WaitForSeconds(1.5f);

        // Restore default PC part chances
        PCPartDatabase db = FindObjectOfType<PCPartDatabase>();
        if (db != null)
        {
            db.gpuChance    = 75f;
            db.ramChance    = 95f;
            db.cpuChance    = 100f;
            db.storageChance = 90.6f;
            db.coolerChance = 93f;
        }

        TaskListUI.Instance?.HidePanel();
        HideArrow();
        tutorialStep = STEP_DONE;
        PlayerPrefs.SetInt("TutorialDone", 1);
        PlayerPrefs.Save();
    }
    IEnumerator DelayedHideAndDialogue(float delay, DialogueSequence seq, System.Action callback)
    {
        yield return new WaitForSeconds(delay);
        TaskListUI.Instance?.HidePanel();
        yield return new WaitForSeconds(0.3f);
        if (seq != null && dialogueManager != null)
            dialogueManager.PlaySequence(seq, callback);
        else
            callback?.Invoke();
    }

    IEnumerator ShowArrowForType(TutorialTarget.TargetType type)
    {
        Transform found  = null;
        float     elapsed = 0f;

        while (found == null && elapsed < 5f)
        {
            TutorialTarget[] all = FindObjectsOfType<TutorialTarget>();
            foreach (TutorialTarget t in all)
                if (t.type == type) { found = t.transform; break; }

            if (found == null) { elapsed += 0.2f; yield return new WaitForSeconds(0.2f); }
        }

        if (found != null) ShowArrow(found);
        else Debug.LogWarning($"[TutorialArrow] No TutorialTarget of type '{type}' found.");
    }

    void ShowArrow(Transform target) { tutorialArrow?.ShowAt(target); }
    void HideArrow()                 { tutorialArrow?.Hide(); }

    public int  GetCurrentStep()  => tutorialStep;

    public bool IsTutorialActive()
    {
        if (PlayerPrefs.GetInt("TutorialDone",  0) == 1) return false;
        if (PlayerPrefs.GetInt("IsLoadingGame", 0) == 1) return false;
        return tutorialStep < STEP_DONE;
    }
}
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public partial class TutorialManager
{
    // ═══════════════════════════════════════════════════════════
    //  PHASE 1 — MOVEMENT & CUSTOMER
    // ═══════════════════════════════════════════════════════════

    IEnumerator DelayedStart()
    {
        yield return new WaitForSeconds(1.0f);
        PCPartDatabase db = GetDatabase();
        if (db != null)
        {
            db.gpuChance = 100f;
            db.ramChance = 100f;
            db.cpuChance = 100f;
            db.storageChance = 100f;
            db.coolerChance = 100f;
            db.buildJobChance = 0f;
        }
        else Debug.LogError("[Tutorial] No PCPartDatabase found!");

        if (dialogueManager == null || dlg_Intro == null)
        { step = STEP_DONE; yield break; }
        dialogueManager.PlaySequence(dlg_Intro, StartStep_WASD);
    }

    void StartStep_WASD()
    {
        wDone = aDone = sDone = dDone = false;
        wTimer = aTimer = sTimer = dTimer = 0f;
        SetTask("CALIBRATION",
            "Hold [W]  — Move Forward",
            "Hold [A]  — Move Left",
            "Hold [S]  — Move Backward",
            "Hold [D]  — Move Right");
        HideArrow();
        step = 1;
    }

    void UpdateWASD()
    {
        if (Input.GetKey(KeyCode.W)) wTimer += Time.deltaTime;
        if (Input.GetKey(KeyCode.A)) aTimer += Time.deltaTime;
        if (Input.GetKey(KeyCode.S)) sTimer += Time.deltaTime;
        if (Input.GetKey(KeyCode.D)) dTimer += Time.deltaTime;
        wTimer = Mathf.Clamp(wTimer, 0, requiredHoldTime);
        aTimer = Mathf.Clamp(aTimer, 0, requiredHoldTime);
        sTimer = Mathf.Clamp(sTimer, 0, requiredHoldTime);
        dTimer = Mathf.Clamp(dTimer, 0, requiredHoldTime);

        var ui = TaskListUI.Instance;
        if (ui != null)
        {
            if (!wDone && wTimer >= requiredHoldTime) { wDone = true; ui.CompleteTask(0); }
            if (!aDone && aTimer >= requiredHoldTime) { aDone = true; ui.CompleteTask(1); }
            if (!sDone && sTimer >= requiredHoldTime) { sDone = true; ui.CompleteTask(2); }
            if (!dDone && dTimer >= requiredHoldTime) { dDone = true; ui.CompleteTask(3); }
        }
        if (wDone && aDone && sDone && dDone)
        {
            step = 2;
            ShopCustomerSpawner.Instance?.AllowSpawn();
            Dialogue(1.0f, dlg_GoCashier, StartStep_GoCashier);
            StartCoroutine(ForceMarkGPUAndRAMBroken());
        }
    }

    void StartStep_GoCashier()
    {
        playerReachedCashier = customerReachedCashier = false;
        SetTask("FIRST CUSTOMER",
            "Head to the cashier counter",
            "Wait for the customer to arrive");
        if (cashierTarget != null) ShowArrow(cashierTarget);
        else StartCoroutine(ShowArrowForType(TutorialTarget.TargetType.Cashier));
        step = 3;
    }

    void TryAdvanceCashier()
    {
        if (!playerReachedCashier || !customerReachedCashier) return;
        foreach (CustomerInside c in FindObjectsOfType<CustomerInside>())
        {
            if (c.isAtSpot)
            {
                c.jobRequest = "My PC won't boot. I think the GPU and RAM are faulty — can you check?";
                break;
            }
        }
        HideArrow();
        step = 4;
        Dialogue(0.5f, dlg_TalkCustomer, StartStep_PreviewCounterPC);
    }

    void StartStep_PreviewCounterPC()
    {
        SetTask("PREVIEW CUSTOMER PC",
            "Walk to the customer's PC on the counter",
            "Press [E] to preview it (view only)",
            "Press [Esc] to exit preview",
            "Press [E] on the customer to talk");
        StartCoroutine(ShowArrowForType(TutorialTarget.TargetType.CashierPC));
        step = 5;
    }

    void StartStep_TalkToCustomer()
    {
        SetTask("ACCEPT THE JOB",
            "Press [E] to talk to the customer",
            "Accept the job");
        StartCoroutine(ShowArrowForType(TutorialTarget.TargetType.Customer));
        step = 6;
    }

    // ═══════════════════════════════════════════════════════════
    //  PHASE 2 — INTAKE
    // ═══════════════════════════════════════════════════════════

    void StartStep_PickupBox()
    {
        SetTask("INTAKE — STEP 1 / 2",
            "Find the customer's PC box",
            "Press [Q] to carry it");
        StartCoroutine(ShowArrowForType(TutorialTarget.TargetType.Box));
        step = 7;
    }

    void StartStep_PlaceBox()
    {
        SetTask("INTAKE — STEP 2 / 2",
            "Walk to the workstation desk",
            "Left-click to set the box down");
        ShowArrow(workstationTarget);
        step = 9;
    }

    // ═══════════════════════════════════════════════════════════
    //  PHASE 3 — DIAGNOSIS
    // ═══════════════════════════════════════════════════════════

    // ── Step 11: Enter Inspect Mode ──────────────────────────────

    void StartStep_InspectPC()
    {
        SetTask("DIAGNOSE — INSPECT",
            "Look at the PC on the desk",
            "Press [E] to enter Inspect Mode");
        StartCoroutine(ShowArrowForType(TutorialTarget.TargetType.PC));
        step = 11;
    }

    // ── Step 12: Connect Power Cord ──────────────────────────────

    void StartStep_ConnectPowerCord()
    {
        SetTask("DIAGNOSE — CONNECT POWER",
            "Click the power cord connector on the back of the PC",
            "The power cord must be plugged in before testing");
        HideArrow();
        step = 12;
    }

    // ── Step 13: Power-On Test ───────────────────────────────────

    void StartStep_PowerOnTest()
    {
        SetTask("DIAGNOSE — POWER TEST",
            "Press the Power Button to turn on the PC",
            "Observe what happens...");
        HideArrow();
        step = 13;
    }

    IEnumerator DelayThenAdvanceFromFirstPower()
    {
        yield return new WaitForSeconds(3.5f);
        if (step != 13) yield break;
        TaskListUI.Instance?.CompleteTask(1);
        yield return new WaitForSeconds(1.0f);
        step = 14;
        Dialogue(0.5f, dlg_OpenCase, StartStep_UnscrewPanel);
    }

    // ── Step 14: Unscrew + Remove Panel ──────────────────────────

    void StartStep_UnscrewPanel()
    {
        screwsRemoved = 0;
        panelRemoved = false;
        totalScrewsOnPanel = CountSidePanelScrews();

        SetTask("DIAGNOSE — OPEN CASE",
            "Select the Screwdriver (press 1)",
            $"Unscrew the side panel screws (0/{totalScrewsOnPanel})",
            "Remove the side panel");
        HideArrow();
        step = 14;
    }

    // ── Step 15: DIAGNOSIS LOOP ──────────────────────────────────

    void StartStep_DiagnoseLoop()
    {
        diagPhase = 0;
        faultyPartsRemoved = 0;
        totalFaultyParts = CountFaultyParts();
        ShowDiagTask_RemoveFirstSuspect();
        step = 15;
    }

    void ShowDiagTask_RemoveFirstSuspect()
    {
        SetTask("DIAGNOSE — PROCESS OF ELIMINATION",
            "Remove the GPU (most likely cause of 'no display')",
            "Use the screwdriver — hold click to remove");
        HideArrow();
    }

    void ShowDiagTask_PowerTestMidDiagnosis()
    {
        SetTask("DIAGNOSE — TEST",
            "Press the Power Button to test without the GPU",
            "Watch if the symptom changes...");
        HideArrow();
    }

    IEnumerator DelayThenAdvanceFromDiagPowerTest()
    {
        yield return new WaitForSeconds(3.0f);
        if (step != 15 || diagPhase != 1) yield break;
        TaskListUI.Instance?.CompleteTask(1);
        yield return new WaitForSeconds(0.5f);
        diagPhase = 2;
        Dialogue(0.5f, dlg_StillFailing, ShowDiagTask_RemoveSecondSuspect);
    }

    void ShowDiagTask_RemoveSecondSuspect()
    {
        SetTask("DIAGNOSE — NEXT SUSPECT",
            "The symptom changed — POST failure means RAM could be bad",
            "Remove the RAM stick");
        HideArrow();
    }

    void OnAllFaultsFound()
    {
        CompleteAllTasks();
        HideArrow();
        Dialogue(0.5f, dlg_FoundAllFaults, () =>
        {
            step = 17;
            StartStep_ExitInspectForStorage();
        });
    }

    // ── Part Removal Handler (steps 14 + 15) ─────────────────────

    void HandlePartRemoval(InspectableItem part)
    {
        if (part == null) return;
        string partName = (part.itemName ?? "").ToLower();
        string partCat = (part.partCategory ?? "").ToLower();

        // ═══ STEP 14: SCREW + PANEL ═══
        if (step == 14)
        {
            if (!panelRemoved && IsScrew(partName, partCat))
            {
                screwsRemoved++;
                TaskListUI.Instance?.UpdateTaskText(1,
                    $"Unscrew the side panel screws ({screwsRemoved}/{totalScrewsOnPanel})");
                if (screwsRemoved == 1) TaskListUI.Instance?.CompleteTask(0);
                if (screwsRemoved >= totalScrewsOnPanel) TaskListUI.Instance?.CompleteTask(1);
                return;
            }
            if (!panelRemoved && IsPanel(partName, partCat))
            {
                panelRemoved = true;
                CompleteAllTasks();
                Dialogue(0.5f, dlg_IdentifyFaults, StartStep_DiagnoseLoop);
                return;
            }
            return;
        }

        // ═══ STEP 15: DIAGNOSIS LOOP ═══
        if (step != 15) return;

        if (diagPhase == 0 && IsSuspectPart(partCat, "GPU"))
        {
            faultyPartsRemoved++;
            TaskListUI.Instance?.CompleteTask(0);
            TaskListUI.Instance?.CompleteTask(1);
            diagPhase = 1;
            Dialogue(0.5f, dlg_TestAfterFirstRemoval, ShowDiagTask_PowerTestMidDiagnosis);
            return;
        }

        if (diagPhase == 2 && IsSuspectPart(partCat, "RAM"))
        {
            faultyPartsRemoved++;
            TaskListUI.Instance?.CompleteTask(0);
            TaskListUI.Instance?.CompleteTask(1);
            diagPhase = 3;
            OnAllFaultsFound();
            return;
        }

        Debug.Log($"[Tutorial] Removed '{part.itemName}' but expecting " +
                  $"{(diagPhase <= 1 ? "GPU" : "RAM")} — ignoring.");
    }

    // ── Step 17: Exit Inspection → Storage ───────────────────────

    void StartStep_ExitInspectForStorage()
    {
        SetTask("DIAGNOSIS COMPLETE",
            "Press [Esc] to exit Inspect Mode",
            "Grab replacements from your storage shelf!");
        HideArrow();
        step = 17;
    }

    // ═══════════════════════════════════════════════════════════
    //  PHASE 4 — STORAGE & REPAIR
    // ═══════════════════════════════════════════════════════════

    void StartStep_StorageShelf()
    {
        SetTask("STORAGE — GRAB PARTS",
            "Walk to the storage shelf",
            "Press [E] to open the Storage Panel",
            "Click the GPU to grab it",
            "Click the RAM to grab it",
            "Press [Esc] to close the panel");
        if (storageShelfTarget != null) ShowArrow(storageShelfTarget);
        else StartCoroutine(ShowArrowForType(TutorialTarget.TargetType.StorageShelf));
        step = 21;
    }

    void StartStep_InstallComponent()
    {
        SetTask("REPAIR — INSTALL",
            "Go to the customer's PC on the workstation",
            "Press [E] to inspect it",
            "Press [Tab] to see your parts",
            "Install the new GPU and RAM");
        ShowArrow(workstationTarget);
        step = 22;
    }

    void StartStep_FinalPowerTest()
    {
        powerOnDone = powerOffDone = false;
        SetTask("QUALITY CHECK",
            "Test the repaired PC",
            "Press the power button → Turn it ON",
            "Press the power button → Turn it OFF");
        StartCoroutine(ShowArrowForType(TutorialTarget.TargetType.PC));
        step = 23;
    }

    // ═══════════════════════════════════════════════════════════
    //  PHASE 5 — EMAIL & SHOP
    // ═══════════════════════════════════════════════════════════

    void StartStep_ExitInspectForEmail()
    {
        SetTask("REPAIR COMPLETE!",
            "Press [Esc] to exit Inspect Mode");
        HideArrow();
        step = 24;
    }

    void StartStep_EmailTutorial()
    {
        SetTask("SUBMIT JOB",
            "Go to the workstation monitor",
            "Press [E] to open your emails",
            "Find the completed job in your inbox",
            "Mark the job as complete");
        if (emailTarget != null) ShowArrow(emailTarget);
        else StartCoroutine(ShowArrowForType(TutorialTarget.TargetType.Email));
        step = 25;
    }

    void StartStep_ShopTutorial()
    {
        SetTask("SHOP TUTORIAL",
            "Go to the Shop PC",
            "Browse the different component categories",
            "Add items to your cart",
            "Keep your storage stocked!");
        if (shopPCTarget != null) ShowArrow(shopPCTarget);
        else StartCoroutine(ShowArrowForType(TutorialTarget.TargetType.ShopPC));
        step = 26;
    }

    public void CompleteShopTutorial()
    {
        if (step != 26) return;
        CompleteAllTasks(); HideArrow();
        step = 27;
        Dialogue(1.0f, dlg_WelcomeSpeech, FinishTutorial);
    }

    void FinishTutorial() => StartCoroutine(FinalCleanup());

    IEnumerator FinalCleanup()
    {
        yield return new WaitForSeconds(1.5f);
        PCPartDatabase db = GetDatabase();
        if (db != null)
        {
            db.gpuChance = 75f;
            db.ramChance = 95f;
            db.cpuChance = 100f;
            db.storageChance = 90.6f;
            db.coolerChance = 93f;
            db.buildJobChance = 35f;
        }
        TaskListUI.Instance?.HidePanel();
        HideArrow(); HidePointer();
        step = STEP_DONE;
        PlayerPrefs.SetInt("TutorialDone", 1);
        PlayerPrefs.Save();
    }

    // ═══════════════════════════════════════════════════════════
    //  PC SETUP
    // ═══════════════════════════════════════════════════════════

    IEnumerator ForceMarkGPUAndRAMBroken()
    {
        yield return new WaitForSeconds(1.2f);
        foreach (CustomerInside c in FindObjectsOfType<CustomerInside>())
        {
            if (c.assignedJob == null || c.assignedJob.startingParts == null) continue;
            bool gpuDone = false, ramDone = false;
            foreach (StartingPCComponent part in c.assignedJob.startingParts)
            {
                string cat = (part.partCategory ?? "").Trim();
                string name = (part.partName ?? "").ToLower();
                if (!gpuDone && (cat == "GPU" || name.Contains("gpu") || name.Contains("gtx")
                    || name.Contains("rtx") || name.Contains("radeon") || name.Contains("geforce")))
                {
                    part.fault = PartFault.Broken;
                    part.faultDescription = "GPU has failed — no display output.";
                    gpuDone = true;
                }
                else if (!ramDone && (cat == "RAM" || name.Contains("ram") || name.Contains("ddr")))
                {
                    part.fault = PartFault.Broken;
                    part.faultDescription = "RAM stick is dead — system fails POST.";
                    ramDone = true;
                }
            }
            int faults = 0;
            foreach (var p in c.assignedJob.startingParts) if (p.fault != PartFault.None) faults++;
            c.assignedJob.originalFaultCount = faults;
            c.assignedJob.pcProblems = new string[] { "No Display on Monitor" };
            c.jobRequest = "My PC won't boot. I think the GPU and RAM are dead — can you replace them?";
            break;
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  HELPERS
    // ═══════════════════════════════════════════════════════════

    bool IsScrew(string name, string category)
    {
        return name.Contains("screw") || name.Contains("bolt") || name.Contains("fastener")
            || category.Contains("screw") || category.Contains("bolt");
    }

    bool IsPanel(string name, string category)
    {
        return name.Contains("panel") || category.Contains("panel");
    }

    bool IsSuspectPart(string partCategory, string suspectCategory)
    {
        return partCategory.Equals(suspectCategory, System.StringComparison.OrdinalIgnoreCase);
    }

    int CountSidePanelScrews()
    {
        int count = 0;
        foreach (InspectableItem item in FindObjectsOfType<InspectableItem>(true))
        {
            if (item == null || !item.isRemovable) continue;
            string n = (item.itemName ?? "").ToLower();
            string c = (item.partCategory ?? "").ToLower();
            if (IsScrew(n, c)) count++;
        }
        if (count == 0) { count = 4; }
        return count;
    }

    int CountFaultyParts()
    {
        int count = 0;
        foreach (CustomerInside c in FindObjectsOfType<CustomerInside>())
        {
            if (c.assignedJob == null || c.assignedJob.startingParts == null) continue;
            foreach (var p in c.assignedJob.startingParts)
                if (p.fault != PartFault.None) count++;
            break;
        }
        return Mathf.Max(count, 1);
    }
}
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
            db.gpuChance = 100f; db.ramChance = 100f;
            db.cpuChance = 100f; db.storageChance = 100f;
            db.coolerChance = 100f; db.buildJobChance = 0f;
        }
        else Debug.LogError("[Tutorial] No PCPartDatabase found!");

        PopulateTutorialStock();

        if (dialogueManager == null || dlg_Intro == null)
        { step = STEP_DONE; yield break; }
        dialogueManager.PlaySequence(dlg_Intro, StartStep_WASD);
    }

    void PopulateTutorialStock()
    {
        if (ShopSystem.Instance == null) { Debug.LogWarning("[Tutorial] ShopSystem not found."); return; }
        List<string> ids = ShopSystem.Instance.GetInventoryIDs();
        List<ItemData> allItems = ShopSystem.Instance.allAvailableItems;
        if (allItems == null || allItems.Count == 0) { Debug.LogWarning("[Tutorial] No shop items."); return; }

        ids.Clear();
        int total = 0;
        foreach (ItemData item in allItems)
        {
            if (item == null || item.itemType != ItemCategory.PCPart) continue;
            for (int i = 0; i < tutorialStockPerItem; i++) { ids.Add(item.id); total++; }
        }
        Debug.Log($"[Tutorial] Pre-stocked {total} items ({tutorialStockPerItem} each).");
    }

    void StartStep_WASD()
    {
        wDone = aDone = sDone = dDone = false;
        wTimer = aTimer = sTimer = dTimer = 0f;
        SetTask("TASK",
            "Hold [W]  — Move Forward", "Hold [A]  — Move Left",
            "Hold [S]  — Move Backward", "Hold [D]  — Move Right");
        HideArrow(); step = 1;
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
        SetTask("TASK", "Head to the cashier counter", "Wait for the customer to arrive");
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
            { c.jobRequest = "My PC won't boot. Can you please fix it?"; break; }
        }
        HideArrow(); step = 4;
        Dialogue(0.5f, dlg_TalkCustomer, StartStep_PreviewCounterPC);
    }

    // ═══════════════════════════════════════════════════════════
    //  PHASE 2 — PC SUMMARY + INTAKE
    //  Step 5: Inspect desk PC + teach G key for PC Summary
    // ═══════════════════════════════════════════════════════════

    void StartStep_PreviewCounterPC()
    {
        summaryShown = false;
        SetTask("TASK",
            "Walk to the customer's PC on the counter",
            "Press [E] to inspect it",
            "Press [G] to open the PC Summary Panel",
            "Press [Esc] to exit inspection",
            "Press [E] on the customer to talk");
        StartCoroutine(ShowArrowForType(TutorialTarget.TargetType.CashierPC));
        step = 5;
    }

    void StartStep_TalkToCustomer()
    {
        SetTask("TASK", "Press [E] to talk to the customer", "Accept the job");
        StartCoroutine(ShowArrowForType(TutorialTarget.TargetType.Customer));
        step = 6;
    }

    // ═══════════════════════════════════════════════════════════
    //  PHASE 2b — INTAKE (same as before)
    // ═══════════════════════════════════════════════════════════

    void StartStep_PickupBox()
    {
        SetTask("TASK", "Find the customer's PC box", "Press [Q] to carry it");
        StartCoroutine(ShowArrowForType(TutorialTarget.TargetType.Box));
        step = 7;
    }

    void StartStep_PlaceBox()
    {
        SetTask("TASK", "Walk to the workstation desk", "Left-click to set the box down");
        ShowArrow(workstationTarget); step = 9;

        if (boxPlacedEarly)
        {
            boxPlacedEarly = false;
            CompletePlaceBoxTask();
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  PHASE 3 — DIAGNOSIS
    // ═══════════════════════════════════════════════════════════

    void StartStep_InspectPC()
    {
        SetTask("TASK — INSPECT", "Look at the PC on the desk", "Press [E] to enter Inspect Mode");
        StartCoroutine(ShowArrowForType(TutorialTarget.TargetType.PC)); step = 11;
    }

    void StartStep_ConnectPowerCord()
    {
        SetTask("TASK — CONNECT POWER",
            "Click the power cord connector on the back of the PC",
            "The power cord must be plugged in before testing");
        HideArrow(); step = 12;
    }

    void StartStep_PowerOnTest()
    {
        SetTask("TASK — POWER TEST", "Press the Power Button to turn on the PC", "Observe what happens...");
        HideArrow(); step = 13;
        RefreshInspectionHighlight(); // highlight the power button
    }

    // ── Step 13 → 13b: After first power test (FailedPOST) ──────

    IEnumerator DelayThenAdvanceFromFirstPower()
    {
        yield return new WaitForSeconds(3.5f);
        if (step != 13) yield break;
        TaskListUI.Instance?.CompleteTask(1);
        tutorialInspectionHighlight?.Hide(); // clear power button highlight
        yield return new WaitForSeconds(1.0f);

        // Instead of explaining the problem, tell the player to check the manual
        Dialogue(0.5f, dlg_CheckManual, StartStep_CheckManual);
    }

    // ── Step 13b: Check Repair Manual ────────────────────────────

    void StartStep_CheckManual()
    {
        manualOpened = false;
        waitingForManualOpen = true;
        SetTask("REPAIR MANUAL",
            "Press [F] to open the Repair Manual",
            "Find the troubleshooting guide for the customer's problem");
        HideArrow();
    }

    // ── Step 14: Exit → Storage ──────────────────────────────────

    void StartStep_ExitForStorage()
    {
        grabbedGPU = grabbedRAM = false;
        SetTask("TASK",
            "Press [Esc] to exit Inspect Mode", "Walk to the storage shelf",
            "Press [E] to open the Storage Panel", "Click the GPU to grab it", "Click the RAM to grab it");
        HideArrow(); step = 14;
    }

    void OnExitInspectForStorage()
    {
        TaskListUI.Instance?.CompleteTask(0);
        if (storageShelfTarget != null) ShowArrow(storageShelfTarget);
        else StartCoroutine(ShowArrowForType(TutorialTarget.TargetType.StorageShelf));
    }

    void OnStoragePartsGrabbed()
    {
        CompleteAllTasks(); HideArrow(); step = 15;
        Dialogue(0.5f, dlg_OpenCase, StartStep_ReenterInspectAndOpenCase);
    }

    // ── Step 15: Re-enter Inspect → Open Case ────────────────────

    void StartStep_ReenterInspectAndOpenCase()
    {
        screwsRemoved = 0; panelRemoved = false;
        SetTask("TASK",
            "Go back to the PC on the workstation", "Press [E] to enter Inspect Mode",
            "Select the Screwdriver (press 1)", "Unscrew the side panel screws (0/?)", "Remove the side panel");
        ShowArrow(workstationTarget); step = 15;
    }

    void OnReenteredInspect()
    {
        totalScrewsOnPanel = CountSidePanelScrews();
        TaskListUI.Instance?.CompleteTask(0);
        TaskListUI.Instance?.CompleteTask(1);
        TaskListUI.Instance?.UpdateTaskText(3, $"Unscrew the side panel screws (0/{totalScrewsOnPanel})");
        RefreshInspectionHighlight();
    }

    // ═══════════════════════════════════════════════════════════
    //  Step 16: Swap-and-Test Loop (RAM FIRST)
    // ═══════════════════════════════════════════════════════════

    void StartStep_SwapAndTest()
    {
        diagState = DiagState.RemoveOldRAM;  // START WITH RAM
        ShowSwapTask(); step = 16;
    }

    void ShowSwapTask()
    {
        HideArrow();
        switch (diagState)
        {
            // RAM swap — PC already off from auto-shutdown, no "Turn off" needed
            case DiagState.RemoveOldRAM:
                SetTask("SWAP TEST — RAM", "Remove the old RAM stick", "Use the screwdriver — hold click to remove"); break;
            case DiagState.InstallNewRAM:
                SetTask("SWAP TEST — RAM", "Press [Tab] to see your parts", "Install the new RAM into the empty slot"); break;
            case DiagState.TestAfterRAMSwap:
                SetTask("SWAP TEST — RAM", "Press the Power Button to test", "Watch what happens..."); break;

            // GPU wire disconnect (NEW)
            case DiagState.DisconnectGPUWire:
                SetTask("SWAP TEST — GPU", "Turn off the PC (click Power Button)", "Disconnect the GPU power cable", "Click and hold the GPU wire port"); break;

            // GPU swap — PC already off from DisconnectGPUWire step
            case DiagState.RemoveOldGPU:
                SetTask("SWAP TEST — GPU", "Remove the old GPU", "Use the screwdriver — hold click to remove"); break;
            case DiagState.InstallNewGPU:
                SetTask("SWAP TEST — GPU", "Press [Tab] to see your parts", "Install the new GPU into the empty slot"); break;
            case DiagState.TestAfterGPUSwap:
                SetTask("SWAP TEST — GPU", "Press the Power Button to test", "Watch the display this time..."); break;

            // RAM verify — PC stays on after Success, needs manual turn off
            case DiagState.RemoveNewRAM:
                SetTask("VERIFY — RAM", "Turn off the PC (click Power Button)", "Remove the new RAM you just installed", "We need to confirm the old RAM was the problem"); break;
            case DiagState.InstallOldRAM:
                SetTask("VERIFY — RAM", "Press [Tab] to see your parts", "Install the OLD RAM (the faulty one)"); break;
            case DiagState.TestWithOldRAM:
                SetTask("VERIFY — RAM", "Press the Power Button to test with old RAM", "Does the problem come back?"); break;

            // Final fix — PC already off from auto-shutdown
            case DiagState.RemoveOldRAMAgain:
                SetTask("FINAL FIX", "Remove the old RAM again", "RAM confirmed bad — put the good one back"); break;
            case DiagState.InstallNewRAMFinal:
                SetTask("FINAL FIX", "Press [Tab] to see your parts", "Install the new RAM to finish the repair"); break;
            case DiagState.TestFinal:
                SetTask("FINAL FIX", "Press the Power Button for the final test", "This should boot clean!"); break;

            // Close up — reinstall side panel and screws before shipping
            case DiagState.TurnOffForClose:
                SetTask("CLOSE UP", "Turn off the PC (click Power Button)", "Time to close up the case"); break;
            case DiagState.CloseUpPC:
                {
                    int remaining = CountRemainingPanelGhosts();
                    int totalParts = totalScrewsOnPanel + 1;
                    int done = totalParts - remaining;
                    SetTask("CLOSE UP",
                        $"Reinstall the side panel and screws ({done}/{totalParts})",
                        "Press [Tab] to grab parts from inventory",
                        "Click the highlighted ghost slot to install");
                    break;
                }
        }
        // Delay by one frame so task UI settles and highlight targets are findable.
        // This fixes the RAM highlight not appearing for RemoveOldRAM.
        StartCoroutine(PostShowSwapSetup());
    }

    /// <summary>
    /// Runs one frame after ShowSwapTask sets up the task list.
    /// Refreshes the inspection highlight, then auto-completes any
    /// tasks the player already finished before reaching this state.
    /// </summary>
    IEnumerator PostShowSwapSetup()
    {
        yield return null; // wait one frame for UI to settle
        RefreshInspectionHighlight();
        CheckSwapAutoComplete();
    }

    /// <summary>
    /// Auto-completes tasks the player already did before the tutorial
    /// told them to, preventing soft-locks.
    /// </summary>
    void CheckSwapAutoComplete()
    {
        if (step != 16) return;

        InspectionManager im = FindFirstObjectByType<InspectionManager>();
        if (im == null || im.currentClone == null) return;

        PCPowerSystem ps = im.currentClone.GetComponent<PCPowerSystem>();
        bool pcOff = ps == null || !ps.isPoweredOn;

        switch (diagState)
        {
            case DiagState.DisconnectGPUWire:
                if (pcOff)
                {
                    TaskListUI.Instance?.CompleteTask(0);
                    bool wireStillConnected = false;
                    foreach (MonoBehaviour mb in im.currentClone.GetComponentsInChildren<MonoBehaviour>(true))
                    {
                        IPrebuiltWire w = mb as IPrebuiltWire;
                        if (w != null && w.IsConnected && w.RequiredPartCategory == "GPU")
                        { wireStillConnected = true; break; }
                    }
                    if (!wireStillConnected)
                    { CompleteAllTasks(); diagState = DiagState.RemoveOldGPU; ShowSwapTask(); }
                    else
                    { StartCoroutine(RefreshHighlightNextFrame()); }
                }
                break;

            case DiagState.RemoveNewRAM:
                if (pcOff)
                { TaskListUI.Instance?.CompleteTask(0); StartCoroutine(RefreshHighlightNextFrame()); }
                break;

            case DiagState.TurnOffForClose:
                if (pcOff)
                {
                    TaskListUI.Instance?.CompleteTask(0);
                    TaskListUI.Instance?.CompleteTask(1);
                    diagState = DiagState.CloseUpPC;
                    ShowSwapTask();
                }
                break;

            case DiagState.CloseUpPC:
                if (CountRemainingPanelGhosts() <= 0)
                {
                    CompleteAllTasks();
                    diagState = DiagState.Done;
                    Dialogue(0.5f, dlg_RepairComplete, () =>
                    {
                        step = 24;
                        StartStep_ExitInspectForCompletion();
                    });
                }
                break;
        }
    }

    // ── Part Removal (steps 15 + 16) ─────────────────────────────

    void HandlePartRemoval(InspectableItem part)
    {
        if (part == null) return;

        string partName = ((part.itemName ?? "") + " " + part.gameObject.name).ToLower();
        string partCat = (part.partCategory ?? "").ToLower();

        // ═══ STEP 15: SCREW + PANEL ═══
        if (step == 15)
        {
            if (!panelRemoved && IsPanelScrew(partName, partCat))
            {
                screwsRemoved++;
                TaskListUI.Instance?.UpdateTaskText(3,
                    $"Unscrew the side panel screws ({screwsRemoved}/{totalScrewsOnPanel})");
                if (screwsRemoved == 1) TaskListUI.Instance?.CompleteTask(2);
                if (screwsRemoved >= totalScrewsOnPanel) TaskListUI.Instance?.CompleteTask(3);
                StartCoroutine(RefreshHighlightNextFrame());
                return;
            }
            if (!panelRemoved && IsPanel(partName, partCat) && !partName.Contains("back"))
            {
                panelRemoved = true;
                CompleteAllTasks();
                Dialogue(0.5f, dlg_SwapRAMFirst, StartStep_SwapAndTest);
                return;
            }
            return;
        }

        // ═══ STEP 16: SWAP-AND-TEST ═══
        if (step != 16) return;

        switch (diagState)
        {
            case DiagState.RemoveOldRAM:
                if (!IsSuspectPart(partCat, "ram")) break;
                Debug.Log($"[Tutorial] Old RAM removed. fault={part.fault}");
                CompleteAllTasks(); diagState = DiagState.InstallNewRAM; ShowSwapTask(); return;

            case DiagState.RemoveOldGPU:
                if (!IsSuspectPart(partCat, "gpu")) break;
                Debug.Log($"[Tutorial] Old GPU removed. fault={part.fault}");
                CompleteAllTasks(); diagState = DiagState.InstallNewGPU; ShowSwapTask(); return;

            case DiagState.RemoveNewRAM:
                if (!IsSuspectPart(partCat, "ram")) break;
                CompleteAllTasks(); diagState = DiagState.InstallOldRAM; ShowSwapTask(); return;

            case DiagState.RemoveOldRAMAgain:
                if (!IsSuspectPart(partCat, "ram")) break;
                CompleteAllTasks(); diagState = DiagState.InstallNewRAMFinal; ShowSwapTask(); return;
        }

        Debug.Log($"[Tutorial] Removed '{part.itemName}' / '{part.gameObject.name}' but DiagState={diagState} — ignoring.");
    }

    // ── Install Handler (step 16) ────────────────────────────────

    void HandlePartInstall_SwapTest()
    {
        switch (diagState)
        {
            case DiagState.InstallNewRAM:
                CompleteAllTasks();
                TrackInstalledPartName("RAM");
                diagState = DiagState.TestAfterRAMSwap;
                ShowSwapTask();
                return;

            case DiagState.InstallNewGPU:
                CompleteAllTasks();
                TrackInstalledPartName("GPU");
                diagState = DiagState.TestAfterGPUSwap;
                ShowSwapTask();
                return;

            case DiagState.InstallOldRAM:
                CompleteAllTasks(); diagState = DiagState.TestWithOldRAM; ShowSwapTask(); return;

            case DiagState.InstallNewRAMFinal:
                CompleteAllTasks(); diagState = DiagState.TestFinal; ShowSwapTask(); return;

            case DiagState.CloseUpPC:
                // Delay by one frame — TryInstallPart calls Destroy(slot)
                // which is deferred to end-of-frame, so CountRemainingPanelGhosts
                // would still see the just-destroyed ghost on this frame.
                StartCoroutine(CheckCloseUpNextFrame());
                return;
        }
        Debug.Log($"[Tutorial] Part installed but DiagState={diagState} — ignoring.");
    }

    /// <summary>
    /// Waits one frame for Destroy() to complete, then counts remaining
    /// panel/screw ghost slots and advances or updates the task text.
    /// </summary>
    IEnumerator CheckCloseUpNextFrame()
    {
        yield return null; // wait for Destroy() to finish
        int remaining = CountRemainingPanelGhosts();
        if (remaining <= 0)
        {
            CompleteAllTasks(); HideArrow();
            diagState = DiagState.Done;
            Dialogue(0.5f, dlg_RepairComplete, () =>
            {
                step = 24;
                StartStep_ExitInspectForCompletion();
            });
        }
        else
        {
            int totalParts = totalScrewsOnPanel + 1;
            int done = totalParts - remaining;
            TaskListUI.Instance?.UpdateTaskText(0,
                $"Reinstall the side panel and screws ({done}/{totalParts})");
            StartCoroutine(RefreshHighlightNextFrame());
        }
    }

    // ── Power Test Handler (step 16) ─────────────────────────────

    void HandleDiagPowerTest()
    {
        TaskListUI.Instance?.CompleteTask(0);

        InspectionManager im = FindObjectOfType<InspectionManager>();
        if (im != null && im.currentClone != null)
        {
            PCPowerSystem ps = im.currentClone.GetComponent<PCPowerSystem>();
            if (ps != null)
                Debug.Log($"[Tutorial] Power test at {diagState}: result={ps.lastPowerResult}, isPoweredOn={ps.isPoweredOn}");
        }

        StartCoroutine(DelayedDiagPowerAdvance());
    }

    IEnumerator DelayedDiagPowerAdvance()
    {
        yield return new WaitForSeconds(3.5f);
        if (step != 16) yield break;

        TaskListUI.Instance?.CompleteTask(1);
        tutorialInspectionHighlight?.Hide(); // clear power button highlight before dialogue
        yield return new WaitForSeconds(0.5f);

        switch (diagState)
        {
            // ── After RAM swap: NoDisplay (GPU still broken) ──
            case DiagState.TestAfterRAMSwap:
                Dialogue(0.5f, dlg_RAMSwapNoDisplay, () =>
                {
                    waitingForManualNoDisplay = true;
                    manualNoDisplayOpened = false;
                    SetTask("CHECK MANUAL",
                        "Press [F] to check the manual for 'No Display' problems");
                });
                break;

            // ── After GPU swap: Success! Both parts working ──
            case DiagState.TestAfterGPUSwap:
                Dialogue(0.5f, dlg_GPUSwapSuccess, () =>
                {
                    diagState = DiagState.RemoveNewRAM;
                    ShowSwapTask();
                });
                break;

            // ── After putting old RAM back: FailedPOST (confirms RAM is dead) ──
            case DiagState.TestWithOldRAM:
                Dialogue(0.5f, dlg_RAMVerifiedBroken, () =>
                {
                    diagState = DiagState.RemoveOldRAMAgain;
                    ShowSwapTask();
                });
                break;

            // ── Final test: Success → close up the PC before completing! ──
            case DiagState.TestFinal:
                CompleteAllTasks(); HideArrow();
                Dialogue(0.5f, dlg_CloseUpPC, () =>
                {
                    diagState = DiagState.TurnOffForClose;
                    ShowSwapTask();
                });
                break;
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  PHASE 4 — COMPLETION
    // ═══════════════════════════════════════════════════════════

    void StartStep_ExitInspectForCompletion()
    {
        SetTask("REPAIR COMPLETE!", "Press [Esc] to exit Inspect Mode");
        HideArrow(); step = 24;
    }

    void StartStep_EmailTutorial()
    {
        SetTask("SUBMIT JOB",
            "Go to the workstation monitor", "Press [E] to open your emails",
            "Find the completed job in your inbox", "Mark the job as complete");
        if (emailTarget != null) ShowArrow(emailTarget);
        else StartCoroutine(ShowArrowForType(TutorialTarget.TargetType.Email));
        step = 25;
    }

    void StartStep_ShopTutorial()
    {
        shopBrowsed = shopAddedToCart = false;
        SetTask("SHOP TUTORIAL",
            "Go to the Shop PC", "Browse the different component categories",
            "Add items to your cart", "Keep your storage stocked!");
        if (shopPCTarget != null) ShowArrow(shopPCTarget);
        else StartCoroutine(ShowArrowForType(TutorialTarget.TargetType.ShopPC));
        step = 26;
    }

    public void CompleteShopTutorial()
    {
        if (step != 26) return;
        CompleteAllTasks(); HideArrow();
        step = 27;
        Dialogue(1.0f, dlg_FinalWelcome, FinishTutorial);
    }

    void FinishTutorial() => StartCoroutine(FinalCleanup());

    IEnumerator FinalCleanup()
    {
        yield return new WaitForSeconds(1.5f);
        PCPartDatabase db = GetDatabase();
        if (db != null)
        {
            db.gpuChance = 75f; db.ramChance = 95f; db.cpuChance = 100f;
            db.storageChance = 90.6f; db.coolerChance = 93f; db.buildJobChance = 35f;
        }
        TaskListUI.Instance?.HidePanel();
        HideArrow(); HidePointer();
        step = STEP_DONE;
        PlayerPrefs.SetInt("TutorialDone", 1);
        PlayerPrefs.Save();
    }

    // ═══════════════════════════════════════════════════════════
    //  PC SETUP — Force GPU + RAM broken, force cooler included
    // ═══════════════════════════════════════════════════════════

    IEnumerator ForceMarkGPUAndRAMBroken()
    {
        yield return new WaitForSeconds(1.2f);
        PCPartDatabase db = GetDatabase();

        foreach (CustomerInside c in FindObjectsOfType<CustomerInside>())
        {
            if (c.assignedJob == null || c.assignedJob.startingParts == null) continue;

            foreach (StartingPCComponent part in c.assignedJob.startingParts)
            { part.fault = PartFault.None; part.faultDescription = ""; part.isDusty = false; }

            if (db != null && db.gpus != null && db.gpus.Length > 0)
            {
                c.assignedJob.startingParts.RemoveAll(p =>
                    (p.partCategory ?? "").Trim() == "GPU");

                StartingPCComponent targetGPU = null;
                foreach (StartingPCComponent gpu in db.gpus)
                {
                    if (gpu.partName != null && gpu.partName.Contains("1660"))
                    { targetGPU = gpu; break; }
                }
                if (targetGPU == null) targetGPU = db.gpus[0];

                StartingPCComponent gpuCopy = CopyPartClean(targetGPU);
                gpuCopy.fault = PartFault.Broken;
                gpuCopy.faultDescription = "GPU has failed — no display output.";
                c.assignedJob.startingParts.Add(gpuCopy);
                Debug.Log($"[Tutorial] Forced GPU: {gpuCopy.partName} (Broken)");
            }

            {
                StartingPCComponent mobo = null;
                foreach (StartingPCComponent p in c.assignedJob.startingParts)
                {
                    if ((p.partCategory ?? "").Trim() == "Motherboard")
                    { mobo = p; break; }
                }

                if (mobo != null && mobo.compatTags != null && db.cpus != null)
                {
                    StartingPCComponent currentCPU = null;
                    foreach (StartingPCComponent p in c.assignedJob.startingParts)
                    {
                        if ((p.partCategory ?? "").Trim() == "CPU")
                        { currentCPU = p; break; }
                    }

                    if (currentCPU != null && !IsTagCompatible(currentCPU, mobo, "socket"))
                    {
                        c.assignedJob.startingParts.Remove(currentCPU);
                        StartingPCComponent[] compatCPUs = db.FilterByCompatibility(db.cpus, mobo, "socket");
                        if (compatCPUs.Length > 0)
                        {
                            StartingPCComponent cpuCopy = CopyPartClean(compatCPUs[Random.Range(0, compatCPUs.Length)]);
                            c.assignedJob.startingParts.Add(cpuCopy);
                            Debug.Log($"[Tutorial] Replaced incompatible CPU with: {cpuCopy.partName}");
                        }
                    }
                }
            }

            if (db != null && db.rams != null && db.rams.Length > 0)
            {
                c.assignedJob.startingParts.RemoveAll(p =>
                    (p.partCategory ?? "").Trim() == "RAM");

                StartingPCComponent mobo = null;
                foreach (StartingPCComponent p in c.assignedJob.startingParts)
                {
                    if ((p.partCategory ?? "").Trim() == "Motherboard")
                    { mobo = p; break; }
                }

                HashSet<string> moboMemTags = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
                if (mobo != null && mobo.compatTags != null)
                {
                    foreach (string tag in mobo.compatTags)
                    {
                        string t = tag.Trim().ToUpper();
                        if (t == "DDR3" || t == "DDR4" || t == "DDR5")
                            moboMemTags.Add(t);
                    }
                }

                List<StartingPCComponent> compatRAM = new List<StartingPCComponent>();
                foreach (StartingPCComponent ram in db.rams)
                {
                    if (ram.compatTags == null || ram.compatTags.Length == 0)
                    { compatRAM.Add(ram); continue; }

                    foreach (string tag in ram.compatTags)
                    {
                        if (moboMemTags.Contains(tag.Trim().ToUpper()))
                        { compatRAM.Add(ram); break; }
                    }
                }

                if (compatRAM.Count == 0)
                {
                    Debug.LogWarning("[Tutorial] No compatible RAM found for motherboard — using full pool.");
                    foreach (StartingPCComponent ram in db.rams) compatRAM.Add(ram);
                }

                StartingPCComponent targetRAM = null;
                foreach (StartingPCComponent ram in compatRAM)
                {
                    if (ram.partName != null && ram.partName.Contains("8"))
                    { targetRAM = ram; break; }
                }
                if (targetRAM == null) targetRAM = compatRAM[0];

                StartingPCComponent ramCopy = CopyPartClean(targetRAM);
                ramCopy.fault = PartFault.Broken;
                ramCopy.faultDescription = "RAM stick is dead — system fails POST.";
                c.assignedJob.startingParts.Add(ramCopy);
                Debug.Log($"[Tutorial] Forced RAM: {ramCopy.partName} (Broken) " +
                          $"[Tags: {string.Join(",", ramCopy.compatTags ?? new string[0])}] " +
                          $"for motherboard {mobo?.partName ?? "unknown"}");
            }

            if (db != null && db.fans != null && db.fans.Length > 0)
            {
                c.assignedJob.startingParts.RemoveAll(p =>
                    (p.partCategory ?? "").Trim() == "Fan");

                for (int i = 0; i < 4; i++)
                {
                    StartingPCComponent fanCopy = CopyPartClean(
                        db.fans[Random.Range(0, db.fans.Length)]);
                    c.assignedJob.startingParts.Add(fanCopy);
                }
                Debug.Log("[Tutorial] Forced 4 fans.");
            }

            bool hasCooler = false;
            foreach (StartingPCComponent part in c.assignedJob.startingParts)
            {
                if ((part.partCategory ?? "").Trim() == "Cooler")
                { hasCooler = true; break; }
            }
            if (!hasCooler && db != null && db.coolers != null && db.coolers.Length > 0)
            {
                StartingPCComponent coolerCopy = CopyPartClean(
                    db.coolers[Random.Range(0, db.coolers.Length)]);
                c.assignedJob.startingParts.Add(coolerCopy);
                Debug.Log($"[Tutorial] Force-added cooler: {coolerCopy.partName}");
            }

            c.assignedJob.originalFaultCount = 2;
            c.assignedJob.pcProblems = new string[] { "No Display on Monitor" };
            c.jobRequest = "My PC won't boot. Can you please fix it?";

            if (db != null)
                c.assignedJob.reward = db.CalculateReward(c.assignedJob.startingParts, false);

            Debug.Log($"[Tutorial] Recalculated reward: ₱{c.assignedJob.reward:N0}");
            break;
        }
    }

    StartingPCComponent CopyPartClean(StartingPCComponent original)
    {
        StartingPCComponent copy = new StartingPCComponent();
        copy.partCategory = original.partCategory;
        copy.partName = original.partName;
        copy.partPrefab = original.partPrefab;
        copy.partIcon = original.partIcon;
        copy.partPrice = original.partPrice;
        copy.compatTags = original.compatTags;
        copy.powerDraw = original.powerDraw;
        copy.maxWattage = original.maxWattage;
        copy.isDusty = false;
        copy.fault = PartFault.None;
        copy.faultDescription = "";
        return copy;
    }

    // ═══════════════════════════════════════════════════════════
    //  HELPERS
    // ═══════════════════════════════════════════════════════════

    bool IsScrew(string name, string cat) =>
        name.Contains("screw") || name.Contains("bolt") || name.Contains("fastener")
        || cat.Contains("screw") || cat.Contains("bolt");

    bool IsPanel(string name, string cat) =>
        name.Contains("panel") || cat.Contains("panel");

    bool IsPanelScrew(string name, string cat) =>
        IsScrew(name, cat)
        && (name.Contains("panel") || name.Contains("front") || name.Contains("side"))
        && !name.Contains("back");

    bool IsSuspectPart(string partCat, string suspect) =>
        partCat.Equals(suspect, System.StringComparison.OrdinalIgnoreCase);

    int CountSidePanelScrews()
    {
        InspectionManager im = FindFirstObjectByType<InspectionManager>();
        if (im == null || im.currentClone == null) return 4;

        int count = 0;
        foreach (InspectableItem item in im.currentClone.GetComponentsInChildren<InspectableItem>(true))
        {
            if (item == null || !item.isRemovable) continue;
            string n = (item.itemName ?? "").ToLower();
            string c = (item.partCategory ?? "").ToLower();
            string gn = item.gameObject.name.ToLower();
            string combined = n + " " + gn;
            if (IsPanelScrew(combined, c)) count++;
        }
        return count == 0 ? 4 : count;
    }

    void TrackInstalledPartName(string category)
    {
        InspectionManager im = FindObjectOfType<InspectionManager>();
        if (im == null || im.currentClone == null) return;

        foreach (InspectableItem part in im.currentClone.GetComponentsInChildren<InspectableItem>())
        {
            if (part.isInventorySlot || part.isMainObject) continue;
            if (part.partCategory != category) continue;

            if (!part.IsFaulty())
            {
                if (category == "GPU") tutorialGPUName = part.itemName;
                else if (category == "RAM") tutorialRAMName = part.itemName;
                Debug.Log($"[Tutorial] Tracked {category} name: {part.itemName}");
                return;
            }
        }
    }

    bool IsTagCompatible(StartingPCComponent part, StartingPCComponent mobo, string tagType)
    {
        if (part.compatTags == null || mobo.compatTags == null) return true;

        HashSet<string> socketTags = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase)
            { "AM4", "AM5", "LGA1151", "LGA1200", "LGA1700", "LGA1851" };
        HashSet<string> memTags = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase)
            { "DDR3", "DDR4", "DDR5" };

        HashSet<string> relevantSet = (tagType == "memory") ? memTags : socketTags;

        HashSet<string> moboTags = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
        foreach (string t in mobo.compatTags)
            if (relevantSet.Contains(t.Trim())) moboTags.Add(t.Trim());

        foreach (string t in part.compatTags)
            if (moboTags.Contains(t.Trim())) return true;

        return false;
    }
}
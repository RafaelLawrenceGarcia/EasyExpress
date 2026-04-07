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

    void StartStep_PreviewCounterPC()
    {
        SetTask("TASK",
            "Walk to the customer's PC on the counter", "Press [E] to preview it (view only)",
            "Press [Esc] to exit preview", "Press [E] on the customer to talk");
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
    //  PHASE 2 — INTAKE
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

        // Failsafe: player already placed the box during the dialogue
        if (boxPlacedEarly)
        {
            boxPlacedEarly = false;
            CompletePlaceBoxTask();
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  PHASE 3 — DIAGNOSIS (swap-and-test + verify both)
    //
    //  Power behavior is handled by PCPowerSystem.EvaluatePowerState()
    //  which checks actual faults on installed parts:
    //    Old GPU (Broken) + Old RAM (Broken)  → FailedPOST (3s then off)
    //    New GPU + Old RAM (Broken)            → FailedPOST (3s then off)
    //    New GPU + New RAM                     → Success (full boot)
    //    Old GPU (Broken) + New RAM            → NoDisplay (fans spin, no picture)
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
    }

    IEnumerator DelayThenAdvanceFromFirstPower()
    {
        yield return new WaitForSeconds(3.5f);
        if (step != 13) yield break;
        TaskListUI.Instance?.CompleteTask(1);
        yield return new WaitForSeconds(1.0f);
        Dialogue(0.5f, dlg_GrabPartsFirst, StartStep_ExitForStorage);
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
        SetTask("TASKE",
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
    }

    // ── Step 16: Swap-and-Test Loop ──────────────────────────────

    void StartStep_SwapAndTest()
    {
        diagState = DiagState.RemoveOldGPU;
        ShowSwapTask(); step = 16;
    }

    void ShowSwapTask()
    {
        HideArrow();
        switch (diagState)
        {
            // GPU swap
            case DiagState.RemoveOldGPU:
                SetTask("SWAP TEST — GPU", "Remove the old GPU", "Use the screwdriver — hold click to remove"); break;
            case DiagState.InstallNewGPU:
                SetTask("SWAP TEST — GPU", "Press [Tab] to see your parts", "Install the new GPU into the empty slot"); break;
            case DiagState.TestAfterGPUSwap:
                SetTask("SWAP TEST — GPU", "Press the Power Button to test", "Watch if the symptom changes..."); break;
            // RAM swap
            case DiagState.RemoveOldRAM:
                SetTask("SWAP TEST — RAM", "Remove the old RAM stick", "Use the screwdriver — hold click to remove"); break;
            case DiagState.InstallNewRAM:
                SetTask("SWAP TEST — RAM", "Press [Tab] to see your parts", "Install the new RAM into the empty slot"); break;
            case DiagState.TestAfterRAMSwap:
                SetTask("SWAP TEST — RAM", "Press the Power Button to test", "Watch what happens this time..."); break;
            // RAM verify
            case DiagState.RemoveNewRAM:
                SetTask("VERIFY — RAM", "Remove the new RAM you just installed", "We need to confirm the RAM was the problem"); break;
            case DiagState.InstallOldRAM:
                SetTask("VERIFY — RAM", "Press [Tab] to see your parts", "Install the OLD RAM (the faulty one)"); break;
            case DiagState.TestWithOldRAM:
                SetTask("VERIFY — RAM", "Press the Power Button to test with old RAM", "Does the problem come back?"); break;
            case DiagState.RemoveOldRAMAgain:
                SetTask("VERIFY — RAM", "Remove the old RAM again", "RAM confirmed bad — put the good one back"); break;
            case DiagState.InstallNewRAMFinal:
                SetTask("VERIFY — RAM", "Press [Tab] to see your parts", "Install the new RAM"); break;
            case DiagState.TestAfterRAMVerify:
                SetTask("VERIFY — RAM", "Press the Power Button to confirm the fix", "This should boot clean..."); break;
            // GPU verify
            case DiagState.RemoveNewGPU:
                SetTask("VERIFY — GPU", "Remove the new GPU", "Let's check if the old GPU was also bad"); break;
            case DiagState.InstallOldGPU:
                SetTask("VERIFY — GPU", "Press [Tab] to see your parts", "Install the OLD GPU (the original one)"); break;
            case DiagState.TestWithOldGPU:
                SetTask("VERIFY — GPU", "Press the Power Button to test with old GPU", "Watch the display output..."); break;
            case DiagState.RemoveOldGPUAgain:
                SetTask("FINAL FIX", "Remove the old GPU again", "GPU confirmed bad — put the new one back"); break;
            case DiagState.InstallNewGPUFinal:
                SetTask("FINAL FIX", "Press [Tab] to see your parts", "Install the new GPU to finish the repair"); break;
            case DiagState.TestFinal:
                SetTask("FINAL FIX", "Press the Power Button for the final test", "This should boot clean!"); break;
        }
    }

    // ── Part Removal (steps 15 + 16) ─────────────────────────────

    void HandlePartRemoval(InspectableItem part)
    {
        if (part == null) return;
        string partName = (part.itemName ?? "").ToLower();
        string partCat = (part.partCategory ?? "").ToLower();

        // ═══ STEP 15: SCREW + PANEL ═══
        if (step == 15)
        {
            if (!panelRemoved && IsScrew(partName, partCat))
            {
                screwsRemoved++;
                TaskListUI.Instance?.UpdateTaskText(3,
                    $"Unscrew the side panel screws ({screwsRemoved}/{totalScrewsOnPanel})");
                if (screwsRemoved == 1) TaskListUI.Instance?.CompleteTask(2);
                if (screwsRemoved >= totalScrewsOnPanel) TaskListUI.Instance?.CompleteTask(3);
                return;
            }
            if (!panelRemoved && IsPanel(partName, partCat))
            {
                panelRemoved = true;
                CompleteAllTasks();
                Dialogue(0.5f, dlg_SwapGPU, StartStep_SwapAndTest);
                return;
            }
            return;
        }

        // ═══ STEP 16: SWAP-AND-TEST ═══
        if (step != 16) return;

        switch (diagState)
        {
            case DiagState.RemoveOldGPU:
                if (!IsSuspectPart(partCat, "gpu")) break;
                Debug.Log($"[Tutorial] Old GPU removed. fault={part.fault}");
                CompleteAllTasks(); diagState = DiagState.InstallNewGPU; ShowSwapTask(); return;
            case DiagState.RemoveOldRAM:
                if (!IsSuspectPart(partCat, "ram")) break;
                Debug.Log($"[Tutorial] Old RAM removed. fault={part.fault}");
                CompleteAllTasks(); diagState = DiagState.InstallNewRAM; ShowSwapTask(); return;
            case DiagState.RemoveNewRAM:
                if (!IsSuspectPart(partCat, "ram")) break;
                CompleteAllTasks(); diagState = DiagState.InstallOldRAM; ShowSwapTask(); return;
            case DiagState.RemoveOldRAMAgain:
                if (!IsSuspectPart(partCat, "ram")) break;
                CompleteAllTasks(); diagState = DiagState.InstallNewRAMFinal; ShowSwapTask(); return;
            case DiagState.RemoveNewGPU:
                if (!IsSuspectPart(partCat, "gpu")) break;
                Debug.Log($"[Tutorial] New GPU removed for verify. fault={part.fault}");
                CompleteAllTasks(); diagState = DiagState.InstallOldGPU; ShowSwapTask(); return;
            case DiagState.RemoveOldGPUAgain:
                if (!IsSuspectPart(partCat, "gpu")) break;
                CompleteAllTasks(); diagState = DiagState.InstallNewGPUFinal; ShowSwapTask(); return;
        }

        Debug.Log($"[Tutorial] Removed '{part.itemName}' but DiagState={diagState} — ignoring.");
    }

    // ── Install Handler (step 16) ────────────────────────────────

    void HandlePartInstall_SwapTest()
    {
        switch (diagState)
        {
            case DiagState.InstallNewGPU:
                CompleteAllTasks(); diagState = DiagState.TestAfterGPUSwap; ShowSwapTask(); return;
            case DiagState.InstallNewRAM:
                CompleteAllTasks(); diagState = DiagState.TestAfterRAMSwap; ShowSwapTask(); return;
            case DiagState.InstallOldRAM:
                CompleteAllTasks(); diagState = DiagState.TestWithOldRAM; ShowSwapTask(); return;

            case DiagState.InstallNewGPUFinal:
                CompleteAllTasks();
                TrackInstalledPartName("GPU");
                diagState = DiagState.TestFinal;
                ShowSwapTask();
                return;

            case DiagState.InstallOldGPU:
                CompleteAllTasks(); diagState = DiagState.TestWithOldGPU; ShowSwapTask(); return;
            case DiagState.InstallNewRAMFinal:
                CompleteAllTasks();
                TrackInstalledPartName("RAM");
                diagState = DiagState.TestAfterRAMVerify;
                ShowSwapTask();
                return;
        }
        Debug.Log($"[Tutorial] Part installed but DiagState={diagState} — ignoring.");
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
        yield return new WaitForSeconds(0.5f);

        switch (diagState)
        {
            case DiagState.TestAfterGPUSwap:
                Dialogue(0.5f, dlg_GPUNotEnough, () =>
                    Dialogue(0.3f, dlg_SwapRAM, () =>
                    { diagState = DiagState.RemoveOldRAM; ShowSwapTask(); }));
                break;

            case DiagState.TestAfterRAMSwap:
                Dialogue(0.5f, dlg_RAMFixed, () =>
                    Dialogue(0.3f, dlg_VerifyOldRAM, () =>
                    { diagState = DiagState.RemoveNewRAM; ShowSwapTask(); }));
                break;

            case DiagState.TestWithOldRAM:
                Dialogue(0.5f, dlg_RAMConfirmed, () =>
                    Dialogue(0.3f, dlg_PutNewRAMBack, () =>
                    { diagState = DiagState.RemoveOldRAMAgain; ShowSwapTask(); }));
                break;

            case DiagState.TestAfterRAMVerify:
                Dialogue(0.5f, dlg_NowCheckGPU, () =>
                { diagState = DiagState.RemoveNewGPU; ShowSwapTask(); });
                break;

            case DiagState.TestWithOldGPU:
                Dialogue(0.5f, dlg_GPUConfirmed, () =>
                    Dialogue(0.3f, dlg_PutNewGPUBack, () =>
                    { diagState = DiagState.RemoveOldGPUAgain; ShowSwapTask(); }));
                break;

            case DiagState.TestFinal:
                diagState = DiagState.Done;
                CompleteAllTasks(); HideArrow();
                step = 24;
                StartStep_ExitInspectForCompletion();
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
        Dialogue(1.0f, dlg_TroubleshootGuide, StartStep_TroubleshootGuide);
    }

    void StartStep_TroubleshootGuide()
    {
        SetTask("TROUBLESHOOTING GUIDE", "Pay attention to the common symptoms!");
        HideArrow(); step = 27;
        Dialogue(0.5f, dlg_TroubleshootGuide, () =>
        {
            CompleteAllTasks(); step = 28;
            Dialogue(1.0f, dlg_FinalWelcome, FinishTutorial);
        });
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
        foreach (CustomerInside c in FindObjectsOfType<CustomerInside>())
        {
            if (c.assignedJob == null || c.assignedJob.startingParts == null) continue;

            // ── Clear all faults first ──
            foreach (StartingPCComponent part in c.assignedJob.startingParts)
            { part.fault = PartFault.None; part.faultDescription = ""; part.isDusty = false; }

            // ── Mark GPU and RAM as broken ──
            bool gpuDone = false, ramDone = false;
            foreach (StartingPCComponent part in c.assignedJob.startingParts)
            {
                string cat = (part.partCategory ?? "").Trim();
                string name = (part.partName ?? "").ToLower();
                if (!gpuDone && (cat == "GPU" || name.Contains("gpu") || name.Contains("gtx")
                    || name.Contains("rtx") || name.Contains("radeon") || name.Contains("geforce")))
                { part.fault = PartFault.Broken; part.faultDescription = "GPU has failed — no display output."; gpuDone = true; }
                else if (!ramDone && (cat == "RAM" || name.Contains("ram") || name.Contains("ddr")))
                { part.fault = PartFault.Broken; part.faultDescription = "RAM stick is dead — system fails POST."; ramDone = true; }
            }

            // ── Force cooler — ensure the tutorial PC always has one ──
            bool hasCooler = false;
            foreach (StartingPCComponent part in c.assignedJob.startingParts)
            {
                if ((part.partCategory ?? "").Trim() == "Cooler")
                { hasCooler = true; break; }
            }
            if (!hasCooler)
            {
                PCPartDatabase db = GetDatabase();
                if (db != null && db.coolers != null && db.coolers.Length > 0)
                {
                    StartingPCComponent original = db.coolers[Random.Range(0, db.coolers.Length)];
                    StartingPCComponent coolerCopy = new StartingPCComponent();
                    coolerCopy.partCategory = "Cooler";
                    coolerCopy.partName = original.partName;
                    coolerCopy.partPrefab = original.partPrefab;
                    coolerCopy.partIcon = original.partIcon;
                    coolerCopy.partPrice = original.partPrice;
                    coolerCopy.compatTags = original.compatTags;
                    coolerCopy.powerDraw = original.powerDraw;
                    coolerCopy.maxWattage = original.maxWattage;
                    coolerCopy.isDusty = false;
                    coolerCopy.fault = PartFault.None;
                    coolerCopy.faultDescription = "";
                    c.assignedJob.startingParts.Add(coolerCopy);
                    Debug.Log($"[Tutorial] Force-added cooler: {coolerCopy.partName}");
                }
            }

            c.assignedJob.originalFaultCount = 2;
            c.assignedJob.pcProblems = new string[] { "No Display on Monitor" };
            c.jobRequest = "My PC won't boot. Can you please fix it?";
            break;
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  HELPERS
    // ═══════════════════════════════════════════════════════════

    bool IsScrew(string name, string cat) =>
        name.Contains("screw") || name.Contains("bolt") || name.Contains("fastener")
        || cat.Contains("screw") || cat.Contains("bolt");

    bool IsPanel(string name, string cat) =>
        name.Contains("panel") || cat.Contains("panel");

    bool IsSuspectPart(string partCat, string suspect) =>
        partCat.Equals(suspect, System.StringComparison.OrdinalIgnoreCase);

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
        return count == 0 ? 4 : count;
    }

    /// <summary>
    /// Captures the name of the part the player just installed so the
    /// shop tutorial arrow can point at matching items in the store.
    /// </summary>
    void TrackInstalledPartName(string category)
    {
        InspectionManager im = FindObjectOfType<InspectionManager>();
        if (im == null || im.currentClone == null) return;

        foreach (InspectableItem part in im.currentClone.GetComponentsInChildren<InspectableItem>())
        {
            if (part.isInventorySlot || part.isMainObject) continue;
            if (part.partCategory != category) continue;

            // Check if this is a NEW part (not faulty = it's the replacement)
            if (!part.IsFaulty())
            {
                if (category == "GPU") tutorialGPUName = part.itemName;
                else if (category == "RAM") tutorialRAMName = part.itemName;
                Debug.Log($"[Tutorial] Tracked {category} name: {part.itemName}");
                return;
            }
        }
    }
}
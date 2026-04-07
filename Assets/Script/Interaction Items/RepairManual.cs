// ============================================================
//  RepairManual.cs
//  Press [F] to open. Freezes player movement + camera.
//  Shows troubleshooting procedures for each PC problem.
// ============================================================
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class RepairManual : MonoBehaviour
{
    public static RepairManual Instance;

    [Header("UI References")]
    public GameObject manualPanel;
    public Transform categoryContent;
    public GameObject categoryButtonPrefab;

    [Header("Procedure View")]
    public GameObject procedurePanel;
    public TextMeshProUGUI procedureTitle;
    public TextMeshProUGUI procedureBody;
    public Button backButton;
    public Button closeButton;

    [Header("Controls")]
    public KeyCode manualKey = KeyCode.F;

    private bool isOpen = false;
    private Dictionary<string, ManualEntry> entries = new Dictionary<string, ManualEntry>();

    // Cached references for freeze/unfreeze
    private GTAMovement playerMovement;
    private OrbitCamera playerCamera;

    class ManualEntry
    {
        public string title;
        public string symptoms;
        public string procedure;
    }

    void Awake()
    {
        Instance = this;
        BuildEntries();
        if (manualPanel != null) manualPanel.SetActive(false);
        if (procedurePanel != null) procedurePanel.SetActive(false);
    }

    void Start()
    {
        if (backButton != null)
            backButton.onClick.AddListener(ShowCategories);
        if (closeButton != null)
            closeButton.onClick.AddListener(CloseManual);

        // Cache player references
        playerMovement = FindFirstObjectByType<GTAMovement>();
        playerCamera = FindFirstObjectByType<OrbitCamera>();
    }

    void Update()
    {
        if (Input.GetKeyDown(manualKey))
        {
            // Block during dialogue
            IntroDialogueManager dlg = FindFirstObjectByType<IntroDialogueManager>();
            if (dlg != null && dlg.isDialogueActive) return;

            // Block during inspection (G key handles its own summary)
            InspectionManager im = FindFirstObjectByType<InspectionManager>();
            if (im != null && im.isInspecting) return;

            ToggleManual();
        }

        if (isOpen && Input.GetKeyDown(KeyCode.Escape))
            CloseManual();
    }

    // ─── Public API ──────────────────────────────────────────────

    public void ToggleManual()
    {
        if (isOpen) CloseManual();
        else OpenManual();
    }

    public void OpenManual()
    {
        if (manualPanel == null) return;
        isOpen = true;
        manualPanel.SetActive(true);
        ShowCategories();
        FreezePlayer(true);
    }

    public void CloseManual()
    {
        if (manualPanel == null) return;
        isOpen = false;
        manualPanel.SetActive(false);
        FreezePlayer(false);
    }

    public bool IsOpen() => isOpen;

    // ─── Player Freeze ───────────────────────────────────────────

    void FreezePlayer(bool freeze)
    {
        // Re-find if null (scene reload, etc.)
        if (playerMovement == null) playerMovement = FindFirstObjectByType<GTAMovement>();
        if (playerCamera == null) playerCamera = FindFirstObjectByType<OrbitCamera>();

        if (playerMovement != null)
            playerMovement.SetMovementState(!freeze);
        if (playerCamera != null)
            playerCamera.SetCameraState(!freeze);

        if (freeze)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        // Block pause menu from catching the Escape that closes the manual
        if (!freeze)
            PauseManager.BlockPause = true;
    }

    // ─── Category List ───────────────────────────────────────────

    void ShowCategories()
    {
        if (procedurePanel != null) procedurePanel.SetActive(false);
        if (categoryContent == null) return;

        // Show the category scroll view (hidden when procedure was open)
        GetCategoryScrollRoot()?.SetActive(true);

        foreach (Transform child in categoryContent)
            Destroy(child.gameObject);

        foreach (var kvp in entries)
        {
            string key = kvp.Key;
            ManualEntry entry = kvp.Value;

            GameObject btnObj;
            if (categoryButtonPrefab != null)
            {
                btnObj = Instantiate(categoryButtonPrefab, categoryContent);
                btnObj.SetActive(true);
            }
            else
            {
                btnObj = CreateSimpleButton(categoryContent);
            }

            // Set label text — search including inactive children
            TextMeshProUGUI label = btnObj.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
            {
                label.text = entry.title;
                label.fontSize = 18;
            }

            if (label == null)
            {
                Text legacyText = btnObj.GetComponentInChildren<Text>(true);
                if (legacyText != null) legacyText.text = entry.title;
            }

            Button btn = btnObj.GetComponent<Button>();
            if (btn != null)
            {
                string capturedKey = key;
                btn.onClick.AddListener(() => ShowProcedure(capturedKey));
            }

            // Ensure button is tall enough
            LayoutElement btnLE = btnObj.GetComponent<LayoutElement>();
            if (btnLE == null) btnLE = btnObj.AddComponent<LayoutElement>();
            btnLE.preferredHeight = 72;
            btnLE.flexibleWidth = 1;
        }
    }

    void ShowProcedure(string key)
    {
        if (!entries.ContainsKey(key) || procedurePanel == null) return;

        // Hide category list so procedure takes its place
        GetCategoryScrollRoot()?.SetActive(false);

        ManualEntry entry = entries[key];
        procedurePanel.SetActive(true);

        // Auto-fix layout: ensure ProcedurePanel's VLG controls child heights
        VerticalLayoutGroup procVLG = procedurePanel.GetComponent<VerticalLayoutGroup>();
        if (procVLG != null)
        {
            procVLG.childControlHeight = true;
            procVLG.childControlWidth = true;
        }

        if (procedureTitle != null)
        {
            procedureTitle.text = entry.title;
            procedureTitle.fontSize = 40;
        }

        if (procedureBody != null)
        {
            string fullText = $"<b><color=#FF6B6B>SYMPTOMS:</color></b>\n{entry.symptoms}\n\n";
            fullText += $"<b><color=#4AE0FF>STEP-BY-STEP PROCEDURE:</color></b>\n{entry.procedure}";
            procedureBody.text = fullText;
            procedureBody.fontSize = 32;
            procedureBody.alignment = TextAlignmentOptions.TopLeft;

            // Force the body RectTransform to stretch and fill parent width
            RectTransform bodyRt = procedureBody.GetComponent<RectTransform>();
            bodyRt.anchorMin = new Vector2(0, 1);
            bodyRt.anchorMax = new Vector2(1, 1);
            bodyRt.pivot = new Vector2(0.5f, 1);
            bodyRt.offsetMin = new Vector2(0, bodyRt.offsetMin.y);
            bodyRt.offsetMax = new Vector2(0, bodyRt.offsetMax.y);

            // Ensure the body text fills available space in the layout
            LayoutElement bodyLE = procedureBody.GetComponent<LayoutElement>();
            if (bodyLE == null) bodyLE = procedureBody.gameObject.AddComponent<LayoutElement>();
            bodyLE.flexibleHeight = 1;
            bodyLE.flexibleWidth = 1;

            // Also fix the title alignment
            if (procedureTitle != null)
                procedureTitle.alignment = TextAlignmentOptions.TopLeft;

            // If inside a scroll view, stretch the viewport and fix scroll LE
            ScrollRect parentScroll = procedureBody.GetComponentInParent<ScrollRect>();
            if (parentScroll != null)
            {
                LayoutElement scrollLE = parentScroll.GetComponent<LayoutElement>();
                if (scrollLE == null) scrollLE = parentScroll.gameObject.AddComponent<LayoutElement>();
                scrollLE.flexibleHeight = 1;
                scrollLE.flexibleWidth = 1;
            }
        }

        // Keep cursor visible
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // ─── Scroll Root Helper ─────────────────────────────────────

    /// <summary>
    /// Navigate up from Content → Viewport → CategoryScrollView.
    /// This lets us hide the entire scroll area when showing a procedure.
    /// </summary>
    GameObject GetCategoryScrollRoot()
    {
        if (categoryContent == null) return null;
        // Content → Viewport → CategoryScrollView (or whatever the scroll root is)
        Transform viewport = categoryContent.parent;
        if (viewport == null) return null;
        Transform scrollRoot = viewport.parent;
        if (scrollRoot == null) return viewport.gameObject;
        return scrollRoot.gameObject;
    }

    // ─── Fallback Button Creator ─────────────────────────────────

    GameObject CreateSimpleButton(Transform parent)
    {
        GameObject btnObj = new GameObject("ManualBtn", typeof(RectTransform), typeof(Button), typeof(Image));
        btnObj.transform.SetParent(parent, false);

        Image img = btnObj.GetComponent<Image>();
        img.color = new Color(0.15f, 0.25f, 0.4f, 1f);

        LayoutElement le = btnObj.AddComponent<LayoutElement>();
        le.preferredHeight = 56;
        le.flexibleWidth = 1;

        GameObject textObj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(btnObj.transform, false);

        RectTransform textRt = textObj.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(14, 0);
        textRt.offsetMax = new Vector2(-14, 0);

        TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
        tmp.fontSize = 32;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.color = new Color(0.78f, 0.82f, 0.88f, 1f);

        return btnObj;
    }

    // ═══════════════════════════════════════════════════════════
    //  MANUAL CONTENT
    // ═══════════════════════════════════════════════════════════

    void BuildEntries()
    {
        entries.Clear();

        entries["general_tips"] = new ManualEntry
        {
            title = "General Diagnostic Tips",
            symptoms = "\u2022 These apply to ALL repair jobs\n\u2022 Follow the swap-and-test methodology",
            procedure =
                "1. <b>ALWAYS test before opening the case</b>\n" +
                "   \u2022 Connect the power cord and press the power button.\n" +
                "   \u2022 Observe the symptoms BEFORE making any changes.\n\n" +
                "2. <b>Swap-and-Test methodology</b>\n" +
                "   \u2022 Change ONE component at a time.\n" +
                "   \u2022 Test after each swap to see if symptoms change.\n" +
                "   \u2022 This isolates which part is actually faulty.\n\n" +
                "3. <b>ALWAYS verify your fix</b>\n" +
                "   \u2022 After fixing, put the OLD part back in.\n" +
                "   \u2022 If the problem returns, the old part is confirmed bad.\n" +
                "   \u2022 Put the NEW part back to complete the repair.\n\n" +
                "4. <b>Check for dust FIRST</b> \u2014 If the PC is dusty, clean it before diagnosis.\n\n" +
                "5. <b>Check power/cables</b> \u2014 Many issues are simply loose connections.\n\n" +
                "6. <b>Component summary</b> \u2014 Press [G] while inspecting to see all installed parts."
        };

        entries["no_display"] = new ManualEntry
        {
            title = "No Display on Monitor",
            symptoms = "\u2022 Monitor shows no signal or stays black\n\u2022 PC fans may spin but nothing appears on screen\n\u2022 POST beeps may occur",
            procedure =
                "1. <b>Check cables</b> \u2014 Ensure the monitor cable is connected to the GPU, not the motherboard.\n\n" +
                "2. <b>Power on the PC</b> \u2014 Observe whether fans spin and lights turn on.\n\n" +
                "3. <b>If fans spin but no display</b> \u2014 This usually indicates a GPU or RAM issue.\n\n" +
                "4. <b>Swap the GPU</b> \u2014 Remove the existing GPU and install a known-good replacement.\n" +
                "   \u2022 Power on and test. If display appears, the old GPU was faulty.\n\n" +
                "5. <b>If still no display after GPU swap</b> \u2014 The RAM may also be faulty.\n" +
                "   \u2022 Remove the old RAM and install a known-good stick.\n" +
                "   \u2022 Power on and test again.\n\n" +
                "6. <b>Verify each part</b> \u2014 Put the OLD part back in and test to confirm.\n" +
                "   \u2022 If the problem returns, the old part is confirmed bad.\n\n" +
                "7. <b>Boot to desktop</b> \u2014 If the PC boots successfully, the repair is complete."
        };

        entries["bsod"] = new ManualEntry
        {
            title = "Blue Screen of Death (BSOD)",
            symptoms = "\u2022 PC crashes with a blue screen\n\u2022 Random restarts during use\n\u2022 Error codes displayed on screen",
            procedure =
                "1. <b>Open the case</b> and inspect all components visually.\n\n" +
                "2. <b>Check RAM seating</b> \u2014 Remove the RAM sticks and reseat them firmly.\n" +
                "   \u2022 You should hear/feel a click when properly seated.\n\n" +
                "3. <b>If reseating doesn't fix it</b> \u2014 Try swapping the RAM with a known-good stick.\n\n" +
                "4. <b>Check storage</b> \u2014 A corrupted drive can cause BSOD.\n" +
                "   \u2022 Try replacing the storage drive if RAM swap didn't help.\n\n" +
                "5. <b>Test after each swap</b> \u2014 Power on and check if BSOD still occurs.\n\n" +
                "6. <b>Verify</b> \u2014 Put the old part back to confirm it was the issue."
        };

        entries["overheating"] = new ManualEntry
        {
            title = "PC Keeps Overheating",
            symptoms = "\u2022 PC shuts down during heavy use\n\u2022 Fans running extremely loud\n\u2022 Components feel very hot",
            procedure =
                "1. <b>Clean the dust</b> \u2014 Use compressed air to clean all components.\n" +
                "   \u2022 Equip the air can (press 2) and spray on dusty areas.\n\n" +
                "2. <b>Check the CPU cooler</b> \u2014 Inspect if it's properly mounted.\n" +
                "   \u2022 The thermal paste may have dried out.\n" +
                "   \u2022 Replace the cooler if it's not making proper contact.\n\n" +
                "3. <b>Check case fans</b> \u2014 Make sure all fans are spinning.\n" +
                "   \u2022 Replace any broken or stuck fans.\n\n" +
                "4. <b>Test</b> \u2014 Power on and monitor temperatures."
        };

        entries["wont_turn_on"] = new ManualEntry
        {
            title = "Won't Turn On",
            symptoms = "\u2022 No response when pressing the power button\n\u2022 No fans, no lights, completely dead\n\u2022 Or brief power then immediate shutdown",
            procedure =
                "1. <b>Check the power cord</b> \u2014 Make sure it's connected from the wall outlet to the PSU.\n\n" +
                "2. <b>Check the PSU</b> \u2014 Inspect the power supply.\n" +
                "   \u2022 If the PSU is dead, replace it with a working one.\n\n" +
                "3. <b>If PSU is OK</b> \u2014 Check the motherboard power connections.\n" +
                "   \u2022 The 24-pin ATX cable may be loose.\n" +
                "   \u2022 Reseat or replace the cable.\n\n" +
                "4. <b>Test</b> \u2014 Power on after each change to isolate the problem."
        };

        entries["dust"] = new ManualEntry
        {
            title = "Full of Dust",
            symptoms = "\u2022 Visible dust buildup on components\n\u2022 Reduced airflow\n\u2022 Higher temperatures than normal",
            procedure =
                "1. <b>Equip compressed air</b> \u2014 Press 2 to select the air can.\n\n" +
                "2. <b>Spray all dusty areas</b> \u2014 Hold left-click and sweep across components.\n" +
                "   \u2022 Focus on fans, heatsinks, and crevices.\n\n" +
                "3. <b>Continue until clean</b> \u2014 The dust overlay will disappear when done.\n\n" +
                "4. <b>Check for damage</b> \u2014 Dust buildup can cause overheating damage.\n" +
                "   \u2022 Inspect components for heat-related faults after cleaning."
        };

        entries["random_shutdowns"] = new ManualEntry
        {
            title = "Random Shutdowns",
            symptoms = "\u2022 PC turns off randomly during use\n\u2022 No BSOD \u2014 just instant power loss\n\u2022 May happen under load",
            procedure =
                "1. <b>Check PSU capacity</b> \u2014 The power supply may be underpowered.\n" +
                "   \u2022 Look at the total wattage draw vs. PSU max wattage.\n" +
                "   \u2022 If the draw exceeds capacity, replace with a higher-wattage PSU.\n\n" +
                "2. <b>Check CPU temperatures</b> \u2014 Overheating CPU will force shutdown.\n" +
                "   \u2022 Inspect the cooler and thermal paste.\n" +
                "   \u2022 Replace the cooler if needed.\n\n" +
                "3. <b>Test after each fix</b> \u2014 Power on and observe stability."
        };

        entries["loud_fans"] = new ManualEntry
        {
            title = "Loud Fan Noise",
            symptoms = "\u2022 Grinding or rattling noise from the PC\n\u2022 Fans vibrating excessively\n\u2022 Noise gets worse over time",
            procedure =
                "1. <b>Identify the noisy fan</b> \u2014 Listen carefully to locate the source.\n" +
                "   \u2022 It could be a case fan, GPU fan, or CPU cooler fan.\n\n" +
                "2. <b>Check for dust</b> \u2014 Clogged fans run at max RPM and make more noise.\n" +
                "   \u2022 Clean with compressed air first.\n\n" +
                "3. <b>Replace the fan</b> \u2014 If the bearing is worn out, cleaning won't fix it.\n" +
                "   \u2022 Remove the faulty fan and install a replacement.\n\n" +
                "4. <b>Test</b> \u2014 Power on and verify the noise is gone."
        };

        entries["slow_performance"] = new ManualEntry
        {
            title = "Slow Performance",
            symptoms = "\u2022 PC takes very long to boot\n\u2022 Applications load slowly\n\u2022 General sluggishness",
            procedure =
                "1. <b>Check storage health</b> \u2014 An old or failing HDD will be extremely slow.\n" +
                "   \u2022 Replace with an SSD if the storage is outdated.\n\n" +
                "2. <b>Check RAM compatibility</b> \u2014 Wrong-speed RAM can bottleneck performance.\n" +
                "   \u2022 Verify the RAM matches the motherboard's supported speed.\n" +
                "   \u2022 Replace with compatible RAM if needed.\n\n" +
                "3. <b>Test</b> \u2014 Power on and check boot/load times."
        };

        entries["boot_loop"] = new ManualEntry
        {
            title = "Boot Loop",
            symptoms = "\u2022 PC starts, shows logo, then restarts endlessly\n\u2022 Never reaches the desktop\n\u2022 May beep or flash error codes",
            procedure =
                "1. <b>Reseat RAM</b> \u2014 Improperly seated RAM is the most common cause.\n" +
                "   \u2022 Remove and firmly reinstall all RAM sticks.\n\n" +
                "2. <b>If reseating fails</b> \u2014 Try one RAM stick at a time to find the faulty one.\n\n" +
                "3. <b>Check motherboard</b> \u2014 A corrupted BIOS can cause boot loops.\n" +
                "   \u2022 The motherboard may need replacement if BIOS is corrupted.\n\n" +
                "4. <b>Test after each change</b> \u2014 Power on to check if the loop stops."
        };

        entries["no_internet"] = new ManualEntry
        {
            title = "No Internet Connection",
            symptoms = "\u2022 PC boots normally but cannot connect to network\n\u2022 Ethernet port not detected\n\u2022 Wi-Fi adapter missing",
            procedure =
                "1. <b>Check the motherboard</b> \u2014 The onboard network adapter may have failed.\n\n" +
                "2. <b>If onboard NIC is dead</b> \u2014 Install a PCIe network card as a replacement.\n\n" +
                "3. <b>Test</b> \u2014 Power on and verify network connectivity."
        };
    }
}
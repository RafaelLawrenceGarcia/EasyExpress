// ============================================================
//  PCComponentSummary.cs  (FIXED — reliable entry rendering)
//
//  Press [G] during Inspect Mode to view installed parts.
//
//  FAULT VISIBILITY:
//    HIDDEN: Broken, NotSeated, Dusty, Overheating, Corrupted,
//            LooseConnection, WrongSlot, Overloaded
//    SHOWN:  Outdated, Incompatible, Empty slots, Overloaded PSU
//
//  Entries use simple TMP rich text (no complex nested layouts).
// ============================================================
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class PCComponentSummary : MonoBehaviour
{
    public static PCComponentSummary Instance;

    [Header("UI References")]
    public GameObject summaryPanel;
    public Transform contentContainer;

    [Header("Buttons")]
    public Button closeButton;

    [Header("Controls")]
    public KeyCode summaryKey = KeyCode.G;

    private bool isOpen = false;

    static readonly HashSet<PartFault> hiddenFaults = new HashSet<PartFault>
    {
        PartFault.Broken, PartFault.NotSeated, PartFault.Dusty,
        PartFault.Overheating, PartFault.Corrupted,
        PartFault.LooseConnection, PartFault.WrongSlot, PartFault.Overloaded
    };

    void Awake()
    {
        Instance = this;
        if (summaryPanel != null) summaryPanel.SetActive(false);
    }

    void Start()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(Close);
    }

    void Update()
    {
        InspectionManager im = FindFirstObjectByType<InspectionManager>();
        if (im == null || !im.isInspecting) return;
        if (Input.GetKeyDown(summaryKey)) Toggle();
        if (isOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            PauseManager.BlockPause = true;   // ← add this
            Close();
        }
    }

    public void Toggle() { if (isOpen) Close(); else Open(); }

    public void Open()
    {
        if (summaryPanel == null) return;
        InspectionManager im = FindFirstObjectByType<InspectionManager>();
        if (im == null || im.currentClone == null) return;
        isOpen = true;
        summaryPanel.SetActive(true);

        HideFixedMetrics();
        EnsureScrollable();
        RefreshSummary(im.currentClone);
        if (TutorialManager.Instance != null)
            TutorialManager.Instance.NotifyPCSummaryOpened();
    }

    public void Close()
    {
        isOpen = false;
        if (summaryPanel != null) summaryPanel.SetActive(false);
    }

    public bool IsOpen() => isOpen;

    // ─── Helpers ─────────────────────────────────────────────

    void HideFixedMetrics()
    {
        if (summaryPanel == null) return;
        foreach (Transform child in summaryPanel.transform)
        {
            string n = child.name.ToLower();
            if (n.Contains("metric") || n.Contains("divider"))
                child.gameObject.SetActive(false);
        }
    }

    void EnsureScrollable()
    {
        if (contentContainer == null) return;

        ContentSizeFitter csf = contentContainer.GetComponent<ContentSizeFitter>();
        if (csf == null) csf = contentContainer.gameObject.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        VerticalLayoutGroup vlg = contentContainer.GetComponent<VerticalLayoutGroup>();
        if (vlg != null) vlg.childControlHeight = true;
    }

    // ═══════════════════════════════════════════════════════════
    //  REFRESH
    // ═══════════════════════════════════════════════════════════

    void RefreshSummary(GameObject pcRoot)
    {
        if (contentContainer == null) return;
        foreach (Transform child in contentContainer) Destroy(child.gameObject);

        InspectableItem[] allParts = pcRoot.GetComponentsInChildren<InspectableItem>(true);
        Dictionary<string, List<InspectableItem>> grouped = new Dictionary<string, List<InspectableItem>>();
        string[] order = { "Motherboard", "CPU", "GPU", "RAM", "Storage", "PSU", "Cooler", "Fan" };

        foreach (InspectableItem part in allParts)
        {
            if (part.isMainObject || part.isInventorySlot || part.isWirePort) continue;
            if (string.IsNullOrEmpty(part.partCategory)) continue;
            string cat = part.partCategory.Trim();
            string nl = (part.itemName ?? "").ToLower();

            if (nl.Contains("screw") || nl.Contains("panel") || nl.Contains("bolt")) continue;
            if (nl.Contains("cable") || nl.Contains("wire") || nl.Contains("connector")) continue;
            if (nl.Contains("power button") || nl.Contains("unknown")) continue;
            if (part.isPowerButton) continue;
            if (cat == "Generic" || cat == "Button") continue;

            if (!grouped.ContainsKey(cat)) grouped[cat] = new List<InspectableItem>();
            grouped[cat].Add(part);
        }

        foreach (string c in order)
        {
            if (!grouped.ContainsKey(c)) continue;
            foreach (InspectableItem p in grouped[c])
                AddEntry(c, p);
            grouped.Remove(c);
        }
        foreach (var kvp in grouped)
            foreach (InspectableItem p in kvp.Value)
                AddEntry(kvp.Key, p);

        // Empty slots
        List<string> missing = new List<string>();
        foreach (InspectableItem p in allParts)
        {
            if (!p.isInventorySlot) continue;
            string cat = p.partCategory ?? "Unknown";
            if (!missing.Contains(cat)) missing.Add(cat);
        }
        if (missing.Count > 0)
        {
            AddSpacer();
            foreach (string m in missing)
                AddTextRow($"<color=#C04050>\u2298  {m} \u2014 Not installed</color>", 28);
        }

        AddSpacer();
        AddMetricsToContent(pcRoot, allParts, missing);

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentContainer.GetComponent<RectTransform>());
    }

    // ═══════════════════════════════════════════════════════════
    //  ENTRY CREATION
    // ═══════════════════════════════════════════════════════════

    void AddEntry(string category, InspectableItem part)
    {
        string catHex = GetCatHex(category);
        string warning = GetVisibleWarning(part);

        string line = $"<color=#{catHex}><b>{category}:</b></color>  {part.itemName}";
        if (warning == "outdated")
            line += "  <color=#D4A843><size=10>OUTDATED</size></color>";
        else if (warning == "incompatible")
            line += "  <color=#E06060><size=10>INCOMPATIBLE</size></color>";

        string specs = "";
        if (part.compatTags != null && part.compatTags.Length > 0)
            specs += "<color=#7090B0>[" + string.Join(", ", part.compatTags) + "]</color>";
        if (part.powerDraw > 0)
        {
            if (specs.Length > 0) specs += "   ";
            specs += $"<color=#D4A843>{part.powerDraw}W</color>";
        }
        if (part.maxWattage > 0)
        {
            if (specs.Length > 0) specs += "   ";
            specs += $"<color=#4AE04A>Max {part.maxWattage}W</color>";
        }

        string fullText = line;
        if (specs.Length > 0)
            fullText += $"\n  <size=11>{specs}</size>";

        int height = specs.Length > 0 ? 46 : 28;
        AddTextRow(fullText, height);
        AddDivider();
    }

    void AddTextRow(string richText, int height)
    {
        GameObject bg = new GameObject("Entry", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(contentContainer, false);
        bg.GetComponent<Image>().color = new Color(0.10f, 0.12f, 0.22f, 0.6f);

        LayoutElement bgLE = bg.AddComponent<LayoutElement>();
        bgLE.preferredHeight = height;
        bgLE.flexibleWidth = 1;

        GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(bg.transform, false);

        RectTransform textRt = textObj.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(10, 2);
        textRt.offsetMax = new Vector2(-10, -2);

        TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
        tmp.text = richText;
        tmp.fontSize = 14;
        tmp.color = new Color(0.80f, 0.84f, 0.92f, 1f);
        tmp.richText = true;
        tmp.enableWordWrapping = true;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
    }

    void AddDivider()
    {
        GameObject div = new GameObject("Divider", typeof(RectTransform), typeof(Image));
        div.transform.SetParent(contentContainer, false);
        div.GetComponent<Image>().color = new Color(0.20f, 0.24f, 0.38f, 0.5f);

        LayoutElement le = div.AddComponent<LayoutElement>();
        le.preferredHeight = 1;
        le.flexibleWidth = 1;
    }

    void AddSpacer()
    {
        GameObject s = new GameObject("Spacer", typeof(RectTransform), typeof(LayoutElement));
        s.transform.SetParent(contentContainer, false);
        s.GetComponent<LayoutElement>().preferredHeight = 8;
    }

    string GetVisibleWarning(InspectableItem part)
    {
        if (part.fault == PartFault.Outdated) return "outdated";
        if (part.fault == PartFault.Incompatible) return "incompatible";
        return "";
    }

    // ═══════════════════════════════════════════════════════════
    //  METRICS (inline in scroll content)
    // ═══════════════════════════════════════════════════════════

    void AddMetricsToContent(GameObject pcRoot, InspectableItem[] allParts, List<string> missingParts)
    {
        AddTextRow("<color=#5A6A8A><b>SYSTEM METRICS</b></color>", 24);
        AddDivider();

        float totalDraw = 0f, psuCap = 0f;
        foreach (InspectableItem p in allParts)
        {
            if (p.isInventorySlot || p.isMainObject) continue;
            totalDraw += p.powerDraw;
            if (p.partCategory == "PSU" && p.maxWattage > 0) psuCap = p.maxWattage;
        }

        // POWER
        string powerLine;
        if (psuCap > 0)
        {
            float u = (totalDraw / psuCap) * 100f;
            string s, h;
            if (totalDraw > psuCap) { s = "OVERLOADED"; h = "E06060"; }
            else if (u > 80f) { s = "Near capacity"; h = "D4A843"; }
            else { s = "Within safe range"; h = "4AE04A"; }
            powerLine = $"<b>POWER:</b>  {totalDraw}W / {psuCap}W  ({u:F0}%)\n<color=#{h}>{s}</color>";
        }
        else
        {
            powerLine = $"<b>POWER:</b>  Total draw: {totalDraw}W  |  No PSU detected";
        }
        AddTextRow(powerLine, 46);

        // COMPATIBILITY
        List<string> issues = new List<string>();
        HashSet<string> moboT = new HashSet<string>(), ramT = new HashSet<string>(), cpuT = new HashSet<string>();
        bool hasCooler = false;
        foreach (InspectableItem p in allParts)
        {
            if (p.isInventorySlot || p.isMainObject) continue;
            if (p.partCategory == "Cooler") hasCooler = true;
            if (p.compatTags == null) continue;
            if (p.partCategory == "Motherboard") foreach (string t in p.compatTags) moboT.Add(t.ToUpper().Trim());
            else if (p.partCategory == "RAM") foreach (string t in p.compatTags) ramT.Add(t.ToUpper().Trim());
            else if (p.partCategory == "CPU") foreach (string t in p.compatTags) cpuT.Add(t.ToUpper().Trim());
        }

        bool m4 = moboT.Contains("DDR4"), m5 = moboT.Contains("DDR5");
        bool r4 = ramT.Contains("DDR4"), r5 = ramT.Contains("DDR5");
        if ((m4 && r5) || (m5 && r4))
            issues.Add($"<color=#E06060>RAM ({(r4 ? "DDR4" : "DDR5")}) vs Motherboard ({(m4 ? "DDR4" : "DDR5")})</color>");
        else if (ramT.Count > 0)
            issues.Add("<color=#4AE04A>RAM matches motherboard</color>");

        string ms = "", cs = "";
        foreach (string s in new[] { "LGA", "AM4", "AM5", "TR" })
        {
            foreach (string t in moboT) if (t.StartsWith(s)) ms = t;
            foreach (string t in cpuT) if (t.StartsWith(s)) cs = t;
        }
        if (ms != "" && cs != "")
            issues.Add(ms == cs ? $"<color=#4AE04A>CPU socket ({cs}) matches</color>"
                                : $"<color=#E06060>CPU ({cs}) vs Motherboard ({ms})</color>");
        if (psuCap > 0 && totalDraw > psuCap)
            issues.Add($"<color=#D4A843>PSU ({psuCap}W) too low ({totalDraw}W draw)</color>");
        if (cpuT.Count > 0 && !hasCooler)
            issues.Add("<color=#D4A843>No CPU cooler!</color>");

        string compatLine = "<b>COMPATIBILITY:</b>\n" + (issues.Count == 0
            ? "<color=#4AE04A>All parts compatible</color>" : string.Join("\n", issues));
        int compatH = 36 + (issues.Count > 1 ? (issues.Count - 1) * 18 : 0);
        AddTextRow(compatLine, compatH);

        // HEALTH
        int tot = 0, vis = 0, emp = missingParts.Count;
        bool hid = false;
        foreach (InspectableItem p in allParts)
        {
            if (p.isMainObject) continue;
            string nl = (p.itemName ?? "").ToLower();
            string cat = (p.partCategory ?? "").Trim();
            if (nl.Contains("screw") || nl.Contains("panel") || nl.Contains("bolt")) continue;
            if (nl.Contains("cable") || nl.Contains("wire") || nl.Contains("connector")) continue;
            if (nl.Contains("power button") || nl.Contains("unknown")) continue;
            if (p.isPowerButton) continue;
            if (cat == "Generic" || cat == "Button") continue;
            if (p.isInventorySlot) continue;
            if (p.isWirePort) continue;
            tot++;
            if (p.IsFaulty()) { if (hiddenFaults.Contains(p.fault)) hid = true; else vis++; }
        }
        DustSystem d = pcRoot.GetComponent<DustSystem>();
        bool dusty = d != null && d.isDusty;
        List<string> probs = new List<string>();
        if (vis > 0) probs.Add($"{vis} part(s) need replacement");
        if (emp > 0) probs.Add($"{emp} empty slot(s)");
        if (dusty) probs.Add("needs cleaning");
        string sh, st;
        if (probs.Count == 0 && !hid) { sh = "4AE04A"; st = "System healthy"; }
        else if (probs.Count == 0 && hid) { sh = "C0C8DD"; st = $"{tot} parts - power on to verify"; }
        else { sh = (vis > 0 || emp > 0) ? "E06060" : "D4A843"; st = string.Join(", ", probs); }
        AddTextRow($"<b>SYSTEM:</b>  {tot} parts installed  |  <color=#{sh}>{st}</color>", 30);
    }

    string GetCatHex(string cat)
    {
        switch (cat)
        {
            case "Motherboard": return "4A90D9";
            case "CPU": return "D35400";
            case "GPU": return "C0392B";
            case "RAM": return "27AE60";
            case "Storage": return "8E44AD";
            case "PSU": return "7A8AAA";
            case "Cooler": return "A0785A";
            case "Fan": return "2980B9";
            default: return "8090B0";
        }
    }
}
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class EmailManager : MonoBehaviour
{
    public static EmailManager Instance;

    [Header("Inbox UI (Left Panel)")]
    public GameObject emailInboxPrefab;
    public Transform inboxContentContainer;

    [Header("Detail UI (Right Panel)")]
    public GameObject detailPanel;
    public TextMeshProUGUI senderText;
    public TextMeshProUGUI subjectText;
    public TextMeshProUGUI bodyText;
    public TextMeshProUGUI labourText;
    public TextMeshProUGUI budgetText;
    public TextMeshProUGUI objectivesText;
    public Image detailProfilePic;

    [Header("PC Status Panel UI")]
    public GameObject pcStatusPanel;
    public TextMeshProUGUI pcProblemsText;

    [Header("Build Request Panel UI")]
    public TextMeshProUGUI requestedPartsText;

    public Transform pcSpecsContentContainer;
    public GameObject pcSpecItemPrefab;

    [Header("Action Buttons")]
    public Button acceptButton;
    public Button rejectButton;
    public Button completeButton;

    [Header("Job Database")]
    public List<EmailData> activeEmails = new List<EmailData>();
    public List<EmailData> acceptedJobs = new List<EmailData>();
    private EmailData currentlySelectedEmail;

    [Header("Shop Integration")]
    public Transform[] shelfLocations;
    public GameObject deliveryBoxPrefab;

    [Header("Shipping / Drop-off")]
    public Transform shippingZone;
    public float shippingRadius = 2.0f;

    [Header("Completion Popup UI")]
    public GameObject completionPanel;
    public TextMeshProUGUI completionTitle;
    public TextMeshProUGUI completionDetails;
    public TextMeshProUGUI completionPay;
    public TextMeshProUGUI completionRating;
    public Button completionOKButton;
    [Header("PC Status Button")]
    public Button pcStatusButton;
    public void SetActiveMonitorUI(
        Transform newInboxContainer,
        GameObject newDetailPanel,
        TextMeshProUGUI newSenderText,
        TextMeshProUGUI newSubjectText,
        TextMeshProUGUI newBodyText,
        TextMeshProUGUI newLabourText,
        TextMeshProUGUI newBudgetText,
        TextMeshProUGUI newObjectivesText,
        Image newDetailProfilePic,
        GameObject newPcStatusPanel,
        TextMeshProUGUI newPcProblemsText,
        TextMeshProUGUI newRequestedPartsText,
        Transform newPcSpecsContainer,
        Button newAcceptButton,
        Button newRejectButton,
        Button newCompleteButton,
        GameObject newCompletionPanel,
        TextMeshProUGUI newCompletionTitle,
        TextMeshProUGUI newCompletionDetails,
        TextMeshProUGUI newCompletionPay,
        TextMeshProUGUI newCompletionRating,
        Button newCompletionOKButton,
        Button newPcStatusButton
    )
    {
        inboxContentContainer = newInboxContainer;
        detailPanel = newDetailPanel;
        senderText = newSenderText;
        subjectText = newSubjectText;
        bodyText = newBodyText;
        labourText = newLabourText;
        budgetText = newBudgetText;
        objectivesText = newObjectivesText;
        detailProfilePic = newDetailProfilePic;
        pcStatusPanel = newPcStatusPanel;
        pcProblemsText = newPcProblemsText;
        requestedPartsText = newRequestedPartsText;
        pcSpecsContentContainer = newPcSpecsContainer;
        acceptButton = newAcceptButton;
        rejectButton = newRejectButton;
        completeButton = newCompleteButton;
        completionPanel = newCompletionPanel;
        completionTitle = newCompletionTitle;
        completionDetails = newCompletionDetails;
        completionPay = newCompletionPay;
        completionRating = newCompletionRating;
        completionOKButton = newCompletionOKButton;

        // Refresh with new UI
        if (detailPanel != null) detailPanel.SetActive(false);
        if (pcStatusPanel != null) pcStatusPanel.SetActive(false);
        if (completionPanel != null) completionPanel.SetActive(false);
        if (pcStatusButton != null)
            pcStatusButton.onClick.RemoveListener(TogglePCStatusPanel);
        pcStatusButton = newPcStatusButton;
        if (pcStatusButton != null)
            pcStatusButton.onClick.AddListener(TogglePCStatusPanel);
        RefreshInboxUI();
    }
    void Awake() { Instance = this; }

    void Start()
    {
        if (pcStatusButton != null)
            pcStatusButton.onClick.AddListener(TogglePCStatusPanel);
        if (detailPanel != null) detailPanel.SetActive(false);
        if (pcStatusPanel != null) pcStatusPanel.SetActive(false);
        if (completionPanel != null) completionPanel.SetActive(false);
        RefreshInboxUI();

    }

    public void OpenEmailApp()
    {
        if (detailPanel != null) detailPanel.SetActive(false);
        if (pcStatusPanel != null) pcStatusPanel.SetActive(false);
        RefreshInboxUI();
    }

    public void TogglePCStatusPanel()
    {
        if (pcStatusPanel == null) return;
        pcStatusPanel.SetActive(!pcStatusPanel.activeSelf);
    }

    public void ClosePCStatusPanel()
    {
        if (pcStatusPanel == null) return;
        pcStatusPanel.SetActive(false);
    }

    public void RefreshInboxUI()
    {
        foreach (Transform child in inboxContentContainer) Destroy(child.gameObject);
        foreach (EmailData email in activeEmails) CreateInboxButton(email, "PENDING");
        foreach (EmailData email in acceptedJobs) CreateInboxButton(email, "IN PROGRESS");
    }

    private Transform FindChildRecursive(Transform parent, string targetName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == targetName) return child;
            Transform result = FindChildRecursive(child, targetName);
            if (result != null) return result;
        }
        return null;
    }

    void CreateInboxButton(EmailData email, string statusPrefix)
    {
        GameObject newEmailBtn = Instantiate(emailInboxPrefab, inboxContentContainer);
        Transform t = newEmailBtn.transform;

        Transform senderTransform = FindChildRecursive(t, "Sender Name");
        if (senderTransform != null) senderTransform.GetComponent<TextMeshProUGUI>().text = email.senderName;

        Transform subjectTransform = FindChildRecursive(t, "Subject Line");
        if (subjectTransform != null) subjectTransform.GetComponent<TextMeshProUGUI>().text = $"[{statusPrefix}] " + email.subjectLine;

        Transform profileImageTransform = FindChildRecursive(t, "Profile Image");
        if (profileImageTransform != null && email.profilePic != null) profileImageTransform.GetComponent<Image>().sprite = email.profilePic;

        Button btn = newEmailBtn.GetComponent<Button>();
        if (btn != null) btn.onClick.AddListener(() => SelectEmail(email));
    }

    public void SelectEmail(EmailData email)
    {
        currentlySelectedEmail = email;

        if (detailPanel != null) detailPanel.SetActive(true);
        if (pcStatusPanel != null) pcStatusPanel.SetActive(false);

        if (senderText != null) senderText.text = email.senderName;
        if (subjectText != null) subjectText.text = email.subjectLine;
        if (bodyText != null) bodyText.text = email.bodyText;

        if (pcSpecsContentContainer != null)
        {
            foreach (Transform child in pcSpecsContentContainer) Destroy(child.gameObject);

            if (email.startingParts != null && pcSpecItemPrefab != null)
            {
                Dictionary<string, PartGroup> groupedParts = new Dictionary<string, PartGroup>();
                foreach (StartingPCComponent part in email.startingParts)
                {
                    if (groupedParts.ContainsKey(part.partName)) groupedParts[part.partName].amount++;
                    else { PartGroup newGroup = new PartGroup(); newGroup.part = part; newGroup.amount = 1; groupedParts.Add(part.partName, newGroup); }
                }

                foreach (KeyValuePair<string, PartGroup> kvp in groupedParts)
                {
                    PartGroup group = kvp.Value;
                    GameObject newSpecObj = Instantiate(pcSpecItemPrefab, pcSpecsContentContainer);
                    Transform t = newSpecObj.transform;

                    Transform textObj = FindChildRecursive(t, "Spec Text");
                    if (textObj != null)
                    {
                        string amountString = group.amount > 1 ? $" x{group.amount}" : "";
                        textObj.GetComponent<TextMeshProUGUI>().text = $"<b>{group.part.partCategory}:</b> {group.part.partName}{amountString}";
                    }

                    Transform imgObj = FindChildRecursive(t, "Spec Image");
                    if (imgObj != null && group.part.partIcon != null) imgObj.GetComponent<Image>().sprite = group.part.partIcon;
                }
            }
        }

        if (labourText != null) labourText.text = "Labour: ₱" + email.labourCost.ToString("N0");
        if (budgetText != null) budgetText.text = "Budget: ₱" + email.partsBudget.ToString("N0");
        if (detailProfilePic != null && email.profilePic != null) detailProfilePic.sprite = email.profilePic;

        string combinedObjectives = "";
        if (email.objectives != null) foreach (string obj in email.objectives) combinedObjectives += "• " + obj + "\n";
        if (objectivesText != null) objectivesText.text = combinedObjectives;

        string combinedProblems = "";
        if (email.pcProblems != null)
        {
            foreach (string problem in email.pcProblems) combinedProblems += "<color=red>■</color> " + problem + "\n\n";
        }
        if (pcProblemsText != null) pcProblemsText.text = combinedProblems;

        if (requestedPartsText != null)
        {
            if (email.jobType == JobType.Build && email.requestedParts != null)
            {
                string buildList = "<color=#4AE0FF>■</color> <b>BUILD REQUEST</b>\n\n";
                Dictionary<string, int> grouped = new Dictionary<string, int>();
                foreach (var part in email.requestedParts)
                {
                    string key = part.partCategory + ": " + part.partName;
                    if (grouped.ContainsKey(key)) grouped[key]++;
                    else grouped[key] = 1;
                }
                foreach (var kvp in grouped)
                {
                    string amount = kvp.Value > 1 ? $" x{kvp.Value}" : "";
                    buildList += $"  • {kvp.Key}{amount}\n";
                }
                requestedPartsText.text = buildList;
            }
            else
            {
                requestedPartsText.text = "";
            }
        }

        if (activeEmails.Contains(email))
        {
            if (acceptButton != null) { acceptButton.gameObject.SetActive(true); acceptButton.onClick.RemoveAllListeners(); acceptButton.onClick.AddListener(() => AcceptJob(email)); }
            if (rejectButton != null) { rejectButton.gameObject.SetActive(true); rejectButton.onClick.RemoveAllListeners(); rejectButton.onClick.AddListener(() => RejectJob(email)); }
            if (completeButton != null) completeButton.gameObject.SetActive(false);
        }
        else if (acceptedJobs.Contains(email))
        {
            if (acceptButton != null) acceptButton.gameObject.SetActive(false);
            if (rejectButton != null) rejectButton.gameObject.SetActive(false);
            if (completeButton != null) { completeButton.gameObject.SetActive(true); completeButton.onClick.RemoveAllListeners(); completeButton.onClick.AddListener(() => CompleteJob(email)); }
        }
    }

    public bool SpawnBoxFromData(GameObject casePrefab, List<StartingPCComponent> parts, EmailData email = null)
    {
        int emptySlot = GetEmptyShelfSpot();
        if (emptySlot == -1) return false;

        Transform spot = shelfLocations[emptySlot];

        if (deliveryBoxPrefab != null)
        {
            GameObject newBox = Instantiate(deliveryBoxPrefab, spot.position, spot.rotation);
            JobBox boxScript = newBox.GetComponent<JobBox>();
            if (boxScript != null) boxScript.SetupBox(casePrefab, parts, email);
        }
        return true;
    }

    public void AcceptJob(EmailData email)
    {
        bool success = SpawnBoxFromData(email.basePCCasePrefab, email.startingParts, email);

        if (success)
        {
            activeEmails.Remove(email);
            acceptedJobs.Add(email);
            RefreshInboxUI();
            SelectEmail(email);
        }
        else
        {
            Debug.LogWarning("No empty shelf space!");
        }
    }

    public void RejectJob(EmailData email)
    {
        activeEmails.Remove(email);
        if (detailPanel != null) detailPanel.SetActive(false);
        RefreshInboxUI();
    }

    public void CompleteJob(EmailData email)
    {
        if (shippingZone == null)
        {
            Debug.LogError("Assign a Shipping Zone in the Inspector!");
            return;
        }

        PCCaseBuilder[] allPCs = FindObjectsOfType<PCCaseBuilder>();
        PCCaseBuilder pcToShip = null;

        foreach (PCCaseBuilder pc in allPCs)
        {
            if (Vector3.Distance(pc.transform.position, shippingZone.position) <= shippingRadius)
            {
                pcToShip = pc;
                break;
            }
        }

        if (pcToShip == null)
        {
            Debug.LogWarning("Put the finished PC in the Shipping Zone first!");
            return;
        }

        JobCompletionResult result = EvaluateRepair(pcToShip, email);

        PlayerWallet wallet = FindObjectOfType<PlayerWallet>();
        if (wallet != null) wallet.AddGold(result.totalPay);

        ShowCompletionPopup(result);

        Destroy(pcToShip.gameObject);
        acceptedJobs.Remove(email);
        if (detailPanel != null) detailPanel.SetActive(false);
        RefreshInboxUI();

        Debug.Log($"Job Complete! Rating: {result.starRating}★ | Pay: ₱{result.totalPay:N0}");
    }

    // =============================================
    //  REPAIR & BUILD EVALUATION SYSTEM
    // =============================================
    JobCompletionResult EvaluateRepair(PCCaseBuilder pc, EmailData email)
    {
        JobCompletionResult result = new JobCompletionResult();
        result.customerName = email.senderName;
        result.jobType = email.jobType;

        if (email.jobType == JobType.Build)
        {
            result.problemDescription = "New PC Build";
            EvaluateBuildJob(pc, email, result);
        }
        else
        {
            result.problemDescription = (email.pcProblems != null && email.pcProblems.Length > 0)
                ? email.pcProblems[0] : "General Repair";
            EvaluateRepairJob(pc, email, result);
        }

        return result;
    }

    void EvaluateRepairJob(PCCaseBuilder pc, EmailData email, JobCompletionResult result)
    {
        InspectableItem[] allParts = pc.GetComponentsInChildren<InspectableItem>(true);
        int remainingFaults = 0;
        List<string> unfixedIssues = new List<string>();

        foreach (InspectableItem part in allParts)
        {
            if (part.isInventorySlot) continue;
            if (part.IsFaulty())
            {
                remainingFaults++;
                unfixedIssues.Add($"{part.itemName}: {part.faultDescription}");
            }
        }

        DustSystem dust = pc.GetComponent<DustSystem>();
        bool stillDusty = (dust != null && dust.isDusty);
        if (stillDusty) unfixedIssues.Add("PC is still dusty");

        int emptyEssentialSlots = 0;
        string[] essentialCategories = { "Motherboard", "PSU", "CPU" };
        foreach (InspectableItem part in allParts)
        {
            if (!part.isInventorySlot) continue;
            foreach (string essential in essentialCategories)
            {
                if (part.partCategory == essential)
                {
                    emptyEssentialSlots++;
                    unfixedIssues.Add($"Missing {essential}!");
                    break;
                }
            }
        }

        result.unfixedIssues = unfixedIssues;
        result.remainingFaults = remainingFaults;
        result.originalFaultCount = email.originalFaultCount;

        float score = 1.0f;
        if (email.originalFaultCount > 0)
        {
            float faultPenalty = (float)remainingFaults / email.originalFaultCount;
            score -= faultPenalty * 0.6f;
        }
        if (stillDusty) score -= 0.15f;
        score -= emptyEssentialSlots * 0.2f;
        score = Mathf.Clamp01(score);

        CalculatePayment(score, email, result);
    }

    void EvaluateBuildJob(PCCaseBuilder pc, EmailData email, JobCompletionResult result)
    {
        if (email.requestedParts == null || email.requestedParts.Count == 0)
        {
            result.score = 1f;
            CalculatePayment(1f, email, result);
            return;
        }

        InspectableItem[] allParts = pc.GetComponentsInChildren<InspectableItem>(true);
        List<string> unfixedIssues = new List<string>();

        Dictionary<string, int> requestedCounts = new Dictionary<string, int>();
        foreach (var req in email.requestedParts)
        {
            if (requestedCounts.ContainsKey(req.partCategory)) requestedCounts[req.partCategory]++;
            else requestedCounts[req.partCategory] = 1;
        }

        Dictionary<string, int> installedCounts = new Dictionary<string, int>();
        foreach (InspectableItem part in allParts)
        {
            if (part.isInventorySlot) continue;
            if (part.isMainObject) continue;

            if (installedCounts.ContainsKey(part.partCategory)) installedCounts[part.partCategory]++;
            else installedCounts[part.partCategory] = 1;
        }

        int totalRequested = email.requestedParts.Count;
        int totalFulfilled = 0;

        foreach (var kvp in requestedCounts)
        {
            string category = kvp.Key;
            int needed = kvp.Value;
            int installed = installedCounts.ContainsKey(category) ? installedCounts[category] : 0;

            int fulfilled = Mathf.Min(installed, needed);
            totalFulfilled += fulfilled;

            int missing = needed - fulfilled;
            if (missing > 0)
            {
                string label = missing > 1 ? $"Missing {category} x{missing}" : $"Missing {category}";
                unfixedIssues.Add(label);
            }
        }

        result.unfixedIssues = unfixedIssues;
        result.originalFaultCount = totalRequested;
        result.remainingFaults = totalRequested - totalFulfilled;

        float score = (totalRequested > 0) ? (float)totalFulfilled / totalRequested : 1f;

        score = Mathf.Clamp01(score);
        CalculatePayment(score, email, result);
    }

    void CalculatePayment(float score, EmailData email, JobCompletionResult result)
    {
        result.score = score;

        if (score >= 0.95f) result.starRating = 5;
        else if (score >= 0.80f) result.starRating = 4;
        else if (score >= 0.60f) result.starRating = 3;
        else if (score >= 0.40f) result.starRating = 2;
        else result.starRating = 1;

        float basePay = email.labourCost;
        float earnedLabour = basePay * score;

        float tipBonus = 0f;
        if (result.starRating == 5) tipBonus = basePay * 0.25f;
        else if (result.starRating == 4) tipBonus = basePay * 0.10f;

        result.basePay = basePay;
        result.earnedLabour = Mathf.Round(earnedLabour);
        result.tipBonus = Mathf.Round(tipBonus);
        result.totalPay = Mathf.Round(earnedLabour + tipBonus);
    }

    void ShowCompletionPopup(JobCompletionResult result)
    {
        if (completionPanel == null) return;

        completionPanel.SetActive(true);

        if (completionTitle != null)
        {
            string prefix = (result.jobType == JobType.Build) ? "Build" : "Repair";

            string[] titles5 = { $"Perfect {prefix}!", $"Flawless Work!", "Master Technician!" };
            string[] titles4 = { "Great Job!", $"Solid {prefix}!", "Well Done!" };
            string[] titles3 = { "Job Done", "Acceptable Work", "Could Be Better" };
            string[] titles2 = { "Sloppy Work...", "Customer Unhappy", "Needs Improvement" };
            string[] titles1 = { "Terrible Job!", "Customer Furious!", "What Happened?!" };

            string[] pool;
            switch (result.starRating)
            {
                case 5: pool = titles5; break;
                case 4: pool = titles4; break;
                case 3: pool = titles3; break;
                case 2: pool = titles2; break;
                default: pool = titles1; break;
            }
            completionTitle.text = pool[Random.Range(0, pool.Length)];
        }

        if (completionRating != null)
        {
            string stars = "";
            for (int i = 0; i < 5; i++)
            {
                if (i < result.starRating) stars += "<color=#FFD700>★</color>";
                else stars += "<color=#555555>★</color>";
            }
            completionRating.text = stars;
        }

        if (completionDetails != null)
        {
            string details = $"<b>Customer:</b> {result.customerName}\n";
            details += $"<b>Job:</b> {result.problemDescription}\n\n";

            if (result.unfixedIssues.Count == 0)
            {
                string doneMsg = (result.jobType == JobType.Build)
                    ? "All requested parts installed!"
                    : "All issues resolved!";
                details += $"<color=#4AFF4A>{doneMsg}</color>";
            }
            else
            {
                string issueLabel = (result.jobType == JobType.Build)
                    ? "Missing parts:"
                    : "Unfixed issues:";
                details += $"<color=#FF6B6B>{issueLabel}</color>\n";
                foreach (string issue in result.unfixedIssues)
                {
                    details += $"  <color=#FF8888>• {issue}</color>\n";
                }
            }

            if (result.starRating == 5)
                details += "\n\n<color=#FFD700>Customer left a tip!</color>";

            completionDetails.text = details;
        }

        if (completionPay != null)
        {
            string payText = $"Labour: ₱{result.earnedLabour:N0}";
            if (result.tipBonus > 0) payText += $"\n<color=#FFD700>Tip: +₱{result.tipBonus:N0}</color>";
            payText += $"\n\n<b><size=120%>Total: ₱{result.totalPay:N0}</size></b>";
            completionPay.text = payText;
        }

        if (completionOKButton != null)
        {
            completionOKButton.onClick.RemoveAllListeners();
            completionOKButton.onClick.AddListener(() => { completionPanel.SetActive(false); });
        }
    }

    public void ReceiveWalkInJob(EmailData emailTemplate, string customerName)
    {
        EmailData newJob = Instantiate(emailTemplate);
        newJob.senderName = customerName + " (Walk-In)";
        newJob.subjectLine = "In-Store Repair Drop-off";

        bool success = SpawnBoxFromData(newJob.basePCCasePrefab, newJob.startingParts, newJob);

        if (!success)
        {
            Debug.LogWarning("Shelves are full! Cannot accept walk-in.");
            return;
        }

        acceptedJobs.Add(newJob);
        RefreshInboxUI();
    }

    private int GetEmptyShelfSpot()
    {
        for (int i = 0; i < shelfLocations.Length; i++)
        {
            Transform spot = shelfLocations[i];
            Collider[] itemsOnShelf = Physics.OverlapSphere(spot.position, 0.2f);
            bool isFull = false;
            foreach (Collider item in itemsOnShelf)
            {
                if (item.GetComponentInParent<JobBox>() != null || item.GetComponentInParent<PCCaseBuilder>() != null)
                {
                    isFull = true;
                    break;
                }
            }
            if (!isFull) return i;
        }
        return -1;
    }
}

public class PartGroup
{
    public StartingPCComponent part;
    public int amount;
}

public class JobCompletionResult
{
    public string customerName;
    public string problemDescription;
    public JobType jobType;

    public int originalFaultCount;
    public int remainingFaults;
    public bool wasDustCleaned;
    public int missingEssentialParts;
    public List<string> unfixedIssues = new List<string>();

    public float score;
    public int starRating;

    public float basePay;
    public float earnedLabour;
    public float tipBonus;
    public float totalPay;
}
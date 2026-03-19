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

    // NEW: Container and Prefab for individual PC Specs
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

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (detailPanel != null) detailPanel.SetActive(false);
        if (pcStatusPanel != null) pcStatusPanel.SetActive(false);
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

        foreach (EmailData email in activeEmails)
        {
            CreateInboxButton(email, "PENDING");
        }

        foreach (EmailData email in acceptedJobs)
        {
            CreateInboxButton(email, "IN PROGRESS");
        }
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
        if (btn != null)
        {
            btn.onClick.AddListener(() => SelectEmail(email));
        }
    }

    public void SelectEmail(EmailData email)
    {
        currentlySelectedEmail = email;

        if (detailPanel != null) detailPanel.SetActive(true);
        if (pcStatusPanel != null) pcStatusPanel.SetActive(false);

        if (senderText != null) senderText.text = email.senderName;
        if (subjectText != null) subjectText.text = email.subjectLine;
        if (bodyText != null) bodyText.text = email.bodyText;

        // ---------------------------------------------------------
        // --- NEW: DYNAMIC PC SPECS GROUPING & SPAWNING ---
        // ---------------------------------------------------------
        if (pcSpecsContentContainer != null)
        {
            // 1. Clear out old specs from the previous email
            foreach (Transform child in pcSpecsContentContainer)
            {
                Destroy(child.gameObject);
            }

            if (email.startingParts != null && pcSpecItemPrefab != null)
            {
                // 2. Count and group identical parts
                Dictionary<string, PartGroup> groupedParts = new Dictionary<string, PartGroup>();

                foreach (StartingPCComponent part in email.startingParts)
                {
                    if (groupedParts.ContainsKey(part.partName))
                    {
                        groupedParts[part.partName].amount++; // Found a duplicate, add to the count!
                    }
                    else
                    {
                        PartGroup newGroup = new PartGroup();
                        newGroup.part = part;
                        newGroup.amount = 1;
                        groupedParts.Add(part.partName, newGroup); // First time seeing this part
                    }
                }

                // 3. Spawn a prefab for each unique group
                foreach (KeyValuePair<string, PartGroup> kvp in groupedParts)
                {
                    PartGroup group = kvp.Value;

                    GameObject newSpecObj = Instantiate(pcSpecItemPrefab, pcSpecsContentContainer);
                    Transform t = newSpecObj.transform;

                    // Apply the grouped text (e.g., "RAM: 8GB DDR4 x4")
                    Transform textObj = FindChildRecursive(t, "Spec Text");
                    if (textObj != null)
                    {
                        string amountString = group.amount > 1 ? $" x{group.amount}" : "";
                        textObj.GetComponent<TextMeshProUGUI>().text = $"<b>{group.part.partCategory}:</b> {group.part.partName}{amountString}";
                    }

                    // Apply the icon
                    Transform imgObj = FindChildRecursive(t, "Spec Image");
                    if (imgObj != null && group.part.partIcon != null)
                    {
                        imgObj.GetComponent<Image>().sprite = group.part.partIcon;
                    }
                }
            }
        }
        // ---------------------------------------------------------

        if (labourText != null) labourText.text = "Labour: ₱" + email.labourCost.ToString("N0");
        if (budgetText != null) budgetText.text = "Budget: ₱" + email.partsBudget.ToString("N0");
        if (detailProfilePic != null && email.profilePic != null) detailProfilePic.sprite = email.profilePic;

        string combinedObjectives = "";
        if (email.objectives != null)
        {
            foreach (string obj in email.objectives) combinedObjectives += "• " + obj + "\n";
        }
        if (objectivesText != null) objectivesText.text = combinedObjectives;

        string combinedProblems = "";
        if (email.pcProblems != null)
        {
            foreach (string problem in email.pcProblems) combinedProblems += "<color=red>■</color> " + problem + "\n\n";
        }
        if (pcProblemsText != null) pcProblemsText.text = combinedProblems;

        if (activeEmails.Contains(email))
        {
            if (acceptButton != null) acceptButton.gameObject.SetActive(true);
            if (rejectButton != null) rejectButton.gameObject.SetActive(true);
            if (completeButton != null) completeButton.gameObject.SetActive(false);

            if (acceptButton != null)
            {
                acceptButton.onClick.RemoveAllListeners();
                acceptButton.onClick.AddListener(() => AcceptJob(email));
            }

            if (rejectButton != null)
            {
                rejectButton.onClick.RemoveAllListeners();
                rejectButton.onClick.AddListener(() => RejectJob(email));
            }
        }
        else if (acceptedJobs.Contains(email))
        {
            if (acceptButton != null) acceptButton.gameObject.SetActive(false);
            if (rejectButton != null) rejectButton.gameObject.SetActive(false);
            if (completeButton != null) completeButton.gameObject.SetActive(true);

            if (completeButton != null)
            {
                completeButton.onClick.RemoveAllListeners();
                completeButton.onClick.AddListener(() => CompleteJob(email));
            }
        }
    }

    public bool SpawnBoxFromData(GameObject casePrefab, List<StartingPCComponent> parts)
    {
        int emptySlot = GetEmptyShelfSpot();
        if (emptySlot == -1) return false;

        Transform spot = shelfLocations[emptySlot];

        if (deliveryBoxPrefab != null)
        {
            GameObject newBox = Instantiate(deliveryBoxPrefab, spot.position, spot.rotation);
            JobBox boxScript = newBox.GetComponent<JobBox>();
            if (boxScript != null) boxScript.SetupBox(casePrefab, parts);
        }
        return true;
    }

    public void AcceptJob(EmailData email)
    {
        bool success = SpawnBoxFromData(email.basePCCasePrefab, email.startingParts);

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

        if (pcToShip != null)
        {
            Destroy(pcToShip.gameObject);

            float totalPay = email.labourCost + email.partsBudget;
            PlayerWallet wallet = FindObjectOfType<PlayerWallet>();
            if (wallet != null) wallet.AddGold(totalPay);

            acceptedJobs.Remove(email);
            if (detailPanel != null) detailPanel.SetActive(false);
            RefreshInboxUI();

            Debug.Log("PC Shipped! Paid ₱" + totalPay);
        }
        else
        {
            Debug.LogWarning("Put the finished PC in the Shipping Zone first!");
        }
    }

    public void ReceiveWalkInJob(EmailData emailTemplate, string customerName)
    {
        bool success = SpawnBoxFromData(emailTemplate.basePCCasePrefab, emailTemplate.startingParts);

        if (!success)
        {
            Debug.LogWarning("Shelves are full! Cannot accept walk-in.");
            return;
        }

        EmailData newJob = Instantiate(emailTemplate);
        newJob.senderName = customerName + " (Walk-In)";
        newJob.subjectLine = "In-Store Repair Drop-off";

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

// Helper class to group duplicate parts together
public class PartGroup
{
    public StartingPCComponent part;
    public int amount;
}
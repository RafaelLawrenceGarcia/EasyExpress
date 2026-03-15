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

    [Header("Action Buttons")]
    public Button acceptButton;
    public Button rejectButton;
    public Button completeButton; // For finishing jobs

    [Header("Job Database")]
    public List<EmailData> activeEmails = new List<EmailData>(); // Pending Emails
    public List<EmailData> acceptedJobs = new List<EmailData>(); // Active Jobs (Emails + Walk-ins)
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

    public void RefreshInboxUI()
    {
        foreach (Transform child in inboxContentContainer) Destroy(child.gameObject);

        // 1. Show Pending Emails
        foreach (EmailData email in activeEmails)
        {
            CreateInboxButton(email, "PENDING");
        }

        // 2. Show Accepted/Walk-in Jobs
        foreach (EmailData email in acceptedJobs)
        {
            CreateInboxButton(email, "IN PROGRESS");
        }
    }

    void CreateInboxButton(EmailData email, string statusPrefix)
    {
        GameObject newEmailBtn = Instantiate(emailInboxPrefab, inboxContentContainer);
        Transform t = newEmailBtn.transform;

        Transform senderTransform = t.Find("Sender Name");
        if (senderTransform != null) senderTransform.GetComponent<TextMeshProUGUI>().text = email.senderName;
        
        Transform subjectTransform = t.Find("Subject Line");
        if (subjectTransform != null) subjectTransform.GetComponent<TextMeshProUGUI>().text = $"[{statusPrefix}] " + email.subjectLine;

        Transform profileImageTransform = t.Find("Profile Image");
        if (profileImageTransform != null) profileImageTransform.GetComponent<Image>().sprite = email.profilePic;

        Button btn = newEmailBtn.GetComponent<Button>();
        btn.onClick.AddListener(() => SelectEmail(email));
    }

    public void SelectEmail(EmailData email)
    {
        currentlySelectedEmail = email;
        detailPanel.SetActive(true); 

        senderText.text = email.senderName;
        subjectText.text = email.subjectLine;
        
        string specText = "\n\n<b>Current PC Specs:</b>\n";
        foreach (StartingPCComponent part in email.startingParts) specText += $"• <b>{part.partCategory}</b>: {part.partName}\n";
        bodyText.text = email.bodyText + specText;

        labourText.text = "Labour: ₱" + email.labourCost.ToString("N0");
        budgetText.text = "Budget: ₱" + email.partsBudget.ToString("N0");
        if (detailProfilePic != null) detailProfilePic.sprite = email.profilePic;

        string combinedObjectives = "";
        foreach (string obj in email.objectives) combinedObjectives += "• " + obj + "\n";
        objectivesText.text = combinedObjectives;

        string combinedProblems = "";
        foreach (string problem in email.pcProblems) combinedProblems += "<color=red>■</color> " + problem + "\n\n";
        if (pcProblemsText != null) pcProblemsText.text = combinedProblems;

        if (pcStatusPanel != null) pcStatusPanel.SetActive(false);

        // TOGGLE BUTTONS: If it's a pending email, show Accept/Reject. If it's active, show Complete.
        if (activeEmails.Contains(email))
        {
            acceptButton.gameObject.SetActive(true);
            rejectButton.gameObject.SetActive(true);
            if (completeButton != null) completeButton.gameObject.SetActive(false);

            acceptButton.onClick.RemoveAllListeners();
            acceptButton.onClick.AddListener(() => AcceptJob(email));

            rejectButton.onClick.RemoveAllListeners();
            rejectButton.onClick.AddListener(() => RejectJob(email));
        }
        else if (acceptedJobs.Contains(email))
        {
            acceptButton.gameObject.SetActive(false);
            rejectButton.gameObject.SetActive(false);
            if (completeButton != null) completeButton.gameObject.SetActive(true);

            completeButton.onClick.RemoveAllListeners();
            completeButton.onClick.AddListener(() => CompleteJob(email));
        }
    }

    // --- REUSABLE SPAWNING LOGIC ---
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

    // --- EMAIL BUTTON LOGIC ---
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
        detailPanel.SetActive(false); 
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
            detailPanel.SetActive(false);
            RefreshInboxUI();

            Debug.Log("PC Shipped! Paid ₱" + totalPay);
        }
        else
        {
            Debug.LogWarning("Put the finished PC in the Shipping Zone first!");
        }
    }

    // --- WALK-IN CUSTOMER LOGIC ---
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

    // --- NEW SMART PHYSICS SHELF CHECKER ---
    private int GetEmptyShelfSpot()
    {
        for (int i = 0; i < shelfLocations.Length; i++)
        {
            Transform spot = shelfLocations[i];
            
            // Draw a tiny invisible sphere on the shelf to see if a box is touching it
            Collider[] itemsOnShelf = Physics.OverlapSphere(spot.position, 0.2f);
            bool isFull = false;
            
            foreach (Collider item in itemsOnShelf)
            {
                // If the sphere touches a box or a PC, this spot is taken!
                if (item.GetComponentInParent<JobBox>() != null || item.GetComponentInParent<PCCaseBuilder>() != null)
                {
                    isFull = true;
                    break;
                }
            }

            // If the sphere didn't touch anything, this shelf is completely empty!
            if (!isFull) return i; 
        }
        return -1; 
    }
}
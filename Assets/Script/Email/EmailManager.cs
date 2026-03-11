using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class EmailManager : MonoBehaviour
{
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

    [Header("Email Database")]
    public List<EmailData> activeEmails = new List<EmailData>();
    private EmailData currentlySelectedEmail;

    [Header("Shop Integration")]
    [Tooltip("Drag your green cardboard box Transforms here.")]
    public Transform[] shelfLocations; 
    private bool[] locationOccupied;

    void Start()
    {
        if (detailPanel != null) detailPanel.SetActive(false);
        if (pcStatusPanel != null) pcStatusPanel.SetActive(false); 
        
        // Initialize shelf tracking
        if (shelfLocations.Length > 0) locationOccupied = new bool[shelfLocations.Length];

        RefreshInboxUI();
    }

    public void RefreshInboxUI()
    {
        foreach (Transform child in inboxContentContainer)
        {
            Destroy(child.gameObject);
        }

        foreach (EmailData email in activeEmails)
        {
            GameObject newEmailBtn = Instantiate(emailInboxPrefab, inboxContentContainer);
            Transform t = newEmailBtn.transform;

            Transform senderTransform = t.Find("Sender Name");
            if (senderTransform != null) senderTransform.GetComponent<TextMeshProUGUI>().text = email.senderName;
            
            Transform subjectTransform = t.Find("Subject Line");
            if (subjectTransform != null) subjectTransform.GetComponent<TextMeshProUGUI>().text = email.subjectLine;

            Transform profileImageTransform = t.Find("Profile Image");
            if (profileImageTransform != null) profileImageTransform.GetComponent<Image>().sprite = email.profilePic;

            Button btn = newEmailBtn.GetComponent<Button>();
            btn.onClick.AddListener(() => SelectEmail(email));
        }
    }

    public void SelectEmail(EmailData email)
    {
        currentlySelectedEmail = email;
        detailPanel.SetActive(true); 

        senderText.text = email.senderName;
        subjectText.text = email.subjectLine;
        
        // Dynamically build the text showing the PC specs!
        string specText = "\n\n<b>Current PC Specs:</b>\n";
        foreach (StartingPCComponent part in email.startingParts)
        {
            specText += $"• <b>{part.partCategory}</b>: {part.partName}\n";
        }
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

        acceptButton.onClick.RemoveAllListeners();
        acceptButton.onClick.AddListener(() => AcceptJob(email));

        rejectButton.onClick.RemoveAllListeners();
        rejectButton.onClick.AddListener(() => RejectJob(email));
    }

    public void TogglePCStatusPanel()
    {
        if (pcStatusPanel != null) pcStatusPanel.SetActive(!pcStatusPanel.activeSelf);
    }

    public void AcceptJob(EmailData email)
    {
        int emptySlot = GetEmptyShelfSpot();

        if (emptySlot == -1)
        {
            Debug.LogWarning("No empty shelf space to accept this job!");
            return; 
        }

        Debug.Log("Job Accepted from " + email.senderName + "!");
        
        locationOccupied[emptySlot] = true;

        if (email.basePCCasePrefab != null)
        {
            Transform spot = shelfLocations[emptySlot];
            
            // FIX: Spawn at the spot's position, but DO NOT become a child of the tiny box!
            GameObject newPC = Instantiate(email.basePCCasePrefab, spot.position, spot.rotation);

            PCCaseBuilder builder = newPC.GetComponent<PCCaseBuilder>();
            if (builder != null)
            {
                builder.BuildFromData(email.startingParts);
            }
        }

        RemoveEmail(email);
    }

    public void RejectJob(EmailData email)
    {
        Debug.Log("Job Rejected from " + email.senderName + "!");
        RemoveEmail(email);
    }

    private void RemoveEmail(EmailData email)
    {
        activeEmails.Remove(email);
        detailPanel.SetActive(false); 
        RefreshInboxUI();             
    }

    // Helper method to find the first available shelf box
    private int GetEmptyShelfSpot()
    {
        for (int i = 0; i < shelfLocations.Length; i++)
        {
            if (!locationOccupied[i]) return i;
        }
        return -1; // -1 means all spots are full
    }
}
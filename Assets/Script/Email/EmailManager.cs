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

    void Start()
    {
        // Hide both panels when the game starts
        if (detailPanel != null) detailPanel.SetActive(false);
        if (pcStatusPanel != null) pcStatusPanel.SetActive(false); 
        
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

        // SAFELY look for the Sender Name
        Transform senderTransform = t.Find("Sender Name");
        if (senderTransform != null) {
            senderTransform.GetComponent<TextMeshProUGUI>().text = email.senderName;
        } else {
            Debug.LogWarning("Could not find a child named 'Sender Name' on your prefab!");
        }

        // SAFELY look for the Subject Line
        Transform subjectTransform = t.Find("Subject Line");
        if (subjectTransform != null) {
            subjectTransform.GetComponent<TextMeshProUGUI>().text = email.subjectLine;
        }

        // SAFELY look for the Profile Image
        Transform profileImageTransform = t.Find("Profile Image");
        if (profileImageTransform != null)
        {
            profileImageTransform.GetComponent<Image>().sprite = email.profilePic;
        }

        // Because the script didn't crash above, this will successfully run!
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
        bodyText.text = email.bodyText;
        labourText.text = "Labour: ₱" + email.labourCost.ToString("N0");
        budgetText.text = "Budget: ₱" + email.partsBudget.ToString("N0");

        if (detailProfilePic != null)
        {
            detailProfilePic.sprite = email.profilePic;
        }

        string combinedObjectives = "";
        foreach (string obj in email.objectives)
        {
            combinedObjectives += "• " + obj + "\n";
        }
        objectivesText.text = combinedObjectives;

        // Format the PC problems with a red square!
        string combinedProblems = "";
        foreach (string problem in email.pcProblems)
        {
            combinedProblems += "<color=red>■</color> " + problem + "\n\n"; 
        }
        
        if (pcProblemsText != null) pcProblemsText.text = combinedProblems;

        // Force the status panel closed when clicking a new email
        if (pcStatusPanel != null) pcStatusPanel.SetActive(false);

        acceptButton.onClick.RemoveAllListeners();
        acceptButton.onClick.AddListener(() => AcceptJob(email));

        rejectButton.onClick.RemoveAllListeners();
        rejectButton.onClick.AddListener(() => RejectJob(email));
    }

    public void TogglePCStatusPanel()
    {
        if (pcStatusPanel != null)
        {
            pcStatusPanel.SetActive(!pcStatusPanel.activeSelf);
        }
    }

    public void AcceptJob(EmailData email)
    {
        Debug.Log("Job Accepted from " + email.senderName + "!");
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
}
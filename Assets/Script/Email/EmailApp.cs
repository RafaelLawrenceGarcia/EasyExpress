using UnityEngine;
using TMPro;

public class EmailApp : MonoBehaviour
{
    [Header("Panels")]
    public GameObject inboxPanel;      // The middle part (Email List)
    public GameObject pcStatusPanel;   // The left side panel

    [Header("Status Text")]
    public TextMeshProUGUI statusDetailsText; // The text inside the status panel

    private void OnEnable()
    {
        // Default state when opening Email app
        ShowInbox();
    }

    public void ShowInbox()
    {
        if (inboxPanel) inboxPanel.SetActive(true);
        if (pcStatusPanel) pcStatusPanel.SetActive(false);
    }

    public void ShowPCStatus()
    {
        if (inboxPanel) inboxPanel.SetActive(false); // Optional: hide inbox?
        if (pcStatusPanel) pcStatusPanel.SetActive(true);

        UpdateStatusText();
    }

    void UpdateStatusText()
    {
        if (statusDetailsText)
        {
            // You can link this to real variables later
            statusDetailsText.text = "SYSTEM HEALTH: GOOD\n" +
                                     "-------------------\n" +
                                     "CPU Temp: 45°C\n" +
                                     "Storage: 500GB Free\n" +
                                     "Antivirus: Active";
        }
    }
}
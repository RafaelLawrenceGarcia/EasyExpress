using UnityEngine;

public class PCController : MonoBehaviour
{
    [Header("Desktop")]
    public GameObject desktopPanel; // Assign 'Computer_own' here

    [Header("Applications")]
    public GameObject storeAppPanel;    // Assign 'Store' here
    public GameObject furnitureShopPanel; // Assign 'Shop' here (if different)
    public GameObject emailAppPanel;    // Assign 'Email' here
    public GameObject programsAppPanel; // Assign 'ADD/REMOVE PROGRAMS' here

    private void Start()
    {
        ShowDesktop();
    }

    // --- NAVIGATION LOGIC ---

    public void ShowDesktop()
    {
        // 1. Turn ON the Desktop
        if (desktopPanel) desktopPanel.SetActive(true);

        // 2. Turn OFF all apps
        if (storeAppPanel) storeAppPanel.SetActive(false);
        if (furnitureShopPanel) furnitureShopPanel.SetActive(false);
        if (emailAppPanel) emailAppPanel.SetActive(false);
        if (programsAppPanel) programsAppPanel.SetActive(false);
    }

    // --- APP OPENING FUNCTIONS ---
    // Connect these to your Desktop Icons!

    public void OpenStoreApp()
    {
        // We keep desktop TRUE so you can see the wallpaper behind it
        if (storeAppPanel) storeAppPanel.SetActive(true);
    }

    public void OpenFurnitureShop()
    {
        if (furnitureShopPanel) furnitureShopPanel.SetActive(true);
    }

    public void OpenEmailApp()
    {
        if (emailAppPanel) emailAppPanel.SetActive(true);
    }

    public void OpenProgramsApp()
    {
        if (programsAppPanel) programsAppPanel.SetActive(true);
    }

    // --- INPUT HANDLING ---
    public bool HandleEscapeInput()
    {
        // Check if ANY app is open. If so, close it and go to Desktop.
        bool isAnyAppOpen = 
            (storeAppPanel != null && storeAppPanel.activeSelf) ||
            (furnitureShopPanel != null && furnitureShopPanel.activeSelf) ||
            (emailAppPanel != null && emailAppPanel.activeSelf) ||
            (programsAppPanel != null && programsAppPanel.activeSelf);

        if (isAnyAppOpen)
        {
            ShowDesktop(); // Close apps, return to desktop
            return false;  // Do NOT stand up from chair yet
        }

        // If no apps are open, we are already at the desktop, so we can leave.
        return true; 
    }
}
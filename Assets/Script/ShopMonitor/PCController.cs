using UnityEngine;

public class PCController : MonoBehaviour
{
    [Header("Desktop")]
    public GameObject desktopPanel; // Assign 'Computer_own' here

    [Header("Applications")]
    public GameObject storeAppPanel;
    public GameObject furnitureShopPanel;
    public GameObject emailAppPanel;
    public GameObject programsAppPanel;
    public GameObject wallpaperAppPanel; // New Wallpaper App slot!

    private void Start()
    {
        ShowDesktop();
    }

    // --- NAVIGATION LOGIC ---

    public void ShowDesktop()
    {
        // 1. Turn ON the Desktop
        if (desktopPanel) desktopPanel.SetActive(true);

        // 2. Turn OFF all apps to prevent overlapping
        if (storeAppPanel) storeAppPanel.SetActive(false);
        if (furnitureShopPanel) furnitureShopPanel.SetActive(false);
        if (emailAppPanel) emailAppPanel.SetActive(false);
        if (programsAppPanel) programsAppPanel.SetActive(false);
        if (wallpaperAppPanel) wallpaperAppPanel.SetActive(false);
    }

    // --- NEW: CLOSE BUTTON FUNCTION ---
    // Connect this to the red "X" buttons on your app panels!
    public void CloseCurrentApp()
    {
        ShowDesktop(); 
    }

    // --- APP OPENING FUNCTIONS ---

    public void OpenStoreApp()
    {
        ShowDesktop(); // Closes everything else first!
        if (storeAppPanel) storeAppPanel.SetActive(true);
    }

    public void OpenFurnitureShop()
    {
        ShowDesktop();
        if (furnitureShopPanel) furnitureShopPanel.SetActive(true);
    }

    public void OpenEmailApp()
    {
        ShowDesktop();
        if (emailAppPanel) emailAppPanel.SetActive(true);
    }

    public void OpenProgramsApp()
    {
        ShowDesktop();
        if (programsAppPanel) programsAppPanel.SetActive(true);
    }

    public void OpenWallpaperApp()
    {
        ShowDesktop();
        if (wallpaperAppPanel) wallpaperAppPanel.SetActive(true);
    }

    // --- INPUT HANDLING ---
    public bool HandleEscapeInput()
    {
        // Added the wallpaper app to the check list
        bool isAnyAppOpen = 
            (storeAppPanel != null && storeAppPanel.activeSelf) ||
            (furnitureShopPanel != null && furnitureShopPanel.activeSelf) ||
            (emailAppPanel != null && emailAppPanel.activeSelf) ||
            (programsAppPanel != null && programsAppPanel.activeSelf) ||
            (wallpaperAppPanel != null && wallpaperAppPanel.activeSelf);

        if (isAnyAppOpen)
        {
            ShowDesktop(); // Close apps, return to desktop
            return false;  // Do NOT stand up from chair yet
        }

        // If no apps are open, we are already at the desktop, so we can leave.
        return true; 
    }
}
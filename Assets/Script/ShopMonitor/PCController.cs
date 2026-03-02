using UnityEngine;

public class PCController : MonoBehaviour
{
    [Header("Desktop")]
    public GameObject desktopPanel; 

    [Header("Applications")]
    public GameObject storeAppPanel;  // This is StoreUI (Categories)
    public GameObject storeAppPanel2; // NEW: This is StoreUI2 (Item List)
    public GameObject furnitureShopPanel;
    public GameObject emailAppPanel;
    public GameObject programsAppPanel;
    public GameObject wallpaperAppPanel; 

    private void Start()
    {
        ShowDesktop();
    }

    public void ShowDesktop()
    {
        if (desktopPanel) desktopPanel.SetActive(true);

        // Turn OFF BOTH store panels when going to desktop
        if (storeAppPanel) storeAppPanel.SetActive(false);
        if (storeAppPanel2) storeAppPanel2.SetActive(false); 
        
        if (furnitureShopPanel) furnitureShopPanel.SetActive(false);
        if (emailAppPanel) emailAppPanel.SetActive(false);
        if (programsAppPanel) programsAppPanel.SetActive(false);
        if (wallpaperAppPanel) wallpaperAppPanel.SetActive(false);
    }

    public void CloseCurrentApp()
    {
        ShowDesktop(); 
    }

    public void OpenStoreApp()
    {
        ShowDesktop(); 
        // Always start the store at the Category screen
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
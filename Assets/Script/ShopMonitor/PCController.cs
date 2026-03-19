using UnityEngine;
using TMPro; // Needed for the Search Bar
using UnityEngine.UI;

public class PCController : MonoBehaviour
{
    [Header("Main System")]
    public GameObject screenContainer; // NEW: Groups Desktop + Apps together. Disable to "Power Off"
    public GameObject desktopPanel;
    public GameObject biosPanel;       // NEW: The BIOS screen

    [Header("Applications")]
    public GameObject storeAppPanel;
    public GameObject storeAppPanel2;
    public GameObject furnitureShopPanel;
    public GameObject emailAppPanel;
    public GameObject programsAppPanel;
    public GameObject wallpaperAppPanel;

    [Header("Search System")]
    public GameObject searchPanel;         // NEW: The popup menu
    public TMP_InputField searchInput;     // NEW: The typing area
    public GameObject[] searchableIcons;   // NEW: The app icons you want to filter

    private bool isPoweredOn = true;

    private void Start()
    {
        BootToOS(); // Start up normally

        // Auto-listen to the search bar when the player types
        if (searchInput != null)
        {
            searchInput.onValueChanged.AddListener(FilterApps);
        }
    }

    // --- POWER & BIOS LOGIC ---
    public void PowerOff()
    {
        isPoweredOn = false;
        if (screenContainer) screenContainer.SetActive(false);
        if (biosPanel) biosPanel.SetActive(false);
    }

    public void RestartToBIOS()
    {
        isPoweredOn = true;
        if (screenContainer) screenContainer.SetActive(false); // Hide the OS
        if (biosPanel) biosPanel.SetActive(true);              // Show the BIOS
    }

    public void BootToOS()
    {
        isPoweredOn = true;
        if (biosPanel) biosPanel.SetActive(false);
        if (screenContainer) screenContainer.SetActive(true);
        ShowDesktop();
    }

    // --- DESKTOP & APP LOGIC ---
    public void ShowDesktop()
    {
        if (desktopPanel) desktopPanel.SetActive(true);

        // Turn OFF apps
        if (storeAppPanel) storeAppPanel.SetActive(false);
        if (storeAppPanel2) storeAppPanel2.SetActive(false);
        if (furnitureShopPanel) furnitureShopPanel.SetActive(false);
        if (emailAppPanel) emailAppPanel.SetActive(false);
        if (programsAppPanel) programsAppPanel.SetActive(false);
        if (wallpaperAppPanel) wallpaperAppPanel.SetActive(false);

        // Turn OFF search
        if (searchPanel) searchPanel.SetActive(false);
    }

    public void CloseCurrentApp()
    {
        ShowDesktop();
    }

    public void OpenStoreApp() { ShowDesktop(); if (storeAppPanel) storeAppPanel.SetActive(true); }
    public void OpenFurnitureShop() { ShowDesktop(); if (furnitureShopPanel) furnitureShopPanel.SetActive(true); }
    public void OpenEmailApp() { ShowDesktop(); if (emailAppPanel) emailAppPanel.SetActive(true); }
    public void OpenProgramsApp() { ShowDesktop(); if (programsAppPanel) programsAppPanel.SetActive(true); }
    public void OpenWallpaperApp() { ShowDesktop(); if (wallpaperAppPanel) wallpaperAppPanel.SetActive(true); }

    // --- SEARCH LOGIC ---
    public void ToggleSearchPanel()
    {
        if (searchPanel == null) return;

        bool isOpening = !searchPanel.activeSelf;
        searchPanel.SetActive(isOpening);

        // If opening, clear the bar and select it so the player can type immediately
        if (isOpening && searchInput != null)
        {
            searchInput.text = "";
            searchInput.Select();
        }
    }

    public void FilterApps(string searchTerm)
    {
        string lowerSearchTerm = searchTerm.ToLower();

        foreach (GameObject icon in searchableIcons)
        {
            if (icon == null) continue;

            // Grab the text component on the icon to see what it's called
            TextMeshProUGUI label = icon.GetComponentInChildren<TextMeshProUGUI>();
            string appName = (label != null) ? label.text.ToLower() : icon.name.ToLower();

            // If search is empty OR the name matches, turn it on. Otherwise, turn it off.
            bool isMatch = string.IsNullOrEmpty(lowerSearchTerm) || appName.Contains(lowerSearchTerm);
            icon.SetActive(isMatch);
        }
    }

    // --- INPUT HANDLING ---
    public bool HandleEscapeInput()
    {
        // If the PC is off or in BIOS, maybe let them stand up immediately
        if (!isPoweredOn || (biosPanel != null && biosPanel.activeSelf)) return true;

        bool isAnyAppOpen =
            (storeAppPanel != null && storeAppPanel.activeSelf) ||
            (furnitureShopPanel != null && furnitureShopPanel.activeSelf) ||
            (emailAppPanel != null && emailAppPanel.activeSelf) ||
            (programsAppPanel != null && programsAppPanel.activeSelf) ||
            (wallpaperAppPanel != null && wallpaperAppPanel.activeSelf) ||
            (searchPanel != null && searchPanel.activeSelf); // Added Search Panel check!

        if (isAnyAppOpen)
        {
            ShowDesktop(); // Close apps/search, return to desktop
            return false;  // Do NOT stand up from chair yet
        }

        // If no apps are open, we are already at the desktop, so we can leave.
        return true;
    }
}
using UnityEngine;
using UnityEngine.UI;
using TMPro; // NEW: Required to talk to the Search Input Field!
using System.Collections.Generic;

[System.Serializable]
public class ProgramInfo
{
    public string name;            // Keep this for debugging/logic
    public Sprite appRowSprite;    // The single image with text/icon baked in
    public bool isInstalled;
    public GameObject desktopIcon; // The icon on your wallpaper
}

public class ProgramManager : MonoBehaviour
{
    [Header("UI Setup")]
    public Transform container;
    public GameObject programRowPrefab;
    public TMP_InputField searchInputField; // NEW: Slot for your search bar

    [Header("App Inventory")]
    public List<ProgramInfo> allPrograms = new List<ProgramInfo>();

    private enum ViewMode { Add, Remove }
    private ViewMode currentMode = ViewMode.Add;

    // NEW: Memory to track what we are currently typing
    private string currentSearchTerm = "";

    private void Start()
    {
        UpdateDesktopIcons();

        // Hook up the search bar to listen for our typing automatically!
        if (searchInputField != null)
        {
            searchInputField.onValueChanged.AddListener(UpdateSearchFilter);
        }

        ShowAddProgramsTab();
    }

    public void OpenProgramsApp()
    {
        ShowAddProgramsTab();
    }

    public void ShowAddProgramsTab()
    {
        currentMode = ViewMode.Add;
        ClearSearch(); // Wipes the search bar clean when swapping tabs
        RefreshProgramList();
    }

    public void ShowRemoveProgramsTab()
    {
        currentMode = ViewMode.Remove;
        ClearSearch(); // Wipes the search bar clean when swapping tabs
        RefreshProgramList();
    }

    // --- NEW: SEARCH LOGIC ---
    public void UpdateSearchFilter(string searchTerm)
    {
        currentSearchTerm = searchTerm.ToLower(); // Force lowercase for easy matching
        RefreshProgramList(); // Instantly update the list as we type!
    }

    private void ClearSearch()
    {
        currentSearchTerm = "";
        if (searchInputField != null)
        {
            // SetTextWithoutNotify clears the visual text without accidentally triggering a double-refresh
            searchInputField.SetTextWithoutNotify("");
        }
    }
    // -------------------------

    public void RefreshProgramList()
    {
        // 1. Clear the current list
        foreach (Transform child in container)
        {
            Destroy(child.gameObject);
        }

        // 2. Go through all programs and filter them
        foreach (ProgramInfo app in allPrograms)
        {
            // Tab Filters:
            if (currentMode == ViewMode.Add && app.isInstalled) continue;
            if (currentMode == ViewMode.Remove && !app.isInstalled) continue;

            // NEW Search Filter:
            // If the search bar isn't empty, AND the app's name doesn't contain what we typed, skip it!
            if (!string.IsNullOrEmpty(currentSearchTerm) && !app.name.ToLower().Contains(currentSearchTerm))
            {
                continue;
            }

            // 3. If it passes the filters, spawn it!
            GameObject newRow = Instantiate(programRowPrefab, container);
            ProgramEntry entry = newRow.GetComponent<ProgramEntry>();

            if (entry != null)
            {
                entry.Setup(app.appRowSprite);
                entry.actionButton.onClick.RemoveAllListeners();
                entry.actionButton.onClick.AddListener(() => ToggleInstallation(app));
            }
        }
    }

    public void ToggleInstallation(ProgramInfo app)
    {
        app.isInstalled = !app.isInstalled;
        UpdateDesktopIcons();
        RefreshProgramList();
    }

    public void UpdateDesktopIcons()
    {
        foreach (ProgramInfo app in allPrograms)
        {
            if (app.desktopIcon != null)
            {
                app.desktopIcon.SetActive(app.isInstalled);
            }
        }
    }
}
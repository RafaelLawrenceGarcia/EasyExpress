using UnityEngine;
using UnityEngine.UI;
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

    [Header("App Inventory")]
    public List<ProgramInfo> allPrograms = new List<ProgramInfo>();

    private void Start()
    {
        UpdateDesktopIcons();
        CreateProgramList();
    }

    public void CreateProgramList()
    {
        foreach (Transform child in container) Destroy(child.gameObject);

        foreach (ProgramInfo app in allPrograms)
        {
            GameObject newRow = Instantiate(programRowPrefab, container);
            ProgramEntry entry = newRow.GetComponent<ProgramEntry>();

            if (entry != null)
            {
                // Set the baked-in image
                entry.Setup(app.appRowSprite);

                // Add the logic to the button component
                entry.actionButton.onClick.AddListener(() => ToggleInstallation(app));
            }
        }
    }

    public void ToggleInstallation(ProgramInfo app)
    {
        app.isInstalled = !app.isInstalled;
        UpdateDesktopIcons();
        
        // We don't necessarily need to refresh the whole list 
        // unless you want the button to change color/look!
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
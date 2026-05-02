using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ProgramsApp : MonoBehaviour
{
    [Header("UI Rows")]
    // Drag the row objects (e.g. "Google Chrome Row", "Spotify Row") here
    public GameObject[] programRows; 

    // Connect this to the "Uninstall" buttons inside each row
    public void UninstallProgram(GameObject rowToHide)
    {
        // For now, we just hide the row to simulate uninstallation
        rowToHide.SetActive(false);
    }

    // Connect this to the "Install" buttons (if you have an install list)
    public void InstallProgram(GameObject rowToShow)
    {
        rowToShow.SetActive(true);
    }
}
using UnityEngine;

public class PCScreenController : MonoBehaviour
{
    [Header("Screen Canvas")]
    public GameObject screenCanvas;

    [Header("All Panels (in order)")]
    public GameObject[] panels; // Drag all your screen panels here

    private int lastPanelIndex = 0; // Remembers last open panel

    void Start()
    {
        // Screen starts CLOSED — do NOT open on startup
        screenCanvas.SetActive(false);
    }

    public void OpenScreen()
    {
        screenCanvas.SetActive(true);
        ShowPanel(lastPanelIndex); // Restore last panel
    }

    public void CloseScreen()
    {
        screenCanvas.SetActive(false);
    }

    // Call this whenever the player navigates to a new panel
    public void ShowPanel(int index)
    {
        for (int i = 0; i < panels.Length; i++)
            panels[i].SetActive(i == index);

        lastPanelIndex = index; // Save the last panel
    }
}
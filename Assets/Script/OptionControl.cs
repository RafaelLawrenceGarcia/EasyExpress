using UnityEngine;
using UnityEngine.UI;

public class OptionsController : MonoBehaviour
{
    [Header("Content Panels")]
    public GameObject[] allPanels; 

    // NO START FUNCTION HERE! We deleted it.
    // Now the script does NOTHING until you actually click a button.

    public void OpenTab(GameObject tabToOpen)
    {
        // 1. Turn OFF every panel in the list
        foreach (GameObject panel in allPanels)
        {
            panel.SetActive(false);
        }

        // 2. Turn ON only the one we clicked
        if (tabToOpen != null)
        {
            tabToOpen.SetActive(true);
        }
    }
}
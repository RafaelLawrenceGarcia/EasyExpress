using UnityEngine;
using UnityEngine.UI;
using TMPro;


/// </summary>
public class InteractionPromptUI : MonoBehaviour
{
    [Header("References")]
    public GameObject promptPanel;         // The main prompt container
    public TextMeshProUGUI keyText;       // "E" or "Q" 
    public TextMeshProUGUI actionText;    // "Talk to Customer", "Use Computer", etc.
    public Image keyBackground;           // The key icon background (for coloring)

    /// <summary>
    /// Show the prompt with a specific key and action description.
    /// </summary>
    public void Show(string key, string action)
    {
        if (promptPanel == null) return;

        if (keyText != null) keyText.text = key;
        if (actionText != null) actionText.text = action;

        promptPanel.SetActive(true);
    }

    /// <summary>
    /// Hide the prompt.
    /// </summary>
    public void Hide()
    {
        if (promptPanel != null) promptPanel.SetActive(false);
    }

    public bool IsVisible()
    {
        return promptPanel != null && promptPanel.activeSelf;
    }
}
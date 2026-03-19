using UnityEngine;
using UnityEngine.UI;

public class ProgramEntry : MonoBehaviour
{
    [Header("UI References")]
    public Image appIconImage;  // The picture of the app
    public Button actionButton; // The button the player clicks to install/uninstall

    // The ProgramManager calls this to pass the picture data to the prefab!
    public void Setup(Sprite icon)
    {
        if (appIconImage != null && icon != null)
        {
            appIconImage.sprite = icon;
        }
    }
}
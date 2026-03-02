using UnityEngine;
using UnityEngine.UI;

public class WallpaperManager : MonoBehaviour
{
    [Header("References")]
    public Image desktopBackground; // Drag your 'Computer_own' background Image here

    // This is the function each button will call
    public void ChangeWallpaper(Sprite newWallpaper)
    {
        if (desktopBackground != null)
        {
            desktopBackground.sprite = newWallpaper;
            Debug.Log("Wallpaper updated!");
        }
    }
}
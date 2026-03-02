using UnityEngine;
using UnityEngine.UI;

public class ProgramEntry : MonoBehaviour
{
    public Button actionButton; // The whole prefab image should probably have the Button component

    // We only need to tell the button what its job is (Install or Uninstall)
    public void Setup(Sprite appImage)
    {
        // This sets the visual for the prefab in the grid
        GetComponent<Image>().sprite = appImage;
    }
}
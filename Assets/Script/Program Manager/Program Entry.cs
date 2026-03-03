using UnityEngine;
using UnityEngine.UI;

public class ProgramEntry : MonoBehaviour
{
    public Button actionButton; 

    public void Setup(Sprite appImage)
    {
        // FIX: Instead of looking for an Image on itself, 
        // it grabs the Image from the button you already linked in the Inspector!
        if (actionButton != null)
        {
            actionButton.GetComponent<Image>().sprite = appImage;
        }
    }
}
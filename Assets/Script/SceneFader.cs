using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class SceneFader : MonoBehaviour
{
    public float fadeSpeed = 1.5f;
    private CanvasGroup canvasGroup;

    void Start()
    {
        // Add a CanvasGroup if you forgot to add one in Editor
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        // Start fully black
        canvasGroup.alpha = 1; 
        
        // Start fading out
        StartCoroutine(FadeOut());
    }

    IEnumerator FadeOut()
    {
        while (canvasGroup.alpha > 0)
        {
            canvasGroup.alpha -= Time.deltaTime * fadeSpeed;
            yield return null;
        }
        
        // Disable the object so it doesn't block mouse clicks
        gameObject.SetActive(false); 
    }
}
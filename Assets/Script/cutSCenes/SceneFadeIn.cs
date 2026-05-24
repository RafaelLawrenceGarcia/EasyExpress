using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class SceneFadeIn : MonoBehaviour
{
    [Tooltip("The black image overlay that covers the screen.")]
    public Image fadeOverlay;
    
    [Tooltip("How long to wait in pure black before revealing the menu.")]
    public float delayBeforeFade = 0.3f;

    [Tooltip("How long it takes to fade from black to clear (2.0 is very elegant).")]
    public float fadeDuration = 2.0f;

    void Start()
    {
        if (fadeOverlay != null)
        {
            // Ensure it starts completely black
            Color c = fadeOverlay.color;
            c.a = 1f;
            fadeOverlay.color = c;
            
            fadeOverlay.gameObject.SetActive(true);
            StartCoroutine(FadeIn());
        }
    }

    IEnumerator FadeIn()
    {
        // 1. The Cinematic Pause (Wait just a tiny bit before fading in)
        yield return new WaitForSeconds(delayBeforeFade);

        // 2. Smooth Fade In
        float elapsed = 0f;
        Color c = fadeOverlay.color;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            c.a = 1f - Mathf.Clamp01(elapsed / fadeDuration);
            fadeOverlay.color = c;
            yield return null;
        }

        // Clean up
        fadeOverlay.gameObject.SetActive(false);
    }
}
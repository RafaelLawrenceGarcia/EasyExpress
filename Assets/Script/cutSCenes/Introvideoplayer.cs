using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using TMPro; 

public class IntroVideoPlayer : MonoBehaviour
{
    [Header("Video Settings")]
    public VideoPlayer videoPlayer;
    public GameObject skipHintText;

    [Header("Scene Transition Settings")]
    public string loginSceneName = "MAINMENU"; 
    public float fadeDuration = 1.0f; 
    public float pauseInBlack = 0.3f; 
    
    [Tooltip("How many seconds the loading screen will stay on screen.")]
    public float loadingScreenDuration = 4.0f; // Now you can change this in Unity!
    
    [Tooltip("The pure black UI image that covers the whole screen.")]
    public Image fadeOverlay;         

    [Header("Loading Canvas (Optional)")]
    public GameObject loadingScreenPanel;
    public Slider progressBar;
    public TextMeshProUGUI progressText;

    private bool isTransitioning = false;

    void Start()
    {
        if (fadeOverlay != null)
        {
            Color c = fadeOverlay.color;
            c.a = 0f;
            fadeOverlay.color = c;
            fadeOverlay.gameObject.SetActive(false); 
        }

        if (loadingScreenPanel != null) loadingScreenPanel.SetActive(false);

        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached += EndVideo;
            videoPlayer.Play();
        }
    }

    void Update()
    {
        if (isTransitioning) return;

        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
        {
            EndVideo(videoPlayer);
        }
    }

    void EndVideo(VideoPlayer vp)
    {
        if (isTransitioning) return;
        isTransitioning = true;

        if (videoPlayer != null) videoPlayer.Stop();
        if (skipHintText != null) skipHintText.SetActive(false);
        
        StartCoroutine(FadeAndLoadSequence());
    }

    IEnumerator FadeAndLoadSequence()
    {
        // ---------------------------------------------------------
        // PHASE 1: Fade the Video to pure Black
        // ---------------------------------------------------------
        if (fadeOverlay != null)
        {
            fadeOverlay.gameObject.SetActive(true);
            float elapsed = 0f;
            Color c = fadeOverlay.color;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                c.a = Mathf.Clamp01(elapsed / fadeDuration);
                fadeOverlay.color = c;
                yield return null;
            }
        }

        yield return new WaitForSeconds(pauseInBlack);

        // ---------------------------------------------------------
        // PHASE 2: Turn on Loading UI, Fade Black away to reveal it!
        // ---------------------------------------------------------
        if (loadingScreenPanel != null)
        {
            loadingScreenPanel.SetActive(true);
            if (progressBar != null) progressBar.value = 0f;
            if (progressText != null) progressText.text = "0%";

            if (fadeOverlay != null)
            {
                float elapsed = 0f;
                Color c = fadeOverlay.color;
                while (elapsed < fadeDuration)
                {
                    elapsed += Time.deltaTime;
                    c.a = 1f - Mathf.Clamp01(elapsed / fadeDuration);
                    fadeOverlay.color = c;
                    yield return null;
                }
                fadeOverlay.gameObject.SetActive(false);
            }
        }

        // ---------------------------------------------------------
        // PHASE 3: Actually Load the Main Menu with Smooth Bar
        // ---------------------------------------------------------
        AsyncOperation operation = SceneManager.LoadSceneAsync(loginSceneName);
        operation.allowSceneActivation = false;

        float timer = 0f;

        while (!operation.isDone)
        {
            timer += Time.deltaTime;

            float loadProgress = Mathf.Clamp01(operation.progress / 0.9f);
            float timeProgress = Mathf.Clamp01(timer / loadingScreenDuration);
            float currentProgress = Mathf.Min(loadProgress, timeProgress);

            if (progressBar != null) progressBar.value = currentProgress;
            if (progressText != null) progressText.text = Mathf.RoundToInt(currentProgress * 100f) + "%"; 

            // When the timer is up AND the scene is ready...
            if (operation.progress >= 0.9f && timer >= loadingScreenDuration)
            {
                // Force it to exactly 100%
                if (progressBar != null) progressBar.value = 1f;
                if (progressText != null) progressText.text = "100%";
                
                // WAIT FOR HALF A SECOND SO THE PLAYER CAN ACTUALLY SEE IT HIT 100%
                yield return new WaitForSeconds(0.5f);

                // Jump to the Main Menu 
                operation.allowSceneActivation = true;
            }

            yield return null;
        }
    }
}
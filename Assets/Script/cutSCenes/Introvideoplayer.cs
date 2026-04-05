using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// INTRO VIDEO PLAYER
/// ─────────────────────────────────────────────────────────────────────
/// Place this on a GameObject in a dedicated "IntroVideo" scene.
///
/// SCENE SETUP:
///   1. Create a new Scene called "IntroVideo" and add it as Scene 0
///      in File > Build Settings (drag it to the top of the list).
///   2. In the scene, create:
///        • A Canvas (Screen Space - Overlay, Sort Order 10)
///            └─ RawImage (stretch to fill canvas) ← assign to videoDisplay
///            └─ Text/TMP "Press ESC to Skip"      ← assign to skipHintText
///        • An empty GameObject with a VideoPlayer component
///            └─ Assign your .mov file to "Video Clip"
///            └─ Set Render Mode to "Render Texture"
///            └─ Create a RenderTexture asset and assign it here AND
///               to the RawImage's Texture field
///   3. Assign the scene name that holds your Login/MainMenu to
///      mainMenuSceneName below.
/// ─────────────────────────────────────────────────────────────────────
/// </summary>
public class IntroVideoPlayer : MonoBehaviour
{
    [Header("Video")]
    [Tooltip("The VideoPlayer component that will play your .mov intro.")]
    public VideoPlayer videoPlayer;

    [Header("UI")]
    [Tooltip("RawImage used to display the video. Set its texture to the same RenderTexture assigned to the VideoPlayer.")]
    public RawImage videoDisplay;

    [Tooltip("Optional 'Press ESC to Skip' hint text shown on screen.")]
    public Text skipHintText;          // use TMP_Text if you prefer TextMeshPro

    [Header("Scene Transition")]
    [Tooltip("Name of the scene to load after the video (your MainMenu / Login scene).")]
    public string mainMenuSceneName = "MainMenu";

    [Tooltip("Fade-to-black duration in seconds before scene switch.")]
    public float fadeDuration = 0.5f;

    // ── private state ───────────────────────────────────────────────────
    private bool _skipping = false;

    // ── optional fade image (create a full-screen black Image sibling of videoDisplay) ──
    [Header("Fade (optional)")]
    [Tooltip("A full-screen black Image used for the fade-out. Leave empty to skip fade.")]
    public Image fadeOverlay;

    // ────────────────────────────────────────────────────────────────────
    void Start()
    {
        // Make sure fade overlay starts invisible
        if (fadeOverlay != null)
        {
            Color c = fadeOverlay.color;
            c.a = 0f;
            fadeOverlay.color = c;
        }

        if (videoPlayer == null)
        {
            Debug.LogError("[IntroVideoPlayer] No VideoPlayer assigned! Skipping to menu.");
            LoadMainMenu();
            return;
        }

        // Hook the finished event
        videoPlayer.loopPointReached += OnVideoFinished;

        videoPlayer.Play();
    }

    // ────────────────────────────────────────────────────────────────────
    void Update()
    {
        if (_skipping) return;

        // ESC or any key / touch skips the intro
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Space))
        {
            Skip();
        }
    }

    // ────────────────────────────────────────────────────────────────────
    void OnVideoFinished(VideoPlayer vp)
    {
        if (_skipping) return;
        StartCoroutine(FadeAndLoad());
    }

    public void Skip()
    {
        if (_skipping) return;
        _skipping = true;

        if (videoPlayer != null) videoPlayer.Stop();
        if (skipHintText != null) skipHintText.gameObject.SetActive(false);

        StartCoroutine(FadeAndLoad());
    }

    // ────────────────────────────────────────────────────────────────────
    IEnumerator FadeAndLoad()
    {
        // Optional fade to black
        if (fadeOverlay != null && fadeDuration > 0f)
        {
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

        LoadMainMenu();
    }

    void LoadMainMenu()
    {
        SceneManager.LoadScene(mainMenuSceneName);
    }
}
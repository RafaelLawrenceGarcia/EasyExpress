using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// PRE-TUTORIAL CUTSCENE PLAYER
/// ─────────────────────────────────────────────────────────────────────
/// Place this on a GameObject in your GAMEPLAY scene.
/// It plays a story .mov cutscene for NEW PLAYERS only,
/// BEFORE the tutorial dialogue starts.
///
/// REQUIRES: TutorialManager has been patched (see TutorialManager_Patch.cs)
///
/// SCENE SETUP (Gameplay scene):
///   1. Create a Canvas (Screen Space – Overlay, Sort Order 99)
///        └─ Name it "CutsceneCanvas" and assign to cutsceneCanvas
///        └─ Add a full-screen black Image (background)      ← cutsceneBackground
///        └─ Add a RawImage (stretch to fill)                ← videoDisplay
///        └─ Add a Text/TMP "Press ESC to Skip"              ← skipHintText
///        └─ Add a full-screen black Image for fade          ← fadeOverlay
///
///   2. Create a VideoPlayer GameObject (child of CutsceneCanvas or separate)
///        • Set Render Mode → Render Texture
///        • Create a RenderTexture and assign to both VideoPlayer and RawImage
///        • Assign your story .mov to "Video Clip"
///        • Uncheck "Play On Awake"
///        • Assign the VideoPlayer here → videoPlayer
///
///   3. The CutsceneCanvas should be INACTIVE in the Inspector.
///      This script activates it only when needed.
///
///   IMPORTANT: This script must be on a GameObject that is ALWAYS active
///   so it can check the new-player flag in Awake.
/// ─────────────────────────────────────────────────────────────────────
/// </summary>
public class PreTutorialCutscene : MonoBehaviour
{
    [Header("Video")]
    [Tooltip("The VideoPlayer component for your story cutscene .mov.")]
    public VideoPlayer videoPlayer;

    [Header("UI References")]
    [Tooltip("The Canvas that holds all cutscene UI. Should be inactive by default.")]
    public GameObject cutsceneCanvas;

    [Tooltip("The RawImage that displays the video. Its texture = same RenderTexture as the VideoPlayer.")]
    public RawImage videoDisplay;

    [Tooltip("Full-screen black background image (shows before video starts rendering).")]
    public Image cutsceneBackground;

    [Tooltip("'Press ESC to Skip' label.")]
    public Text skipHintText;   // swap for TMP_Text if needed

    [Tooltip("Full-screen black Image used for fade in/out.")]
    public Image fadeOverlay;

    [Header("Timing")]
    [Tooltip("Duration of fade-in from black at the start.")]
    public float fadeInDuration = 0.8f;

    [Tooltip("Duration of fade-out to black at the end.")]
    public float fadeOutDuration = 0.8f;

    // ── private state ────────────────────────────────────────────────────
    private bool _isNewPlayer = false;
    private bool _skipping    = false;
    private bool _cutsceneDone = false;

    // ────────────────────────────────────────────────────────────────────
   void Awake()
{
    bool tutorialDone = PlayerPrefs.GetInt("TutorialDone", 0) == 1;
    bool isLoading    = PlayerPrefs.GetInt("IsLoadingGame", 0) == 1;
    _isNewPlayer = !tutorialDone && !isLoading;

    if (_isNewPlayer)
    {
        if (DayTransitionManager.Instance != null)
            DayTransitionManager.Instance.skipIntroForCutscene = true;

        if (TutorialManager.Instance != null)
            TutorialManager.Instance.waitingForCutscene = true;

        // Force black screen from frame 1
        if (cutsceneCanvas != null)
        {
            cutsceneCanvas.SetActive(true);
            SetFadeAlpha(1f);
            if (videoDisplay != null) videoDisplay.gameObject.SetActive(false);
            if (skipHintText != null) skipHintText.gameObject.SetActive(false);
        }
    }
}

    // ────────────────────────────────────────────────────────────────────
    void Start()
{
    if (cutsceneCanvas != null) cutsceneCanvas.SetActive(false);

    if (_isNewPlayer)
    {
        if (DayTransitionManager.Instance != null)
            DayTransitionManager.Instance.skipIntroForCutscene = true;

        DayTransitionManager.OnNewDayStarted += OnFirstDayStarted;
    }
}

    // ────────────────────────────────────────────────────────────────────
    void OnDestroy()
    {
        DayTransitionManager.OnNewDayStarted -= OnFirstDayStarted;
        if (videoPlayer != null) videoPlayer.loopPointReached -= OnVideoFinished;
    }

    // ────────────────────────────────────────────────────────────────────
    void OnFirstDayStarted(int day)
    {
        if (day != 1) return;

        // Unsubscribe so this only fires once
        DayTransitionManager.OnNewDayStarted -= OnFirstDayStarted;

        StartCoroutine(PlayCutscene());
    }

    private float _mouseTimer = 0f;
    private float _mouseHideDelay = 3f;
    private Vector3 _lastMousePos;

    void Update()
    {
        if (!_isNewPlayer || _cutsceneDone || _skipping) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Skip();
        }

        // Show/hide skip hint based on mouse movement
        if (Input.mousePosition != _lastMousePos)
        {
            // Mouse moved — show the hint and reset timer
            _lastMousePos = Input.mousePosition;
            _mouseTimer = _mouseHideDelay;
            if (skipHintText != null) skipHintText.gameObject.SetActive(true);
        }
        else
        {
            // Mouse not moving — count down and hide
            if (_mouseTimer > 0f)
            {
                _mouseTimer -= Time.deltaTime;
                if (_mouseTimer <= 0f)
                {
                    if (skipHintText != null) skipHintText.gameObject.SetActive(false);
                }
            }
        }
    }

    // ────────────────────────────────────────────────────────────────────
    IEnumerator PlayCutscene()
    {
        GameObject dayTransCanvas = GameObject.Find("DayTransitionCanvas");
        if (dayTransCanvas != null) dayTransCanvas.SetActive(false);
        if (videoPlayer == null)
        {
            Debug.LogWarning("[PreTutorialCutscene] No VideoPlayer assigned! Skipping cutscene.");
            FinishCutscene();
            yield break;
        }

        // ── 1. Show canvas & prepare fade overlay ────────────────────────
        if (cutsceneCanvas != null) cutsceneCanvas.SetActive(true);
        if (videoDisplay != null)   videoDisplay.gameObject.SetActive(false); // hidden until video ready

        SetFadeAlpha(1f);   // start fully black

        // ── 2. Prepare and play video ─────────────────────────────────────
        videoPlayer.loopPointReached += OnVideoFinished;
        videoPlayer.Prepare();

        // Wait until video is prepared
        float prepTimeout = 5f;
        float prepElapsed = 0f;
        while (!videoPlayer.isPrepared && prepElapsed < prepTimeout)
        {
            prepElapsed += Time.deltaTime;
            yield return null;
        }

        videoPlayer.Play();

        if (videoDisplay != null) videoDisplay.gameObject.SetActive(true);

        // ── 3. Fade IN (black → video) ────────────────────────────────────
        yield return StartCoroutine(Fade(1f, 0f, fadeInDuration));

        // ── 4. Show skip hint ─────────────────────────────────────────────
        if (skipHintText != null) skipHintText.gameObject.SetActive(true);

        // Video plays... OnVideoFinished() handles the end.
        // Skip() handles ESC.
    }

    // ────────────────────────────────────────────────────────────────────
    void OnVideoFinished(VideoPlayer vp)
    {
        if (_skipping) return;
        StartCoroutine(EndCutscene());
    }

    public void Skip()
    {
        if (_skipping) return;
        _skipping = true;
        if (videoPlayer != null) videoPlayer.Stop();
        if (skipHintText != null) skipHintText.gameObject.SetActive(false);
        StartCoroutine(EndCutscene());
    }

    // ────────────────────────────────────────────────────────────────────
    IEnumerator EndCutscene()
    {
        // Fade out to black
        yield return StartCoroutine(Fade(0f, 1f, fadeOutDuration));

        FinishCutscene();
    }

    void FinishCutscene()
{
    _cutsceneDone = true;

    if (cutsceneCanvas != null) cutsceneCanvas.SetActive(false);

    // Re-show day transition canvas  ← ADD THIS
    GameObject dayCanvas = GameObject.Find("DayTransitionCanvas");
    if (dayCanvas != null) dayCanvas.SetActive(false);

    if (TutorialManager.Instance != null)
        TutorialManager.Instance.OnCutsceneComplete();
}

    // ────────────────────────────────────────────────────────────────────
    IEnumerator Fade(float from, float to, float duration)
    {
        if (fadeOverlay == null) yield break;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            SetFadeAlpha(Mathf.Lerp(from, to, elapsed / duration));
            yield return null;
        }
        SetFadeAlpha(to);
    }

    void SetFadeAlpha(float alpha)
    {
        if (fadeOverlay == null) return;
        Color c = fadeOverlay.color;
        c.a = alpha;
        fadeOverlay.color = c;
    }
}
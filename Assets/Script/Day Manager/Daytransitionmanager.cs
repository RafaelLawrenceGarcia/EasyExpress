using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class DayTransitionManager : MonoBehaviour
{
    public static DayTransitionManager Instance;

    [Header("UI References")]
    public Canvas transitionCanvas;
    public Image transitionImage;
    public TextMeshProUGUI dayText;
    public TextMeshProUGUI hudDayText;
    public bool skipIntroForCutscene = false;
    [Header("Timing")]
    public float fadeInDuration = 0.8f;
    public float holdDuration = 2.0f;
    public float fadeOutDuration = 1.2f;

    [Header("Player References")]
    public GTAMovement playerMovement;
    public OrbitCamera playerCamera;

    // --- STATE ---
    private int currentDay = 1;
    private bool isTransitioning = false;

    // Static: survives scene loads, resets when game restarts
    private static bool dayHasStarted = false;

    public static System.Action<int> OnNewDayStarted;

    void Awake()
    {
        Instance = this;

        if (PlayerPrefs.GetInt("IsLoadingGame", 0) == 1 || PlayerPrefs.GetInt("TutorialDone", 0) == 1)
        {
            currentDay = PlayerPrefs.GetInt("CurrentDay", 1);
        }
        else
        {
            currentDay = 1;
        }

        // If new player, skip Day 1 intro — cutscene plays instead
        bool tutorialDone = PlayerPrefs.GetInt("TutorialDone", 0) == 1;
        bool isLoading = PlayerPrefs.GetInt("IsLoadingGame", 0) == 1;
        if (!tutorialDone && !isLoading)
            skipIntroForCutscene = true;
    }

    void Start()
    {
        if (transitionCanvas != null) transitionCanvas.enabled = true;
        if (transitionImage != null) SetImageAlpha(1f);
        if (dayText != null) dayText.alpha = 0f;

        bool isChangingRooms = PlayerPrefs.GetInt("ChangingRooms", 0) == 1;

        // Always clear ChangingRooms so it doesn't carry over
        if (isChangingRooms)
        {
            PlayerPrefs.SetInt("ChangingRooms", 0);
            PlayerPrefs.Save();
        }

        if (isChangingRooms || dayHasStarted)
        {
            // ── ROOM CHANGE or RE-ENTRY ──
            // Load cloud data for jobs/boxes/deliveries, but skip gold/time override
            if (GameSession.IsLoggedIn && CloudDataHandler.Instance != null)
            {
                CloudDataHandler.IsRoomChangeLoad = true;
                CloudDataHandler.Instance.LoadGameData();
            }
            StartCoroutine(ResumeFromRoomChange());
        }
        else
        {
            // ── FIRST ENTRY: new game or continuing from main menu ──
            if (PlayerPrefs.GetInt("TutorialDone", 0) == 1
                && GameSession.IsLoggedIn
                && CloudDataHandler.Instance != null)
            {
                CloudDataHandler.Instance.LoadGameData();
            }

            if (skipIntroForCutscene)
            {
                if (transitionCanvas != null) transitionCanvas.enabled = true;
                if (transitionImage != null) SetImageAlpha(1f);
                if (dayText != null) dayText.alpha = 0f;
                FreezePlayer(true);
                isTransitioning = false;
                if (hudDayText != null) hudDayText.text = "Day " + currentDay;
                dayHasStarted = true;
                // Delay firing the event so the black screen has time to render first
                StartCoroutine(DelayedDayStart());
            }
            else
            {
                PlayDayIntro(null);
            }
        }
    }
    IEnumerator DelayedDayStart()
    {
        yield return new WaitForSecondsRealtime(1.5f);
        OnNewDayStarted?.Invoke(currentDay);
    }
    /// <summary>
    /// Called when re-entering a scene mid-day.
    /// Keeps screen black briefly so customers can walk to their spots,
    /// then fades out. Game time is preserved (doesn't tick during wait).
    /// </summary>
    IEnumerator ResumeFromRoomChange()
    {
        isTransitioning = true;
        FreezePlayer(true);

        // Keep screen black
        if (transitionCanvas != null) transitionCanvas.enabled = true;
        if (transitionImage != null) SetImageAlpha(1f);
        if (dayText != null) dayText.alpha = 0f;
        if (hudDayText != null) hudDayText.text = "Day " + currentDay;

        // Save the current game time so the clock doesn't advance during the wait
        DayTimeUI clock = FindFirstObjectByType<DayTimeUI>();
        float savedTime = (clock != null) ? clock.GetCurrentTime() : -1f;

        // Wait 2 seconds (real time) for customers/boxes to settle into position
        yield return new WaitForSecondsRealtime(2f);

        // Restore exact game time (undo any ticking that happened during the wait)
        if (clock != null && savedTime >= 0f)
            clock.SetTime(savedTime);

        // Fade out the black screen
        yield return StartCoroutine(FadePanel(1f, 0f, fadeOutDuration));

        if (transitionCanvas != null) transitionCanvas.enabled = false;

        FreezePlayer(false);
        isTransitioning = false;

        Debug.Log("[DayTransition] Resumed from room change — Day " + currentDay);
    }

    /// <summary>
    /// Call this when returning to the main menu so the next
    /// game start plays the day intro fresh.
    /// </summary>
    public static void ResetDayFlag()
    {
        dayHasStarted = false;
    }

    public void PlayDayIntro(System.Action onComplete)
    {
        StartCoroutine(DayIntroSequence(onComplete));
    }

    public void EndDay(System.Action onNewDayReady = null)
    {
        if (isTransitioning) return;
        StartCoroutine(EndDaySequence(onNewDayReady));
    }

    public int GetCurrentDay() { return currentDay; }
    public bool IsTransitioning() { return isTransitioning; }

    /// <summary>
    /// Re-reads CurrentDay from PlayerPrefs and updates the HUD.
    /// Called by CloudDataHandler after cloud data loads or resets.
    /// </summary>
    public void SyncCurrentDay()
    {
        currentDay = PlayerPrefs.GetInt("CurrentDay", 1);
        if (hudDayText != null) hudDayText.text = "Day " + currentDay;
        Debug.Log("[DayTransition] Synced to Day " + currentDay);
    }

    public void SkipDayIntro()
    {
        if (transitionCanvas != null) transitionCanvas.enabled = false;
        if (transitionImage != null) SetImageAlpha(0f);
        if (dayText != null) dayText.alpha = 0f;
        if (hudDayText != null) hudDayText.text = "Day " + currentDay;

        FreezePlayer(false);
        isTransitioning = false;

        OnNewDayStarted?.Invoke(currentDay);
    }

    // =============================================
    //  INTRO SEQUENCE
    // =============================================
    IEnumerator DayIntroSequence(System.Action onComplete)
    {
        isTransitioning = true;
        FreezePlayer(true);

        if (dayText != null)
        {
            dayText.text = "Day " + currentDay;
            dayText.alpha = 0f;
        }

        yield return new WaitForSecondsRealtime(0.5f);
        yield return StartCoroutine(FadeText(0f, 1f, 0.6f));
        yield return new WaitForSecondsRealtime(holdDuration);
        yield return StartCoroutine(FadeText(1f, 0f, 0.4f));

        yield return StartCoroutine(FadePanel(1f, 0f, fadeOutDuration));

        if (transitionCanvas != null) transitionCanvas.enabled = false;

        FreezePlayer(false);
        isTransitioning = false;
        if (hudDayText != null) hudDayText.text = "Day " + currentDay;

        // Mark day as started so re-entering any scene skips the intro
        dayHasStarted = true;

        OnNewDayStarted?.Invoke(currentDay);
        onComplete?.Invoke();
    }

    // =============================================
    //  END DAY SEQUENCE
    // =============================================
    IEnumerator EndDaySequence(System.Action onNewDayReady)
    {
        isTransitioning = true;
        FreezePlayer(true);

        if (transitionCanvas != null) transitionCanvas.enabled = true;
        if (transitionImage != null) SetImageAlpha(0f);
        if (dayText != null) dayText.alpha = 0f;

        yield return StartCoroutine(FadePanel(0f, 1f, fadeInDuration));

        WalkInLimiter.ResetDaily();
        CustomerRetainer.Clear();

        // Reset so next day's intro will play
        dayHasStarted = false;

        currentDay++;
        PlayerPrefs.SetInt("CurrentDay", currentDay);
        PlayerPrefs.Save();
        // ── Demo lock check ──
        if (DemoLockManager.Instance != null && DemoLockManager.Instance.CheckDemoLock(currentDay))
        {
            Debug.Log("[DayTransition] Demo locked at Day " + currentDay);
            if (transitionCanvas != null) transitionCanvas.enabled = false;
            isTransitioning = false;
            yield break;
        }

        // ── CHECKPOINT SAVE — before deliveries tick ──
        if (GameSession.IsLoggedIn && CloudDataHandler.Instance != null)
        {
            CloudDataHandler.Instance.SaveGameData();
            Debug.Log("[DayTransition] Checkpoint saved for Day " + currentDay);
        }

        yield return new WaitForSecondsRealtime(0.5f);

        if (dayText != null) dayText.text = "Day " + currentDay;
        yield return StartCoroutine(FadeText(0f, 1f, 0.6f));

        yield return new WaitForSecondsRealtime(holdDuration);

        yield return StartCoroutine(FadeText(1f, 0f, 0.4f));

        onNewDayReady?.Invoke();

        yield return new WaitForSecondsRealtime(0.3f);

        yield return StartCoroutine(FadePanel(1f, 0f, fadeOutDuration));

        if (transitionCanvas != null) transitionCanvas.enabled = false;

        FreezePlayer(false);
        isTransitioning = false;
        if (hudDayText != null) hudDayText.text = "Day " + currentDay;

        // Mark new day as started
        dayHasStarted = true;

        OnNewDayStarted?.Invoke(currentDay);   // deliveries tick, emails generate, boxes spawn

        // Reset clock to 6:00 AM for the new day
        DayTimeUI clock = FindFirstObjectByType<DayTimeUI>();
        if (clock != null) clock.ResetTimeForNewDay();
    }

    // =============================================
    //  UTILITY
    // =============================================
    IEnumerator FadePanel(float from, float to, float duration)
    {
        if (transitionImage == null) yield break;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            SetImageAlpha(Mathf.Lerp(from, to, t));
            yield return null;
        }
        SetImageAlpha(to);
    }

    void SetImageAlpha(float alpha)
    {
        if (transitionImage != null)
        {
            Color c = transitionImage.color;
            c.a = alpha;
            transitionImage.color = c;
        }
    }

    IEnumerator FadeText(float from, float to, float duration)
    {
        if (dayText == null) yield break;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            dayText.alpha = Mathf.Lerp(from, to, t);
            yield return null;
        }
        dayText.alpha = to;
    }

    void FreezePlayer(bool freeze)
    {
        if (playerMovement != null) playerMovement.SetMovementState(!freeze);
        if (playerCamera != null) playerCamera.SetCameraState(!freeze);

        if (freeze)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (transitionCanvas != null && !skipIntroForCutscene)
                transitionCanvas.enabled = false;
        }
    }
}
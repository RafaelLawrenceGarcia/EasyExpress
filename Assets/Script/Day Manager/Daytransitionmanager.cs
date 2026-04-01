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
    }

    void Start()
    {
        if (transitionCanvas != null) transitionCanvas.enabled = true;
        if (transitionImage != null) SetImageAlpha(1f);
        if (dayText != null) dayText.alpha = 0f;

        bool isChangingRooms = PlayerPrefs.GetInt("ChangingRooms", 0) == 1;

        // Auto-load cloud data if we have a saved day (not first time)
        // Only call PlayFab if actually logged in
        if (PlayerPrefs.GetInt("TutorialDone", 0) == 1
            && GameSession.IsLoggedIn
            && CloudDataHandler.Instance != null)
        {
            CloudDataHandler.Instance.LoadGameData();
        }

        if (isChangingRooms)
        {
            PlayerPrefs.SetInt("ChangingRooms", 0);
            PlayerPrefs.Save();
            SkipDayIntro();
        }
        else
        {
            PlayDayIntro(null);
        }
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

        // Fade to black
        yield return StartCoroutine(FadePanel(0f, 1f, fadeInDuration));

        // Reset daily walk-in counter
        WalkInLimiter.ResetDaily();

        // Advance to next day
        currentDay++;
        PlayerPrefs.SetInt("CurrentDay", currentDay);
        PlayerPrefs.Save();

        // =============================================
        //  SAVE TO CLOUD AFTER ADVANCING THE DAY
        //  (so the cloud stores the NEW day number)
        // =============================================
        if (GameSession.IsLoggedIn && CloudDataHandler.Instance != null)
        {
            CloudDataHandler.Instance.SaveGameData();
            Debug.Log("[DayTransition] Cloud save triggered for Day " + currentDay);
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
        OnNewDayStarted?.Invoke(currentDay);
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

            if (transitionCanvas != null) transitionCanvas.enabled = false;
        }
    }
}
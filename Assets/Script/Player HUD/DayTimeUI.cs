using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DayTimeUI : MonoBehaviour
{
    [Header("UI References")]
    public Image timeIcon;
    public Sprite sunSprite;
    public Sprite moonSprite;
    public TextMeshProUGUI dayText;
    public TextMeshProUGUI timeText;

    [Header("Time Settings")]
    public float dayLengthInMinutes = 30f;

    [Header("Day Boundaries")]
    public float dayStartHour = 6f;    // 6:00 AM
    public float dayEndHour = 19f;   // 7:00 PM

    private float currentTime;
    private int currentDay = 1;
    private bool endDayTriggered = false;

    public float GetCurrentTime() { return currentTime; }

    public void SetTime(float time)
    {
        currentTime = time;
    }

    void Start()
    {
        currentDay = PlayerPrefs.GetInt("CurrentDay", 1);

        if (PlayerPrefs.HasKey("SavedGameTime"))
        {
            currentTime = PlayerPrefs.GetFloat("SavedGameTime");
        }
        else
        {
            currentTime = dayStartHour;
        }

        endDayTriggered = false;
    }

    void OnEnable()
    {
        DayTransitionManager.OnNewDayStarted += SetDay;
        endDayTriggered = false;
    }
    void OnDisable() { DayTransitionManager.OnNewDayStarted -= SetDay; }

    void SetDay(int newDay)
    {
        currentDay = newDay;
        endDayTriggered = false;
    }

    public void ResetTimeForNewDay()
    {
        currentTime = dayStartHour;
        endDayTriggered = false;
        PlayerPrefs.SetFloat("SavedGameTime", currentTime);
        PlayerPrefs.Save();
    }

    void Update()
    {
        // ── HIDE DURING TUTORIAL ──────────────────────────────
        bool tutorialActive = TutorialManager.Instance != null
                              && TutorialManager.Instance.IsTutorialActive();

        if (timeIcon != null) timeIcon.gameObject.SetActive(!tutorialActive);
        if (dayText != null) dayText.gameObject.SetActive(!tutorialActive);
        if (timeText != null) timeText.gameObject.SetActive(!tutorialActive);

        if (tutorialActive) return;
        // ─────────────────────────────────────────────────────

        // Playable window = dayEndHour - dayStartHour (13 hours: 6AM→7PM)
        // Spread across dayLengthInMinutes real minutes
        float playableHours = dayEndHour - dayStartHour;
        float hoursPerSecond = playableHours / (dayLengthInMinutes * 60f);
        currentTime += hoursPerSecond * Time.deltaTime;

        // ── AUTO END DAY AT 7 PM ──
        if (currentTime >= dayEndHour && !endDayTriggered)
        {
            endDayTriggered = true;
            currentTime = dayEndHour; // clamp — don't overshoot

            if (DayTransitionManager.Instance != null
                && !DayTransitionManager.Instance.IsTransitioning())
            {
                Debug.Log("[DayTimeUI] 7:00 PM reached — ending day automatically.");
                DayTransitionManager.Instance.EndDay();
            }
        }

        bool isDaytime = currentTime >= 6f && currentTime < 18f;
        if (timeIcon != null)
            timeIcon.sprite = isDaytime ? sunSprite : moonSprite;

        int hours = Mathf.FloorToInt(currentTime);
        int minutes = Mathf.FloorToInt((currentTime - hours) * 60f);
        string ampm = hours >= 12 ? "PM" : "AM";
        int displayHour = hours % 12;
        if (displayHour == 0) displayHour = 12;

        if (timeText != null) timeText.text = $"{displayHour}:{minutes:D2} {ampm}";
        if (dayText != null) dayText.text = $"Day {currentDay}";
    }
}
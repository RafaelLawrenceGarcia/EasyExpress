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
    public float dayLengthInMinutes = 10f;

    private float currentTime;
    private int currentDay = 1;

    public float GetCurrentTime() { return currentTime; }

    /// <summary>
    /// Called by CloudDataHandler to restore the saved game time directly,
    /// in case Start() already ran and defaulted to 6:00 AM.
    /// </summary>
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
            // Don't delete the key — it needs to survive for room changes.
            // SceneDoor overwrites it on each scene transition anyway.
        }
        else
        {
            currentTime = 6f;
        }
    }

    void OnEnable() { DayTransitionManager.OnNewDayStarted += SetDay; }
    void OnDisable() { DayTransitionManager.OnNewDayStarted -= SetDay; }

    /// <summary>
    /// Called when OnNewDayStarted fires. Only updates the day number.
    /// Time is NOT reset here — EndDaySequence handles that via ResetTimeForNewDay().
    /// This prevents the saved time from being overwritten on first load.
    /// </summary>
    void SetDay(int newDay)
    {
        currentDay = newDay;
        // Time is NOT reset to 6 AM here.
        // For new days, DayTransitionManager calls ResetTimeForNewDay() explicitly.
    }

    /// <summary>
    /// Called by DayTransitionManager.EndDaySequence to reset clock to 6:00 AM
    /// at the start of a genuinely new day.
    /// </summary>
    public void ResetTimeForNewDay()
    {
        currentTime = 6f;
        // Also update PlayerPrefs so room changes get the correct time
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

        if (tutorialActive) return; // Don't update time during tutorial
        // ─────────────────────────────────────────────────────

        float hoursPerSecond = 24f / (dayLengthInMinutes * 60f);
        currentTime += hoursPerSecond * Time.deltaTime;

        if (currentTime >= 24f)
        {
            currentTime -= 24f;
            currentDay++;
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
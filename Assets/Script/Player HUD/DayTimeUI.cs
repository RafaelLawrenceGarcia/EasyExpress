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

    void Start()
    {
        currentDay = PlayerPrefs.GetInt("CurrentDay", 1);

        if (PlayerPrefs.HasKey("SavedGameTime"))
        {
            currentTime = PlayerPrefs.GetFloat("SavedGameTime");
            PlayerPrefs.DeleteKey("SavedGameTime");
            PlayerPrefs.Save();
        }
        else
        {
            currentTime = 6f;
        }
    }

    void OnEnable()  { DayTransitionManager.OnNewDayStarted += SetDay; }
    void OnDisable() { DayTransitionManager.OnNewDayStarted -= SetDay; }

    void SetDay(int newDay)
    {
        currentDay = newDay;
        currentTime = 6f;
    }

    void Update()
    {
        // ── HIDE DURING TUTORIAL ──────────────────────────────
        bool tutorialActive = TutorialManager.Instance != null
                              && TutorialManager.Instance.IsTutorialActive();

        if (timeIcon != null) timeIcon.gameObject.SetActive(!tutorialActive);
        if (dayText  != null) dayText.gameObject.SetActive(!tutorialActive);
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
        if (dayText  != null) dayText.text  = $"Day {currentDay}";
    }
}
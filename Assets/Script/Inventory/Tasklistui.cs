using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// TaskListUI — Professional task panel with animated completion.
///
/// NEW: MONITOR MODE
///   When the player opens a workstation monitor, the task panel
///   repositions to the RIGHT side of the screen and its canvas
///   sort order is boosted above the monitor overlay (sort 50).
///   Press [T] to toggle visibility while in the monitor.
///
///   TutorialManager calls EnterMonitorMode() / ExitMonitorMode()
///   automatically from NotifyMonitorOpenedForTutorial / NotifyMonitorClosed.
/// </summary>
public class TaskListUI : MonoBehaviour
{
    public static TaskListUI Instance;

    [Header("Panel")]
    public GameObject taskPanel;
    public CanvasGroup taskCanvasGroup;

    [Header("Toggle Button")]
    public Button toggleButton;
    public TextMeshProUGUI toggleButtonLabel;

    [Header("Header")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI counterText;
    public Image dotImage;

    [Header("Task Container")]
    public Transform taskContainer;
    public GameObject taskRowPrefab;

    [Header("Colors")]
    public Color pendingCircleColor = new Color(1f, 1f, 1f, 0.2f);
    public Color completedCircleColor = new Color(0.365f, 0.792f, 0.647f, 1f);
    public Color completedCircleBG = new Color(0.365f, 0.792f, 0.647f, 0.15f);
    public Color pendingTextColor = new Color(1f, 1f, 1f, 0.85f);
    public Color completedTextColor = new Color(1f, 1f, 1f, 0.3f);
    public Color flashColor = new Color(0.365f, 0.792f, 0.647f, 1f);

    [Header("Monitor Mode Settings")]
    [Tooltip("Key to toggle task panel while monitor is open.")]
    public KeyCode monitorToggleKey = KeyCode.T;

    [Tooltip("Sort order for the task canvas when in monitor mode.\n" +
             "Must be higher than the monitor overlay (50).")]
    public int monitorSortOrder = 120;

    private List<TaskRowData> currentTasks = new List<TaskRowData>();
    private bool isVisible = false;
    private bool isTemporarilyHidden = false;
    private bool userCollapsed = false;

    // ── Monitor mode state ───────────────────────────────────
    private bool isInMonitorMode = false;
    private Canvas _taskCanvas;
    private int _originalSortOrder;
    private Vector2 _savedAnchorMin;
    private Vector2 _savedAnchorMax;
    private Vector2 _savedPivot;
    private Vector2 _savedAnchoredPos;
    private Vector2 _savedSizeDelta;

    void Awake() { Instance = this; }

    void Start()
    {
        if (taskPanel != null) taskPanel.SetActive(false);
        if (taskCanvasGroup != null) taskCanvasGroup.alpha = 0f;
        if (toggleButton != null) toggleButton.gameObject.SetActive(false);
        UpdateToggleLabel();
    }

    void Update()
    {
        // ── Monitor mode toggle key ──────────────────────────────
        if (isInMonitorMode && isVisible && Input.GetKeyDown(monitorToggleKey))
        {
            TogglePanel();
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  MONITOR MODE — show task panel over the monitor overlay
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Called when the player opens a workstation monitor.
    /// Repositions the task panel to the right side and boosts
    /// the canvas sort order above the monitor (50).
    /// </summary>
    public void EnterMonitorMode()
    {
        if (isInMonitorMode) return;
        isInMonitorMode = true;

        // ── Find or cache the canvas ─────────────────────────────
        if (_taskCanvas == null && taskPanel != null)
            _taskCanvas = taskPanel.GetComponentInParent<Canvas>();

        if (_taskCanvas == null) return;

        // ── Save original canvas state ───────────────────────────
        _originalSortOrder = _taskCanvas.sortingOrder;

        // ── Boost sort order above the monitor overlay ───────────
        _taskCanvas.sortingOrder = monitorSortOrder;

        // ── Save original panel position ─────────────────────────
        RectTransform rt = taskPanel.GetComponent<RectTransform>();
        if (rt != null)
        {
            _savedAnchorMin = rt.anchorMin;
            _savedAnchorMax = rt.anchorMax;
            _savedPivot = rt.pivot;
            _savedAnchoredPos = rt.anchoredPosition;
            _savedSizeDelta = rt.sizeDelta;

            // ── Reposition to right side of screen ───────────────
            rt.anchorMin = new Vector2(1, 0.3f);
            rt.anchorMax = new Vector2(1, 0.85f);
            rt.pivot = new Vector2(1, 0.5f);
            rt.anchoredPosition = new Vector2(-20, 0);
            // Keep existing width, auto height from anchors
            rt.sizeDelta = new Vector2(_savedSizeDelta.x > 0 ? _savedSizeDelta.x : 280, 0);
        }

        // ── Restore visibility ───────────────────────────────────
        isTemporarilyHidden = false;
        userCollapsed = false;

        if (isVisible)
        {
            if (taskPanel != null) taskPanel.SetActive(true);
            if (taskCanvasGroup != null)
                StartCoroutine(FadeCanvasGroup(taskCanvasGroup.alpha, 1f, 0.3f));
        }

        // Show toggle button with monitor key hint
        if (toggleButton != null) toggleButton.gameObject.SetActive(true);
        UpdateToggleLabel();

        Debug.Log("[TaskListUI] Entered monitor mode — panel on right, sort order " + monitorSortOrder);
    }

    /// <summary>
    /// Called when the player closes the workstation monitor.
    /// Restores the task panel to its original position and sort order.
    /// </summary>
    public void ExitMonitorMode()
    {
        if (!isInMonitorMode) return;
        isInMonitorMode = false;

        // ── Restore canvas sort order ────────────────────────────
        if (_taskCanvas != null)
            _taskCanvas.sortingOrder = _originalSortOrder;

        // ── Restore panel position ───────────────────────────────
        RectTransform rt = taskPanel != null ? taskPanel.GetComponent<RectTransform>() : null;
        if (rt != null)
        {
            rt.anchorMin = _savedAnchorMin;
            rt.anchorMax = _savedAnchorMax;
            rt.pivot = _savedPivot;
            rt.anchoredPosition = _savedAnchoredPos;
            rt.sizeDelta = _savedSizeDelta;
        }

        // ── Restore collapsed state ──────────────────────────────
        userCollapsed = false;

        if (isVisible)
        {
            if (taskPanel != null) taskPanel.SetActive(true);
            if (taskCanvasGroup != null)
                StartCoroutine(FadeCanvasGroup(taskCanvasGroup.alpha, 1f, 0.3f));
        }

        UpdateToggleLabel();
        Debug.Log("[TaskListUI] Exited monitor mode — restored original position.");
    }

    /// <summary>Is the task panel currently in monitor mode?</summary>
    public bool IsInMonitorMode() => isInMonitorMode;

    // ═══════════════════════════════════════════════════════════
    //  PUBLIC API — Show / Hide
    // ═══════════════════════════════════════════════════════════

    public void SetTitle(string title)
    {
        if (titleText != null) titleText.text = title;
    }

    public void SetTasks(string[] taskDescriptions)
    {
        ClearTasks();
        foreach (string desc in taskDescriptions)
            SpawnTaskRow(desc);
        UpdateCounter();
        ShowPanel();
    }

    public void ShowPanel()
    {
        isVisible = true;
        isTemporarilyHidden = false;

        if (!userCollapsed)
        {
            if (taskPanel != null) taskPanel.SetActive(true);
            if (taskCanvasGroup != null)
                StartCoroutine(FadeCanvasGroup(taskCanvasGroup.alpha, 1f, 0.4f));
        }

        UpdateToggleLabel();
    }

    public void HidePanel()
    {
        isVisible = false;
        isTemporarilyHidden = false;
        userCollapsed = false;

        if (toggleButton != null) toggleButton.gameObject.SetActive(false);

        if (taskCanvasGroup != null)
            StartCoroutine(FadeAndDisable(0.3f));
        else if (taskPanel != null)
            taskPanel.SetActive(false);

        UpdateToggleLabel();
    }

    /// <summary>
    /// Called by the toggle button OR the monitor toggle key.
    /// Lets the player show/hide the task panel manually.
    /// </summary>
    public void TogglePanel()
    {
        if (!isVisible) return;

        userCollapsed = !userCollapsed;

        if (userCollapsed)
        {
            if (taskCanvasGroup != null)
                StartCoroutine(FadeAndDisablePanel(0.2f));
            else if (taskPanel != null)
                taskPanel.SetActive(false);
        }
        else
        {
            if (taskPanel != null) taskPanel.SetActive(true);
            if (taskCanvasGroup != null)
                StartCoroutine(FadeCanvasGroup(0f, 1f, 0.2f));
        }

        UpdateToggleLabel();
    }

    public void HideTemporarily()
    {
        // ── In monitor mode, DON'T hide — the panel stays visible ──
        if (isInMonitorMode) return;

        if (!isVisible) return;
        isTemporarilyHidden = true;

        if (taskCanvasGroup != null)
            StartCoroutine(FadeCanvasGroup(taskCanvasGroup.alpha, 0f, 0.2f));
        else if (taskPanel != null)
            taskPanel.SetActive(false);

        if (toggleButton != null) toggleButton.gameObject.SetActive(false);
    }

    public void RestoreIfNeeded()
    {
        // ── In monitor mode, panel is already visible — skip ──
        if (isInMonitorMode) return;

        if (!isVisible || !isTemporarilyHidden) return;
        isTemporarilyHidden = false;

        if (!userCollapsed)
        {
            if (taskPanel != null) taskPanel.SetActive(true);
            if (taskCanvasGroup != null)
                StartCoroutine(FadeCanvasGroup(0f, 1f, 0.3f));
        }

        UpdateToggleLabel();
    }

    // ── Toggle label ─────────────────────────────────────────────

    void UpdateToggleLabel()
    {
        if (toggleButtonLabel == null) return;

        if (isInMonitorMode)
        {
            // Show the toggle key hint in monitor mode
            string keyName = monitorToggleKey.ToString();
            toggleButtonLabel.text = userCollapsed
                ? $"[{keyName}] Show Tasks"
                : $"[{keyName}] Hide Tasks";
        }
        else
        {
            toggleButtonLabel.text = userCollapsed ? "Show" : "Hide";
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  PUBLIC API — Complete Tasks
    // ═══════════════════════════════════════════════════════════

    public void CompleteTask(int index)
    {
        if (index < 0 || index >= currentTasks.Count) return;
        if (currentTasks[index].isCompleted) return;
        currentTasks[index].isCompleted = true;
        StartCoroutine(AnimateCompletion(currentTasks[index]));
        UpdateCounter();
    }

    public void CompleteTaskByName(string description)
    {
        for (int i = 0; i < currentTasks.Count; i++)
        {
            if (!currentTasks[i].isCompleted && currentTasks[i].description == description)
            { CompleteTask(i); return; }
        }
    }

    public bool AreAllTasksComplete()
    {
        foreach (var t in currentTasks)
            if (!t.isCompleted) return false;
        return currentTasks.Count > 0;
    }

    public int GetCompletedCount()
    {
        int count = 0;
        foreach (var t in currentTasks) if (t.isCompleted) count++;
        return count;
    }

    public void UpdateTaskText(int index, string newText)
    {
        if (index < 0 || index >= currentTasks.Count) return;
        if (currentTasks[index].isCompleted) return;
        currentTasks[index].description = newText;
        if (currentTasks[index].textComponent != null)
            currentTasks[index].textComponent.text = newText;
    }

    // ═══════════════════════════════════════════════════════════
    //  INTERNAL — Spawning
    // ═══════════════════════════════════════════════════════════

    void SpawnTaskRow(string description)
    {
        if (taskRowPrefab == null || taskContainer == null) return;

        GameObject row = Instantiate(taskRowPrefab, taskContainer);

        TextMeshProUGUI text = null;
        Image circle = null;
        GameObject checkmark = null;
        CanvasGroup rowCG = row.GetComponent<CanvasGroup>();

        Transform circleTransform = row.transform.Find("Circle");
        if (circleTransform != null)
        {
            circle = circleTransform.GetComponent<Image>();
            Transform checkTransform = circleTransform.Find("Checkmark");
            if (checkTransform != null) checkmark = checkTransform.gameObject;
        }

        Transform textTransform = row.transform.Find("TaskText");
        if (textTransform != null) text = textTransform.GetComponent<TextMeshProUGUI>();

        if (text != null) text.text = description;
        if (checkmark != null) checkmark.SetActive(false);
        if (circle != null)
        {
            Outline outline = circle.GetComponent<Outline>();
            if (outline != null) outline.effectColor = pendingCircleColor;
        }

        currentTasks.Add(new TaskRowData
        {
            description = description,
            rowObject = row,
            textComponent = text,
            circleImage = circle,
            checkmarkObject = checkmark,
            rowCanvasGroup = rowCG,
            isCompleted = false
        });
    }

    void ClearTasks()
    {
        StopAllCoroutines();
        foreach (var t in currentTasks)
            if (t.rowObject != null) Destroy(t.rowObject);
        currentTasks.Clear();
    }

    void UpdateCounter()
    {
        if (counterText == null) return;
        counterText.text = GetCompletedCount() + "/" + currentTasks.Count;
    }

    // ═══════════════════════════════════════════════════════════
    //  ANIMATION
    // ═══════════════════════════════════════════════════════════

    IEnumerator AnimateCompletion(TaskRowData task)
    {
        if (task.circleImage == null || task.textComponent == null) yield break;

        Outline outline = task.circleImage.GetComponent<Outline>();
        if (outline != null) outline.effectColor = completedCircleColor;
        task.circleImage.color = completedCircleBG;

        float elapsed = 0f, duration = 0.3f;
        Transform circleT = task.circleImage.transform;
        Vector3 origScale = circleT.localScale;

        if (task.checkmarkObject != null) task.checkmarkObject.SetActive(true);
        task.textComponent.color = flashColor;

        while (elapsed < duration)
        {
            if (circleT == null) yield break;
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            circleT.localScale = origScale * (1f + Mathf.Sin(t * Mathf.PI) * 0.35f);
            yield return null;
        }
        if (circleT != null) circleT.localScale = origScale;

        yield return new WaitForSeconds(0.3f);

        elapsed = 0f; duration = 0.4f;
        Color startColor = flashColor, endColor = completedTextColor;

        while (elapsed < duration)
        {
            if (task.textComponent == null) yield break;
            elapsed += Time.deltaTime;
            task.textComponent.color = Color.Lerp(startColor, endColor,
                                                   Mathf.SmoothStep(0f, 1f, elapsed / duration));
            yield return null;
        }

        if (task.textComponent != null)
        {
            task.textComponent.color = completedTextColor;
            task.textComponent.fontStyle = FontStyles.Strikethrough;
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  UTILITY
    // ═══════════════════════════════════════════════════════════

    IEnumerator FadeCanvasGroup(float from, float to, float duration)
    {
        if (taskCanvasGroup == null) yield break;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            taskCanvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        taskCanvasGroup.alpha = to;
    }

    IEnumerator FadeAndDisable(float duration)
    {
        float start = taskCanvasGroup != null ? taskCanvasGroup.alpha : 1f;
        yield return StartCoroutine(FadeCanvasGroup(start, 0f, duration));
        if (taskPanel != null) taskPanel.SetActive(false);
    }

    /// <summary>Called by InspectionManager.Inspect() — show the toggle button.</summary>
    public void ShowToggleButton()
    {
        if (toggleButton != null)
        {
            toggleButton.gameObject.SetActive(true);
            UpdateToggleLabel();
        }
    }

    /// <summary>Called by InspectionManager.StopInspection() — hide the toggle button and restore panel.</summary>
    public void HideToggleButton()
    {
        if (toggleButton != null) toggleButton.gameObject.SetActive(false);

        if (userCollapsed)
        {
            userCollapsed = false;
            if (!isTemporarilyHidden && isVisible && !isInMonitorMode)
            {
                if (taskPanel != null) taskPanel.SetActive(true);
                if (taskCanvasGroup != null)
                    StartCoroutine(FadeCanvasGroup(taskCanvasGroup.alpha, 1f, 0.2f));
            }
            UpdateToggleLabel();
        }
    }

    IEnumerator FadeAndDisablePanel(float duration)
    {
        float start = taskCanvasGroup != null ? taskCanvasGroup.alpha : 1f;
        yield return StartCoroutine(FadeCanvasGroup(start, 0f, duration));
        if (taskPanel != null) taskPanel.SetActive(false);
    }
}

public class TaskRowData
{
    public string description;
    public GameObject rowObject;
    public TextMeshProUGUI textComponent;
    public Image circleImage;
    public GameObject checkmarkObject;
    public CanvasGroup rowCanvasGroup;
    public bool isCompleted;
}
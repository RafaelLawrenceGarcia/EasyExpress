using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// TaskListUI — Professional task panel with animated completion.
/// Now includes a toggle button to show/hide the task list.
///
/// SETUP — add a toggle button:
///   1. Create a Button inside TaskCanvas (NOT inside TaskPanel) — e.g. anchor top-right, above the panel
///   2. Give it a label TMP child that shows "Hide" / "Show"
///   3. Drag the Button into "toggleButton" and the label into "toggleButtonLabel"
///   4. Wire the Button's OnClick() to TaskListUI.TogglePanel()
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
    public Color pendingCircleColor   = new Color(1f, 1f, 1f, 0.2f);
    public Color completedCircleColor = new Color(0.365f, 0.792f, 0.647f, 1f);
    public Color completedCircleBG    = new Color(0.365f, 0.792f, 0.647f, 0.15f);
    public Color pendingTextColor     = new Color(1f, 1f, 1f, 0.85f);
    public Color completedTextColor   = new Color(1f, 1f, 1f, 0.3f);
    public Color flashColor           = new Color(0.365f, 0.792f, 0.647f, 1f);

    private List<TaskRowData> currentTasks    = new List<TaskRowData>();
    private bool isVisible            = false;
    private bool isTemporarilyHidden  = false;
    private bool userCollapsed        = false; // player manually hid it

    void Awake() { Instance = this; }

    void Start()
    {
        if (taskPanel != null)       taskPanel.SetActive(false);
        if (taskCanvasGroup != null) taskCanvasGroup.alpha = 0f;
        if (toggleButton != null)    toggleButton.gameObject.SetActive(false);
        UpdateToggleLabel();
    }

    // ── Public API — Show / Hide ─────────────────────────────────

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
        isVisible           = true;
        isTemporarilyHidden = false;

        // Show the toggle button whenever a task panel is active

        // Only actually show the panel if the player hasn't manually collapsed it
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
        isVisible           = false;
        isTemporarilyHidden = false;
        userCollapsed       = false;

        if (toggleButton != null) toggleButton.gameObject.SetActive(false);

        if (taskCanvasGroup != null)
            StartCoroutine(FadeAndDisable(0.3f));
        else if (taskPanel != null)
            taskPanel.SetActive(false);

        UpdateToggleLabel();
    }

    /// <summary>
    /// Called by the toggle button — lets the player show/hide the task panel manually.
    /// </summary>
    public void TogglePanel()
    {
        if (!isVisible) return; // no tasks active, nothing to toggle

        userCollapsed = !userCollapsed;

        if (userCollapsed)
        {
            // Collapse
            if (taskCanvasGroup != null)
                StartCoroutine(FadeAndDisablePanel(0.2f));
            else if (taskPanel != null)
                taskPanel.SetActive(false);
        }
        else
        {
            // Expand
            if (taskPanel != null) taskPanel.SetActive(true);
            if (taskCanvasGroup != null)
                StartCoroutine(FadeCanvasGroup(0f, 1f, 0.2f));
        }

        UpdateToggleLabel();
    }

    public void HideTemporarily()
    {
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
        toggleButtonLabel.text = userCollapsed ? "Show" : "Hide";
    }

    // ── Public API — Complete Tasks ──────────────────────────────

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

    // ── Internal — Spawning ──────────────────────────────────────

    void SpawnTaskRow(string description)
    {
        if (taskRowPrefab == null || taskContainer == null) return;

        GameObject row = Instantiate(taskRowPrefab, taskContainer);

        TextMeshProUGUI text = null;
        Image circle         = null;
        GameObject checkmark = null;
        CanvasGroup rowCG    = row.GetComponent<CanvasGroup>();

        Transform circleTransform = row.transform.Find("Circle");
        if (circleTransform != null)
        {
            circle = circleTransform.GetComponent<Image>();
            Transform checkTransform = circleTransform.Find("Checkmark");
            if (checkTransform != null) checkmark = checkTransform.gameObject;
        }

        Transform textTransform = row.transform.Find("TaskText");
        if (textTransform != null) text = textTransform.GetComponent<TextMeshProUGUI>();

        if (text != null)     text.text = description;
        if (checkmark != null) checkmark.SetActive(false);
        if (circle != null)
        {
            Outline outline = circle.GetComponent<Outline>();
            if (outline != null) outline.effectColor = pendingCircleColor;
        }

        currentTasks.Add(new TaskRowData
        {
            description      = description,
            rowObject        = row,
            textComponent    = text,
            circleImage      = circle,
            checkmarkObject  = checkmark,
            rowCanvasGroup   = rowCG,
            isCompleted      = false
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

    // ── Animation ────────────────────────────────────────────────

    IEnumerator AnimateCompletion(TaskRowData task)
    {
        if (task.circleImage == null || task.textComponent == null) yield break;

        Outline outline = task.circleImage.GetComponent<Outline>();
        if (outline != null) outline.effectColor = completedCircleColor;
        task.circleImage.color = completedCircleBG;

        float elapsed = 0f, duration = 0.3f;
        Transform circleT  = task.circleImage.transform;
        Vector3 origScale  = circleT.localScale;

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
            task.textComponent.color     = completedTextColor;
            task.textComponent.fontStyle = FontStyles.Strikethrough;
        }
    }

    // ── Utility ──────────────────────────────────────────────────

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

        // If the player had collapsed the panel, restore it when leaving inspection
        if (userCollapsed)
        {
            userCollapsed = false;
            if (!isTemporarilyHidden && isVisible)
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
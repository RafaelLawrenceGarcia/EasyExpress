using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// TaskListUI — Professional task panel with animated completion.
/// 
/// SETUP IN UNITY:
/// 1. Create Canvas "TaskCanvas" (Sort Order 8)
///    - Canvas Scaler: Scale With Screen Size, 1920x1080
///
/// 2. Inside, create Panel "TaskPanel"
///    - Anchor: top-right
///    - Pivot: (1, 1)
///    - Width: 280, Height: auto (use ContentSizeFitter vertical: PreferredSize)
///    - Pos X: -20, Pos Y: -20
///    - Image color: (0, 0, 0, 160)
///    - Add Outline: (255,255,255,20), distance (1,1)
///    - Corner radius via rounded sprite or just leave default
///    - Add CanvasGroup (for fading)
///    - Add VerticalLayoutGroup: padding 16 all sides, spacing 0, childForceExpand width true
///    - Add ContentSizeFitter: vertical = PreferredSize
///
/// 3. Inside TaskPanel, create "Header" (empty)
///    - Add HorizontalLayoutGroup: spacing 8, childAlignment MiddleLeft
///    - Height: 24 (use LayoutElement preferredHeight 24)
///    
///    Inside Header:
///    - "DotImage" (UI Image): 6x6, green circle sprite, LayoutElement 6x6
///    - "TitleText" (TMP): "OBJECTIVES", size 11, bold, white 40% alpha, uppercase
///    - "CounterText" (TMP): "0/3", size 11, white 25% alpha, right-aligned
///
/// 4. Inside TaskPanel, create "TaskContainer" (empty)
///    - Add VerticalLayoutGroup: spacing 0, padding top 8
///    - This is where task rows get spawned
///
/// 5. Create a PREFAB called "TaskRowPrefab":
///    - Panel, Height: 36, transparent bg
///    - Add LayoutElement: preferredHeight 36
///    - Inside, manually position:
///      - "Circle" (Image): 20x20, anchor middle-left, PosX 0
///        - Transparent bg, Outline (255,255,255,50) distance (1,1)
///        - Inside Circle: "Checkmark" (Image) 12x12, centered, with a checkmark sprite, START DISABLED
///      - "TaskText" (TMP): anchor middle-left, PosX 30, width 220
///        - Size 13, white 85% alpha, left+middle
///    - Add CanvasGroup (for fade animations)
///    - Drag to Project folder as prefab, delete from scene
///
/// 6. Put TaskListUI script on TaskPanel or a manager object, drag references
/// </summary>
public class TaskListUI : MonoBehaviour
{
    public static TaskListUI Instance;

    [Header("Panel")]
    public GameObject taskPanel;
    public CanvasGroup taskCanvasGroup;

    [Header("Header")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI counterText;
    public Image dotImage;

    [Header("Task Container")]
    public Transform taskContainer;    // Where rows get spawned
    public GameObject taskRowPrefab;   // The task row prefab

    [Header("Colors")]
    public Color pendingCircleColor = new Color(1f, 1f, 1f, 0.2f);
    public Color completedCircleColor = new Color(0.365f, 0.792f, 0.647f, 1f); // #5DCAA5
    public Color completedCircleBG = new Color(0.365f, 0.792f, 0.647f, 0.15f);
    public Color pendingTextColor = new Color(1f, 1f, 1f, 0.85f);
    public Color completedTextColor = new Color(1f, 1f, 1f, 0.3f);
    public Color flashColor = new Color(0.365f, 0.792f, 0.647f, 1f); // Green flash

    // --- STATE ---
    private List<TaskRowData> currentTasks = new List<TaskRowData>();
    private bool isVisible = false;
    private bool isTemporarilyHidden = false;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (taskPanel != null) taskPanel.SetActive(false);
        if (taskCanvasGroup != null) taskCanvasGroup.alpha = 0f;
    }

    // =============================================
    //  PUBLIC API — Show / Hide
    // =============================================

    /// <summary>
    /// Set the header title (e.g. "OBJECTIVES" or "REPAIR TASKS")
    /// </summary>
    public void SetTitle(string title)
    {
        if (titleText != null) titleText.text = title;
    }

    /// <summary>
    /// Clear all tasks and set new ones. Each string becomes a task row.
    /// </summary>
    public void SetTasks(string[] taskDescriptions)
    {
        ClearTasks();

        foreach (string desc in taskDescriptions)
        {
            SpawnTaskRow(desc);
        }

        UpdateCounter();
        ShowPanel();
    }

    /// <summary>
    /// Show the panel with fade-in.
    /// </summary>
    public void ShowPanel()
    {
        isVisible = true;
        isTemporarilyHidden = false;
        if (taskPanel != null) taskPanel.SetActive(true);
        if (taskCanvasGroup != null)
            StartCoroutine(FadeCanvasGroup(taskCanvasGroup.alpha, 1f, 0.4f));
    }

    /// <summary>
    /// Hide the panel with fade-out.
    /// </summary>
    public void HidePanel()
    {
        isVisible = false;
        isTemporarilyHidden = false;
        if (taskCanvasGroup != null)
            StartCoroutine(FadeAndDisable(0.3f));
        else if (taskPanel != null)
            taskPanel.SetActive(false);
    }

    /// <summary>
    /// Temporarily hide (e.g. during PC inspection or shop computer).
    /// </summary>
    public void HideTemporarily()
    {
        if (!isVisible) return;
        isTemporarilyHidden = true;
        if (taskCanvasGroup != null)
            StartCoroutine(FadeCanvasGroup(taskCanvasGroup.alpha, 0f, 0.2f));
        else if (taskPanel != null)
            taskPanel.SetActive(false);
    }

    /// <summary>
    /// Restore after temporary hide.
    /// </summary>
    public void RestoreIfNeeded()
    {
        if (!isVisible || !isTemporarilyHidden) return;
        isTemporarilyHidden = false;
        if (taskPanel != null) taskPanel.SetActive(true);
        if (taskCanvasGroup != null)
            StartCoroutine(FadeCanvasGroup(0f, 1f, 0.3f));
    }

    // =============================================
    //  PUBLIC API — Complete Tasks
    // =============================================

    /// <summary>
    /// Complete a task by index (0-based). Plays the green flash → strikethrough animation.
    /// </summary>
    public void CompleteTask(int index)
    {
        if (index < 0 || index >= currentTasks.Count) return;
        if (currentTasks[index].isCompleted) return;

        currentTasks[index].isCompleted = true;
        StartCoroutine(AnimateCompletion(currentTasks[index]));
        UpdateCounter();
    }

    /// <summary>
    /// Complete a task by matching its description text.
    /// </summary>
    public void CompleteTaskByName(string description)
    {
        for (int i = 0; i < currentTasks.Count; i++)
        {
            if (!currentTasks[i].isCompleted && currentTasks[i].description == description)
            {
                CompleteTask(i);
                return;
            }
        }
    }

    /// <summary>
    /// Check if ALL tasks are completed.
    /// </summary>
    public bool AreAllTasksComplete()
    {
        foreach (var t in currentTasks)
        {
            if (!t.isCompleted) return false;
        }
        return currentTasks.Count > 0;
    }

    /// <summary>
    /// Get count of completed tasks.
    /// </summary>
    public int GetCompletedCount()
    {
        int count = 0;
        foreach (var t in currentTasks)
            if (t.isCompleted) count++;
        return count;
    }

    // =============================================
    //  INTERNAL — Spawning
    // =============================================

    void SpawnTaskRow(string description)
    {
        if (taskRowPrefab == null || taskContainer == null) return;

        GameObject row = Instantiate(taskRowPrefab, taskContainer);

        // Find components in the prefab
        TextMeshProUGUI text = null;
        Image circle = null;
        GameObject checkmark = null;
        CanvasGroup rowCG = row.GetComponent<CanvasGroup>();

        // Find circle (first Image child that isn't the row's own Image)
        Transform circleTransform = row.transform.Find("Circle");
        if (circleTransform != null)
        {
            circle = circleTransform.GetComponent<Image>();
            // Find checkmark inside circle
            Transform checkTransform = circleTransform.Find("Checkmark");
            if (checkTransform != null)
                checkmark = checkTransform.gameObject;
        }

        // Find text
        Transform textTransform = row.transform.Find("TaskText");
        if (textTransform != null)
            text = textTransform.GetComponent<TextMeshProUGUI>();

        if (text != null) text.text = description;
        if (checkmark != null) checkmark.SetActive(false);
        if (circle != null)
        {
            // Set the outline/border color to pending
            Outline outline = circle.GetComponent<Outline>();
            if (outline != null) outline.effectColor = pendingCircleColor;
        }

        TaskRowData data = new TaskRowData
        {
            description = description,
            rowObject = row,
            textComponent = text,
            circleImage = circle,
            checkmarkObject = checkmark,
            rowCanvasGroup = rowCG,
            isCompleted = false
        };

        currentTasks.Add(data);
    }

    void ClearTasks()
    {
        StopAllCoroutines(); // stop AnimateCompletion before destroying rows
        foreach (var t in currentTasks)
        {
            if (t.rowObject != null) Destroy(t.rowObject);
        }
        currentTasks.Clear();
    }

    void UpdateCounter()
    {
        if (counterText == null) return;
        int completed = GetCompletedCount();
        counterText.text = completed + "/" + currentTasks.Count;
    }

    // =============================================
    //  ANIMATION — Task Completion
    // =============================================

    IEnumerator AnimateCompletion(TaskRowData task)
    {
        if (task.circleImage == null || task.textComponent == null) yield break;

        // --- PHASE 1: Green flash (circle lights up, text turns green, circle scales up) ---
        Outline outline = task.circleImage.GetComponent<Outline>();

        // Flash the circle border green
        if (outline != null) outline.effectColor = completedCircleColor;
        task.circleImage.color = completedCircleBG;

        // Scale up the circle
        float elapsed = 0f;
        float duration = 0.3f;
        Transform circleT = task.circleImage.transform;
        Vector3 originalScale = circleT.localScale;

        // Show checkmark
        if (task.checkmarkObject != null) task.checkmarkObject.SetActive(true);

        // Flash text green
        task.textComponent.color = flashColor;

        while (elapsed < duration)
        {
            if (circleT == null) yield break;
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float scale = 1f + Mathf.Sin(t * Mathf.PI) * 0.35f;
            circleT.localScale = originalScale * scale;
            yield return null;
        }
        if (circleT != null) circleT.localScale = originalScale;

        // --- PHASE 2: Settle (text fades to grey + strikethrough) ---
        yield return new WaitForSeconds(0.3f);

        // Fade text to completed color and add strikethrough
        elapsed = 0f;
        duration = 0.4f;
        Color startColor = flashColor;
        Color endColor = completedTextColor;

        while (elapsed < duration)
        {
            if (task.textComponent == null) yield break;
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            task.textComponent.color = Color.Lerp(startColor, endColor, t);
            yield return null;
        }
        if (task.textComponent != null)
        {
            task.textComponent.color = completedTextColor;
            task.textComponent.fontStyle = FontStyles.Strikethrough;
        }
    }

    // =============================================
    //  UTILITY
    // =============================================

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
        yield return StartCoroutine(FadeCanvasGroup(taskCanvasGroup != null ? taskCanvasGroup.alpha : 1f, 0f, duration));
        if (taskPanel != null) taskPanel.SetActive(false);
    }
    /// <summary>
    /// Updates the text of a specific task without changing its completion state.
    /// Used by the tutorial to show live progress like "Unscrew side panel (2/4)".
    /// </summary>
    public void UpdateTaskText(int index, string newText)
    {
        if (index < 0 || index >= currentTasks.Count) return;
        if (currentTasks[index].isCompleted) return; // Don't update completed tasks

        currentTasks[index].description = newText;

        if (currentTasks[index].textComponent != null)
            currentTasks[index].textComponent.text = newText;
    }

}

// =============================================
//  DATA CLASS
// =============================================
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
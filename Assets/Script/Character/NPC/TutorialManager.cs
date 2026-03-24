using UnityEngine;
using TMPro;
using System.Collections;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance;

    [Header("Managers")]
    public IntroDialogueManager dialogueManager;
    public DayTransitionManager dayTransitionManager;

    [Header("Dialogue Files")]
    public DialogueSequence part1_MoveDialogue;
    public DialogueSequence part2_CustomerDialogue;
    public DialogueSequence part3_PickupBoxDialogue;
    public DialogueSequence part4_PlaceBoxDialogue;
    public DialogueSequence part5_PCDialogue;
    public DialogueSequence part6_HoverDialogue;
    public DialogueSequence part7_RemoveDialogue;

    [Header("Movement Task Settings")]
    public float requiredHoldTime = 2.0f;
    private float wTimer, aTimer, sTimer, dTimer;
    private bool wDone, aDone, sDone, dDone;

    // --- STATE ---
    private int tutorialStep = 0;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (PlayerPrefs.GetInt("TutorialDone", 0) == 1 || PlayerPrefs.GetInt("IsLoadingGame", 0) == 1)
        {
            tutorialStep = 14;

            // FIX: Persist TutorialDone so it survives after IsLoadingGame gets reset
            if (PlayerPrefs.GetInt("TutorialDone", 0) != 1)
            {
                PlayerPrefs.SetInt("TutorialDone", 1);
                PlayerPrefs.Save();
            }
        }
        else
        {
            // Brand new game, start the tutorial!
            tutorialStep = 0;
            // ... rest stays the same
        }
    }
    IEnumerator DelayedStart()
    {
        yield return new WaitForSeconds(1.0f);
        dialogueManager.PlaySequence(part1_MoveDialogue, StartMovementTask);
    }

    IEnumerator DelayThenDialogue(float delay, DialogueSequence seq, System.Action callback)
    {
        yield return new WaitForSeconds(delay);
        dialogueManager.PlaySequence(seq, callback);
    }

    public void HideTaskTemporarily()
    {
        if (TaskListUI.Instance != null) TaskListUI.Instance.HideTemporarily();
    }

    public void RestoreTaskIfNeeded()
    {
        if (TaskListUI.Instance != null) TaskListUI.Instance.RestoreIfNeeded();
    }

    void StartMovementTask()
    {
        wDone = false; aDone = false; sDone = false; dDone = false;

        if (TaskListUI.Instance != null)
        {
            TaskListUI.Instance.SetTitle("CALIBRATION");
            TaskListUI.Instance.SetTasks(new string[] {
                "Hold W to move forward",
                "Hold A to move left",
                "Hold S to move backward",
                "Hold D to move right"
            });
        }
        tutorialStep = 1;
    }

    void Update()
    {
        if (tutorialStep == 1)
        {
            if (Input.GetKey(KeyCode.W)) wTimer += Time.deltaTime;
            if (Input.GetKey(KeyCode.A)) aTimer += Time.deltaTime;
            if (Input.GetKey(KeyCode.S)) sTimer += Time.deltaTime;
            if (Input.GetKey(KeyCode.D)) dTimer += Time.deltaTime;

            wTimer = Mathf.Clamp(wTimer, 0, requiredHoldTime);
            aTimer = Mathf.Clamp(aTimer, 0, requiredHoldTime);
            sTimer = Mathf.Clamp(sTimer, 0, requiredHoldTime);
            dTimer = Mathf.Clamp(dTimer, 0, requiredHoldTime);

            if (TaskListUI.Instance != null)
            {
                if (!wDone && wTimer >= requiredHoldTime) { wDone = true; TaskListUI.Instance.CompleteTask(0); }
                if (!aDone && aTimer >= requiredHoldTime) { aDone = true; TaskListUI.Instance.CompleteTask(1); }
                if (!sDone && sTimer >= requiredHoldTime) { sDone = true; TaskListUI.Instance.CompleteTask(2); }
                if (!dDone && dTimer >= requiredHoldTime) { dDone = true; TaskListUI.Instance.CompleteTask(3); }
            }

            if (wTimer >= requiredHoldTime && aTimer >= requiredHoldTime &&
                sTimer >= requiredHoldTime && dTimer >= requiredHoldTime)
            {
                tutorialStep = 2;
                StartCoroutine(DelayedHideAndDialogue(1.0f, part2_CustomerDialogue, StartCustomerTask));
            }
        }
    }

    void StartCustomerTask()
    {
        if (TaskListUI.Instance != null)
        {
            TaskListUI.Instance.SetTitle("OBJECTIVES");
            TaskListUI.Instance.SetTasks(new string[] {
                "Approach the customer",
                "Press E to talk",
                "Accept the job"
            });
        }
        tutorialStep = 3;
    }

    public void CompleteCustomerTask()
    {
        if (tutorialStep == 3)
        {
            if (TaskListUI.Instance != null)
            {
                TaskListUI.Instance.CompleteTask(0);
                TaskListUI.Instance.CompleteTask(1);
                TaskListUI.Instance.CompleteTask(2);
            }

            tutorialStep = 4;
            StartCoroutine(DelayedHideAndDialogue(1.0f, part3_PickupBoxDialogue, StartPickupBoxTask));
        }
    }

    void StartPickupBoxTask()
    {
        if (TaskListUI.Instance != null)
        {
            TaskListUI.Instance.SetTitle("OBJECTIVES");
            TaskListUI.Instance.SetTasks(new string[] {
                "Find the PC box on the shelf",
                "Press Q to pick it up"
            });
        }
        tutorialStep = 5;
    }

    public void CompletePickupBoxTask()
    {
        if (tutorialStep == 5)
        {
            if (TaskListUI.Instance != null)
            {
                TaskListUI.Instance.CompleteTask(0);
                TaskListUI.Instance.CompleteTask(1);
            }

            tutorialStep = 6;
            StartCoroutine(DelayedHideAndDialogue(1.0f, part4_PlaceBoxDialogue, StartPlaceBoxTask));
        }
    }

    void StartPlaceBoxTask()
    {
        if (TaskListUI.Instance != null)
        {
            TaskListUI.Instance.SetTitle("OBJECTIVES");
            TaskListUI.Instance.SetTasks(new string[] {
                "Look at the workstation desk",
                "Left-click to place the box"
            });
        }
        tutorialStep = 7;
    }

    public void CompletePlaceBoxTask()
    {
        if (tutorialStep == 7)
        {
            if (TaskListUI.Instance != null)
            {
                TaskListUI.Instance.CompleteTask(0);
                TaskListUI.Instance.CompleteTask(1);
            }

            tutorialStep = 8;
            StartCoroutine(DelayedHideAndDialogue(1.0f, part5_PCDialogue, StartPCTask));
        }
    }

    void StartPCTask()
    {
        if (TaskListUI.Instance != null)
        {
            TaskListUI.Instance.SetTitle("OBJECTIVES");
            TaskListUI.Instance.SetTasks(new string[] {
                "Inspect the unpacked PC"
            });
        }
        tutorialStep = 9;
    }

    public void CompletePCTask()
    {
        if (tutorialStep == 9)
        {
            if (TaskListUI.Instance != null)
                TaskListUI.Instance.CompleteTask(0);

            tutorialStep = 10;
            StartCoroutine(DelayedHideAndDialogue(1.0f, part6_HoverDialogue, StartHoverTask));
        }
    }

    void StartHoverTask()
    {
        if (TaskListUI.Instance != null)
        {
            TaskListUI.Instance.SetTitle("OBJECTIVES");
            TaskListUI.Instance.SetTasks(new string[] {
                "Hover over a PC component"
            });
        }
        tutorialStep = 11;
    }

    public void CompleteHoverTask()
    {
        if (tutorialStep == 11)
        {
            if (TaskListUI.Instance != null)
                TaskListUI.Instance.CompleteTask(0);

            tutorialStep = 12;
            StartCoroutine(DelayedHideAndDialogue(1.0f, part7_RemoveDialogue, StartRemoveTask));
        }
    }

    void StartRemoveTask()
    {
        if (TaskListUI.Instance != null)
        {
            TaskListUI.Instance.SetTitle("OBJECTIVES");
            TaskListUI.Instance.SetTasks(new string[] {
                "Left-click to remove the part"
            });
        }
        tutorialStep = 13;
    }

    public void CompleteRemoveTask()
    {
        if (tutorialStep == 13)
        {
            if (TaskListUI.Instance != null)
                TaskListUI.Instance.CompleteTask(0);

            tutorialStep = 14;
            StartCoroutine(FinalCleanup());
        }
    }

    IEnumerator FinalCleanup()
    {
        yield return new WaitForSeconds(1.5f);
        if (TaskListUI.Instance != null) TaskListUI.Instance.HidePanel();

        PlayerPrefs.SetInt("TutorialDone", 1);
        PlayerPrefs.Save();
        Debug.Log("Tutorial Fully Complete!");
    }

    IEnumerator DelayedHideAndDialogue(float delay, DialogueSequence seq, System.Action callback)
    {
        yield return new WaitForSeconds(delay);
        if (TaskListUI.Instance != null) TaskListUI.Instance.HidePanel();
        yield return new WaitForSeconds(0.3f);
        dialogueManager.PlaySequence(seq, callback);
    }

    public int GetCurrentStep() { return tutorialStep; }

    public bool IsTutorialActive()
    {
        if (PlayerPrefs.GetInt("TutorialDone", 0) == 1) return false;
        return tutorialStep > 0 && tutorialStep < 14;
    }
}
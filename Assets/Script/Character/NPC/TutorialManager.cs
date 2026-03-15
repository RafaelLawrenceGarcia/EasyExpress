using UnityEngine;
using TMPro;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance;

    [Header("Managers")]
    public IntroDialogueManager dialogueManager;
    
    [Header("Task UI")]
    public GameObject taskUIPanel;
    public TextMeshProUGUI taskText;

    [Header("Dialogue Files")]
    public DialogueSequence part1_MoveDialogue;
    public DialogueSequence part2_CustomerDialogue; // NEW: Talk to customer to get the box
    public DialogueSequence part3_PickupBoxDialogue;// NEW: Go to the shelf and pick up the box
    public DialogueSequence part4_PlaceBoxDialogue; // NEW: Put the box on the workstation
    public DialogueSequence part5_PCDialogue;       // Inspect the PC
    public DialogueSequence part6_HoverDialogue;    // Hover over a part
    public DialogueSequence part7_RemoveDialogue;   // Remove a part

    [Header("Movement Task Settings")]
    public float requiredHoldTime = 2.0f; 
    private float wTimer, aTimer, sTimer, dTimer;

    private int tutorialStep = 0;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        taskUIPanel.SetActive(false);

        if (PlayerPrefs.GetInt("IsLoadingGame", 0) == 0)
        {
            tutorialStep = 0;
            dialogueManager.PlaySequence(part1_MoveDialogue, StartMovementTask);
        }
    }

    // --- 1. MOVEMENT TASK ---
    void StartMovementTask()
    {
        taskUIPanel.SetActive(true);
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

            taskText.text = "System Check - Calibrating Movement:\n" +
                            $"Hold W: {(wTimer / requiredHoldTime) * 100:0}%\n" +
                            $"Hold A: {(aTimer / requiredHoldTime) * 100:0}%\n" +
                            $"Hold S: {(sTimer / requiredHoldTime) * 100:0}%\n" +
                            $"Hold D: {(dTimer / requiredHoldTime) * 100:0}%";

            if (wTimer >= requiredHoldTime && aTimer >= requiredHoldTime && 
                sTimer >= requiredHoldTime && dTimer >= requiredHoldTime)
            {
                tutorialStep = 2; 
                taskUIPanel.SetActive(false); 
                dialogueManager.PlaySequence(part2_CustomerDialogue, StartCustomerTask);
            }
        }
    }

    // --- 2. CUSTOMER TASK ---
    void StartCustomerTask()
    {
        taskUIPanel.SetActive(true);
        taskText.text = "- Approach the waiting customer\n- Press E to talk\n- Click 'Accept' to take the job";
        tutorialStep = 3; 
    }

    public void CompleteCustomerTask()
    {
        if (tutorialStep == 3)
        {
            tutorialStep = 4;
            taskUIPanel.SetActive(false);
            dialogueManager.PlaySequence(part3_PickupBoxDialogue, StartPickupBoxTask);
        }
    }

    // --- 3. PICK UP BOX TASK ---
    void StartPickupBoxTask()
    {
        taskUIPanel.SetActive(true);
        taskText.text = "- Find the customer's PC Box on the shelf\n- Press Q to pick it up";
        tutorialStep = 5;
    }

    public void CompletePickupBoxTask()
    {
        if (tutorialStep == 5)
        {
            tutorialStep = 6;
            taskUIPanel.SetActive(false);
            dialogueManager.PlaySequence(part4_PlaceBoxDialogue, StartPlaceBoxTask);
        }
    }

    // --- 4. PLACE BOX TASK ---
    void StartPlaceBoxTask()
    {
        taskUIPanel.SetActive(true);
        taskText.text = "- Look at the empty Workstation desk\n- Click Left Mouse Button to place and unpack the box";
        tutorialStep = 7;
    }

    public void CompletePlaceBoxTask()
    {
        if (tutorialStep == 7)
        {
            tutorialStep = 8;
            taskUIPanel.SetActive(false);
            dialogueManager.PlaySequence(part5_PCDialogue, StartPCTask);
        }
    }

    // --- 5. PC INTERACT TASK ---
    void StartPCTask()
    {
        taskUIPanel.SetActive(true);
        taskText.text = "- Press E on the Unpacked PC to inspect it";
        tutorialStep = 9; 
    }

    public void CompletePCTask()
    {
        if (tutorialStep == 9)
        {
            tutorialStep = 10;
            taskUIPanel.SetActive(false);
            dialogueManager.PlaySequence(part6_HoverDialogue, StartHoverTask);
        }
    }

    // --- 6. HOVER TASK ---
    void StartHoverTask()
    {
        taskUIPanel.SetActive(true);
        taskText.text = "- Move your mouse over a PC component to highlight it";
        tutorialStep = 11;
    }

    public void CompleteHoverTask()
    {
        if (tutorialStep == 11)
        {
            tutorialStep = 12;
            taskUIPanel.SetActive(false);
            dialogueManager.PlaySequence(part7_RemoveDialogue, StartRemoveTask);
        }
    }

    // --- 7. REMOVE TASK ---
    void StartRemoveTask()
    {
        taskUIPanel.SetActive(true);
        taskText.text = "- Left-click on the highlighted component to remove it";
        tutorialStep = 13;
    }

    public void CompleteRemoveTask()
    {
        if (tutorialStep == 13)
        {
            tutorialStep = 14;
            taskUIPanel.SetActive(false);
            Debug.Log("Tutorial Fully Complete! You are now ready to run EasyExpress.");
        }
    }
}
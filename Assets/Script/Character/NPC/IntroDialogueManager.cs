using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class IntroDialogueManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject dialogueUIPanel;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI dialogueText;
    public Image portraitImage;

    [Header("Player References")]
    public GTAMovement playerMovement;
    public OrbitCamera playerCamera;

    private DialogueSequence currentSequence;
    private int currentLineIndex = 0;
    public bool isDialogueActive = false;

    private System.Action onDialogueFinished;

    void Update()
    {
        if (isDialogueActive && Input.GetMouseButtonDown(0))
        {
            NextLine();
        }
    }

    public void PlaySequence(DialogueSequence newSequence, System.Action onFinished)
    {
        currentSequence = newSequence;
        onDialogueFinished = onFinished;

        isDialogueActive = true;
        currentLineIndex = 0;
        dialogueUIPanel.SetActive(true);
        // Hide inspection toolbar so it doesn't cover dialogue
        GameObject toolbar = GameObject.Find("InspectionToolbarCanvas");
        if (toolbar != null) toolbar.SetActive(false);

        if (playerMovement != null) playerMovement.SetMovementState(false);
        if (playerCamera != null) playerCamera.SetCameraState(false);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        DisplayCurrentLine();
    }

    void DisplayCurrentLine()
    {
        DialogueLine line = currentSequence.lines[currentLineIndex];

        nameText.text = line.speakerName;
        dialogueText.text = line.text;

        if (line.portrait != null)
        {
            portraitImage.sprite = line.portrait;
            portraitImage.gameObject.SetActive(true);
        }
        else
        {
            portraitImage.gameObject.SetActive(false);
        }
    }

    void NextLine()
    {
        currentLineIndex++;

        if (currentLineIndex < currentSequence.lines.Length)
        {
            DisplayCurrentLine();
        }
        else
        {
            EndIntro();
        }
    }

    void EndIntro()
    {
        isDialogueActive = false;
        dialogueUIPanel.SetActive(false);
        // Restore inspection toolbar
        GameObject toolbar = GameObject.Find("InspectionToolbarCanvas");
        if (toolbar != null) toolbar.SetActive(true);

        if (playerMovement != null) playerMovement.SetMovementState(true);
        if (playerCamera != null) playerCamera.SetCameraState(true);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (onDialogueFinished != null)
        {
            onDialogueFinished.Invoke();
            onDialogueFinished = null;
        }
    }
}
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
            NextLine();
    }

    public void PlaySequence(DialogueSequence newSequence, System.Action onFinished)
    {
        currentSequence = newSequence;
        onDialogueFinished = onFinished;

        isDialogueActive = true;
        currentLineIndex = 0;
        dialogueUIPanel.SetActive(true);

        // FIX: use Hide() instead of SetActive(false) on the whole canvas.
        // SetActive(false) on the canvas kills the script instance and breaks
        // InspectionToolbarUI.Instance for the rest of the session.
        if (InspectionToolbarUI.Instance != null)
            InspectionToolbarUI.Instance.Hide();

        if (playerMovement != null) playerMovement.SetMovementState(false);
        if (playerCamera   != null) playerCamera.SetCameraState(false);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        DisplayCurrentLine();
    }

    void DisplayCurrentLine()
    {
        DialogueLine line = currentSequence.lines[currentLineIndex];

        nameText.text    = line.speakerName;
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
            DisplayCurrentLine();
        else
            EndIntro();
    }

    void EndIntro()
    {
        isDialogueActive = false;
        dialogueUIPanel.SetActive(false);

        // FIX: only restore toolbar if we're currently inspecting a PC.
        // If we're not in inspection mode the toolbar should stay hidden.
        if (InspectionToolbarUI.Instance != null)
        {
            InspectionManager im = FindFirstObjectByType<InspectionManager>();
            if (im != null && im.isInspecting)
                InspectionToolbarUI.Instance.Show();
        }

        if (playerMovement != null) playerMovement.SetMovementState(true);
        if (playerCamera   != null) playerCamera.SetCameraState(true);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (onDialogueFinished != null)
        {
            onDialogueFinished.Invoke();
            onDialogueFinished = null;
        }
    }
}
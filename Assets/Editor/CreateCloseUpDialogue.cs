#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// Creates the dlg_CloseUpPC DialogueSequence asset.
/// Run from Unity menu: EasyExpress > Create Close-Up Dialogue
///
/// After running, drag the created asset into TutorialManager's
/// "Dialogue — Phase 3g: Close Up PC" → dlg_CloseUpPC slot.
///
/// You can also edit the text in the Inspector after creation.
/// </summary>
public static class CreateCloseUpDialogue
{
    [MenuItem("EasyExpress/Create Close-Up Dialogue")]
    public static void Create()
    {
        // Create the ScriptableObject
        DialogueSequence asset = ScriptableObject.CreateInstance<DialogueSequence>();

        asset.lines = new DialogueLine[]
        {
            new DialogueLine
            {
                speakerName = "CEO",
                portrait = null,  // Drag your CEO portrait here after creation
                text = "Excellent work! The PC boots perfectly now. All faults have been confirmed and fixed!",
                showOnRightSide = true
            },
            new DialogueLine
            {
                speakerName = "CEO",
                portrait = null,
                text = "Before we send it back, let's close up the case. Turn off the PC, then put the side panel and screws back on.",
                showOnRightSide = true
            },
            new DialogueLine
            {
                speakerName = "CEO",
                portrait = null,
                text = "A professional repair shop always returns hardware looking as clean and tidy as when it came in!",
                showOnRightSide = true
            }
        };

        // Save to Assets folder
        string path = "Assets/ScriptableObjects/Dialogues/dlg_CloseUpPC.asset";

        // Make sure the folder exists
        if (!AssetDatabase.IsValidFolder("Assets/ScriptableObjects"))
            AssetDatabase.CreateFolder("Assets", "ScriptableObjects");
        if (!AssetDatabase.IsValidFolder("Assets/ScriptableObjects/Dialogues"))
            AssetDatabase.CreateFolder("Assets/ScriptableObjects", "Dialogues");

        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Select the new asset in the Project window
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = asset;

        Debug.Log($"[EasyExpress] Created dialogue asset at: {path}\n" +
                  "Drag it into TutorialManager → dlg_CloseUpPC slot.\n" +
                  "Don't forget to assign the CEO portrait sprite on each line!");
    }
}
#endif

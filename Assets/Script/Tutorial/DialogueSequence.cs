using UnityEngine;

[System.Serializable]
public class DialogueLine
{
    public string speakerName;
    public Sprite portrait;
    [TextArea(3, 5)] 
    public string text;
    
    [Header("Positioning")]
    [Tooltip("Check this if the character should appear on the right side. Uncheck for left.")]
    public bool showOnRightSide = true; 
}

[CreateAssetMenu(fileName = "New Intro Sequence", menuName = "EasyExpress/Intro Dialogue")]
public class DialogueSequence : ScriptableObject
{
    public DialogueLine[] lines;
}
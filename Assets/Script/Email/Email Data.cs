using UnityEngine;

[CreateAssetMenu(fileName = "New Email", menuName = "EasyExpress/Email Job")]
public class EmailData : ScriptableObject
{
    [Header("Sender Info")]
    public string senderName;
    public Sprite profilePic;
    public string subjectLine; 
    
    [TextArea(5, 10)]
    public string bodyText; 

    [Header("Job Stats")]
    public float labourCost;
    public float partsBudget;
    
    [Header("Requirements")]
    public string[] objectives; 

    [Header("PC Diagnostics")]
    public string[] pcProblems; 
}
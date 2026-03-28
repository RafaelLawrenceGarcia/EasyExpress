using UnityEngine;

/// <summary>
/// Attach this to your prefabs so the Tutorial Arrow can find them.
///
/// SETUP (do this once per prefab):
/// 1. Select the Customer NPC prefab → Add Component → TutorialTarget → set Type to "Customer"
/// 2. Select the PC Box prefab       → Add Component → TutorialTarget → set Type to "Box"
/// 3. Select the PC prefab           → Add Component → TutorialTarget → set Type to "PC"
///
/// That's it! TutorialManager finds them automatically.
/// </summary>
public class TutorialTarget : MonoBehaviour
{
    public enum TargetType { Customer, Box, PC }

    [Tooltip("What type of object is this?")]
    public TargetType type;
}
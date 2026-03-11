using UnityEngine;

/// <summary>
/// Attached to a wireHead GameObject when a wire is committed.
/// Destroying the wireCollider (via normal removal) triggers WireCleanup
/// which hides the heads. This component is removed when the wire is removed.
/// </summary>
public class WireHeadRemover : MonoBehaviour
{
    [HideInInspector] public GameObject wireCollider;
}
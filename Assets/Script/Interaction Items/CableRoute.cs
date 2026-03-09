using UnityEngine;

/// <summary>
/// Attach to an empty GameObject inside your PC case.
/// Set connectorType to match InspectableItem.connectorType (e.g. "24pin").
/// Add child empty GameObjects as waypoints and assign them in order.
/// The cyan gizmo lines in the Scene view show the route.
/// </summary>
public class CableRoute : MonoBehaviour
{
    [Tooltip("Must match InspectableItem.connectorType exactly, e.g. '24pin'")]
    public string connectorType;

    [Tooltip("Ordered waypoints the cable will follow through the case.")]
    public Transform[] waypoints;

    void OnDrawGizmos()
    {
        if (waypoints == null || waypoints.Length < 2) return;
        Gizmos.color = Color.cyan;
        for (int i = 0; i < waypoints.Length - 1; i++)
        {
            if (waypoints[i] == null || waypoints[i + 1] == null) continue;
            Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
            Gizmos.DrawSphere(waypoints[i].position, 0.01f);
        }
        Gizmos.DrawSphere(waypoints[waypoints.Length - 1].position, 0.01f);
    }
}
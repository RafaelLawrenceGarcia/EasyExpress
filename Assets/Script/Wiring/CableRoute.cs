using UnityEngine;

public class CableRoute : MonoBehaviour
{
    [Tooltip("Must match InspectableItem.connectorType exactly, e.g. '24pin'")]
    public string connectorType;

    [Tooltip("Ordered waypoints the cable will follow through the case. " +
             "For a 24-pin: MB port → behind board → cable-mgmt hole → PSU shroud → PSU socket.")]
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
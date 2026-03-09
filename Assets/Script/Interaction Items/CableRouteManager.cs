using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Place ONE instance of this anywhere in the scene.
/// Drag all your CableRoute objects into the routes list.
/// </summary>
public class CableRouteManager : MonoBehaviour
{
    public static CableRouteManager Instance;

    [Tooltip("Drag all CableRoute GameObjects here")]
    public List<CableRoute> routes = new List<CableRoute>();

    void Awake() { Instance = this; }

    /// <summary>
    /// Returns world-space waypoints for the given connector type,
    /// with the actual port positions inserted at start and end.
    /// Falls back to a straight two-point path if no route is defined.
    /// </summary>
    public Vector3[] GetRoute(string connectorType, Vector3 startWorld, Vector3 endWorld)
    {
        CableRoute route = routes.Find(r => r.connectorType == connectorType);

        if (route == null || route.waypoints == null || route.waypoints.Length == 0)
            return new Vector3[] { startWorld, endWorld };

        var path = new List<Vector3>();
        path.Add(startWorld);
        foreach (var wp in route.waypoints)
            if (wp != null) path.Add(wp.position);
        path.Add(endWorld);

        return path.ToArray();
    }
}
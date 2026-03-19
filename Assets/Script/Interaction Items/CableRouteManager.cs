using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Place ONE instance of this anywhere in the scene.
/// Drag all your CableRoute objects into the routes list.
///
/// After assembling the raw waypoints the path is passed through
/// CablePathSolver, which SphereCasts each segment and inserts
/// detour points wherever a PC component would block the wire.
///
/// QUICK SETUP
/// ───────────
/// • Create a layer called "PCObstacle" in Project Settings ▸ Tags & Layers.
/// • Set every solid PC mesh collider (GPU body, RAM, PSU shroud, etc.)
///   to that layer.  Do NOT add wire ports to it.
/// • In this component's inspector, set Obstacle Mask = PCObstacle.
/// </summary>
public class CableRouteManager : MonoBehaviour
{
    public static CableRouteManager Instance;

    // ─────────────────────────────────────────────────────────────────────────
    //  INSPECTOR
    // ─────────────────────────────────────────────────────────────────────────
    [Tooltip("Drag all CableRoute GameObjects here.")]
    public List<CableRoute> routes = new List<CableRoute>();

    [Header("Obstacle Avoidance")]
    [Tooltip("Layers the solver treats as solid obstacles.\n" +
             "Add your 'PCObstacle' layer here.\n" +
             "Leave at Nothing to disable avoidance entirely.")]
    public LayerMask obstacleMask = 0;

    [Tooltip("SphereCast radius for obstacle detection — roughly half the " +
             "thickest cable bundle width.  The default of 0.01 (1 cm) works " +
             "for most 1:1-scale models.")]
    public float cableRadius = 0.01f;

    [Tooltip("Extra clearance beyond the obstacle surface so the cable " +
             "floats free rather than sliding along the part.")]
    public float clearance = 0.015f;

    [Tooltip("Recursion depth per segment.  3–4 handles typical PC layouts.  " +
             "Raise only if you still see clipping through very thin parts.")]
    [Range(1, 6)]
    public int solverDepth = 4;

    [Tooltip("Uncheck to fall back to raw waypoints only (handy for debugging).")]
    public bool enableObstacleAvoidance = true;

    // ─────────────────────────────────────────────────────────────────────────
    //  UNITY LIFECYCLE
    // ─────────────────────────────────────────────────────────────────────────
    void Awake() { Instance = this; }

    // ─────────────────────────────────────────────────────────────────────────
    //  PUBLIC API
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns world-space waypoints for <paramref name="connectorType"/>,
    /// with the actual port positions inserted at start and end.
    /// Falls back to a straight two-point path when no CableRoute is defined.
    ///
    /// If enableObstacleAvoidance is true and obstacleMask is not empty,
    /// the path is then refined by CablePathSolver to avoid scene geometry.
    /// </summary>
    public Vector3[] GetRoute(string connectorType, Vector3 startWorld, Vector3 endWorld)
    {
        Debug.Log($"GetRoute called for: '{connectorType}' | Routes registered: {routes.Count}");
        CableRoute route = routes.Find(r => r.connectorType == connectorType);
        Debug.Log($"Route found: {route != null}");

        Vector3[] rawPath;
        if (route == null || route.waypoints == null || route.waypoints.Length == 0)
        {
            rawPath = new Vector3[] { startWorld, endWorld };
        }
        else
        {
            var path = new List<Vector3>();
            path.Add(startWorld);
            foreach (var wp in route.waypoints)
                if (wp != null) path.Add(wp.position);
            path.Add(endWorld);
            rawPath = path.ToArray();
        }

        // ── 2. Obstacle-avoidance pass ───────────────────────────────────────
        // CablePathSolver refines each segment with SphereCasts so the wire
        // routes over/around any collider on obstacleMask rather than through it.
        if (enableObstacleAvoidance && obstacleMask != 0)
        {
            rawPath = CablePathSolver.Solve(
                rawPath,
                cableRadius,
                obstacleMask,
                clearance,
                solverDepth);
        }

        return rawPath;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  EDITOR VISUALIZATION
    // ─────────────────────────────────────────────────────────────────────────
#if UNITY_EDITOR
    [Header("Editor Debug")]
    [Tooltip("Show the solver's refined path in the Scene view (Editor only).")]
    public bool showSolvedPath = true;

    [Tooltip("A sample start/end to preview the solver result in the Scene view.")]
    public Transform debugStart;
    public Transform debugEnd;
    public string    debugConnectorType = "24pin";

    void OnDrawGizmosSelected()
    {
        if (!showSolvedPath || debugStart == null || debugEnd == null) return;

        Vector3[] solved = GetRoute(
            debugConnectorType,
            debugStart.position,
            debugEnd.position);

        if (solved == null || solved.Length < 2) return;

        Gizmos.color = Color.green;
        for (int i = 0; i < solved.Length - 1; i++)
        {
            Gizmos.DrawLine(solved[i], solved[i + 1]);
            Gizmos.DrawSphere(solved[i], cableRadius);
        }
        Gizmos.DrawSphere(solved[solved.Length - 1], cableRadius);
    }
#endif
}
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Static utility that refines a cable waypoint path to avoid scene colliders.
///
/// HOW IT WORKS
/// ────────────
/// For each segment A → B a SphereCast is fired along the wire.
/// If a PC component is hit, a detour point is injected just above/outside
/// the obstacle surface (using the hit normal + a slight upward bias, which
/// matches how real PC cable management works: cables tend to go over things).
/// The algorithm recurses on both sub-segments up to maxDepth times.
///
/// LAYER SETUP (do this once in your project)
/// ──────────────────────────────────────────
/// 1. In Edit ▸ Project Settings ▸ Tags and Layers, create a new layer called
///    "PCObstacle" (or any name you like).
/// 2. On every solid PC component mesh that a cable should NOT pass through
///    (GPU body, RAM sticks, PSU shroud, motherboard PCB, etc.) set its
///    Collider's GameObject layer to "PCObstacle".
///    • Wire ports (InspectableItem.isWirePort) should stay on InspectLayer —
///      do NOT put them on PCObstacle, or the solver will try to route around
///      the connectors it is supposed to reach.
///    • Existing WireCollider GameObjects are fine on PCObstacle; cables routing
///      around already-placed wires is realistic.
/// 3. On the CableRouteManager inspector, set obstacleMask = PCObstacle.
///
/// NOTE: During inspection the PC case is moved to a "void" anchor at -1000 Y.
/// Physics casts work in world space, so this is fully transparent to the solver.
/// </summary>
public static class CablePathSolver
{
    // ─────────────────────────────────────────────────────────────────────────
    //  PUBLIC API
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Refine <paramref name="rawPath"/> so no segment passes through a collider
    /// on <paramref name="obstacleLayer"/>.
    /// </summary>
    /// <param name="rawPath">World-space waypoints (from CableRouteManager).</param>
    /// <param name="cableRadius">SphereCast radius — roughly half the cable-bundle width.</param>
    /// <param name="obstacleLayer">LayerMask for solid PC components.</param>
    /// <param name="clearance">Extra gap added beyond the obstacle surface.</param>
    /// <param name="maxDepth">Recursion limit per segment (3-4 is plenty).</param>
    public static Vector3[] Solve(
        Vector3[] rawPath,
        float cableRadius,
        LayerMask obstacleLayer,
        float clearance = 0.015f,
        int maxDepth = 4)
    {
        if (rawPath == null || rawPath.Length < 2) return rawPath;

        var result = new List<Vector3>();
        result.Add(rawPath[0]);

        for (int i = 0; i < rawPath.Length - 1; i++)
        {
            var seg = SolveSegment(
                rawPath[i], rawPath[i + 1],
                cableRadius, clearance, obstacleLayer, maxDepth);

            // seg always starts with rawPath[i], which we already added.
            for (int j = 1; j < seg.Count; j++)
                result.Add(seg[j]);
        }

        return result.ToArray();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  INTERNALS
    // ─────────────────────────────────────────────────────────────────────────

    static List<Vector3> SolveSegment(
        Vector3 start,
        Vector3 end,
        float radius,
        float clearance,
        LayerMask mask,
        int depth)
    {
        var pts = new List<Vector3> { start };

        // Base cases ──────────────────────────────────────────────────────────
        if (depth <= 0) { pts.Add(end); return pts; }
        Vector3 dir = end - start;
        float dist = dir.magnitude;
        if (dist < 0.001f) { pts.Add(end); return pts; }

        Vector3 dirNorm = dir / dist;

        // Cast a sphere along this segment ───────────────────────────────────
        RaycastHit hit;
        bool blocked = Physics.SphereCast(
            start, radius, dirNorm, out hit, dist,
            mask, QueryTriggerInteraction.Ignore);

        if (!blocked) { pts.Add(end); return pts; }

        // Build a detour point just outside the obstacle surface ─────────────
        // Slight upward bias mimics real cable routing: cables go over things.
        Vector3 pushDir = (hit.normal + Vector3.up * 0.4f).normalized;
        if (pushDir.sqrMagnitude < 0.01f) pushDir = Vector3.up;

        // Use half the obstacle's extent so we get fully clear of its far side.
        float pushDist = radius + clearance + hit.collider.bounds.extents.magnitude * 0.5f;
        Vector3 detour = hit.point + pushDir * pushDist;

        // Recurse on both sub-segments ────────────────────────────────────────
        var left = SolveSegment(start, detour, radius, clearance, mask, depth - 1);
        var right = SolveSegment(detour, end, radius, clearance, mask, depth - 1);

        for (int i = 1; i < left.Count; i++) pts.Add(left[i]);
        for (int i = 1; i < right.Count; i++) pts.Add(right[i]);

        return pts;
    }

}

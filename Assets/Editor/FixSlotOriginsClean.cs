using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Fixes Slot_ objects whose Transform is at (0,0,0) but whose
/// mesh geometry sits at an offset (common Blender→Unity import issue).
///
/// How it works:
///   1. Computes the average vertex position (centroid) of each Slot_'s mesh
///   2. Moves the Transform to that world position
///   3. REMOVES the MeshFilter + MeshRenderer (the mesh is decorative —
///      BuildFromData destroys these objects at runtime anyway)
///   4. Preserves child positions so nothing shifts
///
/// USAGE:
///   1. Place this file in Assets/Editor/
///   2. Open your PCCase6 prefab
///   3. Select the ROOT object (PCCase6)
///   4. Click  Tools → Fix Slot Origins (Clean)
///   5. Save the prefab (Ctrl+S)
///
/// After running, slots will be invisible empty GameObjects at the correct
/// positions. You can verify by selecting each Slot_ and checking the
/// move gizmo appears at the right mount point on the case.
/// </summary>
public class FixSlotOriginsClean
{
    [MenuItem("Tools/Fix Slot Origins (Clean)")]
    static void Fix()
    {
        GameObject root = Selection.activeGameObject;
        if (root == null)
        {
            Debug.LogError("[FixSlots] Select the root PC case object first!");
            return;
        }

        int fixedCount = 0;
        int skippedCount = 0;

        Transform[] allChildren = root.GetComponentsInChildren<Transform>(true);

        foreach (Transform slot in allChildren)
        {
            if (!slot.name.StartsWith("Slot_")) continue;

            // Skip slots that already have a non-zero position (already fixed)
            if (slot.localPosition.sqrMagnitude > 0.0001f)
            {
                Debug.Log($"[FixSlots] SKIPPED '{slot.name}' — already positioned at {slot.localPosition}");
                skippedCount++;
                continue;
            }

            // Find mesh data — check the object itself AND its children
            MeshFilter mf = slot.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null)
            {
                mf = slot.GetComponentInChildren<MeshFilter>();
            }

            if (mf == null || mf.sharedMesh == null)
            {
                Debug.LogWarning($"[FixSlots] SKIPPED '{slot.name}' — no mesh found. Position it manually.");
                skippedCount++;
                continue;
            }

            // ── Compute the mesh centroid (average of all vertices) ──
            // This is more accurate than bounds.center for asymmetric meshes
            Vector3[] vertices = mf.sharedMesh.vertices;
            if (vertices.Length == 0)
            {
                Debug.LogWarning($"[FixSlots] SKIPPED '{slot.name}' — mesh has no vertices.");
                skippedCount++;
                continue;
            }

            Vector3 localCentroid = Vector3.zero;
            foreach (Vector3 v in vertices)
                localCentroid += v;
            localCentroid /= vertices.Length;

            // Convert the centroid from the mesh object's local space to world space
            Vector3 worldTarget = mf.transform.TransformPoint(localCentroid);

            // ── Snapshot direct children's world positions ──
            List<(Transform child, Vector3 pos, Quaternion rot)> childSnapshot
                = new List<(Transform, Vector3, Quaternion)>();

            foreach (Transform child in slot)
            {
                childSnapshot.Add((child, child.position, child.rotation));
            }

            // ── Move the slot transform to the mesh centroid ──
            Undo.RecordObject(slot, "Fix Slot Origin");
            slot.position = worldTarget;

            // ── Restore children to their original world positions ──
            foreach (var (child, pos, rot) in childSnapshot)
            {
                Undo.RecordObject(child, "Fix Slot Origin - Child");
                child.position = pos;
                child.rotation = rot;
            }

            // ── Remove the mesh components (they're just visual markers) ──
            // The Slot_ object gets Destroy()'d at runtime by BuildFromData anyway,
            // so the mesh serves no purpose. Removing it prevents the
            // "displaced mesh" visual in the editor.
            MeshRenderer mr = slot.GetComponent<MeshRenderer>();
            MeshFilter slotMf = slot.GetComponent<MeshFilter>();

            if (mr != null) { Undo.DestroyObjectImmediate(mr); }
            if (slotMf != null) { Undo.DestroyObjectImmediate(slotMf); }

            Debug.Log($"[FixSlots] FIXED '{slot.name}': localPos → {slot.localPosition}  |  mesh removed");
            fixedCount++;
        }

        Debug.Log($"[FixSlots] Done! Fixed {fixedCount} slots, skipped {skippedCount}.");

        if (fixedCount > 0)
        {
            EditorUtility.SetDirty(root);
            Debug.Log("[FixSlots] Save the prefab with Ctrl+S!");
        }
    }
}

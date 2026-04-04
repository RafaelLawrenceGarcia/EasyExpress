using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// One-click fix for Slot_ objects whose Transform is at (0,0,0)
/// but whose mesh geometry is at an offset position.
/// 
/// USAGE:
/// 1. Place this file in Assets/Editor/
/// 2. Open your PCCase6 prefab
/// 3. Select the ROOT object (PCCase6)
/// 4. Click  Tools → Fix Slot Origins
/// 5. Check the Console for results, then save the prefab (Ctrl+S)
/// </summary>
public class FixSlotOrigins
{
    [MenuItem("Tools/Fix Slot Origins")]
    static void Fix()
    {
        GameObject root = Selection.activeGameObject;
        if (root == null)
        {
            Debug.LogError("[FixSlotOrigins] Select the root PC case object first!");
            return;
        }

        int fixedCount = 0;
        int skippedCount = 0;

        Transform[] allChildren = root.GetComponentsInChildren<Transform>(true);

        foreach (Transform slot in allChildren)
        {
            if (!slot.name.StartsWith("Slot_")) continue;

            // Try to get a mesh to compute the real position
            MeshFilter mf = slot.GetComponent<MeshFilter>();
            Renderer rend = slot.GetComponent<Renderer>();

            Vector3 targetWorldPos;

            if (rend != null)
            {
                // Use the renderer bounds center (most reliable visual center)
                targetWorldPos = rend.bounds.center;
            }
            else if (mf != null && mf.sharedMesh != null)
            {
                // Fallback: compute from mesh bounds
                targetWorldPos = slot.TransformPoint(mf.sharedMesh.bounds.center);
            }
            else
            {
                // No mesh — skip (user must position manually)
                Debug.LogWarning($"[FixSlotOrigins] SKIPPED '{slot.name}' — no mesh found. Position manually.");
                skippedCount++;
                continue;
            }

            // Check if it actually needs fixing (already positioned = skip)
            if (slot.localPosition.sqrMagnitude > 0.0001f)
            {
                Debug.Log($"[FixSlotOrigins] SKIPPED '{slot.name}' — already has a non-zero position: {slot.localPosition}");
                skippedCount++;
                continue;
            }

            // ── Save direct children's world positions before moving the parent ──
            List<(Transform child, Vector3 worldPos, Quaternion worldRot)> childSnapshot
                = new List<(Transform, Vector3, Quaternion)>();

            foreach (Transform child in slot)
            {
                childSnapshot.Add((child, child.position, child.rotation));
            }

            // ── Move the slot transform to the mesh center ──
            Vector3 oldWorld = slot.position;
            Undo.RecordObject(slot, "Fix Slot Origin");
            slot.position = targetWorldPos;

            // ── Restore children to their original world positions ──
            foreach (var (child, pos, rot) in childSnapshot)
            {
                Undo.RecordObject(child, "Fix Slot Origin - Child");
                child.position = pos;
                child.rotation = rot;
            }

            Debug.Log($"[FixSlotOrigins] FIXED '{slot.name}': "
                    + $"localPos {Vector3.zero} → {slot.localPosition}  "
                    + $"(world {oldWorld} → {targetWorldPos})");
            fixedCount++;
        }

        Debug.Log($"[FixSlotOrigins] Done! Fixed {fixedCount} slots, skipped {skippedCount}.");

        if (fixedCount > 0)
        {
            EditorUtility.SetDirty(root);
            Debug.Log("[FixSlotOrigins] Don't forget to SAVE the prefab (Ctrl+S)!");
        }
    }
}

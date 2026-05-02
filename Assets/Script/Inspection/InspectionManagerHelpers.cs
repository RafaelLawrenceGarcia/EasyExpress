// ============================================================
//  InspectionManager.Helpers.cs  (partial class 6/6)
//  Layers, animations, spawn helpers, case shell detection
// ============================================================
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public partial class InspectionManager
{
    // ─── Layer Management ────────────────────────────────────────

    internal void SetLayerRecursively(GameObject obj, int newLayer)
    {
        obj.layer = newLayer;
        foreach (Transform child in obj.transform) SetLayerRecursively(child.gameObject, newLayer);
    }

    void SaveAndSetLayers(GameObject obj, int newLayer)
    {
        if (!originalLayerCache.ContainsKey(obj)) originalLayerCache[obj] = obj.layer;
        obj.layer = newLayer;
        foreach (Transform child in obj.transform) SaveAndSetLayers(child.gameObject, newLayer);
    }

    void RestoreLayers(GameObject obj)
    {
        obj.layer = originalLayerCache.ContainsKey(obj) ? originalLayerCache[obj] : LayerMask.NameToLayer("Default");
        foreach (Transform child in obj.transform) RestoreLayers(child.gameObject);
    }

    // ─── Case Shell Detection ────────────────────────────────────

    bool IsCaseShellObject(GameObject obj)
    {
        string[] caseNames = { "case", "case.001", "case.003", "psucase" };
        Transform current = obj.transform;
        while (current != null)
        {
            foreach (string name in caseNames)
                if (current.name == name) return true;
            current = current.parent;
        }
        return false;
    }

    // ─── Wire Head Spawning ──────────────────────────────────────

    GameObject SpawnWireHead(Transform port, Transform parent)
    {
        if (wireHeadPrefab == null) return null;
        GameObject head = Instantiate(wireHeadPrefab, port.position, port.rotation, parent);
        head.transform.Translate(wireHeadOffset, Space.Self);
        SetLayerRecursively(head, parent.gameObject.layer);
        return head;
    }

    // ─── Removal Animation ───────────────────────────────────────

    IEnumerator AnimateRemovalAndDestroy(GameObject obj)
    {
        foreach (Collider c in obj.GetComponentsInChildren<Collider>()) c.enabled = false;

        Vector3 startPos = obj.transform.position;
        Vector3 startScale = obj.transform.localScale;

        Dictionary<Renderer, Material[]> originalMats = new Dictionary<Renderer, Material[]>();
        foreach (Renderer r in obj.GetComponentsInChildren<Renderer>()) originalMats[r] = r.materials;

        // Phase 1: Pull toward camera with glow
        float pullDuration = 0.3f;
        float elapsed = 0f;
        Vector3 pullDir = (inspectionCamera.transform.position - startPos).normalized;
        Vector3 pullTarget = startPos + pullDir * 0.25f;

        while (elapsed < pullDuration)
        {
            if (obj == null) yield break;
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / pullDuration);
            obj.transform.position = Vector3.Lerp(startPos, pullTarget, t);
            obj.transform.localScale = startScale * (1f + Mathf.Sin(t * Mathf.PI) * 0.08f);
            foreach (var kv in originalMats)
            {
                if (kv.Key == null) continue;
                foreach (Material mat in kv.Key.materials)
                    if (mat.HasProperty("_EmissionColor")) { mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", Color.white * t * 1.5f); }
            }
            yield return null;
        }

        // Phase 2: Fly to corner and shrink
        float flyDuration = 0.35f;
        elapsed = 0f;
        Vector3 flyStart = obj.transform.position;
        Vector3 flyTarget = inspectionCamera.ViewportToWorldPoint(new Vector3(0.1f, 0.1f, 0.3f));
        Vector3 arcMid = Vector3.Lerp(flyStart, flyTarget, 0.5f) + inspectionCamera.transform.up * 0.3f;

        while (elapsed < flyDuration)
        {
            if (obj == null) yield break;
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / flyDuration);
            obj.transform.position = (1 - t) * (1 - t) * flyStart + 2 * (1 - t) * t * arcMid + t * t * flyTarget;
            obj.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t * t);
            foreach (var kv in originalMats)
            {
                if (kv.Key == null) continue;
                foreach (Material mat in kv.Key.materials)
                    if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", Color.white * (1f - t) * 1.5f);
            }
            yield return null;
        }

        if (obj != null) Destroy(obj);
    }

    // ─── Install Animation ───────────────────────────────────────

    IEnumerator AnimateInstall(GameObject obj)
    {
        Vector3 finalPos = obj.transform.position;
        Vector3 finalScale = obj.transform.localScale;
        Quaternion finalRot = obj.transform.rotation;

        Vector3 startPos = inspectionCamera.ViewportToWorldPoint(new Vector3(0.1f, 0.1f, 0.3f));
        Vector3 arcMid = Vector3.Lerp(startPos, finalPos, 0.6f) + inspectionCamera.transform.up * 0.25f;

        obj.transform.position = startPos;
        obj.transform.localScale = finalScale * 0.3f;
        obj.transform.rotation = finalRot;
        foreach (Collider c in obj.GetComponentsInChildren<Collider>()) c.enabled = false;

        // Phase 1: Arc glide
        float glideDuration = 0.4f;
        float elapsed = 0f;
        while (elapsed < glideDuration)
        {
            if (obj == null) yield break;
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / glideDuration);
            obj.transform.position = (1 - t) * (1 - t) * startPos + 2 * (1 - t) * t * arcMid + t * t * finalPos;
            obj.transform.localScale = Vector3.Lerp(finalScale * 0.3f, finalScale * 1.1f, t);
            obj.transform.rotation = Quaternion.Slerp(obj.transform.rotation, finalRot, t * 3f);
            yield return null;
        }

        // Phase 2: Snap settle
        float snapDuration = 0.18f;
        elapsed = 0f;
        while (elapsed < snapDuration)
        {
            if (obj == null) yield break;
            elapsed += Time.deltaTime;
            float t = elapsed / snapDuration;
            float scaleX = 1f + Mathf.Sin(t * Mathf.PI) * -0.1f;
            float scaleY = 1f + Mathf.Sin(t * Mathf.PI) * 0.15f;
            float scaleZ = 1f + Mathf.Sin(t * Mathf.PI) * -0.1f;
            obj.transform.localScale = new Vector3(finalScale.x * scaleX, finalScale.y * scaleY, finalScale.z * scaleZ);
            obj.transform.position = finalPos;
            obj.transform.rotation = Quaternion.Lerp(obj.transform.rotation, finalRot, t * 10f);
            yield return null;
        }

        if (obj != null)
        {
            obj.transform.position = finalPos;
            obj.transform.localScale = finalScale;
            obj.transform.rotation = finalRot;
            foreach (Collider c in obj.GetComponentsInChildren<Collider>()) c.enabled = true;
        }
    }
}
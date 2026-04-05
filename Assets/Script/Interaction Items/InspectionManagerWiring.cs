// ============================================================
//  InspectionManager.Wiring.cs  (partial class 5/6)
//  Wire port handling, cable drawing, pre-built wire connect/disconnect
// ============================================================
using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.Rendering;
using System.Collections;
using System.Collections.Generic;

public partial class InspectionManager
{
    // ─── Pre-built Wire Connect/Disconnect ───────────────────────

    void HandlePrebuiltWireConnect(IPrebuiltWire wire)
    {
        if (!RequireScrewdriver()) return;

        if (!wire.IsRequiredComponentInstalled(currentClone.transform))
        {
            string msg = string.IsNullOrEmpty(wire.RequiredPartCategory)
                ? "Required component is not installed."
                : $"Install the {wire.RequiredPartCategory} first.";
            ShowTooltipMessage("Missing Component", msg);
            return;
        }

        if (screwdriverSystem != null)
        {
            Transform screwTarget = wire.ConnectorPort != null ? wire.ConnectorPort.transform : ((Component)wire).transform;
            isConfirmingRemoval = true;
            screwdriverSystem.onCancelled = () => { isConfirmingRemoval = false; };
            screwdriverSystem.BeginUnscrewing(screwTarget, () =>
            {
                isConfirmingRemoval = false;
                wire.Connect(currentClone.transform);
                if (wire.WireMeshRoot != null) SetLayerRecursively(wire.WireMeshRoot, currentClone.layer);
                ShowTooltipMessage("Connected!", wire.IsPowerCord ? "Power cord plugged in." : $"{wire.WireName} connected.");
            });
        }
        else
        {
            wire.Connect(currentClone.transform);
            if (wire.WireMeshRoot != null) SetLayerRecursively(wire.WireMeshRoot, currentClone.layer);
        }
    }

    void HandlePrebuiltWireDisconnect(IPrebuiltWire wire)
    {
        if (!RequireScrewdriver()) return;

        if (screwdriverSystem != null)
        {
            Transform screwTarget = wire.ConnectorPort != null ? wire.ConnectorPort.transform : ((Component)wire).transform;
            isConfirmingRemoval = true;
            screwdriverSystem.onCancelled = () => { isConfirmingRemoval = false; };
            screwdriverSystem.BeginUnscrewing(screwTarget, () =>
            {
                isConfirmingRemoval = false;
                wire.Disconnect(currentClone.transform);
                ShowTooltipMessage("Disconnected", wire.IsPowerCord ? "Power cord unplugged." : $"{wire.WireName} disconnected.");
            });
        }
        else wire.Disconnect(currentClone.transform);
    }

    void AutoDisconnectPrebuiltWires(InspectableItem removedPart)
    {
        if (currentClone == null) return;
        foreach (MonoBehaviour mb in currentClone.GetComponentsInChildren<MonoBehaviour>(true))
        {
            IPrebuiltWire wire = mb as IPrebuiltWire;
            if (wire == null || !wire.IsConnected) continue;
            if (!string.IsNullOrEmpty(wire.RequiredPartCategory) && wire.RequiredPartCategory == removedPart.partCategory)
            {
                wire.Disconnect(currentClone.transform);
                Debug.Log($"[PrebuiltWire] Auto-disconnected '{wire.WireName}' because {removedPart.itemName} was removed.");
            }
        }
    }

    // ─── Dynamic Wire Port Handling ──────────────────────────────

    void HandleWirePort(InspectableItem clickedPort)
    {
        if (!isWiring)
        {
            if (clickedPort.isOccupied) return;
            StartWiring(clickedPort);
        }
        else
        {
            if (clickedPort == wireStartPortItem) { CancelWiring(); return; }
            if (!IsWireCompatible(wireStartPortItem, clickedPort) || clickedPort.isOccupied) return;
            CommitWire(wireStartPortItem, clickedPort);
        }
    }

    void StartWiring(InspectableItem port)
    {
        isWiring = true;
        wireStartPortItem = port;
        wireStartTransform = port.transform;
        activeWireConnectorType = port.connectorType;

        if (wireHeadPrefab != null)
            activeWireHead = SpawnWireHead(port.transform, currentClone.transform);
        else
        {
            activeWireHead = new GameObject("DynamicCable_Head");
            activeWireHead.transform.SetParent(currentClone.transform);
            activeWireHead.transform.position = port.transform.position;
            activeWireHead.transform.rotation = port.transform.rotation;
        }

        activeWireLines.Clear();
        CableTypeProfile profile = CableProfile.Instance != null ? CableProfile.Instance.Get(port.connectorType) : null;
        int strandCount = profile != null ? profile.strandCount : 1;

        for (int s = 0; s < strandCount; s++)
        {
            GameObject strandGO = new GameObject($"Strand_{s}");
            strandGO.transform.SetParent(activeWireHead.transform);
            SetLayerRecursively(strandGO, currentClone.layer);

            LineRenderer lr = strandGO.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.numCornerVertices = 5;
            lr.numCapVertices = 5;
            lr.shadowCastingMode = ShadowCastingMode.Off;

            float w = profile != null ? profile.strandWidth : 0.02f;
            lr.startWidth = w; lr.endWidth = w;

            Material mat = cableMaterial != null ? new Material(cableMaterial) : new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Sprites/Default"));
            Color col = (profile != null && profile.strandColors != null && profile.strandColors.Length > 0)
                ? profile.strandColors[s % profile.strandColors.Length] : Color.white;
            mat.color = col;
            if (mat.HasProperty("_EmissionColor")) { mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", col * 0.4f); }
            lr.material = mat;
            activeWireLines.Add(lr);
        }

        activeWireLine = activeWireLines.Count > 0 ? activeWireLines[0] : null;
        HighlightCompatiblePorts(port.connectorType, port.isPSUPort);
    }

    bool IsWireCompatible(InspectableItem portA, InspectableItem portB)
    {
        return portA.connectorType == portB.connectorType && portA.isPSUPort != portB.isPSUPort;
    }

    void CommitWire(InspectableItem startPort, InspectableItem endPort)
    {
        isWiring = false;
        ClearPortHighlights();

        // Determine device vs PSU port
        InspectableItem devicePort, psuPort;
        if (startPort.placeHeadHere && !endPort.placeHeadHere) { devicePort = startPort; psuPort = endPort; }
        else if (endPort.placeHeadHere && !startPort.placeHeadHere) { devicePort = endPort; psuPort = startPort; }
        else { devicePort = startPort.isPSUPort ? endPort : startPort; psuPort = startPort.isPSUPort ? startPort : endPort; }

        activeRibbonDir = devicePort.ribbonAxis.normalized;
        if (activeRibbonDir.sqrMagnitude < 0.001f) activeRibbonDir = Vector3.up;
        activeEndRibbonDir = psuPort.ribbonAxis.normalized;
        if (activeEndRibbonDir.sqrMagnitude < 0.001f) activeEndRibbonDir = Vector3.right;

        Vector3 startPos = devicePort.transform.position;
        Vector3 endPos = psuPort.transform.position;
        float cLen = Vector3.Distance(startPos, endPos);
        float sag = Mathf.Max(cLen * cableSag, 0.05f);

        Vector3[] finalPath;
        if (CableRouteManager.Instance != null) finalPath = CableRouteManager.Instance.GetRoute(startPort.connectorType, startPos, endPos);
        else
        {
            Vector3 behindBoard = startPos - devicePort.transform.forward * (cLen * 0.25f);
            behindBoard.y = startPos.y - sag * 0.5f;
            Vector3 risePoint = new Vector3(endPos.x, endPos.y - cLen * 0.25f, endPos.z);
            finalPath = new Vector3[] { startPos, behindBoard, risePoint, endPos };
        }

        List<LineRenderer> lrSnapshot = new List<LineRenderer>(activeWireLines);
        string snapConnType = activeWireConnectorType;
        foreach (LineRenderer lr in lrSnapshot) if (lr != null) lr.transform.SetParent(currentClone.transform);
        if (activeWireHead != null) { Destroy(activeWireHead); activeWireHead = null; }

        if (devicePort.wireHead != null) devicePort.wireHead.SetActive(true);
        else devicePort.wireHead = SpawnWireHead(devicePort.transform, currentClone.transform);
        if (psuPort.wireHead != null) psuPort.wireHead.SetActive(true);
        else psuPort.wireHead = SpawnWireHead(psuPort.transform, currentClone.transform);

        // Wire collider
        GameObject wireColliderGO = new GameObject("WireCollider");
        wireColliderGO.transform.SetParent(currentClone.transform);
        wireColliderGO.transform.position = Vector3.Lerp(startPos, endPos, 0.5f);
        wireColliderGO.transform.LookAt(endPos);
        SetLayerRecursively(wireColliderGO, currentClone.layer);

        BoxCollider lineCol = wireColliderGO.AddComponent<BoxCollider>();
        lineCol.size = new Vector3(0.06f, 0.06f, Vector3.Distance(startPos, endPos));

        InspectableItem wireItem = wireColliderGO.AddComponent<InspectableItem>();
        wireItem.itemName = $"{startPort.connectorType} Cable";
        wireItem.itemDescription = $"Connects {startPort.itemName} to {endPort.itemName}";
        wireItem.isRemovable = true;

        startPort.isOccupied = true; startPort.connectedTo = endPort; startPort.attachedWire = wireColliderGO;
        endPort.isOccupied = true; endPort.connectedTo = startPort; endPort.attachedWire = wireColliderGO;
        AddWireBlocker(startPort, wireItem);
        AddWireBlocker(endPort, wireItem);

        var wireCleaner = wireColliderGO.AddComponent<WireCleanup>();
        wireCleaner.portA = startPort; wireCleaner.portB = endPort;

        DrawCommittedCurve(lrSnapshot, snapConnType, startPos, endPos, sag, activeRibbonDir, activeEndRibbonDir);
        if (activeSnapCoroutine != null) StopCoroutine(activeSnapCoroutine);
        activeSnapCoroutine = StartCoroutine(AnimateCableSnap(lrSnapshot, snapConnType, finalPath, 0.4f, activeRibbonDir, activeEndRibbonDir));

        wireStartPortItem = null; wireStartTransform = null; activeWireConnectorType = "";
    }

    void AddWireBlocker(InspectableItem port, InspectableItem wireItem)
    {
        if (port.parentComponent == null) return;
        if (port.parentComponent.blockingParts == null) port.parentComponent.blockingParts = new List<InspectableItem>();
        port.parentComponent.blockingParts.Add(wireItem);
    }

    void CancelWiring()
    {
        isWiring = false;
        ClearPortHighlights();
        if (activeWireHead != null) { Destroy(activeWireHead); activeWireHead = null; }
        activeWireLines.Clear();
        activeWireLine = null; wireStartPortItem = null; wireStartTransform = null; activeWireConnectorType = "";
    }

    // ─── Port Highlights ─────────────────────────────────────────

    void HighlightCompatiblePorts(string connType, bool startIsPSU)
    {
        foreach (InspectableItem port in allPorts)
        {
            if (port == wireStartPortItem) continue;
            Renderer rend = port.GetComponentInChildren<Renderer>();
            if (rend == null) continue;

            bool compatible = port.connectorType == connType && port.isPSUPort != startIsPSU && !port.isOccupied;
            Material mat = compatible ? validPortMaterial : invalidPortMaterial;
            if (mat != null)
            {
                if (!originalMaterialCache.ContainsKey(rend)) originalMaterialCache.Add(rend, rend.sharedMaterials);
                Material[] currentMats = rend.sharedMaterials;
                Material[] newMats = new Material[currentMats.Length + 1];
                for (int i = 0; i < currentMats.Length; i++) newMats[i] = currentMats[i];
                newMats[newMats.Length - 1] = mat;
                rend.sharedMaterials = newMats;
            }
        }
    }

    void ClearPortHighlights()
    {
        foreach (var kv in originalMaterialCache) if (kv.Key != null) kv.Key.sharedMaterials = kv.Value;
        originalMaterialCache.Clear();
        lastHitObject = null;
    }

    // ─── Wire Drawing ────────────────────────────────────────────

    void HandleWireDrawing()
    {
        if (!isWiring || activeWireLines.Count == 0 || wireStartTransform == null) return;

        InspectableItem hoveredPort = lastHitObject != null ? lastHitObject.GetComponent<InspectableItem>() : null;
        if (hoveredPort != null && hoveredPort.isWirePort && hoveredPort != wireStartPortItem)
            DrawDragSpline(wireStartTransform.position, hoveredPort.transform.position);
        else
        {
            Ray ray = inspectionCamera.ScreenPointToRay(Input.mousePosition);
            DrawDragSpline(wireStartTransform.position, ray.GetPoint(currentDistance * 0.7f));
        }
    }

    // ─── Spline Utilities ────────────────────────────────────────

    Spline BuildSpline(Vector3[] waypoints)
    {
        var spline = new Spline();
        foreach (var wp in waypoints) spline.Add(new BezierKnot(wp), TangentMode.AutoSmooth);
        return spline;
    }

    Vector3[] SampleSpline(Spline spline, int resolution)
    {
        var pts = new Vector3[resolution];
        for (int i = 0; i < resolution; i++) pts[i] = spline.EvaluatePosition(i / (float)(resolution - 1));
        return pts;
    }

    Vector3 StrandOffset(int strandIndex, int strandCount, float totalWidth, Vector3 ribbonDir)
    {
        float t = strandCount > 1 ? (strandIndex / (float)(strandCount - 1)) - 0.5f : 0f;
        return ribbonDir * (t * totalWidth);
    }

    void DrawDragSpline(Vector3 startWorld, Vector3 endWorld)
    {
        if (activeWireLines == null || activeWireLines.Count == 0) return;

        float cableLen = Vector3.Distance(startWorld, endWorld);
        float sag = Mathf.Max(cableLen * cableSag, 0.05f);
        Vector3 pushDir = inspectionCamera.transform.position - startWorld;
        pushDir.y = 0f;
        if (pushDir.sqrMagnitude > 0.001f) pushDir.Normalize();

        if (CableRouteManager.Instance != null && CableRouteManager.Instance.enableObstacleAvoidance
            && CableRouteManager.Instance.obstacleMask != 0 && cableLen > 0.001f)
        {
            RaycastHit hit;
            if (Physics.SphereCast(startWorld, CableRouteManager.Instance.cableRadius,
                (endWorld - startWorld).normalized, out hit, cableLen,
                CableRouteManager.Instance.obstacleMask, QueryTriggerInteraction.Ignore))
                pushDir = (hit.normal + Vector3.up * 0.4f).normalized;
        }

        Vector3 mid1 = Vector3.Lerp(startWorld, endWorld, 0.33f) + pushDir * (cableLen * 0.35f) + Vector3.down * sag;
        Vector3 mid2 = Vector3.Lerp(startWorld, endWorld, 0.66f) + pushDir * (cableLen * 0.35f) + Vector3.down * sag;

        CableTypeProfile profile = CableProfile.Instance?.Get(activeWireConnectorType);
        int strandCount = activeWireLines.Count;
        float strandSpacing = profile != null ? profile.strandSpacing : 0.003f;
        float totalWidth = strandSpacing * (strandCount - 1);

        Vector3 startRibbonDir = wireStartPortItem != null ? wireStartPortItem.ribbonAxis.normalized : inspectionCamera.transform.up;
        if (startRibbonDir.sqrMagnitude < 0.001f) startRibbonDir = Vector3.up;
        Vector3 endRibbonDir = startRibbonDir;
        if (lastHitObject != null)
        {
            InspectableItem hovered = lastHitObject.GetComponent<InspectableItem>();
            if (hovered != null && hovered.isWirePort && hovered != wireStartPortItem)
            { endRibbonDir = hovered.ribbonAxis.normalized; if (endRibbonDir.sqrMagnitude < 0.001f) endRibbonDir = Vector3.right; }
        }

        Spline baseSpline = BuildSpline(new Vector3[] { startWorld, mid1, mid2, endWorld });
        Vector3[] basePts = SampleSpline(baseSpline, cableResolution);

        for (int s = 0; s < strandCount; s++)
        {
            LineRenderer lr = activeWireLines[s];
            if (lr == null) continue;
            lr.useWorldSpace = true;
            lr.positionCount = cableResolution;
            for (int i = 0; i < cableResolution; i++)
            {
                float t = i / (float)(cableResolution - 1);
                Vector3 ribbonDir = Vector3.Slerp(startRibbonDir, endRibbonDir, t).normalized;
                lr.SetPosition(i, basePts[i] + StrandOffset(s, strandCount, totalWidth, ribbonDir));
            }
        }
    }

    void DrawCommittedCurve(List<LineRenderer> lines, string connType, Vector3 startPos, Vector3 endPos,
                            float sag, Vector3 startRibbonDir, Vector3 endRibbonDir)
    {
        if (lines == null || lines.Count == 0) return;

        CableTypeProfile profile = CableProfile.Instance?.Get(connType);
        int strandCount = lines.Count;
        float strandSpacing = profile != null ? profile.strandSpacing : 0.003f;
        float totalWidth = strandSpacing * (strandCount - 1);
        float cableLen = Vector3.Distance(startPos, endPos);
        float useSag = Mathf.Max(cableLen * cableSag, sag);

        Vector3 mid = new Vector3(Mathf.Lerp(startPos.x, endPos.x, 0.3f), startPos.y - useSag, Mathf.Lerp(startPos.z, endPos.z, 0.3f));
        Spline baseSpline = BuildSpline(new Vector3[] { startPos, mid, endPos });
        Vector3[] basePts = SampleSpline(baseSpline, cableResolution);

        for (int s = 0; s < strandCount; s++)
        {
            LineRenderer lr = lines[s];
            if (lr == null) continue;
            lr.useWorldSpace = true;
            lr.positionCount = cableResolution;
            for (int i = 0; i < cableResolution; i++)
            {
                float t = i / (float)(cableResolution - 1);
                Vector3 ribbonDir = Vector3.Slerp(startRibbonDir, endRibbonDir, t).normalized;
                lr.SetPosition(i, basePts[i] + StrandOffset(s, strandCount, totalWidth, ribbonDir));
            }
        }
    }

    IEnumerator AnimateCableSnap(List<LineRenderer> lrSnapshot, string connType,
        Vector3[] routePositions, float duration, Vector3 startRibbonDir, Vector3 endRibbonDir)
    {
        if (lrSnapshot == null || lrSnapshot.Count == 0) yield break;

        int res = cableResolution;
        CableTypeProfile profile = CableProfile.Instance?.Get(connType);
        int strandCount = lrSnapshot.Count;
        float strandSpacing = profile != null ? profile.strandSpacing : 0.003f;
        float totalWidth = strandSpacing * (strandCount - 1);

        Vector3[][] fromPositions = new Vector3[strandCount][];
        for (int s = 0; s < strandCount; s++)
        {
            fromPositions[s] = new Vector3[res];
            LineRenderer lr = lrSnapshot[s];
            if (lr != null && lr.positionCount == res) lr.GetPositions(fromPositions[s]);
            else if (lr != null) for (int i = 0; i < res; i++) fromPositions[s][i] = lr.transform.position;
        }

        Spline baseRouteSpline = BuildSpline(routePositions);
        Vector3[] baseRoutePts = SampleSpline(baseRouteSpline, res);

        Vector3[][] targetPositions = new Vector3[strandCount][];
        for (int s = 0; s < strandCount; s++)
        {
            targetPositions[s] = new Vector3[res];
            for (int i = 0; i < res; i++)
            {
                float t = i / (float)(res - 1);
                Vector3 ribbonDir = Vector3.Slerp(startRibbonDir, endRibbonDir, t).normalized;
                targetPositions[s][i] = baseRoutePts[i] + StrandOffset(s, strandCount, totalWidth, ribbonDir);
            }
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float eased = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            for (int s = 0; s < strandCount; s++)
            {
                LineRenderer lr = lrSnapshot[s];
                if (lr == null) continue;
                lr.positionCount = res;
                for (int i = 0; i < res; i++) lr.SetPosition(i, Vector3.Lerp(fromPositions[s][i], targetPositions[s][i], eased));
            }
            yield return null;
        }

        for (int s = 0; s < strandCount; s++)
        {
            LineRenderer lr = lrSnapshot[s];
            if (lr == null) continue;
            lr.positionCount = res;
            for (int i = 0; i < res; i++) lr.SetPosition(i, targetPositions[s][i]);
        }
    }
}
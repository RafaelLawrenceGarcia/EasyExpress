using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using TMPro;
using System.Collections.Generic;
using System.Collections;

public class InspectionManager : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────────
    //  INSPECTOR REFERENCES
    // ─────────────────────────────────────────────────────────────────────────────
    [Header("UI Reference")]
    public GameObject controlsUI;
    public GameObject infoPanel;
    public GameObject gameplayUI;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descText;

    [Header("Tooltip Reference")]
    public GameObject tooltipPanel;
    public TextMeshProUGUI tooltipTitle;
    public TextMeshProUGUI tooltipBody;
    public Material highlightMaterial;
    public Material validPortMaterial;
    public Material invalidPortMaterial;

    [Header("Cameras")]
    public Camera mainCamera;
    public Camera inspectionCamera;

    [Header("Blur Settings")]
    public Volume blurVolume;

    [Header("Scripts & Animators to Disable")]
    public GTAMovement playerMovement;
    public OrbitCamera cameraScript;
    public PlayerInteract interactScript;
    public Animator playerAnimator;
    public GameObject playerRootObject;

    [Header("Controls")]
    public float orbitSpeed = 5f;
    public float panSpeed = 0.5f;
    public float zoomSpeed = 5f;
    public float smoothTime = 10f;

    [Header("Wiring System")]
    [Tooltip("Connector mesh that spawns at both ends of a wire.")]
    public GameObject wireHeadPrefab;
    public Material cableMaterial;
    [Tooltip("Base sag multiplier. 0.15-0.3 looks natural.")]
    public float cableSag = 0.2f;
    [Range(0f, 2f)]
    public float sagLengthScale = 0.6f;
    [Tooltip("Higher = smoother curve.")]
    public int cableResolution = 20;

    // ─────────────────────────────────────────────────────────────────────────────
    //  PRIVATE STATE
    // ─────────────────────────────────────────────────────────────────────────────
    private List<LineRenderer> activeWireLines       = new List<LineRenderer>();
    private string             activeWireConnectorType = "";
    private LineRenderer       activeWireLine;

    private InspectableItem wireStartPortItem;
    private Transform       wireStartTransform;
    private GameObject      activeWireHead;
    private GameObject      activeWireTail;

    private Coroutine activeSnapCoroutine;

    private GameObject currentClone;
    private GameObject voidAnchor;
    private bool isInspecting = false;
    private bool isWiring     = false;
    private bool isPCOn       = false;

    private Vector3 focusPoint;
    private Vector3 targetFocusPoint;
    private float   currentDistance;
    private float   targetDistance;
    private Vector2 orbitAngles;
    private Vector2 targetOrbitAngles;

    private GameObject lastHitObject;
    private Dictionary<Renderer, Material[]> originalMaterialCache = new Dictionary<Renderer, Material[]>();
    private float optimalDistance;

    private List<InspectableItem> allPorts = new List<InspectableItem>();

    // ─────────────────────────────────────────────────────────────────────────────
    //  UNITY LIFECYCLE
    // ─────────────────────────────────────────────────────────────────────────────
    void Start()
    {
        if (blurVolume != null) blurVolume.weight = 0;

        voidAnchor = new GameObject("Void_Inspection_Anchor");
        voidAnchor.transform.position = new Vector3(0, -1000, 0);

        if (mainCamera && inspectionCamera)
        {
            var mainCamData    = mainCamera.GetUniversalAdditionalCameraData();
            var overlayCamData = inspectionCamera.GetUniversalAdditionalCameraData();
            overlayCamData.renderType = CameraRenderType.Overlay;

            bool isStacked = false;
            foreach (var cam in mainCamData.cameraStack)
                if (cam == inspectionCamera) { isStacked = true; break; }
            if (!isStacked) mainCamData.cameraStack.Add(inspectionCamera);
        }

        if (inspectionCamera)
        {
            inspectionCamera.transform.parent = null;
            inspectionCamera.gameObject.SetActive(false);
        }

        if (controlsUI)   controlsUI.SetActive(false);
        if (infoPanel)    infoPanel.SetActive(false);
        if (tooltipPanel) tooltipPanel.SetActive(false);
    }

    void Update()
    {
        if (!isInspecting || currentClone == null) return;

        HandleInput();
        ApplyCameraMovement();
        HandleHover();
        HandleClickInteractions();
        HandleWireDrawing();

        if (Input.GetKeyDown(KeyCode.R)) ResetView();
        if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.X))
            StopInspection();
    }

    // ─────────────────────────────────────────────────────────────────────────────
    //  INSPECTION ENTRY / EXIT
    // ─────────────────────────────────────────────────────────────────────────────
    public void Inspect(InspectableItem originalItem)
    {
        isInspecting = true;

        if (blurVolume != null) blurVolume.weight = 1;

        if (controlsUI)   controlsUI.SetActive(true);
        if (infoPanel)    infoPanel.SetActive(true);
        if (gameplayUI)   gameplayUI.SetActive(false);
        if (nameText)     nameText.text = originalItem.itemName;
        if (descText)     descText.text = originalItem.itemDescription;

        if (mainCamera)       mainCamera.gameObject.SetActive(true);
        if (inspectionCamera) inspectionCamera.gameObject.SetActive(true);

        if (playerMovement) playerMovement.enabled = false;
        if (cameraScript)   cameraScript.enabled   = false;
        if (interactScript) interactScript.enabled = false;
        if (playerAnimator) playerAnimator.enabled = false;

        if (playerRootObject)
            foreach (Renderer r in playerRootObject.GetComponentsInChildren<Renderer>())
                r.enabled = false;

        if (voidAnchor == null)
        {
            voidAnchor = new GameObject("Void_Inspection_Anchor");
            voidAnchor.transform.position = new Vector3(0, -1000, 0);
        }

        currentClone = Instantiate(originalItem.gameObject, voidAnchor.transform);
        currentClone.transform.localPosition = Vector3.zero;
        currentClone.transform.localRotation = Quaternion.identity;
        currentClone.transform.localScale    = Vector3.one;

        Destroy(currentClone.GetComponent<Rigidbody>());

        InspectableItem rootScript = currentClone.GetComponent<InspectableItem>();
        if (rootScript != null) Destroy(rootScript);

        Collider mainCollider = currentClone.GetComponent<Collider>();
        if (mainCollider != null) mainCollider.enabled = false;

        SetLayerRecursively(currentClone, LayerMask.NameToLayer("InspectLayer"));

        Bounds bounds = new Bounds(currentClone.transform.position, Vector3.zero);
        foreach (Renderer r in currentClone.GetComponentsInChildren<Renderer>())
            bounds.Encapsulate(r.bounds);
        currentClone.transform.position += voidAnchor.transform.position - bounds.center;

        float objectSize = bounds.extents.magnitude;
        if (objectSize == 0) objectSize = 1f;
        float fov = inspectionCamera.fieldOfView * 0.5f * Mathf.Deg2Rad;
        optimalDistance = (objectSize / Mathf.Sin(fov)) * 2.0f;

        targetDistance    = optimalDistance;
        currentDistance   = optimalDistance;
        targetFocusPoint  = Vector3.zero;
        focusPoint        = Vector3.zero;
        targetOrbitAngles = Vector2.zero;
        orbitAngles       = Vector2.zero;

        allPorts.Clear();
        foreach (var item in currentClone.GetComponentsInChildren<InspectableItem>())
            if (item.isWirePort) allPorts.Add(item);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
    }

    public void StopInspection()
    {
        isInspecting = false;

        CancelWiring();
        ClearPortHighlights();

        if (blurVolume != null) blurVolume.weight = 0;
        ClearHighlight();

        if (currentClone != null) Destroy(currentClone);
        allPorts.Clear();

        if (infoPanel)    infoPanel.SetActive(false);
        if (tooltipPanel) tooltipPanel.SetActive(false);
        if (controlsUI)   controlsUI.SetActive(false);
        if (gameplayUI)   gameplayUI.SetActive(true);

        if (inspectionCamera) inspectionCamera.gameObject.SetActive(false);

        if (playerMovement) playerMovement.enabled = true;
        if (cameraScript)   cameraScript.enabled   = true;
        if (interactScript) interactScript.enabled = true;
        if (playerAnimator) playerAnimator.enabled = true;

        if (playerRootObject)
            foreach (Renderer r in playerRootObject.GetComponentsInChildren<Renderer>())
                r.enabled = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
    }

    // ─────────────────────────────────────────────────────────────────────────────
    //  CLICK INTERACTIONS
    // ─────────────────────────────────────────────────────────────────────────────
    void HandleClickInteractions()
    {
        if (isWiring && Input.GetMouseButtonDown(1)) { CancelWiring(); return; }
        if (!Input.GetMouseButtonDown(0)) return;
        if (lastHitObject == null) return;

        InspectableItem part = lastHitObject.GetComponent<InspectableItem>();
        if (part == null) return;

        if      (part.isRemovable)   TryRemovePart(part);
        else if (part.isPowerButton) TogglePCPower();
        else if (part.isWirePort)    HandleWirePort(part);
    }

    void TryRemovePart(InspectableItem part)
    {
        foreach (InspectableItem blocker in part.blockingParts)
        {
            if (blocker != null)
            {
                Debug.Log($"Cannot remove {part.itemName} - blocked by {blocker.itemName}.");
                return;
            }
        }
        Debug.Log($"Removing {part.itemName}");
        GameObject objToDestroy = lastHitObject;
        ClearHighlight();
        StartCoroutine(AnimateRemovalAndDestroy(objToDestroy));
    }

    // ─────────────────────────────────────────────────────────────────────────────
    //  PORT / WIRING LOGIC
    // ─────────────────────────────────────────────────────────────────────────────
    void HandleWirePort(InspectableItem clickedPort)
    {
        if (!isWiring)
        {
            if (clickedPort.isOccupied) { Debug.Log($"{clickedPort.itemName} is already connected."); return; }

            isWiring                = true;
            wireStartPortItem       = clickedPort;
            wireStartTransform      = clickedPort.transform;
            activeWireConnectorType = clickedPort.connectorType;

            // Head connector mesh — stays at start port
            if (wireHeadPrefab != null)
            {
                activeWireHead = Instantiate(wireHeadPrefab,
                                             clickedPort.transform.position,
                                             clickedPort.transform.rotation,
                                             currentClone.transform);
                SetLayerRecursively(activeWireHead, currentClone.layer);
            }
            else
            {
                activeWireHead = new GameObject("DynamicCable_Head");
                activeWireHead.transform.SetParent(currentClone.transform);
                activeWireHead.transform.position = clickedPort.transform.position;
            }

            // Build strands
            activeWireLines.Clear();
            CableTypeProfile profile = CableProfile.Instance != null
                ? CableProfile.Instance.Get(clickedPort.connectorType) : null;

            int strandCount = profile != null ? profile.strandCount : 1;

            for (int s = 0; s < strandCount; s++)
            {
                GameObject strandGO = new GameObject($"Strand_{s}");
                strandGO.transform.SetParent(activeWireHead.transform);
                SetLayerRecursively(strandGO, currentClone.layer);

                LineRenderer lr      = strandGO.AddComponent<LineRenderer>();
                lr.useWorldSpace     = true;
                lr.numCornerVertices = 5;
                lr.numCapVertices    = 5;
                lr.shadowCastingMode = ShadowCastingMode.Off;

                float w = profile != null ? profile.strandWidth : 0.02f;
                lr.startWidth = w;
                lr.endWidth   = w;

                Material mat;
                if (cableMaterial != null)
                    mat = new Material(cableMaterial);
                else
                {
                    mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    if (mat == null || mat.shader == null)
                        mat = new Material(Shader.Find("Sprites/Default"));
                }

                Color col = Color.white;
                if (profile != null && profile.strandColors != null && profile.strandColors.Length > 0)
                    col = profile.strandColors[s % profile.strandColors.Length];

                mat.color = col;
                if (mat.HasProperty("_EmissionColor"))
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", col * 0.4f);
                }
                lr.material = mat;
                activeWireLines.Add(lr);
            }

            activeWireLine = activeWireLines.Count > 0 ? activeWireLines[0] : null;
            HighlightCompatiblePorts(clickedPort.connectorType, clickedPort.isPSUPort);
            Debug.Log($"Wiring started from [{clickedPort.connectorType}] port: {clickedPort.itemName}");
        }
        else
        {
            if (clickedPort == wireStartPortItem) { CancelWiring(); return; }
            if (!IsCompatible(wireStartPortItem, clickedPort))
            {
                Debug.Log($"Incompatible ports: [{wireStartPortItem.connectorType}] -> [{clickedPort.connectorType}].");
                return;
            }
            if (clickedPort.isOccupied) { Debug.Log($"{clickedPort.itemName} is already connected."); return; }
            CommitWire(wireStartPortItem, clickedPort);
        }
    }

    bool IsCompatible(InspectableItem portA, InspectableItem portB)
    {
        if (portA.connectorType != portB.connectorType) return false;
        if (portA.isPSUPort == portB.isPSUPort) return false;
        return true;
    }

    void CommitWire(InspectableItem startPort, InspectableItem endPort)
    {
        isWiring = false;
        ClearPortHighlights();

        Vector3 startPos = wireStartTransform.position;
        Vector3 endPos   = endPort.transform.position;
        float   cLen     = Vector3.Distance(startPos, endPos);
        float   sag      = Mathf.Max(cLen * cableSag, 0.05f);

        Vector3[] finalPath = CableRouteManager.Instance != null
            ? CableRouteManager.Instance.GetRoute(startPort.connectorType, startPos, endPos)
            : new Vector3[]
            {
                startPos,
                Vector3.Lerp(startPos, endPos, 0.25f) + Vector3.down * sag,
                Vector3.Lerp(startPos, endPos, 0.75f) + Vector3.down * sag,
                endPos
            };

        // ── Reparent all strands to currentClone BEFORE destroying the head ─────────
        // ── Reparent strands to currentClone BEFORE destroying head ─────────────
        List<LineRenderer> lrSnapshot   = new List<LineRenderer>(activeWireLines);
        string             snapConnType = activeWireConnectorType;

        foreach (LineRenderer lr in lrSnapshot)
            if (lr != null) lr.transform.SetParent(currentClone.transform);

        // Spawn a permanent head connector at start port (replaces the one on activeWireHead)
        if (wireHeadPrefab != null)
        {
            GameObject permHead = Instantiate(wireHeadPrefab,
                                            startPos,
                                            wireStartTransform.rotation,
                                            currentClone.transform);
            SetLayerRecursively(permHead, currentClone.layer);
        }

        // NOW safe to destroy the temporary drag head
        if (activeWireHead != null) { Destroy(activeWireHead); activeWireHead = null; }

        // Tail connector
        if (wireHeadPrefab != null)
        {
            activeWireTail = Instantiate(wireHeadPrefab,
                                        endPort.transform.position,
                                        endPort.transform.rotation,
                                        currentClone.transform);
            SetLayerRecursively(activeWireTail, currentClone.layer);
        }

        // Collider at midpoint
        GameObject wireColliderGO = new GameObject("WireCollider");
        wireColliderGO.transform.SetParent(currentClone.transform);
        wireColliderGO.transform.position = Vector3.Lerp(startPos, endPos, 0.5f);
        wireColliderGO.transform.LookAt(endPos);
        SetLayerRecursively(wireColliderGO, currentClone.layer);

        BoxCollider lineCol = wireColliderGO.AddComponent<BoxCollider>();
        lineCol.size = new Vector3(0.06f, 0.06f, Vector3.Distance(startPos, endPos));

        InspectableItem wireItem     = wireColliderGO.AddComponent<InspectableItem>();
        wireItem.itemName            = $"{startPort.connectorType} Cable";
        wireItem.itemDescription     = $"Connects {startPort.itemName} to {endPort.itemName}";
        wireItem.isRemovable         = true;

        startPort.isOccupied   = true;
        startPort.connectedTo  = endPort;
        startPort.attachedWire = wireColliderGO;

        endPort.isOccupied   = true;
        endPort.connectedTo  = startPort;
        endPort.attachedWire = wireColliderGO;

        AddWireBlocker(startPort, wireItem);
        AddWireBlocker(endPort,   wireItem);

        var wireCleaner   = wireColliderGO.AddComponent<WireCleanup>();
        wireCleaner.portA = startPort;
        wireCleaner.portB = endPort;

        // Immediately draw committed curve — inside case, zero camera push
        DrawCommittedCurve(lrSnapshot, snapConnType, startPos, endPos, sag);

        // Then animate to route
        if (activeSnapCoroutine != null) StopCoroutine(activeSnapCoroutine);
        activeSnapCoroutine = StartCoroutine(
            AnimateCableSnap(lrSnapshot, snapConnType, finalPath, 0.4f));

        activeWireTail          = null;
        activeWireLine          = null;
        activeWireLines.Clear();
        wireStartPortItem       = null;
        wireStartTransform      = null;
        activeWireConnectorType = "";
    }

    // Immediately positions all strands at their final committed location
    // Uses pure downward sag — zero camera push, zero chance of going outside
    void DrawCommittedCurve(List<LineRenderer> lines, string connType,
                            Vector3 startPos, Vector3 endPos, float sag)
    {
        if (lines == null || lines.Count == 0) return;

        CableTypeProfile profile = CableProfile.Instance != null
            ? CableProfile.Instance.Get(connType) : null;

        int   strandCount   = lines.Count;
        float strandSpacing = profile != null ? profile.strandSpacing : 0.003f;
        float totalWidth    = strandSpacing * (strandCount - 1);

        Vector3 cableDir  = (endPos - startPos).normalized;
        Vector3 ribbonDir = Vector3.Cross(cableDir, Vector3.up).normalized;
        if (ribbonDir.sqrMagnitude < 0.001f)
            ribbonDir = Vector3.Cross(cableDir, Vector3.forward).normalized;

        for (int s = 0; s < strandCount; s++)
        {
            LineRenderer lr = lines[s];
            if (lr == null) continue;

            lr.useWorldSpace = true;

            float   t_off  = strandCount > 1 ? (s / (float)(strandCount - 1)) - 0.5f : 0f;
            Vector3 offset = ribbonDir * (t_off * totalWidth);

            Vector3 p0 = startPos + offset;
            Vector3 p3 = endPos   + offset;
            // Control points stay BETWEEN the two ports — mathematically cannot go outside
            Vector3 c1 = Vector3.Lerp(p0, p3, 0.25f) + Vector3.down * sag;
            Vector3 c2 = Vector3.Lerp(p0, p3, 0.75f) + Vector3.down * sag;

            lr.positionCount = cableResolution;
            for (int i = 0; i < cableResolution; i++)
            {
                float t = i / (float)(cableResolution - 1);
                float u = 1f - t;
                lr.SetPosition(i, (u*u*u)*p0 + 3*(u*u)*t*c1 + 3*u*(t*t)*c2 + (t*t*t)*p3);
            }
        }
    }

    void AddWireBlocker(InspectableItem port, InspectableItem wireItem)
    {
        if (port.parentComponent == null) return;
        if (port.parentComponent.blockingParts == null)
            port.parentComponent.blockingParts = new List<InspectableItem>();
        port.parentComponent.blockingParts.Add(wireItem);
    }

    void CancelWiring()
    {
        isWiring = false;
        ClearPortHighlights();
        if (activeWireHead != null) { Destroy(activeWireHead); activeWireHead = null; }
        activeWireLines.Clear();
        activeWireLine          = null;
        wireStartPortItem       = null;
        wireStartTransform      = null;
        activeWireConnectorType = "";
        Debug.Log("Wiring cancelled.");
    }

    // ─────────────────────────────────────────────────────────────────────────────
    //  PORT HIGHLIGHT
    // ─────────────────────────────────────────────────────────────────────────────
    void HighlightCompatiblePorts(string connType, bool startIsPSU)
    {
        foreach (InspectableItem port in allPorts)
        {
            if (port == wireStartPortItem) continue;
            Renderer rend = port.GetComponentInChildren<Renderer>();
            if (rend == null) continue;

            bool compatible = port.connectorType == connType
                           && port.isPSUPort     != startIsPSU
                           && !port.isOccupied;

            Material mat = compatible ? validPortMaterial : invalidPortMaterial;
            if (mat != null)
            {
                if (!originalMaterialCache.ContainsKey(rend))
                    originalMaterialCache.Add(rend, rend.sharedMaterials);
                rend.sharedMaterials = new Material[] { mat };
            }
        }
    }

    void ClearPortHighlights()
    {
        foreach (var kv in originalMaterialCache)
            if (kv.Key != null) kv.Key.sharedMaterials = kv.Value;
        originalMaterialCache.Clear();
        lastHitObject = null;
    }

    // ─────────────────────────────────────────────────────────────────────────────
    //  LIVE WIRE DRAWING (while dragging)
    // ─────────────────────────────────────────────────────────────────────────────
    void HandleWireDrawing()
    {
        if (!isWiring || activeWireLines.Count == 0 || wireStartTransform == null) return;

        InspectableItem hoveredPort = lastHitObject != null
            ? lastHitObject.GetComponent<InspectableItem>() : null;

        if (hoveredPort != null && hoveredPort.isWirePort && hoveredPort != wireStartPortItem)
            DrawDragCurve(wireStartTransform.position, hoveredPort.transform.position);
        else
        {
            Ray ray = inspectionCamera.ScreenPointToRay(Input.mousePosition);
            DrawDragCurve(wireStartTransform.position, ray.GetPoint(currentDistance * 0.7f));
        }
    }

    // Drag-only curve — uses camera push so wire clears the case opening while dragging
    void DrawDragCurve(Vector3 startWorld, Vector3 endWorld)
    {
        if (activeWireLines == null || activeWireLines.Count == 0) return;

        float   cableLen  = Vector3.Distance(startWorld, endWorld);
        float   sag       = Mathf.Max(cableLen * cableSag, 0.05f);
        Vector3 cableDir  = (endWorld - startWorld).normalized;
        Vector3 ribbonDir = Vector3.Cross(cableDir, Vector3.up).normalized;
        if (ribbonDir.sqrMagnitude < 0.001f)
            ribbonDir = Vector3.Cross(cableDir, Vector3.forward).normalized;

        CableTypeProfile profile = CableProfile.Instance != null
            ? CableProfile.Instance.Get(activeWireConnectorType) : null;

        int   strandCount   = activeWireLines.Count;
        float strandSpacing = profile != null ? profile.strandSpacing : 0.003f;
        float totalWidth    = strandSpacing * (strandCount - 1);

        Vector3 pushDir = inspectionCamera.transform.position - startWorld;
        pushDir.y = 0f;
        if (pushDir.sqrMagnitude > 0.001f) pushDir.Normalize();

        for (int s = 0; s < strandCount; s++)
        {
            LineRenderer lr = activeWireLines[s];
            if (lr == null) continue;
            lr.useWorldSpace = true;

            float   t_off  = strandCount > 1 ? (s / (float)(strandCount - 1)) - 0.5f : 0f;
            Vector3 offset = ribbonDir * (t_off * totalWidth);

            Vector3 p0 = startWorld + offset;
            Vector3 p3 = endWorld   + offset;
            Vector3 c1 = p0 + pushDir * (cableLen * 0.35f) + Vector3.down * sag;
            Vector3 c2 = p3 + pushDir * (cableLen * 0.35f) + Vector3.down * sag;

            lr.positionCount = cableResolution;
            for (int i = 0; i < cableResolution; i++)
            {
                float t = i / (float)(cableResolution - 1);
                float u = 1f - t;
                lr.SetPosition(i, (u*u*u)*p0 + 3*(u*u)*t*c1 + 3*u*(t*t)*c2 + (t*t*t)*p3);
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────────
    //  CABLE SNAP ANIMATION
    //  Animates from the already-correct committed position to the routed path.
    //  Since both start and end are inside the case this can never go outside.
    // ─────────────────────────────────────────────────────────────────────────────
    private IEnumerator AnimateCableSnap(
        List<LineRenderer> lrSnapshot,
        string             connType,
        Vector3[]          routePositions,
        float              duration)
    {
        if (lrSnapshot == null || lrSnapshot.Count == 0) yield break;

        int res = cableResolution;

        // Capture current positions as the "from" state (already committed, inside case)
        Vector3[][] fromPositions = new Vector3[lrSnapshot.Count][];
        for (int s = 0; s < lrSnapshot.Count; s++)
        {
            LineRenderer lr = lrSnapshot[s];
            fromPositions[s] = new Vector3[res];
            if (lr != null && lr.positionCount == res)
                lr.GetPositions(fromPositions[s]);
            else if (lr != null)
                for (int i = 0; i < res; i++) fromPositions[s][i] = lr.transform.position;
        }

        Vector3[] toPoints = SampleCatmullRom(routePositions, res);

        CableTypeProfile profile = CableProfile.Instance != null
            ? CableProfile.Instance.Get(connType) : null;

        int   strandCount   = lrSnapshot.Count;
        float strandSpacing = profile != null ? profile.strandSpacing : 0.003f;
        float totalWidth    = strandSpacing * (strandCount - 1);

        Vector3 cableDir  = (routePositions[routePositions.Length - 1] - routePositions[0]).normalized;
        Vector3 ribbonDir = Vector3.Cross(cableDir, Vector3.up).normalized;
        if (ribbonDir.sqrMagnitude < 0.001f)
            ribbonDir = Vector3.Cross(cableDir, Vector3.forward).normalized;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float eased = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));

            for (int s = 0; s < strandCount; s++)
            {
                LineRenderer lr = lrSnapshot[s];
                if (lr == null) continue;

                float   t_off  = strandCount > 1 ? (s / (float)(strandCount - 1)) - 0.5f : 0f;
                Vector3 offset = ribbonDir * (t_off * totalWidth);

                lr.positionCount = res;
                for (int i = 0; i < res; i++)
                    lr.SetPosition(i, Vector3.Lerp(fromPositions[s][i], toPoints[i] + offset, eased));
            }
            yield return null;
        }

        // Snap to exact final
        for (int s = 0; s < strandCount; s++)
        {
            LineRenderer lr = lrSnapshot[s];
            if (lr == null) continue;
            float   t_off  = strandCount > 1 ? (s / (float)(strandCount - 1)) - 0.5f : 0f;
            Vector3 offset = ribbonDir * (t_off * totalWidth);
            lr.positionCount = res;
            for (int i = 0; i < res; i++)
                lr.SetPosition(i, toPoints[i] + offset);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────────
    //  CATMULL-ROM SAMPLER
    // ─────────────────────────────────────────────────────────────────────────────
    Vector3[] SampleCatmullRom(Vector3[] points, int count)
    {
        Vector3[] result = new Vector3[count];

        if (points.Length == 1) { for (int i = 0; i < count; i++) result[i] = points[0]; return result; }
        if (points.Length == 2)
        {
            for (int i = 0; i < count; i++)
                result[i] = Vector3.Lerp(points[0], points[1], i / (float)(count - 1));
            return result;
        }

        Vector3[] ext = new Vector3[points.Length + 2];
        ext[0] = points[0] + (points[0] - points[1]);
        for (int i = 0; i < points.Length; i++) ext[i + 1] = points[i];
        ext[ext.Length - 1] = points[points.Length - 1]
                            + (points[points.Length - 1] - points[points.Length - 2]);

        int segments = points.Length - 1;
        for (int i = 0; i < count; i++)
        {
            float globalT = i / (float)(count - 1) * segments;
            int   seg     = Mathf.Min((int)globalT, segments - 1);
            float localT  = globalT - seg;

            Vector3 p0 = ext[seg];
            Vector3 p1 = ext[seg + 1];
            Vector3 p2 = ext[seg + 2];
            Vector3 p3 = ext[seg + 3];

            result[i] = 0.5f * (
                  (2f * p1)
                + (-p0 + p2)                    * localT
                + (2f*p0 - 5f*p1 + 4f*p2 - p3) * localT * localT
                + (-p0 + 3f*p1 - 3f*p2 + p3)   * localT * localT * localT);
        }
        return result;
    }

    // ─────────────────────────────────────────────────────────────────────────────
    //  PC POWER
    // ─────────────────────────────────────────────────────────────────────────────
    void TogglePCPower()
    {
        isPCOn = !isPCOn;
        Debug.Log("PC Power: " + (isPCOn ? "ON" : "OFF"));

        foreach (PCFanController fan in currentClone.GetComponentsInChildren<PCFanController>())
        {
            fan.enabled = isPCOn;
            if (!isPCOn)
            {
                Renderer r = fan.GetComponentInChildren<Renderer>();
                if (r != null) r.material.SetColor("_EmissionColor", Color.black);
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────────
    //  HOVER / HIGHLIGHT
    // ─────────────────────────────────────────────────────────────────────────────
    void HandleHover()
    {
        Ray ray = inspectionCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 100f, ~0))
        {
            GameObject hitObj = hit.collider.gameObject;

            InspectableItem part = hitObj.GetComponent<InspectableItem>();
            if (part == null)
            {
                part = hitObj.GetComponentInParent<InspectableItem>();
                if (part != null) hitObj = part.gameObject;
            }

            if (part != null)
            {
                if (hitObj != lastHitObject)
                {
                    if (!isWiring) ClearHighlight();
                    HighlightObject(hitObj);
                    ShowTooltip(part);
                }
                MoveTooltip();
                return;
            }
        }

        if (!isWiring) ClearHighlight();
        else lastHitObject = null;

        if (tooltipPanel) tooltipPanel.SetActive(false);
    }

    void HighlightObject(GameObject obj)
    {
        lastHitObject = obj;
        foreach (Renderer rend in obj.GetComponentsInChildren<Renderer>())
        {
            if (!originalMaterialCache.ContainsKey(rend))
                originalMaterialCache.Add(rend, rend.sharedMaterials);
            Material[] newMats = new Material[rend.sharedMaterials.Length];
            for (int i = 0; i < newMats.Length; i++) newMats[i] = highlightMaterial;
            rend.sharedMaterials = newMats;
        }
    }

    void ClearHighlight()
    {
        foreach (var kv in originalMaterialCache)
            if (kv.Key != null) kv.Key.sharedMaterials = kv.Value;
        originalMaterialCache.Clear();
        lastHitObject = null;
    }

    void ShowTooltip(InspectableItem part)
    {
        if (!tooltipPanel) return;
        string extra = "";
        if (part.isWirePort)
        {
            string side = part.isPSUPort ? "PSU" : "Device";
            extra = $"\n<size=80%>[{part.connectorType}] {side} port"
                  + (part.isOccupied ? " - CONNECTED" : " - empty") + "</size>";
        }
        tooltipPanel.SetActive(true);
        if (tooltipTitle) tooltipTitle.text = part.itemName;
        if (tooltipBody)  tooltipBody.text  = part.itemDescription + extra;
    }

    void MoveTooltip()
    {
        if (tooltipPanel)
            tooltipPanel.transform.position = Input.mousePosition + new Vector3(20, -20, 0);
    }

    // ─────────────────────────────────────────────────────────────────────────────
    //  REMOVAL ANIMATION
    // ─────────────────────────────────────────────────────────────────────────────
    private IEnumerator AnimateRemovalAndDestroy(GameObject obj)
    {
        Collider col = obj.GetComponent<Collider>();
        if (col != null) col.enabled = false;

        float   duration   = 0.4f;
        float   elapsed    = 0f;
        Vector3 startScale = obj.transform.localScale;
        Vector3 startPos   = obj.transform.position;
        Vector3 spin       = new Vector3(Random.Range(180f, 360f),
                                         Random.Range(180f, 360f),
                                         Random.Range(180f, 360f));

        while (elapsed < duration)
        {
            if (obj == null) break;
            elapsed += Time.deltaTime;
            float   easeT  = Mathf.Pow(elapsed / duration, 3f);
            Vector3 target = inspectionCamera.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 0.2f));
            obj.transform.position   = Vector3.Lerp(startPos, target, easeT);
            obj.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, easeT);
            obj.transform.Rotate(spin * Time.deltaTime, Space.Self);
            yield return null;
        }

        if (obj != null) Destroy(obj);
    }

    // ─────────────────────────────────────────────────────────────────────────────
    //  INPUT & CAMERA
    // ─────────────────────────────────────────────────────────────────────────────
    void HandleInput()
    {
        if (Input.GetMouseButton(1) && !isWiring)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible   = false;
            targetOrbitAngles.y += Input.GetAxis("Mouse X") * orbitSpeed;
            targetOrbitAngles.x -= Input.GetAxis("Mouse Y") * orbitSpeed;
            targetOrbitAngles.x  = Mathf.Clamp(targetOrbitAngles.x, -89f, 89f);
        }
        else { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }

        float inputX = Input.GetAxis("Horizontal");
        float inputY = Input.GetAxis("Vertical");
        if (inputX != 0 || inputY != 0)
        {
            Quaternion camRot = Quaternion.Euler(targetOrbitAngles.x, targetOrbitAngles.y, 0);
            targetFocusPoint += (camRot * Vector3.right * inputX + camRot * Vector3.up * inputY)
                              * panSpeed * Time.deltaTime;
        }

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            targetDistance -= scroll * zoomSpeed;
            targetDistance  = Mathf.Clamp(targetDistance, optimalDistance * 0.1f, optimalDistance * 5f);
        }
    }

    void ApplyCameraMovement()
    {
        float dt = Time.deltaTime * smoothTime;
        orbitAngles     = Vector2.Lerp(orbitAngles,   targetOrbitAngles, dt);
        currentDistance = Mathf.Lerp(currentDistance, targetDistance,    dt);
        focusPoint      = Vector3.Lerp(focusPoint,     targetFocusPoint,  dt);

        Quaternion rotation = Quaternion.Euler(orbitAngles.x, orbitAngles.y, 0);
        Vector3    position = rotation * new Vector3(0, 0, -currentDistance) + focusPoint;

        inspectionCamera.transform.position = voidAnchor.transform.position + position;
        inspectionCamera.transform.rotation = rotation;
    }

    // ─────────────────────────────────────────────────────────────────────────────
    //  HELPERS
    // ─────────────────────────────────────────────────────────────────────────────
    public void ResetView()
    {
        targetFocusPoint  = Vector3.zero;
        targetDistance    = optimalDistance;
        targetOrbitAngles = Vector2.zero;
    }

    void SetLayerRecursively(GameObject obj, int newLayer)
    {
        obj.layer = newLayer;
        foreach (Transform child in obj.transform)
            SetLayerRecursively(child.gameObject, newLayer);
    }
}
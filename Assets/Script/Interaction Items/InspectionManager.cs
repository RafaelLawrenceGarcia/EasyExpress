// ============================================================
//  InspectionManager.cs  (CORE — partial class 1/6)
//  Fields, lifecycle, Inspect(), StopInspection()
//  
//  The other partial files handle:
//    .Camera.cs   — orbit, pan, zoom
//    .Parts.cs    — remove/install components
//    .Wiring.cs   — cable/wire system
//    .UI.cs       — hover, tooltips, power, dust, click dispatch
//    .Helpers.cs  — layers, animations, utilities
// ============================================================
using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using TMPro;
using System.Collections.Generic;
using System.Collections;

public partial class InspectionManager : MonoBehaviour
{
    // ─── Inspector Fields ────────────────────────────────────────

    [Header("UI Reference")]
    public GameObject controlsUI;
    public GameObject infoPanel;
    public GameObject gameplayUI;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descText;
    public InspectionInventoryUI inventoryUI;
    public GameObject partPendingInstallation = null;

    [Header("Tooltip Reference")]
    public GameObject tooltipPanel;
    public TextMeshProUGUI tooltipTitle;
    public TextMeshProUGUI tooltipBody;

    [Header("Materials")]
    [Tooltip("Outline material using Custom/OutlineEdge shader")]
    public Material outlineMaterial;
    [Tooltip("Subtle material for hovering over components inside Inspect Mode")]
    public Material highlightMaterial;
    public Material validPortMaterial;
    public Material invalidPortMaterial;
    public Material ghostMaterial;

    [Header("HUD References")]
    public GameObject goldHUD;

    [Header("Dust System")]
    public Material dustMaterial;

    [Header("Player Storage (Inventory)")]
    public List<GameObject> playerStorage = new List<GameObject>();

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
    public GameObject wireHeadPrefab;
    public Vector3 wireHeadOffset = new Vector3(0, 0, 0.02f);
    public Material cableMaterial;
    public float cableSag = 0.2f;
    [Range(0f, 2f)] public float sagLengthScale = 0.6f;
    public int cableResolution = 20;

    [Header("Screwdriver / Hold-to-Remove & Install")]
    public ScrewdriverInteraction screwdriverSystem;

    // ─── Shared State (accessed by partial classes) ──────────────

    internal bool isConfirmingRemoval = false;
    internal GameObject currentClone;
    internal GameObject voidAnchor;
    internal bool isPlacingFromInventory = false;
    public bool isInspecting = false;
    [HideInInspector] public bool viewOnlyMode = false;
    internal float inspectCooldown = 0f;
    internal bool isWiring = false;
    internal bool isPCOn = false;
    internal bool tutorialHoverDone = false;

    // Camera state
    internal Vector3 focusPoint, targetFocusPoint;
    internal float currentDistance, targetDistance;
    internal Vector2 orbitAngles, targetOrbitAngles;
    internal float optimalDistance;

    // Highlight/material cache
    internal GameObject lastHitObject;
    internal Dictionary<Renderer, Material[]> originalMaterialCache = new Dictionary<Renderer, Material[]>();
    internal List<InspectableItem> allPorts = new List<InspectableItem>();

    // Saved transform state
    internal Vector3 savedOriginalPosition;
    internal Quaternion savedOriginalRotation;
    internal Transform savedOriginalParent;
    internal bool savedRbKinematic;
    internal Dictionary<GameObject, int> originalLayerCache = new Dictionary<GameObject, int>();
    internal bool tooltipAnchored = false;

    // Wiring state
    internal List<LineRenderer> activeWireLines = new List<LineRenderer>();
    internal string activeWireConnectorType = "";
    internal LineRenderer activeWireLine;
    internal InspectableItem wireStartPortItem;
    internal Transform wireStartTransform;
    internal GameObject activeWireHead;
    internal Coroutine activeSnapCoroutine;
    internal Vector3 activeRibbonDir = Vector3.right;
    internal Vector3 activeEndRibbonDir = Vector3.up;

    // ─── Lifecycle ───────────────────────────────────────────────

    void Start()
    {
        if (blurVolume != null) blurVolume.weight = 0;

        voidAnchor = new GameObject("Void_Inspection_Anchor");
        voidAnchor.transform.position = new Vector3(0, -1000, 0);

        SetupCameraStack();

        if (inspectionCamera)
        {
            inspectionCamera.transform.parent = null;
            inspectionCamera.gameObject.SetActive(false);
        }

        if (controlsUI) controlsUI.SetActive(false);
        if (infoPanel) infoPanel.SetActive(false);
        if (tooltipPanel) tooltipPanel.SetActive(false);

        if (screwdriverSystem != null && inspectionCamera != null)
            screwdriverSystem.inspectionCamera = inspectionCamera;
    }

    void SetupCameraStack()
    {
        if (!mainCamera || !inspectionCamera) return;
        var mainCamData = mainCamera.GetUniversalAdditionalCameraData();
        var overlayCamData = inspectionCamera.GetUniversalAdditionalCameraData();
        overlayCamData.renderType = CameraRenderType.Overlay;

        bool isStacked = false;
        foreach (var cam in mainCamData.cameraStack)
            if (cam == inspectionCamera) { isStacked = true; break; }
        if (!isStacked) mainCamData.cameraStack.Add(inspectionCamera);
    }

    void Update()
    {
        if (!isInspecting || currentClone == null) return;
        if (isConfirmingRemoval) return;

        if (InspectionToolbarUI.Instance != null) InspectionToolbarUI.Instance.HandleInput();

        HandleInput();         // Camera.cs
        ApplyCameraMovement(); // Camera.cs
        HandleHover();         // UI.cs
        HandleClickInteractions(); // UI.cs
        HandleWireDrawing();   // Wiring.cs

        if (inspectCooldown > 0f)
        {
            inspectCooldown -= Time.deltaTime;
        }
        else
        {
            if (Input.GetKeyDown(KeyCode.R)) ResetView();
            if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.X))
                StopInspection();
        }

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (inventoryUI != null && !viewOnlyMode) inventoryUI.ToggleInventory();
        }

        if (ToolBelt.Instance != null) ToolBelt.Instance.HandleToolInput();
        HandleDustCleaning(); // UI.cs
    }

    // ─── Inspect (enter inspection mode) ─────────────────────────

    public void InspectViewOnly(InspectableItem originalItem)
    {
        viewOnlyMode = true;
        Inspect(originalItem);
    }

    public void Inspect(InspectableItem originalItem)
    {
        inspectCooldown = 0.3f;
        isInspecting = true;
        tutorialHoverDone = false;

        if (TutorialManager.Instance != null && TutorialManager.Instance.IsInstallComponentStep())
            StartCoroutine(AutoOpenInventoryForInstall());

        // UI state
        if (blurVolume != null) blurVolume.weight = 1;
        if (controlsUI) controlsUI.SetActive(true);
        if (infoPanel) infoPanel.SetActive(true);
        if (gameplayUI) gameplayUI.SetActive(false);
        if (goldHUD != null) goldHUD.SetActive(false);
        if (TaskListUI.Instance != null) TaskListUI.Instance.HideTemporarily();
        if (nameText) nameText.text = originalItem.itemName;
        if (descText) descText.text = originalItem.itemDescription;
        if (mainCamera) mainCamera.gameObject.SetActive(true);
        if (inspectionCamera) inspectionCamera.gameObject.SetActive(true);

        if (screwdriverSystem != null && inspectionCamera != null)
            screwdriverSystem.inspectionCamera = inspectionCamera;

        // Disable player
        if (playerMovement) playerMovement.enabled = false;
        if (cameraScript) cameraScript.enabled = false;
        if (interactScript) interactScript.enabled = false;
        if (playerAnimator) playerAnimator.enabled = false;
        if (playerRootObject)
            foreach (Renderer r in playerRootObject.GetComponentsInChildren<Renderer>())
                r.enabled = false;

        // Move PC to void
        if (voidAnchor == null)
        {
            voidAnchor = new GameObject("Void_Inspection_Anchor");
            voidAnchor.transform.position = new Vector3(0, -1000, 0);
        }

        currentClone = originalItem.gameObject;
        savedOriginalPosition = currentClone.transform.position;
        savedOriginalRotation = currentClone.transform.rotation;
        savedOriginalParent = currentClone.transform.parent;

        currentClone.transform.SetParent(voidAnchor.transform);
        currentClone.transform.localPosition = Vector3.zero;
        currentClone.transform.localRotation = Quaternion.Euler(0, 180f, 0);

        Rigidbody rb = currentClone.GetComponent<Rigidbody>();
        if (rb != null) { savedRbKinematic = rb.isKinematic; rb.isKinematic = true; }

        originalItem.enabled = false;
        InspectableItem rootScript = currentClone.GetComponent<InspectableItem>();
        if (rootScript != null) rootScript.enabled = false;
        PCCaseBuilder builderScript = currentClone.GetComponent<PCCaseBuilder>();
        if (builderScript != null) builderScript.enabled = false;

        foreach (Collider col in currentClone.GetComponents<Collider>()) col.enabled = false;

        // Hide case shell from raycasts
        string[] caseChildNames = { "case", "Front Panel", "Back Panel", "PSU Cover", "case.004" };
        foreach (string childName in caseChildNames)
        {
            foreach (Transform t in currentClone.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == childName)
                {
                    SetLayerRecursively(t.gameObject, LayerMask.NameToLayer("Ignore Raycast"));
                    break;
                }
            }
        }

        SaveAndSetLayers(currentClone, LayerMask.NameToLayer("InspectLayer"));

        // Center PC
        Bounds bounds = new Bounds(currentClone.transform.position, Vector3.zero);
        foreach (Renderer r in currentClone.GetComponentsInChildren<Renderer>()) bounds.Encapsulate(r.bounds);
        currentClone.transform.position += voidAnchor.transform.position - bounds.center;

        HideAllGhostSlots();

        // Camera distance
        float objectSize = bounds.extents.magnitude;
        if (objectSize == 0) objectSize = 1f;
        float fov = inspectionCamera.fieldOfView * 0.5f * Mathf.Deg2Rad;
        optimalDistance = (objectSize / Mathf.Sin(fov)) * 2.0f;
        targetDistance = currentDistance = optimalDistance;
        targetFocusPoint = focusPoint = Vector3.zero;
        targetOrbitAngles = orbitAngles = Vector2.zero;

        // Wire ports
        allPorts.Clear();
        foreach (var item in currentClone.GetComponentsInChildren<InspectableItem>())
            if (item.isWirePort) allPorts.Add(item);

        if (InspectionToolbarUI.Instance != null) InspectionToolbarUI.Instance.Show();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Dust
        DustSystem dust = currentClone.GetComponent<DustSystem>();
        if (dust == null && PlayerPrefs.GetInt("NextPCDusty", 0) == 1)
        {
            dust = currentClone.AddComponent<DustSystem>();
            dust.isDusty = true;
            PlayerPrefs.SetInt("NextPCDusty", 0);
            PlayerPrefs.Save();
        }
        if (dust != null && dust.isDusty)
        {
            if (dust.dustOverlayMaterial == null && dustMaterial != null) dust.dustOverlayMaterial = dustMaterial;
            dust.ApplyDust();
        }
    }

    // ─── StopInspection (exit) ───────────────────────────────────

    public void StopInspection()
    {
        if (screwdriverSystem != null) screwdriverSystem.CancelUnscrewing();
        isConfirmingRemoval = false;
        isInspecting = false;
        viewOnlyMode = false;
        HideAllGhostSlots();

        if (inventoryUI != null && inventoryUI.inventoryPanel != null)
            inventoryUI.inventoryPanel.SetActive(false);

        CancelWiring();
        ClearPortHighlights();

        if (blurVolume != null) blurVolume.weight = 0;
        ClearHighlight();

        if (currentClone != null)
        {
            RestoreLayers(currentClone);
            originalLayerCache.Clear();

            InspectableItem rootScript = currentClone.GetComponent<InspectableItem>();
            if (rootScript != null) rootScript.enabled = true;
            PCCaseBuilder builderScript = currentClone.GetComponent<PCCaseBuilder>();
            if (builderScript != null) builderScript.enabled = true;

            foreach (Collider col in currentClone.GetComponents<Collider>()) col.enabled = true;

            string[] caseChildNames = { "case", "case.001", "case.003", "psucase" };
            foreach (string childName in caseChildNames)
            {
                Transform caseChild = null;
                foreach (Transform t in currentClone.GetComponentsInChildren<Transform>(true))
                    if (t.name == childName) { caseChild = t; break; }
                if (caseChild != null)
                    foreach (Collider col in caseChild.GetComponentsInChildren<Collider>()) col.enabled = true;
            }

            Rigidbody rb = currentClone.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = savedRbKinematic;

            currentClone.transform.SetParent(savedOriginalParent);
            currentClone.transform.position = savedOriginalPosition;
            currentClone.transform.rotation = savedOriginalRotation;

            // Re-enable renderers, then hide ghost slots
            foreach (Renderer rend in currentClone.GetComponentsInChildren<Renderer>(true)) rend.enabled = true;
            foreach (InspectableItem item in currentClone.GetComponentsInChildren<InspectableItem>(true))
            {
                if (item.isInventorySlot)
                    foreach (Renderer rend in item.GetComponentsInChildren<Renderer>(true)) rend.enabled = false;
            }
        }

        currentClone = null;
        allPorts.Clear();

        // Restore UI
        if (infoPanel) infoPanel.SetActive(false);
        if (tooltipPanel) tooltipPanel.SetActive(false);
        if (controlsUI) controlsUI.SetActive(false);
        tooltipAnchored = false;
        if (InspectionToolbarUI.Instance != null) InspectionToolbarUI.Instance.Hide();
        if (gameplayUI) gameplayUI.SetActive(true);
        if (inspectionCamera) inspectionCamera.gameObject.SetActive(false);
        if (goldHUD != null) goldHUD.SetActive(true);
        if (TaskListUI.Instance != null) TaskListUI.Instance.RestoreIfNeeded();

        // Re-enable player
        if (playerMovement) playerMovement.enabled = true;
        if (cameraScript) cameraScript.enabled = true;
        if (interactScript) interactScript.enabled = true;
        if (playerAnimator) playerAnimator.enabled = true;
        if (playerRootObject)
            foreach (Renderer r in playerRootObject.GetComponentsInChildren<Renderer>()) r.enabled = true;

        if (TutorialManager.Instance != null) TutorialManager.Instance.OnPlayerExitedInspection();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    IEnumerator AutoOpenInventoryForInstall()
    {
        yield return new WaitForSeconds(0.4f);
        if (inventoryUI != null)
        {
            inventoryUI.currentMode = InspectionInventoryUI.InventoryMode.PlayerStorage;
            if (inventoryUI.inventoryPanel != null) inventoryUI.inventoryPanel.SetActive(true);
            inventoryUI.RefreshInventory();
        }
    }
}
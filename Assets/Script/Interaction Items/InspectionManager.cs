using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using TMPro;
using System.Collections.Generic;
using System.Collections;

public class InspectionManager : MonoBehaviour
{
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

    private bool isConfirmingRemoval = false;

    private List<LineRenderer> activeWireLines       = new List<LineRenderer>();
    private string             activeWireConnectorType = "";
    private LineRenderer       activeWireLine;

    private InspectableItem wireStartPortItem;
    private Transform       wireStartTransform;
    private GameObject      activeWireHead;
    private GameObject      activeWireTail;
    private Coroutine       activeSnapCoroutine;
    private Vector3         activeRibbonDir    = Vector3.right; 
    private Vector3         activeEndRibbonDir = Vector3.up;    

    private GameObject currentClone;
    private GameObject voidAnchor;
    private bool isPlacingFromInventory = false;
    public bool isInspecting = false;
    private float inspectCooldown = 0f;
    private bool isWiring     = false;
    private bool isPCOn       = false;
    private bool tutorialHoverDone = false;
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

    private Vector3    savedOriginalPosition;
    private Quaternion savedOriginalRotation;
    private Transform  savedOriginalParent;
    private bool       savedRbKinematic;
    private Dictionary<GameObject, int> originalLayerCache = new Dictionary<GameObject, int>();

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

        if (screwdriverSystem != null && inspectionCamera != null)
            screwdriverSystem.inspectionCamera = inspectionCamera;
    }

    void Update()
    {
        if (!isInspecting || currentClone == null) return;

        if (isConfirmingRemoval) return;
        if (InspectionToolbarUI.Instance != null) InspectionToolbarUI.Instance.HandleInput();

        HandleInput();
        ApplyCameraMovement();
        HandleHover();
        HandleClickInteractions();
        HandleWireDrawing();

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
            if (inventoryUI != null) inventoryUI.ToggleInventory();
        }
        // Tool switching (1=Screwdriver, 2=Air Can, 3=Bare hands)
        if (ToolBelt.Instance != null)
            ToolBelt.Instance.HandleToolInput();
        
        // Dust cleaning with compressed air
        HandleDustCleaning();
    }

    public void Inspect(InspectableItem originalItem)
    {
        inspectCooldown = 0.3f;
        isInspecting = true;

        if (blurVolume != null) blurVolume.weight = 1;

        if (controlsUI)   controlsUI.SetActive(true);
        if (infoPanel)    infoPanel.SetActive(true);
        if (gameplayUI)   gameplayUI.SetActive(false);
        if (goldHUD != null) goldHUD.SetActive(false);
        if (TaskListUI.Instance != null) TaskListUI.Instance.HideTemporarily();
        if (nameText)     nameText.text = originalItem.itemName;
        if (descText)     descText.text = originalItem.itemDescription;

        if (mainCamera)       mainCamera.gameObject.SetActive(true);
        if (inspectionCamera) inspectionCamera.gameObject.SetActive(true);

        if (screwdriverSystem != null && inspectionCamera != null)
            screwdriverSystem.inspectionCamera = inspectionCamera;

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

        currentClone = originalItem.gameObject;

        savedOriginalPosition = currentClone.transform.position;
        savedOriginalRotation = currentClone.transform.rotation;
        savedOriginalParent   = currentClone.transform.parent;

        currentClone.transform.SetParent(voidAnchor.transform);
        currentClone.transform.localPosition = Vector3.zero;
        currentClone.transform.localRotation = Quaternion.Euler(0, 180f, 0);

        Rigidbody rb = currentClone.GetComponent<Rigidbody>();
        if (rb != null)
        {
            savedRbKinematic = rb.isKinematic;
            rb.isKinematic = true;
        }

        originalItem.enabled = false;
        InspectableItem rootScript = currentClone.GetComponent<InspectableItem>();
        if (rootScript != null) rootScript.enabled = false;

        PCCaseBuilder builderScript = currentClone.GetComponent<PCCaseBuilder>();
        if (builderScript != null) builderScript.enabled = false;

        foreach (Collider col in currentClone.GetComponents<Collider>())
            col.enabled = false;

        string[] caseChildNames = { "case", "case.001", "case.003", "psucase" };
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

        Bounds bounds = new Bounds(currentClone.transform.position, Vector3.zero);
        foreach (Renderer r in currentClone.GetComponentsInChildren<Renderer>())
            bounds.Encapsulate(r.bounds);
        currentClone.transform.position += voidAnchor.transform.position - bounds.center;

        HideAllGhostSlots();

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

        if (InspectionToolbarUI.Instance != null) InspectionToolbarUI.Instance.Show();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
        
        // --- DUST INITIALIZATION LOGIC MOVED HERE ---
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
            if (dust.dustOverlayMaterial == null && dustMaterial != null)
                dust.dustOverlayMaterial = dustMaterial;
            dust.ApplyDust();
        }
    }

    void HandleDustCleaning()
    {
        if (currentClone == null) return;
        
        DustSystem dust = currentClone.GetComponent<DustSystem>();
        if (dust == null || !dust.isDusty) return;
        
        // Check if player has AIR CAN selected (press 2)
        if (InspectionToolbarUI.Instance == null) return;
        if (!InspectionToolbarUI.Instance.IsAirCanSelected()) return;
        
        if (Input.GetMouseButton(0))
        {
            bool cleaned = dust.CleanTick(Time.deltaTime);
            
            if (cleaned)
            {
                Debug.Log("PC is now clean!");
            }
        }
    }

    public void PrepareInstallationFromUI(GameObject partObj, InspectableItem partData)
    {
        if (currentClone == null) return;
        
        partPendingInstallation = partObj;
        isPlacingFromInventory = true;

        foreach (Collider col in currentClone.GetComponentsInChildren<Collider>(true))
            col.enabled = false;

        InspectableItem[] allItems = currentClone.GetComponentsInChildren<InspectableItem>(true);
        foreach (var item in allItems)
        {
            if (item.isInventorySlot)
            {
                if (item.partCategory == partData.partCategory)
                {
                    foreach (Renderer rend in item.GetComponentsInChildren<Renderer>(true)) rend.enabled = true;
                    foreach (Collider col in item.GetComponentsInChildren<Collider>(true)) col.enabled = true;
                }
                else
                {
                    foreach (Renderer rend in item.GetComponentsInChildren<Renderer>(true)) rend.enabled = false;
                }
            }
        }
    }

    public void HideAllGhostSlots()
    {
        if (currentClone == null) return;
        
        partPendingInstallation = null;
        isPlacingFromInventory = false;

        InspectableItem[] allItems = currentClone.GetComponentsInChildren<InspectableItem>(true);
        
        foreach (var item in allItems)
        {
            if (!item.isInventorySlot)
            {
                if (item.gameObject == currentClone || IsCaseShellObject(item.gameObject)) continue;
                foreach (Collider col in item.GetComponentsInChildren<Collider>(true)) 
                    col.enabled = true;
            }
        }

        foreach (var item in allItems)
        {
            if (item.isInventorySlot)
            {
                foreach (Renderer rend in item.GetComponentsInChildren<Renderer>(true)) rend.enabled = false;
                foreach (Collider col in item.GetComponentsInChildren<Collider>(true)) col.enabled = false;
            }
        }
    }

    public void StopInspection()
    {
        if (screwdriverSystem != null) screwdriverSystem.CancelUnscrewing();
        isConfirmingRemoval = false;

        isInspecting = false;

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

            Collider mainCollider = currentClone.GetComponent<Collider>();
            if (mainCollider != null) mainCollider.enabled = true;

            string[] caseChildNames = { "case", "case.001", "case.003", "psucase" };
            foreach (string childName in caseChildNames)
            {
                Transform caseChild = null;
                foreach (Transform t in currentClone.GetComponentsInChildren<Transform>(true))
                    if (t.name == childName) { caseChild = t; break; }

                if (caseChild != null)
                    foreach (Collider col in caseChild.GetComponentsInChildren<Collider>())
                        col.enabled = true;
            }

            Rigidbody rb = currentClone.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = savedRbKinematic;

            currentClone.transform.SetParent(savedOriginalParent);
            currentClone.transform.position = savedOriginalPosition;
            currentClone.transform.rotation = savedOriginalRotation;
        }

        currentClone = null;
        allPorts.Clear();

        if (infoPanel)     infoPanel.SetActive(false);
        if (tooltipPanel)  tooltipPanel.SetActive(false);
        if (controlsUI)    controlsUI.SetActive(false);
        
        if (InspectionToolbarUI.Instance != null) InspectionToolbarUI.Instance.Hide();
        
        if (gameplayUI)    gameplayUI.SetActive(true);

        if (inspectionCamera) inspectionCamera.gameObject.SetActive(false);
        if (goldHUD != null) goldHUD.SetActive(true);
        if (TaskListUI.Instance != null) TaskListUI.Instance.RestoreIfNeeded();
        if (playerMovement) playerMovement.enabled = true;
        if (cameraScript)   cameraScript.enabled   = true;
        if (interactScript) interactScript.enabled = true;
        if (playerAnimator) playerAnimator.enabled = true;

        if (playerRootObject)
            foreach (Renderer r in playerRootObject.GetComponentsInChildren<Renderer>())
                r.enabled = true;
        if (TutorialManager.Instance != null && TutorialManager.Instance.GetCurrentStep() == 11)
        {
            if (TutorialManager.Instance != null) TutorialManager.Instance.CompleteHoverTask();
        }
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
    }

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

    // ─────────────────────────────────────────────────────────────────────────────
    //  CLICK INTERACTIONS
    // ─────────────────────────────────────────────────────────────────────────────
    void HandleClickInteractions()
    {
        // Block interactions if PC is dusty (except air can cleaning)
        DustSystem dust = currentClone != null ? currentClone.GetComponent<DustSystem>() : null;
        if (dust != null && dust.isDusty)
        {
            if (Input.GetMouseButtonDown(0) && tooltipPanel)
            {
                // Only show warning if NOT using air can 
                if (InspectionToolbarUI.Instance == null || !InspectionToolbarUI.Instance.IsAirCanSelected())
                {
                    tooltipPanel.SetActive(true);
                    if (tooltipTitle) tooltipTitle.text = "Too Dusty!";
                    if (tooltipBody) tooltipBody.text = "Clean the PC with compressed air first.\nPress 2 to equip air can.";
                }
            }
            return;
        }

        if (inventoryUI != null && inventoryUI.inventoryPanel.activeSelf) return;
        if (isWiring && Input.GetMouseButtonDown(1)) { CancelWiring(); return; }
        if (!Input.GetMouseButtonDown(0)) return;

        Ray ray = inspectionCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, 100f, ~0);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        InspectableItem part = null;
        foreach (RaycastHit hit in hits)
        {
            InspectableItem candidate = hit.collider.gameObject.GetComponent<InspectableItem>();
            if (candidate == null) continue;
            if (candidate.isMainObject) continue;
            if (isPlacingFromInventory && !candidate.isInventorySlot) continue;
            part = candidate;
            break;
        }

        if (part == null) return;
        if (isPlacingFromInventory && !part.isInventorySlot) return;

        if      (part.isInventorySlot) BeginInstallConfirmation(part);
        else if (part.isRemovable)     BeginRemovalConfirmation(part);
        else if (part.isPowerButton)   TogglePCPower();
        else if (part.isWirePort)      HandleWirePort(part);
    }
    
    // ─────────────────────────────────────────────────────────────────────────────
    //  HOLD-TO-INSTALL  
    // ─────────────────────────────────────────────────────────────────────────────
    void BeginInstallConfirmation(InspectableItem slot)
    {
        if (InspectionToolbarUI.Instance != null && !InspectionToolbarUI.Instance.IsScrewdriverSelected())
        {
            if (tooltipPanel)
            {
                tooltipPanel.SetActive(true);
                if (tooltipTitle) tooltipTitle.text = "Tool Required";
                if (tooltipBody) tooltipBody.text = "Select the Screwdriver first.\nPress 1 to equip.";
            }
            return;
        }

        GameObject partToInstall = null;

        if (partPendingInstallation != null)
        {
            InspectableItem pending = partPendingInstallation.GetComponent<InspectableItem>();
            if (pending == null || pending.partCategory != slot.partCategory)
            {
                if (tooltipPanel)
                {
                    tooltipPanel.SetActive(true);
                    if (tooltipTitle) tooltipTitle.text = "Wrong Slot!";
                    if (tooltipBody)  tooltipBody.text  = $"This slot is for: {slot.partCategory}";
                }
                return;
            }
            partToInstall = partPendingInstallation;
        }
        else
        {
            foreach (GameObject item in playerStorage)
            {
                InspectableItem stored = item.GetComponent<InspectableItem>();
                if (stored != null && stored.partCategory == slot.partCategory)
                {
                    partToInstall = item;
                    break;
                }
            }
        }

        if (partToInstall == null) return;

        if (screwdriverSystem != null)
        {
            isConfirmingRemoval = true;

            screwdriverSystem.onCancelled = () =>
            {
                isConfirmingRemoval = false;
            };

            screwdriverSystem.BeginUnscrewing(slot.transform, () =>
            {
                isConfirmingRemoval = false;
                TryInstallPart(slot);
            });
        }
        else
        {
            TryInstallPart(slot);
        }
    }
    

    // ─────────────────────────────────────────────────────────────────────────────
    //  HOLD-TO-REMOVE
    // ─────────────────────────────────────────────────────────────────────────────
    void BeginRemovalConfirmation(InspectableItem part)
    {
        if (InspectionToolbarUI.Instance != null && !InspectionToolbarUI.Instance.IsScrewdriverSelected())
        {
            if (tooltipPanel)
            {
                tooltipPanel.SetActive(true);
                if (tooltipTitle) tooltipTitle.text = "Tool Required";
                if (tooltipBody) tooltipBody.text = "Select the Screwdriver first.\nPress 1 to equip.";
            }
            return; 
        }

        foreach (InspectableItem blocker in part.blockingParts)
        {
            if (blocker != null && !blocker.isInventorySlot)
            {
                Debug.Log($"Cannot remove {part.itemName} - blocked by {blocker.itemName}.");
                if (tooltipPanel)
                {
                    tooltipPanel.SetActive(true);
                    if (tooltipTitle) tooltipTitle.text = "Blocked!";
                    if (tooltipBody)  tooltipBody.text  = $"Remove {blocker.itemName} first.";
                }
                return;
            }
        }

        if (screwdriverSystem != null)
        {
            isConfirmingRemoval = true;
            screwdriverSystem.onCancelled = () =>
            {
                isConfirmingRemoval = false;
            };
            screwdriverSystem.BeginUnscrewing(part.transform, () =>
            {
                isConfirmingRemoval = false;
                TryRemovePart(part);
            });
        }
        else
        {
            TryRemovePart(part);
        }
    }

    void TryRemovePart(InspectableItem part)
    {
        foreach (InspectableItem blocker in part.blockingParts)
        {
            if (blocker != null && !blocker.isInventorySlot)
            {
                Debug.Log($"Cannot remove {part.itemName} - blocked by {blocker.itemName}.");
                return;
            }
        }
        
        Debug.Log($"Removing {part.itemName}");

        if (TutorialManager.Instance != null) TutorialManager.Instance.CompleteRemoveTask();

        ClearHighlight();

        GameObject storedPart = Instantiate(part.gameObject, voidAnchor.transform);
        storedPart.SetActive(false);
        SetLayerRecursively(storedPart, LayerMask.NameToLayer("Default")); 
        playerStorage.Add(storedPart);

        GameObject flyingCopy = Instantiate(part.gameObject, part.transform.position, part.transform.rotation, voidAnchor.transform);
        Destroy(flyingCopy.GetComponent<InspectableItem>()); 
        
        foreach (Light l in flyingCopy.GetComponentsInChildren<Light>()) Destroy(l);
        foreach (TrailRenderer t in flyingCopy.GetComponentsInChildren<TrailRenderer>()) Destroy(t);
        foreach (ParticleSystem p in flyingCopy.GetComponentsInChildren<ParticleSystem>()) Destroy(p);

        StartCoroutine(AnimateRemovalAndDestroy(flyingCopy));

        part.isRemovable = false;
        part.isInventorySlot = true;

        foreach (Renderer rend in part.GetComponentsInChildren<Renderer>())
        {
            Material[] ghostMats = new Material[rend.sharedMaterials.Length];
            for (int i = 0; i < ghostMats.Length; i++) ghostMats[i] = ghostMaterial;
            rend.sharedMaterials = ghostMats;
            rend.enabled = false;
        }
        
        foreach (Collider col in part.GetComponentsInChildren<Collider>())
            col.enabled = false;
    }

    void TryInstallPart(InspectableItem slot)
    {
        GameObject partToInstall = null;
        if (partPendingInstallation != null)
        {
            InspectableItem pendingScript = partPendingInstallation.GetComponent<InspectableItem>();
            if (pendingScript == null || pendingScript.partCategory != slot.partCategory)
            {
                if (tooltipPanel)
                {
                    tooltipPanel.SetActive(true);
                    if (tooltipTitle) tooltipTitle.text = "Wrong Slot!";
                    if (tooltipBody)  tooltipBody.text  = $"This slot is for: {slot.partCategory}";
                }
                return;
            }
            partToInstall = partPendingInstallation;
        }
        else
        {
            foreach (GameObject item in playerStorage)
            {
                InspectableItem storedScript = item.GetComponent<InspectableItem>();
                if (storedScript != null && storedScript.partCategory == slot.partCategory)
                {
                    partToInstall = item;
                    break; 
                }
            }
        }

        if (partToInstall == null) return;
        Debug.Log($"Installing {partToInstall.GetComponent<InspectableItem>().itemName}");
        ClearHighlight();
        playerStorage.Remove(partToInstall);

        partToInstall.transform.SetParent(slot.transform.parent, false);
        partToInstall.transform.localPosition = slot.transform.localPosition;
        partToInstall.transform.localRotation = slot.transform.localRotation;
        partToInstall.transform.localScale    = slot.transform.localScale;
        
        SetLayerRecursively(partToInstall, currentClone.layer);
        partToInstall.SetActive(true);

        InspectableItem newPartScript = partToInstall.GetComponent<InspectableItem>();
        newPartScript.isRemovable     = true;
        newPartScript.isInventorySlot = false;

        if (slot.blockingParts != null)
            newPartScript.blockingParts = new List<InspectableItem>(slot.blockingParts);

        InspectableItem[] allItems = currentClone.GetComponentsInChildren<InspectableItem>(true);
        foreach (var item in allItems)
        {
            if (item.blockingParts != null && item.blockingParts.Contains(slot))
            {
                item.blockingParts.Remove(slot);
                item.blockingParts.Add(newPartScript);
            }
        }

        allPorts.RemoveAll(p => p == null || p.transform.IsChildOf(slot.transform));
        foreach (var p in partToInstall.GetComponentsInChildren<InspectableItem>())
        {
            if (p.isWirePort) allPorts.Add(p);
        }

        Destroy(slot.gameObject);
        HideAllGhostSlots();
        StartCoroutine(AnimateInstall(partToInstall));
    }

    // ─────────────────────────────────────────────────────────────────────────────
    //  INSTALLATION ANIMATION
    // ─────────────────────────────────────────────────────────────────────────────
    private IEnumerator AnimateInstall(GameObject obj)
    {
        Vector3    finalPos   = obj.transform.position;
        Vector3    finalScale = obj.transform.localScale;
        Quaternion finalRot   = obj.transform.rotation;

        Vector3 startPos = inspectionCamera.ViewportToWorldPoint(new Vector3(0.1f, 0.1f, 0.3f));
        Vector3 arcMid   = Vector3.Lerp(startPos, finalPos, 0.6f)
                        + inspectionCamera.transform.up * 0.25f;

        obj.transform.position   = startPos;
        obj.transform.localScale = finalScale * 0.3f;
        obj.transform.rotation   = finalRot;

        foreach (Collider c in obj.GetComponentsInChildren<Collider>()) c.enabled = false;

        float glideDuration = 0.4f;
        float elapsed = 0f;

        while (elapsed < glideDuration)
        {
            if (obj == null) yield break;
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / glideDuration);

            Vector3 pos = (1 - t) * (1 - t) * startPos
                        + 2 * (1 - t) * t   * arcMid
                        + t * t             * finalPos;

            obj.transform.position   = pos;
            obj.transform.localScale = Vector3.Lerp(finalScale * 0.3f, finalScale * 1.1f, t);
            obj.transform.rotation   = Quaternion.Slerp(obj.transform.rotation, finalRot, t * 3f);

            yield return null;
        }

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

            obj.transform.localScale = new Vector3(
                finalScale.x * scaleX,
                finalScale.y * scaleY,
                finalScale.z * scaleZ);

            obj.transform.position = finalPos;
            obj.transform.rotation = Quaternion.Lerp(obj.transform.rotation, finalRot, t * 10f);

            yield return null;
        }

        if (obj != null)
        {
            obj.transform.position   = finalPos;
            obj.transform.localScale = finalScale;
            obj.transform.rotation   = finalRot;
            foreach (Collider c in obj.GetComponentsInChildren<Collider>()) c.enabled = true;
        }
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

            if (wireHeadPrefab != null)
                activeWireHead = SpawnWireHead(clickedPort.transform, currentClone.transform);
            else
            {
                activeWireHead = new GameObject("DynamicCable_Head");
                activeWireHead.transform.SetParent(currentClone.transform);
                activeWireHead.transform.position = clickedPort.transform.position;
                activeWireHead.transform.rotation = clickedPort.transform.rotation;
            }

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
        }
        else
        {
            if (clickedPort == wireStartPortItem) { CancelWiring(); return; }
            if (!IsCompatible(wireStartPortItem, clickedPort)) return;
            if (clickedPort.isOccupied) return;
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

        InspectableItem devicePort;
        InspectableItem psuPort;

        if (startPort.placeHeadHere && !endPort.placeHeadHere)
        { devicePort = startPort; psuPort = endPort; }
        else if (endPort.placeHeadHere && !startPort.placeHeadHere)
        { devicePort = endPort; psuPort = startPort; }
        else
        { devicePort = startPort.isPSUPort ? endPort : startPort; psuPort = startPort.isPSUPort ? startPort : endPort; }

        activeRibbonDir = devicePort.ribbonAxis.normalized;
        if (activeRibbonDir.sqrMagnitude < 0.001f) activeRibbonDir = Vector3.up;
        activeEndRibbonDir = psuPort.ribbonAxis.normalized;
        if (activeEndRibbonDir.sqrMagnitude < 0.001f) activeEndRibbonDir = Vector3.right;

        Vector3 startPos = devicePort.transform.position;
        Vector3 endPos   = psuPort.transform.position;
        float   cLen     = Vector3.Distance(startPos, endPos);
        float   sag      = Mathf.Max(cLen * cableSag, 0.05f);

        Vector3[] finalPath;
        if (CableRouteManager.Instance != null)
            finalPath = CableRouteManager.Instance.GetRoute(startPort.connectorType, startPos, endPos);
        else
        {
            Vector3 behindBoard = startPos - devicePort.transform.forward * (cLen * 0.25f);
            behindBoard.y = startPos.y - sag * 0.5f;
            Vector3 risePoint = new Vector3(endPos.x, endPos.y - cLen * 0.25f, endPos.z);
            finalPath = new Vector3[] { startPos, behindBoard, risePoint, endPos };
        }

        List<LineRenderer> lrSnapshot   = new List<LineRenderer>(activeWireLines);
        string             snapConnType = activeWireConnectorType;

        foreach (LineRenderer lr in lrSnapshot)
            if (lr != null) lr.transform.SetParent(currentClone.transform);

        if (activeWireHead != null) { Destroy(activeWireHead); activeWireHead = null; }

        if (devicePort.wireHead != null) devicePort.wireHead.SetActive(true);
        else devicePort.wireHead = SpawnWireHead(devicePort.transform, currentClone.transform);

        if (psuPort.wireHead != null) psuPort.wireHead.SetActive(true);
        else psuPort.wireHead = SpawnWireHead(psuPort.transform, currentClone.transform);

        GameObject wireColliderGO = new GameObject("WireCollider");
        wireColliderGO.transform.SetParent(currentClone.transform);
        wireColliderGO.transform.position = Vector3.Lerp(startPos, endPos, 0.5f);
        wireColliderGO.transform.LookAt(endPos);
        SetLayerRecursively(wireColliderGO, currentClone.layer);

        BoxCollider lineCol = wireColliderGO.AddComponent<BoxCollider>();
        lineCol.size = new Vector3(0.06f, 0.06f, Vector3.Distance(startPos, endPos));

        InspectableItem wireItem = wireColliderGO.AddComponent<InspectableItem>();
        wireItem.itemName        = $"{startPort.connectorType} Cable";
        wireItem.itemDescription = $"Connects {startPort.itemName} to {endPort.itemName}";
        wireItem.isRemovable     = true;

        startPort.isOccupied = true; startPort.connectedTo = endPort; startPort.attachedWire = wireColliderGO;
        endPort.isOccupied   = true; endPort.connectedTo   = startPort; endPort.attachedWire  = wireColliderGO;

        AddWireBlocker(startPort, wireItem);
        AddWireBlocker(endPort,   wireItem);

        var wireCleaner = wireColliderGO.AddComponent<WireCleanup>();
        wireCleaner.portA = startPort;
        wireCleaner.portB = endPort;

        DrawCommittedCurve(lrSnapshot, snapConnType, startPos, endPos, sag, activeRibbonDir, activeEndRibbonDir);

        if (activeSnapCoroutine != null) StopCoroutine(activeSnapCoroutine);
        activeSnapCoroutine = StartCoroutine(AnimateCableSnap(lrSnapshot, snapConnType, finalPath, 0.4f, activeRibbonDir, activeEndRibbonDir));

        activeWireTail = null; activeWireLine = null; activeWireLines.Clear();
        wireStartPortItem = null; wireStartTransform = null; activeWireConnectorType = "";
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
        activeWireLine = null; wireStartPortItem = null; wireStartTransform = null; activeWireConnectorType = "";
    }

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

    Spline BuildSpline(Vector3[] waypoints)
    {
        var spline = new Spline();
        foreach (var wp in waypoints)
            spline.Add(new BezierKnot(wp), TangentMode.AutoSmooth);
        return spline;
    }

    Vector3[] SampleSpline(Spline spline, int resolution)
    {
        var pts = new Vector3[resolution];
        for (int i = 0; i < resolution; i++)
        {
            float t = i / (float)(resolution - 1);
            pts[i] = spline.EvaluatePosition(t);
        }
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
        float sag      = Mathf.Max(cableLen * cableSag, 0.05f);

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
        int   strandCount   = activeWireLines.Count;
        float strandSpacing = profile != null ? profile.strandSpacing : 0.003f;
        float totalWidth    = strandSpacing * (strandCount - 1);

        Vector3 startRibbonDir = wireStartPortItem != null ? wireStartPortItem.ribbonAxis.normalized : inspectionCamera.transform.up;
        if (startRibbonDir.sqrMagnitude < 0.001f) startRibbonDir = Vector3.up;

        Vector3 endRibbonDir = startRibbonDir;
        if (lastHitObject != null)
        {
            InspectableItem hovered = lastHitObject.GetComponent<InspectableItem>();
            if (hovered != null && hovered.isWirePort && hovered != wireStartPortItem)
            {
                endRibbonDir = hovered.ribbonAxis.normalized;
                if (endRibbonDir.sqrMagnitude < 0.001f) endRibbonDir = Vector3.right;
            }
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
                float   t         = i / (float)(cableResolution - 1);
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
        int   strandCount   = lines.Count;
        float strandSpacing = profile != null ? profile.strandSpacing : 0.003f;
        float totalWidth    = strandSpacing * (strandCount - 1);
        float cableLen      = Vector3.Distance(startPos, endPos);
        float useSag        = Mathf.Max(cableLen * cableSag, sag);

        Vector3 mid = new Vector3(Mathf.Lerp(startPos.x, endPos.x, 0.3f), startPos.y - useSag, Mathf.Lerp(startPos.z, endPos.z, 0.3f));
        Spline   baseSpline = BuildSpline(new Vector3[] { startPos, mid, endPos });
        Vector3[] basePts   = SampleSpline(baseSpline, cableResolution);

        for (int s = 0; s < strandCount; s++)
        {
            LineRenderer lr = lines[s];
            if (lr == null) continue;
            lr.useWorldSpace = true;
            lr.positionCount = cableResolution;
            for (int i = 0; i < cableResolution; i++)
            {
                float   t         = i / (float)(cableResolution - 1);
                Vector3 ribbonDir = Vector3.Slerp(startRibbonDir, endRibbonDir, t).normalized;
                lr.SetPosition(i, basePts[i] + StrandOffset(s, strandCount, totalWidth, ribbonDir));
            }
        }
    }

    private IEnumerator AnimateCableSnap(List<LineRenderer> lrSnapshot, string connType,
        Vector3[] routePositions, float duration, Vector3 startRibbonDir, Vector3 endRibbonDir)
    {
        if (lrSnapshot == null || lrSnapshot.Count == 0) yield break;

        int res = cableResolution;
        CableTypeProfile profile = CableProfile.Instance?.Get(connType);
        int   strandCount   = lrSnapshot.Count;
        float strandSpacing = profile != null ? profile.strandSpacing : 0.003f;
        float totalWidth    = strandSpacing * (strandCount - 1);

        Vector3[][] fromPositions = new Vector3[strandCount][];
        for (int s = 0; s < strandCount; s++)
        {
            fromPositions[s] = new Vector3[res];
            LineRenderer lr = lrSnapshot[s];
            if (lr != null && lr.positionCount == res) lr.GetPositions(fromPositions[s]);
            else if (lr != null) for (int i = 0; i < res; i++) fromPositions[s][i] = lr.transform.position;
        }

        Spline   baseRouteSpline = BuildSpline(routePositions);
        Vector3[] baseRoutePts   = SampleSpline(baseRouteSpline, res);

        Vector3[][] targetPositions = new Vector3[strandCount][];
        for (int s = 0; s < strandCount; s++)
        {
            targetPositions[s] = new Vector3[res];
            for (int i = 0; i < res; i++)
            {
                float   t         = i / (float)(res - 1);
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
                for (int i = 0; i < res; i++)
                    lr.SetPosition(i, Vector3.Lerp(fromPositions[s][i], targetPositions[s][i], eased));
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

    GameObject SpawnWireHead(Transform port, Transform parent)
    {
        if (wireHeadPrefab == null) return null;
        GameObject head = Instantiate(wireHeadPrefab, port.position, port.rotation, parent);
        head.transform.Translate(wireHeadOffset, Space.Self);
        SetLayerRecursively(head, parent.gameObject.layer);
        return head;
    }

    void TogglePCPower()
    {
        isPCOn = !isPCOn;
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

    void HandleHover()
    {
        // Removed the dust check that was stopping the highlights from appearing!

        Ray ray = inspectionCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, 100f, ~0);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        GameObject hitObj = null;
        InspectableItem part = null;

        foreach (RaycastHit hit in hits)
        {
            InspectableItem candidate = hit.collider.gameObject.GetComponent<InspectableItem>();
            if (candidate == null) continue;
            if (candidate.isMainObject) continue;
            if (isPlacingFromInventory && !candidate.isInventorySlot) continue;
            hitObj = hit.collider.gameObject;
            part = candidate;
            break;
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
            extra = $"\n<size=80%>[{part.connectorType}] {side} port" + (part.isOccupied ? " - CONNECTED" : " - empty") + "</size>";
        }
        else if (part.isRemovable)
            extra = "\n<size=75%><color=#FFD84A>Hold to remove</color></size>";
        else if (part.isInventorySlot)
            extra = "\n<size=75%><color=#4AE0FF>Hold to install</color></size>";

        tooltipPanel.SetActive(true);
        if (tooltipTitle) tooltipTitle.text = part.itemName;
        if (tooltipBody)  tooltipBody.text  = part.itemDescription + extra;
    }

    void MoveTooltip()
    {
        if (tooltipPanel)
            tooltipPanel.transform.position = Input.mousePosition + new Vector3(20, -20, 0);
    }

    private IEnumerator AnimateRemovalAndDestroy(GameObject obj)
    {
        foreach (Collider c in obj.GetComponentsInChildren<Collider>()) c.enabled = false;

        Vector3 startPos   = obj.transform.position;
        Vector3 startScale = obj.transform.localScale;

        Dictionary<Renderer, Material[]> originalMats = new Dictionary<Renderer, Material[]>();
        foreach (Renderer r in obj.GetComponentsInChildren<Renderer>())
            originalMats[r] = r.materials;

        float pullDuration = 0.3f;
        float elapsed = 0f;
        Vector3 pullDir    = (inspectionCamera.transform.position - startPos).normalized;
        Vector3 pullTarget = startPos + pullDir * 0.25f;

        while (elapsed < pullDuration)
        {
            if (obj == null) yield break;
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / pullDuration);
            obj.transform.position   = Vector3.Lerp(startPos, pullTarget, t);
            obj.transform.localScale = startScale * (1f + Mathf.Sin(t * Mathf.PI) * 0.08f);
            foreach (var kv in originalMats)
            {
                if (kv.Key == null) continue;
                foreach (Material mat in kv.Key.materials)
                    if (mat.HasProperty("_EmissionColor")) { mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", Color.white * t * 1.5f); }
            }
            yield return null;
        }

        float flyDuration = 0.35f;
        elapsed = 0f;
        Vector3 flyStart  = obj.transform.position;
        Vector3 flyTarget = inspectionCamera.ViewportToWorldPoint(new Vector3(0.1f, 0.1f, 0.3f));
        Vector3 arcMid    = Vector3.Lerp(flyStart, flyTarget, 0.5f) + inspectionCamera.transform.up * 0.3f;

        while (elapsed < flyDuration)
        {
            if (obj == null) yield break;
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / flyDuration);
            obj.transform.position   = (1-t)*(1-t)*flyStart + 2*(1-t)*t*arcMid + t*t*flyTarget;
            obj.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t*t);
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

    void HandleInput()
    {
        if (Input.GetMouseButton(1) && !isWiring)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible   = false;
            
            // Camera rotation ONLY happens here now!
            targetOrbitAngles.y += Input.GetAxis("Mouse X") * orbitSpeed;
            targetOrbitAngles.x -= Input.GetAxis("Mouse Y") * orbitSpeed;
            targetOrbitAngles.x  = Mathf.Clamp(targetOrbitAngles.x, -89f, 89f);
        }
        else { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }

        if (Input.GetMouseButton(2))
        {
            Quaternion camRot = Quaternion.Euler(targetOrbitAngles.x, targetOrbitAngles.y, 0);
            targetFocusPoint += (camRot * Vector3.right * -Input.GetAxis("Mouse X") + camRot * Vector3.up * -Input.GetAxis("Mouse Y")) * panSpeed * 0.5f;
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
        inspectionCamera.transform.position = voidAnchor.transform.position + rotation * new Vector3(0, 0, -currentDistance) + focusPoint;
        inspectionCamera.transform.rotation = rotation;
    }

    public void ResetView()
    {
        targetFocusPoint  = Vector3.zero;
        targetDistance    = optimalDistance;
        targetOrbitAngles = Vector2.zero;
    }

    void SetLayerRecursively(GameObject obj, int newLayer)
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
}
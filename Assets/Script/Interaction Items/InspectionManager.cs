using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using TMPro;
using System.Collections.Generic;

public class InspectionManager : MonoBehaviour
{
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

    // Internal State
    private GameObject currentClone;
    private GameObject voidAnchor;
    private bool isInspecting = false;

    private Vector3 focusPoint;
    private Vector3 targetFocusPoint;
    private float currentDistance;
    private float targetDistance;
    private Vector2 orbitAngles;
    private Vector2 targetOrbitAngles;

    private GameObject lastHitObject;
    
    // CHANGED: We now use a Dictionary to remember materials for MANY parts at once
    private Dictionary<Renderer, Material[]> originalMaterialCache = new Dictionary<Renderer, Material[]>();
    
    private float optimalDistance;

    void Start()
    {
        // 1. START CLEAR (Turn off Blur immediately)
        if (blurVolume != null) blurVolume.weight = 0;

        voidAnchor = new GameObject("Void_Inspection_Anchor");
        voidAnchor.transform.position = new Vector3(0, -1000, 0);

        // Auto-Stack Cameras
        if (mainCamera && inspectionCamera)
        {
            var mainCamData = mainCamera.GetUniversalAdditionalCameraData();
            var overlayCamData = inspectionCamera.GetUniversalAdditionalCameraData();

            overlayCamData.renderType = CameraRenderType.Overlay;

            bool isStacked = false;
            foreach (var cam in mainCamData.cameraStack)
            {
                if (cam == inspectionCamera) isStacked = true;
            }
            if (!isStacked) mainCamData.cameraStack.Add(inspectionCamera);
        }

        if (inspectionCamera)
        {
            inspectionCamera.transform.parent = null;
            inspectionCamera.gameObject.SetActive(false);
        }

        if (controlsUI) controlsUI.SetActive(false);
        if (infoPanel) infoPanel.SetActive(false);
        if (tooltipPanel) tooltipPanel.SetActive(false);
    }

    void Update()
    {
        if (!isInspecting || currentClone == null) return;

        HandleInput();
        ApplyCameraMovement();
        HandleHover();

        if (Input.GetKeyDown(KeyCode.R)) ResetView();
        if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.X)) StopInspection();
    }

    public void Inspect(InspectableItem originalItem)
    {
        isInspecting = true;

        // 2. TURN ON BLUR
        if (blurVolume != null) blurVolume.weight = 1;

        if (controlsUI) controlsUI.SetActive(true);
        if (infoPanel) infoPanel.SetActive(true);
        if (gameplayUI) gameplayUI.SetActive(false);
        if (nameText) nameText.text = originalItem.itemName;
        if (descText) descText.text = originalItem.itemDescription;

        if (mainCamera) mainCamera.gameObject.SetActive(true);
        if (inspectionCamera) inspectionCamera.gameObject.SetActive(true);

        if (playerMovement) playerMovement.enabled = false;
        if (cameraScript) cameraScript.enabled = false;
        if (interactScript) interactScript.enabled = false;
        if (playerAnimator) playerAnimator.enabled = false;

        if (playerRootObject)
        {
            Renderer[] renderers = playerRootObject.GetComponentsInChildren<Renderer>();
            foreach (Renderer r in renderers) r.enabled = false;
        }

        if (voidAnchor == null)
        {
            voidAnchor = new GameObject("Void_Inspection_Anchor");
            voidAnchor.transform.position = new Vector3(0, -1000, 0);
        }

        currentClone = Instantiate(originalItem.gameObject, voidAnchor.transform);
        currentClone.transform.localPosition = Vector3.zero;
        currentClone.transform.localRotation = Quaternion.identity;
        currentClone.transform.localScale = Vector3.one;

        Destroy(currentClone.GetComponent<Rigidbody>());

        // --- NEW FIX: DELETE THE PARENT SCRIPT ON THE CLONE ---
        // This stops the entire PC from turning green!
        InspectableItem rootScript = currentClone.GetComponent<InspectableItem>();
        if (rootScript != null) Destroy(rootScript);

        // --- TURN OFF THE OUTER SHELL COLLIDER ---
        // Lets your mouse laser pass through the invisible outer box
        Collider mainCollider = currentClone.GetComponent<Collider>();
        if (mainCollider != null) mainCollider.enabled = false; 

        SetLayerRecursively(currentClone, LayerMask.NameToLayer("InspectLayer"));

        // Auto-Frame
        Bounds bounds = new Bounds(currentClone.transform.position, Vector3.zero);
        Renderer[] renderersClone = currentClone.GetComponentsInChildren<Renderer>();
        if (renderersClone.Length > 0) foreach (Renderer r in renderersClone) bounds.Encapsulate(r.bounds);

        Vector3 centerOffset = voidAnchor.transform.position - bounds.center;
        currentClone.transform.position += centerOffset;

        float objectSize = bounds.extents.magnitude;
        if (objectSize == 0) objectSize = 1f;

        float fov = inspectionCamera.fieldOfView * 0.5f * Mathf.Deg2Rad;
        float distanceRequired = objectSize / Mathf.Sin(fov);
        optimalDistance = distanceRequired * 2.0f;

        targetDistance = optimalDistance;
        currentDistance = optimalDistance;
        targetFocusPoint = Vector3.zero;
        focusPoint = Vector3.zero;
        targetOrbitAngles = new Vector2(0, 0);
        orbitAngles = targetOrbitAngles;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void StopInspection()
    {
        isInspecting = false;

        // 3. TURN OFF BLUR
        if (blurVolume != null) blurVolume.weight = 0;

        ClearHighlight();

        if (currentClone != null) Destroy(currentClone);

        if (infoPanel) infoPanel.SetActive(false);
        if (tooltipPanel) tooltipPanel.SetActive(false);
        if (controlsUI) controlsUI.SetActive(false);
        if (gameplayUI) gameplayUI.SetActive(true);

        if (inspectionCamera) inspectionCamera.gameObject.SetActive(false);

        if (playerMovement) playerMovement.enabled = true;
        if (cameraScript) cameraScript.enabled = true;
        if (interactScript) interactScript.enabled = true;
        if (playerAnimator) playerAnimator.enabled = true;

        if (playerRootObject)
        {
            Renderer[] renderers = playerRootObject.GetComponentsInChildren<Renderer>();
            foreach (Renderer r in renderers) r.enabled = true;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // HELPERS
    void HandleInput()
    {
        if (Input.GetMouseButton(1))
        {
            Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false;
            targetOrbitAngles.y += Input.GetAxis("Mouse X") * orbitSpeed;
            targetOrbitAngles.x -= Input.GetAxis("Mouse Y") * orbitSpeed;
            targetOrbitAngles.x = Mathf.Clamp(targetOrbitAngles.x, -89f, 89f);
        }
        else { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
        float inputX = Input.GetAxis("Horizontal"); float inputY = Input.GetAxis("Vertical");
        if (inputX != 0 || inputY != 0)
        {
            Quaternion camRot = Quaternion.Euler(targetOrbitAngles.x, targetOrbitAngles.y, 0);
            Vector3 right = camRot * Vector3.right; Vector3 up = camRot * Vector3.up;
            targetFocusPoint += ((right * inputX) + (up * inputY)) * panSpeed * Time.deltaTime;
        }
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0) { targetDistance -= scroll * zoomSpeed; targetDistance = Mathf.Clamp(targetDistance, optimalDistance * 0.1f, optimalDistance * 5f); }
    }

    void ApplyCameraMovement()
    {
        float dt = Time.deltaTime * smoothTime;
        orbitAngles = Vector2.Lerp(orbitAngles, targetOrbitAngles, dt);
        currentDistance = Mathf.Lerp(currentDistance, targetDistance, dt);
        focusPoint = Vector3.Lerp(focusPoint, targetFocusPoint, dt);
        Quaternion rotation = Quaternion.Euler(orbitAngles.x, orbitAngles.y, 0);
        Vector3 negDistance = new Vector3(0.0f, 0.0f, -currentDistance);
        Vector3 position = rotation * negDistance + focusPoint;
        inspectionCamera.transform.position = voidAnchor.transform.position + position;
        inspectionCamera.transform.rotation = rotation;
    }

    // --- UPDATED HOVER LOGIC ---
    void HandleHover()
    {
        Ray ray = inspectionCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        // CHANGED: "~0" means "Hit Everything". 
        // This ensures we hit the Box Collider on the NPC layer first!
        int layerMask = ~0; 

        if (Physics.Raycast(ray, out hit, 100f, layerMask))
        {
            GameObject hitObj = hit.collider.gameObject;
            
            // SMART CHECK: Look for script on object. If not found, look at Parent!
            InspectableItem part = hitObj.GetComponent<InspectableItem>();
            if (part == null)
            {
                part = hitObj.GetComponentInParent<InspectableItem>();
                if (part != null) hitObj = part.gameObject; // Redirect hit to the parent
            }

            if (part != null)
            {
                // Only highlight if it's a new object
                if (hitObj != lastHitObject)
                {
                    ClearHighlight(); // Clear old one
                    HighlightObject(hitObj); // Highlight new one (and all its children)
                    ShowTooltip(part);
                }
                MoveTooltip();
                return;
            }
        }
        
        // If we hit nothing (or a wall with no script), clear highlight
        ClearHighlight();
        if (tooltipPanel) tooltipPanel.SetActive(false);
    }

    // --- UPDATED HIGHLIGHT LOGIC ---
    void HighlightObject(GameObject obj)
    {
        lastHitObject = obj;
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();

        foreach (Renderer rend in renderers)
        {
            // 1. Save original materials
            if (!originalMaterialCache.ContainsKey(rend))
            {
                originalMaterialCache.Add(rend, rend.sharedMaterials);
            }

            // 2. Create new array of the SAME size (Swap, don't Add)
            Material[] newMats = new Material[rend.sharedMaterials.Length];
            
            // 3. Fill every slot with Green Glass
            for (int i = 0; i < newMats.Length; i++)
            {
                newMats[i] = highlightMaterial;
            }
            
            // 4. Apply
            rend.sharedMaterials = newMats;
        }
    }

    void ClearHighlight()
    {
        // Restore materials for everything we painted
        foreach (KeyValuePair<Renderer, Material[]> entry in originalMaterialCache)
        {
            if (entry.Key != null)
            {
                entry.Key.sharedMaterials = entry.Value;
            }
        }
        
        originalMaterialCache.Clear();
        lastHitObject = null;
    }

    void ShowTooltip(InspectableItem part) { if (tooltipPanel) { tooltipPanel.SetActive(true); if (tooltipTitle) tooltipTitle.text = part.itemName; if (tooltipBody) tooltipBody.text = part.itemDescription; } }
    void MoveTooltip() { if (tooltipPanel) tooltipPanel.transform.position = Input.mousePosition + new Vector3(20, -20, 0); }
    public void ResetView() { targetFocusPoint = Vector3.zero; targetDistance = optimalDistance; targetOrbitAngles = new Vector2(0, 0); }
    void SetLayerRecursively(GameObject obj, int newLayer) { obj.layer = newLayer; foreach (Transform child in obj.transform) SetLayerRecursively(child.gameObject, newLayer); }
}
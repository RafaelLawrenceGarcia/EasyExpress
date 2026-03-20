using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class ScrewdriverInteraction : MonoBehaviour
{
    [Header("Screwdriver 3D Model")]
    public Transform screwdriverModelRoot;
    [Tooltip("How far the handle sits behind the screw toward the camera.")]
    public float handleDistance = 0.15f;
    
    [Header("Screwdriver Animation")]
    public float driverSpinSpeed = 720f;
    [Tooltip("Change these if your screwdriver points sideways instead of at the screw!")]
    public Vector3 modelRotationFix = Vector3.zero;

    [Header("Screw Spin")]
    [Tooltip("Keep this at 0 if your screws are combined into one 3D object to prevent the 'Windmill' glitch!")]
    public float screwSpinSpeed = 0f; 

    [Header("Hold Duration")]
    public float holdDuration = 1.4f;

    [Header("Radial Progress UI")]
    public Canvas progressCanvas;
    public Image radialFillImage;
    public TextMeshProUGUI progressLabel;
    public Color ringColorStart = new Color(1f, 0.85f, 0.2f);
    public Color ringColorEnd   = new Color(0.2f, 1f, 0.45f);
    public Vector2 ringScreenOffset = new Vector2(0f, 0f);

    [Header("Camera Reference")]
    public Camera inspectionCamera;

    public System.Action onCancelled;

    private bool          isUnscrewing       = false;
    private float         holdProgress       = 0f;
    private Transform     targetPart         = null;
    private System.Action onCompleteCallback = null;

    private Coroutine lockCoroutine;
    private Coroutine screwSpinCoroutine;
    private Coroutine hideCoroutine;

    private RectTransform progressRT;
    
    // Stores the EXACT pixel/3D coordinate where you clicked
    private Vector3 exactScrewPos;

    void Awake()
    {
        if (screwdriverModelRoot)
        {
            SetLayerRecursively(screwdriverModelRoot.gameObject, LayerMask.NameToLayer("InspectLayer"));
            screwdriverModelRoot.gameObject.SetActive(false);
        }

        if (progressCanvas)
        {
            progressRT = progressCanvas.GetComponent<RectTransform>();
            progressCanvas.gameObject.SetActive(false);
        }

        if (radialFillImage) radialFillImage.fillAmount = 0f;
    }

    void Update()
    {
        if (!isUnscrewing) return;

        if (!Input.GetMouseButton(0))
        {
            CancelUnscrewing();
            return;
        }

        holdProgress += Time.deltaTime / holdDuration;
        holdProgress  = Mathf.Clamp01(holdProgress);

        UpdateRingUI(holdProgress);

        if (holdProgress >= 1f)
            CompleteUnscrewing();
    }

    public void BeginUnscrewing(Transform part, System.Action onComplete)
    {
        if (isUnscrewing) return;

        isUnscrewing       = true;
        holdProgress       = 0f;
        targetPart         = part;
        onCompleteCallback = onComplete;

        // --- THE FIX: Find exactly where your mouse clicked to ignore broken pivots! ---
        exactScrewPos = part.position; 
        if (inspectionCamera != null)
        {
            Ray ray = inspectionCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit[] hits = Physics.RaycastAll(ray, 100f);
            foreach (RaycastHit hit in hits)
            {
                if (hit.transform == part || hit.transform.IsChildOf(part))
                {
                    exactScrewPos = hit.point;
                    break;
                }
            }
        }

        if (screwdriverModelRoot != null)
        {
            SetLayerRecursively(screwdriverModelRoot.gameObject, LayerMask.NameToLayer("InspectLayer"));
            screwdriverModelRoot.gameObject.SetActive(true);

            SnapToScrew();

            StopSafe(ref lockCoroutine);
            lockCoroutine = StartCoroutine(LockTipToScrew());
        }

        // Only spin the screw if the speed is above 0
        StopSafe(ref screwSpinCoroutine);
        if (screwSpinSpeed > 0f)
        {
            screwSpinCoroutine = StartCoroutine(SpinScrew(part));
        }

        if (progressCanvas  != null) progressCanvas.gameObject.SetActive(true);
        if (radialFillImage != null) radialFillImage.fillAmount = 0f;
        UpdateRingUI(0f);

        if (progressLabel) progressLabel.text = "Hold to remove...";
    }

    public void CancelUnscrewing()
    {
        if (!isUnscrewing) return;

        isUnscrewing       = false;
        holdProgress       = 0f;
        onCompleteCallback = null;

        StopSafe(ref lockCoroutine);
        StopSafe(ref screwSpinCoroutine);

        if (progressCanvas       != null) progressCanvas.gameObject.SetActive(false);
        if (screwdriverModelRoot != null) screwdriverModelRoot.gameObject.SetActive(false);
        if (radialFillImage      != null) radialFillImage.fillAmount = 0f;
        if (progressLabel        != null) progressLabel.text = "";

        onCancelled?.Invoke();
    }

    void CompleteUnscrewing()
    {
        isUnscrewing = false;

        StopSafe(ref lockCoroutine);
        StopSafe(ref screwSpinCoroutine);
        StopSafe(ref hideCoroutine);

        if (radialFillImage)
        {
            radialFillImage.color      = ringColorEnd;
            radialFillImage.fillAmount = 1f;
        }
        if (progressLabel) progressLabel.text = "Removed!";

        hideCoroutine = StartCoroutine(FlashAndHide());

        onCompleteCallback?.Invoke();
        onCompleteCallback = null;
    }

    void StopSafe(ref Coroutine c)
    {
        if (c != null) { StopCoroutine(c); c = null; }
    }

    void SnapToScrew()
    {
        if (inspectionCamera == null || screwdriverModelRoot == null) return;
        
        Vector3 camToScrew = (exactScrewPos - inspectionCamera.transform.position).normalized;
        screwdriverModelRoot.rotation = Quaternion.LookRotation(camToScrew, inspectionCamera.transform.up);
        screwdriverModelRoot.position = exactScrewPos - (camToScrew * handleDistance);
    }

    void UpdateRingUI(float t)
    {
        if (radialFillImage)
        {
            radialFillImage.fillAmount = t;
            radialFillImage.color      = Color.Lerp(ringColorStart, ringColorEnd, t);
        }

        if (progressRT != null)
            progressRT.position = new Vector3(
                Input.mousePosition.x + ringScreenOffset.x,
                Input.mousePosition.y + ringScreenOffset.y,
                0f);
    }

    IEnumerator LockTipToScrew()
    {
        float currentSpin = 0f;

        while (screwdriverModelRoot != null && isUnscrewing)
        {
            if (inspectionCamera != null)
            {
                // 1. Aim perfectly from the camera to the screw
                Vector3 camToScrew = (exactScrewPos - inspectionCamera.transform.position).normalized;
                Quaternion baseAim = Quaternion.LookRotation(camToScrew, inspectionCamera.transform.up);

                // 2. Add the spinning animation
                currentSpin += driverSpinSpeed * Time.deltaTime;

                // 3. Apply aim, blender axis fix, and spin all at once
                screwdriverModelRoot.rotation = baseAim * Quaternion.Euler(modelRotationFix.x, modelRotationFix.y, currentSpin + modelRotationFix.z);

                // 4. Lock the TIP exactly to the exact coordinate clicked
                screwdriverModelRoot.position = exactScrewPos - (camToScrew * handleDistance);
            }
            yield return null;
        }
    }

        IEnumerator SpinScrew(Transform part)
    {
        if (part == null) yield break;
 
        // Search ALL descendants recursively for something named "screw"/"bolt"/"fastener"
        // If nothing found → null, and we simply don't spin anything (no fallback to whole part)
        Transform screwMesh = FindScrewMeshRecursive(part);
 
        if (screwMesh == null)
        {
            // This part has no screw child — skip spinning entirely
            yield break;
        }
 
        // Use the renderer bounds center so a broken pivot doesn't wobble
        Renderer rend = screwMesh.GetComponentInChildren<Renderer>();
        Vector3 center = rend != null ? rend.bounds.center : screwMesh.position;
 
        while (screwMesh != null && isUnscrewing)
        {
            Vector3 spinAxis = inspectionCamera != null
                ? (inspectionCamera.transform.position - center).normalized
                : Vector3.up;
 
            screwMesh.RotateAround(center, spinAxis, screwSpinSpeed * Time.deltaTime);
            yield return null;
        }
    }
 
    /// <summary>
    /// Recursively walks every descendant looking for "screw", "bolt", or "fastener" in the name.
    /// Returns null if nothing is found — caller must handle null (do not fall back to part root).
    /// </summary>
    Transform FindScrewMeshRecursive(Transform parent)
    {
        foreach (Transform child in parent)
        {
            string n = child.name.ToLower();
            if (n.Contains("screw") || n.Contains("bolt") || n.Contains("fastener"))
                return child;
 
            // Check deeper levels
            Transform found = FindScrewMeshRecursive(child);
            if (found != null) return found;
        }
        return null; // Nothing found
    }

    IEnumerator FlashAndHide()
    {
        yield return new WaitForSeconds(0.3f);

        if (progressCanvas       != null) progressCanvas.gameObject.SetActive(false);
        if (radialFillImage      != null) radialFillImage.fillAmount = 0f;
        yield return new WaitForSeconds(0.1f);

        if (screwdriverModelRoot != null) screwdriverModelRoot.gameObject.SetActive(false);
    }

    void SetLayerRecursively(GameObject obj, int layer)
    {
        if (layer == -1) return;

        obj.layer = layer;
        foreach (Transform child in obj.transform)
            SetLayerRecursively(child.gameObject, layer);
    }
}
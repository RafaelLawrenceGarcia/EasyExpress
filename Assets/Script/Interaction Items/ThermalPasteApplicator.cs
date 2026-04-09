// Place this file at:  Assets/Script/PC area/ThermalPasteApplicator.cs
// Attach it to the same GameObject as InspectionManager (the Player or a manager object).
//
// SETUP:
//   1. Add this component to your player / InspectionManager GameObject.
//   2. In InspectionManager, drag this component into the new
//      "Thermal Paste Applicator" field (see InspectionManager.cs changes).
//   3. Assign the progressCanvas and radialFillImage — you can reuse the
//      SAME canvas/image already used by ScrewdriverInteraction.
//   4. (Optional) Assign ThermalPasteNew and ThermalPasteDry materials
//      from Assets/Materials/PC Parts/ to get the visual swap on the part.

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// ThermalPasteApplicator — Hold left-click on a Cooler or CPU that has
/// PartFault.Overheating to apply fresh thermal paste and fix the fault.
///
/// Works identically to ScrewdriverInteraction: hold left-click to fill
/// the radial ring, release early to cancel.
/// </summary>
public class ThermalPasteApplicator : MonoBehaviour
{
    [Header("Hold Duration")]
    [Tooltip("How long the player must hold to apply the paste.")]
    public float applyDuration = 2.2f;

    [Header("Radial Progress UI")]
    [Tooltip("Can be the same Canvas used by ScrewdriverInteraction.")]
    public Canvas progressCanvas;
    public Image radialFillImage;
    public TextMeshProUGUI progressLabel;
    public Color fillColorStart = new Color(0.30f, 0.80f, 1.00f);
    public Color fillColorEnd = new Color(0.20f, 1.00f, 0.45f);
    public Vector2 ringScreenOffset = Vector2.zero;

    [Header("Thermal Paste Visual Materials (optional)")]
    [Tooltip("Assigned automatically by EasyExpressMaterialCreator. " +
             "Swap this onto the part's renderer to show fresh paste.")]
    public Material thermalPasteNewMaterial;

    [Tooltip("Assigned automatically by EasyExpressMaterialCreator. " +
             "Currently used as a reference; the part already looks correct " +
             "by default until paste is applied.")]
    public Material thermalPasteDryMaterial;

    // ── Callbacks ───────────────────────────────────────────────────
    /// <summary>Fires if the player releases the mouse before completing.</summary>
    public System.Action onCancelled;

    // ── Private state ────────────────────────────────────────────────
    private bool isApplying = false;
    private float holdProgress = 0f;
    private Transform targetTransform = null;
    private System.Action onCompleteCallback = null;
    private RectTransform progressRT;
    private Coroutine hideCoroutine;

    // ─────────────────────────────────────────────────────────────────
    //  UNITY LIFECYCLE
    // ─────────────────────────────────────────────────────────────────

    void Awake()
    {
        if (progressCanvas != null)
            progressRT = progressCanvas.GetComponent<RectTransform>();

        HideUI();
    }

    void Update()
    {
        if (!isApplying) return;

        // Cancel if mouse released
        if (!Input.GetMouseButton(0))
        {
            Cancel();
            return;
        }

        holdProgress += Time.deltaTime / applyDuration;
        holdProgress = Mathf.Clamp01(holdProgress);

        UpdateRingUI(holdProgress);

        if (holdProgress >= 1f)
            Complete();
    }

    // ─────────────────────────────────────────────────────────────────
    //  PUBLIC API  (called by InspectionManager)
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Begin applying thermal paste to the given part transform.
    /// onComplete fires when the hold finishes successfully.
    /// </summary>
    public void BeginApplying(Transform part, System.Action onComplete)
    {
        if (isApplying) return;

        isApplying = true;
        holdProgress = 0f;
        targetTransform = part;
        onCompleteCallback = onComplete;

        if (progressCanvas != null)
            progressCanvas.gameObject.SetActive(true);

        if (radialFillImage != null)
            radialFillImage.fillAmount = 0f;

        if (progressLabel != null)
            progressLabel.text = "Applying thermal paste...";

        UpdateRingUI(0f);
    }

    /// <summary>Cancel an in-progress application (called on mouse release or StopInspection).</summary>
    public void Cancel()
    {
        if (!isApplying) return;

        isApplying = false;
        holdProgress = 0f;
        onCompleteCallback = null;
        targetTransform = null;

        HideUI();
        onCancelled?.Invoke();
    }

    /// <summary>Returns true while paste is being applied.</summary>
    public bool IsApplying() => isApplying;

    // ─────────────────────────────────────────────────────────────────
    //  INTERNAL
    // ─────────────────────────────────────────────────────────────────

    void Complete()
    {
        isApplying = false;

        // Flash ring green
        if (radialFillImage != null)
        {
            radialFillImage.color = fillColorEnd;
            radialFillImage.fillAmount = 1f;
        }
        if (progressLabel != null)
            progressLabel.text = "Paste applied!";

        // Swap to fresh paste material if the part has a renderer and the material is assigned
        TrySwapToFreshPasteMaterial();

        // Fire the callback (clears the fault in InspectionManager)
        onCompleteCallback?.Invoke();
        onCompleteCallback = null;
        targetTransform = null;

        if (hideCoroutine != null) StopCoroutine(hideCoroutine);
        hideCoroutine = StartCoroutine(FlashAndHide());
    }

    void TrySwapToFreshPasteMaterial()
    {
        if (thermalPasteNewMaterial == null || targetTransform == null) return;

        // Only swap the FIRST renderer on the target — avoids clobbering
        // the whole cooler mesh (which may have multiple material slots)
        Renderer rend = targetTransform.GetComponentInChildren<Renderer>();
        if (rend == null) return;

        Material[] mats = rend.materials;
        if (mats.Length == 0) return;

        // Replace only the LAST material slot (typically the thermal-paste decal slot)
        mats[mats.Length - 1] = thermalPasteNewMaterial;
        rend.materials = mats;
    }

    void UpdateRingUI(float t)
    {
        if (radialFillImage != null)
        {
            radialFillImage.fillAmount = t;
            radialFillImage.color = Color.Lerp(fillColorStart, fillColorEnd, t);
        }

        if (progressRT != null)
        {
            progressRT.position = new Vector3(
                Input.mousePosition.x + ringScreenOffset.x,
                Input.mousePosition.y + ringScreenOffset.y,
                0f);
        }
    }

    void HideUI()
    {
        if (progressCanvas != null)
            progressCanvas.gameObject.SetActive(false);

        if (radialFillImage != null)
            radialFillImage.fillAmount = 0f;

        if (progressLabel != null)
            progressLabel.text = "";
    }

    IEnumerator FlashAndHide()
    {
        yield return new WaitForSeconds(0.35f);
        HideUI();
    }
}

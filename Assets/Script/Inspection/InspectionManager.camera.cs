// ============================================================
//  InspectionManager.Camera.cs  (partial class 2/6)
//  Camera orbit, pan, zoom, reset
//  
//  CHANGE: Camera input is blocked when any overlay panel is
//  open (Repair Manual, PC Summary, or Inventory).
// ============================================================
using UnityEngine;

public partial class InspectionManager
{
    /// <summary>
    /// Returns true if any overlay panel is currently open on top of
    /// the inspection view. Used to block camera input and prevent
    /// Escape from exiting inspection while a panel is active.
    /// </summary>
    bool IsOverlayPanelOpen()
    {
        if (RepairManual.Instance != null && RepairManual.Instance.IsOpen())
            return true;
        if (PCComponentSummary.Instance != null && PCComponentSummary.Instance.IsOpen())
            return true;
        if (inventoryUI != null && inventoryUI.inventoryPanel != null && inventoryUI.inventoryPanel.activeSelf)
            return true;
        return false;
    }

    void HandleInput()
    {
        // ── Block ALL camera input while an overlay panel is open ──
        if (IsOverlayPanelOpen())
            return;

        // Right-click drag = orbit
        if (Input.GetMouseButton(1) && !isWiring)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            targetOrbitAngles.y += Input.GetAxis("Mouse X") * orbitSpeed;
            targetOrbitAngles.x -= Input.GetAxis("Mouse Y") * orbitSpeed;
            targetOrbitAngles.x = Mathf.Clamp(targetOrbitAngles.x, -89f, 89f);
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // Middle-click drag = pan
        if (Input.GetMouseButton(2))
        {
            Quaternion camRot = Quaternion.Euler(targetOrbitAngles.x, targetOrbitAngles.y, 0);
            Vector3 right = camRot * Vector3.right * -Input.GetAxis("Mouse X");
            Vector3 up = camRot * Vector3.up * -Input.GetAxis("Mouse Y");
            targetFocusPoint += (right + up) * panSpeed * 0.5f;
        }

        // Scroll = zoom
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            targetDistance -= scroll * zoomSpeed;
            targetDistance = Mathf.Clamp(targetDistance, optimalDistance * 0.1f, optimalDistance * 5f);
        }
    }

    void ApplyCameraMovement()
    {
        float dt = Time.deltaTime * smoothTime;
        orbitAngles = Vector2.Lerp(orbitAngles, targetOrbitAngles, dt);
        currentDistance = Mathf.Lerp(currentDistance, targetDistance, dt);
        focusPoint = Vector3.Lerp(focusPoint, targetFocusPoint, dt);

        Quaternion rotation = Quaternion.Euler(orbitAngles.x, orbitAngles.y, 0);
        inspectionCamera.transform.position = voidAnchor.transform.position
            + rotation * new Vector3(0, 0, -currentDistance) + focusPoint;
        inspectionCamera.transform.rotation = rotation;
    }

    public void ResetView()
    {
        targetFocusPoint = Vector3.zero;
        targetDistance = optimalDistance;
        targetOrbitAngles = Vector2.zero;
    }
}
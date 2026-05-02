using UnityEngine;

public class MonitorInteraction : MonoBehaviour
{
    [Header("References")]
    public PCScreenController screenController;

    [Header("Settings")]
    public float interactDistance = 3f; // How close the player must be

    public Transform playerTransform; // Drag your Player object here

    // Unity calls this automatically when the player clicks the collider
    private void OnMouseDown()
    {
        // Optional: check if player is close enough
        if (playerTransform != null)
        {
            float dist = Vector3.Distance(transform.position, playerTransform.position);
            if (dist > interactDistance) return;
        }

        if (screenController != null)
        {
            // Toggle: open if closed, close if open
            if (screenController.screenCanvas.activeSelf)
                screenController.CloseScreen();
            else
                screenController.OpenScreen();
        }
    }

    // Keep this if you want to call it from another system too
    public void OnPlayerInteract()
    {
        if (screenController != null)
            screenController.OpenScreen();
    }
}
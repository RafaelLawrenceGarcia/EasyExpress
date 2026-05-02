using UnityEngine;
using System.Collections;

public class ShopDoor : MonoBehaviour
{
    [Header("Where to go?")]
    public Transform interiorSpawnPoint; // Drag the "Inside" empty object here

    [Header("UI References")]
    public GameObject loadingScreen; // Drag your Black Panel here

    [Header("Settings")]
    public float loadingTime = 2.0f; // How long to stay on black screen

    public void EnterShop(GameObject player)
    {
        StartCoroutine(TeleportSequence(player));
    }

    IEnumerator TeleportSequence(GameObject player)
    {
        // 1. Show Black Screen
        loadingScreen.SetActive(true);

        // 2. Freeze Player (Optional: prevents walking while loading)
        GTAMovement movement = player.GetComponent<GTAMovement>();
        if (movement) movement.canMove = false;

        // 3. Wait (The "Loading" feel)
        yield return new WaitForSeconds(loadingTime);

        // 4. Teleport (CRITICAL STEP FOR CHARACTER CONTROLLER)
        // You MUST turn off the Controller before moving, or it will snap back.
        CharacterController cc = player.GetComponent<CharacterController>();
        if(cc) cc.enabled = false; 

        player.transform.position = interiorSpawnPoint.position;
        player.transform.rotation = interiorSpawnPoint.rotation;

        if(cc) cc.enabled = true; // Turn it back on

        // 5. Unfreeze & Hide Screen
        yield return new WaitForSeconds(0.5f); // Small delay so we don't see the pop-in
        loadingScreen.SetActive(false);
        if (movement) movement.canMove = true;
    }
}
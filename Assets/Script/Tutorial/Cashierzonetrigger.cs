using UnityEngine;

/// <summary>
/// CashierZoneTrigger — Attach this to your Cashier Counter GameObject.
///
/// SETUP:
///   1. Select your Cashier Counter GameObject in the Hierarchy.
///   2. Add a Box Collider (or any Collider) to it.
///   3. Tick "Is Trigger" on the Collider.
///   4. Add this script (CashierZoneTrigger.cs) to the same GameObject.
///   5. Make sure your Player GameObject has the tag "Player".
///
/// That's it. When the player walks into the trigger zone,
/// it tells TutorialManager to check off Task 0 ("Head to the cashier counter").
/// </summary>
public class CashierZoneTrigger : MonoBehaviour
{
    private bool playerHasArrived = false;

    void OnTriggerEnter(Collider other)
    {
        // Only fire once, only for the player, only during the cashier step
        if (playerHasArrived) return;
        if (!other.CompareTag("Player")) return;

        if (TutorialManager.Instance != null && TutorialManager.Instance.IsTutorialActive()
            && TutorialManager.Instance.GetCurrentStep() == 3)
        {
            playerHasArrived = true;
            TutorialManager.Instance.CompleteGoToCashierTask();
        }
    }

    /// <summary>
    /// Reset so the trigger can fire again if the tutorial restarts.
    /// </summary>
    public void Reset()
    {
        playerHasArrived = false;
    }
}
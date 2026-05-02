// ============================================================
//  ITutorialQuery.cs
//  Easy Express — Tutorial Query Interface
// ============================================================
//  Location: Assets/Script/Core/ITutorialQuery.cs
//
//  WHY THIS EXISTS:
//    Events are fire-and-forget — great for notifications.
//    But some systems need to ASK "can I do this?" and get
//    a yes/no answer BEFORE acting. You can't do that with
//    an event.
//
//    Example: DoorInteractionMenu needs to check
//    "is End Day allowed?" before letting the player end the day.
//
//  HOW IT DECOUPLES:
//    WITHOUT this interface:
//      DoorInteractionMenu → depends on TutorialManager (800+ lines)
//      PlayerInteract      → depends on TutorialManager
//      StorageRoomShelf    → depends on TutorialManager
//
//    WITH this interface:
//      DoorInteractionMenu → depends on ITutorialQuery (6 methods)
//      PlayerInteract      → depends on ITutorialQuery
//      StorageRoomShelf    → depends on ITutorialQuery
//      TutorialManager implements ITutorialQuery (in Tutorial/ folder)
//
//    If you delete the entire Tutorial/ folder, the only breakage
//    is that the ITutorialQuery reference becomes null — and every
//    system treats null as "no tutorial, everything allowed."
//
//  SETUP:
//    1. Drop this file in Assets/Script/Core/
//    2. TutorialManager adds ITutorialQuery to its class declaration:
//         public partial class TutorialManager : MonoBehaviour, ITutorialQuery
//    3. Systems that query tutorial state hold an ITutorialQuery:
//         private ITutorialQuery _tutorial;
//         void Start() {
//             _tutorial = FindObjectOfType<TutorialManager>() as ITutorialQuery;
//         }
//    4. Query with null-safety:
//         if (_tutorial != null && _tutorial.IsTutorialActive() && !_tutorial.IsEndDayAllowed())
//             ShowWarning("Finish your tasks first!");
//
//  LIVES IN CORE/ BECAUSE:
//    Core/ holds shared contracts (interfaces, events, utilities)
//    that any folder can depend on. No folder depends on Core/
//    except to read these definitions.
// ============================================================

/// <summary>
/// Query interface for tutorial state.
/// Systems that need to CHECK tutorial state (not just react to events)
/// depend on this interface instead of TutorialManager directly.
///
/// If no tutorial system exists (ITutorialQuery is null), all actions
/// are allowed — systems should treat null as "no restrictions."
/// </summary>
public interface ITutorialQuery
{
    /// <summary>Is the tutorial currently running?</summary>
    bool IsTutorialActive();

    /// <summary>Can the player end the day right now?</summary>
    bool IsEndDayAllowed();

    /// <summary>Can the player inspect PCs right now?</summary>
    bool IsInspectAllowed();

    /// <summary>Can the player use the shop PC right now?</summary>
    bool IsShopPCAllowed();

    /// <summary>Can the player use the email monitor right now?</summary>
    bool IsEmailAllowed();

    /// <summary>Can the player talk to customers right now?</summary>
    bool IsCustomerTalkAllowed();
}
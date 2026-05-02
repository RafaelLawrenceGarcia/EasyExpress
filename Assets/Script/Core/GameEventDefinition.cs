// ============================================================
//  GameEvents.cs
//  Easy Express — Event Definitions
// ============================================================
//
//  All event structs live here. Each struct is PLAIN DATA —
//  no MonoBehaviour references, no GameObjects, just values.
//
//  NAMING CONVENTION:
//    [System][Action]Event
//    e.g. JobCompletedEvent, PartRemovedEvent, DayStartedEvent
//
//  ADDING NEW EVENTS:
//    1. Add a struct here
//    2. Publish it from the source system
//    3. Subscribe from any system that cares
//    4. That's it — no wiring, no inspector drag
// ============================================================

// ═══════════════════════════════════════════════════════════
//  DAY / TIME EVENTS
// ═══════════════════════════════════════════════════════════

/// <summary>Fired by DayTransitionManager when a new day begins.</summary>
public struct DayStartedEvent
{
    public int dayNumber;
}

/// <summary>Fired by DayTransitionManager when EndDay is called.</summary>
public struct DayEndingEvent
{
    public int dayNumber;
}

/// <summary>Fired by DayTimeUI when 7PM is reached.</summary>
public struct DayTimeExpiredEvent
{
    public float currentTime;  // 19.0
}

// ═══════════════════════════════════════════════════════════
//  CUSTOMER / NPC EVENTS
// ═══════════════════════════════════════════════════════════

/// <summary>A customer NPC reached their queue spot at the counter.</summary>
public struct CustomerArrivedEvent
{
    public string customerName;
    public bool isFirstInQueue;
}

/// <summary>The player accepted a customer's job.</summary>
public struct CustomerJobAcceptedEvent
{
    public string customerName;
    public int reward;
}

/// <summary>The player rejected a customer's job.</summary>
public struct CustomerJobRejectedEvent
{
    public string customerName;
}

/// <summary>A customer left the shop (served or rejected).</summary>
public struct CustomerLeftEvent
{
    public string customerName;
    public bool wasServed;
}

/// <summary>A desk PC was spawned for the first-in-queue customer.</summary>
public struct DeskPCSpawnedEvent
{
    public UnityEngine.Transform pcTransform;
    public string customerName;
}

// ═══════════════════════════════════════════════════════════
//  JOB / EMAIL EVENTS
// ═══════════════════════════════════════════════════════════

/// <summary>Player accepted an email job from the inbox.</summary>
public struct JobAcceptedEvent
{
    public string senderName;
    public string subjectLine;
    public int jobType; // 0=Repair, 1=Build
}

/// <summary>Player rejected an email job.</summary>
public struct JobRejectedEvent
{
    public string senderName;
}

/// <summary>Player completed a job (PC submitted via email).</summary>
public struct JobCompletedEvent
{
    public string customerName;
    public int starRating;
    public float basePay;
    public float earnedReward;
    public float tipBonus;
    public float totalPay;
    public int jobType;  // 0=Repair, 1=Build
    public int bonusPoints;
}

/// <summary>New emails were generated for a new day.</summary>
public struct EmailsGeneratedEvent
{
    public int emailCount;
    public int totalPending;
    public int totalAccepted;
}

// ═══════════════════════════════════════════════════════════
//  INSPECTION EVENTS
// ═══════════════════════════════════════════════════════════

/// <summary>Player entered inspection mode on a PC.</summary>
public struct InspectionEnteredEvent
{
    public string pcName;
    public bool isViewOnly;
}

/// <summary>Player exited inspection mode.</summary>
public struct InspectionExitedEvent { }

/// <summary>The inspection clone is ready (moved to void anchor).</summary>
public struct InspectionCloneReadyEvent
{
    public UnityEngine.GameObject clone;
}

/// <summary>Player hovered over a part for the first time.</summary>
public struct PartHoveredEvent
{
    public string partName;
    public string partCategory;
}

// ═══════════════════════════════════════════════════════════
//  PART REMOVAL / INSTALLATION EVENTS
// ═══════════════════════════════════════════════════════════

/// <summary>A part was removed from a PC during inspection.</summary>
public struct PartRemovedEvent
{
    public string partName;
    public string partCategory;
    public PartFault fault;
    public string sourceOwner;
}

/// <summary>A part was installed into a PC during inspection.</summary>
public struct PartInstalledEvent
{
    public string partName;
    public string partCategory;
}

/// <summary>A prebuilt wire was connected.</summary>
public struct WireConnectedEvent
{
    public string wireName;
    public bool isPowerCord;
    public string requiredPartCategory;
}

/// <summary>A prebuilt wire was disconnected.</summary>
public struct WireDisconnectedEvent
{
    public string wireName;
    public bool isPowerCord;
    public string requiredPartCategory;
}

// ═══════════════════════════════════════════════════════════
//  PC POWER EVENTS
// ═══════════════════════════════════════════════════════════

/// <summary>PC power was toggled during inspection.</summary>
public struct PCPowerToggledEvent
{
    public bool isPoweredOn;
    public int powerResult; // cast from PowerResult enum
    public string reason;
}

/// <summary>Power cord was plugged into a PC.</summary>
public struct PowerCordConnectedEvent
{
    public string pcName;
}

/// <summary>Power cord was unplugged from a PC.</summary>
public struct PowerCordDisconnectedEvent
{
    public string pcName;
}

// ═══════════════════════════════════════════════════════════
//  INVENTORY / STORAGE EVENTS
// ═══════════════════════════════════════════════════════════

/// <summary>A part was added to the player's storage (picked up from world).</summary>
public struct InventoryPartAddedEvent
{
    public string partName;
    public string partCategory;
}

/// <summary>A part was grabbed from the storage shelf UI.</summary>
public struct StoragePartGrabbedEvent
{
    public string partCategory;
}

/// <summary>The storage shelf was opened by the player.</summary>
public struct StorageShelfOpenedEvent { }

/// <summary>The storage shelf was closed by the player.</summary>
public struct StorageShelfClosedEvent { }

// ═══════════════════════════════════════════════════════════
//  SHOP EVENTS
// ═══════════════════════════════════════════════════════════

/// <summary>Player browsed a shop category.</summary>
public struct ShopCategoryBrowsedEvent
{
    public string categoryName;
}

/// <summary>Player added an item to the shopping cart.</summary>
public struct ShopItemAddedToCartEvent
{
    public string itemName;
    public float price;
}

/// <summary>Player completed a checkout.</summary>
public struct ShopCheckoutEvent
{
    public float totalCost;
    public int itemCount;
}

// ═══════════════════════════════════════════════════════════
//  DELIVERY EVENTS
// ═══════════════════════════════════════════════════════════

/// <summary>A delivery order was placed.</summary>
public struct DeliveryOrderPlacedEvent
{
    public string itemName;
    public int quantity;
    public int deliveryDays;
}

/// <summary>A delivery arrived (box spawned).</summary>
public struct DeliveryArrivedEvent
{
    public string itemName;
    public int quantity;
}

/// <summary>A delivery box was unpacked by the player.</summary>
public struct DeliveryUnpackedEvent
{
    public string itemName;
}

// ═══════════════════════════════════════════════════════════
//  BOX / PLACEMENT EVENTS
// ═══════════════════════════════════════════════════════════

/// <summary>Player picked up a box (Q key).</summary>
public struct BoxPickedUpEvent
{
    public bool isDeliveryBox;
    public bool isJobBox;
}

/// <summary>Player placed a box on a workstation/storage slot.</summary>
public struct BoxPlacedEvent
{
    public string slotTag; // "Workstation", "Storage", etc.
}

// ═══════════════════════════════════════════════════════════
//  WALLET EVENTS
// ═══════════════════════════════════════════════════════════

/// <summary>Gold was added to the wallet.</summary>
public struct GoldAddedEvent
{
    public float amount;
    public float newTotal;
}

/// <summary>Gold was spent from the wallet.</summary>
public struct GoldSpentEvent
{
    public float amount;
    public float newTotal;
}

// ═══════════════════════════════════════════════════════════
//  MONITOR / UI EVENTS
// ═══════════════════════════════════════════════════════════

/// <summary>Player opened a workstation monitor.</summary>
public struct MonitorOpenedEvent
{
    public string monitorName;
}

/// <summary>Player closed a workstation monitor.</summary>
public struct MonitorClosedEvent { }

/// <summary>Repair Manual was opened.</summary>
public struct ManualOpenedEvent { }

/// <summary>Repair Manual was closed.</summary>
public struct ManualClosedEvent { }

/// <summary>PC Summary panel was opened (G key).</summary>
public struct PCSummaryOpenedEvent { }

// ═══════════════════════════════════════════════════════════
//  TUTORIAL EVENTS
//  (Tutorial publishes these so OTHER systems can react
//   without the tutorial knowing about them)
// ═══════════════════════════════════════════════════════════

/// <summary>Tutorial step changed.</summary>
public struct TutorialStepChangedEvent
{
    public int previousStep;
    public int newStep;
}

/// <summary>Tutorial completed entirely.</summary>
public struct TutorialCompletedEvent { }

// ═══════════════════════════════════════════════════════════
//  PLAYER MOVEMENT EVENTS
// ═══════════════════════════════════════════════════════════

/// <summary>Player entered a trigger zone.</summary>
public struct PlayerEnteredZoneEvent
{
    public string zoneType; // "Cashier", "Workstation", "StorageShelf", etc.
}

// ═══════════════════════════════════════════════════════════
//  SAVE / LOAD EVENTS
// ═══════════════════════════════════════════════════════════

/// <summary>Cloud save completed.</summary>
public struct CloudSaveCompletedEvent
{
    public int currentDay;
    public float gold;
}

/// <summary>Cloud load completed.</summary>
public struct CloudLoadCompletedEvent
{
    public int currentDay;
    public float gold;
    public bool isRoomChange;
}
using UnityEngine;
using UnityEngine.AI;

public class NPCWalker : MonoBehaviour
{
    [Header("NPC Info")]
    public string npcName = "Citizen";

    [Header("Dialogue Options")]
    [TextArea] public string greeting = "Hello traveler.";
    [TextArea] public string option2Response = "Have you seen my cat?";

    [Header("Shop Invite System")]
    [Range(0f, 100f)] public float agreePercentage = 50f;
    public string agreeText = "Sure, I'll check out your shop right now!";
    public string refuseText = "No thanks, I'm too busy today.";

    [Header("Auto-Shopper System")]
    [Range(0f, 100f)] public float autoVisitPercentage = 20f;
    public string autoVisitGreeting = "I'm actually heading to your shop right now!";

    public static int incomingCustomers = 0;

    [Header("Wander Settings")]
    public float wanderRadius = 10f;
    public float waitTime = 2f;

    NavMeshAgent agent;
    float timer;
    public bool isStopped = false;
    public bool isGoingToShop = false;
    Transform shopDoorOutside;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        timer = waitTime;

        // --- AUTO-NAMING SYSTEM ---
        if (npcName == "Citizen")
        {
            npcName = GenerateRandomName();
            gameObject.name = "NPC_" + npcName;
        }

        // --- AUTO-SHOPPER LOGIC ---
        // ── BLOCK DURING TUTORIAL ──────────────────────────────
        // Don't let street NPCs auto-walk into the shop while the
        // tutorial is still running. They just wander normally instead.
        bool tutorialRunning = TutorialManager.Instance != null
                               && TutorialManager.Instance.IsTutorialActive();

        if (!tutorialRunning)
        {
            float randomRoll = Random.Range(0f, 100f);
            if (randomRoll <= autoVisitPercentage)
            {
                greeting = autoVisitGreeting;
                GoToShop();
            }
        }
        // ───────────────────────────────────────────────────────
    }

    string GenerateRandomName()
    {
        string[] firstNames = new string[] {
            "James", "Mary", "Robert", "Patricia", "John", "Jennifer", "Michael", "Linda",
            "David", "Elizabeth", "William", "Barbara", "Richard", "Susan", "Joseph", "Jessica"
        };
        string[] lastNames = new string[] {
            "Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia", "Miller", "Davis"
        };
        return firstNames[Random.Range(0, firstNames.Length)] + " " + lastNames[Random.Range(0, lastNames.Length)];
    }

    void Update()
    {
        // 1. If talking, stop moving
        if (isStopped)
        {
            agent.isStopped = true;
            return;
        }
        agent.isStopped = false;

        // 2. Check if heading to shop
        if (isGoingToShop)
        {
            // ── BLOCK DURING TUTORIAL ──────────────────────────
            // If the tutorial started after this NPC was already
            // heading to the shop, cancel it and make them wander.
            bool tutorialRunning = TutorialManager.Instance != null
                                   && TutorialManager.Instance.IsTutorialActive();
            if (tutorialRunning)
            {
                isGoingToShop = false;
                return;
            }
            // ───────────────────────────────────────────────────

            if (shopDoorOutside == null)
            {
                Debug.LogError("NPC wants to go to shop, but cannot find object with tag 'ShopExteriorDoor'!");
                isGoingToShop = false;
                return;
            }

            agent.SetDestination(shopDoorOutside.position);

            if (Vector3.Distance(transform.position, shopDoorOutside.position) < 4f)
            {
                incomingCustomers++;
                Debug.Log("Customer went inside! Total waiting: " + incomingCustomers);
                Destroy(gameObject);
            }
            return;
        }

        // 3. DEBUG: Force go to shop with 'M'
        if (Input.GetKeyDown(KeyCode.M))
        {
            GoToShop();
        }

        // 4. Random Wandering
        timer += Time.deltaTime;
        if (timer >= waitTime)
        {
            Vector3 newPos = RandomNavSphere(transform.position, wanderRadius, -1);
            agent.SetDestination(newPos);
            timer = 0;
        }
    }

    public static Vector3 RandomNavSphere(Vector3 origin, float dist, int layermask)
    {
        Vector3 randDirection = Random.insideUnitSphere * dist;
        randDirection += origin;
        NavMeshHit navHit;
        NavMesh.SamplePosition(randDirection, out navHit, dist, layermask);
        return navHit.position;
    }

    public void StartConversation()
    {
        isStopped = true;
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null) transform.LookAt(player.transform);
    }

    public void EndConversation()
    {
        isStopped = false;
        if (!isGoingToShop) timer = waitTime;
    }

    public void GoToShop()
    {
        // ── BLOCK DURING TUTORIAL ──────────────────────────────
        bool tutorialRunning = TutorialManager.Instance != null
                               && TutorialManager.Instance.IsTutorialActive();
        if (tutorialRunning) return;
        // ───────────────────────────────────────────────────────

        isGoingToShop = true;

        GameObject door = GameObject.FindGameObjectWithTag("ShopExteriorDoor");
        if (door != null)
        {
            shopDoorOutside = door.transform;
        }
        else
        {
            Debug.LogError("Could not find 'ShopExteriorDoor'. Did you tag your door?");
        }
    }

    public string TryInviteToShop()
    {
        if (isGoingToShop) return "I'm already heading there!";

        float randomRoll = Random.Range(0f, 100f);
        if (randomRoll <= agreePercentage)
        {
            GoToShop();
            return agreeText;
        }
        else
        {
            return refuseText;
        }
    }
}
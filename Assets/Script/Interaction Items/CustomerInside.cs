using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

public class CustomerInside : MonoBehaviour
{
    [Header("Details")]
    public string npcName = "Customer";
    public string jobRequest;
    public int reward;

    [Tooltip("If you leave this empty, the game will automatically generate a random PC using the lists below!")]
    public EmailData assignedJob;

    [Header("Dynamic Job Generator")]
    [Tooltip("Drag the shared PC Part Database asset here.")]
    public PCPartDatabase partDatabase;

    [Header("Browsing System")]
    public bool willBrowseFirst = true;
    public float browseTime = 5f;
    [Range(0f, 100f)] public float buyChance = 60f;
    public int minItemsToBrowse = 2;
    public int maxItemsToBrowse = 5;
    private Transform currentBrowseSpot;

    [Header("State")]
    public bool isBrowsing  = false;
    public bool isServed    = false;
    public bool isAtSpot    = false;
    public ShopCustomerSpawner mySpawner;

    private NavMeshAgent agent;
    private Transform myQueueSpot;
    private Transform exitPos;
    private bool collisionDisabled = false;

    // ── Desk PC system ────────────────────────────────────────────────────────
    private bool _pcSpawnedOnDesk = false;

    // ─────────────────────────────────────────────────────────────────────────

    public void DisableCollisionUntilAtSpot()
    {
        collisionDisabled = true;
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;
    }

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        if (npcName == "Customer")
        {
            npcName = GenerateRandomCustomerName();
            gameObject.name = "Customer_" + npcName;
        }

        GenerateRandomJob();
    }

    // =========================================================
    //  INITIALIZE — called by ShopCustomerSpawner.SpawnCustomer
    //  This is the CORRECT entry point. It sets exitPos so
    //  customers walk out instead of instantly disappearing.
    // =========================================================
    public void Initialize(Transform assignedSpot, Transform exitLocation)
    {
        myQueueSpot = assignedSpot;
        exitPos     = exitLocation;
        isAtSpot    = false;

        if (willBrowseFirst)
            StartCoroutine(BrowseStoreRoutine());
        else
            WalkToCounter();
    }

    // =========================================================
    //  ASSIGN QUEUE SPOT — called when queue shuffles forward
    //  (e.g. customer 2 moves to spot 1 after customer 1 leaves)
    // =========================================================
    public void AssignQueueSpot(Transform spot)
    {
        myQueueSpot = spot;
        isAtSpot    = false;
        _pcSpawnedOnDesk = false; // reset so desk PC spawns again if needed

        if (!isBrowsing && agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.SetDestination(myQueueSpot.position);
        }
    }

    // =========================================================
    //  BROWSE — walks to random BrowseSpot tagged objects
    //  then walks to the counter queue
    // =========================================================
    IEnumerator BrowseStoreRoutine()
{
    isBrowsing = true;

    GameObject[] spots = GameObject.FindGameObjectsWithTag("BrowseSpot");

    if (spots.Length > 0)
    {
        int itemsToBrowse = Random.Range(minItemsToBrowse, maxItemsToBrowse + 1);

        for (int i = 0; i < itemsToBrowse; i++)
        {
            // Pick a random browse spot
            Transform target = spots[Random.Range(0, spots.Length)].transform;
            currentBrowseSpot = target;

            if (target == null || agent == null || !agent.isOnNavMesh)
                continue;

            // Walk to the spot
            agent.SetDestination(target.position);

            // Wait until they actually arrive
            yield return new WaitUntil(() =>
                !agent.pathPending &&
                agent.remainingDistance <= agent.stoppingDistance + 0.3f);

            // Stop and face the shelf
            agent.velocity = Vector3.zero;
            agent.isStopped = true;

            // Slowly rotate to face the browse spot direction
            float lookTimer = 0f;
            float lookDuration = browseTime;
            Quaternion targetRot = target.rotation;

            while (lookTimer < lookDuration)
            {
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRot,
                    Time.deltaTime * 3f);
                lookTimer += Time.deltaTime;
                yield return null;
            }

            // Resume movement for next spot
            agent.isStopped = false;
        }
    }
    else
    {
        Debug.LogWarning("[CustomerInside] No BrowseSpots found! " +
                         "Tag your shelf GameObjects with 'BrowseSpot'.");
        yield return new WaitForSeconds(2f);
    }

    // Done browsing — now walk to counter and join the queue
    isBrowsing = false;
    WalkToCounter();
}

    void WalkToCounter()
    {
        if (myQueueSpot != null && agent != null && agent.isOnNavMesh)
            agent.SetDestination(myQueueSpot.position);
    }

    // =========================================================
    //  UPDATE — check if customer reached their queue spot
    // =========================================================
    void Update()
    {
        if (isServed || isBrowsing || myQueueSpot == null) return;

        float dist = Vector3.Distance(transform.position, myQueueSpot.position);

        if (dist <= agent.stoppingDistance + 0.2f)
        {
            isAtSpot = true;
            RotateTowards(myQueueSpot.forward);

            // Re-enable collision once at spot
            if (collisionDisabled)
            {
                collisionDisabled = false;
                Collider col = GetComponent<Collider>();
                if (col != null) col.enabled = true;
            }

            // ── Spawn PC on desk if this customer is first in line ────────────
            // CustomerDeskManager checks if this is queue slot 0
            if (!_pcSpawnedOnDesk)
            {
                _pcSpawnedOnDesk = true;
                CustomerDeskManager.Instance?.TrySpawnDeskPC(this);
            }
        }
        else
        {
            isAtSpot = false;
        }
    }

    void RotateTowards(Vector3 direction)
    {
        if (direction == Vector3.zero) return;
        Quaternion targetRot = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 5f);
    }

    // =========================================================
    //  SHOP INTERACTION
    // =========================================================
    public void StartShopConversation()  { }
    public void EndShopConversation()    { }

    public void AcceptJob()
    {
        // Add the repair/build job to the email inbox
        if (EmailManager.Instance != null && assignedJob != null)
            EmailManager.Instance.ReceiveWalkInJob(assignedJob, npcName);

        // Remove PC from desk
        CustomerDeskManager.Instance?.ClearDeskPC();

        LeaveStore();
    }

    public void RejectJob()
    {
        // Remove PC from desk without adding the job
        CustomerDeskManager.Instance?.ClearDeskPC();

        LeaveStore();
    }

    // =========================================================
    //  LEAVE STORE — walks to exit, THEN gets destroyed
    //  exitPos MUST be set via Initialize() for this to work
    // =========================================================
    public void LeaveStore()
    {
        isServed = true;
        isAtSpot = false;
        _pcSpawnedOnDesk = false;

        if (mySpawner != null)
            mySpawner.CustomerLeft(this);

        if (exitPos != null && agent != null && agent.isOnNavMesh)
            agent.SetDestination(exitPos.position);
        else
            Debug.LogWarning($"[CustomerInside] {npcName} has no exitPos assigned! " +
                             "Make sure ShopCustomerSpawner.exitPoint is set.");

        StartCoroutine(DestroyWhenOutside());
    }

    IEnumerator DestroyWhenOutside()
    {
        if (exitPos == null)
        {
            // No exit set — wait a moment then destroy so they at least fade out
            yield return new WaitForSeconds(2f);
            Destroy(gameObject);
            yield break;
        }

        // Wait until they're close to the exit
        while (Vector3.Distance(transform.position, exitPos.position)
               > agent.stoppingDistance + 1f)
        {
            yield return new WaitForSeconds(0.5f);
        }

        // Walk a bit further past the door so they disappear off-screen
        agent.SetDestination(exitPos.position + exitPos.forward * 5f);
        yield return new WaitForSeconds(3f);

        Destroy(gameObject);
    }

    // =========================================================
    //  RANDOM NAME GENERATOR
    // =========================================================
    string GenerateRandomCustomerName()
    {
        string[] firstNames = {
            "James", "Mary", "Robert", "Patricia", "John", "Jennifer",
            "Michael", "Linda", "David", "Elizabeth", "Carlos", "Sofia",
            "Andre", "Maria", "Kevin", "Angela", "Brian", "Karen",
            "Ryan", "Samantha", "Derek", "Nina", "Marcus", "Chloe"
        };
        string[] lastNames = {
            "Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia",
            "Miller", "Davis", "Rodriguez", "Martinez", "Hernandez", "Lopez",
            "Wilson", "Anderson", "Thomas", "Taylor", "Moore", "Jackson",
            "Lee", "Perez", "White", "Harris", "Clark", "Lewis"
        };
        return firstNames[Random.Range(0, firstNames.Length)]
             + " " + lastNames[Random.Range(0, lastNames.Length)];
    }

    // =========================================================
    //  INITIALIZE (overload for restored customers — no browse)
    // =========================================================
    public void Initialize(Transform assignedSpot, Transform exitLocation, bool skipBrowse)
    {
        myQueueSpot = assignedSpot;
        exitPos     = exitLocation;
        isAtSpot    = false;

        if (!skipBrowse && willBrowseFirst)
            StartCoroutine(BrowseStoreRoutine());
        else
            WalkToCounter();
    }

    // =========================================================
    //  JOB GENERATION
    // =========================================================
    void GenerateRandomJob()
    {
        if (assignedJob != null)
        {
            reward     = (int)assignedJob.reward;
            jobRequest = BuildSpokenDialogue(assignedJob);
            return;
        }

        if (partDatabase != null)
        {
            assignedJob            = partDatabase.GenerateRandomJob();
            assignedJob.senderName = npcName;
            reward                 = (int)assignedJob.reward;
            jobRequest             = BuildSpokenDialogue(assignedJob);
            return;
        }

        Debug.LogWarning($"[{npcName}] No assignedJob AND no partDatabase!");
        reward     = Random.Range(500, 3000);
        jobRequest = "Hey, can you take a look at my PC?\n\nReward: " + reward;
    }

    string BuildSpokenDialogue(EmailData job)
    {
        string[] openers = { "Hey there!", "Excuse me!", "Hi!", "Good day!" };
        string opener    = openers[Random.Range(0, openers.Length)];

        if (job.jobType == JobType.Build)
        {
            string purposeDesc = GetPurposeDescription(job.buildPurpose);
            string[] buildLines = {
                $"{opener} I need someone to build me a PC {purposeDesc}.\n\nReward: ₱{job.reward:N0}",
                $"{opener} I'm looking to get a new PC assembled {purposeDesc}.\n\nReward: ₱{job.reward:N0}",
                $"{opener} Can you build a PC for me? It's {purposeDesc}.\n\nReward: ₱{job.reward:N0}"
            };
            return buildLines[Random.Range(0, buildLines.Length)];
        }
        else
        {
            string problem = (job.pcProblems != null && job.pcProblems.Length > 0)
                ? job.pcProblems[0] : "some issue";

            string[] repairLines = {
                $"{opener} My PC has a problem — {problem}. Can you fix it?\n\nReward: ₱{job.reward:N0}",
                $"{opener} I need help with my PC. It's been {problem}.\n\nReward: ₱{job.reward:N0}",
                $"{opener} Something's wrong with my computer: {problem}.\n\nReward: ₱{job.reward:N0}"
            };
            return repairLines[Random.Range(0, repairLines.Length)];
        }
    }

    string GetPurposeDescription(BuildPurpose purpose)
    {
        switch (purpose)
        {
            case BuildPurpose.Gaming:  return "for gaming";
            case BuildPurpose.Office:  return "for office work";
            default:                   return "for general use";
        }
    }
}
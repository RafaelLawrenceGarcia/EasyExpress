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
    public bool isBrowsing = false;
    public bool isServed = false;
    public bool isAtSpot = false;
    public ShopCustomerSpawner mySpawner;

    [Header("Animation")]
    public Animator animator;
    private NavMeshAgent agent;
    private Transform myQueueSpot;
    private Transform exitPos;
    private bool collisionDisabled = false;

    private bool _pcSpawnedOnDesk = false;

    public void DisableCollisionUntilAtSpot()
    {
        collisionDisabled = true;
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;
    }

    void Awake()
    {
        Debug.Log("[Customer] Animator found: " + (animator != null ? animator.gameObject.name : "NULL"));
        agent = GetComponent<NavMeshAgent>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (animator == null) animator = GetComponent<Animator>();

        if (npcName == "Customer")
        {
            npcName = GenerateRandomCustomerName();
            gameObject.name = "Customer_" + npcName;
        }

        GenerateRandomJob();
    }

    void Update()
    {
        // ── Drive walk/idle animation from NavMeshAgent speed ──
        // Normalized to match the player's Animator Controller:
        //   0.0 = Idle, 0.5 = Walk, 1.0 = Run
        if (animator != null && agent != null)
        {
            float velocity = agent.velocity.magnitude;
            float normalizedSpeed = 0f;

            if (velocity > 0.1f)
                normalizedSpeed = 0.5f; // walking

            animator.SetFloat("Speed", normalizedSpeed, 0.1f, Time.deltaTime);
        }
        if (isServed || isBrowsing || myQueueSpot == null) return;

        float dist = Vector3.Distance(transform.position, myQueueSpot.position);

        if (dist <= agent.stoppingDistance + 0.2f)
        {
            isAtSpot = true;
            if (TutorialManager.Instance != null)
                TutorialManager.Instance.NotifyCustomerArrivedAtCashier();
            RotateTowards(myQueueSpot.forward);

            if (collisionDisabled)
            {
                collisionDisabled = false;
                Collider col = GetComponent<Collider>();
                if (col != null) col.enabled = true;
            }

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

    public void Initialize(Transform assignedSpot, Transform exitLocation)
    {
        myQueueSpot = assignedSpot;
        exitPos = exitLocation;
        isAtSpot = false;

        if (willBrowseFirst)
            StartCoroutine(BrowseStoreRoutine());
        else
            WalkToCounter();
    }

    public void Initialize(Transform assignedSpot, Transform exitLocation, bool skipBrowse)
    {
        myQueueSpot = assignedSpot;
        exitPos = exitLocation;
        isAtSpot = false;

        if (!skipBrowse && willBrowseFirst)
            StartCoroutine(BrowseStoreRoutine());
        else
            WalkToCounter();
    }

    public void AssignQueueSpot(Transform spot)
    {
        myQueueSpot = spot;
        isAtSpot = false;
        _pcSpawnedOnDesk = false;

        if (!isBrowsing && agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.SetDestination(myQueueSpot.position);
        }
    }

    IEnumerator BrowseStoreRoutine()
    {
        isBrowsing = true;

        GameObject[] spots = GameObject.FindGameObjectsWithTag("BrowseSpot");

        if (spots.Length > 0)
        {
            int itemsToBrowse = Random.Range(minItemsToBrowse, maxItemsToBrowse + 1);

            for (int i = 0; i < itemsToBrowse; i++)
            {
                Transform target = spots[Random.Range(0, spots.Length)].transform;
                currentBrowseSpot = target;

                if (target == null || agent == null || !agent.isOnNavMesh) continue;

                agent.isStopped = false;
                agent.SetDestination(target.position);

                yield return new WaitUntil(() =>
                    !agent.pathPending &&
                    agent.remainingDistance <= agent.stoppingDistance + 0.3f);

                agent.velocity = Vector3.zero;
                agent.isStopped = true;

                float lookTimer = 0f;
                Quaternion targetRot = target.rotation;

                while (lookTimer < browseTime)
                {
                    transform.rotation = Quaternion.Slerp(
                        transform.rotation, targetRot, Time.deltaTime * 3f);
                    lookTimer += Time.deltaTime;
                    yield return null;
                }

                agent.isStopped = false;
            }
        }
        else
        {
            Debug.LogWarning("[CustomerInside] No BrowseSpots found! Tag shelves with 'BrowseSpot'.");
            yield return new WaitForSeconds(2f);
        }

        isBrowsing = false;
        WalkToCounter();
    }

    void WalkToCounter()
    {
        if (myQueueSpot != null && agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.SetDestination(myQueueSpot.position);
        }
    }

    void RotateTowards(Vector3 direction)
    {
        if (direction == Vector3.zero) return;
        Quaternion targetRot = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 5f);
    }

    public void StartShopConversation() { }
    public void EndShopConversation() { }

    public void AcceptJob()
    {
        if (EmailManager.Instance != null && assignedJob != null)
            EmailManager.Instance.ReceiveWalkInJob(assignedJob, npcName);

        CustomerDeskManager.Instance?.ClearDeskPC();
        LeaveStore();
    }

    public void RejectJob()
    {
        CustomerDeskManager.Instance?.ClearDeskPC();
        LeaveStore();
    }

    public void LeaveStore()
    {
        isServed = true;
        isAtSpot = false;
        _pcSpawnedOnDesk = false;

        if (mySpawner != null) mySpawner.CustomerLeft(this);

        if (exitPos != null && agent != null && agent.isOnNavMesh)
            agent.SetDestination(exitPos.position);
        else
            Debug.LogWarning($"[CustomerInside] {npcName} has no exitPos!");

        StartCoroutine(DestroyWhenOutside());
    }

    IEnumerator DestroyWhenOutside()
    {
        if (exitPos == null)
        {
            yield return new WaitForSeconds(2f);
            Destroy(gameObject);
            yield break;
        }

        while (Vector3.Distance(transform.position, exitPos.position) > agent.stoppingDistance + 1f)
            yield return new WaitForSeconds(0.5f);

        agent.SetDestination(exitPos.position + exitPos.forward * 5f);
        yield return new WaitForSeconds(3f);
        Destroy(gameObject);
    }

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

    void GenerateRandomJob()
    {
        if (assignedJob != null)
        {
            reward = (int)assignedJob.reward;
            jobRequest = BuildSpokenDialogue(assignedJob);
            return;
        }

        if (partDatabase != null)
        {
            assignedJob = partDatabase.GenerateRandomJob();
            assignedJob.senderName = npcName;
            reward = (int)assignedJob.reward;
            jobRequest = BuildSpokenDialogue(assignedJob);
            return;
        }

        Debug.LogWarning($"[{npcName}] No assignedJob AND no partDatabase!");
        reward = Random.Range(500, 3000);
        jobRequest = "Hey, can you take a look at my PC?\n\nReward: " + reward;
    }

    string BuildSpokenDialogue(EmailData job)
    {
        string[] openers = { "Hey there!", "Excuse me!", "Hi!", "Good day!" };
        string opener = openers[Random.Range(0, openers.Length)];

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
            case BuildPurpose.Gaming: return "for gaming";
            case BuildPurpose.Office: return "for office work";
            default: return "for general use";
        }
    }
}
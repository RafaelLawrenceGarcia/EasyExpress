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
    [Tooltip("Drag the shared PC Part Database asset here. If assigned, random jobs use this instead of manual arrays.")]
    public PCPartDatabase partDatabase;
    [Header("Browsing System")]
    public bool willBrowseFirst = true;
    public float browseTime = 3f;
    [Range(0f, 100f)] public float buyChance = 60f;

    public int minItemsToBrowse = 1;
    public int maxItemsToBrowse = 4;
    private Transform currentBrowseSpot;

    [Header("State")]
    public bool isBrowsing = false;
    public bool isServed = false;
    public bool isAtSpot = false;
    public ShopCustomerSpawner mySpawner;

    private NavMeshAgent agent;
    private Transform myQueueSpot;
    private Transform exitPos;
    private bool collisionDisabled = false;

    // Add this method
    public void DisableCollisionUntilAtSpot()
    {
        collisionDisabled = true;
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;
    }
    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        // Generate a random name if still using the default "Customer"
        if (npcName == "Customer")
        {
            npcName = GenerateRandomCustomerName();
            gameObject.name = "Customer_" + npcName;
        }

        GenerateRandomJob();
    }

    /// <summary>
    /// Generates a random first + last name for walk-in customers.
    /// </summary>
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
        return firstNames[Random.Range(0, firstNames.Length)] + " " + lastNames[Random.Range(0, lastNames.Length)];
    }

    public void Initialize(Transform assignedSpot, Transform exitLocation)
    {
        myQueueSpot = assignedSpot;
        exitPos = exitLocation;

        if (willBrowseFirst) StartCoroutine(BrowseStoreRoutine());
        else WalkToCounter();
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
                currentBrowseSpot = spots[Random.Range(0, spots.Length)].transform;

                if (currentBrowseSpot != null && agent.isOnNavMesh)
                {
                    agent.SetDestination(currentBrowseSpot.position);

                    while (Vector3.Distance(transform.position, currentBrowseSpot.position) > agent.stoppingDistance + 0.5f)
                    {
                        yield return null;
                    }
                    yield return new WaitForSeconds(browseTime);
                }
            }
        }
        else
        {
            Debug.LogWarning("No BrowseSpots found! Make sure your shelves have the 'BrowseSpot' tag.");
            yield return new WaitForSeconds(1f);
        }

        isBrowsing = false;
        WalkToCounter();
    }
    void WalkToCounter()
    {
        if (myQueueSpot != null) agent.SetDestination(myQueueSpot.position);
    }

    void Update()
    {
        if (!isServed && !isBrowsing && myQueueSpot != null)
        {
            if (Vector3.Distance(transform.position, myQueueSpot.position) <= agent.stoppingDistance + 0.2f)
            {
                isAtSpot = true;
                RotateTowards(myQueueSpot.forward);

                // Re-enable collision once at their spot
                if (collisionDisabled)
                {
                    collisionDisabled = false;
                    Collider col = GetComponent<Collider>();
                    if (col != null) col.enabled = true;
                }
            }
            else
            {
                isAtSpot = false;
            }
        }
    }
    public void AssignQueueSpot(Transform spot)
    {
        myQueueSpot = spot;
        isAtSpot = false;

        if (!isBrowsing && agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.SetDestination(myQueueSpot.position);
        }
    }
    void RotateTowards(Vector3 direction)
    {
        if (direction != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 5f);
        }
    }

    public void StartShopConversation()
    {
        // Add conversation opening logic if any
    }

    public void EndShopConversation()
    {
        // Add conversation closing logic if any
    }

    public void AcceptJob()
    {
        if (EmailManager.Instance != null && assignedJob != null)
        {
            EmailManager.Instance.ReceiveWalkInJob(assignedJob, npcName);
        }
        LeaveStore();
    }

    public void RejectJob()
    {
        LeaveStore();
    }

    public void LeaveStore()
    {
        isServed = true;
        isAtSpot = false;

        if (mySpawner != null) mySpawner.CustomerLeft(this);

        if (exitPos != null) agent.SetDestination(exitPos.position);
        StartCoroutine(DestroyWhenOutside());
    }

    IEnumerator DestroyWhenOutside()
    {
        while (exitPos != null && Vector3.Distance(transform.position, exitPos.position) > agent.stoppingDistance + 1f)
        {
            yield return new WaitForSeconds(1f);
        }

        if (exitPos != null)
        {
            agent.SetDestination(exitPos.position + (exitPos.forward * 5f));
            yield return new WaitForSeconds(3f);
        }

        Destroy(gameObject);
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

            string jobLabel = assignedJob.jobType == JobType.Build ? "BUILD" : "REPAIR";
            Debug.Log($"[{npcName}] {jobLabel} job generated.");
            return;
        }

        Debug.LogWarning($"[{npcName}] No assignedJob AND no partDatabase! Giving generic text.");
        reward = Random.Range(500, 3000);
        jobRequest = "Hey, can you take a look at my PC? It's been acting up lately.\n\nReward: " + reward;
    }

    string BuildSpokenDialogue(EmailData job)
    {
        string[] openers = { "Hey there!", "Excuse me!", "Hi!", "Good day!" };
        string opener = openers[Random.Range(0, openers.Length)];

        if (job.jobType == JobType.Build)
        {
            string purposeDesc = GetPurposeDescription(job.buildPurpose);

            string[] buildLines = {
                $"{opener} I need someone to build me a PC {purposeDesc}. Can you do it?\n\nReward: {job.reward:N0}",
                $"{opener} I'm looking to get a new PC assembled {purposeDesc}. I have the parts list ready!\n\nReward: {job.reward:N0}",
                $"{opener} Can you build a PC for me? It's {purposeDesc}. I know exactly what I want.\n\nReward: {job.reward:N0}"
            };
            return buildLines[Random.Range(0, buildLines.Length)];
        }
        else
        {
            string problem = (job.pcProblems != null && job.pcProblems.Length > 0)
                ? job.pcProblems[0]
                : "some issue";

            string[] repairLines = {
                $"{opener} My PC has a problem - {problem}. Can you fix it?\n\nReward: {job.reward:N0}",
                $"{opener} I need help with my PC. It keeps having this issue: {problem}.\n\nReward: {job.reward:N0}",
                $"{opener} Something's wrong with my computer. The problem is {problem}. Think you can repair it?\n\nReward: {job.reward:N0}",
                $"{opener} My PC is broken! It's been {problem} for days now. Please help!\n\nReward: {job.reward:N0}"
            };
            return repairLines[Random.Range(0, repairLines.Length)];
        }
    }

    string GetPurposeDescription(BuildPurpose purpose)
    {
        switch (purpose)
        {
            case BuildPurpose.School: return "for school work";
            case BuildPurpose.Streaming: return "for streaming";
            case BuildPurpose.Gaming: return "for gaming";
            case BuildPurpose.Office: return "for office use";
            default: return "";
        }
    }
}
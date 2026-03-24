using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

public class CustomerInside : MonoBehaviour
{
    [Header("Details")]
    public string npcName = "Customer";
    public string jobRequest;
    public int budget;

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

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        GenerateRandomJob();
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

        // THE FIX: Find the spots using the Unity Tag system like you had originally!
        GameObject[] spots = GameObject.FindGameObjectsWithTag("BrowseSpot");

        if (spots.Length > 0)
        {
            int itemsToBrowse = Random.Range(minItemsToBrowse, maxItemsToBrowse + 1);

            for (int i = 0; i < itemsToBrowse; i++)
            {
                // Pick a random shelf from the array
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
            yield return new WaitForSeconds(1f); // Brief pause if no spots exist
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

        // THE FIX: Use the correct method name from your spawner script!
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
            budget = (int)assignedJob.partsBudget;
            jobRequest = assignedJob.bodyText + "\n\nBudget: ₱" + budget;
            return;
        }

        if (partDatabase != null)
        {
            Debug.Log($"[{npcName}] Generating random job from PCPartDatabase...");

            // CHANGED: Now randomly picks Repair or Build
            assignedJob = partDatabase.GenerateRandomJob();
            assignedJob.senderName = npcName;

            budget = (int)assignedJob.partsBudget;
            jobRequest = assignedJob.bodyText + "\n\nBudget: ₱" + budget;

            string jobLabel = assignedJob.jobType == JobType.Build ? "BUILD" : "REPAIR";
            Debug.Log($"[{npcName}] {jobLabel} job generated.");

            if (assignedJob.requestedParts != null && assignedJob.jobType == JobType.Build)
            {
                Debug.Log($"[{npcName}] Customer wants {assignedJob.requestedParts.Count} parts installed:");
                foreach (StartingPCComponent part in assignedJob.requestedParts)
                    Debug.Log($"  - Requested: {part.partCategory}: {part.partName}");
            }
            return;
        }

        Debug.LogWarning($"[{npcName}] No assignedJob AND no partDatabase! Giving generic text.");
        budget = Random.Range(50, 300);
        jobRequest = "Hello, I need someone to take a look at my PC. It's been acting up lately.\n\nBudget: ₱" + budget;
    }
}
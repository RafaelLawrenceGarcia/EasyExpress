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

    [Header("Dynamic Job Generator (Random Parts)")]
    public GameObject[] possibleCases;
    public StartingPCComponent[] possibleMotherboards;
    public StartingPCComponent[] possibleRAMs;
    public StartingPCComponent[] possibleGPUs;
    public StartingPCComponent[] possiblePSUs;

    [Header("Browsing System")]
    public bool willBrowseFirst = true;
    public float browseTime = 3f;       // How long they look at EACH shelf
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
        
        // Generate the random job BEFORE they start walking!
        GenerateRandomJob();
    }

    void Start()
    {
        GameObject exitObj = GameObject.FindGameObjectWithTag("ShopInsideDoor");
        if (exitObj != null) exitPos = exitObj.transform;

        // Start the browsing phase right when they walk in
        if (willBrowseFirst)
        {
            StartCoroutine(BrowseRoutine());
        }
    }

    IEnumerator BrowseRoutine()
    {
        isBrowsing = true;

        GameObject[] spots = GameObject.FindGameObjectsWithTag("BrowseSpot");
        
        if (spots.Length > 0)
        {
            int itemsToLookAt = Random.Range(minItemsToBrowse, maxItemsToBrowse + 1);
            
            for (int i = 0; i < itemsToLookAt; i++)
            {
                Transform targetShelf = spots[Random.Range(0, spots.Length)].transform;
                
                NavMeshHit hit;
                if (NavMesh.SamplePosition(targetShelf.position, out hit, 2.5f, NavMesh.AllAreas))
                {
                    if (agent.isOnNavMesh)
                    {
                        agent.isStopped = false;
                        agent.SetDestination(hit.position);
                    }

                    while (Vector3.Distance(transform.position, hit.position) > 1.5f || agent.velocity.sqrMagnitude > 0.1f)
                    {
                        yield return null;
                    }

                    Vector3 lookPos = targetShelf.position;
                    lookPos.y = transform.position.y; 
                    transform.LookAt(lookPos);

                    yield return new WaitForSeconds(browseTime);
                }
            }
        }
        else
        {
            Vector3 randomWander = transform.position + Random.insideUnitSphere * 4f;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomWander, out hit, 4.0f, NavMesh.AllAreas))
            {
                if (agent.isOnNavMesh)
                {
                    agent.isStopped = false;
                    agent.SetDestination(hit.position);
                }
                yield return new WaitForSeconds(browseTime);
            }
        }

        float roll = Random.Range(0f, 100f);
        if (roll <= buyChance)
        {
            Debug.Log(npcName + " finished browsing and decided to buy something!");
            isBrowsing = false;
            if (myQueueSpot != null) AssignQueueSpot(myQueueSpot);
        }
        else
        {
            Debug.Log(npcName + " finished browsing but didn't find anything to buy.");
            isBrowsing = false;
            LeaveShop();
        }
    }

    void Update()
    {
        if (!isBrowsing && myQueueSpot != null && !isServed)
        {
            float dist = Vector3.Distance(transform.position, myQueueSpot.position);
            if (dist < 1.5f && agent.velocity.sqrMagnitude < 0.1f)
            {
                isAtSpot = true;
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

    public void StartShopConversation()
    {
        if (agent.isOnNavMesh) agent.isStopped = true;
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            Vector3 lookPos = player.transform.position;
            lookPos.y = transform.position.y; 
            transform.LookAt(lookPos);
        }
    }

    public void EndShopConversation()
    {
        if (!isServed && agent.isOnNavMesh) 
        {
            agent.isStopped = false;
            if (myQueueSpot != null) agent.SetDestination(myQueueSpot.position);
        }
    }

    public void AcceptJob()
    {
        isServed = true;
        
        if (assignedJob != null && EmailManager.Instance != null)
        {
            EmailManager.Instance.ReceiveWalkInJob(assignedJob, npcName);
        }
        else
        {
            Debug.LogWarning("WARNING: Either this NPC has no 'Assigned Job' in the Inspector, or EmailManager is missing!");
        }

        Debug.Log("Job Accepted in person!");
        LeaveShop();
    }

    public void RejectJob()
    {
        isServed = true;
        LeaveShop();
    }

    public void LeaveShop()
    {
        if (mySpawner != null) mySpawner.CustomerLeft(this);
        
        StartCoroutine(LeaveRoutine());
    }

    IEnumerator LeaveRoutine()
    {
        if (exitPos != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.SetDestination(exitPos.position);

            while (Vector3.Distance(transform.position, exitPos.position) > 1.5f)
            {
                yield return null; 
            }
        }
        else
        {
            yield return new WaitForSeconds(3f);
        }

        Destroy(gameObject);
    }

    void GenerateRandomJob()
    {
        if (assignedJob == null && possibleCases != null && possibleCases.Length > 0)
        {
            Debug.Log("Generating a completely random PC for walk-in customer!");
            
            assignedJob = ScriptableObject.CreateInstance<EmailData>();
            assignedJob.basePCCasePrefab = possibleCases[Random.Range(0, possibleCases.Length)];
            assignedJob.startingParts = new List<StartingPCComponent>();

            // --- FIXED: Using Helper to ensure category data is saved correctly ---
            AddRandomPartToJob(possibleMotherboards);
            AddRandomPartToJob(possibleRAMs);
            AddRandomPartToJob(possibleGPUs);
            AddRandomPartToJob(possiblePSUs);

            assignedJob.labourCost = Random.Range(100, 500);
            assignedJob.partsBudget = Random.Range(500, 3000);
            budget = (int)assignedJob.partsBudget;

            string[] problems = { "Blue Screen of Death", "No Display on Monitor", "PC Keeps Overheating", "Won't Turn On" };
            assignedJob.pcProblems = new string[] { problems[Random.Range(0, problems.Length)] };
            assignedJob.objectives = new string[] { "Diagnose Issue", "Replace Broken Part", "Boot to Desktop" };

            assignedJob.bodyText = "Hey, my PC is acting up. I think the issue is: " + assignedJob.pcProblems[0] + ". Can you take a look?";
            jobRequest = assignedJob.bodyText + "\n\nBudget: ₱" + budget;
        }
        else if (assignedJob != null)
        {
            budget = (int)assignedJob.partsBudget;
            jobRequest = assignedJob.bodyText + "\n\nBudget: ₱" + budget;
        }
        else
        {
            budget = Random.Range(100, 1000);
            jobRequest = "Can you fix my PC? My budget is ₱" + budget + ".";
        }
    }

    // --- NEW HELPER FUNCTION: Prevents data loss during random generation ---
    void AddRandomPartToJob(StartingPCComponent[] partPool)
    {
        if (partPool != null && partPool.Length > 0)
        {
            StartingPCComponent selected = partPool[Random.Range(0, partPool.Length)];
            
            // Create a fresh copy so we don't overwrite categories on other NPCs
            StartingPCComponent copy = new StartingPCComponent();
            copy.partCategory = selected.partCategory;
            copy.partName = selected.partName;
            copy.partPrefab = selected.partPrefab;
            copy.partIcon = selected.partIcon;
            
            assignedJob.startingParts.Add(copy);
        }
    }
}
using UnityEngine;
using UnityEngine.AI;

public class CustomerInside : MonoBehaviour
{
    [Header("Details")]
    public string npcName = "Customer";
    public string jobRequest; 
    public int budget;
    
    [Header("State")]
    public bool isServed = false;
    public bool isAtSpot = false; 
    public ShopCustomerSpawner mySpawner; 

    private NavMeshAgent agent;
    private Transform myQueueSpot;
    private Transform exitPos;

    void Awake() 
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent != null) agent.baseOffset = 0f; 
        GenerateRandomJob();
    }

    void Start()
    {
        GameObject exitObj = GameObject.FindGameObjectWithTag("ShopInsideDoor");
        if (exitObj != null) exitPos = exitObj.transform;
    }

    void Update()
    {
        // CHECK IF ARRIVED AT QUEUE SPOT
        if (myQueueSpot != null && !isServed)
        {
            float dist = Vector3.Distance(transform.position, myQueueSpot.position);
            // If close (1.5m) and barely moving (velocity < 0.1), consider them "Arrived"
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
        if (agent != null && agent.isOnNavMesh)
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
        Debug.Log("Job Accepted!");
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

        if (exitPos != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.SetDestination(exitPos.position);
        }
        Destroy(gameObject, 5f);
    }

    void GenerateRandomJob()
    {
        string[] issues = { "broken screen", "virus", "won't turn on", "keyboard broken" };
        string issue = issues[Random.Range(0, issues.Length)];
        budget = Random.Range(100, 1000);
        jobRequest = "Can you fix my " + issue + "? My budget is $" + budget + ".";
    }
}
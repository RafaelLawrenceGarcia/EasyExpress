using UnityEngine;
using UnityEngine.AI;

public class NPCSpawner : MonoBehaviour
{
    public GameObject npcPrefab;      // Drag your NPC here
    public int maxNPCs = 10;          // Max people on street
    public float spawnInterval = 5f;  // Time between spawns
    public float spawnRadius = 15f;   // How big is the spawn area

    private float timer;

    void Update()
    {
        if (GameObject.FindGameObjectsWithTag("NPC").Length < maxNPCs)
        {
            timer += Time.deltaTime;
            if (timer >= spawnInterval)
            {
                Spawn();
                timer = 0;
            }
        }
    }

    void Spawn()
    {
        // Get a random point in a circle
        Vector3 randomPoint = transform.position + Random.insideUnitSphere * spawnRadius;
        
        // Find the nearest safe spot on the NavMesh (Sidewalk)
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomPoint, out hit, 4.0f, NavMesh.AllAreas))
        {
            Instantiate(npcPrefab, hit.position, Quaternion.identity);
        }
    }
}
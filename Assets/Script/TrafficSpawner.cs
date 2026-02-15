using UnityEngine;

public class TrafficSpawner : MonoBehaviour
{
    [Header("Settings")]
    // 1. We put brackets [] here to make it a LIST
    public GameObject[] carPrefabs; 
    
    public TrafficNode spawnPoint;
    public float spawnRate = 3.0f;

    private float timer;

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= spawnRate)
        {
            SpawnRandomCar();
            timer = 0;
        }
    }

    void SpawnRandomCar()
    {
        // Safety Check: Don't crash if the list is empty
        if (carPrefabs.Length == 0) return;

        // 2. Pick a random number between 0 and the number of cars you have
        int randomIndex = Random.Range(0, carPrefabs.Length);
        GameObject chosenCar = carPrefabs[randomIndex];

        // 3. Spawn the chosen car (keeping your rotation fix!)
        Quaternion fixedRotation = spawnPoint.transform.rotation * Quaternion.Euler(-90, 0, 0); // Change -90 to 0 or 90 if needed
        GameObject newCar = Instantiate(chosenCar, spawnPoint.transform.position, fixedRotation);

        // 4. Give it the destination
        CarAI ai = newCar.GetComponentInChildren<CarAI>();
        if (ai != null)
        {
            ai.currentNode = spawnPoint;
        }
    }
}
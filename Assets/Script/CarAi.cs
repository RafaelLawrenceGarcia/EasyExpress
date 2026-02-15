using UnityEngine;

public class CarAI : MonoBehaviour
{
    [Header("Movement Settings")]
    public float speed = 5f;
    public float rotationSpeed = 5f;
    public float stopDistance = 3f;
    public LayerMask obstacleLayer;

    [Header("Slope Settings")]
    public LayerMask groundLayer;    // Must be set to "ROAD"
    public float heightOffset = 0.1f; // Keeps tires above asphalt

    [Header("3D Model Fix")]
    public Vector3 modelCorrection = new Vector3(0, 0, 0); 

    [Header("Path")]
    public TrafficNode currentNode;

    private void Update()
    {
        // 1. Crash Detection
        if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, stopDistance, obstacleLayer))
        {
            Debug.DrawRay(transform.position, transform.forward * stopDistance, Color.red);
            return; 
        }

        // 2. Movement Logic
        if (currentNode != null)
        {
            MoveAndRotate();
            CheckWaypointDistance();
        }
    }

    void MoveAndRotate()
    {
        // --- STEP A: CALCULATE MOVEMENT ---
        Vector3 targetDirection = currentNode.transform.position - transform.position;
        targetDirection.y = 0; // Don't look up/down at the node, only left/right

        if (targetDirection == Vector3.zero) return;

        // --- STEP B: DETECT GROUND SLOPE ---
        Vector3 nextPosition = transform.position + (targetDirection.normalized * speed * Time.deltaTime);
        Vector3 groundNormal = Vector3.up; // Default to flat

        // Shoot laser from above the car DOWN to find the road
        Vector3 rayStart = transform.position + Vector3.up * 2.0f;
        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 10.0f, groundLayer))
        {
            // We found the road! Adjust Y position to sit on it
            nextPosition.y = hit.point.y + heightOffset;
            groundNormal = hit.normal; // Save the slope angle
            Debug.DrawLine(rayStart, hit.point, Color.green);
        }
        else
        {
            Debug.DrawRay(rayStart, Vector3.down * 10.0f, Color.red); // Missed the road
        }

        // --- STEP C: APPLY POSITION ---
        transform.position = nextPosition;

        // --- STEP D: APPLY ROTATION (COMBINED) ---
        // 1. Calculate the forward direction projected onto the slope
        Vector3 slopeForward = Vector3.ProjectOnPlane(targetDirection, groundNormal).normalized;

        // 2. Create the rotation looking along that slope
        if (slopeForward != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(slopeForward, groundNormal);
            
            // 3. Add your manual Model Fix (e.g. -90 degrees)
            targetRotation *= Quaternion.Euler(modelCorrection);

            // 4. Smoothly rotate
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        }
    }

    void CheckWaypointDistance()
    {
        // Calculate distance (ignoring height)
        float dist = Vector2.Distance(new Vector2(transform.position.x, transform.position.z), 
                                      new Vector2(currentNode.transform.position.x, currentNode.transform.position.z));

        // If we reached the node...
        if (dist < 1.5f)
        {
            // 1. CHECK: Is this the End of the Road? (No more nodes to go to)
            if (currentNode.nextNodes.Count == 0)
            {
                Destroy(gameObject); // DELETE THE CAR
                return;
            }

            // 2. Otherwise, pick the next path and keep driving
            currentNode = currentNode.nextNodes[Random.Range(0, currentNode.nextNodes.Count)];
        }
    }
}
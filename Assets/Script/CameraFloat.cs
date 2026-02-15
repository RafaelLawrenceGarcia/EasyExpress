using UnityEngine;

public class CameraFloat : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Which way should it move? (X=Left/Right, Y=Up/Down, Z=Forward/Back)")]
    public Vector3 moveDirection = new Vector3(1, 0, 0); // Default: Moves sideways

    [Tooltip("How fast it moves")]
    public float speed = 0.5f;

    [Tooltip("How far it moves from the start point")]
    public float distance = 1.0f;

    private Vector3 startPosition;

    void Start()
    {
        // Remember where the camera started
        startPosition = transform.position;
    }

    void Update()
    {
        // Create a number that goes from -1 to 1 smoothly (like a wave)
        float wave = Mathf.Sin(Time.time * speed);

        // Apply that wave to your direction and distance
        transform.position = startPosition + (moveDirection.normalized * wave * distance);
        //w
    }
}
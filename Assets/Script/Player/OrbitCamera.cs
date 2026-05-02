using UnityEngine;

public class OrbitCamera : MonoBehaviour
{
    public Transform target; // The Player Capsule

    [Header("View Settings")]
    public KeyCode viewModeKey = KeyCode.V; 
    public bool isFirstPerson = false;
    
    [HideInInspector] public bool forceFPSMode = false; 

    [Header("3rd Person Settings")]
    public float tpsDistance = 5.0f;
    public float tpsHeight = 1.5f;   
    public float tpsFOV = 60f;

    [Header("1st Person Settings")]
    public float fpsDistance = 0.0f; 
    public float fpsHeight = 1.6f;   
    public float fpsFOV = 80f;       

    [Header("General Settings")]
    public float mouseSensitivity = 3.0f; 
    public bool invertY = false;          
    public Vector2 pitchLimits = new Vector2(-60, 85);

    [Header("Wall Collision (TPS Only)")]
    public LayerMask collisionLayers; 
    public float cameraRadius = 0.2f; 
    public float minDistance = 1.0f; 
    public float wallOffset = 0.1f; 

    [Header("Smoothing")]
    public float rotationSmoothTime = 0.12f;
    Vector3 rotationSmoothVelocity;
    Vector3 currentRotation;

    float yaw;
    float pitch;
    public bool canRotate = true; // Controlled by PlayerInteract
    private Camera cam; 

    // --- NEW HELPER FUNCTION ---
    // Call this from PlayerInteract to freeze/unfreeze looking around
    public void SetCameraState(bool state)
    {
        canRotate = state;
    }
    // ---------------------------

    void Start()
    {
        cam = GetComponent<Camera>();
        mouseSensitivity = PlayerPrefs.GetFloat("MouseSensitivity", 3.0f);
        invertY = PlayerPrefs.GetInt("InvertY", 0) == 1;

        if (target != null)
        {
            Vector3 angles = transform.eulerAngles;
            yaw = angles.y;
            pitch = angles.x;
            currentRotation = angles;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void LateUpdate()
    {
        if (!target) return;
        if (PauseManager.isPaused) return;

        // --- 0. DETERMINE MODE ---
        bool activeFPS = isFirstPerson || forceFPSMode;

        if (!forceFPSMode && Input.GetKeyDown(viewModeKey))
        {
            isFirstPerson = !isFirstPerson;
        }

        // --- 1. MOUSE INPUT ---
        // Only calculate mouse movement if we are allowed to rotate
        if (canRotate)
        {
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

            yaw += mouseX;
            if (invertY) pitch += mouseY;
            else pitch -= mouseY;

            pitch = Mathf.Clamp(pitch, pitchLimits.x, pitchLimits.y);
        }

        // --- 2. SMOOTH ROTATION ---
        Vector3 targetRotation = new Vector3(pitch, yaw);
        currentRotation = Vector3.SmoothDamp(currentRotation, targetRotation, ref rotationSmoothVelocity, rotationSmoothTime);
        transform.eulerAngles = currentRotation;

        // --- 3. CALCULATE POSITION ---
        float currentHeight = activeFPS ? fpsHeight : tpsHeight;
        float targetDistance = activeFPS ? fpsDistance : tpsDistance;
        float targetFOV = activeFPS ? fpsFOV : tpsFOV;

        if(cam) cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, Time.deltaTime * 10f);

        Vector3 focusPoint = target.position + (Vector3.up * currentHeight);
        Vector3 finalPosition = focusPoint - transform.forward * targetDistance;

        // --- 4. WALL COLLISION (TPS ONLY) ---
        if (!activeFPS)
        {
            RaycastHit hit;
            if (Physics.SphereCast(focusPoint, cameraRadius, -transform.forward, out hit, tpsDistance, collisionLayers))
            {
                float distanceToWall = hit.distance - wallOffset;
                if (distanceToWall < minDistance) distanceToWall = minDistance;
                finalPosition = focusPoint - transform.forward * distanceToWall;
            }
        }

        // --- 5. APPLY POSITION ---
        transform.position = finalPosition;
    }
}
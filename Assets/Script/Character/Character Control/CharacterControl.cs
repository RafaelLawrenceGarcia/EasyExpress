using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class GTAMovement : MonoBehaviour
{
    [Header("Stats")]
    public float walkSpeed = 3f;
    public float runSpeed = 8f;
    public float gravity = -9.81f;
    // Jump height removed

    [Header("Turning Feel")]
    public float turnSmoothTime = 0.25f; 
    float turnSmoothVelocity;

    [Header("References")]
    public Transform cam; 
    public Animator animator; 

    CharacterController controller;
    Vector3 velocity;
    bool isGrounded;
    
    // We keep this variable so PlayerInteract can change it!
    public bool canMove = true; 

    void Start()
    {
        controller = GetComponent<CharacterController>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // --- HELPER FUNCTION FOR INTERACTION ---
    public void SetMovementState(bool state)
    {
        canMove = state;
        
        if (animator != null)
        {
            if (state == false) 
            {
                // FORCE STOP: Set speed to 0 immediately
                animator.SetFloat("Speed", 0f);
                animator.SetBool("IsTalking", true); 
            }
            else
            {
                animator.SetBool("IsTalking", false);
            }
        }
    }
    // ---------------------------

    void Update()
    {
        isGrounded = controller.isGrounded;
        if (animator != null) animator.SetBool("IsGrounded", isGrounded);

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        // --- STOP LOGIC (When Talking) ---
        if (!canMove) 
        {
            // 1. Force Animation to Idle (0 speed)
            if (animator != null) animator.SetFloat("Speed", 0f);

            // 2. Apply gravity only (so you don't float)
            velocity.y += gravity * Time.deltaTime;
            controller.Move(velocity * Time.deltaTime);
            return; // Stop here! Don't process walking inputs.
        }

        // --- WALKING LOGIC ---
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 direction = new Vector3(horizontal, 0f, vertical).normalized;

        bool isRunning = Input.GetKey(KeyCode.LeftShift);
        float currentSpeed = isRunning ? runSpeed : walkSpeed;

        if (direction.magnitude >= 0.1f)
        {
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + cam.eulerAngles.y;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);

            Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            controller.Move(moveDir.normalized * currentSpeed * Time.deltaTime);

            float animationValue = isRunning ? 1f : 0.5f;
            if (animator != null)
            {
                animator.SetFloat("Speed", animationValue, 0.1f, Time.deltaTime);
            }
        }
        else
        {
            if (animator != null)
            {
                animator.SetFloat("Speed", 0f, 0.1f, Time.deltaTime);
            }
        }

        // (Jump Logic Removed)

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}
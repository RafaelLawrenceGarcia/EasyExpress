using UnityEngine;
using UnityEngine.AI;

public class NPCAnimationController : MonoBehaviour
{
    private NavMeshAgent agent;
    private Animator animator;

    void Start()
    {
        // Grab the components
        agent = GetComponent<NavMeshAgent>();
        
        // If the Animator is on a child object, use GetComponentInChildren<Animator>() instead
        animator = GetComponentInChildren<Animator>(); 
    }

    void Update()
    {
        float currentSpeed = agent.velocity.magnitude;
        animator.SetFloat("velocity", currentSpeed);
    }
}
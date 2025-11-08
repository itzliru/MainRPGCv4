using UnityEngine;
using UnityEngine.AI;
using System;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(NavMeshAgent))]
public class NPCAgentController : MonoBehaviour
{
    public Animator animator;
    private NavMeshAgent agent;

    [Header("Animation Parameters")]
    public string speedParam = "Speed";
    public string horizontalParam = "MoveX";
    public string verticalParam = "MoveZ";
    public bool use2DBlend = true;

    [Header("Movement Settings")]
    public float blendSmooth = 0.15f;

    public Func<Vector3> OnCustomTargetVelocity;

    private Vector3 lastVelocity;
    private float smoothedSpeed;
    private Vector2 smoothedDir;



    private void Awake()
    {
        animator = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
    }

    private void Update()
    {
        if (!agent.enabled) return;
         // pull from lambda or agent velocity
        Vector3 velocity = OnCustomTargetVelocity?.Invoke() ?? agent.velocity;
        Vector3 localVel = transform.InverseTransformDirection(velocity);
    
        smoothedSpeed = Mathf.Lerp(smoothedSpeed, agent.velocity.magnitude, Time.deltaTime / blendSmooth);

        if (use2DBlend)
        {
            smoothedDir.x = Mathf.Lerp(smoothedDir.x, localVel.x, Time.deltaTime / blendSmooth);
            smoothedDir.y = Mathf.Lerp(smoothedDir.y, localVel.z, Time.deltaTime / blendSmooth);

            animator.SetFloat(horizontalParam, smoothedDir.x);
            animator.SetFloat(verticalParam, smoothedDir.y);
        }

        animator.SetFloat(speedParam, smoothedSpeed);
    }

    public void SetDestination(Vector3 point)
    {
        agent.isStopped = false;
        agent.SetDestination(point);
    }

    public void Stop()
    {
        agent.isStopped = true;
    }

    public bool ReachedDestination(float tolerance = 0.3f)
    {
        if (!agent.pathPending && agent.remainingDistance <= tolerance)
            return true;
        return false;
    }
}

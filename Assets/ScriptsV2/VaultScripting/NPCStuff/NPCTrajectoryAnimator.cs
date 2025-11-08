using UnityEngine;
using UnityEngine.AI;
using System;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(NavMeshAgent))]
public class NPCTrajectoryAnimator : MonoBehaviour
{
    [Header("References")]
    public Animator animator;
    public NavMeshAgent agent;

    [Header("Animation Parameters")]
    public string moveXParam = "MoveX";
    public string moveZParam = "MoveZ";
    public string speedParam = "Speed";
    public string hasWeaponParam = "HasWeapon";
    public string isAttackingParam = "IsAttacking";

    [Header("Debug Visualization")]
    public bool drawGizmos = true;
    public Color forwardColor = Color.green;
    public Color velocityColor = Color.cyan;
    public Color strafeColor = Color.magenta;
    public float gizmoLength = 1.5f;

    [Header("Movement Scaling")]
    public float maxSpeed = 3.5f;
    public float blendSmooth = 8f;

    // Runtime properties
    private Vector3 localVelocity;
    private float targetMoveX;
    private float targetMoveZ;
    private float targetSpeed;

    // 🔗 Lambda-style delegates for modular hooks
    public Func<bool> OnHasWeapon;
    public Func<bool> OnIsAttacking;
    public Func<Vector3> OnCustomTargetVelocity;

    private void Awake()
    {
        animator = animator ?? GetComponent<Animator>();
        agent = agent ?? GetComponent<NavMeshAgent>();
    }

    private void Update()
    {
        // 🧠 Grab source velocity (either AI, or override from lambda)
        Vector3 velocity = OnCustomTargetVelocity?.Invoke() ?? agent.velocity;
        localVelocity = transform.InverseTransformDirection(velocity);

        // Normalize and scale to [-1, 1]
targetMoveX = Mathf.Clamp(localVelocity.x / (maxSpeed * 0.75f), -1f, 1f);
targetMoveZ = Mathf.Clamp(localVelocity.z / maxSpeed, -1f, 1f);

        targetSpeed = Mathf.Clamp01(velocity.magnitude / maxSpeed);

        // Smoothly blend toward targets
        float currentX = animator.GetFloat(moveXParam);
        float currentZ = animator.GetFloat(moveZParam);
        float currentSpeed = animator.GetFloat(speedParam);

        animator.SetFloat(moveXParam, Mathf.Lerp(currentX, targetMoveX, Time.deltaTime * blendSmooth));
        animator.SetFloat(moveZParam, Mathf.Lerp(currentZ, targetMoveZ, Time.deltaTime * blendSmooth));
        animator.SetFloat(speedParam, Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * blendSmooth));

        // Handle weapon & attack logic via lambdas (optional hooks)
        if (!string.IsNullOrEmpty(hasWeaponParam))
            animator.SetBool(hasWeaponParam, OnHasWeapon?.Invoke() ?? false);

        if (!string.IsNullOrEmpty(isAttackingParam))
            animator.SetBool(isAttackingParam, OnIsAttacking?.Invoke() ?? false);
    }

    // 🎯 Debug Gizmo Visualization
    private void OnDrawGizmos()
    {
        if (!drawGizmos) return;

        Gizmos.color = forwardColor;
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * gizmoLength);

        Gizmos.color = velocityColor;
        Vector3 vel = agent ? agent.velocity : Vector3.zero;
        Gizmos.DrawLine(transform.position, transform.position + vel.normalized * gizmoLength);

        Gizmos.color = strafeColor;
        Gizmos.DrawLine(transform.position, transform.position + transform.right * targetMoveX * gizmoLength * 0.75f);

        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 1.0f, targetSpeed * 0.3f);
    }
}


//////Lambda hooks
/// npcTrajectory.OnHasWeapon = () => npcAI.HasWeapon;
/// npcTrajectory.OnIsAttacking = () => npcAI.IsAttacking;
/// npcTrajectory.OnCustomTargetVelocity = () => customVelocityOverride;

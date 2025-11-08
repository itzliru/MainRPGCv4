using UnityEngine;
using System;
using System.Collections;
using VaultSystems.Data;
using VaultSystems.Invoker;

[RequireComponent(typeof(Animator))]
public class HumanoidAimSolver : MonoBehaviour
{
    [Header("Settings")]
    public bool solverEnabled = true;          // Can be toggled via NPCCombatController
    public float globalWeight = 1f;
    public float aimSpeed = 5f;

    [Header("Angle Limit")]
    public float maxAngle = 75f;
    public bool clampAimDirection = true;

    [Header("Affected Bones")]
    public HumanBoneData[] humanBones;

    [Serializable]
    public struct HumanBoneData
    {
        public HumanBodyBones bone;
        [Range(0f, 1f)] public float weight;
    }

    [Header("References")]
    public NPCCombatController npcCombatController; // assign via inspector or dynamically

public Transform targetTransform; // optional override

    private Animator animator;
    private Transform[] boneTransforms;
    private Quaternion[] initialRotations;
    private Vector3 smoothedDirection;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        if (animator == null || humanBones.Length == 0) return;

        boneTransforms = new Transform[humanBones.Length];
        initialRotations = new Quaternion[humanBones.Length];

        for (int i = 0; i < humanBones.Length; i++)
        {
            boneTransforms[i] = animator.GetBoneTransform(humanBones[i].bone);
            if (boneTransforms[i] != null)
                initialRotations[i] = boneTransforms[i].localRotation;
        }

        // Optional: auto-find NPCCombatController on the same GameObject
        if (npcCombatController == null)
            npcCombatController = GetComponent<NPCCombatController>();
    }

    private void LateUpdate()
    {
        if (!solverEnabled || boneTransforms == null) return;

        // Get current target dynamically from NPCCombatController
        Transform targetTransform = npcCombatController != null ? npcCombatController.currentTarget : null;
        if (targetTransform == null) return;

        Vector3 aimDir = (targetTransform.position - transform.position).normalized;
        Vector3 forward = transform.forward;

        float angle = Vector3.Angle(forward, aimDir);
        float angleWeight = 1f;

        if (angle > maxAngle)
        {
            float over = Mathf.Clamp01((angle - maxAngle) / maxAngle);
            angleWeight = Mathf.Lerp(1f, 0f, over);

            if (clampAimDirection)
            {
                Quaternion clampRot = Quaternion.RotateTowards(
                    Quaternion.LookRotation(forward),
                    Quaternion.LookRotation(aimDir),
                    maxAngle
                );
                aimDir = clampRot * Vector3.forward;
            }
        }

        smoothedDirection = Vector3.Slerp(smoothedDirection == Vector3.zero ? forward : smoothedDirection,
                                          aimDir, Time.deltaTime * aimSpeed);

        for (int i = 0; i < boneTransforms.Length; i++)
        {
            Transform bone = boneTransforms[i];
            if (bone == null) continue;

            float boneWeight = humanBones[i].weight * globalWeight * angleWeight;
            Quaternion targetRot = Quaternion.LookRotation(smoothedDirection, transform.up);
            bone.rotation = Quaternion.Slerp(bone.rotation, targetRot, boneWeight * Time.deltaTime * aimSpeed);
        }
        
    Transform target = targetTransform != null ? targetTransform : npcCombatController?.currentTarget;
if (target == null) return;

        // Debug lines
        Debug.DrawRay(transform.position, forward * 2f, Color.gray);
        Debug.DrawRay(transform.position, smoothedDirection * 2f, angle > maxAngle ? Color.red : Color.green);
    }

    // Methods to enable/disable solver from NPC controller
    public void EnableSolver() => solverEnabled = true;
    public void DisableSolver() => solverEnabled = false;
}

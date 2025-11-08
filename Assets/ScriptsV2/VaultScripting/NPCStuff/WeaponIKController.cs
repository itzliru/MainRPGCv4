using UnityEngine;
using UnityEngine.Animations.Rigging;
using System.Collections;

/// <summary>
/// Manages IK systems for weapon aiming and hand positioning.
/// Supports both AvatarIK (legacy) and RigBuilder constraints (modern).
/// </summary>
public class WeaponIKController : MonoBehaviour
{
    #region === COMPONENTS ===
    private Animator animator;
    private RigBuilder rigBuilder;
    #endregion

    #region === HAND IK (AvatarIK) ===
    [Header("Hand IK Targets")] 
    public Transform rightHandTarget;
    public Transform leftHandTarget;
    private float ikWeight = 0f;
    private bool ikActive = false;

    [Header("Hand IK Hints (TwoBoneIK)")]
    public Transform rightHandHint;
    public Transform leftHandHint;
    #endregion

    #region === AIM IK ===
    [Header("Aim IK")]
    public Transform aimTarget;
    public bool aimEnabled = false;
    public Rig aimRig;
    public float aimRigBlendSpeed = 3f;
    [Range(0f, 1f)] public float aimWeight = 1f;
    #endregion

    #region === SPINE ROTATION ===
    [Header("Spine Rotation")]
    public Transform spineBone;
    [Range(0.1f, 10f)] public float spineRotationSpeed = 5f; // ← Changed from weight to speed
    public float maxSpineAngle = 135f;
    #endregion

    #region === HUMANOID AIM SOLVER ===
    [Header("Humanoid Aim Solver")]
    public HumanoidAimSolver humanoidAimSolver;
    #endregion

    private void Awake()
    {
        animator = GetComponent<Animator>();
        rigBuilder = GetComponent<RigBuilder>();

        if (!aimRig)
        {
            aimRig = GetComponentInChildren<Rig>();
            if (aimRig) 
                Debug.Log($"[WeaponIKController] Auto-assigned AimRig: {aimRig.name}");
        }
    }

    #region === IK TARGET SETUP ===

    /// <summary>
    /// Set hand targets for AvatarIK (legacy method, still valid)
    /// </summary>
    public void SetIKTargets(Transform right, Transform left)
    {
        rightHandTarget = right;
        leftHandTarget = left;
    }

    /// <summary>
    /// Alias for SetIKTargets to match PlayerWeaponController expectation
    /// </summary>
    public void SetHandTargets(Transform right, Transform left)
    {
        SetIKTargets(right, left);
    }

    /// <summary>
    /// Bind IK from weapon data and prefab instance
    /// </summary>
    public void BindFromWeapon(WeaponData weapon, GameObject weaponPrefabInstance)
    {
        if (weapon == null || weaponPrefabInstance == null) 
            return;

        // Find and bind hand targets
        Transform rightTarget = weaponPrefabInstance.transform.Find(weapon.rightHandTargetName);
        Transform leftTarget = weaponPrefabInstance.transform.Find(weapon.leftHandTargetName);

        if (!rightTarget || !leftTarget)
        {
            Debug.LogWarning($"[WeaponIKController] Could not find hand targets on weapon '{weapon.weaponID}'!");
            return;
        }

        SetIKTargets(rightTarget, leftTarget);

        // Bind constraint-based IK
        BindTwoBoneIKToWeapon(weaponPrefabInstance);

        // Bind aim rig
        BindAimRigConstraints();

        Debug.Log($"[WeaponIKController] Bound weapon '{weapon.weaponID}' IK targets");
    }

    #endregion

    #region === TWOBONE IK CONSTRAINT BINDING ===

    /// <summary>
    /// Bind TwoBoneIK constraints to weapon targets (modern constraint-based approach)
    /// </summary>
    public void BindTwoBoneIKToWeapon(GameObject weaponPrefabInstance)
    {
        if (weaponPrefabInstance == null) 
            return;

        // Right Hand IK
        TwoBoneIKConstraint rightHandIK = FindConstraintInChildren<TwoBoneIKConstraint>("RightHandIK");
        if (rightHandIK != null)
        {
            Transform rightTarget = FindDeepChild(weaponPrefabInstance.transform, "rightHandTarget");
            if (rightTarget != null)
            {
                rightHandIK.data.target = rightTarget;
                Debug.Log($"[WeaponIKController] Bound Right Hand IK to {rightTarget.name}");
            }

            if (rightHandHint != null)
                rightHandIK.data.hint = rightHandHint;
        }

        // Left Hand IK
        TwoBoneIKConstraint leftHandIK = FindConstraintInChildren<TwoBoneIKConstraint>("LeftHandIK");
        if (leftHandIK != null)
        {
            Transform leftTarget = FindDeepChild(weaponPrefabInstance.transform, "leftHandTarget");
            if (leftTarget != null)
            {
                leftHandIK.data.target = leftTarget;
                Debug.Log($"[WeaponIKController] Bound Left Hand IK to {leftTarget.name}");
            }

            if (leftHandHint != null)
                leftHandIK.data.hint = leftHandHint;
        }
    }

    /// <summary>
    /// Helper: Find constraint by name in children
    /// </summary>
    private T FindConstraintInChildren<T>(string constraintName) where T : Behaviour
    {
        foreach (T constraint in GetComponentsInChildren<T>())
        {
            if (constraint.name == constraintName || constraint.gameObject.name == constraintName)
                return constraint;
        }
        return null;
    }

    /// <summary>
    /// Recursive search for child by name
    /// </summary>
    private Transform FindDeepChild(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name)
                return child;

            Transform result = FindDeepChild(child, name);
            if (result != null)
                return result;
        }
        return null;
    }

    #endregion

    #region === AIM RIG CONSTRAINT BINDING ===

    /// <summary>
    /// Bind AimRig MultiAimConstraints to the aim target
    /// </summary>
    public void BindAimRigConstraints()
    {
        if (!aimRig)
        {
            Debug.LogWarning("[WeaponIKController] No AimRig assigned!");
            return;
        }

        var constraints = aimRig.GetComponentsInChildren<MultiAimConstraint>();
        foreach (var constraint in constraints)
        {
            WeightedTransformArray sources = constraint.data.sourceObjects;
            sources.Clear();

            if (aimTarget)
            {
                sources.Add(new WeightedTransform(aimTarget, 1f));
                constraint.data.sourceObjects = sources;
                Debug.Log($"[WeaponIKController] Bound {constraint.name} to aim target");
            }
            else
            {
                Debug.LogWarning($"[WeaponIKController] No aim target for {constraint.name}");
            }
        }
    }

    /// <summary>
    /// Unbind all AimRig constraints (for unequip)
    /// </summary>
    public void UnbindAimRigConstraints()
    {
        if (!aimRig) 
            return;

        var constraints = aimRig.GetComponentsInChildren<MultiAimConstraint>();
        foreach (var constraint in constraints)
        {
            WeightedTransformArray sources = constraint.data.sourceObjects;
            sources.Clear();
            constraint.data.sourceObjects = sources;
            Debug.Log($"[WeaponIKController] Unbound {constraint.name}");
        }
    }

    #endregion

    #region === IK ENABLE/DISABLE ===

    public void EnableHumanoidSolver(bool enable)
    {
        if (humanoidAimSolver != null)
            humanoidAimSolver.solverEnabled = enable;
    }

    public void EnableIK(bool enable, float blendTime = 0.25f)
    {
        StopAllCoroutines();
        StartCoroutine(BlendIK(enable, blendTime));
    }

    private IEnumerator BlendIK(bool enable, float duration)
    {
        ikActive = enable;
        float start = ikWeight;
        float end = enable ? 1f : 0f;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            ikWeight = Mathf.Lerp(start, end, elapsed / duration);
            yield return null;
        }

        ikWeight = end;
    }

    /// <summary>
    /// Complete IK unbind (for weapon unequip)
    /// </summary>
    public void UnbindAllIK()
    {
        // Clear hand targets
        rightHandTarget = null;
        leftHandTarget = null;
        ikActive = false;
        ikWeight = 0f;

        // Clear TwoBoneIK constraints
        foreach (var twoBone in GetComponentsInChildren<TwoBoneIKConstraint>(true))
        {
            twoBone.data.target = null;
            twoBone.data.hint = null;
        }

        // Clear aim rig
        UnbindAimRigConstraints();
        aimTarget = null;
        aimEnabled = false;

        Debug.Log("[WeaponIKController] All IK unbound");
    }

    #endregion

    #region === ANIMATION IK CALLBACK ===

    private void OnAnimatorIK(int layerIndex)
    {
        if (!ikActive || animator == null) 
            return;

        // Right Hand IK
        if (rightHandTarget)
        {
            animator.SetIKPositionWeight(AvatarIKGoal.RightHand, ikWeight);
            animator.SetIKRotationWeight(AvatarIKGoal.RightHand, ikWeight);
            animator.SetIKPosition(AvatarIKGoal.RightHand, rightHandTarget.position);
            animator.SetIKRotation(AvatarIKGoal.RightHand, rightHandTarget.rotation);
        }
        else
        {
            animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 0f);
            animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 0f);
        }

        // Left Hand IK
        if (leftHandTarget)
        {
            animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, ikWeight);
            animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, ikWeight);
            animator.SetIKPosition(AvatarIKGoal.LeftHand, leftHandTarget.position);
            animator.SetIKRotation(AvatarIKGoal.LeftHand, leftHandTarget.rotation);
        }
        else
        {
            animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0f);
            animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 0f);
        }

        // Aim Look At
        if (aimEnabled && aimTarget)
        {
            animator.SetLookAtWeight(aimWeight, 0.5f, 1f, 0.5f, 0.5f);
            animator.SetLookAtPosition(aimTarget.position);
        }
    }

    #endregion

    #region === UPDATE LOOPS ===

    private void LateUpdate()
    {
        // Update HumanoidAimSolver
        if (humanoidAimSolver != null)
        {
            humanoidAimSolver.targetTransform = aimTarget;
            humanoidAimSolver.globalWeight = aimRig != null ? aimRig.weight : 1f;
        }

        // Spine rotation towards aim target
        if (aimEnabled && aimTarget && spineBone != null)
        {
            Vector3 aimDirection = (aimTarget.position - spineBone.position).normalized;
            Quaternion targetRotation = Quaternion.LookRotation(aimDirection);
            spineBone.rotation = Quaternion.Slerp(
                spineBone.rotation, 
                targetRotation, 
                Time.deltaTime * spineRotationSpeed  // ← Now properly a speed multiplier
            );
        }

        // Blend AimRig weight
        if (aimRig != null)
        {
            float targetWeight = aimEnabled ? 1f : 0f;
            aimRig.weight = Mathf.Lerp(aimRig.weight, targetWeight, Time.deltaTime * aimRigBlendSpeed);
        }
    }

    #endregion

    #region === DEBUG VISUALIZATION ===

    private void OnDrawGizmosSelected()
    {
        if (rightHandTarget)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(rightHandTarget.position, 0.02f);
            Gizmos.DrawWireSphere(rightHandTarget.position, 0.05f);
        }

        if (leftHandTarget)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(leftHandTarget.position, 0.02f);
            Gizmos.DrawWireSphere(leftHandTarget.position, 0.05f);
        }

        if (aimTarget)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(aimTarget.position, 0.03f);
            Gizmos.DrawWireSphere(aimTarget.position, 0.08f);

            if (spineBone)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(spineBone.position, aimTarget.position);
                Gizmos.DrawSphere(spineBone.position, 0.015f);
            }
        }
    }

    #endregion
}
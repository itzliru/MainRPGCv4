using UnityEngine;
using System;
using System.Collections;

[RequireComponent(typeof(Animator))]
public class NPCBaseAnimator : MonoBehaviour
{
    [Header("Layer Settings")]
    public int locomotionLayer = 0;
    public int weaponLayer = 1;
    public int combatLayer = 2;

    [Header("Blend Settings")]
    public float blendSpeed = 6f;

    private Animator animator;
    private float weaponWeightTarget;
    private float combatWeightTarget;

    // Lambda hooks
    public Func<bool> OnHasWeapon;
    public Func<bool> OnIsAttacking;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        bool hasWeapon = OnHasWeapon?.Invoke() ?? false;
        bool isAttacking = OnIsAttacking?.Invoke() ?? false;

        // Blend layer weights smoothly
        weaponWeightTarget = Mathf.MoveTowards(
            weaponWeightTarget,
            hasWeapon ? 1f : 0f,
            Time.deltaTime * blendSpeed
        );

        combatWeightTarget = Mathf.MoveTowards(
            combatWeightTarget,
            isAttacking ? 1f : 0f,
            Time.deltaTime * blendSpeed
        );

        animator.SetLayerWeight(weaponLayer, weaponWeightTarget);
        animator.SetLayerWeight(combatLayer, combatWeightTarget);
    }

    public void TriggerAttack()
    {
        animator.SetTrigger("Attack");
    }

    public void ResetAttackTrigger()
    {
        animator.ResetTrigger("Attack");
    }

    public void SetBool(string param, bool value)
    {
        animator.SetBool(param, value);
    }

    public void SetFloat(string param, float value)
    {
        animator.SetFloat(param, value);
    }

    public Animator GetAnimator() => animator;
}

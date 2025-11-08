using UnityEngine;
using System;

[RequireComponent(typeof(NPCBaseAnimator))]
public class NPCCombatAnimatorLayer : MonoBehaviour
{
    [Header("Combat Animation Control")]
    public string attackTrigger = "Attack";
    public string blockTrigger = "Block";
    public float attackCooldown = 1.0f;

    private NPCBaseAnimator baseAnimator;
    private float attackTimer = 0f;

    // Lambdas (pulled from combat)
    public Func<bool> OnIsAttacking;
    public Func<bool> OnHasWeapon;
    public Func<float> OnAttackSpeed;

    private void Awake()
    {
        baseAnimator = GetComponent<NPCBaseAnimator>();
    }

    private void Update()
    {
        attackTimer -= Time.deltaTime;

        bool hasWeapon = OnHasWeapon?.Invoke() ?? false;
        bool isAttacking = OnIsAttacking?.Invoke() ?? false;

        baseAnimator.OnHasWeapon = OnHasWeapon;
        baseAnimator.OnIsAttacking = OnIsAttacking;

        if (isAttacking && attackTimer <= 0f)
        {
            float atkSpeed = OnAttackSpeed?.Invoke() ?? 1f;
            attackTimer = attackCooldown / atkSpeed;
            baseAnimator.TriggerAttack();
        }
    }
}

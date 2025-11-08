using System.Collections;
using UnityEngine;

public class AttackTargetAction : GOAPAction
{
    private bool done = false;

    public override void Awake()
    {
        base.Awake();
        actionName = "AttackTarget";
        cost = 1f;

        preconditions.SetState("InRange", true);
        preconditions.SetState("HasWeapon", true);

        effects.SetState("IsAttacking", true);
    }

    public override bool CheckProceduralPrecondition(GameObject agentObj)
    {
        if (combat && combat.currentTarget && combat.hasWeapon)
        {
            target = combat.currentTarget;
            return true;
        }
        return false;
    }

    public override bool PerformAction(GameObject agentObj)
    {
        if (!target)
        {
            done = true;
            return false;
        }

        if (!combat.isAttacking)
        {
            combat.StartCoroutine(combat.AttackRoutine());
        }

        done = true; // One-shot attack, can be expanded for multi-attack
        return true;
    }

    public override bool IsDone() => done;
}

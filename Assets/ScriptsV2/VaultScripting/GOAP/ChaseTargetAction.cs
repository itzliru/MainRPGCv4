using UnityEngine;

public class ChaseTargetAction : GOAPAction
{
    private float stopDistance = 2.5f;
    private bool done = false;

    public override void Awake()
    {
        base.Awake();
        actionName = "ChaseTarget";
        cost = 2f;

        preconditions.SetState("HasTarget", true);
        effects.SetState("InRange", true);
    }

    public override bool CheckProceduralPrecondition(GameObject agentObj)
    {
        if (combat && combat.currentTarget)
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

        float dist = Vector3.Distance(transform.position, target.position);

        if (dist > stopDistance)
        {
            agent.SetDestination(target.position);
        }
        else
        {
            agent.Stop();
            done = true;
        }

        return true;
    }

    public override bool IsDone() => done;
}

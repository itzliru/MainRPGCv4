using UnityEngine;

public class SearchAction : GOAPAction
{
    private float timer;
    private float duration = 10f;
    private bool done = false;

    public override void Awake()
    {
        base.Awake();
        actionName = "SearchArea";
        cost = 3f;

        preconditions.SetState("HasTarget", false);
        effects.SetState("Searching", true);
    }

    public override bool CheckProceduralPrecondition(GameObject agentObj)
    {
        return !combat.currentTarget && combat.LastKnownPosition != Vector3.zero;
    }

    public override bool PerformAction(GameObject agentObj)
    {
        if (combat.LastKnownPosition == Vector3.zero)
        {
            done = true;
            return false;
        }

        if (agent.ReachedDestination())
        {
            Vector3 rnd = combat.LastKnownPosition + Random.insideUnitSphere * 4f;
            if (UnityEngine.AI.NavMesh.SamplePosition(rnd, out UnityEngine.AI.NavMeshHit hit, 2f, UnityEngine.AI.NavMesh.AllAreas))
                agent.SetDestination(hit.position);
        }

        timer += Time.deltaTime;
        if (timer >= duration)
            done = true;

        return true;
    }

    public override bool IsDone() => done;
}

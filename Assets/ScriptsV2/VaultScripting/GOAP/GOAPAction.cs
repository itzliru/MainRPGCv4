using UnityEngine;
using System.Collections;
using VaultSystems.Data;
using VaultSystems.Invoker;
public abstract class GOAPAction : MonoBehaviour
{
    public string actionName = "Unnamed Action";
    public float cost = 1f;
    public float duration = 1f;
    public bool requiresInRange = false;
    public Transform target;

    protected bool inRange = false;

    public GOAPWorldState preconditions = new GOAPWorldState();
    public GOAPWorldState effects = new GOAPWorldState();

    protected NPCCombatController combat;
    protected NPCAgentController agent;

    public virtual void Awake()
    {
        combat = GetComponent<NPCCombatController>();
        agent = GetComponent<NPCAgentController>();
    }

    public abstract bool CheckProceduralPrecondition(GameObject agentObj);
    public abstract bool PerformAction(GameObject agentObj);
    public abstract bool IsDone();

    public virtual bool RequiresInRange() => requiresInRange;
    public virtual bool IsInRange() => inRange;
    public virtual void SetInRange(bool value) => inRange = value;

    // ✅ Added to fix the error
    public virtual void ResetAction()
    {
        inRange = false;
    }
}

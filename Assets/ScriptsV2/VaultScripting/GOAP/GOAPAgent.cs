using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Collections;
using VaultSystems.Data;
using VaultSystems.Invoker;
[DefaultExecutionOrder(50)]
public class GOAPAgent : MonoBehaviour
{
    public GOAPWorldState worldState = new GOAPWorldState();
    public List<GOAPAction> availableActions = new List<GOAPAction>();
    public GOAPAction currentAction;

    private NPCCombatController combat;

    private void Awake()
    {
        combat = GetComponent<NPCCombatController>();
        availableActions.AddRange(GetComponents<GOAPAction>());
    }

    private void Update()
    {
        if (!combat) return;

        // Update world state from combat controller
        worldState["HasTarget"] = combat.currentTarget != null;
        worldState["Searching"] = combat.IsSearching;
        worldState["HasWeapon"] = combat.hasWeapon;
        worldState["InRange"] = combat.currentTarget &&
                                Vector3.Distance(combat.transform.position, combat.currentTarget.position) <= combat.attackRange;
        worldState["IsAttacking"] = combat.isAttacking;

        DecideAction();

        if (currentAction != null)
            currentAction.PerformAction(gameObject);
    }
    
    private void DecideAction()
    {
        if (currentAction != null && !currentAction.IsDone()) return;

        // Priority or cheapest first
        currentAction = availableActions
            .Where(a => a.CheckProceduralPrecondition(gameObject))
            .OrderBy(a => a.cost)
            .FirstOrDefault();

        if (currentAction != null)
        {
            currentAction.ResetAction();
            Debug.Log($"[GOAPAgent] Switching to action: {currentAction.actionName}");
        }
    }
}

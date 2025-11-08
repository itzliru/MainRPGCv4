using UnityEngine;
using System.Collections;
using VaultSystems.Data;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(NPCAgentController))]
public class FistCombatController : MonoBehaviour
{
    [Header("Melee Settings")]
    public float attackRange = 2f;           // How close to punch
    public float attackCooldown = 1f;        // Delay between punches
    public int damage = 10;                  // Punch damage

       [Header("Runtime References")]
    public Animator animator; // 👈 Animator on npc
    public NPCEquipmentManager _0xNPCE;  // 👈 NPCEquipmentManager.cs
    public NPCAgentController _0xNPCA; // 👈 NPCAgentController.cs
    public WeaponIKController _0xWIK1; // 👈 WeaponIKController.cs
    public UnityEngine.AI.NavMeshAgent _0xNAVM; // 👈 navAgent we have 2 scripts
    private GOAPAgent _0xG0AP; //👈 GOAPAgent.cs
    public NPCDataContainer _0xNPCD; // 👈 link to data
    public NPCCombatController _0xNPC2; // 👈 NPCCombatController

    private Transform currentTarget;
    private bool isFistMode = false;
    private bool isAttacking = false;

private void Awake()
{
    animator = GetComponent<Animator>();
    _0xNPCA = GetComponent<NPCAgentController>();
    _0xNPCE = GetComponent<NPCEquipmentManager>();
    _0xWIK1 = GetComponent<WeaponIKController>();
    _0xNAVM = GetComponent<UnityEngine.AI.NavMeshAgent>();
    _0xG0AP = GetComponent<GOAPAgent>();
    _0xNPCD = GetComponent<NPCDataContainer>();
    _0xNPC2 = GetComponent<NPCCombatController>();
}

	


    /// <summary>
    /// Called by NPCCombatController to enable fist mode
    /// </summary>
    public void EnableFistMode()
    {
        isFistMode = true;
        Debug.Log($"[{name}] Fist combat enabled.");
    }

    /// <summary>
    /// Called by NPCCombatController to disable fist mode
    /// </summary>
    public void DisableFistMode()
    {
        isFistMode = false;
        StopAllCoroutines();
        isAttacking = false;
    }

    /// <summary>
    /// Update loop for fist combat
    /// </summary>
public void EngageFistTarget(Transform target)
{
    if (!isFistMode || target == null) return;

    currentTarget = target;

    // Start coroutine that handles both movement and attacking
    if (!isAttacking)
        StartCoroutine(FistCombatRoutine());
}

private IEnumerator FistCombatRoutine()
{
    isAttacking = true;

    while (currentTarget != null && isFistMode)
    {
        float distance = Vector3.Distance(transform.position, currentTarget.position);

        if (distance > attackRange)
        {
            // Move toward the target
            _0xNPCA.SetDestination(currentTarget.position);
        }
        else
        {
            // Stop and attack
            _0xNPCA.Stop();
            animator.SetTrigger("Punch");
            yield return new WaitForSeconds(attackCooldown);
            continue; // loop again to check distance
        }

        yield return null; // wait a frame
    }

    isAttacking = false;
}

private IEnumerator FistTrackingRoutine()
{
    while (isFistMode && currentTarget != null)
    {
        // Update destination if target moves
        float distance = Vector3.Distance(transform.position, currentTarget.position);
        if (distance > attackRange)
            _0xNPCA.SetDestination(currentTarget.position);

        yield return null; // wait a frame
    }
}
}
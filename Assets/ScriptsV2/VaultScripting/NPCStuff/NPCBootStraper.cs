using System;
using UnityEngine;
using UnityEngine.AI;
using VaultSystems.Data;
using VaultSystems.Invoker;
using VaultSystems.Containers;
using VaultSystems.Controllers;

[DefaultExecutionOrder(-100)]
public class NPCBootStrapper : MonoBehaviour
{
    [Header("NPC Spawn Settings")]
    public GameObject npcPrefab;
    public NPCDataContainer baseData;
    public Transform spawnPoint;
    public bool autoSpawn = true;

    [Header("Testing")]
    public bool spawnTestTarget = true;
    public float targetRadius = 3f;
    public GameObject testTargetPrefab;

    private GameObject npcInstance;
    private NPCDataContainer npcDataClone;
    private EventDataContainer eventWrapper;  // ✅ NEW: Event broadcasting

    private NPCCombatController combat;
    private NPCTrajectoryAnimator trajectory;
    private WeaponIKController ik;
    private NavMeshAgent agent;

    private GameObject targetInstance;

    private void Awake()
    {
        if (autoSpawn)
            SpawnNPC();
    }

    public void SpawnNPC()
    {
        if (npcPrefab == null)
        {
            Debug.LogError("[NPCBootStrapper] Missing NPC Prefab reference.");
            return;
        }

        // 🔹 Clone Data
        npcDataClone = Instantiate(baseData);
        npcDataClone.displayName = baseData.displayName + "_Clone_" + UnityEngine.Random.Range(100, 999);
        npcDataClone.currentHP = npcDataClone.maxHP;
        

        // 🔹 Spawn NPC
        npcInstance = Instantiate(npcPrefab, spawnPoint ? spawnPoint.position : transform.position, Quaternion.identity);
        npcInstance.name = npcDataClone.displayName;

        // 🔹 Attach cloned data
        var existingData = npcInstance.GetComponent<NPCDataContainer>();
        if (existingData != null)
        {
            DestroyImmediate(existingData);
        }
        npcInstance.AddComponent<NPCDataContainer>().CopyFrom(npcDataClone);

        // 🔹 Get references
        combat = npcInstance.GetComponent<NPCCombatController>();
        trajectory = npcInstance.GetComponent<NPCTrajectoryAnimator>();
        ik = npcInstance.GetComponent<WeaponIKController>();
        agent = npcInstance.GetComponent<NavMeshAgent>();

        if (!combat || !trajectory)
        {
            Debug.LogError("[NPCBootStrapper] Missing key components on NPC prefab.");
            return;
        }

        // 🔹 Wire Lambdas with proper formatting
        trajectory.OnHasWeapon = () => combat.hasWeapon;
        trajectory.OnIsAttacking = () => combat.isAttacking;
        trajectory.OnCustomTargetVelocity = () => agent ? agent.velocity : Vector3.zero;

        // 🔹 Setup IK Binding - ✅ FIXED INDENTATION
        if (ik != null && ik.spineBone == null)
        {
            ik.spineBone = combat.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.Chest);
        }

        // 🔹 Equip weapon
        if (combat.equippedWeapon != null)
            npcDataClone.EquipWeaponRuntime(combat.equippedWeapon);

        // 🔹 Bind IK if weapon equipped - ✅ FIXED INDENTATION
        if (ik != null && combat.equippedWeapon != null && combat.equippedWeaponInstance != null)
        {
            ik.BindFromWeapon(combat.equippedWeapon, combat.equippedWeaponInstance);
        }

        // ========== ✅ NEW: EVENT WRAPPER SETUP ==========
        eventWrapper = new EventDataContainer(
            npcDataClone,
            $"npc_{npcDataClone.displayName.ToLower()}",
            EventDataContainer.EventBroadcastMode.Both
        );

        // Register with WorldBridge for other systems
        eventWrapper.RegisterWithWorldBridge();

        // Subscribe to death events (optional)
        eventWrapper.OnDeath += (finalHP) =>
        {
            Debug.Log($"[NPCBootStrapper] NPC {npcDataClone.displayName} died with {finalHP} HP");
            // Trigger loot drop, despawn animation, etc.
        };

        eventWrapper.OnTakeDamage += (damage, newHP, maxHP) =>
        {
            Debug.Log($"[NPCBootStrapper] NPC took {damage} damage: {newHP}/{maxHP} HP remaining");
        };

        // ========== ✅ NEW: PLAYER CASE CONTROLLER INTEGRATION ==========
        // Push NPC into "spawned" case so it can be managed alongside player state
        var caseController = PlayerCaseController.Instance;
        if (caseController != null)
        {
            caseController.PushCase(PlayerCaseController.PlayerCase.Custom, () =>
            {
                // Update AI every frame while case is active
                if (combat && npcDataClone.CurrentDeathState == DeathState.Alive)
                    combat.UpdateAI();
            });
        }

        // 🔹 Spawn test target
        if (spawnTestTarget)
            CreateTarget();

        Debug.Log($"[NPCBootStrapper] Spawned NPC: {npcDataClone.displayName} with event broadcasting");
    }

    private void CreateTarget()
    {
        if (testTargetPrefab == null)
        {
            testTargetPrefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            testTargetPrefab.transform.localScale = Vector3.one * 0.5f;
        }

        targetInstance = Instantiate(testTargetPrefab);
        targetInstance.name = "TestTarget";
        targetInstance.transform.position = npcInstance.transform.position + 
                                            (UnityEngine.Random.insideUnitSphere * targetRadius);
        targetInstance.layer = LayerMask.NameToLayer("Player");

        if (combat && (targetInstance.layer == LayerMask.NameToLayer("Player") || 
                       targetInstance.layer == LayerMask.NameToLayer("Enemy")))
        {
            combat.target = targetInstance.transform;  // ✅ Convert GameObject to Transform

        }
    }

    private void Update()
    {
        // ✅ NEW: Update event wrapper each frame to broadcast state changes
        eventWrapper?.UpdateEvents();
    }
}

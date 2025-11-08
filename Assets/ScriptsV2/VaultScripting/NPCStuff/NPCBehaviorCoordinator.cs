using UnityEngine.Events;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using VaultSystems.Data;
using VaultSystems.Invoker;
using VaultSystems.Controllers;
namespace VaultSystems.Data
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NPCDataContainer))]
    public class NPCBehaviorCoordinator : MonoBehaviour
    {
        protected NPCDataContainer npcData;
        protected NPCCombatController combatController;
        protected Animator animator;
        protected Transform player;

        [Header("Behavior Settings")]
        [Tooltip("How far the NPC can detect or interact with the player.")]
        public float detectionRange = 15f;

        [Tooltip("How often the coordinator updates behavior logic (seconds).")]
        public float behaviorTickRate = 0.25f;

        private float behaviorTimer;

        // 🧩 Unity Lifecycle
        protected virtual void Awake()
        {
            npcData = GetComponent<NPCDataContainer>();
            combatController = GetComponent<NPCCombatController>();
            animator = GetComponentInChildren<Animator>();

            // Optional: cache player ref (assumes Player tagged)
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
        }

        // ✅ NEW: Subscribe to death state changes
        protected virtual void OnEnable()
        {
            if (npcData != null)
            {
                npcData.OnDeathStateChanged += HandleDeathStateChange;
            }
        }

        // ✅ NEW: Unsubscribe from death state changes
        protected virtual void OnDisable()
        {
            if (npcData != null)
            {
                npcData.OnDeathStateChanged -= HandleDeathStateChange;
            }
        }

        // ✅ NEW: Handle death state transitions
        protected virtual void HandleDeathStateChange(DeathState newState)
        {
            switch (newState)
            {
                case DeathState.Alive:
                    OnNPCAlive();
                    break;
                case DeathState.Dying:
                    OnNPCDying();
                    break;
                case DeathState.RagdollActive:
                    OnNPCRagdoll();
                    break;
                case DeathState.Dead:
                    OnNPCDead();
                    break;
            }
        }

        protected virtual void OnNPCAlive() { }
        protected virtual void OnNPCDying() { }
        protected virtual void OnNPCRagdoll() 
        { 
            // Disable behavior when ragdoll active
            enabled = false;
        }
        protected virtual void OnNPCDead() { }

        protected virtual void Start()
        {
            RefreshCombatModule();
            if (npcData == null)
                Debug.LogError($"[NPCBehaviorCoordinator] Missing NPCDataContainer on {gameObject.name}");
        }

        protected virtual void Update()
        {
            // ✅ CHANGED: Use new IsDead property
            if (npcData == null || npcData.IsDead)
                return;

            behaviorTimer += Time.deltaTime;
            if (behaviorTimer >= behaviorTickRate)
            {
                behaviorTimer = 0f;
                EvaluateBehavior();
            }
        }

        private void RefreshCombatModule()
        {
            if (npcData == null || combatController == null) return;

            if (npcData.isEnemy)
                combatController.EnableCombatModule();
            else
                combatController.DisableCombatModule();
        }

        protected virtual void EvaluateBehavior()
        {
            if (player == null)
            {
                player = GameObject.FindGameObjectWithTag("Player")?.transform;
            }

            if (player != null)
            {
                float distance = Vector3.Distance(transform.position, player.position);

                if (npcData.isFriendly)
                    HandleFriendlyBehavior(distance);
                else if (npcData.isNeutral)
                    HandleNeutralBehavior(distance);
            }
        }

        // 🧩 Behavior Stubs — can be overridden by subclasses

        protected virtual void HandleFriendlyBehavior(float distance)
        {
            if (distance < 3f)
            {
                animator?.SetTrigger("Greet");
                npcData.OnDialogueProgress("friendly_greet");
            }
        }

        protected virtual void HandleNeutralBehavior(float distance)
        {
            if (distance < 5f)
            {
                FaceTarget(player.position);
            }
        }

        // 🧭 Utility
        protected void MoveToward(Vector3 targetPos)
        {
            animator?.SetBool("IsMoving", true);
            transform.position = Vector3.MoveTowards(transform.position, targetPos, Time.deltaTime * 2f);
            FaceTarget(targetPos);
        }

        protected void FaceTarget(Vector3 targetPos)
        {
            Vector3 direction = (targetPos - transform.position).normalized;
            direction.y = 0;
            if (direction != Vector3.zero)
            {
                Quaternion lookRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
            }
        }
    }
}

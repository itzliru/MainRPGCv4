using System;
using UnityEngine;
using VaultSystems.Data;
using VaultSystems.Invoker;

namespace VaultSystems.Quests
{
    /// <summary>
    /// Base class for quest nodes in the quest system.
    /// Provides common functionality for quest progression.
    /// </summary>
    public abstract class QuestNode : MonoBehaviour
    {
        [Header("Quest Node Settings")]
        public string nodeId;
        public string description;
        public bool isActive = false;

        protected PlayerDataContainer playerData;
        protected WorldBridgeSystem worldBridge;
        protected DynamicDictionaryInvoker invoker;

        protected virtual void Awake()
        {
            playerData = FindObjectOfType<PlayerDataContainer>();
            worldBridge = WorldBridgeSystem.Instance;
            invoker = DynamicDictionaryInvoker.Instance;
        }

        /// <summary>
        /// Activate the quest node.
        /// </summary>
        public virtual void Activate()
        {
            isActive = true;
            Debug.Log($"[QuestNode] Activated: {nodeId} - {description}");
        }

        /// <summary>
        /// Deactivate the quest node.
        /// </summary>
        public virtual void Deactivate()
        {
            isActive = false;
            Debug.Log($"[QuestNode] Deactivated: {nodeId}");
        }

        /// <summary>
        /// Check if the node can progress.
        /// </summary>
        public abstract bool CanProgress();

        /// <summary>
        /// Progress the quest node.
        /// </summary>
        public abstract void Progress();

        /// <summary>
        /// Complete the quest node.
        /// </summary>
        public virtual void Complete()
        {
            Deactivate();
            Debug.Log($"[QuestNode] Completed: {nodeId}");
        }
    }
}

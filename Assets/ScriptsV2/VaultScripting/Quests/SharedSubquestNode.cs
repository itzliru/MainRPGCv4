using System;
using UnityEngine;
using VaultSystems.Data;
using VaultSystems.Invoker;

namespace VaultSystems.Quests
{
    /// <summary>
    /// A quest node that invokes shared subquest logic via DynamicDictionaryInvoker.
    /// Allows reusable quest segments with progress tracking.
    /// </summary>
    public class SharedSubquestNode : QuestNode
    {
        [Header("Shared Subquest Settings")]
        public string subquestKey; // Key to invoke shared logic, e.g., "CollectItems", "DefeatEnemies"
        public object[] invokeArgs; // Arguments to pass to the shared logic

        [Header("Completion Settings")]
        public bool waitForCompletion = true; // If true, node completes when invoked logic finishes
        public QuestNode nextNode; // Node to activate after completion

        [Header("Progress Tracking")]
        public SubquestProgress progress; // Optional progress tracker

        private bool isCompleted = false;

        public override void Activate()
        {
            base.Activate();
            InvokeSharedLogic();
        }

        private void InvokeSharedLogic()
        {
            if (invoker == null || string.IsNullOrEmpty(subquestKey))
            {
                Debug.LogWarning($"[SharedSubquestNode] Invoker or key not set for {nodeId}");
                Complete();
                return;
            }

            // Register a completion callback if needed
            if (waitForCompletion)
            {
                // Assume shared logic will invoke a completion key
                string completionKey = $"{subquestKey}_Completed";
                invoker.Register(completionKey, OnSubquestCompleted, DynamicDictionaryInvoker.Layer.Func, nodeId);
            }

            // Invoke the shared subquest logic
            invoker.Invoke(subquestKey, invokeArgs);

            if (!waitForCompletion)
            {
                Complete();
            }
        }

        private void OnSubquestCompleted(object[] args)
        {
            if (!isActive) return;

            isCompleted = true;
            Debug.Log($"[SharedSubquestNode] Subquest completed: {subquestKey}");

            if (nextNode != null)
            {
                nextNode.Activate();
            }

            Complete();
        }

        public override bool CanProgress()
        {
            return isCompleted;
        }

        public override void Progress()
        {
            if (progress == null)
            {
                Debug.LogWarning($"[SharedSubquestNode] No progress tracker on {nodeId}");
                return;
            }

            progress.Increment();
            Debug.Log($"[SharedSubquestNode] Progress: {progress.current}/{progress.target}");

            if (progress.IsComplete)
            {
                OnSubquestCompleted(null);
            }
        }

        public override void Complete()
        {
            base.Complete();
            isCompleted = true;
        }
    }
}

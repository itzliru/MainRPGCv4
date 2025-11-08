using System.Collections.Generic;
using UnityEngine;
using VaultSystems.Data;

namespace VaultSystems.AI
{
    /// <summary>
    /// GOAP Action for subscribing to vision batch updates based on NPC state
    /// </summary>
    public class GOAPVisionSubscriptionAction : GOAPAction
    {
        private NPCDataContainer npcData;
        private IVisionSubscriber subscriber;
        private bool isSubscribed = false;

        public GOAPVisionSubscriptionAction(NPCDataContainer data, IVisionSubscriber sub)
        {
            npcData = data;
            subscriber = sub;

            preconditions = new() {
                { "IsAlive", true },
                { "HasNPCData", true }
            };

            effects = new() {
                { "HasVisionAwareness", true },
                { "CanDetectThreats", true }
            };
        }

        public override bool CheckProceduralPrecondition(GameObject agent)
        {
            // Check if NPC should subscribe based on disposition/factions
            return npcData.ShouldSubscribeToVision();
        }

        public override bool PerformAction(GameObject agent)
        {
            if (!isSubscribed && VisionBatchManager.Instance != null)
            {
                VisionBatchManager.Instance.Register(subscriber);
                isSubscribed = true;

                // Broadcast subscription event
                WorldBridgeSystem.Instance?.InvokeKey(EventKeys.Scene.NPC_VISION_SUBSCRIBED,
                    new object[] { npcData.npcId, npcData.GetVisionPriority() });
            }
            return true;
        }

        public override bool IsDone()
        {
            return isSubscribed;
        }

        public override void ResetAction()
        {
            if (isSubscribed && VisionBatchManager.Instance != null)
            {
                VisionBatchManager.Instance.Unregister(subscriber);
                isSubscribed = false;

                // Broadcast unsubscription event
                WorldBridgeSystem.Instance?.InvokeKey(EventKeys.Scene.NPC_VISION_UNSUBSCRIBED,
                    new object[] { npcData.npcId });
            }
            base.ResetAction();
        }
    }

    /// <summary>
    /// Weighted vision processor for faction-aware target filtering
    /// </summary>
    public class WeightedVisionProcessor : IVisionSubscriber
    {
        private NPCDataContainer npcData;
        private Dictionary<int, float> layerWeights;

        public WeightedVisionProcessor(NPCDataContainer data)
        {
            npcData = data;
            layerWeights = CalculateLayerWeights();
        }

        private Dictionary<int, float> CalculateLayerWeights()
        {
            var weights = new Dictionary<int, float>();
            float disposition = npcData.CalculateEffectiveDisposition();

            // Weight based on disposition and factions
            weights[GlobalLayerMaskManager.PlayerMask] = Mathf.Clamp01((50f - disposition) / 100f);
            weights[GlobalLayerMaskManager.EnemyMask] = Mathf.Clamp01((disposition + 50f) / 100f);
            weights[GlobalLayerMaskManager.InteractableMask] = 0.3f; // Always somewhat aware

            return weights;
        }

        public void OnVisionBatchUpdate(Dictionary<int, List<Transform>> batchResults)
        {
            foreach (var kvp in batchResults)
            {
                int layerMask = kvp.Key;
                var targets = kvp.Value;

                if (layerWeights.TryGetValue(layerMask, out float weight))
                {
                    // Process targets with weight-based priority
                    ProcessWeightedTargets(targets, weight);
                }
            }
        }

        private void ProcessWeightedTargets(List<Transform> targets, float weight)
        {
            if (weight <= 0f) return;

            foreach (var target in targets)
            {
                // Apply faction-based filtering
                float targetWeight = CalculateTargetWeight(target) * weight;

                if (targetWeight > 0.5f) // Threshold for consideration
                {
                    // Process high-priority target
                    OnHighPriorityTargetDetected(target, targetWeight);
                }
            }
        }

        private float CalculateTargetWeight(Transform target)
        {
            // Base weight from layer
            float weight = 1f;

            // Apply faction modifiers
            if (FactionSystem.Instance != null && npcData.factions.Count > 0)
            {
                // Check if target belongs to hostile faction
                // This would need additional component on targets to identify their factions
                // For now, use disposition as proxy
                float targetDisposition = npcData.CalculateEffectiveDisposition();
                if (targetDisposition < 0)
                {
                    weight *= 1.5f; // Increase weight for hostile targets
                }
            }

            return Mathf.Clamp01(weight);
        }

        protected virtual void OnHighPriorityTargetDetected(Transform target, float weight)
        {
            // Override in subclasses for specific behavior
            Debug.Log($"[WeightedVisionProcessor] High priority target detected: {target.name} (weight: {weight})");
        }
    }
}

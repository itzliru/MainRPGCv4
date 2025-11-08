using System;
using UnityEngine;
using VaultSystems.Data;
using VaultSystems.Invoker;

namespace VaultSystems.Quests
{
    /// <summary>
    /// A quest node that branches based on events listened via WorldBridge.
    /// Uses player stats for conditions.
    /// </summary>
    public class BranchingQuestNode : QuestNode
    {
        [Header("Branching Settings")]
        public string eventKey; // e.g., "MysticPowerChanged", "ScrollLevelChanged"
        public QuestNode branchA; // If condition met
        public QuestNode branchB; // If condition not met

        [Header("Condition Settings")]
        public string statName; // e.g., "mysticPower", "scrollLevel"
        public int thresholdValue;
        public ComparisonType comparison = ComparisonType.GreaterThan;

        public enum ComparisonType { GreaterThan, LessThan, EqualTo, NotEqualTo }

        private IDisposable eventRegistration;

        protected override void Awake()
        {
            base.Awake();
            // Register for the event via WorldBridge
            eventRegistration = worldBridge.RegisterInvoker(eventKey, OnEventTriggered);
        }

        private void OnDestroy()
        {
            eventRegistration?.Dispose();
            eventRegistration = null;
        }

        public override void Activate()
        {
            base.Activate();
            // Check initial condition
            if (CheckCondition())
            {
                ProgressToBranch(branchA);
            }
            else
            {
                ProgressToBranch(branchB);
            }
        }

        private void OnEventTriggered(object[] args)
        {
            if (!isActive) return;

            // Assume args contain old and new values, e.g., for stat changes
            if (args.Length >= 2 && args[1] is int newValue)
            {
                if (EvaluateCondition(newValue))
                {
                    ProgressToBranch(branchA);
                }
                else
                {
                    ProgressToBranch(branchB);
                }
            }
        }

        private bool CheckCondition()
        {
            if (playerData == null) return false;

            int currentValue = GetStatValue(statName);
            return EvaluateCondition(currentValue);
        }

        private bool EvaluateCondition(int value)
        {
            switch (comparison)
            {
                case ComparisonType.GreaterThan: return value > thresholdValue;
                case ComparisonType.LessThan: return value < thresholdValue;
                case ComparisonType.EqualTo: return value == thresholdValue;
                case ComparisonType.NotEqualTo: return value != thresholdValue;
                default: return false;
            }
        }

        private int GetStatValue(string stat)
        {
            switch (stat.ToLower())
            {
                case "mysticpower": return playerData.mysticPower;
                case "mysticimplants": return playerData.mysticImplants;
                case "scrolllevel": return playerData.scrollLevel;
                case "level": return playerData.level;
                case "agility": return playerData.agility;
                case "strength": return playerData.strength;
                case "wepskill": return playerData.wepSkill;
                default: return 0;
            }
        }

        private void ProgressToBranch(QuestNode branch)
        {
            if (branch != null)
            {
                branch.Activate();
            }
            Complete();
        }

        public override bool CanProgress()
        {
            return CheckCondition();
        }

        public override void Progress()
        {
            // Progression handled by event or initial check
        }
    }
}

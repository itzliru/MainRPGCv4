// EventKeys Usage Examples for Quest System Integration

/*
EventKeys provides centralized constants for all event types in the game.
This ensures consistency and makes refactoring easier.

USAGE PATTERNS:
*/

using System;
using UnityEngine;
using VaultSystems.Data;
using VaultSystems.Invoker;
using VaultSystems.Quests;
using VaultSystems.Containers;
using EventKeys = VaultSystems.Quests.EventKeys;

public class EventKeysUsageExamples : MonoBehaviour
{
    private void Start()
    {
        // Example 1: BranchingQuestNode listening to player stat changes
        SetupStatBasedBranching();

        // Example 2: Subscribing to player events for quest triggers
        SetupEventSubscriptions();

        // Example 3: Broadcasting events from quest logic
        SetupEventBroadcasting();

        // Example 4: NPC interaction events
        SetupNPCInteractions();
    }

    private void SetupStatBasedBranching()
    {
        // Create a branching node that reacts to mystic power changes
        GameObject branchObj = new GameObject("MysticPowerBranch");
        var branchNode = branchObj.AddComponent<BranchingQuestNode>();

        branchNode.nodeId = "PowerBranch";
        branchNode.description = "Branch based on Mystic Power level";

        // Use EventKeys constant for consistent event naming
        branchNode.eventKey = EventKeys.Player.MYSTIC_POWER_CHANGED;
        branchNode.statName = "mysticpower"; // This should match the stat name in PlayerDataContainer
        branchNode.thresholdValue = 5;
        branchNode.comparison = BranchingQuestNode.ComparisonType.GreaterThan;

        // Create branches
        GameObject highPowerObj = new GameObject("HighPowerPath");
        var highPowerNode = highPowerObj.AddComponent<SharedSubquestNode>();
        highPowerNode.nodeId = "HighPowerQuest";
        highPowerNode.subquestKey = "CollectRareItems";

        GameObject lowPowerObj = new GameObject("LowPowerPath");
        var lowPowerNode = lowPowerObj.AddComponent<SharedSubquestNode>();
        lowPowerNode.nodeId = "LowPowerQuest";
        lowPowerNode.subquestKey = "CollectCommonItems";

        branchNode.branchA = highPowerNode; // > 5 mystic power
        branchNode.branchB = lowPowerNode;  // <= 5 mystic power

        // Activate the branching node
        branchNode.Activate();
    }

    private void SetupEventSubscriptions()
    {
        // Subscribe to player level up events
        EventDataContainer.SubscribeTo(EventKeys.Player.LEVEL_UP, (args) => {
            int newLevel = (int)args[0];
            Debug.Log($"Player leveled up to {newLevel}! Triggering level-up quest.");

            // Trigger a quest or subquest
            DynamicDictionaryInvoker.Instance.Invoke("LevelUpQuest", newLevel);
        });

        // Subscribe to XP gained events
        EventDataContainer.SubscribeTo(EventKeys.Player.XP_GAINED, (args) => {
            int xpAmount = (int)args[0];
            Debug.Log($"Player gained {xpAmount} XP");

            // Could trigger progress on XP-based quests
            DynamicDictionaryInvoker.Instance.Invoke("XPProgress", xpAmount);
        });

        // Subscribe to scene changes
        EventDataContainer.SubscribeTo(EventKeys.Player.SCENE_CHANGED, (args) => {
            string newScene = (string)args[0];
            Debug.Log($"Player entered scene: {newScene}");

            // Trigger location-based quests
            DynamicDictionaryInvoker.Instance.Invoke("SceneQuest_" + newScene);
        });
    }

    private void SetupEventBroadcasting()
    {
        // In your quest completion logic, broadcast events
        // This would typically be called from SharedSubquestNode or custom quest logic

        // Example: Broadcast quest completion
        void OnQuestCompleted(string questId)
        {
            // Use WorldBridge to broadcast
            WorldBridgeSystem.Instance.InvokeKey("quest_completed", questId);

            // Or use EventDataContainer if you have a player container
            // playerEventContainer.BroadcastCustomEvent("quest_completed", questId);
        }

        // Example: Broadcast achievement unlocked
        void OnAchievementUnlocked(string achievementId)
        {
            WorldBridgeSystem.Instance.InvokeKey("achievement_unlocked", achievementId);
        }
    }

    private void SetupNPCInteractions()
    {
        // NPC events use prefix-based keys
        string npcId = "bandit_leader";

        // Subscribe to NPC behavior changes
        EventDataContainer.SubscribeTo(EventKeys.GetBehaviorChangedKey(npcId), (args) => {
            string newBehavior = (string)args[0];
            Debug.Log($"NPC {npcId} behavior changed to: {newBehavior}");

            // Trigger quest updates based on NPC state
            if (newBehavior == "hostile")
            {
                DynamicDictionaryInvoker.Instance.Invoke("HostileNPCEncounter", npcId);
            }
        });

        // Subscribe to NPC faction changes
        EventDataContainer.SubscribeTo(EventKeys.GetFactionChangedKey(npcId), (args) => {
            string newFaction = (string)args[0];
            Debug.Log($"NPC {npcId} joined faction: {newFaction}");

            // Update quest objectives
            DynamicDictionaryInvoker.Instance.Invoke("FactionChangeQuest", npcId, newFaction);
        });

        // Subscribe to quest interactions
        EventDataContainer.SubscribeTo(EventKeys.GetQuestInteractionKey(npcId), (args) => {
            string questId = (string)args[0];
            string action = (string)args[1];
            Debug.Log($"NPC {npcId} quest interaction: {questId} - {action}");

            // Handle quest dialogue or objectives
            DynamicDictionaryInvoker.Instance.Invoke("NPCQuestInteraction", npcId, questId, action);
        });
    }

    private void SetupFactionEvents()
    {
        // Subscribe to faction reputation changes
        EventDataContainer.SubscribeTo(EventKeys.Faction.REPUTATION_CHANGED, (args) => {
            string factionId = (string)args[0];
            int oldRep = (int)args[1];
            int newRep = (int)args[2];
            Debug.Log($"Faction {factionId} reputation changed: {oldRep} -> {newRep}");

            // Trigger reputation-based quests
            if (newRep >= 25 && oldRep < 25)
            {
                DynamicDictionaryInvoker.Instance.Invoke("FactionFriendlyQuest", factionId);
            }
            else if (newRep >= 75 && oldRep < 75)
            {
                DynamicDictionaryInvoker.Instance.Invoke("FactionHonoredQuest", factionId);
            }
        });

        // Subscribe to faction rank changes
        EventDataContainer.SubscribeTo(EventKeys.Faction.RANK_CHANGED, (args) => {
            string factionId = (string)args[0];
            var oldRank = (FactionStanding.FactionRank)args[1];
            var newRank = (FactionStanding.FactionRank)args[2];
            Debug.Log($"Faction {factionId} rank changed: {oldRank} -> {newRank}");

            // Trigger rank-based quest branches
            DynamicDictionaryInvoker.Instance.Invoke($"FactionRankQuest_{factionId}", newRank);
        });

        // Subscribe to faction joins
        EventDataContainer.SubscribeTo(EventKeys.Faction.JOINED, (args) => {
            string factionId = (string)args[0];
            Debug.Log($"Joined faction: {factionId}");

            // Trigger faction initiation quests
            DynamicDictionaryInvoker.Instance.Invoke("FactionInitiationQuest", factionId);
        });

        // Subscribe to faction leaves
        EventDataContainer.SubscribeTo(EventKeys.Faction.LEFT, (args) => {
            string factionId = (string)args[0];
            Debug.Log($"Left faction: {factionId}");

            // Trigger faction betrayal consequences
            DynamicDictionaryInvoker.Instance.Invoke("FactionBetrayalQuest", factionId);
        });
    }
}

/*
ADDITIONAL EXAMPLES:

1. Weapon Events (for combat quests):
   - EventKeys.Player.WEAPON_FIRED: Track shots fired in combat quests
   - EventKeys.Player.RELOADED: Trigger ammo management quests
   - EventKeys.Player.WEAPON_CHANGED: Update weapon proficiency quests

2. Health Events (for survival quests):
   - EventKeys.GetDamagedKey("player"): Track damage taken
   - EventKeys.GetHealedKey("player"): Track healing used
   - EventKeys.GetDiedKey("player"): Trigger respawn quests

3. Custom Quest Events:
   You can extend EventKeys with your own constants:
   public static class Quest
   {
       public const string STARTED = "quest_started";
       public const string COMPLETED = "quest_completed";
       public const string FAILED = "quest_failed";
       public const string OBJECTIVE_UPDATED = "quest_objective_updated";
   }

4. Dynamic Event Keys:
   For quests with multiple instances, use helper methods:
   string questKey = $"quest_{questId}_progress";
   WorldBridgeSystem.Instance.InvokeKey(questKey, progressValue);

INTEGRATION WITH QUEST NODES:

- BranchingQuestNode: Set eventKey to EventKeys.Player.MYSTIC_POWER_CHANGED
- SharedSubquestNode: Use invoker to trigger completion events
- QuestNode: Override Activate() to register event listeners

This system allows quests to be event-driven, modular, and easily extensible.
*/

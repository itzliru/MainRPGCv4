// Comprehensive Usage Guide for Quest Nodes with WorldBridge and DynamicDictionaryInvoker

/*
OVERVIEW:
The quest system uses modular nodes that integrate with WorldBridge for event-driven branching
and DynamicDictionaryInvoker for shared, reusable subquest logic. This allows dynamic quests
that respond to player stats and events.

SETUP:
1. Ensure WorldBridgeSystem and DynamicDictionaryInvoker are in your scene.
2. Create quest nodes as GameObjects with the appropriate components.
3. Register shared subquest logic with the invoker.

EXAMPLE SETUP SCRIPT:
*/

using System;
using UnityEngine;
using VaultSystems.Data;
using VaultSystems.Invoker;
using VaultSystems.Quests;

public class QuestSystemSetup : MonoBehaviour
{
    private void Start()
    {
        // Register shared subquest logic
        RegisterSharedSubquests();

        // Create and configure quest nodes
        SetupExampleQuest();
    }

    private void RegisterSharedSubquests()
    {
        var invoker = DynamicDictionaryInvoker.Instance;

        // Register "CollectItems" subquest
        invoker.Register("CollectItems", (args) => {
            // args[0] = item count needed
            int needed = (int)args[0];
            Debug.Log($"Starting item collection quest: need {needed} items");

            // In a real implementation, this would spawn items and track collection
            // For demo, simulate completion after delay
            StartCoroutine(SimulateItemCollection(needed));
        });

        // Register "DefeatEnemies" subquest
        invoker.Register("DefeatEnemies", (args) => {
            int enemyCount = (int)args[0];
            Debug.Log($"Starting enemy defeat quest: defeat {enemyCount} enemies");

            // Simulate enemy spawning and tracking
            StartCoroutine(SimulateEnemyDefeat(enemyCount));
        });

        // Register completion callbacks
        invoker.Register("CollectItems_Completed", (args) => {
            Debug.Log("Item collection completed!");
        });

        invoker.Register("DefeatEnemies_Completed", (args) => {
            Debug.Log("Enemy defeat completed!");
        });
    }

    private System.Collections.IEnumerator SimulateItemCollection(int needed)
    {
        yield return new WaitForSeconds(2f); // Simulate time to collect
        DynamicDictionaryInvoker.Instance.Invoke("CollectItems_Completed");
    }

    private System.Collections.IEnumerator SimulateEnemyDefeat(int needed)
    {
        yield return new WaitForSeconds(3f); // Simulate combat time
        DynamicDictionaryInvoker.Instance.Invoke("DefeatEnemies_Completed");
    }

    private void SetupExampleQuest()
    {
        // Create branching node
        GameObject branchObj = new GameObject("MysticPowerBranch");
        var branchNode = branchObj.AddComponent<BranchingQuestNode>();
        branchNode.nodeId = "PowerBranch";
        branchNode.description = "Branch based on Mystic Power level";
        branchNode.eventKey = "player_mystic_power_changed"; // From EventKeys.Player.MYSTIC_POWER_CHANGED
        branchNode.statName = "mysticpower";
        branchNode.thresholdValue = 5;
        branchNode.comparison = BranchingQuestNode.ComparisonType.GreaterThan;

        // Create subquest nodes
        GameObject highPowerObj = new GameObject("HighPowerPath");
        var highPowerNode = highPowerObj.AddComponent<SharedSubquestNode>();
        highPowerNode.nodeId = "HighPowerQuest";
        highPowerNode.description = "Advanced collection quest";
        highPowerNode.subquestKey = "CollectItems";
        highPowerNode.invokeArgs = new object[] { 10 }; // Need 10 items
        highPowerNode.waitForCompletion = true;
        highPowerNode.progress = new SubquestProgress { key = "CollectItems", target = 10 };

        GameObject lowPowerObj = new GameObject("LowPowerPath");
        var lowPowerNode = lowPowerObj.AddComponent<SharedSubquestNode>();
        lowPowerNode.nodeId = "LowPowerQuest";
        lowPowerNode.description = "Basic enemy defeat quest";
        lowPowerNode.subquestKey = "DefeatEnemies";
        lowPowerNode.invokeArgs = new object[] { 3 }; // Defeat 3 enemies
        lowPowerNode.waitForCompletion = true;
        lowPowerNode.progress = new SubquestProgress { key = "DefeatEnemies", target = 3 };

        // Connect branches
        branchNode.branchA = highPowerNode;
        branchNode.branchB = lowPowerNode;

        // Start the quest
        branchNode.Activate();
    }
}

/*
USAGE FLOW:

1. INITIALIZATION:
   - WorldBridgeSystem automatically finds/creates DynamicDictionaryInvoker
   - Register shared logic with invoker using Register(key, action)

2. QUEST CREATION:
   - Create QuestNode GameObjects in scene or via script
   - Configure properties (eventKey, statName, subquestKey, etc.)
   - Link nodes together (branchA, branchB, nextNode)

3. EVENT INTEGRATION:
   - BranchingQuestNode listens to WorldBridge events (e.g., stat changes)
   - Events are broadcast via PlayerEventDataContainer (automatic on stat changes)
   - Use EventKeys constants for consistent event naming

4. SHARED LOGIC:
   - SharedSubquestNode invokes registered actions by key
   - Actions receive object[] args for parameters
   - Completion signaled by invoking "{key}_Completed"

5. ADVANCED USAGE:
   - Chain multiple nodes: nodeA.nextNode = nodeB
   - Use SubquestProgress for tracking within shared logic
   - Register multiple handlers per key (invoker supports layers)

EXAMPLE SHARED LOGIC WITH PROGRESS:

invoker.Register("CollectItems", (args) => {
    int needed = (int)args[0];
    var progress = new SubquestProgress { key = "items", target = needed };

    // Register progress tracking
    invoker.Register("ItemCollected", (itemArgs) => {
        progress.Increment();
        if (progress.IsComplete) {
            invoker.Invoke("CollectItems_Completed");
        }
    });
});

Then in item pickup code:
DynamicDictionaryInvoker.Instance.Invoke("ItemCollected");

This creates reusable, progress-tracked subquests.

FACTION SYSTEM INTEGRATION:

The FactionSystem integrates with quests through reputation-based branching and faction-specific content.

EXAMPLE FACTION-BASED QUEST:

private void SetupFactionBasedQuest()
{
    // Create a quest that branches based on faction membership
    GameObject factionBranchObj = new GameObject("FactionBranch");
    var factionBranchNode = factionBranchObj.AddComponent<BranchingQuestNode>();
    factionBranchNode.nodeId = "FactionBranch";
    factionBranchNode.description = "Branch based on faction membership";

    // Listen to faction join events
    factionBranchNode.eventKey = EventKeys.Faction.JOINED;
    factionBranchNode.statName = "faction_count"; // Custom stat tracking faction membership
    factionBranchNode.thresholdValue = 0;
    factionBranchNode.comparison = BranchingQuestNode.ComparisonType.GreaterThan;

    // Create faction-specific quest paths
    GameObject guildQuestObj = new GameObject("GuildQuestPath");
    var guildQuestNode = guildQuestObj.AddComponent<SharedSubquestNode>();
    guildQuestNode.nodeId = "GuildQuest";
    guildQuestNode.subquestKey = "GuildFactionQuest";
    guildQuestNode.progress = new SubquestProgress { key = "guild_quest", target = 5 };

    GameObject neutralQuestObj = new GameObject("NeutralQuestPath");
    var neutralQuestNode = neutralQuestObj.AddComponent<SharedSubquestNode>();
    neutralQuestNode.nodeId = "NeutralQuest";
    neutralQuestNode.subquestKey = "NeutralFactionQuest";
    neutralQuestNode.progress = new SubquestProgress { key = "neutral_quest", target = 3 };

    factionBranchNode.branchA = guildQuestNode; // Has faction membership
    factionBranchNode.branchB = neutralQuestNode; // No faction membership

    // Chain quests
    guildQuestNode.nextNode = neutralQuestNode;

    factionBranchNode.Activate();
}

FACTION REPUTATION QUESTS:

// Register reputation-based quest triggers
FactionSystem.Instance.OnReputationChanged.AddListener((factionId, oldRep, newRep) => {
    if (newRep >= 25 && oldRep < 25)
    {
        DynamicDictionaryInvoker.Instance.Invoke("FactionFriendlyQuest", factionId);
    }
    else if (newRep >= 75 && oldRep < 75)
    {
        DynamicDictionaryInvoker.Instance.Invoke("FactionHonoredQuest", factionId);
    }
});

// Register rank change quests
FactionSystem.Instance.OnRankChanged.AddListener((factionId, oldRank, newRank) => {
    DynamicDictionaryInvoker.Instance.Invoke($"FactionRankQuest_{factionId}", newRank);
});

This creates a comprehensive quest system with faction integration.
*/

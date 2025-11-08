// Example usage and explanation of quest nodes using WorldBridge and DynamicDictionaryInvoker

/*
Quest Flow Example:

1. Main Quest Starts:
   - QuestNode: "StartQuest" activates BranchingQuestNode.

2. Branching Based on MysticPower:
   - BranchingQuestNode listens to "MysticPowerChanged" event via WorldBridge.
   - If player.mysticPower > 5, branch to SharedSubquestNode "HighPowerPath".
   - Else, branch to SharedSubquestNode "LowPowerPath".

3. Shared Subquests:
   - SharedSubquestNode invokes "CollectItems" key via DynamicDictionaryInvoker.
   - Registered logic for "CollectItems" might spawn items, track collection, etc.
   - Upon completion, invokes "CollectItems_Completed" to notify the node.
   - Node then activates next QuestNode.

Integration with Player Stats:
- BranchingQuestNode checks stats like mysticPower, scrollLevel, etc., from PlayerDataContainer.
- Events are broadcasted via PlayerEventDataContainer (e.g., BroadcastMysticPowerChanged).

Shared Logic Registration Example:
In some manager script:
invoker.Register("CollectItems", (args) => {
    // Logic to spawn and track item collection
    // ...
    // On completion: invoker.Invoke("CollectItems_Completed");
});

This allows modular, event-driven quests with reusable subquests.
*/

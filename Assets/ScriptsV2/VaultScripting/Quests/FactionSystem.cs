using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using VaultSystems.Data;
using VaultSystems.Invoker;
using VaultSystems.Containers;

namespace VaultSystems.Quests
{
    /// <summary>
    /// Comprehensive faction system with reputation, relationships, and quest integration.
    /// </summary>
    [Serializable]
    public class FactionData
    {
        public string factionId;
        public string displayName;
        public string description;
        public Color factionColor = Color.white;

        [Header("Relationships")]
        public Dictionary<string, int> relationships = new(); // factionId -> standing (-100 to 100)

        [Header("Requirements")]
        public int joinReputationRequired = 0;
        public List<string> prerequisiteFactions = new();

        public FactionData(string id, string name, string desc = "")
        {
            factionId = id;
            displayName = name;
            description = desc;
        }
    }

    /// <summary>
    /// Player's standing with a specific faction.
    /// </summary>
    [Serializable]
    public class FactionStanding
    {
        public string factionId;
        public int reputation = 0; // -100 (hated) to 100 (revered)
        public FactionRank currentRank = FactionRank.Neutral;

        public enum FactionRank
        {
            Hated = -3,
            Hostile = -2,
            Unfriendly = -1,
            Neutral = 0,
            Friendly = 1,
            Honored = 2,
            Revered = 3
        }

        public FactionRank GetRank()
        {
            if (reputation <= -75) return FactionRank.Hated;
            if (reputation <= -25) return FactionRank.Hostile;
            if (reputation <= -10) return FactionRank.Unfriendly;
            if (reputation >= 75) return FactionRank.Revered;
            if (reputation >= 25) return FactionRank.Honored;
            if (reputation >= 10) return FactionRank.Friendly;
            return FactionRank.Neutral;
        }

        public void UpdateRank()
        {
            currentRank = GetRank();
        }
    }

    /// <summary>
    /// Main faction system manager.
    /// </summary>
    public class FactionSystem : MonoBehaviour
    {
        public static FactionSystem Instance { get; private set; }

        [Header("Faction Definitions")]
        public List<FactionData> allFactions = new();

        [Header("Player Standings")]
        public Dictionary<string, FactionStanding> playerStandings = new();

        [Header("Events")]
        public UnityEvent<string, int, int> OnReputationChanged; // factionId, oldRep, newRep
        public UnityEvent<string, FactionStanding.FactionRank, FactionStanding.FactionRank> OnRankChanged; // factionId, oldRank, newRank
        public UnityEvent<string> OnFactionJoined; // factionId
        public UnityEvent<string> OnFactionLeft; // factionId

        private PlayerDataContainer playerData;
        private PlayerEventDataContainer eventContainer;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeDefaultFactions();
        }

        private void Start()
        {
            // Find player data
            playerData = FindObjectOfType<PlayerDataContainer>();
            if (playerData != null)
            {
                eventContainer = new PlayerEventDataContainer(playerData);
                playerData.InitializeEventDataContainer(eventContainer);
                eventContainer.RegisterWithWorldBridge();
            }

            // Register faction events
            RegisterFactionEvents();
        }

        private void InitializeDefaultFactions()
        {
            // Example factions - customize as needed
            allFactions.Add(new FactionData("merchants_guild", "Merchants Guild", "Traders and businessmen"));
            allFactions.Add(new FactionData("warriors_guild", "Warriors Guild", "Soldiers and fighters"));
            allFactions.Add(new FactionData("mages_academy", "Mages Academy", "Scholars of magic"));
            allFactions.Add(new FactionData("thieves_guild", "Thieves Guild", "Shadow operatives"));
            allFactions.Add(new FactionData("nobles_court", "Nobles Court", "Aristocracy and rulers"));

            // Set up relationships
            foreach (var faction in allFactions)
            {
                InitializeFactionRelationships(faction);
            }
        }

        private void InitializeFactionRelationships(FactionData faction)
        {
            // Example relationships - customize based on game lore
            switch (faction.factionId)
            {
                case "mystics_guild":
                    faction.relationships["thieves_guild"] = -50;
                    faction.relationships["nobles_court"] = 20;
                    break;
                case "bandits_guild":
                    faction.relationships["mages_academy"] = -10;
                    faction.relationships["nobles_court"] = 30;
                    break;
                case "card_academy":
                    faction.relationships["warriors_guild"] = -10;
                    faction.relationships["thieves_guild"] = -20;
                    break;
                case "thieves_guild":
                    faction.relationships["merchants_guild"] = -50;
                    faction.relationships["nobles_court"] = -40;
                    break;
                case "nobles_court":
                    faction.relationships["merchants_guild"] = 20;
                    faction.relationships["warriors_guild"] = 30;
                    faction.relationships["thieves_guild"] = -40;
                    break;
            }
        }

        private void RegisterFactionEvents()
        {
            // Register with WorldBridge for faction events
            var bridge = WorldBridgeSystem.Instance;
            if (bridge != null)
            {
                bridge.RegisterInvoker("faction_reputation_changed", (args) => {
                    string factionId = (string)args[0];
                    int oldRep = (int)args[1];
                    int newRep = (int)args[2];
                    OnReputationChanged?.Invoke(factionId, oldRep, newRep);
                });

                bridge.RegisterInvoker("faction_rank_changed", (args) => {
                    string factionId = (string)args[0];
                    var oldRank = (FactionStanding.FactionRank)args[1];
                    var newRank = (FactionStanding.FactionRank)args[2];
                    OnRankChanged?.Invoke(factionId, oldRank, newRank);
                });

                bridge.RegisterInvoker("faction_joined", (args) => {
                    string factionId = (string)args[0];
                    OnFactionJoined?.Invoke(factionId);
                });

                bridge.RegisterInvoker("faction_left", (args) => {
                    string factionId = (string)args[0];
                    OnFactionLeft?.Invoke(factionId);
                });
            }
        }

        #region Public API

        /// <summary>
        /// Get or create faction standing for player.
        /// </summary>
        public FactionStanding GetStanding(string factionId)
        {
            if (!playerStandings.TryGetValue(factionId, out var standing))
            {
                standing = new FactionStanding { factionId = factionId };
                playerStandings[factionId] = standing;
            }
            return standing;
        }

        /// <summary>
        /// Get player standing (alias for GetStanding).
        /// </summary>
        public FactionStanding GetPlayerStanding(string factionId)
        {
            return GetStanding(factionId);
        }

        /// <summary>
        /// Modify reputation with a faction.
        /// </summary>
        public void ModifyReputation(string factionId, int amount)
        {
            var standing = GetStanding(factionId);
            int oldRep = standing.reputation;
            standing.reputation = Mathf.Clamp(standing.reputation + amount, -100, 100);

            var oldRank = standing.currentRank;
            standing.UpdateRank();

            // Broadcast events
            WorldBridgeSystem.Instance?.InvokeKey("faction_reputation_changed",
                factionId, oldRep, standing.reputation);

            if (oldRank != standing.currentRank)
            {
                WorldBridgeSystem.Instance?.InvokeKey("faction_rank_changed",
                    factionId, oldRank, standing.currentRank);
            }

            Debug.Log($"[FactionSystem] {factionId} reputation: {oldRep} -> {standing.reputation} (Rank: {oldRank} -> {standing.currentRank})");
        }

        /// <summary>
        /// Attempt to join a faction.
        /// </summary>
        public bool JoinFaction(string factionId)
        {
            if (playerData == null || playerData.IsInFaction(factionId))
                return false;

            var faction = allFactions.Find(f => f.factionId == factionId);
            if (faction == null) return false;

            // Check prerequisites
            var standing = GetStanding(factionId);
            if (standing.reputation < faction.joinReputationRequired)
                return false;

            foreach (var prereq in faction.prerequisiteFactions)
            {
                if (!playerData.IsInFaction(prereq))
                    return false;
            }

            playerData.AddFaction(factionId);
            WorldBridgeSystem.Instance?.InvokeKey("faction_joined", factionId);

            Debug.Log($"[FactionSystem] Joined faction: {factionId}");
            return true;
        }

        /// <summary>
        /// Leave a faction.
        /// </summary>
        public bool LeaveFaction(string factionId)
        {
            if (playerData == null || !playerData.IsInFaction(factionId))
                return false;

            playerData.RemoveFaction(factionId);
            WorldBridgeSystem.Instance?.InvokeKey("faction_left", factionId);

            Debug.Log($"[FactionSystem] Left faction: {factionId}");
            return true;
        }

        /// <summary>
        /// Get relationship between two factions.
        /// </summary>
        public int GetFactionRelationship(string factionA, string factionB)
        {
            var faction = allFactions.Find(f => f.factionId == factionA);
            if (faction != null && faction.relationships.TryGetValue(factionB, out int rel))
                return rel;
            return 0; // Neutral
        }

        /// <summary>
        /// Check if player can access faction-specific content.
        /// </summary>
        public bool HasFactionAccess(string factionId, int minReputation = 0)
        {
            if (!playerData.IsInFaction(factionId))
                return false;

            var standing = GetStanding(factionId);
            return standing.reputation >= minReputation;
        }

        #endregion

        #region Quest Integration

        /// <summary>
        /// Check if player meets faction requirements for a quest.
        /// </summary>
        public bool MeetsFactionRequirements(List<string> requiredFactions, Dictionary<string, int> minReputations)
        {
            foreach (var reqFaction in requiredFactions)
            {
                if (!playerData.IsInFaction(reqFaction))
                    return false;
            }

            foreach (var kvp in minReputations)
            {
                var standing = GetStanding(kvp.Key);
                if (standing.reputation < kvp.Value)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Get available faction quests based on current standings.
        /// </summary>
        public List<string> GetAvailableFactionQuests()
        {
            var available = new List<string>();

            foreach (var faction in allFactions)
            {
                if (playerData.IsInFaction(faction.factionId))
                {
                    var standing = GetStanding(faction.factionId);
                    // Add quest keys based on rank
                    switch (standing.currentRank)
                    {
                        case FactionStanding.FactionRank.Friendly:
                            available.Add($"{faction.factionId}_friendly_quests");
                            break;
                        case FactionStanding.FactionRank.Honored:
                            available.Add($"{faction.factionId}_honored_quests");
                            break;
                        case FactionStanding.FactionRank.Revered:
                            available.Add($"{faction.factionId}_revered_quests");
                            break;
                    }
                }
            }

            return available;
        }

        #endregion
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;
using VaultSystems.Data;
using VaultSystems.Invoker;
using VaultSystems.Quests;

namespace VaultSystems.Data
{
    /// <summary>
    /// Debug script for testing FactionSystem integration with WorldBridgeSystem.
    /// Attach to any GameObject and press keys to run tests.
    /// </summary>
    public class FactionSystemDebug : MonoBehaviour
    {
        private FactionSystem _factionSystem;
        private WorldBridgeSystem _worldBridge;
        private PlayerDataContainer _playerData;

        private void Start()
        {
            _factionSystem = FactionSystem.Instance;
            _worldBridge = WorldBridgeSystem.Instance;
            _playerData = FindObjectOfType<PlayerDataContainer>();

            if (_factionSystem == null)
                UnityEngine.Debug.LogError("[FactionSystemDebug] FactionSystem not found!");
            
            if (_worldBridge == null)
                UnityEngine.Debug.LogError("[FactionSystemDebug] WorldBridgeSystem not found!");

            if (_playerData == null)
                UnityEngine.Debug.LogError("[FactionSystemDebug] PlayerDataContainer not found!");

            SetupEventListeners();
            PrintInstructions();
        }

        private void Update()
        {
            // Test keybinds
            if (Input.GetKeyDown(KeyCode.F1)) TestReputationModification();
            if (Input.GetKeyDown(KeyCode.F2)) TestFactionJoinLeave();
            if (Input.GetKeyDown(KeyCode.F3)) TestFactionAccess();
            if (Input.GetKeyDown(KeyCode.F4)) TestRankThresholds();
            if (Input.GetKeyDown(KeyCode.F5)) TestWorldBridgeEvents();
            if (Input.GetKeyDown(KeyCode.F6)) PrintStatus();
        }

        #region Test Methods

        /// <summary>Test 1: Reputation Modification with threshold detection</summary>
        private void TestReputationModification()
        {
            UnityEngine.Debug.Log("\n========== TEST 1: REPUTATION MODIFICATION ==========");

            if (_factionSystem == null) return;

            var standing = _factionSystem.GetStanding("merchants_guild");

            UnityEngine.Debug.Log($"[TEST] Initial reputation: {standing.reputation}");
            UnityEngine.Debug.Log($"[TEST] Initial rank: {standing.currentRank}");

            // Test 1A: Increase reputation
            _factionSystem.ModifyReputation("merchants_guild", 15);
            standing = _factionSystem.GetStanding("merchants_guild");
            
            if (standing.reputation >= 10 && standing.currentRank == FactionStanding.FactionRank.Friendly)
            {
                UnityEngine.Debug.Log("✓ PASS: Reputation increased and rank updated to Friendly");
            }
            else
            {
                UnityEngine.Debug.LogError("✗ FAIL: Reputation/rank not updated correctly");
            }

            // Test 1B: Continue increasing to Honored threshold
            _factionSystem.ModifyReputation("merchants_guild", 15);
            standing = _factionSystem.GetStanding("merchants_guild");

            if (standing.reputation >= 25 && standing.currentRank == FactionStanding.FactionRank.Honored)
            {
                UnityEngine.Debug.Log("✓ PASS: Reputation increased to Honored (25+)");
            }
            else
            {
                UnityEngine.Debug.LogError("✗ FAIL: Honored threshold not reached");
            }

            // Test 1C: Max clamping
            _factionSystem.ModifyReputation("merchants_guild", 100);
            standing = _factionSystem.GetStanding("merchants_guild");

            if (standing.reputation == 100)
            {
                UnityEngine.Debug.Log("✓ PASS: Reputation clamped at 100");
            }
            else
            {
                UnityEngine.Debug.LogError($"✗ FAIL: Reputation not clamped (value: {standing.reputation})");
            }
        }

        /// <summary>Test 2: Faction Join/Leave with prerequisites</summary>
        private void TestFactionJoinLeave()
        {
            UnityEngine.Debug.Log("\n========== TEST 2: FACTION JOIN/LEAVE ==========");

            if (_factionSystem == null || _playerData == null) return;

            // Test 2A: Join faction (should succeed with 0 rep requirement)
            bool joined = _factionSystem.JoinFaction("merchants_guild");

            if (joined && _playerData.IsInFaction("merchants_guild"))
            {
                UnityEngine.Debug.Log("✓ PASS: Successfully joined merchants_guild");
            }
            else
            {
                UnityEngine.Debug.LogError("✗ FAIL: Failed to join merchants_guild");
            }

            // Test 2B: Check faction list
            if (_playerData.factions.Contains("merchants_guild"))
            {
                UnityEngine.Debug.Log($"✓ PASS: Faction appears in player data ({string.Join(", ", _playerData.factions)})");
            }
            else
            {
                UnityEngine.Debug.LogError("✗ FAIL: Faction not in player factions list");
            }

            // Test 2C: Leave faction
            bool left = _factionSystem.LeaveFaction("merchants_guild");

            if (left && !_playerData.IsInFaction("merchants_guild"))
            {
                UnityEngine.Debug.Log("✓ PASS: Successfully left merchants_guild");
            }
            else
            {
                UnityEngine.Debug.LogError("✗ FAIL: Failed to leave faction");
            }
        }

        /// <summary>Test 3: Faction Access Checks</summary>
        private void TestFactionAccess()
        {
            UnityEngine.Debug.Log("\n========== TEST 3: FACTION ACCESS CHECKS ==========");

            if (_factionSystem == null || _playerData == null) return;

            // Test 3A: Join faction first
            _factionSystem.JoinFaction("warriors_guild");
            _factionSystem.ModifyReputation("warriors_guild", 50);

            // Test 3B: Check access with no minimum
            bool hasAccess = _factionSystem.HasFactionAccess("warriors_guild");

            if (hasAccess)
            {
                UnityEngine.Debug.Log("✓ PASS: Player has access to warriors_guild");
            }
            else
            {
                UnityEngine.Debug.LogError("✗ FAIL: Player denied access to joined faction");
            }

            // Test 3C: Check access with reputation requirement
            bool hasHighAccess = _factionSystem.HasFactionAccess("warriors_guild", minReputation: 75);

            if (!hasHighAccess)
            {
                UnityEngine.Debug.Log("✓ PASS: Access correctly denied for insufficient reputation (50 < 75)");
            }
            else
            {
                UnityEngine.Debug.LogError("✗ FAIL: Access should be denied");
            }

            // Test 3D: Reach reputation requirement
            _factionSystem.ModifyReputation("warriors_guild", 30);
            hasHighAccess = _factionSystem.HasFactionAccess("warriors_guild", minReputation: 75);

            if (hasHighAccess)
            {
                UnityEngine.Debug.Log("✓ PASS: Access granted when reputation requirement met (80 >= 75)");
            }
            else
            {
                UnityEngine.Debug.LogError("✗ FAIL: Access should be granted now");
            }
        }

        /// <summary>Test 4: Rank Thresholds and Branching</summary>
        private void TestRankThresholds()
        {
            UnityEngine.Debug.Log("\n========== TEST 4: RANK THRESHOLDS ==========");

            if (_factionSystem == null) return;

            // Reset the faction
            var standing = _factionSystem.GetStanding("mages_academy");
            standing.reputation = 0;
            standing.UpdateRank();

            // Test each rank threshold
            int[] testRepValues = { -100, -75, -50, -25, -10, 0, 10, 25, 50, 75, 100 };
            FactionStanding.FactionRank[] expectedRanks = 
            {
                FactionStanding.FactionRank.Hated,       // -100
                FactionStanding.FactionRank.Hated,       // -75
                FactionStanding.FactionRank.Hostile,     // -50
                FactionStanding.FactionRank.Hostile,     // -25
                FactionStanding.FactionRank.Unfriendly,  // -10
                FactionStanding.FactionRank.Neutral,     // 0
                FactionStanding.FactionRank.Friendly,    // 10
                FactionStanding.FactionRank.Honored,     // 25
                FactionStanding.FactionRank.Honored,     // 50
                FactionStanding.FactionRank.Revered,     // 75
                FactionStanding.FactionRank.Revered      // 100
            };

            bool allThresholdsPassed = true;

            for (int i = 0; i < testRepValues.Length; i++)
            {
                standing.reputation = testRepValues[i];
                standing.UpdateRank();

                if (standing.currentRank == expectedRanks[i])
                {
                    UnityEngine.Debug.Log($"  ✓ Rep {testRepValues[i]:+0;-0;0} -> {standing.currentRank}");
                }
                else
                {
                    UnityEngine.Debug.LogError($"  ✗ Rep {testRepValues[i]:+0;-0;0} -> {standing.currentRank} (expected {expectedRanks[i]})");
                    allThresholdsPassed = false;
                }
            }

            if (allThresholdsPassed)
            {
                UnityEngine.Debug.Log("\n✓ PASS: All rank thresholds correct!");
            }
            else
            {
                UnityEngine.Debug.LogError("\n✗ FAIL: Some rank thresholds incorrect!");
            }
        }

        /// <summary>Test 5: WorldBridge Event Broadcasting</summary>
        private void TestWorldBridgeEvents()
        {
            UnityEngine.Debug.Log("\n========== TEST 5: WORLDBRIDGE EVENT BROADCASTING ==========");

            if (_worldBridge == null || _factionSystem == null) return;

            // Test 5A: Check if reputation_changed event exists
            if (_worldBridge.HasInvoker("faction_reputation_changed"))
            {
                UnityEngine.Debug.Log("✓ PASS: faction_reputation_changed invoker registered");
            }
            else
            {
                UnityEngine.Debug.LogError("✗ FAIL: faction_reputation_changed not registered");
            }

            // Test 5B: Check if rank_changed event exists
            if (_worldBridge.HasInvoker("faction_rank_changed"))
            {
                UnityEngine.Debug.Log("✓ PASS: faction_rank_changed invoker registered");
            }
            else
            {
                UnityEngine.Debug.LogError("✗ FAIL: faction_rank_changed not registered");
            }

            // Test 5C: Check if joined event exists
            if (_worldBridge.HasInvoker("faction_joined"))
            {
                UnityEngine.Debug.Log("✓ PASS: faction_joined invoker registered");
            }
            else
            {
                UnityEngine.Debug.LogError("✗ FAIL: faction_joined not registered");
            }

            // Test 5D: Trigger an event and listen
            UnityEngine.Debug.Log("\n[TEST] Triggering reputation change (listening for broadcast)...");
            _factionSystem.ModifyReputation("thieves_guild", 10);
            UnityEngine.Debug.Log("[TEST] Event should have broadcast above ↑");
        }

        #endregion

        #region Event Listeners

        private void SetupEventListeners()
        {
            if (_factionSystem == null) return;

            // Listen to reputation changes
            _factionSystem.OnReputationChanged?.AddListener((factionId, oldRep, newRep) =>
            {
                UnityEngine.Debug.Log($"<color=cyan>[EVENT] Reputation Changed: {factionId} ({oldRep} → {newRep})</color>");
            });

            // Listen to rank changes
            _factionSystem.OnRankChanged?.AddListener((factionId, oldRank, newRank) =>
            {
                UnityEngine.Debug.Log($"<color=green>[EVENT] Rank Changed: {factionId} ({oldRank} → {newRank})</color>");
            });

            // Listen to faction joined
            _factionSystem.OnFactionJoined?.AddListener((factionId) =>
            {
                UnityEngine.Debug.Log($"<color=yellow>[EVENT] Faction Joined: {factionId}</color>");
            });

            // Listen to faction left
            _factionSystem.OnFactionLeft?.AddListener((factionId) =>
            {
                UnityEngine.Debug.Log($"<color=red>[EVENT] Faction Left: {factionId}</color>");
            });
        }

        #endregion

        #region Utilities

        private void PrintStatus()
        {
            UnityEngine.Debug.Log("\n========== CURRENT STATUS ==========");

            if (_factionSystem == null)
            {
                UnityEngine.Debug.LogError("FactionSystem not found!");
                return;
            }

            if (_playerData == null)
            {
                UnityEngine.Debug.LogError("PlayerDataContainer not found!");
                return;
            }

            UnityEngine.Debug.Log($"Player: {_playerData.displayName}");
            UnityEngine.Debug.Log($"Factions Joined: {(_playerData.factions.Count > 0 ? string.Join(", ", _playerData.factions) : "None")}");
            UnityEngine.Debug.Log("\nFaction Standings:");

            foreach (var standing in _factionSystem.playerStandings)
            {
                var faction = _factionSystem.allFactions.Find(f => f.factionId == standing.Key);
                string factionName = faction?.displayName ?? standing.Key;
                UnityEngine.Debug.Log($"  {factionName}: Rep={standing.Value.reputation:+0;-0;0} | Rank={standing.Value.currentRank}");
            }
        }

        private void PrintInstructions()
        {
            UnityEngine.Debug.Log(@"
╔════════════════════════════════════════════════════════════════════╗
║         FACTION SYSTEM DEBUG - KEY BINDINGS                       ║
╚════════════════════════════════════════════════════════════════════╝

F1 - Test Reputation Modification
   └─ Tests: reputation changes, rank updates, clamping

F2 - Test Faction Join/Leave
   └─ Tests: joining factions, leaving factions, data persistence

F3 - Test Faction Access
   └─ Tests: access checks, reputation requirements

F4 - Test Rank Thresholds
   └─ Tests: all rank boundaries (Hated, Hostile, Unfriendly, Neutral, 
            Friendly, Honored, Revered)

F5 - Test WorldBridge Events
   └─ Tests: event registration and broadcasting

F6 - Print Current Status
   └─ Displays: player factions and all standings

═══════════════════════════════════════════════════════════════════════
");
        }

        #endregion
    }
}

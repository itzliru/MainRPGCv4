#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using System.IO;
using VaultSystems.Data;

public static class DataContainerExporter
{
    // ✅ Export structure that handles the DeathState enum properly
    [System.Serializable]
    public class NPCExport
    {
        public string Id;
        public string Name;
        public string NPCId;
        public int CurrentHP;
        public int MaxHP;
        // ✅ CHANGED: Now stores enum as string for JSON compatibility
        public string DeathState;  // e.g., "Alive", "Dying", "RagdollActive", "Dead"
        // ✅ ADDED: Backward compatibility
        public bool IsDead;        // Computed from DeathState
        public List<string> Factions;
    }

    [System.Serializable]
    public class PlayerExport
    {
        public string Id;
        public string Name;
        public string PlayerId;
        public int CurrentHP;
        public int MaxHP;
        public string DeathState;  // Also use enum string here
        public int Level;
        public int XP;
        public int outfitIndex = 0;
        public List<string> Factions;
    }

    [System.Serializable]
    public class OtherExport
    {
        public string Id;
        public string Type;
        public Vector3 Position;
        public string LastDirtyReason;
        public string LastHash;
    }

    [System.Serializable]
    public class ExportWrapper
    {
        public List<NPCExport> NPCs = new();
        public List<PlayerExport> Players = new();
        public List<OtherExport> Others = new();
    }

    [MenuItem("Game Tools/Export DataContainers JSON")]
    public static void ExportAllContainers()
    {
        ExportWrapper exportData = new();

        // --- NPCs ---
        var npcs = Object.FindObjectsOfType<NPCDataContainer>(true);
        foreach (var npc in npcs)
        {
            exportData.NPCs.Add(new NPCExport
            {
                Id = npc.GetComponent<UniqueId>()?.GetID() ?? "",
                Name = npc.displayName,
                NPCId = npc.npcId,
                CurrentHP = npc.currentHP,
                MaxHP = npc.maxHP,
                
                // ✅ FIXED: Convert enum to string for JSON
                DeathState = npc.CurrentDeathState.ToString(),
                // ✅ ADDED: Backward compatibility - compute boolean
                IsDead = npc.CurrentDeathState != VaultSystems.Data.DeathState.Alive,
                Factions = npc.factions
            });
        }

        // --- Players ---
        var players = Object.FindObjectsOfType<PlayerDataContainer>(true);
        foreach (var player in players)
        {
            exportData.Players.Add(new PlayerExport
            {
                Id = player.GetComponent<UniqueId>()?.GetID() ?? "",
                Name = player.displayName,
                PlayerId = player.playerId,
                CurrentHP = player.currentHP,
                MaxHP = player.maxHP,
                DeathState = player.CurrentDeathState.ToString(),
                Level = player.level,
                XP = player.xp,
                outfitIndex = player.outfitIndex,
                Factions = player.factions
            });
        }

        // --- Others (AdvancedDataContainers not NPC/Player) ---
        var others = Object.FindObjectsOfType<AdvancedDataContainer>(true)
                           .Where(x => !(x is NPCDataContainer) && !(x is PlayerDataContainer));
        foreach (var other in others)
        {
            exportData.Others.Add(new OtherExport
            {
                Id = other.GetComponent<UniqueId>()?.GetID() ?? "",
                Type = other.GetType().Name,
                Position = other.transform.position,
                LastDirtyReason = other.LastDirtyReason.ToString(),
                LastHash = other.LastHash
            });
        }

        // --- BaseDataContainers that are not AdvancedDataContainers ---
        var baseContainers = Object.FindObjectsOfType<BaseDataContainer>(true)
            .Where(x => !(x is AdvancedDataContainer));
        foreach (var bc in baseContainers)
        {
            exportData.Others.Add(new OtherExport
            {
                Id = bc.GetComponent<UniqueId>()?.GetID() ?? "",
                Type = bc.GetType().Name,
                Position = bc.transform.position,
                LastDirtyReason = "N/A",
                LastHash = "N/A"
            });
        }

        // --- Serialize to JSON ---
        string json = JsonUtility.ToJson(exportData, true);

        // --- Write to file ---
        string path = EditorUtility.SaveFilePanel("Save DataContainers JSON", "", "DataContainers.json", "json");
        if (!string.IsNullOrEmpty(path))
        {
            File.WriteAllText(path, json);
            Debug.Log($"[DataContainerExporter] Exported {npcs.Length} NPCs, {players.Length} Players, {others.Count()} Others to {path}");
        }
    }
}
#endif

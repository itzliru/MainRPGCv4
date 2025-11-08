using System;

namespace VaultSystems.Quests
{
    /// <summary>
    /// Centralized event key constants for the quest system.
    /// Ensures consistency and makes refactoring easier.
    /// </summary>
    public static class EventKeys
    {
        #region === Generic Health/Death Events (Prefix-based) ===

        /// <summary>Suffixes for prefixed events: "{prefix}_hp_changed"</summary>
        public const string HEALTH_CHANGED = "_hp_changed";
        public const string DAMAGED = "_damaged";
        public const string HEALED = "_healed";
        public const string DEATH_STATE_CHANGED = "_death_state_changed";
        public const string DIED = "_died";
        public const string REVIVED = "_revived";

        #endregion

        #region === Player Events (Global) ===

        public static class Player
        {
            public const string SPAWNED = "player_spawned";
            public const string RESTORED = "player_restored";
            public const string LEVEL_UP = "player_levelup";
            public const string SCENE_CHANGED = "player_scene_changed";
            public const string XP_GAINED = "player_xp_gained";

            // Stat events
            public const string MYSTIC_POWER_CHANGED = "player_mystic_power_changed";
            public const string MYSTIC_IMPLANTS_CHANGED = "player_mystic_implants_changed";
            public const string SCROLL_LEVEL_CHANGED = "player_scroll_level_changed";

            // Weapon events
            public const string WEAPON_FIRED = "player_weapon_fired";
            public const string WEAPON_CHANGED = "player_weapon_changed";
            public const string AMMO_CHANGED = "player_ammo_changed";
            public const string RELOADED = "player_reloaded";
        }

        #endregion

        #region === NPC Events (Prefix-based) ===

        public static class NPC
        {
            public const string BEHAVIOR_CHANGED = "_behavior_changed";
            public const string FACTION_CHANGED = "_faction_changed";
            public const string QUEST_INTERACTION = "_quest_interaction";
        }

        #endregion

        #region === Faction Events (Global) ===

        public static class Faction
        {
            public const string REPUTATION_CHANGED = "faction_reputation_changed";
            public const string RANK_CHANGED = "faction_rank_changed";
            public const string JOINED = "faction_joined";
            public const string LEFT = "faction_left";
            public const string RELATIONSHIP_CHANGED = "faction_relationship_changed";
        }

        #endregion

        #region === Weapon Events (Prefix-based) ===

        public static class Weapon
        {
            public const string FIRED = "_weapon_fired";
            public const string CHANGED = "_weapon_changed";
            public const string AMMO_CHANGED = "_ammo_changed";
            public const string RELOADED = "_reloaded";
        }

        #endregion

        #region === Helper Methods for Prefix-Based Keys ===

        /// <summary>Get health changed key for a prefixed entity (e.g., "npc_bandit_01")</summary>
        public static string GetHealthKey(string prefix) => $"{prefix}{HEALTH_CHANGED}";

        /// <summary>Get damaged key for a prefixed entity</summary>
        public static string GetDamagedKey(string prefix) => $"{prefix}{DAMAGED}";

        /// <summary>Get healed key for a prefixed entity</summary>
        public static string GetHealedKey(string prefix) => $"{prefix}{HEALED}";

        /// <summary>Get death state changed key for a prefixed entity</summary>
        public static string GetDeathStateKey(string prefix) => $"{prefix}{DEATH_STATE_CHANGED}";

        /// <summary>Get died key for a prefixed entity</summary>
        public static string GetDiedKey(string prefix) => $"{prefix}{DIED}";

        /// <summary>Get revived key for a prefixed entity</summary>
        public static string GetRevivedKey(string prefix) => $"{prefix}{REVIVED}";

        /// <summary>Get behavior changed key for NPC</summary>
        public static string GetBehaviorChangedKey(string npcPrefix) => $"{npcPrefix}{NPC.BEHAVIOR_CHANGED}";

        /// <summary>Get faction changed key for NPC</summary>
        public static string GetFactionChangedKey(string npcPrefix) => $"{npcPrefix}{NPC.FACTION_CHANGED}";

        /// <summary>Get quest interaction key for NPC</summary>
        public static string GetQuestInteractionKey(string npcPrefix) => $"{npcPrefix}{NPC.QUEST_INTERACTION}";

        /// <summary>Get weapon fired key for entity</summary>
        public static string GetWeaponFiredKey(string prefix) => $"{prefix}{Weapon.FIRED}";

        /// <summary>Get weapon changed key for entity</summary>
        public static string GetWeaponChangedKey(string prefix) => $"{prefix}{Weapon.CHANGED}";

        /// <summary>Get ammo changed key for entity</summary>
        public static string GetAmmoChangedKey(string prefix) => $"{prefix}{Weapon.AMMO_CHANGED}";

        /// <summary>Get reloaded key for entity</summary>
        public static string GetReloadedKey(string prefix) => $"{prefix}{Weapon.RELOADED}";

        #endregion
    }
}

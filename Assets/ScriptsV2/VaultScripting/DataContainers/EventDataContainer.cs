using UnityEngine;
using VaultSystems.Data;
using VaultSystems.Invoker;
using System;
using System.Collections.Generic;
using UnityEngine.Events;

namespace VaultSystems.Containers
{
    /// <summary>
    /// Central repository for all event key constants used throughout the event system.
    /// This eliminates magic strings and makes refactoring easier.
    /// 
    /// Usage:
    ///     EventDataContainer.SubscribeTo(EventKeys.Player.SPAWNED, args => {...});
    ///     string healthKey = EventKeys.GetHealthKey("npc_bandit_01");
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

        #region === Scene/World Events (Global) ===
        /// <summary>
        /// Events related to scene loading, vision, and world state changes.
        /// These are typically broadcast via WorldBridgeSystem.
        /// </summary>
        public static class Scene
        {
            public const string CELL_CHANGED = "scene_cell_changed";
            public const string LOADED = "scene_loaded";
            public const string UNLOADED = "scene_unloaded";
            public const string VISION_BATCH_UPDATE = "scene_vision_batch_update";
            public const string NPC_VISION_SUBSCRIBED = "scene_npc_vision_subscribed";
            public const string NPC_VISION_UNSUBSCRIBED = "scene_npc_vision_unsubscribed";

            public const string THREAT_DETECTED = "scene_threat_detected";
            public const string HOSTILE_NPC_SIGHTED = "scene_hostile_npc_sighted";
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

    public class EventDataContainer
    {
        public enum EventBroadcastMode
        {
            None,           // No broadcasting
            LocalOnly,      // Local events only (C# events)
            WorldBridge,    // Via WorldBridgeSystem only
            Both            // Local + WorldBridge (recommended)
        }

        private readonly BaseDamageableContainer _container;
        private readonly string _eventPrefix;
        private readonly EventBroadcastMode _broadcastMode;

        private int _lastHP;
        private int _lastMaxHP;
        private DeathState _lastDeathState;

        // =========== Local C# Events ===========
        public event Action<int, int> OnHealthChanged;
        public event Action<int, int, int> OnTakeDamage;
        public event Action<int, int, int> OnHeal;
        public event Action<DeathState, DeathState> OnDeathStateChanged;
        public event Action<int> OnDeath;
        public event Action OnRevived;
        public event Action<string> OnPlayerSpawned;
        public event Action<string> OnPlayerRestored;
        public event Action<string> OnSceneChanged;
        public event Action<int> OnLevelUp;
        public event Action<int> OnXPGained;

        // =========== Stat Change Events ===========
        public event Action<string, int, int> OnStatChanged; // statName, oldValue, newValue
        public event Action<string, float, float> OnDerivedStatChanged; // statName, oldValue, newValue

        // =========== Constructor ===========
        public EventDataContainer(
                BaseDamageableContainer container,
                string eventPrefix = null,
                EventBroadcastMode broadcastMode = EventBroadcastMode.Both)
        {
            if (container == null)
                throw new ArgumentNullException(nameof(container));

            _container = container;

            // Generate unique prefix if not provided
            if (string.IsNullOrEmpty(eventPrefix))
            {
                var npc = container as NPCDataContainer;
                var player = container as PlayerDataContainer;

                if (npc != null)
                    eventPrefix = $"npc_{npc.displayName.ToLower().Replace(' ', '_')}";
                else if (player != null)
                    eventPrefix = $"player_{player.displayName.ToLower().Replace(' ', '_')}";
                else
                    eventPrefix = $"{container.GetType().Name.ToLower()}_{container.GetInstanceID()}";
            }

            _eventPrefix = eventPrefix;
            _broadcastMode = broadcastMode;

            // Cache initial state
            _lastHP = container.CurrentHP;
            _lastMaxHP = container.MaxHP;
            _lastDeathState = container.CurrentDeathState;

            Debug.Log($"[EventDataContainer] Created wrapper for '{_eventPrefix}' (mode: {broadcastMode})");
        }

        /// <summary>
        /// Call this every frame or when you need to check for changes.
        /// Automatically fires events if values changed since last call.
        /// </summary>
        public void UpdateEvents()
        {
            if (_container == null) return;

            CheckHealthChanged();
            CheckDeathStateChanged();
        }

        /// <summary>
        /// Manually trigger a health change event (useful for forced updates)
        /// </summary>
        public void ForceHealthUpdate()
        {
            _lastHP = -1; // Force mismatch on next UpdateEvents()
            UpdateEvents();
        }

        /// <summary>
        /// Manually trigger a death state change event
        /// </summary>
        public void ForceDeathStateUpdate()
        {
            _lastDeathState = (DeathState)(-1); // Force mismatch
            UpdateEvents();
        }

        private void CheckHealthChanged()
        {
            int currentHP = _container.CurrentHP;
            int maxHP = _container.MaxHP;

            if (currentHP != _lastHP || maxHP != _lastMaxHP)
            {
                int delta = currentHP - _lastHP;

                if (delta > 0)
                {
                    // Healing
                    OnHeal?.Invoke(delta, currentHP, maxHP);
                    BroadcastEvent($"{_eventPrefix}_healed", new object[] { delta, currentHP, maxHP });
                }
                else if (delta < 0)
                {
                    // Taking damage
                    OnTakeDamage?.Invoke(-delta, currentHP, maxHP);
                    BroadcastEvent($"{_eventPrefix}_damaged", new object[] { -delta, currentHP, maxHP });
                }

                OnHealthChanged?.Invoke(currentHP, maxHP);
                BroadcastEvent($"{_eventPrefix}_hp_changed", new object[] { currentHP, maxHP });

                _lastHP = currentHP;
                _lastMaxHP = maxHP;
            }
        }



        private void CheckDeathStateChanged()
        {
            DeathState currentState = _container.CurrentDeathState;

            if (currentState != _lastDeathState)
            {
                OnDeathStateChanged?.Invoke(currentState, _lastDeathState);
                BroadcastEvent(
                    EventKeys.GetDeathStateKey(_eventPrefix),
                    new object[] { currentState.ToString(), _lastDeathState.ToString() }
                );

                if (currentState != DeathState.Alive && _lastDeathState == DeathState.Alive)
                {
                    OnDeath?.Invoke(_container.CurrentHP);
                    BroadcastEvent(EventKeys.GetDiedKey(_eventPrefix), new object[] { _container.CurrentHP, currentState.ToString() });
                }
                else if (currentState == DeathState.Alive && _lastDeathState != DeathState.Alive)
                {
                    OnRevived?.Invoke();
                    BroadcastEvent(EventKeys.GetRevivedKey(_eventPrefix), new object[] { _container.CurrentHP });
                }

                _lastDeathState = currentState;

                                if (_container != null)
                {
                    _container.MarkDirty();
                }

            }
        }

        /// <summary>Register all event keys this container will broadcast</summary>
        public virtual void RegisterWithWorldBridge()
        {
            if (WorldBridgeSystem.Instance == null)
            {
                Debug.LogWarning($"[EventDataContainer] WorldBridgeSystem not initialized!");
                return;
            }

            var bridge = WorldBridgeSystem.Instance;
            var invoker = DynamicDictionaryInvoker.Instance;

            if (invoker == null)
            {
                Debug.LogWarning($"[EventDataContainer] DynamicDictionaryInvoker not found!");
                return;
            }

            // Register all event keys using constants
            string[] eventKeys = new[]
            {
        EventKeys.GetHealthKey(_eventPrefix),
        EventKeys.GetDamagedKey(_eventPrefix),
        EventKeys.GetHealedKey(_eventPrefix),
        EventKeys.GetDeathStateKey(_eventPrefix),
        EventKeys.GetDiedKey(_eventPrefix),
        EventKeys.GetRevivedKey(_eventPrefix)
    };

            foreach (var key in eventKeys)
            {
                if (!bridge.HasInvoker(key))
                {
                    bridge.RegisterInvoker(key, (args) => { }, DynamicDictionaryInvoker.Layer.Overlay);
                }
            }

            Debug.Log($"[EventDataContainer] Registered '{_eventPrefix}' with {eventKeys.Length} broadcast events");
        }

        /// <summary>Broadcast a stat change event</summary>
        public void BroadcastStatChanged(string statName, int oldValue, int newValue)
        {
            OnStatChanged?.Invoke(statName, oldValue, newValue);
            BroadcastEvent($"{_eventPrefix}_stat_changed", new object[] { statName, oldValue, newValue });
        }

        /// <summary>Broadcast a derived stat change event</summary>
        public void BroadcastDerivedStatChanged(string statName, float oldValue, float newValue)
        {
            OnDerivedStatChanged?.Invoke(statName, oldValue, newValue);
            BroadcastEvent($"{_eventPrefix}_derived_stat_changed", new object[] { statName, oldValue, newValue });
        }


        /// <summary>
        /// Subscribe to this container's events via WorldBridgeSystem.
        /// Example: EventDataContainer.SubscribeTo("npc_bandit_01_hp_changed", (args) => {...})
        /// </summary>
        public static IDisposable SubscribeTo(
            string eventKey,
            Action<object[]> callback,
            DynamicDictionaryInvoker.Layer layer = DynamicDictionaryInvoker.Layer.Overlay)
        {
            var bridge = WorldBridgeSystem.Instance;
            if (bridge != null)
            {
                return bridge.RegisterInvoker(eventKey, callback, layer);
            }
            return null;
        }
/// <summary>
/// Broadcast an event through the WorldBridge system based on the current broadcast mode
/// </summary>
protected void BroadcastEvent(string eventKey, object[] args)
{
    if (_broadcastMode == EventBroadcastMode.None)
        return;

    if ((_broadcastMode == EventBroadcastMode.WorldBridge || _broadcastMode == EventBroadcastMode.Both))
    {
        var bridge = WorldBridgeSystem.Instance;
        if (bridge != null)
        {
            bridge.InvokeKey(eventKey, args);
        }
    }
}


        // =========== Getters ===========
        public string EventPrefix => _eventPrefix;
        public BaseDamageableContainer Container => _container;
        public int LastHP => _lastHP;
        public int LastMaxHP => _lastMaxHP;
        public DeathState LastDeathState => _lastDeathState;
    }
    public class PlayerEventDataContainer : EventDataContainer
    {
        private PlayerDataContainer _playerData;

        public PlayerEventDataContainer(PlayerDataContainer player, string prefix = null)
            : base(player, prefix, EventBroadcastMode.Both)
        {
            _playerData = player;
        }

        public void BroadcastSpawned(string spawnId)
        {
            BroadcastEvent(EventKeys.Player.SPAWNED, new object[] { spawnId });
            _playerData?.MarkDirty();
        }

        public void BroadcastLevelUp(int newLevel)
        {
            BroadcastEvent(EventKeys.Player.LEVEL_UP, new object[] { newLevel });
            _playerData?.MarkDirty();
        }

        public void BroadcastRestored(string scene)
        {
            BroadcastEvent(EventKeys.Player.RESTORED, new object[] { scene });
            _playerData?.MarkDirty();
        }

        public void BroadcastSceneChanged(string newScene)
        {
            BroadcastEvent(EventKeys.Player.SCENE_CHANGED, new object[] { newScene });
            _playerData?.MarkDirty();
        }

        public void BroadcastXPGained(int xpAmount)
        {
            BroadcastEvent(EventKeys.Player.XP_GAINED, new object[] { xpAmount });
            _playerData?.MarkDirty();
        }

        public void BroadcastMysticPowerChanged(int oldValue, int newValue)
        {
            BroadcastEvent(EventKeys.Player.MYSTIC_POWER_CHANGED, new object[] { oldValue, newValue });
            _playerData?.MarkDirty();
        }

        public void BroadcastMysticImplantsChanged(int oldValue, int newValue)
        {
            BroadcastEvent(EventKeys.Player.MYSTIC_IMPLANTS_CHANGED, new object[] { oldValue, newValue });
            _playerData?.MarkDirty();
        }

        public void BroadcastScrollLevelChanged(int oldValue, int newValue)
        {
            BroadcastEvent(EventKeys.Player.SCROLL_LEVEL_CHANGED, new object[] { oldValue, newValue });
            _playerData?.MarkDirty();
        }

        public override void RegisterWithWorldBridge()
        {
            base.RegisterWithWorldBridge();

            if (WorldBridgeSystem.Instance == null) return;

            var bridge = WorldBridgeSystem.Instance;
            string[] playerEventKeys = new[]
            {
            EventKeys.Player.SPAWNED,
            EventKeys.Player.LEVEL_UP,
            EventKeys.Player.RESTORED,
            EventKeys.Player.SCENE_CHANGED,
            EventKeys.Player.XP_GAINED,
            EventKeys.Player.MYSTIC_POWER_CHANGED,
            EventKeys.Player.MYSTIC_IMPLANTS_CHANGED,
            EventKeys.Player.SCROLL_LEVEL_CHANGED
        };

            foreach (var key in playerEventKeys)
            {
                if (!bridge.HasInvoker(key))
                {
                    bridge.RegisterInvoker(key, (args) => { }, DynamicDictionaryInvoker.Layer.Overlay);
                }
            }
        }
    }

    public class NPCEventDataContainer : EventDataContainer
    {
        private NPCDataContainer _npcData;

        public NPCEventDataContainer(NPCDataContainer npc, string prefix = null)
            : base(npc, prefix, EventBroadcastMode.Both)
        {
            _npcData = npc;
        }

        public void BroadcastBehaviorChanged(string newBehavior)
        {
            BroadcastEvent(EventKeys.GetBehaviorChangedKey(EventPrefix), new object[] { newBehavior });
            _npcData?.MarkDirty();
        }

        public void BroadcastFactionChanged(string newFaction)
        {
            BroadcastEvent(EventKeys.GetFactionChangedKey(EventPrefix), new object[] { newFaction });
            _npcData?.MarkDirty();
        }

        public void BroadcastQuestInteraction(string questId, string action)
        {
            BroadcastEvent(EventKeys.GetQuestInteractionKey(EventPrefix), new object[] { questId, action });
            _npcData?.MarkDirty();
        }

        public override void RegisterWithWorldBridge()
        {
            base.RegisterWithWorldBridge();

            if (WorldBridgeSystem.Instance == null) return;

            var bridge = WorldBridgeSystem.Instance;
            string[] npcEventKeys = new[]
            {
            EventKeys.GetBehaviorChangedKey(EventPrefix),
            EventKeys.GetFactionChangedKey(EventPrefix),
            EventKeys.GetQuestInteractionKey(EventPrefix)
        };

            foreach (var key in npcEventKeys)
            {
                if (!bridge.HasInvoker(key))
                {
                    bridge.RegisterInvoker(key, (args) => { }, DynamicDictionaryInvoker.Layer.Overlay);
                }
            }
        }
    }

    public class PlayerWeaponEventDataContainer : EventDataContainer
    {
        private PlayerDataContainer _playerData;
        private GunController _gunController;
        private int _lastAmmo;
        private int _lastReserveAmmo;
        private string _lastEquippedWeapon;

        public PlayerWeaponEventDataContainer(
            PlayerDataContainer player,
            GunController gunController,
            string prefix = null)
            : base(player, prefix, EventBroadcastMode.Both)
        {
            _playerData = player;
            _gunController = gunController;

            // ✅ FIX: Use actual GunController properties
            _lastAmmo = gunController?.AmmoInMagazine ?? 0;
            _lastReserveAmmo = gunController?.AmmoInReserve ?? 0;
            _lastEquippedWeapon = gunController?.CurrentGun?.GunName ?? "None";

            // ✅ NEW: Wire up GunController events
            if (gunController != null)
            {
                gunController.OnAmmoChanged += OnAmmoChanged;
                gunController.OnReserveAmmoChanged += OnReserveAmmoChanged;
                gunController.OnReloadCompleted += OnReloadCompleted;
            }

            Debug.Log("[PlayerWeaponEventDataContainer] Weapon event handler initialized");
        }

        // ✅ NEW: Direct event handlers from GunController
        private void OnAmmoChanged(int ammoCount)
        {
            _lastAmmo = ammoCount;
            BroadcastEvent(
                EventKeys.GetAmmoChangedKey(EventPrefix),
                new object[] { ammoCount, _gunController?.CurrentGun?.GunName ?? "None" }
            );
            _playerData?.MarkDirty();
        }

        private void OnReserveAmmoChanged(int reserveCount)
        {
            _lastReserveAmmo = reserveCount;
            BroadcastEvent(
                EventKeys.GetAmmoChangedKey(EventPrefix),
                new object[] { _gunController?.AmmoInMagazine ?? 0, reserveCount }
            );
            _playerData?.MarkDirty();
        }

        private void OnReloadCompleted()
        {
            BroadcastReload(
                _gunController?.CurrentGun?.GunName ?? "Unknown",
                _gunController?.AmmoInMagazine ?? 0
            );
        }

        // ✅ FIXED: Use actual GunController properties
        public void UpdateWeaponEvents()
        {
            if (_gunController == null) return;

            int currentAmmo = _gunController.AmmoInMagazine;
            string currentWeapon = _gunController.CurrentGun?.GunName ?? "None";

            if (currentAmmo != _lastAmmo)
            {
                int ammoDelta = currentAmmo - _lastAmmo;
                BroadcastEvent(
                    EventKeys.GetAmmoChangedKey(EventPrefix),
                    new object[] { currentAmmo, ammoDelta }
                );
                _lastAmmo = currentAmmo;
                _playerData?.MarkDirty();
            }

            if (currentWeapon != _lastEquippedWeapon)
            {
                BroadcastEvent(
                    EventKeys.GetWeaponChangedKey(EventPrefix),
                    new object[] { currentWeapon }
                );
                _lastEquippedWeapon = currentWeapon;
                _playerData?.MarkDirty();
            }
        }

        public void BroadcastWeaponFired(string weaponName, int ammoUsed)
        {
            BroadcastEvent(
                EventKeys.GetWeaponFiredKey(EventPrefix),
                new object[] { weaponName, ammoUsed }
            );
            _playerData?.MarkDirty();
        }

        public void BroadcastReload(string weaponName, int ammoRestored)
        {
            BroadcastEvent(
                EventKeys.GetReloadedKey(EventPrefix),
                new object[] { weaponName, ammoRestored }
            );
            _playerData?.MarkDirty();
        }

        public override void RegisterWithWorldBridge()
        {
            base.RegisterWithWorldBridge();

            if (WorldBridgeSystem.Instance == null) return;

            var bridge = WorldBridgeSystem.Instance;
            string[] weaponEventKeys = new[]
            {
            EventKeys.GetAmmoChangedKey(EventPrefix),
            EventKeys.GetWeaponChangedKey(EventPrefix),
            EventKeys.GetWeaponFiredKey(EventPrefix),
            EventKeys.GetReloadedKey(EventPrefix)
        };

            foreach (var key in weaponEventKeys)
            {
                if (!bridge.HasInvoker(key))
                {
                    bridge.RegisterInvoker(key, (args) => { }, DynamicDictionaryInvoker.Layer.Overlay);
                }
            }
        }

        // ✅ NEW: Cleanup to prevent memory leaks
        public void Cleanup()
        {
            if (_gunController != null)
            {
                _gunController.OnAmmoChanged -= OnAmmoChanged;
                _gunController.OnReserveAmmoChanged -= OnReserveAmmoChanged;
                _gunController.OnReloadCompleted -= OnReloadCompleted;
            }
        }
    }
}
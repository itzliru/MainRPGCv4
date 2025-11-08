using UnityEngine.Events;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using VaultSystems.Data;
using VaultSystems.Invoker;
using VaultSystems.Controllers;
using VaultSystems.Containers;
namespace VaultSystems.Data
{
    [Serializable]
    public class PlayerDataContainer : BaseDamageableContainer, IResettablePlayer
    {
        #region === Identity ===
        
        [Header("Identity")]
        public string playerId;
        public string displayName;
        public int outfitIndex = 0;
        
        #endregion

        #region === Stats ===
        
        [Header("Stats")]
        [SerializeField] private int _xp = 0;
        [SerializeField] private int _level = 1;

        [Header("Attributes")]
        [SerializeField] private int _agility = 10;
        [SerializeField] private int _strength = 10;
        [SerializeField] private int _wepSkill = 10;
        [SerializeField] private int _mysticPower = 0;
        [SerializeField] private int _mysticImplants = 0;
        [SerializeField] private int _scrollLevel = 1;
        
        #endregion

        #region === Quest Tracking ===
        
        [Header("Questline Tracking")]
        public string mainQuestLine;
        public int mainQuestStage = 0;
        public List<string> completedSubquests = new List<string>();
        
        #endregion

        #region === World State ===
        
        [Header("World Interaction")]
        public bool isActivePlayer = false;
        public virtual Vector3 lastKnownPosition { get; set; } = Vector3.zero;
        public virtual string lastScene { get; set; } = string.Empty;
        public virtual string lastCellId { get; set; } = "Overworld";
        
        #endregion

        #region === Factions ===
        
        [Header("Affiliations")]
        public List<string> factions = new List<string>();
        
        #endregion

        #region === Components ===
        
        private UniqueId uniqueIdRef;
        
        #endregion

        #region === Events ===
        public virtual string lastSpawnPointId { get; set; } = "player_default";
        public virtual string lastSpawnPointScene { get; set; } = "Overworld";
        public event EventHandler<PlayerDataContainer> OnStatsChanged;

        private PlayerEventDataContainer _eventDataContainer;

        protected void RaiseStatsChanged()
        {
            OnStatsChanged?.Invoke(this, this);
            MarkDirty(DirtyReason.ValueChanged);
        }

        /// <summary>Initialize event data container for stat broadcasting</summary>
        public void InitializeEventDataContainer(PlayerEventDataContainer eventContainer)
        {
            _eventDataContainer = eventContainer;
        }
        
        #endregion

        #region === Properties with Notify ===
        
        public int xp
        {
            get => _xp;
            set
            {
                if (_xp == value) return;
                _xp = Mathf.Max(0, value);
                RaiseStatsChanged();
            }
        }

        public int level
        {
            get => _level;
            set
            {
                if (_level == value) return;
                _level = Mathf.Max(1, value);
                RaiseStatsChanged();
            }
        }

        public int agility
        {
            get => _agility;
            set
            {
                if (_agility == value) return;
                _agility = Mathf.Max(0, value);
                RaiseStatsChanged();
            }
        }

        public int strength
        {
            get => _strength;
            set
            {
                if (_strength == value) return;
                _strength = Mathf.Max(0, value);
                RaiseStatsChanged();
            }
        }

        public int wepSkill
        {
            get => _wepSkill;
            set
            {
                if (_wepSkill == value) return;
                _wepSkill = Mathf.Clamp(value, 0, 100);
                RaiseStatsChanged();
            }
        }

        public int mysticPower
        {
            get => _mysticPower;
            set
            {
                if (_mysticPower == value) return;
                int oldValue = _mysticPower;
                _mysticPower = Mathf.Max(0, value);
                RaiseStatsChanged();
                _eventDataContainer?.BroadcastMysticPowerChanged(oldValue, _mysticPower);
            }
        }

        public int mysticImplants
        {
            get => _mysticImplants;
            set
            {
                if (_mysticImplants == value) return;
                int oldValue = _mysticImplants;
                _mysticImplants = Mathf.Max(0, value);
                RaiseStatsChanged();
                _eventDataContainer?.BroadcastMysticImplantsChanged(oldValue, _mysticImplants);
            }
        }

        public int scrollLevel
        {
            get => _scrollLevel;
            set
            {
                if (_scrollLevel == value) return;
                int oldValue = _scrollLevel;
                _scrollLevel = Mathf.Max(1, value);
                RaiseStatsChanged();
                _eventDataContainer?.BroadcastScrollLevelChanged(oldValue, _scrollLevel);
            }
        }

        // ✅ Use property from base for health
        public int currentHP
        {
            get => _currentHP;
            set
            {
                int newHP = Mathf.Clamp(value, 0, _maxHP);
                if (_currentHP == newHP) return;

                _currentHP = newHP;
                if (_currentHP <= 0 && _deathState == DeathState.Alive)
                {
                    OnHealthDepleted();
                }
                RaiseStatsChanged();
            }
        }

        public int maxHP
        {
            get => _maxHP;
            set
            {
                if (_maxHP == value) return;
                _maxHP = Mathf.Max(1, value);
                currentHP = Mathf.Min(currentHP, _maxHP);
                RaiseStatsChanged();
            }
        }
        
        #endregion
private void OnEnable()
{
    OnDeathStateChanged += HandlePlayerDeathStateChange;
}

private void OnDisable()
{
    OnDeathStateChanged -= HandlePlayerDeathStateChange;
}

private void HandlePlayerDeathStateChange(DeathState newState)
{
    // Handle player-specific death UI, etc.
}

        #region === Prefab Registry ===
        
        public Dictionary<string, PlayerDataContainer> prefabLookup { get; private set; } = new();

        protected void InitializePrefabLookup(params PlayerDataContainer[] prefabs)
        {
            prefabLookup.Clear();
            foreach (var p in prefabs)
            {
                if (p == null) continue;
                prefabLookup[p.displayName] = p;
            }
        }
        
        #endregion

        #region === Unity Lifecycle ===
        
        private void Awake()
        {
            uniqueIdRef = GetComponent<UniqueId>();
            if (uniqueIdRef != null)
                playerId = uniqueIdRef.GetID();
            else if (string.IsNullOrEmpty(playerId))
                playerId = Guid.NewGuid().ToString();

            ragdollController = GetComponent<RagdollController>();  // ✅ NEW
            DataContainerManager.Instance?.Register(this);
        }

        private void OnDestroy()
        {
            DataContainerManager.Instance?.Unregister(this);
        }
        
        #endregion

        #region === Methods ===
        
        // ✅ Override base TakeDamage to trigger events
        public override void TakeDamage(int amount)
        {
            base.TakeDamage(amount);
            RaiseStatsChanged();
        }

        public override void Heal(int amount)
        {
            base.Heal(amount);
            RaiseStatsChanged();
        }

        public override void Revive()
        {
            base.Revive();
            RaiseStatsChanged();
        }

        public virtual void LevelUp()
        {
            level += 1;
            maxHP += 10;
            currentHP = maxHP;

            // Broadcast level up event
            WorldBridgeSystem.Instance?.InvokeKey(EventKeys.Player.LEVEL_UP, level);
        }

        public virtual void AddXP(int amount)
        {
            if (amount <= 0) return;
            int oldXP = xp;
            xp += amount;

            // Broadcast XP gained event
            WorldBridgeSystem.Instance?.InvokeKey(EventKeys.Player.XP_GAINED, amount);
        }

        public virtual void IncreaseAttribute(string attribute, int amount)
        {
            if (amount <= 0) return;

            switch (attribute.ToLower())
            {
                case "agility":
                    agility += amount;
                    break;
                case "strength":
                    strength += amount;
                    break;
                case "wepskill":
                    wepSkill += amount;
                    break;
                default:
                    Debug.LogWarning($"[PlayerDataContainer] Unknown attribute '{attribute}'");
                    break;
            }
        }

        public void AddFaction(string faction)
        {
            if (string.IsNullOrEmpty(faction) || factions.Contains(faction)) return;
            factions.Add(faction);
            RaiseStatsChanged();

            // Broadcast faction joined event
            WorldBridgeSystem.Instance?.InvokeKey(EventKeys.GetFactionChangedKey(playerId), faction);
        }

        public void RemoveFaction(string faction)
        {
            if (string.IsNullOrEmpty(faction) || !factions.Contains(faction)) return;
            factions.Remove(faction);
            RaiseStatsChanged();

            // Broadcast faction left event
            WorldBridgeSystem.Instance?.InvokeKey(EventKeys.GetFactionChangedKey(playerId), faction);
        }

        public bool IsInFaction(string faction)
        {
            return factions.Contains(faction);
        }

        protected override void OnDeathSequenceTriggered()
        {
            base.OnDeathSequenceTriggered();
            Debug.Log($"[PlayerDataContainer] {displayName} death sequence triggered");
            // Add player-specific death logic here (UI, sound, etc.)
        }
        
        #endregion

        #region === Initialization ===
        
        public virtual void InitializeDefaults()
        {
            playerId = DefaultPlayerId;
            displayName = DefaultDisplayName;
            outfitIndex = 0;

            _maxHP = 100;
            _currentHP = 100;
            _xp = 0;
            _level = 1;
           CurrentDeathState = DeathState.Alive; 


            _agility = 10;
            _strength = 10;
            _wepSkill = 10;
            _mysticPower = 0;
            _mysticImplants = 0;
            _scrollLevel = 1;

            mainQuestLine = DefaultQuestLine;
            mainQuestStage = 0;
            completedSubquests.Clear();

            lastKnownPosition = DefaultPosition;
            lastCellId = DefaultCellId;
            isActivePlayer = true;
            factions.Clear();

            var uid = GetComponent<UniqueId>() ?? gameObject.AddComponent<UniqueId>();
            uid.isDataContainer = true;
            uid.manualId = playerId;

            DataContainerManager.Instance?.Register(this);
            WorldBridgeSystem.Instance?.RegisterID(uid.GetID(), gameObject);

            Debug.Log($"[{displayName}] Initialized with manual ID: {uid.GetID()}");
            RaiseStatsChanged();
        }

        public virtual void ResetCharacter()
        {
            InitializeDefaults();
            MarkDirty();
        }

        public void UpdateQuestProgress(string questId, bool isSubquest = false)
        {
            if (isSubquest && !completedSubquests.Contains(questId))
                completedSubquests.Add(questId);
            else
                mainQuestStage++;

            MarkDirty();
            RaiseStatsChanged();
        }
        
        #endregion

        #region === Defaults ===
        
        public virtual string DefaultPlayerId => uniqueIdRef != null ? uniqueIdRef.GetID() : Guid.NewGuid().ToString();
        public virtual string DefaultDisplayName => "Player";
        public virtual string DefaultQuestLine => "QLA";
        public virtual Vector3 DefaultPosition => Vector3.zero;
        public virtual string DefaultCellId => "Overworld";
        
        #endregion
    }
}
   

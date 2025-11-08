using System;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using VaultSystems.Data;
using VaultSystems.Invoker;
using VaultSystems.Controllers;
using VaultSystems.Quests;
namespace VaultSystems.Data
{
    /// <summary>
    /// Death state enum - represents lifecycle of NPC from alive to despawned.
    /// </summary>
 
    [Serializable]
    [DisallowMultipleComponent]
    public class NPCDataContainer : BaseDamageableContainer, IDamageable
    {
        #region === Death State System ===
        
        [Header("Death State")]
        [SerializeField] private DeathState _deathState = DeathState.Alive;
        
        public DeathState CurrentDeathState
        {
            get => _deathState;
            private set
            {
                if (_deathState == value) return;
                _deathState = value;
                MarkDirty(DirtyReason.ValueChanged);
                OnDeathStateChanged?.Invoke(_deathState);  // 🔔 Event-driven notification
            }
        }

        /// <summary>
        /// Determines if NPC is not in Alive state.
        /// ✅ Use this instead of the old isDead boolean
        /// </summary>
        public bool IsDead => _deathState != DeathState.Alive;

        /// <summary>
        /// Event fired whenever death state changes.
        /// Subscribe to this in behavior components (Combat, Ragdoll, AI, etc.)
        /// </summary>
        public event System.Action<DeathState> OnDeathStateChanged;

        #endregion

        #region === Ragdoll Physics ===
        
        /// <summary>
        /// Reference to the RagdollController component for managing ragdoll physics.
        /// </summary>
        private RagdollController ragdollController;
        
        #endregion

        #region === Identity ===
        
        [Header("Identity")]
        public string npcId;
        public string displayName;
        
        #endregion

        #region === Stats ===
        
        [Header("Stats")]
       // public int currentHP = 100;
       // public int maxHP = 100;
        public int level = 1;
        
        #endregion

        #region === Weapon Runtime ===
        
        [Header("Weapon Runtime")]
        public WeaponData equippedWeaponData; // The actual WeaponData assigned
        public int currentAmmo;               // Tracks ammo per NPC
        
        #endregion

        #region === Dialogue / Quest ===
        
        [Header("Dialogue / Quest")]
        public bool hasSpokenToPlayer = false;
        public int dialogueStage = 0;
        public string lastDialogueKey;
        
        #endregion

        #region === Quest Flags ===
        
        [Header("Quest Flags")]
        public bool isQuestGiver = false;
        public bool questAccepted = false;
        public bool questCompleted = false;
        
        #endregion

        #region === Faction / Behavior ===

        [Header("Faction / Behavior")]
        [Tooltip("Overall disposition toward the player. -100 = Hostile, 0 = Neutral, +100 = Friendly.")]
        [Range(-100f, 100f)]
        public float disposition = 0f;

        public List<string> factions = new List<string>();
        public bool isEnemy;
        public bool isNeutral;
        public bool isFriendly;

        [Header("Faction Integration")]
        [Tooltip("Weight of faction relationships vs global disposition (0-1). 1 = faction relationships dominate.")]
        [Range(0f, 1f)]
        public float factionInfluence = 0.7f;
        
        #endregion

        #region === Misc ===
        
        [Header("Misc")]
        public Vector3 lastKnownPosition;
        public string customTag;
        
        #endregion

        #region === Component References ===
        
        private UniqueId uniqueIdRef;
        private NPCCombatController combatController;
        
        #endregion

        #region === Unity Lifecycle ===
        
        private void Awake()
        {
            uniqueIdRef = GetComponent<UniqueId>();
            combatController = GetComponent<NPCCombatController>();
            ragdollController = GetComponent<RagdollController>();

            // Ensure npcId is set
            if (uniqueIdRef != null)
                npcId = uniqueIdRef.GetID();
            else if (string.IsNullOrEmpty(npcId))
                npcId = Guid.NewGuid().ToString();

            // Update combat state based on disposition and register to manager
            UpdateDispositionFlags();
            DataContainerManager.Instance?.Register(this);
        }

        private void OnDestroy()
        {
            DataContainerManager.Instance?.Unregister(this);
        }
        
        #endregion

        #region === Weapon Management ===
        
        /// <summary>
        /// Runtime method for equipping weapons. Called by NPCCombatController.
        /// </summary>
        public void EquipWeaponRuntime(WeaponData weapon)
        {
            if (equippedWeaponData == weapon)
            {
                // Same weapon — do not reset ammo
                
                return;
            }

            equippedWeaponData = weapon;
            currentAmmo = weapon.ammo; // Only assign full ammo if new weapon
        }

        /// <summary>
        /// Replenish ammo for the equipped weapon.
        /// </summary>
        public void ReplenishAmmo(int amount)
        {
            if (equippedWeaponData == null) return;
            currentAmmo = Mathf.Clamp(currentAmmo + amount, 0, equippedWeaponData.ammo);
            MarkDirty(DirtyReason.ValueChanged);
        }
        
        #endregion

        #region === Health Management ===
        
        /// <summary>
        /// Writable current HP property (clamped to 0-maxHP).
        /// ✅ Use this to get/set NPC health
        /// </summary>
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
                    TransitionToRagdoll();
                }
                MarkDirty(DirtyReason.ValueChanged);
            }
        }

        /// <summary>
        /// Writable max HP property.
        /// ✅ Use this to get/set NPC max health
        /// </summary>
        public int maxHP
        {
            get => _maxHP;
            set
            {
                if (_maxHP == value) return;
                _maxHP = Mathf.Max(1, value);
                currentHP = Mathf.Min(currentHP, _maxHP);
                MarkDirty(DirtyReason.ValueChanged);
            }
        }

        /// <summary>
        /// Set HP directly (clamped to 0-maxHP).
        /// Triggers death if HP reaches 0.
        /// </summary>
        public void SetHP(int newHP)
        {
            currentHP = Mathf.Clamp(newHP, 0, maxHP);
            if (currentHP <= 0 && _deathState == DeathState.Alive)
            {
                TransitionToRagdoll();
            }
            MarkDirty(DirtyReason.ValueChanged);
        }

        /// <summary>
        /// Apply damage to NPC. Triggers death sequence if HP reaches 0.
        /// IDamageable implementation.
        /// </summary>
        public void TakeDamage(int amount)
        {
            if (amount <= 0 || IsDead) 
                return;

            currentHP -= amount;

            if (currentHP <= 0 && _deathState == DeathState.Alive)
            {
                TransitionToRagdoll();
            }

            MarkDirty(DirtyReason.ValueChanged);
        }
        
        #endregion

        #region === Death / Ragdoll Sequence ===
        
        /// <summary>
        /// Initiates the death sequence: Dying → RagdollActive → Dead
        /// </summary>
        private void TransitionToRagdoll()
        {
            CurrentDeathState = DeathState.Dying;

            if (ragdollController == null)
                ragdollController = GetComponent<RagdollController>();

            if (ragdollController != null)
            {
                ragdollController.EnablePhysics();
                CurrentDeathState = DeathState.RagdollActive;
                Debug.Log($"[NPCDataContainer] {displayName} entered ragdoll state");
                OnDeath();
            }
            else
            {
                Debug.LogWarning($"[NPCDataContainer] No RagdollController found on {displayName}");
                CurrentDeathState = DeathState.Dead;  // Fallback
            }
        }

        /// <summary>
        /// Public method to enable ragdoll (for external triggers).
        /// </summary>
        public void EnableRagdoll()
        {
            if (ragdollController == null)
                ragdollController = GetComponent<RagdollController>();

            if (ragdollController != null)
            {
                ragdollController.EnablePhysics();
                CurrentDeathState = DeathState.RagdollActive;
                Debug.Log($"[NPCDataContainer] {displayName} ragdoll enabled");
            }
            else
            {
                Debug.LogWarning($"[NPCDataContainer] No RagdollController found on {displayName}");
                CurrentDeathState = DeathState.Dead;
            }
        }

        /// <summary>
        /// Disable ragdoll and mark as dead.
        /// </summary>
        public void DisableRagdoll()
        {
            if (ragdollController != null)
            {
                ragdollController.DisablePhysics();
            }
            CurrentDeathState = DeathState.Dead;
        }

        /// <summary>
        /// Called when NPC dies. Override in subclasses for custom death behavior.
        /// </summary>
        protected void OnDeath()
        {
            Debug.Log($"[NPCDataContainer] NPC {npcId} ({displayName}) has died.");
            UpdateCombatStateFromData();
        }
        
        #endregion

        #region === Dialogue / Quest Updates ===
        
        public void OnDialogueProgress(string dialogueKey)
        {
            hasSpokenToPlayer = true;
            lastDialogueKey = dialogueKey;
            dialogueStage++;
            MarkDirty(DirtyReason.ValueChanged);
        }

        public virtual void OnQuestStateChanged(bool accepted, bool completed)
        {
            questAccepted = accepted;
            questCompleted = completed;
            MarkDirty(DirtyReason.ValueChanged);

            // Broadcast quest interaction event
            WorldBridgeSystem.Instance?.InvokeKey(EventKeys.GetQuestInteractionKey(npcId), accepted ? "accepted" : "completed");
        }
        
        #endregion

        #region === Disposition System ===
        
        /// <summary>
        /// Set disposition to exact value (-100 to +100).
        /// </summary>
        public void SetDisposition(float newValue)
        {
            disposition = Mathf.Clamp(newValue, -100f, 100f);
            UpdateDispositionFlags();
            MarkDirty(DirtyReason.ValueChanged);
        }

        /// <summary>
        /// Modify disposition by delta amount.
        /// </summary>
        public void ModifyDisposition(float delta)
        {
            SetDisposition(disposition + delta);
        }

        /// <summary>
        /// Adjust disposition with custom dirty reason.
        /// </summary>
        public void AdjustDisposition(float amount, DirtyReason reason = DirtyReason.ValueChanged)
        {
            disposition = Mathf.Clamp(disposition + amount, -100f, 100f);
            MarkDirty(reason);
            UpdateCombatStateFromData();
        }

        /// <summary>
        /// Update isEnemy/isNeutral/isFriendly flags based on disposition value and faction relationships.
        /// </summary>
        private void UpdateDispositionFlags()
        {
            // Calculate effective disposition considering faction relationships
            float effectiveDisposition = CalculateEffectiveDisposition();

            isEnemy = effectiveDisposition <= -35f;
            isNeutral = effectiveDisposition > -35f && effectiveDisposition < 35f;
            isFriendly = effectiveDisposition >= 35f;

            // Sync combat state when disposition changes
            UpdateCombatStateFromData();
        }

        /// <summary>
        /// Calculate effective disposition considering both global disposition and faction relationships.
        /// </summary>
        private float CalculateEffectiveDisposition()
        {
            float factionDisposition = GetFactionDispositionModifier();
            return Mathf.Lerp(disposition, factionDisposition, factionInfluence);
        }

        /// <summary>
        /// Get disposition modifier based on player's faction standings with this NPC's factions.
        /// </summary>
        private float GetFactionDispositionModifier()
        {
            if (FactionSystem.Instance == null || factions.Count == 0)
                return disposition; // Fallback to global disposition

            float totalModifier = 0f;
            int validFactions = 0;

            foreach (string factionId in factions)
            {
                var standing = FactionSystem.Instance.GetPlayerStanding(factionId);
                if (standing != null)
                {
                    // Convert reputation to disposition modifier (-100 to +100)
                    float repModifier = Mathf.Clamp(standing.reputation / 50f * 100f, -100f, 100f);
                    totalModifier += repModifier;
                    validFactions++;
                }
            }

            return validFactions > 0 ? totalModifier / validFactions : disposition;
        }

        /// <summary>
        /// Check if this NPC is hostile toward the player based on current disposition and factions.
        /// </summary>
        public bool IsHostileToPlayer()
        {
            return CalculateEffectiveDisposition() <= -35f;
        }

        /// <summary>
        /// Add NPC to a faction and broadcast the change.
        /// </summary>
        public void JoinFaction(string factionId)
        {
            if (!factions.Contains(factionId))
            {
                factions.Add(factionId);
                UpdateDispositionFlags();
                MarkDirty(DirtyReason.ValueChanged);

                // Broadcast faction change event
                WorldBridgeSystem.Instance?.InvokeKey(EventKeys.GetFactionChangedKey(npcId), factionId);
                Debug.Log($"[NPCDataContainer] {displayName} joined faction: {factionId}");

                // Broadcast behavior change event
                WorldBridgeSystem.Instance?.InvokeKey(EventKeys.GetBehaviorChangedKey(npcId), "faction_joined");
            }
        }

        /// <summary>
        /// Remove NPC from a faction and broadcast the change.
        /// </summary>
        public void LeaveFaction(string factionId)
        {
            if (factions.Remove(factionId))
            {
                UpdateDispositionFlags();
                MarkDirty(DirtyReason.ValueChanged);

                // Broadcast faction change event
                WorldBridgeSystem.Instance?.InvokeKey(EventKeys.GetFactionChangedKey(npcId), factionId);
                Debug.Log($"[NPCDataContainer] {displayName} left faction: {factionId}");

                // Broadcast behavior change event
                WorldBridgeSystem.Instance?.InvokeKey(EventKeys.GetBehaviorChangedKey(npcId), "faction_left");
            }
        }
        
        #endregion

        #region === Combat State Management ===
        
        /// <summary>
        /// Called when data changes or on load.
        /// Syncs combat controller with container state.
        /// </summary>
        public void UpdateCombatStateFromData()
        {
            if (combatController == null)
                combatController = GetComponent<NPCCombatController>();

            if (combatController == null)
                return;

            // Use IsHostileToPlayer() for more reliable enemy detection
            if (IsHostileToPlayer())
                combatController.EnableCombatModule();
            else
                combatController.DisableCombatModule();
        }
        
        #endregion

        #region === Serialization ===
        
        public override string SerializeData()
        {
            return HexSerializationHelper.ToHex(this);
        }

        public override void DeserializeData(string data)
        {
            HexSerializationHelper.FromHex(this, data);
            
            // Restore ragdoll state on load
            if (CurrentDeathState == DeathState.RagdollActive && ragdollController != null)
            {
                ragdollController.EnablePhysics();
            }

            UpdateCombatStateFromData();
        }
        
        #endregion

        #region === Vision & Faction Clone Support ===

        /// <summary>
        /// Interface for vision-aware NPC clones
        /// </summary>
        public interface IVisionClone
        {
            NPCDataContainer NPCData { get; }
            void OnWeightedVisionUpdate(Dictionary<int, List<Transform>> results, Dictionary<string, float> factionWeights);
            float GetVisionPriority();
            bool ShouldSubscribeToVision();
        }

        /// <summary>
        /// Clone data for faction-aware vision processing
        /// </summary>
        [Serializable]
        public class NPCCloneData
        {
            public string cloneId;
            public List<string> inheritedFactions = new();
            public float dispositionModifier = 0f; // Adjust disposition for this clone
            public Dictionary<string, float> factionModifiers = new(); // Per-faction adjustments
            public bool inheritsVisionSubscription = true;
            public float visionPriorityMultiplier = 1f;
        }

        [Header("Clone Data")]
        [SerializeField] private NPCCloneData cloneData;

        /// <summary>
        /// Get vision priority based on disposition and factions
        /// </summary>
        public float GetVisionPriority()
        {
            float basePriority = Mathf.Clamp01((50f - CalculateEffectiveDisposition()) / 100f);

            // Apply clone multiplier
            if (cloneData != null)
            {
                basePriority *= cloneData.visionPriorityMultiplier;
            }

            // Modify based on faction relationships
            if (FactionSystem.Instance != null)
            {
                float factionMultiplier = 1f;
                foreach (string factionId in factions)
                {
                    var standing = FactionSystem.Instance.GetPlayerStanding(factionId);
                    if (standing.reputation < 0)
                    {
                        factionMultiplier += Mathf.Abs(standing.reputation) / 200f; // Increase priority for hostile factions
                    }
                }
                basePriority *= factionMultiplier;
            }

            return Mathf.Clamp01(basePriority);
        }

        /// <summary>
        /// Determine if this NPC should subscribe to vision batching
        /// </summary>
        public bool ShouldSubscribeToVision()
        {
            // Always subscribe if in combat-ready state
            if (isEnemy) return true;

            // Check clone settings
            if (cloneData != null && !cloneData.inheritsVisionSubscription)
            {
                return false; // Clone explicitly opts out
            }

            // Subscribe if disposition suggests potential hostility
            if (CalculateEffectiveDisposition() < 25f) return true;

            // Subscribe if in relevant factions
            if (factions.Count > 0 && FactionSystem.Instance != null)
            {
                foreach (string factionId in factions)
                {
                    var standing = FactionSystem.Instance.GetPlayerStanding(factionId);
                    if (standing.reputation < 50) return true; // Subscribe if not in good standing
                }
            }

            return false; // Neutral/friendly NPCs don't need constant vision updates
        }

        /// <summary>
        /// Get faction standing for a specific faction
        /// </summary>
        private float GetFactionStanding(string factionId)
        {
            if (FactionSystem.Instance == null) return disposition;

            var standing = FactionSystem.Instance.GetPlayerStanding(factionId);
            return standing.reputation;
        }

        #endregion

        #region === Clone / Copy ===

        /// <summary>
        /// Copy data from another NPC container.
        /// </summary>
        public void CopyFrom(NPCDataContainer source)
        {
            uniqueIdRef = GetComponent<UniqueId>();
            npcId = uniqueIdRef ? uniqueIdRef.GetID() : Guid.NewGuid().ToString();

            displayName = source.displayName;
            currentHP = source.currentHP;  // ✅ FIXED: Copy current HP, not max HP
            maxHP = source.maxHP;
            level = source.level;

            hasSpokenToPlayer = source.hasSpokenToPlayer;
            dialogueStage = source.dialogueStage;
            lastDialogueKey = source.lastDialogueKey;

            isQuestGiver = source.isQuestGiver;
            questAccepted = false;
            questCompleted = false;

            disposition = source.disposition;
            factions = new List<string>(source.factions);

            lastKnownPosition = Vector3.zero;
            customTag = source.customTag;

            // Reset death state to alive
            CurrentDeathState = DeathState.Alive;

            DataContainerManager.Instance?.Register(this);
            UpdateCombatStateFromData();
        }

        /// <summary>
        /// Enhanced copy method for factioned clones
        /// </summary>
        public void CopyFromWithFactionContext(NPCDataContainer source, NPCCloneData cloneData = null)
        {
            // Base copy
            CopyFrom(source);

            // Apply clone-specific modifications
            if (cloneData != null)
            {
                this.cloneData = cloneData;
                npcId = cloneData.cloneId;

                // Modify disposition
                disposition += cloneData.dispositionModifier;

                // Apply faction modifiers
                foreach (var modifier in cloneData.factionModifiers)
                {
                    if (factions.Contains(modifier.Key))
                    {
                        // For now, we'll adjust the global disposition as a proxy
                        // In a more complex system, you'd track per-faction modifiers separately
                        disposition += modifier.Value * 0.1f; // Subtle adjustment
                    }
                }

                // Inherit or modify factions
                if (cloneData.inheritedFactions.Count > 0)
                {
                    factions = new List<string>(cloneData.inheritedFactions);
                }
            }

            // Update flags and register
            UpdateDispositionFlags();
            DataContainerManager.Instance?.Register(this);
        }

        #endregion
    }
}
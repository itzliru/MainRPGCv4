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
namespace VaultSystems.Data
{

    
    ///<summary>
    /// Base class for any entity that can take damage and die (Player, NPC, etc.)
    /// Handles death state machine and ragdoll physics transitions.
    /// </summary>
    [Serializable]
    public abstract class BaseDamageableContainer : AdvancedDataContainer, IDamageable
    {
        #region === Death State System ===
        
        [Header("Death State")]
        [SerializeField] protected DeathState _deathState = DeathState.Alive;
        
        public DeathState CurrentDeathState
        {
            get => _deathState;
            protected set
            {
                if (_deathState == value) return;
                
                _deathState = value;
                MarkDirty(DirtyReason.ValueChanged);
                OnDeathStateChanged?.Invoke(_deathState);
                
                Debug.Log($"[{GetType().Name}] Death state: {_deathState}");
            }
        }

        public bool IsDead => _deathState != DeathState.Alive;
        
        public event Action<DeathState> OnDeathStateChanged;
        
        #endregion

        #region === Health ===
        
        [Header("Health")]
        [SerializeField] protected int _currentHP = 100;
        [SerializeField] protected int _maxHP = 100;
        
        public int CurrentHP => _currentHP;
        public int MaxHP => _maxHP;
        
        #endregion

        #region === Ragdoll Physics ===
        
        protected RagdollController ragdollController;
        
        #endregion

        #region === Public API ===
        
        /// <summary>
        /// Apply damage to this entity.
        /// Triggers death sequence if HP reaches 0.
        /// </summary>
        public virtual void TakeDamage(int amount)
        {
            if (amount <= 0 || IsDead) 
                return;

            _currentHP -= amount;
            _currentHP = Mathf.Clamp(_currentHP, 0, _maxHP);
            
            Debug.Log($"[{GetType().Name}] Took {amount} damage. HP: {_currentHP}/{_maxHP}");

            if (_currentHP <= 0 && _deathState == DeathState.Alive)
            {
                OnHealthDepleted();
            }

            MarkDirty(DirtyReason.ValueChanged);
        }

        /// <summary>
        /// Restore HP to full.
        /// </summary>
        public virtual void Heal(int amount)
        {
            if (IsDead || amount <= 0) 
                return;

            _currentHP = Mathf.Clamp(_currentHP + amount, 0, _maxHP);
            MarkDirty(DirtyReason.ValueChanged);
        }

        /// <summary>
        /// Revive from death state.
        /// </summary>
        public virtual void Revive()
        {
            if (!IsDead) 
                return;

            _currentHP = _maxHP / 2;
            CurrentDeathState = DeathState.Alive;
            DisableRagdoll();
            
            Debug.Log($"[{GetType().Name}] Revived with {_currentHP} HP");
        }

        /// <summary>
        /// Enable physics-based ragdoll.
        /// </summary>
        public virtual void EnableRagdoll()
        {
            if (ragdollController == null)
                ragdollController = GetComponent<RagdollController>();

            if (ragdollController != null)
            {
                ragdollController.EnablePhysics();
                CurrentDeathState = DeathState.RagdollActive;
                Debug.Log($"[{GetType().Name}] Ragdoll ENABLED");
            }
            else
            {
                Debug.LogWarning($"[{GetType().Name}] No RagdollController found! Skipping to Dead state.");
                CurrentDeathState = DeathState.Dead;
            }
        }

        /// <summary>
        /// Disable ragdoll and mark as dead.
        /// </summary>
        public virtual void DisableRagdoll()
        {
            if (ragdollController != null)
            {
                ragdollController.DisablePhysics();
            }
            CurrentDeathState = DeathState.Dead;
            Debug.Log($"[{GetType().Name}] Ragdoll DISABLED - now in Dead state");
        }

        #endregion

        #region === Protected Virtual Methods (Override in Subclasses) ===
        
        /// <summary>
        /// Called when HP reaches 0. Triggers death sequence.
        /// Override to customize death behavior.
        /// </summary>
        protected virtual void OnHealthDepleted()
        {
            CurrentDeathState = DeathState.Dying;
            Debug.Log($"[{GetType().Name}] Health depleted - transitioning to Dying state");
            
            // Trigger ragdoll sequence
            EnableRagdoll();
            
            // Subclasses can override to add effects, loot, etc.
            OnDeathSequenceTriggered();
        }

        /// <summary>
        /// Called after OnHealthDepleted. Override for custom death logic.
        /// </summary>
        protected virtual void OnDeathSequenceTriggered()
        {
            // Override in subclasses for:
            // - Death animations
            // - Loot drops
            // - Sound effects
            // - Despawn scheduling
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
            
            // Restore ragdoll if needed
            if (CurrentDeathState == DeathState.RagdollActive && ragdollController != null)
            {
                ragdollController.EnablePhysics();
            }
        }

        #endregion
    }
}

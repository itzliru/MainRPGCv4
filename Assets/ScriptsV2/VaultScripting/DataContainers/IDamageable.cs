using System;
using VaultSystems.Data;

namespace VaultSystems.Data
{
    /// <summary>
    /// Common interface for anything that can take damage and die.
    /// </summary>
    public interface IDamageable
    {
        // === State ===
        DeathState CurrentDeathState { get; }
        bool IsDead { get; }

        // === Health ===
        int CurrentHP { get; }
        int MaxHP { get; }

        // === Methods ===
        void TakeDamage(int amount);
        void EnableRagdoll();
        void DisableRagdoll();

        // === Events ===
        event Action<DeathState> OnDeathStateChanged;
    }
}

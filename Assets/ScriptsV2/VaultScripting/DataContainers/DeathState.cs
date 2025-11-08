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
    /// <summary>
    /// Represents the lifecycle of any damageable entity (Player, NPC, etc.)
    /// </summary>
    public enum DeathState
    {
        Alive,           // Normal, can take damage
        Dying,           // Health depleted, transitioning to ragdoll
        RagdollActive,   // Physics-based ragdoll active
        Dead             // Final state, can remove/despawn
    }
}

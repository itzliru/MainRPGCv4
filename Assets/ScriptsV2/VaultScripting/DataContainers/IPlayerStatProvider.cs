using System;
using VaultSystems.Data;
using System.Linq;
using UnityEngine.SceneManagement;
using System;
using System.Collections.Generic;
using UnityEngine;
using VaultSystems.Invoker;
using UnityEngine.Events;
 
namespace VaultSystems.Data
{
 
public interface IPlayerStatProvider
{
    // Derived stats
    float GetAimSwayAmplitude();         // weapon skill
    float GetAimSwaySpeedMultiplier();   // weapon skill, level
    float GetBaseMovementSpeed();        // agility, level, equipment
    float GetAimMovementSpeed();

    // Raw stats
    int GetLevel();
    int GetAgility();
    int GetStrength();
    int GetWepSkill();
    int GetCurrentHP();
    int GetMaxHP();
    int GetMysticPower();
    int GetMysticImplants();
    int GetScrollLevel();
    int GetXP();

    // Additional derived or utility
    //float GetCurrentMaxHealth();         // buffs
    // etc.
}

}
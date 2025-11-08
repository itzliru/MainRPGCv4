using System;
using System.Collections.Generic;
using UnityEngine;
using VaultSystems.Invoker;
using UnityEngine.Events;
using VaultSystems.Data;
using System.Linq;


namespace VaultSystems.Data

{
    public class PlayerStatContext : MonoBehaviour, IPlayerStatProvider
    {
        [SerializeField] private PlayerDataContainer data;
        [SerializeField] private AnimationCurve agilityToSpeed = AnimationCurve.Linear(0, 0, 100, 1);
        [SerializeField] private AnimationCurve weaponSkillToSway = AnimationCurve.Linear(0, 1f, 100, 0.1f);

        private float baseMoveSpeed = 5f;
        private float baseAimSpeed = 4f;
        private float baseAimSwayAmplitude = 0.5f;
        private float minAimSway = 0.05f;
        private float aimSwayFrequency = 2f;

        private void Awake()
        {
            if (!data)
                data = GetComponentInParent<PlayerDataContainer>();

            if (data == null)
            {
                Debug.LogError($"[PlayerStatContext] No PlayerDataContainer found on {gameObject.name} or its parents!");
                enabled = false;
                return;
            }

            data.OnStatsChanged += HandleStatsChanged;
            HandleStatsChanged(this, data); // ✅ Correct two-argument call
        }

        private void OnDestroy()
        {
            if (data != null)
                data.OnStatsChanged -= HandleStatsChanged;
        }

      private void HandleStatsChanged(object sender, PlayerDataContainer source)
{
    if (source == null) return;

    CurrentMoveSpeed = baseMoveSpeed * (1f + agilityToSpeed.Evaluate(source.agility));
    CurrentAimMoveSpeed = baseAimSpeed * (1f + agilityToSpeed.Evaluate(source.agility) * 0.5f + source.level * 0.02f);
    CurrentAimSwayAmplitude = Mathf.Lerp(baseAimSwayAmplitude, minAimSway,
                                         weaponSkillToSway.Evaluate(source.wepSkill));
    AimSwayFrequency = aimSwayFrequency * Mathf.Lerp(1f, 0.6f, source.wepSkill / 100f);
}



        public float CurrentMoveSpeed { get; private set; }
        public float CurrentAimMoveSpeed { get; private set; }
        public float CurrentAimSwayAmplitude { get; private set; }
        public float AimSwayFrequency { get; private set; }

        // Derived stats
        public float GetAimSwayAmplitude() => CurrentAimSwayAmplitude;
        public float GetAimSwaySpeedMultiplier() => AimSwayFrequency;
        public float GetBaseMovementSpeed() => CurrentMoveSpeed;
        public float GetAimMovementSpeed() => CurrentAimMoveSpeed;

        // Raw stats from PlayerDataContainer
        public int GetLevel() => data != null ? data.level : 1;
        public int GetAgility() => data != null ? data.agility : 10;
        public int GetStrength() => data != null ? data.strength : 10;
        public int GetWepSkill() => data != null ? data.wepSkill : 10;
        public int GetCurrentHP() => data != null ? data.currentHP : 100;
        public int GetMaxHP() => data != null ? data.maxHP : 100;
        public int GetMysticPower() => data != null ? data.mysticPower : 0;
        public int GetMysticImplants() => data != null ? data.mysticImplants : 0;
        public int GetScrollLevel() => data != null ? data.scrollLevel : 1;
        public int GetXP() => data != null ? data.xp : 0;
    }
}
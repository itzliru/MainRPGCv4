using UnityEngine;
using UnityEngine.UI;
using VaultSystems.Data;

namespace VaultSystems.Data
{
    /// <summary>
    /// Debug UI for displaying player stats in real-time.
    /// Displays raw and derived stats from IPlayerStatProvider.
    /// Toggle visibility with F1 key.
    /// </summary>
    public class PlayerStatDebugUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Text rawStatsText;
        [SerializeField] private Text derivedStatsText;
        [SerializeField] private Text hashText;

        [Header("Configuration")]
        [SerializeField] private KeyCode toggleKey = KeyCode.F1;

        private IPlayerStatProvider provider;
        private PlayerStatDirtyTracker dirtyTracker;
        private bool isVisible = true;

        private void Awake()
        {
            provider = GetComponent<IPlayerStatProvider>() ?? GetComponentInParent<IPlayerStatProvider>();
            if (provider == null)
            {
                Debug.LogError("[PlayerStatDebugUI] No IPlayerStatProvider found! Disabling.");
                enabled = false;
                return;
            }

            dirtyTracker = GetComponent<PlayerStatDirtyTracker>() ?? GetComponentInParent<PlayerStatDirtyTracker>();
            if (dirtyTracker != null)
            {
                dirtyTracker.onStatsChanged.AddListener(UpdateUI);
            }

            UpdateUI();
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey))
            {
                ToggleVisibility();
            }
        }

        private void ToggleVisibility()
        {
            isVisible = !isVisible;
            if (rawStatsText != null) rawStatsText.gameObject.SetActive(isVisible);
            if (derivedStatsText != null) derivedStatsText.gameObject.SetActive(isVisible);
            if (hashText != null) hashText.gameObject.SetActive(isVisible);
        }

        private void UpdateUI()
        {
            if (provider == null) return;

            // Raw stats
            if (rawStatsText != null)
            {
                rawStatsText.text = $"RAW STATS:\n" +
                    $"Level: {provider.GetLevel()}\n" +
                    $"Agility: {provider.GetAgility()}\n" +
                    $"Strength: {provider.GetStrength()}\n" +
                    $"WepSkill: {provider.GetWepSkill()}\n" +
                    $"Current HP: {provider.GetCurrentHP()}\n" +
                    $"Max HP: {provider.GetMaxHP()}\n" +
                    $"Mystic Power: {provider.GetMysticPower()}\n" +
                    $"Mystic Implants: {provider.GetMysticImplants()}\n" +
                    $"Scroll Level: {provider.GetScrollLevel()}";
            }

            // Derived stats
            if (derivedStatsText != null)
            {
                derivedStatsText.text = $"DERIVED STATS:\n" +
                    $"Aim Sway Amplitude: {provider.GetAimSwayAmplitude():F3}\n" +
                    $"Aim Sway Speed: {provider.GetAimSwaySpeedMultiplier():F3}\n" +
                    $"Base Move Speed: {provider.GetBaseMovementSpeed():F3}\n" +
                    $"Aim Move Speed: {provider.GetAimMovementSpeed():F3}";
            }

            // Hash (from dirty tracker if available)
            if (hashText != null && dirtyTracker != null)
            {
                // Access private field via reflection or add public getter
                // For now, placeholder
                hashText.text = $"HASH: [Not implemented]";
            }
        }

        private void OnDestroy()
        {
            if (dirtyTracker != null)
            {
                dirtyTracker.onStatsChanged.RemoveListener(UpdateUI);
            }
        }
    }
}

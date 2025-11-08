using UnityEngine;
using System.Collections.Generic;

namespace VaultSystems.Data 
{

/// <summary>
/// 🔹 Global Layer Manager
/// Centralizes all layer names, indices, and bitmasks for easy use in raycasts, physics, AI, and vision.
/// Supports caching, dynamic combination/exclusion, and debug utilities.
/// </summary>
[DefaultExecutionOrder(-120)]
public class GlobalLayerMaskManager : MonoBehaviour
{
    public static GlobalLayerMaskManager Instance { get; private set; }

    [Header("Registered Layers")]
    public string playerLayer = "Player";
    public string enemyLayer = "Enemy";
    public string groundLayer = "Ground";
    public string interactableLayer = "Interactable";
    public string projectileLayer = "Projectile";
    public string environmentLayer = "Environment";
    public string uiLayer = "UI";

    // Cached indices (auto-set at runtime)
    private static readonly Dictionary<string, int> _layerCache = new();

    // Cached masks
    private static readonly Dictionary<string, int> _maskCache = new();

    // Properties
    public static int Player => GetLayer(Instance.playerLayer);
    public static int Enemy => GetLayer(Instance.enemyLayer);
    public static int Ground => GetLayer(Instance.groundLayer);
    public static int Interactable => GetLayer(Instance.interactableLayer);
    public static int Projectile => GetLayer(Instance.projectileLayer);
    public static int Environment => GetLayer(Instance.environmentLayer);
    public static int UI => GetLayer(Instance.uiLayer);

    // Prebuilt Masks
    public static int PlayerMask => 1 << Player;
    public static int EnemyMask => 1 << Enemy;
    public static int GroundMask => 1 << Ground;
    public static int InteractableMask => 1 << Interactable;
    public static int ProjectileMask => 1 << Projectile;
    public static int EnvironmentMask => 1 << Environment;
    public static int UIMask => 1 << UI;
    public static int All => ~0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        CacheLayers();
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Cache all known layer names for fast runtime access.
    /// </summary>
    private void CacheLayers()
    {
        string[] names =
        {
            playerLayer, enemyLayer, groundLayer, interactableLayer,
            projectileLayer, environmentLayer, uiLayer
        };

        foreach (string name in names)
        {
            if (string.IsNullOrEmpty(name)) continue;

            int index = LayerMask.NameToLayer(name);
            if (index < 0)
            {
                Debug.LogWarning($"[GlobalLayerMaskManager] Layer '{name}' not found. " +
                                 "Check your Project Settings > Tags and Layers.");
                continue;
            }

            _layerCache[name] = index;
            _maskCache[name] = 1 << index;
        }

        Debug.Log("[GlobalLayerMaskManager] Layers cached successfully.");
    }

    /// <summary>
    /// Retrieve a cached layer index by name.
    /// </summary>
    public static int GetLayer(string name)
    {
        if (string.IsNullOrEmpty(name)) return -1;

        if (_layerCache.TryGetValue(name, out int index))
            return index;

        int fresh = LayerMask.NameToLayer(name);
        if (fresh >= 0)
        {
            _layerCache[name] = fresh;
            _maskCache[name] = 1 << fresh;
        }

        return fresh;
    }

    /// <summary>
    /// Retrieve a cached layer mask by name.
    /// </summary>
    public static int GetMask(string name)
    {
        if (string.IsNullOrEmpty(name)) return 0;

        if (_maskCache.TryGetValue(name, out int mask))
            return mask;

        int index = GetLayer(name);
        if (index >= 0)
        {
            _maskCache[name] = 1 << index;
            return 1 << index;
        }

        return 0;
    }

    /// <summary>
    /// Combine multiple layers into one bitmask.
    /// </summary>
    public static int Combine(params int[] layers)
    {
        int mask = 0;
        foreach (int layer in layers)
            mask |= (1 << layer);
        return mask;
    }

    /// <summary>
    /// Returns a mask that includes everything except the given layers.
    /// </summary>
    public static int Exclude(params int[] layers)
    {
        int mask = All;
        foreach (int layer in layers)
            mask &= ~(1 << layer);
        return mask;
    }

    /// <summary>
    /// Combine multiple masks into one.
    /// </summary>
    public static int CombineMasks(params int[] masks)
    {
        int result = 0;
        foreach (int mask in masks)
            result |= mask;
        return result;
    }

    /// <summary>
    /// Returns true if a given layer index is included in the mask.
    /// </summary>
    public static bool Contains(int mask, int layer)
    {
        return (mask & (1 << layer)) != 0;
    }

    /// <summary>
    /// Prints which layers are active in a mask.
    /// </summary>
    public static void DebugMask(int mask, string label = "LayerMask")
    {
        System.Text.StringBuilder sb = new(label + " includes: ");
        bool hasAny = false;

        for (int i = 0; i < 32; i++)
        {
            if ((mask & (1 << i)) != 0)
            {
                sb.Append($"{i} ({LayerMask.LayerToName(i)}), ");
                hasAny = true;
            }
        }

        if (!hasAny) sb.Append("None");
        Debug.Log(sb.ToString());
    }

    /// <summary>
    /// Quickly test a raycast with a specific layer mask.
    /// </summary>
    public static bool Raycast(Vector3 origin, Vector3 dir, float distance, int mask, out RaycastHit hit)
    {
        return Physics.Raycast(origin, dir, out hit, distance, mask, QueryTriggerInteraction.Ignore);
    }

    /// <summary>
    /// Convert a LayerMask to readable string for logs or debug.
    /// </summary>
    public static string MaskToString(int mask)
    {
        string result = "";
        for (int i = 0; i < 32; i++)
        {
            if ((mask & (1 << i)) != 0)
                result += LayerMask.LayerToName(i) + ", ";
        }
        return string.IsNullOrEmpty(result) ? "None" : result.TrimEnd(',', ' ');
    }

    // ==============================
    // 🔹 Vision Layer Utilities
    // ==============================

    /// <summary>
    /// Predefined masks for vision/AI logic — batch configurable.
    /// </summary>
    public static class Vision
    {
        public static int VisionBlocking => Combine(Environment, Ground);
        public static int VisionTargets => Combine(Player, Enemy, Interactable);

        public static int Default => Exclude(UI, Projectile);
    }

    /// <summary>
    /// Checks if target is visible from origin considering vision masks.
    /// </summary>
    public static bool HasLineOfSight(Vector3 origin, Vector3 target, int mask, float maxDistance = 100f)
    {
        Vector3 dir = (target - origin);
        float dist = dir.magnitude;
        dir.Normalize();

        if (Physics.Raycast(origin, dir, out RaycastHit hit, Mathf.Min(dist, maxDistance), mask))
        {
            // Hit something before reaching the target
            return hit.collider.transform.position == target;
        }

        return true;
    }

    /// <summary>
    /// Batch vision check for multiple targets using Physics.RaycastNonAlloc.
    /// </summary>
    public static void BatchVisionCheck(Vector3 origin, List<Transform> targets, int mask, float maxDistance = 50f)
    {
        foreach (Transform t in targets)
        {
            if (t == null) continue;
            bool visible = HasLineOfSight(origin, t.position, mask, maxDistance);
            Debug.DrawLine(origin, t.position, visible ? Color.green : Color.red, 0.1f);
        }
    }

}
}
using UnityEngine;
using System.Collections.Generic;
using VaultSystems.Data;
using VaultSystems.Invoker;
using VaultSystems.Containers;
using System.Linq;
using System;
/// <summary>
/// 🔹 VisionBatchManager — global AI vision batching
/// Efficiently batches line-of-sight or proximity scans for multiple agents
/// using shared layer masks from GlobalLayerMaskManager.
/// Now integrated with cell-based scanning for better performance.
/// </summary>
[DefaultExecutionOrder(-120)]
public class VisionBatchManager : MonoBehaviour
{
    public static VisionBatchManager Instance { get; private set; }

    [Header("Scan Settings")]
    public float scanInterval = 0.25f; // How often to update batches
    public float maxScanDistance = 40f;
    public bool showDebug = false;
    public bool useCellBasedScanning = true; // Enable cell-aware scanning
    private IDisposable _cellChangedToken;
    private float nextScanTime;

    // Cached global results (shared between all agents)
    private readonly Dictionary<int, List<Transform>> _batchResults = new();

    // Registered AI subscribers
    private readonly HashSet<IVisionSubscriber> _subscribers = new();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Subscribe to cell changes for optimized scanning
         if (useCellBasedScanning)
        {
            _cellChangedToken = WorldBridgeSystem.Instance?.RegisterInvoker(
                EventKeys.Scene.CELL_CHANGED,
                OnCellChanged,                              // Named method (not lambda!)
                DynamicDictionaryInvoker.Layer.Func,           // Execution layer (enum inside DynamicDictInvoker!)
                id: "vision_batch_cell_tracker",                // Debugging ID ()
                metadata: "vision_batching"                
            );
        }
    }

    void Update()
    {
        if (Time.time >= nextScanTime)
        {
            nextScanTime = Time.time + scanInterval;
            PerformBatchedScan();
        }
    }

    /// <summary>
    /// Handle cell changes to optimize scanning regions.
    /// </summary>
    private void OnCellChanged(object[] args)
    {
        if (!useCellBasedScanning) return;

        string newCellId = (string)args[0];
        Debug.Log($"[VisionBatchManager] Cell changed to {newCellId}, optimizing scan regions");

        // Force immediate scan when cell changes
        nextScanTime = 0f;
    }

    /// <summary>
    /// Subscribe an AI or detector to receive shared vision results.
    /// </summary>
    public void Register(IVisionSubscriber sub)
    {
        _subscribers.Add(sub);
    }

    public void Unregister(IVisionSubscriber sub)
    {
        _subscribers.Remove(sub);
    }

    /// <summary>
    /// Perform shared batch scans per vision mask.
    /// Now optimized for cell-based scanning when enabled.
    /// </summary>
    private void PerformBatchedScan()
    {
        _batchResults.Clear();

        if (useCellBasedScanning)
        {
            // Only scan active cells + adjacent cells for better performance
            string currentCell = PlayerWorldManager.Instance?.currentCellId;
            if (!string.IsNullOrEmpty(currentCell))
            {
                BatchScanCell(currentCell);
                return;
            }
        }

        // Fallback to global scanning
        BatchScanGlobal();
    }

    /// <summary>
    /// Scan only the current cell and adjacent cells.
    /// </summary>
    private void BatchScanCell(string currentCell)
    {
        // Get cell bounds (assuming 10f cell size like PlayerWorldManager)
        Vector3 cellCenter = GetCellCenter(currentCell);
        float cellSize = 10f;
        float scanRadius = cellSize * 2f; // Cover current + adjacent cells

        // Define vision masks
        int playerMask = GlobalLayerMaskManager.PlayerMask;
        int enemyMask = GlobalLayerMaskManager.EnemyMask;
        int interactableMask = GlobalLayerMaskManager.InteractableMask;

        // Scan from cell center with optimized radius
        BatchScanMaskFromPoint(cellCenter, scanRadius, playerMask);
        BatchScanMaskFromPoint(cellCenter, scanRadius, enemyMask);
        BatchScanMaskFromPoint(cellCenter, scanRadius, interactableMask);

        // Notify subscribers
        foreach (var sub in _subscribers)
            sub.OnVisionBatchUpdate(_batchResults);

        // Broadcast enhanced vision update with faction context
        BroadcastWeightedVisionUpdate(_batchResults, currentCell);
    }

    /// <summary>
    /// Fallback global scanning when cell system not available.
    /// </summary>
    private void BatchScanGlobal()
    {
        // Define vision masks (you can add more channels if needed)
        int playerMask = GlobalLayerMaskManager.PlayerMask;
        int enemyMask = GlobalLayerMaskManager.EnemyMask;
        int interactableMask = GlobalLayerMaskManager.InteractableMask;

        BatchScanMask(playerMask);
        BatchScanMask(enemyMask);
        BatchScanMask(interactableMask);

        // Notify subscribers
        foreach (var sub in _subscribers)
            sub.OnVisionBatchUpdate(_batchResults);

        // Broadcast event
        WorldBridgeSystem.Instance?.InvokeKey(EventKeys.Scene.VISION_BATCH_UPDATE,
            new object[] { _batchResults, "global" });
    }

    /// <summary>
    /// Scans all colliders on a given mask from a specific point.
    /// </summary>
    private void BatchScanMaskFromPoint(Vector3 center, float radius, int mask)
    {
        Collider[] hits = Physics.OverlapSphere(center, radius, mask, QueryTriggerInteraction.Ignore);
        List<Transform> found = new();

        foreach (var h in hits)
        {
            if (h != null)
                found.Add(h.transform);
        }

        _batchResults[mask] = found;

        if (showDebug)
        {
            string maskName = GlobalLayerMaskManager.MaskToString(mask);
            Debug.Log($"[VisionBatchManager] Cell scan found {found.Count} on mask {maskName} from {center}");
        }
    }

    /// <summary>
    /// Scans all colliders on a given mask globally.
    /// </summary>
    private void BatchScanMask(int mask)
    {
        Collider[] hits = Physics.OverlapSphere(Vector3.zero, maxScanDistance, mask, QueryTriggerInteraction.Ignore);
        List<Transform> found = new();

        foreach (var h in hits)
        {
            if (h != null)
                found.Add(h.transform);
        }

        _batchResults[mask] = found;

        if (showDebug)
        {
            string maskName = GlobalLayerMaskManager.MaskToString(mask);
            Debug.Log($"[VisionBatchManager] Found {found.Count} on mask {maskName}");
        }
    }

    /// <summary>
    /// Calculate the center position of a cell based on its ID.
    /// </summary>
    private Vector3 GetCellCenter(string cellId)
    {
        // Parse cell coordinates from ID (e.g., "Cell_05" -> x=0, y=5)
        string num = new string(cellId.Where(char.IsDigit).ToArray());
        if (int.TryParse(num, out int cellIndex))
        {
            int x = cellIndex % 8;
            int y = cellIndex / 8;
            float cellSize = 10f;
            return new Vector3(x * cellSize + cellSize/2, 0, y * cellSize + cellSize/2);
        }

        return Vector3.zero;
    }

    /// <summary>
    /// Gets shared batch results for a specific mask.
    /// </summary>
    public List<Transform> GetResults(int mask)
    {
        return _batchResults.TryGetValue(mask, out var list) ? list : new List<Transform>();
    }

    private void BroadcastWeightedVisionUpdate(Dictionary<int, List<Transform>> results, string cellId)
    {
        // Include faction/disposition context in broadcast
        var enhancedResults = new Dictionary<string, object> {
            { "batchResults", results },
            { "cellId", cellId },
            { "factionContext", GetFactionContext() }
        };

        WorldBridgeSystem.Instance?.InvokeKey(EventKeys.Scene.VISION_BATCH_UPDATE, enhancedResults);
    }

    private Dictionary<string, float> GetFactionContext()
    {
        // Return current faction standings for contextual processing
        var context = new Dictionary<string, float>();
        if (FactionSystem.Instance != null)
        {
            foreach (var faction in FactionSystem.Instance.allFactions)
            {
                var standing = FactionSystem.Instance.GetPlayerStanding(faction.factionId);
                context[faction.factionId] = standing.reputation;
            }
        }
        return context;
    }

    void OnDestroy()
    {
        if (useCellBasedScanning)
        {
            _cellChangedToken?.Dispose(); 
            _cellChangedToken = null;
            
        }
    }
}

/// <summary>
/// Interface for AI or vision consumers.
/// </summary>
public interface IVisionSubscriber
{
    void OnVisionBatchUpdate(Dictionary<int, List<Transform>> batchResults);
}

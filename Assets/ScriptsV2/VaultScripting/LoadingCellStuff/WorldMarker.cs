using VaultSystems.Invoker;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using VaultSystems.Data;
public partial class WorldMarker : MonoBehaviour
{
    [Header("Marker Info")]
    public string markerId;
    public string cellId;
    public string displayName = "Marker";
    public Sprite icon;
    public bool isVisible = true;

    [Header("Tracking")]
    public Vector3 lastKnownPosition;

    private void Awake()
    {
        if (string.IsNullOrEmpty(markerId))
            markerId = gameObject.name;
    }

    private void OnEnable()
    {
        lastKnownPosition = transform.position;
        MarkerSystem.Register(this);
    }

    private void OnDisable()
    {
        MarkerSystem.Unregister(this);
    }

    private void Update()
    {
        // Keep position updated for marker operands
        lastKnownPosition = transform.position;
    }

    public void SetVisible(bool visible)
    {
        if (isVisible == visible) return;
        isVisible = visible;
        gameObject.SetActive(visible);
    }

    /// <summary>
    /// XOR toggle visibility state
    /// </summary>
    public void ToggleVisibility()
    {
        isVisible ^= true;
        gameObject.SetActive(isVisible);
    }

    /// <summary>
    /// XOR combine cell IDs for multi-cell markers
    /// </summary>
    public void CombineCellId(string newCellId)
    {
        if (!string.IsNullOrEmpty(cellId) && !string.IsNullOrEmpty(newCellId))
        {
            // XOR-based cell combination for overlapping regions
            cellId = CombineStringsXOR(cellId, newCellId);
        }
        else
        {
            cellId = newCellId ?? cellId;
        }
    }

    /// <summary>
    /// XOR-based string combination for marker properties
    /// </summary>
    private string CombineStringsXOR(string a, string b)
    {
        var chars = a.ToCharArray();
        var otherChars = b.ToCharArray();
        var result = new char[Mathf.Max(chars.Length, otherChars.Length)];

        for (int i = 0; i < result.Length; i++)
        {
            char charA = i < chars.Length ? chars[i] : '\0';
            char charB = i < otherChars.Length ? otherChars[i] : '\0';
            result[i] = (char)(charA ^ charB);
        }

        return new string(result).TrimEnd('\0');
    }

    
    /// <summary>
/// XOR position update for marker tracking
/// </summary>
public void UpdatePositionXOR(Vector3 delta)
{
    int xBits = Mathf.RoundToInt(lastKnownPosition.x) ^ Mathf.RoundToInt(delta.x);
    int yBits = Mathf.RoundToInt(lastKnownPosition.y) ^ Mathf.RoundToInt(delta.y);
    int zBits = Mathf.RoundToInt(lastKnownPosition.z) ^ Mathf.RoundToInt(delta.z);
    
    lastKnownPosition.x = xBits;
    lastKnownPosition.y = yBits;
    lastKnownPosition.z = zBits;
}
}

// Static marker system for global access
public static class MarkerSystem
{
    private static HashSet<WorldMarker> activeMarkers = new();

    public static void Register(WorldMarker marker)
    {
        activeMarkers.Add(marker);
    }

    public static void Unregister(WorldMarker marker)
    {
        activeMarkers.Remove(marker);
    }

    public static IEnumerable<WorldMarker> GetMarkersInCell(string cellId)
    {
        return activeMarkers.Where(m => m.cellId == cellId);
    }

    public static WorldMarker GetMarkerById(string markerId)
    {
        return activeMarkers.FirstOrDefault(m => m.markerId == markerId);
    }

    /// <summary>
    /// Get all active markers (for UI systems)
    /// </summary>
    public static IEnumerable<WorldMarker> GetAllMarkers()
    {
        return activeMarkers;
    }

    /// <summary>
    /// Toggle all markers in a cell (cell-based approach)
    /// </summary>
    public static void ToggleCellVisibility(string cellId)
    {
        var cellMarkers = GetMarkersInCell(cellId);
        foreach (var marker in cellMarkers)
        {
            marker.ToggleVisibility();
        }
    }

    /// <summary>
    /// Toggle markers within collider bounds (spatial approach)
    /// </summary>
    public static void ToggleMarkersInCollider(Collider bounds)
    {
        foreach (var marker in activeMarkers)
        {
            if (bounds.bounds.Contains(marker.lastKnownPosition))
            {
                marker.ToggleVisibility();
            }
        }
    }

    /// <summary>
    /// Get markers within XOR distance (encrypted proximity)
    /// </summary>
    /// <summary>
/// Get markers within XOR distance (encrypted proximity)
/// </summary>
public static IEnumerable<WorldMarker> GetMarkersInXORRadius(Vector3 center, float radius, Vector3 xorKey)
{
    Vector3 encryptedCenter = new Vector3(
        Mathf.RoundToInt(center.x) ^ Mathf.RoundToInt(xorKey.x),
        Mathf.RoundToInt(center.y) ^ Mathf.RoundToInt(xorKey.y),
        Mathf.RoundToInt(center.z) ^ Mathf.RoundToInt(xorKey.z)
    );

    return activeMarkers.Where(marker => {
        Vector3 encryptedPos = new Vector3(
            Mathf.RoundToInt(marker.lastKnownPosition.x) ^ Mathf.RoundToInt(xorKey.x),
            Mathf.RoundToInt(marker.lastKnownPosition.y) ^ Mathf.RoundToInt(xorKey.y),
            Mathf.RoundToInt(marker.lastKnownPosition.z) ^ Mathf.RoundToInt(xorKey.z)
        );
        return Vector3.Distance(encryptedCenter, encryptedPos) <= radius;
    });
}
}

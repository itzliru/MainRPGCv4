using VaultSystems.Invoker;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using VaultSystems.Data;
public class MapUIManager : MonoBehaviour
{
    [Header("Map UI Components")]
    public RectTransform mapContainer;
    public GameObject markerPrefab;
    public float mapScale = 0.1f; // World units to UI units
    public Vector2 mapCenterOffset = Vector2.zero;

    [Header("Marker Sprites")]
    public Sprite[] markerSprites; // Loaded from Resources

    [Header("Player Tracking")]
    public bool followPlayer = true;
    public float followSpeed = 5f;
    public bool showPlayerMarker = true;
    public Sprite playerMarkerSprite;
    public float playerMarkerScale = 1.2f;

    [Header("Camera Integration")]
    public bool scaleWithCamera = true;
    public float minScale = 0.05f;
    public float maxScale = 0.5f;
    public bool useOrthographicProjection = true;

    [Header("XOR Positioning")]
    public bool useXORPositioning = true;
    public Vector3 xorKey = new Vector3(12345f, 67890f, 11111f); // XOR encryption key

    [HideInInspector]
    public Dictionary<WorldMarker, RectTransform> markerUIElements = new();
    private Camera mainCamera;
    private PlayerDataContainer playerData;
    private WorldMarker playerMarker;
    private Transform playerTransform;
    private string currentCellId = "Overworld";

    void Awake()
    {
        mainCamera = Camera.main;
        LoadMarkerSprites();
        InitializePlayerTracking();
        RefreshMapMarkers();
    }

    void Start()
    {
        if (showPlayerMarker)
        {
            CreatePlayerMarker();
        }
    }

    /// <summary>
    /// Load marker sprites from Resources folder
    /// </summary>
    private void LoadMarkerSprites()
    {
        if (markerSprites == null || markerSprites.Length == 0)
        {
            // Load all sprites from Resources/MapMarkers folder
            markerSprites = Resources.LoadAll<Sprite>("MapMarkers");
            if (markerSprites.Length == 0)
            {
                Debug.LogWarning("[MapUIManager] No marker sprites found in Resources/MapMarkers. Please add sprites to this folder.");
            }
            else
            {
                Debug.Log($"[MapUIManager] Loaded {markerSprites.Length} marker sprites from Resources.");
            }
        }
    }

    void Update()
    {
        UpdatePlayerTracking();
        UpdateCameraIntegration();
        UpdateMarkerPositions();
    }

    /// <summary>
    /// Refresh all markers on the map
    /// </summary>
    public void RefreshMapMarkers()
    {
        // Clear existing UI markers
        foreach (var uiElement in markerUIElements.Values)
        {
            Destroy(uiElement.gameObject);
        }
        markerUIElements.Clear();

        // Create UI markers for all registered world markers
        foreach (var worldMarker in MarkerSystem.GetAllMarkers())
        {
            CreateUIMarker(worldMarker);
        }
    }

    /// <summary>
    /// Create marker for existing world object using UID
    /// </summary>
    public void CreateMarkerForObject(string uid, string cellId, int spriteIndex = 0)
    {
        var obj = UniqueId.FindById(uid);
        if (obj == null) return;

        var marker = obj.gameObject.GetComponent<WorldMarker>();
        if (marker == null)
        {
            marker = obj.gameObject.AddComponent<WorldMarker>();
        }

        marker.markerId = uid;
        marker.cellId = cellId;
        marker.displayName = obj.name;

        // Assign sprite from array if available
        if (markerSprites != null && spriteIndex >= 0 && spriteIndex < markerSprites.Length)
        {
            marker.icon = markerSprites[spriteIndex];
        }

        // Force refresh to show new marker
        RefreshMapMarkers();
    }

    /// <summary>
    /// Create UI representation for a world marker
    /// </summary>
    private void CreateUIMarker(WorldMarker worldMarker)
    {
        if (markerPrefab == null || mapContainer == null) return;

        GameObject uiMarkerObj = Instantiate(markerPrefab, mapContainer);
        RectTransform uiMarker = uiMarkerObj.GetComponent<RectTransform>();

        // Set up marker appearance - use worldMarker.icon first, fallback to array
        Image markerImage = uiMarkerObj.GetComponent<Image>();
        if (markerImage != null)
        {
            if (worldMarker.icon != null)
            {
                markerImage.sprite = worldMarker.icon;
            }
            else if (markerSprites != null && markerSprites.Length > 0)
            {
                // Default to first sprite if no specific icon set
                markerImage.sprite = markerSprites[0];
            }
        }

        // Add click handler for individual toggle
        Button markerButton = uiMarkerObj.GetComponent<Button>();
        if (markerButton == null)
        {
            markerButton = uiMarkerObj.AddComponent<Button>();
        }
        markerButton.onClick.AddListener(() => ToggleMarker(worldMarker));

        markerUIElements[worldMarker] = uiMarker;
        UpdateMarkerPosition(worldMarker);
    }

    /// <summary>
    /// Update positions of all UI markers
    /// </summary>
    private void UpdateMarkerPositions()
    {
        foreach (var kvp in markerUIElements)
        {
            UpdateMarkerPosition(kvp.Key);
        }
    }

    /// <summary>
    /// Update position of a specific UI marker
    /// </summary>
    private void UpdateMarkerPosition(WorldMarker worldMarker)
    {
        if (!markerUIElements.TryGetValue(worldMarker, out RectTransform uiMarker))
            return;

        Vector3 worldPos = worldMarker.lastKnownPosition;

        // Apply XOR transformation if enabled
        if (useXORPositioning)
        {
            worldPos = ApplyXORPosition(worldPos);
        }

        // Convert world position to UI position
        Vector2 uiPos = WorldToMapPosition(worldPos);
        uiMarker.anchoredPosition = uiPos;

        // Update visibility
        uiMarker.gameObject.SetActive(worldMarker.isVisible);
    }

    /// <summary>
    /// Convert world position to map UI position
    /// </summary>
    private Vector2 WorldToMapPosition(Vector3 worldPos)
    {
        // Simple orthographic projection (adjust for your camera setup)
        Vector2 mapPos = new Vector2(worldPos.x, worldPos.z) * mapScale;
        return mapPos + mapCenterOffset;
    }

    /// <summary>
    /// Apply XOR transformation to position
    /// </summary>
    private Vector3 ApplyXORPosition(Vector3 position)
{
    int xBits = System.BitConverter.SingleToInt32Bits(position.x) ^ System.BitConverter.SingleToInt32Bits(xorKey.x);
    int yBits = System.BitConverter.SingleToInt32Bits(position.y) ^ System.BitConverter.SingleToInt32Bits(xorKey.y);
    int zBits = System.BitConverter.SingleToInt32Bits(position.z) ^ System.BitConverter.SingleToInt32Bits(xorKey.z);

    return new Vector3(
        System.BitConverter.Int32BitsToSingle(xBits),
        System.BitConverter.Int32BitsToSingle(yBits),
        System.BitConverter.Int32BitsToSingle(zBits)
    );
}

    /// <summary>
    /// Individual marker toggle (recommended over global toggle)
    /// </summary>
    private void ToggleMarker(WorldMarker marker)
    {
        marker.ToggleVisibility();
        UpdateMarkerPosition(marker); // Immediate UI update
    }

    /// <summary>
    /// Toggle all markers in a cell (cell-based toggle)
    /// </summary>
    public void ToggleCellMarkers(string cellId)
    {
        var cellMarkers = MarkerSystem.GetMarkersInCell(cellId);
        foreach (var marker in cellMarkers)
        {
            marker.ToggleVisibility();
        }
        UpdateMarkerPositions();
    }

    /// <summary>
    /// Set XOR key for position encryption/decryption
    /// </summary>
    public void SetXORKey(Vector3 newKey)
    {
        xorKey = newKey;
        UpdateMarkerPositions(); // Recalculate all positions
    }

    /// <summary>
    /// Initialize player tracking using PlayerDataContainer
    /// </summary>
    private void InitializePlayerTracking()
    {
        // Find player data container
        var playerContainers = FindObjectsOfType<PlayerDataContainer>();
        playerData = playerContainers.FirstOrDefault(pdc => pdc.isActivePlayer);

        if (playerData != null)
        {
            playerTransform = playerData.transform;
            Debug.Log($"[MapUIManager] Found active player: {playerData.displayName}");
        }
        else
        {
            Debug.LogWarning("[MapUIManager] No active player found for tracking");
        }
    }

    /// <summary>
    /// Update player tracking and map centering
    /// </summary>
    private void UpdatePlayerTracking()
    {
        if (!followPlayer || playerData == null || playerTransform == null) return;

        // Update player marker position if it exists
        if (playerMarker != null)
        {
            playerMarker.lastKnownPosition = playerTransform.position;
        }

        // Update map center to follow player
        Vector3 playerPos = playerTransform.position;

        // Apply XOR if enabled
        if (useXORPositioning)
        {
            playerPos = ApplyXORPosition(playerPos);
        }

        // Convert to map space and center
        Vector2 targetCenter = WorldToMapPosition(playerPos);
        mapCenterOffset = Vector2.Lerp(mapCenterOffset, -targetCenter, Time.deltaTime * followSpeed);
    }

    /// <summary>
    /// Update camera integration (scaling, projection)
    /// </summary>
    private void UpdateCameraIntegration()
    {
        if (mainCamera == null) return;

        // Scale with camera zoom if enabled
        if (scaleWithCamera)
        {
            float cameraScale = useOrthographicProjection && mainCamera.orthographic ?
                mainCamera.orthographicSize :
                Mathf.Tan(mainCamera.fieldOfView * 0.5f * Mathf.Deg2Rad) * mainCamera.transform.position.y;

            mapScale = Mathf.Clamp(cameraScale * 0.1f, minScale, maxScale);
        }
    }

    /// <summary>
    /// Create player marker for tracking
    /// </summary>
    private void CreatePlayerMarker()
    {
        if (playerData == null || playerTransform == null) return;

        // Create or get existing player marker
        playerMarker = playerTransform.gameObject.GetComponent<WorldMarker>();
        if (playerMarker == null)
        {
            playerMarker = playerTransform.gameObject.AddComponent<WorldMarker>();
        }

        playerMarker.markerId = "player_marker";
        playerMarker.displayName = playerData.displayName;
        playerMarker.icon = playerMarkerSprite ?? (markerSprites.Length > 0 ? markerSprites[0] : null);
        playerMarker.isVisible = true;
        playerMarker.cellId = playerData.lastCellId;

        Debug.Log($"[MapUIManager] Created player marker for {playerData.displayName}");
    }

    /// <summary>
    /// Get current player position from PlayerDataContainer
    /// </summary>
    public Vector3 GetPlayerPosition()
    {
        return playerData != null ? playerData.lastKnownPosition : Vector3.zero;
    }

    /// <summary>
    /// Get current player cell from PlayerDataContainer
    /// </summary>
    public string GetPlayerCell()
    {
        return playerData != null ? playerData.lastCellId : "Overworld";
    }

    /// <summary>
    /// Reset map state when entering a new cell/scene
    /// Called by StreamingCellManager when transitioning cells
    /// </summary>
    public void OnCellTransition(string newCellId)
    {
        currentCellId = newCellId;

        // Reset map center offset to prevent carry-over from previous cell
        mapCenterOffset = Vector2.zero;

        // Reset camera scale to default
        mapScale = 0.1f;

        // Clear all existing markers (they belong to previous cell)
        foreach (var uiElement in markerUIElements.Values)
        {
            Destroy(uiElement.gameObject);
        }
        markerUIElements.Clear();

        // Reinitialize player tracking for new cell
        InitializePlayerTracking();

        // Refresh markers for new cell
        RefreshMapMarkers();

        // Recreate player marker if enabled
        if (showPlayerMarker)
        {
            CreatePlayerMarker();
        }

        Debug.Log($"[MapUIManager] Reset for new cell: {newCellId}");
    }

    /// <summary>
    /// Subscribe to cell transition events from StreamingCellManager
    /// </summary>
    private void OnEnable()
    {
        // Subscribe to cell transition events if StreamingCellManager exists
        if (StreamingCellManager.Instance != null)
        {
            StreamingCellManager.Instance.OnCellTransition += OnCellTransition;
        }
    }

    private void OnDisable()
    {
        // Unsubscribe from events
        if (StreamingCellManager.Instance != null)
        {
            StreamingCellManager.Instance.OnCellTransition -= OnCellTransition;
        }
    }
}



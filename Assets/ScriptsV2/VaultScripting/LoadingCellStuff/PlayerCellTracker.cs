using UnityEngine;
using VaultSystems.Data;
using System.Linq;
using VaultSystems.Containers;
using VaultSystems.Invoker;
using System.Collections;
using System.Collections.Generic;
using System;


public class PlayerCellTracker : MonoBehaviour
{
    [Header("References")]
    public PlayerDataContainer playerData;

    [Header("Grid Settings")]
    public int gridWidth = 8;
    public int gridHeight = 8;
    public float cellSize = 10f;

    private int[] cellFlags;  // Bitmask per cell
    private const int OCCUPIED = 0;
    private const int VISIBLE = 1;
    private const int ACTIVE = 2;
    private IDisposable _cellChangedSubscription;
    private int currentCellIndex = -1;

    void Awake()
    {
        cellFlags = new int[gridWidth * gridHeight];

        // Subscribe once here (lambda for handler)
        _cellChangedSubscription = EventDataContainer.SubscribeTo(
            EventKeys.Scene.CELL_CHANGED,  // Fixed to Scene key per your system
            args => OnCellChanged(args)
        );

        // Fallback if playerData null
        if (playerData == null)
            playerData = FindObjectOfType<PlayerDataContainer>();
    }

    void Update()
    {
        if (playerData == null)
            return;

        int newCellIndex = GetCellIndexFromPosition(playerData.lastKnownPosition);

        // Detect cell transition
        if (newCellIndex != currentCellIndex)
        {
            if (currentCellIndex >= 0)
                ClearCell(currentCellIndex, OCCUPIED);

            currentCellIndex = newCellIndex;
            SetCell(currentCellIndex, OCCUPIED);

            // Update lastCellId to reflect change
            playerData.lastCellId = $"Cell_{currentCellIndex}";


            // Broadcast change (decoupled via bridge)
            WorldBridgeSystem.Instance?.InvokeKey(EventKeys.Scene.CELL_CHANGED, currentCellIndex);



        }
    }

    private void OnCellChanged(object[] args)
    {
        // Handle cell change logic here (e.g., update visibility flags)
        if (args.Length > 0 && args[0] is int cellIndex)
        {
            SetCell(cellIndex, VISIBLE | ACTIVE);  // Example: Mark as visible/active
            Debug.Log($"[PlayerCellTracker] Cell changed to {cellIndex}");
        }
    }
    private int GetCellIndexFromPosition(Vector3 position)
    {
        int x = Mathf.Clamp((int)(position.x / cellSize), 0, gridWidth - 1);
        int y = Mathf.Clamp((int)(position.z / cellSize), 0, gridHeight - 1);
        return y * gridWidth + x;
    }

    private void SetCell(int index, int flag)
    {
        cellFlags[index] |= (1 << flag);
    }

    private void ClearCell(int index, int flag)
    {
        cellFlags[index] &= ~(1 << flag);
    }

    private bool CellHasFlag(int index, int flag)
    {
        return (cellFlags[index] & (1 << flag)) != 0;
    }


    void OnDestroy()
    {
        _cellChangedSubscription?.Dispose();
        _cellChangedSubscription = null;
    }
    // Public method to get cell state per scene
    public int[] GetCellPerSceneState()
    {
        return (int[])cellFlags.Clone();
    }

}


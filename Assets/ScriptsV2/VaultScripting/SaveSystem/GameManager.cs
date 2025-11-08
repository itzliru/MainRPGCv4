using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using VaultSystems.Data;
using System;
using System.Linq;
using VaultSystems.Invoker;     
using VaultSystems.Controllers;
[DefaultExecutionOrder(-51)]
public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public int currentSlot = 0;
    public GameObject playerObject;
    public Transform playerTransform => playerObject?.transform;
    public bool isGameLoaded = false;

    private void Awake()
    {
        if (Instance == null) 
        { 
            Instance = this; 
            DontDestroyOnLoad(gameObject); 
        }
        else 
        { 
            Destroy(gameObject); 
        }
    }

       private void Start()
    {
        // Automatically load from default slot (optional)
        // LoadGame();
    }

    // ---------- SAVE ----------
    public void SaveGame()
    {
        string currentScene = SceneManager.GetActiveScene().name;

        QuantumArchive archive = new QuantumArchive();
        var playerData = PlayerRuntimeInitializer.Instance?.GetActiveData();  // ✅ USE NEW METHOD
        
        if (playerData != null)
        {
            var playerInstance = PlayerRuntimeInitializer.Instance?.GetPlayerInstance();  // ✅ USE NEW METHOD
            if (playerInstance != null)
            {
                playerData.lastKnownPosition = playerInstance.transform.position;
                playerData.lastScene = currentScene;
            }
            archive.playerData.lastSpawnPointId = playerData.lastSpawnPointId;
            // Character type switch (same as PlayerRuntimeInitializer.GetOutfits())
            string charType = playerData switch
            {
                LiraData => "Lira",
                KinueeData => "Kinuee",
                HosData => "Hos",
                _ => "Unknown"
            };
            
            archive.playerData.playerDataHex = HexSerializationHelper.ToHex(playerData);
            archive.playerData.playerId = playerData.playerId;
            archive.playerData.displayName = playerData.displayName;
            archive.playerData.characterType = charType;
            archive.playerData.outfitIndex = playerData.outfitIndex;
            archive.playerData.lastPosition = playerData.lastKnownPosition;
            archive.playerData.lastScene = playerData.lastScene;
            
            Debug.Log($"[GameManager] Player saved: {charType} - {playerData.displayName}");
        }

        if (!string.IsNullOrEmpty(currentScene))
        {
            PersistentWorldManager.Instance.SaveCurrentSceneToCell(currentScene);
        }

        if (StreamingCellManager.Instance != null)
        {
            StreamingCellManager.Instance.ExitCell();
        }

        archive.cells = PersistentWorldManager.Instance.GetAllCellData();
        PersistentWorldManager.Instance.SaveAllWorlds(currentSlot);

        ChronoVaultXML.LockVault(archive, currentSlot);

        Debug.Log($"[GameManager] ✅ Complete save: Player + World to slot {currentSlot}");

        // Debug breakdown
        int totalCells = archive.cells.Count;
        int totalObjects = 0;
        int totalDataContainers = 0;

        foreach (var kvp in archive.cells)
        {
            string sceneName = kvp.Key;
            var container = kvp.Value;

            int objCount = container.objects != null ? container.objects.Count : 0;
            int dataCount = container.dataContainers != null ? container.dataContainers.Count : 0;

            totalObjects += objCount;
            totalDataContainers += dataCount;

            Debug.Log($"[SaveDebug] Scene '{sceneName}' → {objCount} objects, {dataCount} data containers.");
        }

        Debug.Log($"[GameManager] ✅ Saved to slot {currentSlot} | {totalCells} scenes | {totalObjects} objects | {totalDataContainers} containers");
    }

    public void SaveGameSlot(int slot)
    {
        currentSlot = slot;
        SaveGame();
    }

    // ---------- LOAD ----------
    public void LoadGame()
    {
        QuantumArchive archive = ChronoVaultXML.UnlockVault<QuantumArchive>(currentSlot);

        if (archive == null)
        {
            Debug.LogWarning($"[GameManager] No save found in slot {currentSlot}");
            return;
        }

        // === RESTORE PLAYER ===
        if (archive.playerData != null && !string.IsNullOrEmpty(archive.playerData.playerDataHex))
        {
            try
            {
                GameObject[] outfits = GetOutfitsForCharacterType(archive.playerData.characterType);
                
                if (outfits.Length == 0)
                {
                    Debug.LogError($"[GameManager] No outfits for {archive.playerData.characterType}");
                    return;
                }

                int outfitIdx = Mathf.Clamp(archive.playerData.outfitIndex, 0, outfits.Length - 1);
                GameObject prefab = outfits[outfitIdx];

                GameObject playerInstance = Instantiate(prefab, archive.playerData.lastPosition, Quaternion.identity);
                playerInstance.name = $"{archive.playerData.displayName}_Player";
                DontDestroyOnLoad(playerInstance);

                var playerData = playerInstance.GetComponent<PlayerDataContainer>();
                if (playerData != null)
                {
                    HexSerializationHelper.FromHex(playerData, archive.playerData.playerDataHex);
                    playerData.outfitIndex = archive.playerData.outfitIndex;

                    var uid = playerInstance.GetComponent<UniqueId>() ?? playerInstance.AddComponent<UniqueId>();
                    uid.isDataContainer = true;
                    uid.manualId = playerData.playerId;

                    DataContainerManager.Instance?.Register(playerData);
                    
                    // ✅ FIX: Use fully qualified name to avoid ambiguity
                    WorldBridgeSystem.Instance?.RegisterID("player", playerInstance.GetComponent<PlayerController>());
                    WorldBridgeSystem.Instance?.RegisterID(uid.GetID(), playerInstance);

                    playerObject = playerInstance;

                    Debug.Log($"[GameManager] ✅ Restored {archive.playerData.characterType}: {archive.playerData.displayName} (outfit #{archive.playerData.outfitIndex})");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[GameManager] Failed to restore player: {ex}");
            }
        }

        // === RESTORE WORLD ===
        if (archive.cells != null && archive.cells.Count > 0)
        {
            PersistentWorldManager.Instance.RestoreCells(archive.cells);
            Debug.Log($"[GameManager] ✅ Loaded all world cells from slot {currentSlot}");

            string currentScene = SceneManager.GetActiveScene().name;
            var sceneContainer = PersistentWorldManager.Instance.GetWorldStateForScene(currentScene);

            if (sceneContainer != null)
            {
                sceneContainer.RestoreAll();
                sceneContainer.RestoreDataContainers();
                
                if (StreamingCellManager.Instance != null)
                {
                    var dynamicStates = PersistentWorldManager.Instance.GetDynamicStatesForScene(currentScene);
                    DynamicObjectManager.Instance?.RestoreAll(dynamicStates);
                }

                var ecsContainer = PersistentWorldManager.Instance.GetECSContainerForScene(currentScene);
                if (ecsContainer != null && ECSWorldBridge.Instance != null)
                {
                    ecsContainer.RestoreAll(ECSWorldBridge.Instance);
                    Debug.Log($"[GameManager] ECS entities restored for {currentScene}");
                }

                // ✅ FIX: Use UnityEngine.Object to avoid ambiguity
                foreach (var dataContainer in UnityEngine.Object.FindObjectsOfType<BaseDataContainer>())
                {
                    if (dataContainer != null)
                        DataContainerManager.Instance?.Register(dataContainer);
                }

                Debug.Log($"[GameManager] Scene '{currentScene}' objects restored");
            }
        }

        isGameLoaded = true;
    }

    public void LoadGameSlot(int slot)
    {
        currentSlot = slot;
        LoadGame();
    }

    // ✅ HELPER: Get outfits for character type
    private GameObject[] GetOutfitsForCharacterType(string characterType)
    {
        const string OUTFIT_RESOURCE_PATH = "Outfits/";
        
        var loaded = Resources.LoadAll<GameObject>($"{OUTFIT_RESOURCE_PATH}{characterType}")?.ToArray() ?? System.Array.Empty<GameObject>();
        
        if (loaded.Length == 0)
            Debug.LogWarning($"[GameManager] No outfits found at Resources/Outfits/{characterType}/");
        
        return loaded;
    }
}

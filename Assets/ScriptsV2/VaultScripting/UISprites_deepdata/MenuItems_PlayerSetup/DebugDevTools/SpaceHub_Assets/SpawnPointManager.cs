using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VaultSystems.Data;

namespace VaultSystems.Data
{
    [DefaultExecutionOrder(4)]
    public class SpawnPointManager : MonoBehaviour
    {
        private const string DEFAULT_SPAWN_ID = "player_default";
        
        public static SpawnPointManager Instance { get; private set; }
        
        private Dictionary<string, SpawnPointData> spawnRegistry = new();
        private string currentSceneName;

        [System.Serializable]
        public struct SpawnPointData
        {
            public string spawnId;
            public Vector3 position;
            public Quaternion rotation;
            public string sceneName;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            RegisterSpawnPoints();
        }

        private void RegisterSpawnPoints()
        {
            spawnRegistry.Clear();
            
            var allSpawnPoints = FindObjectsOfType<PlayerSpawnPoint>(includeInactive: true);
            
            if (allSpawnPoints.Length == 0)
            {
                Debug.LogWarning("[SpawnPointManager] No spawn points found in scene. Using default position.");
                return;
            }

            foreach (var sp in allSpawnPoints)
            {
                string spawnId = sp.spawnId ?? DEFAULT_SPAWN_ID;
                var data = new SpawnPointData
                {
                    spawnId = spawnId,
                    position = sp.transform.position,
                    rotation = sp.transform.rotation,
                    sceneName = currentSceneName
                };
                spawnRegistry[spawnId] = data;
            }

            Debug.Log($"[SpawnPointManager] Registered {spawnRegistry.Count} spawn points in scene '{currentSceneName}'");
        }

        public SpawnPointData GetSpawnPoint(string spawnId = null)
        {
            spawnId ??= DEFAULT_SPAWN_ID;

            if (spawnRegistry.TryGetValue(spawnId, out var spawnData))
            {
                Debug.Log($"[SpawnPointManager] Retrieved spawn point '{spawnId}' at {spawnData.position}");
                return spawnData;
            }

            Debug.LogWarning($"[SpawnPointManager] Spawn point '{spawnId}' not found. Using default position.");
            return GetDefaultSpawnPoint();
        }

        private SpawnPointData GetDefaultSpawnPoint()
        {
            if (spawnRegistry.TryGetValue(DEFAULT_SPAWN_ID, out var defaultSpawn))
                return defaultSpawn;

            var firstSpawn = spawnRegistry.Values.FirstOrDefault();
            if (firstSpawn.spawnId != null)
                return firstSpawn;

            return new SpawnPointData
            {
                spawnId = DEFAULT_SPAWN_ID,
                position = Vector3.zero,
                rotation = Quaternion.identity,
                sceneName = currentSceneName
            };
        }

        public Vector3 GetSpawnPosition(string spawnId = null)
        {
            return GetSpawnPoint(spawnId).position;
        }

        public Quaternion GetSpawnRotation(string spawnId = null)
        {
            return GetSpawnPoint(spawnId).rotation;
        }

        public string[] GetAllSpawnIds()
        {
            return spawnRegistry.Keys.ToArray();
        }

        public void SaveSpawnPointToData(PlayerDataContainer data, string spawnId)
        {
            if (data == null) return;
            data.lastSpawnPointId = spawnId ?? DEFAULT_SPAWN_ID;
            data.MarkDirty();
            Debug.Log($"[SpawnPointManager] Saved spawn point '{data.lastSpawnPointId}' to player data");
        }

        public string LoadSpawnPointFromData(PlayerDataContainer data)
{
    if (data == null) return DEFAULT_SPAWN_ID;
    
    var savedSpawnId = data.lastSpawnPointId ?? DEFAULT_SPAWN_ID;
    
    if (spawnRegistry.ContainsKey(savedSpawnId))
        return savedSpawnId;  // Spawn exists in this cell
    
    Debug.LogWarning($"[SpawnPointManager] Saved spawn '{savedSpawnId}' not in cell '{currentSceneName}', using fallback");
    return DEFAULT_SPAWN_ID;  // Fallback to default
}


        public void OnSceneLoaded(string newScene)
        {
            if (newScene != currentSceneName)
            {
                currentSceneName = newScene;
                RegisterSpawnPoints();
            }
        }

        public void ClearRegistry()
        {
            spawnRegistry.Clear();
            Debug.Log("[SpawnPointManager] Spawn registry cleared");
        }
    }
}

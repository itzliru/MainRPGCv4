using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using VaultSystems.Data;
using VaultSystems.Invoker;
using VaultSystems.Controllers;
using VaultSystems.Containers;

namespace VaultSystems.Data
{
    [DefaultExecutionOrder(5)]




    public class PlayerRuntimeInitializer : MonoBehaviour
    {
        private const string OUTFIT_RESOURCE_PATH = "Outfits/";
        private const string PLAYER_INVOKER_KEY = "player.OnExit";
        #region Inspector
        [Header("UI")]
        public Text hpText;
        public Text levelText;
        public Text xpText;

        [Header("Mode")]
        public PlayerStartMode startMode = PlayerStartMode.Fresh;
        public enum PlayerStartMode { Fresh, Restore }
        #endregion

        #region Runtime State
        
        private PlayerDataContainer activeData;
        public static PlayerRuntimeInitializer Instance { get; private set; }
        private PlayerCaseController CaseController;
        private GameObject playerInstance;
        private Transform playerRoot;
        private IDisposable saveInvokerToken;
        private IDisposable _saveOnExitToken;
        private static PlayerDataContainer cachedActiveData;
        private const int MaxOutfits = 8;

        private GameObject pooledPlayerInstance;       
        private PlayerEventDataContainer playerEvents;
        private PlayerWeaponEventDataContainer weaponEvents;
        private readonly Dictionary<string, GameObject[]> outfitCache = new();

                /// <summary>
        /// Gets the active player data container (safe access from GameManager)
        /// </summary>
        public PlayerDataContainer GetActiveData() => activeData;

        /// <summary>
        /// Gets the current player instance GameObject
        /// </summary>
        public GameObject GetPlayerInstance() => playerInstance;

        /// <summary>
        /// Gets the player's root transform
        /// </summary>
        public Transform GetPlayerRoot() => playerRoot;

        #endregion

        #region Unity Lifecycle
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
            try
            {


                ResolveActiveData();

                CaseController = FindObjectOfType<PlayerCaseController>(true);
                if (CaseController == null)
                {
                    Debug.LogWarning("[PlayerRuntimeInitializer] No PlayerCaseController found in the scene. Player functionality may be limited.");
                }
                          
                // Spawn / restore the player
                if (startMode == PlayerStartMode.Fresh) SpawnPlayer();
                else RestorePlayer();

                // One-time invokers (only after the player exists)
                RegisterInvokers();
                RegisterPlayerEvents();
                StartCoroutine(DeferredGCCollect());
                SceneManager.sceneUnloaded += OnSceneUnload;

            }
            catch (Exception ex)
            {
                Debug.LogError($"[{nameof(PlayerRuntimeInitializer)}] Init failed: {ex}");
            }
        }
        private IEnumerator DeferredGCCollect()
        {
            yield return null; // wait 1 frame
            CaseController?.InvokeMethod(() =>
            {
                System.GC.Collect();
                Debug.Log("[PlayerRuntimeInitializer] Deferred GC.Collect() executed after startup frame.");
            });
        }
        private void OnDestroy()
        {
            if (playerInstance != null)
            {
                var uid = playerInstance.GetComponent<UniqueId>();
                WorldBridgeSystem.Instance?.UnregisterID(uid?.GetID());
                WorldBridgeSystem.Instance?.UnregisterID("player");
            }
            playerEvents = null;
            weaponEvents = null;
            pooledPlayerInstance = null;
           SceneManager.sceneUnloaded -= OnSceneUnload;
    _saveOnExitToken?.Dispose();
            _saveOnExitToken = null;
    weaponEvents = null;
        }
        #endregion

        #region Core Logic
        private void ResolveActiveData()
        {
            if (cachedActiveData != null && cachedActiveData.isActivePlayer)
            {
                activeData = cachedActiveData;
                return;
            }

            activeData = FindObjectsOfType<PlayerDataContainer>(true)
            .FirstOrDefault(data => data.isActivePlayer);

            if (activeData == null)
                throw new InvalidOperationException("No active PlayerDataContainer found.");

            cachedActiveData = activeData;
            // Moved LinkDataToPlayer to after playerInstance creation - no early call here
        }

        private void SpawnPlayer()
        {
            var (prefab, pos, rot) = PreparePrefabAndPosition(useSaved: false);
            CreatePlayerInstance(prefab, pos, rot);
            RegisterPlayerWithWorld();
            LinkAndUI();
        }

        private void RestorePlayer()
        {


            var (prefab, pos, rot) = PreparePrefabAndPosition(useSaved: true);
            CreatePlayerInstance(prefab, pos, rot);

            // Restore transform + data from PersistentWorldManager
            RestorePlayerState();

            RegisterPlayerWithWorld();
            LinkAndUI();

            // If the saved scene differs → async load it *after* the player exists
            if (!string.IsNullOrEmpty(activeData.lastScene) &&
                activeData.lastScene != SceneManager.GetActiveScene().name)
            {
                StartCoroutine(LoadSavedSceneAndRestore(activeData.lastScene));
            }
        }

        private (GameObject prefab, Vector3 pos, Quaternion rot) PreparePrefabAndPosition(bool useSaved)
        {
            var outfits = GetOutfits();
            int idx = Mathf.Clamp(activeData.outfitIndex, 0, outfits.Length - 1);
            var prefab = outfits[idx];
          
            Debug.Log($"Spawned player activedata outfit index: {activeData.outfitIndex} using prefab {prefab.name}");
            Vector3 pos;
            Quaternion rot;
    
              if (useSaved && activeData.lastKnownPosition != Vector3.zero)
                {
                    pos = activeData.lastKnownPosition;
                        rot = Quaternion.identity;
                }
                else if (SpawnPointManager.Instance != null)
                {
                string spawnId = useSaved ? activeData.lastSpawnPointId : "player_default";
                    pos = SpawnPointManager.Instance.GetSpawnPosition(spawnId);
                        rot = SpawnPointManager.Instance.GetSpawnRotation(spawnId);
                }
                else
                {
                Debug.LogWarning("[PreparePrefabAndPosition] SpawnPointManager not found, using default position");
                     pos = Vector3.zero;
                         rot = Quaternion.identity;
             }
    
             return (prefab, pos, rot);
        }

        private void CreatePlayerInstance(GameObject prefab, Vector3 pos, Quaternion rot)
        {

            // 🔹 If we already have a pooled player, reuse it
            if (pooledPlayerInstance != null)
{
    playerInstance = pooledPlayerInstance;
    playerInstance.transform.SetPositionAndRotation(pos, rot);
    
 (playerInstance.GetComponent<PlayerController>() 
 ?? playerInstance.GetComponentInChildren<PlayerController>())
    ?.ResetPlayerState();

(playerInstance.GetComponent<PlayerAnimator1>() 
 ?? playerInstance.GetComponentInChildren<PlayerAnimator1>())
    ?.ResetAnimatorState();

    
    playerInstance.SetActive(true);
    Debug.Log($"[PlayerInit] Reusing pooled player, state reset");
    return;
}


            playerInstance = Instantiate(prefab, pos, rot);
            playerInstance.name = $"{activeData.displayName}_Player";
            playerRoot = playerInstance.transform;

            pooledPlayerInstance = playerInstance; // Cache AFTER creation
            DontDestroyOnLoad(playerInstance);



            var ctrl = playerInstance.GetComponent<PlayerController>() ?? playerInstance.AddComponent<PlayerController>();
           
            if (ctrl.activeCamera == null)
                ctrl.activeCamera = playerInstance.GetComponentInChildren<Camera>();
            
            var ragdoll = playerInstance.GetComponent<RagdollController>();
            if (ragdoll == null)
            {
                Debug.LogWarning($"[PlayerInit] {playerInstance.name} has no RagdollController. Add it to the prefab!");
            }

            // UniqueId
            var uid = playerInstance.GetComponent<UniqueId>() ?? playerInstance.AddComponent<UniqueId>();
            uid.isDataContainer = true;
          // Use the ACTUAL saved playerId, not the property
            uid.manualId = !string.IsNullOrEmpty(activeData.playerId) 
            ? activeData.playerId 
            : GenerateFallbackId();

            activeData.playerId = uid.manualId;
            Debug.Log($"[PlayerInit] Instantiated +++++ All Setup {playerInstance.name} @ {pos}");

        }


        private string GenerateFallbackId()
        {
            return activeData switch
            {
                LiraData l => l.DefaultPlayerId,
                KinueeData k => k.DefaultPlayerId,
                HosData h => h.DefaultPlayerId,
                _ => $"unknown_{Guid.NewGuid():N}"
            };
        }

        private void RegisterPlayerWithWorld()
        {
            var uid = playerInstance.GetComponent<UniqueId>();
            var ctrl = playerInstance.GetComponent<PlayerController>();
            
            // DataContainer side
            DataContainerManager.Instance?.Register(activeData);

            // WorldBridge side
            WorldBridgeSystem.Instance?.RegisterID("player", ctrl);
            WorldBridgeSystem.Instance?.RegisterID(uid.GetID(), playerInstance);

            // PersistentWorldManager side (both object + data)
            var container = PersistentWorldManager.Instance?.GetWorldStateForScene(SceneManager.GetActiveScene().name);
            container?.AddOrUpdateObject(playerInstance.GetComponent<MonoBehaviour>());
            container?.AddOrUpdateDataContainer(uid);

            //var weaponEventContainer = new PlayerWeaponEventDataContainer(playerData, gunController, "player");
            //weaponEventContainer.RegisterWithWorldBridge();

        }

        private void LinkAndUI()
        {
            LinkDataToPlayer(playerInstance, activeData);
            UpdateUI(activeData);
        }

private void RegisterPlayerEvents()
{
    // Register core player events
    playerEvents = new PlayerEventDataContainer(activeData, "player_main");
    playerEvents.RegisterWithWorldBridge();
    Debug.Log("[PlayerInit] Player events registered");
    
    // ===== NEW: Register weapon events =====
    var gunController = playerInstance.GetComponent<GunController>() 
        ?? playerInstance.GetComponentInChildren<GunController>();
    
    if (gunController != null)
    {
        // Create weapon event container
        weaponEvents = new PlayerWeaponEventDataContainer(
            activeData,           // PlayerDataContainer
            gunController,        // GunController
            "player_weapon"       // Event prefix
        );
        
        weaponEvents.RegisterWithWorldBridge();
        Debug.Log("[PlayerInit] Weapon events registered");
    }
    else
    {
        Debug.LogWarning("[PlayerInit] GunController not found on player");
    }
}




        #endregion
        #region Exit Save Handling + Invokers
private void OnSceneUnload(Scene scene)
{
    // EXPLICITLY invoke when leaving the scene
    WorldBridgeSystem.Instance?.InvokeKey("player.OnExit");
}


 
        
   private void OnEnable()
        {
            if (_saveOnExitToken == null)
            {
                _saveOnExitToken = WorldBridgeSystem.Instance?.RegisterInvoker(
                    "player.OnExit",
                    _ =>
                    {
                        if (activeData == null || playerInstance == null) return;

                        // ✅ Track position changes
                        Vector3 currentPos = playerInstance.transform.position;
                        if (currentPos != activeData.lastKnownPosition)
                        {
                            activeData.lastKnownPosition = currentPos;
                            activeData.MarkDirty();
                        }

                        // ✅ Track scene changes
                        string currentScene = SceneManager.GetActiveScene().name;
                        if (currentScene != activeData.lastScene)
                        {
                            activeData.lastScene = currentScene;
                            activeData.MarkDirty();
                        }

                        if (SpawnPointManager.Instance != null)
                            SpawnPointManager.Instance.SaveSpawnPointToData(activeData, "player_default");

                        activeData.MarkDirty();
                        Debug.Log($"[PlayerInit] OnExit saved scene {activeData.lastScene} at {currentPos}");
                    },
                    DynamicDictionaryInvoker.Layer.Func,
                    id: "SaveLastSceneAndPosition"
                );
            }
        }
        


        private void OnDisable()
        {
            _saveOnExitToken?.Dispose();
            _saveOnExitToken = null;
        }

        private void RegisterInvokers()
        {
            if (playerInstance == null)
            {
                Debug.LogWarning("[RegisterInvokers] No player instance found. Cannot register save invoker.");
                return;
            }
            var uid = playerInstance.GetComponent<UniqueId>();
            if (uid == null) return;

            
            

            // Regular save command
           // saveInvokerToken = WorldBridgeSystem.Instance?.RegisterInvoker(
            //    $"player.{uid.GetID()}.Save",
            //    _ => CaseController?.InvokeMethod(() => SavePlayerState()),
             //   DynamicDictionaryInvoker.Layer.Func,
             //   id: uid.GetID(),
             //   metadata: playerInstance
            //);



        }
        #endregion

        #region Helper Methods
        private GameObject[] GetOutfits()
        {
            string charName = activeData switch
            {
                LiraData => "Lira",
                KinueeData => "Kinuee",
                HosData => "Hos",
                _ => "Unknown"
            };

            if (charName == "Unknown")
            {
                Debug.LogError($"[GetOutfits] Unknown character type {activeData.GetType()}");
                return Array.Empty<GameObject>();
            }

            if (outfitCache.TryGetValue(charName, out var cached)) return cached;

           var loaded = Resources.LoadAll<GameObject>($"{OUTFIT_RESOURCE_PATH}{charName}")?.ToArray() ?? Array.Empty<GameObject>();
            if (loaded.Length == 0)
            {
                Debug.LogWarning($"[GetOutfits] No outfits under Resources/Outfits/{charName}");
                outfitCache[charName] = Array.Empty<GameObject>();
                return outfitCache[charName];
            }

            if (loaded.Length > MaxOutfits)
            {
                loaded = loaded.Take(MaxOutfits).ToArray();
                Debug.Log($"[GetOutfits] Trimmed {charName} outfits to {MaxOutfits}");
            }

            outfitCache[charName] = loaded;
            Debug.Log($"[GetOutfits] Cached {loaded.Length} outfits for {charName}");
            return loaded;
        }

        private void LinkDataToPlayer(GameObject player, PlayerDataContainer data)
        {
            if (!player || !data) return;
            var linker = player.GetComponent<PlayerStatsLinker>() ?? player.AddComponent<PlayerStatsLinker>();
            try { linker.Initialize(data); }
            catch (Exception ex) { Debug.LogError($"[LinkData] {ex}"); }
        }

        private void UpdateUI(PlayerDataContainer data)
        {
            if (!data) return;
            if (hpText) hpText.text = $"HP: {data.currentHP}/{data.maxHP}";
            if (levelText) levelText.text = $"Level: {data.level}";
            if (xpText) xpText.text = $"XP: {data.xp}";
        }

        private void RestorePlayerState()
        {
            if (!playerInstance) return;
            var uid = playerInstance.GetComponent<UniqueId>();
            if (!uid) return;

            var container = PersistentWorldManager.Instance?.GetWorldStateForScene(playerInstance.scene.name);
            if (container == null) return;

            container.objects.Find(o => o.uniqueId == uid.GetID())?.Restore();
            container.dataContainers.Find(d => d.uniqueId == uid.GetID())?.Restore();

            Debug.Log($"[RestorePlayerState] Restored {uid.GetID()}");
        }

        public void SavePlayerState()
        {
            if (!playerInstance) return;
            var uid = playerInstance.GetComponent<UniqueId>();
            if (!uid) return;

            // Update PlayerDataContainer
            if (activeData != null)
            {
                activeData.lastKnownPosition = playerInstance.transform.position;
                activeData.lastScene = SceneManager.GetActiveScene().name;
                activeData.MarkDirty();
            }

            var container = PersistentWorldManager.Instance?.GetWorldStateForScene(playerInstance.scene.name);
            container?.AddOrUpdateObject(playerInstance.GetComponent<MonoBehaviour>());
            container?.AddOrUpdateDataContainer(uid);

            Debug.Log($"[SavePlayerState] Saved {uid.GetID()}");
        }

        private IEnumerator LoadSavedSceneAndRestore(string targetScene)
        {
            var asyncOp = SceneManager.LoadSceneAsync(targetScene);
            asyncOp.allowSceneActivation = false;

            var lsm = LoadingScreenManager.Instance;
            lsm?.Show($"Loading {targetScene}...");
            while (asyncOp.progress < 0.9f)
            {
                lsm?.SetProgress(asyncOp.progress / 0.9f);
                yield return null;
            }

            asyncOp.allowSceneActivation = true;
            yield return asyncOp;

            // After the new scene is active, restore everything again
            RestorePlayerState();
            lsm?.Hide();
        }
        #endregion
    }
}
           
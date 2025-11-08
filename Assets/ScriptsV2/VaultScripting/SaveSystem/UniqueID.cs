
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using System.Collections;
using System.Security.Cryptography;
using VaultSystems.Invoker;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;
using VaultSystems.Data;
using VaultSystems.Errors;
using VaultSystems.Controllers;


namespace VaultSystems.Data
{
    [ExecuteAlways]
    [DefaultExecutionOrder(-35)]
    public class UniqueId : MonoBehaviour
    {
        [Header("Unique ID")]
        [SerializeField, ReadOnlyInspector]
        private string uniqueId;

        [Header("Settings")]
        public bool isNPC = false;
        public bool isDynamic = false;
        public bool isDataContainer = false;
        public bool skipUniqueID = false;

        [Header("Manual ID Override")]
        public string manualId;

        private static readonly Dictionary<string, UniqueId> allIds = new();
        private static readonly Dictionary<string, Dictionary<string, UniqueId>> sceneCache = new();
        public static IEnumerable<UniqueId> GetAllCached()
        {
            return allIds.Values;
        }
        public static UniqueId GetByID(string id)
        {
            allIds.TryGetValue(id, out var obj);
            return obj;
        }
        //Version 1.0 
        //Remove Logs in build
        private void Awake()
        {
            AssignIdIfMissing();
            if (Application.isPlaying)
            {

                if (isDataContainer && DataContainerManager.Instance != null)
                {
                    var container = GetComponent<BaseDataContainer>();
                    if (container != null)
                    {
                        DataContainerManager.Instance.Register(container);
                        Debug.Log($"[UniqueId] Registered data container {name} ({uniqueId}) with DataContainerManager");
                    }
                }
                else if (!isDataContainer && PersistentWorldManager.Instance != null)
                {
                    PersistentWorldManager.Instance.GetWorldStateForScene(gameObject.scene.name)
                        .AddOrUpdateObject(this);
                        Debug.Log($"[UniqueId] Registered {uniqueId} objects to PersistentWorldManager");
                }
                var worldBridge = VaultSystems.Invoker.WorldBridgeSystem.Instance;
                if (worldBridge != null)
                {
                    worldBridge.RegisterID(uniqueId, this);
                    Debug.Log($"[UniqueId] Registered {uniqueId} with WorldBridgeSystem ({name})");
                }
                if (PersistentWorldManager.Instance == null)
                Debug.LogError($"[UniqueId] Failed to Register {uniqueId} objects to PersistentWorldManager");
                
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!Application.isPlaying && string.IsNullOrEmpty(uniqueId) && !skipUniqueID)
            {
                AssignIdIfMissing(editorMode: true);
            }
        }
#endif

        private void AssignIdIfMissing(bool editorMode = false)
        {
            if (skipUniqueID)
                return;
            if (!string.IsNullOrEmpty(manualId))
            {
                CacheSelf();

                return;
            }


            // Auto-generate hex ID from name + scene
            string baseString = $"{gameObject.name}_{gameObject.scene.name}";
            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] hashBytes = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(baseString));
                uniqueId = $"0x{BitConverter.ToString(hashBytes).Replace("-", "").Substring(0, 8).ToLower()}"; // 8 chars for brevity
            }

            // Ensure uniqueness
            int collisionCount = 0; //this isnt supposed to happen
            string originalId = uniqueId;
            while (allIds.ContainsKey(uniqueId))
            {
                collisionCount++;
                uniqueId = $"{originalId}_{collisionCount:X}";
                Debug.LogWarning($"[UniqueId] Collision on {originalId}, using {uniqueId}");
            }

            if (!allIds.ContainsKey(uniqueId))
                allIds[uniqueId] = this;
            else
                allIds[uniqueId] = this; // Force update if conflict

#if UNITY_EDITOR
            if (editorMode)
                EditorUtility.SetDirty(this);
#endif
        }
        private void CacheSelf()
        {
            if (skipUniqueID || string.IsNullOrEmpty(uniqueId)) return;
            uniqueId = manualId;
            allIds[uniqueId] = this;

            string sceneName = gameObject.scene.name;
            if (!sceneCache.ContainsKey(sceneName))
                sceneCache[sceneName] = new Dictionary<string, UniqueId>();

            sceneCache[sceneName][uniqueId] = this;
        }



        private void OnDestroy()
        {
            if (skipUniqueID || string.IsNullOrEmpty(uniqueId))
                return;

            // 🔹 Remove from global + scene caches
            allIds.Remove(uniqueId);
            if (sceneCache.TryGetValue(gameObject.scene.name, out var sceneDict))
                sceneDict.Remove(uniqueId);

            var worldBridge = WorldBridgeSystem.Instance;
            worldBridge?.UnregisterID(uniqueId);
        }



        public string GetID() => uniqueId;

        public void SetID(string newId)
        {
            if (string.IsNullOrEmpty(newId)) return;
            if (allIds.ContainsKey(uniqueId))
                allIds.Remove(uniqueId);
            uniqueId = $"0x{newId.ToLower()}";
            allIds[uniqueId] = this;
#if UNITY_EDITOR
            if (!Application.isPlaying)
                EditorUtility.SetDirty(this);
#endif
        }

        public static UniqueId FindById(string id)
        {
            allIds.TryGetValue(id, out var obj);
            return obj;
        }

        public static void ClearAllIds()
        {
            allIds.Clear();
            sceneCache.Clear();
        }
    }

#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(ReadOnlyInspectorAttribute))]
    public class ReadOnlyInspectorDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            GUI.enabled = false;
            EditorGUI.PropertyField(position, property, label);
            GUI.enabled = true;
        }
    }

    public class ReadOnlyInspectorAttribute : PropertyAttribute { }
#endif

    /// <summary>
    /// UIDRegistry - central dispatcher for all UniqueId events and caches.
    /// Keeps track of scene-level and global UniqueIds and notifies subscribers.
    /// </summary>


    public class UIDRegistry : MonoBehaviour
    {
        // Singleton-like instance (non-static reference)
        public static UIDRegistry Instance { get; private set; }

        // Global + per-scene caches
        private readonly Dictionary<string, UniqueId> globalCache = new();
        private readonly Dictionary<string, Dictionary<string, UniqueId>> sceneCache = new();

        // Reactive events
        public event Action<UniqueId> OnRegistered;
        public event Action<UniqueId> OnDestroyed;
        public event Action<string> OnSceneLoaded;
        public event Action<string> OnSceneUnloaded;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[UIDRegistry] Duplicate instance detected; destroying extra one.");
                DestroyImmediate(this);
                return;
            }
     


            Instance = this;
            DontDestroyOnLoad(this);
         
            SceneManager.sceneLoaded += HandleSceneLoaded;
            SceneManager.sceneUnloaded += HandleSceneUnloaded;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                SceneManager.sceneLoaded -= HandleSceneLoaded;
                SceneManager.sceneUnloaded -= HandleSceneUnloaded;
                Instance = null;
            }
        }

        // =============================================================
        // UID Registration / Deregistration
        // =============================================================
        public void Register(UniqueId uid)
        {
            if (uid == null || string.IsNullOrEmpty(uid.GetID())) return;

            string id = uid.GetID();
            globalCache[id] = uid;

            string scene = uid.gameObject.scene.name;
            if (!sceneCache.ContainsKey(scene))
                sceneCache[scene] = new Dictionary<string, UniqueId>();

            sceneCache[scene][id] = uid;

            OnRegistered?.Invoke(uid);
            // Debug.Log($"[UIDRegistry] Registered {id} in scene {scene}");
        }

        public void Unregister(UniqueId uid)
        {
            if (uid == null) return;

            string id = uid.GetID();
            globalCache.Remove(id);

            string scene = uid.gameObject.scene.name;
            if (sceneCache.TryGetValue(scene, out var dict))
                dict.Remove(id);

            OnDestroyed?.Invoke(uid);
        }

        public UniqueId Get(string id)
        {
            globalCache.TryGetValue(id, out var uid);
            return uid;
        }

        public IEnumerable<UniqueId> GetAll() => globalCache.Values;

        public IEnumerable<UniqueId> GetScene(string sceneName)
        {
            if (sceneCache.TryGetValue(sceneName, out var dict))
                return dict.Values;
            return Enumerable.Empty<UniqueId>();
        }

        // =============================================================
        // Scene Management Hooks
        // =============================================================
        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            OnSceneLoaded?.Invoke(scene.name);

            // Build cache for the new scene (auto)
            var uids = FindObjectsOfType<UniqueId>(true);
            foreach (var uid in uids)
                Register(uid);

            Debug.Log($"[UIDRegistry] Scene '{scene.name}' loaded with {uids.Length} UniqueIds");
        }

        private void HandleSceneUnloaded(Scene scene)
        {
            OnSceneUnloaded?.Invoke(scene.name);
            sceneCache.Remove(scene.name);
        }

        // =============================================================
        // Integration Helpers
        // =============================================================
        public void BroadcastToBridge(UniqueId uid, string eventKey, object[] args = null)
        {
            var bridge = WorldBridgeSystem.Instance;
            if (bridge != null && uid != null)
            {
                bridge.InvokeKey($"{uid.GetID()}_{eventKey}", args ?? Array.Empty<object>());
            }
        }
        public event Action<UniqueId, string> OnUIDEvent;
        public void DispatchUIDEvent(UniqueId uid, string eventType)
        {
            OnUIDEvent?.Invoke(uid, eventType);
        }

        public void Clear()
        {
            globalCache.Clear();
            sceneCache.Clear();
        }
    }
}

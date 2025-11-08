using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using VaultSystems.Data;
using System.Linq;
namespace VaultSystems.Data
{
    [DefaultExecutionOrder(-40)]
    public class PersistentWorldManager : MonoBehaviour
    {
        public static PersistentWorldManager Instance;           // The one and only—bow down!
        
        private Dictionary<string, WorldObjectContainer> sceneContainers = new();    // Where our world goodies live
        private Dictionary<string, ECSWorldObjectContainer> ecsContainers = new();   // ECS stuff, VIP section only
        private HashSet<string> restoredCells = new();                                // Keep track of our restored VIPs

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;                                // Claiming the throne!
                DontDestroyOnLoad(gameObject);                  // Eternal glory, baby!
            }
            else
            {
                Destroy(gameObject);                           // Out with the impostor!
            }
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;          // Hook into scene shenanigans
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;          // Unhook like a pro
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            LoadCellObjects(scene.name);                        // Time to load the party!
        }

        public void SaveCurrentSceneToCell(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
                return;                                         // Nope, no empty scenes allowed!

            if (!sceneContainers.TryGetValue(sceneName, out var container))
            {
                container = new WorldObjectContainer(sceneName); // Fresh container, hot off the press!
                sceneContainers[sceneName] = container;          // Stash it away
            }

            foreach (var uid in Object.FindObjectsOfType<UniqueId>().Where(u => u != null && !u.skipUniqueID))
            {
                if (uid.isDataContainer)
                    container.AddOrUpdateDataContainer(uid);    // Data containers get special treatment
                else
                    container.AddOrUpdateObject(uid);           // Regular objects, you’re up!
            }

            if (DynamicObjectManager.Instance != null)
            {
                foreach (var kv in DynamicObjectManager.Instance.CaptureAll())
                {
                    if (kv.Value != null && !kv.Value.skipUniqueID)
                        container.AddOrUpdateObject(kv.Value);   // Dynamic divas join the party
                }
            }

            if (ECSWorldBridge.Instance != null)
            {
                if (!ecsContainers.TryGetValue(sceneName, out var ecsContainer))
                    ecsContainer = new ECSWorldObjectContainer(sceneName); // ECS gets its own VIP room

                ECSWorldBridge.Instance.CaptureECSWorldState(ecsContainer);
                ecsContainers[sceneName] = ecsContainer;            // Lock it down
            }

            Debug.Log($"[PersistentWorldManager] Snapshot saved for {sceneName} ({container.objects.Count} objects)"); // Bragging rights!
        }

        public WorldObjectContainer GetWorldStateForScene(string sceneName)
        {
            if (!sceneContainers.TryGetValue(sceneName, out var container))
            {
                container = new WorldObjectContainer(sceneName); // New container, who dis?
                sceneContainers[sceneName] = container;          // Add to the collection
            }
            return container;                                   // Serve it up!
        }

        public ECSWorldObjectContainer GetECSContainerForScene(string sceneName)
        {
            ecsContainers.TryGetValue(sceneName, out var container); // Peek at the ECS stash
            return container;                                       // Hand it over, or null if shy
        }

                public void RestoreCells(Dictionary<string, WorldObjectContainer> loadedCells)
            {
                if (loadedCells == null)
                    return;

                // 🔹 SUPPRESS during entire restoration
                if (DataContainerManager.Instance != null)
                    DataContainerManager.Instance.SetRestoringState(true);

                sceneContainers = loadedCells;
                restoredCells.Clear();

                foreach (var kvp in sceneContainers)
                {
                    string sceneName = kvp.Key;
                    var sceneContainer = kvp.Value;

                    if (!restoredCells.Contains(sceneName))
                    {
                        sceneContainer.RestoreAll();
                        sceneContainer.RestoreDataContainers();  // 🔹 No MarkDirty spam!

                        if (DynamicObjectManager.Instance != null)
                            DynamicObjectManager.Instance.RestoreAll(GetDynamicStatesForScene(sceneName));

                        var ecsContainer = GetECSContainerForScene(sceneName);
                        if (ecsContainer != null && ECSWorldBridge.Instance != null)
                        {
                            ecsContainer.RestoreAll(ECSWorldBridge.Instance);
                            Debug.Log($"[PersistentWorldManager] ECS restored for {sceneName}");
                        }

                        foreach (var dataContainer in Object.FindObjectsOfType<BaseDataContainer>())
                            if (dataContainer != null)
                                DataContainerManager.Instance?.Register(dataContainer);

                        restoredCells.Add(sceneName);
                        Debug.Log($"[PersistentWorldManager] Restored {sceneName}");
                    }
                }

                // 🔹 RESUME and fire single notification
                if (DataContainerManager.Instance != null)
                {
                    DataContainerManager.Instance.SetRestoringState(false);
                    DataContainerManager.Instance.UpdateAllDataContainers();  // Optional: refresh if needed
                    Debug.Log("[PersistentWorldManager] Restoration complete—dirty events resumed");
                }
            }  
            
        

        public void LoadCellObjects(string sceneName)
        {
            if (restoredCells.Contains(sceneName))
                return;                                             // Already restored? Chill out!

            var sceneContainer = GetWorldStateForScene(sceneName);   // Grab the container
            sceneContainer.RestoreAll();                             // Static objects, assemble!
            sceneContainer.RestoreDataContainers();                  // Data containers, join the fun

            if (DynamicObjectManager.Instance != null)
                DynamicObjectManager.Instance.RestoreAll(GetDynamicStatesForScene(sceneName)); // Dynamic crew, you’re up

            var ecsContainer = GetECSContainerForScene(sceneName);
            if (ecsContainer != null && ECSWorldBridge.Instance != null)
            {
                ecsContainer.RestoreAll(ECSWorldBridge.Instance);    // ECS VIPs strut back in
                Debug.Log($"[PersistentWorldManager] ECS restored for {sceneName}"); // ECS flex!
            }

            foreach (var dataContainer in Object.FindObjectsOfType<BaseDataContainer>())
                if (dataContainer != null)
                    DataContainerManager.Instance?.Register(dataContainer); // Re-register the data squad
            
            restoredCells.Add(sceneName);                           // Mark it restored
            Debug.Log($"[PersistentWorldManager] Loaded {sceneName}"); // Party’s on!
        }

        public Dictionary<string, UniqueId> GetDynamicStatesForScene(string sceneName)
        {
            var container = GetWorldStateForScene(sceneName);        // Fetch the scene goodies
            var dict = new Dictionary<string, UniqueId>();           // Fresh dict for the dynamic crew

            foreach (var objData in container.objects)
                if (objData.isDynamic)
                {
                    var obj = UniqueId.FindById(objData.uniqueId);   // Hunt down the dynamic diva
                    if (obj != null && !obj.skipUniqueID)
                        dict[objData.uniqueId] = obj;               // Add to the lineup
                }

            return dict;                                            // Deliver the goods!
        }

        public void SaveAllWorlds(int slot)
        {
            ChronoVaultXML.LockVault(sceneContainers, slot);         // Lock down the regular world
            ChronoVaultXML.LockVault(ecsContainers, slot + 1000);    // ECS gets its own fancy slot—don’t worry, GameManager will sort 0-2 later! 😉

            Debug.Log($"[PersistentWorldManager] Saved to slot {slot}"); // Saved like a boss!
        }

        public void LoadAllWorlds(int slot)
        {
            sceneContainers = ChronoVaultXML.UnlockVault<Dictionary<string, WorldObjectContainer>>(slot) ?? sceneContainers; // Load the world, or keep the old if null
            ecsContainers = ChronoVaultXML.UnlockVault<Dictionary<string, ECSWorldObjectContainer>>(slot + 1000) ?? ecsContainers; // ECS gets its special unlock

            restoredCells.Clear();                                  // Wipe the slate
            Debug.Log($"[PersistentWorldManager] Loaded from slot {slot}"); // Load complete—cheers!
        }

        public Dictionary<string, WorldObjectContainer> GetAllCellData() => sceneContainers; // Peek at all the cell data
        public void SetSceneContainers(Dictionary<string, WorldObjectContainer> data) => sceneContainers = data; // Set the scene containers like a pro
    }
}
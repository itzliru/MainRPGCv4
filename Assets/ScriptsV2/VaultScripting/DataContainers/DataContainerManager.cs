using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using VaultSystems.Data;
using UnityEngine.SceneManagement;
namespace VaultSystems.Data
{
    /// <summary>
    /// Manages all data containers in the scene, handling registration, updates, and serialization.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    [DisallowMultipleComponent]
    public class DataContainerManager : MonoBehaviour
    {
        public static DataContainerManager Instance { get; private set; }

        [Header("Auto Update Settings")]
        public bool autoUpdate = false;
        public float updateInterval = 5f;
        private bool isRestoringFromSave = false;
        private float timer;
        private readonly List<BaseDataContainer> containers = new();

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
            // Auto-register any containers already in the scene
            var foundContainers = FindObjectsOfType<BaseDataContainer>();
            foreach (var c in foundContainers)
                Register(c);

            Debug.Log($"[DataContainerManager] Registered {foundContainers.Length} data containers in scene '{UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}'.");
        }

        private void Update()
        {
            if (!autoUpdate) return;

            timer += Time.deltaTime;
            if (timer >= updateInterval)
            {
                timer = 0f;
                UpdateAllDataContainers();
            }
        }

        public void Register(BaseDataContainer container)
        {
            if (container == null)
            {
                Debug.LogWarning("[DataContainerManager] Attempted to register a null container!");
                return;
            }

            if (!containers.Contains(container))
            {
                containers.Add(container);
                container.OnDataChanged += HandleContainerChanged;

                string containerName = container.name;
                string containerType = container.GetType().Name;
                Debug.Log($"[DataContainerManager] Registered container: {containerName} ({containerType})");
            }
            else
            {
                Debug.Log($"[DataContainerManager] Container already registered: {container.name}");
            }
        }

        public void Unregister(BaseDataContainer container)
        {
            if (container == null) return;

            if (containers.Contains(container))
            {
                containers.Remove(container);
                container.OnDataChanged -= HandleContainerChanged;
            }
        }

       public void UpdateContainer(BaseDataContainer container)
{
    if (container == null) return;

    string hex = HexSerializationHelper.ToHex(container);
    if (!string.IsNullOrEmpty(hex))
    {
        Debug.Log($"[DataContainerManager] Serialized {container.gameObject.name}: {hex.Substring(0, Mathf.Min(20, hex.Length))}...");
    }
    else
    {
        Debug.LogWarning($"[DataContainerManager] Failed to serialize {container.gameObject.name}.");
    }
}

        public void UpdateAllDataContainers()
        {
            foreach (var container in containers)
                UpdateContainer(container);
        }

        

        public void SaveAll()
        {
            foreach (var container in containers)
                container.ForceSaveNow();

            Debug.Log($"[DataContainerManager] Saved {containers.Count} containers.");
        }

        public void LoadAll()
        {
            foreach (var container in containers)
                container.ForceLoadNow();

            Debug.Log($"[DataContainerManager] Loaded {containers.Count} containers.");
        }

       // 🔹 NEW METHOD: Control restoration state
    public void SetRestoringState(bool restoring)
    {
        isRestoringFromSave = restoring;
        if (!restoring)
            Debug.Log("[DataContainerManager] ✅ Restoration complete—monitoring dirty events resumed");
    }

    private void HandleContainerChanged()
    {
        // 🔹 NEW: Skip if we're loading
        if (isRestoringFromSave)
        {
            Debug.Log("[DataContainerManager] ⏸️ Skipping update during restoration phase");
            return;
        }

        CancelInvoke(nameof(UpdateAllDataContainers));
        Invoke(nameof(UpdateAllDataContainers), 0.1f);
    }

        // 🔹 Added for Editor tools and debugging
        public List<BaseDataContainer> GetAllContainers()
        {
            return containers;
        }

        // 🔹 NEW: Cleanup helper to prevent duplicates before registering new data
        public void ClearAll()
        {
            foreach (var container in containers.ToList())
            {
                if (container != null)
                    Destroy(container.gameObject);
            }

            containers.Clear();
            Debug.Log("[DataContainerManager] Cleared all registered data containers.");
        }
    }
}
using UnityEngine;
using System.Collections.Generic;
using VaultSystems.Invoker;
using VaultSystems.Containers;
using System.Linq;
using UnityEngine.SceneManagement;
using VaultSystems.Data;
using System.Collections;
using System;



namespace VaultSystems.Controllers
{
    public class PlayerWorldManager : MonoBehaviour
    {
        [Header("Dependencies")]
        public PlayerDataContainer playerData;
        public Camera playerCamera;
        public Transform uiMarkerRoot;

        [Header("World Tracking")]
        public string currentScene;
        public string currentCellId;
        public HashSet<string> visibleObjects = new();
        public Dictionary<string, GameObject> trackedSceneObjects = new();

        [Header("Batching / Optimization")]
        public bool useCellBatching = true;

        [Header("Quest/Object Overrides")]
        public bool skipCellRenderingForQuests = false; // Allow quest objects to always render
        private Dictionary<string, int[]> cellGridStates = new(); // per scene/cell
        private Dictionary<string, List<UniqueId>> cellObjects = new(); // cellId -> objects in cell
        private IDisposable _cellChange;
        private void Awake()
        {
            if (playerData == null)
                playerData = GetComponent<PlayerDataContainer>() ?? FindObjectOfType<PlayerDataContainer>();

            // Make persistent
            DontDestroyOnLoad(gameObject);

            // Add/setup UniqueId as data container
            var uid = GetComponent<UniqueId>() ?? gameObject.AddComponent<UniqueId>();
            uid.isDataContainer = true;
            uid.manualId = "PlayerWorldManager";  // Or generate if preferred
        }






        private void OnEnable()
        {
            SceneManager.sceneLoaded += (scene, mode) => OnSceneLoaded(scene, mode);
            _cellChange = EventDataContainer.SubscribeTo(EventKeys.Scene.CELL_CHANGED, OnCellChanged);
        }

        private void OnDestroy()
        {
            _cellChange?.Dispose();
            _cellChange = null;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= (scene, mode) => OnSceneLoaded(scene, mode);
            _cellChange?.Dispose();
            _cellChange = null;
        }

        public void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            currentScene = scene.name;
            playerData.lastScene = currentScene;
            Debug.Log($"[PlayerWorldManager] Entered scene: {currentScene}");
            InitializeCellGrid(currentScene);
        }

        public void OnCellChanged(object[] args)
        {
            if (args.Length == 0 || !(args[0] is int cellIndex)) return;
            string newCellId = $"Cell_{cellIndex}";
            if (currentCellId == newCellId) return;

            currentCellId = newCellId;
            playerData.lastCellId = newCellId;

            Debug.Log($"[PlayerWorldManager] Cell switched to {newCellId}");

            //  Trigger event to update UI / map / batching
            UpdateCellVisibility(newCellId);
        }

        public void InitializeCellGrid(string sceneName)
        {
            if (!cellGridStates.ContainsKey(sceneName))
                cellGridStates[sceneName] = new int[64]; // default 8x8 grid
        }

        public void UpdateCellVisibility(string cellId)
        {
            if (!useCellBatching) return;

            int[] cellGrid = cellGridStates[currentScene];
            int cellIndex = CellIndexFromId(cellId);

            // Mark this cell active
            for (int i = 0; i < cellGrid.Length; i++)
                cellGrid[i] = (i == cellIndex) ? 1 : 0;

            // Enable/disable rendering for objects in cells
            UpdateObjectRendering(cellId);
        }

        public void UpdateObjectRendering(string activeCellId)
        {
            // Get all UniqueId components in current scene
            var allObjects = UniqueId.GetAllCached();

            foreach (var obj in allObjects)
            {
                if (obj.gameObject.scene.name != currentScene) continue;

                // Skip rendering toggle for quest objects if override is enabled
                if (skipCellRenderingForQuests && IsQuestObject(obj))
                {
                    SetObjectRendering(obj.gameObject, true); // Always render quest objects
                    continue;
                }

                // Try to get the cell this object belongs to
                string objectCellId = GetCellIdForObject(obj);

                // Enable rendering for objects in active cell and adjacent cells
                bool shouldRender = IsCellVisible(objectCellId, activeCellId);

                // Toggle rendering on the object's meshes/Renderers
                SetObjectRendering(obj.gameObject, shouldRender);
            }
        }

        public bool IsQuestObject(UniqueId obj)
        {
            // Check for quest-related tags or components
            // This could be enhanced to check for specific quest components
            return obj.gameObject.CompareTag("Quest") ||
                   obj.gameObject.name.Contains("Quest") ||
                   obj.GetComponent<QuestMarker>() != null;
        }

        public string GetCellIdForObject(UniqueId obj)
        {
            // For now, use position-based cell detection
            // In future, could store cellId in UniqueId component
            Vector3 pos = obj.transform.position;
            int x = Mathf.Clamp((int)(pos.x / 10f), 0, 7); // Assuming 10f cell size
            int y = Mathf.Clamp((int)(pos.z / 10f), 0, 7);
            return $"Cell_{y * 8 + x}";
        }

        public bool IsCellVisible(string objectCellId, string activeCellId)
        {
            if (objectCellId == activeCellId) return true;

            // Make adjacent cells visible too for smooth transitions
            int activeIndex = CellIndexFromId(activeCellId);
            int objectIndex = CellIndexFromId(objectCellId);

            int activeX = activeIndex % 8;
            int activeY = activeIndex / 8;
            int objectX = objectIndex % 8;
            int objectY = objectIndex / 8;

            // Check if adjacent (including diagonals)
            int dx = Mathf.Abs(activeX - objectX);
            int dy = Mathf.Abs(activeY - objectY);

            return dx <= 1 && dy <= 1;
        }

        private void SetObjectRendering(GameObject obj, bool shouldRender)
        {
            // Get all renderers on this object and its children
            var renderers = obj.GetComponentsInChildren<Renderer>(true);

            foreach (var renderer in renderers)
            {
                renderer.enabled = shouldRender;
            }

            // LOD Groups - force lower quality for distant objects
            if (obj.TryGetComponent(out LODGroup lodGroup))
            {
                if (!shouldRender)
                {
                    lodGroup.ForceLOD(2); // Force lowest LOD level when not rendering
                }
                else
                {
                    lodGroup.ForceLOD(-1); // Reset to automatic LOD
                }
            }

            // Lights - disable for performance
            if (obj.TryGetComponent(out Light light))
            {
                light.enabled = shouldRender;
            }

            // Colliders - disable for performance (physics)
            var colliders = obj.GetComponentsInChildren<Collider>(true);
            foreach (var collider in colliders)
            {
                collider.enabled = shouldRender;
            }

            // Rigidbodies - set kinematic to reduce physics calculations
            if (obj.TryGetComponent(out Rigidbody rb))
            {
                rb.isKinematic = !shouldRender;
            }

            // Reflection probes - disable for performance
            if (obj.TryGetComponent(out ReflectionProbe probe))
            {
                probe.enabled = shouldRender;
            }

            // Terrain details/trees - disable for distant terrain
            if (obj.TryGetComponent(out Terrain terrain))
            {
                terrain.drawTreesAndFoliage = shouldRender;
                if (!shouldRender)
                {
                    terrain.detailObjectDistance = 0;
                    terrain.treeDistance = 0;
                }
                else
                {
                    // Restore original distances (you might want to cache these)
                    terrain.detailObjectDistance = 250;
                    terrain.treeDistance = 2000;
                }
            }
        }

        private int CellIndexFromId(string id)
        {
            // Example: "Cell_03" → 3
            string num = new string(id.Where(char.IsDigit).ToArray());
            return int.TryParse(num, out int i) ? i : 0;
        }

        public void RegisterVisibleObject(string id, GameObject obj)
        {
            if (string.IsNullOrEmpty(id) || obj == null) return;
            if (!trackedSceneObjects.ContainsKey(id))
                trackedSceneObjects[id] = obj;

            visibleObjects.Add(id);
        }

        public void UnregisterVisibleObject(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            visibleObjects.Remove(id);
            trackedSceneObjects.Remove(id);
        }
    }
}
 
    


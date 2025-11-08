using UnityEngine;
using System.Collections;
using System.Security.Cryptography;
using VaultSystems.Data;
using VaultSystems.Invoker;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;
using VaultSystems.Data;
using System.Diagnostics.CodeAnalysis;
using VaultSystems.Controllers;
namespace VaultSystems.Data
{
    [DefaultExecutionOrder(-40)]
    public class StreamingCellManager : MonoBehaviour
    {
        public static StreamingCellManager Instance;
        public string CurrentSceneName { get; private set; }
        private Dictionary<string, bool> loadedCells = new Dictionary<string, bool>();

        // Event for cell transitions
        public delegate void CellTransitionHandler(string newCellId);
        public event CellTransitionHandler OnCellTransition;

        public PlayerWorldManager pwm;

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
        public void OnCellEntered(string sceneName)
        {
            if (pwm == null) { return; }
            if (string.IsNullOrEmpty(sceneName)) return;
            CurrentSceneName = sceneName;  // Sync if needed
            if (pwm != null)
            {
                pwm.currentScene = sceneName;
                pwm.playerData.lastScene = sceneName;
                pwm.InitializeCellGrid(sceneName);
                if (!string.IsNullOrEmpty(pwm.currentCellId))
                {
                    pwm.UpdateCellVisibility(pwm.currentCellId);
                }
                Debug.Log($"[StreamingCellManager] Cell entered: {sceneName}");
            }
            else
            {
                Debug.LogWarning("[StreamingCellManager] PlayerWorldManager not found for OnCellEntered.");
            }
            // Optional: Broadcast for other systems
            WorldBridgeSystem.Instance?.InvokeKey("OnCellEntered", sceneName);
        }
       public void ExitCell()
        {
            // Invoke the registered OnExit handler (decoupled from direct player access)
            //note this saves the player and is located in PlayerDataInitializer
            WorldBridgeSystem.Instance?.InvokeKey("player.OnExit");

            if (!string.IsNullOrEmpty(CurrentSceneName))
            {
                PersistentWorldManager.Instance.SaveCurrentSceneToCell(CurrentSceneName);
                Debug.Log($"[StreamingCell] Exited {CurrentSceneName}");
            }
        }
        public void EnterCell(string sceneName, Action onComplete = null)
        {
            StartCoroutine(EnterCellRoutine(sceneName, onComplete));
        }

        private IEnumerator EnterCellRoutine(string sceneName,  Action onComplete)
        {
            ExitCell(); CurrentSceneName = sceneName;
             
            if (!loadedCells.ContainsKey(sceneName)) loadedCells[sceneName] = false;

            var lsm = LoadingScreenManager.Instance;
            
            if (lsm) { lsm.Show($"Loading {sceneName}..."); lsm.SetProgress(0f); }

            else Debug.LogWarning("[StreamingCell] LoadingScreenManager not found!");

            AsyncOperation asyncOp = SceneManager.LoadSceneAsync(sceneName);
             asyncOp.allowSceneActivation = false;
            while (asyncOp.progress < 0.9f)

            { lsm?.SetProgress(asyncOp.progress / 0.9f); yield return null; }
            asyncOp.allowSceneActivation = true;
            while (!asyncOp.isDone)
            yield return null; // Ensure one frame passes for scene activation
            

            // Broadcast cell transition event
            OnCellTransition?.Invoke(sceneName);
            if (!loadedCells[sceneName])
            {
                loadedCells[sceneName] = true;
                var container = PersistentWorldManager.Instance.GetWorldStateForScene(sceneName); 
                
                container.RestoreAll();
                container.RestoreDataContainers();
                
                if (DynamicObjectManager.Instance != null)
                DynamicObjectManager.Instance.RestoreAll(PersistentWorldManager.Instance.GetDynamicStatesForScene(sceneName));
                
                var ecsContainer = PersistentWorldManager.Instance.GetECSContainerForScene(sceneName);

                if (ecsContainer != null && ECSWorldBridge.Instance != null)
                {
                    ecsContainer.RestoreAll(ECSWorldBridge.Instance);
                    Debug.Log($"[StreamingCell] ECS restored for {sceneName}");
                }
                
                Debug.Log($"[StreamingCell] Entered {sceneName}");

            
            
            }
            SpawnPointManager.Instance?.OnSceneLoaded(sceneName);  // REFRESH SPAWNS HERE

            // Call OnCellEntered post-load Call (integrates with PlayerWorldManager logic)
            OnCellEntered(sceneName);
            //done switching xorcells & loaded scene, cells and containers

            onComplete?.Invoke();

            lsm?.Hide();
            yield break;
        }
    }
}
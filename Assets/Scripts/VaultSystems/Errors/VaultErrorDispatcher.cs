using UnityEngine;
using System;
using System.Collections.Generic;
using VaultSystems.Data;

namespace VaultSystems.Errors
{
        /// <summary>
    /// Do not use this system its not yet implemented! it can break the game if used!
    /// </summary>
    [DefaultExecutionOrder(-200)]
    public class VaultErrorDispatcher : MonoBehaviour
    {
        public static VaultErrorDispatcher Instance { get; private set; }
        private static int nextErrorID = 1;
        private readonly List<VaultError> activeErrors = new();
        private static readonly Dictionary<int, float> recentErrorTimestamps = new();
        private const float SUPPRESSION_WINDOW = 2.0f;
        private const int MAX_CACHE_SIZE = 128;

        private void Awake()
        {
            if (Instance && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                activeErrors.Clear();
                recentErrorTimestamps.Clear();
                Instance = null;
            }
        }

        public static void Dispatch(VaultErrorType type, string message, UnityEngine.Object context = null, string trace = null)
        {
            if (!Instance)
            {
                var go = new GameObject("VaultErrorDispatcher");
                Instance = go.AddComponent<VaultErrorDispatcher>();
            }

            trace ??= Environment.StackTrace;
            int hash = (message + type.ToString() + (context ? context.name : "null")).GetHashCode();

            if (recentErrorTimestamps.TryGetValue(hash, out float lastTime) && Time.unscaledTime - lastTime < SUPPRESSION_WINDOW)
                return;

            recentErrorTimestamps[hash] = Time.unscaledTime;
            if (recentErrorTimestamps.Count > MAX_CACHE_SIZE)
                recentErrorTimestamps.Clear();

            int id = nextErrorID++;
            var error = new VaultError(id, type, message, trace, context);
            error.AddContextData("Scene", UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
            if (context != null && context is Component comp && comp.GetComponent<UniqueId>() != null)
                error.AddContextData("PlayerID", comp.GetComponent<UniqueId>().GetID());
            else
                error.AddContextData("PlayerID", "None");

            Instance.activeErrors.Add(error);

            Debug.LogError($"[VaultError #{id}] {message}\n{error.ToFormattedTrace()}");
            VaultErrorLogFileWriter.LogError(error);

#if UNITY_EDITOR
            if (type == VaultErrorType.Breakpoint || type == VaultErrorType.Assertion || type == VaultErrorType.Critical)
                Debug.Break();
#endif
            VaultErrorPauseController.HandleErrorPause(type);
            VaultErrorPopupUI.ShowPopup(error);
        }

        public static void ClearError(VaultError error)
        {
            if (Instance && Instance.activeErrors.Contains(error))
                Instance.activeErrors.Remove(error);
        }
    }
}
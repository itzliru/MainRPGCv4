using UnityEngine;
using UnityEngine.SceneManagement;
using VaultSystems.Data;
namespace VaultSystems.Data
{
    // The scene cell queen—holding the world together!
    [DefaultExecutionOrder(-40)]
    public class SceneCellContainer : BaseDataContainer
    {
        [Header("Cell Info")]               // The juicy details of this cell!
        public string cellName;             // What’s this cell called, darling?
        public bool isIndoor;               // Inside cozy vibes?
        public bool isLoaded;               // Ready to roll?
        public string sceneName;            // The scene’s backstage pass
        public string uniqueCellId;         // Unique hex-y ID for flair

        private void OnEnable()
        {
            sceneName = SceneManager.GetActiveScene().name; // Grab the scene
            if (string.IsNullOrEmpty(cellName)) cellName = sceneName; // Default name, why not?
            if (string.IsNullOrEmpty(uniqueCellId)) uniqueCellId = $"{sceneName}_{cellName}"; // Craft a unique tag
            isLoaded = true;                    // We’re live!
            DataContainerManager.Instance?.Register(this); // Sign up
            GlobalWorldManager.Instance?.SetCell(this); // Tell the world
            Debug.Log($"[SceneCellContainer] Registered cell '{cellName}' (Indoor: {isIndoor})"); // Party time!
        }

        private void OnDisable()
        {
            isLoaded = false;                   // Lights out
            DataContainerManager.Instance?.Unregister(this); // Unsubscribe
            Debug.Log($"[SceneCellContainer] Unloaded cell '{cellName}'"); // Farewell!
        }

        public void SetIndoorState(bool state)
        {
            if (isIndoor == state) return;      // No repeat drama
            isIndoor = state;                   // Flip the switch
            GlobalWorldManager.Instance?.SetCell(this); // Update the world
            MarkDirty();                        // Mark it dirty
            Debug.Log($"[SceneCellContainer] Indoor flag set to {isIndoor} for {sceneName}"); // Log the change
        }

        public string SerializeData() => JsonUtility.ToJson(this); // Package it up!

        public void DeserializeData(string json)
        {
            JsonUtility.FromJsonOverwrite(json, this); // Unpack the goods
            MarkDirty();                        // Mark it fresh
        }
    }
}
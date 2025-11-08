using System;
using System.Collections.Generic;
using UnityEngine;
using VaultSystems.Data;
 [DefaultExecutionOrder(0)]
public class DataContainerSaveHelper : MonoBehaviour
{
    public static DataContainerSaveHelper Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Hook into existing containers
        RegisterAllContainers();
    }

    private void OnEnable()
    {
        // Hook future container registrations
        DataContainerManager.Instance?.GetAllContainers()
            .ForEach(RegisterContainer);
    }

    /// <summary>
    /// Register all containers found in scene at start
    /// </summary>
    public void RegisterAllContainers()
    {
        if (DataContainerManager.Instance == null) return;

        foreach (var container in DataContainerManager.Instance.GetAllContainers())
            RegisterContainer(container);
    }

    /// <summary>
    /// Hook into container's OnDataChanged to automatically update WorldObjectContainer
    /// </summary>
    public void RegisterContainer(BaseDataContainer container)
    {
        if (container == null) return;

        container.OnDataChanged -= HandleContainerDirty;
        container.OnDataChanged += HandleContainerDirty;
    }

    private void HandleContainerDirty()
    {
        // Called whenever any IDataContainer marks dirty
        // Find which container called this
        foreach (var container in DataContainerManager.Instance.GetAllContainers())
        {
            // Update the WorldObjectContainer snapshot in PersistentWorldManager
            if (container != null)
                UpdateSnapshot(container);
        }
    }

    private void UpdateSnapshot(BaseDataContainer container)
    {
        if (container == null) return;

        var uid = container.GetComponent<UniqueId>();
        if (uid == null) return;

        // Get scene container
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        var sceneContainer = PersistentWorldManager.Instance.GetWorldStateForScene(sceneName);

        sceneContainer.AddOrUpdateDataContainer(uid);
    }

    /// <summary>
    /// Force save everything to ChronoVault for the given slot
    /// </summary>
    public void ForceSaveSlot(int slot)
    {
        // Make sure all dirty containers are captured
        foreach (var container in DataContainerManager.Instance.GetAllContainers())
            UpdateSnapshot(container);

        // Save all dynamic + ECS objects
        PersistentWorldManager.Instance.SaveAllWorlds(slot);

        Debug.Log($"[DataContainerSaveHelper] Forced save complete for slot {slot}");
    }

    /// <summary>
    /// Restore all containers for current scene without triggering OnDataChanged
    /// </summary>
    public void RestoreSceneContainersSuppressDirty(string sceneName)
    {
        var sceneContainer = PersistentWorldManager.Instance.GetWorldStateForScene(sceneName);

        foreach (var data in sceneContainer.dataContainers)
        {
            var obj = UniqueId.FindById(data.uniqueId);
            if (obj == null) continue;

            var container = obj.GetComponent<BaseDataContainer>();
            if (container == null) continue;

            // Temporarily suppress OnDataChanged during load
            bool suppress = container is BaseDataContainer b ? b.SuppressDirtyEvents : false;
            if (container is BaseDataContainer bd) bd.SuppressDirtyEvents = true;

            data.Restore();

            if (container is BaseDataContainer bd2) bd2.SuppressDirtyEvents = suppress;
        }

        Debug.Log($"[DataContainerSaveHelper] Scene '{sceneName}' containers restored without firing OnDataChanged.");
    }
}

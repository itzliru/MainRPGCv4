using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class ECSWorldBridge : MonoBehaviour
{
    public static ECSWorldBridge Instance;

    private Dictionary<string, Entity> ecsIdMap = new Dictionary<string, Entity>();
    private EntityManager entityManager;

    public Dictionary<string, Entity> GetAllEntities() => ecsIdMap;
    public EntityManager EntityManager => entityManager;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    #region Registration

    public void RegisterEntity(Entity entity, string id)
    {
        if (string.IsNullOrEmpty(id)) return;
        if (!ecsIdMap.ContainsKey(id))
            ecsIdMap.Add(id, entity);
    }

    public void UnregisterEntity(string id)
    {
        if (ecsIdMap.ContainsKey(id))
            ecsIdMap.Remove(id);
    }

    public bool TryGetEntity(string id, out Entity entity)
    {
        return ecsIdMap.TryGetValue(id, out entity);
    }

    #endregion

    #region Capture

    public void CaptureECSWorldState(ECSWorldObjectContainer container)
    {
        if (container == null) return;

        foreach (var kvp in ecsIdMap)
        {
            var entity = kvp.Value;
            if (!entityManager.Exists(entity)) continue;
            if (!entityManager.HasComponent<LocalTransform>(entity)) continue;

            var t = entityManager.GetComponentData<LocalTransform>(entity);

            var ecsData = entityManager.HasComponent<UniqueIdECS>(entity)
                ? entityManager.GetComponentData<UniqueIdECS>(entity)
                : new UniqueIdECS { IsDynamic = false, IsActiveScene = true };

            container.AddOrUpdateEntity(
                entity,
                kvp.Key,
                t.Position,
                t.Rotation,
                ecsData.IsDynamic,
                ecsData.IsActiveScene
            );
        }
    }

    #endregion

    #region Restore

    /// <summary>
    /// Restores ECS entities for a cell/scene.
    /// Updates transform and active state without duplicating entities.
    /// </summary>
    public void RestoreECSWorldState(ECSWorldObjectContainer container)
    {
        if (container == null || ecsIdMap.Count == 0) return;

        foreach (var objData in container.ecsObjects)
        {
            // Skip if entity doesn't exist
            if (!TryGetEntity(objData.uniqueId, out var entity)) continue;
            if (!entityManager.Exists(entity)) continue;
            if (!entityManager.HasComponent<UniqueIdECS>(entity)) continue;

            // Update ECS flags
            var ecsData = entityManager.GetComponentData<UniqueIdECS>(entity);
            ecsData.IsActiveScene = objData.isActiveScene;
            entityManager.SetComponentData(entity, ecsData);

            // Update transform
            if (ecsData.IsActiveScene && entityManager.HasComponent<LocalTransform>(entity))
            {
                var t = entityManager.GetComponentData<LocalTransform>(entity);
                t.Position = objData.position;
                t.Rotation = objData.rotation;
                entityManager.SetComponentData(entity, t);
            }

            // Update rendering/visibility
            SetEntityRenderingActive(entity, ecsData.IsActiveScene);
        }
    }

    #endregion

    #region Rendering Helper

    /// <summary>
    /// Stub for toggling ECS entity visibility.
    /// Implement with your rendering system (Hybrid Renderer, etc.).
    /// </summary>
    private void SetEntityRenderingActive(Entity entity, bool active)
    {
        // Example:
        // if using Hybrid Renderer: enable/disable RenderMesh or Renderer component
        // Currently a stub, implement as needed
    }

    #endregion
}

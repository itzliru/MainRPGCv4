using System;
using Unity.Entities;
using UnityEngine;

[DisallowMultipleComponent]
public class UniqueIdECSAuthoring : MonoBehaviour
{
    [Tooltip("Used to track this object between saves.")]
    public string uniqueId;

    [Tooltip("Mark true if this object moves or changes often.")]
    public bool isDynamic = false;

    [HideInInspector]
    public bool IsActiveScene = true; // Only active in the current scene

    private void OnValidate()
    {
        if (string.IsNullOrEmpty(uniqueId))
            uniqueId = Guid.NewGuid().ToString();
    }

    public string GetID() => uniqueId;

    class Baker : Baker<UniqueIdECSAuthoring>
    {
        public override void Bake(UniqueIdECSAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Renderable);
            AddComponent(entity, new UniqueIdECS
            {
                Value = authoring.uniqueId.GetHashCode(),
                IsDynamic = authoring.isDynamic,
                IsActiveScene = authoring.IsActiveScene
            });

            if (ECSWorldBridge.Instance != null)
                ECSWorldBridge.Instance.RegisterEntity(entity, authoring.uniqueId);
        }
    }
}

public struct UniqueIdECS : IComponentData
{
    public int Value;
    public bool IsDynamic;
    public bool IsActiveScene; // Tracks if entity should render/update for current scene
}

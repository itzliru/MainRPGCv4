using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[Serializable]
public class ECSWorldObjectData
{
    public string uniqueId;
    public string sceneName;
    public float3 position;
    public quaternion rotation;
    public bool isDynamic;
    public bool isActiveScene;

    public ECSWorldObjectData() { }

    public ECSWorldObjectData(string id, string scene, float3 pos, quaternion rot, bool dynamic, bool activeScene)
    {
        uniqueId = id;
        sceneName = scene;
        position = pos;
        rotation = rot;
        isDynamic = dynamic;
        isActiveScene = activeScene;
    }
}

[Serializable]
public class ECSWorldObjectContainer
{
    public string sceneName;
    public List<ECSWorldObjectData> ecsObjects = new List<ECSWorldObjectData>();

    public ECSWorldObjectContainer() { }
    public ECSWorldObjectContainer(string scene) { sceneName = scene; }

    public void AddOrUpdateEntity(Entity entity, string uniqueId, float3 pos, quaternion rot, bool dynamic, bool activeScene)
    {
        var existing = ecsObjects.Find(o => o.uniqueId == uniqueId);
        if (existing != null)
        {
            existing.position = pos;
            existing.rotation = rot;
            existing.isDynamic = dynamic;
            existing.isActiveScene = activeScene;
        }
        else
        {
            ecsObjects.Add(new ECSWorldObjectData(uniqueId, sceneName, pos, rot, dynamic, activeScene));
        }
    }

    // Restore via ECSWorldBridge
    public void RestoreAll(ECSWorldBridge bridge)
    {
        if (bridge == null) return;
        bridge.RestoreECSWorldState(this);
    }
}

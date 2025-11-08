using VaultSystems.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VaultSystems.Data
{
    [DefaultExecutionOrder(-40)]
    // Data for a world object—serializable and sassy!
    [Serializable]
    public class WorldObjectData
    {
        public string uniqueId;          // The hex-y ID of this cool cat
        public Vector3 position;         // Where this object likes to chill
        public Quaternion rotation;      // Spin it right round, baby!
        public bool isActive;            // Lights on or off?
        public bool isDynamic;           // Is it a wild mover?
        public bool isECS;               // ECS VIP status

        public WorldObjectData()
        {
            // Default constructor—empty but ready to party!
        }

        public WorldObjectData(UniqueId obj)
        {
            if (obj == null)
                return;                     // Nope, no null nonsense!

            uniqueId = obj.GetID();         // Grab that hex ID
            position = obj.transform.position; // Snag the spot
            rotation = obj.transform.rotation; // Lock the spin
            isActive = obj.gameObject.activeSelf; // Check the glow
            isDynamic = obj.isDynamic;      // Is it a dancer?
            isECS = false;                  // No ECS flair by default
        }

        public void Restore()
        {
            var obj = UniqueId.FindById(uniqueId); // Hunt down the star
            if (obj != null)
            {
                obj.transform.position = position; // Put it back where it belongs
                obj.transform.rotation = rotation; // Spin it back
                obj.gameObject.SetActive(isActive); // Flip the switch
            }
            else
                Debug.LogWarning($"[WorldObjectData] Object {uniqueId} not found for restore"); // Uh-oh, lost one!
        }
    }
}
    // Data container for the data containers—meta much?
    [Serializable]
    public class WorldDataContainerData
    {
        public string uniqueId;          // The hex key to this treasure
        public string containerType;     // What kind of data diva is this?
        public string serializedData;    // The juicy serialized goods

        public WorldDataContainerData()
        {
            // Empty shell, ready to fill!
        }
    
public WorldDataContainerData(UniqueId obj)
{
    if (obj == null) return;

    uniqueId = obj.GetID();
    containerType = obj.GetType().Name;
    var data = obj.GetComponent<IDataContainer>();
    if (data != null)
    {
        try
        {
            serializedData = data.SerializeData();
        }
        catch (Exception e)
        {
            Debug.LogError($"[WorldDataContainerData] Failed to serialize {obj.name}: {e}");
            serializedData = null;
        }
    }
}
    
public void Restore()
{
    var obj = UniqueId.FindById(uniqueId);
    if (obj != null)
    {
        var data = obj.GetComponent<IDataContainer>();
        if (data != null && !string.IsNullOrEmpty(serializedData))
        {
            try
            {
                data.DeserializeData(serializedData);
            }
            catch (Exception e)
            {
                Debug.LogError($"[WorldDataContainerData] Failed to deserialize {obj.name}: {e}");
            }
        }
    }

}
    }
    // The big container holding all the world’s secrets!
    [Serializable]
    public class WorldObjectContainer
    {
        public string sceneName;         // The stage for this show
        public List<WorldObjectData> objects = new(); // The regular crew
        public List<WorldDataContainerData> dataContainers = new(); // The data VIPs

        public WorldObjectContainer()
        {
            // Fresh container, ready to roll!
        }

public WorldObjectContainer(string scene) => sceneName = scene; // Set the scene

public void AddOrUpdateObject(MonoBehaviour obj)
{
    if (obj is UniqueId uid && !uid.skipUniqueID)
    {
        if (uid.isDataContainer)
        {
            AddOrUpdateDataContainer(uid); // Data divas get special care
        }
        else
        {
            var existing = objects.Find(o => o.uniqueId == uid.GetID());
            if (existing != null)
            {
                existing.position = uid.transform.position; // Update the spot
                existing.rotation = uid.transform.rotation; // Spin it fresh
                existing.isActive = uid.gameObject.activeSelf; // Toggle the lights
                existing.isDynamic = uid.isDynamic; // Keep the dance alive
            }
            else
            {
                objects.Add(new WorldObjectData(uid)); // New member, welcome!
            }
        }
    }
}


        public void AddOrUpdateDataContainer(UniqueId obj)
        {
            if (obj != null && obj.isDataContainer)
            {
                var existing = dataContainers.Find(d => d.uniqueId == obj.GetID());
                var data = obj.GetComponent<IDataContainer>();
                if (data != null)
                {
                    if (existing != null)
                        existing.serializedData = data.SerializeData(); // Refresh the data
                    else
                        dataContainers.Add(new WorldDataContainerData(obj)); // New VIP joins!
                    Debug.Log($"[WorldObjectContainer] Stored DataContainer: {obj.name} ({obj.GetID()})"); // Bragging rights!
                }
            }
        
    }
        public void RestoreAll() => objects.ForEach(o => o.Restore()); // Bring back the crew!

        public void RestoreDataContainers() => dataContainers.ForEach(d => d.Restore()); // Revive the data stars!
}

    
  //  // ✅ Safe Hex Serialization Layer (Does NOT register or call DataContainerManager)
  //  public string SerializeHex() => HexSerializationHelper.ToHex(this);
  //  public static WorldObjectContainer DeserializeHex(string hex)
   // {
 //       return HexSerializationHelper.FromHex<WorldObjectContainer>(hex);
  

using UnityEngine;
using System.Collections.Generic;
using VaultSystems.Data;
using System.Linq;

namespace VaultSystems.Data
{
    // The dynamic object maestro—ruling the runtime chaos!
    public class DynamicObjectManager : MonoBehaviour
    {
        public static DynamicObjectManager Instance;         // The one true boss—kneel!

        private Dictionary<string, UniqueId> dynamicObjects = new Dictionary<string, UniqueId>(); // Our VIP guest list

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;                            // Claiming the crown!
                DontDestroyOnLoad(gameObject);              // Eternal party time!
            }
            else
            {
                Destroy(gameObject);                        // Out with the pretender!
            }
        }

        // Register a new dynamic diva—welcome to the club!
        public void Register(UniqueId obj)
        {
            if (obj != null && !string.IsNullOrEmpty(obj.GetID()))
            {
                obj.isDynamic = true;                       // Mark as a mover and shaker
                if (!dynamicObjects.ContainsKey(obj.GetID()))
                    dynamicObjects[obj.GetID()] = obj;      // Add to the lineup
            }
        }

        // Unregister a diva—time to leave the stage!
        public void Unregister(UniqueId obj)
        {
            if (obj != null && dynamicObjects.ContainsKey(obj.GetID()))
                dynamicObjects.Remove(obj.GetID());         // Kick ‘em out!
        }

        // Capture all the dynamic stars—snapshot time!
        public Dictionary<string, UniqueId> CaptureAll()
        {
            var keysToRemove = dynamicObjects
                .Where(kv => kv.Value == null)
                .Select(kv => kv.Key)
                .ToList();                                  // Hunt down the null troublemakers

            keysToRemove.ForEach(k => dynamicObjects.Remove(k)); // Clean house!

            return new Dictionary<string, UniqueId>(dynamicObjects); // Return the shiny new list
        }

        // Restore the dynamic crew from a snapshot—back to the spotlight!
        public void RestoreAll(Dictionary<string, UniqueId> snapshot)
        {
            if (snapshot != null)
            {
                foreach (var kv in snapshot)
                {
                    var obj = UniqueId.FindById(kv.Key);    // Track down the star
                    if (obj != null)
                    {
                        obj.transform.position = kv.Value.transform.position; // Move ‘em back
                        obj.transform.rotation = kv.Value.transform.rotation; // Spin ‘em right
                        obj.gameObject.SetActive(kv.Value.gameObject.activeSelf); // Lights on or off!
                    }
                }
            }
        }
    }
}
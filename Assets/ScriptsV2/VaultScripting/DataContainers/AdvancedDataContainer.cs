using UnityEngine;
using System.Collections;
using System.Security.Cryptography;
using VaultSystems.Data;
using VaultSystems.Invoker;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;
using VaultSystems.Data;
using System;

namespace VaultSystems.Data
{

    // Enum of all the naughty reasons data might get dirty—ooh la la!
    public enum DirtyReason
    {
        Generic,                // Just a little mess, no biggie
        ValueChanged,           // Someone tweaked the goods!
        OwnerChanged,           // New boss in town
        LoadComplete,           // Fresh from the oven
        Custom,                 // Custom chaos unleashed
        RuntimeChange,          // Live and wild changes
        RuntimeSetup            // Setting up the party
    }

    // The big data daddy—abstract and fabulous!
    public abstract class AdvancedDataContainer : BaseDataContainer, IAdvancedDataContainer
    {
        [Header("Advanced Data Settings")]          // Fancy settings, strut your stuff!
        public float saveFrequency = 0f;            // How often we save—chill pace!
        public int savePriority = 0;                // Who gets saved first? You decide!
        public int loadPriority = 0;                // Loading order, VIP or not?
        public string ownerId;                      // Who owns this data diva?
        public DirtyReason lastDirtyReason;         // Last scandalous reason
        public string lastHash;                     // Hash to prove it’s legit

        // Property getters—read-only, but oh so useful!
        public float SaveFrequency => saveFrequency;
        public int SavePriority => savePriority;
        public int LoadPriority => loadPriority;
        public string OwnerID => ownerId;
        public DirtyReason LastDirtyReason => lastDirtyReason;
        public string LastHash => lastHash;

        // Event to shout when things get dirty—party time!
        public event Action<DirtyReason> OnDirty;

        // Mark this data as dirty with a reason—spill the tea!
        public virtual void MarkDirty(DirtyReason reason = DirtyReason.Generic)
        {
            lastDirtyReason = reason;                   // Update the scandal log
            lastHash = ComputeHash();                   // Recalculate that hash magic
            base.MarkDirty();                           // Call the base class, don’t skip leg day!
            OnDirty?.Invoke(reason);                    // Alert the crew—drama’s on!
        }

        // Compute a hash to keep things honest—fingers crossed!
        protected string ComputeHash()
        {
            try
            {
                string json = JsonUtility.ToJson(this, false);   // Serialize the goods
                using (var md5 = MD5.Create())                  // Whip out the MD5 wand
                {
                    return BitConverter.ToString(md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(json)))
                        .Replace("-", "");                      // Smooth it out, no dashes!
                }
            }
            catch
            {
                return "HASH_ERR";                          // Oops, hash gone wrong—panic mode!
            }
        }

        // Async setup for the runtime party—default to chilling
        public virtual IEnumerator RuntimeAsyncSetup()
        {
            yield break;                                    // Nothing to see here, move along!
        }
    }
}
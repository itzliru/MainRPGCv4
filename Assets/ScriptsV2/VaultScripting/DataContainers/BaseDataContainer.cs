using System;
using UnityEngine;

namespace VaultSystems.Data
{
    [DefaultExecutionOrder(-50)]
    [DisallowMultipleComponent]
    public abstract class BaseDataContainer : MonoBehaviour, IDataContainer
    {
        public bool SuppressDirtyEvents;
        public event Action OnDataChanged;

        public void MarkDirty()
        {
            if (SuppressDirtyEvents) return; // ⏸️ Can pause dirty notifications
            OnDataChanged?.Invoke();        // 📢 Fire the OnDataChanged event
        }

        // Default implementations using Hex helper
        public virtual string SerializeData()
        {
            return HexSerializationHelper.ToHex(this);
        }

        public virtual void DeserializeData(string data)
        {
            SuppressDirtyEvents = true;
            try
            {
                HexSerializationHelper.FromHex(this, data);
            }
            finally
            {
                SuppressDirtyEvents = false;
                MarkDirty();
            }
        }

        // Optional: JSON helpers for subclasses
        protected string SerializeToJson()
        {
            return JsonUtility.ToJson(this);
        }

        protected void DeserializeFromJson(string json)
        {
            JsonUtility.FromJsonOverwrite(json, this);
            MarkDirty();
        }

#if UNITY_EDITOR
        [ContextMenu("Print Serialized Data")]
        private void DebugPrint()
        {
            Debug.Log(SerializeData());
        }
#endif

        // Manual save/load shortcuts
        public void ForceSaveNow()
        {
            Debug.Log($"[BaseDataContainer] Force saving {name}");
            SerializeData();
        }

        public void ForceLoadNow()
        {
            Debug.Log($"[BaseDataContainer] Force loading {name}");
            // e.g. read from storage and call DeserializeData
        }
    }
}

using System.Collections;
using System;
using VaultSystems.Data;
using System.Linq;
using UnityEngine.SceneManagement;
namespace VaultSystems.Data
{

    public interface IAdvancedDataContainer : IDataContainer
    {
        float SaveFrequency { get; }
        int SavePriority { get; }
        int LoadPriority { get; }
        string OwnerID { get; }
        DirtyReason LastDirtyReason { get; }
        string LastHash { get; }

        IEnumerator RuntimeAsyncSetup();   // Async method for coroutines
        void MarkDirty(DirtyReason reason = DirtyReason.Generic);
    }
}

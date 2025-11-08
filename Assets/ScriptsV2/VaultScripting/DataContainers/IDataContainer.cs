using System;
using VaultSystems.Data;
using System.Linq;
using UnityEngine.SceneManagement;
namespace VaultSystems.Data
{
   
    
public interface IDataContainer
{
    string SerializeData();
    void DeserializeData(string data);

    // 🔔 Notify listeners (like DataContainerUpdater or Manager) when something changes
    event Action OnDataChanged;
}
}
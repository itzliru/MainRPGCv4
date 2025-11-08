using UnityEngine;
using System.Collections;
using System.Security.Cryptography;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;
using VaultSystems.Data;
using VaultSystems.Invoker;

namespace VaultSystems.Data
{
public class GameInit : MonoBehaviour
{
    [Header("Starting Scene / Cell")]
    public string startingScene = "Overworld";

    private void Awake()
    {
        // Ensure managers exist
        if (PersistentWorldManager.Instance == null)
        {
            var go = new GameObject("PersistentWorldManager");
            go.AddComponent<PersistentWorldManager>();
        }

        if (StreamingCellManager.Instance == null)
        {
            var go = new GameObject("StreamingCellManager");
            go.AddComponent<StreamingCellManager>();
        }

        // Load initial cell
        StreamingCellManager.Instance.EnterCell(startingScene);
    }
}
}
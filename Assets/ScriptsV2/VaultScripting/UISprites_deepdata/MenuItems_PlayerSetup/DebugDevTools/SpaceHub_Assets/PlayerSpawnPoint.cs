using System.Collections;
using System.Collections.Generic;

using UnityEngine;

public class PlayerSpawnPoint : MonoBehaviour
{
    public static Transform Instance;
    
    [Header("Spawn Configuration")]
    public string spawnId = "player_default";

    private void Awake()
    {
        Instance = transform;
    }
}

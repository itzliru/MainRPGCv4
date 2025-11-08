using System.Collections;
using UnityEngine;
using System;
using System.Collections.Generic;
using VaultSystems.Data;

namespace VaultSystems.Data
{

    [Serializable]
    public class QuantumArchive
    {
        public Dictionary<string, WorldObjectContainer> cells = new Dictionary<string, WorldObjectContainer>();
        public PlayerArchive playerData = new PlayerArchive();

        [Serializable]
        public class PlayerArchive
        {
            public string playerDataHex;           // Full serialized PlayerDataContainer
            public string playerId;                // "Lira_001", "Kinuee_001", etc.
            public string displayName;             // "Lira", "Kinuee", "Hos"
            public string characterType;           // "Lira", "Kinuee", "Hos" ← matches GetOutfits() switch
            public int outfitIndex;                // Which outfit/prefab to use
            public Vector3 lastPosition;
            public string lastScene;
            public string lastSpawnPointId;
        }
    }
}
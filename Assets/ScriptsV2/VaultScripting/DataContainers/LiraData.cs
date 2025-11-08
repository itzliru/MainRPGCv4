using System.Collections.Generic;
using UnityEngine;
using VaultSystems.Data;
using VaultSystems.Errors;
using VaultSystems.Invoker;
using UnityEngine.SceneManagement;
using VaultSystems.Controllers;
namespace VaultSystems.Data
{
/// <summary>
/// Lira's player data container. Persists across scenes and is registered as a runtime data container.
/// NOTE there are 3 character not just this so expect outfit and id and name and quest line to differ
/// </summary>
public class LiraData : PlayerDataContainer, IResettablePlayer
{
    // Optional lookup for other prefabs if you plan to swap characters dynamically
    public Dictionary<string, PlayerDataContainer> prefabLookup;

    public override string DefaultPlayerId => "Pc1:of0";
    public override string DefaultDisplayName => "Lira";
    public override string DefaultQuestLine => "QLA";
            public override Vector3 DefaultPosition => Vector3.zero;
        public override string DefaultCellId => "Overworld";
    [Header("Runtime Location Tracking")]
    public Vector3 lastKnownPosition;
    public string lastScene;



    private void Awake()
    {
        prefabLookup = new Dictionary<string, PlayerDataContainer>();

        // Ensure this object persists across scene loads
        DontDestroyOnLoad(gameObject);

        // Ensure UniqueId component exists
        var uid = GetComponent<UniqueId>();
        if (uid == null)
        {
                            uid = gameObject.AddComponent<UniqueId>();
           // VaultBreakpoint.Primitive(false, $"Missing UniqueId on {name}", this);
        }


        uid.isDataContainer = true;
        uid.manualId = "Pc1:of0"; // fixed ID for Lira

        // Register with the DataContainerManager
        DataContainerManager.Instance?.Register(this);

        // Initialize default data
        InitializeDefaults();
    }

    public override void InitializeDefaults()
{
    // 1️⃣ Assign base info
    playerId = "Pc1:of0"; // or Pc2:of0, Pc3:of0 etc.
    displayName = "Lira";
    outfitIndex = 0;

    // 2️⃣ Default stats
    currentHP = maxHP = 100;
    xp = 0;
    level = 1;

    CurrentDeathState = DeathState.Alive;


    // 3️⃣ World info
    mainQuestLine = "QLA";
    mainQuestStage = 0;
    completedSubquests = new List<string>();
    lastKnownPosition = Vector3.zero;
    lastCellId = "Overworld";
    isActivePlayer = true;
    factions = new List<string>();

    // 4️⃣ Auto-link manual readable ID
    var uid = GetComponent<UniqueId>() ?? gameObject.AddComponent<UniqueId>();
    uid.isDataContainer = true;
    // 5️⃣ Sync the readable manual ID into UniqueId
    uid.manualId = playerId; 


    // 6️⃣ Register to managers
    DataContainerManager.Instance?.Register(this);
    WorldBridgeSystem.Instance?.RegisterID(uid.GetID(), gameObject);

    Debug.Log($"[{displayName}] Initialized with manual ID: {uid.manualId}");
}


    private void OnDestroy()
    {
        DataContainerManager.Instance?.Unregister(this);
    }
public void RestoreFromLoad()
{
    // Step 1: Ensure UniqueId exists
    var uid = GetComponent<UniqueId>();
    if (uid == null)
        uid = gameObject.AddComponent<UniqueId>();

    uid.isDataContainer = true;
    uid.manualId = playerId;

    // Step 2: Register with managers
    DataContainerManager.Instance?.Register(this);
    WorldBridgeSystem.Instance?.RegisterID(uid.GetID(), gameObject);


    // Step 3: Check if we need to switch scene
    if (!string.IsNullOrEmpty(lastCellId) && lastCellId != SceneManager.GetActiveScene().name)
    {
        // Use StreamingCellManager to enter the correct scene
        if (StreamingCellManager.Instance != null)
        {
            StreamingCellManager.Instance.EnterCell(lastCellId, () =>
            {
                // Callback after scene is loaded
                transform.position = lastKnownPosition;
                Debug.Log($"[LiraData] Player restored to {lastCellId} at {lastKnownPosition}");
            });
        }
    }
    else
    {
        // Already in the correct scene
        transform.position = lastKnownPosition;
        
        Debug.Log($"[LiraData] Player restored in current scene at {lastKnownPosition}");
    }
}

    /// <summary>
    /// Resets only identity and quest defaults (useful for restarting character)
    /// </summary>
    public void ResetCharacter()
    {
        playerId = "Pc1:of0";
        displayName = "Lira";
        mainQuestLine = "QLA";
        mainQuestStage = 0;
        completedSubquests.Clear();
        currentHP = maxHP;
        xp = 0;
        level = 1;
        CurrentDeathState = DeathState.Alive;
        lastKnownPosition = Vector3.zero;
        lastCellId = "Overworld";
        isActivePlayer = true;
        factions.Clear();
        MarkDirty();
    }
}
}

using System.Collections.Generic;
using UnityEngine;
using VaultSystems.Data;
using VaultSystems.Invoker;
using VaultSystems.Errors;
using UnityEngine.SceneManagement;
using VaultSystems.Controllers;
namespace VaultSystems.Data
{
/// <summary>
///  Kinuee's player data container. Persists across scenes and is registered as a runtime data container.
/// </summary>
public class KinueeData : PlayerDataContainer, IResettablePlayer
{
    // Optional lookup for other prefabs if you plan to swap characters dynamically
    public Dictionary<string, PlayerDataContainer> prefabLookup;

    public override string DefaultPlayerId => "Pc2:of0";
    public override string DefaultDisplayName => "Kinuee";
    public override string DefaultQuestLine => "QLB";
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
        uid.manualId = "Pc2:of0"; // fixed ID for Lira

        // Register with the DataContainerManager
        DataContainerManager.Instance?.Register(this);

        // Initialize default data
        InitializeDefaults();
    }

    public void InitializeDefaults()
    {
        playerId = "Pc2:of0";
        displayName = "Kinuee";
        outfitIndex = 0;

        // Default stats
        currentHP = maxHP = 100;
        xp = 0;
        level = 1;
        CurrentDeathState = DeathState.Alive;

        // Quest
        mainQuestLine = "QLB";
        mainQuestStage = 0;
        completedSubquests = new List<string>();

        // World interaction
        lastKnownPosition = Vector3.zero;
        lastCellId = "Overworld";
        isActivePlayer = true;

        // Affiliations
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

    /// <summary>
    /// Resets only identity and quest defaults (useful for restarting character)
    /// </summary>
    public void ResetCharacter()
    {
        playerId = "Pc2:of0";
        displayName = "Kinuee";
        mainQuestLine = "QLB";
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

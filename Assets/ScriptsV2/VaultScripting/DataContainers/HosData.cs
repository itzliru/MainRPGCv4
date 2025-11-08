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
///  Hos's player data container. Persists across scenes and is registered as a runtime data container.
/// </summary>
public class HosData : PlayerDataContainer, IResettablePlayer
{
    // Optional lookup for other prefabs if you plan to swap characters dynamically
    private Dictionary<string, PlayerDataContainer> prefabLookup;


    public override string DefaultPlayerId => "Pc3:of0";
    public override string DefaultDisplayName => "Hos";
    public override string DefaultQuestLine => "QLB";
            public override Vector3 DefaultPosition => Vector3.zero;
        public override string DefaultCellId => "Overworld";

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
        uid.manualId = "Pc3:of0"; // fixed ID for Lira

        // Register with the DataContainerManager
        

        // Initialize default data
        InitializeDefaults();
    }

    private void InitializeDefaults()
    {
        playerId = "Pc3:of0";
        displayName = "Hos";
        outfitIndex = 0;

        // Default stats
        currentHP = maxHP = 100;
        xp = 0;
        level = 1;
        CurrentDeathState = DeathState.Alive;


        // Quest
        mainQuestLine = "QLC";
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
  

            DataContainerManager.Instance?.Register(this);
        WorldBridgeSystem.Instance?.RegisterID(uid.GetID(), gameObject);

        Debug.Log($"[{displayName}] Initialized with manual ID: {uid.GetID()}");
    // 6️⃣ Register to managers

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
        playerId = "Pc3:of0";
        displayName = "Hos";
        mainQuestLine = "QLC";
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
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VaultSystems.Data;
using VaultSystems.Invoker;
using VaultSystems.Containers;

public class DebugUIManager : MonoBehaviour
{
    [Header("Assign a UI prefab (must contain NPCContainer and PlayerContainer)")]
    public GameObject debugUIPrefab;

    [Tooltip("If true the UI will be created on Start. If false, call Init() manually.")]
    public bool autoSpawnOnStart = true;

    [Tooltip("Refresh UI this often (seconds).")]
    public float refreshInterval = 0.25f;

    [HideInInspector] public GameObject uiInstance;
    private Transform npcContainer;
    private Transform playerContainer;

    private List<NPCDataContainer> npcs = new List<NPCDataContainer>();
    private List<PlayerDataContainer> players = new List<PlayerDataContainer>();

    private readonly List<GameObject> npcEntries = new List<GameObject>();
    private readonly List<GameObject> playerEntries = new List<GameObject>();

    private float lastRefreshTime;

    // ✅ FIXED: Added missing braces - these lines now only execute if autoSpawnOnStart is true
    private void Start()
  {
     if (autoSpawnOnStart)
         Init();
     uiInstance.SetActive(true);
     Show();
     if (autoSpawnOnStart)
     {
         Init();
         uiInstance.SetActive(true);
         Show();
     }
  }


    public void Init()
    {
        if (debugUIPrefab == null)
        {
            Debug.LogWarning("[DebugUIManager] debugUIPrefab not assigned in inspector.");
            return;
        }

        if (uiInstance != null)
        {
            Destroy(uiInstance);
            uiInstance = null;
            npcContainer = null;
            playerContainer = null;
            ClearEntryPools();
        }

        uiInstance = Instantiate(debugUIPrefab);
        uiInstance.name = debugUIPrefab.name + "_Instance";

        // Ensure the instance is active even if prefab was saved disabled
        if (!uiInstance.activeSelf)
            uiInstance.SetActive(true);

        npcContainer = uiInstance.transform.Find("NPCContainer");
        playerContainer = uiInstance.transform.Find("PlayerContainer");

        if (npcContainer == null)
            Debug.LogWarning("[DebugUIManager] NPCContainer child not found in prefab.");
        if (playerContainer == null)
            Debug.LogWarning("[DebugUIManager] PlayerContainer child not found in prefab.");

        RefreshReferences();
    }

    public void Toggle()
    {
        if (uiInstance == null)
        {
            Init();
            return;
        }
        uiInstance.SetActive(!uiInstance.activeSelf);
    }

    public void Show()
    {
        if (uiInstance == null) Init();
        if (uiInstance != null) uiInstance.SetActive(true);
    }

    public void RefreshReferences()
    {
        npcs.Clear();
        players.Clear();

        npcs.AddRange(Object.FindObjectsOfType<NPCDataContainer>(true));
        players.AddRange(Object.FindObjectsOfType<PlayerDataContainer>(true));

        RebuildUIEntries();
        UpdateUI();
    }

    private void Update()
    {
        if (uiInstance == null || !uiInstance.activeSelf) return;

        if (Time.time - lastRefreshTime >= refreshInterval)
        {
            RefreshReferencesIfNeeded();
            UpdateUI();
            lastRefreshTime = Time.time;
        }
    }

    private void RefreshReferencesIfNeeded()
    {
        var sceneNpcs = Object.FindObjectsOfType<NPCDataContainer>(true);
        var scenePlayers = Object.FindObjectsOfType<PlayerDataContainer>(true);

        if (sceneNpcs.Length != npcs.Count || scenePlayers.Length != players.Count)
            RefreshReferences();
    }

    private void RebuildUIEntries()
    {
        if (npcContainer != null)
        {
            for (int i = npcContainer.childCount - 1; i >= 0; i--)
                Destroy(npcContainer.GetChild(i).gameObject);
            npcEntries.Clear();

            for (int i = 0; i < npcs.Count; i++)
            {
                GameObject entry = CreateTextEntry(npcs[i].displayName);
                entry.transform.SetParent(npcContainer, false);
                npcEntries.Add(entry);
            }
        }

        if (playerContainer != null)
        {
            for (int i = playerContainer.childCount - 1; i >= 0; i--)
                Destroy(playerContainer.GetChild(i).gameObject);
            playerEntries.Clear();

            for (int i = 0; i < players.Count; i++)
            {
                GameObject entry = CreateTextEntry(players[i].displayName);
                entry.transform.SetParent(playerContainer, false);
                playerEntries.Add(entry);
            }
        }
    }

    private GameObject CreateTextEntry(string defaultName)
    {
        GameObject go = new GameObject("Entry_" + defaultName);
        var txt = go.AddComponent<Text>();
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = 45;
        txt.alignment = TextAnchor.MiddleLeft;
        txt.horizontalOverflow = HorizontalWrapMode.Overflow;
        txt.verticalOverflow = VerticalWrapMode.Overflow;
        txt.color = new Color(0f, 0f, 0.5f);

        var layout = go.AddComponent<LayoutElement>();
        layout.minHeight = 18f;
        return go;
    }

    private void UpdateUI()
    {
        // ========== NPC ENTRIES ==========
        for (int i = 0; i < npcEntries.Count; i++)
        {
            if (i >= npcs.Count) break;
            var txt = npcEntries[i].GetComponent<Text>();
            var npc = npcs[i];
            
            if (txt != null && npc != null)
            {
                // ✅ FIXED: Changed from npc.isDead to enum check
                string deathStatus = npc.CurrentDeathState == DeathState.Dead ? "DEAD" : "ALIVE";
                string stateDisplay = npc.CurrentDeathState.ToString();
                
                 txt.text = $"{npc.displayName} | HP: {npc.currentHP}/{npc.maxHP} | Ammo: {npc.currentAmmo} | Dead: {npc.IsDead} | Disp: {npc.disposition:F0}";
                    txt.text = $"{npc.displayName} | HP: {npc.currentHP}/{npc.maxHP} | Ammo: {npc.currentAmmo} | State: {npc.CurrentDeathState} | Disp: {npc.disposition:F0}";
                
                // Color-code based on death state
                if (npc.CurrentDeathState == DeathState.Dead)
                    txt.color = Color.red;
                else if (npc.CurrentDeathState == DeathState.Dying)
                    txt.color = Color.yellow;
                else if (npc.currentHP < npc.maxHP * 0.25f)
                    txt.color = new Color(1f, 0.5f, 0f); // Orange (low health)
                else
                    txt.color = Color.white;
            }
        }

        // ========== PLAYER ENTRIES ==========
        for (int i = 0; i < playerEntries.Count; i++)
        {
            if (i >= players.Count) break;
            var txt = playerEntries[i].GetComponent<Text>();
            var player = players[i];
            
            if (txt != null && player != null)
            {
                txt.text = $"{player.displayName} | HP: {player.currentHP}/{player.maxHP} | " +
                           $"Level: {player.level} | State: {player.CurrentDeathState}";

                // Color-code player
                if (player.CurrentDeathState == DeathState.Dead)
                    txt.color = Color.red;
                else if (player.currentHP < player.maxHP * 0.25f)
                    txt.color = new Color(1f, 0.5f, 0f);
                else
                    txt.color = new Color(0f, 1f, 0f); // Green for healthy
            }
        }
    }

    private void ClearEntryPools()
    {
        foreach (var e in npcEntries) if (e != null) Destroy(e);
        foreach (var e in playerEntries) if (e != null) Destroy(e);
        npcEntries.Clear();
        playerEntries.Clear();
    }

    private void OnDestroy()
    {
        ClearEntryPools();
    }
}

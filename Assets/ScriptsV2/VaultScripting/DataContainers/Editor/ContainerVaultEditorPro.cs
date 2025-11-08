#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using VaultSystems.Data;
using VaultSystems.Containers;
/// <summary>
/// Container Vault Pro – an in-editor dashboard for viewing and managing
/// BaseDataContainer objects in the scene, with runtime sync and filters.
/// </summary>
public class ContainerVaultEditorPro : EditorWindow
{
    private Vector2 scrollPos;
    private string searchFilter = "";
    private BaseDataContainer selectedContainer;

    private bool showNPCs = true;
    private bool showPlayers = true;
    private bool showOthers = true;

    private double lastCacheTime;
    private List<BaseDataContainer> cachedContainers = new();

    private readonly List<string> availableFactions = new() { "Faction01", "Faction02", "Faction03" };

    [MenuItem("Game Tools/Container Vault Pro")]
    public static void ShowWindow()
    {
        GetWindow<ContainerVaultEditorPro>("📦 Container Vault Pro");
    }

    private void OnFocus()
    {
        RefreshCache();
    }

    private void OnGUI()
    {
        GUILayout.Label("📦 Container Vault Pro", EditorStyles.largeLabel);
        EditorGUILayout.Space(5);

        if (DataContainerManager.Instance == null)
        {
            EditorGUILayout.HelpBox("No DataContainerManager found in scene.", MessageType.Warning);
            if (GUILayout.Button("Refresh"))
                RefreshCache();
            return;
        }

        DrawSearchAndFilter();
        DrawContainerList();
        DrawSelectedContainerActions();
    }

    // ------------------------
    // 🔍 Search + Filter
    // ------------------------
    private void DrawSearchAndFilter()
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Search (Name / ID):", GUILayout.Width(130));
        searchFilter = GUILayout.TextField(searchFilter);
        if (GUILayout.Button("🔄", GUILayout.Width(30)))
            RefreshCache();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        showNPCs = GUILayout.Toggle(showNPCs, "NPCs", GUILayout.Width(80));
        showPlayers = GUILayout.Toggle(showPlayers, "Players", GUILayout.Width(80));
        showOthers = GUILayout.Toggle(showOthers, "Others", GUILayout.Width(80));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);
    }

    // ------------------------
    // 📋 Container List
    // ------------------------
    private void DrawContainerList()
    {
        // Auto refresh every second if in play mode
        if (EditorApplication.isPlaying && EditorApplication.timeSinceStartup - lastCacheTime > 1.0)
            RefreshCache();

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(320));

        foreach (var container in cachedContainers.ToList())
        {
            if (container == null)
            {
                cachedContainers.Remove(container);
                continue;
            }

            // Filtering
            if (container is NPCDataContainer && !showNPCs) continue;
            if (container is PlayerDataContainer && !showPlayers) continue;
            if (!(container is NPCDataContainer) && !(container is PlayerDataContainer) && !showOthers) continue;

            // Build display text
            string displayText = container.name;
            string id = "";

            if (container is NPCDataContainer npc)
            {
                id = npc.npcId;
                displayText += $" | {npc.displayName} | {npc.npcId}";
            }
            else if (container is PlayerDataContainer player)
            {
                id = player.playerId;
                displayText += $" | {player.displayName} | {player.playerId}";
            }

            if (!string.IsNullOrEmpty(searchFilter) &&
                !displayText.ToLower().Contains(searchFilter.ToLower()) &&
                !id.ToLower().Contains(searchFilter.ToLower()))
                continue;

            DrawContainerEntry(container);
        }

        EditorGUILayout.EndScrollView();
    }

    // ------------------------
    // 📦 Container Entry
    // ------------------------
    private void DrawContainerEntry(BaseDataContainer container)
    {
        bool isSelected = selectedContainer == container;
        GUI.backgroundColor = isSelected ? new Color(0.5f, 0.8f, 1f, 0.3f) : Color.white;

        GUILayout.BeginVertical("box");
        GUI.backgroundColor = Color.white;

        GUILayout.BeginHorizontal();
        GUILayout.Label($"🧱 {container.name}", EditorStyles.boldLabel);

        if (GUILayout.Button("Select", GUILayout.Width(60)))
        {
            selectedContainer = container;
            Selection.activeGameObject = container.gameObject;
        }

        if (GUILayout.Button("View", GUILayout.Width(60)))
        {
            if (SceneView.lastActiveSceneView != null)
            {
                SceneView.lastActiveSceneView.pivot = container.transform.position;
                SceneView.lastActiveSceneView.Repaint();
            }
            else
            {
                Debug.LogWarning("[ContainerVault] No active SceneView to focus.");
            }
        }

        if (GUILayout.Button(container.gameObject.activeSelf ? "Hide" : "Show", GUILayout.Width(60)))
        {
            container.gameObject.SetActive(!container.gameObject.activeSelf);
        }

        GUILayout.EndHorizontal();

        // Detail Panel
        if (isSelected)
        {
            EditorGUI.indentLevel++;
            if (container is NPCDataContainer npc) DrawNPCEditor(npc);
            else if (container is PlayerDataContainer player) DrawPlayerEditor(player);
            else EditorGUILayout.LabelField("Unknown container type.");
            EditorGUI.indentLevel--;
        }

        GUILayout.EndVertical();
    }

    // ------------------------
    // 🧍 NPC Inspector
    // ------------------------
    // 🧍 NPC Inspector
private void DrawNPCEditor(NPCDataContainer npc)
{
    EditorGUILayout.LabelField("🆔 Identity", EditorStyles.boldLabel);
    npc.displayName = EditorGUILayout.TextField("Display Name", npc.displayName);
    npc.npcId = EditorGUILayout.TextField("NPC ID", npc.npcId);

    EditorGUILayout.Space(3);
    EditorGUILayout.LabelField("❤️ Status", EditorStyles.boldLabel);
    npc.currentHP = EditorGUILayout.IntField("Current HP", npc.currentHP);
    npc.maxHP = EditorGUILayout.IntField("Max HP", npc.maxHP);
    
    // ✅ Refactored: IsDead is now read-only property (based on CurrentDeathState)
    EditorGUILayout.LabelField($"Death State: {npc.CurrentDeathState}", EditorStyles.label);
    EditorGUILayout.LabelField($"Is Dead: {(npc.IsDead ? "✅ YES" : "❌ NO")}", EditorStyles.label);
    
    // Quick state action buttons
    EditorGUILayout.BeginHorizontal();
    if (GUILayout.Button("💚 Revive", GUILayout.Width(100)))
        npc.Revive();
    if (GUILayout.Button("💀 Ragdoll", GUILayout.Width(100)))
        npc.EnableRagdoll();
    if (GUILayout.Button("⚰️ Dead", GUILayout.Width(100)))
        npc.DisableRagdoll();
    EditorGUILayout.EndHorizontal();

    EditorGUILayout.Space(3);
    EditorGUILayout.LabelField("⚔️ Factions / Behavior", EditorStyles.boldLabel);
    npc.isEnemy = EditorGUILayout.Toggle("Enemy", npc.isEnemy);
    npc.isNeutral = EditorGUILayout.Toggle("Neutral", npc.isNeutral);
    npc.isFriendly = EditorGUILayout.Toggle("Friendly", npc.isFriendly);

    DrawFactionCheckboxes(npc.factions);

    if (GUILayout.Button("💾 Save NPC"))
    {
        EditorUtility.SetDirty(npc);
        Debug.Log($"[ContainerVault] Saved NPC {npc.displayName} ({npc.npcId})");
    }
}


    // ------------------------
    // 🧍‍♂️ Player Inspector
    // ------------------------
    private void DrawPlayerEditor(PlayerDataContainer player)
    {
        EditorGUILayout.LabelField("🆔 Player Identity", EditorStyles.boldLabel);
        player.displayName = EditorGUILayout.TextField("Display Name", player.displayName);
        player.playerId = EditorGUILayout.TextField("Player ID", player.playerId);

        EditorGUILayout.Space(3);
        EditorGUILayout.LabelField("📊 Stats", EditorStyles.boldLabel);
        player.currentHP = EditorGUILayout.IntField("Current HP", player.currentHP);
        player.maxHP = EditorGUILayout.IntField("Max HP", player.maxHP);
        player.level = EditorGUILayout.IntField("Level", player.level);
        player.xp = EditorGUILayout.IntField("XP", player.xp);

        DrawFactionCheckboxes(player.factions);

        if (GUILayout.Button("💾 Save Player"))
        {
            EditorUtility.SetDirty(player);
            Debug.Log($"[ContainerVault] Saved Player {player.displayName} ({player.playerId})");
        }
    }

    // ------------------------
    // 🏷️ Faction Checkbox UI
    // ------------------------
    private void DrawFactionCheckboxes(List<string> factions)
    {
        if (factions == null) return;
        EditorGUILayout.Space(3);
        EditorGUILayout.LabelField("🏳️ Factions", EditorStyles.boldLabel);
        for (int i = 0; i < availableFactions.Count; i++)
        {
            bool hasFaction = factions.Contains(availableFactions[i]);
            bool toggle = EditorGUILayout.Toggle(availableFactions[i], hasFaction);
            if (toggle && !hasFaction)
                factions.Add(availableFactions[i]);
            else if (!toggle && hasFaction)
                factions.Remove(availableFactions[i]);
        }
    }

    // ------------------------
    // ⚙️ Bulk + Tools
    // ------------------------
    private void DrawSelectedContainerActions()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("⚙️ Bulk Tools", EditorStyles.boldLabel);

        if (GUILayout.Button("💾 Save All Containers"))
        {
            DataContainerManager.Instance.SaveAll();
            EditorUtility.DisplayDialog("ContainerVault", "All containers saved successfully!", "OK");
        }

        if (GUILayout.Button("❤️ Heal All NPCs"))
        {
            foreach (var c in cachedContainers.OfType<NPCDataContainer>())
            {
                c.SetHP(c.maxHP);
                EditorUtility.SetDirty(c);
            }
            Debug.Log("[ContainerVault] Healed all NPCs.");
        }

        if (GUILayout.Button("🧹 Purge Null / Missing Entries"))
        {
            cachedContainers.RemoveAll(c => c == null);
            Debug.Log("[ContainerVault] Purged null containers from cache.");
        }

        if (GUILayout.Button("🔍 Verify Unique IDs"))
        {
            foreach (var c in cachedContainers)
            {
                if (c == null) continue;
                var uid = c.GetComponent<UniqueId>();
                if (uid != null)
                    uid.SetID(uid.GetID());
            }
            Debug.Log("[ContainerVault] Verified Unique IDs.");
        }
    }

    // ------------------------
    // 🧠 Helpers
    // ------------------------
    private void RefreshCache()
    {
        if (DataContainerManager.Instance == null)
        {
            cachedContainers.Clear();
            return;
        }

        cachedContainers = DataContainerManager.Instance.GetAllContainers()?.Where(c => c != null).ToList() ?? new List<BaseDataContainer>();
        lastCacheTime = EditorApplication.timeSinceStartup;
    }
}
#endif

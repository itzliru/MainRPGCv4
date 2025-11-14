# Copilot / Assistant Instructions (repository guidance)

Keep this short and actionable — what an AI coding agent needs to be productive in this Unity repository.

Assistant note (short): prefer the helpers in the `VaultSystems.Components` namespace for hierarchical lookups and use the `VerboseLogger`/`HierarchyLogger` helpers for diagnostics. When copying examples, add:

```csharp
using VaultSystems.Components;
```

Summary
- Unity project. Key code lives under `Assets/ScriptsV2/VaultScripting/` and `Assets/Scripts/` (engine managers under `Assets/Scripts/VaultSystems/Managers/`). Editor custom inspectors usually sit next to runtime types or under `Assets/Editor`.

High-level architecture notes
- Marker system: `WorldMarker` instances register with a static `MarkerSystem`. UI mapping is implemented in `MapUIManager`.
- Data & namespaces: code uses `VaultSystems.Invoker`, `VaultSystems.Data`, and other `VaultSystems.*` namespaces for shared services.

Preferred helper methods (use these first)
- `VaultSystems.Components.ComponentExtensions` provides convenient hierarchical lookup helpers. Prefer these over long `GetComponent`/`GetComponentInChildren` chains:
  - `TryGet<T>(out T result, string name = null)` — full-hierarchy, Unity-style out param
  - `TryGetComponent<T>(string name)` — returns `T` or `null` (convenient shorthand)
  - `TryGetComponentInChildren<T>(...)`, `TryGetComponentInParent<T>(...)`, `FindDeep<T>(...)`, `FindByPath<T>(...)`

Logging & diagnostics
- Use `HierarchyLogger` for bone/hierarchy dumps:
  - `HierarchyLogger.LogFullHierarchy(this.transform, "Assets/Resources/Debug/HierarchyFullLog.txt");`
- Use the `VerboseLogger` singleton for file-backed verbose logs. Use a local shorthand when appropriate:

```csharp
private VerboseLogger? DebugLogger => VerboseLogger.Instance;
DebugLogger?.Log("Diagnostic message");
```

Typical patterns and examples
- Auto-wiring / lookups in `Awake` or `Initialize`:

```csharp
using VaultSystems.Components; // make extension methods visible

// find Animator anywhere under/above this component
if (this.TryGet(out Animator animator)) { /* use animator */ }

// find a named child socket and align
var gunSocket = this.TryGetComponent<Transform>("GunSocket");
if (gunSocket != null) gunSocket.localPosition = Vector3.zero;
```

- Use `InvokeComponents(transform)` to get an `InvokedComponents` struct that bundles common player components (PlayerAnimator1, PlayerController, GunController, PlayerDataContainer).

Performance & safety notes
- These helpers walk the hierarchy and may allocate (LINQ). Cache results for repeated use.
- Name-based lookups require stable GameObject names—prefer type lookups unless multiple same-type components exist.
- `GetComponentsInChildren(..., true)` includes inactive objects—use intentionally.

Editor vs runtime
- Guard editor-only code (PrefabUtility, baking) with the `UNITY_EDITOR` build symbol to avoid runtime references.
- Place editor UI under `Assets/Editor` and document bake steps in README.

Where to look (high-value files)
- Component helpers: `Assets/ScriptsV2/VaultScripting/componentExtensions.cs`
- Hierarchy dump: `Assets/Scripts/VaultSystems/Debug/HierarchyLogger.cs`
- Verbose logging: `Assets/Scripts/VaultSystems/Debug/VerboseLogger.cs`

Quick checklist for the assistant
1. Try `VaultSystems.Components` helpers first for wiring components.
2. Use `VerboseLogger.Instance` via `DebugLogger` shorthand for file-backed logs.
3. Use `HierarchyLogger` for structure/bone dumps.
4. Cache lookups performed every frame.
5. If a name-based lookup fails, ask about naming conventions (common names: `GunSocket`, `RightHandIKTarget`, `PlayerAnimator`).

---

## Event system & PlayerCase patterns (copy-paste examples)

### PlayerCase multi-case checks

Use `PlayerCaseController.AreAnyCasesActive(...)` to check if any of several cases are active:

```csharp
using VaultSystems.Invoker;

// Check if Combat OR Aim is active
if (PlayerCaseController.AreAnyCasesActive(PlayerCaseController.PlayerCase.Combat, PlayerCaseController.PlayerCase.Aim))
{
    // Player is in combat/aiming mode
    targetIKWeight = 1f;
}
else
{
    // Player is not in combat/aiming
    targetIKWeight = 0f;
}

// Check a single case (shorthand)
if (PlayerCaseController.Instance?.HasCase(PlayerCaseController.PlayerCase.Dialogue) == true)
{
    // Dialogue active
}
```

### WorldBridge event registration (InvokeKey/PayInvoke)

Register event handlers with `WorldBridgeSystem.Instance.RegisterInvoker(...)`. Returns an `IDisposable` token you can use to unregister:

```csharp
using VaultSystems.Invoker;
using VaultSystems.Containers;

private IDisposable _cellChangedToken;

void Start()
{
    // Register a named method (preferred for clarity and debugging)
    _cellChangedToken = WorldBridgeSystem.Instance?.RegisterInvoker(
        EventKeys.Scene.CELL_CHANGED,               // Event key (see EventKeys class)
        OnCellChanged,                              // Named method (not lambda!)
        DynamicDictionaryInvoker.Layer.Func,        // Execution layer (enum inside DynamicDictionaryInvoker)
        id: "vision_batch_cell_tracker",            // Debugging ID (optional, helps identify registrations)
        metadata: "vision_batching"                 // Metadata (optional, any object)
    );
}

void OnCellChanged(object[] args)
{
    // Handle cell changed event
    var cellId = args.Length > 0 ? args[0] as string : null;
    Debug.Log($"Cell changed to {cellId}");
}

void OnDestroy()
{
    // Clean up: dispose the token to unregister
    _cellChangedToken?.Dispose();
}
```

**Broadcast events** with `InvokeKey`:

```csharp
// Fire an event (all registered handlers will execute)
WorldBridgeSystem.Instance?.InvokeKey(EventKeys.Scene.CELL_CHANGED, "cell_0x0001");

// Pass multiple arguments
WorldBridgeSystem.Instance?.InvokeKey(
    EventKeys.Player.SPAWNED, 
    playerUid, 
    playerPosition, 
    playerLevel
);
```

**PayInvoke** (conditional/transactional invoke):

```csharp
// PayInvoke returns true if any handler executed
bool handled = WorldBridgeSystem.Instance?.PayInvoke(
    EventKeys.Player.WEAPON_FIRED, 
    token: someTokenObject, 
    "weaponID", 
    damageAmount
) ?? false;

if (handled)
{
    Debug.Log("Weapon fire event was consumed by a handler");
}
```

### UID event helpers (scoped events per entity)

Use `UIDEventHelpers` to register/broadcast events scoped to a specific UID (e.g., per-NPC or per-player):

```csharp
using VaultSystems.Invoker;
using VaultSystems.Data;

private IDisposable _uidHealthToken;

void Start()
{
    var uid = GetComponent<UniqueId>();
    if (uid == null) return;

    // Register a UID-scoped handler (key format: "uid:<uid>:<eventKey>")
    _uidHealthToken = UIDEventHelpers.RegisterUIDInvoker(
        uid,                                        // UniqueId component (or uid.GetID() string)
        EventKeys.HEALTH_CHANGED,                   // Event key (no prefix needed)
        OnHealthChanged,                            // Handler
        DynamicDictionaryInvoker.Layer.Func,
        id: "npc_health_listener",
        metadata: null
    );
}

void OnHealthChanged(object[] args)
{
    int newHP = args.Length > 0 ? (int)args[0] : 0;
    int maxHP = args.Length > 1 ? (int)args[1] : 0;
    Debug.Log($"Health changed: {newHP}/{maxHP}");
}

void OnDestroy()
{
    _uidHealthToken?.Dispose();
}
```

**Broadcast UID-scoped events**:

```csharp
var uid = GetComponent<UniqueId>();

// Broadcast to UID-scoped key only
UIDEventHelpers.BroadcastUIDEvent(uid, EventKeys.HEALTH_CHANGED, new object[] { currentHP, maxHP });

// Broadcast to UID-scoped key AND global key (UID is passed as first arg to global)
UIDEventHelpers.BroadcastUIDEvent(
    uid, 
    EventKeys.DIED, 
    new object[] { killerUID }, 
    alsoInvokeGlobal: true
);
```

**PayInvoke UID events**:

```csharp
bool consumed = UIDEventHelpers.PayUIDEvent(uid, EventKeys.DAMAGED, token: damageSource, damageAmount);
```

### Common event keys (use EventKeys class)

```csharp
using VaultSystems.Containers;

// Player events (global)
EventKeys.Player.SPAWNED
EventKeys.Player.LEVEL_UP
EventKeys.Player.WEAPON_FIRED

// Scene events (global)
EventKeys.Scene.CELL_CHANGED
EventKeys.Scene.LOADED

// Prefixed (per-entity, use helpers to compose full key)
EventKeys.GetHealthKey("npc_bandit_01")  // "npc_bandit_01_hp_changed"
EventKeys.GetDamagedKey("player_001")    // "player_001_damaged"
```

---

Last updated: 2025-11-14
---


---

# My Agent


# Copilot instructions for this repository

Keep this short and actionable — what an AI coding agent needs to be productive here.

Summary
- This is a Unity project. Key gameplay/UI code lives under:
  - `Assets/ScriptsV2/VaultScripting/LoadingCellStuff/` (Map UI, marker code)
  - `Assets/ScriptsV2/VaultScripting/DataContainers` (Event's, player npc, managers, worldbridge data, dynamic dictionary invoker)
  - `Assets/Scripts/VaultSystems/Managers/` (engine managers like VisionBatchManager)
    - `Assets/Scripts/` (Normal Scripts folder other stuff)
        - `Assets/ScriptsV2/` (Secondary Scripts folder)
            - `Assets/ScriptsV2/VaultScripting/SaveSystem` (Main backend scripts for save system)
  - Editor custom inspectors live next to runtime types (e.g. `MapUIManagerEditor.cs`).

High-level architecture notes
- Marker system: `WorldMarker` instances are registered with a static `MarkerSystem` (HashSet-based registry). UI is driven by `MapUIManager` which maps `WorldMarker -> RectTransform` using `markerUIElements`.
- Map UI: `MapUIManager` exposes serialized public fields (markerPrefab, markerSprites, mapContainer, mapScale, xorKey, useXORPositioning). Editor script `MapUIManagerEditor` customizes the inspector. note add other systems in instructions, in a manageable way.

- Data & namespaces: code uses `VaultSystems.Invoker` and `VaultSystems.Data` — look for other `VaultSystems.*` files for cross-cutting services.

Project-specific patterns and conventions
- Serialization-first: most configuration is via public fields on MonoBehaviours. Avoid renaming serialized fields unless you provide a migration path.
- Editor + Runtime pairs: Editor classes are in the same folder and use `[CustomEditor(typeof(X))]` — when changing runtime fields, update the editor UI accordingly.


Developer workflows (minimal)
- Open project with Unity Editor / Unity Hub. Make changes in an IDE (VSCode/Visual Studio) and save.
- Build from CLI (example):
  - Unity 2022.3.51f+: `Unity -batchmode -quit -projectPath "<path>" -buildTarget <Target> -executeMethod <BuildMethod>`

Testing & debugging pointers
- There are no obvious test projects in the repo. Use Unity Play Mode / Editor to test Map UI and marker behavior.
- When modifying serialized fields, remember to update scenes or prefabs that rely on them.

Files to read first (high-value)
- `Assets/ScriptsV2/VaultScripting/SaveSystem/` — Main save system.
- `Assets/ScriptsV2/VaultScripting/LoadingCellStuff/` — Cell logic for map markers and cell stuff.
- `Assets/ScriptsV2/VaultScripting/DataContainers/` — data.
- `Assets/Scripts/VaultSystems/Managers/VisionBatchManager.cs` — ALSO CHECK GOAP FOLDER needs updateing and help with logic.

Recent edits you should know about
- `SocketManager` now exposes a small registration API so systems can register additive sockets by id (RegisterSocket/UnregisterSocket, EnableSocketById/DisableSocketById, GetCameraById). Use `SocketManager.Instance.GetCameraById("Out")` or `EnableSocketById("Out")` from other scripts.
- `PlayerCase` is a nested enum on `PlayerCaseController` (use `PlayerCaseController.PlayerCase.<Name>` when referring to it from other classes).

When editing
- Preserve public field names. If you must change serialized fields, add [FormerlySerializedAs("oldName")] or provide a migration path.
- Update custom editors near runtime classes to keep inspectors in sync.
- Follow existing naming conventions and folder structures for new scripts.
- ask before adding new dependencies or packages.

Committing changes / which repo to push to
- I can create or update these scripts locally in this workspace. I cannot push to a remote for you, but I can produce the exact git commands to run from PowerShell.
- Which remote should I target when I give commands? `MainRPGCv4`. Pick the branch that represents - the main development branch; if unsure, use `MainRPGCv4` Master branch... stable position, Main will  be final output Master will be stabel development branch. the branches are: ?  Main : master
- Local Computer branch is under E:/MainRPGC/Rpgv3 there is the .git folder and all files.
- Net Branch is under https:/Github.com/itzliru/MainRPGCv4.git Branch pushed as master
- if files have been "changed" as in path changes and are ready to be committed. make a new branch for the changes, otherwise if the paths are still the same and files are changed and are ready to be commited, commit them to master origin.
If you'd like, I will generate the exact PowerShell commands to add, commit and push the changed files to the repo you choose.
Example git remote -v output from local computer repo:
- origin  https://github.com/itzliru/MainRPGCv4 (fetch)
- origin  https://github.com/itzliru/MainRPGCv4 (push)
- PS E:\MainRPGC\Rpgv3>



If anything above is missing or incorrect, tell me the specific files you want called out or examples you want added and I'll iterate.


# Copilot / Assistant Instructions (repository guidance)

Use this document as a concise reference for automated assistants and contributors working in this repository.

## Namespace & helper usage
- Prefer the extension helpers defined in `VaultSystems.Components.ComponentExtensions` for component lookups and hierarchy searches. These helpers simplify common patterns like "find by type or name" and fall back to parent/child searches.

- When searching for components prefer these methods over repeated `GetComponentInChildren`/`GetComponentInParent` chains:
  - `TryGet<T>(out T result, string name = null)` — full-hierarchy, Unity-style out parameter
  - `TryGetComponent<T>(string name)` — returns `T` or `null` (convenient for concise code)
  - `TryGetComponentInChildren<T>(...)`, `TryGetComponentInParent<T>(...)`, `FindDeep<T>(...)`, `FindByPath<T>(...)`

## Logging and hierarchy diagnostics
- Use the static `HierarchyLogger` for bone/hierarchy dumps and structure logging. Example:
  - `HierarchyLogger.LogFullHierarchy(this.transform, "Assets/Resources/Debug/HierarchyFullLog.txt");`

- Use the `VerboseLogger` singleton for file-backed verbose logging. Use a local shorthand when frequent logging is needed:

```csharp
private VerboseLogger? DebugLogger => VerboseLogger.Instance;

// then:
DebugLogger?.Log("Some diagnostic message");
```

## Typical patterns and examples
- Auto-wiring / lookup in MonoBehaviours (Awake/Initialize):

```csharp
// find animator anywhere under/above this component
if (this.TryGet(out Animator animator)) { /* use animator */ }

// find a named child socket
var gunSocket = this.TryGetComponent<Transform>("GunSocket");
if (gunSocket != null) gunSocket.localPosition = Vector3.zero;
```

- Use `InvokeComponents(transform)` to get a grouped `InvokedComponents` struct that contains common player components (PlayerAnimator1, PlayerController, GunController, PlayerDataContainer). This is handy for initial wiring on spawned prefabs.

## Performance & safety notes
- These helpers walk the hierarchy and may allocate (LINQ). Cache results for repeated use (store references in Awake/Initialize).
- Name-based lookups require stable GameObject names—prefer type lookups unless multiple same-type components exist.
- `GetComponentsInChildren(..., true)` includes inactive objects—use that intentionally when you need disabled objects included.

## Editor vs Runtime
- Methods that interact with PrefabUtility or perform baking should be guarded with UNITY_EDITOR conditional compilation (wrap editor-only code using the UNITY_EDITOR build symbol) so runtime builds are not impacted.
- For editor-only authoring tools (socket baking, mapping), keep UI code under `Assets/Editor` and document bake steps in README.

## Where to look
- Component helpers: `Assets/ScriptsV2/VaultScripting/componentExtensions.cs`
- Hierarchy dump: `Assets/Scripts/VaultSystems/Debug/HierarchyLogger.cs`
- Verbose logging: `Assets/Scripts/VaultSystems/Debug/VerboseLogger.cs`

## Quick checklist for the assistant
1. When wiring components, try the `VaultSystems.Components` extensions first. (They are in namespace `VaultSystems.Components`.)
2. Use `VerboseLogger.Instance` via `DebugLogger` shorthand for file-backed logs.
3. Use `HierarchyLogger` for structure/bone dumps.
4. Cache lookups performed every frame.
5. Ask for missing naming conventions if a name-based lookup fails—project uses `GunSocket`, `RightHandIKTarget`, `PlayerAnimator` etc. in several prefabs.

---
Last updated: 2025-11-14


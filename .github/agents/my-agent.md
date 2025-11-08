---
name: Mystereo42
description: Copilot Agent
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

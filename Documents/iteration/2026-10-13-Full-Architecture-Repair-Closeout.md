# Full Architecture Repair Closeout

Date: 2026-10-13
Project: Zephyr
Scope: Continue the independent architecture repair and remove the remaining business-level coupling identified in the latest reassessment.

## Result

The repair is accepted as complete for the current architecture scope.

- Architecture and EditMode tests: `59/59` passed.
- Vertical Slice PlayMode test: `1/1` passed.
- Final Unity Console check: `0 errors`, `0 warnings`.
- Architecture policy ledger: rebuilt from the current IL scanner inventory, `30` reviewed runtime entries.
- No project scene, prefab, ScriptableObject, or serialized asset was manually edited in this iteration.

## Repairs Completed

### Service and flow boundaries

- `GameManagerBootstrap` now resolves Persistent services once, caches the gateway interfaces, and exposes a narrow scene-flow gateway through `IGameManagerServices`.
- `LoadingRunController` and `SceneProgressionController` no longer resolve `GameFlow.Instance`; they consume the scene-flow gateway supplied by the GameManager composition root.
- `MainMenuController` consumes save-slot, save-data, flow, and localization gateways through `IGameManagerServices`.
- Main Menu startup now waits for `GameManagerBootstrap.Ready` when scene execution order delivers the menu before service configuration. Initialization remains one-shot and does not fall back to a singleton lookup.
- Tutorial completion is represented by `ITutorialCompletionGateway`. `TutorialObjectiveExit` depends on that contract, while the GameManager composition root owns the save mutation. This preserves the `Gameplay.Levels -> Flow` direction without adding a Persistence dependency to the levels assembly.

### Meta and player runtime context

- `MechanicUnlockSystem` no longer resolves Save or Story singletons from its overloads.
- `PlayerStatsInitializer` receives the Meta gateway through the player composition path.
- `MechanicUnlockedCondition` caches the player-owned provider once and evaluates against the injected Meta gateway.

### Scene discovery and camera boundaries

- `MainMenuView` and `DialogueManager` use the current UI EventSystem instead of scanning the scene for an EventSystem.
- `SceneStatsModifierController` uses its authored SceneSO catalog and no longer scans every SceneContext in the scene.
- `GridAligner` now requires its explicit serialized or parent-owned Grid reference. The MetaHub instance already carries the authored Grid override.
- `CameraController` and `CameraBounds` now use registration lists with enable/disable ownership. The three runtime camera `FindObjectsByType` scans were removed.
- Camera follow remains owned by `PlayerSpawner.SetFollow`; the camera no longer searches the scene for a Player tag.

### Architecture ledger

- Removed stale allowances for migrated Room, Enemy, Projectile, Buff, Story, Dialogue, Menu, Grid, SceneStats, and progression consumers.
- Rebuilt `Assets/Scripts/Tests/Architecture/ArchitecturePolicy.json` from the current scanner output rather than increasing budgets to hide regressions.
- Remaining reviewed calls are limited to startup composition, direct-open bootstrap, SceneLoader, Persistent service ownership, and the Localization base boundary.

## Verification

Commands and Unity MCP verification used:

- Unity project root confirmed as `D:/GitHub/Zephyr_v3/Zephyr(AI)/Zephyr`.
- Script refresh and compile completed without project compilation errors.
- `Zephyr.Architecture.Tests` + `Zephyr.Tests.EditMode`: `59/59` passed.
- `Zephyr.Tests.PlayMode`: `1/1` passed.
- Final `Unity_GetConsoleLogs`: `0` errors and `0` warnings.
- Static scan confirmed no runtime business callsites for `FindAnyObjectByType`, `FindObjectOfType`, `FindObjectsByType`, `GameObject.Find`, or the migrated service singletons outside tests/editor tooling and the reviewed bootstrap/SceneLoader boundaries.

## Remaining Reviewed Exceptions

These are intentionally retained and are not considered repair blockers:

1. `ColdStartupBootstrap` resolves startup singletons and performs the one direct-open additive load needed before Persistent services exist.
2. `InitializerBootstrap` performs the startup Persistent additive load; `SceneLoader` remains the normal runtime scene-load authority.
3. `GameFlow`, `PersistentBootstrap`, and `GameManagerBootstrap` resolve concrete service instances at composition boundaries and immediately expose gateway interfaces to consumers.
4. `LocalizedBehaviour` is the legacy localization adapter boundary. It remains the only runtime localization base class that resolves `LocalizationManager.Instance`; ordinary UI consumers do not do so directly.

These exceptions are ledgered and must not be copied into new gameplay, UI, interaction, enemy, pickup, or actor code.

## Acceptance Decision

Accepted. The architecture repair has high completion for the reviewed scope: business consumers now use explicit gateways or runtime contexts, scene discovery is removed from the repaired gameplay/camera paths, the assembly graph remains acyclic and policy-reviewed, and both structural and vertical-slice tests pass.

The next work should be ordinary feature development under `AGENTS.md` architecture gates. Further cleanup can be treated as optional debt reduction, not a prerequisite for continuing content work.

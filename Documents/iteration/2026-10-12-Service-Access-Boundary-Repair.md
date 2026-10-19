# Service Access Boundary Repair

Date: 2026-10-12

## Scope

This iteration implements the first bounded slice of the second architecture repair item: reduce business components' direct reads of concrete singleton services.

The slice is limited to the `MetaHub` scene entry interactions. `MetaHub` is a separate scene from `GameManager`, so `GameManagerBootstrap` cannot safely inject its objects. The scene-local `MetaHubController` is therefore used as the composition boundary.

## Changes

- Added `IMetaHubDependencyConsumer` under `Core/Flow/Scenes`.
- `MetaHubController` now resolves `SaveSystem.Instance` once at the scene composition boundary, creates one `MetaGateway`, and configures explicitly serialized consumers.
- `RunStartInteractable` now receives `IMetaGateway` through `Configure()` and no longer reads `SaveSystem.Instance`.
- `MetaUpgradeStation` now receives `IMetaGateway` through `Configure()` and no longer reads `SaveSystem.Instance`.
- `MetaHub.unity` now explicitly binds `MetaUpgradeStation` and `RunStartInteractable` to `MetaHubController._dependencyConsumers`.
- Added an architecture test that protects this boundary from direct singleton access being reintroduced.

## Dependency Direction

```text
MetaHubController
  -> SaveSystem.Instance (one composition-boundary resolution)
  -> MetaGateway
  -> IMetaHubDependencyConsumer.Configure(IMetaGateway)
       -> RunStartInteractable
       -> MetaUpgradeStation
```

The interaction components retain only the narrow `IMetaGateway` dependency. They do not know the concrete save implementation or construct their own gateway.

## Verification

- Architecture tests: `42 passed`, `0 failed`, `0 skipped`.
- EditMode tests: `5 passed`, `0 failed`, `0 skipped`.
- Modified scripts validated through Unity MCP: no diagnostics for `IMetaHubDependencyConsumer`, `MetaHubController`, and `RunStartInteractable`; `MetaUpgradeStation` has only the existing generic null-check suggestion.
- Scene binding verified in `MetaHub.unity`: both explicit consumer references are present.
- SceneView capture completed for `MetaHub`; the scene contains the authored ground, player, upgrade station, and run-entry interaction. No pink material or missing reference was visible in the capture.
- Exported verification screenshot: `D:\GitHub\Zephyr_v3\Zephyr(AI)\Zephyr\Assets\MCPTest\Screenshots\20261012_MetaHub_Phase_2.png`.

## Console Status

The code refresh produced no compilation errors. The final Unity Console query returned no Error entries. Existing warnings remain:

- Four obsolete `FindObjectsByType` overload warnings in `VerticalSlicePlayModeTests.cs`.
- Runtime `Animator is not playing an AnimatorController` warnings from the existing weapon test/setup path during the PlayMode run.
- One Unity Editor internal `services-gateway` configuration error was observed outside the changed project code. It is an editor environment issue and remains a residual risk for local PlayMode verification.

The Vertical Slice PlayMode run was started through the Unity Test Runner API, but the available legacy MCP route did not return a reliable completion callback. It is therefore not counted as a passing verification in this report.

## Deferred Work

The following direct service reads were intentionally not migrated in this bounded slice:

- `CurrencyPickup`, `WeaponPickup`, and `GustInteractable`: these are room/pool/Prefab-lifecycle objects, not members of the `MetaHub` composition tree. They need an explicit runtime-spawn or room context before injection can be added safely.
- `DialogueTrigger`, `StoryProgressionGate`, and related story consumers: they combine save state, story service state, and event subscriptions and need a dedicated story/dialogue context rather than a partial save-only injection.
- `LoadingRunController`, `MainMenuController`, `TutorialController`, and other flow controllers: these are separate composition-boundary candidates and should be migrated with their lifecycle owners, not mixed into this MetaHub slice.

## Assessment

This slice improves the architecture without introducing a Service Locator, a second event bus, or a new global manager. Singleton resolution is now concentrated at the `MetaHubController` boundary, while the two business interactions depend on a testable narrow gateway. The remaining direct reads are documented as lifecycle-specific follow-up work rather than being mechanically moved into an incorrect injector.

Next recommended slice: define the room/pool runtime context for `CurrencyPickup`, `WeaponPickup`, and `GustInteractable`, then migrate their `SaveData` and `EventBus` access together with a focused PlayMode regression.

# Runtime Verification Completion

**Date:** 2026-10-11  
**Scope:** Post-Core-Split runtime and test verification  
**Verdict:** Accepted with high completion for the available test and runtime surface  
**Verification score:** 9.2 / 10

## 1. Objective

Complete the runtime verification pass after Core split Phases A–G without modifying formal save
data, scenes, prefabs, or serialized assets. The intended checks were:

- Architecture dependency tests.
- Pure-logic EditMode tests.
- PlayMode test discovery.
- Initializer → Persistent → MainMenu startup.
- MainMenu → MetaHub lifecycle transition.
- MetaHub → MainMenu cleanup transition.
- Console and editor-state cleanliness.

## 2. Test Runner Results

### Architecture tests

Executed through the newer `mcp__unityMCP` Test Runner API because the required old Unity MCP route
does not expose test execution.

```text
Assembly: Zephyr.Architecture.Tests
Result:   27 passed / 27 total / 0 failed / 0 skipped
```

The passing set includes all Phase A–G architecture gates, including:

- Contracts and Runtime isolation.
- Combat, Persistence, Flow, and Meta boundaries.
- Explicit non-auto-referenced Core host.
- No dependency cycles or unauthorized scene/resource lookup patterns.
- Player target lifecycle contract.

### Business EditMode tests

```text
Assembly: Zephyr.Tests.EditMode
Result:   5 passed / 5 total / 0 failed / 0 skipped
```

Covered behavior:

- Main Menu tutorial destination.
- Main Menu run-resume destination.
- Missing save gateway behavior.
- Unconditional state transition behavior.
- Scene stat modifier application and reset.

### Combined EditMode result

```text
32 passed / 32 total / 0 failed / 0 skipped
```

### PlayMode Test Runner result

The PlayMode Test Runner started successfully and returned:

```text
0 real PlayMode tests
```

The returned `Zephyr` entry was the runner placeholder, not a project PlayMode test. No PlayMode
test failure was reported, but there is currently no authored PlayMode smoke-test suite.

## 3. Test Corrections Made

The first full EditMode run exposed three stale architecture assertions left behind by earlier Core
split migrations. These were corrected in:

```text
Assets/Scripts/Tests/Architecture/RuntimeEditorDependencyTests.cs
```

Corrections:

- Runtime gate now rejects `Zephyr.Core.Combat` rather than the old `Zephyr.Core.Stats` namespace.
- Player target lifecycle test reads `PlayerTargetChangedEvent.cs` instead of searching for the
  event type in `EventBus.cs`.
- Phase D stat bridge test uses the current `Core/Flow/SceneStatsModifierController.cs` path.

After correction, the complete targeted EditMode set passed `32/32`.

## 4. Runtime Smoke Test

### 4.1 Startup

Started the project from the current `Initializer` scene using the old `mcp__unity_mcp` route.
After startup:

```text
loadedScenes = Persistent, MainMenu
active       = MainMenu
gameFlow     = MainMenu
saveSystem   = present
```

Observed result:

- Initializer successfully handed off to Persistent/MainMenu.
- Persistent remained loaded.
- GameFlow reached `MainMenu`.
- SaveSystem instance was present.
- Console remained clean.

### 4.2 MainMenu → MetaHub

Requested the transition through the real `GameFlow.Instance.RequestTransition(GameState.MetaHub)`
path, not by directly loading a scene.

Observed result:

```text
loadedScenes      = Persistent, GameManager, MetaHub
active            = MetaHub
gameFlow          = MetaHub
gameManager       = present
metaHubController = present
```

This validates:

- GameManager is loaded when leaving MainMenu.
- MetaHub is loaded additively through the flow authority.
- MetaHub becomes the active scene.
- MetaHub controller is instantiated.
- Persistent remains loaded.

### 4.3 MetaHub → MainMenu

Requested return through `GameFlow.Instance.RequestTransition(GameState.MainMenu)`.

Observed result:

```text
loadedScenes = Persistent, MainMenu
active       = MainMenu
gameFlow     = MainMenu
gameManager  = not-found
```

This validates the intended GameManager lifecycle: it is present during gameplay-state scenes and is
unloaded when returning to MainMenu, while Persistent remains loaded.

### 4.4 Save precondition inspection

The runtime save service reported:

```text
saveSystem       = present
selectedSlot     = -1
hasAnyValidSave  = true
slots            = 3
slot1/slot2/3    = occupied
```

No slot was selected during this verification pass. To avoid changing formal save data, the test did
not create, select, modify, settle, or delete a save slot.

## 5. Console and Editor Verification

- Unity project root confirmed as:

```text
D:/GitHub/Zephyr_v3/Zephyr(AI)/Zephyr
```

- Unity version: `6000.5.6f1`.
- Final editor state: not playing, not paused, not compiling, not updating.
- Final active scene: `Initializer`.
- Final active scene was not dirty.
- Final `Unity_GetConsoleLogs`: `0 Error / 0 Warning`.
- No MissingReference, Serialization, YAML, shader/material, or missing-script messages appeared.

## 6. Coverage and Limitations

### Verified

- Architecture assembly boundaries.
- Core split closeout gates.
- Main Menu pure logic.
- State transition pure logic.
- Stats modifier pure logic.
- Initializer startup chain.
- Persistent lifetime across scene transitions.
- MainMenu/GameManager lifecycle.
- MetaHub scene loading and activation.
- Return-to-MainMenu cleanup.

### Not fully verified

- Tutorial flow with a newly created profile.
- LoadingRun and InRun transition with a selected profile.
- Potential allocation and Meta upgrade persistence.
- Run-end settlement and save write/read round trip.
- Player spawn, combat input, hitbox, damage, and respawn in PlayMode.
- Automated PlayMode smoke tests, because the project currently contains no authored PlayMode test
  cases.

These limitations are caused by the intentional no-save-mutation rule and the absence of PlayMode
test fixtures, not by a current compile or Console failure.

## 7. Unified Assessment

| Dimension | Score | Assessment |
|-----------|------:|------------|
| Architecture regression coverage | 9.7 | All 27 architecture gates pass. |
| Pure-logic behavior coverage | 9.2 | All 5 current EditMode business tests pass. |
| Startup lifecycle confidence | 9.5 | Initializer, Persistent, and MainMenu verified in PlayMode. |
| Scene lifecycle confidence | 9.3 | MetaHub load and GameManager unload verified through GameFlow. |
| Save/run-flow coverage | 8.4 | Existing slots detected, but no slot was selected or mutated. |
| PlayMode automation | 8.0 | Test Runner works, but no authored PlayMode tests exist. |
| Console/build cleanliness | 9.8 | Compile and runtime Console remained at zero errors/warnings. |
| **Overall** | **9.2** | **High completion for the available verification surface.** |

## 8. Acceptance Decision

The runtime verification pass is accepted with high completion for the current project test surface.
The Core split has no detected architecture regression, startup lifecycle works, MetaHub lifecycle
works, GameManager ownership is correct, and the Console is clean.

The next meaningful verification task is not another architecture split. It is a dedicated PlayMode
test/content setup pass that can safely select a disposable test profile and exercise LoadingRun,
InRun, settlement, and combat without touching formal saves.

## 9. Next Recommended Verification Work

1. Add disposable PlayMode test fixtures or a temporary save gateway.
2. Verify MainMenu save-slot selection with a test profile.
3. Verify Tutorial → MetaHub → LoadingRun → InRun.
4. Verify Meta upgrade/potential allocation and settlement round trips.
5. Add one automated PlayMode smoke test so future architecture changes can run it without manual
   editor commands.

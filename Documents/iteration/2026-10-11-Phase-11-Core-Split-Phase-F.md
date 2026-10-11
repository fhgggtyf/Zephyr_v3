# Phase F — Meta Extraction

**Date:** 2026-10-11  
**Scope:** Core split Phase F — Meta progression compile-time boundary  
**Verdict:** Accepted with high completion  
**Phase score:** 9.4 / 10

## 1. Objective

Extract the meta-progression framework from the legacy `Zephyr.Core` host while preserving
serialized namespaces, ScriptableObject identities, scene references, and existing runtime behavior.
The intended boundary was:

```text
Zephyr.Core.Meta
    → Zephyr.Core.Contracts
    → Zephyr.Core.Runtime
    → Zephyr.Core.Persistence
    → Unity.Localization
```

Meta must not depend on Flow, Combat, UI, Player, Enemies, Weapons, or the legacy `Zephyr.Core`
implementation assembly.

## 2. Completed Changes

### 2.1 New Meta assembly

Created:

```text
Assets/Scripts/Core/Meta/Zephyr.Core.Meta.asmdef
```

Properties:

- Explicit, non-auto-referenced assembly.
- References only Contracts, Runtime, Persistence, and `Unity.Localization`.
- Unity generated and retained the matching `.meta` file with a unique GUID.

The following existing Meta implementation files are now compiled by `Zephyr.Core.Meta`:

- `MechanicUnlockSO.cs`
- `MechanicUnlockSystem.cs`
- `MetaUpgradeSO.cs`
- `MetaUpgradeSystem.cs`
- `MilestoneConditionSO.cs`
- `MilestoneEffectSO.cs`
- `MilestoneSO.cs`
- `RunEndSettlement.cs`
- `RunEntryPotentialService.cs`
- `StoryFactSO.cs`
- `StoryProgressionDatabaseSO.cs`
- `StoryProgressionGate.cs`
- `StoryProgressionService.cs`

No serialized namespace or public type name was changed.

### 2.2 Dependency cleanup

`RunEntryPotentialService` already consumed `StatType` from the Contracts source tree. The new
assembly therefore does not need a compatibility reference to the old Core implementation.
This is the key dependency cleanup that makes the extraction a real compiler boundary rather than
just a second assembly name.

### 2.3 Direct consumer references

Explicit `Zephyr.Core.Meta` references were added to the assemblies that actually consume Meta
types:

- `Zephyr.Core`
- `Zephyr.Core.Flow`
- `Zephyr.Gameplay.Interaction`
- `Zephyr.Player`
- `Zephyr.Testing.Editor`

No unrelated assembly was given a transitive or speculative Meta reference.

### 2.4 Architecture gates

Added Phase F checks to:

```text
Assets/Scripts/Tests/Architecture/RuntimeEditorDependencyTests.cs
```

The checks assert that:

- Meta references only the approved stable layers and Localization.
- Meta does not reference legacy Core, Flow, Combat, UI, Player, Enemies, or Weapons.
- Meta source does not use direct scene-loading or GameFlow singleton APIs.
- All current Meta consumers declare the Meta dependency directly.

## 3. Dependency Assessment

### Before

```text
Zephyr.Core
 ├─ Meta upgrades and unlocks
 ├─ Run settlement and run-entry handoff
 ├─ Story progression and milestone evaluation
 ├─ UI/dialogue consumers
 └─ Remaining Core services
```

Meta behavior was visible to consumers only through the broad legacy Core compilation surface.
That made accidental coupling easy and prevented the compiler from enforcing the intended Meta
boundary.

### After

```text
Zephyr.Core.Contracts    ← stable types and interfaces
Zephyr.Core.Runtime      ← shared runtime services and EventBus
Zephyr.Core.Persistence  ← save models and SaveSystem
Zephyr.Core.Meta         ← progression rules and Meta ScriptableObjects
Zephyr.Core              ← remaining legacy Core services → Meta where needed
Zephyr.Core.Flow         → Meta for settlement and run-entry orchestration
Gameplay / Player        → Meta only where Meta types are consumed directly
```

Static dependency analysis after the change found:

- `25` asmdefs.
- `86` project-assembly edges.
- `0` dependency cycles.
- Meta dependencies exactly limited to Contracts, Runtime, Persistence, and Localization.
- Five direct Meta consumers.
- No Meta source dependency on the legacy Core implementation.

## 4. Architecture Quality Review

### Improvements

- Meta progression is now a compiler-enforced module instead of an implicit part of Core.
- The Meta assembly is non-auto-referenced, so new code cannot receive it accidentally through
  Unity's default assembly visibility.
- Save persistence remains below Meta, preserving the intended data-flow direction.
- Flow can use Meta explicitly without making Persistence depend on Meta.
- Contracts-owned `StatType` remains reusable without pulling Combat or the legacy Core host into
  Meta.
- Existing serialized namespaces, ScriptableObject types, asset paths, scene files, and prefab
  identities remain unchanged.
- The extraction is limited to code and assembly definitions; no scene or prefab YAML was edited.

### Remaining deductions

- `Zephyr.Core.Flow` still depends on legacy Core for non-Meta services such as UI/stat/interface
  integration. Meta extraction reduces this bridge but does not remove all Flow-to-Core coupling.
- `SaveSystem.Instance` and `StoryProgressionService.Instance` remain direct singleton access
  points inside Meta. A future Save/Meta gateway pass should narrow those accesses at composition
  boundaries rather than mixing lifecycle changes into this extraction.
- `Zephyr.Core` still contains UI, Localization, and other legacy services. A future UI or
  Localization extraction requires a separate dependency plan.
- Existing domain migration bridges such as Enemies → Items/Weapons and Items → Weapons remain
  because serialized reward/drop assets still need a dedicated migration gate.
- Unity Test Runner execution and Play Mode regression were not run: the fixed old Unity MCP route
  available in this environment exposes compilation, console, and script validation but no direct
  test-runner API. This is a verification limitation, not a known Phase F failure.

## 5. Validation Evidence

### Unity MCP route

All Unity operations used the required old `mcp__unity_mcp` route. The confirmed project root was:

```text
D:/GitHub/Zephyr_v3/Zephyr(AI)/Zephyr
```

### Compilation and Console

- `AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate)` executed successfully.
- Unity command compilation succeeded.
- Final `Unity_GetConsoleLogs` result: `0 Error / 0 Warning`.
- No MissingReference, Serialization, YAML, shader/material, or missing-script messages appeared.

### Script validation

Standard validation returned no diagnostics for all 13 Meta scripts and for:

```text
Assets/Scripts/Tests/Architecture/RuntimeEditorDependencyTests.cs
```

### Static architecture gates

- All touched `.asmdef` files parse as valid JSON.
- Meta dependency list matches the approved boundary exactly.
- Meta consumers declare direct references.
- Dependency graph contains no cycles.
- Meta source contains no `SceneManager.` or `GameFlow.Instance` usage.
- No unrelated scene, prefab, ScriptableObject, or serialized asset contents were modified by Phase F.

### Scene and prefab impact

No scene, prefab, or serialized asset was created or modified in Phase F. Consequently, no SceneView
capture was required. The serialized type namespaces and asset-facing class names were preserved.

## 6. Unified Self-Assessment

| Dimension | Score | Assessment |
|-----------|------:|------------|
| Boundary ownership | 9.6 | Meta has a clear independent assembly and source tree. |
| Dependency direction | 9.5 | Meta points downward to stable layers and has no legacy Core dependency. |
| Consumer explicitness | 9.4 | Actual consumers declare Meta directly; no broad speculative references were added. |
| Migration safety | 9.6 | Namespaces, GUIDs, scenes, prefabs, and SO identities were preserved. |
| Verification confidence | 9.0 | Compile, Console, script validation, JSON checks, and graph checks passed; Test Runner/PlayMode remain unexecuted. |
| Residual architecture debt | 9.1 | Singleton access and remaining Core/Flow bridges are explicit and isolated, but not fully removed. |
| **Overall** | **9.4** | **High completion; safe to accept Phase F.** |

The overall result is high enough for acceptance because the requested boundary is implemented and
compiler-enforced, the dependency graph is acyclic, Unity compilation is clean, and the remaining
deductions are known follow-up architecture work rather than hidden Phase F defects.

## 7. Acceptance Decision

Phase F is accepted with high completion. Meta progression is now an explicit, non-auto-referenced
assembly with a downward dependency direction, direct consumer declarations, clean Unity compilation,
and no Console errors or warnings. The change is safe to use as the baseline for the next architecture
phase or for ordinary feature work that stays within the documented boundaries.

## 8. Rollback

Rollback must be limited to the Phase F file set. Remove `Zephyr.Core.Meta.asmdef` and its generated
`.meta`, remove the five direct Meta references, revert the Phase F architecture tests, and restore
the Phase E architecture-document snapshot. Do not use a broad working-tree reset because earlier
phases and user changes coexist in the same checkout.

## 9. Next Safe Phase

The next architecture-only candidates are:

1. A Save/Meta gateway pass that narrows `SaveSystem.Instance` and Meta service singleton access at
   composition boundaries.
2. A focused UI/Localization extraction from legacy Core.
3. A dedicated migration plan for the remaining serialized domain bridges.

Ordinary content work can proceed now if it consumes Meta through the explicit assembly and existing
systems. It must not add new singleton access, direct scene loading, or new dependencies on the
legacy Core host without a separate architecture proposal.

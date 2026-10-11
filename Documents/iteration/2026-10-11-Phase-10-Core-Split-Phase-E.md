# Phase E — Flow and Persistence Extraction

**Date:** 2026-10-11  
**Scope:** Core split Phase E — Persistence and Flow compile-time boundaries  
**Verdict:** Accepted with high completion  
**Phase score:** 9.2 / 10

## 1. Objective

Continue the Core split after Combat by extracting two remaining high-value boundaries without
changing serialized namespaces or scene-facing component identities:

```text
Zephyr.Core.Persistence  → no Flow / Meta / Combat implementation
Zephyr.Core.Flow         → Contracts + Runtime + Persistence + legacy Core APIs
Zephyr.Core              → Persistence, but not Flow
```

The migration intentionally keeps the remaining Meta, UI, Localization, and other legacy Core
services in `Zephyr.Core` until their own boundaries are ready. This avoids a large multi-domain
rewrite while still making scene loading and save code visible to the compiler.

## 2. Completed Changes

### 2.1 Persistence assembly

Created:

```text
Assets/Scripts/Core/Persistence/Zephyr.Core.Persistence.asmdef
```

Properties:

- Explicit, non-auto-referenced assembly.
- No project assembly references.
- Contains only save models, save slot contract, and JSON/file persistence implementation.

Moved from `Core/Save` to `Core/Persistence` while preserving namespaces and `.meta` GUIDs:

- `SaveSystem.cs`
- `MetaData.cs`
- `RunData.cs`
- `ISaveSlotGateway.cs`

The runtime namespaces remain `Zephyr.Core.Save` to avoid serialized or source-level namespace
migrations during this architecture pass.

### 2.2 Flow assembly

Created:

```text
Assets/Scripts/Core/Flow/Zephyr.Core.Flow.asmdef
```

References:

- `Zephyr.Core.Contracts`
- `Zephyr.Core.Runtime`
- `Zephyr.Core.Persistence`
- `Zephyr.Core`

The Flow assembly now owns the scene lifecycle boundary, including:

- `GameFlow`
- `SceneLoader`
- `SceneSO`
- `SceneContext`
- `GameFlowTransitionPolicy`
- Flow bootstrap scripts
- Flow scene controllers and Main Menu composition scripts
- Flow-specific ScriptableObject event channels
- `SceneStatsModifierController`
- `RoomController` and its room-entered event payload

Flow event channel files moved from `Core/Events/Flow` to `Core/Flow/Events`. The scene statistics
adapter moved from `Core/Stats` to `Core/Flow`, and the room lifecycle bridge moved from
`Core/Levels` to `Core/Flow`. Their namespaces and `.meta` GUIDs were preserved.

### 2.3 Legacy Core adjustment

`Zephyr.Core.asmdef` now directly references `Zephyr.Core.Persistence` because the remaining Core
Meta, UI, and item-adjacent services still consume save models or `SaveSystem`. It does **not**
reference `Zephyr.Core.Flow`, preventing a reverse dependency from the legacy host into Flow.

## 3. Consumer Reference Updates

Direct assembly references were added where source code uses the extracted contracts or types:

- `Zephyr.Player` → Flow and Persistence
- `Zephyr.Rooms` → Flow
- `Zephyr.Gameplay.Interaction` → Flow and Persistence
- `Zephyr.Items` → Persistence
- `Zephyr.Tests.EditMode` → Flow and Persistence

Existing Combat, Runtime, and Contracts references remain unchanged.

## 4. Dependency Assessment

### Before

```text
Zephyr.Core
 ├─ SaveSystem / MetaData / RunData
 ├─ GameFlow / SceneLoader / SceneSO
 ├─ Flow event channels
 ├─ SceneStatsModifierController
 └─ RoomController
```

The compiler could not prevent new code from treating save, scene loading, and runtime services as a
single Core implementation surface.

### After

```text
Zephyr.Core.Contracts  ← stable contracts
Zephyr.Core.Runtime    ← event bus / timer / pool / logging
Zephyr.Core.Persistence← save models + SaveSystem; no project assembly dependencies
Zephyr.Core.Combat     ← Contracts + Runtime
Zephyr.Core            ← Contracts + Runtime + Persistence + remaining legacy services
Zephyr.Core.Flow       ← Contracts + Runtime + Persistence + Core
```

The legacy Core host does not reference Flow. Flow depends on the legacy Core only for the still
unextracted Meta, UI, Localization, and related services. This is a deliberate one-way migration
bridge, not a hidden cycle.

Static dependency analysis found:

- `24` asmdefs discovered.
- `0` dependency cycles.
- Persistence references: none.
- Flow references: Contracts, Runtime, Persistence, and Core only.
- Core references Persistence but not Flow.
- No old Save, Flow-event, scene-stat adapter, or RoomController source remained in the old paths.

## 5. Architecture Quality Review

### Improvements

- Save persistence is now independently visible and testable as a boundary.
- Scene lifecycle code has an explicit assembly instead of being transitively hidden in Core.
- Flow-specific event channels no longer contaminate the legacy Core host.
- `SceneStatsModifierController` now belongs to scene integration rather than the Combat/stat
  implementation boundary.
- `RoomController` is no longer compiled as an unrelated Core implementation while being consumed
  by Flow.
- Existing namespaces and `.meta` GUIDs were preserved. Verified examples include:
  - `SaveSystem`: `6fa6d5b5b02743d4681d0e3bcb03f50e`
  - `SceneStatsModifierController`: `6cfdb6717ec08004187f21b9b9fa76fc`
  - `RoomController`: `50715f9ea0824ebbb3d4e5f60718293a`

### Remaining deductions

- Flow still depends on the legacy Core host for Meta, UI, Localization, and related services. A
  future Meta/UI extraction is required before Flow becomes independently reusable.
- `SaveSystem` still exposes a static `Instance`; the assembly boundary is improved, but lifecycle
  access is not fully gateway-only yet.
- `GameFlow` still coordinates settlement and time-pause integration. Those are known orchestration
  responsibilities and remain separate from this file-boundary extraction.
- Existing domain migration bridges (`Enemies → Items/Weapons`, `Items → Weapons`) remain because
  serialized reward/drop assets still need a dedicated migration gate.
- Unity Test Runner execution and Play Mode regression were not run because the fixed old Unity MCP
  route exposes no direct test execution API in this environment.

## 6. Validation Evidence

### Unity MCP route

All Unity operations used the required old `mcp__unity_mcp` route.

### Compilation and Console

- AssetDatabase refresh and compilation succeeded.
- Final Console check: `0 Error / 0 Warning`.
- No MissingReference, Serialization, YAML, shader/material, or missing-script messages were
  reported.

### Script validation

Standard validation returned no diagnostics for:

- `Core/Persistence/SaveSystem.cs`
- `Core/Flow/GameFlow.cs`
- `Core/Flow/SceneLoader.cs`
- `Core/Flow/SceneStatsModifierController.cs`
- `Tests/Architecture/RuntimeEditorDependencyTests.cs`

### Static architecture gates

- Persistence source tree contains no Flow, Meta, Combat, or scene-loading dependency.
- Flow asmdef has the expected downward references and no Combat dependency.
- Legacy Core asmdef references Persistence and does not reference Flow.
- Flow/Persistence source files are in their new paths.
- `git diff --check` passed for the Phase E file set; unrelated earlier working-tree whitespace
  warnings remain outside this phase.

### Scene and prefab impact

No scenes, prefabs, or serialized asset contents were edited in Phase E. The work moved C# scripts,
asmdefs, and matching `.meta` files only, so no SceneView capture was required for this phase.

## 7. Acceptance Decision

Phase E is accepted with high completion. The intended Persistence and Flow compile-time boundaries
are present, the dependency graph is acyclic, the old Core host no longer owns Flow implementation,
Unity compilation succeeds, and the Console is clean. The remaining Core dependency of Flow is
explicit, localized, and documented as a migration bridge rather than an accidental cycle.

## 8. Rollback

Rollback must be limited to the Phase E file set. Move the Persistence files and matching `.meta`
files back to `Core/Save`, restore Flow event files to `Core/Events/Flow`, restore
`SceneStatsModifierController` to `Core/Stats`, restore `RoomController` to `Core/Levels`, remove
the two new asmdefs, revert the Phase E consumer references, architecture gates, and design-doc
snapshot. Do not use a broad working-tree reset because Phases A–D and earlier user work remain in
the same checkout.

## 9. Next Safe Phase

The next architecture-only candidate is Meta extraction or a focused Save gateway/lifecycle pass.
Ordinary feature work may use Flow and Persistence through their explicit assembly references, but
should not remove the legacy Core bridge or replace `SaveSystem.Instance` globally without a separate
architecture proposal and migration gate.

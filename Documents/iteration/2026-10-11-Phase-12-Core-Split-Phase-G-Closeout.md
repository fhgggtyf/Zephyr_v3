# Phase G — Core Split Closeout

**Date:** 2026-10-11  
**Scope:** Core split Phase G — explicit legacy host and architecture closeout  
**Verdict:** Accepted with high completion; Core split closed for this architecture pass  
**Phase score:** 9.5 / 10

## 1. Objective

Finish the Core split without creating unnecessary micro-assemblies. Phase F extracted Meta. The
remaining `Zephyr.Core` code was reviewed to determine whether another extraction would improve
architecture or only fragment composition-facing services.

The remaining host contains:

- Audio service and audio event asset types.
- Camera bounds, zones, and gameplay camera controller.
- Localization gateway and Unity Localization adapter.
- Runtime UI, dialogue, popup, and gameplay time-pause infrastructure.
- Shared constants and the grid alignment utility.

These systems are cross-system composition infrastructure rather than a single isolated domain.
Phase G therefore closes the split by hardening the host boundary instead of introducing parallel
Audio, Camera, UI, and Localization assemblies without a concrete reuse requirement.

## 2. Completed Changes

### 2.1 Legacy Core host made explicit

Updated:

```text
Assets/Scripts/Core/Zephyr.Core.asmdef
```

Change:

- `autoReferenced` changed from `true` to `false`.
- Existing dependency direction remains downward: Contracts, Runtime, Persistence, Meta,
  StateMachine, Inputs, and required Unity packages.
- Core still does not reference Flow, Combat, Player, Enemies, Weapons, Items, or Projectiles.

This is the final safety boundary for the remaining host. New code can no longer receive the broad
Core surface transitively through Unity's default assembly behavior.

### 2.2 Explicit consumer audit

The following assemblies explicitly reference `Zephyr.Core` and were verified after disabling
auto-referencing:

- `Zephyr.Core.Flow`
- `Zephyr.Items`
- `Zephyr.Projectiles`
- `Zephyr.Weapons`
- `Zephyr.Enemies`
- `Zephyr.Gameplay.Interaction`
- `Zephyr.Rooms`
- `Zephyr.Player`
- `Zephyr.Testing`
- `Zephyr.Testing.Editor`
- `Zephyr.Tests.EditMode`
- `Zephyr.UI`

No runtime assembly that imports the remaining host namespaces (`Audio`, `Camera`, `Localization`,
or `UI`) was found without a direct Core reference.

### 2.3 Final architecture gates

Added Phase G checks to:

```text
Assets/Scripts/Tests/Architecture/RuntimeEditorDependencyTests.cs
```

The checks assert that:

- Core is explicitly non-auto-referenced.
- Core does not depend upward on Flow, Combat, Player, Enemies, Weapons, Items, or Projectiles.
- Known Core consumers declare direct Core references.
- Runtime code using the remaining Core host namespaces cannot compile without a direct Core
  reference.

The gate intentionally distinguishes `using Zephyr.Core;` used by Contracts-owned root types from
actual legacy host namespaces. This prevents Combat/Contracts from being falsely classified as
legacy Core consumers.

## 3. Final Dependency Assessment

### Core split result

```text
Zephyr.Core.Contracts  ← stable contracts and value types
Zephyr.Core.Runtime    ← shared runtime services
Zephyr.Core.Combat     ← combat implementation
Zephyr.Core.Persistence← save models and persistence
Zephyr.Core.Meta       ← progression and settlement
Zephyr.Core.Flow       ← scene lifecycle and flow orchestration
Zephyr.Core            ← explicit composition-facing host
```

The legacy host is now an explicit compatibility boundary rather than an invisible global import.
The split is complete enough to support ordinary feature work without allowing new code to silently
depend on the old monolith.

### Why UI/Camera/Audio were not split further

The remaining 17 host scripts are tightly connected to scene composition, presentation, localization,
input/UI timing, and camera lifecycle. Splitting them now would create several small assemblies with
little independent value and would increase serialized/prefab and lifecycle migration risk. There is
no current dependency-cycle or compile-time isolation problem that requires those extra boundaries.

Future extraction remains possible, but only when a concrete use case establishes a stable boundary:

1. Save gateway/lifecycle work may isolate profile access from Meta and UI.
2. UI/Localization may be separated if a dedicated presentation module or alternate front end is
   introduced.
3. Audio/Camera may be separated if another gameplay host must reuse them independently.
4. Serialized domain bridges may be removed only after asset migration is designed and verified.

These are separate architecture proposals, not unfinished work hidden inside Phase G.

## 4. Architecture Quality Review

### Improvements

- The Core host is now compiler-visible and explicitly referenced.
- New code cannot accidentally inherit legacy Core through auto-referencing.
- The host has no upward dependency on domain implementations.
- Existing extracted boundaries remain intact: Contracts, Runtime, Combat, Persistence, Meta, and
  Flow continue to compile independently according to their declared references.
- The decision not to split UI/Camera/Audio further is documented and justified by cohesion and
  migration risk rather than by leaving an unreviewed monolith.
- No scene, prefab, ScriptableObject, or serialized asset content was modified.

### Remaining deductions

- `SaveSystem.Instance`, `LocalizationManager.Instance`, `AudioManager.Instance`, and other
  existing singleton surfaces remain architecture debt. They are not expanded by this phase, but
  they still require a future gateway/lifecycle pass if the project needs stricter dependency
  inversion.
- Flow still references the explicit Core host for remaining orchestration-facing services.
- Enemies → Items/Weapons and Items → Weapons migration bridges remain because serialized assets need
  a dedicated migration plan.
- Unity Test Runner and Play Mode regression were not run. The connected old Unity MCP route provided
  compilation, Console, and script validation, but no direct test-runner API in this environment.

## 5. Validation Evidence

### Unity MCP route

All Unity operations used the required old `mcp__unity_mcp` route. Confirmed project root:

```text
D:/GitHub/Zephyr_v3/Zephyr(AI)/Zephyr
```

### Compilation and Console

- `AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate)` completed successfully.
- Unity compilation succeeded after disabling Core auto-referencing.
- Final `Unity_GetConsoleLogs`: `0 Error / 0 Warning`.
- No MissingReference, Serialization, YAML, shader/material, or missing-script messages appeared.

### Script and static validation

- Standard validation of `RuntimeEditorDependencyTests.cs` returned no diagnostics.
- Static assembly analysis: `25` asmdefs, `86` project-assembly edges, `0` dependency cycles.
- `Zephyr.Core.autoReferenced = false`.
- Core consumers: `12` explicit assembly references.
- Implicit legacy-host consumer violations: `0`.
- `git diff --check` passed for the Phase G file set.

### Scene and prefab impact

No scene, prefab, or serialized asset was created or modified in Phase G. No SceneView capture was
required. Unity compilation confirms the changed assembly boundary remains compatible with current
serialized content.

## 6. Unified Self-Assessment

| Dimension | Score | Assessment |
|-----------|------:|------------|
| Boundary ownership | 9.7 | Extracted modules have clear ownership; remaining Core is explicitly defined as a host. |
| Dependency direction | 9.6 | No dependency cycles and no upward Core dependency. |
| Compile-time enforcement | 9.7 | Core is non-auto-referenced and consumer declarations are audited. |
| Migration safety | 9.6 | No serialized assets, scenes, prefabs, namespaces, or GUIDs were changed. |
| Verification confidence | 9.1 | Compile, Console, static graph, gate validation, and diff checks passed; Test Runner/PlayMode remain unexecuted. |
| Future-work clarity | 9.7 | Deferred UI/Localization/gateway work is explicitly separated from Core split completion. |
| **Overall** | **9.5** | **High completion; Phase G accepted and Core split closed.** |

The result is high enough for acceptance because the remaining Core surface has been assessed,
bounded, and made explicit. Further splitting would currently trade cohesion for fragmentation and
would not materially improve dependency safety.

## 7. Acceptance Decision

Phase G is accepted with high completion. The Core split is closed for this architecture pass:

- Contracts, Runtime, Combat, Persistence, Meta, and Flow are explicit assemblies.
- The remaining Core host is explicit and non-auto-referenced.
- Direct consumers are verified.
- Unity compilation and Console are clean.
- Remaining singleton and serialized migration debt is documented as separate future work.

Ordinary feature development may proceed within the documented module boundaries. New code must not
add singleton access, direct scene loading, or implicit Core dependencies. A future architecture task
should be opened only when a concrete gateway, UI/Localization, or serialized migration objective is
ready to justify further change.

## 8. Rollback

Rollback is limited to the Phase G file set: restore `autoReferenced: true` in
`Zephyr.Core.asmdef`, revert the Phase G architecture gates, and restore the Phase G architecture
document/report changes. Do not reset the broader working tree because Phases A–F and user changes
coexist in the same checkout.

## 9. Core Split Status

```text
Phase A/B  Contracts          accepted
Phase C    Runtime            accepted
Phase D    Combat             accepted
Phase E    Flow/Persistence   accepted
Phase F    Meta               accepted
Phase G    Explicit host      accepted — closeout
```

**Core split status: complete for the current architecture scope.**

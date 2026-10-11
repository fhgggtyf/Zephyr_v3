# Phase D — Combat Extraction and Architecture Acceptance

**Date:** 2026-10-11  
**Scope:** Core split Phase D — Combat, Health, Damage, Stats, Buff  
**Verdict:** Accepted with high completion  
**Architecture score:** 9.2 / 10 for the Phase D target boundary

## 1. Objective

Extract the combat implementation from the legacy `Zephyr.Core` assembly without changing
serialized namespaces or prefab-facing component types. The target was a one-way dependency:

```text
Zephyr.Core.Combat → Zephyr.Core.Contracts
                     Zephyr.Core.Runtime
```

Combat was not allowed to depend on Flow, Persistence, UI, Player, Enemies, Weapons, Items, or
scene loading.

## 2. Completed Changes

### 2.1 New assembly

Created:

```text
Assets/Scripts/Core/Combat/Zephyr.Core.Combat.asmdef
```

The assembly is explicit and non-auto-referenced. Its only references are:

- `Zephyr.Core.Contracts`
- `Zephyr.Core.Runtime`

### 2.2 Moved implementation files

The following implementation files moved into `Core/Combat` while preserving their original
namespaces and `.meta` GUIDs:

- `CombatSystem.cs`
- `CombatTargetAvailability.cs`
- `Stats/StatsCore.cs`
- `Stats/StatRecord.cs`
- `Stats/IMetaStatSource.cs`
- `Health/HealthComponent.cs`
- `DamageSystem/DamageCalculator.cs`
- `Buff/BuffSystem.cs`
- `Interfaces/IBuffable.cs`

The former `Stats/StatModifierDefinition.cs` moved into:

```text
Assets/Scripts/Core/Contracts/Stats/StatModifierDefinition.cs
```

Its namespace remains `Zephyr.Core.Stats` so existing serialized and source-level contracts do not
need a namespace migration.

### 2.3 Dependency bridge cleanup

Added:

- `Core/Contracts/Constants/StatConstants.cs`
- `Core/Runtime/Stats/StatModifierRegistry.cs`
- `Core/Combat/CombatConstants.cs`

`StatsCore` no longer calls `SceneStatsModifierController` directly. Instead:

```text
SceneStatsModifierController → StatModifierRegistry ← StatsCore
```

The scene controller remains in the legacy Core host because it owns scene lifecycle integration.
The registry lives in Runtime and only consumes stable stat-modifier contracts. This removes the
former direct Combat → Flow coupling and avoids a circular Core/Combat dependency.

`StatsCore` now implements `IStatModifierTarget`. `StatModifierDefinition` and
`IStatModifierSource` no longer require the concrete `StatsCore` type, which keeps the contract
assembly independent of Combat implementation classes.

### 2.4 Consumer assembly updates

Direct Combat consumers now reference `Zephyr.Core.Combat`, including:

- `Zephyr.Player`
- `Zephyr.Enemies`
- `Zephyr.Weapons`
- `Zephyr.Projectiles`
- `Zephyr.Items`
- `Zephyr.UI`
- `Zephyr.Testing`
- `Zephyr.Tests.EditMode`

The legacy `Zephyr.Core` assembly does not reference `Zephyr.Core.Combat`.

## 3. Dependency Assessment

### Before

```text
Domain assemblies → Zephyr.Core
Zephyr.Core      → Flow + Combat + Stats + Health + Buff + Runtime
StatsCore        → SceneStatsModifierController
SceneStatsModifierController → Flow / SceneSO
```

The old path made combat implementation transitively depend on the Core monolith and coupled stat
calculation to a concrete scene singleton.

### After

```text
Zephyr.Core.Contracts  ← no implementation dependencies
          ↑
Zephyr.Core.Runtime    ← Contracts
          ↑
Zephyr.Core.Combat     ← Contracts + Runtime

Zephyr.Core             ← Contracts + Runtime + Flow infrastructure
  └─ SceneStatsModifierController → Runtime.StatModifierRegistry

Player / Enemies / Weapons / Items / Projectiles / UI
  └─→ Combat + Core compatibility host
```

Static dependency analysis found:

- `22` asmdefs discovered.
- `0` dependency cycles.
- `Zephyr.Core.Combat` references exactly Contracts and Runtime.
- No Combat source imports Flow, Save, UI, Player, Enemies, Weapons, or Items.
- No Combat source performs scene loading or `Resources.Load`.

## 4. Architecture Quality Review

### Improvements

- Combat is now a real compile-time boundary instead of a namespace-only folder.
- Health, damage, stats, and Buff implementation have a coherent ownership boundary.
- Stable stat-modifier data moved downward into Contracts.
- Scene integration no longer leaks a concrete scene controller into `StatsCore`.
- Existing serialized component identities remain stable because `.meta` files were moved with the
  source files.
- Domain consumers explicitly declare the new dependency instead of receiving Combat transitively
  through the legacy Core assembly.

### Remaining deductions

- `Zephyr.Core` still contains Flow, Persistence, UI, and other migration-host responsibilities.
- `SceneStatsModifierController` still exposes a static `Instance`; it is now isolated as a scene
  integration adapter, but its lifecycle governance is not fully removed.
- `GameConstants.cs` remains a mixed legacy constants host. Stat and combat constants used by the
  extracted boundary were separated, but unrelated Player/Gameplay constants remain for later
  dedicated extraction.
- Existing domain migration bridges (`Enemies → Items/Weapons`, `Items → Weapons`) remain because
  serialized reward/drop assets still need a separate migration plan.
- Runtime Test Runner execution and Play Mode regression were not available through the fixed old
  Unity MCP route in this run. The result is compile/static-validation accepted, not a claim of full
  runtime test coverage.

## 5. Validation Evidence

### Unity MCP route

All Unity operations used the required old `mcp__unity_mcp` route.

### Compilation and Console

- `Unity_RunCommand` requested an AssetDatabase refresh and final compile.
- Compilation succeeded.
- Final Console check: `0 Error / 0 Warning`.
- No MissingReference, Serialization, YAML, shader/material, or missing-script messages were
  reported.

### Script validation

Standard validation returned no diagnostics for:

- `Core/Combat/Stats/StatsCore.cs`
- `Core/Combat/Health/HealthComponent.cs`
- `Core/Combat/DamageSystem/DamageCalculator.cs`
- `Core/Runtime/Stats/StatModifierRegistry.cs`

### Post-acceptance regression correction

After the initial Phase D report, Unity exposed a compile regression in
`Core/Combat/Stats/StatsCore.cs`: the migration had removed the `using Zephyr.Core.DamageSystem;`
directive even though `GetMultipliersForWeapon` still uses `WeaponCategory`, `ElementalType`, and
`WeaponRarity`. The source contracts themselves were present and correctly located in
`Core/Contracts/DamageSystem`; only the import was missing. The directive was restored, followed by
another AssetDatabase refresh and compile.

The corrective validation result is:

- Unity compilation: successful.
- Console: `0 Error / 0 Warning`.
- `StatsCore.cs` standard validation: no diagnostics.

This was a Phase D migration regression, not a new architectural dependency. The final acceptance
score remains `9.2 / 10` because the dependency direction is unchanged and the correction is local.

`git diff --check` was also run. It reported only pre-existing trailing whitespace in the already
modified `BasicEnemy.asset`, `LightningBolt.asset`, and unrelated prior working-tree changes; no
Phase D C# or Markdown patch introduced a new whitespace error.

### Scene and prefab impact

No scenes, prefabs, or serialized asset files were directly edited in Phase D. Therefore no
SceneView capture was required for this phase. The migration preserved existing script `.meta`
files; Unity reimported the new assembly successfully.

## 6. Acceptance Decision

Phase D is accepted because the intended Combat boundary is implemented, the dependency graph is
acyclic, the new assembly has only stable dependencies, Unity compilation succeeds, and the Console
is clean. The remaining deductions are known migration work outside the Phase D acceptance target,
not hidden coupling introduced by this phase.

## 7. Rollback

Rollback must be limited to the Phase D file set. Restore the moved files and their matching `.meta`
files to their previous `Core/Stats`, `Core/Health`, `Core/DamageSystem`, `Core/Buff`, `Core/Combat`,
and `Core/Interfaces` locations; remove the Phase D asmdef and new bridge files; restore the listed
consumer asmdefs and the two architecture-test additions. Do not use a broad working-tree reset,
because Phases A–C and earlier user work are intentionally still present.

## 8. Next Safe Phase

The next architecture-only candidate is Flow/Persistence extraction, but it should remain a separate
phase. Ordinary feature work can use Combat through the explicit assembly reference now. Do not remove
the legacy Core references or migration bridges until their serialized asset and scene contracts have
their own migration and regression gates.

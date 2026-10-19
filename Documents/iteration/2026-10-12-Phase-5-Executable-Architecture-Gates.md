# Phase 5: Executable Architecture Gates

Date: 2026-10-12

## Goal

Turn the architecture boundaries established in earlier phases into repeatable checks that fail when dependency structure, runtime ownership, or restricted global access regresses. This phase is limited to architecture enforcement and verification; it does not migrate the remaining consumers.

## Implementation

- `Zephyr(AI)/Zephyr/Assets/Scripts/Tests/Architecture/ArchitectureInspection.cs` parses assembly definitions, resolves GUID references, builds the project dependency graph, detects cycles, and inspects loaded runtime IL for restricted calls.
- `Zephyr(AI)/Zephyr/Assets/Scripts/Tests/Architecture/StructuralArchitectureTests.cs` validates declared assembly ownership and references, acyclic dependencies, runtime/editor separation, and method-level restricted-call budgets.
- `Zephyr(AI)/Zephyr/Assets/Scripts/Tests/Architecture/ArchitecturePolicy.json` records all 30 project assemblies and the observed runtime call inventory. Every call allowance identifies its caller type as owner and records its count, risk, replacement boundary, reason, and removal condition.
- The runtime scan includes every non-Editor, non-test project assembly. The previous `Zephyr.Testing` diagnostic exclusion was removed after inspecting its runtime contents; the two runtime test-helper components are scanned with no allowance required.
- Architecture tests reject `Resources.Load`, `GameObject.Find`/tag lookup, and `DontDestroyOnLoad` allowances. Existing restricted calls are limited to singleton access, scene scans, and scene loading, each with a method-level count budget.

## Current Inventory

| Restricted call class | Method/target entries | Observed calls |
| --- | ---: | ---: |
| Singleton access | 55 | 80 |
| Scene scan | 7 | 7 |
| Direct scene load/unload | 4 | 4 |
| **Total** | **66** | **91** |

The entries are grandfathered observations, not architectural approval. New callsites, increased counts, unreviewed assembly changes, missing compiled project assemblies, unresolved references, and dependency cycles fail the test suite. When a call is removed, its allowance must also be removed or reduced.

## Validation

- Unity standard script validation: `ArchitectureInspection.cs` and `StructuralArchitectureTests.cs` have no diagnostics.
- EditMode: `Zephyr.Architecture.Tests` plus `Zephyr.Tests.EditMode`: **59 passed, 0 failed, 0 skipped**.
- PlayMode: `FullVerticalSliceUsesDisposableSaveStorage`: **1 passed, 0 failed, 0 skipped**.
- Final Unity Console check through the legacy `mcp__unity_mcp` route: **0 errors, 0 warnings**.
- PlayMode test output reported `There are no audio listeners in the scene`; this did not fail the test and was not present in the final Console query. It remains an existing test-scene/content warning to investigate separately.
- `git diff --check` is not clean across the shared worktree. It reports trailing whitespace in existing scene, prefab, and unrelated script changes, alongside line-ending warnings. Those files contain broad pre-existing user work and were not normalized or edited as part of this architecture-gate task.

## Independent Acceptance

**Accepted: high completion for the scoped architecture-gate task.** The check executes against the currently compiled assemblies, includes the previously excluded runtime test-helper assembly, verifies that the inventory is non-empty, and rejects unregistered calls or budget growth. The complete EditMode architecture suite and the vertical-slice PlayMode test pass after the policy/schema change.

This acceptance does not mean the architecture debt is resolved. The inventory still contains **91** concrete singleton, scene-scan, and scene-transition calls. The policy makes that debt visible and prevents silent growth; it does not make every existing call desirable. Each entry now names its source type, risk, intended replacement boundary, and removal condition so later migrations can be assigned and verified.

## Scope and Limitations

- IL inspection measures compiled call sites and call counts; it does not establish that a call is behaviorally safe in every runtime path.
- Method-level allowlists can require deliberate review updates when methods are renamed or code is reorganized. A policy update must accompany the code review, not be used to silence a failing test without boundary analysis.
- Assembly reference policy captures the present graph. New assemblies or reference changes require deliberate review and an updated policy.
- Unity Test Runner was started through the available test-job API because the legacy Unity MCP route has no equivalent test-run action. Other Unity project inspection, validation, and Console checks followed the repository's legacy MCP routing rule.
- No scenes, prefabs, or serialized Unity assets were modified in this phase; a SceneView capture was therefore not applicable.

## Operating Rule

Run the architecture EditMode suite with changes that affect assembly definitions, global service access, scene discovery/loading, runtime ownership, or composition boundaries. Update `ArchitecturePolicy.json` only after reviewing the new dependency or callsite and recording an owner, reason, risk, replacement boundary, and removal condition. Do not increase a budget merely to make the suite pass. Keep the PlayMode vertical slice as a complementary integration check for lifecycle-sensitive changes.

## Files

- `Zephyr(AI)/Zephyr/Assets/Scripts/Tests/Architecture/ArchitectureInspection.cs`
- `Zephyr(AI)/Zephyr/Assets/Scripts/Tests/Architecture/StructuralArchitectureTests.cs`
- `Zephyr(AI)/Zephyr/Assets/Scripts/Tests/Architecture/ArchitecturePolicy.json`
- `Documents/iteration/2026-10-12-Phase-5-Executable-Architecture-Gates.md`

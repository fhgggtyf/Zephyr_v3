# Legacy Core Removal, Serialized Type Migration and Explicit Dependencies

Date: 2026-10-12

## 1. Scope and acceptance target

This iteration closes the three remaining architecture-cleanliness items identified after the previous phases:

1. Migrate Unity serialized type ownership away from the deleted legacy `Zephyr.Core` assembly.
2. Remove remaining consumers of the legacy `Zephyr.Core` assembly and delete the empty host assembly.
3. Disable implicit project assembly references so dependency direction is declared explicitly and protected by tests.

Acceptance requires successful compilation, clean Unity Console state, architecture tests, a representative PlayMode vertical slice, serialized-reference scans, asmdef scans, and visual checks for every scene rewritten during migration.

## 2. Implemented changes

### 2.1 Legacy Core assembly removal

- Deleted `Assets/Scripts/Core/Zephyr.Core.asmdef` through Unity MCP.
- Removed explicit `Zephyr.Core` references from Flow, Projectiles, Interaction, Rooms, Player, Testing, UI, EditMode tests and PlayMode tests.
- Earlier domain cleanup for Enemies, Items and Weapons is retained and now covered by the same final gate.
- Confirmed no C# implementation remains owned by the deleted assembly.

### 2.2 Script ownership corrections

- Moved `GridAligner.cs` to `Assets/Scripts/Gameplay/Levels/GridAligner.cs` and changed its namespace to `Zephyr.Gameplay.Levels`.
- Preserved the `GridAligner` asset GUID during the move.
- Moved `GameConstants.cs` and `GameSettingsSO.cs` to `Assets/Scripts/Core/Runtime/Constants/` and changed their namespace to `Zephyr.Core.Runtime`.
- Changed `GridAligner.AlignNow()` so a missing Grid only logs in Play Mode, avoiding false editor warnings while Unity loads Prefabs.

### 2.3 Controlled serialized type migration

The migration was performed through Unity Editor APIs and `SerializedObject`; no Prefab, Scene or ScriptableObject YAML was hand-edited.

- Migrated 137 serialized objects.
- Migrated 24 assets.
- Re-saved 8 affected scenes.
- Replaced legacy assembly ownership such as `Zephyr.Core::...` with the actual current assembly, including `Zephyr.Weapons`, `Zephyr.Core.Combat`, `Zephyr.Rooms` and `Zephyr.Core.Flow`.
- Static scanning confirms there are no remaining `m_EditorClassIdentifier: Zephyr.Core::` or `asm: Zephyr.Core` records in Prefabs, Scenes or ScriptableObjects.

Existing namespaces such as `Zephyr.Core.Weapons` and `Zephyr.Core.Stats` remain where changing the CLR type name would create unnecessary serialization compatibility risk. They are namespace compatibility surfaces, not dependencies on the deleted assembly.

### 2.4 Explicit assembly dependency policy

- Changed every project asmdef that previously used `autoReferenced: true` to `autoReferenced: false`.
- Confirmed all 30 project asmdefs parse as valid JSON.
- Confirmed zero asmdefs reference `Zephyr.Core`.
- Updated architecture tests so the legacy host must remain absent and project assemblies must not regain implicit auto-references.

### 2.5 Runtime warning cleanup

`WeaponRuntime.Equip()` previously cleared the Animator controller and then called Animator parameter APIs, producing repeated warnings when equipping `null` or a weapon without a controller. Animator trigger and integer initialization now runs only when a runtime controller is present. This preserves combat behavior while removing invalid Animator calls.

## 3. Verification results

### 3.1 Static architecture gates

- Project asmdefs: 30.
- Invalid asmdef JSON: 0.
- Legacy `Assets/Scripts/Core/Zephyr.Core.asmdef`: absent.
- Explicit references to `Zephyr.Core`: 0.
- `autoReferenced: true`: 0.
- Legacy serialized assembly identifiers: 0.
- `git diff --check`: no whitespace errors; only existing LF/CRLF conversion warnings were reported.

### 3.2 Automated tests

- Architecture EditMode suite: 45 passed, 0 failed, 0 skipped.
- Vertical slice PlayMode test: 1 passed, 0 failed, 0 skipped.
- The vertical slice covered Initializer, MainMenu, Tutorial phases, combat objective completion, MetaHub, LoadingRun, run-scene loading and disposable save storage.

The PlayMode output still contains two transient `There are no audio listeners in the scene` messages during additive scene transitions. They do not remain in the Unity Console, do not fail the test and are not caused by the assembly or serialization migration. This is recorded as a separate test-fixture/audio-lifecycle cleanup item rather than hidden as part of this architecture acceptance.

### 3.3 Final Unity Console

After clearing the Console and completing all validation:

- Errors: 0.
- Warnings: 0.
- Logs: 0.

The earlier Unity Services `services-gateway` editor exception did not reappear in the final clean Console. It remains an external editor-services risk if it returns, not a project assembly dependency.

## 4. Scene visual verification

All eight scenes rewritten by serialized migration were loaded individually with the old `mcp__unity_mcp`, their hierarchy was read, a 2D SceneView capture was inspected, and a PNG was exported.

| Scene | Observation | Screenshot |
| --- | --- | --- |
| `LV1Placeholder` | Tilemap, bounds, spawn point, enemies, pickup and exit are present; no pink materials or obvious missing references. | `D:\GitHub\Zephyr_v3\Zephyr(AI)\Zephyr\Assets\MCPTest\Screenshots\20261012_LV1Placeholder_Migration.png` |
| `LV2Placeholder` | Same room contract remains intact; visible Tilemap geometry is rendered and Console is clean. | `D:\GitHub\Zephyr_v3\Zephyr(AI)\Zephyr\Assets\MCPTest\Screenshots\20261012_LV2Placeholder_Migration.png` |
| `LV3Placeholder` | Ground Tilemap and level objects render; player and room structure remain present without pink materials. | `D:\GitHub\Zephyr_v3\Zephyr(AI)\Zephyr\Assets\MCPTest\Screenshots\20261012_LV3Placeholder_Migration.png` |
| `Tutorial_Phase_1` | Tilemap, camera bounds, spawn point, enemies, weapon pickup and tutorial exit remain authored and visible. | `D:\GitHub\Zephyr_v3\Zephyr(AI)\Zephyr\Assets\MCPTest\Screenshots\20261012_Tutorial_Phase_1_Migration.png` |
| `Tutorial_Phase_2` | Tutorial room contract, boss hierarchy, reward point, pickup and exit remain intact. | `D:\GitHub\Zephyr_v3\Zephyr(AI)\Zephyr\Assets\MCPTest\Screenshots\20261012_Tutorial_Phase_2_Migration.png` |
| `MetaHub` | Player hierarchy, Tilemap, upgrade station and run-start interactable remain present; colored graybox objects are expected. | `D:\GitHub\Zephyr_v3\Zephyr(AI)\Zephyr\Assets\MCPTest\Screenshots\20261012_MetaHub_Migration.png` |
| `GameManager` | Service-only composition scene has an intentionally empty 2D world view; bootstrap, UI, combat, projectile, spawner and camera-rig objects are present. | `D:\GitHub\Zephyr_v3\Zephyr(AI)\Zephyr\Assets\MCPTest\Screenshots\20261012_GameManager_Migration.png` |
| `SampleScene` | Camera, light, Tilemap, player hierarchy and test dummy remain present; the sparse test layout is expected. | `D:\GitHub\Zephyr_v3\Zephyr(AI)\Zephyr\Assets\MCPTest\Screenshots\20261012_SampleScene_Migration.png` |

No inspected scene showed a pink material, missing root object, obvious overlap introduced by migration, or Console error/warning.

## 5. Tool routing

- Scene loading, hierarchy inspection, serialized migration, asset moves, C# structured edits, Console checks and SceneView captures used the old `mcp__unity_mcp` route required by project rules.
- The newer Unity MCP Test Runner API was used only for EditMode and PlayMode test execution because the old route does not expose an equivalent direct test-runner operation. This follows the user's previously granted fallback authorization.
- No image-generation tool was used.

## 6. Independent acceptance assessment

Completion is high and this architecture cleanup is accepted.

- The legacy Core assembly is physically removed rather than left as an empty compatibility host.
- Serialized Unity assets point at their real assembly owners.
- Project assembly dependencies are explicit instead of being silently supplied by Unity auto-reference behavior.
- Tests prevent the deleted host, implicit references and migration bridges from returning unnoticed.
- The representative runtime flow still passes after the migration.
- All rewritten scenes were visually inspected and have persistent PNG evidence.

The remaining old namespace names are intentionally retained compatibility surfaces and do not make the dependency graph dirty. A future namespace rename would be a separate serialized API migration, not required for the current architecture to remain clean.

## 7. Remaining non-blocking work

1. Remove the transient no-AudioListener messages from the PlayMode transition fixture or ensure an AudioListener is continuously owned across additive transitions.
2. Consider namespace modernization only when there is a concrete naming benefit and a dedicated serialized-type migration plan.
3. Continue enforcing the architecture tests and `autoReferenced: false` policy for every new asmdef.

These items do not block normal feature development under the current architecture rules.

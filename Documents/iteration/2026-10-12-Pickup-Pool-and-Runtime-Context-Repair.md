# Pickup Pool and Runtime Context Repair

Date: 2026-10-12
Status: Implementation complete; runtime acceptance pending Unity domain reload

## Scope

This iteration completes the shared runtime path for currency and weapon drops. Ordinary enemies and bosses are separate producers over the same room-scoped pool boundary.

## Implemented

- Extended IDropSpawnGateway with typed SpawnCurrency and SpawnWeapon operations, plus deterministic run seed and room depth context.
- Added ICurrencyPickup and weapon-id spawning to IWeaponPickup; pickup implementations remain responsible for their own runtime behavior.
- Added room-scoped tracking in RoomDropSpawnGateway; every typed spawn is tracked and returned during room teardown.
- Added pooled currency and weapon prefab bindings to Persistent PoolRegistry.
- Added weapon catalog binding to WeaponPickup.prefab for Excalibur and LightningBolt; weapon resolution remains outside enemy/drop-table code.
- Added composition-root discovery in RoomController so room-owned consumers, including wave-spawned enemy brains, receive the current gateway without hand-editing every scene consumer list.
- Added an enemy death-drop path to EnemyBrain; it rolls DropSystem using the room seed, source instance id, room depth, and enemy luck, then emits all results through the room gateway.
- Created and assigned BasicEnemyDrops.asset with one deterministic currency result, proving the ordinary-enemy path is data-driven rather than boss-only.
- Preserved BossRewardDrop as an explicit boss reward producer to avoid double emission when boss rewards are authored separately.
- Added an architecture test covering the typed gateway, shared pool fields, enemy-side dependency direction, and the basic drop table asset.

## Verification

- Architecture tests: 43/43 passed.
- EditMode tests: 5/5 passed.
- Script validation: no diagnostics for IDropSpawnGateway.cs, IWeaponPickup.cs, PoolRegistry.cs, RoomController.cs, or EnemyBrain.cs.
- Unity Console after modifications: 0 Error and 0 Warning entries returned by the connected Console query.
- Persistent scene saved and inspected; PoolRegistry contains both pickup prefab references.
- Weapon pickup catalog inspected through Unity serialization: 2 entries bound.
- Scene verification screenshots exported:
  - D:/GitHub/Zephyr_v3/Zephyr(AI)/Zephyr/Assets/MCPTest/Screenshots/2026-10-12_Persistent_DropPool.png
  - D:/GitHub/Zephyr_v3/Zephyr(AI)/Zephyr/Assets/MCPTest/Screenshots/2026-10-12_LV3_DropPool.png

## Acceptance Caveat

The source asset imported by Unity contains the new EnemyBrain gateway implementation, but the currently loaded Zephyr.Enemies runtime type still reports only its previous ILevelObjective interface during reflection. The connected Unity editor is therefore holding a stale enemy assembly even after refresh and compilation requests. This is the same class of issue behind the reported compilation-errors-on-open but no red Console marker.

Because of that stale assembly state, the ordinary-enemy death-to-pickup path is not accepted as PlayMode-verified in this iteration. Static architecture and EditMode gates pass, and all source/resource wiring is in place, but a Unity editor restart or successful domain reload is still required before claiming runtime acceptance.

## Required Final Check

After Unity fully reloads scripts, verify that Zephyr.Gameplay.Enemies.EnemyBrain implements IDropSpawnGatewayConsumer, then run one PlayMode scenario that kills a BasicEnemy and confirms a pooled CurrencyPickup is spawned and returned on room teardown. Recheck Console for compile errors after the reload.

## Follow-up: Unity 6 EntityId API Migration

- Replaced the obsolete `Object.GetInstanceID()` call in `EnemyBrain` with `GetEntityId().GetHashCode()` for the deterministic drop-roll source value.
- Kept `DropContext`'s existing integer seed contract unchanged; no additional coupling or serialization surface was introduced.
- Forced an AssetDatabase refresh after the edit. The connected Unity Console then reported 0 errors and 0 warnings.
- Standard validation passed for `EnemyBrain.cs` and `DropSystem.cs`; project search found no remaining `GetInstanceID()` call under `Assets/Scripts`.
- The previous compile error was an API compatibility issue exposed after reopening Unity, not a defect in the pickup-pool runtime boundary.

## Architectural Result

The shared pool is now a reusable infrastructure boundary for both ordinary-enemy and boss drops. Drop tables stay data-only, enemy code does not reference WeaponSO, weapon-id resolution stays in the pickup/catalog boundary, and room teardown owns spawned-drop lifetime. Additional drop kinds can be added without creating a second pooling system or expanding enemy-to-item assembly coupling.

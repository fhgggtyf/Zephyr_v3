# Zephyr — Detailed Architecture Design (Draft)

> Status: Draft / all 30 chapters authored — chapters 0–30 reviewed (✅); No chapters pending user review (⏳)
> Project: Zephyr — 2D side-scrolling action Roguelike (Death-Cells-like, smaller scope)
> Companion doc: "游戏项目纸面地基（最终定稿版）" (the Architecture Constitution)
> Layer legend: 🟥 = locked foundation / 🟧 = foundation defines framework, upper layer fills business / 🟩 = upper layer only (constraints only). Locked chapters, once finalized, enter the "lifetime lock" list with the same authority as the Constitution.

---

## Current Implementation Baseline (2026-08-15)

This document is reconciled against the repository under `Assets/Scripts`, scene YAML, prefabs,
ScriptableObject assets, input actions, and generated Unity project files.

1. **Current implementation is authoritative.** When an older passage conflicts with working code
   and the code is a positive evolution, the implementation snapshot in that chapter supersedes it.
2. **Implemented** means the runtime class exists and compiles. **Integrated** additionally means its
   scene/prefab/SO references are authored. A framework without authored content is not content-complete.
3. Concrete foundation contracts are implemented when they can be added without replacing a working
   subsystem. Art, levels, enemy rosters, balance content, and optional tooling remain planned.
4. Old wording that forbids AI edits describes a stability boundary, not the repository authorization
   model. This reconciliation pass is an explicit maintenance exception.

| Area | Current repository status |
|------|---------------------------|
| Assemblies | Seven asmdefs: Core, StateMachine, StateMachine Editor, Weapons Editor, Inputs, Player, UI |
| Combat | Stats, damage, health, resources, weapon components, dual-slot attacks, air-attack gating implemented |
| Buffs | Buff config/controller implemented; authored assets and tests pending |
| Projectiles | Pooled Rigidbody2D projectile implemented; ranged weapon launch integration pending |
| Save/flow | Three manual slots, JSON DTOs, SceneSO event-driven additive loading implemented |
| Meta | Settlement, upgrades, unlocks, objectives, and story-beat framework implemented; UI/content pending |
| Items | Deterministic rolls, pooled pickups, nearest-target interaction implemented; assets pending |
| Rooms/enemies | Basic room lifecycle and BasicEnemy AI/data/component attacks implemented; selector, waves, and roster content pending |
| UI/dialogue | 960x540 UI, menu, HUD, localized two-person dialogue (SO-based, trigger-gated, input-switched), localized dialogue and input lock implemented; modal gameplay UI pauses scaled time (death excluded) |
| Camera | Scene-owned bounds/zones and Hollow-Knight-style follow implemented |
| Tests | No test assemblies yet; the test strategy remains planned |

## Table of Contents & Progress

| # | Chapter | Layer | Status |
|---|---------|-------|--------|
| 0 | Design Charter | 🟥 | ✅ |
| 1 | Engineering Structure & Assembly Definition Strategy | 🟥 | ✅ |
| 2 | Core Abstractions & Interface Contracts (full signatures) | 🟥 | ✅ |
| 3 | Event System / Message Bus | 🟥 | ✅ |
| 4 | Damage System (AD/AP/True + Armor/MR + Pen + Shield-Break) | 🟥 | ✅ |
| 5 | Stats System (Four-Layer Additive Model + Category Multipliers) | 🟥 | ✅ |
| 6 | State Machine System (transitions + priority/interrupts) | 🟥 | ✅ |
| 7 | Buff / Modifier System | 🟥 | ✅ |
| 8 | Object Pool System | 🟥 | ✅ |
| 9 | Weapon System (dual weapons + combos) | 🟧 | ✅ |
| 10 | Projectile System (physics projectiles) | 🟥 | ✅ |
| 11 | Enemy AI System (targeted-potential mechanic) | 🟧 | ✅ |
| 12 | Roguelike Data Layering + Save System | 🟥 | ✅ |
| 13 | Level / Room System (hand-crafted levels) | 🟧 | ✅ |
| 14 | Scene & Game Flow (global state machine) | 🟥 | ✅ |
| 15 | Pickup / Drop System | 🟧 | ✅ |
| 16 | UI Architecture (HUD + Stats Panel + Floating Text + Dialogue) | 🟧 | ✅ |
| 17 | Audio System | 🟧 | ✅ |
| 18 | Input System (new Input System) | 🟥 | ✅ |
| 19 | Camera System | 🟥 | ✅ |
| 20 | Timer / Coroutine Service | 🟥 | ✅ |
| 21 | Animation Conventions | 🟥 | ✅ |
| 22 | Meta Game / Run-End Settlement (potential upgrades + permanent currency) | 🟧 | ✅ |
| 23 | Global Constants & Config (split) | 🟥 | ✅ |
| 24 | Update Loop / Execution Order | 🟥 | ✅ |
| 25 | Logging / Debug / Analytics | 🟥 | ✅ |
| 26 | Testing Strategy | 🟥 | ✅ |
| 27 | Localization (zh + en) | 🟥 | ✅ |
| 28 | Performance & Budget | 🟥 | ✅ |
| 29 | AI Coding Rules / Red Lines | 🟥 | ✅ |
| 30 | Versioning & Iteration Rules | 🟥 | ✅ |

---

## Chapter 0 — Design Charter

### 0.1 Project Positioning

| Dimension | Decision |
|-----------|----------|
| Name | Zephyr |
| Genre | 2D side-scrolling action Roguelike (reference: Dead Cells, smaller scope) |
| Platform | PC Only |
| Team | Single-player game; solo developer + AI collaboration |
| View / Movement | 2D side-scroller standard: left/right + jump / double-jump / ground-slam / climb |
| Levels | Hand-crafted levels primarily; procedural generation not in scope (monitoring cost too high); revisit later if feasible |
| Localization | Chinese + English (zh-CN / en-US) |

### 0.2 Tech Stack

| Item | Choice |
|------|--------|
| Engine | Unity 6000.5.6f1 |
| Render pipeline | URP |
| Input backend | New Input System |
| Programming paradigm | Pure MonoBehaviour (**no DOTS/ECS**) |
| Data-driven | ScriptableObject-first |
| Architecture style | SO event-driven publish-subscribe + SO state machine + object pool + interface contracts |
| Serialization (save) | JSON (JsonUtility) + version field + migration hooks (see Ch.12) |
| Asset loading | Direct references primarily; scene-based level loading; Addressables only if memory pressure (see Ch.1.4) |

> **On DOTS/ECS (not adopted):** DOTS/ECS is Unity's high-performance data-oriented architecture for thousands of on-screen entities (bullet-hell, RTS). Zephyr is 2D side-scroll with < 50 concurrent enemies; MonoBehaviour is more than sufficient, and DOTS would sharply raise development/debug cost. Not adopted.

### 0.3 Core Gameplay Pillars

1. **Four-layer additive stat system**: Base / RunGains / Modifiers / TempModifiers; Potential (max 10) gates RunGains gain efficiency only. See Ch.5.
2. **LoL-style damage**: AD / AP / True + Armor/MR + flat/% penetration + Shield with Shield-Break coefficient. See Ch.4.
3. **Down-and-respawn risk mechanic**: HP reaching 0 is a **down**, not run-over — the player respawns with an escalating potential deduction (each down costs more than the last). **Any potential ≤ 0 = true death** (run over). Potentials are **run-local**: at run entry the system **randomly allocates N−M points** of the budget across the 10 types (the random baseline — the source of dynamic difficulty), after which the player **allocates the remaining M points**; they erode on downs and reset each run. **No cross-run erosion** — each run is a clean slate. Monsters carry a "targeted potential" that directs the down-cost onto a specific stat. See Ch.5.6/5.7.
4. **Dual-input weapon combat**: Two weapons are carried simultaneously. PrimaryAttack always requests Primary and SecondaryAttack always requests Secondary; an optional slot marker only identifies which slot a pickup will replace. Combos are weapon-inherent.
5. **Energy/Stamina resources**: Two regenerating bars managed by `PlayerResourceController`. Stamina currently powers configured weapon attacks, sprinting, jumping, and double-jumping; Energy powers configured weapon attacks and is reserved for dash/roll/channeling/skills. Each bar starts regenerating only after its own post-use delay. Architecture supports both skill and skill-less modes.
6. **Meta progression**: Hades-like permanent currency buys upgrades (base stat value / extra damage multiplier / small weapon-codex bonuses / **grow the run-entry potential budget N** / **grow the player-allocatable share M**); in-run weapons are Dead-Cells-like (drops / shops / boss rooms).

### 0.4 Non-Goals (this phase)

- Procedural level generation (deferred)
- Multiplayer / online
- Mod / workshop support (no special openness built in)
- DOTS/ECS
- Mobile / console

### 0.5 Performance Budget (recommended, pending your sign-off)

| Metric | Target |
|--------|--------|
| Target frame rate | 60 FPS (≥ 30 without stutter) |
| Concurrent enemies | ≤ 50 (projectiles ≤ 200) |
| Memory budget | ≤ 8 GB (dev machine 16 GB) |
| Level load time | ≤ 3 s |

> 2D + URP + < 50 enemies makes this budget comfortable; calibrate via Profiler later.

---

## Chapter 1 — Engineering Structure & Assembly Definition Strategy

### 1.1 Directory Structure (extended from the Constitution)

```Plain Text
Assets/
├─ Scripts/
│  ├─ Core/                        # 🟥 foundation, hand-locked, AI must not edit source
│  │  ├─ Constants/                # GameConstants, GameSettingsSO, StatType, MetaStatType
│  │  ├─ Interfaces/               # global interface contracts (IDamageable, IStatSource, IPooledObject, etc.)
│  │  ├─ Events/                   # GameEventSO, GameEventSO<T>, GameEventListener<T>, EventBus
│  │  ├─ ObjectPool/               # ObjectPool, PoolRegistry (MonoBehaviour), PooledObject, IPooledObject
│  │  ├─ DamageSystem/             # DamageCalculator, DamageInfo, DamageResult, DamageType, ElementalType, WeaponCategory
│  │  ├─ Stats/                    # StatRecord, StatsCore (4-layer additive model + category multipliers)
│  │  ├─ StateMachine/             # StateSO, StateActionSO, StateConditionSO, TransitionTableSO, runtime State/StateMachine
│  │  ├─ Save/                     # SaveSystem, MetaData, RunData, serializers
│  │  ├─ GameFlow/                 # GameFlow (MonoBehaviour singleton), GameState, SceneId, SceneLoader
│  │  ├─ Timer/                    # TimerService, TimerHandle
│  │  ├─ Logging/                  # ZephyrLogger, LogLevel
│  └─ Zephyr.Core.asmdef           # assembly definition for all Core code
│  ├─ Player/
│  │  ├─ Core/                     # MovementCore, StatsCore, HurtReceiver, PlayerResourceController
│  │  ├─ States/                   # Idle/Walk/Run/Jump/DoubleJump/Dash/AirDash/Roll/Climb/Crouch/Channeling/Attack/Hurt/Death
│  │  ├─ Animation/                # anim params, anim event bindings
│  │  └─ Input/                    # PlayerInputReader
│  ├─ Enemies/
│  │  ├─ BaseEnemyCore/            # enemy core, stats, targeted-potential data
│  │  └─ States/                   # enemy behavior StateSOs
│  ├─ Weapons/
│  │  ├─ WeaponBase.cs             # 🟥 permanently locked
│  │  └─ WeaponBehaviours/         # per-weapon logic + combo data
│  ├─ Projectiles/
│  │  ├─ ProjectileBase.cs         # 🟥 locked base
│  │  └─ ProjectileBehaviours/     # per-projectile logic (incl. gravity-affected)
│  ├─ Rooms/                       # level/room loading framework (hand-crafted)
│  ├─ Items/                      # pickup + drop tables (NO inventory/backpack)
│  ├─ Meta/                        # meta progression, potential upgrades, weapon codex
│  ├─ UI/
│  │  ├─ CoreUIManager/            # UI foundation manager
│  │  ├─ FloatingTextPool/         # multi-type floating text pool
│  │  └─ Panels/                   # HUD, menu, stats panel
│  ├─ Audio/                       # AudioManager, pooling, mixer groups
│  ├─ Camera/                      # follow, room transition, shake
│  └─ Tests/                       # EditMode + PlayMode tests
│
├─ ScriptableObjects/              # all SO config, decoupled from Scripts
│  ├─ Events/                      # event channel SOs
│  ├─ PlayerStates/  ├─ EnemyStates/
│  ├─ WeaponConfigs/  ├─ BuffConfigs/
│  ├─ DropTables/  ├─ MetaUpgrades/
│  └─ GameSettings/                # global constants config, level config, balance knobs
│
├─ Prefabs/  (Player/Enemies/Weapons/Bullets/DropItems/UIPrefabs/Rooms)
├─ Animations/  (Player/Enemies)
├─ ArtResources/  (Sprites/Materials/Particles/Audio/Fonts)
├─ Scenes/  (MainMenu/Hub/Levels/Test)
└─ Settings/  (URP/Input/Layers/Tags)
```

### 1.2 Mandatory Directory Rules (inherited +补充)

1. Core may not be modified at scale once finalized.
2. New features may only add upper-layer scripts; no foundation intrusion.
3. All numeric values go into SOs; hardcoding forbidden.
4. **Domain modules may not reference each other** (Player↛Enemies, Weapons↛Projectiles…); cross-domain communication goes only through the Core event system (Ch.3).
5. AI may only **register new pool instances**; it must not create new foundation pool logic (ObjectPool/PoolRegistry/PooledObject are immutable).

### 1.3 Assembly Definition Strategy

**Implemented topology (supersedes the one-asmdef-per-domain proposal below):** runtime assemblies are
`Zephyr.Core`, `Zephyr.Core.StateMachine`, `Zephyr.Inputs`, `Zephyr.Player`, and `Zephyr.UI`; editor
assemblies are StateMachine Editor and Weapons Editor. Weapons, projectiles, items, meta, camera, audio,
save, and flow currently live in Core. Splitting them before a demonstrated ownership need would add
assembly churn without useful isolation. The following diagram is retained as a long-term target.

**Principle**: Core is its own asmdef, `internal` by default, only explicit public API. Each domain module is its own asmdef. Domain modules depend only on Core, never on each other.

```Plain Text
Zephyr.Core              (no deps, internal by default, public API surface only)
 ├─ Zephyr.Player        → Core
 ├─ Zephyr.Enemies       → Core
 ├─ Zephyr.Weapons       → Core
 ├─ Zephyr.Projectiles   → Core
 ├─ Zephyr.Rooms         → Core
 ├─ Zephyr.Items         → Core
 ├─ Zephyr.Meta          → Core
 ├─ Zephyr.UI            → Core
 ├─ Zephyr.Audio         → Core
 ├─ Zephyr.Camera        → Core
 └─ Zephyr.Tests         → all (EditMode + PlayMode)
```

Dependency direction (one-way):

```Plain Text
Domain module ──► Zephyr.Core
   │
   └─(cross-domain)─► Core Event System ──► subscribing domain module
```

**Benefits**: faster compilation; compile-time isolation (cross-domain refs error out); Core internals protected by `internal`, preventing AI misuse.

### 1.4 Asset Loading Strategy

| Asset type | Loading |
|------------|---------|
| SO configs (weapon/buff/state/event channel) | Inspector direct reference (serialized field) |
| Core prefabs (player/enemy/weapon) | Inspector direct reference |
| Level scenes | Scene-based loading (hand-crafted; one scene per level; rooms additive within) |
| Bulk same-type assets (e.g., multi-level textures) | Addressables only if memory pressure |

> SM scope + 2D: direct references suffice; Addressables added too early adds complexity.

### 1.5 Naming Conventions (summary)

- Scripts: `PascalCase`; interfaces prefixed `I`; SO definitions suffixed `...SO` (e.g., `WeaponConfigSO`); SO asset instances suffixed `...Asset`.
- State SOs: `<Actor><State>StateSO` (e.g., `PlayerDashStateSO`).
- Event channel SOs: `On<Event>EventSO` (e.g., `OnEnemyDiedEventSO`).
- asmdef: `Zephyr.<Domain>`.

---

## Chapter 2 — Core Abstractions & Interface Contracts

All cross-module interaction goes through these contracts. **Full C# signatures are part of the lock** — parameter types and return values are binding. The foundation provides default/base implementations; upper layers consume them.

### 2.1 IPooledObject

```csharp
// Contract for pooled components (projectiles, pickups, VFX, audio).
// OnSpawn/OnDespawn pair replaces Init/Recycle from the original design.
public interface IPooledObject
{
    void OnSpawn();        // called when taken from pool; activate + reset state
    void OnDespawn();      // called when returned to pool; deactivate + clean up
    void ReturnToPool();   // convenience for the object to return itself
}
```

### 2.2 IDamageable

```csharp
// Single damage entry point. Implementations (HealthComponent) MUST call
// DamageCalculator and must not re-implement calculation logic.
// Properties like IsInvulnerable/IsAlive/CurrentHp/MaxHp live on HealthComponent,
// not on this interface — they are checked by the caller before calling TakeDamage.
public interface IDamageable
{
    void TakeDamage(DamageInfo damageInfo);
}
```

### 2.3 IShieldable

```csharp
// Contract for entities with a shield layer that absorbs damage before health.
// DamageCalculator checks this interface to split damage between shield and HP.
public interface IShieldable
{
    float CurrentShield { get; }
    float MaxShield { get; }
    float ApplyShieldDamage(float damage);  // returns overflow to health
}
```

### 2.4 IHealable

```csharp
// Contract for entities that can be healed (player only).
// Healing always succeeds; overhealing is silently clamped to MaxHp.
public interface IHealable
{
    void Heal(float amount);
}
```

### 2.5 IInteractable

```csharp
// Contract for any GameObject the player can interact with
// (doors, ladders, weapon pickups, NPCs). No IInteractor parameter —
// all interaction types share a single input (no dedicated per-type keys).
public interface IInteractable
{
    bool CanInteract();
    void OnInteract();
}
```

### 2.7 IFacingProvider

```csharp
// Provides facing direction (2D Vector2). Used by enemy AI detection/aggro,
// backstab calculation, and camera directional look-ahead.
public interface IFacingProvider
{
    UnityEngine.Vector2 Facing { get; }
}
```

### 2.8 ITickable

```csharp
// Marker interface for MonoBehaviours that tick through the centralized
// Timer/Tick service instead of private Update loops.
public interface ITickable
{
    void Tick(float deltaTime);
}
```

### 2.10 Core Data Structures (binding)

```csharp
// Immutable damage payload. Unified damage value (no separate flat/percent portions).
// AdditionalMultipliers carry pre-resolved per-category sums from the attacker's StatsCore
// (melee, fire, rare, etc.) — these are applied multiplicatively by DamageCalculator.
public readonly struct DamageInfo
{
    public readonly float Damage;                    // base damage value
    public readonly DamageType Type;                  // AD / AP / True
    public readonly bool IsCritical;
    public readonly bool IsBackstab;
    public readonly float Piercing;                  // 0..1, armor/MR penetration
    public readonly Vector2 SourcePosition;
    public readonly GameObject Source;
    public readonly float ShieldCoeff;               // P, weapon-inherent, ≥ 0, no upper bound (Ch.4.7)
    public readonly WeaponCategory? Category;        // Melee/Ranged — for multiplier lookup
    public readonly ElementalType? Element;          // elemental type — for multiplier lookup
    public readonly WeaponRarity? Rarity;            // rarity tier — for multiplier lookup
    public readonly float[] AdditionalMultipliers;   // post-backstab, pre-variance category-based buffs
                                                    // e.g. [0.05, 0.10] → damage *= 1.05 * 1.10

    public DamageInfo(float damage, DamageType type, bool isCritical, bool isBackstab,
        float piercing, Vector2 sourcePosition, GameObject source, float shieldCoeff,
        WeaponCategory? category, ElementalType? element, WeaponRarity? rarity,
        float[] additionalMultipliers);
}

// Final result of a damage calculation, split between shield and health.
public readonly struct DamageResult
{
    public readonly float DealtToShield;
    public readonly float DealtToHp;
    public readonly bool ShieldBroken;
    public readonly bool TargetKilled;
    public float TotalDamage => DealtToShield + DealtToHp;
}

public enum DamageType { AD, AP, True }      // Attack Damage / Ability Power / True

public enum WeaponCategory { Melee, Ranged, Shield }
public enum ElementalType { None, Physical, Fire, Ice, Lightning, Wind, Earth }
public enum WeaponRarity { Common, Uncommon, Rare, Epic, Legendary }

// Runtime stat record: permanent Base + per-run Potential (0-10, re-rolled each run).
public struct StatRecord
{
    public float Base;        // permanent meta-upgrade value (profile lifetime, never reset)
    public float Potential;   // per-run allocation, gates in-run gain scaling

    public float GetScaleMultiplier() => Potential / (float)GameConstants.Stats.PotentialMax;
    public float GetScaledGain(float rawGain) => rawGain * GetScaleMultiplier();
}

// Meta-only stat types — permanently set by meta upgrades, no Potential gating.
public enum MetaStatType
{
    CritChance, CritDamage, Piercing, MaxShield, ShieldCoeffBonus,
    MoveSpeed, PickupRange, VarianceReduction, LuckBonus,
    DamageReduction, PotentialBudget, ShareMultiplier
}

// Category-based multiplicative buffs (meta base + in-run addends).
// Same-category buffs sum additively in StatsCore; different categories multiply in DamageCalculator.
// Example: meta fire buff 5% + in-run fire buff 10% = 15% summed → damage *= 1.15 for fire category.
```

### 2.8 Contract Rules (binding)

- All characters/monsters/weapons interact **only** through the interfaces above.
- Direct `GetComponent` across modules is forbidden; cross-module hard references are forbidden.
- `IDamageable.TakeDamage` must delegate to `DamageCalculator`; it must not re-implement math.
- Any new cross-module need requires a new interface in Core first (doc rule, Ch.30).

---

## Chapter 3 — Event System / Message Bus

The SO event-driven publish-subscribe layer is the **decoupling backbone**. It is the only sanctioned cross-module communication channel (Ch.1.2 rule 4). Without it, the "states can't write transitions", "in-run can't touch out-run saves", and "domain modules can't reference each other" rules are unenforceable.

### 3.1 Core Types

```csharp
// Abstract non-generic base for all GameEvent ScriptableObjects.
// GameEventSO<T> extends this for typed payload events.
// Events are authored as assets in ScriptableObjects/Events/ and raised at runtime.
public abstract class GameEventSO : ScriptableObject
{
    public abstract void Raise();
}

// Generic typed event channel (SO asset). Lives in ScriptableObjects/Events/.
// Uses List<Action<T>> for subscribers, iterates in reverse order on Raise.
// Payloads should be readonly structs to prevent mutation during dispatch.
public class GameEventSO<T> : GameEventSO
{
    private readonly List<Action<T>> _listeners = new List<Action<T>>();

    public void Subscribe(Action<T> listener)
    {
        if (!_listeners.Contains(listener))
            _listeners.Add(listener);
    }

    public void Unsubscribe(Action<T> listener)
    {
        _listeners.Remove(listener);
    }

    public void Raise(T payload)
    {
        for (int i = _listeners.Count - 1; i >= 0; i--)
            _listeners[i]?.Invoke(payload);
    }

    public override void Raise() => Raise(default);
}
```

```csharp
// Abstract base listener for GameEventSO (non-generic).
// Attach to any GameObject; assign a GameEventSO in the Inspector.
// OnEnable/OnDisable auto-subscribe/unsubscribe via RegisterListener/UnregisterListener.
public abstract class GameEventListener : MonoBehaviour
{
    [SerializeField] protected GameEventSO Event;

    protected virtual void OnEnable()  => RegisterListener();
    protected virtual void OnDisable() => UnregisterListener();

    protected abstract void RegisterListener();
    protected abstract void UnregisterListener();
}

// Concrete generic listener for GameEventSO<T>.
// Extends GameEventListener with typed reference + UnityEvent<T> response.
public class GameEventListener<T> : GameEventListener
{
    [SerializeField] private GameEventSO<T> _typedEvent;
    [SerializeField] private UnityEvent<T> _response;

    private void OnEventRaised(T payload) => _response?.Invoke(payload);

    protected override void RegisterListener()   => _typedEvent?.Subscribe(OnEventRaised);
    protected override void UnregisterListener() => _typedEvent?.Unsubscribe(OnEventRaised);
}
```

```csharp
// Central typed event bus singleton for runtime, ad-hoc event publishing.
// Unlike GameEventSO<T> (authored in editor as SO assets), EventBus uses
// dictionary-based delegate dispatch for events created and published at
// runtime without pre-authored assets. Lives in the Persistent scene.
public sealed class EventBus : MonoBehaviour
{
    public static EventBus Instance { get; private set; }

    private readonly Dictionary<Type, Delegate> _subscribers = new Dictionary<Type, Delegate>();

    public void Subscribe<T>(Action<T> handler);
    public void Unsubscribe<T>(Action<T> handler);
    public void Publish<T>(T payload);
}
```

### 3.2 SO Event System vs EventBus (binding)

| Feature | GameEventSO<T> | EventBus |
|---------|----------------|----------|
| Authoring | Editor-authored SO asset | Code-created at runtime |
| Persistence | Survives scene loads | Persistent scene (always available) |
| Use case | Cross-scene, persistent events (OnEnemyDied, OnSceneLoaded) | Ad-hoc runtime events (ProjectileHit, temp state) |
| Subscription | Subscribe/Unsubscribe on SO | Subscribe/Unsubscribe on EventBus singleton |

**Rule**: Use `GameEventSO<T>` for all persistent, cross-module events. Use `EventBus` only for short-lived, runtime-created events that don't warrant a SO asset. All GameEventSO<T> instances are Core assets in `ScriptableObjects/Events/`.

### 3.3 Event Catalog (categories, not exhaustive)

| Category | Example events | Payload |
|----------|----------------|---------|
| Combat | `OnEnemyDied`, `OnPlayerHit`, `OnCritLanded` | `EnemyDiedInfo` / `HitInfo` |
| Stats | `OnStatChanged`, `OnPotentialChanged`, `OnResourceChanged` | `StatChangeInfo` |
| Flow | `OnRunStart`, `OnRunEnd`, `OnSceneLoaded`, `OnDeath` | `RunSummary` / `SceneInfo` |
| Pickup | `OnItemPickedUp`, `OnWeaponAcquired`, `OnCurrencyChanged` | `ItemPickupInfo` |
| Save | `OnSaveRequested`, `OnSaveCompleted`, `OnLoadCompleted` | `SaveSlotInfo` |
| State | `OnStateChanged` (actor, from→to) | `StateChangeInfo` |

All event channel SOs are **Core assets** in `ScriptableObjects/Events/`. Domain modules may not define new event channel *types*; new events = new SO asset + registration in this catalog.

### 3.4 Binding Rules (binding)

1. Subscribe in `OnEnable`, unsubscribe in `OnDisable`. Pooled objects subscribe in `Init()`, unsubscribe in `Recycle()` — never rely on `OnEnable`/`OnDisable` for pooled objects.
2. **Payloads must be `readonly struct`s or IDs** — never `MonoBehaviour`/`GameObject` references. Use `StatId`, `ItemId`, `EnemyId`. Prevents dangling refs after scene unload / pool recycle.
3. Events fire **synchronously on the main thread within the raising frame**. Handlers must stay lightweight (no allocation, no heavy I/O).
4. **No synchronous cycles**: a handler must not raise an event that (directly or indirectly) raises the same event in the same frame. Debug builds run a cycle detector that logs and breaks the loop.
5. **Forced ordering**: if two handlers must run in order, do not rely on subscription order — split into two sequenced events or use a single handler that calls both in order.

### 3.5 Lifecycle & Cross-Scene

- Event channel SOs survive scene loads (they are assets, not scene objects).
- Listeners are MonoBehaviours; they re-subscribe automatically via `OnEnable` on scene load.
- On scene unload, `PoolRegistry.ClearAll(sceneKey)` (Ch.8) and pooled listeners unsubscribe via `Recycle()`.
- Persistent systems (meta, save) subscribe once at boot and survive scene transitions.

### 3.6 Red Lines

- Domain modules publish/subscribe **only** to Core event SOs.
- No domain may invent a new event channel type; new events require Core registration (doc-first, Ch.30).
- Event payloads must not smuggle behavior (no `Action`/`Func` in payloads) — payloads are data only.
- Raising events from `OnDestroy` is forbidden (order is non-deterministic); use explicit teardown.

---

## Chapter 4 — Damage System

### 4.1 Damage Types

Only three types at the foundation: **AD (Attack Damage / 物理)**, **AP (Ability Power / 法术)**, **True (真实)**. Elemental附加 (Fire, Ice, Lightning, Wind, Earth) is a *tag* layered on top of one of these three via `DamageInfo.Element`. Elemental异常 (burn/freeze/shock) are status effects delivered via the Buff system (Ch.7), not separate damage types.

### 4.2 Unified Damage Model

Per the confirmed rule: **DamageInfo carries a single unified `Damage` value**, not separate flat/percent portions. The damage value is computed by the weapon/skill builder before creating `DamageInfo`:

- `Damage = (char.BaseStat + weapon.BaseStat) × comboStepMultiplier × otherBonuses`

All damage values (flat, percent-of-max, etc.) are folded into the single `Damage` value by the attack creator. There is no separate percent-damage portion in the pipeline.

### 4.3 Armor / Magic Resist (with penetration)

For AD and AP portions (True bypasses entirely):

```
effectivePierce = Clamp01(info.Piercing)
mitigatedDamage = rawDamage × (1 − Clamp01(defense / (defense + 100)) × (1 − effectivePierce))
```

- `defense` = Armor (for AD) or MagicResist (for AP), read from `IStatSource.GetStatValue()`.
- Pen comes from `DamageInfo.Piercing` — the total resolved penetration (weapon-inherent + player meta-acquired), computed by the attack creator before pipeline entry.
- There is **no independent "% resistance" stat**; monster difficulty scaling is achieved by raising Armor/MR values per stage.

### 4.4 Crit

Crit multiplies the **unified damage value** (pre-mitigation), by `GameConstants.Combat.CriticalDamageMultiplier` (default 2.0). The `IsCritical` flag is set on `DamageInfo` at attack creation time.

### 4.5 Backstab (facing-dependent, enemies only)

A hit is a **backstab** when the attacker strikes an enemy from behind (the enemy's facing is away from the attack source). On a backstab, the unified damage is multiplied by `GameConstants.Combat.BackstabDefaultMultiplier` (default 1.0 = no bonus). Backstab applies **only to enemies** — players are never backstabbed. It stacks multiplicatively with crit (both are pre-mitigation multipliers). Backstab is detected from the target's facing at hit time (Ch.11.3) and flagged on `DamageInfo.IsBackstab`.

### 4.6 Additional Multipliers (category-based buffs)

After crit and backstab, **category-based multiplicative buffs** from the attacker's `StatsCore` are applied. These are pre-resolved per-category sums (meta base + in-run addends summed in `StatsCore`) carried in `DamageInfo.AdditionalMultipliers[]`. Cross-category stacking is multiplicative:

```
damage *= (1 + sum_melee) × (1 + sum_fire) × (1 + sum_rare) × ...
```

Same-category buffs sum additively in `StatsCore`; different categories multiply here. Each value is a decimal: +5% = 0.05f, -10% debuff = -0.10f. Populated by the attack creator via `StatsCore.GetMultipliersForWeapon()`.

### 4.7 Full Resolution Pipeline (binding order — must never be reordered)

```
1. Apply critical hit       — if IsCritical, damage ×= CriticalDamageMultiplier (2.0)
2. Apply backstab           — if IsBackstab, damage ×= BackstabDefaultMultiplier (1.0)
3. Apply additional multipliers — damage *= ∏(1 + multiplier[i]) for each category-based buff
4. Apply damage variance    — ±5% normal distribution (non-deterministic per hit)
5. Apply mitigation         — armor/MR reduction with piercing (True bypasses)
6. Shield-break split       — using post-mitigation D, ShieldCoeff P, current shield S (see 4.8)
7. Return DamageResult      — DealtToShield, DealtToHp, ShieldBroken
```

> **Note**: Invulnerability checks (i-frames) are performed by the caller (HealthComponent) before calling `DamageCalculator.Calculate()`, not inside the calculator.

### 4.8 Shield-Break Resolution (locked formula)

Let `D = post-mitigation damage`, `P = ShieldCoeff` (weapon-inherent, ≥ 0), `S = CurrentShield`.

**Case A — `P < 1`:**
```
shieldDamage = D × P
if shieldDamage ≤ S:                          // shield holds
    hpDamage = D × (1 − P) × 0.3              // 0.3 leak mechanic
else:                                         // shield breaks
    hpDamage = D × (1 − P) + (D × P − S)      // no ×0.3, plus overflow
```

**Case B — `P ≥ 1`:**
```
shieldDamage = D × P
if shieldDamage ≤ S:                          // shield holds
    hpDamage = 0                              // full absorb
else:                                         // shield breaks
    hpDamage = (D × P − S) / P               // overflow divided by P
```

- `P = 0` falls under Case A: shield takes 0, HP takes `D × 1 × 0.3 = 0.3D` (leak).
- `P` is a weapon-inherent value; there is no "normal attack default P" — each weapon defines its own.
- Shield does **not** participate in armor/MR reduction (armor/MR applies before shield split).

### 4.9 Worked Examples (disambiguation table)

Post-mitigation `D = 100` in all rows; varying `P` and `S`:

| P | S | Case | shieldDamage | Shield holds? | hpDamage | Notes |
|---|---|------|--------------|---------------|----------|-------|
| 0.0 | 80 | A | 0 | yes | 30 | 0.3 leak, shield untouched |
| 0.5 | 80 | A | 50 | yes | 15 | `100×0.5×0.3` |
| 0.5 | 30 | A | 50 | **no (break)** | 70 | `100×0.5 + (50−30)` |
| 1.0 | 120 | B | 100 | yes | 0 | full absorb |
| 1.0 | 80 | B | 100 | **no (break)** | 20 | `(100−80)/1` |
| 2.0 | 250 | B | 200 | yes | 0 | full absorb |
| 2.0 | 150 | B | 200 | **no (break)** | 25 | `(200−150)/2` |

Full-pipeline example: `damage = 240 phys, crit (×2.0), backstab (×1.0), no additional multipliers, target 3000HP/100armor, P=0.5, S=40, pierce=0`:
1. crit: 240 × 2.0 = 480
2. backstab: 480 × 1.0 = 480 (no change)
3. additional multipliers: none → 480
4. variance: 480 × 0.97 = 465.6 (example roll)
5. mitigate: armor 100 → 465.6 × (1 − 100/200 × 1.0) = 465.6 × 0.5 = 232.8
6. shield split (P=0.5, S=40): shieldDamage = 232.8×0.5 = 116.4 > 40 → break; hpDamage = 232.8×0.5 + (116.4−40) = 116.4 + 76.4 = 192.8
7. DamageResult { DealtToShield: 40, DealtToHp: 192.8, ShieldBroken: true }

> Note: the variance roll is **non-deterministic** — it uses `UnityEngine.Random` and re-rolls every hit. Attacks are high-frequency, so per-hit variance is not seed-locked (unlike reward RNG, Ch.12.5).

### 4.10 DoT (Damage over Time)

- DoT is **adaptive**: each tick inherits the source weapon's damage type (AD/AP/True) and benefits from weapon + character stats (computed at application time, snapshot at application).
- Delivered as repeated `DamageInfo` via the Timer service (Ch.20).
- Elemental异常 DoT (burn/shock) follow the same rule; freeze is a control effect, not damage.

### 4.11 Kill Credit & Source Tracking

`DamageInfo.Source` carries the attack source GameObject through to `DamageResult`. On kill, the event system emits `OnEnemyDied` with killer info, consumed by Meta/loot/potential-attack subsystems.

### 4.12 Constants

All constants are in `GameConstants`:

```csharp
GameConstants.Combat:
    CriticalDamageMultiplier = 2f;       // default crit multiplier
    BackstabDefaultMultiplier = 1f;      // default backstab multiplier (no bonus)
    WeaponPickupRange = 1.5f;
    CurrencyMagnetRadius = 5f;

GameConstants.Damage:
    DamageVarianceStdDev = 0.05f;        // 5% base variance (normal distribution σ)
    LuckVarianceMeanShift = 0.01f;       // +1% per Luck point (mean shift)
    PenetrationMin = 0f;
    PenetrationMax = 1f;
```

### 4.13 Damage Variance (normal distribution, non-deterministic)

A base **±5%** normal-distribution fluctuation is applied to the unified damage value **after additional multipliers, before mitigation**. The variance is applied via `Random.Range(-3, 3) * StdDev`, giving a range of ±15% with σ = 5%.

```csharp
// In DamageCalculator.ApplyVariance — applied after step 3, before step 5
float variance = 1f + Random.Range(-3f, 3f) * GameConstants.Damage.DamageVarianceStdDev;
float variedDamage = damage * variance;
```

- **StdDev = 0.05f**: at Luck=0, damage fluctuates with σ = 5% (≈68% of rolls within ±5%, ≈95% within ±15%).
- **Luck shift**: The `Luck` stat shifts the distribution's mean upward (handled by the attack creator populating AdditionalMultipliers with Luck-based bonuses).
- **Applied after additional multipliers, before mitigation** (step 4 in pipeline 4.7).
- **Not applied to DoT**: DoT damage is deterministic per application (no per-tick variance).
- **Non-deterministic (per-hit random)**: variance uses `UnityEngine.Random`, not the run-seeded RNG. Attacks are high-frequency, so per-hit variance is not seed-locked.

---

## Chapter 5 — Stats System (Four-Layer Additive Model + Category Multipliers)

### 5.1 Core Stat Types (10 — these carry Potential)

```csharp
public enum StatType
{
    Attack,        // 攻击  (physical attack power)
    MagicAttack,   // 魔攻
    Armor,         // 物防
    MagicResist,   // 魔防
    MaxHp,         // 生命
    Stamina,       // 耐力  (stamina bar length)
    Luck,          // 运气
    Tenacity,      // 韧性  (CC resistance)
    Energy,        // 能量  (energy bar length)
    AttackSpeed    // 攻速
}
```

> These 10 are the **only** stats that carry a Potential layer. Other stats exist in the game — e.g., crit chance, crit damage, piercing, max shield — and are defined by **MetaStatType** (Ch.5.1b) which are permanently set by meta upgrades without Potential gating. The 4-layer model below applies to the 10 core `StatType` values.

### 5.1b Meta-Only Stat Types (no Potential, permanent)

```csharp
public enum MetaStatType
{
    CritChance,        // base critical chance (decimal, e.g., 0.05 = 5%)
    CritDamage,        // base crit damage multiplier (added to base 2.0)
    Piercing,          // base piercing (0.0–1.0)
    MaxShield,         // max shield value (flat)
    ShieldCoeffBonus,  // bonus shield coefficient P (added to weapon P)
    MoveSpeed,         // move speed multiplier
    PickupRange,       // weapon/currency pickup range multiplier
    VarianceReduction, // reduces damage variance
    LuckBonus,         // luck bonus for drop quality
    DamageReduction,   // damage reduction %
    PotentialBudget,   // additional run-entry Potential budget
    ShareMultiplier    // player-allocatable share multiplier
}
```

> `MetaStatType` values are set once via `StatsCore.SetMetaStat()` and never reset — they are profile-lifetime. They are **not** Potential-gated and do not participate in the 4-layer stat calculation. They are read directly via `StatsCore.GetMetaStat()` by subsystems like `DamageCalculator` (crit, piercing, shield coefficient) and player controllers (move speed, pickup range).

### 5.2 Four-Layer Additive Model

Every **core** stat (the 10 `StatType` values) uses a four-layer additive model to compute its total value. The formula is:

```
GetStatValue(stat) = Base + RunGains + Modifiers + TempModifiers
```

Each layer has a different lifetime and gating rules:

| Layer | Name | Lifetime | Potential-Gated? | Description |
|-------|------|----------|-------------------|-------------|
| 1 | **Base** | Profile (never reset) | No | Permanent meta-upgrade value. Set via `SetBase()`. Survives across runs. |
| 2 | **RunGains** | Run (reset each run) | **Yes** | In-run acquired gains (relics, weapons, room rewards). Each gain is pre-scaled: `actual = raw × (Potential / PotentialMax)`. Applied via `ApplyPotentialScaledGain()`. |
| 3 | **Modifiers** | Run (reset each run) | No | In-run non-Potential modifiers (room events, status effects). Added/removed via `AddModifier()`/`RemoveModifier()`. |
| 4 | **TempModifiers** | Timer/Condition expiry | No | Short-lived buffs (e.g., potion: +20% attack speed for 30s). Managed by external timer/status systems. Added/removed via `AddTemporaryModifier()`/`RemoveTemporaryModifier()`. |

```csharp
// StatRecord holds Base (permanent) + Potential (per-run, 0–10)
public struct StatRecord
{
    public float Base;        // Layer 1 — permanent meta value
    public float Potential;   // 0..10 — gates Layer 2 gain scaling

    public float GetScaleMultiplier() => Potential / (float)GameConstants.Stats.PotentialMax;
    public float GetScaledGain(float rawGain) => rawGain * GetScaleMultiplier();
}
```

**Run lifecycle** (`StatsCore.ResetForRunStart()`): At run start, all run-lifetime layers (Potential, RunGains, Modifiers, TempModifiers, category in-run addends) are cleared. `Base` values and `MetaStatType` values survive the reset. After reset, `InitializeRunPotentials()` sets the run's per-stat Potential values from the player's total budget.

### 5.3 How Potential Gates Gains (binding)

Only **Layer 2 (RunGains)** is Potential-gated. When an in-run bonus is acquired (relic, weapon affix, room reward, etc.), the raw value is pre-scaled before being added to RunGains:

```
actualGain = rawGain × (statPotential / PotentialMax)
```

- **Meta upgrades (Layer 1 Base values) are NOT potential-scaled.** They are permanent, set outside runs.
- **Layer 3 (Modifiers) and Layer 4 (TempModifiers) are NOT potential-scaled.** They apply at full value.
- Designers tune `rawGain` magnitudes for a readable damage curve across the run: **early-game ~2 digits**, **mid-game low 3 digits**, **late-game ~600**, and **~low 1k+** when everything goes perfectly for the player. Potential stays 0–10 (intuitive).

### 5.4 Category Multipliers (cross-category stacking)

Beyond the four additive stat layers, the stats system supports **category-based multiplicative buffs** that cross categories. These are stored separately and combined into `DamageInfo.AdditionalMultipliers` at attack creation time.

```
totalMultiplier = metaBase (permanent) + Σ inRunAddends (run-lifetime)
```

Multiple categories stack multiplicatively in `DamageCalculator`:

```
finalMultiplier = Π(1 + multiplier_i) for each non-zero category
```

Examples of category keys (string-based, typically from enum `.ToString()`):
- Weapon category: `"Melee"`, `"Ranged"`, `"Shield"`
- Elemental type: `"Physical"`, `"Fire"`, `"Ice"`, `"Lightning"`, `"Wind"`, `"Earth"`
- Weapon rarity: `"Common"`, `"Uncommon"`, `"Rare"`, `"Epic"`, `"Legendary"`

**Lifetime**:
- Category **meta bases** (permanent, set via `SetCategoryMultiplier()`) — profile lifetime, never reset.
- Category **in-run addends** (added via `AddCategoryMultiplier()`) — run lifetime, cleared by `ResetForRunStart()`.

**API**:
```csharp
// Set permanent meta base (survives across runs)
void SetCategoryMultiplier(string category, float value);
// Add in-run addend (cleared each run)
void AddCategoryMultiplier(string category, float value);
// Get total (meta base + in-run addends)
float GetCategoryMultiplier(string category);
// Get all non-zero multipliers for given category keys
float[] GetMultipliersForCategories(params string[] categories);
// Get multipliers for a specific weapon's properties
float[] GetMultipliersForWeapon(WeaponCategory? category, ElementalType? element, WeaponRarity? rarity);
```

### 5.5 Resources: Stamina & Energy (decision: dual-bar, Option B)

Two regenerating resource bars share one actor-owned runtime component:

```csharp
// PlayerResourceController owns the current values.
// Max values are derived from StatsCore.GetStatValue(StatType.Stamina/Energy).
// Regen rates, post-use delay, and movement costs are serialized on the Player prefab.
```

| Bar | StatType | Current consumers | Planned consumers |
|-----|----------|-------------------|-------------------|
| Stamina | `StatType.Stamina` | weapon attacks configured for Stamina, sprint, jump, double-jump | additional physical actions |
| Energy | `StatType.Energy` | weapon attacks configured for Energy | dash, roll, channeling, skills |

**Current Player prefab defaults (binding implementation values):**

| Setting | Default | Semantics |
|---------|---------|-----------|
| Stamina regen | `15 units/s` | Flat regeneration, clamped to current Max Stamina |
| Energy regen | `5 units/s` | Flat regeneration, clamped to current Max Energy |
| Post-use regen delay | `1.0 s` | Tracked independently for Stamina and Energy |
| Sprint cost | `10 Stamina/s` | Consumed continuously while the Sprint state updates |
| Jump cost | `15 Stamina` | Consumed once for both grounded jump and double-jump |

**Regeneration rules:**

- A successful consumption resets only that resource's delay timer. Stamina use does not delay Energy regeneration, and vice versa.
- Regeneration begins after one full second without further consumption. Continuous sprinting repeatedly resets the Stamina timer, so Stamina cannot regenerate while sprinting.
- Failed consumption attempts do not reset a timer. Values are always clamped to `[0, Max]`.
- Rates are flat units per second in the current implementation, not a percentage of Max. All time-based changes use `Time.deltaTime`.

**State-machine integration:**

- `SprintSpeedAction` drains Stamina every `OnUpdate`. `IdleToSprintCondition` and `WalkToSprintCondition` require `CanSprint`; `SprintToWalkCondition` also fires when Stamina reaches zero.
- `GroundedToJumpCondition` and `AirborneToDoubleJumpCondition` require enough Stamina before consuming buffered jump input. `JumpForceAction` then consumes the Jump cost once before applying force.
- Both jump types currently share `_jumpStaminaCost`. If they need different balance later, split this into separate serialized costs without modifying the StateMachine foundation.
- Weapon attacks continue to use `WeaponSO.AttackResourceCost`, so each weapon can independently select Stamina, Energy, or no resource.

- If skills are added later, Energy already has a dedicated role; if skills are never added, it remains available to configured weapons and future traversal actions.
- Alternative considered (single Stamina bar) rejected to preserve the 10-potential model and keep both bars purposeful.

### 5.6 Death, Downs & Respawns (binding)

HP reaching 0 is a **down**, not an immediate run-over. `HealthComponent` clamps HP to 0 and raises `Died`; `PlayerDeathController` owns the penalty, presentation, and respawn.

```text
downLoss = source.PotentialAttack × 2 ^ DownCount
```

- Every enemy carries a `PotentialAttack` value and one `TargetPotential`. Production enemies normally use `PotentialAttack >= 1`; `0` is allowed for testing and produces no penalty.
- There is no missing-source/even-distribution branch. Only a resolved `IPotentialAttackSource` can deduct Potential; an unknown or environmental source deducts 0.
- `DownCount` starts at 0 and is monotonic for the run, producing multipliers `1, 2, 4, 8, 16...`. It increments after every down, including a zero-penalty test down.
- The loss applies only to the configured `TargetPotential`. If the resulting value is above 0, Respawn remains available. If it reaches 0, this is **True Death** and Respawn is disabled.
- On down, `CombatTargetAvailability` marks the player unavailable. Enemies clear the player target and their existing lose-target transition returns them to Patrol. Player input/state processing stops while the death animation plays.
- After the configured player death animation completes (or its fallback duration expires), the GameManager-owned localized Death UI shows the targeted Potential loss, Respawn, and Give Up.
- Respawn replaces the downed player with a clean instance at the active `PlayerSpawnPoint`/checkpoint while preserving Potentials and `DownCount`. Give Up ends the run through `GameFlow -> MetaHub`.
- `RunData.Potentials` and `RunData.DownCount` are serialized by `SaveSystem`, so an interrupted run retains the escalating penalty state.

### 5.6a Player Impact, Knockback & Hitback (implemented)

`PlayerImpactController` subscribes to `HealthComponent.DamageTaken` on the Player prefab and owns
the hit-reaction timers, damage scaling, invincibility, flash, and Rigidbody impulse data. The actual
reaction is represented by three authored player states in the normal transition table: `Knockback`,
`Hitback`, and `Getup`. `PlayerImpactConditionSO` drives entry/exit transitions, while
`PlayerImpactStateActionSO` owns motion during the active state. This keeps the StateMachine enabled
and lets designers add `AnimatorParameterActionSO` (or other state actions) directly to each impact
state. A lethal hit is left to `PlayerDeathController`.

```
knockbackDuration = postMitigationDamage / 100 * (1 - clamp(Tenacity / 100, 0, 0.8))
```

- Player base Tenacity is inspector-authored on `PlayerStatsInitializer` and is capped at 20%. Run/meta
  gains may raise the resolved value, but impact reduction is capped at 80%.
- Ordinary knockback can be hit again. A shorter new duration does not replace the remaining timer; a
  longer duration does. The projected accumulated reaction time is checked against the 0.8-second cap.
- Crossing 0.8 seconds enters Hitback: the player is launched in a Rigidbody arc, becomes invincible,
  bounces from walls/ceilings, and remains invincible after landing.
- Landing enters a one-second Getup stage. The player sprite flashes throughout Hitback and Getup, then
  the completion transition returns to the authored normal states.
- Every accepted hit, including a lethal hit, calls `CameraController.ShakeForDamage`; filtered invincible
  hits never reach `DamageTaken` and therefore do not shake.

`CameraController` keeps a separate logical follow position. Damage shake is applied only as a temporary
visual world offset after bounds/dead-zone solving; the offset is never used as the next frame's follow
input, so camera shake cannot shift the camera's logical anchor or cause follow drift.

### 5.7 Monster Targeted-Potential Attacks

```csharp
public interface IPotentialAttackSource
{
    StatType TargetPotential { get; }
    float PotentialAttack { get; }
}
```

- Every monster carries this data through `EnemySO`/`EnemyStats`. When it downs the player, the player-owned down flow reads it from `DamageInfo.Source` and applies the Ch.5.6 formula to exactly one Potential.
- `PotentialAttack == 0` is an explicit no-penalty test configuration, not an absent component and not a request to distribute loss.
- One-directional: enemies do not target each other or themselves (Ch.11.7). **Enemies do not have Potential** — the Potential system (Ch.5) is player-only. Only the player's potentials erode.
- The enemy only **carries the spec**; the down flow (Ch.5.6) applies the reduction. The enemy itself does not execute it (Ch.11.5).
- Accumulated downs on the same targeted potential can drive it ≤ 0 → true death (Ch.5.6). This is how specific monsters "specialize" in killing builds that lean on one stat.

### 5.8 Stat Change Events

All stat/potential/resource changes emit events via the Core event system (Ch.3): `OnStatChanged`, `OnPotentialChanged`, `OnResourceChanged`, plus the down/death events `OnPlayerDowned` (respawn, Ch.5.6) and `OnPotentialDepleted` (true death, Ch.5.6). Subscribers (UI, combat, audio, meta) react through events, never by polling or hard refs.

### 5.9 Constants (in `GameConstants.Stats`)

- `PotentialMax = 10` — cap for per-stat Potential values
- `PotentialScaleMultiplier = 0.1f` — normalized scale factor (Potential / PotentialMax)
- Damage-related constants (`DamageVarianceStdDev`, `LuckVarianceMeanShift`, `PenetrationMin`, `PenetrationMax`) are in `GameConstants.Damage`
- Combat-related constants (`WeaponPickupRange`, `CurrencyMagnetRadius`, `BackstabDefaultMultiplier`, `CriticalDamageMultiplier`) are in `GameConstants.Combat`
- Runtime-tunable values (`InitialPotentialBudget`, `InitialPotentialAllocatable`, player movement speeds) live in `GameSettingsSO` (tunable via Inspector, not compile-time constants)

### 5.10 Luck Effects (binding)

`Luck` does **not** affect crit (crit is weapon-inherent + meta-sourced only, Ch.9.6). It affects:

- **Damage variance**: each dealt damage fluctuates ±5% around the computed value (normal distribution); `Luck` weights the distribution toward the high end by shifting the mean. Applied during the damage resolution pipeline (`DamageCalculator.ApplyVariance`, Ch.4.12).
- **Drop quality**: higher Luck shifts dropped-weapon rarity rolls upward (Ch.15).
- **Shop quality**: higher Luck shifts shop-refresh weapon rarity rolls upward (Ch.16).

Luck itself is a potential-gated stat: Luck bonuses gained from meta upgrades are potential-scaled per 5.3.

---

## Chapter 6 — State Machine System

> Source of truth: the implemented code under `Assets/Scripts/Core/StateMachine/`. This chapter documents that implementation as the locked foundation. All types live under the `Zephyr.Core.StateMachine` namespace (sub-namespaces: `.ScriptableObjects`, `.Debugging`, `.Editor`).

### 6.1 Architecture

A data-oriented ScriptableObject state machine. A runtime `State` is a container holding an array of `StateAction`s (behaviour) and an array of `StateTransition`s (outgoing edges). A `StateTransition` holds a target state and grouped `StateCondition`s. Everything is authored as SO assets (`StateSO`, `StateActionSO<T>`, `StateConditionSO<T>`, `TransitionTableSO`) and materialized into runtime instances at `Awake`. A single `StateMachine` MonoBehaviour ticks the current state each `Update`.

Consequence: **there is no `IState` interface and no per-state `Enter/Update/Exit` class** — behaviour is composed by attaching `StateAction` SOs to a `StateSO`, and transitions are declared in a `TransitionTableSO`.

### 6.2 Runtime Core (locked)

```csharp
namespace Zephyr.Core.StateMachine
{
    // Internal contract for anything participating in enter/exit lifecycle.
    internal interface IStateComponent
    {
        void OnStateEnter();
        void OnStateExit();
    }

    public sealed class State
    {
        internal StateSO _originSO;
        internal StateMachine _stateMachine;
        internal StateTransition[] _transitions;
        internal StateAction[] _actions;
        public StateTag stateTag;

        public void OnStateEnter();      // forwards to transitions then actions
        public void OnUpdate();          // calls OnUpdate on each action
        public void OnStateExit();
        public bool TryGetTransition(out State state);  // first matching transition wins
    }

    public abstract class StateAction : IStateComponent
    {
        internal StateActionSO _originSO;
        public abstract void OnUpdate();
        public virtual void Awake(StateMachine stateMachine) { }   // cache components here
        public virtual void OnStateEnter() { }
        public virtual void OnStateExit() { }
        public enum SpecificMoment { OnStateEnter, OnStateExit, OnUpdate }
    }

    public abstract class Condition : IStateComponent
    {
        protected abstract bool Statement();        // the test (cached per evaluation sweep)
        internal bool GetStatement();               // cached wrapper
        internal void ClearStatementCache();
        public virtual void Awake(StateMachine stateMachine) { }
        public virtual void OnStateEnter() { }
        public virtual void OnStateExit() { }
    }

    public readonly struct StateCondition
    {
        // (Condition, expectedResult) pair; IsMet = (Statement() == expectedResult)
        public bool IsMet();
    }

    public sealed class StateTransition : IStateComponent
    {
        public bool TryGetTransition(out State state);
        internal void ClearConditionsCache();
    }

    public sealed class StateMachine : MonoBehaviour
    {
        internal State _currentState;
        internal State _previousState;
        // Awake: materialize TransitionTableSO → runtime instances.
        // Start: _currentState.OnStateEnter().
        // Update: if TryGetTransition → Transition(); then _currentState.OnUpdate().
    }
}
```

### 6.3 ScriptableObject Layer (locked)

**Implementation correction:** only runtime `State` objects are deduplicated by `StateSO`. Every action
and condition usage creates an independent runtime instance while its SO remains shared and stateless.
Legacy signatures below that pass one `createdInstances` map into actions/conditions are obsolete.
`MechanicUnlockedConditionSO` remains planned; the meta query framework exists, but no mechanic gate is
authored into the current player transition table.

```csharp
namespace Zephyr.Core.StateMachine.ScriptableObjects
{
    [CreateAssetMenu(menuName = "State Machines/State")]
    public class StateSO : ScriptableObject
    {
        [SerializeField] private StateActionSO[] _actions;
        [SerializeField] public StateTag stateTag;
        internal State GetState(StateMachine sm, Dictionary<ScriptableObject, object> createdInstances);
    }

    public abstract class StateActionSO : DescriptionSMActionBaseSO
    {
        internal StateAction GetAction(StateMachine sm, Dictionary<ScriptableObject, object> createdInstances);
        protected abstract StateAction CreateAction();
    }
    public abstract class StateActionSO<T> : StateActionSO where T : StateAction, new()
    { protected override StateAction CreateAction() => new T(); }

    public abstract class StateConditionSO : ScriptableObject
    {
        internal StateCondition GetCondition(StateMachine sm, bool expectedResult, Dictionary<ScriptableObject, object> createdInstances);
        protected abstract Condition CreateCondition();
    }
    public abstract class StateConditionSO<T> : StateConditionSO where T : Condition, new()
    { protected override Condition CreateCondition() => new T(); }

    // --- Foundation-provided concrete conditions (locked; no upper-layer subclassing needed for these) ---

    /// <summary>Runtime state condition: gates transitions on a meta-persistent mechanic unlock flag.
    /// E.g. Jump→DoubleJump transition checks that DoubleJump is unlocked in MetaData.UnlockedMechanics.</summary>
    [CreateAssetMenu(menuName = "State Machines/Conditions/Mechanic Unlocked")]
    public class MechanicUnlockedConditionSO : StateConditionSO<MechanicUnlockedCondition>
    {
        public MechanicUnlockId MechanicId;
    }

    public class MechanicUnlockedCondition : Condition
    {
        private MechanicUnlockId _mechanicId;
        protected override void OnCreate()
        {
            // reads MetaData.UnlockedMechanics via MechanicUnlockSystem (Ch.22.6)
            // implementation is framework-internal — never reads MetaData directly
        }
        protected override bool Statement() => MechanicUnlockSystem.IsUnlocked(_mechanicId);
    }

    [CreateAssetMenu(menuName = "State Machines/Transition Table")]
    public class TransitionTableSO : ScriptableObject
    {
        [SerializeField] private TransitionItem[] _transitions;
        internal State GetInitialState(StateMachine sm);

        [Serializable] public struct TransitionItem { public StateSO FromState; public StateSO ToState; public ConditionUsage[] Conditions; }
        [Serializable] public struct ConditionUsage { public Result ExpectedResult; public StateConditionSO Condition; public Operator Operator; }
        public enum Result { True, False }
        public enum Operator { And, Or }
    }
}
```

Upper layers add behaviour by:
- Subclassing `StateActionSO<T>` (override `OnUpdate` / `OnStateEnter` / `OnStateExit` / `Awake`).
- Subclassing `StateConditionSO<T>` (override `Statement()`).
- Assembling `StateSO` assets (attach action SOs, set `stateTag`).
- Wiring a `TransitionTableSO` (From → To + conditions + And/Or operators).

### 6.4 Transition Evaluation (priority = ordering)

- `State.TryGetTransition` iterates `_transitions` in **array order** and returns the first whose conditions are met. **There is no priority enum** — priority is expressed by ordering transitions in the `TransitionTableSO`.
- Condition grouping: consecutive conditions joined by `Operator.And` form one AND-group; `Operator.Or` separates groups. The transition fires if **any** AND-group is fully satisfied (OR across groups, AND within).
- Conditions are cached per sweep: `Statement()` runs once per `TryGetTransition`; caches are cleared after the sweep.
- **Place critical transitions first** in each From-state's list: Death (condition: any potential ≤ 0), Hurt (took damage), high-priority cancels (Dash cancels Attack). There is no forced-transition mechanism; Death is just a condition-led transition ordered first.

### 6.5 Lifecycle

1. `StateMachine.Awake` → `TransitionTableSO.GetInitialState(this)` materializes the full graph (states, actions, conditions, transitions) into runtime instances, deduplicated via a `createdInstances` map. It is called **once**; the result is assigned to both `_currentState` and `_previousState`. Initial state = the `_initialState` field if set, otherwise the first `FromState` in the table.
2. `StateMachine.Start` → `_currentState.OnStateEnter()`.
3. `StateMachine.Update` → if `TryGetTransition` hits, `Transition()` (exit old, set `_previousState`, enter new); then `_currentState.OnUpdate()`.
4. On a transition frame the old state's `OnUpdate` is skipped; the new state's `OnUpdate` runs right after its `OnStateEnter`.

### 6.6 StateTag (identification enum)

`StateTag` tags states for lookup/comparison (NOT priority). It lives **inside** the `Zephyr.Core.StateMachine` namespace and uses Zephyr's state set:

```csharp
public enum StateTag
{
    Idle, Walk, Run, Jump, DoubleJump, Dash, AirDash, Roll,
    Climb, Crouch, Channeling, Attack, Hurt, Death
}
```

### 6.7 Owner Binding

- The `StateMachine` MonoBehaviour lives on the actor GameObject (e.g., Player). It is the "owner".
- **Component lookup uses `GetComponentInChildren<T>(true)`** — the StateMachine's cache searches root and all children (depth-first, including inactive GameObjects). This allows actor-logic components to live on a child "Core" GameObject while the StateMachine and shared-core components (`StatsCore`) remain on the root. `GetOrAddComponent<T>()` still adds to root if no match is found.
- `StateAction.Awake(StateMachine)` / `Condition.Awake(StateMachine)` are called once at materialization; cache actor components there via `stateMachine.GetOrAddComponent<T>()` / `stateMachine.GetCachedComponent<T>()` (component-cached on the StateMachine).
- Actions/conditions call **only** the actor's public APIs (MovementCore, StatsCore, HurtReceiver, etc.) — never private fields.
- There is no `StateContext` struct; per-actor runtime data lives on the actor's components, not on the stateless SOs.

**Actor Hierarchy Pattern:**

```
Actor Root:    StateMachine + StatsCore + Rigidbody2D + Colliders
Actor Core:    MovementCore, HurtReceiver, PlayerResourceController,
               PlayerInputReader, PlayerInteractor
Actor Sprite:  SpriteRenderer + Animator
Actor Socket:  Empty Transform for runtime weapon attachment
```

### 6.8 Physics / FixedUpdate

The StateMachine ticks only in `Update`. Rigidbody physics is owned by `MovementCore` (constitution §IV), which runs its own `FixedUpdate`. `StateAction`s that affect motion must call `MovementCore.Move(dir)` / `Jump()` (buffered, applied in FixedUpdate) — they must **not** write `Rigidbody.velocity` directly. This keeps physics deterministic and decoupled from the Update-tick state machine.

### 6.9 Red Lines (binding)

1. The state machine foundation (`Core/StateMachine/` runtime + SO + Editor + `InitOnlyAttribute`) is **locked — no modifications are permitted**. A one-time exception was applied to `TryGetCachedComponent<T>()` (now uses `GetComponentInChildren<T>(true)` to support the actor child-hierarchy pattern). This change is **already merged**; future modifications to the StateMachine foundation remain forbidden.
2. New behaviour = new `StateActionSO<T>` / `StateConditionSO<T>` subclasses + new `StateSO` / `TransitionTableSO` assets. No foundation edits — the one-time cache-lookup exception is already applied.
3. Actions/conditions must not mutate actor private fields; only public APIs.
4. Actions/conditions must not request transitions directly (no `machine.GoTo()`). Transitions are declared in `TransitionTableSO` and evaluated by the machine.
5. `StateAction`/`Condition` instances are per-actor runtime objects (materialized per StateMachine); the `...SO` assets are shared stateless configs — do not store per-actor mutable state on SO assets.
6. A component type must appear at most once in the actor hierarchy. `GetComponentInChildren` returns the first depth-first match; duplicates cause silent, non-deterministic cache results.

---

## Chapter 7 — Buff / Modifier System

### 7.1 Buff Categories

| Category | Examples | Carried via |
|----------|----------|-------------|
| Stat modifier | +attack, +% crit, mult damage | `BuffData.Modifiers` |
| Status effect (元素异常) | Burn (DoT), Freeze (control), Shock (interrupt/-resist) | `BuffData.StatusEffect` + `StatusIntensity` |
| Control | stun, slow, root | `BuffData.Tag` + params |
| Special | proc-on-hit, shield, aura | `BuffData.Tag` + custom payload (upper layer) |

> Elemental异常 exist; **elemental reactions do not** (confirmed). Elemental异常 do **not** go through the Potential system (potential governs character-body stats only).

### 7.2 Stacking Policy (binding)

```csharp
public enum StackPolicy { Refresh, Stack, Ignore }
```

- **Refresh**: new application resets duration (stacks stay at current count or 1).
- **Stack**: increment count up to `MaxStacks`; refresh duration.
- **Ignore**: if already active, new application discarded.

Policy is per-buff-config (BuffConfigSO), not per-application.

### 7.3 Application Order (binding, relative to stat calc)

1. Aggregate all active buffs' stat modifications.
2. Potential-gated buffs (run-lifetime) are pre-scaled by `(targetStatPotential / PotentialMax)` (Ch.5.3).
3. Apply to the appropriate stat layer (Ch.5.2):
   - Permanent buffs → `SetBase()` (Layer 1)
   - Potential-gated in-run buffs → `ApplyPotentialScaledGain()` (Layer 2)
   - Non-potential in-run buffs → `AddModifier()` (Layer 3)
   - Temporary buffs → `AddTemporaryModifier()` (Layer 4)
4. Category-based multiplicative buffs → `AddCategoryMultiplier()` (Ch.5.4)
5. Category multipliers stack multiplicatively in `DamageCalculator` at attack creation time.

### 7.4 Duration Types

- **Timed**: `Duration > 0`; ticks down via Timer service; auto-removed at 0.
- **Permanent**: `Duration ≤ 0`; removed only by explicit `RemoveBuff` / `ClearBuffsByTag`.
- **Conditional**: timed base + a removal predicate (e.g., "remove on HP full"); upper-layer evaluates.

### 7.5 Conflict Resolution

- Same `BuffTag` + same source → governed by `StackPolicy`.
- Same `BuffTag` + different source → both coexist (separate instances), each subject to its own `MaxStacks`.
- Mutually-exclusive tags (e.g., `Frozen` vs `Burning`) defined in a conflict table in GameSettings SO; later application either dispels the earlier or is rejected per config.

### 7.6 Elemental异常 Detail

| 异常 | Effect | Damage type |
|------|--------|-------------|
| Burn | DoT over duration | weapon's damage type (adaptive), benefits from weapon + char stats (Ch.4.9) |
| Frost | slow / move + attack speed | non-damage (control) |
| Shock | interrupt + temporary resist reduction | non-damage (debuff); the triggering hit itself is typed normally |

- No reactions: Burn + Frost does not combine into anything; they simply coexist or conflict per the conflict table.
- 异常 intensity scales with `StatusIntensity` and the source weapon's stats.

### 7.7 Removal Conditions

- Duration expiry (timed).
- Explicit dispel (skill / item / buff).
- Conflict-table override.
- Owner death → `ClearAllBuffs()`.

### 7.8 Interface (from Ch.2.3, restated for convenience)

```csharp
BuffInstanceId AddBuff(in BuffData data);
bool RemoveBuff(BuffInstanceId id);
void ClearAllBuffs();
void ClearBuffsByTag(BuffTag tag);
bool HasBuff(BuffTag tag);
```

### 7.9 Constants (in `GameConstants.Buff`)

- `MaxConcurrentBuffs` (soft cap for UI/perf; excess evicts lowest-priority).
- Default `MaxStacks` per category fallback.
- Conflict table lives in GameSettings SO (data, not code).

---

## Chapter 8 — Object Pool System

> Layer 🟥: locked foundation. Namespace `Zephyr.Core.ObjectPool`. Provides non-generic object pooling for frequently-spawned Unity objects. The foundation is `ObjectPool` (MonoBehaviour), `PoolRegistry` (MonoBehaviour singleton), `PooledObject` (component), and `IPooledObject` (interface).

### 8.1 Foundation (locked)

```csharp
namespace Zephyr.Core.ObjectPool
{
    public class ObjectPool : MonoBehaviour
    {
        [SerializeField] private PooledObject _prefab;
        [SerializeField] private int _initialSize = 10;
        [SerializeField] private bool _canGrow = true;

        private readonly Queue<PooledObject> _available = new Queue<PooledObject>();
        private readonly HashSet<PooledObject> _active = new HashSet<PooledObject>();

        public PooledObject Spawn(Vector3 position, Quaternion rotation);
        public void Despawn(PooledObject obj);
        public int ActiveCount { get; }
        public int AvailableCount { get; }
    }

    public sealed class PoolRegistry : MonoBehaviour
    {
        public static PoolRegistry Instance { get; }

        public void RegisterPool(string id, ObjectPool pool);
        public ObjectPool GetPool(string id);
        public PooledObject Spawn(string poolId, Vector3 position, Quaternion rotation);
        public void Despawn(string poolId, PooledObject obj);
    }

    public class PooledObject : MonoBehaviour, IPooledObject
    {
        public void Initialize(ObjectPool pool);
        public void OnSpawn();      // called when taken from pool — sets active true
        public void OnDespawn();   // called when returned to pool — sets active false
        public void ReturnToPool(); // convenience: object returns itself to its pool
    }
}
```

```csharp
namespace Zephyr.Core.Interfaces
{
    public interface IPooledObject
    {
        void OnSpawn();
        void OnDespawn();
        void ReturnToPool();
    }
}
```

### 8.2 Architecture Overview

The pool system uses a **non-generic** design where:
- **`ObjectPool`** (per-prefab MonoBehaviour): One `ObjectPool` instance exists per pooled prefab type. It manages a `Queue<PooledObject>` of available objects and a `HashSet<PooledObject>` of active objects. On `Awake`, it pre-initializes `_initialSize` objects. When exhausted, it can optionally grow (`_canGrow = true`) or return null.
- **`PoolRegistry`** (singleton MonoBehaviour in `Persistent` scene): Central registry mapping string IDs to `ObjectPool` instances. Gameplay code references pools by ID string without holding direct `ObjectPool` references. Lives in the `Persistent` scene (Ch.14.2) — never unloaded.
- **`PooledObject`** (component on every pooled prefab): Implements `IPooledObject`. Handles the spawn/despawn lifecycle by toggling `gameObject.SetActive(true/false)`. Stores a back-reference to its owning `ObjectPool` for `ReturnToPool()`.

### 8.3 What Must Be Pooled

All frequently-spawned objects go through pools:
- Bullets / projectiles
- Drop items (weapons, currency)
- VFX (hit sparks, death bursts)
- Floating combat text
- Audio sources (on-hit SFX trigger very frequently — pooled to avoid per-hit `Instantiate`)
- Temporary enemy-spawned objects

### 8.4 Growth & Capacity Policy

- **Prewarm**: `ObjectPool.Awake()` pre-initializes `_initialSize` objects (configurable per-prefab, default 10).
- **Growth**: When the available queue is empty and `_canGrow = true`, a new object is `Instantiate`d.
- **Exhaustion**: If the queue is empty and `_canGrow = false`, `Spawn()` logs a warning and returns `null`.
- Objects are parented to the pool's transform for organizational cleanliness.

### 8.5 Pooled Object Rules (binding)

1. Every pooled prefab **must** have a `PooledObject` component attached. This component implements `IPooledObject`.
2. `OnSpawn()` is called when the object is taken from the pool — it sets `gameObject.SetActive(true)`. Reset all runtime state (position, velocity, particle effects, etc.) in code after calling the base `OnSpawn()`.
3. `OnDespawn()` is called when the object is returned to the pool — it sets `gameObject.SetActive(false)`. Clear all runtime state (stop particle effects, disable colliders, etc.) before calling the base `OnDespawn()`.
4. **No `OnDestroy` side-effects** on pooled objects (OnDestroy fires on application quit / scene unload and is unreliable). All teardown goes in `OnDespawn()`.
5. Pooled objects subscribe to events in `OnSpawn()` (or during `Initialize()`), unsubscribe in `OnDespawn()` (Ch.3.3 rule 1).
6. **Auto-return**: projectiles/VFX should call `ReturnToPool()` on lifetime expiry (via Timer service, Ch.20) or on hit. They must never rely on someone else calling `Despawn()`.

### 8.6 Registration Flow for New Pools (upper layer)

New content (e.g., a new bullet type) needs a new pool. The flow keeps the foundation immutable:

1. Create an `ObjectPool` component on a GameObject (typically in the `GameManager` scene during gameplay initialization).
2. Call `PoolRegistry.Instance.RegisterPool(id, pool)` during scene initialization.
3. Gameplay code retrieves via `PoolRegistry.Instance.Spawn(id, pos, rot)` or `PoolRegistry.Instance.GetPool(id).Spawn(pos, rot)`.
4. On scene unload, pools are automatically destroyed with the scene; no explicit deregistration needed.

> This satisfies "AI may only use existing pools; new pool logic is forbidden" — AI registers *instances* and *uses* them; the `ObjectPool`/`PoolRegistry`/`PooledObject` foundation is never modified.

### 8.7 Red Lines

- `ObjectPool`, `PoolRegistry`, `PooledObject`, and `IPooledObject` are locked; AI may not modify.
- New pool *instances* are allowed; new pool *types/foundation* are not.
- Direct `Instantiate`/`Destroy` for pooled categories is forbidden (enforced via code review + a debug assert that flags un-pooled spawns of known pooled prefabs).
- Pooled prefabs must have the `PooledObject` component attached at all times.

---

## Chapter 9 — Weapon System

> **Implemented snapshot:** `WeaponSO` is a template asset and `WeaponInstance` only wraps that
> template. Components are polymorphic `[SerializeReference] ComponentData` records: hit box, attack
> direction, facing, movement, and air attack. `WeaponGenerator` adds/removes required runtime
> components; only a weapon with the air-attack component can attack airborne. Combo steps author
> animation, damage multiplier, duration, and reset delay. Primary/secondary input directly selects
> either slot. Affix generation and rarity rolling are planned. Ranged fields exist, but launch wiring
> is not integrated yet.

> Layer 🟧: the foundation defines the weapon framework (data model, affix socket, combo structure, dual-slot holder, DamageInfo builder). Upper layer authors specific `WeaponSO` assets, affix definitions, and combo data. Namespace: `Zephyr.Core.Weapons`.

### 9.1 Data Model

Weapons are ScriptableObject templates (`WeaponSO`) materialized into runtime instances (`WeaponInstance`) when picked up. Affixes are rolled at drop time and carried on the instance, so the same `WeaponSO` can drop with different affix sets.

```csharp
namespace Zephyr.Core.Weapons
{
    public enum WeaponRarity { Common, Uncommon, Rare, Epic, Legendary }
    public enum WeaponSlot { Primary, Secondary }

    [CreateAssetMenu(menuName = "Zephyr/Weapons/Weapon")]
    public class WeaponSO : ScriptableObject
    {
        [TextArea] public string description;          // conventional display field
        public ItemId Id;
        public WeaponRarity BaseRarity;
        public DamageType DamageType;                  // phys / mag / true — selects Attack vs MagicAttack base
        public DamageType PercentType;

        [Header("Intrinsic (base, NOT potential-scaled)")]
        public float BaseFlatDamage;                   // added to Attack/MagicAttack Base when equipped
        public float BasePercentDamage;                // 0..1 of target MaxHp
        public float ShieldCoeff;                      // P, weapon-inherent (Ch.4.7)
        public ElementTag Element;
        public float BaseCritChance;                   // 0..1
        public float BaseCritMultiplier;               // e.g. 2.0
        public float BaseBackstabMultiplier;           // 1.0 = none; >1.0 = backstab bonus (Ch.4.5b/11.3)
        public float BaseAttackSpeed;                  // feeds AttackSpeed stat base

        [Header("Penetration (weapon-inherent; player pen is meta-only — Ch.4.3/9.2)")]
        public float BaseFlatPen;
        public float BasePercentPen;                   // 0..1

        [Header("Combo")]
        public ComboData Combo;

        [Header("Cost")]
        public float AttackStaminaCost;
        public float AttackEnergyCost;                 // when no skills: attacks cost energy (Ch.5.5)

        [Header("Ranged (null = melee)")]
        public ProjectileSpawnSpec Projectile;         // null for melee weapons
    }

    public struct WeaponInstance                       // runtime, serializable for save (Ch.12)
    {
        public ItemId Id;                              // resolves back to WeaponSO
        public WeaponRarity RolledRarity;
        public WeaponAffix[] RolledAffixes;            // rolled at drop
        // resolved values (base + affixes) computed on equip via WeaponResolver
    }
}
```

### 9.2 Affix (词条) Model

Affixes are the weapon's per-instance modifiers, rolled at drop based on rarity. They are one source of penetration (weapon-inherent base + affixes). **Penetration also exists on the player** — base 0, only modifiable by meta upgrades (`CombatAcquiredUpgrades`, Ch.12.3), NOT potential-scaled, and not one of the 10 core `StatType`s. `DamageCalculator` sums weapon pen + player pen (Ch.4.3).

```csharp
public enum AffixKind
{
    FlatPen, PercentPen,                       // applied to the defense matching the weapon's DamageType
    FlatDamageBonus, PercentDamageBonus,
    CritChanceBonus, CritMultiplierBonus,
    ShieldCoeffBonus, AttackSpeedBonus,
    ElementIntensity                            // scales elemental异常 strength (Ch.7.6)
}

public struct WeaponAffix
{
    public AffixKind Kind;
    public float Value;
    public ElementTag Element;                  // relevant for ElementIntensity
}
```

`WeaponResolver` aggregates `WeaponSO` base values + `RolledAffixes` into a `ResolvedWeaponStats` snapshot at equip time; the snapshot is what the DamageInfo builder reads at hit time. Affix values are **not** potential-scaled — they are weapon-intrinsic, like the base values. Only character-body acquired modifiers go through potential (Ch.5.3).

### 9.3 Combo System

Combos are a per-weapon authored sequence. The StateMachine (Ch.6) drives a single `Attack` state; a `ComboControllerAction` (a `StateAction`) tracks the current step and advances on input within the cancel window. Each weapon's `ComboData` defines its own step count, timings, and per-step damage multiplier.

```csharp
[Serializable]
public struct ComboStep
{
    public string AnimTrigger;
    public float Windup, Active, Recovery;
    public float CancelWindowStart, CancelWindowEnd;   // input accepted here → advance to next step
    public float DamageMultiplier;                     // scales weapon flat for this step
    public bool IsFinisher;                            // last step; returns combo to 0 after
}

[Serializable]
public struct ComboData
{
    public ComboStep[] Steps;        // length ≥ 1
    public float IdleResetTime;      // no input for this long → combo resets to step 0
}
```

- Input arriving inside `[CancelWindowStart, CancelWindowEnd]` queues the next step; on `Recovery` end the controller advances the step index.
- No input before `IdleResetTime` resets the combo to step 0.
- Changing the requested attack slot does not merge combo sequences. The newly requested weapon follows its own combo state and starts at step 0 unless its own reset rules say otherwise.
- Combo step index is runtime state on the actor, **not** on the SO.

### 9.4 Dual-Weapon Slots

The player holds exactly two weapons (`Primary`, `Secondary`) and can request either one directly at any time. `PrimaryAttack` routes to Primary and `SecondaryAttack` routes to Secondary; there is no combat stance that must be switched before attacking. Both attack transitions are authored separately in the StateMachine and each condition checks the weapon in its own slot.

The design may retain one slot marker for loadout feedback. Its semantic name is **Replacement Target**: it tells the player which slot the next weapon pickup will overwrite. It does not determine attack routing, does not make the other weapon inactive, and must not be presented as the only currently usable weapon. The existing runtime field may still be named `ActiveSlot` for compatibility, but documentation and UI must treat it as this replacement target.

```csharp
public struct WeaponSet   // on the actor
{
    public WeaponInstance? Primary;
    public WeaponInstance? Secondary;
    public WeaponSlot ReplacementTarget;
    public WeaponInstance GetWeapon(WeaponSlot slot);
}
```

Picking up a weapon **directly replaces the selected Replacement Target slot** — there is **no deep inventory** and no third selection layer. The player selects the slot to be overwritten before interacting with the pickup; the replaced weapon is discarded. The replacement marker is a loadout decision, not a combat weapon selector.

### 9.5 Rarity & Acquisition

- Rarity gates affix count and roll magnitude. `WeaponSO.BaseRarity` is the floor; drops may roll ≥ floor based on the drop source (shop/boss drop higher).
- Acquisition sources (all in-run): enemy drops, shop purchase, boss-room rewards (Ch.13/15).
- A weapon codex (unlock records) lives in meta data (Ch.12) and grants a small permanent bonus when a weapon type is first unlocked (Q14).

### 9.6 Weapon → DamageInfo Build (binding)

At hit time, `WeaponDamageBuilder` produces a `DamageInfo` from the weapon requested by the current attack transition (`Primary` or `Secondary`) + that weapon's current combo step:

```
flatBase    = GetFinalStat(Attack or MagicAttack)   // char+weapon intrinsic + potential-scaled mods (Ch.5.4)
flatRaw     = flatBase × comboStep.DamageMultiplier
isCrit      = Roll(resolvedCritChance)               // crit chance = weapon base + meta bonus (Ch.12.3), no luck
flatRaw    ×= isCrit ? resolvedCritMultiplier : 1    // crit applied to flat portion pre-mitigation
percentRaw  = resolvedPercentDamage                  // 0..1, typed via PercentType
→ DamageInfo { FlatDamage=flatRaw, FlatType=DamageType, PercentOfMaxHp=percentRaw,
               PercentType, ShieldCoeff=resolvedShieldCoeff, Element, IsCrit, CritMultiplier,
               Attacker=actor, SourceWeaponId=weapon.Id, IsDoT=false }
// Note: damage variance (±5% normal distribution, Luck-weighted) is applied during
// DamageCalculator resolution, not at weapon-build time — see Ch.4.12.
```

- **Pen is NOT placed in `DamageInfo`.** `DamageCalculator` sums pen from two sources: weapon pen (inherent, via `SourceWeaponId`) + player pen (meta-acquired, NOT potential-scaled, Ch.12.3/5.3). `DamageInfo` stays a pure payload (Ch.4.3).
- Weapon-intrinsic values (flat, percent, shield-coeff, crit, pen) are **not** potential-scaled; only character-body acquired modifiers (already folded into `GetFinalStat`) are (Ch.5.3).

### 9.7 Weapon → StateMachine, Resources & Ranged

- The Primary and Secondary attack states/transitions and their actions are part of the player's `TransitionTableSO`. Each action reads the requested slot's `WeaponSO.Combo` to drive step timings — there is no prerequisite ActiveWeapon switch and the StateMachine foundation is unchanged.
- Each attack step consumes `AttackStaminaCost` and/or `AttackEnergyCost` (Ch.5.5). If the resource is insufficient, the attack is cancelled (transition to Idle) — enforced by a `StateCondition` checking the resource bar.
- Ranged weapons spawn a projectile via the pool (Ch.8/10) instead of applying damage directly. The weapon's `ProjectileSpawnSpec` is translated to a `ProjectileLaunchData` (Ch.2.7) at spawn time:

```csharp
[Serializable]
public struct ProjectileSpawnSpec
{
    public ItemId ProjectileId;       // resolves to the pooled prefab (Ch.10.5)
    public Vector2 MuzzleOffset;      // local-space, added to actor position
    public float Speed;
    public bool AffectedByGravity;
    public float GravityScale;
    public float Lifetime;
    public int PierceCount;
    public DamageType ProjectileDamageType;   // usually equals the weapon's DamageType
}
```

The `DamageOnHit` carried by the spawned projectile is built from the same `WeaponDamageBuilder` snapshot so ranged and melee share the damage pipeline (Ch.4).

### 9.8 Interface Conformance

- A weapon pickup is a pooled `Pickup : IPickup` (Ch.15.2); `OnPickedUp` directly replaces the selected Replacement Target slot — no deep inventory storage (Ch.9.4).
- The weapon is a damage source: `DamageInfo.SourceWeaponId` carries `WeaponInstance.Id` for kill credit (Ch.4.10) and DoT-type inheritance (Ch.4.9).

### 9.9 Constants (in `GameConstants.Weapons`)

- `ComboIdleResetDefault`
- Per-rarity affix-count table (in GameSettings SO, not hardcoded)
- `MinCritChance = 0f`, `MaxCritChance = 1f`

### 9.10 Red Lines

1. `WeaponSO`, `WeaponInstance`, `WeaponAffix`, `ComboData`, `WeaponSet`, `WeaponDamageBuilder` are the locked framework; AI may add `WeaponSO` **assets** and affix definitions, not new framework types.
2. Pen is **not** one of the 10 core `StatType`s, but it **does exist on the player** — base 0, only modifiable by meta upgrades (`CombatAcquiredUpgrades`, Ch.12.3), NOT potential-scaled. Weapon pen is weapon-inherent (`WeaponSO` / affixes, also not potential-scaled). Both are summed by `DamageCalculator` (Ch.4.3).
3. `DamageInfo` must not carry pen fields; `DamageCalculator` resolves pen via `SourceWeaponId`.
4. Weapon-intrinsic values are never potential-scaled; only character-body acquired modifiers are (Ch.5.3).
5. Combo step index is runtime actor state, never stored on the SO.

---

## Chapter 10 — Projectile System

> Layer 🟥: locked foundation. Namespace `Zephyr.Core.Projectiles`. Projectiles are pooled (Ch.8) via `PooledObject` component which implements `IPooledObject`. The `Projectile` MonoBehaviour handles gameplay logic; pool lifecycle is managed by `PooledObject` on the same GameObject.

### 10.1 Foundation Types

```csharp
namespace Zephyr.Core.Projectiles
{
    public sealed class Projectile : MonoBehaviour
    {
        // Gameplay logic
        public void Launch(in ProjectileLaunchData data);
        public void OnHit(IDamageable target, in DamageInfo info);

        // Reset runtime state (called after PooledObject.OnSpawn())
        public void ResetState();   // reset velocity, pierce counter, lifetime
    }
    // Note: PooledObject component (Ch.8.1) handles IPooledObject (OnSpawn/OnDespawn/ReturnToPool).
    // The Projectile component calls pooledObject.ReturnToPool() for auto-return.
}
```

`ProjectileLaunchData` is defined in Ch.2.7 (Origin, Velocity, AffectedByGravity, GravityScale, Lifetime, PierceCount, DamageOnHit).

### 10.2 Launch & Physics

- On `Launch`, the projectile is positioned at `Origin`, given `Velocity`, and its Rigidbody2D is configured: `gravityScale = AffectedByGravity ? GravityScale : 0`.
- Motion is Rigidbody-based (Rigidbody2D velocity), **not** transform-translate — physics stays in `FixedUpdate`, consistent with the constitution (§IV, Ch.6.8). The projectile sets velocity once and lets the physics engine integrate.
- The projectile never writes another body's velocity; collisions are detected via trigger colliders + `OnTriggerEnter2D`.

### 10.3 Hit Resolution

- On trigger enter with an `IDamageable` (resolved via the interaction layer, not direct cross-module `GetComponent` — Ch.2.8), call `OnHit(target, DamageOnHit)`.
- `PierceCount` = number of **additional** targets the projectile passes through after the first (0 = stops on first hit). A pierce counter decrements per hit; at `< 0` the projectile recycles.
- Friendly fire is off by default; team/faction filtering is an upper-layer check on the target before `OnHit`.
- Default: identical `DamageOnHit` (snapshot at launch) is applied to each pierced target. Falloff / per-hit modification is an upper-layer extension.

### 10.4 Lifetime & Auto-Return

- On `Launch`, the projectile schedules self-return at `Lifetime` via the Timer service (Ch.20). It must never rely on an external caller to return it to the pool.
- Auto-return also fires on: pierce exhausted, world-geometry collision (configurable: bounce vs. destroy), and scene unload (via `PooledObject.OnDespawn()`, never `OnDestroy` — Ch.8.5).
- On return, the projectile unsubscribes every event it subscribed to in `OnSpawn` (Ch.3.3 / Ch.8.5).

### 10.5 Pool Integration

- Projectiles use `ObjectPool` (Ch.8.1). Each projectile prefab has both a `Projectile` component and a `PooledObject` component.
- Pool registration: `PoolRegistry.Instance.RegisterPool(id, objectPool)` during scene initialization (Ch.8.6).
- Spawning: `PoolRegistry.Instance.Spawn(id, position, rotation)` returns a `PooledObject`. The gameplay code gets the `Projectile` component via `GetComponent<Projectile>()`, calls `ResetState()`, then `Launch(data)`.
- Prewarm: `ObjectPool.Awake()` pre-initializes `_initialSize` objects (default 10) per pool.
- Direct `Instantiate` of a projectile prefab is forbidden (Ch.8.7); enforced by the pool debug assert.

### 10.6 Red Lines

1. `Projectile` and its integration with `PooledObject` are locked; AI may not modify.
2. Projectiles must be pooled; direct `Instantiate`/`Destroy` of projectile prefabs is forbidden.
3. Projectiles must self-return (Timer lifetime or hit) — no external dependency.
4. `OnDestroy` side-effects are forbidden on `Projectile`; all teardown in `OnDespawn` (Ch.8.5).
5. Projectile motion is Rigidbody-only; no transform-translate motion.

---

## Chapter 11 — Enemy AI System

> Layer 🟧: the foundation reuses the Ch.6 StateMachine (no separate "AI pool"); upper layer authors enemy `StateAction`/`StateCondition` subclasses and `TransitionTableSO` assets. Enemy behaviour code lives under `Zephyr.Gameplay.Enemies` (upper layer), reusing the locked `Zephyr.Core.StateMachine`.

### 11.1 Architecture

**Implemented basic enemy baseline:** `Assets/Prefabs/Enemies/BasicEnemy.prefab` uses the same
`StateMachine` component and `TransitionTableSO` runtime as the player. Its authored graph is
`Patrol -> Alert -> Chase -> Telegraph -> Attack`, with `Hurt` and `Death` transitions ordered before
normal behavior. `EnemySO` stores identity/tags, all panel stats, movement/physics tuning, perception
ranges, alert/wind-up/cooldown timings, attack data, and drop reference. `EnemyStats` applies the
base-only values to `StatsCore` and returns full Potential for enemy-side buff scaling. `EnemyBrain`
tracks the tagged player, aggro/lose-aggro range, attack timing, damage interruption, and death;
`EnemyMotor` owns Rigidbody2D movement and facing. The prefab has HealthComponent, StatsCore,
Rigidbody2D, BoxCollider2D, and a replaceable SpriteRenderer placeholder. Enemy behavior scripts
live under `Assets/Scripts/Gameplay/Enemies`; the enemy-specific state graph is under
`Assets/ScriptableObjects/Enemies/BasicEnemy`.

**Implemented built-in attack component layer:** regular enemies do not own custom weapons and do
not use `WeaponSO`, `WeaponRuntime`, or the player weapon generator. Their attacks are authored
directly on `EnemySO` as `EnemyAttackDefinition` entries. Optional attack behaviours are composed
from polymorphic `EnemyComponentData` records on the same `EnemySO`; `EnemyComponentGenerator`
materializes the required `EnemyComponent` types once when the enemy instance initializes.
`EnemyAttackController` is the single runtime attack coordinator: the StateMachine attack action
only requests start/cancel, while the controller owns total duration, cooldown, active-window
events, damage payload creation, and attack completion. The current `BasicEnemy` uses one
`BasicMelee` definition and `EnemyMeleeHitboxData`/`EnemyMeleeHitboxComponent`; its hitbox geometry
is data-authored and queried with `Physics2D.OverlapBox`, so no second physical trigger collider is
required on the prefab. Advanced projectile/lunge/status components are not part of this baseline.

Every enemy is an actor driven by the **same** StateMachine foundation as the player (Ch.6). An enemy has its own `TransitionTableSO` asset; its behaviour is composed of `StateActionSO<T>` / `StateConditionSO<T>` subclasses authored for enemies (e.g. `ChaseAction`, `AttackAction`, `PlayerInRangeCondition`). There is no separate AI framework and no "AI pool" — the StateMachine is the only behaviour engine.

```
Enemy actor GameObject:
  - StateMachine (Ch.6) + enemy TransitionTableSO
  - IDamageable implementation (Ch.2.2)
  - IStatSource  implementation (Ch.2.6)
  - IBuffable    implementation (Ch.2.3)
```

### 11.2 Enemy Data Model

```csharp
public class EnemySO : ScriptableObject          // upper layer
{
    public ItemId Id;
    public string[] Tags;                         // 1+ classification tags (e.g. "Melee", "Ranged", "Heavy", "Flier");
                                                  // spawn points resolve which enemies can spawn here via tag matching (Ch.13.2)
    public TransitionTableSO BehaviourTable;      // the enemy's state graph (Ch.6)
    public float[] BaseStats;                     // base-only stat values (enemies have NO Potential, Ch.5.7)
    public PotentialAttack PotentialAttack;       // Ch.5.7 — which player potential this enemy's down-cost hits
    public DropTable Drops;
    public EnemySetId SetId;                      // stage-locked enemy set this enemy belongs to (Ch.13.6)
    public float AggroRange;                      // engagement range when facing the player (11.3)
    public float DetectionRange;                  // sensing range when NOT facing the player (≥ AggroRange, 11.3)
    public float AlertDuration;                   // ~0.5s alert window before turning + engaging (11.3)
    public float PatrolSpeed;                     // patrol-state move speed (11.3)
    public float AttackRange;
    public float LoseAggroMultiplier;             // de-aggro threshold = AggroRange × this (11.3)
    public List<EnemyAttackDefinition> Attacks;    // built-in attacks; no WeaponSO for regular enemies
    [SerializeReference]
    public List<EnemyComponentData> Components;    // optional per-attack behaviours/effects
}
```

`EnemyAttackDefinition` currently owns attack identity, animation state, range, cooldown, telegraph
duration, total duration, active-window start/duration, and damage multiplier. Damage type/category
remain enemy-level fields and base damage comes from the enemy's Attack stat. Component data is
synchronized to the attack count by `EnemySO.OnValidate` and the custom `EnemySOEditor`; duplicate
component types are prevented and component records support Undo-safe add/remove operations.

`EnemyMeleeHitboxComponent` keeps its editable local offset, size, target layer mask, and gizmo
preview settings on the generated component itself. The `EnemyMeleeHitboxData` record only selects
the behavior. In edit mode the component draws a cyan wire box using the configured preview facing;
during an active attack it changes to red.

`EnemySO` is a static config; runtime instances are materialized by a spawner (upper layer). **Enemies do NOT have Potential** — they use base-only stats; the Potential system (Ch.5) is player-only. Enemies still implement `IStatSource` (Ch.2.6), but `GetPotential()` returns `PotentialMax` (10 → coefficient 1.0), so enemy in-run buffs (e.g., curse/tradeoff modifiers like "player +10 attack but enemies +10% move speed") apply at **full value** with no potential-gating. The targeted-potential mechanic is one-directional (5.7).

### 11.3 Perception, Patrol & Detection (facing-aware)

Enemies patrol authored **patrol sections** on the map and use a **facing-aware** detection model. Facing also drives the backstab mechanic (Ch.4.5b).

**Patrol** — each enemy is assigned a patrol section (a pair of checkpoints) by the spawner at spawn time; the checkpoints themselves are level/room data (Ch.13). The enemy walks between the two checkpoints at `PatrolSpeed`, flipping facing to match movement direction. In `Patrol`/`Idle` state it is not actively hunting.

**Detection model** (engagement trigger):
- **Facing the player + within `AggroRange`** → enter the engage cycle immediately (`Patrol`/`Idle` → `Alert`/`Chase` → `Telegraph` → `Attack`).
- **Not facing the player + within `DetectionRange`** → the enemy enters an **`Alert`** state for `AlertDuration` (~0.5s): it stops, then turns to face the player and begins the engage cycle. `DetectionRange` is typically ≥ `AggroRange` (the enemy "senses" a player behind it).
- Outside both ranges → continue patrolling / idle.
- Once aggroed, the enemy persists target until the player leaves `AggroRange × LoseAggroMultiplier` or the enemy dies. Facing changes after engagement do **not** de-aggro.

Perception is implemented as `StateConditionSO<T>` subclasses (e.g. `PlayerWithinRangeCondition`, `FacingPlayerCondition`, `LineOfSightCondition`). These read the player's position + the enemy's facing via a shared read-only service (a `PlayerReference` exposed through the Core/event layer), **not** via direct cross-module `GetComponent` (Ch.2.8). Aggro is a runtime field on the enemy actor. No faction system in the foundation; "enemy targets player only" is the default; faction extension is upper-layer.

**Facing & backstab** — the enemy's facing (left/right) is runtime actor state. A hit from behind registers as a backstab (Ch.4.5b), multiplying the flat portion by the weapon's `BackstabMultiplier`. This gives the player a reason to approach enemies from behind during patrol and makes the `Alert` turn-window meaningful.

### 11.4 Behaviour Composition

Enemy behaviour is authored exactly like player behaviour — by composing `StateAction` SOs onto `StateSO`s and wiring transitions in a `TransitionTableSO` (Ch.6.3). Typical enemy states: `Idle`, `Patrol`, `Chase`, `Telegraph`, `Attack`, `Hurt`, `Death`.

- Telegraph states (wind-up before an attack) are ordinary states with a timed transition to `Attack`; they give the player a react window.
- `Hurt` is a condition-led transition ordered early (Ch.6.4) so hits interrupt attacks.
- `Death` is the first transition (condition: HP ≤ 0), emitting `OnEnemyDied` with killer info (Ch.4.10).
- The `Attack` state delegates execution to `EnemyAttackController`. Generated components subscribe
  to attack-start/end and hit-window-open/close events; they never initiate StateMachine transitions.
- Leaving `Attack` early cancels the active window and marks the attack complete. Repeated attacks
  remain governed by the existing `AttackReady`/`AttackFinished` conditions and authored cooldown.
- Telegraph turning is opt-in through `EnemyTelegraphTurnData`/`EnemyTelegraphTurnComponent`.
  Without that component, an enemy faces its target once when Telegraph starts and cannot rotate
  again until the attack preparation ends, preserving the player's sidestep/dodge window.

### 11.4a Boss Timeline Composition (implemented)

Bosses reuse `EnemyStats`, `EnemyBrain`, `EnemyMotor`, `HealthComponent`, `EnemyAnimation`, and the
standard `DamageInfo` pipeline, but complex multi-part attacks are executed by `BossController` from
a `BossSO` timeline rather than by expanding the regular-enemy transition graph. `BossSO` contains
phase thresholds/attack intervals and a list of `BossAttackDefinition` records. Each attack contains
selection range/height constraints, telegraph and total duration, movement parameters, animation name,
and any number of independently timed `BossHitboxSegment` boxes. Segment geometry, target layers,
damage multiplier, spear-anchor use, and scene gizmo visibility are all designer-authored.

The current `Boss.prefab`/`Boss.asset` implements:

- Far, same-level dash with Continuous Rigidbody collision and a damaging dash window.
- High jump -> ground-probed spear impact -> dash to the stored spear anchor; spear and dash are separate
  hitbox segments and can each damage once. `SpearVisual` is a prefab child with a replaceable renderer.
- Close vertical-to-horizontal slash combo with separate damage windows.
- Close ground stab with two independent two-unit left/right hitboxes.
- Stage 2 at 50% HP. After each primary attack it adds simultaneous three-unit left/right surrounding
  slash boxes, then waits 1.5 seconds before the next attack. During every attack interval the boss only
  approaches the player.

The Boss prefab intentionally disables the regular enemy `StateMachine`; `BossController` is the
high-level scheduler for this authored boss while regular enemies continue to use Ch.6 exclusively.

### 11.5 Targeted-Potential Attack (5.7 integration)

When an enemy downs the player (HP reaches 0, Ch.5.6), its `TargetPotential` selects the only Potential that can be reduced and its numeric `PotentialAttack` is multiplied by `2^DownCount`. Every enemy owns this configuration; zero is reserved for no-penalty testing. No even-distribution fallback exists. The enemy never executes the reduction itself: `PlayerDeathController` resolves `IPotentialAttackSource` from `DamageInfo.Source` and owns the complete down/respawn flow.

### 11.6 Difficulty Scaling

Enemy stats are **authored per-set** — each enemy set is tuned for the stage it belongs to, and sets are **stage-locked** (a set only rolls in its own stage, Ch.13.6). Cross-stage difficulty comes from *which set is chosen*, not from scaling one baseline enemy across stages. Two multipliers sit on top of authored stats:

- **Room-progress multiplier** — within a single stage, later rooms apply a small multiplier to enemy HP/Armor/MR so the back half of a stage feels harder than the front. Driven by a **spawner-computed `StageScalingTier`** (Ch.13.4a step 5: function of `CurrentStage`, room depth band, and wave index within the room); the spawner applies it per-enemy at spawn time. This is **not** cross-stage scaling.
- **Challenge mode** (planned, post-first-successful-run, Hades-like): a run-level multiplier scales enemy base stats upward, stacking on top of authored stats + room-progress. Enemies still have no Potential (Ch.5.7/11.2); the multiplier applies to base values only.

- `% damage` cap (`CapMultiplier`, Ch.4.4) is tuned per boss to prevent % weapons trivializing them.
- No new combat mechanics are added at higher stages — only numbers scale (authored per-set, then room-progress + challenge multipliers).

### 11.7 Red Lines

1. Regular-enemy AI uses the Ch.6 StateMachine exclusively. Bosses may use a scoped `BossController`
   timeline scheduler for multi-part authored attacks, but no second generic AI framework / "AI pool"
   may be introduced.
2. Enemy `StateAction` / `StateCondition` subclasses are upper-layer (`Zephyr.Gameplay.Enemies`); the `Zephyr.Core.StateMachine` foundation is not modified.
3. Perception reads the player via the shared Core service/event layer, never via direct cross-module references (Ch.2.8).
4. The targeted-potential attack is one-directional (enemy → player on **down**, Ch.5.6/5.7); enemies do not erode each other or themselves.
5. Regular enemies have built-in `EnemySO` attacks. They must not depend on `WeaponSO`,
   `WeaponRuntime`, player weapon sockets, or player weapon component data.

---

## Chapter 12 — Roguelike Data Layering + Save System

> **Implemented snapshot:** `SaveSystem` owns three manual slots under
> `persistentDataPath/Save/slot_1..3`, each with `meta_data.json` and optional `run_data.json`. First
> launch leaves all slots empty. `JsonUtility` DTO lists round-trip dictionaries/hash sets. There is no
> schema migration or background I/O yet. RunData stores the current compact run model plus acquired
> weapon IDs and an idempotent settlement flag; MetaData stores profile, story, mechanics, purchases,
> currency, weapon codex, upgrade maps, and challenge data.

> Layer 🟥: locked foundation. Namespace `Zephyr.Core.Save`. Strict separation between **run data** (in-run, volatile, reset on true death / run end) and **meta data** (out-of-run, persistent). Multi-slot save with interrupt-save (Q16).

### 12.1 Data Layer Separation (binding)

Two layers, **never** mixed in a single object graph:

| Layer | Scope | Examples | Lifetime |
|-------|-------|----------|----------|
| **Run Data** | a single run | run seed, current HP, current potentials (run-local: system-random N−M + player-allocated M), down count, equipped `WeaponSet`, active buffs, run currency, current room/level | created at run start; discarded at run end (true death or victory) |
| **Meta Data** | cross-run, per profile | permanent currency, base-stat upgrades, potential-budget upgrades (N), potential-allocatable upgrades (M), unlocked multipliers, weapon-codex unlocks, settings | persists across runs and sessions |

- Run data may **read** meta data (e.g. apply meta upgrades at run start) but may **not write** to it during a run.
- Meta data is written **only** at run-end settlement (Ch.22) and on settings changes — never mid-run.

### 12.2 Run Data

```csharp
[Serializable]
public class RunData
{
    public RunId RunId;
    public int Seed;                                    // deterministic run seed
    public float CurrentHp;
    public Dictionary<StatType, StatRecord> Stats;      // current Base + Potential; Potential starts from run-entry
                                                        // random allocation (Ch.5.2) and erodes on downs (Ch.5.6)
    public int DownCount;                               // monotonic per run; drives scaling down-cost (Ch.5.6)
    public ResourceBar Stamina, Energy;                 // Ch.5.5
    public WeaponSet Weapons;                           // Ch.9.4
    public BuffInstanceId[] ActiveBuffs;                // serialized buff state (Ch.7)
    public int RunCurrency;                             // in-run currency (converts to meta on death, Ch.22)
    public LevelProgress LevelProgress;                 // current level/room (Ch.13)
    public HashSet<ObjectiveId> PendingObjectives;      // objectives fulfilled this run; committed to meta at run-end (Ch.22.7)
    public float PlayTime;
}
```

- Potentials are **run-local**: they start from the run-entry allocation — system-random N−M points as the baseline, then the player-allocated M points (Ch.5.2) — and erode on downs (Ch.5.6). They reset every run — nothing potential-related is committed to meta (no cross-run erosion).
- `DownCount` is monotonic per run and drives the escalating down-cost; it resets each run.
- Buff serialization must round-trip `BuffData` + remaining duration (Ch.7).

### 12.3 Meta Data

```csharp
[Serializable]
public class MetaData
{
    public int PermanentCurrency;                          // spent on meta upgrades (Hades-like)
    public Dictionary<StatType, float> BaseStatUpgrades;       // added to char intrinsic Base at run start
    public int PotentialBudgetUpgrades;                    // grows N — total run-entry potential points (system randomly allocates N−M, player allocates M; Ch.5.2)
    public int PotentialAllocatableUpgrades;               // grows M — # of those points the player manually allocates (M ≤ N)
    public List<MultiplierUnlock> UnlockedMultipliers;         // extra PercentMult damage tiers (Ch.5.4) — meta, NOT potential-scaled (base)
    public CombatAcquiredUpgrades Combat;                  // crit + pen — player-own meta upgrades, NOT potential-scaled (base, Ch.5.3)
    public HashSet<ItemId> WeaponCodex;                    // unlocked weapon types → small permanent bonus
    public bool HasCompletedTutorial;                      // one-time per profile: tutorial has been finished (Ch.14.3)
    public HashSet<ObjectiveId> FulfilledObjectives;       // meta-persistent story objectives (monotonic, Ch.22.8)
    public HashSet<StoryBeatId> TriggeredStoryBeats;       // story beats already fired (prevents repeat, Ch.22.8)
    public HashSet<MechanicUnlockId> UnlockedMechanics;  // gameplay mechanics permanently unlocked (Ch.22.6)
    public Settings Settings;
}

[Serializable]
public struct MultiplierUnlock { public StatType Stat; public float PercentMult; }

[Serializable]
public struct CombatAcquiredUpgrades                       // player-own combat bonuses, NOT weapon-inherent
{
    public float CritChanceBonus;        // added to resolved crit chance
    public float CritMultiplierBonus;    // added to resolved crit multiplier
    public float FlatArmorPen;           // 双固穿
    public float FlatMagicPen;
    public float PercentArmorPen;        // 双百分比穿, 0..1
    public float PercentMagicPen;
}
```

- At run start, `RunData.Stats` **Base** values are seeded from character intrinsic + `BaseStatUpgrades` (both are base values, NOT potential-scaled, Ch.5.3). `RunData.Stats` **Potential** values are seeded from the run-entry allocation (Ch.5.2): the system randomly allocates N−M points across the 10 types (random baseline), after which the player allocates the remaining M points, where N = `GameSettings.Stats.PotentialBudgetBase + PotentialBudgetUpgrades` and M (player-allocatable) = `PotentialAllocatableBase + PotentialAllocatableUpgrades`. There is no meta-persistent potential baseline.
- `CombatAcquiredUpgrades` are **meta upgrades** (base values, NOT potential-scaled, Ch.5.3): summed with weapon-inherent values at hit time (Ch.9.6 / 4.3). They are the **only** player-side source of pen and player-side crit.
- `WeaponCodex` unlocks grant a small permanent stat bonus when a weapon type is first picked up (Q14).

### 12.4 Save System Architecture

```csharp
namespace Zephyr.Core.Save
{
    public static class SaveSystem
    {
        // N independent slots = N independent profiles (multi-slot, Q16)
        public static void SaveSlot(int slotIndex, in SaveBundle bundle);
        public static bool LoadSlot(int slotIndex, out SaveBundle bundle);
        public static void DeleteSlot(int slotIndex);
        public static bool SlotExists(int slotIndex);
    }

    [Serializable]
    public struct SaveBundle
    {
        public MetaData Meta;
        public RunData Run;          // null if not mid-run
        public SaveKind Kind;        // Manual | Interrupt | RunEnd
        public int SchemaVersion;
    }

    public enum SaveKind { Manual, Interrupt, RunEnd }
}
```

- Multi-slot: each slot is a fully independent profile (own `MetaData` + at most one `RunData`).
- **Interrupt save**: the player may save mid-run and resume later. On resume the run is reconstructed exactly (room, HP, weapons, buffs). This is the `Interrupt` save kind.

### 12.5 Save Triggers (binding)

| Trigger | What is saved | Kind |
|---------|---------------|------|
| Run start | Meta + new Run | RunEnd (meta only, run just begun) |
| Mid-run (player requests / checkpoint) | Meta + Run | Interrupt |
| Run end (true death / victory) | Meta (run currency → permanent) | RunEnd |
| Settings change | Meta only | Manual |

- On true death (potential ≤ 0, Ch.5.6) or victory: `RunData.RunCurrency` is converted to `MetaData.PermanentCurrency` per the run-end settlement (Ch.22), then `RunData` is discarded. A **down** (HP ≤ 0) is NOT a run end — the run continues with a respawn (Ch.5.6), so no save/write occurs on a down.
- Reward RNG (drops, shop rolls, upgrade rewards) is fully driven by `RunData.Seed`. Reloading a save yields the same rewards for the same state, so SL-farming is moot — no anti-SL restriction is needed (single-player).

### 12.6 Serialization

- Format: JSON (human-readable, debuggable). Versioned via `SchemaVersion`; migrations run on load.
- Unity object references (e.g. `WeaponSO`, `TransitionTableSO`) are saved as `ItemId` / GUID and re-resolved from an asset registry on load — never serialized as scene-embedded references.
- Save I/O runs on a background thread: the main thread builds the `SaveBundle` snapshot, then hands off serialization.

### 12.7 Red Lines

1. `SaveSystem`, `SaveBundle`, `RunData`, `MetaData` are locked; AI may not modify.
2. Run data and meta data must never share an object graph; run data must not write meta data mid-run.
3. Unity object references are saved as IDs and re-resolved, never serialized inline.
4. All reward RNG is seeded by `RunData.Seed`; no ad-hoc per-event RNG for drops/shop/upgrade rewards.
5. Save schema is versioned; breaking changes require a migration.

---

## Chapter 13 — Level / Room System

> Layer 🟧: the foundation defines the room framework (`RoomSO`, `RoomType`, `LevelProgress`, `RoomController`, door/lock lifecycle). Upper layer authors specific `RoomSO` assets (layouts, enemy spawns, decorations). Namespace: `Zephyr.Core.Levels` (framework) + `Zephyr.Gameplay.Levels` (authored rooms).

### 13.1 Design Stance (binding)

No procedural **layout** generation (confirmed constraint — no procgen tech, no AI procgen monitoring). Instead: **procedural selection of handcrafted rooms**. A run's room sequence is chosen by `RunData.Seed` from pools of authored `RoomSO`s. Layout, enemy placement, and decoration are all hand-authored inside each `RoomSO`; only *which room comes next* is rolled. A fully-linear hand-authored sequence is the degenerate case (single-room pool per slot).

### 13.2 Room Data Model

```csharp
namespace Zephyr.Core.Levels
{
    public enum RoomType { Combat, Elite, Shop, Treasure, Rest, Boss, Event }

    [CreateAssetMenu(menuName = "Zephyr/Levels/Room")]
    public class RoomSO : ScriptableObject
    {
        public ItemId Id;
        public RoomType Type;
        public SceneReference Scene;        // additively-loaded scene for this room (or prefab root)
        public Rect WorldBounds;            // outer soft camera bounds for the room (Ch.19.2 — room may exceed viewport)
        public SpawnPointSpec[] SpawnPoints; // hardcoded spawn positions + tag filters; wave count & per-wave assignments are
                                             // spawner-computed at runtime (Ch.13.4 Model B)
        public ExitSpec[] Exits;            // doors + where they lead (logical, not spatial)
        public RewardSpec Reward;           // drop/weapon/currency reward for clearing (Ch.15)
        public bool LocksUntilCleared;      // combat rooms lock doors until all waves cleared
    }

    /// <summary>One hardcoded spawn position in a room. Tags filter which active-set enemies may spawn here (Ch.13.4).</summary>
    public struct SpawnPointSpec
    {
        public Vector2 Position;            // the ONLY hardcoded room layout value; everything else is dynamic / tag-driven
        public string[] Tags;               // 1+ tag filters; an active-set enemy is eligible at this point iff the intersection
                                            // of its EnemySO.Tags and this point's Tags is non-empty (Ch.13.4 Model B)
    }

    public struct ExitSpec { public DoorId Door; public RoomSlot TargetSlot; }   // TargetSlot = which pool to draw next room from
    public struct RewardSpec { public DropTableId DropTable; public int Currency; public bool GuaranteesWeapon; }
    public enum RoomSlot { CombatA, CombatB, Elite, Shop, Treasure, Rest, Boss }  // pools the selector draws from
}
```

### 13.3 Level Progress & Seed-Driven Selection

```csharp
[Serializable]
public class LevelProgress
{
    public int CurrentDepth;                // how deep into the run (0 = first room)
    public int CurrentStage;                // 1-based stage index (Ch.13.6)
    public EnemySetId ActiveSetId;          // the stage-locked set rolled for the current stage (Ch.13.6)
    public RoomId CurrentRoomId;
    public List<RoomId> Visited;            // for minimap / backtrack gates
    public RoomSlot NextSlot;               // which pool the next room is drawn from
}
```

- When the player takes an exit, `RoomSelector` draws the next `RoomSO` from the `ExitSpec.TargetSlot` pool using a deterministic RNG seeded by `RunData.Seed + CurrentDepth` (same seed+depth ⇒ same room — SL-safe, Ch.12.5).
- Boss rooms are placed at fixed depth intervals (e.g. every N rooms); the selector forces `RoomSlot.Boss` at those depths.
- `Visited` is retained for the current run only; backtracking into a cleared combat room keeps it cleared (spawns not respawned).

### 13.4 Room Lifecycle (RoomController) + Dynamic Wave Spawning (Model B)

A single `RoomController` (one per active room) drives the room lifecycle. Wave count and per-wave composition are **spawner-computed at runtime** (Model B — Ch.13.4), not authored per room.

```
Room lifecycle:

1. **Load** — additively load `RoomSO.Scene`; register room pools (Ch.8.5); `CameraController.SetRoomBounds(RoomSO.WorldBounds)` + `SnapTo(entryPosition)` (Ch.19.5).
             Room spawner pre-computes the wave schedule for this room (seed-deterministic, Ch.13.4a).
2. **Enter** — place player at the entry door; if `LocksUntilCleared`, lock all exits.
             Spawn wave 0 per the pre-computed schedule. Camera zones in the room scene become active via physics triggers (Ch.19.3) — they activate when the player enters each zone, *not* at room load.
3. Active  → normal gameplay. Wave-by-wave advance:
             3.1 all living enemies in the current wave die → increment wave index.
             3.2 if wave index ≥ TotalWaves → goto 4 (room clear).
             3.3 else wait InterWaveDelay s, then spawn the next wave per schedule.
4. Clear   → unlock exits + emit OnRoomCleared (combat rooms require all waves cleared;
             shop/rest/treasure skip straight here).
5. **Exit** → player picks a door → RoomSelector draws next room → return pooled objects →
             unload current room → load next. On room exit, `CameraController.ResetToRoomBounds()` + `SetRoomBounds(nextRoom.WorldBounds)` is called as part of the next room's Load step (Ch.19.5).
```

- Additively-loaded room scenes are unloaded on exit (not kept in memory). Only one room is active at a time.
- Cleared waves stay cleared within the room. On interrupt-save mid-room, the wave index and alive/dead state of the current wave's enemies are persisted so a reload resumes from the current wave (already-cleared waves are not replayed).

### 13.4a Wave Schedule (spawner-computed, seed-deterministic)

At room-load time, the spawner (upper layer) runs the following deterministic steps using the seed **`RunData.Seed + (int)RoomSO.Id.Value + CurrentDepth`**:

| Step | What is computed | Driven by |
|------|------------------|-----------|
| 1. **WaveCount** W | How many waves this room has. Range tunable per RoomType. | `RoomType` + `CurrentStage` + `CurrentDepth` (deeper in stage → more waves) + challenge-mode multiplier flag. E.g. Combat: 2–4 waves, Elite: 3–5 waves, Boss: 1 wave. |
| 2. **PerWaveEnemyCount** K_w | How many enemies spawn in wave w (0..W−1). Later waves generally have higher K. | Same inputs as WaveCount, plus monotonic bias so K_w ≥ K_w−1. `K_w ≤ RoomSO.SpawnPoints.Length` always (can't spawn more enemies than there are spawn points). |
| 3. **PerWaveSpawnPointSelection** | For wave w, draw a **random subset of K_w spawn points** from `RoomSO.SpawnPoints` without replacement within the wave. The remaining `N − K_w` spawn points sit unused this wave (a spawn point may be selected in multiple different waves across the room — Model B reuse). | Deterministic sample from seed + w. |
| 4. **PerPointEnemyPick** | For each selected spawn point p in wave w: collect the **tag-eligible subset** of the active enemy set: `{ e ∈ ActiveSetRoster  |  e.Tags ∩ p.Tags  ≠ ∅ }`. Then seed-randomly pick one enemy from that subset. If the eligible subset is empty, fall back to the entire active-set roster (with a Dev warning — tag-miss shouldn't happen with good authoring). | Seed + w + point index. |
| 5. **StageScalingTier** | Per-enemy scaling tier used at spawn time (Ch.11.6 room-progress multiplier). **Spawner-computed, not authored.** | `tier = clamp(floor(CurrentStage / 2) + roomDepthBand + waveIndex / max(1, W), 0, MaxTier)`. Spawner applies the tiered HP/Armor/MR multiplier per-enemy at spawn time. |
| 6. **InterWaveDelay** | Delay (s) between wave-w clear and wave-(w+1) spawn. Tunable global constant. | `GameSettings.Combat.InterWaveDelay` (default ~1.0s). |

The full schedule (W, each K_w, each per-wave selected points, each point's enemy pick, each enemy's tier) is the deterministic output of these 6 steps and is cached on `RoomController` for the room's lifetime. Because everything derives from `RunData.Seed`, SL/load yields the identical wave schedule (Ch.12.5 determinism guarantee).

### 13.4b Tag Matching Rules

- Tags are plain `string`s; comparison is ordinal case-insensitive. An enemy is eligible at a spawn point iff the tag-set intersection is non-empty.
- **One spawn point, multiple tags = union filter.** Example: spawn point tags `{ "Melee", "Ranged" }` → any enemy tagged Melee **or** Ranged is eligible (allows mixed waves from a single point).
- **One enemy, multiple tags = classification.** Example: enemy tags `{ "Heavy", "Melee", "Armored" }` → matches any spawn point that needs Heavy **or** Melee **or** Armored.
- No negative tags, no weight system on tags — all eligible enemies are drawn from uniformly. (Upper layer can extend with weights later; the foundation just requires the subset to be resolved deterministically.)

### 13.4c Spawn-Point Pressure & Telegraph

Because Model B reuses spawn points across waves and randomly selects which K_w points fire per wave, the next wave can materialize directly under the player's feet. Clearing a wave is **not** a safe stand-still moment — this is by design (unlike Dead Cells' sparse, one-and-done encounters, it's more Hades-like sustained pressure).

Consequence: the upper layer **should** render a brief spawn telegraph at each selected point K_w before the enemy materializes (portal / warning VFX / shadow pool), with a react window of `SpawnTelegraphDuration` (~0.35 s default, tunable in `GameSettings.Combat`). The foundation does **not** enforce a telegraph but requires that all enemy materializations go through the spawner and pool system (Ch.8 / Ch.11), never a raw `Instantiate`.

### 13.5 Doors & Exits

- Doors are `IInteractable` (Ch.2.4 / Ch.15.2a). Locked doors return `CanInteract = false` and show a locked state. When unlocked:
  - **Walking through** the door trigger auto-triggers the room transition (QoL shortcut).
  - Pressing the interaction button (Ch.18 `OnInteract` → `PlayerInteractor.Nearest.Interact`) also triggers the transition (primary path — consistent with ladders and weapon pickups).
- Each `ExitSpec` maps a door to a `RoomSlot` (next-room pool), not to a specific room — the selector decides the actual room. This keeps each authored room reusable across runs.

### 13.6 Stages & Enemy-Set Selection

A run is divided into **stages**. Each stage has a small pool of **stage-locked enemy sets**; each set carries its own enemy roster + a corresponding **background/theme**. At stage entry, `RunData.Seed` picks **one set** (with its theme) for that stage — the chosen set's enemies populate that stage's rooms, and the set's theme drives the background art/audio.

```csharp
namespace Zephyr.Core.Levels
{
    public struct EnemySetId { public int Value; }   // stage-locked set membership (Ch.11.2 EnemySO.SetId)

    [CreateAssetMenu(menuName = "Zephyr/Levels/EnemySet")]
    public class EnemySetSO : ScriptableObject        // upper layer
    {
        public EnemySetId Id;
        public int Stage;                             // the stage this set is locked to (1-based)
        public ItemId[] EnemyRoster;                  // EnemySOs that belong to this set
        public SceneReference BackgroundTheme;        // background art/audio applied at stage load
    }

    [CreateAssetMenu(menuName = "Zephyr/Levels/Stage")]
    public class StageSO : ScriptableObject            // upper layer
    {
        public int StageNumber;
        public EnemySetSO[] CandidateSets;            // stage-locked pool; one is rolled per run
        public RoomSlot[] RoomSequence;               // room-slot pattern for this stage
    }
}
```

- **Stage-locking is strict**: an `EnemySetSO` with `Stage = 3` only ever rolls in stage 3; its enemies never appear in another stage. This is why enemy stats are authored per-set (Ch.11.6) — no cross-stage scaling of the same enemy is needed.
- Set selection is seed-driven (`RunData.Seed + StageNumber`) — SL-safe (Ch.12.5). The chosen `EnemySetId` is stored on `LevelProgress.ActiveSetId` so the spawner resolves spawns against the active set.
- Rooms draw their enemies via tag-based resolution against the active set's roster (Ch.13.4a step 4: a spawn point's `Tags` intersect an enemy's `Tags` → eligible; seed picks one). Authored placements carry no fixed enemy identity — only positions + tag filters. A `RoomSO` is theme-agnostic; the set's `BackgroundTheme` is applied at stage load, decoupling room layout from theme and enemy roster.

### 13.7 Red Lines

1. `RoomSO`, `RoomType`, `SpawnPointSpec`, `LevelProgress`, `RoomController`, `RoomSelector`, `EnemySetSO`, `StageSO` are the locked framework; AI may add `RoomSO`/`EnemySetSO`/`StageSO` **assets**, not new framework types.
2. No procedural **layout** generation; only seed-driven selection of authored rooms + dynamic wave scheduling. Spawn point positions and tags are **authored** (hardcoded per room); enemy identity, per-wave spawn count, and wave count are **spawner-computed** (Ch.13.4a Model B).
3. Room/set-selection RNG and wave-schedule RNG are `Seed + Depth` / `Seed + Stage` / `Seed + RoomId + w`-deterministic (Ch.12.5/13.4a); no ad-hoc RNG.
4. Enemy sets are **stage-locked** — a set never rolls outside its `Stage`. Cross-stage stat scaling of the same enemy is forbidden (stats are authored per-set, Ch.11.6).
5. Only one room active at a time; exiting a room unloads its scene after returning pooled objects.
6. Cleared rooms stay cleared within a run (no respawn on backtrack).
7. Wave count and per-wave enemy counts are **never authored per room** — they come from the spawner's schedule (Ch.13.4a). Adding `WaveCount` or `Waves[]` back onto `RoomSO` is forbidden; the only authored spawn data is `SpawnPoints[]` (position + tags).
8. `RoomSO.WorldBounds` is the outer soft camera bounds (Ch.19.2/19.5). Camera zones live in the room's scene (not on `RoomSO`) — they are authored triggers attached to the scene's physics colliders, activated when the player enters them. Camera zone activation does not escalate to `GameFlow`.

---

## Chapter 14 — Scene & Game Flow

> **Implemented snapshot:** `SceneSO` stores SceneId, editor scene reference, and cached name/path.
> Normal transitions raise SO load/unload events and are serialized by Persistent `SceneLoader`; the
> Initializer's direct Persistent load is the boot exception. GameManager owns gameplay services and
> spawns the player, then moves the player object into Tutorial/room scene ownership at the authored
> spawn point. Returning to MainMenu unloads GameManager. InRun/Paused to MetaHub settles and clears the
> current run before scene replacement.

> Layer 🟥: locked foundation. Namespace `Zephyr.Core.Flow`. A high-level global state machine drives scene loading and game-phase transitions; it is separate from the per-actor Ch.6 StateMachine.

### 14.1 Global Game States

```csharp
namespace Zephyr.Core.Flow
{
    public enum GameState
    {
        Boot,            // initialize persistent systems
        MainMenu,        // title / slot select
        Tutorial,        // one-time per-profile preset tutorial stage → MetaHub on complete (Ch.14.3a)
        MetaHub,         // out-of-run upgrade hub (Hades-like courtyard)
        LoadingRun,      // build RunData + load first room
        InRun,           // active gameplay (one room active)
        Paused,          // overlay; InRun frozen
    }

    public sealed class GameFlow : MonoBehaviour
    {
        public static GameFlow Instance { get; }  // singleton, lives in Persistent scene
        public GameState CurrentState { get; }
        public event Action<GameState, GameState> OnStateChanged;  // prev, next

        // Request a state transition (validated against guarded transition table)
        public void RequestTransition(GameState targetState);

        // Delegate scene load/unload to SceneLoader via event bus
        public void RequestSceneLoad(SceneId sceneId, LoadSceneMode mode);
        public void RequestSceneUnload(SceneId sceneId);
    }
}
```

- Transitions are **requested**, not forced: `RequestTransition` validates against a guarded transition table (`IsValidTransition`) and routes through the scene loader. Invalid transitions are rejected + logged (Ch.25).
- State changes are published via **both** a C# event (`OnStateChanged`) and a `GameEventSO<GameState>` (`_onGameStateChanged`) for cross-scene decoupled subscribers.
- `OnStateChanged` is the single source of truth for game phase; subsystems subscribe via the event system (Ch.3) — never poll `GameFlow.CurrentState`.
- There is **no `RunEnd` state** — true-death/victory transitions go directly to `MetaHub` via `InRun → MetaHub` transition.

### 14.2 Scene Layout (additive multi-scene structure)

The game uses a **multi-scene additive loading** structure with two tiers of persistent systems and scene-bound gameplay scenes. The `Persistent` scene is never unloaded; the `GameManager` scene is loaded when gameplay starts and unloaded when returning to `MainMenu`. All gameplay scene loads (`Tutorial`, `MetaHub`, `Room_*`) are additive.

| Scene | Owner | Lifetime |
|-------|-------|----------|
| `Initializer` | boot bootstrap (tiny, one script) | loaded first (single-mode); loads `Persistent` additively; unloads self after initialization |
| `Persistent` | infrastructure systems | loaded additively by `Initializer`; **never unloaded** during a play session |
| `GameManager` | gameplay-specific systems | loaded additively on `MainMenu → Tutorial/MetaHub/LoadingRun`; **unloaded** on return to `MainMenu` |
| `MainMenu` | menu | additively loaded on `Boot → MainMenu`, unloaded on play |
| `Tutorial` | one-time per-profile preset stage | additively loaded for Tutorial state; unloaded after tutorial completion → MetaHub |
| `MetaHub` | upgrade hub | additively loaded for MetaHub state |
| `LoadingRun` | run-entry transition | additively loaded for LoadingRun state |
| `Room_Generic` | procedural rooms (Ch.13) | additively loaded per room, unloaded on exit |
| `Room_Boss` | boss encounter rooms | additively loaded for boss rooms |

**Persistent-scene managers (infrastructure — always loaded):**
`EventBus` (Ch.3), `SaveSystem` (Ch.12), `GameFlow` (Ch.14.1), `AudioManager` (Ch.17), `LocalizationManager` (Ch.27), `PoolRegistry` (Ch.8), `Timer` (Ch.20), `SceneLoader` (async scene loading + loading-screen management).

**GameManager-scene managers (gameplay-specific — loaded for Tutorial/MetaHub/InRun):**
`CameraController` (Ch.19), `UIManager` (Ch.16), `ProjectileSystem` (Ch.10), combat subsystems (DamageCalculator host, buff management, spawner routing).

These are **not** `DontDestroyOnLoad` objects — their persistence is guaranteed by their scenes not being unloaded. GameManager managers survive `Tutorial → MetaHub → LoadingRun → InRun → MetaHub` cycles; they are **only** unloaded when `MainMenu` is re-entered.

**Scene loading model (binding):**
1. `Initializer` scene loads in **single mode** (the only single-mode load in the entire game).
2. `Initializer` loads `Persistent` scene **additively** and waits for `PersistentReady` event.
3. `Initializer` then **unloads itself**. `GameFlow` transitions `Boot → MainMenu`, loading `MainMenu` additively.
4. On `MainMenu → Tutorial/MetaHub/LoadingRun`: the `SceneLoader` (in `Persistent`) loads `GameManager` scene **additively** and waits for `GameManagerReady` event. Then the destination scene (`Tutorial`/`MetaHub`/first `Room_*`) is loaded additively.
5. On any transition **back to `MainMenu`**: `GameManager` scene is unloaded (its managers and all gameplay state are destroyed). `Persistent` and `MainMenu` remain.
6. All intermediate scene transitions (`Tutorial → MetaHub`, `MetaHub → LoadingRun`, `Room_* → Room_*`) keep `GameManager` loaded — only the gameplay scene is swapped.

**Why split Persistent vs GameManager:** separates infrastructure (needed for MainMenu: scene loading, audio, localization, save) from gameplay-specific systems (needed only during runs: camera, gameplay UI, projectiles, combat). The `MainMenu` has its own camera and UI that live in the `MainMenu` scene itself; they do not depend on `GameManager`.

**SceneLoader binding:** `SceneLoader` lives in `Persistent` and handles **all** scene loading requests. Gameplay code (including `GameManager` components) never calls `SceneManager.LoadScene` directly — it raises a `OnRequestSceneLoad(SceneLoadRequest)` event (Ch.3) which `SceneLoader` subscribes to and processes asynchronously. `SceneLoader` owns the loading-screen UI and fade transitions.

```csharp
namespace Zephyr.Core.Flow
{
    public sealed class SceneLoader : MonoBehaviour
    {
        public static SceneLoader Instance { get; }

        // Subscribed event channels (assigned in Persistent scene Inspector)
        [SerializeField] private GameEventSO<SceneLoadRequest> _onRequestSceneLoad;
        [SerializeField] private GameEventSO<SceneInfo> _onSceneLoaded;

        // Private handler — subscribed via OnEnable/OnDisable
        private void HandleSceneLoadRequest(SceneLoadRequest request);

        // Internal coroutines — manage loading screen + fade transitions
        private IEnumerator LoadSceneAsync(SceneId sceneId, LoadSceneMode mode);
        private IEnumerator UnloadSceneAsync(SceneId sceneId);
        private void ShowLoadingScreen();
        private void HideLoadingScreen();
    }

    // Payload for OnRequestSceneLoad event
    public readonly struct SceneLoadRequest
    {
        public readonly SceneId SceneId;
        public readonly LoadSceneMode Mode;
        public SceneLoadRequest(SceneId sceneId, LoadSceneMode mode = LoadSceneMode.Additive);
    }

    // Payload for OnSceneLoaded completion event
    public readonly struct SceneInfo
    {
        public readonly SceneId SceneId;
        public readonly string SceneName;
    }
}
```

**Event flow:**
1. Gameplay code calls `GameFlow.Instance.RequestSceneLoad(sceneId, mode)` which raises `_onRequestSceneLoad.Raise(new SceneLoadRequest(sceneId, mode))`.
2. `SceneLoader.HandleSceneLoadRequest` receives the event and starts `LoadSceneAsync`.
3. After successful load, `_onSceneLoaded.Raise(new SceneInfo(...))` notifies other systems (spawn points, camera zones, etc.).

### 14.3 Transition Flow (binding)

- **Boot → MainMenu**: `Initializer` loads `Persistent` scene additively (Ch.14.2 steps 1-3) → managers initialize → `PersistentReady` event fires → `Initializer` unloads itself → `GameFlow` transitions to `MainMenu` → `MainMenu` scene loads additively on top of `Persistent`.
- **MainMenu → Tutorial / MetaHub / LoadingRun** (slot-picked, branch on profile state): `SceneLoader` loads `GameManager` additively and waits for `GameManagerReady` → then picks destination based on profile state:
  - **New profile** (`!MetaData.HasCompletedTutorial`) → `Tutorial` scene loads additively (Ch.14.3a).
  - **Returning profile** (`HasCompletedTutorial == true`) with no interrupt save → `MetaHub` scene loads additively.
  - **Returning profile** with an interrupt save → `LoadingRun` scene loads additively (resume).
- **Tutorial → MetaHub**: tutorial completion sets `MetaData.HasCompletedTutorial = true`, persists the meta save, then transitions to `MetaHub` (one-time per profile — never re-entered). `GameManager` stays loaded.
- **→ MetaHub (any source)**: on MetaHub entry, `StoryProgressionSystem.EvaluatePendingBeats(meta)` runs — any pending story beats queue their conversations for sequential playback via the `DialoguePlayer` panel before MetaHub interaction unlocks. `GameManager` stays loaded (it owns `UIManager` and `CameraController`).
- **MetaHub → LoadingRun**: apply meta upgrades to a fresh `RunData` (Ch.12.3 seeding) → load first `Room_Generic` scene additively on top of `GameManager`.
- **LoadingRun → InRun**: first room ready → enter gameplay.
- **InRun → Paused**: overlay opens; gameplay tick frozen (Ch.24); save is **not** auto-written.
- **InRun → MetaHub** (true death / victory): freeze gameplay → run-end settlement (Ch.22) → write meta save → transition to `MetaHub`. **True death** = any potential ≤ 0 (Ch.5.6); a **down** (HP ≤ 0) does **not** escalate to `MetaHub` — it is handled inside `InRun` by the Player module (respawn with potential deduction, Ch.5.6) and never reaches `GameFlow`.
- **Paused → MetaHub**: quit to meta hub from pause menu (abandon run).
- **Any → MainMenu**: unload gameplay scenes (Tutorial/MetaHub/Room_*/LoadingRun) → **unload `GameManager` scene** (all gameplay-specific managers destroyed) → `MainMenu` scene stays loaded. `Persistent` never unloads.

### 14.3a Tutorial Stage (one-time per profile)

A **preset tutorial stage** runs exactly once per save profile, the first time the player enters a new slot. It is gated by `MetaData.HasCompletedTutorial` (Ch.12.3) — once `true`, the `Tutorial` state is never re-entered from `MainMenu` (the slot goes straight to `MetaHub`/`LoadingRun`).

```csharp
namespace Zephyr.Core.Flow
{
    [CreateAssetMenu(menuName = "Zephyr/Flow/TutorialStage")]
    public class TutorialStageSO : ScriptableObject        // upper layer
    {
        public SceneReference Scene;                       // the preset tutorial map (a single hand-authored stage, not a Room_*)
        public TutorialStep[] Steps;                       // ordered, scripted steps (move / jump / attack / dodge / pickup …)
    }

    [Serializable]
    public struct TutorialStep
    {
        public string Id;                                  // e.g. "move", "attack", "swap_weapon"
        public string LocalizedPromptKey;                  // Ch.27 localization key for the on-screen prompt
        public string AdvanceEvent;                        // event id that marks this step complete (e.g. "OnFirstAttack")
        public float MinDuration;                          // min hold time before the step can advance (prevents skip-on-accident)
    }
}
```

- **Stage, not room** — the tutorial is a single hand-authored map with a **preset, scripted procedure** (fixed step sequence). It is **not** a `RoomSO` and does **not** participate in the Ch.13 room/wave system: no `SpawnPoints`, no wave scheduling, no `RoomSelector`. Enemy placements (if any) are fixed authored props in the scene, not spawner-driven.
- **No `RunData`** — the tutorial runs **without** a `RunData`. There is no seed, no potentials, no run currency, no weapon pool; the tutorial grants the player a fixed authored loadout for the duration. Nothing that happens in the tutorial is persisted to `RunData` (there isn't one), and the tutorial does **not** contribute to `WeaponCodex` (Ch.12.3) — codex unlocks happen only in real runs.
- **No failure path** — the tutorial cannot be "failed". A down (HP ≤ 0, if the tutorial even models HP) simply respawns the player in-place; there is no potential deduction (no potentials exist) and no run-end settlement. The only exit is **completion**.
- **Completion flow** (binding):
  1. The last `TutorialStep` fires its `AdvanceEvent` → `TutorialController` raises `OnTutorialComplete`.
  2. `GameFlow.RequestTransition(MetaHub)` is called.
  3. On transition confirm, the tutorial scene is unloaded, `MetaData.HasCompletedTutorial` is set to `true`, and the meta save is written (`SaveKind.Manual` — out-of-run, Ch.12.5).
  4. `MetaHub` loads — the player is now introduced to the meta hub.
- **Re-entry forbidden** — once `HasCompletedTutorial == true`, the `MainMenu → Tutorial` transition is rejected by the transition guard (logged as an invalid transition, Ch.25). A profile that has completed the tutorial always goes to `MetaHub`/`LoadingRun`. Replaying the tutorial (if ever desired) would require a separate upper-layer "replay tutorial" action that explicitly bypasses the guard; the foundation does not provide this.

### 14.4 Room Transitions (within InRun)

Room-to-room transitions stay **inside** the `InRun` state — they do not go through `GameFlow`. The `RoomController` (Ch.13) additively loads/unloads `Room_Generic` / `Room_Boss` scenes while `GameFlow.CurrentState` remains `InRun`. Only true-death/victory (→ `MetaHub`), pause (→ `Paused`), or quit (→ `MetaHub`/`MainMenu`) escalate to `GameFlow`. Downs (Ch.5.6) also stay inside `InRun` — the player respawns at the room-entry checkpoint without a `GameFlow` transition.

### 14.5 Red Lines

1. `GameFlow`, `GameState`, the transition table, `SceneLoader`, and the persistent-systems registry are locked.
2. Subsystems react to phase changes via `OnStateChanged` (Ch.3); polling `GameFlow.CurrentState` is forbidden.
3. Persistent systems are pre-authored in the `Persistent` scene and initialized once on scene load; gameplay actors never live in the `Persistent` scene.
4. Room transitions do not escalate to `GameFlow`; only true-death/victory, pause, or quit do.
5. No direct `SceneManager.LoadScene` outside `SceneLoader` — all scene ops route through `SceneLoader` via the `OnRequestSceneLoad(SceneLoadRequest)` event (Ch.3).
6. The `Tutorial` state is **one-time per profile**, gated by `MetaData.HasCompletedTutorial`. The `MainMenu → Tutorial` transition is rejected once the flag is `true`; the flag is set exactly once, on tutorial completion, and never reset.
7. The tutorial is a **preset scripted stage**, not a `RoomSO`. It must not participate in the Ch.13 room/wave/spawner system, must not create a `RunData`, and must not write to `WeaponCodex` or any other meta-progression field except `HasCompletedTutorial`.
8. The `Persistent` scene is loaded additively by `Initializer` and **never unloaded** during a play session. Adding code that unloads `Persistent` is forbidden.
9. The `GameManager` scene is loaded on `MainMenu → anything else` and **unloaded only on return to `MainMenu`**. Intermediate transitions (Tutorial → MetaHub, MetaHub → LoadingRun, Room → Room) keep `GameManager` loaded.
10. Only the `Initializer` scene may use `LoadSceneMode.Single`. All other scene loads must use `LoadSceneMode.Additive`. Violating this would wipe `Persistent` or `GameManager`.
11. No `DontDestroyOnLoad` is used on individual managers. Managers persist because their scenes never unload, not because of `DontDestroyOnLoad`.
12. `GameFlow` must stay in the `Persistent` scene — it manages `MainMenu` transitions and must exist before `GameManager` loads. `GameManager` components must not assume `GameFlow` is in their scene.
13. `SceneLoader` lives in `Persistent` and is the **only** legitimate scene-loading path. Gameplay code must never call `SceneManager.LoadScene` / `SceneManager.UnloadSceneAsync` directly.
14. `GameFlow.RequestTransition()` validates all transitions via `IsValidTransition()` — any transition not in the guarded table is rejected and logged. There is no `RunEnd` state; run-end goes through `InRun → MetaHub`.

---

## Chapter 15 — Pickup / Drop System

> **Implemented snapshot:** deterministic rolls hash run seed, source ID, and room depth and use
> `System.Random`. Drop tables directly reference `WeaponSO` because no item registry exists. Currency
> pickups are magnetic and pooled; weapon pickups are pooled, interaction-only, and replace the selected
> replacement-target slot. `PlayerInteractor` selects the nearest valid target and owns one configurable Interact action,
> disabled during dialogue. Authored pools/tables/prompts and source event wiring remain pending.

> Layer 🟧: the foundation defines the pickup/drop framework (`IPickup`, `DropTable`, `DropSystem`). There is **no inventory/backpack** — pickups resolve immediately on pickup (weapon → directly replaces the selected Replacement Target slot, currency → run currency). Upper layer authors drop tables and pickup prefabs. Namespace: `Zephyr.Core.Items` (framework) + `Zephyr.Gameplay.Items` (authored).

### 15.1 Drop Model (no inventory — weapons + currency only)

There is no item storage. Drops are **weapons** (→ 2-slot `WeaponSet`, Ch.9) and **currency** (→ `RunData.RunCurrency`, Ch.12). A picked-up weapon directly replaces the selected Replacement Target slot; the replaced weapon is discarded. Currency is added immediately.

```csharp
namespace Zephyr.Core.Items
{
    public enum DropKind { Weapon, Currency }

    [Serializable]
    public struct DropEntry
    {
        public DropKind Kind;
        public ItemId WeaponPoolId;            // Weapon: which authored weapon pool to roll from
        public float Weight;
        public int CurrencyMin, CurrencyMax;   // Currency
        public float LuckQualityBonus;         // multiplies rarity roll (Ch.5.10)
    }

    [CreateAssetMenu(menuName = "Zephyr/DropTable")]
    public class DropTableSO : ScriptableObject
    {
        public DropEntry[] Entries;
        public int RollCount;                  // independent rolls
    }

    public struct DropResult { public DropKind Kind; public ItemId WeaponId; public WeaponRarity RolledRarity; public int CurrencyAmount; }

    public static class DropSystem
    {
        // RNG seeded by RunData.Seed + drop context (enemy id / room depth) → SL-safe (Ch.12.5)
        public static DropResult[] Roll(DropTableSO table, DropContext ctx, float luck);
    }
}
```

- Drops spawn as pooled pickups in the world (Ch.8); a pickup is a small pooled entity with a `PooledObject` component (implementing `IPooledObject`) handling pool lifecycle.
- `Luck` shifts quality/rarity rolls upward via `LuckQualityBonus` (Ch.5.10 binding): weapon drops and shop refreshes both use the player's Luck.
- Drop RNG is `Seed + context`-deterministic: the same enemy in the same run drops the same loot on reload (Ch.12.5).

### 15.2 Pickup Entities (two classes: currency = magnetic; weapon = interaction-button)

Pickups split into two pooled classes with **different collection semantics**: currency is auto-collected via magnet; weapons require the interaction button (Ch.18 `OnInteract`). Both have a `PooledObject` component (Ch.8.1) for pool lifecycle management and self-return on lifetime expiry (Timer, Ch.20) — same self-return contract as projectiles (Ch.10.4).

```csharp
// Currency pickup: magnet to player on contact radius, auto-collect.
// Has PooledObject component for OnSpawn/OnDespawn/ReturnToPool.
public sealed class CurrencyPickup : MonoBehaviour
{
    public void ResetState();                       // reset sprite, state (called after OnSpawn)
    public void Spawn(Vector2 pos, int amount);
    public void OnPickedUp(IInteractor actor);      // add amount → RunData.RunCurrency, then ReturnToPool()
}

// Weapon pickup: NOT magnetic. Must be collected via IInteractable + interaction button.
// Has PooledObject component for OnSpawn/OnDespawn/ReturnToPool.
public sealed class WeaponPickup : MonoBehaviour, IInteractable   // Ch.2.4
{
    public void ResetState();                       // reset sprite, highlight state (called after OnSpawn)
    public void Spawn(Vector2 pos, ItemId weaponId, WeaponRarity rarity);

    // IInteractable — triggered by player's OnInteract press (Ch.18) when in range
    public void OnInteract(IInteractor actor);      // replaces Replacement Target (Ch.9.4), then ReturnToPool()
    public bool CanInteract(IInteractor actor);     // true if actor is within interaction range (≠ magnet radius)
}
```

- **CurrencyPickup — magnetic contact collect.** When the player enters an attraction radius, the pickup homes toward the player (kinematic, not physics-driven) and collects on contact → `RunData.RunCurrency += amount`. No button press.
- **WeaponPickup — interaction-button collect.** No magnet; the pickup does not move toward the player. When the player is within `GameSettings.Combat.WeaponPickupRange` (small, near-the-body range — much smaller than currency magnet):
  - An interaction prompt UI is shown (Ch.16 floating prompt above pickup, e.g. "[E] PICKUP").
  - Player presses the interaction button → `OnInteract` event fires → `PlayerInteractor` calls `NearestInteractable().Interact(player)` → weapon replaces the selected Replacement Target slot (Ch.9.4; replaced weapon is discarded).
- `OnPickedUp` resolves **immediately** with no storage — no inventory/backpack (Ch.15 binding).

### 15.2a Interaction System (doors, ladders, weapon pickups)

The same interaction button (Ch.18 `OnInteract`) drives doors (Ch.13.5), ladders/climbables, and weapon pickups. One shared `IInteractable` pipeline (Ch.2.4):

```csharp
// Player-side component — scans for nearest in-range IInteractable each frame,
// shows a prompt when one exists, and forwards OnInteract to it when pressed.
public sealed class PlayerInteractor : MonoBehaviour
{
    public IInteractable Nearest;      // current in-range target (null if none)
    public float InteractionRange;     // small, near-body; tuned per GameSettings
}
```

- **Doors** (Ch.13.5): unlocked doors are `IInteractable` — press interact to enter. Locked doors return `CanInteract = false` and show a locked state. Walking through an unlocked door also triggers it (shortcut; interact press still works).
- **Ladders/climbables**: climb zones are `IInteractable` — press interact to enter/exit climb state (Ch.6 StateTag `Climb`).
- **Weapon pickups** (`WeaponPickup`, above): `IInteractable` — press interact to pick up and replace the selected Replacement Target slot.
- Interaction prompt UI (Ch.16) is driven by `PlayerInteractor.Nearest` being non-null. One prompt at a time; `Nearest` is the minimum-distance valid target.

**Input binding (Ch.18 `InputReader.OnInteract`)** is the single shared input for all three — no separate door-button / ladder-button / pickup-button. If the player is in range of a weapon pickup AND standing on an unlocked door, `PlayerInteractor.Nearest` picks the closer one (deterministic: min distance).

### 15.2b Why weapons are not magnetic

Weapons are high-signal pickups: picking one up discards the weapon in the selected Replacement Target slot (Ch.15 binding). Auto-collecting a weapon via magnet would mean the player can accidentally discard a weapon they want to keep just by walking near a drop. Requiring the interaction button means the choice is deliberate — the player presses a key only when they want to replace a slot.

Currency, by contrast, is low-signal (always beneficial, never a trade-off), so magnet auto-collect is a QoL win and the default.

### 15.3 Acquisition Sources

- **Enemy drops**: on `OnEnemyDied` (Ch.4.10), roll the enemy's `DropTableSO` (Ch.11.2) and spawn pickups at the death position.
- **Room rewards**: on `OnRoomCleared` (Ch.13.4), roll the room's `RewardSpec.DropTable`.
- **Shop**: a shop room (Ch.13 `RoomType.Shop`) offers weapons for run currency; refresh rolls are Luck-weighted (Ch.5.10). The shop sells weapons only — no item stock.
- **Boss rooms**: guarantee a weapon drop (Ch.9.5) via `RewardSpec.GuaranteesWeapon`.

### 15.4 Red Lines

1. `DropTableSO`, `DropSystem`, `DropResult`, `CurrencyPickup`, `WeaponPickup`, `PlayerInteractor` are the locked framework; AI may add `DropTableSO` **assets**, not new framework types.
2. **No inventory/backpack** exists; pickups resolve immediately. Adding item storage is forbidden (design decision).
3. Weapons never stack beyond the 2-slot `WeaponSet` (Ch.9); the replaced weapon is discarded.
4. Drop RNG is `Seed + context`-deterministic (Ch.12.5); no ad-hoc RNG.
5. Pickups are pooled (Ch.8) and self-recycle; direct `Instantiate` of pickup prefabs is forbidden.
6. Two-class collection semantics is locked:
   - **CurrencyPickup = magnetic contact-collect** (no button press). Adding a magnet to weapon pickups is forbidden (design decision — 15.2b).
   - **WeaponPickup = interaction-button only** (no magnet, no contact-collect). Weapon pickups must implement `IInteractable` and be collected via `OnInteract` press.
7. Doors, ladders/climbables, and weapon pickups share the **single `OnInteract` input** (Ch.18). Adding a dedicated per-type interaction input (separate door-key / pickup-key) is forbidden; `PlayerInteractor.Nearest` disambiguates.
8. `WeaponPickup` is always `IInteractable`; weapon pickups never fire `OnPickedUp` on contact with the player (no auto-pickup).

---

## Chapter 16 — UI Architecture

> **Implemented snapshot:** runtime UI uses `UnityEngine.UI` with a 960x540 CanvasScaler. MainMenu
> builds the four-button shell, settings, three save slots, and reusable confirmation popup. Gameplay
> HUD lives in GameManager, appears only while a player exists, polls health/resources/currency, and
> displays both weapon icons. Runtime-generated text selects a CJK-capable OS font. Generic panel stack
> and floating combat text are not implemented.

> Layer 🟧: the foundation defines the UI framework (`UIManager`, `UIPanel` base, floating-text system, event-driven binding). Upper layer authors specific panels. Namespace: `Zephyr.Core.UI` (framework) + `Zephyr.Gameplay.UI` (panels).

### 16.1 UI Manager & Panel Base

```csharp
namespace Zephyr.Core.UI
{
    public static class UIManager
    {
        public static T Open<T>() where T : UIPanel;     // pushes onto panel stack
        public static void Close(UIPanel panel);
        public static void CloseTop();
        public static bool IsOpen<T>();
    }

    public abstract class UIPanel : MonoBehaviour
    {
        protected virtual void OnOpen() { }              // subscribe events here (Ch.3)
        protected virtual void OnClose() { }             // unsubscribe here — never leak
        protected virtual void OnRefresh() { }           // called when bound data changes (event-driven)
    }
}
```

- Panels form a **stack**: opening a modal pushes; closing pops and resumes the one beneath. `Pause` (Ch.14) opens the pause panel as a modal overlay, freezing gameplay.
- Panels subscribe to events in `OnOpen` and **must** unsubscribe in `OnClose` (Ch.3.3 binding) — a leaked subscription is a red-line violation.
- UI never polls gameplay state; it refreshes only on events (`OnStatChanged`, `OnResourceChanged`, `OnWeaponChanged`, etc.).

### 16.2 Panel Catalog

| Panel | Trigger | Layer |
|-------|---------|-------|
| HUD (HP/Stamina/Energy, minimap) | always-on during InRun | 🟩 authored |
| Floating Text (damage/heal/crit) | `OnDamageDealt` / `OnHealed` events (Ch.4/3) | 🟧 framework + 🟩 styles |
| Stats panel | player input | 🟩 authored |
| Dialogue | `DialogueTrigger` or story beat queue (Ch.16.6) | 🟧 framework + 🟩 authored |
| Pause menu | `GameFlow → Paused` (Ch.14) | 🟩 authored |
| Main menu / slot select | `MainMenu` state | 🟩 authored |
| Meta-hub upgrade UI | `MetaHub` state (Ch.22) | 🟩 authored |
| Shop UI | entering a shop room (Ch.13/15) | 🟩 authored |

### 16.3 Floating Text System (multi-type, Q21)

```csharp
public static class FloatingTextSystem
{
    // Pooled text popups; type selects color/font/size preset.
    public static void Spawn(Vector2 worldPos, string text, FloatingTextType type);
}

public enum FloatingTextType { NormalDamage, CritDamage, BackstabDamage, Heal, PotentialDamage, Element, Currency, Miss }
```

- Floating text is pooled (Ch.8) — damage numbers are high-frequency, never `Instantiate`d per hit.
- `OnDamageDealt` (Ch.3/4) → `FloatingTextSystem.Spawn` with type derived from `DamageInfo` (crit → `CritDamage`, potential-erosion → `PotentialDamage`, element → `Element`).
- Text rise/fade is a simple tween; no physics. Lifetime is short and self-recycling.

### 16.4 HUD Binding

- HUD subscribes to `OnStatChanged` / `OnResourceChanged` / `OnPotentialChanged` (Ch.5.8) and updates bars reactively.
- Minimap (if present) subscribes to `OnRoomCleared` / `OnRoomEntered` (Ch.13) to reveal visited rooms.
- HUD is part of the `InRun` scene set; it is hidden in `MainMenu` / `MetaHub` (driven by `GameFlow.OnStateChanged`, Ch.14).

### 16.5 Localization Hook

All user-facing strings route through `LocalizationManager` (Ch.27) via a key, never hardcoded. Panels call `Loc.Get("key")` (or a UIDocument string-reference) at open/refresh. zh + en locales are both supported (Q22).

### 16.6 Dialogue System

> Layer 🟧: the foundation defines the dialogue framework (`DialogueSO`, `DialogueManager`, `DialogueTrigger`, `DialogueView`). Upper layer authors `DialogueSO` assets and places triggers in scenes. Namespace: `Zephyr.Core.Dialogue` (framework) + `Zephyr.Gameplay.Dialogue` (authored).

Dialogue is a localized two-person conversation system with trigger-based activation, progression gating, and automatic input-map switching. It reuses the same `DialogueSO` data for both in-world trigger dialogue and meta-hub story beats.

#### 16.6a Dialogue Data Model

```csharp
namespace Zephyr.Core.Dialogue
{
    public enum DialogueSpeaker { Left, Right }

    [CreateAssetMenu(menuName = "Zephyr/Dialogue/Dialogue")]
    public class DialogueSO : ScriptableObject
    {
        public string Id;                                        // unique identifier for save tracking
        public Participant LeftParticipant;
        public Participant RightParticipant;
        public DialogueLine[] Lines;                             // ordered pages of dialogue
    }

    [Serializable]
    public struct Participant
    {
        public LocalizedString LocalizedName;                   // Unity Localization reference
        public string FallbackName;                              // authoring fallback
        public Sprite Portrait;                                  // frameless 160x160 portrait sprite
    }

    [Serializable]
    public struct DialogueLine
    {
        public DialogueSpeaker Speaker;                          // Left or Right participant
        public LocalizedString Text;                             // Unity Localization table entry
        public string FallbackText;                              // authoring fallback
    }
}
```

- Create dialogue assets from `Create > Zephyr > Dialogue > Dialogue` and store in `Assets/ScriptableObjects/Dialogue/`.
- Duplicate `ExampleDialogue.asset` as a starting template.
- Only the active speaker's portrait and name are shown; the inactive side is hidden. Portrait slots are fixed at 160 x 160 reference pixels, preserve aspect ratio, and sit at the corresponding screen edge.

#### 16.6b Dialogue Trigger (scene placement)

`DialogueTrigger` is a scene-side `MonoBehaviour` with a `Collider2D` trigger that activates dialogue when the player enters the area:

1. Create an empty GameObject (e.g. `DialogueTrigger_Intro`) and add `DialogueTrigger`.
2. Unity auto-adds a `Collider2D`; set `Is Trigger = true` and size the collider to the activation area.
3. Assign the target `DialogueSO` to the `Dialogue` field.
4. Place the GameObject at the desired world location. The trigger uses the `Player` tag — no manual player reference needed.
5. Enable `Trigger Once` for one-time story beats. On completion, the dialogue ID is written to `MetaData.TriggeredStoryBeats` and persisted to save (Ch.12.3).

The Tutorial scene contains `DialogueTrigger_Example` at `(3, 0, 0)` using `ExampleDialogue` as a reference.

#### 16.6c Progress Gating (optional fields)

Each trigger supports optional progression requirements. All filled requirements are ANDed — all must be satisfied for the trigger to fire.

| Field | Source | Effect |
|-------|--------|--------|
| `Required Objective Id` | `MetaData.FulfilledObjectives` | Requires the exact objective ID to be fulfilled |
| `Required Story Beat Id` | `MetaData.TriggeredStoryBeats` | Requires the exact story beat ID to have been triggered |
| `Minimum Run Stage` | `RunData.LevelProgress.CurrentStage` | Requires the active run's stage to be ≥ this value; `-1` = ignore |
| `Require Completed Tutorial` | `MetaData.HasCompletedTutorial` | Requires tutorial to be finished |

Leave all requirements empty for triggers that fire solely from location. A gated trigger does nothing until a valid save with all requirements is present.

#### 16.6d Dialogue Manager & View Prefab

`DialogueManager` lives in the `GameManager` scene (Ch.14.2) and instantiates the `DialogueView` prefab when a conversation starts.

The view prefab (`Assets/Prefabs/UIPrefabs/Dialogue/DialogueView.prefab`) is a normal editable prefab (not runtime-only drawing). Customize these `Image` sprites directly on the prefab:

| UI Element | Purpose |
|------------|---------|
| `Backdrop` | Full-screen overlay |
| `LeftPortrait` / `RightPortrait` | Frameless 160 x 160 portrait slots |
| `DialogueBox` | Dialogue panel background |
| `ContinueIndicator` | Advance-prompt marker |

Text objects (`LeftSpeaker`, `RightSpeaker`, `Body`, `Counter`) can be restyled or replaced while keeping their references on `DialogueView`. Name labels are aligned to their participant side. `AdvanceButton` is transparent and covers the dialogue box so pointer input advances the sequence.

The prefab uses a `CanvasScaler` reference resolution of **960 x 540** and a sorting order of **200**, so portraits render above gameplay HUD and above the dialogue box.

#### 16.6e Runtime Flow

```
DialogueTrigger detects player (with tag check + gating)
        │
        ▼
DialogueManager.TryStart(dialogueSO)
        │
        ├─ Instantiate DialogueView prefab
        ├─ Resolve Unity Localization LocalizedString references for names and text
        ├─ Switch input: disable GamePlay map → enable Dialogue map
        ├─ Clear player's cached movement/sprint/jump/roll/crouch/attack inputs
        │
        ▼
Per NextLine input ── advance one DialogueLine ──► update speaker/portrait/text
        │
        ▼ (last line completed)
Invoke trigger callback → record dialogue ID to TriggeredStoryBeats (if Trigger Once)
        │
        ├─ Destroy DialogueView
        └─ Switch input: disable Dialogue map → re-enable GamePlay map
```

Every completion, explicit close, component disable, and manager-destruction path disables the `Dialogue` map and restores the `GamePlay` map. If a player is enabled while dialogue is already open, gameplay input remains disabled until that dialogue closes.

#### 16.6f Input Integration

Dialogue input comes from the `Dialogue` Action Map in `Assets/Settings/Input/Zephyr.inputactions`. `DialogueManager` implements the generated `IDialogueActions` interface and advances only when `NextLine` is performed. The current default binding is **Space**; add keyboard/gamepad alternatives to `NextLine` in the Input Actions editor rather than hard-coding keys.

See Ch.18 for the complete action-map switching rules during dialogue.

#### 16.6g Meta Hub Story Beat Playback

On `MetaHub` entry, `StoryProgressionSystem.EvaluatePendingBeats(meta)` runs (Ch.14.3). Any pending story beats queue their conversations for sequential playback via `DialogueManager` before MetaHub interaction unlocks. These conversations reuse the same `DialogueSO` data model and `DialogueView` prefab as in-world triggers.

### 16.7 Red Lines

1. `UIManager`, `UIPanel`, `FloatingTextSystem`, `DialogueManager`, `DialogueSO`, `DialogueTrigger` are the locked framework; AI may add panels and `DialogueSO` assets, not new framework types.
2. Panels unsubscribe every event subscribed in `OnOpen` during `OnClose` (Ch.3.3).
3. UI never polls; refresh is event-driven only.
4. Floating text and HUD elements are pooled (Ch.8); per-frame `Instantiate` of UI elements is forbidden.
5. No hardcoded user-facing strings; all text via `LocalizationManager` (Ch.27) or Unity Localization `LocalizedString`.
6. Dialogue input switching is the exclusive domain of `DialogueManager`; gameplay code must not manually enable/disable the Dialogue action map.
7. Dialogue IDs used with `Trigger Once` must be unique across the project to avoid save-state collisions.
8. The `DialogueView` prefab is the single source of dialogue UI; creating alternate dialogue UI prefabs is forbidden (restyle the existing prefab instead).

---

## Chapter 17 — Audio System

> Layer 🟧: the foundation defines the audio framework (`AudioManager`, `AudioEventSO`, buses). Upper layer authors `AudioEventSO` assets and music tracks. Namespace: `Zephyr.Core.Audio` (framework) + `Zephyr.Gameplay.Audio` (authored).

### 17.1 Architecture

`AudioManager` is a persistent system (lives in the `Persistent` scene, Ch.14.2). It owns three buses — `SFX`, `Music`, `UI` — each with independent volume/mute.

```csharp
namespace Zephyr.Core.Audio
{
    public enum AudioBus { SFX, Music, UI }

    public static class AudioManager
    {
        public static void Play(AudioEventSO evt, Vector2? worldPos = null);  // null = non-spatial (UI/music)
        public static void Stop(AudioEventSO evt);
        public static void SetBusVolume(AudioBus bus, float volume);
        public static void MuteBus(AudioBus bus, bool muted);
    }

    [CreateAssetMenu(menuName = "Zephyr/Audio/Event")]
    public class AudioEventSO : ScriptableObject
    {
        public AudioClip[] Clips;       // randomly picked per play
        public AudioBus Bus;
        [Range(0,1)] public float Volume;
        public float PitchMin, PitchMax; // random pitch range
        public bool Loop;
        public int Priority;             // higher = survives voice eviction
    }
}
```

### 17.2 Event-Driven Playback (binding)

Gameplay code **never** calls `AudioManager.Play` directly. Playback is triggered by subscribing to Ch.3 events:

| Event | Audio |
|-------|-------|
| `OnDamageDealt` | hit SFX (typed by `DamageType`/element) |
| `OnEnemyDied` | enemy death SFX |
| `OnPlayerDowned` | down SFX |
| `OnRoomCleared` | room-clear stinger |
| `OnRoomEntered` | ambient bed swap |
| `OnWeaponChanged` | equip SFX |
| `OnPotentialDepleted` | death music sting |

This keeps audio decoupled: a system that raises an event gets audio for free; the audio layer never imports gameplay types.

### 17.3 Music & State Transitions

- Music crossfades on `GameFlow.OnStateChanged` (Ch.14): `MainMenu` → menu theme, `MetaHub` → hub theme, `InRun` → biome theme. Run-end death/victory sting plays during `InRun → MetaHub` transition.
- Within `InRun`, room-type changes (Ch.13) swap the ambient bed (shop/rest vs combat).
- Music transitions are crossfade-timed via the Timer service (Ch.20), never instantaneous cuts.

### 17.4 Voice Pooling

- SFX play through a pool of `AudioSource`s (Ch.8). When all voices are busy, the lowest-priority playing voice is evicted.
- High-frequency SFX (hit, footstep) are pooled; one-shot `PlayOneShot` is acceptable for non-spatial UI blips only.
- Music uses two dedicated `AudioSource`s (crossfade pair), not the SFX pool.

### 17.5 Red Lines

1. `AudioManager`, `AudioEventSO`, `AudioBus` are the locked framework; AI may add `AudioEventSO` **assets**, not new framework types.
2. Gameplay code must not call `AudioManager.Play` directly; route through Ch.3 events.
3. SFX `AudioSource`s are pooled (Ch.8); per-play `Instantiate` of audio sources is forbidden.
4. `AudioManager` is persistent (lives in `Persistent` scene, Ch.14.2); it is never re-created per scene.

---

## Chapter 18 — Input System

> **Implemented snapshot (supersedes the `InputReader` SO proposal below):** the input asset has
> `GamePlay` and `Dialogue` maps. GamePlay exposes Move, Sprint, Roll, Jump, Crouch, PrimaryAttack, and
> SecondaryAttack through player-owned `PlayerInputReader`; Dialogue exposes NextLine. DialogueManager
> disables gameplay input and enables Dialogue for the conversation lifetime. Interact is one
> configurable runtime action owned by `PlayerInteractor`. Pause/UI/MetaHub maps remain planned.

> Layer 🟥: locked foundation. Namespace `Zephyr.Core.Input`. Uses Unity's new Input System (Q24). Input is decoupled from gameplay via an `InputReader` SO that broadcasts intent events.

### 18.1 InputReader

```csharp
namespace Zephyr.Core.Input
{
    public enum InputMap { Gameplay, UI, MetaHub, Dialogue, None }

    [CreateAssetMenu(menuName = "Zephyr/Input/Reader")]
    public class InputReader : ScriptableObject
    {
        public void EnableMap(InputMap map);
        public void EnableGameplay() => EnableMap(InputMap.Gameplay);
        public void EnableUI()      => EnableMap(InputMap.UI);
        public void EnableDialogue() => EnableMap(InputMap.Dialogue);

        // Intent events — rebroadcast through the Ch.3 event bus
        public event Action<Vector2> OnMove;
        public event Action OnJump, OnDash, OnRoll, OnPrimaryAttack, OnSecondaryAttack, OnInteract;
        public event Action OnPause, OnStats;
        public event Action OnNextLine;   // Dialogue advance (Ch.16.6f)
    }
}
```

- A single `InputReader` asset is the bridge between the Input System and the game. It owns the `InputActionAsset` and exposes intent as C# events that are rebroadcast on the Ch.3 event bus.
- Gameplay code subscribes to **intent events** (via the bus), never to raw `InputAction` callbacks.

### 18.2 Action-Map Switching (binding)

Action maps switch with `GameFlow` state (Ch.14) **and** with transient UI modes like dialogue:

| Context | Active map |
|---------|------------|
| `InRun` (normal gameplay) | `Gameplay` |
| `Paused` | `UI` (Gameplay disabled) |
| `MetaHub` | `MetaHub` |
| `MainMenu` | `UI` |
| Dialogue active (Ch.16.6) | `Dialogue` (Gameplay disabled) |

`GameFlow.OnStateChanged` drives `InputReader.EnableMap` for state transitions. Dialogue overrides are managed exclusively by `DialogueManager` (Ch.16.6f) — it disables `GamePlay` and enables `Dialogue` when a conversation starts, and restores `GamePlay` on completion. Pausing disables the Gameplay map so gameplay intent events stop firing while a modal is open.

### 18.3 Red Lines

1. `InputReader`, `InputMap` are locked; AI may not modify.
2. Gameplay code reads intent via the Ch.3 event bus, never via direct `InputAction`/`InputReader` refs (except the bootstrap that wires the reader to the bus).
3. Action-map switching for state transitions is driven solely by `GameFlow.OnStateChanged` (Ch.14). Dialogue map switching is driven solely by `DialogueManager` (Ch.16.6f); gameplay code must not manually enable/disable maps itself.

---

## Chapter 19 — Camera System (Hollow Knight-style)

> **Implemented snapshot (supersedes `CameraZoneSO` below):** camera authorship is scene-local.
> `CameraBounds` defines room bounds, default behavior, and priority custom regions. `[ExecuteAlways]`
> `CameraZone` objects are editable rectangles with Follow, VerticalLock, HorizontalLock,
> VerticalCenter, and FullLock modes plus offset, look-ahead, anchor, and priority. CameraController
> provides deadzone follow, smoothed velocity look-ahead, viewport clamping, and edge-jitter
> suppression. Shake is not implemented.

> Layer 🟥: locked foundation. Namespace `Zephyr.Core.Camera`. A **deadzone-based** follow camera with **soft** room bounds and **authored camera zones** that reframe the view when the player enters them (Hollow Knight / Metroidvania-style, not Dead-Cells-style rigid confinement).

### 19.1 CameraController

```csharp
namespace Zephyr.Core.Camera
{
    /// <summary>Deadzone-based follow camera. Lives in the GameManager scene (not persistent).
///  Computes the camera target in LateUpdate after gameplay position is finalized.</summary>
    public sealed class CameraController : MonoBehaviour
    {
        public void SetFollow(Transform target);                         // bind the player (init + respawn)
        public void SetRoomBounds(Rect worldBounds);                    // SOFT per-room bounds (may exceed viewport) — Ch.19.2
        public void SnapTo(Vector2 worldPosition);                      // instant reposition (room transition / telegraph)
        public void ApplyCameraZone(CameraZoneSO zone);                  // reframe to zone's FollowOffset + SoftBounds (Ch.19.3)
        public void ResetToRoomBounds();                                // exit zone framing; revert to room-level soft bounds
    }
}
```

- `CameraController` owns the main `Camera` component and runs all motion in `LateUpdate` (after `Update`/`FixedUpdate` resolve gameplay position). The camera **never writes gameplay transforms** — it only reads the follow target.
- Frame-rate independent damping (`Mathf.SmoothDamp` with `Time.deltaTime`) is always applied — no transform-translate snapping (except explicit `SnapTo` on room transitions / telegraphs).

### 19.2 Deadzone Follow Model (binding — the core behavior)

The camera does **not** center on the player every frame. Instead, it has a **deadzone** (rectangular region around the current camera center, tunable in `GameSettings.Camera.DeadzoneSize`). While the player stays within the deadzone, the camera **does not move**. When the player exits the deadzone on any axis, the camera **re-centers on the player's current position**, and the deadzone moves with the camera center.

```
Visual intuition (top-down view of camera + deadzone + player):

        ┌────────── Viewport ──────────┐
        │                              │
        │    ┌─────── Deadzone ───┐    │
        │    │                      │    │
        │    │        @player       │    │     ← player in deadzone: camera STAYS
        │    │                      │    │
        │    └──────────────────────┘    │
        │                              │
        └──────────────────────────────┘

Player exits deadzone → camera snaps to player → deadzone re-centers on new camera position →
player may now move freely inside the new deadzone without triggering camera movement.
```

- **Why deadzone, not follow + damping** — the "follow with damping" model the old spec used produces constant camera drift on every player input (the camera *always* lags the player slightly). Deadzone camera stays rock-still when the player stands still in a small region, then moves decisively when the player commits to leaving it. This is the HK feel.
- **Vertical deadzone** — deadzone also applies on Y (gravity axis). Jumping/descending in place does not move the camera; only a large upward/downward commitment does. Critical for platforming.
- **Soft room bounds** — room bounds are **soft** (not hard). If the player stands at the edge of a large room, the camera can still be pushed gently against the room's `Rect` world bounds, but never renders outside (clamped, not snapped). This means rooms can be larger than the viewport — the camera scrolls with the player within the room's soft bounds. **"Camera is room-confined" is removed from the red lines** — the camera is softly bounded by the current room's world rect, not hard-clamped by the viewport.

### 19.3 Authored Camera Zones (per-room reframing triggers)

A room may contain one or more **camera zones** — authored trigger volumes that temporarily override the follow behavior when the player enters them. This is how HK frames boss arenas, tight corridors, and platforming setups without room transitions.

#### 19.3a CameraZoneSO (authored data)

```csharp
namespace Zephyr.Core.Camera
{
    [CreateAssetMenu(menuName = "Zephyr/Camera/Zone")]
    public class CameraZoneSO : ScriptableObject    // upper layer
    {
        public Rect TriggerRect;                   // world-space trigger rect for zone activation
        public Vector2 FollowOffset;               // offset applied to camera center relative to player (e.g. boss-arena centers player-low)
        public Rect SoftBounds;                    // per-zone soft viewport bounds (tighter than room bounds for corridors / boss frames)
        public float DeadzoneMultiplier;           // >1 = larger deadzone (calmer), <1 = smaller (more responsive)
        public int Priority;                       // zone with highest priority wins if overlaps
    }
}
```

#### 19.3b CameraZoneTrigger (scene-side MonoBehaviour bridge)

Each zone in the room scene is a trigger collider (or distance volume) driven by a small `MonoBehaviour` that holds a reference to a `CameraZoneSO`. It detects the player's entry/exit and fires the event (Ch.3):

```csharp
// Placed in the room scene's camera zone trigger volumes.
public sealed class CameraZoneTrigger : MonoBehaviour
{
    public CameraZoneSO Zone;                    // the authored zone this trigger drives

    void OnTriggerEnter2D(Collider2D other)      // or distance check in Update if not using physics
    {
        if (other.CompareTag("Player"))
            EventBus.Raise(GameEvents.OnEnterCameraZone, Zone);   // CameraController subscribes
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            EventBus.Raise(GameEvents.OnExitCameraZone, Zone);
    }
}
```

- **Zone activation** — `CameraZoneTrigger` entry raises `OnEnterCameraZone(zone)`. `CameraController` subscribes and calls `ApplyCameraZone(zone)`, which applies the zone's `FollowOffset`, `SoftBounds`, and `DeadzoneMultiplier`.
- **Zone exit & overlapping-zone re-evaluation** (binding) — when the player **exits** a zone, the controller must **not** immediately revert to room-level framing. Instead, it re-evaluates all **currently active** zones (by `Priority`). If at least one zone remains active, the controller applies the highest-priority remaining zone's framing. Only when **zero** zones are active does it call `ResetToRoomBounds()`. This prevents a player walking from one overlapping zone to another (or stepping through an exit zone while still inside a higher-priority one) from experiencing a camera glitch.
- **Multiple zones per room** — zones coexist within a single `RoomSO`. Entering a boss sub-area within a large room reframes the camera without a room transition. `CameraZoneSO.Priority` resolves overlapping zones deterministically.
- **No "room = camera"** — a `RoomSO` does **not** define the camera framing of the entire room. The room provides the outer soft bounds; zones define the framing inside specific sub-areas.
- **Zone activation does not escalate to `GameFlow`** (Ch.14) — it stays entirely inside `InRun`.

### 19.4 Camera Shake

The foundation exposes a simple shake API for upper-layer triggers (on-hit, boss slam, impact):

```csharp
public sealed class CameraController : MonoBehaviour
{
    // Applies a time-decaying screen offset. Multiplies the default shake falloff.
    public void Shake(float intensity, float duration);
}
```

- Shake runs in `LateUpdate` (after deadzone follow + soft-bounds clamping). It is additive with the deadzone result and decays linearly over `duration` seconds.
- Shake does **not** write the camera transform directly — it drives a per-frame screen-space offset that is added inside the `LateUpdate` compute chain.

### 19.4a Look-Offset (deferred to upper layer)

The old spec had `SetLookAhead` (camera offset in movement direction). This is **intentionally removed from the foundation API** — the deadzone model naturally eliminates most need for look-ahead (camera moves decisively when the player commits to leaving the deadzone, not continuously). Subtle directional peek (e.g. player holding Up to peek at an upper platform, as in HK) is an **upper-layer extension** — the upper layer can add a small offset to the `FollowOffset` passed into `ApplyCameraZone` or drive a per-frame offset from outside `CameraController` without modifying the foundation API. If the project later decides peek behavior needs first-class support, it can be added via a new `SetLookOffset(Vector2)` method without breaking the deadzone contract.

### 19.5 Room Lifecycle Integration (Ch.13.4)

- **Room enter**: `RoomController` calls `SetRoomBounds(currentRoom.WorldBounds)`. The camera snaps to the player's spawn position via `SnapTo`; deadzone resets to default.
- **Room during play**: camera zones activate/deactivate via `OnEnterCameraZone`/`OnExitCameraZone` events (Ch.3). `CameraController` applies each zone's framing via `ApplyCameraZone`/`ResetToRoomBounds`.
- **Room exit**: new room loaded → `SetRoomBounds` with new room bounds → `SnapTo` on the new spawn position.
- **Player teleports** (respawn at room-entry checkpoint, Ch.5.6): `SnapTo` to the checkpoint position; no shake.

### 19.6 Red Lines

1. `CameraController`, `CameraZoneSO` are the locked framework; AI may add `CameraZoneSO` **assets**, not new framework types.
2. **Deadzone-based follow is binding** — replacing it with a rigid follow (camera always centers on player) is forbidden (design decision — 19.2).
3. Camera is **soft-bounded** by the current room's world bounds, not hard-clamped to viewport. Rooms may exceed the viewport size; the camera scrolls within the room.
4. Camera motion runs in `LateUpdate`; gameplay position is finalized in `Update`/`FixedUpdate` — camera never writes gameplay transforms.
5. **Camera zones are authored trigger volumes** that reframe the view without room transitions. Entering a zone does not escalate to `GameFlow` (Ch.14); it stays inside `InRun`.
6. Shake is additive and time-decaying; it never bypasses the deadzone follow / soft-bounds chain.
7. No direct `Camera.main.transform.position = ...` writes outside `CameraController` — all camera motion routes through the controller.

---

## Chapter 20 — Timer / Coroutine Service

> **Implemented snapshot:** `Zephyr.Core.Timer.TimerService` is a Persistent MonoBehaviour singleton.
> `SetTimeout`/`SetInterval` return allocated `TimerHandle` objects and use coroutines. Gameplay timers
> are scaled; UI timers use unscaled time. The old static pooled no-coroutine API below is superseded.

> Layer 🟥: locked foundation. Namespace `Zephyr.Core.Timers`. Centralized timing used by projectiles, pickups, buffs, DoT, and audio crossfades. Replaces ad-hoc per-`MonoBehaviour` coroutines.

### 20.1 Timer API

```csharp
namespace Zephyr.Core.Timers
{
    public static class Timer
    {
        public static TimerHandle After(float seconds, Action callback);   // one-shot
        public static TimerHandle Every(float interval, Action callback);  // repeating
        public static void Cancel(TimerHandle handle);
        public static void CancelAll(ref List<TimerHandle> handles);       // bulk cancel (for pooled objects)
    }

    public struct TimerHandle { public bool IsValid; /* ... */ }
}
```

- `Timer` is a persistent system (lives in `Persistent` scene, Ch.14.2). Handles are pooled structs — no per-call allocation.
- `After`/`Every` are the only timing primitives gameplay code may use.

### 20.2 No Coroutines (binding)

Per-`MonoBehaviour` `StartCoroutine` is **forbidden** for gameplay timing (Ch.29). Rationale: coroutines are tied to the `MonoBehaviour` instance, break on disable/destroy, and are invisible to the pause system. The centralized `Timer` is pause-aware and pooled-object-safe.

### 20.3 Integration Points

- **Projectiles** (Ch.10.4): `Timer.After(Lifetime, Recycle)`.
- **Pickups** (Ch.15.2): `Timer.After(Lifetime, Recycle)`.
- **Buffs** (Ch.7): `Timer.After(duration, Expire)` + `Timer.Every(tickInterval, ApplyTick)` for DoT.
- **Audio** (Ch.17.3): `Timer.After(crossfadeTime, ...)` for music swaps.

### 20.4 Pause & Time Scaling

- When `GameFlow → Paused` (Ch.14), gameplay timers (tagged `TimerDomain.Gameplay`) pause; UI timers (`TimerDomain.UI`) continue on unscaled time.
- Pooled objects must cancel their handles in `Recycle`/`Init` (Ch.8.4) to avoid firing callbacks on a recycled instance.

### 20.5 Red Lines

1. `Timer`, `TimerHandle` are locked; AI may not modify.
2. `StartCoroutine` for gameplay timing is forbidden (Ch.29); use `Timer`.
3. Pooled objects cancel their timer handles on recycle (Ch.8.4).
4. Gameplay timers pause with `GameFlow`; UI timers run on unscaled time.

---

## Chapter 21 — Animation Conventions

> Layer 🟥: locked foundation. Namespace conventions only (no framework type). Governs how the Ch.6 StateMachine drives `Animator` and how animation events feed back into gameplay.

### 21.1 Animator Structure

- One `Animator` per actor. Parameters are driven by `StateAction`s (Ch.6), never by gameplay code calling `anim.SetBool` directly outside a `StateAction`.
- Parameters: `Triggers` for state entries (e.g. `Attack`, `Hurt`, `Death`), `Bools`/`Floats` for continuous values (e.g. `Speed`, `Grounded`, `AttackStep`).
- The `AnimatorController` is a visual layer; it is **not** authoritative for game logic. The Ch.6 StateMachine is authoritative.

### 21.2 StateMachine ↔ Animator (binding)

- On `State.OnStateEnter`, the active `StateAction` sets the corresponding anim trigger/bool. This is the only place anim parameters are written.
- `StateCondition`s may read anim-state (via `Animator.GetCurrentAnimatorStateInfo`) for transition gating — e.g., "attack anim finished" gates the Attack→Idle transition. This keeps hit-frame timing data-driven by the animation, not by hardcoded timers.
- StateMachine state names and Animator state names need not match 1:1; the coupling is via the `StateAction`'s trigger set, not by name.

### 21.3 Animation Events (hit frames)

- Damage is applied at the correct animation frame via **Animation Events**: a keyframe event calls a method on the attack `StateAction` (e.g., `ApplyHit()`), which runs `WeaponDamageBuilder` (Ch.9.6) and resolves the hit.
- This binds damage timing to the visual swing, not to a timer. The `StateAction` owns the logic; the Animation Event only signals "now".
- VFX spawn (hit sparks, trails) is fired the same way — Animation Event → `StateAction` → pooled VFX spawn (Ch.8).

### 21.4 Red Lines

1. Animator parameters are written only inside `StateAction`s (Ch.6); direct `anim.SetBool` from non-StateAction gameplay code is forbidden.
2. The `Animator` is visual, not authoritative; game logic lives in `StateAction`s, never in `StateMachineBehaviour` scripts.
3. Damage/VFX timing is driven by Animation Events into `StateAction`s; hardcoded timer-based hit windows are forbidden.
4. `AnimatorController` assets are upper-layer authored; the StateMachine→Animator coupling convention (this chapter) is locked.

---

## Chapter 22 — Meta Game / Run-End Settlement

> **Implemented snapshot:** IDs follow the existing save/dialogue architecture and remain strings.
> `RunEndSettlement` commits currency, acquired/equipped weapons, and pending objectives exactly once,
> saves MetaData, then clears RunData. Meta upgrade and mechanic unlock SO/systems implement purchases,
> prerequisite checks, and permanent records. Objective/story-beat systems stage in-run objectives,
> commit at settlement, order eligible beats by priority, and reuse localized two-person `DialogueSO`.
> MetaHub purchase/story UI and authored content remain pending. The legacy `ConversationSO` model below
> is superseded by `DialogueSO`.

> Layer 🟧: the foundation defines the settlement + meta-upgrade framework. Upper layer authors specific `MetaUpgradeSO` assets. Namespace: `Zephyr.Core.Meta` (framework) + `Zephyr.Gameplay.Meta` (authored upgrades).

### 22.1 Settlement Flow

On true death (`OnPotentialDepleted`, Ch.5.6) or victory, `GameFlow` transitions `InRun → MetaHub` (Ch.14.3). During this transition, `RunEndSettlement.Settle` runs — committing currency, codex, and story objectives to meta, and processing mechanic unlock purchases — then meta is saved and the player arrives at `MetaHub`.

```csharp
namespace Zephyr.Core.Meta
{
    public static class RunEndSettlement
    {
        // Commits run results into meta. Called once per run end. Respects Ch.12.1 (the only meta-write path besides settings).
        public static void Settle(RunData run, MetaData meta);
    }
}
```

### 22.2 Currency Conversion

`RunData.RunCurrency` is added in full to `MetaData.PermanentCurrency`. This is the sole source of permanent currency (Hades-like, Q14). Run currency does not carry over as-is — only its converted value persists.

### 22.3 Potential: No Cross-Run Erosion (binding)

Potentials are **run-local** (Ch.5.2): each run starts from a fresh random allocation of N points and erodes on downs (Ch.5.6). **Nothing potential-related is committed to `MetaData` at settlement** — there is no cross-run death-spiral. The `MetaData.PotentialFloor` concept (a non-zero meta baseline) is **removed**; a run cannot "start already-dead" because every run re-randomizes its potentials from the budget N (which is ≥ 1 once any budget upgrade is purchased, and ≥ the starting `PotentialBudgetBase` otherwise).

The only meta levers on potentials are the two budget upgrades in 22.5 (`PotentialBudgetUpgrades` grows N, `PotentialAllocatableUpgrades` grows M). Settlement therefore touches only: currency (22.2), codex (22.4), mechanic unlocks (22.6), and story objectives (22.8).

### 22.4 Weapon Codex Unlock

Any weapon type picked up for the first time this run is added to `MetaData.WeaponCodex` (Ch.12.3). Each new codex entry grants a small permanent stat bonus (magnitude tuned in GameSettings; intentionally small per Q14). The codex is monotonic — it only grows, never shrinks.

### 22.5 Meta Upgrades (MetaHub)

In the `MetaHub` state (Ch.14), the player spends `PermanentCurrency` on upgrades. Each upgrade is a `MetaUpgradeSO` authored by the upper layer; the framework applies the purchased effect to `MetaData`:

| Upgrade target | MetaData field | Effect |
|----------------|----------------|--------|
| Base stat | `BaseStatUpgrades` | raises char intrinsic Base at next run start |
| Potential budget (N) | `PotentialBudgetUpgrades` | more run-entry potential points (system-random N−M baseline + player-allocated M; Ch.5.2) |
| Potential allocatable (M) | `PotentialAllocatableUpgrades` | more of N the player manually allocates (M ≤ N) |
| Combat (crit/pen) | `CombatAcquiredUpgrades` | player-own crit + dual pen (Ch.12.3) |
| Damage multiplier tier | `UnlockedMultipliers` | extra PercentMult tier (Ch.5.4) |
| Mechanic unlock (Ch.22.6) | `UnlockedMechanics` | permanently unlocks gameplay capabilities (e.g. DoubleJump) that gate StateMachine transitions |

```csharp
public static class MetaUpgradeSystem
{
    public static bool CanPurchase(MetaUpgradeId id);
    public static bool Purchase(MetaUpgradeId id);   // spends PermanentCurrency, applies to MetaData
}
```

Upgrades are **persistent** — once purchased they apply to every subsequent run. Purchases write `MetaData` immediately (MetaHub is out-of-run, so the Ch.12.1 no-mid-run-write rule does not apply).

### 22.6 Mechanic Unlocks (gated gameplay capabilities)

**Mechanic unlocks** are a distinct meta-upgrade type that permanently grants the player **gameplay capabilities** rather than stat bonuses. Examples: Double Jump, Air Dash, Roll, Wall Jump, Charged Attack. These are **state machine transition gates** — the player's StateMachine (Ch.6) cannot transition to certain states until the corresponding mechanic is unlocked.

```csharp
namespace Zephyr.Core.Meta
{
    public struct MechanicUnlockId { public int Value; }

    /// <summary>An authored mechanic unlock upgrade. Gates a gameplay capability behind a prerequisite objective.</summary>
    [CreateAssetMenu(menuName = "Zephyr/Meta/MechanicUnlock")]
    public class MechanicUnlockSO : ScriptableObject       // upper layer
    {
        public MechanicUnlockId Id;
        public string DisplayNameKey;                      // localization key (Ch.27)
        public string DescriptionKey;                      // localization key for the prerequisite description
        public int Cost;                                   // PermanentCurrency cost
        public ObjectiveId RequiredObjective;              // prerequisite: must be in MetaData.FulfilledObjectives (Ch.22.8)
                                                           // e.g. "OnKillEnemyInMidAir" → unlocks DoubleJump
    }

    public static class MechanicUnlockSystem
    {
        // Runtime query — called by MechanicUnlockedCondition (Ch.6) to gate StateMachine transitions.
        // Reads MetaData.UnlockedMechanics. Never reads MetaData directly from gameplay code.
        public static bool IsUnlocked(MechanicUnlockId id);

        // Purchase flow (MetaHub only, via MetaUpgradeSystem.Purchase).
        // CanPurchase checks both currency AND prerequisite fulfillment.
        public static bool CanPurchase(MechanicUnlockSO unlock, MetaData meta);
        public static bool Purchase(MechanicUnlockSO unlock, MetaData meta);
    }
}
```

- **Prerequisite system** — each mechanic unlock has a `RequiredObjective` that must be in `MetaData.FulfilledObjectives` (Ch.22.8) before purchase is allowed. Example:
  - `DoubleJump` unlock → prerequisite objective: `OnKillEnemyInMidAir` (fulfilled when the player kills any enemy while airborne).
  - Player can see the unlock in the MetaHub upgrade UI even before the prerequisite is met, but it is greyed out with the prerequisite description visible.
  - Once the prerequisite is fulfilled (via the objective system, Ch.22.8), the unlock becomes available for purchase at the next MetaHub entry.
- **How it gates gameplay** — the player's `TransitionTableSO` (Ch.6) uses `MechanicUnlockedConditionSO` (Ch.6) as a condition on the relevant transition. Example: the `Jump → DoubleJump` transition requires `MechanicUnlockedConditionSO { MechanicId = DoubleJump }`. Until the mechanic is unlocked, the transition never fires — the player can jump once but never enter the `DoubleJump` state.
- **Mechanic unlocks are permanent and monotonic** — once purchased, they're in `MetaData.UnlockedMechanics` forever. No refunds, no removal.
- **Runtime query path** — gameplay code (StateMachine conditions) calls `MechanicUnlockSystem.IsUnlocked(id)`, which internally reads `MetaData.UnlockedMechanics`. **Gameplay code must never read `MetaData.UnlockedMechanics` directly** — it must go through the system.

### 22.7 Story Progression (meta-persistent objectives → conversations)

A **story progression** system tracks meta-persistent **objectives** fulfilled by the player across the profile's lifetime and uses them to gate **conversations** (linear dialogue, no branching) that play in the `MetaHub` state. Story beats fire **once per profile** — Hades-like long-arc narrative.

```csharp
namespace Zephyr.Core.Meta
{
    public struct ObjectiveId { public int Value; }
    public struct StoryBeatId { public int Value; }
    public struct ConversationId { public int Value; }

    /// <summary>An authored objective. Fulfilled by a named event raised during a run (or in MetaHub).</summary>
    [CreateAssetMenu(menuName = "Zephyr/Story/Objective")]
    public class ObjectiveSO : ScriptableObject        // upper layer
    {
        public ObjectiveId Id;
        public string FulfillEventId;                  // event id that fulfills this (e.g. "OnFirstBossKill", "OnStageCleared:1", "OnFirstWeaponPickup:Dagger")
    }

    /// <summary>An authored story beat: when all RequiredObjectives are fulfilled and this beat hasn't fired, play its Conversation.</summary>
    [CreateAssetMenu(menuName = "Zephyr/Story/StoryBeat")]
    public class StoryBeatSO : ScriptableObject        // upper layer
    {
        public StoryBeatId Id;
        public ObjectiveId[] RequiredObjectives;       // ALL must be in FulfilledObjectives for the beat to fire
        public ConversationSO Conversation;            // linear dialogue to play (Ch.22.8)
        public int Priority;                           // higher = evaluated first when multiple beats are pending
    }

    public static class StoryProgressionSystem
    {
        // Raised during a run (or MetaHub) when an objective is fulfilled.
        // Mid-run: appends to RunData.PendingObjectives (NOT meta — Ch.12.1 no-mid-run-write).
        // In MetaHub: commits directly to MetaData.FulfilledObjectives (out-of-run, allowed).
        public static void FulfillObjective(ObjectiveId id);

        // Called at run-end settlement (Ch.22.1): moves RunData.PendingObjectives into MetaData.FulfilledObjectives.
        public static void CommitRunObjectives(RunData run, MetaData meta);

        // Called on MetaHub entry (Ch.14.3): returns the ordered queue of conversations to play.
        // A beat fires iff (all RequiredObjectives ∈ FulfilledObjectives) AND (beat.Id ∉ TriggeredStoryBeats).
        public static ConversationId[] EvaluatePendingBeats(MetaData meta);

        // Called when a conversation completes: records the beat as fired so it never repeats.
        public static void MarkBeatTriggered(MetaData meta, StoryBeatId beatId);
    }
}
```

- **Objectives are monotonic** — once fulfilled, they stay fulfilled for the profile's lifetime. `MetaData.FulfilledObjectives` only grows.
- **Story beats fire once** — `TriggeredStoryBeats` prevents repeats. A beat that has already fired is never re-evaluated.
- **Two fulfillment paths**:
  - **In-run events** (e.g. first boss kill, reached stage 3): `FulfillObjective` appends to `RunData.PendingObjectives` (run-local). At run-end settlement, `CommitRunObjectives` moves them into `MetaData.FulfilledObjectives`. Conversations do NOT play mid-run — they play on the next MetaHub entry.
  - **MetaHub events** (e.g. first codex open, first upgrade purchased): `FulfillObjective` commits directly to `MetaData.FulfilledObjectives` (out-of-run, Ch.12.1 allows it). The next `EvaluatePendingBeats` pass picks them up.
- **No branching / no choices** — conversations are a linear sequence of lines (Ch.22.8). Branching dialogue is explicitly out of scope (upper-layer extension only).

### 22.8 Conversation System (linear dialogue, no branching)

A **conversation** is an ordered sequence of `DialogueLine`s — speaker name, portrait, localized text, and an auto-advance duration. No choices, no branching. The `DialoguePlayer` is a `UIPanel` (Ch.16) that plays one conversation at a time.

```csharp
namespace Zephyr.Core.Meta
{
    [CreateAssetMenu(menuName = "Zephyr/Story/Conversation")]
    public class ConversationSO : ScriptableObject     // upper layer
    {
        public ConversationId Id;
        public DialogueLine[] Lines;                   // sequential, played in order
    }

    [Serializable]
    public struct DialogueLine
    {
        public string SpeakerNameKey;                  // localization key (Ch.27) — empty = narrator
        public Sprite Portrait;                        // speaker portrait; null = no portrait
        public string TextKey;                         // localization key (Ch.27) for the line text
        public float Duration;                         // auto-advance seconds; 0 = wait for player advance-input
    }
}
```

- The `DialoguePlayer` panel (upper layer, `Zephyr.Gameplay.UI`) subscribes to `OnPlayConversation(ConversationId)` from the event bus (Ch.3), resolves the `ConversationSO` from an asset registry, and plays lines sequentially.
- **Advance**: if `Duration > 0`, the line auto-advances after `Duration` seconds (Timer, Ch.20); the player may also press the advance input to skip. If `Duration == 0`, the line waits for the advance input.
- **Localization**: all text is keyed (Ch.27); the `DialoguePlayer` resolves keys at render time. No hardcoded strings.
- **No input lock during dialogue** — MetaHub dialogue plays in the `MetaHub` state where there is no gameplay tick to freeze; the player simply can't interact with MetaHub NPCs/upgrade UI while a conversation is playing (the panel is modal). This is why conversations are MetaHub-only (per design decision) — no need to pause `InRun`.

### 22.9 Story Flow Integration

```
Run (InRun):
  gameplay event → EventBus.OnObjectiveFulfillRequested(eventId)
                  → StoryProgressionSystem.FulfillObjective(id)
                  → RunData.PendingObjectives.Add(id)            // run-local, NOT meta

Run-end settlement (Ch.22.1):
  RunEndSettlement.Settle(run, meta):
    1. Currency conversion (22.2)
    2. Codex unlock (22.4)
    3. StoryProgressionSystem.CommitRunObjectives(run, meta)     // PendingObjectives → FulfilledObjectives
    4. Clear RunData

MetaHub entry (Ch.14.3, after InRun → MetaHub OR fresh MetaHub load):
  1. StoryProgressionSystem.EvaluatePendingBeats(meta) → ConversationId[] queue
  2. If queue non-empty: DialoguePlayer plays them in priority order, one at a time.
     On each conversation complete: StoryProgressionSystem.MarkBeatTriggered(meta, beatId).
  3. Meta save written (out-of-run write, Ch.12.5).
  4. Once queue is empty: MetaHub interaction unlocked for the player.
```

- **MetaHub entry is the single evaluation point** — beats are not re-evaluated mid-conversation. The queue is computed once on entry; conversations play sequentially; the player gets MetaHub control back when the queue drains.
- **First-ever MetaHub entry** (post-tutorial, Ch.14.3a): the same evaluation runs — any objectives fulfilled during the tutorial (if the tutorial reports any) or seed beats (objectives pre-fulfilled at profile creation) trigger their conversations here.

### 22.10 Red Lines

1. `RunEndSettlement`, `MetaUpgradeSystem`, `MechanicUnlockSystem`, `StoryProgressionSystem` are the locked framework; AI may add `MetaUpgradeSO`/`MechanicUnlockSO`/`ObjectiveSO`/`StoryBeatSO`/`ConversationSO` **assets**, not new framework types.
2. Potentials are run-local (Ch.5.2); **no potential data is committed to meta at settlement** — no cross-run erosion.
3. The only meta levers on potentials are `PotentialBudgetUpgrades` (N) and `PotentialAllocatableUpgrades` (M); M ≤ N is enforced.
4. In-run potential restoration is forbidden (Ch.5.6) — downs only ever erode; the run-end lever is true death (potential ≤ 0).
5. The weapon codex is monotonic (grows only).
6. **Mechanic unlocks are permanent and monotonic** — `MetaData.UnlockedMechanics` only grows, never shrinks. Runtime gating of gameplay capabilities must go through `MechanicUnlockSystem.IsUnlocked(id)` — gameplay code must never read `MetaData.UnlockedMechanics` directly.
7. `MechanicUnlockedConditionSO` (Ch.6) is the **only** legitimate gameplay gate for mechanic unlocks on StateMachine transitions. Adding a non-mechanic-unlock condition that reads `MetaData.UnlockedMechanics` directly is forbidden.
8. Story objectives are **meta-persistent and monotonic** — `FulfilledObjectives` only grows, never shrinks. Objectives fulfilled mid-run are buffered in `RunData.PendingObjectives` and committed to meta **only at run-end settlement** (Ch.12.1 no-mid-run-write).
9. Story beats fire **once per profile** — `TriggeredStoryBeats` prevents repeats. A fired beat is never re-evaluated.
10. Conversations are **linear, no branching** — `ConversationSO` is a flat sequence of `DialogueLine`s. Adding branching/choices to the foundation is forbidden (upper-layer extension only).
11. Conversations play **only in the `MetaHub` state** — never mid-run. In-run objective fulfillment is recorded silently and surfaces as dialogue on the next MetaHub entry.
12. `DialoguePlayer` is a modal `UIPanel` (Ch.16); while a conversation is playing, MetaHub interaction is blocked. The queue is evaluated once on MetaHub entry and drains sequentially.

---

## Chapter 23 — Global Constants & Config (split)

> Layer 🟥: the two-tier constants model is locked. Namespace: `Zephyr.Core.Constants`. The split between **code-locked** `GameConstants` and **data-tunable** `GameSettings` is binding — getting it wrong either freezes balance knobs in source (un-tunable) or lets data drift into logic (un-auditable).

### 23.1 Two-Tier Model

| Tier | Type | Location | Mutability | Purpose |
|------|------|----------|------------|---------|
| `GameConstants` | `static class` (code) | `Scripts/Core/Constants/GameConstants.cs` | 🔒 source-locked, AI must not edit | Formula coefficients, structural caps, magic numbers that are part of the *definition* of a system (e.g. the `100` in `100/(100+armor)`, `PotentialMax = 10`) |
| `GameSettings` | `ScriptableObject` asset | `ScriptableObjects/GameSettings/` | ✅ tunable by designer in Inspector | Balance knobs, per-stat min/max, rarity tables, drop weights — anything a designer should retune without a code change |

**Decision rule (binding)**: a number goes into `GameConstants` only if changing it changes *what the system is* (a formula coefficient, a structural limit like `MaxConcurrentBuffs`). It goes into `GameSettings` if changing it only changes *how the game feels* (a regen rate, a cost, a weight). When in doubt, prefer `GameSettings` — over-frozen numbers block tuning.

### 23.2 `GameConstants` — Static, Code-Locked

Split into nested static classes, one per domain. Each chapter's "Constants" section (4.11, 5.9, 7.9, 9.9, …) is the authoritative listing for that domain; this section is the **index**.

```csharp
namespace Zephyr.Core.Constants
{
    public static class GameConstants
    {
        public static class Combat    { /* Ch.4.11  */ }   // ShieldLeakFactor, PercentCapMultiplierDefault, ArmorFormulaBase
        public static class Stats     { /* Ch.5.9   */ }   // PotentialMax, PotentialNormalizedDivisor
        public static class Buff      { /* Ch.7.9   */ }   // MaxConcurrentBuffs, DefaultMaxStacks
        public static class Weapons   { /* Ch.9.9   */ }   // ComboIdleResetDefault, MinCritChance, MaxCritChance
        public static class Projectiles { /* Ch.10   */ }  // MaxPierceCount, LifetimeDefault, GravityScaleDefault
        public static class Movement  { /* Ch.18/23 */ }   // i-frame duration, dash/roll distances, jump buffer window
        public static class Physics   { /* Ch.23    */ }   // gravityScale baseline, fixedDeltaTime budget
        public static class Pool      { /* Ch.8     */ }   // default prewarm, hard caps per pool
        public static class Save      { /* Ch.12    */ }   // SchemaVersion, slot count, interrupt-save flag
        public static class Meta      { /* Ch.22    */ }   // codex bonus magnitude (PotentialFloor removed — potentials are run-local)
        public static class Timer     { /* Ch.20    */ }   // min delay resolution
        public static class AI        { /* Ch.11    */ }   // perception ranges, target-acquire cooldown
        public static class UI        { /* Ch.16    */ }   // floating-text lifetime, panel stack depth
        public static class Audio     { /* Ch.17    */ }   // max concurrent voices per mixer group
        public static class Camera    { /* Ch.19    */ }   // shake decay, dead-zone, blend time
    }
}
```

- `const` / `static readonly` only — never instance fields. No mutation API exists.
- Values are referenced as `GameConstants.Combat.ShieldLeakFactor` everywhere; no local copies (avoids drift when a constant is retuned in source).
- Adding a new constant requires editing source (a 🟥 Core file) — which triggers the Ch.30 doc-first review. This is intentional: constants are *part of the design*, not tuning.

### 23.3 `GameSettings` — SO, Tunable

A single `GameSettingsSO` asset (with nested sub-SOs) holds all designer-tunable data. Loaded once in `Boot` (Ch.14), exposed as a read-only service.

```csharp
[CreateAssetMenu(menuName = "Zephyr/GameSettings")]
public class GameSettingsSO : ScriptableObject
{
    public CombatSettingsSO   Combat;     // per-stat Min/Max clamps, crit floor/ceil
    public StatsSettingsSO    Stats;      // regen curves, potential gain coefficients
    public DropSettingsSO     Drops;      // rarity weights, luck quality curve
    public WeaponSettingsSO   Weapons;    // per-rarity affix-count table
    public BuffSettingsSO     Buffs;      // conflict table, category stack caps
    public AISettingsSO       AI;         // per-enemy-tier stat scale (Armor/MR/HP, Ch.11.4)
    public LocalizationSettingsSO Localization; // active locale, fallback locale
}
```

- All numeric tuning goes here. Hardcoding a tunable in a domain script is a red-line violation (Ch.29).
- `GameSettingsSO` is **read-only at runtime** — gameplay code reads via `GameSettings.Current.Combat.CritFloor`, never writes. Writing is editor-only.
- Sub-SOs are plain `[Serializable]` data holders; no logic. Logic stays in the system that *consumes* the data (e.g. `DamageCalculator` reads `Combat`, doesn't live in `CombatSettingsSO`).

### 23.4 Per-Stat Min/Max Clamps (binding)

Per Ch.5.9, stat min/max clamps live in `GameSettings` (not `GameConstants`), because they are balance knobs, not formula coefficients. The clamp is applied in `StatsCore` after all modifiers resolve:

```csharp
// In StatsCore, after final value computed:
float clamped = Mathf.Clamp(finalValue,
    GameSettings.Current.Stats.GetMin(type),
    GameSettings.Current.Stats.GetMax(type));
```

- `PotentialMax = 10f` is a `GameConstant` (structural — the potential scale is definitionally 0..10, Ch.5).
- `MaxHp` clamp is a `GameSettings` value (tunable — how tanky is "max tanky" is a feel question).

### 23.5 Red Lines

1. `GameConstants` source is 🟥 locked; AI must not edit it without a doc-first review (Ch.30).
2. `GameSettings` is the **only** place for tunable numbers; hardcoding tunables in domain scripts is forbidden.
3. `GameSettings` is read-only at runtime; no gameplay code may write it.
4. The constant-vs-setting decision rule (23.1) is binding; ambiguous values default to `GameSettings`.
5. No domain may declare its own ad-hoc constants file; all constants funnel through `GameConstants.<Domain>`.

---

## Chapter 24 — Update Loop / Execution Order

> Layer 🟥: the tick model and execution order are locked. Pause semantics, physics-vs-logic phase separation, and persistent-scene update ownership are all binding — they are what make "freeze gameplay on pause" (Ch.14.3) and "camera follows finalized position" (Ch.19) actually work.

### 24.1 Three Update Phases

| Phase | Method | Owns | Notes |
|-------|--------|------|-------|
| Physics | `FixedUpdate` | `MovementCore`, `Projectile` Rigidbody2D integration, knockback resolution | Runs at fixed `Time.fixedDeltaTime`; all motion authority lives here (constitution §IV, Ch.6.8, Ch.10.2). |
| Logic | `Update` | `StateMachine` tick (Ch.6), `Timer` service (Ch.20), input intent (Ch.18), buff expiry checks | `StateMachine.Tick()` is called once per frame on the active state; `StateAction`s run here. |
| Follow | `LateUpdate` | Camera follow (Ch.19), floating-text follow, VFX attach | Reads finalized player position; never writes motion. |

- `StateAction`s that need to apply motion must call `MovementCore.Move(dir)` / `Jump()` (queued, applied in next `FixedUpdate`) — they must **not** write `Rigidbody.velocity` directly (Ch.6.8).
- `Timer` callbacks fire in `Update`, after the StateMachine tick, so timed expiry (buffs, projectile lifetime) is observed by the same frame's state logic where possible.

### 24.2 Script Execution Order (Project Settings → Script Execution Order)

Only the **foundation** systems get explicit order values; domain scripts default to 0 and must not rely on inter-domain ordering (cross-domain = event bus, Ch.3).

| Order | Script | Reason |
|-------|--------|--------|
| -1000 | `Initializer` (scene bootstrap) | Loads `Persistent` scene additively; waits for `PersistentReady`; unloads self |
| -900 | `EventBus` | Subscribers in later scripts must find a ready bus (lives in `Persistent` scene) |
| -800 | `SceneLoader` | Scene loading must be available before GameManager/destination scenes request loads (lives in `Persistent` scene) |
| -700 | `PoolRegistry` | Spawners in `Start` need pools prewarmed (lives in `Persistent` scene) |
| -600 | `SaveSystem` | Run/Meta data must be loaded before gameplay reads it (lives in `Persistent` scene) |
| -500 | `GameFlow` | Global FSM transitions Boot→MainMenu before UI spawns (lives in `Persistent` scene) |
| -400 | `Timer` service | Buffs/projectiles schedule into it on `Start` (lives in `Persistent` scene) |
| -300 | `LocalizationManager` | UI panels localize on open (lives in `Persistent` scene) |
| -200 | `GameManager bootstrap` | Initializes GameManager-scene managers (`CameraController`, `UIManager`, `ProjectileSystem`) and fires `GameManagerReady` |
| 0 | (default) | All domain scripts — order among them must not matter |
| +1000 | `CameraController` (`LateUpdate`) | Reads finalized positions (lives in `GameManager` scene) |

- Domain scripts **must not** request custom execution order values. If two domain scripts seem to need ordering between them, the dependency is missing an event — fix the architecture, not the order.
- `CameraController` is the only exception on the positive side, because `LateUpdate` ordering between cameras and other late-follow scripts is genuinely load-bearing.

### 24.3 Pause Semantics (binding — implements Ch.14.3 InRun → Paused)

`GameFlow` exposes a `PauseScope` that, when active, sets `Time.timeScale = 0` and signals `Timer` to skip gameplay-scheduled callbacks. Concretely:

| System | On Pause |
|--------|----------|
| `Time.timeScale` | `0` — freezes `Update`-driven gameplay motion and `FixedUpdate` (no physics ticks) |
| `StateMachine` (player + enemies) | Frozen — tick is a no-op while `PauseScope` is active |
| `Timer` | Gameplay-scheduled callbacks (buffs, DoT, projectile lifetime) are suspended; **UI-scheduled** callbacks (panel animations, input-repeat) continue on unscaled time |
| `AudioManager` | Continues (menu SFX, music duck) — uses `AudioSettings.timescaleIndependent` |
| `UIManager` | Continues — pause overlay animates on unscaled time |
| `CameraController` | Continues (so the pause overlay can have camera-relative effects if needed) |

- The split is enforced by `Timer` exposing two schedulers: `Timer.Gameplay` (timeScale-bound) and `Timer.UI` (unscaled). Schedulers pick the right one at schedule time; gameplay code must never use `Timer.UI` for gameplay timing.
- Resuming restores `Time.timeScale = 1` and unfreezes `Timer.Gameplay`. Pending gameplay callbacks resume — they do **not** fire all-at-once on resume.

### 24.4 Scene Lifecycle & Update Ownership

The game has two tiers of persistent managers:

- **Persistent-scene managers** (infrastructure — always loaded): `EventBus`, `SaveSystem`, `SceneLoader`, `PoolRegistry`, `Timer`, `AudioManager`, `LocalizationManager`, `GameFlow`. These tick every frame from boot to shutdown.
- **GameManager-scene managers** (gameplay-specific — loaded for Tutorial/MetaHub/InRun): `CameraController`, `UIManager`, `ProjectileSystem`, combat subsystems. These tick only when gameplay is active. When `GameManager` is unloaded (returning to `MainMenu`), these managers are destroyed and their tick stops completely.

Scene-bound gameplay actors (player, enemies, rooms, tutorials) live in additively-loaded scenes that are destroyed on scene unload.

**Rules for cross-scene references:**
- **Persistent-scene managers must not hold direct references to GameManager or scene-bound GameObjects** — they subscribe/unsubscribe via the event bus (Ch.3), not direct refs. Example: `Timer` in `Persistent` cancels callbacks via `OnRecycle` events when gameplay objects are destroyed (Ch.8/20).
- **GameManager-scene managers may hold direct references to scene-bound gameplay actors** (since both are destroyed when GameManager unloads, no dangling refs).
- Scene-bound scripts subscribe in `OnEnable`/`OnDisable` (not `Awake`/`OnDestroy`) so they cleanly detach on scene unload.
- **No `DontDestroyOnLoad` is used on individual managers** — persistence is structural (scene never unloads for Persistent; GameManager stays loaded across gameplay phases), not runtime-managed.

### 24.5 Red Lines

1. The three-phase model (FixedUpdate=physics, Update=logic, LateUpdate=follow) is binding; no script may run physics in `Update` or camera follow in `Update`.
2. Domain scripts must not set custom Script Execution Order; only the foundation list in 24.2 has explicit values.
3. `Timer.Gameplay` vs `Timer.UI` split is binding; gameplay code using `Timer.UI` to dodge pause is forbidden.
4. Persistent-scene managers must not hold scene-bound references across scene loads; communication is event-bus only.
5. `Time.timeScale` is owned by `GameFlow` exclusively; no other script may write it.
6. Only the `Initializer` scene may use `LoadSceneMode.Single`. All subsequent scene loads must be additive (`LoadSceneMode.Additive`). Loading a gameplay scene in single mode would wipe the `Persistent` or `GameManager` scene — forbidden.
7. The `Persistent` scene is never unloaded during a play session. Adding code that unloads it (directly or by loading another scene in single mode) is forbidden.
8. The `GameManager` scene is unloaded **only** on return to `MainMenu`. Unloading `GameManager` during gameplay (Tutorial/MetaHub/InRun) is forbidden — it would destroy `CameraController`, `UIManager`, and `ProjectileSystem` mid-run.
9. `GameFlow` stays in `Persistent` — `GameManager` components must not read `GameFlow.State` directly (use the event bus, Ch.3) and must not assume `GameFlow` lives in their scene.

---

## Chapter 25 — Logging / Debug / Analytics

> Layer 🟥: the logging and debug-cheat model is locked. Namespace: `Zephyr.Core.Logging`. Goals: (1) leveled, filterable logs that survive shipping; (2) editor-only cheats that never ship; (3) minimal analytics — Zephyr is single-player offline, so heavy telemetry is a non-goal.

### 25.1 `LogManager` — Leveled Logging

```csharp
namespace Zephyr.Core.Logging
{
    public enum LogLevel { Debug, Info, Warning, Error }

    public static class LogManager
    {
        // Channel-based filtering: e.g. "Damage", "Save", "AI", "Pool".
        // Channels are toggled in GameSettings (editor) or via PlayerPrefs (runtime console).
        public static void Log(LogLevel level, string channel, string message, Object context = null);

        // Convenience: ZLog.D("Damage", $"hit {target} for {dmg}");
        public static class ZLog
        {
            public static void D(string channel, string msg, Object ctx = null);  // Debug
            public static void I(string channel, string msg, Object ctx = null);  // Info
            public static void W(string channel, string msg, Object ctx = null);  // Warning
            public static void E(string channel, string msg, Object ctx = null);  // Error
        }
    }
}
```

- **Channels** (`Damage`, `Save`, `AI`, `Pool`, `Flow`, `Input`, `UI`, `Audio`, …) let you silence noisy systems without touching code. Channel set is fixed in `GameConstants.Logging.Channels`; new channels require a doc edit (Ch.30).
- **Compile-time stripping**: `Debug`-level calls compile out in shipping builds via `[Conditional("ZEPHYR_DEBUG")]`. `Info`/`Warning`/`Error` always compile.
- `context` is a `UnityEngine.Object` so a console click pings the offending object in the Inspector — critical for debugging pooled entities and SO assets.
- All cross-module red-line violations (invalid transition rejected, pool cap hit, save schema mismatch) log at `Warning`/`Error` with their channel — never silent.

### 25.2 Debug Flags & Cheats (Editor-Only)

```csharp
#if UNITY_EDITOR
public static class DebugCheats
{
    public static bool GodMode;          // IDamageable.IsInvulnerable returns true for player
    public static bool OneHitKill;       // outgoing damage ×= 1000
    public static bool InfiniteStamina;  // PlayerResourceController bypasses Stamina drain
    public static bool InfiniteEnergy;   // PlayerResourceController bypasses Energy drain
    public static bool ShowHitboxes;     // draws hitbox gizmos each frame
    public static bool ShowStateGraph;   // opens the StateMachine debugger (Ch.6.9)
    public static bool DisableAI;        // enemies idle
}
#endif
```

- The entire `DebugCheats` class is `#if UNITY_EDITOR` — it does not exist in player builds. The player-side `IDamageable`/resource checks branch on `#if UNITY_EDITOR && DebugCheats.GodMode` so there is zero shipping cost.
- Cheats are toggled from a debug panel (Ch.16 `UIManager` exposes a dev panel behind a hidden gesture, editor-only).
- Cheats never touch `RunData`/`MetaData` — they are runtime-only state, not persisted. A cheated run is not save-corrupted.

### 25.3 Analytics (minimal, optional)

Zephyr is single-player offline; there is **no mandatory telemetry**. If analytics are ever added (e.g., for the dev's own balance tuning), they are:

- **Opt-in** from the main menu ("Send anonymous balance data to help development"), default off.
- **Batched locally** into `MetaData` (Ch.12.3) and flushed only on explicit user action — no background network calls during gameplay.
- **Schema-versioned** like the save system (Ch.12.4) so old analytics survive a meta-schema bump.
- Limited to aggregate balance signals (run depth reached, death cause by enemy type, weapon pick-rate) — never PII, never gameplay-frame telemetry.

A network analytics SDK is **not** in scope for the foundation. Adding one requires a Ch.30 doc-first review and a new opt-in UI.

### 25.4 Red Lines

1. `LogManager` is the only logging API; direct `Debug.Log` in domain scripts is forbidden (use `ZLog` with a channel).
2. `Debug`-level logs must use `[Conditional("ZEPHYR_DEBUG")]` so they compile out of shipping builds.
3. `DebugCheats` is `#if UNITY_EDITOR` only; any cheat leaking into player builds is a critical bug.
4. No mandatory network analytics; opt-in only, batched, schema-versioned.
5. Red-line violations (invalid transitions, pool cap, save mismatch) must log at `Warning`/`Error` — never silent failures.

---

## Chapter 26 — Testing Strategy

> Layer 🟥: the test scope and boundary are locked. Zephyr is solo-dev + AI; the test strategy is deliberately **EditMode-heavy, PlayMode-light** — pure logic gets unit tests, integration is verified by hand + a small smoke-test suite.

### 26.1 Test Pyramid

| Layer | Assembly | What | Target coverage |
|-------|----------|------|-----------------|
| Unit (EditMode) | `Zephyr.Tests.EditMode` | Pure-logic systems with no Unity dependencies (or only `GameObject`-isolated): `DamageCalculator`, `StatsCore` modifier resolution, `DropSystem` RNG determinism, `SaveSystem` migration, `MetaUpgradeSystem` purchase logic, state-machine condition grouping (Ch.6 `StateTransition.ShouldTransition`) | High — these are the systems where regressions are expensive |
| Integration (PlayMode) | `Zephyr.Tests.PlayMode` | A small smoke suite: boot to MainMenu, start a run, deal damage to a dummy, save+load round-trip, run-end settlement | Low — a handful of tests, run before a build |
| Manual / feel | (none) | Combat feel, animation timing, camera, audio, level pacing | By playthrough; not automatable |

- **Why EditMode-heavy**: EditMode tests are fast (ms), deterministic, and don't need a scene. PlayMode tests load scenes and are 100× slower — reserve them for "does the wiring hold end-to-end" checks.
- **What is NOT tested**: SO asset correctness (weapon balance, drop weights) — these are data, validated by playtesting and a future balance lint tool (out of scope). Visual/audio feel is manual.

### 26.2 What to Test (binding — these are mandatory)

1. **`DamageCalculator`** (Ch.4): every branch — armor/MR reduction, flat/% pen ordering, true damage bypass, shield-break Case A (P<1, no-break and break) and Case B (P≥1, overflow), percent-of-max-hp, crit, variance. This is the highest-regression-cost system.
2. **`StatsCore`** (Ch.5): FlatAdd → PercentAdd → PercentMult order, potential scaling of modifiers, min/max clamp from `GameSettings`, resource bar regen.
3. **`StateTransition.ShouldTransition`** (Ch.6): the zero-condition-true case (B5 fix), grouped AND/OR evaluation, empty table.
4. **`DropSystem.Roll`** (Ch.15): same `Seed + context` produces identical drops across calls (Ch.12.5 determinism); Luck shifts rarity as specified (Ch.5.10).
5. **`SaveSystem`** (Ch.12): round-trip `RunData`/`MetaData` through JSON; schema-version migration from N→N+1 produces equivalent data; interrupt-save is cleared on resume.
6. **`MetaUpgradeSystem.Purchase`** (Ch.22.5): currency spent, effect applied to `MetaData`, idempotency (can't double-purchase a one-shot upgrade).

### 26.3 Test Conventions

- EditMode tests live in `Assets/Scripts/Tests/EditMode/`, PlayMode in `Assets/Scripts/Tests/PlayMode/`. Both reference `Zephyr.Core` (and the domain asmdef under test).
- Tests use `UnityEngine.TestTools` (`[Test]` for EditMode, `[UnityTest]` for PlayMode) — no external test framework (NUnit ships with Unity).
- Each test method names the system + behavior + expectation: `DamageCalculator_WithFlatPen_AppliesBeforePercentPen`.
- Tests build their own `DamageInfo`/`StatModifier` structs inline — no shared fixture state, no test-order coupling.
- RNG-dependent tests seed `DropSystem` explicitly and assert deterministic output, never statistical distributions (flaky tests are worse than no tests).

### 26.4 When Tests Are Required (binding)

- Any change to `DamageCalculator`, `StatsCore`, `StateTransition`, `DropSystem`, `SaveSystem`, `MetaUpgradeSystem` must keep the corresponding tests green or update them in the same change. The CI/PR check (Ch.30) blocks merge on red.
- New foundation systems (a new 🟥 chapter) ship with at least one EditMode test proving its core contract.
- Domain systems (🟧/🟩) are not required to ship with tests, but regressions found in them should be backstopped with a regression test.

### 26.5 Red Lines

1. EditMode tests must not depend on scene load or `Time.timeScale`; they must run in < 100 ms each.
2. PlayMode smoke tests must not assert on feel (timing, animation curves); they assert on state transitions and data round-trips only.
3. RNG tests must be deterministic (explicit seed), never statistical — flaky tests are forbidden.
4. Tests live in `Zephyr.Tests.*` asmdefs and may reference any domain asmdef; domain asmdefs must **not** reference the test asmdefs.
5. No test framework beyond Unity's built-in NUnit; adding an external framework requires a Ch.30 review.

---

## Chapter 27 — Localization (zh + en)

> **Implemented snapshot:** localization is hybrid. Main menu/HUD use the JSON-backed
> `LocalizationManager`; dialogue names/lines use Unity Localization `LocalizedString` with fallbacks.
> Language selection is synchronized to Unity Localization and persisted. Runtime UI font selection
> tries Microsoft YaHei, Noto Sans CJK SC, PingFang SC, Arial Unicode MS, then Arial.

> Layer 🟥: the localization model is locked. Namespace: `Zephyr.Core.Localization`. `LocalizationManager` is a persistent system (lives in the `Persistent` scene, Ch.14.2), initialized at boot, surviving all scene loads. zh-CN and en-US are both first-class (Q22); the system is built to add locales without code changes.

### 27.1 `LocalizationManager`

```csharp
namespace Zephyr.Core.Localization
{
    public enum LocaleId { zh_CN, en_US }   // extensible via GameSettings

    public static class LocalizationManager
    {
        public static LocaleId ActiveLocale { get; }
        public static LocaleId FallbackLocale { get; }   // default en_US

        // Lookup. Returns the key itself if missing (and logs Warning, Ch.25) so misses are visible.
        public static string Get(string key);
        public static string Get(string key, params object[] args);   // formatted: "Hit {0} for {1}"

        // Locale switch — reloads string tables and fires OnLocaleChanged (Ch.3 event bus).
        public static void SetLocale(LocaleId locale);
        public static event Action<LocaleId> OnLocaleChanged;   // UI panels re-render on this
    }
}
```

- All user-facing strings route through `LocalizationManager.Get(key)` — never hardcoded text (Ch.16.5 red line, restated).
- `Get` returns the **key itself** on a miss and logs a `Warning` on the `Localization` channel (Ch.25) so a missing string is immediately visible during play, not silently blank.
- Locale switch fires `OnLocaleChanged` on the event bus; `UIPanel`s subscribe on open (Ch.16) and re-render their text. Mid-run locale switch is supported (Q22).

### 27.2 String Table Format

- Per-locale JSON files: `Resources/Localization/strings.zh_CN.json`, `strings.en_US.json`. JSON for readability and diff-friendliness (project convention).
- Schema: flat key→string map; formatted strings use `.NET`-style `{0}` placeholders so `string.Format` works directly.

```json
{
    "ui.hud.hp.label": "HP",
    "ui.hud.stamina.label": "Stamina",
    "combat.floatingtext.crit": "CRIT!",
    "menu.pause.title": "Paused",
    "enemy.slime.name": "Slime"
}
```

- Keys are dotted, domain-prefixed (`ui.`, `combat.`, `enemy.`, `menu.`, `meta.`, `weapon.`, …) so collisions are obvious and ownership is clear.
- Tables are loaded on `Boot` and on `SetLocale`. Hot-reload in editor is supported via the dev panel (Ch.25.2) for fast iteration.

### 27.3 Font Fallback

- Each locale ships a primary font (zh-CN: a CJK-capable SDF font; en-US: a Latin SDF font).
- A fallback font chain handles missing glyphs (e.g., en-US locale rendering a CJK enemy name) via Unity's `TMP_FontAsset.fallbackFontAssetTable`.
- Font assets are referenced from `LocalizationSettingsSO` (Ch.23.3), not hardcoded — switching locales swaps the font set.

### 27.4 Red Lines

1. No hardcoded user-facing strings; all text via `LocalizationManager.Get`. (Restated from Ch.16.5.)
2. String tables are JSON, key→string; no nested logic, no pluralization engine in v1 (extend later if needed).
3. Locale switch is event-bus-driven; panels re-render on `OnLocaleChanged`, no polling.
4. Missing keys log Warning and return the key — never silent, never throw.
5. Font fallback chain is configured in `LocalizationSettingsSO`, not per-panel.

---

## Chapter 28 — Performance & Budget

> Layer 🟥: the budget table is locked as a *target*, not a guarantee. The red lines are the binding part — the profiling cadence and the per-system budgets that keep a solo-dev project from drifting into un-profilerable territory.

### 28.1 Budget Recap (from Ch.0.5)

| Metric | Target | Hard ceiling |
|--------|--------|--------------|
| Frame rate | 60 FPS | ≥ 30 FPS without stutter |
| Concurrent enemies | ≤ 50 | 50 (design cap; spawn more = bug) |
| Concurrent projectiles | ≤ 200 | 200 (pool cap, Ch.8) |
| Memory | ≤ 8 GB | 8 GB (dev machine 16 GB) |
| Level load time | ≤ 3 s | 5 s |
| GC allocations per frame (gameplay) | 0 | 0 — zero-alloc gameplay tick is a hard red line |

### 28.2 Per-System Budgets

| System | Frame budget | Notes |
|--------|--------------|-------|
| `StateMachine` tick (all actors) | ≤ 0.5 ms | ≤ 50 enemies × cheap condition checks; conditions must be O(1), no `GetComponent` per frame (cache in `Awake`) |
| `DamageCalculator` | ≤ 0.1 ms per hit | No allocations; `in DamageInfo` struct, no LINQ |
| `Timer` service | ≤ 0.2 ms | Sorted callback list, O(log n) insert/remove |
| `ProjectileSystem` | ≤ 1 ms for 200 | Rigidbody-only motion (no per-frame transform writes); pooled |
| `PoolRegistry` | amortized 0 | Allocation only on miss; prewarm covers steady state |
| `AudioManager` | ≤ 0.3 ms | Pooled voices; no per-frame `AudioSource` creation |
| `CameraController` | ≤ 0.2 ms | One `LateUpdate`, no per-frame `GameObject.Find` |
| `UIManager` / floating text | ≤ 0.5 ms | Pooled floating text; no per-frame string allocation |
| `EventBus` dispatch | ≤ 0.2 ms | Direct delegate invocation, no boxing on `readonly struct` payloads (Ch.3) |

- These are *review* budgets — if a Profiler sample shows a system over budget, that's a bug to fix, not a number to retune.
- The 0-allocation gameplay tick is the single most-enforced budget: `DamageInfo`, `StatModifier`, event payloads are all `readonly struct` (Ch.2.7, Ch.3, Ch.7) precisely so the hot path doesn't GC.

### 28.3 Profiling Cadence

| Cadence | Action |
|---------|--------|
| Per feature (during dev) | Author runs Profiler on the new feature's worst case before merge |
| Weekly | Full-run Profiler capture: 60 s of densest gameplay (boss room, 200 projectiles), check frame budget |
| Pre-build | Run the PlayMode smoke tests (Ch.26.1) + a 5-min manual playthrough with Profiler attached; confirm no system over budget |
| On regression | If a frame drop is reported, bisect with the Profiler; do not "optimize by feel" |

- The Profiler is the source of truth, not intuition. A system is "slow" only when the Profiler says so.
- Unity 6000 Profiler markers (`Profiler.BeginSample("Zephyr.DamageCalculator")`) are placed in the foundation systems (Ch.4/5/6/8/10/12) so the Profiler breaks down Zephyr by system, not by Unity-internal frames.

### 28.4 Memory & Loading

- Pools (Ch.8) prewarm at `Persistent` scene load; steady-state allocates nothing. Pool caps are `GameConstants.Pool` hard caps — once full, the oldest instance is recycled (logged Warning, Ch.25).
- Scenes are the load boundary (Ch.1.4, Ch.13). Room transitions within a level are additive + unload, not full scene loads — sub-second.
- Addressables (Ch.1.4) are *not* adopted in v1; direct references suffice for SM scope. If memory pressure appears (Profiler-confirmed), bulk art assets migrate to Addressables — a Ch.30 doc-first change, not a quiet refactor.

### 28.5 Red Lines

1. Zero GC allocations on the gameplay tick (60 FPS hot path); `readonly struct` payloads are binding.
2. Per-system frame budgets (28.2) are review ceilings; exceeding them is a bug.
3. Pool caps are hard; exceeding the cap recycles-oldest + logs Warning, never spawns unbounded.
4. No `GameObject.Find` / `FindObjectWithTag` / `GetComponent` per frame; cache in `Awake`/`OnEnable`.
5. No LINQ in hot paths (`Update`/`FixedUpdate`/damage/timer); LINQ is fine in editor/UI open paths only.
6. Profiler is the source of truth; "optimize by feel" without a Profiler capture is forbidden.

---

## Chapter 29 — AI Coding Rules / Red Lines

> Layer 🟥: these are the binding rules for any AI (or human) collaborator editing the Zephyr codebase. They consolidate the red lines scattered across Ch.1–28 into a single checkable list. Violating any of them is a blocked merge.

### 29.1 Doc-First Rule

- The architecture doc (`Zephyr 底层架构详细设计（草案）.md`) is the source of truth. Code follows the doc; if code and doc disagree, **the doc wins** unless a Ch.30 review explicitly updates the doc.
- Before adding a new cross-module interface, a new event channel, a new pool type, or a new `GameConstants`/`GameSettings` field, the doc must be updated first. PRs that add code without a doc update are blocked.
- The TOC status column (✅/⏳) is the single source of "what's done". A chapter marked ✅ is locked to its described behavior.

### 29.2 Core Immutability (🟥 chapters)

- Files under `Scripts/Core/` (Ch.1.1) are 🟥 locked once their chapter is ✅. AI must **not** edit their source without a Ch.30 doc-first review.
- New features add upper-layer scripts (`Scripts/Player/`, `Scripts/Enemies/`, `Scripts/Weapons/`, …) or new SO **assets** (not new SO **types**) — never foundation intrusions.
- `internal` is the default access modifier in Core; only the explicit public API surface (Ch.2) is `public`. AI widening `internal` to `public` without a doc review is a red-line violation.
- Domain asmdefs (`Zephyr.Player`, `Zephyr.Enemies`, …) depend only on `Zephyr.Core`, never on each other (Ch.1.3). Cross-domain communication is event-bus only (Ch.3).

### 29.3 Coding Red Lines (consolidated)

| # | Rule | Source |
|---|------|--------|
| R1 | No `StartCoroutine` for gameplay timing; use `Timer` (Gameplay vs UI scheduler per Ch.24.3) | Ch.20.5, Ch.24.3 |
| R2 | No direct `Instantiate`/`Destroy` for pooled entities (projectiles, pickups, floating text, audio voices, VFX); use `PoolRegistry` | Ch.8 |
| R3 | No `Rigidbody.velocity` writes from `Update`/`StateAction`; motion goes through `MovementCore` (applied in `FixedUpdate`) | Ch.6.8 |
| R4 | No `Time.timeScale` writes outside `GameFlow` | Ch.24.5 |
| R5 | No hardcoded tunable numbers in domain scripts; tunables go in `GameSettings`, formula coefficients in `GameConstants` | Ch.23 |
| R6 | No hardcoded user-facing strings; use `LocalizationManager.Get` | Ch.16.5, Ch.27 |
| R7 | No `Debug.Log` in domain scripts; use `LogManager.ZLog` with a channel | Ch.25 |
| R8 | No per-frame `GameObject.Find`/`GetComponent`; cache in `Awake`/`OnEnable` | Ch.28 |
| R9 | No LINQ in hot paths (`Update`/`FixedUpdate`/damage/timer) | Ch.28 |
| R10 | No GC allocations on the gameplay tick; event payloads are `readonly struct` or `IDisposable` | Ch.2.7, Ch.3 |
| R11 | No direct cross-domain references; cross-domain = event bus only | Ch.1.2, Ch.3 |
| R12 | No new `GameConstants`/pool/event-channel types without a doc-first review | Ch.23, Ch.8, Ch.3 |
| R13 | No custom Script Execution Order on domain scripts; only the Ch.24.2 foundation list has explicit values | Ch.24.5 |
| R14 | No state-machine transition logic inside `StateAction`s; transitions are declarative SO data | Ch.6 |
| R15 | No `RunData` write to `MetaData` mid-run; meta writes happen only at settlement (Ch.22) or in `MetaHub` (out-of-run) | Ch.12.1 |
| R16 | No ad-hoc RNG for rewards; reward RNG is `Seed + context`-deterministic | Ch.12.5, Ch.15 |
| R17 | No inventory/backpack; pickups resolve immediately | Ch.15 |
| R18 | `DebugCheats` is `#if UNITY_EDITOR` only; no cheats in player builds | Ch.25 |
| R19 | Tests for `DamageCalculator`/`StatsCore`/`StateTransition`/`DropSystem`/`SaveSystem`/`MetaUpgradeSystem` must stay green on any change to those systems | Ch.26 |
| R20 | Profiler is the source of truth for "slow"; no "optimize by feel" | Ch.28 |

### 29.4 Review & Merge

- Every PR is checked against R1–R20 by the author (and the AI reviewer if present) before merge.
- A red-line violation blocks merge regardless of whether the code "works" — the rules exist to keep the architecture survivable across a long solo-dev + AI cadence.
- Breaking a red line to ship a feature requires either (a) a Ch.30 doc-first review that updates the rule, or (b) an explicit, logged, time-boxed exception noted in the PR with a follow-up issue. Silent violations are never acceptable.

---

## Chapter 30 — Versioning & Iteration Rules

> Layer 🟥: the versioning model is locked. It governs three things: (1) the architecture doc's own version + lock state, (2) the save schema version (Ch.12.4), and (3) the protocol for changing a locked chapter. This is the chapter that makes "🟥 locked" actually mean something.

### 30.1 Doc Version

- This document carries a `DocVersion` (semantic-versioned: `MAJOR.MINOR.PATCH`).
  - `MAJOR`: a 🟥 chapter is added, removed, or has its red lines changed. Resets the "lifetime lock" list review.
  - `MINOR`: a 🟧 chapter is added or significantly revised; a 🟥 chapter gains a non-red-line section.
  - `PATCH`: clarifications, typo fixes, cross-reference fixes. No behavioral change.
- The `DocVersion` is recorded at the top of the doc and in `GameConstants.Save.DocVersionExpected` so a build can refuse to load save data authored against an incompatible doc version (separate from `SchemaVersion`, Ch.12.4 — `DocVersion` is the *architecture* version, `SchemaVersion` is the *save format* version).
- The TOC status column tracks per-chapter **review** state (✅ = reviewed/approved, ⏳ = draft/pending review). ✅ does **not** mean locked — it means the user has reviewed the chapter. Formal locking into the lifetime lock list (30.2) is a separate, explicit step.

### 30.2 Lifetime Lock List

The lifetime lock list is the set of **formally-locked** chapters. A chapter enters the list only by an explicit user lock action (separate from the ✅ review mark); once locked, it may only be changed via the lock-change protocol (30.4). As of this version the list is: **empty** — chapters 0–9 are ✅ (reviewed) but not yet formally locked; chapters 10–30 are ⏳ (pending review). The user finalizes locks one chapter (or batch) at a time.

- "Locked" does not mean "perfect" — it means "changes are auditable". A locked chapter can still be revised; it just can't be revised *quietly*.
- The lock applies to the **red lines** and **binding signatures** of a chapter most strongly. Prose clarifications are `PATCH`-level and lighter-weight (30.4).

### 30.3 Save Schema Version (recap of Ch.12.4)

- `RunData` and `MetaData` each carry a `SchemaVersion` (integer, monotonically increasing).
- On load, `SaveSystem` compares the file's `SchemaVersion` to `GameConstants.Save.SchemaVersionCurrent`; if older, migration hooks (N→N+1) run in order.
- A migration that cannot be auto-applied (e.g., a removed field with no equivalent) prompts the user: "This save is from an older version and may not be fully compatible. Continue?" — never silent data loss.
- `SchemaVersion` bumps are `MINOR` doc versions at minimum (a save-affecting change). A `MAJOR` doc version (red-line change) may force a `SchemaVersion` bump.

### 30.4 Lock-Change Protocol

To change a locked chapter:

1. **Propose**: open a doc-change proposal describing (a) which chapter, (b) which red line or binding signature changes, (c) why, (d) downstream impact (code, save schema, tests).
2. **Review**: the proposal is reviewed against R1–R20 (Ch.29) and the lifetime lock list. A red-line change requires confirming the change is worth breaking the lock.
3. **Update doc + code together**: the doc edit and the code edit ship in the same PR (or a tightly-coupled sequence). A doc edit without the code, or vice versa, is blocked — they must agree at every commit.
4. **Bump version**: `MAJOR` for red-line/binding-signature changes, `MINOR` for non-red-line additions, `PATCH` for clarifications. Record the bump in the doc header.
5. **Update tests**: any test referencing the changed chapter is updated in the same change (Ch.26.4).
6. **Note migrations**: if save schema is affected, write and test the migration hook (30.3) in the same change.

- For `PATCH`-level clarifications (no behavioral change), steps 1–2 are lightweight: a PR note + review, no formal proposal.
- Silent edits to a locked chapter — even "just a typo fix" that sneaks in a behavioral change — are the one thing this protocol exists to prevent. If in doubt, propose.

### 30.5 AI-Specific Clause

Because AI collaborators can produce large edits quickly:

- Any AI edit to a 🟥 chapter (even a "small" one) must be accompanied by a `MAJOR`/`MINOR`/`PATCH` version-bump note in the PR description explaining which chapter changed and why.
- AI must not edit `GameConstants` source, `Scripts/Core/` 🟥 files, or this doc's TOC status column (flipping ⏳→✅ or vice versa) without explicit user sign-off. These are the highest-trust surfaces in the project.
- AI must not mark a chapter ✅ on its own — only the user finalizes a lock.
- When AI is uncertain whether a change touches a red line, it must default to proposing (30.4 step 1) rather than editing. The cost of a false "PATCH" is much higher than the cost of an unnecessary proposal.

### 30.6 Red Lines

1. The doc's `DocVersion` must be bumped on any non-typo change; the bump level follows 30.1.
2. Locked chapters (✅) change only via the 30.4 protocol; silent behavioral edits are forbidden.
3. `SchemaVersion` and `DocVersion` are independent; a save-affecting change bumps both per 30.3.
4. AI must not edit 🟥 Core source, `GameConstants`, or the TOC status column without explicit user sign-off.
5. AI must not mark a chapter ✅; only the user finalizes a lock.

---

> End of document. All 30 chapters locked at this DocVersion. Next iterations follow the Ch.30 lock-change protocol.

# Save / Meta Gateway Architecture Repair

**Date:** 2026-10-11  
**Scope:** Save data access, Meta progression access, singleton boundary cleanup  
**Verdict:** Accepted with high completion  
**Self-assessment:** 9.2 / 10

## 1. Objective

本轮目标是按照前期架构审查中延期的 Save/Meta 单例收敛方向，修复以下问题：

- `ISaveSlotGateway` 同时承担槽位管理、Meta 数据、Run 数据和保存操作，职责过宽。
- Flow、Player、UI、Interaction 和 Meta 逻辑直接依赖 `SaveSystem.Instance`。
- Meta 业务修改数据后需要知道具体的 `SaveSystem` 实现，难以替换和单元测试。
- 结算、故事进度、升级购买和 MetaHub 面板之间缺少明确的依赖方向。

本轮没有创建新的全局单例，也没有修改正式场景、Prefab、ScriptableObject 或正式存档内容。

## 2. Final Gateway Design

### 2.1 Save contracts

`Assets/Scripts/Core/Persistence/ISaveSlotGateway.cs` 现在提供三个分层接口：

```text
ISaveSlotGateway
  └─ HasAnyValidSave
  └─ GetSlotInfos / GetSlotInfo
  └─ TrySelectSlot / TryCreateSlot / DeleteSlot

ISaveDataGateway
  └─ Meta / CurrentRun / SelectedSlotIndex
  └─ LoadMeta / SaveMeta
  └─ LoadRun / SaveRun / ClearRun
  └─ StartNewRun

ISaveGateway
  └─ ISaveSlotGateway + ISaveDataGateway
```

`SaveSystem` 仍然是 Persistent 场景中的唯一文件持久化实现，但现在实现的是 `ISaveGateway`。这样保留了现有生命周期和 JSON 格式，同时把业务消费者从具体 MonoBehaviour 中解耦出来。

### 2.2 Meta gateway

新增 `Assets/Scripts/Core/Meta/IMetaGateway.cs`：

```text
IMetaGateway
  └─ Meta
  └─ HasMeta
  └─ Save()

MetaGateway
  └─ wraps ISaveDataGateway
```

`MetaGateway` 是普通对象，不是静态单例。它只在 Persistent 组合边界或 MetaHub 拥有者边界创建，并作为接口传给 Meta 服务和 UI。

### 2.3 Dependency direction

```text
PersistentBootstrap
  └─ resolves SaveSystem.Instance once
  └─ creates MetaGateway
  └─ configures StoryProgressionService

SaveSystem : ISaveGateway
        │
        ├── ISaveSlotGateway → MainMenuUseCase
        └── ISaveDataGateway → Flow / Player / Runtime consumers

MetaGateway : IMetaGateway
        ├── StoryProgressionService
        ├── MetaUpgradeSystem
        └── MetaHub upgrade / allocation panels
```

依赖方向保持为：业务逻辑依赖端口，持久化实现位于基础设施层；没有新增 `SaveGateway.Instance` 或 `MetaGateway.Instance` 这种换名字的全局入口。

## 3. Implemented Changes

### 3.1 Flow and lifecycle

- `GameFlow` 缓存 `ISaveDataGateway`，Run 结束时调用 `RunEndSettlement.SettleCurrentRun(_saveData)`。
- `RunEndSettlement` 改为通过 `ISaveDataGateway` 完成 Meta 保存和 Run 清理，不再直接读取 `SaveSystem.Instance`。
- `LoadingRunController` 在依赖准备完成后缓存 `ISaveDataGateway`，后续启动、恢复和保存 Run 都通过接口。
- `PersistentBootstrap` 解析一次 Save 实现，创建 `MetaGateway`，并配置 `StoryProgressionService`。
- `MainMenuController` 将槽位接口和数据接口分开传给 `MainMenuUseCase`。

### 3.2 Meta domain

- `StoryProgressionService` 新增 `Configure(IMetaGateway)`，事实、里程碑和立即保存均通过 Meta gateway。
- `MetaUpgradeSystem.Purchase` 接受 `IMetaGateway`，不再在静态规则中调用 `SaveSystem.Instance`。
- `MechanicUnlockSystem` 增加显式 `IMetaGateway` 路径；旧的无参数 `IsUnlocked` 入口保留为兼容桥，详见残余风险。
- `MetaUpgradeStation` 在创建时缓存 Meta gateway，并将其传给 `MetaUpgradePanel`。
- `PotentialAllocationPanel` 通过 `RunStartInteractable` 传入的 Meta gateway读取 Meta 数据。

### 3.3 Player, item and dialogue consumers

以下消费者都改为只在初始化边界解析一次 SaveSystem，然后缓存 `ISaveDataGateway`：

- `PlayerDeathController`
- `PlayerStatsInitializer`
- `GameplayHudController`
- `CurrencyPickup`
- `WeaponPickup`
- `DialogueTrigger`
- `RunStartInteractable`

这些类的 Update、交互、受伤、拾取和显示路径不再反复读取 `SaveSystem.Instance`。

### 3.4 Architecture gates and tests

`RuntimeEditorDependencyTests.cs` 新增门禁，覆盖：

- Save slot / save data / composite gateway 的存在和职责分层。
- `SaveSystem` 实现 `ISaveGateway`。
- `StoryProgressionService`、`MetaUpgradeSystem`、`RunEndSettlement` 不直接依赖 `SaveSystem.Instance`。
- Flow、MainMenu、PlayerDeathController 和 MetaHub 面板使用缓存接口。
- 不允许因为本轮改造新增新的 Gateway 单例。

## 4. Validation Evidence

### 4.1 Unity route

- Unity project root confirmed: `D:/GitHub/Zephyr_v3/Zephyr(AI)/Zephyr`
- Code and non-serialized files were edited with `apply_patch`.
- Unity operations used the old `mcp__unity_mcp` route as required by project rules.
- Unity Test Runner was invoked through the newer MCP only because the old route does not expose a test-runner API, as explicitly permitted by the user.

### 4.2 Compilation and Console

- Unity refresh and script compilation completed successfully.
- Final Console: **0 Error / 0 Warning**.
- No MissingReference, Serialization, YAML parse, missing script, shader/material pink, or failed-load message was present after refresh.
- Standard script validation returned no diagnostics for the changed contract, Flow, Meta, Player and Dialogue scripts.
- The two UI panel validations reported existing-style warnings about string concatenation in `Update()`; they are performance hints, not compile errors and are unrelated to the gateway migration.

### 4.3 EditMode tests

| Test group | Result |
|---|---:|
| `Zephyr.Architecture.Tests` | **29/29 passed** |
| `Zephyr.Tests.EditMode` | **5/5 passed** |
| Total | **34/34 passed** |

The new Save/Meta gateway architecture checks are included in the 29 architecture tests and passed.

### 4.4 Play Mode smoke test

- Entered Play Mode from the existing project state.
- Active scene reached: `Assets/Scenes/MainMenu/MainMenu.unity`.
- Scene was not dirty.
- No formal save slot was selected, created, deleted, or written during this smoke test.
- Final runtime Console remained **0 Error / 0 Warning**.
- Play Mode was stopped cleanly.

No scene, Prefab, ScriptableObject or serialized reference was modified in this pass, so no SceneView capture was required.

### 4.5 Workspace hygiene

- `git diff --check` reported no whitespace errors.
- Existing broad uncommitted changes from earlier architecture phases were preserved; no reset, checkout, or wide deletion was performed.

## 5. Remaining Risks and Deferred Work

本轮已经把 Save/Meta 的主要业务消费路径迁移到接口，但没有声称全局单例债务已经清零：

1. `SaveSystem.Instance` 仍作为 Persistent 场景基础设施的唯一实现入口存在；它没有被替换成第二个单例。
2. `MechanicUnlockSystem.IsUnlocked(string)` 仍保留旧兼容入口，用于当前 `MechanicUnlockedConditionSO` 的无注入状态条件路径；新的显式重载已经接受 `IMetaGateway`。
3. `TutorialController.MarkCompleted()` 仍保留静态兼容入口，因为现有 Tutorial objective 仍通过静态调用完成教程；它应在 Tutorial 组合根具备明确引用后再移除。
4. `ColdStartupBootstrap` 仍然允许直接读取 `SaveSystem.Instance`，这是编辑器直接打开场景的组合/诊断边界，不是普通业务逻辑。
5. 一些 MonoBehaviour 仍在 `Awake`、`Start` 或依赖就绪后的初始化边界解析一次 `SaveSystem.Instance`；它们没有在每个业务方法中重复查询，符合当前项目对组合边界的过渡策略。

下一步如果继续追求更高的整体分数，应优先完成：

- 为状态机条件增加可注入的 Meta runtime context，移除 `MechanicUnlockSystem` 的兼容静态入口。
- 为 Tutorial objective 增加明确的 Controller/UseCase 引用，移除 `TutorialController.MarkCompleted()` 静态入口。
- 在不修改存档格式的前提下，为 SaveSystem 增加更明确的 Persistent composition root 接口绑定和替换测试。

## 6. Unified Self-Assessment

| Dimension | Score | Assessment |
|---|---:|---|
| Boundary ownership | 9.5 | 槽位、Save 数据和 Meta 访问已分层；MetaHub 面板不再自行寻找具体 SaveSystem。 |
| Dependency direction | 9.4 | Flow、Player、Interaction 和 Meta 业务主要依赖接口；持久化实现没有向上依赖。 |
| Lifecycle and singleton safety | 9.0 | SaveSystem 仍是既有 Persistent 单例，但解析集中在初始化边界，没有新增平行单例。 |
| Testability and gates | 9.4 | MainMenu 继续使用可替换接口，新 gateway 门禁已加入并通过。 |
| Migration completeness | 8.9 | 两个旧静态兼容入口和 ColdStartup 诊断路径仍待后续收敛。 |
| Verification confidence | 9.5 | Unity 编译、Console、34 个 EditMode 测试和 MainMenu Play Mode 烟测均通过。 |
| **Overall** | **9.2** | **本轮完成度很高，Save/Meta Gateway 改造验收成功；剩余项已被限制在可识别的兼容边界。** |

## 7. Acceptance

本阶段判定为**验收成功**：

- Save slot 和 Save data 的职责已拆开。
- Meta 业务访问已通过 `IMetaGateway` 收敛。
- Flow 结算、故事进度、升级购买、MetaHub UI、Player 和拾取路径已完成主要迁移。
- 没有引入新的全局 Manager、Gateway 单例、场景加载通道或存档格式。
- Unity 编译干净，Console 干净，架构测试和业务测试全部通过，运行时启动烟测通过。

普通功能开发可以继续在这些接口边界之内进行；新增代码不得绕过 Gateway 直接访问 `SaveSystem.Instance`，除非代码明确属于 Persistent、ColdStartup 或等价的组合边界。

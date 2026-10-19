# Phase 2：组合根与 Singleton 读取预算

日期：2026-10-12  
项目：Zephyr Unity 2D 横版 Metroidvania  
Unity：`6000.5.6f1`  
范围：GameManager 组合根、Player 运行时依赖注入、EventBus Gateway、singleton 读取预算。

## 1. 验收结论

Phase 2 验收成功，完成度评估为高。

本阶段建立了一个明确的 GameManager 组合根：Persistent 中的 `GameFlow`、`SaveSystem` 和 `EventBus` 只在 `GameManagerBootstrap` 这一初始化边界读取一次，然后以接口集合注入 GameManager 内的 PlayerSpawner、PlayerDeathController 和 GameplayHudController。PlayerSpawner 在实例化玩家后，再把 `ISaveDataGateway` 注入 `PlayerStatsInitializer`。

本阶段没有改变 Persistent/GameManager 生命周期协议，没有新增平行 Manager，没有改动 StateMachine、场景、Prefab、ScriptableObject 或序列化类型名。

## 2. 实施内容

### 2.1 Contracts

新增：

- `Assets/Scripts/Core/Contracts/Interfaces/IEventBusGateway.cs`
  - 把事件订阅、取消订阅、发布和 latest payload 查询抽象为纯接口。
  - Contracts 不引用 Persistence、Flow 实现或任何具体 singleton。
- `Assets/Scripts/Core/Contracts/Interfaces/IPlayerSpawnGateway.cs`
  - 暴露当前玩家、生成/销毁事件和重生能力。
  - PlayerDeathController 不再需要把 Gateway 强转成具体 `PlayerSpawner`。

### 2.2 GameManager 组合根

新增：

- `Assets/Scripts/Core/Flow/Bootstrap/IGameManagerDependencyConsumer.cs`
  - 定义 `IGameManagerDependencyConsumer` 和 `IGameManagerServices`。
  - 该接口放在 Flow，而不是无依赖 Contracts，避免 Contracts 反向依赖 Persistence。

修改：

- `Assets/Scripts/Core/Flow/Bootstrap/GameManagerBootstrap.cs`
  - 在 `Awake` 读取 `GameFlow.Instance`、`SaveSystem.Instance`、`EventBus.Instance` 各一次。
  - 在 GameManager 场景根下发现实现 `IGameManagerDependencyConsumer` 的组件并进行一次性注入。
  - 同时解析 `IPlayerSpawnGateway`，通过接口提供给死亡控制器。
  - 保持 `GameManagerBootstrap.IsReady` 原有生命周期语义。

这使具体 singleton 读取集中在组合根，业务消费者不再自行定位 Persistent 服务。

### 2.3 Player 消费者

修改：

- `Assets/Scripts/Player/Flow/PlayerSpawner.cs`
  - 实现 `IGameManagerDependencyConsumer` 和 `IPlayerSpawnGateway`。
  - 通过注入的 `IGameFlowGateway` 监听状态变化。
  - 通过注入的 `IEventBusGateway` 发布 `PlayerTargetChangedEvent`。
  - 生成玩家后，把保存接口注入 `PlayerStatsInitializer`。
- `Assets/Scripts/Player/Flow/PlayerDeathController.cs`
  - 通过 `IGameManagerServices` 获取 Player、Flow 和 Save Gateway。
  - 保留 `ResolveDependencies` 这个明确的依赖边界，但不再读取任何 singleton。
  - `RespawnCoordinator` 继续只依赖 `IPlayerRespawnGateway` 和 `IGameFlowGateway`。
- `Assets/Scripts/Player/UI/GameplayHudController.cs`
  - 通过注入的 `ISaveDataGateway` 读取当前 Run 数据。
  - 通过 `IEventBusGateway` 订阅/取消订阅玩家目标变化。
  - 没有把本阶段扩展为 HUD Prefab/View 重构；运行时 UI 树构造仍保留给后续 UI 专项。
- `Assets/Scripts/Player/Core/PlayerStatsInitializer.cs`
  - `Awake` 只缓存自身 `StatsCore` 并设置角色类型。
  - 通过幂等的 `InitializeNow` 执行默认值应用，允许 PlayerSpawner 在实例化后、发布 `PlayerSpawned` 前注入并完成初始化。
  - 无保存服务时仍使用 Inspector 默认值，保持直接打开测试场景的最低可用行为。
- `Assets/Scripts/Core/Runtime/Events/EventBus.cs`
  - 实现 `IEventBusGateway`，不改变现有 singleton 生命周期。

## 3. Singleton 预算变化

统计口径：运行时代码 `Assets/Scripts` 下的 `*.Instance` 文本读取，排除 `Editor` 和 `Tests`。Phase 0 报告记录的基线为 112 次。

| 指标 | Phase 0 基线 | Phase 2 当前 | 变化 |
| --- | ---: | ---: | ---: |
| 运行时代码 `*.Instance` token | 112 | 103 | -9 |
| GameManager 组合根读取 | 未集中 | 3 个明确读取点 | 集中到 `GameManagerBootstrap` |
| PlayerSpawner 直接 singleton 读取 | 1 | 0 | -1 |
| PlayerDeathController 直接 singleton 读取 | 3 | 0 | -3 |
| GameplayHudController 直接 singleton 读取 | 2 | 0 | -2 |
| PlayerStatsInitializer 直接 singleton 读取 | 1 | 0 | -1 |
| 本阶段新增业务 singleton 读取 | 目标 0 | 0 | 达标 |

当前 103 个 token 中，`CameraController` 内部反射代码的 5 个 `BindingFlags.Instance` 并不是 singleton 读取。按排除该反射语法后的服务 singleton 读取为 98 个。该差异已在本报告中明确，不把反射 API token 误计为服务定位。

仍然存在的主要读取热点包括：

- `Core/Flow/Bootstrap/ColdStartupBootstrap.cs`
- `Core/Flow/Scenes/LoadingRunController.cs`
- `Core/Flow/GameFlow.cs`
- `Core/UI/Dialogue/DialogueTrigger.cs`
- `Core/Items/CurrencyPickup.cs`
- `Core/Weapons/WeaponPickup.cs`
- `Gameplay/Enemies/EnemyBrain.cs`
- `Gameplay/Interaction/GustInteractable.cs`
- `Gameplay/Levels/SceneProgressionExit.cs`
- `Gameplay/Levels/TutorialObjectiveExit.cs`

这些读取尚未在本阶段继续迁移，因为它们分别涉及冷启动例外、场景入口、敌人运行时、交互发现和 Gameplay 场景绑定。强行在本阶段统一改动会扩大到 SceneContext、Prefab 注入和场景生命周期，超出本次低风险切片。

## 4. 依赖方向评估

本阶段的依赖方向评估为通过：

1. `Zephyr.Core.Contracts` 只增加纯接口，不依赖 Persistence 或 Flow。
2. `Zephyr.Core.Runtime` 依赖 Contracts，由 EventBus 实现 `IEventBusGateway`。
3. `Zephyr.Core.Flow` 作为 GameManager 组合根，向下读取并组装已存在的 Persistent 服务。
4. `Zephyr.Player` 只依赖 `IGameManagerServices`、Gateway 和自身领域类型，不读取具体 Persistent singleton。
5. PlayerDeathController 不再把接口强转为 `PlayerSpawner`，Player 生成边界通过 `IPlayerSpawnGateway` 表达。
6. 没有新增 `DontDestroyOnLoad`、平行 SceneLoader、平行 EventBus 或新的全局 Manager。

## 5. 验证结果

### Unity/MCP 路由

- Unity 项目根目录确认：`D:/GitHub/Zephyr_v3/Zephyr(AI)/Zephyr`。
- 非图片 Unity 操作统一使用旧版 `mcp__unity_mcp`。
- 通过旧版 MCP 执行 `Assets/Refresh`，触发资源导入和脚本编译。
- 本阶段没有修改场景，因此不执行 SceneView 截图流程。

### 测试与脚本验证

- 架构测试：`39/39 passed`。
- EditMode 测试：`5/5 passed`。
- 新增门禁覆盖：
  - GameManager 消费者不得直接读取 `GameFlow.Instance`、`SaveSystem.Instance` 或 `EventBus.Instance`。
  - GameManagerBootstrap 是允许的解析边界。
  - EventBus Gateway 不得把 Persistence 类型带入 Contracts。
  - PlayerDeathController 必须依赖 `IPlayerSpawnGateway`，不能强转具体 PlayerSpawner。
- 修改文件的 Unity 标准脚本验证：无诊断。
- `git diff --check`：通过。

测试运行器曾有一次初始化超时，未开始测试；清理/重试后架构测试和 EditMode 测试均成功完成。该次超时没有伴随项目 Console 错误。

### Console

最终 `Unity_GetConsoleLogs` 结果：

- Error：`0`
- Warning：`0`
- Exception/MissingReference/Serialization/YAML parse：未发现

脚本级验证对 `GameplayHudController` 提示了原有的 `Update()` 字符串拼接可能产生 GC 的建议。这不是编译错误，也不属于本阶段的组合根架构问题，保留为后续 UI 性能专项项。

## 6. 本阶段未完成与风险

以下内容明确延期，不视为 Phase 2 失败：

1. RunStartInteractable、MetaUpgradeStation、DialogueTrigger、WeaponPickup、CurrencyPickup 等场景/Actor 入口仍有 singleton 读取。
2. LoadingRunController、ColdStartupBootstrap 和 GameFlow 仍承担启动/场景协议中的合法解析职责，但还没有全部收敛成统一的显式上下文对象。
3. GameplayHudController 仍运行时构造 UI 树，Prefab/View 化和事件驱动刷新属于后续 UI 阶段。
4. 未执行完整启动链和 PlayMode 场景回归；本阶段验证覆盖架构门禁、脚本验证和 EditMode 行为测试。
5. `PlayerStatsInitializer` 的注入依赖 PlayerSpawner 的实例化顺序；当前由 Unity `Start` 时序保证，后续可以在 Player 组合根或 Prefab 级上下文中继续显式化。

## 7. 阶段评分

按当前项目架构专项评分标准，本阶段评估如下：

| 维度 | Phase 2 结果 |
| --- | ---: |
| 组合根边界 | 9.0/10 |
| Singleton 读取集中度 | 8.6/10 |
| Gateway/接口依赖方向 | 9.1/10 |
| 生命周期兼容性 | 9.2/10 |
| 回归与门禁 | 9.3/10 |
| 本阶段综合完成度 | 9.0/10 |

综合完成度评估为高，Phase 2 验收成功。

这不是整个项目的最终架构评分。剩余扣分主要来自跨场景入口和 Actor 入口仍有服务定位、场景发现和运行时 UI 构造。下一阶段应继续处理“场景入口显式上下文”和“Interaction/Dialogue/Items/Weapons 的 Gateway 注入”，不应重新扩大 GameManager 组合根或新增全局服务定位器。

## 8. 下一阶段建议

建议 Phase 3 以场景入口为范围，优先处理：

1. `LoadingRunController` 的 Flow/Save/EventBus 解析，保留 ColdStartup 的直接打开例外。
2. `RunStartInteractable`、`MetaUpgradeStation` 的 Save/Meta Gateway 注入。
3. `DialogueTrigger`、`WeaponPickup`、`CurrencyPickup` 的显式服务绑定。
4. 继续维持“新业务代码 singleton 读取为 0”的门禁。
5. PlayMode 启动链回归应在这些入口迁移后补上，而不是在每个局部迁移中引入新的场景发现方式。

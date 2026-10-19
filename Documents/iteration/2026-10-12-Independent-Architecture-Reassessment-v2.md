# Zephyr 全局架构独立复评 v2

日期：2026-10-12  
项目：`D:\GitHub\Zephyr_v3\Zephyr(AI)\Zephyr`  
评估范围：当前全部运行时代码、程序集定义、Contracts、Persistent/GameManager 生命周期、场景加载、Save/Meta、Combat、Player、Enemies、Weapons、Items、Projectiles、Rooms、Interaction、UI/Localization、Audio、Camera，以及现有架构门禁。  
评估目标：只评价 architecture correctness 与 architecture cleanliness，不评价玩法完成度、美术质量、数值设计或性能。

## 1. 直接结论

本轮重新从当前实现出发进行独立审查，不继承 Phase 0-5、Core Split A-G 或其他历史阶段的完成度分数，也不把以前报告中的“阶段 9.x”当作整体分数证据。

**当前整体架构评分：8.3 / 10。**

**当前不能诚实地评为 9.0 以上。**

这个结论不是对前面重构工作的否定。当前项目已经具备清晰的程序集分层、稳定的 Contracts/Runtime 基础层、统一的场景加载 authority，以及比初始版本低得多的 Enemies/Items/Weapons 迁移耦合。扣分主要来自仍未彻底收窄的兼容宿主、运行时具体单例读取、上层程序集依赖面过宽、少量全局场景查找，以及若干职责偏重的实现类。

## 2. 独立评分标准

本轮使用固定加权模型。每一项先按 0-10 评分，再乘以权重：

| 维度 | 权重 | 9.0 的最低含义 |
|---|---:|---|
| 模块边界与程序集依赖方向 | 25% | 依赖单向、无循环、兼容宿主不是公共枢纽，领域模块只依赖必要层 |
| 生命周期与组合根 | 20% | Persistent/GameManager/场景/Actor owner 清楚，业务代码不自行定位全局服务 |
| Contracts 与数据边界 | 15% | 契约按语义归属，配置、运行时状态、存档状态和实现类型不混杂 |
| 跨模块耦合程度 | 15% | 高层通过 gateway/interface/event 通信，不依赖大量具体实现和场景对象 |
| 全局状态、资源与场景访问 | 10% | 单例有明确预算，业务无隐式场景扫描、资源路径查找或直接场景加载 |
| 实现职责与代码架构 | 10% | 关键类职责集中，组合、规则、IO、视图构造和运行时行为没有明显混合 |
| 验证、可维护性与迁移残留 | 5% | 架构门禁验证真实约束，当前测试/编译状态可复现，残余桥接可追踪 |

最终分数：

```text
总分 = Σ（维度分数 × 权重）
     = 8.6×25% + 8.0×20% + 8.7×15% + 7.9×15%
       + 8.1×10% + 8.2×10% + 8.0×5%
     = 8.3 / 10
```

除加权总分外，达到 9.0+ 还必须同时满足以下硬门槛：

1. 模块边界、生命周期、Contracts 三项都不低于 9.0。
2. 不存在未控制的循环程序集依赖。
3. 不新增平行 SceneLoader、EventBus、StateMachine、战斗服务或全局 Manager。
4. 普通业务不直接绕过 `SceneLoader` 加载/卸载场景。
5. 共享 ScriptableObject 不保存 Actor 的运行时可变状态。
6. 架构测试、EditMode 测试、编译和关键运行时链路都有当前可复现证据。
7. Singleton、全局查找和兼容桥接都有明确 owner、预算和删除路径。

当前第 2、4、6、7 项仍不满足 9.0+ 的严格证据标准，因此即使把某些维度向上取整，也不能宣布达到 9.0+。

## 3. 当前结构证据

### 3.1 程序集依赖

当前解析到 30 个 asmdef、277 个 C# 文件。程序集依赖图没有检测到循环依赖，这是当前架构的明确优点。

稳定基础层的方向基本正确：

- `Zephyr.Core.Contracts` 无项目程序集依赖。
- `Zephyr.Core.Runtime` 只向下依赖 Contracts。
- `Zephyr.Core.Combat` 依赖 Contracts 和 Runtime。
- `Zephyr.Core.Persistence` 依赖 Runtime。
- `Zephyr.Core.Meta` 依赖 Contracts、Runtime、Persistence。
- `Zephyr.Core.StateMachine` 独立于业务程序集。
- Audio 和 Camera 已从旧 Core 中分离。

但 `Zephyr.Core` 虽然设置了 `autoReferenced: false`，仍被以下 13 个程序集显式引用：

`Zephyr.Core.Flow`、`Zephyr.Items`、`Zephyr.Projectiles`、`Zephyr.Weapons`、`Zephyr.Enemies`、`Zephyr.Gameplay.Interaction`、`Zephyr.Rooms`、`Zephyr.Player`、`Zephyr.Testing`、`Zephyr.Testing.Editor`、`Zephyr.Tests.EditMode`、`Zephyr.UI`，以及一个额外的测试/工具消费者。

这说明旧 Core 已经从“隐式公共依赖”变成“显式兼容宿主”，但仍没有收敛到少数组合根或适配器。它是当前无法给 9.0+ 的最大结构性原因之一。

### 3.2 Singleton 与隐式发现

排除 Editor 和 Tests 后，运行时代码中统计到具体 `*.Instance` 读取约 **93 处**。这些读取并非全部错误：Bootstrap、Persistent owner、GameFlow 与 SceneLoader 的生命周期协作属于允许的边界。但仍有一组明显的业务侧读取：

- `LoadingRunController`、`MainMenuController`、`TutorialController`、`MetaHubController` 直接读取 Flow/Save。
- `CurrencyPickup`、`WeaponPickup`、`DialogueTrigger`、`MetaUpgradeStation` 直接读取 Save 或 EventBus。
- `EnemyBrain`、`GustInteractable`、玩家输入/交互代码直接读取 EventBus 或 DialogueManager。
- `MechanicUnlockSystem`、`StoryProgressionGate`、`DialogueTrigger` 直接读取 StoryProgressionService。
- `SceneProgressionExit`、`TutorialObjectiveExit` 直接读取 SceneProgressionController。

这类读取有部分缓存，但依赖仍在具体场景对象和 Actor 组件中自行解析，而不是由组合根一次组装后传入。它提高了场景初始化顺序敏感度，也使单元测试必须模拟静态全局状态。

运行时隐式对象发现有 4 处：

- `DialogueManager` 和 `MainMenuView` 查找 `EventSystem`，属于低风险的 UI 基础设施 fallback。
- `TutorialObjectiveExit` 查找 `RoomController`，属于业务级场景扫描。
- `RunStartInteractable` 查找 `MetaHubController`，属于业务级场景扫描。

后两处应由场景引用、GameManager 服务或显式 gateway 提供，不能继续作为新代码范式。

### 3.3 场景加载 authority

运行时代码中的 `SceneManager` 直接调用只出现在：

- `InitializerBootstrap`
- `ColdStartupBootstrap`
- `SceneLoader`

未发现普通领域模块直接绕过 `SceneLoader` 加载或卸载场景，这是正确的生命周期约束。`GameFlow` 负责状态和场景请求，`SceneLoader` 负责实际队列和 Unity 加载，方向合理。

不过 `GameFlow` 同时承担状态转换、场景映射、运行结算、房间卸载、直接打开场景初始化和 SceneStatsModifier 协作，仍然偏向一个“流程编排 + 场景策略 + 运行结算”的复合对象。它现在可用，但还不是足够窄的组合根。

## 4. 模块逐项评估

耦合等级含义：`低` 表示依赖少且方向稳定；`中` 表示有明确边界但仍依赖多个服务；`高` 表示依赖面宽、存在具体实现/全局状态或职责混合。

| 模块 | 主要职责 | 主要依赖/消费者 | 耦合 | 架构分 | 独立判断 |
|---|---|---|---|---:|---|
| Contracts | 接口、事件载荷、Flow/Damage 类型 | 被所有领域层消费 | 低 | 8.7 | 语义 namespace 已明显改善，但 `StatModifierDefinition` 与 `StatType` 等仍留在 `Zephyr.Core.*` 历史命名空间。 |
| Runtime | EventBus、Timer、Pool、生命周期、掉落基础 | Contracts；被所有运行时层消费 | 低 | 8.8 | 依赖方向干净，是当前最稳定的基础层；部分类型仍保留迁移后的旧语义名称。 |
| StateMachine | 玩家/敌人共享状态机基础设施 | 独立；被 Player/Enemies 消费 | 低 | 8.8 | 基础设施没有复制成第二套，编辑器与运行时分离合理；仍需继续防止行为层反向依赖具体 Actor。 |
| Flow | GameFlow、SceneLoader、Bootstrap、场景控制器 | Contracts、Runtime、Persistence、Meta、Localization、UI、旧 Core | 高 | 7.8 | authority 正确，但流程状态、场景策略、存档结算和直接打开兼容逻辑集中度偏高。 |
| Persistence | SaveSystem、DTO、slot/run/meta 存档 | Runtime；被 Flow/Meta/UI/Interaction/Items/Weapons 消费 | 中高 | 8.2 | Gateway 已存在，但 SaveSystem 仍同时承担文件 IO、slot policy、DTO 转换和公开运行时模型。 |
| Meta | MetaGateway、升级、故事、解锁、结算 | Persistence、Runtime、Contracts、Localization | 中高 | 8.1 | 方向基本正确，但 `StoryProgressionService` 和辅助系统仍有 singleton 入口，MetaGateway 在场景组件中重复构造。 |
| Combat | Damage、Health、Stats、Buff、Combat target | Contracts、Runtime；被 Player/Enemies/Weapons/Projectiles 消费 | 中 | 8.4 | 程序集边界稳定；`StatsCore`、Buff 和 Health 仍包含较多运行时规则与兼容类型，继续拆分需谨慎。 |
| Camera | CameraController、Bounds、Zone、Cinemachine 适配 | Unity/Cinemachine；被 Player/Flow/场景消费 | 中 | 8.3 | 功能边界明确，但 `CameraController` 约 483 行并含反射式兼容工具，表现、边界和运行时配置仍较集中。 |
| Audio | AudioManager、事件和播放服务 | Runtime；被 UI/Gameplay 消费 | 低中 | 8.7 | 已独立且没有重新依赖旧 Core，生命周期方向清楚；尚缺更强的组合根注入证据。 |
| Player | 输入、移动、生命、交互、生成、HUD、表现 | 近乎所有基础/领域层；消费 Combat/Flow/Camera/UI/Weapons/Dialogue | 高 | 7.8 | 依赖面最宽之一；玩家既是领域 Actor 又是多个服务的装配入口，仍有直接单例读取和大量跨层引用。 |
| Enemies | EnemyBrain、EnemyMotor、Boss、敌人攻击 | Contracts、Runtime、Combat、StateMachine、旧 Core | 中高 | 8.1 | 已清除对 Items/Weapons 的迁移桥；`BossController` 约 476 行，攻击编排、碰撞 profile、移动和阶段规则集中。 |
| Weapons | WeaponSO、Runtime、Controller、Pickup | Contracts、Runtime、Combat、Persistence、旧 Core | 中高 | 8.2 | 武器数据/运行时已有分离；`WeaponController` 仍承担查找、装备、攻击编排和动画协作，Pickup 仍直接读取 Save。 |
| Items | CurrencyPickup、掉落接口和运行时物品 | Contracts、Runtime、Combat、Persistence、旧 Core | 中 | 8.4 | 与 Weapons 的迁移桥已清除；CurrencyPickup 仍通过 EventBus/Save singleton 获取全局状态。 |
| Projectiles | Projectile、ProjectileSystem | Contracts、Runtime、Combat、旧 Core | 中 | 8.6 | 依赖较窄、池化方向合理；TimerService 的直接 singleton 读取仍可由运行时服务上下文收敛。 |
| Rooms/Levels | RoomController、场景目标、场景推进 | Contracts、Runtime、Flow、旧 Core | 中高 | 7.9 | 场景生命周期依赖正确，但 ProgressionController/Exit 与场景对象之间仍有 singleton 和扫描式耦合。 |
| Gameplay.Interaction | Interactable、MetaHub、Gust、升级和运行入口 | Contracts、Runtime、Persistence、Meta、Flow、Localization、UI、旧 Core | 高 | 7.5 | 程序集引用面很宽；运行时实例化 UI、重复构造 MetaGateway、查找 MetaHubController，边界仍不够干净。 |
| UI/Dialogue | 面板、DialogueManager、触发器、Presenter | UI foundation、Localization、Meta、Persistence、Inputs、旧 Core | 中高 | 7.8 | UI/Localization 已有专属程序集，但 DialogueTrigger 和输入层仍直接读全局服务；部分视图结构仍由代码寻找/创建。 |
| 旧 `Zephyr.Core` 兼容宿主 | 尚未下沉的共享实现、常量、工具、兼容入口 | 被 13 个程序集显式引用 | 高 | 6.8 | `autoReferenced:false` 是控制措施，但不能改变它仍是跨层公共枢纽的事实。应继续收窄，而不是继续向其中添加代码。 |

## 5. 加权复评分

| 维度 | 分数 | 依据 |
|---|---:|---|
| 模块边界与程序集依赖方向 | 8.6 | 无循环、领域 asmdef 已拆分、迁移桥已明显减少；旧 Core 仍有 13 个显式消费者，上层程序集依赖面过宽。 |
| 生命周期与组合根 | 8.0 | Persistent/GameManager owner 和 SingletonLifecycle 已明确；93 处具体单例读取中仍有大量场景/Actor 入口自行定位服务。 |
| Contracts 与数据边界 | 8.7 | Contracts、Flow、Damage 接口/枚举迁移基本完成；Stats 常量/定义仍混在旧 Core namespace，数据模型与实现边界还有残留。 |
| 跨模块耦合程度 | 7.9 | Player、Flow、Interaction、Dialogue 连接面宽；gateway/event 已降低部分耦合，但 concrete service 和场景控制器仍被直接调用。 |
| 全局状态、资源与场景访问 | 8.1 | 没有普通业务直接 `SceneManager` 或 `Resources.Load`；但业务级 `FindAnyObjectByType` 仍有 2 处，singleton 读取仍偏多。 |
| 实现职责与代码架构 | 8.2 | 大多数模块职责可识别；CameraController、BossController、SaveSystem、StatsCore、GameFlow、Interaction UI 仍存在职责偏重。 |
| 验证、可维护性与迁移残留 | 8.0 | 架构测试源码覆盖面较广，关键脚本验证无诊断；本轮 TestJob 未能初始化，且 Console 有 Unity Editor 服务错误，当前无法把运行验证记为全绿。 |
| **最终加权总分** | **8.3** | **达到可持续普通开发的架构门槛，但未达到全局 9.0+。** |

## 6. 验证结果与证据边界

### 已完成的当前核对

- Unity 项目根目录确认：`D:/GitHub/Zephyr_v3/Zephyr(AI)/Zephyr`。
- Unity 版本路径确认：`6000.5.6f1`。
- Unity 当前状态：未播放、未暂停、未编译、未更新。
- asmdef 解析：30 个程序集，无循环依赖。
- 运行时静态扫描：未发现 `Resources.Load`、`GameObject.Find` 或普通业务直接场景加载。
- 关键脚本标准验证无诊断：`GameFlow`、`SaveSystem`、`StatsCore`、`GameManagerBootstrap`、`RuntimeEditorDependencyTests`、`RunStartInteractable`。
- `CameraController`、`BossController` 的标准验证仅报告既有的 Update 字符串拼接警告；这不是本轮架构评分的主要扣分来源，但应在性能专项中单独处理。

### 当前不能过度解释的结果

- 本轮通过新版 MCP 启动 `Zephyr.Architecture.Tests` 时，TestJob 在 120 秒内未初始化，最终失败，测试没有开始执行。
- Unity Console 读取到 1 条 `UnityEditor.Connect.ServicesConfiguration` 的 Editor 服务错误，以及 1 条 MCP TestJob 初始化失败警告；没有读取到项目脚本编译错误或 Missing Script/Reference 错误。
- 因此不能把“当前测试全绿”写入本轮证据。前期报告中记录的 `40/40` 与 `5/5` 只能作为历史验证记录，不作为本轮独立复评的当前执行结果。
- 本轮没有修改场景、Prefab、ScriptableObject 或运行时代码，所以不生成 SceneView 截图。

## 7. 进入 9.0+ 的最小修复集合

不建议继续横向增加程序集数量。进入 9.0+ 的最小集合应集中在以下四个方向：

### P0：继续收窄旧 Core

- 逐个清理 13 个显式消费者中的旧 Core 类型引用。
- 将共享常量、Stats contracts、Grid/工具、UI foundation 和 Dialogue 的剩余类型归入明确程序集。
- 保留旧 Core 仅作为短期迁移适配器，并建立消费者数量下降的硬门禁。

### P0：完成服务组合边界

- 给 LoadingRun、MainMenu、Tutorial、MetaHub、SceneProgression、Dialogue、Items、Weapons、Interaction 和 Enemy 入口提供显式 gateway/context。
- 允许 Singleton 只在 Persistent/GameManager/Initializer 组合边界解析一次，内部组件只保存接口。
- 继续把 `SaveSystem.Instance`、`EventBus.Instance`、`GameFlow.Instance` 和 `StoryProgressionService.Instance` 从场景/Actor 组件中移除。

### P1：消除两个业务级场景扫描

- `TutorialObjectiveExit` 改为序列化/组合根提供 `RoomController`。
- `RunStartInteractable` 改为注入 `IGameFlowGateway` 或 MetaHub run-entry gateway，不再查找 `MetaHubController`。

### P1：收窄复合职责类

- 将 `GameFlow` 的场景解析/结算协作拆到明确策略或 gateway。
- 将 `SaveSystem` 的 slot policy、DTO 转换和文件 IO 分离到 persistence 内部服务。
- 将 `BossController` 的攻击时间线/碰撞 profile 与阶段决策拆分。
- 将 `CameraController` 的兼容反射和运行时跟随策略拆开。
- 将 Interaction UI 的 Prefab/View 组装从业务 Interactable 中移出。

### P1：恢复可复现验证

- 先修复或隔离当前 Unity Editor `ServicesConfiguration` 错误。
- 重新运行 `Zephyr.Architecture.Tests` 和 `Zephyr.Tests.EditMode`，记录当前真实结果。
- 增加一个 Persistent → GameManager → Gameplay 的 PlayMode 装配 smoke test。
- 将架构门禁从部分字符串断言逐步升级为 asmdef JSON 依赖图、具体单例读取和关键对象 owner 的结构检查。

## 8. 最终判断

如果问题是“当前架构是否已经适合继续做普通游戏功能”，答案是：**可以，但必须遵守现有边界，不新增单例、全局查找、平行 SceneLoader 或旧 Core 依赖。**

如果问题是“重新独立评估后，是否能给整个游戏 9.0 以上”，答案是：**不能。**

本轮最准确的结论是：**8.3 / 10**。项目已经从无边界 Core 进入可维护、可继续演进的架构阶段，但离“全局架构正确且干净”的 9.0+ 标准仍差一轮针对兼容宿主、服务组合、业务场景扫描和职责收窄的专项收尾。只有在这些问题处理后，并且当前测试/运行验证可复现通过，才应重新讨论 9.0+。

# Zephyr 当前实现架构审查报告

> 审查日期：2026-10-10
> 审查范围：`Zephyr(AI)/Zephyr/Assets/Scripts`、场景/Prefab/SO 目录、asmdef、现有架构设计文档
> 审查目标：只评价 architecture 的合理性、边界清晰度、依赖方向和代码整洁度；不评价玩法质量、美术质量、数值平衡或性能

## 1. 结论摘要

当前实现不是“没有架构”，而是已经形成了一个可工作的架构雏形，但**逻辑模块化明显领先于编译模块化**。事件通道、ScriptableObject 配置、Core/Player/Gameplay 命名空间、`SceneLoader` 单一加载入口、状态机运行时实例化等设计方向是合理的；问题主要集中在边界没有彻底落地：

1. `Zephyr.Core` 过大，承担基础设施、流程、存档、Meta、UI、相机、武器、物品、房间等多种职责。
2. `Gameplay/Enemies`、`Gameplay/Levels`、`Gameplay/Interaction` 没有独立 asmdef，实际落入 `Assembly-CSharp`，因此跨模块引用没有编译期约束。
3. `PlayerDeathController`、`MainMenuController`、`GameFlow` 等类同时承担业务编排、持久化、UI 和生命周期职责，形成高扇入/高扇出枢纽。
4. 设计文档规定的“Core 只暴露稳定 API、领域模块只依赖 Core、跨领域通过事件”的目标，当前只部分实现。
5. 代码对外暴露面偏大：Core 中统计到 `174` 个 public 类型声明，`internal` 使用很少；这会使“模块内部重构”容易扩散为全局修改。
6. 当前没有测试 asmdef 或 NUnit/UnityTest 用例；架构契约缺少自动化守门机制。

**总体评价：概念架构 7/10，当前落地整洁度 5/10，综合建议按 5.5/10 处理。**

这不是要求立即重写的状态。更合适的判断是：当前代码可以继续迭代，但在进入大量敌人、武器、房间和 UI 内容生产前，应先完成一次“边界收紧”工作，否则后续新增内容会继续把 `Core`、`Assembly-CSharp` 和几个控制器变成默认依赖中心。

## 2. 审查方法与基线

- Unity 项目根目录已确认：`Zephyr(AI)/Zephyr`。
- 工作区存在用户已有未提交修改；本次未修改任何现有代码、场景、Prefab、SO 或资源。
- 代码规模：`247` 个 C# 文件；按当前 asmdef 归属统计，运行时代码约 `230` 个文件、约 `21,000` 行。
- ScriptableObject 定义脚本约 `89` 个，现有 SO 资产约 `139` 个。
- 场景 `13` 个，Prefab `19` 个左右，主要运行时内容已经按 Core、Player、Gameplay、UI、Testing 形成目录分组。
- 现有 asmdef 文件共 `8` 个：5 个运行时程序集、3 个 Editor 程序集。架构文档的基线表仍写成“Seven asmdefs”，与仓库当前数量不一致。
- 当前 Unity Console 快照：`1` 个 Error、`30` 个 Warning。Error 来自 Unity Services 配置初始化；Warning 主要是 `WeaponRuntime` 在 Animator 未绑定 Controller 时写参数。它们不是本报告的主要评分依据，但说明当前运行环境不是零噪声基线。

## 3. 当前实现的模块拆分

下面先按**逻辑职责**拆分，再指出其当前物理落点。逻辑模块和 asmdef 不是一一对应关系，这正是当前架构的主要问题之一。

| 逻辑模块 | 当前主要目录/命名空间 | 当前物理程序集 | 主要职责 | 依赖特征 |
|---|---|---|---|---|
| Foundation / Contracts | `Core/Interfaces`、`Core/Constants`、`Core/Types`、`Core/Events` | `Zephyr.Core` | 接口、枚举、事件通道、公共值类型 | 应最稳定；当前与大量运行时服务同程序集 |
| State Machine | `Core/StateMachine`、`Player/StateMachine` | Core.StateMachine + Player | 状态、Transition、Action/Condition SO、运行时状态实例 | 基础状态机边界清楚；Player 行为仍直接依赖大量 Core 类型 |
| Input | `Inputs`、`Player/Input` | Inputs + Player | Input System 生成代码、输入读取和状态机消费 API | 方向正确；生成代码位于全局命名空间 |
| Flow / Lifetime | `Core/Flow`、Bootstrap、Scene Controllers | `Zephyr.Core` | GameState、启动链、Persistent/GameManager、场景加载、流程状态 | 关键枢纽；直接知道 Meta、UI、Scene 类型和部分业务状态 |
| Data / Save / Meta | `Core/Save`、`Core/Meta`、`Core/DataLayer` | `Zephyr.Core` | MetaData、RunData、JSON 存档、Settlement、升级、故事进度 | 业务价值高，但与 Flow、Player UI 直接通过单例连接 |
| Stats / Combat | `Core/Stats`、`Core/DamageSystem`、`Core/Health`、`Core/Combat` | `Zephyr.Core` | 属性、伤害计算、生命、目标可用性、战斗接口 | Core 内聚性尚可；跨玩家/敌人使用较多 |
| Weapons / Projectiles / Items | `Core/Weapons`、`Core/Projectiles`、`Core/Items`、部分 `Weapons` | `Zephyr.Core` + Assembly-CSharp | 武器数据/运行时、命中框、投射物、掉落/拾取 | 实际是多个可独立演化的领域，却被 Core 合并 |
| Player Runtime | `Player/Core`、`Flow`、`Interaction`、`Visual`、`UI`、`StateMachine` | `Zephyr.Player` | 玩家移动、输入、死亡、生成、动画、交互、状态行为 | 依赖 Core 很深；Player UI 又反向连接 Flow/Save/Localization/Weapons |
| Enemy Runtime | `Gameplay/Enemies` | `Assembly-CSharp` | EnemyBrain、Motor、Stats、Attack、Boss 行为和敌人 SO | 依赖 Core；通过 Tag 查找 Player，运行时目标边界较弱 |
| Levels / Rooms / Interaction | `Core/Levels`、`Gameplay/Levels`、`Gameplay/Interaction` | Core + Assembly-CSharp | Room、CameraBounds、场景进度、门、MetaHub 交互 | 目录和命名空间分裂，直接调用 Flow/SceneManager/查找 API |
| Camera | `Core/Camera` | `Zephyr.Core` | CameraController、Bounds、Zone、跟随和镜头构图 | 归入 Core 可运行，但对领域对象识别使用反射/组件查找 |
| UI / Localization / Dialogue | `Core/UI`、`Core/Localization`、`UI`、Player UI | Core + UI + Player | RuntimeUIFactory、菜单、HUD、对话、本地化 | UI 与业务服务和 Player 运行时互相渗透 |
| Audio / Timer / Pool / Logging | `Core/Audio`、`Timer`、`ObjectPool`、`Logging` | `Zephyr.Core` | 跨场景服务和基础运行时服务 | 适合做基础设施，但目前多以全局单例暴露 |
| Testing / Editor Tooling | `Testing`、各处 `Editor` | Assembly-CSharp + Editor asmdef | 灰盒搭建、资产导入、截图、测试辅助 | Editor 代码未全部隔离；部分运行时脚本直接引用 UnityEditor |

## 4. 当前编译边界

按“最近的 asmdef 目录”计算，当前运行时代码大致如下：

| 程序集 | 文件数 | 估算行数 | 判断 |
|---|---:|---:|---|
| `Zephyr.Core` | 114 | 10,028 | 过宽；基础设施和业务服务混在一起 |
| `Zephyr.Core.StateMachine` | 13 | 1,090 | 边界最清楚，值得保留为独立基础程序集 |
| `Zephyr.Player` | 65 | 5,933 | 目录内聚，但对 Core 和 UI 依赖较深 |
| `Zephyr.Inputs` | 1 | 838 | 只是生成 wrapper，作为独立程序集的价值有限但无害 |
| `Zephyr.UI` | 2 | 181 | 过薄，且 UI 主要逻辑仍在 Core/Player/Gameplay |
| `Assembly-CSharp` | 35 | 3,271 | Enemy、Levels、Interaction、Testing 的运行时/工具代码未形成边界 |

这意味着项目现在存在两个不同的架构：

- **目录/命名空间架构**：看起来已经按领域拆分。
- **编译依赖架构**：真正强约束的只有 Core、StateMachine、Player、Inputs、UI；敌人、房间和交互仍可自由互相引用。

对于大型内容生产，第二层更重要。没有 asmdef 的目录只是约定，不是边界。

## 5. 模块间耦合矩阵

标记含义：`低` = 通过稳定接口或事件交互；`中` = 有少量直接类型依赖；`高` = 直接引用多个具体服务/控制器或依赖全局单例；`结构性高` = 当前必须重构边界，而不是简单减少 using。

| 来源 → 目标 | Core | StateMachine | Player | Enemy | Levels/Interaction | UI | Data/Save/Meta |
|---|---|---|---|---|---|---|---|
| Flow | 高 | 低 | 中 | 低 | 中 | 高 | 高 |
| Player | 高 | 高 | 内部 | 低/间接 | 中 | 中 | 高 |
| Enemy | 高 | 低 | 中（Tag/目标能力） | 内部 | 低 | 低 | 中 |
| Levels/Interaction | 高 | 低 | 中 | 中 | 内部 | 中 | 高 |
| UI | 高 | 低 | 中 | 低 | 中 | 内部 | 高 |
| Weapons/Combat | 高（当前同程序集） | 中 | 中 | 中 | 低 | 低 | 中 |
| Data/Save/Meta | 高（当前同程序集） | 低 | 中 | 低 | 中 | 中 | 内部 |

### 主要耦合热点

1. **Player → Core：高**。Player 状态 Action/Condition、Movement、Death、HUD、Spawner 直接使用 Flow、Stats、Health、Save、Weapons、Camera、UI 等多个 Core 子系统。
2. **Flow → 业务服务：高**。`GameFlow` 直接调用 `RunEndSettlement` 和 `GameplayTimePause`，并把 Core SceneSO、GameManager、MetaHub、Tutorial、Run 等具体概念集中在一个类中。
3. **PlayerDeathController → 多模块：结构性高**。一个类同时处理 Health 事件、Potential 损失、SaveSystem 写盘、GameFlow 状态跳转、PlayerSpawner、Input、StateMachine 禁用、DeathView 实例化和 Rigidbody 状态。
4. **MainMenuController → UI/Flow/Save/Localization：高**。它既是 UI 构建器，又是存档槽位用例控制器、语言设置入口、流程跳转入口和退出游戏入口。
5. **Enemy → Player：中到高的运行时耦合**。`EnemyBrain` 每帧失去目标时通过 `GameObject.FindGameObjectWithTag("Player")` 获取玩家；这不是编译依赖，但属于生命周期和可测试性耦合。
6. **Core 内部自耦合：高**。由于 Flow、Meta、Save、UI、Camera、Weapons 全在同一程序集，编译器无法阻止它们互相引用，导致 Core 逐渐成为“所有东西都能放进去”的容器。

## 6. 逐模块架构评价

### 6.1 Foundation / Interfaces / Events

**评价：方向合理，边界尚未完全收紧。**

`IDamageable`、`IStatSource`、`IFacingProvider`、`IPooledObject`、`IInteractable` 等接口为 Player、Enemy、Weapon 和 Item 提供了正确的交互入口；事件通道 SO 也适合 Unity Inspector 装配，能够减少场景对象之间的直接引用。

主要问题是接口和实现都在 `Zephyr.Core` 这个大程序集里，Core 还不是“纯合同层”。此外项目同时存在 SO 事件通道和运行时 `EventBus`，两套消息机制的适用边界没有在代码层强制表达，后续容易出现同一类事件有两种发布方式。

**结论：7/10。** 保留设计，建议把 Contracts、Events、基础值类型逐渐抽成更小的 `Zephyr.Core.Contracts` 或至少限制依赖方向。

### 6.2 State Machine

**评价：当前最干净、最有架构价值的模块之一。**

`Zephyr.Core.StateMachine` 有独立 asmdef，运行时 Action/Condition 通过 `CreateAction`/`CreateCondition` 创建实例，并在绑定到 StateMachine 时初始化。这符合“SO 是共享配置，运行时状态不可共享”的要求。Player 的 46 个状态机相关命名空间声明说明该模块已经支持较丰富的行为组合。

风险在于：Player Action/Condition 直接引用大量 Player Core 组件和 Core 服务；这对 Player 专属行为是合理的，但基础状态机程序集本身对 UnityEngine/Editor 的边界需要持续保护。当前 `StateMachine.cs` 还直接注册 `UnityEditor.AssemblyReloadEvents`，说明运行时基础设施与 Editor 生命周期存在耦合，必须确认所有 Editor 代码均由条件编译隔离。

**结论：8/10。** 不建议改状态机运行时契约；优先补测试和 Editor/Runtime 编译隔离。

### 6.3 Input

**评价：适配层思路正确，但生成代码和运行时消费层不够清晰。**

`PlayerInputReader` 将生成的 `ZephyrInputActions` 转换成状态机可消费的属性，避免 StateAction 直接读 Input System，这是正确的依赖方向。问题是生成 wrapper 处于全局命名空间，并且 Input asmdef 只包含一个大文件；它更像工具生成物，不是真正的输入领域模块。

**结论：7/10。** 保留 InputReader；可增加 `IPlayerInput`/只读输入快照，进一步让状态机脱离 InputAction 回调类型。

### 6.4 Flow / Lifetime / Scene Loading

**评价：生命周期规则已经有明确中心，但中心过胖。**

`SceneLoader` 通过 SceneSO、事件通道、队列和 Additive 加载来统一场景操作，整体方向合理；`GameFlow` 也集中定义了状态迁移，避免普通业务直接到处调用 `SceneManager.LoadScene`。这是当前项目最重要的架构资产。

问题是 `GameFlow` 同时负责状态机、场景目标映射、Run settlement、暂停时间、进度场景过渡，并通过 `Instance` 让所有模块直接调用。`SceneLoader` 还直接知道 `GameManagerBootstrap` 和多个 Room/Level SceneId，这使基础场景加载器带有具体游戏业务。

另外，`ColdStartupBootstrap` 是合理的 Editor 直开兼容层，但它增加了第二条入口路径；应严格限制为 Editor/测试用途，不要让它逐渐成为正式启动流程的替代实现。

**结论：6.5/10。** 方向正确，建议拆成 `GameStateService`、`SceneTransitionService`、`RunFlowCoordinator` 三个职责，保留一个很薄的外部 façade。

### 6.5 Save / Data / Meta

**评价：数据分层概念清楚，运行时访问方式耦合偏重。**

`SaveSystem` 对 MetaData、RunData 做 DTO 转换，负责 Slot、JSON、schema 数据，这一层职责基本合理。`StatsCore` 也明确区分 Profile、Run、Temporary 多种生命周期，说明数据模型有较强的设计意识。

问题是 SaveSystem 作为全局 MonoBehaviour 被 Player、UI、Flow、Meta 直接访问；`PlayerDeathController` 甚至直接修改 `CurrentRun` 并立刻 SaveRun。这样会把业务决策、数据变更和磁盘持久化绑在一起，未来更换存储介质或增加存档事务时成本较高。

**结论：7/10。** 保留 DTO 和生命周期模型；增加 `ISaveSession`/`IRunState` 之类的只读/命令接口，让业务系统不直接持有 SaveSystem 的完整能力。

### 6.6 Stats / Combat / Health / Damage

**评价：内聚性较好，是 Core 中最像“基础领域库”的部分。**

`StatsCore`、`HealthComponent`、`DamageCalculator`、DamageInfo/Result 和接口契约形成了相对稳定的组合；Player、Enemy、Weapon 都能共享，不依赖具体 Player 或 Enemy 类型，符合可复用基础模块的目标。

主要问题不是算法边界，而是所有这些能力与 Flow、UI、Save、Weapons 共处 Core，导致物理边界不清。没有测试程序集也意味着这些最值得锁定的契约没有自动化保护。

**结论：8/10。** 优先抽成稳定的 `Combat`/`Stats` 程序集或至少限制其引用，随后补单元测试。

### 6.7 Weapons / Projectiles / Items

**评价：数据驱动方向好，但模块归属不合理。**

WeaponSO、WeaponRuntime、WeaponController、WeaponComponent 和命中框等内容已经形成较完整的运行时模型；Projectile/Drop/Item 也有池化和接口抽象的基础。

当前把武器、投射物、物品都放进 Core，等于把高变化率的玩法内容放进低变化率基础设施；与此同时 `Core.Weapons.Editor` 依赖整个 `Zephyr.Core`，进一步放大了耦合。武器还在 Animator 未绑定 Controller 时产生大量 Warning，说明运行时能力对视觉装配存在隐式前置条件。

**结论：6.5/10。** 应将公共 Combat Contract 留在 Core，把 Weapon/Projectile/Item runtime 迁到独立领域程序集。

### 6.8 Player

**评价：内部子模块较多，但外部依赖过深。**

`MovementCore` 将 Rigidbody 写入集中在 `FixedUpdate`，并通过 `PlayerInputReader` 与状态 Action 解耦，这一点很好。Player StateMachine 的 SO 化也支持扩展。

问题集中在流程和 UI：`PlayerSpawner` 直接监听 Unity SceneManager 和 GameFlow；`PlayerDeathController` 同时负责战斗结果、存档、流程、UI 和对象替换；Player HUD 直接访问 Flow、Save、Localization、Weapons。Player 不是单一领域，而是把实体、生命周期、表现和流程都放进一个程序集。

**结论：6/10。** 建议至少拆成 Player Runtime、Player Flow、Player Presentation 三个物理边界；Player Flow 只通过稳定服务接口操作 Save/Flow。

### 6.9 Enemy / Boss

**评价：组件分工初步合理，编译边界和目标解析不够干净。**

`EnemyBrain`、`EnemyMotor`、`EnemyStats`、`EnemyAttackController` 和 Boss 专属控制器体现了“数据 + 组件 + 行为”的组合，避免了一个全能 EnemyController，这是好的方向。Boss 还使用 `IPotentialAttackSource`，说明跨系统交互已有接口意识。

主要问题是整个模块位于 `Assembly-CSharp`，没有编译期隔离；EnemyBrain 在 `Update` 中使用 Tag 搜索 Player，Boss 行为里也直接操作多个具体组件。这样会让敌人越来越依赖当前 Player 实现和场景标签，测试时也难以注入目标。

**结论：6/10。** 建议先建立 `Zephyr.Enemies` asmdef，使用 `ITargetProvider`/`ICombatTargetRegistry` 替代 Tag 搜索，再把 Boss 特殊逻辑留在 Enemy 领域内部。

### 6.10 Levels / Rooms / Interaction

**评价：职责存在，但边界最混乱之一。**

`RoomController`、CameraBounds、SceneProgressionController、Door、GustInteractable、MetaUpgradeStation、RunStartInteractable 等内容同时散落在 Core 和 Gameplay。`SceneProgressionController` 虽然通过 GameFlow 请求场景转移，尊重了 SceneLoader 的权威，但仍然直接枚举 `SceneManager` 并根据字符串/SceneId 推断逻辑状态。

交互组件中还存在 `Transform.Find`、动态创建 GameObject、直接查找 MetaHub/Player 等行为。这些做法在灰盒阶段可接受，但不适合作为长期架构边界。

**结论：5/10。** 应建立 `Zephyr.Rooms`/`Zephyr.Gameplay.Interaction` 边界，场景进度只依赖 `IProgressionService`，交互对象只依赖接口。

### 6.11 UI / Localization / Dialogue

**评价：有本地化意识，但 UI 仍深度绑定业务和运行时构建。**

LocalizationManager、Dialogue SO/Manager、HUD 和 DeathView 的方向基本正确；可替换 Prefab 和事件回调也比把 UI 逻辑写进场景按钮更可维护。

不过 `MainMenuController` 位于 Core.Flow.Scenes，同时负责创建 Canvas、创建所有按钮、读写 SaveSystem、切换语言、跳转 GameFlow 和退出 Editor。`RuntimeUIFactory` 直接依赖旧 UGUI `Text` 和 OS 字体回退，UI 稳定性与资源管线没有完全分离。`Zephyr.UI` asmdef 只有两个小脚本，真正的 UI 逻辑反而在 Core、Player、Gameplay 中。

此外，架构文档要求玩家可见文本统一经过 Localization，但代码里仍能看到直接 Debug 文本和 fallback 文案；这首先是架构纪律问题，其次才是内容问题。

**结论：5.5/10。** UI 应从 Flow/Core 中抽离，Controller 只做用例调用，布局和文案尽量回到 Prefab/Localization 资产。

### 6.12 Camera

**评价：功能边界合理，但归属和依赖方式需要收紧。**

CameraController、CameraBounds、CameraZone 形成了相对完整的场景局部配置方案，符合项目对 Cinemachine/房间边界的设计目标。

当前 Camera 放在 Core，且 Controller 使用反射式组件查找和目标对象探测；这让基础设施知道具体对象形态。更干净的方式是只依赖 `ICameraTarget`、`IFacingProvider`、`ICameraZoneSource` 等接口，让 Player/Room 提供适配器。

**结论：7/10。** 保留运行时能力，收窄目标接口，不要让 Camera 继续认识 Player 具体组件。

### 6.13 Testing / Editor Tooling

**评价：工具很活跃，但测试与生产代码隔离不足。**

Testing 目录承担灰盒场景搭建、截图和资产配置，这是当前开发阶段有价值的生产力工具。但没有测试 asmdef，也没有发现 NUnit/UnityTest 用例；另外 `TutorialPhase1TilemapBuilder.cs`、`TutorialPhase1CaptureExporter.cs` 等非 `Editor` 子目录脚本直接引用 UnityEditor，说明 Editor 边界不完整。

`GridAligner.cs` 和 `MainMenuController.cs` 中发现未被 `#if UNITY_EDITOR` 包裹的 UnityEditor API 使用点；`SceneSO.cs` 和 StateMachine 的相关调用目前已做条件编译，但仍应继续保持这种隔离方式。即使当前 Editor 环境能运行，未包裹的 UnityEditor 依赖也会削弱 Player 构建可靠性和程序集边界。

**结论：3.5/10。** 先清理 Editor/Runtime 混合，再建立 Tests asmdef 和最小架构契约测试。

## 7. 架构中做得好的部分

1. **场景加载权威意识正确**：普通流程大多通过 `GameFlow -> SceneLoader`，而不是业务类直接 LoadScene。
2. **SO 配置与运行时实例分离**：状态 Action/Condition 的 materialization 是合理的，避免把可变状态存入共享 SO。
3. **基础接口抽象有价值**：Damage、Stats、Facing、Pooling、Interaction 等接口能支撑 Player/Enemy/Weapon 复用。
4. **Movement 物理权威集中**：`MovementCore` 将运动写入集中到 FixedUpdate，状态行为只提供输入/速度意图。
5. **数据生命周期意识较强**：Profile/Run/Temporary 的 Stats 分层、MetaData/RunData 分离、DTO 存档都是长期可维护性的正向基础。
6. **事件通道适合 Unity 装配**：Scene Load 和 GameState 的 SO 事件减少了场景对象之间的硬引用。

## 8. 主要问题分级

### P0：进入大规模内容生产前应处理

- **程序集边界不完整**：Enemy、Levels、Interaction 等关键领域落在 `Assembly-CSharp`，无法编译期阻止跨域依赖。
- **Editor API 混入 Runtime 脚本**：`GridAligner.cs:43-54`、`MainMenuController.cs:482` 等位置应隔离或迁移；`SceneSO.cs:28-49` 当前已有条件编译，不列为同等级问题。
- **缺少测试程序集和架构契约测试**：Stats、Damage、StateTransition、Save、Settlement 等关键基础设施没有自动回归保护。

### P1：建议在后续两三个功能迭代内处理

- **Core 过宽**：`Zephyr.Core` 约 114 个 runtime 文件、约 10,028 行，业务服务与基础设施无法独立演化。
- **高耦合控制器**：`PlayerDeathController`、`MainMenuController`、`GameFlow` 需要拆分编排职责。
- **全局单例过多**：至少 14 个 `Instance`/单例模式命中点，导致依赖隐藏在运行时查找中。
- **运行时查找**：EnemyBrain、WeaponPickup、CurrencyPickup、Interaction、Menu 等存在 Tag/Find/Transform.Find；虽然不少不是热路径，但削弱可测试性和装配可见性。

### P2：持续治理项

- **命名空间、目录和程序集名称不完全一致**：例如 Player 目录使用 `Zephyr.Gameplay.Player`，UI、Core、Gameplay 的逻辑分布不一致。
- **日志和用户文本边界混杂**：`Debug.Log`、中文 Debug 文案、Localization fallback、UI fallback 同时存在。
- **运行时动态 UI 构建过多**：让 Prefab 替换、布局审查、本地化和测试难度增加。
- **文档基线滞后**：设计文档声称 7 个 asmdef，但当前仓库已有 8 个；“implemented”与“integrated”也需要在审查报告中持续区分。

## 9. 建议的目标架构

不建议一次性重写。推荐保持现有运行时契约，分阶段把现有逻辑边界转换为编译边界：

```text
Zephyr.Core.Contracts       (接口、事件 payload、值类型、极少量枚举)
Zephyr.Core.StateMachine    (现有状态机基础设施)
Zephyr.Core.Runtime         (Timer、Pool、Logging、基础生命周期服务)
Zephyr.Core.Combat          (Stats、Damage、Health、Combat contracts)
Zephyr.Core.Flow            (GameState、SceneSO、SceneLoader、流程 façade)
Zephyr.Core.Persistence     (Save DTO、存档端口、Meta/Run session)

Zephyr.Inputs               -> Contracts
Zephyr.Player               -> Contracts, StateMachine, Combat, Flow, Inputs
Zephyr.Enemies              -> Contracts, StateMachine, Combat
Zephyr.Weapons              -> Contracts, Combat, Runtime
Zephyr.Projectiles          -> Contracts, Combat, Runtime
Zephyr.Items                -> Contracts, Combat, Runtime, Persistence(仅命令接口)
Zephyr.Rooms                -> Contracts, Flow, Camera API
Zephyr.Camera               -> Contracts, Runtime
Zephyr.UI                   -> Contracts, Flow API, Persistence API, Localization API
Zephyr.Tests                -> all public test seams
```

关键原则：

- Core 不再承载高变化率领域内容；只保留合同、稳定基础设施和薄的服务端口。
- Domain 之间不直接引用具体 Controller；跨域读取用接口，跨域通知用事件。
- `GameFlow` 只负责状态和流程命令，不直接完成 Settlement、存档写盘或构建 UI。
- `PlayerDeathController` 拆为 `PlayerDownUseCase`、`PotentialLossService`、`DeathPresentationController`、`RespawnCoordinator`。
- `MainMenuController` 拆为 `MainMenuView`、`SaveSlotPresenter`、`MainMenuUseCase`、`SettingsPresenter`。
- `EnemyBrain` 使用注入的目标源/目标注册表，不在 Update 中按 Tag 搜索 Player。
- Editor 工具全部位于 `Editor` 文件夹或 Editor-only asmdef，Runtime 程序集不引用 UnityEditor。

## 10. 推荐重构顺序

### 阶段一：先建立安全网

1. 修正架构文档中的 asmdef 基线和当前实现快照。
2. 创建 `Zephyr.Tests`、`Zephyr.Architecture.Tests`，至少覆盖 Damage、Stats、StateTransition、Save DTO、Settlement。
3. 清理 UnityEditor 混入 Runtime 的脚本，先保证 Player build 可编译。
4. 统计并固定当前单例、SceneManager、Find、Resources.Load 的允许清单。

### 阶段二：切开最明显的边界

1. 新建 `Zephyr.Enemies`、`Zephyr.Rooms`、`Zephyr.Weapons`、`Zephyr.Projectiles`、`Zephyr.Items` asmdef。
2. 让这些程序集先只依赖 `Zephyr.Core`，不引入新的跨域依赖。
3. 将 `Gameplay/Enemies` 从 Assembly-CSharp 迁入 Enemies 程序集；迁移过程中让编译器暴露真实耦合。
4. 把 `Gameplay/Levels`、`Gameplay/Interaction` 的场景用例依赖改为 Flow/Contracts 接口。

### 阶段三：拆高耦合编排器

1. 先拆 `PlayerDeathController`，因为它连接最多生命周期和业务系统。
2. 再拆 `MainMenuController`，把 View/UseCase/Save Slot/Settings 分开。
3. 最后收窄 `GameFlow` 和 `SceneLoader`，让 SceneLoader 不知道具体 Room/Meta/Player 业务。

### 阶段四：收紧 API 和运行时查找

1. 对 Core 内部类型默认改为 `internal`，只把接口、数据契约和稳定服务端口设为 public。
2. 用 `[RequireComponent]`、Inspector 引用、初始化绑定或注册表替代运行时 Find/Tag 搜索。
3. 删除未使用的全局单例，保留真正具有会话生命周期的服务，并通过显式入口暴露。
4. 将 Runtime UI 大部分还原为 Prefab + Presenter，减少动态创建布局和字体回退。

## 11. 最终判断

Zephyr 当前最值得保留的是“数据驱动 + SO + 事件 + 稳定 Core 合同 + Player/Enemy 组件化”的方向；最需要避免的是继续把新功能直接塞进 `Zephyr.Core`、`Assembly-CSharp` 或现有几个全能 Controller。

如果只做一件事，优先做**asmdef 边界和测试安全网**，不要先做大规模命名整理。因为当前最大的问题不是文件名不漂亮，而是编译器没有替项目阻止错误依赖。边界建立后，很多“应该怎么拆”的问题会由编译错误自动暴露，重构风险也会明显下降。

本报告不建议立即修改 StateMachine 基础设施；它目前是相对干净的部分，应作为被依赖的稳定内核，而不是继续扩展成所有领域共享的万能框架。


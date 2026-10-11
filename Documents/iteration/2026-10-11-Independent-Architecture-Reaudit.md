# Zephyr 全局架构盲审复评

日期：2026-10-11  
范围：当前仓库全部运行时代码、程序集定义、核心场景生命周期、UI/本地化边界、存档/Meta、战斗、玩家、敌人、武器、道具、投射物、房间、Audio、Camera 和架构测试。  
本轮原则：**不继承前几轮的评分，不以“历史阶段已验收”作为当前架构质量证据，只按当前实现重新评估。**

## 1. 直接回答

按严格的全局架构清洁度标准，当前实现**不能诚实地评为 9.0 以上**。

本轮独立复评分数：**8.1 / 10**。

这不是说项目当前不可用。当前实现已经从“大型无边界 Core”进入“有明确程序集分层、可继续开发、但兼容层和运行时耦合仍明显”的阶段。`36/36` 架构测试和 `5/5` EditMode 测试全部通过，说明已建立的边界没有明显回归；但测试全绿不能证明所有旧耦合已经清除，也不能替代对职责归属和运行时组合方式的审查。

前几轮的 `9.x` 分数可以理解为**某个阶段修理完成度**，而不是“整个游戏已经达到 9 分以上的最终 architecture cleanliness”。如果现在重新从零开始审查，我不会继续使用 9.x 作为全局结论。

## 2. 评分标准

本轮只评价 architecture correctness 和 cleanliness，不评价玩法完成度、美术完成度、数值平衡或代码运行速度。

| 维度 | 权重 | 9 分的含义 |
|---|---:|---|
| 模块边界与依赖方向 | 25% | 依赖图单向、职责边界清楚，没有大型兼容宿主成为公共枢纽 |
| 命名空间与契约清洁度 | 15% | Contracts、Runtime、Domain、Presentation 的命名与程序集归属一致 |
| 生命周期与组合根 | 20% | 服务由明确 owner 组装，业务代码不扩散单例读取和全局查找 |
| 场景、UI、资源边界 | 15% | 场景通过明确引用/事件组装，UI 可替换，业务逻辑不生成视图结构 |
| 可测试性与架构门禁 | 15% | 测试验证真实依赖图和运行时组合，而不是主要依赖字符串断言 |
| 演进成本与残余兼容层 | 10% | 兼容层很薄、可删除，新增模块不会继续依赖历史 Core |

## 3. 当前模块地图

### 稳定基础层

- `Zephyr.Core.Contracts`：接口、值类型、事件载荷和部分旧命名空间下的契约类型。
- `Zephyr.Core.Runtime`：EventBus、Timer、ObjectPool、Logging、生命周期和掉落数据。
- `Zephyr.Core.StateMachine`：状态机基础设施和编辑器工具。
- `Zephyr.Inputs`：Input System 封装。

### 主要运行时领域层

- `Zephyr.Core.Combat`：伤害、生命、Stats、Buff、战斗目标可用性。
- `Zephyr.Core.Persistence`：Save DTO、存档槽位和 JSON 持久化。
- `Zephyr.Core.Meta`：MetaGateway、升级、故事、结算和解锁。
- `Zephyr.Core.Flow`：GameFlow、SceneLoader、RoomController、场景控制器和 Bootstrap。
- `Zephyr.Core.Audio`：Persistent AudioManager、AudioEventSO 和音频总线。
- `Zephyr.Core.Camera`：CameraController、CameraBounds、CameraZone 和 Cinemachine 适配。

### 内容/表现层

- `Zephyr.Player`
- `Zephyr.Enemies`
- `Zephyr.Weapons`
- `Zephyr.Items`
- `Zephyr.Projectiles`
- `Zephyr.Rooms`
- `Zephyr.Gameplay.Interaction`
- `Zephyr.UI`

### 兼容宿主

- `Zephyr.Core` 仍然承载 Localization、UI 基础设施、Dialogue、共享常量、Grid 工具，以及少量跨系统兼容代码。

当前程序集数量为 27 个，其中包含运行时、编辑器和测试程序集。程序集数量本身不是质量指标；关键问题是 legacy `Zephyr.Core` 仍是多个运行时模块的显式依赖中心。

## 4. 模块逐项评分

| 模块 | 分数 | 独立评价 |
|---|---:|---|
| Contracts | 7.8 | 程序集无依赖是正确方向，但契约类型仍大量使用 `Zephyr.Core.*` 历史命名空间；Contracts 与 Flow/Damage/Stats 语义边界没有完全反映在命名上。 |
| Runtime | 8.7 | 对 Contracts 的单向依赖干净，EventBus、Timer、Pool 和 Lifecycle 已形成稳定基础；掉落数据放在 Runtime 是迁移后的合理兼容选择，但 namespace 仍保留旧 Core 语义。 |
| StateMachine | 8.8 | 基础设施独立、Player/Enemy 复用同一套实现，方向合理；编辑器程序集和运行时程序集分离良好。 |
| Combat | 8.0 | 程序集依赖稳定，但 Stats、Health、Buff、Damage 的历史 namespace 仍跨 Contracts/Combat 混杂，增加了理解和继续拆分的成本。 |
| Persistence | 8.4 | Save gateway 和 DTO 分层已明显改善；具体 SaveSystem 仍被多个上层组件在初始化边界读取，尚未形成统一组合根。 |
| Meta | 7.9 | MetaGateway 已减少直接存档调用，但 Meta 程序集仍依赖 Persistence 模型，故事服务仍通过 EventBus singleton 获取运行时基础设施。 |
| Flow | 7.6 | SceneLoader/GameFlow/Bootstrap 的职责大体集中，但 Flow 内部仍有大量 `GameFlow.Instance`、`SaveSystem.Instance`、`EventBus.Instance`，说明组合根边界没有完全落地。 |
| Audio | 8.8 | 已从 Core 独立，Persistent owner 和生命周期声明清晰；静态 Instance API 仍存在，但当前没有发现新的业务扩散。 |
| Camera | 8.2 | 已从 Core 独立，Cinemachine 适配隔离较好；主相机和 bounds 仍通过 `FindObjectsByType`、场景路径和运行时扫描选择，隐式耦合较高。 |
| Weapons | 7.8 | 与 Items/Enemies 的迁移桥已清理，WeaponSO/Runtime/Component 方向合理；WeaponPickup 仍直接读取 SaveSystem 和 EventBus，且保留大量旧 Core namespace。 |
| Projectiles | 8.0 | 作为独立程序集依赖 Combat/Runtime 是合理的；对 Core 的残余引用主要是旧接口/Pool/Timer namespace，边界还没有完全语义化。 |
| Items | 7.7 | 迁移桥已移除，CurrencyPickup 独立；运行时仍直接读取 SaveSystem/EventBus，且 Item 逻辑与 Core host 的历史 namespace 关系较重。 |
| Enemies | 7.9 | 已不再引用 Items/Weapons，复用 StateMachine 的方向正确；EnemyBrain 仍有 EventBus singleton 读取，EnemySO/Stats 继续使用旧 Core 根命名空间。 |
| Rooms | 7.2 | SceneProgressionController 的 GameManager 生命周期明确，但 TutorialObjectiveExit 仍用全局对象搜索和遍历 MonoBehaviour 来找 RoomController/目标接口，属于明显运行时组合耦合。 |
| Player | 7.5 | Player 的职责覆盖输入、运动、战斗、死亡、生成和 UI，依赖面很宽；已有部分 gateway 缓存，但仍有 16 次左右跨服务 singleton 读取，Player 仍是系统集成压力最大的模块。 |
| UI | 7.1 | Localization gateway 已存在，但 Gameplay HUD 仍在运行时创建 Canvas、Bar、WeaponSlot 和 Text，视图结构与布局没有完全由可替换 Prefab/资源承担。 |
| Interaction | 7.4 | 交互模块已使用 Meta/Contracts 边界，但 RunStart、MetaUpgrade 等组件仍在 Awake 中直接解析 SaveSystem 或通过场景搜索控制器。 |
| Testing | 7.6 | 有架构测试和 EditMode 测试，但测试程序集本身引用面较宽，编辑器工具与运行时对象构建存在较强项目耦合。 |

## 5. 主要扣分证据

### 5.1 Legacy Core 仍是实际依赖枢纽

当前 `Zephyr.Core.asmdef` 虽然 `autoReferenced: false`，但仍被 12 个程序集显式引用，包括 Flow、Items、Projectiles、Weapons、Enemies、Interaction、Rooms、Player、Testing、Tests 和 UI。

这说明“禁止隐式依赖”已经实现，但“减少 legacy Core 的实际职责和扇出”还没有完成。`autoReferenced: false` 是审计门槛，不等于架构清洁本身。

更重要的是，项目中的 `Zephyr.Core` 根命名空间仍被 Combat、Contracts、Enemies、Player、UI 等代码直接使用。部分类型实际已经位于 Contracts 或 Combat 程序集，却继续以 `Zephyr.Core`、`Zephyr.Core.Stats`、`Zephyr.Core.DamageSystem`、`Zephyr.Core.Health` 的旧语义暴露。

这会造成两个问题：

1. 新开发者无法仅凭 namespace 判断类型属于哪个程序集。
2. 后续迁移容易继续把新类型放回 `Zephyr.Core`，因为旧 namespace 仍是最短路径。

### 5.2 Contracts 的程序集边界正确，但语义命名不干净

`Core/Contracts` 中的类型仍包括：

- `Zephyr.Core.Flow.GameState`
- `Zephyr.Core.DamageSystem.DamageInfo`
- `Zephyr.Core.Stats.StatModifierDefinition`
- `Zephyr.Core.StatType`
- `Zephyr.Core.Interfaces.*`

`IGameFlowGateway` 也直接 `using Zephyr.Core.Flow`。这不一定形成编译环，但说明“契约层”与“实现域 namespace”仍然混在一起。对于架构审查，namespace 是依赖意图的一部分，不能只看 asmdef 是否无引用。

### 5.3 Singleton 治理改善了生命周期，没有完全消除服务耦合

当前 singleton 已有 owner lifetime、claim/release 和场景归属检查，这是有效修复。但代码扫描仍发现大量运行时直接读取：

- Flow：约 28 次
- Player：约 16 次
- Meta：约 10 次
- Core host：约 11 次
- Enemies、Items、Weapons、Rooms、Interaction 等仍有直接读取

本轮扫描到的相关服务 singleton 调用约 96 次，排除测试文件后仍然广泛存在。典型例子包括：

- `Core/Flow/Scenes/LoadingRunController.cs` 多次读取 `GameFlow.Instance`、`SaveSystem.Instance` 和 `EventBus.Instance`。
- `Player/UI/GameplayHudController.cs` 直接读取 `SaveSystem.Instance` 和 `EventBus.Instance`。
- `Gameplay/Interaction/MetaUpgradeStation.cs` 在初始化时直接 `new MetaGateway(SaveSystem.Instance)`。
- `Core/UI/Dialogue/DialogueTrigger.cs` 同时直接读取 Save、Meta、DialogueManager 三类全局服务。

因此当前状态是“生命周期受控的全局服务”，不是“组合根注入完成的服务架构”。这两者不能按同一分数计算。

### 5.4 UI 仍存在业务脚本生成视图结构

`Player/UI/GameplayHudController.cs` 在运行时创建 `GameplayHUD` Canvas、Bars、Coins、Weapons 和 Text，并直接写入布局尺寸、颜色、字体大小和 fallback 文案。

这与项目架构规则要求的“UI 面板和 Popup 作为可替换 Prefab，视觉资源和布局不写死在脚本中”不完全一致。它可以运行，但从 architecture cleanliness 看，Controller 同时承担了：

- 数据订阅
- 生命周期管理
- UI 树创建
- 布局
- 文案 fallback
- 武器表现绑定

这是当前 UI 模块的最大扣分项之一。

### 5.5 Runtime scene discovery 仍有隐式耦合

当前仍存在运行时全局查找：

- `Gameplay/Levels/TutorialObjectiveExit.cs` 使用 `FindAnyObjectByType<RoomController>`，并遍历所有 `MonoBehaviour` 查找目标。
- `Gameplay/Interaction/RunStartInteractable.cs` 使用 `FindAnyObjectByType<MetaHubController>`。
- `Core/Camera/CameraController.cs` 扫描所有 CameraController、Transform 和 CameraBounds，并通过场景路径选择对象。
- `Core/Flow/SceneStatsModifierController.cs` 扫描场景中的 SceneContext。

这些不是简单的 Unity API 使用问题，而是对象组装责任不明确的问题：对象由谁提供、何时可用、缺失时如何失败，都没有完全由组合根或序列化引用表达。

### 5.6 架构测试覆盖有价值，但不能作为 9+ 的充分证据

当前架构测试全部通过，但其主要形式是读取 asmdef 和源码文本，然后对字符串执行 `Contains` / `DoesNotContain`。这能防止部分回归，却有明显限制：

- 不解析真实 C# 符号引用。
- 不验证运行时对象组合是否满足接口边界。
- 不会系统性禁止所有业务程序集直接读取 singleton。
- 不会检测 namespace 与程序集归属不一致。
- 不会检测 UI Controller 是否生成视图结构。
- 不会覆盖 Tutorial/MetaHub/Run 的完整运行时装配。

因此 `36/36` 应解释为“已写入的门禁全部通过”，不能解释为“整体架构没有结构性扣分”。

## 6. 维度复评分

| 维度 | 分数 | 说明 |
|---|---:|---|
| 模块边界与依赖方向 | 8.2 | 27 个 asmdef 和 Contracts/Runtime/Combat/Flow/Persistence/Meta 拆分已建立；legacy Core 仍有 12 个运行时入边，扇出仍明显。 |
| 命名空间与契约清洁度 | 7.7 | 程序集边界比 namespace 边界干净；Contracts 仍使用大量历史 Core namespace，语义边界不透明。 |
| 生命周期与组合根 | 7.8 | owner lifetime 治理有效；但 singleton 读取和场景发现仍大量存在，组合根尚未成为唯一服务解析位置。 |
| 场景、UI、资源边界 | 7.4 | SceneLoader 协议清楚；UI runtime construction 和 scene discovery 仍违反目标架构的可替换/显式组装原则。 |
| 可测试性与架构门禁 | 8.4 | 测试数量和回归门禁已经建立并全绿；测试仍偏源码字符串检查，运行时架装覆盖不足。 |
| 演进成本与兼容层 | 7.8 | 新增模块可以按 asmdef 工作，但新开发者仍必须理解 legacy Core 和旧 namespace 兼容层。 |
| **整体** | **8.1** | **可继续开发，但还不是 9+ 的全局干净架构。** |

## 7. 已确认的优点

本轮没有因为发现扣分项而否定已经完成的工作。以下部分确实达到较高质量：

- Contracts、Runtime、Combat、Persistence、Meta、Flow、Audio、Camera 已有明确程序集。
- Enemies/Items/Weapons 之间的迁移桥已清除，依赖方向比初始状态明显改善。
- Audio/Camera 已从 legacy Core 中独立，且保留序列化安全性。
- SceneLoader 是普通场景加载的主要 authority，未发现新的平行加载通道。
- singleton 生命周期声明和静态引用释放已有统一规则。
- Save/Meta 的 Gateway 已减少了最危险的业务层直接存档耦合。
- StateMachine 没有被新敌人/玩家逻辑复制成第二套实现。
- 当前 Unity 编译状态正常，Console 最终 Error/Warning 为 0。

## 8. 达到 9.0 以上的必要条件

### P0：收窄 legacy Core

目标不是继续增加 asmdef 数量，而是让 `Zephyr.Core` 只保留真正无法下沉的组合/兼容适配器。

- 将 Localization、UI foundation、Dialogue、Grid、共享常量拆成明确职责边界，或明确它们全部属于 Composition Host。
- 不再让 Combat/Contracts/Player/Enemies 通过旧 `Zephyr.Core` 根命名空间获取已迁移类型。
- 将 `Zephyr.Core` 的运行时入边从当前 12 个逐步降到少数组合根/兼容边界。

### P0：消除业务 singleton 读取扩散

- Flow、Player、Interaction、Rooms、Items、Weapons、Enemies 的服务依赖在组合边界一次解析。
- 业务方法内部只依赖接口或已缓存的 gateway。
- 架构测试改为扫描所有运行时程序集的 concrete `*.Instance` 使用，而不是只保护 Save/Meta 的部分消费者。

### P1：修正 Contracts namespace

- 将 Contract 类型移到与程序集职责一致的 namespace，例如 `Zephyr.Contracts.Flow`、`Zephyr.Contracts.Combat`、`Zephyr.Contracts.Stats`。
- 通过兼容别名或一次性序列化迁移保护旧资源，不继续新增旧 namespace 类型。
- 明确 `IGameFlowGateway` 不应依赖名为 `Core.Flow` 的实现语义。

### P1：把 UI 结构交还给 Prefab/视图

- Gameplay HUD、Death View、MetaUpgrade Panel 等视图树由 Prefab 或专用 ViewFactory 提供。
- Controller 只绑定数据、调用 View API 和转发用户操作。
- 文案全部通过 Localization key/LocalizedString 进入，fallback 只作为受控资源层策略。

### P1：移除运行时全局场景扫描

- `RoomController`、`MetaHubController`、`SceneContext`、CameraBounds 等由组合根或序列化引用提供。
- 对象缺失时通过明确的诊断/状态报告失败，不依赖“扫描当前场景后碰巧找到”。

### P2：强化架构验证

- 解析 asmdef JSON 建立依赖图并检测禁止边。
- 使用 Roslyn/编译器符号或最小静态分析检测 concrete singleton 读取。
- 增加 Persistent → GameManager → Gameplay 的 PlayMode 装配测试。
- 增加 UI Prefab 引用和运行时对象归属检查。

## 9. 当前是否适合普通开发

适合，但必须遵守现有边界：

- 可以继续做新敌人、新武器、新房间、新掉落和普通 UI 内容。
- 新内容不得新增 singleton、全局场景查找、平行 SceneLoader 或新的 `Zephyr.Core` 兼容依赖。
- 如果功能需要修改 legacy Core、StateMachine、Persistent/GameManager 生命周期或 Contracts namespace，应作为架构专项单独立项。

## 10. 复评验证记录

- Unity 项目根目录：`D:/GitHub/Zephyr_v3/Zephyr(AI)/Zephyr`
- Unity 版本：`6000.5.6f1`
- Unity 状态：`IsPlaying: false`、`IsCompiling: false`、`IsUpdating: false`
- `Zephyr.Architecture.Tests`：**36/36 passed**
- `Zephyr.Tests.EditMode`：**5/5 passed**
- Console：本轮复评读取到 Error `0`、Warning `0`
- 本轮没有修改场景、Prefab、ScriptableObject 或运行时代码，因此没有生成 SceneView 截图。

## 11. 最终判断

如果问题是“项目现在是否已经比初始状态好很多”，答案是：**是**。

如果问题是“完全不带历史阶段偏见，按全局 architecture correctness 和 cleanliness 重新评分，是否能给 9 分以上”，答案是：**不能**。

当前最准确的判断是：**8.1/10，架构基础已经可用，模块化方向正确，但 legacy Core、singleton 读取、Contracts namespace、runtime UI construction 和 scene discovery 仍是实质性扣分项。**

在这些问题被处理后，再进行一次同样标准的盲审，才有充分依据讨论 9.0 以上。


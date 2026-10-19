# Phase 0：依赖账本与架构门禁基线

日期：2026-10-11；Unity `6000.5.6f1`  
范围：运行时程序集、程序集引用、namespace、服务生命周期、场景查找、场景加载、运行时 UI 构造和架构测试。  
本阶段性质：只读审计与门禁建立，不修改运行时代码、namespace、asmdef、场景、Prefab、ScriptableObject 或其他 Unity 序列化资源。

## 1. 结论

Phase 0 验收成功。

本阶段建立了后续架构修复所需的基线账本。当前代码可以进入分阶段修复，但不应评为全局 9.0+；独立基线仍为 **8.1/10**。Phase 0 不改变分数，只把扣分项变成可追踪、可复核的证据。

当前最大风险：

1. `Zephyr.Core` 仍是多个内容和表现程序集的直接依赖枢纽。
2. 运行时业务代码仍有大量 `*.Instance` 读取，组合根尚未完全接管服务解析。
3. Contracts 类型仍大量使用历史 `Zephyr.Core.*` namespace，程序集归属和语义归属不一致。
4. HUD、主菜单和对话仍保留运行时 UI 树构造。
5. Tutorial、Interaction、Camera 等仍通过 `FindAnyObjectByType` / `FindObjectsByType` 隐式发现场景对象。
6. 现有架构测试全部通过，但主要是源码字符串和固定 asmdef 断言，不能证明完整运行时组合已经清洁。

## 2. 审计结果

| 项目 | 结果 |
|---|---:|
| Unity 项目根目录 | `Zephyr(AI)/Zephyr` |
| 程序集定义总数 | 27（运行时/支持 20，Editor 5，Test 2） |
| C# 文件总数 | 274 |
| 运行时扫描 C# 文件 | 249（排除 `Editor`、`Tests`、`Testing`） |
| asmdef 引用边数 | 93 |
| `Zephyr.Core` 直接入边 | 12 |
| `autoReferenced: true` 的运行时/支持程序集 | 11 |
| `*.Instance` 静态读取 | 112 |
| 全局对象发现 API | 10 |
| `SceneManager` 加载/卸载调用 | 4 |
| 运行时 UI 构造模式 | 15 |
| `Zephyr.Core` namespace token | 523 |
| Console Error / Warning | 0 / 0 |

扫描按文件目录匹配最近的 asmdef，并统计高风险 API 和 namespace token。计数用于定位热点，不等同于完整语义调用次数；注释、兼容代码和反射代码需在修复阶段逐点复核。

验证结果：`Zephyr.Architecture.Tests` **36/36 passed**；`Zephyr.Tests.EditMode` **5/5 passed**；Unity Editor 未播放、未编译、未更新。

## 3. 程序集依赖账本

| 程序集 | 主要职责 | 直接依赖 | 判断 |
|---|---|---|---|
| `Zephyr.Core.Contracts` | 接口、事件载荷、值类型、枚举 | 无 | 方向正确；28 个文件仍使用历史 namespace |
| `Zephyr.Core.Runtime` | EventBus、Timer、Pool、Logging、Lifecycle | Contracts | 基础层稳定；语义命名仍旧 |
| `Zephyr.Core.StateMachine` | 状态机基础设施 | 无 | 边界清楚，按规则锁定 |
| `Zephyr.Core.Combat` | Damage、Health、Stats、Buff | Contracts、Runtime | 依赖合理；namespace 仍混杂 |
| `Zephyr.Core.Persistence` | Save DTO、槽位、JSON 存档 | Runtime | Gateway 已改善；仍需统一组合根解析 |
| `Zephyr.Core.Meta` | Meta、升级、故事、结算、解锁 | Contracts、Runtime、Persistence、Localization | 业务清楚；仍有 singleton 读取 |
| `Zephyr.Core.Flow` | GameFlow、SceneLoader、Bootstrap、场景控制器 | Contracts、Runtime、Persistence、Meta、Core | 场景集成集中；legacy Core 入边仍明显 |
| `Zephyr.Core.Audio` | Persistent Audio 与音频事件 | Runtime | 已独立，边界良好 |
| `Zephyr.Core.Camera` | Cinemachine、Bounds、Zone、震动 | 无 | 已独立；场景绑定仍偏隐式 |
| `Zephyr.Core` | Localization、Dialogue、旧 UI、常量、Grid、兼容宿主 | 多个 Core 层和 Unity 服务 | 过宽，实际公共依赖中心 |
| `Zephyr.Weapons` | Weapon SO、Runtime、Component、Pickup | Contracts、Runtime、Combat、Persistence、Core | 迁移桥已清理；仍依赖 legacy Core |
| `Zephyr.Items` | CurrencyPickup、Item 运行时 | Contracts、Runtime、Combat、Persistence、Core | 迁移桥已清理；仍依赖 legacy Core |
| `Zephyr.Projectiles` | Projectile 运行时 | Contracts、Runtime、Combat、Core | 方向正确；旧 namespace 仍在 |
| `Zephyr.Enemies` | Enemy、Brain、StateMachine 行为 | Contracts、Runtime、Combat、Core、StateMachine | 不再反向依赖 Weapons/Items |
| `Zephyr.Rooms` | 房间、教程、目标、进度 | Contracts、Runtime、Flow、Core | Flow 方向合理；全局发现危险 |
| `Zephyr.Player` | 输入、移动、战斗、生成、死亡、UI 集成 | Contracts、Runtime、Combat、Persistence、Meta、Flow、Core、Camera、StateMachine、Inputs、UI、Weapons | 集成面最宽，压力最大 |
| `Zephyr.Gameplay.Interaction` | 交互、RunStart、MetaUpgrade | Contracts、Runtime、Persistence、Meta、Flow、Core、Input System | 迁移改善；控制器/Save 解析仍需收敛 |
| `Zephyr.UI` | UI 组件和展示 | Contracts、Runtime、Combat、Core | 程序集存在；部分视图仍由其他层生成 |

`Zephyr.Core.asmdef` 的 `autoReferenced` 已为 false，这是有效门禁，但不是职责清空的证明。当前直接引用它的程序集为：`Zephyr.Core.Flow`、`Zephyr.Enemies`、`Zephyr.Gameplay.Interaction`、`Zephyr.Items`、`Zephyr.Player`、`Zephyr.Projectiles`、`Zephyr.Rooms`、`Zephyr.Testing`、`Zephyr.Testing.Editor`、`Zephyr.Tests.EditMode`、`Zephyr.UI`、`Zephyr.Weapons`。

## 4. 关键证据

### 4.1 Singleton 与组合根

- `Assets/Scripts/Core/Flow/GameFlow.cs:50-63` 直接读取 `SaveSystem.Instance` 和 `SceneLoader.Instance`。
- `Assets/Scripts/Gameplay/Interaction/RunStartInteractable.cs:22,50` 读取 Save singleton，并通过场景查找 MetaHub。
- `Assets/Scripts/Player/UI/GameplayHudController.cs:38-55` 直接读取 Save/EventBus，并自行初始化 UI 和订阅。
- `Assets/Scripts/Core/Meta/MechanicUnlockSystem.cs:15-23` 通过 Save/Story singleton 组合业务判断。
- `Assets/Scripts/Core/UI/Dialogue/DialogueTrigger.cs:28,46-80` 同时依赖 Save、Dialogue、StoryProgression singleton。

生命周期治理已经增加 owner/lifetime/release 机制，改善了“何时存在”；剩余问题是“谁解析服务”。这些读取降低替换能力和测试隔离度，下一阶段应优先收敛业务组件在 Awake/OnEnable 中的服务解析。

### 4.2 全局场景发现与加载

- `Assets/Scripts/Gameplay/Levels/TutorialObjectiveExit.cs:31-36` 查找 RoomController 并遍历所有 MonoBehaviour。
- `Assets/Scripts/Gameplay/Interaction/RunStartInteractable.cs:50` 查找 MetaHubController。
- `Assets/Scripts/Core/Camera/CameraController.cs:65,431,448` 扫描 CameraController、Transform、CameraBounds。
- `Assets/Scripts/Core/Flow/SceneStatsModifierController.cs:91` 扫描 SceneContext。
- 场景加载调用集中在 `SceneLoader` 和两个 Bootstrap，但仍需持续门禁化。

`SceneLoader` 权限集中是正确方向；场景内对象绑定仍依赖“当前场景恰好只有目标对象”，会增加 Prefab、测试场景和叠加场景的不确定性。Camera 扫描可作为过渡白名单，但不应扩散到普通业务。

### 4.3 UI 表现边界

`Assets/Scripts/Player/UI/GameplayHudController.cs:84-166` 创建 Canvas、CanvasScaler、GraphicRaycaster、Bars、WeaponSlot、Text 和 Image；`Assets/Scripts/Core/UI/RuntimeUIFactory.cs:10-62` 提供运行时 UI 工厂；`Assets/Scripts/Core/Flow/Scenes/MainMenuView.cs:29-112` 创建主菜单 UI 树，并在缺少 Prefab 时创建 EventSystem/Popup 退化视图。

这对灰盒阶段有实用价值，但不符合最终“Prefab/View 可替换、Controller 集中业务”的架构目标。结构、布局、字体、Sprite 和按钮层级应逐步移入 Prefab/Scene，脚本仅保留绑定和 Controller 调用。

## 5. Namespace 与职责账本

`Assets/Scripts/Core/Contracts` 下共有 29 个 C# 文件，其中 28 个文件的 namespace 不是 `Zephyr.Core.Contracts.*`，而是历史命名空间：

- `Zephyr.Core.Flow`：`GameState`、`SceneId`。
- `Zephyr.Core.DamageSystem`：`DamageInfo`、`DamageResult`、Damage/Weapon 相关枚举。
- `Zephyr.Core.Interfaces`：多数接口。
- `Zephyr.Core.Stats`：StatModifierDefinition。
- `Zephyr.Core`：StatType、MetaStatType、StatConstants。
- `Zephyr.Core.Events`：事件载荷。

程序集引用已经独立，所以这不一定造成编译错误；但 namespace 失去了“依赖意图”的表达能力，新代码很容易继续把已拆开的契约理解成 legacy Core 的一部分。`Assets/Scripts/Core/Combat` 也同时使用 `Zephyr.Core.Combat`、`Zephyr.Core.DamageSystem`、`Zephyr.Core.Health`、`Zephyr.Core.Stats` 和 `Zephyr.Core.Buff`，说明 Combat 仍是历史 namespace 迁移的中间状态。

## 6. 现有测试门禁的能力与缺口

### 已覆盖

- Contracts、Runtime、Combat、Flow、Persistence、Meta 的程序集存在性和部分依赖方向。
- Audio/Camera 不依赖 legacy Core 的显式断言。
- Enemies/Items/Weapons 迁移桥删除。
- 运行时禁止旧 `GameObject.Find`、全局 Player Tag 查找、未授权场景加载和 `Resources.Load` 服务依赖。
- Save/Meta Gateway、Localization Gateway、部分 singleton owner/lifetime 规则。
- Editor API 不应泄漏到运行时程序集。

### 未充分覆盖

- 完整 asmdef 图及危险入边/反向边检测。
- 业务程序集的 `autoReferenced` 规则。
- singleton 使用点总量、按模块预算和新增回归。
- `FindAnyObjectByType` / `FindObjectsByType` 的业务白名单与场景绑定证据。
- 运行时 UI 构造是否只存在于明确的灰盒/Bootstrap 例外。
- namespace 与文件目录/程序集职责一致性。
- Prefab 引用完整性、Canvas 结构、View 可替换性和实际运行时组合。
- 以 Unity 场景/Prefab 实例运行的 PlayMode 依赖链。

`Assets/Scripts/Tests/Architecture/RuntimeEditorDependencyTests.cs:16-24,47-75,103-142,648-716` 大量通过 `File.ReadAllText`、`Contains` 和固定字符串断言验证结构。这些测试应保留作为快速回归门禁，但不能作为完整依赖账本的唯一证明。

## 7. Phase 0 门禁定义

以下规则作为后续 Phase 1+ 的架构门禁。本阶段只记录规则，不修改现有代码。

### 必须阻断的新增问题

1. 新运行时代码不得新增对 `Zephyr.Core` 的直接 asmdef 引用，除非属于明确登记的迁移文件。
2. 新业务代码不得新增 `*.Instance` 读取；组合根可在初始化边界解析一次并缓存接口。
3. 新业务代码不得新增 `FindAnyObjectByType`、`FindFirstObjectByType`、`FindObjectsByType` 或场景级 Transform 扫描；Camera/Bootstrap 过渡白名单必须单独登记。
4. 新业务代码不得新增运行时 UI 树构造；灰盒或 Bootstrap 例外必须有文件级说明和替换计划。
5. 新程序集不得默认使用 `autoReferenced: true`，除非是 Unity 编辑器基础设施或项目明确批准的基础层。
6. 新 Contracts 文件必须使用与程序集语义一致的 namespace；历史 namespace 迁移文件要登记序列化和引用影响。
7. 新场景加载必须由 Initializer 或 Persistent 的 `SceneLoader` 授权路径完成，内容组件不能直接调用 `SceneManager.LoadScene*`。

### 每阶段必须输出的证据

- 修改前后 asmdef 依赖边差异。
- 修改前后 singleton、全局发现、场景加载和 UI 构造计数。
- 受影响脚本、Prefab、Scene、SO 的文件清单。
- 架构测试、EditMode/PlayMode 测试和 Console 结果。
- 如果涉及场景：SceneView 截图文件和实际观察结果。
- 如果涉及序列化类型或 namespace：Prefab/SO/Scene 引用检查和回滚点。

## 8. 后续修复顺序

### Phase 1：收窄 legacy Core

先把仍位于 Core 宿主、但实际属于 Flow/UI/Dialogue/Localization 的实现迁移到正确程序集或建立窄 Gateway。目标不是立刻改所有 namespace，而是先降低 `Zephyr.Core` 的职责和入边。

### Phase 2：组合根与 Singleton 读取预算

为 Persistent、GameManager、场景入口和 Actor 生成边界建立明确的依赖解析点。业务类持有接口或轻量 Gateway，不在每个调用路径访问具体 `Instance`。为每个运行时程序集建立 singleton 读取预算，新增读取默认阻断。

### Phase 3：Contracts namespace 清洁

先迁移纯 enum、value type、event payload 和 interface，再处理会影响 Prefab/SO 序列化的类型。每批迁移必须保留可回滚点并重新导入验证。

### Phase 4：UI 与场景发现

将 Gameplay HUD 和剩余 Popup 结构迁移为 Prefab/View；将 Tutorial、RunStart、Camera、SceneContext 的隐式发现改为场景组合根显式绑定。Camera 扫描只作为临时白名单，不扩散到普通业务。

### Phase 5：最终门禁与盲审

把本报告中的静态指标纳入架构测试，执行完整依赖图、namespace、singleton、scene discovery、UI 和 PlayMode 链路验证，再进行一次不继承历史评分的盲审。

## 9. 用户当前不需要做的事情

Phase 0 不需要用户提供额外代码、场景、Prefab、资源或设计信息。当前工作区没有未提交改动需要保护；Unity Console 也没有错误或警告。后续进入涉及 Prefab/Scene/序列化 namespace 的阶段时，才需要把对应场景链路和允许的迁移窗口纳入验收。

## 10. 验收结论

Phase 0 完成度评估：**高**。

已完成：

- 读取并确认 Unity 项目根目录和编辑器状态。
- 检查工作区基线，未回滚或清理用户文件。
- 解析 27 个 asmdef、93 条引用边和模块入边。
- 统计运行时代码的 singleton、场景发现、场景加载、UI 构造和 legacy namespace 热点。
- 记录 Contracts namespace 与程序集语义不一致的具体范围。
- 重新运行架构测试与 EditMode 测试，结果全部通过。
- 定义后续阶段的阻断门禁、证据要求和修复顺序。

未在本阶段做的事情是有意保留的：没有修改实现、没有移动类型、没有改场景/Prefab/SO、没有改变运行时行为。因此本报告可作为后续修复前的基线，而不是修复阶段本身。


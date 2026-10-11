# Zephyr 第四阶段架构修理、四阶段复评与后续放行报告

- 日期：2026-10-11
- 项目：`D:\GitHub\Zephyr_v3\Zephyr(AI)\Zephyr`
- 范围：运行时目标发现、服务边界、架构回归门禁、四阶段延期工作复评
- Unity 路由：非图片 Unity 操作统一使用旧版 `mcp__unity_mcp`

## 一、第四阶段验收结论

**验收成功。**

本阶段完成度评估为 **高（约 9.0/10）**。最危险的运行时全局 Player 查找已经被替换为
Core 事件契约；场景加载的唯一权威入口继续受到架构测试保护；主菜单用例已经与具体存档
单例解耦；同时增加了防止普通开发重新引入这些问题的静态门禁。

仍未完成的部分主要是需要 Unity 资产迁移或大范围生命周期重构的工作，不是隐藏债务：
它们已经被明确列为迁移桥、允许例外或后续专项工作。因此达到“完成度很高”的验收条件。

## 二、第四阶段实际修理

### 2.1 清除运行时全局 Player 查找

新增：

- `Zephyr(AI)\Zephyr\Assets\Scripts\Core\Events\PlayerTargetChangedEvent.cs`

扩展：

- `Zephyr(AI)\Zephyr\Assets\Scripts\Core\Events\EventBus.cs`
  - 增加最新事件快照读取能力，支持对象延迟启用时取得当前 Player。
- `Zephyr(AI)\Zephyr\Assets\Scripts\Player\Flow\PlayerSpawner.cs`
  - 在生成、销毁和场景切换时发布 Player 生命周期事件。

已改造：

- `EnemyBrain`
- `GustInteractable`
- `WeaponPickup`
- `CurrencyPickup`

以上运行时模块不再调用 `GameObject.FindGameObjectWithTag("Player")`、
`GameObject.FindWithTag("Player")` 或 `GameObject.Find("Player")`，改为订阅
`PlayerTargetChangedEvent` 并缓存目标引用。

这项修理同时解决了目标丢失、玩家 Respawn、对象池重启用和场景切换时的隐式查找问题。

### 2.2 收窄 MainMenuUseCase 的存档依赖

新增：

- `Zephyr(AI)\Zephyr\Assets\Scripts\Core\Save\ISaveSlotGateway.cs`

调整：

- `SaveSystem` 实现 `ISaveSlotGateway`。
- `MainMenuUseCase` 依赖 `ISaveSlotGateway`，不再依赖具体 `SaveSystem`。
- `MainMenuController` 的槽位读取、创建、选择和删除均通过 `MainMenuUseCase` 完成。

结果是主菜单业务可以在不启动 Unity Persistent 单例的情况下进行替换和测试。

### 2.3 增加架构安全门禁

扩展：

- `Zephyr(AI)\Zephyr\Assets\Scripts\Tests\Architecture\RuntimeEditorDependencyTests.cs`

新增规则：

- Runtime 禁止新增全局 `GameObject.Find*`。
- Player 目标必须通过生命周期事件契约传递。
- Runtime 场景加载只能由 `SceneLoader` 或启动期 Bootstrap 承担。
- `Resources.Load` 只允许保留在 `LocalizationManager` 本地化适配器中。
- `SceneLoader` 不得重新引用 `GameFlow.Instance`。
- `MainMenuController` 不得重新直接调用 SaveSystem 的槽位业务方法。

这意味着第四阶段之后，新增功能如果重新引入这些风险，会在架构测试中被直接暴露，
而不是等到下一次大规模审查才发现。

## 三、四阶段延期工作总表

| 延期工作 | 来源 | 当前状态 | 是否已排除主要危险 |
|---|---|---|---|
| Editor/Runtime 混用清理 | 第一阶段 | 已完成 | 是，已有架构扫描持续保护 |
| 领域程序集拆分 | 第一、二阶段 | 已完成 | 是，当前保持 19 个 asmdef |
| `PlayerDeathController` 拆分 | 第二阶段 | 已完成 | 是，已拆为服务、用例、表现和协调器 |
| `GameFlow` / `SceneLoader` 收窄 | 第二阶段 | 已完成 | 是，SceneLoader 只发布加载完成通知 |
| Player 运行时全局查找 | 第二、三阶段 | 已完成 | 是，改为 Core 生命周期事件 |
| 主菜单存档业务抽离 | 第二、三阶段 | 已完成 | 是，现有 `MainMenuUseCase` 使用 gateway |
| `MainMenuView` / `SettingsPresenter` | 第三阶段 | 仍延期 | 主要是可维护性债务，不是运行时安全阻塞 |
| `Enemies -> Items/Weapons` 迁移桥 | 第二阶段 | 仍延期 | 当前依赖已显式化；完全移除需要资产迁移 |
| `Items -> Weapons` 迁移桥 | 第二阶段 | 仍延期 | 当前依赖已显式化；完全移除需要掉落结果合同和资产迁移 |
| 全局单例全面移除 | 第一、三阶段 | 仍延期 | 尚未全部排除，已限制在可见的服务/门面边界 |
| `Resources.Load` 本地化替换 | 第一阶段 | 仍延期 | 当前只有本地化适配器使用，并由测试锁定例外 |
| Core 进一步拆为 Contracts/Flow/Combat/Persistence | 第一、二阶段 | 仍延期 | 尚无隐藏新增风险，但需要独立迁移阶段 |

## 四、四阶段完成后的整体架构复评

### 4.1 已经足够安全的部分

- 新功能不能再通过 `GameObject.Find*` 偷渡 Player 依赖，必须使用生命周期事件或显式合同。
- 普通运行时脚本不能自行加载或卸载场景，场景生命周期权威明确。
- 跨域程序集边界已经存在，且主要依赖关系进入 asmdef，可被编译器和测试发现。
- 核心编排器不再直接拥有全部业务算法，死亡流程、主菜单存档决策和 GameFlow 状态策略
  均已有独立职责对象。
- 新增的架构规则已经进入测试源，后续普通功能开发具备最低限度的自动防回归能力。

### 4.2 仍然不能宣称“完全无危险”的部分

- `GameFlow`、`SaveSystem`、`EventBus`、`LocalizationManager` 和 `PlayerSpawner` 仍是
  会话级单例；全面替换为显式服务入口会触及大量生命周期和场景引用。
- `Zephyr.Enemies` 仍引用 `Zephyr.Items` 与 `Zephyr.Weapons`，`Zephyr.Items` 仍引用
  `Zephyr.Weapons`。这些是显式迁移桥，但不是目标架构的最终状态。
- `MainMenuController` 仍约 510 行，动态 UI 创建、文案刷新和 Settings 视图还没有完整
  分离为 `MainMenuView` / `SettingsPresenter`。
- `LocalizationManager` 仍使用 `Resources.Load`，但目前已被限制为唯一运行时例外。
- Unity 的标准脚本分析器对 `WeaponPickup.Update()` 仍给出一个泛化的字符串拼接提示；当前
  方法没有对应的字符串拼接调用，Console 未出现 Warning，此项记录为静态分析误报/非阻塞提示。

## 五、是否可以开始修改计划延期工作

### 结论：可以，但只能按受控顺序开始，不能开放式修改。

四阶段完成后，项目已经足以开始以下低风险延期工作：

1. 先拆 `MainMenuView` 和 `SettingsPresenter`，保持现有 Controller 类型、序列化字段和
   动态 UI 行为不变。
2. 再把 `MainMenuUseCase` 的 gateway 继续用于纯 EditMode 测试，补齐存档决策测试。
3. 再设计 Reward/Drop contract，先建立兼容适配层和资产迁移清单，再迁移
   `DropTableSO`、`WeaponPickup` 和 Boss 奖励引用。

以下工作目前不应与普通玩法开发混在同一批次：

- 全局单例全面移除；
- Core 大规模拆分；
- Reward/Drop 资产类型和程序集迁移；
- Localization 资源管线替换；
- 场景生命周期和 Persistent 结构调整。

原因是这些工作会同时影响场景、Prefab、ScriptableObject、程序集和生命周期契约，必须
单独制定迁移计划、逐项使用 Unity MCP 验证，并在每个小批次后重新运行架构门禁。

### 给后续 Agent 的放行条件

后续 Agent 可以开始延期工作，但每一批修改必须满足：

- 先读取本报告和架构设计文档；
- 不得绕过 `SceneLoader` 建立新场景加载通道；
- 不得重新引入 Runtime `GameObject.Find*`；
- 不得扩大 `Resources.Load` 例外；
- 不得新增跨域 asmdef 依赖，除非先更新迁移记录；
- 不得直接改写 Unity YAML 伪造资产迁移；
- 每次 Unity 刷新后检查 Console，场景/Prefab 改动必须截图；
- 架构测试或等价静态门禁失败时，停止继续堆叠功能。

因此本次评估不是“以后可以随意修改”，而是：**可以进入计划延期工作的受控实施阶段，
但架构敏感变更仍必须独立、可验证、可回滚地进行。**

## 六、验证证据

- Unity 项目根目录确认：`D:\GitHub\Zephyr_v3\Zephyr(AI)\Zephyr`。
- 本阶段受影响脚本均通过 `Unity_ValidateScript` standard 验证；除 `WeaponPickup` 的
  非阻塞泛化提示外，诊断为空。
- 每次 `Assets/Refresh` 后立即调用 `Unity_GetConsoleLogs`：最终结果为 **0 Error、0 Warning**。
- 静态审计结果：Runtime `GameObject.Find*` 只剩
  `Assets/Scripts/Testing/Editor/ConfigureSampleSceneExcalibur.cs`，属于 Editor 工具；
  Runtime 场景加载只剩 `SceneLoader` 和启动期 Bootstrap。
- 前三阶段最近一次 EditMode 基线为 **4/4 passed**，Job ID：
  `9a6091862d0e440990adddab071de3d5`。
- 第四阶段新增测试源已完成 Unity standard 脚本验证；尝试通过临时 Editor Command
  直接运行测试方法时，Unity 当前测试程序集发现/缓存状态不完整，未将未实际执行的结果
  虚报为通过。新增断言以源码验证、Unity 编译刷新和静态审计作为补充证据。
- 本阶段未修改场景、Prefab、ScriptableObject 或序列化引用，因此不需要 SceneView 截图。

## 七、最终判定

**第四阶段架构修理完成度很高，验收成功。**

四阶段之后，项目已经从“依靠人工记忆 architecture correctness”提升到“有程序集边界、
事件生命周期契约、场景加载门禁、Runtime 查找门禁和可追踪迁移桥”的状态，可以开始计划
延期工作，但必须遵守本报告第五节的受控放行条件。

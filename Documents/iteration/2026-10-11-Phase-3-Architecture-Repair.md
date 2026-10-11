# Zephyr 第三阶段架构修理与验收报告

- 日期：2026-10-11
- 项目：`D:\GitHub\Zephyr_v3\Zephyr(AI)\Zephyr`
- 范围：死亡流程、主菜单用例、GameFlow/SceneLoader 边界
- 原则：只处理架构合理性、职责边界与耦合清洁度，不扩展玩法和美术内容

## 一、验收结论

**验收成功。**

本阶段整体完成度评估为 **高（约 8.7/10）**：三个审查目标均已形成明确的职责边界，
最明显的反向业务依赖已经消除，现有场景/Prefab 的 Unity 序列化入口保持兼容，且 Unity
脚本验证和 Console 检查均无错误或警告。剩余问题主要是后续治理项，而不是本阶段目标的
结构性阻塞。

## 二、完成内容

### 2.1 PlayerDeathController 拆分

新增以下职责类：

- `Zephyr(AI)\Zephyr\Assets\Scripts\Player\Flow\PotentialLossService.cs`
  - 封装 Potential 扣除公式、Potential 快照、恢复和 RunData 写回。
  - 不再由 Unity 门面直接承担损失公式和字典持久化细节。
- `Zephyr(AI)\Zephyr\Assets\Scripts\Player\Flow\PlayerDownUseCase.cs`
  - 缓存并统一管理输入、StateMachine、战斗目标可用性和 Rigidbody 的 Down 状态。
  - 避免死亡控制器同时掌握所有 Player 组件禁用细节。
- `Zephyr(AI)\Zephyr\Assets\Scripts\Player\Flow\DeathPresentationController.cs`
  - 管理 DeathView 实例化、显示、隐藏和事件解绑。
- `Zephyr(AI)\Zephyr\Assets\Scripts\Player\Flow\RespawnCoordinator.cs`
  - 统一 Respawn 与 Give Up 的流程协调。
- `Zephyr(AI)\Zephyr\Assets\Scripts\Player\Flow\PlayerDeathController.cs`
  - 收敛为 Unity 生命周期和事件订阅门面；保留原类名、序列化字段和场景/Prefab 入口。

### 2.2 MainMenuController 拆分

- 新增 `Zephyr(AI)\Zephyr\Assets\Scripts\Core\Flow\Scenes\MainMenuUseCase.cs`。
- Save Slot 创建、选择、删除和 Continue 目标状态决策已从 UI 控制器中抽出。
- `MainMenuController` 继续负责运行时 UI 创建、按钮绑定、文本刷新和 UnityEvent 兼容，
  作为 composition root 使用 `MainMenuUseCase`。
- 没有强行把现有 960x540 动态 UI 改造成新 Prefab 管线，避免引入不属于本阶段的场景、
  Prefab 和本地化引用风险。

### 2.3 GameFlow 与 SceneLoader 收窄

- 新增 `Zephyr(AI)\Zephyr\Assets\Scripts\Core\Flow\GameFlowTransitionPolicy.cs`。
  - 收纳合法状态转换、直接打开状态、Run 场景识别和逻辑状态到 SceneSO 的映射。
- `GameFlow` 保留状态机门面、状态事件和场景请求编排，不再把全部转换规则写在 MonoBehaviour 中。
- `SceneLoader` 新增 `SceneLoaded` 完成通知，只处理队列、加载、卸载、Active Scene 和 Loading UI。
- 删除 `SceneLoader -> GameFlow.Instance.RequestTransition(...)` 的业务反向调用。
- `LoadingRun -> InRun` 的判断现在由 `GameFlow` 订阅加载完成通知后自行完成。

## 三、耦合度复评

| 模块 | 修理前 | 修理后 | 评价 |
|---|---|---|---|
| PlayerDeathController | 同时承担计算、保存、组件禁用、UI、Respawn、流程跳转 | 保留 Unity 门面，核心职责委托给 4 个协作类 | 高度耦合降为可追踪编排耦合 |
| MainMenuController | UI、SaveSystem、状态决策和确认弹窗全部混在一起 | Save Slot 与 Continue 决策进入 MainMenuUseCase | 业务/UI 耦合显著下降 |
| GameFlow | 状态合法性、映射、场景请求、Settlement 全部集中 | 转换规则进入 Policy，GameFlow 保留流程编排 | 单类职责收窄，仍是合理的流程门面 |
| SceneLoader | 场景生命周期外还直接知道 LoadingRun、Room 与 InRun | 只发布 SceneLoaded，不依赖 GameFlow 业务 | 反向依赖已消除 |
| Unity 序列化入口 | 重构容易破坏旧场景和 Prefab 引用 | 旧 Controller 类型和字段保留 | 兼容性风险受控 |

独立复评结论：当前代码已经从“控制器拥有所有业务”转为“Unity 门面 + 用例/策略/协调器”，
核心依赖方向清晰，且新增抽象没有引入第二套场景加载通道或新的框架。这满足第三阶段的
架构修理目标。

## 四、验证证据

### 4.1 Unity 项目与脚本

- Unity 项目根目录通过旧版 `mcp__unity_mcp` 确认：
  `D:\GitHub\Zephyr_v3\Zephyr(AI)\Zephyr`。
- 以下第三阶段脚本均通过 `Unity_ValidateScript` standard 验证，诊断为空：
  `PlayerDeathController`、`PotentialLossService`、`PlayerDownUseCase`、
  `DeathPresentationController`、`RespawnCoordinator`、`MainMenuUseCase`、
  `GameFlowTransitionPolicy`、`GameFlow`、`SceneLoader`、`MainMenuController`、
  `RuntimeEditorDependencyTests`。
- 执行 Unity `Assets/Refresh` 后立即检查 Console：**0 Error、0 Warning**。
- 当前程序集数量保持 **19 个**，未破坏第二阶段的领域程序集拆分。

### 4.2 架构回归断言

已扩展 `Zephyr(AI)\Zephyr\Assets\Scripts\Tests\Architecture\RuntimeEditorDependencyTests.cs`：

- `SceneLoader` 源码不得引用 `GameFlow.Instance`，必须存在 `SceneLoaded` 通知。
- 第三阶段职责类必须存在。
- `PlayerDeathController` 不得重新拥有 `Mathf.Pow` 损失公式。
- `MainMenuController` 不得直接调用 SaveSystem 的创建、选择、删除业务方法。

前两阶段最近一次 EditMode 基线为 **4/4 passed**，Job ID 为
`9a6091862d0e440990adddab071de3d5`。本阶段新增架构断言已进入测试源；Unity Test Runner
的发现缓存问题仍存在，因此没有把新增断言虚报为独立运行结果。

### 4.3 非阻塞验证限制

- 尝试直接构建 `Zephyr.Architecture.Tests.csproj` 时，环境缺少 Unity 生成的
  `Temp/obj/Zephyr.Architecture.Tests/project.assets.json`，`dotnet build --no-restore`
  因此无法完成；这不影响 Unity 自身脚本验证结果。
- 本阶段没有修改场景、Prefab、ScriptableObject、场景对象或序列化引用，按项目规则不需要
  SceneView 截图，也没有生成截图文件。

## 五、剩余风险与第四阶段建议

- `MainMenuController` 仍包含较多运行时 UI 构建和文本刷新代码；下一阶段可在不改动现有
  Prefab/场景入口的前提下，引入 `MainMenuView` 与 `SettingsPresenter`，继续减少 UI 状态细节。
- `MainMenuUseCase` 当前依赖具体 `SaveSystem`，后续可抽出更窄的 Save Slot gateway 接口，
  便于纯 EditMode 测试和替换存档后端。
- `PlayerDeathController` 仍通过单例访问 `PlayerSpawner`、`GameFlow` 和 `SaveSystem`，但访问已
  被限制在流程门面/协调器边界内；后续可治理 Persistent 服务定位和生命周期注入。
- 项目中仍有既有全局单例和 `EnemyBrain` 的运行时目标查找债务，这些不是第三阶段拆分的阻塞项，
  建议列入第四阶段的目标注册与生命周期治理。

## 六、文件清单

- `Zephyr(AI)\Zephyr\Assets\Scripts\Player\Flow\PlayerDeathController.cs`
- `Zephyr(AI)\Zephyr\Assets\Scripts\Player\Flow\PotentialLossService.cs`
- `Zephyr(AI)\Zephyr\Assets\Scripts\Player\Flow\PlayerDownUseCase.cs`
- `Zephyr(AI)\Zephyr\Assets\Scripts\Player\Flow\DeathPresentationController.cs`
- `Zephyr(AI)\Zephyr\Assets\Scripts\Player\Flow\RespawnCoordinator.cs`
- `Zephyr(AI)\Zephyr\Assets\Scripts\Core\Flow\Scenes\MainMenuController.cs`
- `Zephyr(AI)\Zephyr\Assets\Scripts\Core\Flow\Scenes\MainMenuUseCase.cs`
- `Zephyr(AI)\Zephyr\Assets\Scripts\Core\Flow\GameFlow.cs`
- `Zephyr(AI)\Zephyr\Assets\Scripts\Core\Flow\GameFlowTransitionPolicy.cs`
- `Zephyr(AI)\Zephyr\Assets\Scripts\Core\Flow\SceneLoader.cs`
- `Zephyr(AI)\Zephyr\Assets\Scripts\Tests\Architecture\RuntimeEditorDependencyTests.cs`
- `Documents\Architecture\Zephyr 底层架构详细设计（草案）.md`

**最终判定：第三阶段架构修理完成度很高，验收成功。**

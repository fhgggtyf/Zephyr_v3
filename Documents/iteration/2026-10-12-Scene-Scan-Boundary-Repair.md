# 业务场景扫描边界修复

日期：2026-10-12

## 1. 任务目标

执行架构方案第一项：消除两个业务级场景扫描，改为显式的场景引用和 Gateway 边界。

目标对象：

- `TutorialObjectiveExit`：不再自动查找 `RoomController` 或动态扫描 `ILevelObjective`。
- `RunStartInteractable`：不再通过 `FindAnyObjectByType<MetaHubController>()` 启动 Run。

本阶段不改变场景加载 authority、存档协议、战斗协议或普通交互行为，只收窄对象发现边界。

## 2. 实施内容

### 2.1 Tutorial 出口

修改：

`Zephyr(AI)/Zephyr/Assets/Scripts/Gameplay/Levels/TutorialObjectiveExit.cs`

变化：

- 删除 `FindAnyObjectByType<RoomController>()`。
- 删除 `FindObjectsByType<MonoBehaviour>()` 的目标动态发现。
- `_roomController` 现在必须由场景显式绑定。
- `_requiredObjectives` 现在必须由场景显式配置；没有目标的教程阶段使用空数组。
- 缺少显式绑定时输出错误，避免静默退回隐式扫描。

场景绑定：

- `Tutorial_Phase_1` 的 `TutorialExit` 绑定本场景 `TutorialObjectiveRoom.RoomController`，目标数组为空。
- `Tutorial_Phase_2` 的 `TutorialExit` 绑定本场景 `TutorialObjectiveRoom.RoomController`，目标数组绑定 `ZeusBoss.EnemyBrain`。

### 2.2 MetaHub Run 入口

新增契约：

`Zephyr(AI)/Zephyr/Assets/Scripts/Core/Contracts/Interfaces/IRunEntryGateway.cs`

修改：

- `MetaHubController` 实现 `IRunEntryGateway`。
- `RunStartInteractable` 增加显式序列化字段 `_runEntryGatewaySource`。
- `RunStartInteractable` 在 `Awake` 将该来源转换为 `IRunEntryGateway` 并缓存。
- `BeginRun()` 通过 `_runEntryGateway.StartRun()` 发起 Run，不再查找 `MetaHubController`。
- `MetaHub.unity` 中的 `RunStartInteractable` 显式绑定 `MetaHubRoot` 上的 `MetaHubController`。

`MetaHubController` 仍然是场景组合边界，只有它负责把 Run 入口请求转发到现有 `GameFlow`。交互物本身不再依赖具体的 MetaHub 控制器类型或全局场景扫描。

### 2.3 架构门禁

在 `RuntimeEditorDependencyTests` 新增：

`TutorialAndMetaHubEntrancesUseAuthoredBoundaries`

该门禁检查：

- Tutorial 出口不包含 `FindAnyObjectByType` 或 `FindObjectsByType`。
- Run 入口不包含 `FindAnyObjectByType` 或 `MetaHubController` 具体依赖。
- Run 入口使用 `IRunEntryGateway` 和显式序列化来源。
- MetaHubController 实现 `IRunEntryGateway`。

## 3. 验证结果

### 3.1 静态脚本验证

以下脚本通过 Unity 标准验证，无诊断：

- `IRunEntryGateway.cs`
- `TutorialObjectiveExit.cs`
- `RunStartInteractable.cs`
- `MetaHubController.cs`
- `RuntimeEditorDependencyTests.cs`

### 3.2 Unity 测试

```text
Zephyr.Architecture.Tests: 41 passed, 0 failed, 0 skipped
Zephyr.Tests.EditMode:       5 passed, 0 failed, 0 skipped
Zephyr.Tests.PlayMode:       1 passed, 0 failed, 0 skipped
```

新增的 Architecture 测试已被 Runner 发现并通过。Vertical Slice PlayMode 继续覆盖：

```text
Initializer -> MainMenu -> Tutorial -> MetaHub -> LoadingRun -> InRun -> Settlement -> Save Round Trip
```

这证明显式 Tutorial 目标引用和 MetaHub Run Gateway 没有破坏已有场景生命周期。

### 3.3 Console

最终通过旧版 `mcp__unity_mcp.Unity_GetConsoleLogs` 检查：

```text
Error: 0
Warning: 0
```

PlayMode Test Runner 输出仍显示已有内容层 Warning：

- 灰盒玩家 Animator 未绑定 AnimatorController。
- 部分测试场景没有 AudioListener。

这些 Warning 未由本阶段引入，也没有阻塞测试；它们保留为后续内容/运行环境专项事项。

## 4. 场景截图验证

本次修改了三个场景，并分别完成 SceneView 检查和 PNG 导出：

- `D:\GitHub\Zephyr_v3\Zephyr(AI)\Zephyr\Assets\MCPTest\Screenshots\20261012_Tutorial_Phase_1.png`
  - 可见地面、平台、玩家出生区域和教程出口所在的横版空间；没有粉色材质、明显缺失引用或 UI 裁切。
- `D:\GitHub\Zephyr_v3\Zephyr(AI)\Zephyr\Assets\MCPTest\Screenshots\20261012_Tutorial_Phase_2.png`
  - 可见 Boss 房间的灰盒平台和场景边界；没有粉色材质、明显缺失引用或 UI 裁切。
- `D:\GitHub\Zephyr_v3\Zephyr(AI)\Zephyr\Assets\MCPTest\Screenshots\20261012_MetaHub.png`
  - 可见 MetaHub 地面、Run 入口占位物和场景布局；没有粉色材质、明显缺失引用或 UI 裁切。

场景修改、保存、Console 检查和截图导出均使用旧版 `mcp__unity_mcp` 路由；没有使用另一套 MCP 替代失败操作。

## 5. 架构效果

本阶段将两个业务级隐式依赖改成了可审查的显式依赖：

```text
TutorialExit --serialized--> RoomController
TutorialExit --serialized--> ILevelObjective list
RunStartInteractable --serialized MonoBehaviour
                     -> IRunEntryGateway
                     -> MetaHubController
                     -> GameFlow
```

收益：

- 场景结构缺失时会立即报错，而不是运行时随机绑定错误对象。
- Tutorial 目标集合由场景作者明确声明，目标不会因为其他加载对象而被意外加入。
- `RunStartInteractable` 不再知道 `MetaHubController` 具体类型。
- MetaHub 的 Run 入口依赖方向变成 `Interaction -> Contract <- Flow facade` 的窄边界。
- 新增其他 Run 入口时，可以复用 `IRunEntryGateway`，不需要复制全局查找逻辑。
- Architecture Test 会阻止这两个入口重新引入场景扫描。

本阶段没有试图清理所有 `*.Instance` 读取；`RunStartInteractable` 仍保留已有的 SaveSystem 组合问题，属于下一阶段服务访问边界专项，不在本次范围内顺手扩大。

## 6. 验收结论

本阶段验收成功。

依据：

- 两个业务级场景扫描已从生产脚本删除。
- Tutorial Phase 1/2 和 MetaHub 场景引用已显式绑定并保存。
- Architecture `41/41`、EditMode `5/5`、Vertical Slice PlayMode `1/1` 全部通过。
- 最终 Unity Console 为 `0 Error / 0 Warning`。
- 三个受影响场景均完成 SceneView 检查和 PNG 导出。

下一步应处理服务访问边界：优先收敛 `RunStartInteractable`、`MetaUpgradeStation`、`CurrencyPickup`、`DialogueTrigger` 等组件中的具体 `SaveSystem.Instance`、`EventBus.Instance` 或 Meta service 定位，但应保持本阶段已经建立的显式 Gateway 方向，不重新引入 Service Locator。

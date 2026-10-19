# Singleton Lifecycle Governance: Progression Gateway Slice

日期：2026-10-12

## 1. 范围

本轮继续处理第 3 项 Singleton 生命周期治理中尚未收窄的一个业务边界：
`SceneProgressionExit` 与 `TutorialObjectiveExit` 原本直接读取 GameManager-owned 的
`SceneProgressionController.Instance`。

本轮只处理场景推进入口，不机械替换其他服务访问，也不改变场景加载 authority、存档协议或
`SceneProgressionController` 的 GameManager 生命周期。

## 2. 实施内容

- 新增 `ISceneProgressionGateway` 和 `ISceneProgressionGatewayConsumer`，放入 Contracts 层。
- `SceneProgressionController` 实现 `ISceneProgressionGateway`，继续由 GameManager 场景持有和管理。
- `SceneProgressionController` 在 GameManager 组合边界监听 `SceneManager.sceneLoaded`，并为已加载或新加载场景中的 progression consumers 注入自身接口。
- `SceneProgressionExit` 改为保存 gateway 接口，不再读取 `SceneProgressionController.Instance`。
- `TutorialObjectiveExit` 改为保存 gateway 接口，不再读取 `SceneProgressionController.Instance`。
- 保留可选的序列化 `MonoBehaviour` 来源字段，允许直接打开场景时由显式对象引用提供 gateway；正常启动路径由 GameManager 组合根注入。
- 增加架构门禁 `SceneProgressionConsumersUseGatewayInsteadOfSingleton`，防止两个出口重新依赖具体 singleton。

## 3. 依赖方向

```text
SceneProgressionController (GameManager owner)
    -> ISceneProgressionGateway
    -> ISceneProgressionGatewayConsumer.Configure(...)
         -> SceneProgressionExit
         -> TutorialObjectiveExit
```

出口组件不再知道 controller 的具体类型，也不负责定位 GameManager 对象。`SceneProgressionController`
仍然只负责发出 progression request；实际场景加载仍由既有 `GameFlow -> SceneLoader` 链路完成。

## 4. 验证

- `ISceneProgressionGateway.cs` 标准脚本验证：无诊断。
- `SceneProgressionController.cs` 标准脚本验证：无诊断。
- `SceneProgressionExit.cs` 标准脚本验证：无诊断。
- `TutorialObjectiveExit.cs` 标准脚本验证：无诊断。
- `RuntimeEditorDependencyTests.cs` 标准脚本验证：无诊断。
- 静态搜索确认两个出口脚本不再包含 `SceneProgressionController.Instance`。
- Unity 资产刷新和脚本编译成功。
- 最终 Unity Console：`0 Error / 0 Warning`。
- 本轮未修改场景、Prefab 或 ScriptableObject，因此不生成 SceneView 截图。

## 5. 验收边界

本轮 progression singleton 边界切片验收成功，完成度评定为 **9.2 / 10**：

- GameManager owner 保持唯一，未新增 singleton 或 `DontDestroyOnLoad`。
- Gameplay 场景出口依赖 Contracts gateway，而不是具体控制器 singleton。
- 场景加载时的 consumer 注入由现有 GameManager-owned controller 承担，没有新增平行服务定位器。
- 编译、脚本验证和架构门禁源码均通过。

这不代表第 3 项的所有具体 `*.Instance` 读取已经清零。仍有其他生命周期边界需要按 owner 单独治理，
例如 `LoadingRunController`、`DialogueTrigger`、`StoryProgressionGate`、Player 交互与部分 Flow
controller。它们不能在本轮通过机械替换处理，否则会把不同生命周期错误地塞进同一个 gateway。

## 6. 对后续开发的意义

新建场景出口或其他场景级流程触发器时，可以复用 `ISceneProgressionGatewayConsumer`，不需要扫描场景或
访问 `SceneProgressionController.Instance`。GameManager 生命周期、场景加载 authority 和业务触发器之间
的依赖方向现在更清晰，也更容易在架构测试中回归。


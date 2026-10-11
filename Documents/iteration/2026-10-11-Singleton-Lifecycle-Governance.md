# Singleton Lifecycle Governance

**日期：** 2026-10-11  
**范围：** Persistent / GameManager 单例生命周期、重复实例防护、静态引用释放、组合边界验证  
**前置条件结论：** 不需要额外业务信息；现有场景挂载、设计文档和代码足以确定 owner 边界。  
**验收结论：** 验收成功，完成度很高  
**统一自评：** 9.3 / 10

## 1. 本轮目标

本轮针对前期架构审查中延期的 singleton 生命周期治理进行修理，目标是把既有全局入口纳入明确、可检查、可回收的生命周期协议：

- 明确 singleton 的 owner scene 和生命周期类别。
- 统一重复实例 claim 逻辑，避免重复服务悄悄共存。
- 在对象销毁时释放静态引用，避免跨场景残留旧对象。
- 禁止通过新增 `DontDestroyOnLoad` 伪造持久化结构。
- 通过架构测试检查代码契约和场景挂载是否一致。
- 以 `Initializer -> Persistent -> MainMenu` 入口做最小运行时烟测。

本轮没有修改正式存档内容，没有新增全局 Manager，没有创建新的平行 SceneLoader、EventBus 或 Save gateway，也没有修改正式 Prefab、ScriptableObject 或场景序列化内容。

## 2. 生命周期边界

### 2.1 Persistent owner

以下对象必须存在于 `Assets/Scenes/Core/Persistent.unity`，生命周期覆盖会话：

| 服务 | owner | 责任 |
|---|---|---|
| `EventBus` | Persistent | 全局事件分发基础设施 |
| `SaveSystem` | Persistent | 存档文件和选中槽位的持久化实现 |
| `GameFlow` | Persistent | 游戏流程状态和场景请求协调 |
| `SceneLoader` | Persistent | 统一场景加载/卸载通道 |
| `PoolRegistry` | Persistent | 对象池注册和复用 |
| `TimerService` | Persistent | 非场景局部计时服务 |
| `AudioManager` | Persistent | 会话级音频服务 |
| `LocalizationManager` | Persistent | 本地化运行时服务 |
| `StoryProgressionService` | Persistent | 故事进度和 Meta 进度服务 |
| `ZephyrLogger` | Persistent | 统一日志入口 |

### 2.2 GameManager owner

以下对象只在 `Assets/Scenes/Core/GameManager.unity` 所代表的游戏阶段存在，返回 MainMenu 时不应继续拥有静态引用：

| 服务 | owner | 责任 |
|---|---|---|
| `DialogueManager` | GameManager | 游戏阶段对话服务 |
| `PlayerSpawner` | GameManager | 当前游戏阶段玩家生成 |
| `SceneStatsModifierController` | GameManager | 当前场景属性修正 |
| `SceneProgressionController` | GameManager | 当前场景目标和进度 |

### 2.3 约束关系

```text
Initializer
  └─ loads Persistent
       ├─ Persistent services claim their own scene objects
       └─ SceneLoader requests MainMenu / GameManager / gameplay scenes

Persistent services
  └─ survive scene transitions through Persistent scene ownership

GameManager services
  └─ exist only while the GameManager-owned game phase is loaded
```

生命周期 owner 由场景结构决定，而不是由某个类自行调用 `DontDestroyOnLoad` 决定。

## 3. 实现方案

### 3.1 统一生命周期工具

新增：

```text
Assets/Scripts/Core/Runtime/Lifecycle/SingletonLifetime.cs
```

该文件提供：

- `SingletonLifetime.Persistent`、`GameManager`、`SceneLocal` 三种生命周期分类。
- `SingletonLifetimeAttribute`，用于声明类的 owner 类别。
- `SingletonLifecycle.TryClaim`，统一处理首个实例 claim 和重复实例拒绝。
- `SingletonLifecycle.Release`，统一在销毁时清理静态引用。

这不是一个新的全局服务，也不负责创建对象。它只提供生命周期协议，保留现有场景作为组合根。

### 3.2 Singleton 类治理

治理后的 singleton 在初始化和销毁路径使用统一 claim/release：

```text
Awake / initialization
  └─ SingletonLifecycle.TryClaim(this, ref _instance)

OnDestroy
  └─ SingletonLifecycle.Release(this, ref _instance)
```

治理对象包括：

- Persistent：`AudioManager`、`SaveSystem`、`GameFlow`、`SceneLoader`、`LocalizationManager`、`EventBus`、`ZephyrLogger`、`PoolRegistry`、`TimerService`、`StoryProgressionService`。
- GameManager：`DialogueManager`、`PlayerSpawner`、`SceneStatsModifierController`、`SceneProgressionController`。

`AudioManager` 同时补齐了原本缺失的销毁清理路径，避免其静态引用在对象销毁后继续指向失效对象。

### 3.3 程序集边界

更新：

```text
Assets/Scripts/Core/Persistence/Zephyr.Core.Persistence.asmdef
```

新增必要的向下依赖：

```text
Zephyr.Core.Persistence -> Zephyr.Core.Runtime
```

该依赖方向没有形成循环；Persistence 仍然是具体存档实现所在层，Runtime 只提供基础生命周期契约。

## 4. 架构门禁和验证

### 4.1 架构测试

更新：

```text
Assets/Scripts/Tests/Architecture/RuntimeEditorDependencyTests.cs
```

新增并通过：

- `SingletonServicesDeclareOwnerLifetimeAndReleaseStaticReferences`
- `SingletonOwnerScenesContainTheirDeclaredServiceObjects`

测试检查 singleton 的 owner lifetime、claim、release、`OnDestroy`、`DontDestroyOnLoad` 禁止项，以及 Persistent/GameManager 场景挂载一致性。同时修正 Persistence 组装测试，允许合法的 `Persistence -> Runtime` 依赖。

### 4.2 测试结果

| 验证项 | 结果 |
|---|---:|
| `Zephyr.Architecture.Tests` | **31 / 31 passed** |
| `Zephyr.Tests.EditMode` | **5 / 5 passed** |
| `git diff --check` | **blocked by 3 pre-existing trailing-space findings** |
| Unity 编译 | **passed** |
| 新增场景/Prefab/SO 序列化修改 | **none** |
| 正式存档修改 | **none** |

### 4.3 Play Mode 烟测

通过旧版 `mcp__unity_mcp` 完成场景加载、Play Mode、层级读取和 Console 检查；测试运行使用新版 `mcp__unityMCP`，因为旧版没有可用的 Test Runner API。

烟测路径：

```text
Initializer -> Persistent + MainMenu
```

实际结果：

- 活动场景最终为 `Assets/Scenes/MainMenu/MainMenu.unity`。
- 同时加载 Persistent 和 MainMenu。
- Persistent 中 10 个服务对象均各找到 1 个实例。
- MainMenu 中没有错误地出现 GameManager owner 服务，符合生命周期边界。
- MainMenu 层级包含 `MainMenuRoot`、`MainMenuCamera`、`MainMenuUI` 和 `EventSystem`。
- 停止 Play Mode 后没有新增 C# 编译错误、MissingReference、Serialization 或 duplicate singleton 错误。

本轮没有修改场景对象，因此没有生成 SceneView 截图；本轮只进行了加载和只读层级验证。

## 5. Console 结果和环境风险

最终针对 Error/Warning 的 Console 检查结果为：

```text
Error: 0
Warning: 0
```

工作区差异检查还发现前期已有的 3 处 ScriptableObject 尾随空格，分别位于 `BasicEnemy.asset` 1 处和 `LightningBolt.asset` 2 处；它们不属于本轮生命周期改动，本轮没有擅自清理。

本轮早期检查中曾观察到 Unity Editor 自身的既有环境异常：

```text
KeyNotFoundException: The given key 'services-gateway' was not present in the dictionary.
UnityEditor.Connect.ServicesConfiguration
```

该错误来自 Unity Editor ServicesConfiguration 初始化，不来自本轮 C# 文件、生命周期工具或运行时场景对象；在后续架构测试、业务测试、Play Mode 和最终过滤检查中没有再次产生。它仍作为编辑器环境残余风险记录，不能解释为代码已修复，也不能作为本轮架构回归。

Play Mode 期间还出现过一次 Unity 信息日志：`There are no audio listeners in the scene.`。该日志出现在 Initializer 进入 MainMenu 的过渡阶段，最终 MainMenu 场景包含 `MainMenuCamera`，没有观察到由 singleton 生命周期治理引起的运行时错误。它属于现有入口/相机初始化时序的低风险残余项。

## 6. 影响范围

本轮涉及 Persistent 服务、GameManager 服务、Runtime 生命周期契约、架构测试和 Persistence 程序集依赖声明。

明确没有做以下事情：

- 没有新增 `SaveSystem.Instance` 的平行替代单例。
- 没有把 MetaGateway、SceneLoader 或其他 gateway 改成静态全局入口。
- 没有新增 `DontDestroyOnLoad`。
- 没有改变存档 JSON 格式、槽位数据或正式存档文件。
- 没有把 GameManager 服务提升为 Persistent 生命周期。
- 没有通过手写 Unity YAML 修改场景、Prefab、SO、Animator 或引用。

## 7. 残余风险与后续工作

1. 现有业务代码中仍可能存在旧的 `*.Instance` 读取；本轮重点是 owner、claim 和 release，不是一次性清除所有兼容访问。
2. GameManager owner 的运行时进入/退出路径本轮通过场景门禁和静态架构测试确认，但没有主动启动真实 Run 内容场景；后续可在不改存档的前提下补一条 Tutorial/Run Play Mode smoke。
3. Initializer 过渡阶段的 AudioListener 信息日志仍可单独治理，但不属于 singleton claim/release 缺陷。
4. Unity Editor 的 `services-gateway` 配置异常属于外部环境风险，不应在业务代码中加入规避逻辑。

后续普通功能开发可以继续，但新代码必须遵守：

- 新增会话级服务必须先确定 Persistent 或 GameManager owner。
- 新代码不得用 `DontDestroyOnLoad` 伪造生命周期。
- 新 singleton 必须声明生命周期、统一 claim，并在 `OnDestroy` release。
- 普通业务逻辑不得通过反复读取具体 singleton 代替 Gateway、接口或组合根注入。

## 8. 统一自评

| 维度 | 分数 | 评估 |
|---|---:|---|
| Owner 明确性 | 9.6 | Persistent 与 GameManager 服务集合有明确归属，且有场景门禁。 |
| 重复实例安全 | 9.4 | claim 逻辑统一，重复实例不会继续作为有效全局服务。 |
| 静态引用回收 | 9.4 | 统一 release，且补齐 AudioManager 原缺失的销毁清理。 |
| 依赖方向 | 9.3 | Runtime 契约被 Persistence 向下引用，没有形成循环。 |
| 可测试性 | 9.5 | 架构测试覆盖 owner、claim、release 和场景挂载，业务测试保持通过。 |
| 运行时信心 | 9.0 | 正常入口到 MainMenu 已烟测；GameManager 内容阶段仍留有一条后续 smoke。 |
| 迁移完整度 | 9.1 | 主要生命周期对象已治理，旧 Instance 兼容读取仍需按模块逐步收敛。 |
| **Overall** | **9.3** | **完成度很高，singleton 生命周期治理达到验收标准。** |

## 9. 验收结论

本阶段判定为**验收成功**：

- 生命周期 owner 已明确并与实际场景结构一致。
- Persistent 服务可在正常入口中唯一 claim，未发现重复实例。
- MainMenu 入口烟测通过，未发现本轮引入的运行时错误。
- 架构测试 31/31 和业务 EditMode 测试 5/5 全部通过。
- 没有引入新的全局单例、平行生命周期系统或 `DontDestroyOnLoad` 伪持久化。
- 剩余问题已限制为可识别的兼容访问、未覆盖的 GameManager 深层 smoke，以及 Unity Editor 环境异常。

因此，后续可以继续进行普通内容和玩法开发；涉及新增服务、生命周期改变或跨模块组合根调整的工作，仍必须先按 `AGENTS.md` 中的架构专项规则单独立项。



# 第六阶段：安全架构扣分项修理与统一评分

日期：2026 年 10 月 11 日  
范围：只处理不会改变玩法协议、场景生命周期契约和资源格式的安全架构债务。

## 一、阶段结论

**本阶段验收成功。**

本阶段完成度评估为 **高**。运行时资源隐藏查找已移除，PlayerSpawner 与 HUD 的生命周期依赖进一步收窄，架构门禁同步增强，Unity Console 当前为 **0 Error / 0 Warning**。按统一评分表重新计算后，整体架构评分为 **9.0/10**。

这个分数不是把所有历史问题都视为完成：剩余扣分主要来自领域迁移桥、部分旧式单例入口、Core 仍未按职责继续拆分，以及 Unity Test Runner 尚未形成稳定的独立执行证据。因此当前可以安全继续普通功能开发，但不应把这些基础设施专项与普通玩法提交混在一起。

## 二、本阶段修理内容

### 2.1 移除运行时 `Resources.Load`

修改：

- `Zephyr(AI)/Zephyr/Assets/Scripts/Core/Localization/LocalizationManager.cs`
- `Zephyr(AI)/Zephyr/Assets/Scenes/Core/Persistent.unity`

调整内容：

- `LocalizationManager` 新增四个显式序列化依赖：
  - 英文字符串表
  - 中文字符串表
  - 英文 Unity `Locale`
  - 中文 Unity `Locale`
- `LoadLocale` 和 `SyncUnityLocale` 不再按字符串路径调用 `Resources.Load`。
- 通过 Unity Editor 一次性脚本将 Persistent 场景中的 `LocalizationManager` 绑定到既有资源：
  - `Assets/Resources/Localization/strings.en_US.json`
  - `Assets/Resources/Localization/strings.zh_CN.json`
  - `Assets/Resources/Localization/Locales/English (en).asset`
  - `Assets/Resources/Localization/Locales/Chinese (Simplified) (zh-Hans).asset`

这项修理把资源依赖从运行时隐式路径查找改成了可检查、可序列化、可在 Inspector 中追踪的组合根依赖，没有新建第二套本地化管线。

### 2.2 收窄 PlayerSpawner 的 GameFlow 依赖

修改：

- `Zephyr(AI)/Zephyr/Assets/Scripts/Player/Flow/PlayerSpawner.cs`

调整内容：

- `PlayerSpawner` 在 `Start` 阶段将 `GameFlow.Instance` 解析为 `IGameFlowGateway`。
- 状态订阅、销毁解绑和玩家保留判断均使用缓存的接口引用。
- 不再在 `ShouldKeepCurrentPlayer` 等运行路径中反复读取 `GameFlow.Instance`。
- `PlayerSpawner` 仍保留作为 Persistent 组合边界的单例入口；本阶段没有冒险改变场景序列化和生命周期契约。

这不是把单例伪装成服务定位器，而是把单例读取限制在组合边界，减少了运行逻辑对具体实现的散布。

### 2.3 将 Gameplay HUD 改为事件驱动

修改：

- `Zephyr(AI)/Zephyr/Assets/Scripts/Player/UI/GameplayHudController.cs`

调整内容：

- HUD 不再直接订阅 `PlayerSpawner.Instance.PlayerSpawned/PlayerDespawned`。
- HUD 改为订阅已有的 `PlayerTargetChangedEvent`。
- `PlayerSpawner` 继续作为玩家生命周期事件的发布者，HUD 只依赖 Core Events 契约。
- 保留 `EventBus.TryGetLatest`，保证 HUD 在晚于玩家生成时启用仍能立即绑定当前玩家。

这降低了 UI 对玩家生成器具体生命周期管理器的反向耦合，也避免了为了 UI 再增加一条玩家查找路径。

### 2.4 增强架构回归门禁

修改：

- `Zephyr(AI)/Zephyr/Assets/Scripts/Tests/Architecture/RuntimeEditorDependencyTests.cs`

新增/调整检查：

- 运行时代码不得使用 `Resources.Load`。
- `PlayerSpawner` 必须在组合边界缓存 `IGameFlowGateway`。
- `GameplayHudController` 必须使用 `PlayerTargetChangedEvent`，不得重新依赖 `PlayerSpawner.Instance`。
- 原有的旧程序集 managed-reference 检查、场景加载权威检查、死亡流程接口检查继续保留。

## 三、验证证据

### 3.1 Unity 路由

- Unity 项目根目录由旧版 `mcp__unity_mcp` 确认：
  `D:\GitHub\Zephyr_v3\Zephyr(AI)\Zephyr`
- 所有非图片 Unity 操作均使用旧版 `mcp__unity_mcp`。
- 本阶段没有调用图片生成管线。

### 3.2 脚本标准验证

以下脚本通过 `Unity_ValidateScript` standard，未发现编译诊断：

- `Assets/Scripts/Core/Localization/LocalizationManager.cs`
- `Assets/Scripts/Player/Flow/PlayerSpawner.cs`
- `Assets/Scripts/Player/UI/GameplayHudController.cs`
- `Assets/Scripts/Tests/Architecture/RuntimeEditorDependencyTests.cs`

`GameplayHudController` 的标准检查曾提示一条字符串拼接的性能建议，但这不是本阶段新增的编译错误；刷新 Unity 资产后 Console 未保留该警告。

### 3.3 Unity Console

执行场景资源绑定、保存、资产刷新后读取 Console：

- Error：`0`
- Warning：`0`
- Exception：`0`
- MissingReference / Serialization / YAML parse：未发现

### 3.4 Persistent 场景引用回读

通过 Unity Editor 重新打开并读取 `Assets/Scenes/Core/Persistent.unity`，确认 `LocalizationManager` 的四个序列化字段均已绑定：

- EN table：`strings.en_US`
- ZH table：`strings.zh_CN`
- EN locale：`English (en)`
- ZH locale：`Chinese (Simplified) (zh-Hans)`

### 3.5 SceneView 截图

本阶段修改了 Persistent 场景的服务对象引用，因此按项目规则完成 SceneView 截图和导出。

- 截图文件：`D:\GitHub\Zephyr_v3\Zephyr(AI)\Zephyr\Assets\MCPTest\Screenshots\2026-10-11_Persistent-localization-explicit-references.png`
- 捕获范围：以 `(0, 0)` 为原点、`20 x 12` 世界单位、`800 x 480` 分辨率。
- 实际观察：Persistent 是服务承载场景，捕获区域没有可见 2D 游戏对象；这是预期结果。未观察到粉色材质、Sprite 丢失、对象重叠或 UI 裁切问题。服务对象引用通过序列化回读另行确认。

### 3.6 静态门禁结果

- 运行时代码中的 `Resources.Load` 匹配数：`0`。
- HUD 中 `PlayerSpawner.Instance` 匹配数：`0`。
- `PlayerSpawner` 具备 `IGameFlowGateway` 缓存字段和组合边界解析语句。
- 之前迁移的三个 ScriptableObject 仍不包含 `asm: Zephyr.Core` 或 `asm: Assembly-CSharp`。
- `git diff --check` 的代码范围未发现代码空白错误；Unity 重新序列化资产保留的 `data: ` 等尾随空格未被手工清理，避免违反“不手写 Unity YAML”的项目规则。

## 四、统一评分标准

评分维度保持前阶段统一口径：

| 维度 | 权重 | 关注点 |
|---|---:|---|
| 边界清晰度 | 25% | UI、用例、服务、基础设施是否职责明确，是否存在重复编排 |
| 依赖方向 | 25% | 高层是否依赖接口/事件契约，领域模块是否反向依赖具体实现 |
| 生命周期与单例安全 | 20% | 单例是否限制在组合根，是否存在隐式查找、重复服务入口或越权加载 |
| 可测试性与门禁 | 15% | 核心规则能否静态验证，架构回归是否有明确门禁 |
| 迁移完整度与整洁度 | 15% | 迁移桥、旧程序集引用、临时兼容代码和基础设施债务是否收敛 |

评分解释：

- `9.0–10.0`：边界稳定，剩余问题已局部化，有明确迁移合同，不阻塞普通功能开发。
- `8.0–8.9`：主架构可用，但仍存在会持续放大维护成本的跨域耦合。
- `7.0–7.9`：已有局部模块化，但生命周期、依赖方向或测试门禁仍不稳定。
- `7.0 以下`：不建议继续叠加功能，应优先处理基础架构。

## 五、重新统一评分

| 维度 | 第五阶段后 | 本阶段后 | 变化说明 |
|---|---:|---:|---|
| 边界清晰度 | 9.0 | **9.1** | HUD 改为事件契约，PlayerSpawner 不再成为 UI 的直接依赖 |
| 依赖方向 | 8.9 | **9.0** | 本地化资源改为显式依赖，PlayerSpawner 使用 gateway 缓存 |
| 生命周期与单例安全 | 8.8 | **9.0** | `Resources.Load` 和一处散布的 GameFlow 单例读取已移除 |
| 可测试性与门禁 | 9.0 | **9.1** | 新增运行时资源、Spawner gateway 和 HUD 事件契约门禁 |
| 迁移完整度与整洁度 | 8.8 | **8.8** | 旧程序集资产债务已清理，但领域迁移桥和 Core 拆分仍未完成 |
| **整体加权评分** | **8.9** | **9.0** | 本阶段安全修理后达到可继续普通开发的高完成度门槛 |

### 阶段评分

| 阶段 | 评分 | 当前判断 |
|---|---:|---|
| 第一阶段 | 9.2/10 | Runtime/Editor 边界和基础程序集门禁稳定 |
| 第二阶段 | 9.0/10 | 领域程序集拆分完成，但保留迁移桥 |
| 第三阶段 | 9.3/10 | UI、用例、死亡流程边界完成度高 |
| 第四阶段 | 9.1/10 | 生命周期事件、场景加载和查找禁令稳定 |
| 第五阶段 | 8.9/10 | UI/单例边界已完成主要修理，但仍有资产迁移债务 |
| **第六阶段整体** | **9.0/10** | 安全扣分项已进一步局部化，达到高完成度验收线 |

## 六、仍然存在的扣分项

以下问题本阶段没有冒险修改，因为它们会触及已有资源、跨程序集契约或玩法流程：

1. `Zephyr.Enemies -> Zephyr.Items/Zephyr.Weapons` 与 `Zephyr.Items -> Zephyr.Weapons` 的领域迁移桥仍存在，需要设计稳定的 Reward/Drop Core Contract 后再移除。
2. `SaveSystem.Instance`、`LocalizationManager.Instance`、`GameFlow.Instance` 在 Meta、拾取物、部分 UI 和流程控制器中仍有直接入口；这些入口应按组合根、Presenter 和事件契约逐批收敛，不能一次性全局替换。
3. `Core` 仍承载 Flow、Persistence、Items、Weapons 等多个职责；进一步拆成 Contracts、Flow、Combat、Persistence 应作为独立架构专项，不与普通玩法功能混做。
4. Unity Test Runner 的独立发现/执行在当前编辑器环境仍不稳定；本阶段有脚本标准验证、静态门禁和 Console 证据，但没有虚报完整 EditMode/PlayMode 测试已经通过。
5. Unity 重新序列化生成的少量资产尾随空格仍存在；这不是运行时架构问题，且不应通过手写 YAML 清理。

## 七、放行结论

### 可以继续的工作

- 普通 UI、关卡、敌人、武器和内容功能开发可以继续。
- 新功能必须继续使用现有 SceneLoader、事件契约、接口 gateway 和 ScriptableObject 配置边界。
- 新增运行时代码不得重新引入 `Resources.Load`、全局 `Find`、越权场景加载或直接把具体 Manager 扩散到领域逻辑。

### 暂不建议混入普通提交的工作

- 删除 Enemies/Items/Weapons 迁移桥。
- 全局单例全面替换。
- Core 大规模程序集重划分。
- Localization、Save 和 Player 生命周期服务的统一依赖注入重构。

**最终判定：本阶段验收成功，整体架构完成度为 9.0/10。剩余危险已被局部化并由门禁覆盖到可接受范围，但尚未达到“所有架构债务均已清零”的状态。**

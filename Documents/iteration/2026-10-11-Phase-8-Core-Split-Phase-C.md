# 第八阶段：Core Split 阶段 C（Runtime 基础设施抽取）

## 一、阶段结论

**本阶段验收成功。**

本阶段完成了 Core Split 的 Runtime 基础设施切片：将 EventBus、ScriptableObject 事件基类、Timer、ObjectPool 和 Logging 从旧 `Zephyr.Core` 迁移到独立的 `Zephyr.Core.Runtime` 程序集，同时保持原 namespace 和脚本 `.meta` GUID 不变。

本阶段没有修改场景、Prefab、ScriptableObject 或 StateMachine 契约，也没有引入第二套服务实现。Unity 导入、编译、关键脚本验证和静态架构门禁均通过。

本阶段完成度评估：**9.2/10**。

整体架构评分由上一阶段的 **9.0/10** 提升至 **9.1/10**。提升来自 Runtime 编译边界和服务生命周期约束；Combat、Persistence、Flow 仍在旧 Core 中，因此不能把本阶段误报为完整 Core 拆分完成。

## 二、阶段目标与实际结果

| 目标 | 结果 | 状态 |
|---|---|---|
| 建立 Runtime 独立程序集 | 新增 `Zephyr.Core.Runtime`，仅引用 `Zephyr.Core.Contracts` | 完成 |
| 抽取通用事件基础设施 | 迁移 EventBus、GameEventSO 基类和监听器 | 完成 |
| 抽取 Timer | 迁移 TimerService、TimerHandle | 完成 |
| 抽取 ObjectPool | 迁移 ObjectPool、PooledObject、PoolRegistry | 完成 |
| 抽取 Logging | 迁移 LogLevel、ZephyrLogger | 完成 |
| 保持 Unity 序列化稳定 | 原 `.meta` 文件随脚本移动，GUID 保持不变 | 完成 |
| 收敛服务生命周期 | 移除 ZephyrLogger 的 `DontDestroyOnLoad` | 完成 |
| 删除所有旧 Core 引用 | 领域程序集仍保留旧 Core 兼容依赖 | 延后，符合阶段 C 范围 |

## 三、Runtime 程序集边界

新增：

`D:\GitHub\Zephyr_v3\Zephyr(AI)\Zephyr\Assets\Scripts\Core\Runtime\Zephyr.Core.Runtime.asmdef`

程序集配置：

```text
name: Zephyr.Core.Runtime
references: Zephyr.Core.Contracts
autoReferenced: false
```

Runtime 程序集不依赖：

- `Zephyr.Core`
- `Zephyr.Core.Flow`
- `Zephyr.Core.Save`
- `Zephyr.Core.Stats`
- Player
- Enemies
- Weapons
- Items
- UI

Runtime 中保留原 namespace，是为了避免在程序集迁移、namespace 重命名和 Unity 序列化修复之间形成不可定位的复合风险。namespace 清理属于后续独立工作，不在本阶段混入。

## 四、实际迁移内容

### 4.1 Event Runtime

迁移到：

`D:\GitHub\Zephyr_v3\Zephyr(AI)\Zephyr\Assets\Scripts\Core\Runtime\Events\`

- `EventBus.cs`
- `GameEventSO.cs`
- `GameEventSO_T.cs`
- `GameEventListener.cs`
- `GameEventListener_T.cs`

以下 Flow 专属事件资产没有迁移：

- `GameStateEventSO.cs`
- `SceneIdEventSO.cs`
- `SceneInfoEventSO.cs`
- `SceneLoadRequestEventSO.cs`

原因是它们当前直接依赖 `SceneSO`、`SceneInfo` 和 `SceneLoadRequest`，属于 Flow 迁移范围，不是通用 Runtime 事件实现。

### 4.2 Timer

迁移到：

`D:\GitHub\Zephyr_v3\Zephyr(AI)\Zephyr\Assets\Scripts\Core\Runtime\Timer\`

- `TimerService.cs`
- `TimerHandle.cs`

Timer 仍保留现有 `TimerService.Instance` 入口。本阶段不进行全局单例替换，只确保其实现归属 Runtime，并由 Persistent 场景持有其生命周期。

### 4.3 ObjectPool

迁移到：

`D:\GitHub\Zephyr_v3\Zephyr(AI)\Zephyr\Assets\Scripts\Core\Runtime\ObjectPool\`

- `ObjectPool.cs`
- `PooledObject.cs`
- `PoolRegistry.cs`

`PooledObject` 继续实现 Contracts 中的 `IPooledObject`。ObjectPool 不知道 Player、Enemy、Projectile 或 Pickup 的具体类型，领域模块只通过现有池对象和接口使用它。

### 4.4 Logging

迁移到：

`D:\GitHub\Zephyr_v3\Zephyr(AI)\Zephyr\Assets\Scripts\Core\Runtime\Logging\`

- `LogLevel.cs`
- `ZephyrLogger.cs`

移除了 `ZephyrLogger` 中的：

```csharp
DontDestroyOnLoad(gameObject);
```

这样服务生命周期由 Persistent 场景结构保证，而不是由单个 Manager 通过 `DontDestroyOnLoad` 伪造。静态扫描确认 Runtime 目录不再包含 `DontDestroyOnLoad`。

## 五、程序集引用变化

以下程序集已显式添加 `Zephyr.Core.Runtime` 引用：

- `Zephyr.Core`
- `Zephyr.Enemies`
- `Zephyr.Gameplay.Interaction`
- `Zephyr.Rooms`
- `Zephyr.Player`
- `Zephyr.Items`
- `Zephyr.Projectiles`
- `Zephyr.Weapons`
- `Zephyr.UI`
- `Zephyr.Testing`
- `Zephyr.Testing.Editor`
- `Zephyr.Tests.EditMode`

当前仍保留 `Zephyr.Core` 引用，是因为这些程序集还使用 Core 中尚未拆出的 Combat、Flow、Persistence、Localization、Camera 或其他实现。Phase C 的目标是增加正确的 Runtime 编译边界，而不是在同一回合强行清空旧 Core 引用。

当前局部依赖图：

```text
Zephyr.Core.Contracts
        ↑
Zephyr.Core.Runtime
        ↑
Core / Player / Enemies / Weapons / Items / Projectiles / Rooms / UI

迁移中兼容依赖：
上述模块 ──► Zephyr.Core
```

## 六、架构门禁

更新：

`D:\GitHub\Zephyr_v3\Zephyr(AI)\Zephyr\Assets\Scripts\Tests\Architecture\RuntimeEditorDependencyTests.cs`

新增 Phase C 检查：

- Runtime asmdef 存在。
- Runtime 只声明 `Zephyr.Core.Contracts` 依赖。
- Runtime 不自动引用。
- Runtime 源码不得使用 `SceneManager`。
- Runtime 源码不得使用 `Resources.Load`。
- Runtime 源码不得使用 `DontDestroyOnLoad`。
- Runtime 源码不得反向依赖 Flow、Save 或 Stats 实现。
- 主要消费者必须直接声明 Runtime 引用。
- 既有 Runtime/Editor、SceneLoader authority、单例和资源查找门禁继续保留。

## 七、验证记录

### 7.1 Unity 路由

- 项目根目录由旧版 `mcp__unity_mcp` 确认：
  `D:\GitHub\Zephyr_v3\Zephyr(AI)\Zephyr`
- 资产刷新使用旧版 `mcp__unity_mcp.Unity_RunCommand` 执行 `AssetDatabase.Refresh`。
- 新程序集由旧版 `mcp__unity_mcp.Unity_ManageAsset` 识别为 `AssemblyDefinitionAsset`。
- 本阶段没有使用图片生成管线。
- 本阶段没有修改场景，因此没有执行 SceneView 截图。

### 7.2 编译和 Console

- Unity Editor 编译：成功。
- `Unity_GetConsoleLogs`：`0 Error / 0 Warning`。
- 未发现 `MissingReference`、`Serialization`、`YAML parse`、`Missing Script` 或程序集解析错误。

### 7.3 脚本标准验证

使用旧版 `mcp__unity_mcp.Unity_ValidateScript` 验证并得到空诊断：

- `Core/Runtime/Events/EventBus.cs`
- `Core/Runtime/Timer/TimerService.cs`
- `Core/Runtime/ObjectPool/PoolRegistry.cs`
- `Core/Runtime/Logging/ZephyrLogger.cs`
- `Tests/Architecture/RuntimeEditorDependencyTests.cs`

### 7.4 静态边界验证

PowerShell 静态检查结果：

- `runtime-paths-ok`
- `runtime-boundary-scan-ok`
- `no-stale-runtime-path-references`
- `asmdef-dependency-graph-acyclic`

四个 Persistent 服务脚本的 GUID 检查结果：

| 服务 | 原 `.meta` GUID 是否保留 | Persistent 场景是否引用该 GUID |
|---|---|---|
| EventBus | 是 | 是 |
| TimerService | 是 | 是 |
| PoolRegistry | 是 | 是 |
| ZephyrLogger | 是 | 当前场景未发现引用 |

Persistent 场景层级读取工具在当前 Editor 状态下返回了 `Initializer` 场景根对象，因此没有把该结果误报为 Persistent 的层级验证；服务引用使用了场景文件中的 GUID 静态检查作为补充证据。

### 7.5 测试限制

本回合没有虚报 EditMode/PlayMode 测试已通过。项目规则要求 Unity 操作统一使用旧版 `mcp__unity_mcp`，当前该路由提供 Test Runner 菜单但没有直接暴露测试执行 API。因此本阶段完成了：

- Unity 编译
- Console 检查
- 关键脚本标准验证
- asmdef JSON 检查
- Runtime 边界静态门禁
- 依赖图无环检查

独立 NUnit/Test Runner 作业仍需在可执行测试接口可用时补跑。

## 八、未完成项与残余风险

1. `Zephyr.Core` 仍然是迁移宿主，Combat、Persistence、Flow 尚未独立。
2. EventBus、Timer 和 Pool 仍保留 Singleton 入口；本阶段只收敛实现归属和生命周期，不进行全局依赖替换。
3. `ZephyrLogger` 当前没有在 Persistent 场景中发现对应 GUID 引用，说明它可能不是当前 Persistent 装配的一部分；后续应确认是否需要将其加入 Persistent，或明确它是未使用的遗留服务。
4. Flow 专属 EventSO 仍依赖旧 Core，后续 Flow 拆分时再迁移。
5. `git diff --check` 仍报告既有 ScriptableObject 修改中的尾随空格；这些来自工作区已有资产改动，本阶段未通过手写 YAML 清理，避免破坏 Unity 序列化内容。
6. Unity Test Runner 独立执行证据仍未形成。

## 九、回滚方式

- Runtime 脚本和原 `.meta` GUID 成对移动，没有重建 Unity 资产引用。
- 回滚时可将 12 个 Runtime 脚本及 `.meta` 文件恢复到原目录，移除 `Zephyr.Core.Runtime.asmdef` 和消费者的 Runtime 引用。
- `ZephyrLogger` 的 `DontDestroyOnLoad` 删除是唯一运行时代码行为调整；如验证发现某个场景依赖跨场景保留，需要先修正场景生命周期装配，不应重新为单个 Manager 恢复隐式持久化。
- 未使用 `git reset --hard`、`git checkout --` 或宽范围删除。

## 十、下一阶段建议

下一阶段进入 **Phase D：Combat 抽取**，但应先补齐 Runtime 测试执行证据或至少建立纯 C# 的 Timer/Pool/事件单元测试入口。

建议顺序：

1. 新建 `Zephyr.Core.Combat`。
2. 迁移 `StatsCore`、`DamageCalculator`、`HealthComponent`、`CombatSystem` 和 Buff 实现。
3. 保持 Combat 只依赖 Contracts 和 Runtime。
4. 让 Player、Enemies、Weapons 直接引用 Combat。
5. 禁止 Combat 依赖 Flow、Persistence、UI 和具体领域程序集。
6. 先验证伤害、生命、属性和 Buff，再处理旧 Core 引用删除。

## 十一、统一评分

| 维度 | Phase A/B 后 | Phase C 后 | 变化说明 |
|---|---:|---:|---|
| 边界清晰度 | 9.3 | **9.4** | Runtime 获得独立编译边界，通用设施不再和 Flow/Combat 混编 |
| 依赖方向 | 9.2 | **9.3** | Runtime 仅依赖 Contracts，禁止反向依赖业务实现 |
| 生命周期与单例安全 | 9.0 | **9.1** | Logger 不再使用 `DontDestroyOnLoad`，服务生命周期回到场景边界 |
| 可测试性与门禁 | 9.2 | **9.3** | 增加 Runtime 依赖、资源查找和生命周期门禁 |
| 迁移完整度与整洁度 | 8.9 | **9.0** | 完成 Contracts + Runtime 两条安全迁移切片，旧 Core 仍待继续拆分 |
| **阶段 C 加权评分** | - | **9.2** | 阶段目标完成度高 |
| **整体加权评分** | **9.0** | **9.1** | Runtime 边界稳定，普通功能开发风险进一步降低 |

**最终判定：Phase C 验收成功。Runtime 基础设施已独立、可编译、可静态约束，并保持现有 Unity 资源引用稳定；下一步应进入 Combat 抽取，而不是继续在 Runtime 中堆积业务逻辑。**

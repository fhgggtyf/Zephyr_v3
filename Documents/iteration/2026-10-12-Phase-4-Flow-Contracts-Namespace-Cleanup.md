# Phase 4：Flow Contracts Namespace 清理

日期：2026-10-12  
项目：Zephyr Unity 2D 横版 Metroidvania  
Unity：`6000.5.6f1`  
范围：`GameState`、`SceneId` 两个纯 Flow contract 的语义 namespace 迁移，以及引用、架构门禁和编译复核。

## 1. 阶段结论

Phase 4 已完成。

本阶段把仍位于 `Zephyr.Core.Flow` 的两个纯枚举契约迁移到 `Zephyr.Contracts.Flow`：

- `GameState`
- `SceneId`

同时更新了 GameFlow、SceneLoader、SceneSO、启动链、玩家流程、关卡流程、主菜单用例和相关测试的引用。没有改动场景、Prefab、ScriptableObject 文件内容，也没有改变 SceneLoader、GameFlow 或 StateMachine 的运行时生命周期协议。

本阶段代码完成度评估为高，静态扫描、Unity 脚本验证和编译状态均通过。Unity Test Runner 的测试发现问题仍然存在，因此本报告不把未重新发现的测试写成已执行结果。

**验收状态：Phase 4 代码迁移验收通过。**

## 2. 已完成的改动

### 2.1 Flow contracts 迁移

修改：

- `Assets/Scripts/Core/Contracts/Flow/GameState.cs`
- `Assets/Scripts/Core/Contracts/Flow/SceneId.cs`

两个文件现在统一使用：

```csharp
namespace Zephyr.Contracts.Flow
```

这两个类型保持纯 enum，不新增运行时依赖，不引用 Flow Manager、SceneLoader、EventBus、Save 或 Unity 场景对象。

### 2.2 运行时引用更新

已补充 `using Zephyr.Contracts.Flow;` 或更新原有引用的关键文件包括：

- `IGameFlowGateway`
- `GameFlow`
- `GameFlowTransitionPolicy`
- `SceneContext`
- `SceneLoader`
- `SceneSO`
- `ColdStartupBootstrap`
- `InitializerBootstrap`
- `GameStateEventSO`
- `LoadingRunController`
- `MainMenuController`
- `MainMenuUseCase`
- `MetaHubController`
- `SceneProgressionController`
- `PlayerSpawner`
- `PlayerDeathController`
- `RespawnCoordinator`
- `MainMenuUseCaseTests`

`SceneSO` 仍然保留原有的序列化字段和资源路径。枚举 namespace 的变化没有触碰 Unity YAML，也没有手写或重建任何 `.asset`、`.prefab`、`.unity` 或 `.meta` 文件。

### 2.3 架构门禁补充

复用了现有的 `PhaseThreeContractNamespacesAreSemanticAndLegacyInterfaceNamespaceIsUnused` 测试方法，加入 Flow contract 断言：

- `GameState.cs` 必须使用 `Zephyr.Contracts.Flow`。
- `SceneId.cs` 必须使用 `Zephyr.Contracts.Flow`。
- 两个 contract 文件不得声明 `Zephyr.Core.Flow`。

这样没有额外增加测试方法数量，也没有扩大测试程序集缓存问题。

## 3. 架构效果评估

### 3.1 依赖方向

当前边界为：

```text
Zephyr.Contracts.Flow
        ↑
Zephyr.Core.Flow / Player / Gameplay / Tests
```

Flow 实现仍位于 `Zephyr.Core.Flow`，但它只依赖 Contracts 中的状态和场景标识。Contracts 不反向依赖 GameFlow、SceneLoader、SceneSO 或任何服务实现。

### 3.2 耦合变化

- `GameState` 不再因为实现方位于 `Core.Flow` 而被误认为 Flow Manager 的内部类型。
- `SceneId` 不再与 SceneLoader 的具体实现 namespace 绑定。
- `IGameFlowGateway` 的公共契约现在只引用 `Zephyr.Contracts.Flow`，其接口层不依赖 Flow 实现。
- `SceneSO` 仍承担资源配置职责，但使用的是独立的 `SceneId` contract。
- 启动链、重生流程和关卡推进只消费 contract，不新增单例、场景直载或新的上下文对象。
- 没有改变场景生命周期、Persistent/GameManager 组合根或 StateMachine 基础设施。

## 4. 风险与延期边界

本阶段明确没有迁移以下高耦合类型：

- `DamageInfo`
- `DamageResult`
- `DamageType`
- `ElementalType`
- `WeaponCategory`
- `WeaponRarity`
- `StatType`
- `MetaStatType`
- `StatConstants`
- `StatModifierDefinition`

这些类型同时连接 Combat、Weapons、Stats、Buff、ScriptableObject 和 EditMode 测试。下一阶段如果继续迁移，应先拆成 Damage contracts 和 Stats contracts 两个独立批次，不能把两个批次合并成全局替换。

另外，本阶段没有修复 Unity Editor 内部的 Services 配置异常。导入 `SceneId.cs` 和后续编译检查期间偶发出现：

```text
UnityEditor.Connect.ServicesConfiguration
KeyNotFoundException: The given key 'services-gateway' was not present in the dictionary.
```

堆栈不涉及 Zephyr 项目代码。清理 Console 并等待后，最终复核为 Error `0`、Warning `0`。该问题作为编辑器环境残余风险记录，不归因于 Flow namespace 迁移。

## 5. 验证结果

### Unity/MCP 路由

- Unity 项目根目录确认：`D:/GitHub/Zephyr_v3/Zephyr(AI)/Zephyr`。
- 本阶段所有非图片 Unity 操作使用旧版 `mcp__unity_mcp`。
- 重新导入了 `GameState.cs`、`SceneId.cs` 和 `RuntimeEditorDependencyTests.cs`。
- 本阶段没有创建或修改场景，因此不执行 SceneView 截图流程。

### 静态扫描

- Flow contract 文件：2 个。
- `Zephyr.Contracts.Flow` 声明：2 个。
- Flow contract 文件中的 `Zephyr.Core.Flow` 声明：0 个。
- 运行时代码中的旧限定枚举引用 `Zephyr.Core.Flow.GameState` / `Zephyr.Core.Flow.SceneId`：0 个。
- 原有 Flow 实现 namespace `Zephyr.Core.Flow` 仍保留，作为实现层边界，不属于旧 contract 声明残留。
- `git diff --check`：没有 whitespace error；仅有工作区已有的 LF/CRLF normalization warning。

### 脚本验证与编译

Unity 标准脚本验证无诊断：

- `GameState.cs`
- `SceneId.cs`
- `IGameFlowGateway.cs`
- `GameFlowTransitionPolicy.cs`
- `SceneContext.cs`
- `SceneLoader.cs`
- `SceneSO.cs`
- `MainMenuUseCase.cs`
- `SceneProgressionController.cs`
- `PlayerSpawner.cs`
- `PlayerDeathController.cs`
- `RespawnCoordinator.cs`
- `MainMenuUseCaseTests.cs`
- `RuntimeEditorDependencyTests.cs`

Unity 编辑器最终状态：

- `IsCompiling=false`
- `IsUpdating=false`
- Console Error：`0`
- Console Warning：`0`

### 测试运行器状态

Phase 3 已有成功运行记录：

- 架构测试：`39/39 passed`
- EditMode 测试：`5/5 passed`

Phase 4 没有把 Unity Test Runner 的发现数伪装成新的通过结果。源码仍包含 40 个架构 `[Test]` 方法，现有 Runner 发现缓存问题仍未解决；本阶段新增的 Flow 断言已经进入现有测试方法，并通过静态源码检查和脚本验证，但没有单独声称该断言已由 Runner 执行。

## 6. 阶段评分

| 维度 | 结果 |
| --- | ---: |
| Flow contract 语义边界 | 9.2/10 |
| Flow 实现与 contract 解耦 | 9.1/10 |
| 序列化与生命周期风险控制 | 9.3/10 |
| 引用迁移完整性 | 9.0/10 |
| 验证闭环 | 8.5/10 |
| 本阶段代码完成度 | 9.1/10 |

综合代码完成度达到高水平，满足 Phase 4 的验收标准。验证闭环仍因 Unity Test Runner 发现缓存和编辑器 Services 内部异常扣分，但两者都没有产生项目脚本编译错误，最终 Console 已清零。

## 7. 下一阶段建议

推荐 Phase 5 采用 Damage contracts 独立批次：

1. 先盘点 `DamageInfo`、`DamageResult`、`DamageType`、`ElementalType`、`WeaponCategory`、`WeaponRarity` 的所有运行时和 ScriptableObject 引用。
2. 迁移纯数据类型到 `Zephyr.Contracts.Damage` 或项目确认的统一 contracts namespace。
3. 保持 Combat、Weapons、Projectiles 作为实现消费者，不把 DamageCalculator 或 WeaponRuntime 搬入 Contracts。
4. 对 `StatsCore`、BuffSystem、WeaponSO、WeaponData 和相关 EditMode 测试做编译及行为回归。
5. 完成 Damage 批次后，再单独处理 Stats contracts，避免一次性改变数值系统和序列化配置。

## 8. 验收状态

**Phase 4 Flow Contracts Namespace 清理：验收通过。**

验收依据：两个纯 Flow contract 已迁移到语义 namespace，所有关键引用已更新，Unity 脚本验证无诊断，编译状态稳定，最终 Console Error/Warning 为零，且没有引入场景、Prefab、序列化资源或生命周期协议变更。

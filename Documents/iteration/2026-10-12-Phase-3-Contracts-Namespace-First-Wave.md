# Phase 3：Contracts Namespace 第一批安全迁移

日期：2026-10-12  
项目：Zephyr Unity 2D 横版 Metroidvania  
Unity：`6000.5.6f1`  
范围：纯接口 namespace、一个跨模块事件 payload、迁移后的编译与架构门禁复核。

## 1. 阶段结论

Phase 3 本次执行的是“第一批安全迁移”，不是整个 Contracts 目录的一次性重命名。

本批次已经完成代码层面的主要目标：纯接口从旧的 `Zephyr.Core.Interfaces` 收敛到语义明确的 `Zephyr.Contracts.Interfaces`；`PlayerTargetChangedEvent` 从运行时 EventBus namespace 移到 `Zephyr.Contracts.Events`；所有运行时代码中的旧接口 namespace 引用已经清零。

本阶段代码完成度评估为高，迁移后的编译和 Console 门禁通过。但 Unity Test Runner 仍只发现 39 个架构测试，而源码实际包含 40 个 `[Test]` 方法，新增加的 Phase 3 门禁没有被测试运行器发现。因此本阶段判定为：**代码迁移验收通过，测试发现缓存问题作为残余验证风险保留；不宣称 40/40 已执行。**

## 2. 已完成的改动

### 2.1 纯接口 namespace 收敛

`Assets/Scripts/Core/Contracts/Interfaces/` 下的 17 个接口文件统一使用：

```csharp
namespace Zephyr.Contracts.Interfaces
```

涉及的契约包括伤害、治疗、护盾、交互、朝向、Tick、对象池、玩家生成/重生、流程 Gateway、潜在攻击源和属性来源等接口。

同时更新了所有受影响的 `using` 和程序集引用，使 Player、Gameplay、Items、Weapons、Projectiles、Combat、Flow、UI 和 Runtime 实现继续依赖 Contracts，而不依赖旧的 `Zephyr.Core.Interfaces` namespace。

### 2.2 PlayerTargetChangedEvent 边界调整

`PlayerTargetChangedEvent` 现在位于：

`Assets/Scripts/Core/Contracts/Events/PlayerTargetChangedEvent.cs`

其 namespace 为：

```csharp
namespace Zephyr.Contracts.Events
```

EventBus 的实现仍保留在 `Zephyr.Core.Events`。这次调整只移动了 payload 的语义归属，没有把 EventBus 实现带入 Contracts，也没有建立 Contracts 对 Runtime EventBus 的反向依赖。

已更新的消费者包括：

- `CurrencyPickup`
- `WeaponPickup`
- `EnemyBrain`
- `GustInteractable`
- `PlayerSpawner`
- `GameplayHudController`

### 2.3 迁移遗漏修复

编译复核发现 `PooledObject` 仍使用旧的相对限定名 `Interfaces.IPooledObject`。已修正为引用 `Zephyr.Contracts.Interfaces.IPooledObject`，没有改变 ObjectPool 生命周期或接口契约。

## 3. 明确延期的 Contracts 类型

以下类型没有在本阶段强行迁移，因为它们同时被 Combat、Stats、Flow、ScriptableObject 和序列化数据使用，直接改名会扩大本阶段风险面：

- `GameState`
- `SceneId`
- `DamageInfo`
- `DamageResult`
- `DamageType`
- `ElementalType`
- `WeaponCategory`
- `WeaponRarity`
- `StatType`
- `MetaStatType`
- `StatConstants`
- `StatModifierDefinition` 及其相关 Stats contract 类型

这些文件仍保留在现有的 `Zephyr.Core.Flow`、`Zephyr.Core.DamageSystem`、`Zephyr.Core.Stats` 或 `Zephyr.Core` namespace。它们不是本阶段的遗漏，而是为了让 namespace 清理按依赖风险分批进行的明确延期项。

下一批迁移前必须分别核对：

1. Combat 运行时和 Weapon/Enemy 配置的引用方向。
2. StatsCore 与 ScriptableObject 序列化字段的兼容性。
3. Flow 事件和 SceneSO 引用是否会因 namespace 迁移触发资源重新序列化。
4. 迁移顺序、回滚点和 Unity 重新导入后的测试发现状态。

## 4. 架构效果评估

### 4.1 依赖方向

本批次后的方向为：

```text
Contracts.Interfaces / Contracts.Events
                ↑
     Runtime / Flow / Combat / Player / Gameplay / UI
```

Contracts 只承载跨模块协议和 payload，不引用 EventBus 实现、Save、Flow Manager 或具体 Actor。EventBus 仍位于 Runtime，并作为实现方依赖 Contracts；消费者只需要同时引用协议和 EventBus 的运行时 API。

### 4.2 耦合变化

- 旧接口 namespace 不再暴露 `Core` 运行时实现语义。
- 接口实现可以继续位于 Core、Player、Gameplay、Items、Weapons 等模块，不需要把实现搬入 Contracts。
- `PlayerTargetChangedEvent` 不再与 EventBus 实现 namespace 绑定，事件消费者对 payload 与传输机制的耦合被拆开。
- 没有新增 Service Locator、Singleton、SceneManager 直接调用或平行 EventBus。
- 没有改动 StateMachine 基础设施、Prefab、Scene、ScriptableObject 序列化身份或生命周期协议。

## 5. 验证结果

### Unity/MCP 路由

- Unity 项目根目录确认：`D:/GitHub/Zephyr_v3/Zephyr(AI)/Zephyr`。
- 本阶段所有非图片 Unity 操作使用旧版 `mcp__unity_mcp`。
- 对 `PooledObject.cs` 和 `RuntimeEditorDependencyTests.cs` 执行了旧版 MCP 资源导入。
- 本阶段没有创建或修改场景，因此不执行 SceneView 截图流程。

### 静态扫描

- `Core/Contracts/Interfaces` 接口文件：17 个。
- 使用 `Zephyr.Contracts.Interfaces` 的接口文件：17 个。
- 运行时代码中 `Zephyr.Core.Interfaces` 或旧接口 namespace 声明：0 处。
- 源码中的架构 `[Test]` 方法：40 个。
- `git diff --check`：没有 whitespace error；Git 仅报告现有工作区文件的 LF/CRLF normalization warning。

### 脚本和编译

- `RuntimeEditorDependencyTests.cs`：`Unity_ValidateScript` 无诊断。
- `PooledObject.cs`：`Unity_ValidateScript` 无诊断。
- Unity 编辑器状态：`IsCompiling=false`，`IsUpdating=false`。
- 修复旧接口限定名后，项目 Console 最终复核为：Error `0`，Warning `0`。

### 测试运行器限制

已有成功运行记录：

- 架构测试：`39/39 passed`。
- EditMode 测试：`5/5 passed`。

但源码扫描显示 `RuntimeEditorDependencyTests.cs` 已有 40 个 `[Test]` 方法，其中新增的：

```text
PhaseThreeContractNamespacesAreSemanticAndLegacyInterfaceNamespaceIsUnused
```

没有被当前 Unity Test Runner 发现。已尝试资源刷新、测试脚本重新导入、确认编辑器不在编译/更新状态，但发现数仍为 39。该问题属于测试程序集/Runner 缓存发现问题，不能把静态存在的新门禁写成已执行测试。

### Unity 编辑器环境残余日志

一次测试脚本重新导入后，Console 短暂出现 Unity 内部的：

```text
UnityEditor.Connect.ServicesConfiguration
KeyNotFoundException: The given key 'services-gateway' was not present in the dictionary.
```

堆栈只涉及 UnityEditor.Connect，不涉及 Zephyr 项目脚本。清理 Console 并等待后再次复核，Error/Warning 均为 0，未再次复现。该日志记录为编辑器环境残余风险，不归因于本阶段代码。

## 6. 阶段评分

| 维度 | 结果 |
| --- | ---: |
| Contracts 接口语义边界 | 9.2/10 |
| 事件 payload 与传输实现解耦 | 9.1/10 |
| 依赖方向与程序集影响控制 | 9.0/10 |
| 迁移回归安全性 | 8.8/10 |
| 验证闭环 | 8.4/10 |
| 本阶段代码完成度 | 9.1/10 |

本阶段的代码迁移范围完成度达到高水平，达到“可以继续下一批架构专项”的标准。综合验证分低于代码分的唯一主要原因，是 Unity Test Runner 没有发现新增的第 40 个架构测试；这不应通过删除测试或修改统计口径来规避。

## 7. 下一阶段建议

建议下一阶段继续保持小批次迁移，不要一次性重命名所有 Contracts 类型：

1. 先修复或定位 `Zephyr.Architecture.Tests` 的测试发现缓存，使 40 个架构测试能被实际执行。
2. 单独迁移 Flow contracts：`GameState`、`SceneId` 及其事件消费者。
3. 再迁移 Damage contracts：`DamageInfo`、`DamageResult`、Damage/Weapon/Element 类型。
4. 最后迁移 Stats contracts，并对 `StatsCore`、SO 和相关 EditMode 测试做专门回归。
5. 每一批都保留旧 namespace 零引用门禁、编译门禁和可回滚点。

## 8. 验收状态

**Phase 3 第一批安全迁移：代码验收通过。**

验收边界为：接口和 PlayerTarget 事件的 namespace 清洁已完成，编译错误已修复，最终 Console 清零，架构方向保持合理；未把延期的数据契约迁移和 Unity Test Runner 的第 40 个测试发现问题伪装成已完成。

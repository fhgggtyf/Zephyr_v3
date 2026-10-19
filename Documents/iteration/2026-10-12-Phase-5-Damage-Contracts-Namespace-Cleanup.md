# Phase 5：Damage Contracts Namespace 清理

日期：2026-10-12  
项目：Zephyr Unity 2D 横版 Metroidvania  
Unity：`6000.5.6f1`  
范围：Damage payload、Damage 枚举和 Weapon metadata contract 的语义 namespace 迁移，以及 Combat/Weapons/Projectiles/Enemies/Player 引用、架构门禁和测试复核。

## 1. 阶段结论

Phase 5 已完成，验收通过。

本阶段将六个纯 Damage contract 从 `Zephyr.Core.DamageSystem` 迁移到 `Zephyr.Contracts.Damage`：

- `DamageInfo`
- `DamageResult`
- `DamageType`
- `ElementalType`
- `WeaponCategory`
- `WeaponRarity`

DamageCalculator、CombatSystem、HealthComponent、WeaponRuntime 和其他计算/控制实现仍保留在原有实现 namespace。本阶段只改变契约归属和引用方向，没有重写伤害公式、攻击输入、武器运行时或序列化资源。

验证过程中 Unity Test Runner 恢复发现完整测试程序集，并发现了一个此前静态扫描未覆盖的接口残留：`IWeaponPickup` 仍位于 `Zephyr.Core.Contracts`。该接口已一并迁移到 `Zephyr.Contracts.Interfaces`，相关 Boss reward 和 Weapon pickup 引用已修复。

最终结果：

- 架构测试：`40/40 passed`
- EditMode 测试：`5/5 passed`
- Unity Console 最终 Error：`0`
- Unity Console 最终 Warning：`0`
- 本阶段代码完成度：`9.3/10`

## 2. 已完成的改动

### 2.1 Damage contracts 迁移

以下文件现在统一使用：

```csharp
namespace Zephyr.Contracts.Damage
```

- `Assets/Scripts/Core/Contracts/DamageSystem/DamageInfo.cs`
- `Assets/Scripts/Core/Contracts/DamageSystem/DamageResult.cs`
- `Assets/Scripts/Core/Contracts/DamageSystem/DamageType.cs`
- `Assets/Scripts/Core/Contracts/DamageSystem/ElementalType.cs`
- `Assets/Scripts/Core/Contracts/DamageSystem/WeaponCategory.cs`
- `Assets/Scripts/Core/Contracts/DamageSystem/WeaponRarity.cs`

这些类型仍然是纯数据/枚举契约。`DamageInfo` 和 `DamageResult` 保持原有 readonly struct 形态；DamageCalculator 仍然负责计算，HealthComponent 仍然负责应用结果。

### 2.2 消费者引用更新

已将 Damage contract 消费者改为引用 `Zephyr.Contracts.Damage`，包括：

- `IDamageable`
- `IDamageFilter`
- `DamageCalculator`
- `CombatSystem`
- `HealthComponent`
- `StatsCore`
- `BuffSystem`
- `ProjectileSystem`
- `MeleeHitBoxWeaponComponent`
- `WeaponData`
- `WeaponDamageBuilder`
- `WeaponPickup`
- `WeaponSO`
- `BossController`
- `BossSO`
- `EnemyAttackController`
- `EnemyBrain`
- `EnemySO`
- `EnemyStats`
- `HurtReceiver`
- `PlayerImpactController`
- `PlayerDeathController`
- `TestDummy`

实现层仍保留对 `Zephyr.Core.DamageSystem` 的引用，用于访问 `DamageCalculator` 等实现类型；这不是旧 contract 引用残留。

### 2.3 补齐 IWeaponPickup 接口残留

测试运行器在完整发现 40 个架构测试后指出：

```text
IWeaponPickup.cs
Expected: namespace Zephyr.Contracts.Interfaces
But was: namespace Zephyr.Core.Contracts
```

已完成：

- `IWeaponPickup` 迁移到 `Zephyr.Contracts.Interfaces`。
- `BossRewardDrop` 改为引用 `Zephyr.Contracts.Interfaces`。
- `WeaponPickup` 删除旧的 `Zephyr.Core.Contracts` using。

这使 `Assets/Scripts/Core/Contracts/Interfaces/` 下 18 个接口全部使用语义 namespace。

### 2.4 架构门禁

扩展现有的 `PhaseThreeContractNamespacesAreSemanticAndLegacyInterfaceNamespaceIsUnused` 测试，增加六个 Damage contract 的断言：

- 每个文件必须声明 `Zephyr.Contracts.Damage`。
- 每个文件不得声明 `Zephyr.Core.DamageSystem`。

没有新增平行测试程序集、没有删除旧测试，也没有修改测试统计口径。

## 3. 架构效果评估

### 3.1 依赖方向

当前 Damage 边界为：

```text
Zephyr.Contracts.Damage
          ↑
Combat / Weapons / Projectiles / Enemies / Player / Testing
```

实现方向保持为：

```text
Combat implementation  -> Contracts.Damage
DamageCalculator       -> Contracts.Damage
HealthComponent        -> Contracts.Damage
Weapon implementations -> Contracts.Damage
```

Contracts 不依赖 DamageCalculator、HealthComponent、WeaponRuntime、StatsCore 或 EventBus。计算和运行时控制没有下沉到 Contracts。

### 3.2 耦合变化

- Damage payload 与 DamageCalculator 的实现 namespace 解耦。
- Weapon category、element 和 rarity 不再被误认为 DamageSystem 实现内部类型。
- `IDamageable` 和 `IDamageFilter` 只依赖 Damage contract，不依赖 Combat 实现。
- Combat、Weapons、Projectiles 和 Enemy 仍通过稳定的数据协议交互，未引入新的 Service Locator、Singleton 或事件通道。
- `DamageInfo` 的字段、构造函数、数组语义和盾牌计算输入没有变化。
- 没有修改任何场景、Prefab、ScriptableObject、Animator 或 StateMachine 文件。

## 4. 风险与延期边界

本阶段没有迁移 Stats contracts：

- `StatType`
- `MetaStatType`
- `StatConstants`
- `StatModifierDefinition`

这些类型仍连接 StatsCore、BuffSystem、SceneSO、Meta 和 ScriptableObject 数据。它们应作为独立的 Stats contract 阶段处理，避免与 Damage namespace 迁移绑定在同一次大范围改动中。

本阶段也没有改变 `Zephyr.Core.DamageSystem` 实现 namespace。该 namespace 仍由 `DamageCalculator` 使用，属于实现边界，不应在下一阶段通过全局文本替换删除。

## 5. 验证结果

### Unity/MCP 路由

- Unity 项目根目录：`D:/GitHub/Zephyr_v3/Zephyr(AI)/Zephyr`。
- 非图片 Unity 资源导入、脚本验证和 Console 检查使用旧版 `mcp__unity_mcp`。
- 重新导入了 Damage contract、`IWeaponPickup`、`BossRewardDrop`、`WeaponPickup` 和架构测试脚本。
- 本阶段没有创建或修改场景，因此不执行 SceneView 截图流程。

### 静态扫描

- Damage contract 文件：6 个。
- `Zephyr.Contracts.Damage` 声明：6 个。
- Damage contract 文件中的旧 `Zephyr.Core.DamageSystem` 声明：0 个。
- Contracts 接口文件：18 个。
- Contracts 接口中的旧 `Zephyr.Core.Contracts` 声明：0 个。
- 运行时代码中的旧完整 Damage contract 类型限定名：0 个。
- 架构测试源码方法数：40 个。
- `git diff --check`：没有 whitespace error；Git 只报告现有工作区的 LF/CRLF normalization warning。

### 脚本验证

Damage contract、接口、DamageCalculator、StatsCore、WeaponSO、WeaponData、WeaponDamageBuilder、ProjectileSystem、HealthComponent、BossRewardDrop、WeaponPickup 和架构测试脚本均通过 Unity 标准脚本验证，无编译诊断。

`WeaponPickup` 的脚本验证提示了已有的 `Update()` 字符串拼接可能造成 GC 分配。这不是本阶段引入的问题，也不属于 Damage contract 边界，保留为后续 UI/运行时性能专项。

### 测试

实际 Unity Test Runner 结果：

- `Zephyr.Architecture.Tests`：`40/40 passed`，`0 failed`，`0 skipped`。
- `Zephyr.Tests.EditMode`：`5/5 passed`，`0 failed`，`0 skipped`。

本次测试已经实际发现并验证了 Phase 3/4 contract 门禁和 Phase 5 Damage namespace 门禁，不再存在此前“源码 40 个、Runner 仅发现 39 个”的发现数差异。

### Console

导入和测试过程中偶发出现 Unity Editor 自身的：

```text
UnityEditor.Connect.ServicesConfiguration
KeyNotFoundException: The given key 'services-gateway' was not present in the dictionary.
```

该堆栈只涉及 `UnityEditor.Connect`，不涉及 Zephyr 项目脚本。清理 Console 并等待后最终复核：

- Error：`0`
- Warning：`0`

## 6. 阶段评分

| 维度 | 结果 |
| --- | ---: |
| Damage contract 语义边界 | 9.4/10 |
| Combat/Weapons 实现解耦 | 9.2/10 |
| 引用迁移完整性 | 9.3/10 |
| 序列化与行为风险控制 | 9.4/10 |
| 架构门禁与测试闭环 | 9.4/10 |
| 本阶段代码完成度 | 9.3/10 |

本阶段完成度很高，已达到验收标准。测试发现数恢复为完整的 40 个后，验证闭环相较 Phase 4 明显改善。

## 7. 下一阶段建议

建议 Phase 6 单独处理 Stats contracts：

1. 盘点 `StatType`、`MetaStatType`、`StatConstants`、`StatModifierDefinition` 的引用和 ScriptableObject 序列化字段。
2. 先迁移纯 enum/常量，再迁移 `StatModifierDefinition` 这类带 Unity/数据结构依赖的 contract。
3. 保持 `StatsCore`、`BuffSystem`、`SceneStatsModifierController` 和 Meta services 处于实现层。
4. 对 StatsCore EditMode 测试和 SceneSO/MetaUpgrade 资源做专项回归。
5. 不在 Stats 阶段顺手修改 DamageCalculator 或新增数值服务。

## 8. 验收状态

**Phase 5 Damage Contracts Namespace 清理：验收通过。**

验收依据：六个 Damage contract 已迁移到语义 namespace，18 个接口已无旧 Contracts namespace 残留，隐藏的 `IWeaponPickup` 残留已修复，架构测试 `40/40` 和 EditMode 测试 `5/5` 全部通过，最终项目 Console Error/Warning 为零，未引入场景、Prefab、序列化资源或运行时行为协议变化。

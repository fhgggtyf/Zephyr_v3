# 第七阶段：Core Split 阶段 A/B（安全网与 Contracts 抽取）

## 一、阶段结论

**本阶段按既定范围验收成功。**

本阶段没有尝试一次性重写 `Zephyr.Core`，而是完成了 Core Split 的第一条可回滚切片：先建立程序集和架构门禁，再抽取稳定的跨模块契约。当前 `Zephyr.Core` 仍是迁移中的旧总程序集，领域程序集仍保留对它的兼容引用，因此本报告不把“Contracts 抽取完成”误报为“Core 全部拆分完成”。

本阶段完成度评估：**9.2/10（针对阶段 A/B 范围）**。

整体架构评分维持：**9.0/10**。本阶段改善了编译边界和新依赖的可见性，但尚未处理 Combat、Persistence、Flow 实现程序集的继续拆分，也没有删除历史迁移桥。

## 二、阶段目标与实际结果

| 目标 | 结果 | 状态 |
|---|---|---|
| 建立 Core Split 安全网 | 新增 Contracts 程序集约束和架构测试门禁 | 完成 |
| 防止 Contracts 反向依赖旧 Core | `Zephyr.Core.Contracts` 无程序集引用且关闭自动引用 | 完成 |
| 抽取稳定接口 | 迁移无实现依赖的跨模块接口 | 完成 |
| 抽取稳定值类型和枚举 | 迁移战斗载荷、统计枚举和流程标识 | 完成 |
| 保持 Unity 序列化稳定 | 原 `.meta` 文件随脚本移动，未手写 Scene/Prefab/Asset YAML | 完成 |
| 结束旧 Core 依赖 | 领域程序集仍保留 `Zephyr.Core` 兼容依赖 | 延后，符合本阶段范围 |

## 三、阶段 A：安全网

### 3.1 新增架构边界

新增：

`Zephyr(AI)/Zephyr/Assets/Scripts/Core/Contracts/Zephyr.Core.Contracts.asmdef`

该程序集的关键约束：

- `references` 为空，不依赖 `Zephyr.Core` 或任何领域程序集。
- `autoReferenced` 为 `false`，消费者必须显式声明依赖。
- 允许使用 Unity 基础类型，但不包含场景加载、资源查找或具体服务解析。
- 稳定 namespace 暂时保持不变，降低 Unity 序列化和全局 namespace 迁移风险。

### 3.2 新增架构门禁

更新：

`Zephyr(AI)/Zephyr/Assets/Scripts/Tests/Architecture/RuntimeEditorDependencyTests.cs`

新增门禁覆盖：

- Contracts 程序集存在且不引用旧 Core。
- Contracts 程序集不是隐式自动引用。
- Contracts 源码不得使用 `SceneManager`、`Resources.Load` 或 `GameFlow.Instance`。
- 主要 Runtime/Test 程序集必须直接声明 `Zephyr.Core.Contracts`。
- 原有 Runtime/Editor、场景加载 authority、UI/单例边界门禁继续保留。

这一步的目的不是立刻删除所有旧依赖，而是建立“新代码不能继续扩大旧 Core 依赖”的检测基础。

## 四、阶段 B：Contracts 抽取

### 4.1 已迁移接口

以下接口已移动到 `Core/Contracts/Interfaces/`，namespace 暂时保持 `Zephyr.Core.Interfaces`：

- `IDamageable`
- `IDamageFilter`
- `IFacingProvider`
- `IGameFlowGateway`
- `IGustJumpReceiver`
- `IHealable`
- `IInteractable`
- `IInteractionFocusReceiver`
- `ILevelObjective`
- `IPlayerRespawnGateway`
- `IPooledObject`
- `IPotentialAttackSource`
- `IShieldable`
- `IStatSource`
- `ITickable`

暂未迁移 `IBuffable`。它直接依赖当前 Buff 实现中的 `BuffApplication` 和 `BuffConfigSO`，强行移动会把 Buff 实现和 Contracts 一起带入，违背本阶段“稳定契约先行”的目标。

### 4.2 已迁移值类型和枚举

已迁移到 `Core/Contracts/` 对应子目录：

- `StatType`
- `MetaStatType`
- `DamageInfo`
- `DamageResult`
- `DamageType`
- `ElementalType`
- `WeaponCategory`
- `WeaponRarity`
- `GameState`
- `SceneId`
- `PlayerTargetChangedEvent`

这些类型的 namespace 暂时保持不变，因此本阶段不要求对 Player、Enemy、Weapon、UI 的业务代码进行大范围 namespace 重命名。

### 4.3 已显式接入 Contracts 的程序集

以下程序集已添加直接引用：

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
- `Zephyr.Tests.EditMode`

当前这些程序集仍然引用 `Zephyr.Core`，但 Contracts 的依赖已经显式存在，为后续逐个删除旧 Core 引用提供了编译迁移落点。

## 五、明确保留的旧 Core 内容

以下内容本阶段没有迁移：

- `EventBus` 和 `GameEventSO` 实现。
- `SceneLoader`、`GameFlow`、`SceneSO`、`SceneInfo` 和 `SceneLoadRequest` 实现。
- `SaveSystem`、Meta、Localization、Audio、Camera、Timer、ObjectPool 的具体实现。
- `CombatSystem`、`DamageCalculator`、`HealthComponent`、`StatsCore`、`BuffSystem` 的实现。
- `IBuffable` 及其 Buff 配置依赖。
- `GameConstants`、`GameSettingsSO` 等仍与实现服务耦合的配置。

这样做是有意的：阶段 B 只抽取契约和稳定载荷，不把 Runtime、Combat、Persistence、Flow 四个后续专项混成一次大迁移。

## 六、依赖方向变化

本阶段后的局部依赖图：

```text
Zephyr.Core.Contracts
        ↑
        ├── Zephyr.Core
        ├── Zephyr.Player
        ├── Zephyr.Enemies
        ├── Zephyr.Weapons
        ├── Zephyr.Projectiles
        ├── Zephyr.Items
        ├── Zephyr.Rooms
        ├── Zephyr.Gameplay.Interaction
        ├── Zephyr.UI
        ├── Zephyr.Testing
        └── Zephyr.Tests.EditMode

Legacy transition state:
Player / Enemies / Weapons / Items / UI / Rooms -> Zephyr.Core
```

当前最重要的安全性质是：

```text
Contracts -X-> Zephyr.Core
Contracts -X-> Player / Enemies / Weapons / Items / UI
Contracts -X-> SceneLoader / GameFlow.Instance
```

后续阶段才会把 `Zephyr.Core` 内的实现继续拆为 Runtime、Combat、Persistence 和 Flow，并逐一移除领域程序集对旧 Core 的引用。

## 七、验证记录

### 7.1 Unity 路由

- 项目根目录由旧版 `mcp__unity_mcp` 确认：
  `D:\GitHub\Zephyr_v3\Zephyr(AI)\Zephyr`
- 资产刷新使用旧版 `mcp__unity_mcp.Unity_RunCommand` 调用 `AssetDatabase.Refresh`。
- 本阶段没有使用图片生成管线。
- 本阶段没有修改场景、Prefab、ScriptableObject 或其他 Unity 序列化对象，因此不需要 SceneView 截图。

### 7.2 编译和 Console

- Unity Editor 编译：成功。
- `Unity_GetConsoleLogs`：`0 Error / 0 Warning`。
- 未发现 `MissingReference`、`Serialization`、`YAML parse`、`Missing Script` 或程序集解析错误。

### 7.3 脚本验证

使用旧版 `mcp__unity_mcp.Unity_ValidateScript` 完成标准验证，诊断为空：

- `Core/Contracts/Interfaces/IDamageable.cs`
- `Core/Contracts/DamageSystem/DamageInfo.cs`
- `Core/Contracts/Interfaces/IGameFlowGateway.cs`
- `Core/Contracts/Events/PlayerTargetChangedEvent.cs`
- `Core/Flow/GameFlow.cs`
- `Core/Combat/CombatSystem.cs`
- `Core/Stats/StatsCore.cs`
- `Tests/Architecture/RuntimeEditorDependencyTests.cs`

### 7.4 静态门禁

PowerShell 静态检查结果：

- `phase-ab-static-gates-ok`
- 所有目标 asmdef JSON 可解析。
- Contracts 目录不存在 `SceneManager.`、`Resources.Load`、`GameFlow.Instance`。
- 所有阶段 A/B 目标程序集都直接引用 `Zephyr.Core.Contracts`。
- Contracts asmdef 不引用旧 Core。

## 八、测试限制

本回合没有虚报 EditMode/PlayMode 测试已通过。项目规则指定 Unity 操作统一使用旧版 `mcp__unity_mcp`，当前该路由提供 Test Runner 窗口菜单，但没有直接暴露测试执行 API；因此本阶段完成了 Unity 编译、标准脚本验证和静态架构门禁验证，未执行独立 NUnit/Test Runner 作业。

这不阻塞本阶段的 Contracts 编译验收，但在后续阶段必须补上稳定的 EditMode 执行证据，尤其是：

- `StatsCoreTests`
- `StateTransitionTests`
- `MainMenuUseCaseTests`
- `RuntimeEditorDependencyTests`

## 九、风险与回滚

### 当前残余风险

1. 领域程序集仍引用 `Zephyr.Core`，所以 Core 尚未达到最终拆分状态。
2. `EventBus` 仍是 Core 中的具体实现，后续需要归入 Runtime，而事件 payload 继续留在 Contracts。
3. `SceneLoadRequest`、`SceneInfo` 仍和 `SceneLoader` 同文件，后续 Flow 拆分时需要先建立独立流程载荷文件。
4. `IBuffable` 仍依赖 Buff 实现，后续 Combat/Runtime 拆分时需要设计 `IBuffConfiguration` 或稳定 Buff Contract。
5. namespace 仍保留在原 `Zephyr.Core.*` 位置，这是为了降低本阶段风险；namespace 清理应在程序集稳定后单独执行。

### 回滚方式

- 本阶段没有手写 Unity YAML。
- 所有被迁移脚本的原 `.meta` 文件随脚本移动，GUID 保持不变。
- 如需回滚，可恢复脚本原目录、移除 Contracts asmdef 和各 asmdef 的 Contracts 引用；不需要重建场景/Prefab 引用。
- 不使用 `git reset --hard` 或宽范围清理，以保留本仓库已有的用户修改。

## 十、下一阶段建议

下一步不建议直接删除 `Zephyr.Core` 引用。建议按以下顺序继续：

1. 建立 `Zephyr.Core.Runtime`，先迁移 Timer、ObjectPool、Logging 和 EventBus 实现。
2. 建立 `Zephyr.Core.Combat`，迁移 Stats、Damage、Health、Buff 实现，保持 Contracts 只承载契约和载荷。
3. 建立 `Zephyr.Core.Persistence`，先定义 Save/Session gateway，再替换具体单例读取。
4. 最后收窄 `Zephyr.Core.Flow`，把 GameFlow 和 SceneLoader 从 Save、UI、Meta 具体实现中解耦。
5. 每个子阶段都必须单独编译、跑架构门禁，并记录旧 Core 引用数量变化。

## 十一、验收评分

| 维度 | 阶段 A/B 前 | 阶段 A/B 后 | 说明 |
|---|---:|---:|---|
| 边界清晰度 | 9.1 | **9.3** | 稳定契约获得独立程序集，避免继续和实现混编 |
| 依赖方向 | 9.0 | **9.2** | Contracts 不反向依赖 Core，消费者显式声明依赖 |
| 生命周期与单例安全 | 9.0 | **9.0** | 本阶段不触碰生命周期和单例实现 |
| 可测试性与门禁 | 9.1 | **9.2** | 新增 Contracts 程序集和依赖方向门禁 |
| 迁移完整度与整洁度 | 8.8 | **8.9** | 完成第一条安全迁移切片，但旧 Core 仍存在 |
| **阶段 A/B 加权评分** | - | **9.2** | 本阶段目标完成度高 |

**最终判定：阶段 A/B 验收成功。Contracts 边界已建立，Unity 编译和 Console 验证通过；完整 Core Split 仍需按 Runtime → Combat → Persistence → Flow 继续实施。**

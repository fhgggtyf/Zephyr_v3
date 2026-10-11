# Zephyr 第二阶段架构修理验收报告

> 日期：2026-10-11  
> 项目：`D:\GitHub\Zephyr_v3\Zephyr(AI)\Zephyr`  
> 验收范围：架构第二阶段“切开最明显的边界”  
> 评估重点：领域程序集边界、依赖方向、Levels/Interaction 的跨域耦合与 Editor 工具隔离  
> 非本阶段范围：PlayerDeathController/MainMenuController/GameFlow 的职责拆分、全局单例移除、全部运行时 Find 替换

## 一、验收结论

**第二阶段验收成功。**

本阶段完成度评定为 **高（约 90%）**。审查记录要求的核心目标已经落地：Enemies、Rooms、Weapons、Projectiles、Items 已有独立编译边界；Gameplay/Levels 和 Gameplay/Interaction 已通过 Core 接口减少对具体领域类型的依赖；新增边界能够被 Unity 编译器和静态架构检查识别。

本阶段仍保留两组显式迁移依赖：`Zephyr.Items -> Zephyr.Weapons`，以及 `Zephyr.Enemies -> Zephyr.Items/Zephyr.Weapons`。这些依赖已经从原来的 `Assembly-CSharp` 隐式耦合变成 asmdef 中可见、可审查的依赖，后续可在不重新扩大 Core 的前提下继续收敛。因此它们是已记录的迁移桥接，不构成本阶段验收阻塞。

这次验收成功指的是**第二阶段目标完成度很高**，不代表整个架构审查中列出的第三、第四阶段已经完成。

## 二、修理内容

### 2.1 新增领域程序集

本阶段在第一阶段 13 个 asmdef 的基础上新增 6 个，当前仓库共 **19 个 asmdef**：

| 程序集 | 当前依赖 | 负责范围 |
|---|---|---|
| `Zephyr.Weapons` | `Zephyr.Core` | `Core/Weapons` 下的武器数据、运行时、攻击组件 |
| `Zephyr.Projectiles` | `Zephyr.Core` | `Core/Projectiles` 下的投射物与投射物系统 |
| `Zephyr.Items` | `Zephyr.Core`、`Zephyr.Weapons` | `Core/Items` 下的拾取物、掉落表与掉落系统 |
| `Zephyr.Enemies` | `Zephyr.Core`、`Zephyr.Core.StateMachine`、`Zephyr.Items`、`Zephyr.Weapons` | `Gameplay/Enemies` 下的敌人运行时、SO、攻击组件和 Boss 行为 |
| `Zephyr.Rooms` | `Zephyr.Core` | `Gameplay/Levels` 下的场景进度与房间用例 |
| `Zephyr.Enemies.Editor` | `Zephyr.Enemies`，仅 Editor | `EnemySOEditor` 等敌人专属 Editor 工具 |

原有 `Core/Weapons/Editor` 也已改为引用 `Zephyr.Weapons`，不再把武器 Editor 工具挂在过宽的 `Zephyr.Core` 上。所有新边界都保留原有脚本资产路径和 `.meta` GUID，避免 Unity 序列化引用漂移。

### 2.2 Enemies 从 Assembly-CSharp 迁入独立边界

`Gameplay/Enemies` 已由目录约定升级为 `Zephyr.Enemies` 编译边界。该程序集明确声明了对 Core、状态机、Items 和 Weapons 的依赖，编译器现在可以直接暴露未来的跨域引用，而不是让所有脚本继续落入 `Assembly-CSharp`。

Enemy 运行时的状态机基础设施没有被修改，仍复用锁定的 `Zephyr.Core.StateMachine`。`EnemyBrain` 只新增实现 Core 层的 `ILevelObjective` 合同，用于向房间/关卡暴露“目标是否完成”，没有让 Levels 反向引用 `EnemyBrain` 具体类型。

### 2.3 Levels 改为依赖 Core 合同

新增 `Zephyr.Core.Interfaces.ILevelObjective`，并由 `EnemyBrain` 实现。`TutorialObjectiveExit` 的序列化字段由 `EnemyBrain[]` 改为 `MonoBehaviour[]`，运行时通过 `ILevelObjective` 判断目标完成状态。

结果：

- `Zephyr.Rooms` 只依赖 `Zephyr.Core`。
- `Gameplay/Levels` 不再 `using Zephyr.Gameplay.Enemies`。
- 房间出口不再直接知道敌人类型，未来可以复用同一合同接入机关、Boss 阶段或其他关卡目标。
- 现有 Inspector 中保存的脚本引用仍通过原始 MonoBehaviour GUID 工作，不需要重写场景或 Prefab。

### 2.4 Interaction 改为依赖 Core 合同

新增 `Zephyr.Core.Interfaces.IGustJumpReceiver`，由 `PlayerGustJumpController` 实现。`GustInteractable` 不再引用 `PlayerGustJumpController` 具体类型，而是通过稳定接口查找并提交 Gust 请求。

同时移除了 `Zephyr.Gameplay.Interaction` 对 `Zephyr.Player` 的程序集引用。该改动保留了现有交互行为，但把 Interaction 与 Player 的编译期耦合降为 Core 合同依赖，符合“场景用例依赖端口，不依赖具体 Player 类”的第二阶段目标。

### 2.5 架构基线同步

已更新 `Documents/Architecture/Zephyr 底层架构详细设计（草案）.md`：

- asmdef 基线更新为 19 个。
- 记录 Enemies/Rooms/Weapons/Projectiles/Items 的实际程序集拓扑。
- 明确 Items→Weapons 和 Enemies→Items/Weapons 是当前迁移桥接，而不是目标架构的最终跨域状态。
- 明确 Rooms 只依赖 Core，Levels/Interaction 不再依赖具体 Enemy/Player 类型。

## 三、独立架构复评

### 3.1 本阶段已经明显改善的方面

- **编译边界可见**：敌人、房间、武器、投射物和物品不再全部依赖 `Assembly-CSharp` 的默认归属。
- **依赖方向更清晰**：Weapons、Projectiles、Rooms 只依赖 Core；Items 与 Enemies 的额外依赖已经写入 asmdef，能够被审查和迁移。
- **Levels 不再依赖 Enemy 具体类**：关卡目标使用 Core 合同，降低领域反向引用。
- **Interaction 不再依赖 Player 具体类**：Gust 交互通过 Core 接口提交请求。
- **Editor 工具边界完整**：敌人和武器的 Editor 工具都有明确的 Editor-only asmdef。
- **资产兼容风险受控**：没有移动脚本资产路径，没有手写场景/Prefab YAML，没有生成重复 GUID。

### 3.2 当前仍然存在的结构性问题

- `Zephyr.Core` 仍然过大，流程、存档、Meta、相机、音频、UI 和基础服务尚未继续拆分。
- `Zephyr.Enemies` 仍直接依赖 Items 和 Weapons，主要原因是 `EnemySO` 的 DropTable 数据与 `BossRewardDrop` 的武器掉落仍位于敌人模块边界内；这是下一轮可通过 Reward/Drop contract 收敛的迁移桥。
- `Zephyr.Items` 仍直接依赖 Weapons，因为 `WeaponPickup` 和 `DropSystem` 使用 `WeaponSO`/`WeaponSet`；后续可将掉落结果抽象成 Core 数据合同。
- `EnemyBrain` 仍在目标丢失时使用 `GameObject.FindGameObjectWithTag("Player")`，属于审查报告第四阶段的运行时查找债务，本阶段没有扩大它，但也没有消除它。
- 全局单例和高耦合控制器仍存在，属于第三、第四阶段工作。

### 3.3 阶段性评分

| 复评项 | 结果 |
|---|---|
| 五个目标领域是否获得独立 asmdef | 是 |
| Enemies 是否离开默认 `Assembly-CSharp` 边界 | 是 |
| Rooms 是否反向依赖具体 Enemies 类型 | 否，已改为 Core 合同 |
| Interaction 是否反向依赖具体 Player 类型 | 否，已改为 Core 合同 |
| Editor 工具是否有明确 Editor-only 归属 | 是 |
| 是否已经消除全部跨域耦合 | 否，保留了两组显式迁移桥 |

综合判断：第二阶段的核心验收标准已经满足，剩余问题已被收敛为可见、可计划的下一阶段依赖，而不是继续隐藏在 `Assembly-CSharp` 或万能 Core 中。因此达到“完成度很高”的条件，判定为**验收成功**。

## 四、验证证据

### 4.1 Unity 编译与 Console

- Unity 项目根目录确认：`D:\GitHub\Zephyr_v3\Zephyr(AI)\Zephyr`。
- Unity `Library/ScriptAssemblies` 中已生成：`Zephyr.Enemies.dll`、`Zephyr.Rooms.dll`、`Zephyr.Weapons.dll`、`Zephyr.Projectiles.dll`、`Zephyr.Items.dll`。
- 最终 `Unity_GetConsoleLogs`：**0 Error、0 Warning**。
- 修改后的 EnemyBrain、GustInteractable、TutorialObjectiveExit、PlayerGustJumpController、接口和架构测试脚本均通过脚本验证；架构测试脚本无诊断错误。

### 4.2 EditMode 测试

最近一次 EditMode Test Job：`9a6091862d0e440990adddab071de3d5`。

| 指标 | 结果 |
|---|---:|
| 总数 | 4 |
| 通过 | 4 |
| 失败 | 0 |
| 跳过 | 0 |

通过项包括 Unity 内置文档示例测试、Runtime/Editor 依赖架构测试、StateTransition 测试和 StatsCore 测试。

第二阶段的程序集方向与 Levels/Interaction 反向依赖断言已合并进架构测试源，并已编译进入 `Zephyr.Architecture.Tests.dll`；当前 Unity Test Runner 的发现缓存仍只列出原有测试项，因此这两个新增断言的独立结果以脚本验证、asmdef JSON 静态检查和 DLL 编译结果作为补充证据，没有把它们虚报为本次 4 项运行结果。

### 4.3 残余静态告警

`EnemyBrain.cs` 的脚本验证仍提示既有风险：`Update()` 中的 `GameObject.Find`，以及该路径上的字符串拼接。它们没有造成 Console Error，也不是本阶段程序集边界修理的阻塞项，但应在第四阶段目标查找治理中处理。

### 4.4 场景截图说明

本阶段没有修改场景、Prefab、场景对象或序列化引用，因此不需要执行 SceneView 截图验收，也没有生成截图文件。

## 五、关键变更清单

- `Zephyr(AI)/Zephyr/Assets/Scripts/Core/Weapons/Zephyr.Weapons.asmdef`
- `Zephyr(AI)/Zephyr/Assets/Scripts/Core/Projectiles/Zephyr.Projectiles.asmdef`
- `Zephyr(AI)/Zephyr/Assets/Scripts/Core/Items/Zephyr.Items.asmdef`
- `Zephyr(AI)/Zephyr/Assets/Scripts/Gameplay/Enemies/Zephyr.Enemies.asmdef`
- `Zephyr(AI)/Zephyr/Assets/Scripts/Gameplay/Enemies/Editor/Zephyr.Enemies.Editor.asmdef`
- `Zephyr(AI)/Zephyr/Assets/Scripts/Gameplay/Levels/Zephyr.Rooms.asmdef`
- `Zephyr(AI)/Zephyr/Assets/Scripts/Core/Interfaces/ILevelObjective.cs`
- `Zephyr(AI)/Zephyr/Assets/Scripts/Core/Interfaces/IGustJumpReceiver.cs`
- `Zephyr(AI)/Zephyr/Assets/Scripts/Gameplay/Enemies/EnemyBrain.cs`
- `Zephyr(AI)/Zephyr/Assets/Scripts/Gameplay/Levels/TutorialObjectiveExit.cs`
- `Zephyr(AI)/Zephyr/Assets/Scripts/Gameplay/Interaction/GustInteractable.cs`
- `Zephyr(AI)/Zephyr/Assets/Scripts/Player/Interaction/PlayerGustJumpController.cs`
- `Zephyr(AI)/Zephyr/Assets/Scripts/Tests/Architecture/RuntimeEditorDependencyTests.cs`
- `Documents/Architecture/Zephyr 底层架构详细设计（草案）.md`

## 六、第三阶段建议

1. 优先拆分 `PlayerDeathController`，将死亡用例、Potential 扣除、表现和 Respawn 协调分开。
2. 再拆分 `MainMenuController` 的 View、Save Slot、Settings 和 Use Case 职责。
3. 收窄 `GameFlow` 与 `SceneLoader`，让场景加载器只负责场景生命周期，不直接知道 Meta/Player/Room 业务。
4. 继续处理两个迁移桥：将 Enemy reward/drop 数据改为 Core contract，将 WeaponPickup 的武器引用依赖收敛为掉落结果端口。
5. 将 EnemyBrain 的 Player 查找替换为目标注册表或显式初始化绑定。

**最终状态：第二阶段架构修理验收成功；当前风险已从隐式默认程序集耦合转为显式、可追踪的后续迁移项。**

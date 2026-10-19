# Zephyr 当前实现架构独立复评报告

> 日期：2026-10-13；项目：`D:\GitHub\Zephyr_v3\Zephyr(AI)\Zephyr`  
> 范围：仅评估模块边界、依赖、耦合、生命周期与架构整洁度。

## 1. 独立结论

**整体评分：8.7 / 10；当前不能在全面且一致的标准下认定达到 9.0+。** 本轮从当前代码、asmdef、受控调用清单和测试门禁重新核对，不按历史阶段完成数加分，也不把测试通过等同于架构已经干净。

基础架构可靠：生产程序集依赖无环，Contracts/Runtime 边界明确，旧 Core 不再是 asmdef 依赖枢纽，领域迁移桥和架构门禁均已落地。阻止 9.0+ 的主要因素是上层模块扇出仍宽、业务服务解析尚未完全归位到 owner/context，以及部分编排器职责较重。普通功能开发可继续，不需要推倒重来；架构专项债务仍在。

## 2. 固定评分标准

| 维度 | 权重 | 判断内容 |
|---|---:|---|
| 模块边界与程序集依赖方向 | 25% | asmdef 覆盖、依赖层次、循环、旧宿主是否为枢纽 |
| 生命周期与组合根 | 20% | Persistent/GameManager/Scene/Room/Actor owner 和服务组装位置 |
| Contracts 与数据边界 | 15% | 跨域接口、配置/运行时/存档状态归属 |
| 跨模块耦合 | 15% | 直接引用扇出、实现依赖、Gateway/Context 使用 |
| 全局状态与隐式查找 | 10% | Singleton、场景扫描、直接场景 API 的范围和治理 |
| 实现职责与整洁度 | 10% | 编排器职责、模块内部 API 收敛度 |
| 验证与维护门禁 | 5% | 架构测试、预算门禁、EditMode/PlayMode 证据 |

9.0+ 门槛：依赖无环；边界、生命周期/组合根、Contracts、耦合四项均不低于 9.0；架构债务有 owner 且持续收敛；关键运行链有相称测试。程序集数量和测试通过率不能单独抬高总分。

## 3. 模块清单与耦合

当前共 30 个 asmdef：22 个非 Editor/测试程序集、5 个 Editor、3 个测试；生产/运行时代码程序集间有 70 条直接依赖边。耦合等级：低表示依赖窄且方向稳定；中表示边界明确但依赖较多；高表示扇出宽或具体服务/生命周期耦合显著。

| 模块 | 程序集/职责 | 依赖与耦合判断 | 耦合 | 分数 |
|---|---|---|---|---:|
| Contracts | `Zephyr.Core.Contracts`；接口、事件载荷、值类型 | 17 个生产消费者，自身零项目依赖；共享底座合理，需防契约/旧 namespace 膨胀。 | 低 | 9.4 |
| Runtime | `Zephyr.Core.Runtime`；事件、计时、对象池、生命周期设施 | 仅依赖 Contracts，被 19 个生产程序集消费；扇出高但单向。 | 低 | 9.2 |
| StateMachine | `Zephyr.Core.StateMachine`；共享状态机 | Player/Enemies 共用，Editor 扩展分离，边界清楚。 | 低 | 9.1 |
| Input | `Zephyr.Inputs`；输入动作契约 | 只依赖 Input System，由 Player/Dialogue 消费。 | 低 | 8.9 |
| Flow/Bootstrap | `Zephyr.Core.Flow`；GameFlow、SceneLoader、启动、GameManager 组合 | 引用 Contracts/Runtime/Save/Meta/Localization/UI；组合责任合法但偏宽，direct-open 和 Bootstrap 单例解析残留。 | 高 | 8.3 |
| Persistence | `Zephyr.Core.Persistence`；Save Gateway、DTO、存档读写 | 多个上层消费者；IO、DTO 转换、slot/use-case 策略仍较集中。 | 中 | 8.5 |
| Meta | `Zephyr.Core.Meta`；升级、解锁、故事进度 | 与 Flow/Persistence/Dialogue/Interaction/Player 相连；部分路径仍解析 Save/EventBus/Story 单例。 | 中高 | 8.4 |
| Combat/Stats | `Zephyr.Core.Combat`；伤害、生命、属性、Buff | 多个玩法程序集共享此下层，方向合理；Stats/Buff/Health 策略较厚。 | 中 | 8.7 |
| Camera | `Zephyr.Core.Camera`；跟随、边界、区域、Cinemachine | 外部依赖少；内部有场景扫描、反射兼容及集中绑定逻辑。 | 中 | 8.3 |
| Audio | `Zephyr.Core.Audio`；音频服务、播放池 | 只依赖 Runtime；生产 asmdef 图无直接消费者，需确认实际消费契约。 | 低 | 8.8 |
| Player | `Zephyr.Player`；输入、移动、战斗、死亡、生成、交互、表现 | 直接引用 14 个生产程序集，最大扇出；不应同时成为多个服务模块的集成枢纽。 | 高 | 8.0 |
| Enemies/Boss | `Zephyr.Enemies`；AI、移动、攻击、Boss | 依赖 Contracts/Runtime/Combat/StateMachine；未反向依赖 Items/Weapons，但 EnemyBrain 仍读全局 EventBus。 | 中 | 8.5 |
| Weapons | `Zephyr.Weapons`；配置、运行时、控制器、拾取 | 迁移桥已清理；装备/攻击与 Animator 协作职责仍宽。 | 中 | 8.5 |
| Items/Drops | `Zephyr.Items`；货币、掉落、拾取 | 池化和 Pickup Runtime Context 已建立，边界总体合理。 | 中低 | 8.7 |
| Projectiles | `Zephyr.Projectiles`；投射物和命中 | 依赖窄；TimerService 仍有 Singleton 读取。 | 低中 | 8.7 |
| Rooms/Levels | `Zephyr.Rooms`；房间波次、目标、场景推进 | 依赖 Contracts/Runtime/Flow；RoomController 仍解析多个 Persistent 服务。 | 中高 | 8.4 |
| Interaction | `Zephyr.Gameplay.Interaction`；MetaHub、门、互动、入口和相关 UI | 直接引用 7 个生产程序集，横跨 Flow/Meta/Save/Localization/UI；Gateway/context 改善但职责仍宽。 | 高 | 8.0 |
| Dialogue | `Zephyr.Core.Dialogue`；对话、触发器、视图协调 | 直接引用 8 个生产程序集；虽无循环，跨 Flow/Meta/Save/Localization/UI/Inputs 面过宽，仍有 EventSystem 扫描 fallback。 | 高 | 8.2 |
| UI Presentation | `Zephyr.Core.UI`、`Zephyr.UI`；UI 基础、HUD、表现 | Core.UI 轻量；Zephyr.UI 引用 Combat/Localization/Runtime/Contracts。Controller/View 已分层，需防玩法策略渗入视图。 | 中 | 8.6 |
| Testing/Editor | 测试程序集、Editor 工具 | 编译边界独立；测试引用宽属集成测试需求，不算生产耦合。 | 低 | 9.2 |

## 4. 当前证据

- `ArchitecturePolicy.json` 登记 54 个受控调用：Singleton 43、SceneScan 7、SceneLoad 4。预算匹配说明门禁生效，不代表调用已从实现中消失。
- asmdef 扇出热点：Player 14、Dialogue 8、Interaction 7、Flow 6 个生产程序集。
- 普通场景加载基本集中于 SceneLoader；`ColdStartupBootstrap` 保留 additive `SceneManager.LoadSceneAsync` 启动/direct-open 路径，应限定为例外。
- Singleton 调用包括合法 Bootstrap/组合边界，也包括 RoomController、EnemyBrain、Projectile、Buff、Meta、Localization 等消费者；风险不同，但服务注入尚未完全收敛。
- 本轮 Console 查询：0 Error、0 Warning，另有 9 条 Info。最近测试记录为 Architecture + EditMode 59/59、Vertical Slice PlayMode 1/1；本轮未重跑测试。
- 旧 Final 报告把 30 个 asmdef 写作“Runtime assemblies”；准确分类应为 22 个非 Editor/测试程序集、5 Editor、3 测试。

## 5. 加权评分

| 维度 | 权重 | 分数 | 当前判断 |
|---|---:|---:|---|
| 模块边界与依赖方向 | 25% | 9.3 | 生产图无环、Contracts/Runtime 分层、旧 Core 不再为依赖枢纽；上层引用面仍宽。 |
| 生命周期与组合根 | 20% | 8.4 | Owner 与 Context/Gateway 已建立；Room、Bootstrap、MainMenu/Tutorial 仍有 Singleton/direct-open 服务解析。 |
| Contracts 与数据边界 | 15% | 9.0 | 跨域入口多已合同化；存档模型和共享契约语义可继续收紧。 |
| 跨模块耦合 | 15% | 8.4 | 无循环，但 Player 14、Dialogue 8、Interaction 7、Flow 6 个直接生产引用。 |
| 全局状态与隐式查找 | 10% | 8.2 | 有可执行门禁；仍有 43 个 Singleton 和 7 个 SceneScan。 |
| 实现职责与整洁度 | 10% | 8.3 | GameFlow、RoomController、CameraController、Dialogue 和 Player 集成层仍多职责集中。 |
| 验证与维护门禁 | 5% | 9.3 | 架构扫描、近期 59/59 与 1/1 测试证据，Console 当前干净。 |
| **总分** | **100%** | **8.7 / 10** | **未四舍五入为 8.73。** |

```text
9.3×0.25 + 8.4×0.20 + 9.0×0.15 + 8.4×0.15
+ 8.2×0.10 + 8.3×0.10 + 9.3×0.05 = 8.73 ≈ 8.7
```

生命周期与耦合未达 9.0 硬门槛，因此不能用测试分或程序集分把整体判作 9.0+。

## 6. 与此前 9.0 报告的区别

2026-10-13 的 `Final-Architecture-9-Plus-Cleanup` 使用 5 项评分，偏重当次收尾改动和预算匹配；本报告以七维模型覆盖整个游戏实现，分别核查生命周期、耦合、隐式依赖和职责集中。两者不是同一量尺下的重复评分。此处保留测试通过证据，但不把门禁通过作为实现已经洁净的替代证据。

## 7. 优先改进项

1. 收窄 Player、Dialogue、Interaction 的跨域引用；优先使用窄 Gateway、事件载荷或组合根，不机械增设程序集。
2. 将 RoomController 的 Save/EventBus/Pool/Timer 解析移到明确 owner/context，再治理 Meta、Enemy、Projectile、Buff 非组合边界 Singleton；Bootstrap 例外单独计量。
3. 收敛 Dialogue EventSystem、GridAligner 扫描；保留基础设施必需的查找并为例外定义 owner。
4. 按变化轴拆 GameFlow、CameraController、DialogueManager、RoomController 内部协作者，保持序列化入口及生命周期协议稳定。
5. 让门禁追踪业务 Singleton/SceneScan 债务趋势，而不只保证静态预算不增长。

## 8. 最终判断与验证边界

**当前 8.7/10，尚未达到 9.0+。** 普通功能开发可以继续；上述事项应作为架构专项小步推进。

本轮未修改游戏代码、Prefab、场景或 SO。结构判断基于当前 asmdef、`ArchitecturePolicy.json`、关键实现和测试源码。测试结果引用本轮开始前刚通过的记录（59/59、1/1），本轮没有重跑；Console 为本轮重新查询结果。此结论不声称所有关卡和战斗流程均有 PlayMode 覆盖。

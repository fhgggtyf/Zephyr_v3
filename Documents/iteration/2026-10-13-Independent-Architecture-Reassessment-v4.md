# Zephyr 独立架构复核报告

**复核日期：** 2026-10-13  
**复核范围：** `D:\GitHub\Zephyr_v3\Zephyr(AI)\Zephyr\Assets\Scripts` 运行时代码、程序集边界、运行时依赖、场景生命周期、配置/运行时状态边界和现有架构门禁。  
**复核目的：** 在不因为此前已经完成的修复而自动加分的前提下，重新判断当前 implementation 是否达到 `9.0+`。

## 1. 结论

**当前客观评分：9.2 / 10.0。结论：达到 9.0+，可以继续普通玩法和内容开发。**

这个结论不是“所有架构债务已经清零”，而是：

- 领域代码已经基本脱离旧 Core 宿主、具体单例和全局扫描。
- 依赖图无循环，程序集边界是显式的，运行时调用台账可执行验证。
- Persistent、GameManager、Initializer 和 SceneLoader 的集中依赖符合组合根/基础设施边界，未扩散到普通 Actor、Pickup、View 或领域逻辑。
- 剩余扣分来自 `Player`、`Core.Dialogue`、`Gameplay.Interaction` 的高扇出，以及 `LocalizedBehaviour` 仍在一个 UI 基类中解析 `LocalizationManager.Instance`。这些是可识别、可局部收敛的风险，不是未受控的架构污染。

如果评分标准要求“所有具体单例访问必须为零、所有高扇出程序集必须继续拆分”，则当前不应被称为满分；按本项目已经明确的组合根规则，这些集中点属于受审查例外，因此 `9.2` 是更准确的判断。

## 2. 复核证据

### 2.1 Unity 和测试结果

- Unity 项目根目录确认：`D:\GitHub\Zephyr_v3\Zephyr(AI)\Zephyr`
- Unity 版本：`6000.5.6f1`
- 架构 + EditMode：`59 passed, 0 failed, 0 skipped`
- PlayMode 垂直切片：`1 passed, 0 failed, 0 skipped`
- 最后一轮 `Unity_GetConsoleLogs`：`0 error, 0 warning`
- PlayMode 测试结果文件：`C:\Users\admin\AppData\LocalLow\DefaultCompany\Zephyr\TestResults.xml`
- 测试输出中存在两条“没有 Audio Listener”的运行时提示，但未形成 Console warning/error，也未阻断本次架构判定；它仍是一个运行时场景配置残余风险。

### 2.2 程序集图

对非 Editor、非测试运行时程序集重新解析后得到：

- `22` 个运行时程序集
- `77` 条显式程序集依赖边
- 没有检测到循环依赖
- 最高扇出：`Zephyr.Player = 15`
- 次高扇出：`Zephyr.Core.Dialogue = 10`
- `Zephyr.Gameplay.Interaction = 8`
- `Zephyr.Core.Flow = 6`
- 最高扇入：`Zephyr.Core.Runtime = 16`
- `Zephyr.Core.Contracts = 14`
- `Zephyr.Core.Combat` 和 `Zephyr.Core.Persistence` 各为 `7`

依赖集中在 Contracts/Runtime/Combat/Persistence 等底层能力以及 Player/Dialogue/Interaction/Flow 等编排边界，没有出现领域模块之间的明显反向环。

### 2.3 受控运行时调用

`Assets/Scripts/Tests/Architecture/ArchitecturePolicy.json` 当前登记 `30` 个受控调用：

- `26` 个单例访问
- `4` 个 SceneManager 加载/卸载调用
- 单例访问集中在 `ColdStartupBootstrap`、`InitializerBootstrap`、`PersistentBootstrap`、`GameManagerBootstrap`、`GameFlow` 和 `LocalizedBehaviour`
- 场景加载集中在 `InitializerBootstrap`、`ColdStartupBootstrap` 和 `SceneLoader`
- 运行时代码扫描结果：`Resources.Load = 0`、`GameObject.Find = 0`、`FindObjectOfType = 0`、`FindAnyObjectByType = 0`、`FindObjectsOfType = 0`

这说明直接入口仍存在，但已经从普通业务代码收缩到被策略台账审查的启动、持久化、场景编排和本地化边界。
## 3. 模块拆分与逐项评价

评分中的“低/中/高耦合”表示模块对外依赖和变更传播风险，不表示代码不能工作。

| 模块 | 主要职责 | 耦合度 | 架构评价 |
|---|---|---:|---|
| `Core.Contracts` | 跨模块接口、值类型、事件和战斗数据契约 | 低 | 依赖为空，方向正确，是稳定公共边界。当前规模较大，但主要是契约聚合，不是实现聚合。 |
| `Core.Runtime` | 对象池、计时、掉落、通用运行时服务 | 中 | 只依赖 Contracts，扇入高但符合基础设施角色。需要继续防止加入业务规则。 |
| `Core.Combat` | 伤害、生命、属性、Buff、战斗计算 | 中 | 依赖 Contracts/Runtime，消费方向清晰。Stats 与 Meta 的接缝仍需保持单向。 |
| `Core.Persistence` | Save、RunData、MetaData、存档 Gateway | 中 | 依赖 Runtime，已通过 Gateway 向上提供能力。风险在于任何新功能都可能把它当作全局状态仓库。 |
| `Core.Localization` | 本地化服务和 UI 适配基类 | 中 | 运行时依赖窄；`LocalizedBehaviour` 保留一个集中式 singleton adapter，是当前明确扣分点。 |
| `Core.Meta` | Meta 升级、故事事实、进度 Gateway | 中 | 依赖 Contracts/Runtime/Persistence，数据边界合理；不应继续向 Player 或 UI 反向扩散。 |
| `Core.Flow` | Bootstrap、GameFlow、SceneLoader、场景控制器和组合根 | 高但合理 | 是系统编排中心，依赖多个上层服务；高耦合是职责结果。风险是继续把领域逻辑塞入 Flow。 |
| `Core.Camera` | 相机跟随、边界、区域和震动 | 低 | 当前独立，不引用旧 Core；注册式边界比全局场景扫描干净。 |
| `Core.Audio` | 音频播放/控制 | 低 | 仅依赖 Runtime，边界清晰。PlayMode 的 Audio Listener 提示属于场景配置风险，不是程序集耦合问题。 |
| `Core.StateMachine` | 通用状态机基础设施和 SO 抽象 | 低 | 无运行时程序集依赖，作为锁定基础设施是合理的。 |
| `Core.UI` | UI 基础契约和通用 UI 能力 | 低 | 独立基础层，未反向依赖领域实现。 |
| `Core.Dialogue` | Dialogue SO、Manager、View、输入和触发桥接 | 高 | 目前扇出 10，职责横跨 UI、输入、Flow、Meta、Persistence 和 Localization；仍可工作，但这是最明显的下一步拆分候选。 |
| `Core.Weapons` | Weapon SO、Runtime、组件和攻击实现 | 中 | 依赖 Contracts/Runtime/Combat/Persistence，方向合理；不应被 Player 直接当作全局武器服务。 |
| `Core.Items` | Item/Pickup 运行时入口 | 中 | 依赖窄，已通过 Drop/Pool/Context 接入；生命周期复杂度主要来自 Prefab 和对象池。 |
| `Core.Projectiles` | 投射物与命中实现 | 中 | 依赖 Contracts/Runtime/Combat，边界单向，未出现对 Player/Enemies 的反向引用。 |
| `Gameplay.Enemies` | 敌人 Brain、StateAction/Condition、Boss 和掉落桥 | 中 | 依赖 Combat/StateMachine/Runtime/Contracts，已通过 Room/Drop Gateway 解耦；敌人不直接持有 WeaponSO。 |
| `Gameplay.Interaction` | 场景交互物、Meta/Run/Gust/Door 和 UI 面板 | 高 | 扇出 8，混合了场景交互、Meta、Localization、UI 和 Flow；当前边界可用，但后续新增交互要避免继续成为杂物层。 |
| `Rooms/Gameplay.Levels` | 房间、场景目标、摄像机/进度注册 | 中 | 依赖 Contracts/Runtime/Flow，生命周期上下文已经显式化。应继续保持“房间发请求，Flow 负责加载”。 |
| `Player` | 玩家核心、输入、状态机、武器、相机、Meta、UI、Dialogue 接入 | 高 | 扇出 15，是全项目最高；部分是玩家作为组合实体的自然耦合，部分应继续通过 Player 专属 Gateway/Facade 收窄。 |
| `Inputs` | Input System 生成输入封装 | 低 | 只依赖 Unity InputSystem，边界干净。 |
| `Zephyr.UI` | 统计面板等项目 UI | 中 | 依赖 Contracts/Runtime/Combat/Localization，职责较窄；与 `Core.UI` 的分层要保持“基础契约 vs 项目视图”的区分。 |

## 4. 架构维度评分

评分采用 100 分制，再换算为 10 分制。每项都以源码、asmdef、策略台账和测试证据为依据。

| 维度 | 权重 | 得分 | 说明 |
|---|---:|---:|---|
| 模块边界与职责所有权 | 25 | 23.0 | Contracts、Runtime、Combat、Persistence、Camera、StateMachine 已形成稳定分层；Player/Dialogue/Interaction 仍承担较多跨域编排。 |
| 依赖方向与循环控制 | 20 | 18.5 | 22 个运行时程序集、77 条边，无循环；扣分来自高扇出和 Flow/Dialogue 的上层聚合密度。 |
| 运行时依赖注入与单例收敛 | 20 | 17.5 | 领域代码已基本改为 Gateway/Context/Consumer；30 个受控入口仍存在，`LocalizedBehaviour` 是唯一明显非纯组合根适配点。 |
| 生命周期、场景和资源所有权 | 15 | 14.0 | Persistent/GameManager/Initializer/SceneLoader 分工清楚，池和 Pickup 上下文明确；PlayMode 暴露 Audio Listener 配置残余。 |
| 数据、UI、本地化和视图边界 | 10 | 8.8 | SO/运行时状态基本分离，本地化和 UI 边界已建立；Dialogue 与 Interaction 仍有跨职责密集连接。 |
| 可验证性与反回归能力 | 10 | 10.0 | 59 个架构/EditMode 测试通过，策略台账、非法调用扫描和 1 个垂直切片通过，门禁已可执行。 |
| **总计** | **100** | **91.8** | **四舍五入为 9.2 / 10.0** |

## 5. 当前剩余扣分与影响

### 5.1 `Player` 扇出过高

`Zephyr.Player` 同时接入 Combat、Persistence、Meta、Flow、Camera、StateMachine、Localization、Dialogue、Inputs、UI、Weapons 和 InputSystem。玩家确实是跨系统实体，因此不能简单按“引用多 = 错误”处理；但这会让玩家功能的修改容易扩大编译和回归范围。

**影响：** 中等。主要是可维护性和变更传播风险，不是当前运行时失控。  
**建议：** 后续以 `PlayerRuntimeContext`、`IPlayerCombatGateway`、`IPlayerProgressionGateway`、`IPlayerPresentationGateway` 等已有模式为基础继续分面，禁止再向 Player 直接添加新服务引用。

### 5.2 `Core.Dialogue` 扇出过高

Dialogue 当前同时承担数据、输入、视图触发、Flow、Meta、Persistence 和 Localization 接入。它是可运行的，但在架构上更像“Dialogue feature slice + integration adapter”的合体。

**影响：** 中等偏高。Dialogue 规则、UI 替换和输入变更容易互相触发编译。  
**建议：** 后续拆为稳定 Dialogue Contracts/Data、运行时会话、UI Adapter 和 Flow Integration；这属于独立架构专项，不应在普通对话内容任务中顺便完成。

### 5.3 `Gameplay.Interaction` 仍是跨域混合层

MetaUpgradeStation、RunStartInteractable、Gust、Door、Focus Visual 和多个 Panel 共处一个程序集，导致对 Meta、Flow、Localization、UI 的依赖较宽。

**影响：** 中等。新增交互物容易把更多系统直接拉进该程序集。  
**建议：** 保持当前 Gateway/Consumer 方向；未来可按“世界交互”和“Meta/Run UI 交互”分成两个 feature assembly，但不应在普通玩法任务中随手改 asmdef。
### 5.4 `LocalizedBehaviour` 的适配式 singleton

`Assets/Scripts/Core/Localization/LocalizedBehaviour.cs` 在 `BindLocalization` 中解析 `LocalizationManager.Instance`。它没有扩散到普通逻辑，且只解析一次并缓存接口，但严格来说仍不是纯组合根注入。

**影响：** 低到中。若未来大量 UI 继承它，启动时序和测试替身会继续依赖具体服务存在。  
**建议：** 后续为 UI/Localization 建立明确的 View binding context 或由 UI 组合根调用 `Configure(ILocalizationGateway)`，完成后删除该 adapter 例外。

### 5.5 Audio Listener 场景提示

PlayMode 测试输出记录 `There are no audio listeners in the scene`。这不是本次架构评分中的程序集耦合问题，但说明垂直切片测试场景的 Audio/Camera 场景配置仍不完全。

**影响：** 当前测试未失败；实际玩家运行时可能影响音频监听。  
**建议：** 下一次场景验收时确认 Gameplay Camera 或独立 Audio Listener 的唯一 owner，避免通过额外全局对象修补。

## 6. 是否真的达到 9.0+

**是。** 采用本报告的公开量化标准，当前是 `91.8/100 = 9.18/10`，展示时取 `9.2/10`。

达到 9.0+ 的关键不是“改动很多”，而是以下可验证条件同时成立：

1. 运行时程序集依赖无循环，且引用关系可被策略台账解析。
2. 普通业务代码中没有新增或残留全局查找、Resources 路径加载和直接场景 API。
3. 单例解析已集中到启动/组合边界，且由测试固定其数量和位置。
4. Pickup、Drop、Room、Story、Save、Dialogue、Player 等跨场景接缝已经有 Gateway/Context/Consumer，而不是靠具体实现反向引用。
5. 架构测试、EditMode 和 PlayMode 垂直切片均通过，Console 无 Error/Warning。

如果以上任一条在后续功能开发中恶化，评分应重新下降；尤其是新增 `*.Instance`、`Find*`、`SceneManager.LoadScene*`、旧 Core 引用或新的宽程序集引用时，不应以当前 `9.2` 作为豁免。

## 7. 后续建议排序

1. **优先级 P1：** 为 `LocalizedBehaviour` 引入 UI 组合根注入，删除其 singleton adapter 例外。
2. **优先级 P1：** 将 Dialogue 的数据/会话/UI/Flow 接入拆成可独立测试的边界，降低 `Core.Dialogue` 的 10 条外部引用。
3. **优先级 P2：** 为 Player 建立更窄的运行时 Facade，禁止继续增加 Player asmdef 直接引用。
4. **优先级 P2：** 将 Interaction 按世界交互与 Meta/Run UI 交互拆分，前提是先补迁移顺序和回滚点。
5. **优先级 P3：** 修复垂直切片场景的 Audio Listener owner，并补充场景级门禁。

上述工作不是开始普通功能开发的前置条件。普通敌人、武器、房间内容、掉落、关卡目标、现有 UI 内容、本地化内容和表现层功能可以继续按照 `AGENTS.md` 的现有边界开发；只有修改生命周期、Core Contract、asmdef 依赖或上述高扇出边界时，才必须先单独立项。

## 8. 最终判定

**架构状态：通过 9.0+ 门槛，建议评级 `9.2/10`。**

**可继续做：** 普通敌人、武器、掉落、房间内容、关卡目标、现有 UI 内容、本地化内容和表现层功能。  
**必须单独设计：** Dialogue 继续拆分、Player 依赖面再收窄、Interaction 程序集拆分、Localization 注入替换、Persistent/GameManager/SceneLoader 生命周期改动、任何新单例或新 asmdef 依赖。

本次报告只记录独立架构评估，没有修改游戏实现、Prefab、场景或 ScriptableObject。

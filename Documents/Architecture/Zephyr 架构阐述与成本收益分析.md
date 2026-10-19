# Zephyr 架构阐述与成本收益分析

> 文档状态：正式架构说明
>
> 基线日期：2026-10-19
>
> 项目根目录：D:\GitHub\Zephyr_v3\Zephyr(AI)\Zephyr
>
> 适用范围：运行时代码、Unity 场景与 Prefab、ScriptableObject 配置、存档、UI、本地化、测试和 AI 协作流程
>
> 关联文档：Documents/Architecture/Zephyr 底层架构详细设计（草案）.md、Documents/Architecture/Zephyr 当前实现架构独立复评报告-2026-10-12.md、Documents/GDD/Zephyr Game Design Document.md

---

## 0. 文档目的

本文不是 API 参考手册，也不是目录结构清单，而是解释 Zephyr 为什么采用当前架构、这些选择换来了什么收益、增加了什么成本，以及未来应该如何继续演进。

本文重点回答：

1. 为什么不采用单一 Assembly-CSharp、单例驱动或完全 ECS 化方案。
2. 当前各层、程序集和运行时 owner 分别解决什么问题。
3. 每项选择的直接成本、隐性成本、收益和适用边界。
4. 哪些边界是长期红线，哪些部分可以随内容生产继续调整。
5. 当前未提交工作区改动如何纳入正式架构基线，以及哪些内容仍需收敛。

本文以当前代码、asmdef、场景、Prefab、ScriptableObject 和现有设计文档为事实来源。根据当前协作约定，工作区中的未提交改动被视为当前开发线的一部分纳入基线；本文描述的是正在形成的正式架构，而不是只描述历史提交点。

本文区分三个状态：

- **已实现**：运行时类型和逻辑存在，并能进入编译范围。
- **已集成**：代码、场景、Prefab、SO、输入、UI 或引用已经完成配置。
- **待收敛**：方向正确，但依赖过宽、职责偏重、具体单例读取或运行时发现仍需治理。

---

## 1. 产品与工程背景

Zephyr 是 Unity 2D 横版动作 Roguelike / Metroidvania。核心体验由以下机制共同构成：

- 高频移动、跳跃、闪避、攻击和受击反馈。
- Primary / Secondary 双武器即时选择。
- 基于 Potential 的局内风险博弈：死亡会侵蚀特定属性潜能，Down 和 True Death 具有不同后果。
- 场景、房间、门、交互点、敌人、投射物和掉落的组合。
- Persistent、GameManager、MainMenu、Tutorial、MetaHub 和 Run 的生命周期切换。
- 中文与英文本地化、可替换 UI Prefab、三槽存档、Meta 结算和故事进度。

这些需求形成四类主要矛盾：

| 矛盾 | 忽略后的问题 | 当前架构的应对 |
|---|---|---|
| 快速试错与运行时稳定 | 新敌人或武器直接修改全局代码，功能互相污染 | SO 配置、可复用组件、领域程序集和稳定合同 |
| Unity 编辑器便利性与依赖可见性 | Inspector 很方便，但生命周期和程序集依赖难以推断 | asmdef、Contracts、显式引用、组合根和架构测试 |
| 单人开发速度与工程纪律 | 依赖靠记忆维持，长期无法判断安全修改范围 | 架构文档、红线、门禁、测试和依赖台账 |
| 动作即时性与数据可调性 | 硬编码难以平衡，完全数据化又会混淆运行时状态 | SO 保存共享配置，运行时状态 materialize 到实例 |
| 跨场景连续体验与场景自治 | 场景自行加载造成重复服务和状态丢失 | Persistent + GameManager + SceneLoader |

目标不是追求理论上最先进的架构，而是建立一个足够清晰、可验证、适合单人长期扩展，同时不阻碍 Unity 内容生产的架构。

---

## 2. 总体架构结论

### 2.1 一句话结论

Zephyr 采用 Unity 组件化运行时、ScriptableObject 数据驱动、asmdef 编译边界、Contracts 稳定协议、组合根生命周期管理、事件与 Gateway 跨模块通信，以及自动化架构门禁组成的混合架构。

这是面向中小规模单机动作游戏的务实方案：保留 Unity Inspector、Prefab、Scene 和 MonoBehaviour 的生产效率，同时使用编译期程序集、接口和测试约束跨模块依赖，避免项目退化为 Assembly-CSharp 加一个万能 Core。

### 2.2 逻辑分层

内容与表现层：Player、Enemies、Weapons、Items、Rooms、UI、Dialogue、Prefab、Scene、Animator、Localization、Audio。

领域运行时层：Combat、Meta、Persistence、Flow、Camera、Projectiles、StateMachine、Runtime、Audio、Localization。

稳定基础层：Contracts、事件载荷、值类型、Timer、Pool、Logging、基础状态机语义和少量通用运行时服务。

分层表达的是变化方向和依赖方向，而不是要求所有代码变成纯 C#：

- 内容层变化最快，不能成为其他领域的基础设施。
- 领域运行时层负责游戏规则和跨场景服务，但不能把所有具体内容塞进 Core。
- 稳定基础层变化最慢，只暴露可审查的合同和通用能力。
- Unity 的 Scene、Prefab、Animator 和 Inspector 仍是正式内容管线，不被代码抽象替代。

### 2.3 当前程序集基线

当前 Assets/Scripts 下有 30 个 asmdef，其中包括 22 个运行时程序集、5 个 Editor 程序集和 3 个测试程序集。重要边界如下：

| 程序集 | 主要职责 | 设计定位 |
|---|---|---|
| Zephyr.Core.Contracts | 接口、事件载荷、值类型、稳定枚举和协议 | 最稳定、最少依赖的公共合同 |
| Zephyr.Core.Runtime | EventBus、Timer、ObjectPool、通用运行时能力 | 可复用基础服务 |
| Zephyr.Core.StateMachine | State、Action、Condition、Transition 和 materialization | 锁定基础设施 |
| Zephyr.Core.Combat | Stats、Damage、Health、Buff 和战斗运行时 | 角色与敌人共享的规则层 |
| Zephyr.Core.Flow | Initializer、Persistent、GameManager、GameFlow、SceneLoader | 生命周期和流程组合边界 |
| Zephyr.Core.Persistence | JSON DTO、存档槽、SchemaVersion、迁移入口 | 持久化和数据边界 |
| Zephyr.Core.Meta | 结算、升级、解锁、故事进度和 Run 支撑 | Meta 规则层 |
| Zephyr.Core.Audio | 持久音频总线、音频源池和播放服务 | 跨场景音频服务 |
| Zephyr.Core.Camera | 相机跟随、Bounds、Zone 和震动锚点 | Gameplay 相机能力 |
| Zephyr.Core.Dialogue / Zephyr.UI | 对话、View、Controller、UI 绑定和本地化协作 | 可替换表现层边界 |
| Zephyr.Weapons / Zephyr.Projectiles / Zephyr.Items | 武器、投射物和物品运行时 | 可独立扩展的内容领域 |
| Zephyr.Enemies / Zephyr.Rooms / Zephyr.Gameplay.Interaction | 敌人、房间和场景交互 | 场景内容领域 |
| Zephyr.Player / Zephyr.Inputs | 玩家组合、输入适配和玩家行为 | 高变化率领域 |
| Zephyr.Tests.EditMode / Zephyr.Tests.PlayMode / Zephyr.Architecture.Tests | 单元、集成、运行时和结构门禁 | 防止架构回归 |

旧的大 Core 不再作为其他领域的公共依赖中心。当前策略是把稳定能力拆成职责明确的程序集，并允许旧兼容宿主只在已登记、可追踪的遗留位置存在。

---

## 3. 关键架构选择与原因

### 3.1 Unity 组件化，而不是纯 ECS 或纯脚本对象

**选择：** 使用 MonoBehaviour、Prefab、Scene、Animator、Collider、Rigidbody2D 和 Unity Inspector 作为运行时与内容生产基础；用普通 C# 类承担可独立测试的规则和计算。

**原因：**

1. 核心内容高度依赖 Unity 2D 物理、碰撞、Animator、Sprite、Tilemap、Prefab 和 Scene 工作流。
2. 单人开发需要直接在 Inspector 中检查引用、替换 Prefab 和调节 SO 数据。
3. Actor、Room、Projectile、Weapon Component 和 View 的生命周期天然可以映射到组件与 Prefab 层级。
4. 当前规模的主要性能热点是投射物、伤害数字和音频源，可以用对象池解决，不需要为所有对象引入完整 ECS。

**成本：** MonoBehaviour 依赖 Unity 生命周期，纯单元测试难度较高；Inspector 引用可能形成隐式依赖；Unity 序列化让重命名和字段变更带来兼容成本；缺少组合边界时组件容易读取单例、扫描场景或承担多个职责。

**收益：** 场景和 Prefab 可以直接表达最终结构；设计、程序和测试使用同一套资产；物理、动画、碰撞和 UI 与 Unity 生命周期一致；新敌人、新武器和新房间可以沿用现有内容管线。

**适用边界：** 不把所有规则写进 MonoBehaviour。伤害计算、存档 DTO、数值分析、结算规则和部分 UseCase 应尽量保持为可独立测试的 C# 逻辑；Unity 组件负责绑定、生命周期和表现协调。

### 3.2 asmdef 作为编译期模块边界

**选择：** 用多个 asmdef 表达 Contracts、Runtime、Combat、Flow、Persistence、Player、Enemies、Weapons、Rooms、UI 等依赖方向，并关闭不必要的自动引用。

**原因：** Unity 文件夹分组只能表达组织意图，不能阻止代码引用。对于持续新增敌人、武器和场景的项目，只有编译边界能在代码进入运行时前发现错误依赖。

**成本：** 初始配置和迁移时间增加；移动文件后可能出现 namespace、GUID 或引用问题；跨域 API 需要先判断应放合同、Gateway、事件还是实现程序集；编译器会暴露历史耦合，需要投入修复时间；测试程序集必须显式声明依赖。

**收益：** 领域模块不能无意中依赖另一个领域的具体实现；旧 Core、Assembly-CSharp 和高扇出模块不再自动成为依赖中心；编译器成为架构审查的一部分；内容扩展时影响范围更容易估算。

**当前状态：** 该选择已落地到 30 个 asmdef。当前主要问题不再是有没有程序集，而是部分高层程序集依赖仍较宽、部分类型仍为 public、部分业务运行时仍读取具体单例。后续重点是收窄已有边界，而不是无目的增加 asmdef。

### 3.3 Contracts 与稳定协议

**选择：** 跨程序集公开 API 优先使用接口、readonly struct、事件载荷、枚举、稳定值类型和窄 Gateway；具体 Controller、View、SaveSystem 实例和领域实现不作为公共契约扩散。

**原因：** 领域之间需要通信，但实现变化率不同。让 Enemies 直接依赖 PlayerController，或让 UI 直接依赖 SaveSystem，会把局部变化扩散为全局变化。

**成本：** 需要设计接口命名、生命周期和错误语义；小功能可能要增加 Gateway、事件载荷或适配器；过度抽象会形成“万能 Gateway”。

**收益：** 测试可以替换 fake 或 stub；领域程序集只依赖所需能力；替换存档、输入、UI 或内容实现时迁移范围更可控；依赖方向可以由 asmdef 和架构测试同时检查。

**决策边界：** 只有跨模块真正需要的能力才进入 Contracts。不要把每个类都抽成接口，也不要把领域内部数据结构为了复用暴露到全局。

### 3.4 ScriptableObject 作为共享配置边界

**选择：** WeaponSO、EnemySO、StateActionSO、StateConditionSO、SceneSO、Dialogue SO、MetaUpgrade SO、DropTable SO 等作者数据放在 SO 中；Actor、武器实例、房间实例和玩家局内状态放在运行时对象中。

**原因：** 内容的主要变化来自数值、状态转移、武器参数、敌人配置、场景引用和本地化内容。SO 适合在 Unity 编辑器中查看、复用、引用和审查。

**成本：** 共享资产可能被多个实例引用，错误写入运行时状态会造成跨实例污染；SO 字段变更影响序列化兼容；SO 依赖需要资产检查；作者数据、运行时状态和存档状态必须持续区分。

**收益：** 设计参数不必重新编译即可调整；新敌人、新武器和新状态可以组合已有配置；Prefab 和场景引用可在 Inspector 中检查；共享配置与实例状态分开，有利于复用和测试。

**红线：** SO 只保存共享配置和作者数据，不保存某个玩家、敌人、武器或房间实例的可变状态。需要跨场景保存的状态必须经过 Persistence、Save 或 Meta Gateway。

### 3.5 共享 SO 加运行时 materialization 的状态机

**选择：** 玩家和普通敌人共用 Zephyr.Core.StateMachine 基础设施；SO 只保存无状态定义；每个 Actor 在运行时将状态、动作、条件和转换物化为自己的实例，并在 Awake 缓存组件。

**原因：** 玩家和敌人都需要状态切换、优先级、打断和入口/出口行为，但具体行为不同。共享基础设施可保持转换语义一致，差异由上层 Action、Condition 和 SO 表达；运行时物化避免多个 Actor 共享可变状态；Awake 缓存避免每帧 GetComponent、Find 或场景遍历。

**成本：** 初始化和运行时实例图更复杂；调试需要同时查看 SO、物化实例和 Actor 组件；新行为必须理解 StateAction、StateCondition、TransitionTable 和 MovementCore；状态动作不能为了方便直接写 Rigidbody2D 速度。

**收益：** 玩家和敌人语义一致；设计师可以使用 SO 组合状态；每个 Actor 的状态隔离；可以通过 EditMode 测试覆盖转换优先级和条件逻辑。

**红线：** Assets/Scripts/Core/StateMachine/ 是锁定基础设施。普通功能不得为了新增敌人或玩家动作修改其运行时契约，应在上层增加 Action、Condition、TransitionTable 或领域组件。

### 3.6 Persistent、GameManager 和 SceneLoader 生命周期

**选择：** 启动链为 Initializer -> Persistent -> MainMenu；Persistent 承载会话级服务；GameManager 承载游戏阶段服务；普通场景通过 Persistent 中的 SceneLoader 以事件或请求方式加载。

| Owner | 生命周期 | 主要职责 | 不应承担 |
|---|---|---|---|
| Initializer | 游戏进程启动阶段 | 作为构建后总启动入口，加载 Persistent、等待其就绪并进入 MainMenu | 普通业务逻辑、具体房间策略 |
| Persistent | 整个会话 | SceneLoader、EventBus、存档、本地化、音频、对象池、计时服务 | 具体房间内容、临时 UI、局内 Actor 状态 |
| GameManager | Tutorial、MetaHub、Run 阶段 | 当前游戏阶段 UI、相机、战斗和投射物协调 | 伪装成整个会话的全局服务 |
| 具体场景 | 被加载期间 | 房间、出口、场景目标、局部环境和序列化引用 | 直接创建并管理全局服务 |
| Actor / Prefab 实例 | 实例存活期间 | 自身状态、表现、输入或行为 | 写入共享 SO、保存全局状态 |

**原因：** 存档、事件、音频和场景加载需要跨场景连续存在；房间、敌人和 UI 必须能随场景卸载。明确 owner 比在每个对象上使用 DontDestroyOnLoad 更易推理、测试和回收。

**成本：** 场景加载流程更严格；组合根需要在启动阶段解析服务并绑定引用；跨场景故障要检查 Persistent、GameManager 和具体场景；GameManager 和 Flow 仍可能成为高扇出枢纽。

**收益：** 会话服务只有一个明确 owner；场景可以自治但不重复创建全局服务；Additive 加载符合生命周期契约；退出 MainMenu 时可清晰释放 GameManager。

### 3.7 EventBus 与 SO 事件双通道

**选择：** 运行时高频、短生命周期通知使用 EventBus；需要 Inspector 配置、跨场景或由内容资产触发的事件使用 SO 事件；命令和需要返回结果的操作使用接口或 Gateway。

| 通信方式 | 适用 | 不适用 | 主要成本 |
|---|---|---|---|
| 直接接口 / Gateway | 明确调用者、参数和返回结果 | 多播通知、内容作者配置 | 维护依赖和生命周期 |
| EventBus | 运行时领域事件、多订阅者、短生命周期 | 关键业务命令、需要返回值的流程 | 订阅释放、事件追踪和隐式控制流 |
| SO 事件 | Inspector 配置、跨场景资产关联、内容通知 | 高频 Update、实例私有状态 | 资产检查和调试跳转 |
| UnityEvent | 局部表现绑定、Prefab 内部简单反应 | 核心业务、跨领域规则 | 类型安全和调用路径较弱 |

一个通道无法同时优化运行时解耦和 Unity 内容配置。双通道让通信方式与问题类型匹配，但必须防止 EventBus 变成全局控制流。

### 3.8 统一伤害、属性和生命管线

**选择：** 近战、远程、环境和其他命中统一进入 DamageInfo、DamageCalculator、Health、Shield 和 Buff 管线；不同来源通过载荷和配置表达差异，不各写一套扣血逻辑。

**原因：** Potential、Armor、Magic Resist、护盾、暴击、背刺、元素、Buff 和受击反馈会互相影响。分散扣血逻辑会产生不一致结果，也会使平衡验证失去统一入口。

**成本：** 统一载荷和解析顺序需要前置设计；简单攻击也要遵守公共管线；调试需要查看解析步骤；规则变化可能影响全部命中来源。

**收益：** 平衡规则集中；伤害、护盾、抗性和 Buff 可单独测试；Combat 与 Player、Enemies、Weapons 通过稳定协议连接；数值模拟和运行时结果更容易对齐。

### 3.9 对象池和热路径分配目标

投射物、伤害数字、音频源和其他高频短生命周期对象优先对象池化，并以 60 FPS、同屏敌人不超过 50、投射物不超过 200 作为工程预算。

**成本：** 需要 Reset、回收和引用清理；容量过小会扩容，过大增加内存；遗留订阅、协程或状态会造成复用残留。

**收益：** 减少频繁分配和销毁造成的 GC 峰值；吞吐更稳定；性能问题可以用预算和统计工具验证。

### 3.10 JSON 存档、SchemaVersion 和迁移钩子

**选择：** RunData、MetaData 等存档使用 JSON DTO，带独立 SchemaVersion；加载旧版本时按迁移钩子顺序升级，不把 Unity 场景资产直接当作玩家存档。

**原因：** 玩家进度需要独立于场景和 Prefab；新增字段、升级内容或调整结构时需要可控兼容。

**成本：** DTO、运行时模型和 SO 之间需要转换；每次结构变化都要考虑迁移和失败处理；JSON 可读但不等于安全；状态不能散落在 MonoBehaviour 或静态字段。

**收益：** 玩家进度不依赖具体场景实例；格式可独立演进；迁移逻辑可测试和审查；Save、Meta、Flow 和 UI 可通过窄端口协作。

### 3.11 本地化资产和可替换 UI Prefab

**选择：** 所有玩家可见文本进入 Unity Localization 或现有本地化服务；UI View 使用 Prefab；Controller 负责协调状态和转发操作；按钮不承载业务逻辑。

**原因：** 中文和英文是产品要求；UI 需要在 960 x 540 基线下可替换、可截图审查和持续调整。

**成本：** 新文本要维护 Key、中文值、英文值和 fallback；UI 测试要覆盖布局、锚点、交互状态和语言切换；Prefab 绑定比直接动态创建更繁琐；Controller、View 和 UseCase 会增加文件数量。

**收益：** 文案、布局和逻辑可独立修改；本地化遗漏更容易被发现；Popup 和 HUD 可以替换 Prefab；布局可通过截图客观验收。

### 3.12 架构测试和纵向 PlayMode 验证

**选择：** Zephyr.Architecture.Tests 检查程序集依赖、Editor/Runtime 依赖和受控调用；EditMode 测试验证规则；PlayMode Vertical Slice 验证启动、输入、战斗、场景和结算。

**原因：** 文档和人工 review 无法阻止 AI 或快速迭代重新引入直接单例、场景扫描、Resources.Load、直接场景加载或错误程序集引用。

**成本：** 测试程序集和 policy 要维护；结构变化必须同步测试；PlayMode 较慢且会受场景、资源状态影响。

**收益：** 架构规则变成可执行门禁；重构可以先建立安全网；AI 生成代码风险从记忆约束变为自动失败；真实运行时路径有可复现证据。

---

## 4. 领域边界与依赖方向

### 4.1 依赖原则

1. 稳定基础层不能引用具体内容领域。
2. 敌人、武器、物品和投射物之间不得通过具体 Controller 形成反向依赖。
3. Player 可以组合能力，但不能成为所有模块的服务定位器。
4. UI 读取状态和发出操作请求应经过 Controller、Gateway 或事件，而不是直接修改存档、场景或战斗内部状态。
5. Flow 是组合边界，不是所有业务规则的容器。
6. 新增跨模块能力优先进入窄接口或事件载荷，不自动扩展万能接口。

### 4.2 生命周期与数据 owner

| 数据或对象 | 配置 owner | 运行时 owner | 是否进入存档 |
|---|---|---|---|
| 武器参数 | WeaponSO | WeaponRuntime / Weapon Component | 保存获得状态或槽位，不保存配置副本 |
| 敌人参数 | EnemySO / EnemyStats 配置 | Enemy 实例 / EnemyBrain | 通常不保存实例状态 |
| 状态机图 | TransitionTableSO | Actor 上的 StateMachine 实例 | 通常不保存临时状态 |
| 玩家局内属性 | Player/Run 配置 | Player Stats、Resource、Potential | 通过 RunData 保存必要进度 |
| Meta 升级与解锁 | MetaUpgrade SO、Story DB | Meta 服务 / Session | 通过 MetaData 保存 |
| 场景与房间规则 | SceneSO、Room Prefab | 场景/房间实例 | 只保存跨局或跨场景进度 |
| 对话文案 | Localization Table / Dialogue SO | Dialogue View / Manager | 保存故事进度或完成标记 |
| UI 布局 | UI Prefab | View 实例 | 不保存 |

### 4.3 跨模块通信选择

| 问题 | 默认选择 |
|---|---|
| 需要同步调用并得到结果吗？ | 窄接口、Gateway 或 UseCase |
| 只是通知多个订阅者吗？ | EventBus 领域事件 |
| 需要设计师在 Inspector 中配置吗？ | SO 事件或序列化引用 |
| 需要跨场景存在吗？ | Persistent 服务或 Flow Gateway |
| 只属于单个 Actor 吗？ | Actor 组件或运行时实例 |
| 需要玩家重启后保留吗？ | Save / Meta Gateway 和版本化 DTO |
| 只是局部表现反应吗？ | View 事件、UnityEvent 或表现适配器 |

---

## 5. 主要工程成本与收益总账

### 5.1 汇总矩阵

| 架构投资 | 直接成本 | 主要收益 | 什么时候值得 |
|---|---|---|---|
| 多 asmdef | 配置和迁移时间、编译错误增加 | 编译期阻止错误依赖 | 领域持续增加时立即值得 |
| Contracts / Gateway | API 设计和适配器代码 | 替换实现、测试和依赖收窄 | 跨两个以上职责模块时值得 |
| SO 数据驱动 | 资产治理、序列化兼容、状态隔离 | 内容迭代和 Inspector 可见性 | 数值、敌人、武器和状态持续扩展时值得 |
| 统一 StateMachine | 初始化和调试复杂度 | 玩家/敌人语义一致，行为可组合 | 需要多状态、优先级和打断时值得 |
| Persistent / GameManager | 场景流程约束和组合根工作 | 生命周期稳定、服务不重复创建 | 有教程、Hub、Run、多场景时必须 |
| EventBus / SO 事件 | 隐式控制流和订阅管理 | 多播解耦、内容可配置 | 多个表现系统响应同一事件时值得 |
| 对象池 | Reset 和容量管理 | GC 稳定、动作反馈稳定 | 高频短生命周期对象时值得 |
| JSON + Migration | DTO 和迁移维护 | 存档长期兼容 | 发布后需要保留玩家进度时必须 |
| Localization + Prefab UI | Key、布局和引用治理 | 双语支持、可替换 UI | 所有玩家可见文本和正式 UI 都值得 |
| 架构测试 | 测试维护和 Editor 时间 | 防止依赖回退，保护 AI 协作 | 单人长期开发和快速重构时高收益 |

### 5.2 总体成本判断

相比所有逻辑放进 Assembly-CSharp、通过 Singleton 互相调用的短期方案，当前架构前期成本明显更高：

- 设计阶段需要先确定 owner、依赖方向和数据归属。
- 新增跨模块功能需要写合同、事件或适配器。
- 场景、Prefab、SO 和 Localization 需要资产级验证。
- 迁移和重构需要同步测试、架构文档和版本记录。
- 开发者必须理解 Unity 生命周期和程序集依赖，而不是只关注局部脚本。

但随着内容量和协作轮次增加，直接方案的隐性成本会以耦合、回归、无法定位的场景状态和重复实现形式增长。当前选择的收益不是每个功能都更快，而是让后续功能的边际风险下降：新敌人和武器可以复用稳定边界；新场景不会再创建第二套加载体系；AI 生成代码也更难无声地扩大依赖。

### 5.3 适用规模

当前方案适合：

- 单机 Unity 2D 动作游戏。
- 单人开发或小型团队。
- 内容由 Scene、Prefab、SO 和本地化资产构成。
- 需要跨场景存档、Meta 进度和稳定战斗规则。
- 内容量中等，尚未达到大型在线服务或数千实体并行模拟规模。

当前方案不试图解决多人网络同步、服务器权威状态、大规模开放世界分布式流式加载、极限同质实体并行仿真或完整 mod 脚本沙箱。如果未来进入这些范围，应新开架构专项，而不是继续在现有 Gameplay 组件上堆补丁。

---

## 6. 明确拒绝的替代方案

### 6.1 单一 Assembly-CSharp

初期最省配置，但无法在编译期阻止领域互相引用。随着 Player、Enemies、Weapons、UI 和 Flow 增加，任何类型都可能成为公共依赖。它只适合临时测试、一次性 Editor 工具或尚未形成正式边界的原型代码，不适合作为正式运行时领域的长期落点。

### 6.2 单一万能 Zephyr.Core

大 Core 表面集中，实际同时承载 Contracts、战斗、存档、UI、武器、物品、流程和场景逻辑，会形成高扇出、高耦合和高变更风险。当前通过拆出 Contracts、Runtime、Combat、Flow、Persistence、Meta、Audio、Camera、Weapons、Items 和 Projectiles 等程序集处理；后续重点是收窄 API，而不是继续创建无职责区分的 Core 子目录。

### 6.3 全局 Singleton 作为默认通信方式

Singleton 适合会话级服务的组合根入口，不适合成为普通 Actor、Pickup、Trigger、View 和领域逻辑的默认依赖。它隐藏生命周期、增加测试替换成本，并让场景对象脱离显式 owner。允许组合边界解析一次具体实例，再缓存为接口或服务引用；新领域方法不得在每次调用路径中重复读取 Instance。

### 6.4 每个场景自行加载和卸载

这会产生重复 SceneLoader、Persistent 服务丢失、GameManager 生命周期不一致、场景名硬编码和难以回收的静态状态。当前由统一 SceneLoader 执行策略；只有 Initializer 可以使用 Single，其余加载默认 Additive。

### 6.5 运行时 Find、Tag 扫描和 Resources.Load

这些入口把依赖推迟到运行时，降低可测试性和可审查性，且会在场景重命名、对象禁用或加载顺序变化时产生隐性故障。优先使用序列化引用、Prefab 组合、Context、注册表、Gateway 和初始化绑定；确需基础设施级扫描时必须登记用途、owner、风险和删除条件。

### 6.6 完全 ECS 化

当前游戏的主要复杂度来自状态语义、Inspector 内容、Prefab、Animator、UI、场景和存档，而不是同质实体数量。完整 ECS 会增加混合渲染、编辑器工作流和内容转换成本。未来如果出现明确的高密度并行热点，可以局部引入专门优化，但必须以性能证据和独立架构评估为前提。

### 6.7 把玩法规则写进 Animator Controller

Animator 适合表现状态和过渡，不是伤害、资源、存档和房间目标的权威逻辑。玩法规则由 StateMachine、Combat、Flow 和数据层拥有，Animator 通过参数和表现适配器反映状态。

---

## 7. 当前工作区改动的正式基线说明

截至 2026-10-19，工作区存在大量未提交改动，涉及文档、场景、Prefab、SO、asmdef、Core/领域脚本、测试和若干资源元数据。根据用户要求，这些改动可以作为正式架构说明的事实基线，而不被视为必须回退的临时状态。

### 7.1 可纳入基线的架构变化

- 旧 Zephyr.Core.asmdef 被移除，能力拆分为多个更窄的程序集。
- Contracts、Runtime、Combat、Flow、Persistence、Meta、Audio、Camera、StateMachine 等边界被显式化。
- Enemies、Rooms、Interaction、Weapons、Items、Projectiles、Player 和 UI 拥有独立编译边界。
- Player 死亡、潜能损失、Respawn、交互、敌人行为和武器路径被继续向明确模块归档。
- Scene、Prefab、SO 和测试资产同步调整，以匹配新的类型和引用边界。
- 增加或维护架构测试、EditMode 测试和 PlayMode 测试程序集。
- 相机、音频、流程、存档、Meta、对话和本地化继续沿用统一生命周期与 Gateway 方向。

### 7.2 仍需诚实标记的状态

1. 运行时代码仍有受控的具体 Singleton、场景扫描和场景加载调用；架构测试保证其不随意增长，但不能替代逐步移除。
2. Zephyr.Player、Zephyr.Gameplay.Interaction、Zephyr.Core.Dialogue 和部分 Flow 类仍是高扇出或高职责密度区域。
3. 一些内部实现类型仍以 public 暴露，编译边界已经建立，但类型可见性边界还未完全收紧。
4. 场景扫描、反射兼容和服务定位集中在少数大类中，修改这些类的回归风险高于普通领域类。
5. 文档中的程序集数量、实现状态和集成状态必须随工作区基线同步，不能继续引用过期的 7 个或 8 个 asmdef 表述。

### 7.3 采用未提交改动作为基线的规则

- 本文不为未提交改动自动背书为已验证完成；实现状态仍以编译、测试、Console、场景和资源检查为准。
- 如果后续提交撤销某项架构变化，必须同步更新本文和相关详细设计章节。
- 如果改动触及 StateMachine、Persistent、GameManager、Initializer、SceneLoader、Core Contract 或存档格式，必须按架构专项流程记录影响和回滚路径。
- 文档基线与代码基线不一致时，以当前实现为事实，但必须在本文的待收敛或版本记录中注明差异。

---

## 8. 现阶段主要风险与控制措施

| 风险 | 影响 | 当前控制 | 后续动作 |
|---|---|---|---|
| Singleton 读取继续扩散 | 生命周期隐藏、测试困难、领域耦合增加 | 架构测试和受控调用台账 | 迁移业务级读取到 Gateway/Context |
| Flow 成为万能编排器 | 所有新功能都依赖 GameFlow | 组合根边界和现有 Gateway | 拆分流程命令、场景上下文和 UseCase |
| Player 成为高扇出枢纽 | 玩家小改动影响输入、战斗、UI、流程和存档 | Player asmdef 和组件化目录 | 分离 Flow、Interaction、Combat Adapter、Presentation |
| SO 被写入运行时状态 | 多实例串状态、测试顺序依赖 | 数据/运行时 owner 规则 | 增加资产和运行时只读约束测试 |
| 场景引用失效 | MissingReference、加载失败 | Unity MCP 场景/Prefab 检查、PlayMode | 建立关键场景和 Prefab 完整性检查 |
| UI 文本硬编码 | 本地化遗漏、布局裁切 | Localization 规则和 UI Prefab | 增加 Key/fallback 和 960 x 540 检查 |
| EventBus 过度使用 | 控制流难追踪、订阅泄漏 | 通信选择表和测试 | 关键命令改用接口，订阅增加生命周期测试 |
| 架构文档滞后 | 新代码遵循旧规则，AI 产生错误实现 | 基线日期和版本记录 | 架构专项同步更新文档与测试 |
| 工作区大规模未提交 | 难以区分用户改动和新改动 | 不回退用户文件，记录正式基线 | 提交前按模块拆分并回归验证 |

---

## 9. 未来演进顺序与成本控制

后续不建议通过继续横向增加程序集数量来获得“看起来更模块化”的结果。推荐按以下顺序降低实际风险。

### 阶段 A：保护当前边界

1. 固定当前 asmdef 依赖图和受控调用清单。
2. 确认架构测试、EditMode 测试和关键 PlayMode 测试可以复现。
3. 对工作区中的场景、Prefab、SO 和脚本改动完成完整导入与 Console 检查。
4. 维护本文与详细设计中的当前实现基线。

### 阶段 B：降低运行时隐式依赖

1. 优先迁移业务级 SaveSystem.Instance、LocalizationManager.Instance、GameFlow.Instance、PlayerSpawner.Instance 等读取。
2. 把可序列化引用改成 Inspector 或 Prefab 引用，把必要的场景结构发现集中到明确的基础设施 owner。
3. 让组合根在初始化阶段解析服务，领域逻辑只持有接口或明确服务对象。
4. 统计 Singleton、SceneScan、SceneLoad 和静态可变状态数量，要求每阶段不增加，最好下降。

### 阶段 C：收窄高扇出模块

1. 分离 Player Flow、Player Interaction、Player Combat Adapter 和 Player Presentation 的依赖面。
2. 将 DialogueManager 的流程协调、服务访问和 View 绑定拆成窄职责协作者。
3. 让 GameFlow 只管理状态和流程命令，不直接承载 Settlement、存档写盘、UI 构建和房间策略。
4. 将 Save/Meta 的文件 IO、数据转换、业务结算和 UI Presenter 分开。

### 阶段 D：收紧可见性和验证

1. 对程序集内部实现默认使用 internal，只保留跨程序集真正需要的 public 合同。
2. 增加业务 Singleton 数量下降、高扇出程序集预算和 SO 运行时写入检查。
3. 为关键场景、Prefab、Localization 和存档迁移增加可复现验证。
4. 达成后进行下一轮独立架构复评，不用单纯类型数量或 asmdef 数量作为质量指标。

---

## 10. 架构决策验收标准

任何新增敌人、武器、房间、掉落、Meta 内容、对话、UI、音频或关卡目标，至少应回答：

1. 功能 owner 模块是什么，代码、SO、Prefab、场景和动画最终放在哪里？
2. 生命周期属于 Persistent、GameManager、场景、房间、Prefab 实例还是 Actor 实例？
3. 哪些是共享配置，哪些是运行时状态，哪些需要进入存档？
4. 跨模块通信使用接口、Gateway、序列化引用、领域事件还是 EventBus？为什么？
5. 是否新增具体 Singleton、静态可变状态、全局扫描、直接场景加载或运行时资源路径查找？
6. 是否增加旧 Core 依赖、领域反向引用、循环依赖或高扇出？
7. 新增文本是否有中文 Key、英文 Key 和 fallback？UI 是否适配 960 x 540？
8. 新增场景、Prefab、SO 和引用是否完成导入检查、Console 检查和截图验证？
9. 代码、EditMode、PlayMode、架构测试和存档迁移是否有适配验证证据？
10. 设计文档、GDD 和实现是否同步，是否需要记录 DocVersion、SchemaVersion 或架构专项？

如果这些问题无法回答，功能不应通过“先把逻辑塞进已有 Manager”推进。应先补充边界记录，必要时单独立项为架构专项。

---

## 11. 最终评价

Zephyr 当前架构的核心价值不在于层数多或 asmdef 多，而在于建立了对长期开发真正有用的约束：

- 依赖方向可以在编译期和测试中检查。
- 运行时服务、场景内容和 Actor 实例拥有可描述的生命周期 owner。
- 共享配置、运行时状态和存档状态分开。
- 玩家与敌人共享稳定的状态机语义，领域行为仍可独立扩展。
- 战斗、存档、Meta、相机、音频、UI 和本地化有明确的服务边界。
- 新功能可以沿用现有 Scene、Prefab、SO 和 Unity Inspector 管线。
- AI 协作受到代码结构、asmdef、测试和红线共同约束，而不只依赖提示词记忆。

代价同样明确：设计前置工作更多，跨模块调用更讲究，场景和资产验证不能省略，文档和测试必须持续维护。对于一个从可玩闭环进入内容扩展阶段的单人 Unity 项目，这些成本是合理的工程投资，因为它们直接降低后续内容增长带来的回归半径和架构失控风险。

当前最重要的下一步不是重新设计整套基础设施，而是沿现有方向继续收窄：减少业务级 Singleton，降低 Player、Flow、Dialogue 和 Interaction 的扇出，收紧内部类型可见性，并保持架构测试和场景验证可复现。只要这些指标不恶化，Zephyr 就能在不推倒重来的情况下继续扩展玩法和内容。

---

## 12. 版本记录

| 日期 | 版本 | 变化 |
|---|---|---|
| 2026-10-19 | 1.0 | 首次建立正式架构阐述，纳入当前工作区未提交改动作为实现基线，补充选择原因、成本收益、替代方案、风险和演进顺序。 |

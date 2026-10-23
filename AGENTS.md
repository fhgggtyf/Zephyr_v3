# Zephyr Unity 项目全局规则

本文件是 `Zephyr` Unity 项目的根级协作规则。凡是在本目录及其子目录内进行的开发、资源制作、场景编辑、测试和审查，都必须遵守本文件。子目录中的 `AGENTS.md` 可以补充本文件，但不能降低本文件规定的验证和安全要求。

## 1. 项目基线

- Unity 版本：`6000.5.6f1`。
- 项目类型：Unity 2D 横版 Metroidvania，URP 2D 渲染。
- 关键包版本以 `Packages/manifest.json` 和 `Packages/packages-lock.json` 为准，当前包括：
  - Cinemachine `3.1.7`
  - Input System `1.20.0`
  - Localization `1.5.12`
  - Aseprite/2D Animation/Sprite/Tilemap 等 Unity 2D 包。
- 设计文档：
  - `Documents/Zephyr Game Design Document.md`：面向玩法和内容设计。
  - `Documents/Zephyr 底层架构详细设计（草案）.md`：面向系统边界、运行时契约和实现约束。
- 代码、场景、Prefab 和 ScriptableObject 是项目真实实现；设计文档必须随正向实现同步更新。
- 不凭空引入新的框架、状态机、场景加载通道或资源格式。优先复用仓库已有的模式、SO、事件和服务。

## 2. 工具使用总则

### 2.1 Unity 工具优先

凡是 Unity Editor 可以完成的操作，必须优先通过已连接的 Unity MCP 工具完成，尤其是：

- 创建、打开、保存、复制、移动或删除 Unity 场景、Prefab、材质、动画、Animator Controller、Sprite、Tilemap 和 ScriptableObject。
- 资源导入设置、Texture/Sprite 类型、切片、Sprite pivot、Pixels Per Unit、排序层、动画帧和 Animator 状态配置。
- GameObject 创建、层级调整、Transform、组件添加/移除、序列化字段填写、Prefab 应用和引用绑定。
- 场景中的 Camera、Cinemachine、UI、Collider、Rigidbody、Spawner、CameraBounds、CameraZone 和其他组件配置。
- AssetDatabase 刷新、重导入、Prefab 保存和场景保存。

禁止为了省事直接用文本拼接或手动改写 `.unity`、`.prefab`、`.asset`、`.controller`、`.anim`、`.meta` 等 Unity 序列化文件来模拟拖拽、导入或绑定。这样做容易产生错误 GUID、缺失根对象、无效 YAML 和隐藏引用。

例外：如果 Unity MCP 不可用、操作失败或无法暴露所需 API，必须先在工作更新中说明原因；可以修改 C#、JSON、Markdown 等非 Unity 序列化文件，但不得静默地手写 Unity YAML。需要修改序列化文件时，应优先创建并通过 Unity MCP 执行一次性 Editor 脚本；只有在用户明确允许后才使用其他方式，并在完成后做完整导入验证。

### 2.2 资源工具分工

严格遵守以下分工：

1. `2d_assets`：只用于灰盒、测试、占位图、临时 Sprite、临时 Tile 和调试素材。
2. `unity_mcp` 的 `AssetGeneration`：用于正式资源生成或需要进入最终内容管线的资源；**当前暂时禁止使用 MCP 生图功能**，包括通过 Unity MCP / AssetGeneration 生成或编辑图片、Sprite、Spritesheet、贴图、Cubemap、材质纹理等图像类资源。恢复该能力前必须由用户明确授权并更新本条规则。
3. 任何资源生成后，导入、切片、动画创建、Animator 绑定、Prefab 引用、排序层和碰撞体设置，都必须继续由 `unity_mcp` 自动完成。
4. 不能把灰盒资源误当成正式美术资源；不能把正式资源直接覆盖灰盒文件而不保留清晰的资源路径和命名。
5. 资源命名、目录和引用必须在生成时确定，不在 Unity 中留下依赖临时文件名的引用。

### 2.3 调用后的即时验证

每次通过 Unity MCP 生成或修改任何内容后，都必须在同一工作回合执行验证，不得把验证推迟到任务最后。

- 任何 Unity 工具生成、修改、导入、删除、移动或绑定内容后：调用 `Unity_GetConsoleLogs`。
- 只要本次操作生成或修改了场景，或改变了场景中的 GameObject/组件/引用：必须调用 `Unity_SceneView_Capture2DScene` 截图。
- 场景截图必须在场景保存、资源刷新和引用绑定完成后执行，不能截到半完成状态。
- 截图结果必须在给用户的工作更新或最终回复中描述：当前场景、相机可见范围、主要层级/对象、UI 是否完整、Sprite 是否正常、是否存在粉色材质、是否有明显缺失引用或重叠。
- 如果截图工具返回图片，应同时提供图片结果和文字描述；如果截图失败，必须说明失败原因，并用可用的层级/日志检查替代，不能假装已完成视觉验证。
- Console 中出现 Error、Exception、MissingReference、Serialization、YAML parse、shader/material pink、failed to load 或 missing script 时，任务不能视为完成。必须修复后重新执行日志检查；无法修复时必须明确阻塞点。
- Warning 不能自动忽略：说明其来源和是否影响本次改动；无关的已有 Warning 可以记录为残余风险。

## 3. 标准开发流程

每个非纯问答任务按以下顺序执行：

1. **理解范围**：读取相关脚本、SO、Prefab、场景、asmdef、输入配置和设计文档，检查已有未提交改动，避免覆盖用户工作。
2. **确定边界**：明确哪些是玩法设计、哪些是运行时实现、哪些是资源/场景编辑；确认是否需要 Persistent、GameManager、玩家或敌人。
3. **先给出短计划**：涉及多个系统时列出改动文件、Unity 操作、验证方式和可能风险。
4. **实施**：代码改动使用 `apply_patch`；Unity 对象和引用通过 Unity MCP；保持改动最小且沿用现有模式。
5. **立即验证**：每个 Unity 操作后查 Console；每个场景操作后截图；代码改动后进行适配的编译/测试。
6. **集成验证**：检查相关场景能导入、引用完整、Play Mode 关键流程可走通，并检查 Git diff 不包含无关元数据或临时文件。
7. **回报结果**：说明已完成内容、验证结果、截图观察、未能验证的部分和残余风险。

## 4. 项目架构边界

### 4.1 场景生命周期

- 正常启动链为 `Initializer -> Persistent -> MainMenu`。
- `Persistent` 负责 `SceneLoader`、事件总线、存档、本地化、音频、对象池和计时服务，会话期间不卸载。
- `GameManager` 负责当前游戏阶段的 UI、相机、战斗和投射物服务；进入 Tutorial、MetaHub 或 Run 时加载，返回 MainMenu 时才卸载。
- 普通场景加载必须通过 Persistent 中的 `SceneLoader`。其他场景只能发出 SceneLoad 请求/事件，不能自行绕过 SceneLoader 直接加载或卸载场景。
- 场景引用使用 `SceneSO`，不要在业务代码里硬编码场景名、路径或 Build Index。
- 只有 `Initializer` 可以使用 `LoadSceneMode.Single`；其余加载默认使用 Additive，并保持 Persistent/GameManager 的生命周期契约。
- 不在单个 Manager 上使用 `DontDestroyOnLoad` 伪造结构持久化。

### 4.2 StateMachine

- `Assets/Scripts/Core/StateMachine/` 是锁定基础设施，除非用户明确要求修改基础设施，不得改动其运行时契约。
- 玩家和普通敌人使用同一套 StateMachine 基础架构；行为差异通过上层 `StateActionSO`、`StateConditionSO` 和 `TransitionTableSO` 表达。
- SO 是共享配置，不保存单个 Actor 的运行时可变状态；运行时状态放在 materialized action/condition 或 MonoBehaviour 中。
- 状态机条件和动作应在 `Awake` 缓存组件，不能在每帧 `GetComponent`、`Find` 或遍历场景。
- 物理移动由现有 `MovementCore`/对应运动组件负责；状态动作不要绕过架构直接写 Rigidbody 速度，除非现有该状态的明确设计已经这样做。
- 攻击输入是 `Primary` 左键和 `Secondary` 右键；不要重新引入必须先切换的 Active Weapon 战斗姿态。ActiveSlot 只用于显示或明确替换哪个槽位。

### 4.3 相机

- Gameplay 相机由 GameManager 的 `CameraController` 管理；房间通过 `CameraBounds`、`CameraZone` 和 `CameraConfinerController` 提供局部配置。
- Cinemachine 3.1 是当前相机实现；不要另起一套平行跟随系统。
- 相机必须保留死区、水平速度前瞻、垂直速度构图、手动上下窥视、房间边界和独立震动锚点。
- 震动不能污染逻辑跟随位置；Hit Pause 时仍要使用非缩放时间保持震动。

### 4.4 本地化与 UI

- 所有玩家可见文本都必须使用 Unity Localization/项目现有本地化服务；禁止把中文或英文直接写死在 UI 逻辑中。
- 新增文本必须同时提供中文和英文 Key/值，并检查 fallback。
- UI 面板和 Popup 应作为可替换 Prefab，视觉 Sprite、字体、按钮布局和文案不能硬编码在脚本中。
- UI 分辨率基线为 `960 x 540`；Canvas、Scaler、锚点和安全区域必须在 SceneView 截图中确认可见且无裁切。
- 按钮只负责触发中央控制器公开的方法；业务逻辑集中在 Controller/Manager。

### 4.5 功能开发与架构专项分级

当前架构评分已达到可以继续普通功能开发的门槛，但不代表所有架构债务已经清零。必须将“内容/玩法实现”和“底层架构变更”分开判断，不能因为普通功能需要跨模块就默认重写基础设施。

#### 可以直接开始的普通功能

- 新敌人行为、新武器、新房间、新关卡目标、新掉落内容、玩家表现、普通 UI 和现有模块内的小幅重构，可以在现有架构边界内直接开始。
- 新敌人必须复用现有 StateMachine、`EnemyActionSO`、`EnemyConditionSO`、Animator、EnemyStats 和 EnemyBrain 边界。
- 新武器必须复用现有 `WeaponSO`、`WeaponRuntime`、`WeaponComponent`、命中框、资源消耗和攻击输入边界。
- 新关卡和房间必须复用 `SceneSO`、`SceneLoader`、`CameraBounds`、`CameraZone`、房间目标接口和现有场景生命周期。
- 新 UI 必须复用现有 Controller、View、Popup、Localization 和 EventBus 模式；按钮只调用中央 Controller 的公开方法。
- 新配置数据放入 ScriptableObject；单个 Actor、玩家、敌人和武器的运行时可变状态不能写回共享 SO。

#### 新代码的硬性禁止项

- 不得新增平行的 SceneLoader、玩家生成系统、状态机、事件总线、战斗服务、资源加载服务或全局 Manager。
- 不得新增以 `DontDestroyOnLoad` 伪造生命周期的 Manager。
- 不得在新业务代码中直接使用 `SceneManager.LoadScene`、`SceneManager.UnloadScene`、`Resources.Load`、`GameObject.Find` 或全局 Tag 查找。
- 不得把 `GameFlow.Instance`、`SaveSystem.Instance`、`LocalizationManager.Instance`、`PlayerSpawner.Instance` 等单例访问扩散到新的领域逻辑、运行时服务或 UI 逻辑中。
- 已存在的直接单例访问属于待收敛架构债务，不构成新代码的先例；新代码应优先使用已有 Gateway、接口、EventBus 或组合根注入。
- 组合根可以在初始化边界解析已有单例一次，并缓存为接口或明确的服务引用；内部方法不得在每个调用路径中反复读取具体 `*.Instance`。
- 不得通过局部功能提交顺手引入新的程序集边界、命名空间迁移、Core 大拆分或迁移桥删除。

#### 开始跨模块功能前必须先确定

任何涉及两个以上职责模块的功能，开始编码前必须明确记录以下内容；涉及三个以上模块时必须先给出简短架构方案：

1. 功能归属模块和最终文件目录。
2. 对象生命周期属于 `Persistent`、`GameManager`、具体游戏场景、房间、Prefab 实例还是 Actor 实例。
3. 哪些是 ScriptableObject 配置，哪些是运行时状态，哪些需要写入存档。
4. 模块之间使用方法、Gateway、接口、领域事件还是 EventBus 通信，以及依赖方向。
5. UI 文本、本地化 Key、Prefab、资源引用和 `960 x 540` 布局如何进入现有管线。
6. 需要执行的脚本验证、EditMode/PlayMode 验证、Console 检查、场景截图和 Prefab/SO 引用检查。

#### 必须单独立项的架构专项

以下工作不能与普通玩法提交混做，必须先完成边界设计、迁移顺序和回归门禁：

- 修改 `Assets/Scripts/Core/StateMachine/` 或改变其运行时契约。
- 修改 Persistent、GameManager、Initializer、SceneLoader 或场景生命周期协议。
- 新增全局服务、改变单例生命周期或替换组合根。
- 修改三个以上程序集之间的依赖方向，或新增/删除 asmdef。
- 删除 `Zephyr.Enemies -> Zephyr.Items/Zephyr.Weapons`、`Zephyr.Items -> Zephyr.Weapons` 等迁移桥。
- 对 Core 进行 Contracts、Flow、Persistence、Combat 等职责级拆分。
- 全局替换 Save、Localization、GameFlow、Player 生命周期的依赖方式。

架构专项的最低前置条件：写明目标边界、依赖图、序列化/Prefab/场景影响、迁移顺序、回滚方式和验收门禁；未满足前置条件时，不得以“顺便重构”的方式实施。

#### 默认决策规则

- 如果功能只是新增内容并能复用现有边界：
- 直接实现，不暂停普通开发。
- 如果功能需要改变 Persistent、GameManager、SceneLoader、StateMachine 或 Core Contract：先做架构方案，再实现。
- 如果功能需要新增单例、直接场景加载、运行时资源路径查找、跨模块反向引用或删除迁移桥：默认禁止直接做，先设计替代的 Gateway、事件或组合根方案。
- 如果无法判断属于普通功能还是架构专项：按架构专项处理，先确认边界，不凭感觉修改底层。

### 4.6 功能开发架构不劣化规则

本节是所有后续功能开发的长期门禁。它约束“新增和修改行为”，不要求普通功能先完成全部历史债务清理；当历史架构债务被修复后，本节仍然继续适用，并自动以修复后的更干净边界作为新基线。

#### 4.6.1 开工前的最小架构记录

任何新增敌人、武器、房间、掉落、Meta 内容、对话、UI、音频或关卡目标，开工前必须在工作计划或任务报告中写明：

1. 功能 owner 模块、目标目录、最终资产类型和预期消费者。
2. 生命周期 owner：`Persistent`、`GameManager`、场景、房间、Prefab 实例或 Actor 实例。
3. 配置数据、运行时状态、临时表现状态和存档状态的分别归属。
4. 依赖方向和通信方式：直接接口、Gateway、序列化引用、领域事件或 EventBus；不得只写“由 Manager 处理”。
5. 预期新增的程序集引用、单例解析、场景对象引用、UI 文本 Key、Prefab/SO/动画/音频资源。
6. 代码、资源、Prefab、场景、Console、EditMode/PlayMode 和截图验收范围。

涉及三个以上职责模块时，必须先给出一页以内的架构方案；没有完成该记录，不得开始跨模块实现。

#### 4.6.2 依赖与边界不可恶化

- 新代码不得新增对 `Zephyr.Core` 兼容宿主的依赖；应直接引用目标专用程序集。若任务确实需要旧 Core 类型，必须先记录具体类型、原因、替代方案和删除条件。
- 新代码不得增加领域程序集之间的反向引用、循环依赖或未经说明的宽引用。新增 asmdef、删除 asmdef 或修改三个以上程序集依赖仍属于架构专项。
- 新功能必须复用现有 `SceneLoader`、StateMachine、EventBus、Combat、Weapon、Save、Meta、Localization、Audio、Camera 和 UI 边界；不得建立并行实现。
- 新代码不得把一个跨模块的具体实现类型当作公共契约暴露给其他领域。跨模块公开 API 优先使用已有 Contract、Gateway、值类型或事件载荷。
- 任务结束前必须比较任务前后的 asmdef 依赖图、旧 Core 消费者、直接单例读取、全局查找和直接场景 API。任何指标恶化都必须修复或在报告中明确列为架构专项阻塞项。

#### 4.6.3 生命周期、单例与服务访问

- 只有 `Initializer`、`Persistent`、`GameManager` 或明确声明的组合边界可以解析具体 `*.Instance`；领域逻辑、Actor、Pickup、Trigger、View 和普通 UI Controller 不得新增具体单例读取。
- 组合边界解析后必须缓存为已有接口或明确的依赖对象；业务方法不得反复读取 `*.Instance`，也不得把静态单例包装成新的服务定位器。
- 不得新增 `DontDestroyOnLoad` 对象、静态可变服务、全局事件总线或新的 Manager 以绕过现有 owner 关系。
- 场景对象之间优先使用序列化引用、父级组合、明确的 Context 或已有 Gateway。不得用 `FindAnyObjectByType`、`FindObjectOfType`、`GameObject.Find` 或全局 Tag 查找补齐本应由组合根提供的业务依赖。
- 现有债务允许在未涉及的范围内暂时存在，但新代码不得扩散它；如果修改了已有债务代码，至少不得增加直接读取数量或扩大读取范围。

#### 4.6.4 数据、资源与视图边界

- ScriptableObject 只保存共享配置和作者数据，不保存玩家、敌人、武器或房间实例的运行时可变状态。
- 运行时状态不得通过修改共享 SO、静态字段或场景资产实现跨实例传递；需要跨场景保存的状态必须经过 Save/Meta Gateway。
- 所有场景、Prefab、SO、动画、音频和本地化资源必须使用已有目录、命名和引用管线；不允许用运行时路径查找替代 Inspector/Prefab/SO 引用。
- UI Controller 只负责协调状态、绑定 View 和转发操作；不得在普通功能中复制 Dialogue/UI framework、硬编码可见文案或用代码拼出不可替换的正式视图树。
- 新增玩家可见文本必须同时加入中文和英文 Localization Key，并验证 fallback、`960 x 540` 布局和可替换 Prefab。

#### 4.6.5 任务验收与架构差异检查

每个功能任务结束时，除了功能自测，还必须确认：

1. 没有新增平行基础设施、全局 Manager、直接场景加载、`Resources.Load` 或业务全局扫描。
2. 没有新增旧 Core 依赖、领域反向引用或未记录的具体实现耦合。
3. 单例读取、静态可变状态、场景扫描和运行时 UI 构造数量相对任务前没有增加；若减少，应记录减少内容。
4. 新增配置、运行时状态、存档字段和资源引用都有明确 owner，且共享 SO 没有被写入实例状态。
5. 代码验证、Unity Console、相关测试和场景/Prefab/SO 检查均按第 7 节执行；不能运行的部分必须明确标记为未验证。
6. 设计文档中受影响的玩法规则、完成状态、内容生产标准或架构边界已经同步更新。

#### 4.6.6 历史债务例外登记

历史遗留的 `Zephyr.Core` 引用、具体单例读取、兼容 namespace、场景扫描或静态入口，不因本节发布而要求一次性全部删除。它们只能作为“已登记例外”保留：

- 例外必须写明文件、类型、当前 owner、风险、替代边界和计划删除条件。
- 例外不能成为新代码的参考实现，也不能被复制到新的模块、Prefab、场景或资产。
- 架构修复删除例外后，后续任务必须遵守修复后的边界；不得为了兼容旧写法重新引入例外。
- 如果一项功能必须修改例外本身，应按架构专项处理，并补充迁移顺序、回滚点和回归门禁。

## 5. 资源、Prefab 和场景约定

- 目录按职责分类：
  - `Assets/ArtResources/`：源美术和正式生成资源。
  - `Assets/Animations/`：Animator Controller、Animation Clip 和动画相关资源。
  - `Assets/Prefabs/Player/`、`Enemies/`、`Weapons/`、`Rooms/`、`UIPrefabs/`、`Core/`：可复用 Prefab。
  - `Assets/Scenes/`：场景，按 `Core/`、`MainMenu/`、`Tutorial/`、`Run/`、`Levels/`、`Hub/`、`Test/` 分类。
  - `Assets/ScriptableObjects/`：配置 SO，按 Scenes、Weapons、Enemies、Localization 等职责分类。
  - `Assets/Scripts/`：按 Core、Player、Gameplay、UI、Inputs、Projectiles、Testing 分类，并保持 asmdef 边界。
- 新建文件必须放在正确目录；不要把新资源堆在 Assets 根目录或临时目录。
- Prefab 根对象必须存在且名称与文件用途一致；组件序列化引用必须可解析；不留下 Missing Script、Missing Prefab、Missing Sprite 或无效 GUID。
- 敌人必须按项目现有敌人架构处理 Animator、EnemyStats、EnemyBrain/StateMachine、碰撞体、攻击组件和可视化；Boss 的自定义行为应使用已有 Boss 上层控制器边界，不修改普通敌人基础设施。
- 武器组件、攻击移动、命中框和资源消耗必须沿用现有 WeaponSO/WeaponRuntime/WeaponComponent 模式，数据与运行时逻辑分离。
- 灰盒资源可以简单，但仍必须有正确的排序层、碰撞层、引用和可运行的最小行为。

### 5.1 资产归档规范（按类型归位，按模块分类）

所有 AI 生成或修改的资产，必须遵循本项目既有的目录结构。严禁将新文件堆放在 `Assets/` 根目录或临时目录；`Assets/MCPTest/` 仅用于明确隔离的 MCP 管线测试，不得作为普通内容资产的归档位置。

#### 按资产类型归入对应的大目录

| 资产类型 | 目标根目录 | 本项目当前使用的子目录规则 |
| --- | --- | --- |
| C# 脚本 | `Assets/Scripts/` | 按系统归入现有 `Core/`、`Player/`、`Gameplay/`、`Enemies/`、`Weapons/`、`UI/`、`Levels/`、`Inputs/`、`Projectiles/`、`Items/`、`Meta/`、`Testing/` 或 `Tests/`；优先沿用已有模块目录。 |
| ScriptableObject | `Assets/ScriptableObjects/` | 按用途归入现有 `Enemies/`、`EnemyStates/`、`WeaponConfigs/`、`PlayerStateMachine/`、`PlayerData/`、`Scenes/`、`Dialogue/`、`DropTables/`、`Events/`、`GameSettings/`、`MetaUpgrades/`、`BuffConfigs/` 等目录；没有合适目录时才创建与现有命名风格一致的新目录。 |
| Prefab | `Assets/Prefabs/` | 按实体类型归入现有 `Player/`、`Enemies/`、`Weapons/`、`Rooms/`、`UIPrefabs/` 或 `Core/`。 |
| 场景 | `Assets/Scenes/` | 按用途归入现有 `Core/`、`MainMenu/`、`Tutorial/`、`Run/`、`Levels/`、`Hub/` 或 `Test/`。 |
| 动画与 Animator | `Assets/Animations/` | 按所属实体归入现有 `Player/`、`Enemies/` 或 `Weapons/`；Animator Controller、Animation Clip 和相关动画资源保持同一实体目录。 |
| 美术资源 | `Assets/ArtResources/` | 按现有 `Aseprite/`、`Sprites/`、`Tiles/`、`Materials/`、`Particles/`、`Fonts/` 和 `Audio/` 等来源/用途目录归档；灰盒占位资源必须单独放入明确的 `Placeholders/` 子目录或隔离测试目录。 |
| 本地化资源 | 优先 `Assets/ScriptableObjects/Localization/`，或沿用 Unity Localization 已存在的资源位置 | 按语言和模块分类；创建前先检查仓库中已有 Localization Table Collection 的实际位置，不要在 Assets 根目录另起一套本地化目录。 |

#### 创建新文件时的强制流程

1. 先判断资产类型：脚本、SO、Prefab、场景、动画、材质、Sprite、音频或本地化资源。
2. 再判断模块：它服务于玩家、敌人、武器、UI、关卡、核心基础设施或测试管线。
3. 在对应大目录下查找现有模块子目录，并确定最终路径后再创建文件。
4. 如果找不到合适的子目录，不要随便新建顶层乱名目录；先确认能否归入已有分类。确实需要新分类时，使用与现有目录一致的英文命名风格，并保持职责单一。
5. 新建 Unity 资产后，必须通过 Unity MCP 完成导入、引用绑定和验证；不得仅靠复制文件或手写序列化 YAML 伪造 Unity 资源。

#### 禁止事项

- 禁止在 `Assets/` 根目录直接创建脚本、SO、Prefab、场景、动画或正式美术资源。
- 禁止用 `Temp/`、`New Folder/` 等无意义名称作为归档目录；`Scenes/Test/` 与 `Assets/MCPTest/` 只允许用于明确的测试内容。
- 禁止将同一功能的资产分散到多个不相关目录，例如把敌人脚本放在一个模块、敌人 SO 放在美术目录。
- 禁止为了省事把通用资产复制到关卡目录，造成两套逻辑或无法追踪的引用。
- 禁止复制 Unity 资产后保留重复或非法 GUID；所有 `.meta` 必须有合法且唯一的 GUID。

#### 判断共享 vs. 专属

- 可能被两个以上场景或系统复用的资产，放入对应类型的全局模块目录。
- 只服务于某个特定场景的一次性内容（例如该场景专属触发器脚本或剧情 SO），可放入对应的场景专属目录；但仍必须保持类型归档一致。
- 拿不准时，优先放入全局模块目录；只有确认仅被单处引用后，才考虑下沉到场景专属目录。

## 6. 场景修改强制流程

任何创建或修改场景的任务都必须按以下顺序：

1. 通过 Unity MCP 打开目标场景并读取当前层级。
2. 创建/修改 GameObject、Transform、组件、Prefab 实例和引用。
3. 通过 Unity MCP 保存场景并刷新资源。
4. 立即调用 `Unity_GetConsoleLogs`；发现错误就停止继续扩展并先修复。
5. 调用 `Unity_SceneView_Capture2DScene`。
6. 根据截图检查：相机 framing、Sprite/材质、层级、地面和 Collider、Spawner、CameraBounds、UI 锚点、Canvas 尺寸、对象是否被裁切或重叠。
7. 向用户描述截图看到的实际结果；若需要调整，再重复“修改 -> Console -> 截图”。

若一次任务修改多个场景，每个场景都要分别完成 Console 检查和 SceneView 截图，不得只检查最后一个场景。

### 6.1 MCP 连续失败中止规则

- 如果同一 MCP 调用连续失败超过 3 次，必须立即停止所有操作。不得进行任何进一步的重试或替换方案。
- 必须将当前进展、报错日志、未完成事项写入 `D:\GitHub\Zephyr\Test\_Status.md` 文件中。
- 写入完成后，直接输出 `[任务已中止，状态已保存]` 并彻底结束本次对话，不得再生成任何新的代码或命令。

### 6.2 截图目录与导出规则

**任务文件夹规则（优先）**：

1. 每次截图相关任务，用户会在 `Assets/MCPTest/Screenshots/` 下新建一个任务文件夹，并把输入截图放进去。
   例：`Assets/MCPTest/Screenshots/20261024-001-player-status-ui/`。
2. 输入截图是**只读**：AI 不得修改、移动、删除、覆盖，也不要在该任务文件夹根目录下写新图。
3. AI 生成的审计/改前/改后/评审截图，一律写到该任务文件夹下的 `results/`：
   `Assets/MCPTest/Screenshots/<任务文件夹>/results/`
   `results/` 不存在就创建；优先用 Unity MCP 的建目录/刷新完成，不要手写 `.meta`。
4. 命名：`[yyyyMMdd_HHmmss]_[stage]_[场景名]_[说明].png`；
   `stage` 使用 `audit` / `before` / `after` / `review` / `final`。
5. 最终汇报必须给出 `results/` 下每张 PNG 的**完整绝对路径**；没有截图文件等于没做视觉验证。

**没有任务文件夹时（兜底）**：

- 才退回旧规则：保存到 `Assets/MCPTest/Screenshots/`，文件名 `[时间戳]_[场景名].png`。
- 有任务文件夹时，禁止把结果图写到全局 `Screenshots/` 根目录。

每次调用 `Unity_SceneView_Capture2DScene` 后，仍必须用 `Unity_Camera_Capture` 或 Editor 脚本导出 PNG；禁止只提供文字描述而不保存截图文件。

## 7. 自测门禁

根据改动类型至少执行以下检查：

- 代码：Unity 编译/脚本编译，检查 Console Error 和关键运行时路径。
- Prefab：打开 Prefab、检查根对象、组件、引用、Animator、Collider 和材质。
- 资源：检查导入类型、切片、pivot、PPU、动画帧、排序层和引用。
- 状态机：检查状态是否加入 TransitionTable、入口/出口条件是否存在、动作和动画是否绑定。
- 场景：SceneView 截图、层级检查、相机检查、关键对象激活状态和引用检查。
- UI：以 `960 x 540` 检查布局、Localization、按钮可交互状态和 Popup Prefab 替换能力。
- 战斗：至少检查一次输入、状态转换、命中框、伤害/资源/受击反馈和恢复流程。

不能运行 Unity 或 Play Mode 时，必须执行可用的静态检查/脚本编译，并明确告诉用户 Unity 运行时验证未完成；不能把“代码看起来正确”当成完成。

## 8. Git 和文件安全

- 开始工作前检查 `git status --short`，保留用户已有修改；不使用 `git reset --hard`、`git checkout --` 或宽范围删除来清理工作区。
- 不提交 `Library/`、`Temp/`、构建输出、截图缓存、日志或临时导出物，除非仓库明确要求。
- 不修改与当前任务无关的文件、包版本、设计文档或生成的元数据。
- 删除或移动资源前先确认引用关系；优先使用 Unity MCP 的可恢复/可追踪操作，并在回报中说明删除内容。
- 任何自动生成的 `.meta` 必须保留合法 GUID；不能复制文件后制造重复 GUID。

## 9. 用户回报格式

完成后用简洁但可核验的方式汇报：

- 改动了什么，列出关键脚本、Prefab、场景和 SO。
- 使用了哪些资源生成/Unity 工具流程。
- `Unity_GetConsoleLogs` 的结果：Error 数量、重要 Warning 和是否已清零。
- 每个改动场景的 `Unity_SceneView_Capture2DScene` 结果及实际截图观察。
- 做了哪些自测，哪些只能静态验证。
- 尚未完成的部分、残余风险和用户需要在 Unity 中确认的事项。

禁止声称“已测试”“已截图”“已绑定”而没有对应工具结果或文件证据。

## 10. Unity MCP 工具路由

本项目同时连接了两套 Unity MCP。为避免把不同版本的接口、项目根目录或资源生成管线混用，必须固定使用以下路由：

- **图片生成**：只使用新的 `mcp__unityMCP.generate_image`。
- **测试运行**：允许且固定使用新的 `mcp__unityMCP__run_tests`（EditMode / PlayMode）。这是旧 MCP 之外唯一的明确例外；不需要再向用户申请。
- **其他所有 Unity 操作**：统一使用旧的 `mcp__unity_mcp` 工具，包括场景、GameObject、组件、Prefab、脚本、Tilemap、Animator、资源导入、Console 检查、SceneView 截图和 Play Mode。

执行 Unity 任务时必须遵守：

1. 不得因为某个工具调用失败，就自动把同一个操作切换到另一套 MCP。
2. 开始任务前先用负责该类操作的 MCP 确认项目根目录；当前项目根目录为 `D:\GitHub\Zephyr_v3\Zephyr(AI)\Zephyr`。
3. 一个操作只能由一套 MCP 完成。图片生成/测试运行完成后，后续导入、检查、场景引用和验证仍使用旧的 `mcp__unity_mcp`。
4. 工作更新中注明当前使用的 MCP 路由；最终汇报中说明图片生成、测试运行和其他 Unity 操作分别使用了哪套 MCP。
5. 继续遵守本文件的连续失败熔断规则，不得以切换 MCP 作为重试或替代方案。
6. 测试运行只允许 `mcp__unityMCP__run_tests`；不得因为测试失败或超时，把场景/Prefab/资源等其他操作切到新 MCP。

## 11. 多模型 AI 工作流（`.ai/`）

- 本仓库新增 `.ai/` **三档**多模型编排：**Tier 0/1 的指挥用本家 DeepSeek（`deepseek-account / deepseek-flash`）**；Tier 2 才让 GPT-6-astra 看架构、Claude Opus 5.5 实现、GPT-6.1-sol 评审。档位由 DeepSeek 指挥按 `.ai/checklists/tiers.md` 判断，不确定时升一档。
- Tier 0/1 的机械段可交常驻 `worker-deepseek`（复用同一会话，减少重复上下文）；GPT/Claude 只在 Tier 2、`gptPlan` 或 `gptReview` 时按需调用。
- Tier 1 采用 lean：控制器自己写计划/审 diff + 跑门禁，workflow 只跑 Claude；用 `tier-report.ps1` 盯门禁失败率/回滚率/平均返工，超阈值就把该 pattern 升到 Tier 2。
- 使用该流程前，先读 [`.ai/README.md`](.ai/README.md) 与 [`.ai/roles/orchestrator.md`](.ai/roles/orchestrator.md)。
- 该流程是**编排层**，不替代本文件；冲突时以本文件为准。
- 该流程不改变第 10 节的 Unity MCP 路由：测试运行固定用 `mcp__unityMCP__run_tests`，其他 Unity 操作仍只用 `mcp__unity_mcp`。
- 当用户说“按 `.ai` 流程 / 多模型流程 / 架构-实现-评审”时，先读 `.ai/README.md` 与 `.ai/roles/orchestrator.md`，再按 `.ai/workflows/unity-task.workflow.js` 执行。
- 当前交接见 `.ai/state/handoff.md`。

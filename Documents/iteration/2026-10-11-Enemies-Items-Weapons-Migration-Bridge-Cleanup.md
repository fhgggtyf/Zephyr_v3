# Enemies / Items / Weapons Migration Bridge Cleanup

日期：2026-10-11  
阶段：领域迁移桥清理  
项目：Zephyr Unity 2D Metroidvania

## 1. 结论

本轮不需要额外项目信息。现有代码、程序集、Prefab 和架构测试已经足够确定迁移范围。

本阶段已完成并验收成功：

- Zephyr.Enemies 不再依赖 Zephyr.Items 或 Zephyr.Weapons。
- Zephyr.Items 不再依赖 Zephyr.Weapons。
- 掉落框架不再直接引用 WeaponSO。
- BossRewardDrop 不再直接引用 WeaponSO 或 WeaponPickup。
- WeaponPickup Prefab 和 Boss Prefab 的脚本引用仍可由 Unity 解析。

## 2. 原始问题

原来的依赖桥为：

    Zephyr.Enemies -> Zephyr.Items
    Zephyr.Enemies -> Zephyr.Weapons
    Zephyr.Items   -> Zephyr.Weapons

具体耦合来源：

- DropEntry 和 DropResult 直接持有 WeaponSO。
- EnemySO 通过 DropTableSO 被迫依赖 Items 程序集。
- WeaponPickup 位于 Items 目录，但同时依赖 WeaponController、WeaponSO 和 Persistence。
- BossRewardDrop 位于 Enemies 程序集，并直接引用 WeaponSO、WeaponPickup。

这使敌人奖励、物品掉落和武器运行时之间形成了领域实现级引用，而不是稳定合同级引用。

## 3. 实施内容

### 3.1 掉落数据移到 Runtime

移动并保留原脚本 GUID：

- Assets/Scripts/Core/Items/DropSystem.cs -> Assets/Scripts/Core/Runtime/Items/DropSystem.cs
- Assets/Scripts/Core/Items/DropTableSO.cs -> Assets/Scripts/Core/Runtime/Items/DropTableSO.cs

DropEntry 和 DropResult 的 WeaponSO 字段改为稳定的 WeaponId 字符串：

- DropSystem 不再引用 Zephyr.Core.Weapons。
- DropSystem 只负责确定性掉落计算和 ID 结果输出。
- 武器资产解析不再属于掉落算法的职责。

现有序列化命名空间 Zephyr.Core.Items 保持不变，以降低已有类型引用的风险；程序集归属已经变为 Zephyr.Core.Runtime。

### 3.2 WeaponPickup 移入 Weapons 程序集

移动并保留原脚本 GUID：

- Assets/Scripts/Core/Items/WeaponPickup.cs -> Assets/Scripts/Core/Weapons/WeaponPickup.cs

脚本的公开 namespace 和类型名保持不变：

- Zephyr.Core.Items.WeaponPickup

这样保留 Unity Prefab 中既有类型识别，同时让依赖 WeaponController 和 WeaponSO 的实现归入 Weapons 程序集。Weapons 程序集新增对 Persistence 的显式依赖，用于保留拾取后写入 RunData 的既有行为。

### 3.3 增加 Core Contract

新增：

- Assets/Scripts/Core/Contracts/Interfaces/IWeaponPickup.cs

BossRewardDrop 现在只依赖 IWeaponPickup，并通过 Unity 组件接口解析拾取物：

- Enemies 不再引用 Zephyr.Core.Items。
- Enemies 不再引用 Zephyr.Core.Weapons。
- BossRewardDrop 不再知道 WeaponSO 具体类型。
- WeaponPickup 实现 IWeaponPickup，并继续使用 Prefab 上已有的武器配置。

### 3.4 删除程序集桥接引用

修改：

- Assets/Scripts/Gameplay/Enemies/Zephyr.Enemies.asmdef
- Assets/Scripts/Core/Items/Zephyr.Items.asmdef
- Assets/Scripts/Core/Weapons/Zephyr.Weapons.asmdef

删除结果：

- Zephyr.Enemies 删除 Zephyr.Items、Zephyr.Weapons 引用。
- Zephyr.Items 删除 Zephyr.Weapons 引用。
- Zephyr.Weapons 保留对 Core、Combat、Runtime、Contracts，并显式增加 Persistence。

## 4. 架构门禁

新增架构测试：

- DomainMigrationBridgesAreRemoved

测试检查：

- 三个 asmdef 不再包含迁移桥引用。
- DropSystem 不包含 WeaponSO 或 Zephyr.Core.Weapons。
- BossRewardDrop 不包含 WeaponSO，并使用 IWeaponPickup。

既有 PhaseTwoDomainAssembliesExistWithExpectedDirection 测试同步更新为当前目标依赖方向。

## 5. 序列化与资产检查

- DropTableSO 和 DropSystem 的移动通过旧版 Unity MCP AssetDatabase 路由完成，未手写 Unity YAML。
- 两个脚本的 .meta GUID 保持不变。
- WeaponPickup.prefab 组件仍解析为 Zephyr.Core.Items.WeaponPickup。
- Boss.prefab 组件仍解析为 Zephyr.Gameplay.Enemies.BossRewardDrop。
- 当前 Assets/ScriptableObjects/DropTables 目录没有已创建的 DropTableSO 资产。
- BasicEnemy.asset 和 BossEnemy.asset 的 _drops 当前均为 null，因此本轮没有现存掉落表资产需要进行字段迁移。
- WeaponPickup.prefab 既有武器引用保持可解析，未改动 Prefab 序列化内容。

## 6. 验证结果

### 6.1 Unity 编译与 Console

- Unity Editor：IsPlaying=false。
- Unity Editor：IsCompiling=false。
- Unity Editor：IsUpdating=false。
- 本轮修改后的编译错误已清零。
- 未发现 C# Exception、MissingReference、Serialization、YAML parse 或 Missing Script 错误。

Console 中唯一仍出现的项目运行时提示是：

    There are no audio listeners in the scene. Please ensure there is always one audio listener in the scene

这是前一轮已存在的 MainMenu 场景 AudioListener 配置问题，不是本轮迁移桥清理引入的问题。

Unity MCP 自身的 WebSocket、Discovery 和工具日志也仍存在，但属于工具链环境提示，不属于项目编译错误。

### 6.2 架构 EditMode 测试

程序集：Zephyr.Architecture.Tests  
结果：

    35 total / 35 passed / 0 failed / 0 skipped

新增门禁 DomainMigrationBridgesAreRemoved 通过。

### 6.3 业务 EditMode 测试

程序集：Zephyr.Tests.EditMode  
结果：

    5 total / 5 passed / 0 failed / 0 skipped

### 6.4 MainMenu 运行时烟测

执行：

    Load Initializer
    Enter Play Mode
    等待 Initializer -> Persistent -> MainMenu
    Stop Play Mode

结果：

- Active Scene 到达 Assets/Scenes/MainMenu/MainMenu.unity。
- MainMenuRoot、MainMenuCamera、MainMenuUI、Menu、SaveSlotOverlay、SettingsOverlay 和 ConfirmationPopup 均可创建。
- 程序集移动没有破坏启动链。
- Play Mode 期间没有新增脚本错误、Missing Script 或序列化错误。
- 停止 Play Mode 后运行时场景对象正常卸载。

本轮没有修改正式场景对象，因此不需要 SceneView 截图，也没有生成新的场景截图文件。

## 7. 统一架构评估

### 评分：9.4 / 10

相较于迁移桥仍存在时，迁移完整度和领域隔离度得到提升：

- 依赖方向：9.5
- 模块边界：9.4
- 运行时契约清晰度：9.3
- 序列化风险控制：9.2
- 迁移完整度与整洁度：9.5
- 测试与门禁：9.5

扣分主要来自：

- WeaponPickup 仍保留 Zephyr.Core.Items namespace，这是为了兼容已有序列化类型名，不是程序集依赖；后续若要进一步整理命名空间，需要单独做 Unity 序列化迁移。
- WeaponPickup 仍直接承担拾取后保存 RunData 的表现层/持久化交界行为，Persistence 依赖已显式化，但还可以在后续 Save Gateway 专项中继续收窄。
- MainMenu 仍有既有 AudioListener 场景配置提示。

## 8. 验收结论

### 验收成功

完成度评估为高，可以将 Enemies / Items / Weapons 迁移桥从延期清单中移除。普通内容开发可以继续使用现有 EnemySO、DropTableSO、WeaponPickup 和 WeaponSO 管线；新代码不得重新引入领域实现级反向引用。

下一步不需要继续拆这条迁移桥。后续架构工作应优先处理 AudioListener 场景配置、WeaponPickup 的 Save Gateway 收窄，以及仍在审查记录中的其他独立债务。

## 9. Unity MCP 路由

- 资源移动、Prefab 检查、场景、Play Mode、Console 和脚本校验：使用旧版 mcp__unity_mcp。
- 架构和业务 EditMode 测试：使用新版 mcp__unityMCP。
- 本轮没有调用图片生成能力，没有生成或修改正式图片资源。

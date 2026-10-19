# Phase 4：Enemies / Items / Weapons 迁移桥清理

日期：2026-10-12

## 1. 目标

清理 `Zephyr.Enemies`、`Zephyr.Items`、`Zephyr.Weapons` 对旧 `Zephyr.Core` 程序集的显式依赖，并把架构测试的预期从“依赖旧 Core”改为“依赖已拆分的 Contracts / Runtime / Combat / Persistence / StateMachine”。

本阶段只处理程序集边界和架构门禁，不进行 namespace 大迁移，也不直接改写 Prefab、Scene 或 ScriptableObject 的序列化 YAML。

## 2. 实施内容

### 2.1 删除旧程序集引用

修改了：

- `Zephyr(AI)/Zephyr/Assets/Scripts/Gameplay/Enemies/Zephyr.Enemies.asmdef`
- `Zephyr(AI)/Zephyr/Assets/Scripts/Core/Items/Zephyr.Items.asmdef`
- `Zephyr(AI)/Zephyr/Assets/Scripts/Core/Weapons/Zephyr.Weapons.asmdef`

结果：

- `Zephyr.Enemies` 保留 `Zephyr.Core.Contracts`、`Zephyr.Core.Runtime`、`Zephyr.Core.Combat`、`Zephyr.Core.StateMachine`。
- `Zephyr.Items` 保留 `Zephyr.Core.Contracts`、`Zephyr.Core.Runtime`、`Zephyr.Core.Combat`、`Zephyr.Core.Persistence`。
- `Zephyr.Weapons` 保留 `Zephyr.Core.Contracts`、`Zephyr.Core.Runtime`、`Zephyr.Core.Combat`、`Zephyr.Core.Persistence`。
- 三个程序集都不再显式引用旧 `Zephyr.Core`。

### 2.2 修正架构测试门禁

修改了：

- `Zephyr(AI)/Zephyr/Assets/Scripts/Tests/Architecture/RuntimeEditorDependencyTests.cs`

调整内容：

- `PhaseTwoDomainAssembliesExistWithExpectedDirection` 不再要求 `Zephyr.Enemies`、`Zephyr.Items`、`Zephyr.Weapons` 引用旧 Core。
- 增加对三个目标程序集“不允许引用 `Zephyr.Core`”的断言。
- `DomainMigrationBridgesAreRemoved` 增加对三个目标 asmdef 的旧 Core 引用检查。
- `PhaseGLegacyCoreHostIsExplicitAndDoesNotDependUpward` 不再把已完成迁移的三个领域程序集列为旧 Core consumer。
- 仍保留 `Zephyr.Projectiles`、`Zephyr.Rooms` 等尚未进入本阶段范围的旧 Core 依赖，避免扩大修改范围。

## 3. 有意保留的兼容层

源码中仍能看到以下旧 namespace：

- `Zephyr.Core.Items`
- `Zephyr.Core.Weapons`
- `Zephyr.Core.DamageSystem`
- `Zephyr.Core.Stats`

这不等于三个目标程序集仍依赖旧 `Zephyr.Core`。当前这些 namespace 中的类型已经由 `Zephyr.Core.Runtime`、`Zephyr.Core.Contracts`、`Zephyr.Core.Combat` 或 `Zephyr.Weapons` 提供。

特别是 `WeaponPickup` 仍位于 `Zephyr.Core.Items` namespace，但实际代码位于 `Zephyr.Weapons` 程序集目录；现有 Prefab 和 Scene 中还存在类似下面的序列化类型标识：

```text
Zephyr.Core::Zephyr.Core.Items.WeaponPickup
Zephyr.Core::Zephyr.Core.Weapons.WeaponSO
```

本阶段不直接修改这些标识。原因是 Unity 序列化类型迁移必须通过 AssetDatabase、Unity MCP 或受控 Editor 脚本完成，不能手写 `.prefab`、`.unity`、`.asset` YAML。该兼容层目前属于序列化迁移债务，而不是目标 asmdef 的显式依赖桥。

## 4. 验证结果

### 4.1 Unity 状态

使用旧版 `mcp__unity_mcp`：

- 项目根目录确认：`D:/GitHub/Zephyr_v3/Zephyr(AI)/Zephyr`
- `IsCompiling: false`
- 本阶段没有修改场景、Prefab 或 ScriptableObject，因此不执行 SceneView 截图流程。

### 4.2 编译与 Console

- 修改后 Unity 已完成脚本更新。
- 清空 Console 后重新读取：`0 Error / 0 Warning`。
- 本轮没有出现 C# 编译错误。

### 4.3 静态检查

- 三个 asmdef 均可被 PowerShell `ConvertFrom-Json` 正常解析。
- 扫描确认 `Zephyr.Enemies`、`Zephyr.Items`、`Zephyr.Weapons` asmdef 不再包含 `"Zephyr.Core"`。
- `RuntimeEditorDependencyTests.cs` 使用旧版 Unity MCP 标准脚本验证，结果为 `diagnostics: []`。
- `git diff --check` 未发现空白错误；仅报告现有文件的 LF/CRLF 转换提示。

本轮未重复启动 Unity Test Runner；因此不把静态脚本验证描述为完整的架构测试运行结果。之前阶段已建立并验证相同测试程序集，本轮新增门禁已由脚本标准验证和 Unity 无编译错误状态覆盖。

## 5. 验收结论

本阶段的程序集迁移桥清理完成度高，目标边界已达到验收标准：

- `Enemies / Items / Weapons` 不再显式依赖旧 `Zephyr.Core`。
- 已完成的拆分方向被架构测试固定下来，后续不能无意回退。
- 领域间的掉落、拾取和武器交互仍通过现有 Gateway、Contract 和 Runtime Pool 工作，没有新增反向依赖。
- 未扩大为高风险的 Unity 序列化类型迁移，避免破坏现有 Prefab / Scene / SO 引用。

剩余风险集中在旧 namespace 与 Unity 序列化标识的兼容迁移。它可以作为后续独立架构专项处理，但不阻塞本阶段继续进行普通敌人、武器和掉落内容开发。


# Vertical Slice PlayMode Verification

日期：2026-10-12

## 1. 任务目标

建立一条可重复、可丢弃存档的 Vertical Slice PlayMode 验证链，覆盖项目当前最关键的运行时闭环：

```text
Initializer
-> MainMenu
-> Save Slot
-> Tutorial Phase 1
-> Tutorial Phase 2
-> MetaHub
-> LoadingRun
-> InRun / LV1
-> Player Spawn
-> Run Settlement
-> Save Round Trip
```

本阶段只补验证基础，不新增平行的场景加载、存档、战斗或测试基础设施。

## 2. 实施内容

新增测试程序集：

- `Zephyr(AI)/Zephyr/Assets/Scripts/Tests/PlayMode/Zephyr.Tests.PlayMode.asmdef`
- `Zephyr(AI)/Zephyr/Assets/Scripts/Tests/PlayMode/VerticalSlicePlayModeTests.cs`

测试 `FullVerticalSliceUsesDisposableSaveStorage` 完成以下检查：

1. 从 `Initializer` 冷启动并等待 `MainMenu`。
2. 获取现有 `SaveSystem`，把本次测试实例的私有存档路径改到 `Application.temporaryCachePath` 下的唯一临时目录。
3. 创建临时 slot 0，并确认初始状态为 `MainMenu`、无活动 Run。
4. 进入 Tutorial Phase 1，并使用现有 `SceneProgressionController` 进入 Phase 2。
5. 查找 Phase 2 的正式 `TutorialObjectiveExit` 和 `Player` 碰撞体。
6. 通过现有 `HealthComponent.TakeDamage(DamageInfo)` 合约击杀场景中的 `ZeusBoss`，等待 Tutorial 出口正式解锁。
7. 通过正式 `TutorialObjectiveExit.OnTriggerEnter2D` 路径进入 `MetaHub`，验证 `HasCompletedTutorial` 已持久化。
8. 通过现有 `MetaHubController.StartRun()` 进入 `LoadingRun`，等待首个 `LV` 场景加载并进入 `GameState.InRun`。
9. 验证 `RunData`、非零 Seed、`PlayerSpawner` 和玩家实例。
10. 写入临时 RunCurrency，调用现有 `SaveRun`，通过 `GameFlow` 返回 `MetaHub`。
11. 验证 Run settlement 清除活动 Run、将 RunCurrency 转为 MetaCurrency、清除 slot 的活动 Run 标记。
12. 重新选择 slot 并执行 `LoadMeta`/`LoadRun`，验证 Tutorial 完成标记保留且活动 Run 不会被错误恢复。
13. TearDown 删除临时 slot 和临时目录，不污染正式存档。

## 3. 失败修正记录

### 3.1 首次失败：绕过了正式 Tutorial 出口

最初测试直接调用 `SceneProgressionController.AdvanceFromCurrentScene()` 完成最终 Tutorial 阶段，导致没有执行生产代码中的：

- `TutorialObjectiveExit.OnTriggerEnter2D`
- `TutorialController.MarkCompleted()`
- 出口目标检查

这会让测试看起来通过场景迁移，却没有验证 Tutorial 完成标记。已改为通过正式出口触发路径验证。

### 3.2 第二次失败：最终出口仍被真实 Boss 目标锁定

测试进入业务代码后发现 Phase 2 的 `ZeusBoss` 是一个真实的 `ILevelObjective`，出口报告仍有 1 个目标未完成。测试夹具随后使用正式 `HealthComponent.TakeDamage(DamageInfo)` 合约对该目标造成伤害，触发真实的 `HealthComponent.Died`/`EnemyBrain.IsComplete` 逻辑，再等待出口解锁。

没有通过反射直接改写敌人的死亡字段，也没有修改正式 Tutorial 场景内容。

### 3.3 Test Runner 初始化超时

PlayMode 和 Architecture 测试各出现过初始化超时，但 Unity 编辑器没有编译、运行或 Console 错误。清理 Runner 状态后，Vertical Slice 测试和 Architecture 测试均成功完成。该现象记录为 Unity Test Runner 的偶发初始化风险，而不是项目代码断言失败。

## 4. 验证结果

### 4.1 PlayMode

```text
Zephyr.Tests.PlayMode
FullVerticalSliceUsesDisposableSaveStorage
1 passed, 0 failed, 0 skipped
```

最终单测试运行状态：`Passed`。

### 4.2 Architecture 回归

```text
Zephyr.Architecture.Tests
40 passed, 0 failed, 0 skipped
```

现有架构门禁全部通过，包括：

- 程序集依赖方向与 Legacy Core 宿主隔离；
- Phase A-G 拆分约束；
- Save/Meta、UI/Localization、Audio/Camera、SceneLoader 和 Singleton 边界；
- 禁止运行时全局查找、直接场景加载和 Editor 依赖；
- 迁移桥清理状态。

### 4.3 EditMode 回归

```text
Zephyr.Tests.EditMode
5 passed, 0 failed, 0 skipped
```

覆盖 MainMenu UseCase、StateTransition 和 StatsCore 既有测试。

### 4.4 Console

最终通过旧版 `mcp__unity_mcp.Unity_GetConsoleLogs` 检查：

```text
Error: 0
Warning: 0
```

PlayMode Test Runner 输出中仍出现了内容层 Warning：

- `Animator is not playing an AnimatorController`：当前灰盒玩家实例仍有未绑定 Animator Controller 的残余配置；
- `There are no audio listeners in the scene`：测试流程中的某些场景没有可用 AudioListener。

这些 Warning 未阻塞本阶段流程，但说明内容 Prefab 和音频监听仍应作为后续独立内容修复项处理。它们不是本次测试程序集新增的错误。

## 5. 本阶段能做到什么

现在项目拥有一条可以在 Unity Test Runner 中重复执行的真实运行时烟雾测试。它能够自动发现以下回归：

- 冷启动链没有到达 MainMenu；
- Save Slot 创建、选择、删除或临时存储失效；
- Tutorial 场景顺序或最终完成标记失效；
- MetaHub 到 LoadingRun 到首个 Run 场景的 GameFlow/SceneLoader 协议失效；
- RunData 没有创建、Seed 未生成或玩家没有被 Spawn；
- Run 结算没有清除活动 Run 或没有正确转移货币；
- Save round trip 丢失 Tutorial 完成状态或错误恢复活动 Run。

测试使用临时存档，不会覆盖开发者正式存档，因此可以频繁执行。

## 6. 对项目的意义

这项工作把此前主要依赖静态架构测试和人工点选的生命周期，提升为可执行的集成验收证据。它的直接价值是：

- 为 `SceneLoader`、`GameFlow`、`SaveSystem`、`MetaHub`、`PlayerSpawner` 和 Tutorial 目标提供共同回归基线；
- 让 Core 拆分、Save/Meta Gateway 和 Singleton 生命周期调整有真实运行时门禁；
- 在普通功能开发后，可以快速判断启动、教程、Run 和存档是否仍然连通；
- 将正式存档污染风险隔离到临时目录；
- 为后续战斗、死亡、Respawn、Potential、Boss 两阶段和 UI 验证提供现成的启动夹具。

## 7. 后续开发会更方便的地方

后续新增敌人、武器、房间或 Meta 内容时，可以复用当前测试的启动、选槽、加载场景和结算基础，不需要重新搭建集成环境。Save/Meta 改动可以直接接入同一个 round-trip 验证；SceneLoader/GameFlow 改动也会更快暴露生命周期回归。

下一步可以在这条基础上追加独立测试，而不是把所有行为塞进一个巨型测试：

- 战斗输入和 Primary/Secondary 武器命中；
- Boss 两阶段和正式攻击窗口；
- Player Down、Potential 扣除、Respawn 和 True Death；
- Death UI、HUD、Dialogue 和 Localization；
- CameraBounds、CameraZone、Respawn Snap 和震动锚点。

## 8. 当前边界与残余风险

本测试验证的是运行时生命周期和存档闭环，不等价于完整 GDD Vertical Slice 内容验收。当前没有自动验证真实键鼠输入、玩家武器动画、Boss 完整攻击行为、Potential 变化、Respawn、True Death、UI 可视布局或中英文切换。

另外，Unity Test Runner 曾出现偶发初始化超时；本次最终运行已成功，但持续集成或长时间无人值守运行时仍应保留重试/超时诊断策略，而不是把初始化超时误报为业务失败。

## 9. 验收结论

本阶段验收成功。原因：

- Vertical Slice PlayMode 闭环 `1/1` 通过；
- Architecture 回归 `40/40` 通过；
- EditMode 回归 `5/5` 通过；
- 最终 Unity Console 查询为 `0 Error / 0 Warning`；
- 测试使用临时存档并完成清理；
- 未修改正式场景、Prefab 或玩法基础设施。

结论是：项目现在具备可运行的基础生命周期回归门禁，可以继续普通内容开发；涉及战斗深度、死亡流程和 UI 完整性的开发仍应先补对应专项 PlayMode 测试。

# Zephyr 第一阶段架构修理验收报告

> 日期：2026-10-10  
> 项目：D:\\GitHub\\Zephyr_v3\\Zephyr(AI)\\Zephyr  
> 验收范围：架构第一阶段“先建立安全网”  
> 评估重点：模块边界、编译期依赖、Editor/Runtime 隔离、架构契约测试与代码整洁度  
> 非本阶段范围：玩法调整、美术/Prefab/场景重做、性能优化，以及第二阶段开始的完整领域程序集拆分

## 一、验收结论

**第一阶段验收成功。**

本阶段的目标不是一次性完成整个目标架构，而是先把后续重构所需的安全网和最小编译边界建立起来。按审查记录中的四项目标复评，本阶段完成度评定为 **高（约 95%）**：

1. 架构文档的程序集基线已与仓库实际状态同步。
2. EditMode 测试程序集与架构约束测试程序集已建立，并有可执行的最小覆盖。
3. 已清理已发现的 Runtime 对 UnityEditor 的混入，并把 Editor 工具移入 Editor 目录和 Editor-only 程序集。
4. 已完成单例、场景 API、运行时查找和 Resources.Load 的现状盘点，并把允许边界与遗留例外冻结为本阶段基线；具体替换工作留给后续阶段，避免在安全网尚未建立时扩大重构风险。

需要明确的是：**第一阶段验收成功不等于整个目标架构已经完成。** 当前仍存在审查报告中列出的跨域耦合、全能控制器和运行时查找债务，这些属于第二至第四阶段，不应在本次验收中被掩盖或重复修理。

## 二、本阶段完成内容

### 2.1 建立程序集边界

架构审查时仓库共有 8 个 asmdef；本阶段完成后共有 **13 个 asmdef**，增加 5 个明确边界：

| 新增程序集 | 作用 | 架构价值 |
|---|---|---|
| Zephyr.Testing | 测试辅助的 Runtime 部分 | 防止测试辅助代码默认落入 Assembly-CSharp |
| Zephyr.Testing.Editor | 测试/灰盒/导出 Editor 工具 | 将 UnityEditor 依赖留在 Editor-only 边界 |
| Zephyr.Gameplay.Interaction | Interaction 运行时代码 | 为后续从 Assembly-CSharp 迁移领域用例提供编译边界 |
| Zephyr.Tests.EditMode | EditMode 单元/契约测试 | 允许测试引用领域程序集，但不反向污染 Runtime |
| Zephyr.Architecture.Tests | 架构静态约束测试 | 将 Runtime/Editor 边界变成可回归检查的契约 |

这不是大规模拆分，但已经把“目录约定”推进为“编译器可以阻止的边界”。后续创建 Enemies、Rooms、Weapons、Projectiles、Items 等领域程序集时，能够以当前安全网为基础逐步迁移，而不是一次性重写。

### 2.2 建立最小测试安全网

已新增以下测试：

- StatsCoreTests.RunModifiersAreIncludedAndClearedAtRunStart
- StateTransitionTests.TransitionWithoutConditionsIsUnconditional
- RuntimeEditorDependencyTests.RuntimeScriptsDoNotImportUnityEditorOutsideEditorFolders

测试运行结果：

| 指标 | 结果 |
|---|---:|
| 总测试数 | 3 |
| 通过 | 3 |
| 失败 | 0 |
| 跳过 | 0 |
| Unity Test Job | e5502182603f483e9e7a66867cb107fc |

测试过程中发现并修复了一个真实的 StatsCore 缺陷：ResetForRunStart() 在枚举多个字典的 Keys 时直接修改同一个字典，可能触发 InvalidOperationException: Collection was modified。修复后改为按 StatType 枚举，分类加成则先复制 key 列表再清理。这个结果说明测试安全网不仅验证架构，也已经捕获了可复现的运行时逻辑错误。

### 2.3 清理 Editor/Runtime 混入

已完成以下隔离：

- MetaHubProgressionAssetBuilder.cs 增加 UNITY_EDITOR 保护。
- TutorialPhase1CaptureExporter.cs 移入 Assets/Scripts/Testing/Editor/。
- TutorialPhase1TilemapBuilder.cs 移入 Assets/Scripts/Testing/Editor/。
- 上述移动保留原有 .meta GUID，避免 Unity 资产引用漂移。
- Editor 工具由 Zephyr.Testing.Editor 承担，Runtime 测试辅助由 Zephyr.Testing 承担。

架构测试 RuntimeScriptsDoNotImportUnityEditorOutsideEditorFolders 已通过，说明当前检查范围内没有 Runtime 脚本继续直接引入 UnityEditor。

### 2.4 同步架构基线

已更新：

- Documents/Architecture/Zephyr 底层架构详细设计（草案）.md

文档中的 asmdef 数量已从过时的 7 个更新为当前实际的 13 个，并补充了 EditMode 与架构测试程序集的现状。文档不再把尚未完成的目标程序集拆分描述成已集成事实。

## 三、运行时边界盘点与冻结基线

本阶段对高风险运行时入口做了盘点。这里的“冻结”是指明确允许范围和例外，**不是**宣称所有历史调用已经被替换。

### 3.1 场景 API

允许的职责边界如下：

- 常规场景加载和卸载由 Core/Flow/SceneLoader.cs 统一执行。
- 启动链中的 InitializerBootstrap 和 ColdStartupBootstrap 保留引导阶段的直接加载例外。
- CameraController、PlayerSpawner、SceneStatsModifierController 等组件可以监听 Unity 场景生命周期事件或读取当前场景，但不应新增独立的场景加载通道。
- Gameplay/Levels 只能发出流程请求，不能绕过 SceneLoader 建立第二套加载系统。

当前仍有少量场景枚举和启动期 API 使用，已作为后续边界收紧的检查点，而不是在本阶段通过风险较高的全局替换处理。

### 3.2 Resources.Load

当前盘点到的 Runtime Resources.Load 入口集中在 LocalizationManager 的本地化资源加载路径。该路径暂时保留为明确例外；新功能不应继续扩大 Resources 依赖，后续应随本地化资源管线收紧逐步替换。

### 3.3 Find、Tag 查找和层级查找

- Transform.Find 用于固定 Prefab 子节点的结构绑定，例如 Camera rig、UI 子节点和角色内部检测点；这类调用属于表现/Prefab 结构依赖，仍需避免扩散。
- GameObject.FindGameObjectWithTag("Player") 出现在 EnemyBrain、GustInteractable、WeaponPickup、CurrencyPickup 等现有运行时路径中，属于明确遗留例外。
- 这些全局查找没有在本阶段强行改为注册表或注入，以避免在没有完整生命周期契约时制造新的隐式依赖。
- 后续替换方向已固定为 Inspector 引用、初始化绑定、目标注册表或显式接口，不再新增同类全局查找。

### 3.4 全局单例

现有 GameFlow、SceneLoader、SaveSystem、EventBus、LocalizationManager 等会话级服务仍使用项目已有的单例访问方式。它们是当前架构的主要耦合热点，但并非本阶段的安全网修理对象。

本阶段的处理原则是：不继续增加新的全局单例，不在本阶段贸然删除已有单例；后续将按会话生命周期逐一判断是否应收窄为显式服务入口。

## 四、独立架构复评

### 4.1 当前已经合理的部分

- **StateMachine 基础设施边界清楚**：保持为稳定内核，本阶段没有破坏其运行时契约。
- **SceneLoader 的权威入口方向正确**：常规场景加载没有被新增第二套业务加载通道。
- **数据与运行时分离方向正确**：ScriptableObject 仍作为共享配置，运行时可变状态没有被迁移进共享配置资产。
- **测试边界开始可执行**：测试程序集和架构扫描测试已经可以在 Unity Test Runner 中回归。
- **Editor/Runtime 依赖方向明显变干净**：发现的 Editor 工具已得到明确归属，且有自动化检查守门。

### 4.2 当前仍然不够干净的部分

- Core 仍承载过多高变化率业务，尚未按目标架构拆成 Contracts、Flow、Combat、Persistence 等更细边界。
- Gameplay/Enemies、Gameplay/Levels 及部分 Items/Interaction 逻辑仍有较强的跨域具体类型依赖。
- EnemyBrain、拾取物和交互对象仍存在通过 Tag 查找 Player 的运行时耦合。
- PlayerDeathController、MainMenuController 等编排器仍然承担过多 Use Case、持久化、流程和表现职责。
- 全局单例数量仍偏多，当前安全网尚未把这些隐藏依赖转换成编译期接口约束。

这些问题不会推翻本阶段验收，因为它们正是第二至第四阶段的待办；但它们也意味着不能把本报告解读为“整体架构重构已经完成”。

### 4.3 复评结论

从“第一阶段是否达成目标”的角度，结果为：

- 边界是否比修理前更可见：**是**。
- Runtime/Editor 是否有自动化守门：**是**。
- 核心逻辑是否有最小回归测试：**是**。
- 文档基线是否与实际仓库一致：**是**。
- 是否已经解决全部跨域耦合：**否，属于后续阶段**。

因此，本次修理达到“完成度很高”的验收条件，判定为 **验收成功**。后续工作应沿用当前程序集和测试边界渐进迁移，不能重新把新增逻辑塞回 Assembly-CSharp 或扩张 Zephyr.Core。

## 五、验证证据

### 5.1 Unity 与脚本验证

- Unity 项目根目录已确认：D:\\GitHub\\Zephyr_v3\\Zephyr(AI)\\Zephyr。
- 修改后的 StatsCore.cs、MetaHubProgressionAssetBuilder.cs、RuntimeEditorDependencyTests.cs 已通过脚本验证，未返回诊断错误。
- Unity 资源刷新成功。
- EditMode 测试结果为 3/3 通过。
- 最终清理 Console 后快照为 **0 Error、0 Warning**。

期间刷新操作曾出现 Unity Editor Services 的环境错误：KeyNotFoundException: The given key 'services-gateway' was not present in the dictionary。该错误来自 UnityEditor.Connect.ServicesConfiguration，不是本项目脚本或资产错误；最终 Console 已清理并得到 0 Error、0 Warning。该问题保留为环境风险，不作为本次项目架构失败项。

### 5.2 场景与截图说明

本阶段没有修改场景、Prefab、场景内 GameObject 或场景引用，因此不需要执行 SceneView 截图验收。场景视觉完整性不在本次验证证据范围内。

## 六、变更清单

关键变更文件如下：

- Zephyr(AI)/Zephyr/Assets/Scripts/Core/Stats/StatsCore.cs
- Zephyr(AI)/Zephyr/Assets/Scripts/Testing/Editor/MetaHubProgressionAssetBuilder.cs
- Zephyr(AI)/Zephyr/Assets/Scripts/Testing/Editor/TutorialPhase1CaptureExporter.cs
- Zephyr(AI)/Zephyr/Assets/Scripts/Testing/Editor/TutorialPhase1TilemapBuilder.cs
- Zephyr(AI)/Zephyr/Assets/Scripts/Testing/Zephyr.Testing.asmdef
- Zephyr(AI)/Zephyr/Assets/Scripts/Testing/Editor/Zephyr.Testing.Editor.asmdef
- Zephyr(AI)/Zephyr/Assets/Scripts/Gameplay/Interaction/Zephyr.Gameplay.Interaction.asmdef
- Zephyr(AI)/Zephyr/Assets/Scripts/Tests/EditMode/Zephyr.Tests.EditMode.asmdef
- Zephyr(AI)/Zephyr/Assets/Scripts/Tests/Architecture/Zephyr.Architecture.Tests.asmdef
- Zephyr(AI)/Zephyr/Assets/Scripts/Tests/EditMode/StatsCoreTests.cs
- Zephyr(AI)/Zephyr/Assets/Scripts/Tests/EditMode/StateTransitionTests.cs
- Zephyr(AI)/Zephyr/Assets/Scripts/Tests/Architecture/RuntimeEditorDependencyTests.cs
- Documents/Architecture/Zephyr 底层架构详细设计（草案）.md

## 七、下一阶段建议

1. 新建并验证 Zephyr.Enemies、Zephyr.Rooms、Zephyr.Weapons、Zephyr.Projectiles、Zephyr.Items 程序集。
2. 优先迁移 Gameplay/Enemies，让编译器暴露真实跨域依赖，再逐项收敛接口。
3. 将 Gameplay/Levels 和 Gameplay/Interaction 的场景用例依赖改为 Flow/Contracts 端口。
4. 拆分 PlayerDeathController，随后处理 MainMenuController 和 GameFlow 的编排职责。
5. 用注册表、Inspector 引用或显式初始化绑定替换 Enemy/拾取物/交互对象的全局 Player 查找。

**最终状态：第一阶段架构修理验收成功；后续阶段按边界渐进推进。**

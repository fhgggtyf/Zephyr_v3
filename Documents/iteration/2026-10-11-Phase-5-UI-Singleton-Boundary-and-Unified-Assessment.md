# Zephyr 第五阶段：UI / 单例边界修理与统一架构评分报告

- 日期：2026-10-11
- 项目：D:\GitHub\Zephyr_v3\Zephyr(AI)\Zephyr
- 本阶段范围：完成第三阶段延期的 MainMenuView、SettingsPresenter、主菜单用例测试，以及 PlayerDeathController 的单例访问收敛。
- Unity 路由：非图片 Unity 操作统一使用旧版 mcp__unity_mcp。

## 一、阶段验收结论

**第五阶段验收成功。**

本阶段完成度评估为 **9.3/10（完成度很高）**。第三阶段遗留的 UI 职责过载已经被切开：
MainMenuController 现在主要负责流程编排和 Unity 事件入口，MainMenuView 负责界面构建、
显示状态和按钮事件，SettingsPresenter 负责本地化设置边界；主菜单业务继续通过
MainMenuUseCase 和 ISaveSlotGateway 运行。

死亡流程中的 PlayerSpawner、GameFlow、SaveSystem 单例访问也已经集中到
PlayerDeathController.ResolveDependencies() 这一处组合根。控制器后续只依赖缓存的服务引用，
并通过 ISaveSlotGateway.SaveRun() 保存运行数据，不再在业务方法中反复读取全局单例。

## 二、本阶段实际修理

### 2.1 UI 展示边界

新增：

- Zephyr(AI)\Zephyr\Assets\Scripts\Core\Flow\Scenes\MainMenuView.cs

职责：

- 构建主菜单 Canvas、主菜单按钮、存档槽位面板、设置面板和确认弹窗。
- 持有 UI 控件引用并刷新本地化文本、存档槽位状态和按钮可用性。
- 通过事件向控制器发布用户动作，不直接调用 GameFlow、SaveSystem 或 LocalizationManager。

调整：

- MainMenuController 从原先约 500 行的 UI / 业务混合控制器压缩为流程编排门面。
- 保留原有公开按钮方法和确认弹窗入口，避免破坏现有 Scene/Prefab Event Binding。

### 2.2 设置与本地化边界

新增：

- Zephyr(AI)\Zephyr\Assets\Scripts\Core\Localization\ILocalizationGateway.cs
- Zephyr(AI)\Zephyr\Assets\Scripts\Core\Flow\Scenes\SettingsPresenter.cs

调整：

- LocalizationManager 实现 ILocalizationGateway。
- SettingsPresenter 只依赖本地化接口，负责文本读取、格式化、Locale 切换和变更通知。
- MainMenuController 只在启动时解析一次 LocalizationManager.Instance，之后通过 Presenter 工作。

### 2.3 死亡流程单例边界

调整：

- PlayerDeathController 新增 _playerSpawner、_gameFlow、_saveGateway 缓存依赖。
- 单例解析统一放入 ResolveDependencies()。
- ISaveSlotGateway 增加 SaveRun()，避免死亡流程为了保存数据重新依赖具体 SaveSystem 类型。
- PlayerDeathController 的受伤统计、潜能恢复、潜能保存、倒地次数读取均通过缓存网关完成。

### 2.4 测试与编译回归修复

新增：

- Zephyr(AI)\Zephyr\Assets\Scripts\Tests\EditMode\MainMenuUseCaseTests.cs

覆盖：

- 教学未完成时继续进入 Tutorial。
- 教学完成且存在 Run 时继续进入 LoadingRun。
- 缺少存档网关时回到 MainMenu。

同时修复了本次 Unity 刷新暴露的实际编译回归：

- CurrencyPickup 重复定义 OnDisable()。
- PlayerDownUseCase 将 StateMachine 命名空间误当作类型。
- PlayerDeathController 缺少 HurtReceiver 命名空间引用。
- Zephyr.Testing.Editor 缺少对 Zephyr.Weapons 的程序集引用。
- TutorialObjectiveExit 使用已废弃的 FindObjectsSortMode 重载。

## 三、统一评分标准

本次统一使用同一套 10 分制，不再把“阶段验收成功”直接等同于“整体 9 分以上”。

| 维度 | 权重 | 评分关注点 |
|---|---:|---|
| 边界清晰度 | 25% | UI、用例、服务、基础设施是否有明确职责；是否存在重复编排 |
| 依赖方向 | 25% | 高层是否依赖接口/端口；领域模块是否反向依赖具体实现 |
| 生命周期与单例安全 | 20% | 单例是否只在组合根解析；是否存在隐式查找、越权场景加载或重复服务入口 |
| 可测试性与门禁 | 15% | 纯用例是否可替换依赖；架构规则是否能阻止回归；测试是否可执行 |
| 迁移完整度与整洁度 | 15% | 是否仍有显式迁移桥、序列化风险、临时兼容代码和未收敛基础设施 |

评分解释：

- 9.0–10.0：边界稳定，剩余问题已经局部化且有明确迁移合同，不会阻塞普通功能开发。
- 8.0–8.9：主架构可用，但仍存在会持续放大维护成本的跨域耦合。
- 7.0–7.9：已有局部模块化，但生命周期、依赖方向或测试门禁仍不稳定。
- 7.0 以下：不建议继续叠加功能，应先处理基础架构。

## 四、四阶段重新统一评分

| 阶段 | 统一评分 | 本次复评说明 |
|---|---:|---|
| 第一阶段 | 9.2/10 | Runtime/Editor 边界、基础 asmdef 和初始架构门禁已稳定。 |
| 第二阶段 | 9.0/10 | 领域程序集拆分完成，但仍保留显式 Enemies/Items/Weapons 迁移桥。 |
| 第三阶段 | **9.3/10** | 本阶段补齐 UI View、Settings Presenter、主菜单用例测试和死亡流程组合根。 |
| 第四阶段 | 9.1/10 | Player 生命周期事件、查找禁令和场景加载门禁已稳定。 |
| **四阶段后的整体架构** | **8.9/10** | 普通功能开发风险已显著下降，但迁移桥、全局单例收敛和资产序列化债务仍拉低整体分。 |

整体分没有强行上调到 9.0 以上，原因是整体评分还必须覆盖未完成的跨领域迁移和已有资产风险；
本阶段可以把第三阶段提升到 9.3，但不能把仍存在的整体性债务计入“已完成”。

## 五、验证结果

### 5.1 脚本验证

以下脚本通过 Unity_ValidateScript standard 验证，诊断为空：

- MainMenuController.cs
- MainMenuView.cs
- SettingsPresenter.cs
- ILocalizationGateway.cs
- PlayerDeathController.cs
- PlayerDownUseCase.cs
- MainMenuUseCaseTests.cs
- LocalizationManager.cs

### 5.2 Unity Console

- 本阶段代码编译错误：已清零。
- 仍存在的编辑器输出不是本阶段 C# 编译错误：
  - WeaponSO / EnemySO 资产中存在迁移后缺失类型引用，属于前阶段 asmdef / 序列化资产迁移债务。
  - Unity Editor ServicesConfiguration 报告缺少 services-gateway，属于编辑器环境服务配置异常。
- 这两类问题已记录为残余风险，未被伪装成“全部 Console 清零”。

### 5.3 场景与资源验证

- 本阶段没有修改 Scene、Prefab、ScriptableObject 内容或场景对象引用。
- 因此不需要新增 SceneView 截图，也没有生成新的场景 PNG。
- Unity 已为新增 C# 文件生成对应 .meta 文件。

### 5.4 测试限制

- 新增测试源码已通过脚本标准验证。
- Unity Test Runner 的独立执行仍受当前编辑器测试发现/缓存和 ServicesConfiguration 环境问题影响，本报告不虚报新增测试已经运行通过。

## 六、剩余风险与下一步放行判断

### 已经足够安全、可以继续普通功能开发的部分

- MainMenu 的 UI、设置、本地化和存档业务边界已经分开。
- PlayerDeathController 的单例读取已集中到组合根。
- Runtime 全局 Player 查找、越权场景加载和 Runtime Resources.Load 仍受架构测试保护。
- 新增主菜单用例测试提供了第一批无需启动 Persistent 场景的业务验证。

### 仍不建议与普通玩法功能混做的高风险专项

- Zephyr.Enemies -> Zephyr.Items/Zephyr.Weapons 和 Zephyr.Items -> Zephyr.Weapons 迁移桥。
- 全局单例全面收敛，尤其是仍在 Player/UI/Meta 交互路径中使用的旧入口。
- Core 进一步拆分为 Contracts、Flow、Combat、Persistence，避免 Core 继续膨胀。
- LocalizationManager 的 Resources.Load 替换，以及现有 WeaponSO / EnemySO 序列化引用迁移。

### 最终放行判断

**可以开始普通功能开发，但不应把上述四项高风险基础设施混入普通玩法提交。**

如果下一轮继续追求“整体 9.0+”，优先顺序应为：

1. 修复并验证 WeaponSO / EnemySO 的序列化类型迁移。
2. 设计并落地 Reward/Drop Core Contract，逐步移除领域迁移桥。
3. 继续收敛剩余全局单例，再拆分 Core 的职责程序集。
4. 最后替换本地化资源加载适配器，并重新运行全量架构测试。

**本阶段最终判定：第三阶段 UI / 单例边界修理完成度很高，验收成功；整体架构当前为 8.9/10，尚未诚实达到 9.0+。**

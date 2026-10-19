# Phase 1：收窄 Legacy Core

日期：2026-10-12  
范围：`Zephyr.Core` legacy 宿主的职责收窄、Localization/UI/Dialogue 程序集边界、消费者显式依赖和架构门禁。  
验收原则：不改变场景、Prefab、ScriptableObject 或玩法行为；保留现有 namespace，避免本阶段引入序列化类型名迁移。

## 1. 结论

Phase 1 验收成功，完成度评估为高。

本阶段把 `Zephyr.Core` 从包含 Localization、UI 和 Dialogue 实现的兼容宿主，收窄为只承载三个基础配置/工具文件：`GameConstants`、`GameSettingsSO` 和 `GridAligner`。Localization、通用 UI 基础设施和 Dialogue 现在拥有独立 asmdef，消费者通过显式程序集引用连接，不再依靠 legacy Core 的实现集合。

架构质量从 Phase 0 的独立基线 **8.1/10** 提升到本阶段估算 **8.4/10**。这不是最终评分：Core 的直接入边仍存在，singleton、场景发现和运行时 UI 构造仍需后续阶段处理。

## 2. 实施内容

### 新增程序集

- `Assets/Scripts/Core/Localization/Zephyr.Core.Localization.asmdef`
  - 承载现有 `Zephyr.Core.Localization` namespace 下的 LocalizationManager、ILocalizationGateway、LocalizedBehaviour。
  - 只依赖 `Zephyr.Core.Runtime` 和 `Unity.Localization`。
- `Assets/Scripts/Core/UI/Zephyr.Core.UI.asmdef`
  - 承载 GameplayTimePause、RuntimeUIFactory、UIManager、ConfirmationPopupView。
  - 当前无项目程序集依赖。
- `Assets/Scripts/Core/UI/Dialogue/Zephyr.Core.Dialogue.asmdef`
  - 承载 DialogueManager、DialogueSO、DialogueTrigger、DialogueView。
  - 依赖 Runtime、Meta、Persistence、Localization、Core.UI、Inputs 和 Unity Localization/Input System。

现有 namespace 未改动。这样可以在收窄程序集边界的同时，避免 Prefab/SO 在本阶段因为类型全名变化而失效。

### 收窄与显式引用

- `Assets/Scripts/Core/Zephyr.Core.asmdef`
  - references 从多个 Core/业务程序集收窄为 `[]`。
  - `autoReferenced` 保持 `false`。
  - 实际归属文件从原先宿主内容收窄为 3 个文件。
- `Zephyr.Core.Flow` 增加 Localization 和 Core.UI 显式依赖。
- `Zephyr.Player` 增加 Localization、Core.UI 和 Core.Dialogue 显式依赖。
- `Zephyr.Gameplay.Interaction` 增加 Localization 和 Core.UI 显式依赖。
- `Zephyr.UI` 增加 Localization 显式依赖。
- `Zephyr.Testing.Editor` 增加 Localization 显式依赖，覆盖其直接 AddComponent 的组件基类引用。
- `DialogueManager.cs` 增加 `using Zephyr.Core.UI`，明确使用 GameplayTimePause 的新程序集归属。

### 架构测试更新

`Assets/Scripts/Tests/Architecture/RuntimeEditorDependencyTests.cs` 已更新：

- 不再要求 legacy Core 反向持有 Contracts、Runtime、Meta、Persistence 实现依赖。
- Flow 测试改为验证其自身显式持有 Localization/UI 依赖。
- 增加 `PhaseOneLegacyCoreHostHasNoImplementationReferences`，验证 Core 宿主无实现依赖、新程序集存在且 Dialogue 依赖方向正确。
- Phase G 的 namespace 门禁区分 `Zephyr.Core.Localization` 和 `Zephyr.Core.UI`，不再把新程序集误判为 legacy Core。

## 3. 依赖账本变化

| 指标 | Phase 0 | Phase 1 | 结果 |
|---|---:|---:|---|
| asmdef 总数 | 27 | 30 | 新增 3 个职责程序集 |
| `Zephyr.Core` 直接出边 | 多个 Core/业务实现依赖 | 0 | Legacy 宿主不再向实现层反向依赖 |
| `Zephyr.Core` 归属 C# 文件 | 14 | 3 | Localization/UI/Dialogue 已移出宿主归属 |
| `Zephyr.Core` 直接入边 | 12 | 12 | 本阶段未冒险修改常量/历史 namespace 消费者 |
| 架构测试 | 36/36 | 37/37 | 新增门禁通过 |
| EditMode 测试 | 5/5 | 5/5 | 无回归 |

本阶段没有把 Core 的所有消费者一并删除，因为不少内容仍通过 `GameConstants` 或历史 namespace 使用 Core。继续强行删除这些入边会把 Phase 1 变成 namespace/序列化迁移，扩大回归面；该部分保留到 Contracts namespace 和常量归属专项处理。

## 4. 验证结果

- Unity 项目根目录：`D:\GitHub\Zephyr_v3\Zephyr(AI)\Zephyr`。
- Unity 版本：`6000.5.6f1`。
- 架构测试：`37/37 passed`。
- EditMode 测试：`5/5 passed`。
- 最终 Unity Console：Error `0`、Warning `0`。
- Unity Editor：未播放、未编译、未更新。
- `git diff --check`：通过；仅包含本阶段 asmdef、Dialogue 显式 using、架构测试和 Phase 0/Phase 1 文档变化。

迁移过程中曾出现 4 个编译错误，均已定位并修复：DialogueManager 缺少 Core.UI 依赖/using，以及 Testing.Editor 缺少 Localization 直接依赖。修复后重新运行两组测试并确认 Console 清零。

本阶段没有修改场景、Prefab、ScriptableObject 或资源引用，因此不执行 SceneView 截图流程；序列化类型全名和 namespace 保持不变。

## 5. 保留风险与延期工作

以下问题没有在本阶段混做：

1. `Zephyr.Core` 仍有 12 个直接入边，主要来自常量、历史 namespace 和兼容引用。
2. Contracts 下 28 个文件仍使用历史 `Zephyr.Core.*` namespace。
3. 运行时仍有大量 singleton 读取，下一阶段继续做组合根/服务解析收敛。
4. Gameplay HUD、MainMenu 和部分 Popup 仍运行时创建 UI 树，后续单独做 Prefab/View 化。
5. Tutorial、RunStart、Camera、SceneContext 仍存在全局对象发现，后续单独做显式场景绑定。
6. 本阶段只做 EditMode/架构验证，没有执行完整启动链和 PlayMode 场景回归。

## 6. 下一阶段建议

下一步优先进入 **Phase 2：组合根与 Singleton 读取预算**：

- 先处理 Flow、Player、Interaction 中的服务解析点。
- 保留 Persistent/GameManager 的生命周期协议，不新增平行 Manager。
- 把 `*.Instance` 读取集中到初始化边界，向业务组件传递接口/Gateway。
- 以本阶段的 112 次基线为起点，要求新增业务代码 singleton 读取为 0，并逐步降低存量。

Phase 1 完成的是“legacy Core 不再反向拥有业务实现”；它没有声称整个 Core 依赖债务已经清零。该边界已经通过编译、架构测试和 EditMode 测试验证，可以安全进入下一阶段。

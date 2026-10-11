# Audio / Camera 独立化评估与实施验收

日期：2026-10-11  
项目：Zephyr 2D Metroidvania  工作区：`D:\GitHub\Zephyr_v3\Zephyr(AI)\Zephyr`

## 1. 结论

本轮评估结论：Audio 和 Camera 均已达到可以从 legacy `Zephyr.Core` 独立出来的条件，且已经完成低风险程序集拆分。

- `Zephyr.Core.Audio`：独立化完成。
- `Zephyr.Core.Camera`：独立化完成。
- 原有命名空间、脚本路径和脚本 GUID 保持不变，未破坏场景或 Prefab 中的 MonoBehaviour 序列化引用。
- 两个新程序集均设置为 `autoReferenced: false`，新代码不会通过隐式引用获得它们。
- `Zephyr.Core` 不再承担 Audio/Camera 的程序集归属；它继续作为 Localization、UI、共享常量和兼容性服务的 legacy host。

验收判定：**通过**。本轮整体完成度评估为 **9.3/10**，达到“完成度很高”的验收标准。

## 2. 评估范围

### Audio

检查对象：

- `Assets/Scripts/Core/Audio/AudioManager.cs`
- `AudioEventSO`
- `AudioBus`
- Persistent 场景中的 AudioManager owner
- `Zephyr.Core.Runtime.Lifecycle.SingletonLifecycle` 依赖

结论：Audio 是 Persistent 生命周期下的独立基础设施。它没有依赖 Flow、Player、Enemies、Items、Weapons 或 UI 的具体类型；运行时唯一的项目程序集依赖是生命周期治理所在的 `Zephyr.Core.Runtime`。

### Camera

检查对象：

- `Assets/Scripts/Core/Camera/CameraController.cs`
- `Assets/Scripts/Core/Camera/CameraBounds.cs`
- `Assets/Scripts/Core/Camera/CameraZone.cs`
- GameManager 场景中的 Cinemachine 组合
- PlayerSpawner 和 PlayerImpactController 的 Camera 消费点

结论：Camera 是 GameManager/Gameplay 场景边界上的独立适配器。它没有编译时依赖 Player、GameManager、Enemies 或 Flow；Cinemachine 通过反射解析，避免把第三方相机实现传播到其他程序集。Player 只通过显式 `Zephyr.Core.Camera` 引用使用 Camera API。

## 3. 实施内容

### 新增程序集

- `Assets/Scripts/Core/Audio/Zephyr.Core.Audio.asmdef`
  - 仅显式依赖 `Zephyr.Core.Runtime`。
  - `autoReferenced: false`。
- `Assets/Scripts/Core/Camera/Zephyr.Core.Camera.asmdef`
  - 无项目程序集依赖。
  - `autoReferenced: false`。

### 更新程序集边界

- `Assets/Scripts/Player/Zephyr.Player.asmdef`
  - 增加对 `Zephyr.Core.Camera` 的显式引用。
  - 保留现有 `Zephyr.Core` 引用，因为 Player 仍有其他历史兼容依赖；本轮没有扩大拆分范围。

### 更新架构门禁

`RuntimeEditorDependencyTests.cs` 增加并调整了以下保护：

- 验证 Audio/Camera 专用 asmdef 存在、非自动引用且不依赖 legacy `Zephyr.Core`。
- 验证导入 `Zephyr.Core.Audio` / `Zephyr.Core.Camera` 命名空间的程序集必须显式引用对应程序集。
- 防止未来因为字符串扫描规则过时而把已经独立的 Audio/Camera 误判为 legacy Core 消费者。

### 同步设计文档

`Documents/Architecture/Zephyr 底层架构详细设计（草案）.md` 已同步：

- 程序集数量从 25 更新为 27。
- 增加 Audio 独立程序集状态。
- 更新 Camera 独立程序集状态。
- 修正 Phase G 对“Audio/Camera 仍属于 legacy Core host”的过期描述。

## 4. 耦合度与架构评分

评分口径沿用前几轮：依赖方向、编译耦合、运行时耦合、生命周期耦合、序列化风险、可测试性和未来扩展成本，满分 10 分。

| 模块 | 编译耦合 | 运行时耦合 | 生命周期耦合 | 序列化风险 | 架构合理度 | 结论 |
|---|---:|---:|---:|---:|---:|---|
| Audio | 9.5 | 9.0 | 8.8 | 9.5 | 9.2 | 可独立，保留 Persistent owner |
| Camera | 9.5 | 8.4 | 8.3 | 9.4 | 8.9 | 可独立，保留 GameManager/场景适配职责 |
| 本轮整体 | 9.5 | 8.7 | 8.5 | 9.5 | **9.3** | 验收通过 |

### 扣分原因

1. CameraController 仍通过 `FindObjectsByType` 和 `GameManager.unity` 场景路径选择主相机。这是当前场景生命周期协议的一部分，但比组合根显式注入更隐式。
2. CameraController 使用 Cinemachine 类型名反射，降低了程序集编译耦合，但引入了第三方类型名变化时的运行时风险。
3. AudioManager 仍保留静态 `Instance` 访问面；本轮确认没有发现新的业务调用扩散，但完整移除单例 API 需要另立生命周期/Gateway 专项。
4. Player 当前仍依赖 legacy `Zephyr.Core` 的其他类型，因此本轮只解除 Camera 的程序集归属，不能宣称 Player 已完全脱离 Core。

这些扣分项不阻塞本轮独立化，因为它们不再表现为 Audio/Camera 被 legacy Core 承载的编译耦合，也没有引入新的上层反向依赖。

## 5. 验证记录

### Unity 编译与 Console

- Unity 根目录确认：`D:/GitHub/Zephyr_v3/Zephyr(AI)/Zephyr`
- Unity 版本：`6000.5.6f1`
- `IsCompiling: false`
- 新 asmdef 导入成功，并生成唯一 GUID：
  - Audio：`b16df275521fdca4eb9d8b31c4bed866`
  - Camera：`085daae7e5277e74588877d763bac323`
- 最终清理后的 Console：Error `0`，Warning `0`。

本轮未修改场景、Prefab、ScriptableObject 或 GameObject 序列化数据，因此按项目规则不执行 SceneView 截图，也不存在需要导出的场景截图文件。

### 测试

- `Zephyr.Architecture.Tests`：**36/36 passed**，0 failed，0 skipped。
- `Zephyr.Tests.EditMode`：**5/5 passed**，0 failed，0 skipped。
- 启动链烟测：从 `Initializer` 进入 Play Mode 后，活动场景到达 `MainMenu`，无项目编译错误。

说明：期间一次旧版 MCP 不支持的 `GetLoadedScenes` 参数调用在 Console 产生了工具错误；该错误不是项目编译或运行时错误，已清除 Console，并在最终清理后确认 Error/Warning 均为 0。

## 6. 设计判断

### 已经可以做的事

- 在 `Zephyr.Core.Audio` 内继续增加音频基础设施，例如音量快照、音乐切换适配、AudioEvent 播放策略，但仍必须保持对 Gameplay 类型零依赖。
- 在 `Zephyr.Core.Camera` 内继续完善相机边界、Zone 行为、震动和 Cinemachine 适配，但不要把 Player/Enemy 业务规则放进 Camera。
- 新业务程序集可以显式引用 Audio 或 Camera；不得重新通过 `Zephyr.Core` 间接承载。

### 仍需单独立项的事

- 把 AudioManager 的静态访问面替换为 Gateway/组合根注入。
- 把 CameraController 的场景查找改为 GameManager 组合根显式绑定。
- 直接依赖 Cinemachine 程序集并移除反射适配，除非先确认包升级策略和程序集边界。
- 移除 Player 对 legacy `Zephyr.Core` 的剩余兼容依赖。

以上工作会改变生命周期或广泛程序集依赖，不能作为普通内容开发的顺手重构。

## 7. 结论与后续状态

Audio/Camera 独立化本轮完成，程序集边界、编译依赖、序列化兼容和测试门禁均已验证。当前 legacy Core 的剩余内容已经缩小为明确的 Localization/UI/共享兼容 host，后续可以继续处理剩余单项架构债务，但必须保持“专用程序集显式引用、禁止隐式 Core 依赖”的规则。


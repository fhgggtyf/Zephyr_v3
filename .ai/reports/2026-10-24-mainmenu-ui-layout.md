# 任务总结：MainMenuUI 布局 / 换行 / 背景 Tier 2 修复

> 日期：2026-10-24
> Tier：2（external plan，跳过 GPT-astra 架构）
> planSource：`external`
> 模型：指挥 DeepSeek（`deepseek-account/deepseek-flash`）· 架构 无（跳过 Astra）· 实现 **Claude 连续失败 2 次 → 按 524/fallback 规则由指挥 DeepSeek 完成本轮（非目标模型）** · 评审 GPT-6.1-sol（subagent 复核，因 workflow 的 review 阶段因实现失败未触发）
> 审计/spec：无 Tier 0 审计（用户明确问题已明确）；spec = 用户 Plan + 指挥只读侦察补齐的 `plan.unityAssets`
> 输入任务文件夹：`Zephyr(AI)/Zephyr/Assets/MCPTest/Screenshots/2026_10_24_MainmenuUI/`

## 1. 结果

- 一句话：`menu.save.details` 中英文改为真实换行，存档槽卡片缩短并把 Status 文字挪到卡片与 Close 之间，主菜单背景换成 `MainMenuBackground.png`（居中 / 高 540 / preserveAspect / 不 stretch）。
- 完成项：
  1. 两个 localization JSON 第 22 行的字面 `\n` 改为 JSON 转义的真实换行（C# fallback 未动）。
  2. SaveSlot1/2/3 卡片 300→240 高、y −4→+26；每个 Details 104→86 高、(0,24)→(0,19)。
  3. SaveSlotPanel/Status 由 (0,32)/840×32 挪到 (0,100)/840×44，落在 slot 卡片（世界底 y=176）与 Close（世界 y 33..71）之间，不再重叠、不贴底。
  4. Background 换 `MainMenuBackground.png`：anchor 居中 (0.5,0.5)、sizeDelta(960,540)、preserveAspect=1、Simple、颜色白；旧 `AlothaiPortraitBackdrop` 只停用（未删除）。
- 未完成 / P2：
  - TMP 迁移（用户明确另开 B 单，本单不做）。
  - PlayMode `VerticalSlicePlayModeTests` 失败为**既有问题**（见 §4），需另单处理场景链切换。

## 2. 改动文件

| 文件 | 改动 |
|---|---|
| `Zephyr(AI)/Zephyr/Assets/Resources/Localization/strings.en_US.json` | 第 22 行 `menu.save.details`：`\\n` → `\n`（1 行） |
| `Zephyr(AI)/Zephyr/Assets/Resources/Localization/strings.zh_CN.json` | 第 22 行 `menu.save.details`：`\\n` → `\n`（1 行） |
| `Zephyr(AI)/Zephyr/Assets/Scenes/MainMenu/MainMenu.unity` | 21 行字段：Background RectTransform+Image、AlothaiPortraitBackdrop m_IsActive、SaveSlot1/2/3、3×Details、Status（未手写 YAML，经 Unity MCP 一次性 Editor 脚本 + `EditorSceneManager.SaveScene` 落盘） |
| `Zephyr(AI)/Zephyr/Assets/ArtResources/Sprites/MainMenu/` | 新背景 sprite + `.meta`；场景按 GUID 引用，**必须随场景一起提交** |

`git diff --stat`：3 个 tracked 文件 23 insertions / 23 deletions；另有未跟踪背景 sprite（`MainMenuBackground.png` + `.meta` + folder `.meta`），已加入暂存区，未 commit。`git diff --check` 干净。

## 3. 验证

- 编译 / Console：脚本编译通过；`Unity_GetConsoleLogs` **errorCount=0 / warningCount=0**（唯一一次 Warning 是 Unity 账号 API 网络超时，与本单无关）。
- EditMode / PlayMode：EditMode **63/63 passed**（`mcp__unityMCP__run_tests`）；PlayMode `FullVerticalSliceUsesDisposableSaveStorage` **FAIL**，见 §4 的 A/B 结论。
- 960×540 / 视觉：Game View 固定 960×540；before/after 中英文截图逐张 `read_image` 看过：换行 3 行、卡片缩短、Status 单行完整显示在 Close 之上、背景为居中 540×540 方形（左右露出相机近黑底色，属需求预期）。
- 运行时回读：EN `'Profile 73ac1b3f<LF>No active run<LF>Last played: 2026-10-23 22:48'`，ZH `'档案 73ac1b3f<LF>无进行中的冒险<LF>上次游玩：2026-10-23 22:48'` → 真实换行，无字面 `\n`。
- 截图（results 绝对路径）：
  - `D:\GitHub\Zephyr_v3\Zephyr(AI)\Zephyr\Assets\MCPTest\Screenshots\2026_10_24_MainmenuUI\results\20261024_033500_before_MainMenu_background-en.png`（before，主菜单背景；已按内容重命名为 `background-en`，GUID 不变）
  - `D:\GitHub\Zephyr_v3\Zephyr(AI)\Zephyr\Assets\MCPTest\Screenshots\2026_10_24_MainmenuUI\results\20261024_033700_before_MainMenu_save-slots-en.png`
  - `D:\GitHub\Zephyr_v3\Zephyr(AI)\Zephyr\Assets\MCPTest\Screenshots\2026_10_24_MainmenuUI\results\20261024_033730_before_MainMenu_save-slots-zh.png`
  - `D:\GitHub\Zephyr_v3\Zephyr(AI)\Zephyr\Assets\MCPTest\Screenshots\2026_10_24_MainmenuUI\results\20261024_035200_after_MainMenu_save-slots-en.png`
  - `D:\GitHub\Zephyr_v3\Zephyr(AI)\Zephyr\Assets\MCPTest\Screenshots\2026_10_24_MainmenuUI\results\20261024_035230_after_MainMenu_save-slots-zh.png`
  - `D:\GitHub\Zephyr_v3\Zephyr(AI)\Zephyr\Assets\MCPTest\Screenshots\2026_10_24_MainmenuUI\results\20261024_035300_after_MainMenu_background-en.png`
  - `D:\GitHub\Zephyr_v3\Zephyr(AI)\Zephyr\Assets\MCPTest\Screenshots\2026_10_24_MainmenuUI\results\20261024_035340_after_MainMenu_background-zh.png`
- probe / 机器可读证据：`...\results\probe_rects.txt`（逐节点 RectTransform / Image / Text 回读 + 几何换算 + 运行时文本）。
- 输入图只读：`input.png` SHA256 `f4995f62…` 与开工时一致，未被修改。

## 4. 门禁 / 评审

- static-gate：`-SelfTest` PASS；`-FilesFile .ai\state\changed-files.txt`（3 文件）→ **PASS**，0 FAIL / 1 WARN（WARN = Unity 序列化资产被改动，确认是 `mcp__unity_mcp__*` 改的）。
- workflow-guard：pre `996C5453…`；post **unchanged**（exit 0）。
- review verdict：**PASS**（GPT-6.1-sol，独立 subagent 复核；blocking 0）
  - nonBlocking：① PlayMode 垂直切片为既有失败、A/B 已证；② static-gate 的 Unity 序列化资产确认 WARN。
  - 评审原话（notes）："仅审 plan.files 与给定证据。中英文换行、本地化、存档布局、Status 位置和背景均符合 spec；960x540 截图无裁切或重叠。"
- 流程偏差（如实记录）：workflow Tier 2 两次调用 Claude 实现者都失败——**第一次是 `fastllm API error (400): prompt is too long / CONTEXT_WINDOW_EXCEEDED`，第二次返回 `I'm ready to help you. What would you like to work on?`（退化响应）**。按 524/fallback 规则等 120s 重试仍失败 → **本轮实现由指挥 DeepSeek 完成（非目标模型）**；workflow 的 review 阶段因实现失败未触发，改为独立 `subagent`（`fastllm/gpt-6.1-sol`）复核，评审仍是真正独立的模型与工具证据。**这次不是 524，是 context 超限；下次遇到 400/CONTEXT_WINDOW_EXCEEDED 应缩 payload 或直接 fallback，不要按 120s 重试。**
- 测试结果 / 已知失败（重要）：
  - PlayMode `Zephyr.Tests.PlayMode.VerticalSlicePlayModeTests.FullVerticalSliceUsesDisposableSaveStorage` 失败：`GameFlow did not reach Tutorial with scene Tutorial_Phase_1.`（在 MainMenu→Tutorial 切换处 45s 超时）。
  - 做了 A/B 对照：把本单三个文件**还原成 HEAD 基线**（其余未提交改动保持不变）后重跑同一 PlayMode 测试，**在同一位置以同一条消息失败**；恢复改动后再跑仍失败。→ 判定为**既有/环境问题，不是本单回归**。本单未触碰 `GameFlow` / `SceneLoader` / `GameManager`。
  - **2026-10-24 13:26 当前工作区复跑**：EditMode job `0037782474394e3b826ab882c74f5f5b` = **63/63 passed**（0.7s）；PlayMode job `16499f62d1f24f43bd37d8c3f11b9cef` = 1/1 failed，约 51.7s，仍是 `GameFlow did not reach Tutorial with scene Tutorial_Phase_1.` → EditMode 全绿、PlayMode 红，“run_tests 全绿”确认**未达成**，不是报告陈旧。
  - 该失败在开工前未被记录（`.ai/state/handoff.md` 只记录了 3 个 EditMode 历史失败且已修），建议单独开单。

## 5. 残余风险

- PlayMode 垂直切片测试仍红（既有问题），所以"run_tests 全绿"这条验收**未达成**；本单范围内 EditMode 全绿、Console 0 Error。
- 新背景是 1024×1024 的 1:1 贴图，按需求"居中 + height 540 + preserveAspect + 不 stretch"渲染为 540×540 正方形，左右露出相机近黑底色，人物主体位于菜单面板之后。这是需求的直接结果；若美术想要"右侧立绘 + 全屏底"，需要另开单改构图（本单按 spec 执行，未自行改成 stretch 或右对齐）。
- 旧 `AlothaiPortraitBackdrop` 是**停用**而非删除，`AlothaiPortrait` 资源与节点仍在场景中，可随时恢复。
- 证据命名已统一为 `20261024_033500_before_MainMenu_background-en.png`，报告与 results 目录一致。
- `MainMenu.unity` 工作副本为 LF、仓库按 CRLF 检出（`git` 提示 `LF will be replaced by CRLF`），diff 只含 21 行字段，无整文件换行污染。

## 6. 提交 / 产物

- commit：**未提交**（用户明确"不要 commit"）。
- **P0 资源依赖**：`MainMenu.unity` 引用 `MainMenuBackground.png`（GUID `752eedc1a2b04d54fb02b3646b446c80`），该 sprite 原先未跟踪；已把 folder `.meta` + `MainMenuBackground.png` + `.meta` 加入暂存区，未 commit。提交场景时必须带上这三项，否则 clean checkout 会丢图。
- `results/`：7 张 PNG + `probe_rects.txt`（含 `.meta`，Unity 自动生成）。
- `.ai/ledger` / `.ai/reports`：本文件；`.ai/state/changed-files.txt`；tier 指标一行。
- 快照：`.ai/snapshots/20261024-033557-before-mainmenu-layout-tier2/`。

## 7. 下一步

1. 背景构图用户已确认（540×540 居中、左右黑边符合预期），不需要另开单。
2. 另开单修 PlayMode `VerticalSlicePlayModeTests`（MainMenu→Tutorial 场景链 45s 超时，已证既有）。
3. B 单：MainMenu TMP 迁移（本单明确不做）。
4. 提交时只 stage 本单 3 个 tracked 文件 + 背景 sprite/meta（已 stage）；截图/`.ai` 流程产物按既有约定处理。

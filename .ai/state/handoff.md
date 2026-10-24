# §〇 当前交接

- **我是谁**：Zephyr 多模型 AI 工作流的指挥（**本家 DeepSeek `deepseek-account / deepseek-flash` · max**）。
- **干到哪**：MainMenuUI 布局/换行/背景 Tier 2 已做完（本单 1/2/3/5，item 4 TMP 留给 B 单）；**未 commit**。3 个 tracked 文件已改，背景 sprite/meta 已 stage；EditMode **63/63**，PlayMode `FullVerticalSliceUsesDisposableSaveStorage` 既有失败；static-gate PASS；GPT-6.1 独立评审 PASS；workflow-guard post unchanged。
- **本单路径**：external plan（跳过 Astra）→ Claude 两次失败 → DeepSeek 实现 → 独立 GPT 评审。Claude 第一次是 `400 prompt is too long / CONTEXT_WINDOW_EXCEEDED`，第二次是退化 `I'm ready...`；**不是 524，不能按 120s 重试**。
- **当前工作树**：
  - `MainMenu.unity` + `strings.en_US.json` + `strings.zh_CN.json` 已改，未 stage。
  - 背景 `Assets/ArtResources/Sprites/MainMenu/`（folder `.meta` + png + png `.meta`）已 **stage**；场景 GUID `752eedc1a2b04d54fb02b3646b446c80`，提交必须带上，否则 clean checkout 丢图。
  - `.ai` 流程改动/报告/模板未提交；`results/` 未跟踪；PlayerStatusPanel 7 文件已提交 `ce7c123`。
- **下一步**：
  1. 提交 MainMenu：3 个 tracked 文件 + 背景 sprite/meta（已 stage）；不要 `git add -A`。
  2. 另开单修 PlayMode `VerticalSlicePlayModeTests`（MainMenu→Tutorial 45s 超时，已 A/B 证明既有）。
  3. TMP B0→B5：B0 先 TMP Essentials + 中文 Font Asset，再逐批迁；模板 `.ai/templates/tmp-migration.md`。
  4. 验收措辞改成“EditMode 全绿；PlayMode 既有失败需 A/B”，不再写笼统“测试全绿”。
- **卡点 / 已知坑**：
  - Claude 400 context：缩 brief/plan、截图最多 2 张、AGENTS 只读相关节；仍超限 fallback Astra/DeepSeek。
  - Claude 返回 `I'm ready...`：视为失败，不重试同一 payload。
  - PlayMode 场景链超时是既有问题；当前工作区复跑仍红（job `16499f62…`），不要归到 UI 单。
  - 背景构图已确认：1:1 贴图按 height 540 + preserveAspect = 540×540 居中，左右黑边符合预期。
- **细节在哪**：
  - MainMenu 报告：`.ai/reports/2026-10-24-mainmenu-ui-layout.md`
  - PlayerStatus 报告：`.ai/reports/2026-10-24-player-status-ui.md`
  - 审计：`.ai/audits/2026-10-24-player-status-ui.md`
  - 定档：`.ai/checklists/tiers.md` · UI 侦察：`.ai/checklists/ui-recon.md`
  - 模板：`.ai/templates/ui-fix-direct.md` · `.ai/templates/tmp-migration.md`
  - 流程：`.ai/workflows/unity-task.workflow.js`
  - 台账：`.ai/ledger/dispatch-ledger.jsonl` · `.ai/ledger/tier-metrics.jsonl`
  - 快照：`.ai/snapshots/20261024-033557-before-mainmenu-layout-tier2/`

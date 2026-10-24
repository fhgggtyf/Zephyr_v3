# UI 修复 / 优化任务模板

> 用法：复制本文件到新会话，替换 `<...>`。先填第 1 节，再按阶段 A/B 跑。
> UI 默认 **Tier 2**；问题不清楚时先做阶段 A 审计；已有明确 spec 可删掉 A，直接用 B 的 `plan`。
> **问题已经完全明确、不需要审计：用 [ui-fix-direct.md](ui-fix-direct.md)**。
> 输入图只读；AI 结果只写 `<任务文件夹>/results/`；不 commit。

## 1. 任务

- 任务名：`<一句话>`
- 任务文件夹：`D:\GitHub\Zephyr_v3\Zephyr(AI)\Zephyr\Assets\MCPTest\Screenshots\<yyyy_mm_dd>_<task>\`
- 输入图：`<任务文件夹>\input.png`（只读）
- 目标页面 / Prefab：`<PlayerStatusPanel.prefab / 场景 / 控制器>`
- 现象 / 目标：
  1. `<可观察的问题，例如 background 太小 / 文字被裁 / 左右没分开>`
  2. `<...>`
- 不做：`<P2：HUD 遮挡 / element 中文化 / 每帧 Refresh 等>`
- 验收：`960x540`、中英文、Console 0 Error、EditMode 全绿（PlayMode 既有失败需 A/B）、before/after 截图写 `results/`

## 2. 阶段 A：Tier 0 审计（只读）

按 `.ai` 流程做一次 UI 审计，先不要改文件。

1. 先读 `AGENTS.md`、`.ai/README.md`、`.ai/roles/orchestrator.md`、`.ai/checklists/ui-recon.md`、`.ai/checklists/unity-acceptance.md`。
2. 用 `read_image` 看输入图；读相关 Prefab / 场景 / Controller / Localization。
3. 输出 spec：改哪些文件、锚点 / 尺寸 / 间距 / Key、`960x540` 验收、风险与 P2。
4. 结果图写 `results/`；不改文件、不 commit。
5. 审计完成后停，把 spec 给我确认；确认后再进阶段 B。

## 3. 阶段 B：Tier 2 实施

按阶段 A 的 spec 做 Tier 2 实施，不要 commit。

- **plan**：用审计 spec 作为 `plan`（`classification` / `files` / `acceptance` / `needsApproval`），传 `args.plan` **跳过 Astra**。
- **brief**：≤10 行（目标 / 文件 / 约束 / 验收 / 证据）；实现者只收 brief + 紧凑 plan，不吞审计全文。
- **开工**：preflight 快照；`workflow-guard.ps1 -Mode pre`。
- **实现**：Prefab / 场景只用旧 `mcp__unity_mcp__*`；测试用 `mcp__unityMCP__run_tests`；不手写 YAML；只改 `plan.files`。
- **验收**：`static-gate.ps1 -SelfTest` + `-FilesFile`；Console 0 Error；EditMode 全绿；PlayMode 既有失败需 A/B 证明，其余失败即回归；`960x540` before/after 截图写 `results/`。
- **收尾**：`workflow-guard.ps1 -Mode post`；写 `.ai/reports/<date>-<task>.md` 总结；不 commit。
- **汇报**：planSource、changedFiles、review verdict、static-gate、run_tests、截图绝对路径、残余风险。

## 4. 返回格式

```text
审计：spec + 结果图路径
实施：planSource / changedFiles / review / static-gate / run_tests / 截图 / 残余风险
```

## 5. brief 示例

```text
目标：PlayerStatusPanel 底板盖住世界、Portrait 不发暗、Tooltip 不盖字。
文件：PlayerStatusPanel.prefab + GameManager.unity + 3 个 UI 脚本 + 2 个 localization JSON。
约束：不改 P2、不手写 YAML、只走旧 MCP。
验收：960x540 probe 对齐审计 §6；中英文不截断；Console 0 Error；EditMode 全绿/PlayMode A/B。
```

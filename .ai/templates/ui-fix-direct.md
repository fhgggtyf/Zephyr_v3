# UI 修复 / 优化：问题明确，直接 Tier 2

> 前提：你已经能指出页面/Prefab、现象、验收；不需要先做 Tier 0 审计。
> 如果根因还不清楚，用 [ui-fix.md](ui-fix.md) 的审计版。
> 输入图只读；AI 结果只写 `<任务文件夹>/results/`；不 commit。

## 复制这段

```text
按 .ai 流程直接做 <任务名> 的 Tier 2 修复/优化，不要 commit。问题已明确，不需要 Tier 0 审计。

## 输入
- 任务文件夹：D:\GitHub\Zephyr_v3\Zephyr(AI)\Zephyr\Assets\MCPTest\Screenshots\<yyyy_mm_dd>_<task>\
- 输入图：<任务文件夹>\input.png（只读）
- 目标页面/Prefab：<...>
- 现象/目标：
  1. <可观察的问题>
  2. <...>
- 不做：<P2：HUD 遮挡 / element 中文化 / 每帧 Refresh 等>
- 验收：960x540、中英文、Console 0 Error、EditMode 全绿（PlayMode 既有失败需 A/B）、before/after 截图写 results/

## Plan（控制器补齐后作为 args.plan，跳过 Astra）
```json
{
  "classification": "ordinary-feature",
  "files": [
    "Zephyr(AI)/Zephyr/Assets/Prefabs/UIPrefabs/<...>.prefab",
    "Zephyr(AI)/Zephyr/Assets/Scripts/Player/UI/<...>.cs"
  ],
  "acceptance": [
    "审计/验收项逐条通过",
    "960x540 before/after 截图写入 results",
    "Console 0 Error",
    "EditMode 全绿；PlayMode 既有失败需 A/B 证明"
  ],
  "needsApproval": false
}
```

> 若场景/Prefab 引用新 sprite/material/font：把资产 + `.meta` + folder `.meta` 加进 `files`；否则 clean checkout 会丢图。

## Brief（≤10 行，给实现者）
```text
目标：<...>；文件：<...>；约束：不改 P2/不手写 YAML/只走旧 MCP；验收：960x540 + 中英文 + Console 0 + EditMode 全绿/PlayMode A/B。
```

## 执行
1. 先读 AGENTS.md、.ai/README.md、.ai/roles/orchestrator.md、.ai/checklists/ui-recon.md、.ai/checklists/unity-acceptance.md。
2. 只读相关 Prefab/场景/Controller/Localization，补齐 plan.files/acceptance；不要另开审计。
3. preflight 快照；workflow-guard.ps1 -Mode pre。
4. 调用 workflow：tier:2，传 plan/brief/screenshots/scope/acceptance/maxRounds:2；应返回 planSource="external"，不应出现 Astra。
5. 实现者只改 plan.files；Prefab/场景只用旧 mcp__unity_mcp__*；不手写 YAML；测试用 mcp__unityMCP__run_tests。
6. 验收：static-gate.ps1 -SelfTest + -FilesFile；Console 0 Error；EditMode 全绿；PlayMode 既有失败需 A/B 证明，其余失败即回归；960x540 before/after 截图写 results/。
7. workflow-guard.ps1 -Mode post；写 .ai/reports/<date>-<task>.md 总结。
8. 汇报：planSource、changedFiles、review verdict、static-gate、run_tests、截图绝对路径、残余风险。

## 停止条件
- plan 与当前代码冲突，或需要 P2/架构专项 → 停下来报告，不要扩大范围。
- 实现者失败/超时 → 按 .ai/roles/orchestrator.md 的 524/fallback 规则处理，不要原样连撞。
```

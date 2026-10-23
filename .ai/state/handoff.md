# §〇 当前交接

- **我是谁**：Zephyr 多模型 AI 工作流的指挥（**本家 DeepSeek `deepseek-account / deepseek-flash` · max**）。
- **干到哪**：已在 `D:\GitHub\Zephyr_v3\.ai\` 建立整套编排（角色 / 三档 workflow / 检查清单 / 快照 / 静态门禁 / 台账 / Tier 指标 / 常驻 worker 协议）；**Tier 1 已改为 lean**：控制器自己写计划 + 审 diff + 门禁，workflow 只跑 Claude；Tier 2 保持 GPT 架构 + Claude + GPT 评审。**测试路由已定**：`mcp__unityMCP__run_tests` 是 AGENTS.md §10 的明确例外；其他 Unity 操作仍用旧 MCP。**多模态已验证**：本家 DeepSeek / Claude / GPT-6.1 都能 `read_image`；workflow 支持 `screenshots`，UI 任务默认 Tier 2。文本烟测、工具读文件、结构化 schema、本家 DeepSeek 工具调用均已通过；Tier 0/Tier 1 lean 分支尚未实跑真实任务。
- **下一步**：跑第一个 **Tier 1 lean** 真实任务——
  1. `preflight.ps1` 快照；
  2. 控制器写 `plan`（classification/files/acceptance/needsApproval）；
  3. `workflow(tier:1, plan, mode:'implement')` 只跑 Claude；
  4. 控制器审 diff + static-gate + `mcp__unityMCP__run_tests`；FAIL 用 `mode:'fix'` + `issues`；
  5. 收工跑 `tier-record.ps1` 记一行。
- **UI 任务**：先做 Tier 0 审计（只看截图不改文件，输出具体 spec），再做小范围 Tier 2；传 `screenshots`，控制器/Claude/GPT 都能 `read_image`。
- **卡点**：
  - 工作树有 100+ 未提交改动；`static-gate.ps1` 默认扫描全部改动时会被现有新敌人脚本的 `Zephyr.Core` 依赖误伤 ⇒ **必须用 `-Files` 任务范围模式**。
  - UE MCP（`127.0.0.1:8000`）当前未启动、也未接进 DSH；本套只覆盖 Unity。
  - `subagent` 工具没有 `model` 参数 ⇒ 常驻只对 DeepSeek 成立；GPT/Claude 仍是 workflow 一次性调用（每个子代理约 45k 工具 schema）。
  - Tier 1 lean 的自评审有边界：只审越界/漏测/违反硬规则，工具门禁优先；发现需要架构判断 → 当场升 Tier 2。用 `tier-report.ps1` 盯门禁失败率/回滚率/平均返工；超阈值就收紧 Tier 1，不许硬扛。
  - UI 优化默认 Tier 2（Prefab/场景/本地化/960x540）；不要用 Tier 1 lean 硬跑整页 UI。
  - 中转站缓存不稳定：GPT 第二次仍有 ~41k 未缓存；Claude 第二次几乎没命中。DeepSeek 本家缓存 ~99% 命中，所以指挥固定本家。
- **细节在哪**：
  - 入口：`.ai/README.md`
  - 定档：`.ai/checklists/tiers.md`
  - 模型：`.ai/config/models.md`
  - 流程：`.ai/workflows/unity-task.workflow.js`
  - 常驻 worker：`.ai/roles/worker.md`
  - Tier 记录/复盘：`.ai/scripts/tier-record.ps1` · `.ai/scripts/tier-report.ps1` · `.ai/ledger/tier-metrics.jsonl`
  - 快照：`.ai/snapshots/20261023-222439-setup3/`
  - 台账：`.ai/ledger/dispatch-ledger.jsonl`

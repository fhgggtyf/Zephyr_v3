# Zephyr 多模型 AI 工作流（`.ai`）

> 这是 `D:\GitHub\Zephyr_v3` 的**编排层**，不是第二套项目规则。
> 最高铁律仍是根目录 [AGENTS.md](../AGENTS.md)；本目录任何内容与它冲突时，**以 AGENTS.md 为准**。

---

## 1. 角色与模型

| 角色 | Tier 1（普通） | Tier 2（跨模块/架构） | 权限 |
|---|---|---|---|
| 指挥 / 日志 / commit | **DeepSeek 本家** `deepseek-account / deepseek-flash`（max） | 同左 | 唯一 commit、唯一最终验收 |
| 架构 / 计划 | **控制器自己写**（不起子代理；没把握才 `gptPlan`） | `fastllm / gpt-6-astra` | 只读；ADR、文件清单、验收 |
| 实现 | `fastllm / claude-opus-5-5` | 同左 | 可写代码/Unity 资产；不 commit |
| 独立评审 | **控制器自己审 diff + 门禁**（关键节点才 `gptReview`） | `fastllm / gpt-6.1-sol` | 只读；只看 spec + diff |
| 常驻 worker（Tier 0/1 机械段） | **DeepSeek**（继承主会话） | 不用 | 只做小改动；不 commit |

> **不再用中转站的 DeepSeek。** 指挥固定 `deepseek-account / deepseek-flash`；GPT/Claude 只由 workflow 按需调用。
> 分工第一原则是**频率与缓存热度**：DeepSeek 值班（高频、长会话、缓存热），GPT 当参谋（低频判断），Claude 当工兵。第二原则才是判断力；验证独立性靠工具门禁，不靠“再找一个模型看一眼”。
> **Unity 路由**：测试运行用 `mcp__unityMCP__run_tests`；其他操作只用 `mcp__unity_mcp__*`；图片生成用 `mcp__unityMCP__*`，但当前被 AGENTS.md §2.2 禁止。
> 模型与 provider 细节见 [config/models.md](config/models.md)。

---

## 2. 三档任务（由 DeepSeek 指挥判断）

| 档 | 判据 | 谁上 |
|---|---|---|
| **Tier 0** | 单文件；机械/文档/配置；<30 分钟；无跨模块/场景/Prefab/SO/asmdef | 指挥自己做，或交常驻 worker（DeepSeek） |
| **Tier 1** | 2–5 文件；单模块；普通功能；有现成测试 | 控制器自己写计划 → Claude 实现 → 控制器自评审 + 门禁；命中风险触发器加 `gptReview: true` |
| **Tier 2** | 跨模块；3+ 模块；架构专项；改 asmdef/场景/Prefab/存档/本地化/StateMachine/生命周期 | **有审计/spec：紧凑 plan 跳过 Astra** → Claude → GPT-6.1-sol；无 plan 才 GPT-astra 架构 |

**Tier 1 风险触发器**（满足任一就加 `gptReview: true`，只让 GPT 看 diff）：asmdef、Save/Meta/本地化、场景生命周期、StateMachine、>5 文件、`.unity/.prefab/.so`、GUID/迁移桥、不熟悉模块、上次返工过、安全/客户数据。

**不确定就升一档。** 完整判据见 [checklists/tiers.md](checklists/tiers.md)。

---

## 3. 一次任务的流程

```
DeepSeek 指挥（本家，长会话，缓存热）
  │
  ├─ 0. preflight：git status、快照、定档、写 brief；workflow-guard pre
  │
  ├─ Tier 0 ── 指挥自己做 / 交常驻 worker；不调 workflow
  │
  ├─ Tier 1 lean ── workflow(tier: 1) 只跑 Claude
  │      ├─ 计划：控制器自己写（没把握才 gptPlan）
  │      ├─ 实现：Claude Opus 5.5
  │      └─ 评审：控制器自评审 + static gate/Unity 门禁（关键节点才 gptReview）
  │
  ├─ Tier 2 ── workflow(tier: 2)
  │      ├─ 有审计/spec：控制器紧凑 plan（跳过 Astra）
  │      ├─ 无 plan：GPT-6-astra 架构
  │      ├─ 实现：Claude Opus 5.5（只收 brief + 紧凑 plan）
  │      └─ 评审：GPT-6.1-sol
  │
  ├─ 指挥验收：static-gate + Unity MCP（编译/Console/测试/截图）
  ├─ workflow-guard post（有子代理改 workflow 就还原）
  └─ 指挥提交 + 写回 §〇/台账 + reports 总结
```

**权力分离**：

1. 实现者不 commit，只交 diff；
2. 评审者不看实现者的解释，只给 spec + diff + 测试输出；
3. 指挥是唯一跑最终验收的人——**模型说“过了”不算过，工具结果才算**；
4. architecture-special 必须先批准。

---

## 4. 常驻 worker（Tier 0 / Tier 1 机械段）

- 开场用 `list_agents` 找 `worker-deepseek`；没有就用 `subagent` 创建一个（`description: worker-deepseek`，prompt 指向 [roles/worker.md](roles/worker.md)）。
- 之后用 `send_message` 复用，**不要每个任务新建**；这样工具 schema 和角色前缀能留在会话里。
- worker 模型继承主会话（DeepSeek）；它不能替代 GPT/Claude。
- 只接 Tier 0 / Tier 1 的机械段（≤2 文件、无架构判断）；代码逻辑仍交 Claude。
- 上下文变长/漂移时换一个新的；旧的可归档。

> **限制**：DSH 的 `subagent` 工具没有 `model` 参数，子代理继承主会话模型。所以“常驻”目前只对 DeepSeek 成立；GPT/Claude 仍是 workflow 一次性调用（每个子代理约 45k 工具 schema，别对每个小任务都开）。

---

## 5. 快速开始

### 5.1 开工前快照

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File D:\GitHub\Zephyr_v3\.ai\scripts\preflight.ps1 -Label "before-<task>"
```

快照写到 `.ai/snapshots/<时间戳>-<label>/`，默认不进 git。

### 5.2 填写任务简报

复制 [templates/task-brief.md](templates/task-brief.md)，写清任务/范围/验收/约束/风险，并定档；派单时再压成 ≤10 行 `brief` 传给 workflow。

### 5.3 运行分档 workflow

主会话（DeepSeek）读取 [workflows/unity-task.workflow.js](workflows/unity-task.workflow.js) 全文，作为 `workflow` 工具的 `script` 传入，并传：

```json
{
  "task": "一句话任务",
  "tier": 1,
  "plan": {
    "classification": "ordinary-feature",
    "files": ["Zephyr(AI)/Zephyr/Assets/Scripts/..."],
    "acceptance": ["编译通过", "EditMode 全绿"],
    "needsApproval": false
  },
  "scope": "允许改的目录/文件",
  "acceptance": "编译 + EditMode + 截图 + 无 Console Error",
  "brief": "≤10 行：目标/文件/约束/验收/证据；实现者优先看这个",
  "maxRounds": 2,
  "mode": "implement",
  "screenshots": ["D:\\GitHub\\Zephyr_v3\\Zephyr(AI)\\Zephyr\\Assets\\MCPTest\\Screenshots\\current.png"],
  "gptPlan": false,
  "gptReview": false
}
```

- `tier` **必填**：`0 | 1 | 2`；缺省不会跑，避免误开贵流程。
- `tier: 0` 直接返回 `stopped: "tier-0"`，不产生子代理。
- Tier 1 必须给 `plan`（控制器自己写）；没把握用 `gptPlan: true` 让 GPT 只出计划。
- Tier 1 默认**没有独立评审子代理**：返回 `needsOrchestratorReview: true`，控制器审 diff + 跑门禁；FAIL 用 `mode: "fix"` + `issues` 再调，最多 2 轮。
- `tier: 1` + `gptReview: true` → GPT 只评审，不做架构；用于风险触发器/抽样盲审。
- `tier: 2`：**有审计/spec 就传紧凑 `plan`，跳过 Astra**；没有 plan 才 GPT 架构；Claude + GPT 评审不变。
- **UI 任务**：传 `screenshots`（当前截图路径）；先读 [checklists/ui-recon.md](checklists/ui-recon.md)，写 Recon Brief，再派单；UI 默认 Tier 2。

### 5.4 静态门禁 + workflow 护栏

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File D:\GitHub\Zephyr_v3\.ai\scripts\static-gate.ps1 -SelfTest
powershell -NoProfile -ExecutionPolicy Bypass -File D:\GitHub\Zephyr_v3\.ai\scripts\static-gate.ps1 -FilesFile .ai\state\changed-files.txt
powershell -NoProfile -ExecutionPolicy Bypass -File D:\GitHub\Zephyr_v3\.ai\scripts\workflow-guard.ps1 -Mode pre
# ... 调 workflow ...
powershell -NoProfile -ExecutionPolicy Bypass -File D:\GitHub\Zephyr_v3\.ai\scripts\workflow-guard.ps1 -Mode post
```

只扫任务文件的新增行；FAIL 不许 commit。`-SelfTest` 必须 PASS；`workflow-guard post` exit 1 = 有子代理改了 workflow，已自动还原。

### 5.5 Unity 验收 + 提交 + 总结

按 [checklists/unity-acceptance.md](checklists/unity-acceptance.md)；全过后只 stage 任务路径，commit，更新 `§〇`、台账与 [reports/](reports/) 总结（模板 [templates/task-summary.md](templates/task-summary.md)）。

---

## 6. 文件地图

| 路径 | 作用 |
|---|---|
| `roles/orchestrator.md` | 指挥：定档、worker、派单、验收、commit |
| `roles/worker.md` | 常驻 DeepSeek worker（Tier 0/1 机械段） |
| `roles/architect.md` | 架构师（Tier 2 GPT-astra；Tier 1 仅 `gptPlan`） |
| `roles/implementer.md` | 实现者（Claude Opus 5.5） |
| `roles/reviewer.md` | 独立评审（Tier 2 GPT-6.1-sol；Tier 1 仅 `gptReview`） |
| `workflows/unity-task.workflow.js` | 分档编排 |
| `workflows/README.md` | 参数、返回、失败行为 |
| `checklists/tiers.md` | 三档判据与风险触发器 |
| `checklists/preflight.md` | 开工前检查 |
| `checklists/unity-acceptance.md` | 编译/Console/测试/截图/UI/战斗 |
| `checklists/architecture.md` | AGENTS.md §4.5/§4.6 浓缩门禁 |
| `checklists/ui-recon.md` | UI 任务侦察清单 + Recon Brief 模板 |
| `audits/` | UI/架构审计报告（实施依据） |
| `reports/` | 每单最终总结文档 |
| `templates/task-brief.md` | 任务简报（四件套 + 架构六问） |
| `templates/task-summary.md` | 最终总结模板 |
| `templates/dispatch.md` | 派单/回执/台账格式 |
| `templates/handoff.md` | `§〇` 模板（≤4 KB） |
| `config/models.md` | provider/模型/回退/实测 |
| `config/cordis.patch.snippet.yml` | 可合并的 DSH 配置片段（无密钥） |
| `scripts/preflight.ps1` | 快照 + 状态检查 |
| `scripts/static-gate.ps1` | 任务范围静态门禁（含 `-SelfTest`） |
| `scripts/workflow-guard.ps1` | workflow 越权改动的哨兵/还原 |
| `scripts/tier-record.ps1` | 追加一条 Tier 指标 |
| `scripts/tier-report.ps1` | 看 Tier 1 是否漂移（门禁/回滚/返工） |
| `ledger/` | 派单台账 + `tier-metrics.jsonl` |
| `state/handoff.md` | 当前 `§〇` |
| `snapshots/` | 任务前快照（git 忽略） |

---

## 7. 成本纪律

- **DeepSeek 指挥保持长会话**：本家缓存命中极高（实测 20.6M cache read vs 195k uncached），别每个任务新开指挥会话。
- **Tier 1 lean 只跑 1 个 Claude 子代理**：控制器自己写计划/审 diff，省掉两个约 45k 工具 schema 的 DeepSeek 子代理。
- **Tier 2 有 plan 只开 2 个子代理**：Claude 实现 + GPT 评审（跳过 Astra）；无 plan 才三个。
- 中转站缓存不稳定：GPT 第二次仍有 ~41k 未缓存；Claude 第二次几乎没命中。按 token 计费时，别把它当“便宜”用。
- 实现者只收 `brief` + 紧凑 plan；不要把审计全文/全部截图塞给它。
- 任务结束看 `tokenUsage`，并用 `tier-record.ps1` 记录 gate/rollback/rework；`tier-report.ps1` 超过阈值就收紧 Tier 1。

### 7.1 UI / 多模态

- 本家 DeepSeek、Claude Opus 5.5、GPT-6.1-sol 都已声明 `input: [text, image]`，实测都能用 `read_image` 看截图。
- UI 任务默认 **Tier 2**（碰 Prefab/场景/本地化/`960x540`）；只有纯 UI 脚本、不碰这些，才可能 Tier 1。
- 流程：控制器抓当前截图 → `args.screenshots` → 架构/实现/评审 `read_image` → 实现后截图 → 控制器/评审对比 → 验收。
- **先读 [checklists/ui-recon.md](checklists/ui-recon.md)，写 Recon Brief（≤10 行），再派单**；不要在 reasoning 里反复枚举方案。
- **截图目录**：用户在 `Assets/MCPTest/Screenshots/<任务文件夹>/` 放输入图；AI 只读输入图，结果写 `<任务文件夹>/results/`（不存在就建）；命名 `[yyyyMMdd_HHmmss]_[stage]_[场景]_[说明].png`，stage 用 `audit/before/after/review/final`。
- `screenshots` 传输入图路径；`screenshotDir` 可省略（默认第一张图的父目录）；`resultsDir` 默认 `<screenshotDir>/results`。
- 第一单建议先做 **Tier 0 UI 审计**（只看不改，输出具体改动 spec），再做小范围 Tier 2，别一上来做整页重排。

---

## 8. 已知冲突与处理

| 冲突 | 处理 |
|---|---|
| 测试运行路由 | AGENTS.md §10 明确例外：测试用 `mcp__unityMCP__run_tests`；其他操作仍用旧 MCP；不得因测试失败切换其他操作 |
| `subagent` 工具没有 model 参数 | 常驻只对 DeepSeek 成立；GPT/Claude 走 workflow 一次性调用 |
| 子代理越权改 workflow | 调用前后跑 `workflow-guard.ps1`；post exit 1 = 已还原，写进汇报 |
| `static-gate` 曾把 `Zephyr.Core.*` 专用命名空间误判 | 已改为只匹配 `using Zephyr.Core;`；先跑 `-SelfTest` |
| Tier 1 lean 的自评审有边界 | 只审越界/漏测/违反硬规则；工具门禁优先；发现需要架构判断 → 升 Tier 2；用 `tier-report.ps1` 盯漂移 |
| AGENTS.md §6.1 写的状态路径是 `D:\GitHub\Zephyr\Test\_Status.md` | 实际仓库是 `D:\GitHub\Zephyr_v3\Test\_Status.md`；写实际存在的那个并注明 |
| 当前工作树有未提交改动 | 先快照；只改任务文件；只 stage 明确路径 |

---

## 9. 不做什么

- 不做第二套状态机/场景加载/事件总线/Manager；
- 不手写 Unity 序列化 YAML；
- 不让多个 agent 同时写同一个 Unity 资产；
- 不用这套流程替代 AGENTS.md 的架构专项审批。

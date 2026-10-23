# 分档 workflow：怎么用

`unity-task.workflow.js` 是 **DSH `workflow` 工具的脚本主体**，不是 Node 模块，不能直接 `node` 运行。

## 1. 谁运行它

主会话（**DeepSeek 本家 `deepseek-account / deepseek-flash` max**，即“指挥”）读取本文件全文，作为 `workflow` 工具的 `script` 参数传入。

## 2. 参数

```json
{
  "task": "复活 Boss 二阶段：血量低于 50% 时切换攻击模式",
  "tier": 1,
  "plan": {
    "classification": "ordinary-feature",
    "files": ["Zephyr(AI)/Zephyr/Assets/Scripts/Gameplay/Enemies/BossPhaseTwo.cs"],
    "acceptance": ["编译通过", "EditMode 全绿", "Boss 场景 PlayMode 冒烟"],
    "needsApproval": false
  },
  "scope": "Assets/Scripts/Gameplay/Enemies/",
  "acceptance": "编译 + EditMode + PlayMode 冒烟 + Console 0 Error",
  "brief": "目标：Boss 50% 血切二阶段；文件：BossPhaseTwo.cs；约束：不改基础设施；验收：编译+EditMode+冒烟",
  "maxRounds": 2,
  "mode": "implement",
  "screenshots": ["D:\\...\\current.png"],
  "gptPlan": false,
  "gptReview": false
}
```

| 参数 | 必填 | 说明 |
|---|---|---|
| `task` | ✅ | 一句话任务 |
| `tier` | ✅ | `0` 指挥自己做；`1` 普通功能 lean；`2` 跨模块/架构专项。缺省报错，避免误开贵流程 |
| `plan` | Tier 1 ✅ / Tier 2 可选 | 指挥写的紧凑计划：`classification` / `files[]` / `acceptance[]` / `needsApproval`。Tier 2 传了就跳过 Astra |
| `brief` | | ≤10 行任务简报（目标/文件/约束/验收/证据）；实现者优先按它做，不要塞审计全文 |
| `mode` | | `implement`（默认）或 `fix`（修复轮） |
| `issues` | | `mode: "fix"` 时的阻塞项数组；逐条回给 Claude。**fix 模式必须非空** |
| `screenshots` | | UI/画面任务的输入截图路径数组；架构/实现/评审都能 `read_image`。输入图只读 |
| `screenshotDir` | | 任务文件夹；省略时取第一张输入图的父目录 |
| `resultsDir` | | AI 结果图目录；省略时为 `<screenshotDir>/results`（不存在由执行方创建） |
| `gptPlan` | | Tier 1 没把握时让 GPT-6-astra 只出计划；仍由控制器验收 |
| `gptReview` | | Tier 1 关键节点让 GPT-6.1-sol 只审 diff；日常 Tier 1 不开 |
| `scope` / `acceptance` | | 允许范围 / 验收标准 |
| `maxRounds` | | Tier 2 实现→评审循环上限，1~3，默认 2 |

### 2.1 截图目录

- 用户建 `Assets/MCPTest/Screenshots/<任务文件夹>/` 并放输入图；输入图**只读**。
- AI 结果写 `<任务文件夹>/results/`（不存在就建）；`screenshots` 传输入图路径，`screenshotDir` 可省略（默认取第一张图的父目录）。
- 命名：`[yyyyMMdd_HHmmss]_[stage]_[场景名]_[说明].png`，`stage` 用 `audit/before/after/review/final`。
- 有任务文件夹时禁止写全局 `Assets/MCPTest/Screenshots/` 根目录；只有没有任务文件夹才走旧兜底。

### 2.2 流程产物白名单

`.ai/ledger/**`、`.ai/state/**`、`.ai/snapshots/**`、`.ai/reports/**`、`.ai/audits/**`、`Assets/MCPTest/Screenshots/<任务文件夹>/results/**` 是流程强制产物，不算实现范围；评审不要把 `git status` 里的这些当阻塞项。

## 3. 两档映射

### Tier 1 lean（默认路径）

```
指挥写 plan → workflow 只跑 Claude → 返回 diff
→ 指挥审 diff（对着 acceptance）+ static gate + Unity 门禁
→ FAIL：指挥用 mode:"fix" + issues 再调一次（最多 2 轮）
→ 仍 FAIL：升 Tier 2 或停
```

- 只产生 **1 个 Claude 子代理**；不再起 DeepSeek 架构/评审子代理。
- `gptPlan: true`：多一个 GPT 架构子代理，只出计划。
- `gptReview: true`：多一个 GPT 评审子代理，只看 diff；这是“关键节点/抽样盲审”用的。
- 返回 `needsOrchestratorReview: true` 时，**没有 review 字段**；控制器必须自己审。

### Tier 2（重武器）

```
有审计/spec：指挥写紧凑 plan → Claude 实现 → GPT-6.1-sol 评审   （跳过 Astra）
无 plan：     GPT-6-astra 架构 → Claude 实现 → GPT-6.1-sol 评审
  → FAIL：回给 Claude，最多 maxRounds 轮
```

- **传 `args.plan` 就跳过 Astra**（`planSource: "external"`）；不传才让 GPT-6-astra 架构。
- 实现者只收 `brief` + 紧凑 plan（不是完整审计）；需要细节自己读 `plan.files`。
- 返回 `review`；控制器仍要跑 static gate + Unity 验收。

## 4. 输出

```json
{
  "ok": true,
  "task": "...",
  "tier": "1",
  "mode": "implement",
  "plan": { "...": "..." },
  "implementation": { "status": "done", "changedFiles": [], "diff": "...", "...": "..." },
  "review": null,
  "needsOrchestratorReview": true
}
```

| 字段 | 含义 | 指挥要做什么 |
|---|---|---|
| `stopped: "tier-0"` | 不产生子代理 | 指挥自己做或交 worker |
| `stopped: "needs-approval"` | 计划判为架构专项 | 停下来，向用户要批准 |
| `stopped: "blocked"` | 实现者阻塞 | 看 `implementation.blockers` |
| `stopped: "diff-too-large"` | diff 超过 80 KB | 拆任务或按文件分批评审 |
| `needsOrchestratorReview: true` | Tier 1 lean，没有独立评审 | 控制器按 acceptance 审 diff + 跑门禁 |
| `review.verdict = "FAIL"`（Tier 2 或 gptReview） | 评审未过 | 看 `blocking`；最多再 2 轮 |
| `planSource: "external"` | Tier 2 用了外部 plan，跳过 Astra | 确认 plan 来自审计/spec |
| `ok: false` | 参数/模型失败 | 修正参数或降档；不要假装成功 |

## 5. 失败行为

- `tier` 缺失/非法：直接返回，不花模型调用；
- Tier 1 缺 `plan` 且没开 `gptPlan`：直接返回错误，不花模型调用；
- `tier: 0`：直接返回，不产生子代理；
- `agent()` 失败返回 `null`，脚本返回 `ok:false`；
- Tier 2 架构师判 `architecture-special`：实现前返回，不写文件；
- 脚本本身不能读文件/跑命令；所有读写由子代理完成。

## 6. 不要做

- 不要改脚本去“自动 commit”；
- 不要让实现者兼任评审；
- 不要把 `provider` 写成不存在的名字（当前只有 `fastllm` / `deepseek-account`）；
- 不要在 `agent()` 里传未文档化的选项（会终止整个 workflow）。

# 派单台账 + Tier 指标

## 1. 派单台账

`dispatch-ledger.jsonl` 只追加，不删改历史。

一行一条：

```json
{"ts":"2026-10-23T22:30:00+08:00","task":"Boss 二阶段","from":"orchestrator","to":"architect","model":"fastllm/gpt-6-astra","status":"done","evidence":"plan.json","commit":null}
```

字段：

| 字段 | 必填 | 说明 |
|---|---|---|
| `ts` | ✅ | ISO 8601 带时区 |
| `task` | ✅ | 任务标识（同一任务用同一字符串） |
| `from` / `to` | ✅ | `orchestrator` / `architect` / `implementer` / `reviewer` / `user` |
| `model` | ✅ | `provider/model` |
| `status` | ✅ | `dispatched` / `done` / `blocked` / `pass` / `fail` |
| `evidence` | ✅ | 文件、命令输出、截图、commit 路径 |
| `commit` | | commit hash；没有就 `null` |

两个查询（这就是抓手）：

- 我发出、还没人回的 → 我欠不欠催？
- 别人在等我回的 → 我是不是让别人空等？

## 2. Tier 指标

`tier-metrics.jsonl` 每单一行，由 `scripts/tier-record.ps1` 追加：

```json
{"ts":"2026-10-24T09:00:00+08:00","task":"Boss 二阶段","tier":1,"gate":"pass","rework":0,"rollback":"none","gptReview":false,"gptPlan":false,"notes":""}
```

字段：

| 字段 | 说明 |
|---|---|
| `tier` | 0 / 1 / 2 |
| `gate` | `pass` / `fail` / `blocked` |
| `rework` | 实现→评审/修复轮数 |
| `rollback` | `none` / `revert` / `fix-forward` / `reopen` / `post-commit-regression` / `user-rejected` |
| `gptReview` / `gptPlan` | 本单是否用了 GPT 评审/计划（抽样盲审要标） |

看趋势：`scripts/tier-report.ps1`。Tier 1 门禁失败 >20%、回滚 >10%、平均返工 >1.5（最近 10 单）→ 收紧 Tier 1，把该 pattern 移到 Tier 2。

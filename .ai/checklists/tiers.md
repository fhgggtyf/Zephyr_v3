# 三档任务分级（由 DeepSeek 指挥判断）

> 目标：不是所有任务都值得让 GPT/Claude 上。DeepSeek 本家缓存命中高、便宜；GPT/Claude 每个子代理都要重载约 45k 工具 schema，贵。
> **判据不确定时，升一档。**

## 1. 判定表

| 档 | 判据（满足多数即归此档） | 架构 | 实现 | 评审 |
|---|---|---|---|---|
| **Tier 0** | 单文件；机械修改；文档/配置/日志；<30 分钟；无跨模块、无场景/Prefab/SO/asmdef | 指挥自己做 | 指挥或常驻 worker（DeepSeek） | 指挥自检 |
| **Tier 1** | 2–5 个文件；单模块；普通功能；有现成测试；不改场景/Prefab/asmdef/存档/本地化 | **指挥自己写计划** | **Claude Opus 5.5** | **指挥审 diff + 门禁**；可选 GPT 关键节点评审 |
| **Tier 2** | 跨模块；3+ 模块；架构专项；改 asmdef/场景/Prefab/存档/本地化/StateMachine/生命周期；>5 文件 | **有审计/spec：紧凑 plan 跳过 Astra；无 plan 才 GPT-6-astra** | **Claude Opus 5.5** | **GPT-6.1-sol** |

> **Tier 2 的 plan 来源只能有一个**：有审计/spec → 控制器写紧凑 plan（`args.plan`），跳过 Astra；没有才让 GPT-6-astra 架构。不要“审计 plan + Astra plan”两边都跑。

## 2. Tier 1 的风险触发器（满足任一 → `gptReview: true`）

- 改 asmdef / 程序集边界
- 改 Save / Meta / 本地化 / 场景生命周期
- 改 StateMachine / Persistent / GameManager / SceneLoader
- 超过 5 个文件，或涉及 `.unity` / `.prefab` / `.so`
- UI 视觉改动（Prefab/场景/Canvas/锚点/`960x540`）
- 资产 GUID / 序列化 / 迁移桥
- 不熟悉的模块，或上一次同类改动返工过
- 涉及安全、权限、外部客户数据
- **说不清验收怎么跑**（没有命令/测试/截图判据）

> 只升“评审”不升“架构”：`tier: 1` + `gptReview: true`。GPT 只看 diff，不重读全仓。
> 如果计划阶段就没把握，用 `gptPlan: true` 让 GPT 只出计划；控制器仍负责验收和 commit。

### `gptPlan` 触发器（不是 `gptReview`）

Tier 1 默认由控制器写计划，但以下情况应加 `gptPlan: true`：

- 不熟悉的模块，或第一次改这块
- 说不清验收怎么跑（没有命令/测试/截图判据）
- 有多个合理方案，控制器拿不准
- 改共享契约/公共 API/跨团队接口
- 上次同类任务返工过
- 涉及存档、本地化、场景生命周期、StateMachine（这些通常直接升 Tier 2）

> 如果 Tier 1 任务频繁触发 `gptPlan`，说明 Tier 1 判据太松：把该 pattern 升到 Tier 2，而不是把 GPT 计划变成默认。

## 3. Tier 1 lean 的自评审边界

Tier 1 省掉了独立的架构/评审子代理，控制器要自己审。成立的前提：

1. **对着验收标准审，不是对着自己的计划审**。计划是假设，验收是合同。
2. **先看工具证据，再看 diff**：static gate、编译、Console、测试、截图；LLM 评审只是补充。
3. **只审“有没有越界/漏测/违反硬规则”**，不在 Tier 1 做架构重设计；发现需要架构判断 → 当场升 Tier 2。
4. **控制器说 PASS 不等于完事**：门禁 FAIL 就是 FAIL，不许用“看起来对”覆盖。

## 4. 度量：滞后 + 前导

| 类型 | 指标 | 采集 |
|---|---|---|
| 滞后 | **回滚率** | `tier-record.ps1 -Rollback ...` |
| 前导 | 门禁失败率 | `-Gate fail` |
| 前导 | 平均返工轮数 | `-Rework N` |
| 前导 | GPT 抽样盲审发现阻塞项的比例 | `-GptReview` + 审计记录 |
| 前导 | Token/耗时异常 | 任务后看 `tokenUsage` |

**回滚定义**（任一即算）：

- `revert`：git revert / 回到任务前
- `fix-forward`：24 小时内必须补修
- `reopen`：任务重新打开
- `post-commit-regression`：提交后 7 天内出现回归
- `user-rejected`：用户不接受结果

## 5. 阈值与升级（Tier 1 专用）

看**最近 10 个 Tier 1 任务**：

| 信号 | 阈值 | 动作 |
|---|---|---|
| 门禁失败率 | > 20% | 收紧 Tier 1 判据；把该 pattern 移到 Tier 2 |
| 回滚率 | > 10% | 同上；lean 不许拿质量换 token |
| 平均返工 | > 1.5 轮 | 边界太松；升级 |
| 任一 post-commit regression（7 天） | 1 次 | 该 pattern 直接定为 Tier 2 |

**抽样盲审**：每 5 个 Tier 1 任务抽 1 个，加 `gptReview: true` 做独立盲审（不告诉 GPT 控制器已经 PASS）。
如果 5 个抽样里有 ≥1 个发现阻塞项 → 收紧 Tier 1。这是比回滚率更早的信号。

## 6. 记录与复盘

- 每单收工：`scripts/tier-record.ps1` 追加一行。
- 每 10 单或每周：`scripts/tier-report.ps1` 看趋势；有 WARN 就改 `tiers.md`，不是硬扛。
- 任务简报和台账都要写 `tier` 与理由（一句话）。

## 7. 常驻 worker

- Tier 0 / Tier 1 的机械段可交常驻 `worker-deepseek`；复用同一会话，省重复工具/规则加载。
- Tier 1 的代码逻辑仍交 Claude；worker 不替代 Claude。
- Tier 2 不用 worker，直接 workflow。
- worker 上下文变长/漂移时换一个；不要无限复用。

# 派单 / 回执 / 台账格式

## 1. 派单四件套（缺一件对方就得追问）

```text
【派单】<一句话标题>
① 现象：现在是什么样（可观察，不要形容词）
② 照哪做：文件/章节/工具（唯一出处）
③ 怎么验：跑什么、看到什么算过
④ 回一句：结论 + 证据 + 落盘路径，我在等回执
```

反例：「索引有点乱，你整理一下。」
正例：「`Zephyr.Architecture.Tests` 报 3 条 policy 不一致（A/B/C）。照 `ArchitecturePolicy.json` 补行。补完跑 `ProjectAssemblyGraphIsExplicitAcyclicAndReviewed`，应 PASS。我在等你的回执。」

## 2. 回执格式

```text
【回执】<标题>
结论：做了什么
证据：命令/测试输出/截图路径/diff 路径
落盘：<文件路径>
【结束】
```

- 只说“好了”= 没回执；
- 只在自己窗口说“我做完了”= 没交付；
- 收到“知悉”不许再回（会互相点燃）。

## 3. 上限

- 同一件事 ≤3 条（派单 + 回执 + 收尾知悉）
- 单条 ≤200 字 / ≤10 行
- 末行 `【结束】`

## 4. 台账（JSONL，只追加）

`ledger/dispatch-ledger.jsonl`，一行一条：

```json
{"ts":"2026-10-23T22:30:00+08:00","task":"Boss 二阶段","from":"orchestrator","to":"architect","model":"gpt-6-astra","status":"done","evidence":"plan.json","commit":null}
```

字段：

| 字段 | 说明 |
|---|---|
| `ts` | ISO 时间 |
| `task` | 任务标识 |
| `from` / `to` | 角色 |
| `model` | provider/model |
| `status` | dispatched / done / blocked / pass / fail |
| `evidence` | 文件/命令/截图路径 |
| `commit` | commit hash 或 null |

## 5. 分诊（没人认领时）

- 有对口角色 → 派单（四件套）
- 谁都不合适 → 指挥自己做，台账落一行
- 拿不准 → 问用户，别猜

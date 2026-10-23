# 开工前检查清单

> 每次任务开始前逐项过；没过的项写进任务简报的“风险/待确认”。

## 1. 仓库状态

- [ ] `git -C D:\GitHub\Zephyr_v3 status --short` 已看；知道哪些是用户已有改动
- [ ] 当前分支：`main`（或用户指定分支）
- [ ] 没有被 `reset/checkout/clean` 清掉的改动
- [ ] 已跑 `powershell -NoProfile -ExecutionPolicy Bypass -File .ai\scripts\preflight.ps1 -Label "before-<task>"`
- [ ] 快照目录 `.ai\snapshots\<时间戳>-<label>\` 有 `git-status.txt` / `git-diff.patch` / `manifest.json`

## 2. 项目基线

- [ ] Unity 版本 `6000.5.6f1`（`ProjectSettings\ProjectVersion.txt`）
- [ ] Unity 项目根：`D:\GitHub\Zephyr_v3\Zephyr(AI)\Zephyr`
- [ ] 读过根 `AGENTS.md`（尤其 §4.5/§4.6/§7/§8/§10）
- [ ] 读过相关 `Documents/` 设计文档

## 3. Unity MCP

- [ ] 旧 MCP `mcp__unity_mcp__*` 可用（所有**非测试** Unity 操作用它）
- [ ] 新 MCP `mcp__unityMCP__run_tests` 可用（EditMode/PlayMode 测试；AGENTS.md §10 明确允许）
- [ ] 新 MCP `mcp__unityMCP__*` 的图片生成当前被 §2.2 禁止
- [ ] 已确认当前项目根（`Unity_ManageEditor` / `GetProjectRoot`）
- [ ] 连续失败 >3 次的熔断规则已知（§6.1）

## 4. 任务分级

- [ ] 普通功能 / 跨模块 / 架构专项
- [ ] 跨模块：六问已写
- [ ] 架构专项：已停，等用户批准
- [ ] 允许改的文件/目录已列清
- [ ] 验收标准可执行（命令 / 测试 / 截图 / Console）

## 5. 派单

- [ ] 任务简报已填（`.ai\templates\task-brief.md`）
- [ ] 四件套齐全：现象 / 照哪做 / 怎么验 / 回什么
- [ ] 同一时间只有一个 Unity 资产写入者
- [ ] 实现者知道“不 commit、不碰用户改动”

## 6. 失败预案

- [ ] 中转站不可用 → 回退 `deepseek-account / deepseek-flash`
- [ ] 实现者阻塞 → 看 `blockers`，不硬推
- [ ] Unity MCP 熔断 → 写 `Test\_Status.md`，输出 `[任务已中止，状态已保存]`
- [ ] 工作树被意外改动 → 停，查快照，不用 reset 掩盖

# MainMenu TMP 渐进迁移模板（只迁当前场景）

> 范围：只迁 `Assets/Scenes/MainMenu/MainMenu.unity`，不碰其他场景/全局 Prefab。
> 原则：一次只迁一个批次；每批独立 Tier 2；不要一次性全量替换。
> 前置：TMP Essential Resources + 中文 TMP Font Asset 必须先就绪，否则中文一定 tofu。

## 1. 前置检查

- [ ] `Assets/TextMesh Pro` 是否存在；没有先导入 TMP Essential Resources（单独 setup，不混迁移批次）
- [ ] 是否已有中文 `TMP_FontAsset`；没有就用 `Assets/ArtResources/Fonts/fusion-pixel-12px-proportional-zh_hans.otf` 生成（含中文字形）
- [ ] 确认 `ConfirmationPopup` 是 Prefab：`Assets/Prefabs/UIPrefabs/Dialogs/ConfirmationPopup.prefab`
  - 若只改场景实例，不能单独替换 Prefab 里的 Text；要么把 Prefab 加进 B4，要么本单跳过它
- [ ] `MainMenuView.cs` 的 `SetText(Text target, ...)` 需要加 `SetText(TMP_Text target, ...)` 重载；混合迁移期间两种字段并存

## 2. 批次表

| 批次 | 只改这些 | 文件 | 验收 |
|---|---|---|---|
| **B0** | TMP 前置 + 试迁 `_title` | TMP Essentials、中文 Font Asset、`MainMenu.unity`、`MainMenuView.cs` | 标题中英文正常、无 tofu、无 missing reference |
| **B1** | `_title` / `_subtitle` / `_startLabel` / `_continueLabel` / `_settingsLabel` / `_exitLabel` | `MainMenu.unity`、`MainMenuView.cs` | 主界面全部 TMP；其他 Text 保留；布局不变 |
| **B2** | `_saveTitle` / `_saveStatus` / `_saveCloseLabel` + `SlotView.Title/Details/PrimaryLabel/DeleteLabel` | `MainMenu.unity`、`MainMenuView.cs` | 存档面板 + slot 正常；换行正常 |
| **B3** | `_settingsTitle` / `_languageLabel` / `_englishLabel` / `_chineseLabel` / `_settingsCloseLabel` | `MainMenu.unity`、`MainMenuView.cs` | 设置面板正常；中英文切换正常 |
| **B4** | `ConfirmationPopup`（Prefab 边界，先决定） | `ConfirmationPopup.prefab`、`ConfirmationPopupView.cs` | 弹窗 4 个 Text → TMP；其他场景不受影响或已确认 |
| **B5** | 清理 | `MainMenuView.cs`、`MainMenu.unity` | 当前场景无 legacy Text；无 `using UnityEngine.UI` 遗留；全量截图 |

> B1–B3 每批只改该批字段；其他字段继续是 `Text`，`SetText` 双重重载保证编译。
> B4 若 `ConfirmationPopup` 只被 MainMenu 使用，可以一起迁；否则本单跳过，另开 Dialog 迁移单。

## 3. 每批 prompt 模板

```text
按 .ai 流程做 MainMenu TMP 迁移 batch <B0/B1/B2/B3/B4/B5>：<本批字段>，不要 commit。
只迁当前 MainMenu 场景，不要全量替换，不要碰其他场景/Prefab（B4 除外，需先确认）。

## 输入
- 任务文件夹：D:\GitHub\Zephyr_v3\Zephyr(AI)\Zephyr\Assets\MCPTest\Screenshots\<yyyy_mm_dd>_<task>\
- 输入图：<任务文件夹>\input.png（只读）
- 目标 scene：Zephyr(AI)/Zephyr/Assets/Scenes/MainMenu/MainMenu.unity
- 本批字段：<...>
- 不做：本批之外的 Text、其他场景、布局重做、无关重构。

## Plan（控制器补齐后作为 args.plan，跳过 Astra）
{
  "classification": "ordinary-feature",
  "files": [
    "Zephyr(AI)/Zephyr/Assets/Scenes/MainMenu/MainMenu.unity",
    "Zephyr(AI)/Zephyr/Assets/Scripts/Core/Flow/Scenes/MainMenuView.cs"
  ],
  "acceptance": [
    "本批字段全部为 TMP，其他 Text 保留",
    "中文/英文无 tofu、无 missing glyph/reference",
    "960x540 截图与迁移前布局一致",
    "Console 0 Error；EditMode 全绿；PlayMode 既有失败需 A/B"
  ],
  "needsApproval": false
}

> B0 另加 TMP Font Asset；B4 另加 `ConfirmationPopup.prefab` + `ConfirmationPopupView.cs`。其他批次不要扩 plan.files。

## Brief（≤10 行）

目标：只迁 <本批字段>；文件：MainMenu.unity + MainMenuView.cs（+ B4 的 ConfirmationPopup）；约束：保留其他 Text、SetText 双重重载、不手写 YAML、只走旧 MCP；验收：本批字段 TMP + 中英文无 tofu + 960x540 + EditMode 全绿/PlayMode A/B。

## 执行
1. 先读 AGENTS.md、.ai/README.md、.ai/roles/orchestrator.md、.ai/checklists/ui-recon.md、.ai/checklists/unity-acceptance.md。
2. 只读本批字段和对应场景节点；补齐 plan.files；不要动其他批次。
3. preflight 快照；workflow-guard.ps1 -Mode pre。
4. 调用 workflow：tier:2，传 plan/brief/screenshots/scope/acceptance/maxRounds:2；应返回 planSource="external"，不应出现 Astra。
5. 实现：本批字段 `Text` → `TMP_Text`/`TextMeshProUGUI`；加 `SetText(TMP_Text, string)` 重载；分配中文 Font Asset；不手写 YAML。
6. 验收：static-gate.ps1 -SelfTest + -FilesFile；Console 0 Error；EditMode 全绿；PlayMode 既有失败需 A/B；960x540 before/after 截图写 results/。
7. workflow-guard.ps1 -Mode post；写 .ai/reports/<date>-<task>.md 总结。
8. 汇报：planSource、changedFiles、review verdict、static-gate、run_tests、截图绝对路径、残余风险。

## 停止条件
- 本批需要改 Prefab（B4 例外）或超出当前场景 → 停，说明边界。
- 中文 Font Asset 缺失/无中文字形 → 停，先做 B0。
- 实现者失败/超时 → 按 .ai/roles/orchestrator.md 的 524/fallback 规则处理。
```

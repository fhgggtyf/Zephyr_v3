# UI Recon Checklist（Tier 2 UI 任务开工前）

> 目的：把“雷霆大思考”收敛成 10 行 brief。先查这里，再读审计；不要在 reasoning 里反复枚举方案。
> 读完本页 + 审计 + 任务文件后，直接写 Recon Brief，然后派单。

## 1. 先看这三处

- [ ] `AGENTS.md` §4.4（UI/本地化）、§6.2（截图目录）、§10（MCP 路由）
- [ ] 审计/spec（`.ai/audits/<task>.md`）
- [ ] 只打开任务涉及的 Prefab + 场景 + 3 个 UI 脚本 + 2 个本地化 JSON；不要全仓扫

## 2. 场景覆盖（最容易漏）

- [ ] `git diff -- <scene>` 是否为空；若有覆盖，逐条列：`m_AnchoredPosition` / anchors / `m_PreserveAspect` / `m_LocalScale`
- [ ] 只回退审计明确点名的覆盖；其他“看起来也脏”的覆盖不动，记入残余风险
- [ ] 场景/Prefab 引用的新 sprite / material / font 是否已跟踪？`plan.files` 必须包含资产 + `.meta` + folder `.meta`；`git ls-files` 为空就是未跟踪，提交会丢引用
- [ ] Unity 6 的 `PrefabUtility.RevertPropertyOverride` 只接受 `SerializedProperty`：`new SerializedObject(rt).FindProperty("m_AnchoredPosition.y")`

## 3. Prefab 常见坑

- [ ] `Scrollbar.UpdateVisuals()` 会在 OnEnable 用 `m_Value/m_Size` 重写 Handle 的 anchors；**不要**指望序列化 anchors 落盘，正确做法是 `sizeDelta=(-4,0)`，高度由 Scrollbar 驱动
- [ ] `Mask.showMaskGraphic` 决定 Viewport 底板是否可见
- [ ] Tooltip `Background` 要在 sibling 0 并拉伸；先确认实际 sibling 顺序，审计可能写错
- [ ] 所有 RectTransform 改动后用运行时 probe 读回 screen rect，不靠“看起来对了”

## 4. Play Mode / 截图

- [ ] Game View 需要 16:9 / 960×540：用 `GameViewSizes` 反射加自定义 size，再设 `selectedSizeIndex`；不要用 `Screen.SetResolution`
- [ ] 多场景 Play：Initializer → Persistent → MainMenu → StartSelectedProfile，才有真实 HUD + Localization
- [ ] 截图用 `ScreenCapture.CaptureScreenshot(path)`（异步、下一帧落盘）；**不要** `CaptureScreenshotAsTexture()`（菜单项里会卡死/退 Play）
- [ ] 结果图只写 `<任务文件夹>/results/`；输入图只读

## 5. Unity MCP / 测试

- [ ] Prefab/场景/Console 只用旧 `mcp__unity_mcp__*`
- [ ] `Unity_RunCommand` 动态脚本禁 `using System.Reflection;` / `using System.IO;` / `BindingFlags`；`Image` 写全限定 `UnityEngine.UI.Image`；if/else 必须完整大括号
- [ ] `mcp__unityMCP__run_tests` 超时 → 旧 MCP 里一次性 Editor `TestRunnerApi`；记录 `probe_tests.txt`
- [ ] 3 个历史失败已修；`run_tests` 应全绿，失败即回归

## 6. Recon Brief（≤10 行，写完就派单）

```text
目标：
场景覆盖：
Prefab 节点/字段：
代码文件：
验收/截图：
风险/未知：
```

> 如果 brief 已经能回答“改哪些文件、改什么、怎么验”，停止侦察，调用 workflow。
> 不要在 reasoning 里同时推演 3 套实现方案；选一套，把理由写进 brief。

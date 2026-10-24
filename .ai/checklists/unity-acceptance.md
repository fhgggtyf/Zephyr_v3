# Unity 验收清单

> 指挥亲自执行；按改动类型至少做对应项。没有证据 = 未验证。

## 1. 静态门禁

- [ ] `powershell -NoProfile -ExecutionPolicy Bypass -File .ai\scripts\static-gate.ps1 -Files <implementation.changedFiles>` → PASS
- [ ] `git diff --check` 无空白错误
- [ ] `git status --short` 只包含任务相关文件（+ 用户已有改动）
- [ ] 没有 `Library/`、`Temp/`、`Logs/`、构建产物进入暂存

## 2. 编译与 Console

- [ ] Unity 编译通过（脚本编译无错误）
- [ ] `Unity_GetConsoleLogs`：Error = 0
- [ ] Warning 逐条说明来源；与本次无关的记为残余风险
- [ ] 无 MissingReference / Missing Script / YAML parse / 粉色材质 / failed to load

## 3. 测试

按 AGENTS.md §10，**测试运行固定使用 `mcp__unityMCP__run_tests`**；这是明确例外，不需要再申请。
其他 Unity 操作仍只用 `mcp__unity_mcp__*`，不得因测试失败把场景/Prefab/资源操作切到新 MCP。

必跑（按改动）：

- [ ] EditMode：`Zephyr.Tests.EditMode`（至少 `MainMenuUseCaseTests`、`StateTransitionTests`、`StatsCoreTests`）
- [ ] PlayMode：`Zephyr.Tests.PlayMode`（`VerticalSlicePlayModeTests`，冒烟级）
- [ ] 架构：`Zephyr.Architecture.Tests`
  - `ProjectAssemblyGraphIsExplicitAcyclicAndReviewed`
  - `CompiledScriptsHaveExplicitOwnersAndNoRuntimeEditorEdges`
  - `RuntimeScriptsDoNotImportUnityEditorOutsideEditorFolders`
  - `PhaseTwoDomainAssembliesExistWithExpectedDirection`
  - `DomainMigrationBridgesAreRemoved`

如果 `run_tests` 不可用/超时：记录为阻塞；可以用 `mcp__unity_mcp__Unity_RunCommand` 调项目测试入口兜底，但不得把其他 Unity 操作切到新 MCP。

**测试判定**：EditMode 应全绿；PlayMode 若命中已知既有失败，需 A/B 证明不是本单回归；其余失败一律按回归处理，不许写“测试全绿”。

## 4. 场景 / Prefab / SO

- [ ] 场景：打开 → 改 → 保存 → 刷新 → Console → `Unity_SceneView_Capture2DScene`
- [ ] 有任务文件夹时：结果图存 `<任务文件夹>/results/`（不存在就建）；无任务文件夹才用全局 `Assets/MCPTest/Screenshots/`
- [ ] 输入图只读；结果命名 `[yyyyMMdd_HHmmss]_[stage]_[场景名]_[说明].png`（stage: audit/before/after/review/final）
- [ ] 汇报 results 下 PNG 的绝对路径
- [ ] 检查相机 framing、层级、Collider、Spawner、CameraBounds、UI 锚点、`960 x 540`、裁切/重叠
- [ ] Prefab：根对象、组件、引用、Animator、Collider、材质、GUID
- [ ] SO：只读共享配置；没有运行时状态写回；引用可解析
- [ ] 没有手写 `.unity/.prefab/.asset/.meta` YAML

## 5. UI / 本地化

- [ ] 改动前后都有截图；控制器/评审用 `read_image` 看过；路径在 `evidence`
- [ ] 所有玩家可见文本走 Localization
- [ ] 中文 + 英文 Key 同时存在，fallback 正常
- [ ] 布局按 `960 x 540`；无裁切；Button 只调 Controller 公开方法
- [ ] UI Prefab 可替换；没有在代码里拼正式视图树

## 6. 战斗 / 玩法（涉及才做）

- [ ] 输入：Primary 左键 / Secondary 右键
- [ ] 状态转换：TransitionTable 有入口/出口条件
- [ ] 命中框、伤害、资源消耗、受击反馈、恢复流程
- [ ] PlayMode 走通 `Initializer -> Persistent -> MainMenu -> Tutorial_Phase_1`（或任务相关链）

## 7. 架构差异

- [ ] asmdef 依赖图与 `ArchitecturePolicy.json` 一致
- [ ] 没有新增旧 `Zephyr.Core` 消费者
- [ ] 没有新增直接单例读取 / 全局查找 / 直接场景 API
- [ ] 没有新增平行基础设施
- [ ] 任务前后指标对比已记录（恶化 = 阻塞）

## 8. 汇报（AGENTS.md §9）

- [ ] 改动路径（脚本/Prefab/场景/SO）
- [ ] Unity MCP 路由与工具
- [ ] Console Error/Warning
- [ ] 截图路径 + 实际观察
- [ ] 跑了哪些测试；哪些只能静态验证
- [ ] 未完成 / 残余风险 / 需要用户确认

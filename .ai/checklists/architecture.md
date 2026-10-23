# 架构门禁（AGENTS.md §4.5 / §4.6 浓缩）

> 用于架构师出方案、评审者查 diff、指挥做提交前判断。

## 1. 先分级

| 分级 | 触发 | 动作 |
|---|---|---|
| 普通功能 | 复用现有边界的敌人/武器/房间/掉落/UI/内容 | 可直接实现 |
| 跨模块 | 2+ 职责模块 | 先写六问；3+ 模块先给一页方案 |
| 架构专项 | 动 StateMachine、Persistent、GameManager、SceneLoader、asmdef、全局服务、迁移桥、Core 拆分 | 先批准，再实现 |

## 2. 硬性禁止（新代码）

- 平行 SceneLoader / 玩家生成 / 状态机 / 事件总线 / 战斗服务 / 资源加载 / 全局 Manager
- `DontDestroyOnLoad` 伪生命周期
- `SceneManager.LoadScene/UnloadScene`
- `Resources.Load`
- `GameObject.Find` / `FindObjectOfType` / `FindAnyObjectByType` / 全局 Tag
- 新领域/UI 直接读 `GameFlow.Instance` / `SaveSystem.Instance` / `LocalizationManager.Instance` / `PlayerSpawner.Instance`
- 新代码依赖 `Zephyr.Core` 兼容宿主
- 新增领域反向引用 / 循环依赖 / 未说明的宽引用
- SO 存运行时可变状态
- 运行时路径查找替代 Inspector/Prefab/SO 引用
- UI 硬编码可见文案 / 代码拼正式视图树

## 3. 允许的单例边界

只有 `Initializer`、`Persistent`、`GameManager` 或明确声明的组合边界可以解析具体 `*.Instance`，并缓存为接口/服务引用；业务方法不得反复读。

## 4. asmdef 与策略

- 新增/删除 asmdef，或修改 3+ 程序集依赖 → 架构专项
- 任何 asmdef 变更都必须同步 `Assets/Scripts/Tests/Architecture/ArchitecturePolicy.json`
- `Zephyr.Architecture.Tests` 必须通过：
  - 依赖图无环
  - 程序集集合与 policy 完全一致
  - 无 `Zephyr.Core` 残留依赖
  - runtime 不引用 editor-only / test 程序集
  - 每个编译脚本有明确 owner

## 5. 数据与资源

- SO：共享配置/作者数据；运行时状态放 materialized action / MonoBehaviour / 实例
- 跨场景状态走 Save/Meta Gateway
- 资产按 §5.1 目录；不在 `Assets/` 根目录建文件
- 新资源必须有合法唯一 GUID，不复制 `.meta`

## 6. 验收

任务结束前对比任务前后：

1. 平行基础设施：0 新增
2. 旧 Core 消费者：0 新增
3. 直接单例读取 / 静态可变 / 场景扫描 / 运行时 UI 构造：不增加
4. 配置/运行时/存档 owner：明确
5. Console / 测试 / 场景 / Prefab 检查：有证据
6. 设计文档：已同步

任何一项恶化 → 修复，或在报告中列为架构专项阻塞项。

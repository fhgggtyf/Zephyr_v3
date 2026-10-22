# Zephyr 底层架构详细设计（草案）

## 战斗反馈与 Hitback

伤害反馈由 `DamageInfo` 携带不可变的 `DamageFeedbackSettings` 快照，来源可以是 `WeaponSO`、敌人攻击定义或测试攻击组件。快照只保存强度、持续时间、优先级、冷却组和粒子逻辑 ID，不保存粒子 Prefab 或运行时状态。

`ICombatFeedbackGateway` 是跨领域契约。GameManager 场景中的 `CombatFeedbackCoordinator` 是唯一的运行时协调实现，负责把伤害反馈分发到 Hitstop、GameplayTimePause 和 Gameplay 相机震动。玩家、敌人和武器只提交反馈请求，不直接修改 `Time.timeScale`，也不直接查找或管理相机。

普通 Hitstop 使用强度计算持续时间，并通过短冷却与优先级合并请求；更高优先级的请求可以延长当前 Hitstop，低优先级请求在当前窗口内不会反复打断。Hitback 反馈使用更高优先级、独立冷却和更长上限。玩家的一个 Hitback Episode 只发送一次大 Hitstop，连续命中由 PlayerImpactController 的累计硬直和 Hitback 状态边界统一收口。

`GameplayTimePause` 使用 owner 集合叠加 UI、GameFlow 和战斗 Hitstop 暂停，任何一个 owner 仍有效时都不会错误恢复时间。Coordinator 使用 `Time.unscaledTime` 管理结束时间，因此 Hitstop 期间仍能正常结束并允许相机震动使用非缩放时间。

粒子表现通过 `ImpactVfxId` 进入后续 Catalog/Pool 分发层。DamageInfo 不持有粒子资源引用，具体粒子资产由表现层按 ID 解析；这样同一份伤害数据可以被玩家攻击、敌人攻击和测试攻击复用。

测试用敌人 `Assets/Prefabs/Test/HitbackComboTestEnemy.prefab` 复用普通敌人 Prefab 的 StateMachine、Health、Motor、碰撞体和攻击组件，并额外挂载 `HitbackComboTestEnemy`。该组件通过现有玩家目标上下文取得目标，在配置的总时长内均匀分布指定次数的伤害，用于验证累计受击、Hitback、大 Hitstop、震动和反馈冷却。

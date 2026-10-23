# Zephyr 底层架构详细设计（草案）

## 战斗反馈与 Hitback

伤害反馈由 `DamageInfo` 携带不可变的 `DamageFeedbackSettings` 快照，来源可以是 `WeaponSO`、敌人攻击定义或测试攻击组件。快照只保存强度、持续时间、优先级、冷却组和粒子逻辑 ID，不保存粒子 Prefab 或运行时状态。

`ICombatFeedbackGateway` 是跨领域契约。GameManager 场景中的 `CombatFeedbackCoordinator` 是唯一的运行时协调实现，负责把伤害反馈分发到 Hitstop、GameplayTimePause 和 Gameplay 相机震动。玩家、敌人和武器只提交反馈请求，不直接修改 `Time.timeScale`，也不直接查找或管理相机。

普通 Hitstop 使用强度计算持续时间，并通过短冷却与优先级合并请求；更高优先级的请求可以延长当前 Hitstop，低优先级请求在当前窗口内不会反复打断。Hitback 反馈使用更高优先级、独立冷却和更长上限。玩家的一个 Hitback Episode 只发送一次大 Hitstop，连续命中由 PlayerImpactController 的累计硬直和 Hitback 状态边界统一收口。

`GameplayTimePause` 使用 owner 集合叠加 UI、GameFlow 和战斗 Hitstop 暂停，任何一个 owner 仍有效时都不会错误恢复时间。Coordinator 使用 `Time.unscaledTime` 管理结束时间，因此 Hitstop 期间仍能正常结束并允许相机震动使用非缩放时间。

Actor 的第一手受击入口是 `IndividualDamageHandler`，对外统一暴露 `IDamageReceiver.ReceiveDamage(DamageInfo, Vector2)`。命中框、投射物、Boss 命中逻辑、持续伤害和测试攻击都只依赖这个契约；IDH 同步调用程序集内的 `HealthComponent.ApplyDamage`，通过返回的 `DamageResolution` 判断是否真的结算出有效伤害，再把反馈请求提交给 `ICombatFeedbackGateway`。`HealthComponent` 不再实现公开伤害接口，也不再暴露 `TakeDamage`；它继续持有生命值、护盾和 `DamageTaken`/`Died` 事件，供玩家和敌人的局部受击逻辑使用。IDH 不订阅 `DamageTaken`，避免出现“IDH -> HC -> IDH”的往返通信和重复反馈。

粒子表现通过 `ImpactVfxId` 进入后续 Catalog/Pool 分发层。DamageInfo 不持有粒子资源引用，具体粒子资产由表现层按 ID 解析；这样同一份伤害数据可以被玩家攻击、敌人攻击和测试攻击复用。

测试用敌人 `Assets/Prefabs/Test/HitbackComboTestEnemy.prefab` 复用普通敌人 Prefab 的 StateMachine、Health、Motor、碰撞体和攻击组件，并额外挂载 `HitbackComboTestEnemy`。该组件通过现有玩家目标上下文取得目标，在配置的总时长内均匀分布指定次数的伤害，用于验证累计受击、Hitback、大 Hitstop、震动和反馈冷却。

## 普通敌人行为组合

普通敌人沿用玩家共用的 StateMachine 基础设施，不增加独立的敌人状态机运行时。每个敌人由专属 `EnemySO`、`TransitionTableSO`、State SO、Condition SO 和 Action SO 组合；SO 只保存共享配置，实例运行状态保留在 materialized action/condition 或 `EnemyBrain`、`EnemyMotor` 等组件中。TransitionTable 负责状态选择，例如 Chase 仅在目标进入攻击范围且攻击冷却完成后转入 Telegraph；Attack 状态动作通过 `EnemyAttackController` 选择并启动攻击模组。

`NewBasicEnemy` 是该组合方式的简单实例，根对象继续复用 `StateMachine`、`EnemyStats`、`HealthComponent`、`EnemyMotor`、`EnemyBrain`、`EnemyAttackController` 和近战命中框。各 StateAction 负责单一状态行为，条件单独表达转换判断；移动仍交给 `EnemyMotor`。方向是 Motor 的运行时 Facing，由 `EnemyAnimation` 将 Idle/Patrol/Chase 映射到 Aseprite `idleleft`、`idleright`、`walkleft`、`walkright` tag，`EnemyVisual` 在使用方向帧时关闭额外 Sprite 镜像，避免动作帧和 Facing 产生双重翻转。没有独立 Facing GameObject 或第二套朝向系统。

普通敌人和 Boss 共用 StateMachine 底层契约，但不要求共用同一个行为 Prefab：Boss 的阶段编排、阶段专属状态和变招由 Boss 上层组合负责，不把阶段逻辑塞进普通敌人 Action，也不修改基础 StateMachine。新增普通敌人优先以独立配置资产组合已有 Actor 组件和职责单一的 State SO；超出普通行为范围的 Boss 专属阶段编排应作为上层行为扩展实现。

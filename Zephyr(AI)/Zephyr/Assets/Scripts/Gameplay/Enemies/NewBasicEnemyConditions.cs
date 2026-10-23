
using UnityEngine;
using Zephyr.Core.StateMachine;
using Zephyr.Core.StateMachine.ScriptableObjects;
using CoreSM = Zephyr.Core.StateMachine.StateMachine;

namespace Zephyr.Gameplay.Enemies
{
    public abstract class NewBasicEnemyConditionSO<T> : StateConditionSO<T> where T : NewBasicEnemyCondition, new() { }

    public abstract class NewBasicEnemyCondition : Condition
    {
        protected EnemyBrain Brain { get; private set; }
        public override void Awake(CoreSM sm) => Brain = sm.GetCachedComponent<EnemyBrain>();
    }
    public sealed class NewBasicEnemyCanEngageCondition : NewBasicEnemyCondition
    {
        protected override bool Statement() => Brain != null && !Brain.IsDead && Brain.HasTarget && Brain.PlayerWithin(Brain.Stats.DetectionRange);
    }
    public sealed class NewBasicEnemyAlertFinishedCondition : NewBasicEnemyCondition
    {
        protected override bool Statement() => Brain != null && Brain.PhaseFinished;
    }
    public sealed class NewBasicEnemyInAttackRangeCondition : NewBasicEnemyCondition
    {
        protected override bool Statement() => Brain != null && !Brain.IsDead && Brain.PlayerWithin(Brain.Stats.AttackRange);
    }
    public sealed class NewBasicEnemyAttackReadyCondition : NewBasicEnemyCondition
    {
        protected override bool Statement() => Brain != null && !Brain.IsDead && Brain.IsAttackReady;
    }
    public sealed class NewBasicEnemyTelegraphFinishedCondition : NewBasicEnemyCondition
    {
        protected override bool Statement() => Brain != null && Brain.PhaseFinished;
    }
    public sealed class NewBasicEnemyAttackFinishedCondition : NewBasicEnemyCondition
    {
        protected override bool Statement() => Brain != null && Brain.AttackFinished;
    }
    public sealed class NewBasicEnemyHurtCondition : NewBasicEnemyCondition
    {
        protected override bool Statement() => Brain != null && !Brain.IsDead && Brain.WasRecentlyDamaged;
    }
    public sealed class NewBasicEnemyHurtFinishedCondition : NewBasicEnemyCondition
    {
        protected override bool Statement() => Brain != null && Brain.PhaseFinished;
    }
    public sealed class NewBasicEnemyDeadCondition : NewBasicEnemyCondition
    {
        protected override bool Statement() => Brain != null && Brain.IsDead;
    }
    public sealed class NewBasicEnemyLoseTargetCondition : NewBasicEnemyCondition
    {
        protected override bool Statement() => Brain == null || !Brain.PlayerWithinLoseRange();
    }
}

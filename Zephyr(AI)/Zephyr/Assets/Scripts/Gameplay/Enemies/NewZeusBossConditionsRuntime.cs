using Zephyr.Core.StateMachine;
using Zephyr.Core.StateMachine.ScriptableObjects;
using CoreSM = Zephyr.Core.StateMachine.StateMachine;

namespace Zephyr.Gameplay.Enemies
{
    public abstract class NewZeusBossCondition : Condition
    {
        protected BossController Boss { get; private set; }
        protected EnemyBrain Brain { get; private set; }

        public override void Awake(CoreSM stateMachine)
        {
            Boss = stateMachine.GetCachedComponent<BossController>();
            Brain = stateMachine.GetCachedComponent<EnemyBrain>();
        }
    }

    public sealed class NewZeusBossHasTargetCondition : NewZeusBossCondition
    {
        protected override bool Statement() => Brain != null && !Brain.IsDead && Brain.HasTarget;
    }

    public sealed class NewZeusBossCanSelectAttackCondition : NewZeusBossCondition
    {
        protected override bool Statement()
        {
            Boss?.RefreshStage();
            return Boss != null && Boss.CanSelectAttack;
        }
    }

    public sealed class NewZeusBossHasSelectedAttackCondition : NewZeusBossCondition
    {
        protected override bool Statement() => Boss != null && Boss.HasSelectedAttack;
    }

    public sealed class NewZeusBossAttackFinishedCondition : NewZeusBossCondition
    {
        protected override bool Statement() => Boss != null && Boss.IsAttackFinished;
    }

    public sealed class NewZeusBossIsStageTwoCondition : NewZeusBossCondition
    {
        protected override bool Statement()
        {
            Boss?.RefreshStage();
            return Boss != null && Boss.IsStageTwo;
        }
    }

    public sealed class NewZeusBossSurroundFinishedCondition : NewZeusBossCondition
    {
        protected override bool Statement() => Boss != null && Boss.IsSurroundFinished;
    }

    public sealed class NewZeusBossDeadCondition : NewZeusBossCondition
    {
        protected override bool Statement() => Brain == null || Brain.IsDead;
    }
}

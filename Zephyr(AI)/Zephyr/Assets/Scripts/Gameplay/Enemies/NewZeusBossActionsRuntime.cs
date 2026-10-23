using UnityEngine;
using Zephyr.Core.StateMachine;
using CoreSM = Zephyr.Core.StateMachine.StateMachine;

namespace Zephyr.Gameplay.Enemies
{
    public abstract class NewZeusBossAction : StateAction
    {
        protected BossController Boss { get; private set; }
        protected EnemyBrain Brain { get; private set; }
        protected EnemyAnimation Animation;

        public override void Awake(CoreSM stateMachine)
        {
            Boss = stateMachine.GetCachedComponent<BossController>();
            Brain = stateMachine.GetCachedComponent<EnemyBrain>();
            stateMachine.TryGetCachedComponent(out Animation);
        }
    }

    public sealed class NewZeusBossApproachAction : NewZeusBossAction
    {
        public override void OnStateEnter() => Animation?.Play(EnemyActionKind.Chase);
        public override void OnUpdate() => Boss?.TickApproach();
        public override void OnStateExit() => Brain?.Motor.Stop();
    }

    public sealed class NewZeusBossSelectAttackAction : NewZeusBossAction
    {
        public override void OnStateEnter() => Boss?.SelectNextAttack();
        public override void OnUpdate() => Boss?.StopForStateMachine();
    }

    public sealed class NewZeusBossAttackAction : NewZeusBossAction
    {
        public override void OnStateEnter()
        {
            Boss?.BeginSelectedAttack();
        }

        public override void OnUpdate() => Boss?.TickCurrentAttack();
        public override void OnStateExit() => Boss?.StopForStateMachine();
    }

    public sealed class NewZeusBossSurroundAction : NewZeusBossAction
    {
        public override void OnStateEnter() => Animation?.Play(EnemyActionKind.Attack);
        public override void OnUpdate() => Boss?.TickStageTwoSurround();
        public override void OnStateExit() => Brain?.Motor.Stop();
    }

    public sealed class NewZeusBossDeathAction : NewZeusBossAction
    {
        public override void OnStateEnter()
        {
            Brain?.Motor.Stop();
            Animation?.Play(EnemyActionKind.Death);
        }

        public override void OnUpdate() => Brain?.Motor.Stop();
    }
}

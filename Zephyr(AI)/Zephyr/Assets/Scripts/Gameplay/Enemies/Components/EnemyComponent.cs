using UnityEngine;

namespace Zephyr.Gameplay.Enemies
{
    /// <summary>Runtime behavior generated from EnemyComponentData.</summary>
    public abstract class EnemyComponent : MonoBehaviour
    {
        protected EnemyAttackController Controller { get; private set; }
        protected EnemyComponentData Data { get; private set; }

        public void Initialize(EnemyAttackController controller, EnemyComponentData data)
        {
            UnbindController();
            Controller = controller;
            Data = data;
            BindController();
            OnInitialized();
        }

        public void Shutdown()
        {
            UnbindController();
            Controller = null;
            Data = null;
            OnShutdown();
        }

        protected virtual void OnEnable() => BindController();
        protected virtual void OnDisable() => UnbindController();
        protected virtual void OnInitialized() { }
        protected virtual void OnShutdown() { }
        protected virtual void HandleAttackStarted(int attackIndex) { }
        protected virtual void HandleAttackEnded() { }
        protected virtual void HandleHitWindowOpened() { }
        protected virtual void HandleHitWindowClosed() { }

        private void BindController()
        {
            if (Controller == null) return;
            UnbindController();
            Controller.AttackStarted += HandleAttackStarted;
            Controller.AttackEnded += HandleAttackEnded;
            Controller.HitWindowOpened += HandleHitWindowOpened;
            Controller.HitWindowClosed += HandleHitWindowClosed;
        }

        private void UnbindController()
        {
            if (Controller == null) return;
            Controller.AttackStarted -= HandleAttackStarted;
            Controller.AttackEnded -= HandleAttackEnded;
            Controller.HitWindowOpened -= HandleHitWindowOpened;
            Controller.HitWindowClosed -= HandleHitWindowClosed;
        }
    }

    public abstract class EnemyComponent<TData, TAttackData> : EnemyComponent
        where TData : EnemyComponentData<TAttackData>
        where TAttackData : EnemyAttackData, new()
    {
        protected TData ComponentData { get; private set; }
        protected TAttackData CurrentAttackData { get; private set; }

        protected override void OnInitialized()
        {
            ComponentData = Data as TData;
        }

        protected override void HandleAttackStarted(int attackIndex)
        {
            CurrentAttackData = ComponentData?.GetAttackData(attackIndex);
        }

        protected override void OnShutdown()
        {
            ComponentData = null;
            CurrentAttackData = null;
        }
    }
}

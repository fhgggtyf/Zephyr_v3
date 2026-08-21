using UnityEngine;

namespace Zephyr.Core.Weapons
{
    public abstract class WeaponComponent : MonoBehaviour
    {
        protected WeaponRuntime Runtime { get; private set; }
        protected ComponentData Data { get; private set; }

        public void Initialize(WeaponRuntime runtime, ComponentData data)
        {
            UnbindRuntime();
            Runtime = runtime;
            Data = data;
            BindRuntime();
            OnInitialized();
        }

        public void Shutdown()
        {
            UnbindRuntime();
            Runtime = null;
            Data = null;
            OnShutdown();
        }

        protected virtual void OnEnable()
        {
            BindRuntime();
        }

        protected virtual void OnDisable()
        {
            UnbindRuntime();
        }

        protected virtual void OnInitialized()
        {
        }

        protected virtual void OnShutdown()
        {
        }

        protected virtual void HandleAttackStarted(int comboIndex)
        {
        }

        protected virtual void HandleAttackEnded()
        {
        }

        protected virtual void HandleHitWindowOpened()
        {
        }

        protected virtual void HandleHitWindowClosed()
        {
        }

        private void BindRuntime()
        {
            if (Runtime == null) return;

            Runtime.AttackStarted -= HandleAttackStarted;
            Runtime.AttackEnded -= HandleAttackEnded;
            Runtime.HitWindowOpened -= HandleHitWindowOpened;
            Runtime.HitWindowClosed -= HandleHitWindowClosed;
            Runtime.AttackStarted += HandleAttackStarted;
            Runtime.AttackEnded += HandleAttackEnded;
            Runtime.HitWindowOpened += HandleHitWindowOpened;
            Runtime.HitWindowClosed += HandleHitWindowClosed;
        }

        private void UnbindRuntime()
        {
            if (Runtime == null) return;

            Runtime.AttackStarted -= HandleAttackStarted;
            Runtime.AttackEnded -= HandleAttackEnded;
            Runtime.HitWindowOpened -= HandleHitWindowOpened;
            Runtime.HitWindowClosed -= HandleHitWindowClosed;
        }
    }

    public abstract class WeaponComponent<TData, TAttackData> : WeaponComponent
        where TData : ComponentData<TAttackData>
        where TAttackData : AttackData, new()
    {
        protected TData ComponentData { get; private set; }
        protected TAttackData CurrentAttackData { get; private set; }

        protected override void OnInitialized()
        {
            ComponentData = Data as TData;
        }

        protected override void HandleAttackStarted(int comboIndex)
        {
            CurrentAttackData = ComponentData?.GetAttackData(comboIndex);
        }

        protected override void OnShutdown()
        {
            ComponentData = null;
            CurrentAttackData = null;
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Zephyr.Gameplay.Enemies
{
    /// <summary>
    /// Materializes the component data declared by EnemySO once for this enemy
    /// instance. Regular enemies keep their attacks on EnemySO and never create
    /// a WeaponSO/WeaponRuntime.
    /// </summary>
    [DefaultExecutionOrder(-350)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyStats), typeof(EnemyAttackController))]
    public sealed class EnemyComponentGenerator : MonoBehaviour
    {
        private readonly List<EnemyComponent> _activeComponents = new List<EnemyComponent>();
        private EnemyAttackController _attackController;
        private EnemyStats _stats;

        private void Awake()
        {
            Generate();
        }

        public void Generate()
        {
            _stats ??= GetComponent<EnemyStats>();
            _attackController ??= GetComponent<EnemyAttackController>();
            _attackController.Initialize(GetComponent<EnemyBrain>(), _stats);

            foreach (EnemyComponent component in _activeComponents)
                if (component != null) component.Shutdown();
            _activeComponents.Clear();

            var requiredTypes = new HashSet<Type>();
            EnemySO config = _stats != null ? _stats.Config : null;
            if (config != null)
            {
                foreach (EnemyComponentData data in config.Components)
                {
                    Type dependency = data?.ComponentDependency;
                    if (dependency == null || !typeof(EnemyComponent).IsAssignableFrom(dependency)) continue;
                    if (!requiredTypes.Add(dependency)) continue;

                    EnemyComponent component = GetComponent(dependency) as EnemyComponent;
                    if (component == null)
                        component = gameObject.AddComponent(dependency) as EnemyComponent;

                    if (component == null) continue;
                    component.Initialize(_attackController, data);
                    _activeComponents.Add(component);
                }
            }

            foreach (EnemyComponent existing in GetComponents<EnemyComponent>())
            {
                if (requiredTypes.Contains(existing.GetType())) continue;
                existing.Shutdown();
                _activeComponents.Remove(existing);
                Destroy(existing);
            }
        }

        private void OnDestroy()
        {
            foreach (EnemyComponent component in _activeComponents)
                if (component != null) component.Shutdown();
            _activeComponents.Clear();
            _attackController?.Shutdown();
        }
    }
}

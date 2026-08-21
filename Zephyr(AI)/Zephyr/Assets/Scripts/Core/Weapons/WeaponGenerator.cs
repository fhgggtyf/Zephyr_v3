using System;
using System.Collections.Generic;
using UnityEngine;

namespace Zephyr.Core.Weapons
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(WeaponRuntime))]
    public class WeaponGenerator : MonoBehaviour
    {
        private readonly List<WeaponComponent> m_activeComponents = new List<WeaponComponent>();
        private WeaponRuntime m_runtime;

        private void Awake()
        {
            m_runtime = GetComponent<WeaponRuntime>();
        }

        public void Generate(WeaponSO weapon)
        {
            if (m_runtime == null) m_runtime = GetComponent<WeaponRuntime>();

            foreach (WeaponComponent activeComponent in m_activeComponents)
            {
                if (activeComponent != null) activeComponent.Shutdown();
            }

            m_activeComponents.Clear();
            var requiredTypes = new HashSet<Type>();
            if (weapon != null)
            {
                foreach (ComponentData data in weapon.Components)
                {
                    if (data == null || data.ComponentDependency == null) continue;
                    if (!typeof(WeaponComponent).IsAssignableFrom(data.ComponentDependency)) continue;
                    if (!requiredTypes.Add(data.ComponentDependency)) continue;

                    WeaponComponent component = GetComponent(data.ComponentDependency) as WeaponComponent;
                    if (component == null)
                    {
                        component = gameObject.AddComponent(data.ComponentDependency) as WeaponComponent;
                    }

                    component.Initialize(m_runtime, data);
                    m_activeComponents.Add(component);
                }
            }

            WeaponComponent[] existingComponents = GetComponents<WeaponComponent>();
            foreach (WeaponComponent component in existingComponents)
            {
                if (requiredTypes.Contains(component.GetType())) continue;

                component.Shutdown();
                m_activeComponents.Remove(component);
                Destroy(component);
            }

            EnsureAnimEventHandler();
        }

        private void EnsureAnimEventHandler()
        {
            Animator childAnimator = GetComponentInChildren<Animator>();
            if (childAnimator == null) return;

            WeaponAnimEventHandler handler = childAnimator.GetComponent<WeaponAnimEventHandler>();
            if (handler == null)
            {
                childAnimator.gameObject.AddComponent<WeaponAnimEventHandler>();
            }
        }

        private void OnDestroy()
        {
            foreach (WeaponComponent component in m_activeComponents)
            {
                if (component != null) component.Shutdown();
            }

            m_activeComponents.Clear();
        }
    }
}

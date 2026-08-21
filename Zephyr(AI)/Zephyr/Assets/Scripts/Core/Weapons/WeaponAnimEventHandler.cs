/*
 * WeaponAnimEventHandler.cs
 * -------------------------
 * Module:  Core / Weapons
 * Purpose: Bridge between animation events on the Weapon child's Animator and the
 *          WeaponRuntime on the parent WeaponSocket. Unity animation events can
 *          only call methods on MonoBehaviours attached to the same GameObject as
 *          the Animator, so this component forwards those calls up to the runtime.
 * Scene:    Attached to the Weapon child object (the one with the Animator).
 */
using UnityEngine;

namespace Zephyr.Core.Weapons
{
    [DisallowMultipleComponent]
    public class WeaponAnimEventHandler : MonoBehaviour
    {
        private WeaponRuntime m_runtime;

        private void Awake()
        {
            m_runtime = GetComponentInParent<WeaponRuntime>();
        }

        private void OnEnable()
        {
            if (m_runtime == null) m_runtime = GetComponentInParent<WeaponRuntime>();
        }

        public void BeginHitWindow()
        {
            m_runtime?.BeginHitWindow();
        }

        public void EndHitWindow()
        {
            m_runtime?.EndHitWindow();
        }

        public void FinishAttack()
        {
            m_runtime?.FinishAttack();
        }
    }
}
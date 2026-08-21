using UnityEngine;

namespace Zephyr.Gameplay.Enemies
{
    /// <summary>
    /// Optional behavior that lets an enemy keep facing its target while the
    /// Telegraph state is active. The component's presence is the opt-in.
    /// </summary>
    [RequireComponent(typeof(EnemyBrain))]
    public sealed class EnemyTelegraphTurnComponent : EnemyComponent
    {
        private EnemyBrain _brain;

        private void Awake()
        {
            _brain = GetComponent<EnemyBrain>();
        }

        private void Update()
        {
            if (_brain == null || !_brain.IsTelegraphing || _brain.IsDead || !_brain.HasTarget)
                return;

            _brain.FaceTarget();
        }
    }
}

using UnityEngine;

namespace Zephyr.Gameplay.Enemies
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class EnemyVisual : MonoBehaviour
    {
        [SerializeField] private EnemyMotor _motor;
        private SpriteRenderer _renderer;

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            _motor ??= GetComponentInParent<EnemyMotor>();
        }
        private void LateUpdate()
        {
            if (_renderer != null && _motor != null) _renderer.flipX = _motor.Facing.x > 0f;
        }
    }
}

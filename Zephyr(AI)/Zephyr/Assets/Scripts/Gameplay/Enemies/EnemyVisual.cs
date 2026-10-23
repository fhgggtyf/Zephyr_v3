using UnityEngine;

namespace Zephyr.Gameplay.Enemies
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class EnemyVisual : MonoBehaviour
    {
        [SerializeField] private EnemyMotor _motor;
        [SerializeField] private Animator _animator;
        [SerializeField] private bool _useDirectionalAsepriteTags;

        private SpriteRenderer _renderer;

private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            _motor ??= GetComponentInParent<EnemyMotor>();
            _animator ??= GetComponentInParent<Animator>();
        }
private void LateUpdate()
        {
            if (_renderer == null || _motor == null) return;
            bool usingDirectionalClip = false;
            if (_useDirectionalAsepriteTags && _animator != null && _animator.isActiveAndEnabled)
            {
                AnimatorStateInfo state = _animator.GetCurrentAnimatorStateInfo(0);
                usingDirectionalClip = state.IsName("walkleft") || state.IsName("walkright")
                    || state.IsName("idleleft") || state.IsName("idleright");
            }
            _renderer.flipX = !usingDirectionalClip && _motor.Facing.x > 0f;
        }
    }
}

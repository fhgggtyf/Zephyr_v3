using UnityEngine;

namespace Zephyr.Gameplay.Enemies
{
    /// <summary>Maps enemy state actions to Animator states created from Aseprite tags.</summary>
    [RequireComponent(typeof(Animator))]
    public sealed class EnemyAnimation : MonoBehaviour
    {
        [SerializeField] private Animator _animator;
        [Header("Aseprite animation tags")]
        [SerializeField] private string _idle = "Patrol";
        [SerializeField] private string _patrol = "Patrol";
        [SerializeField] private string _alert = "Alert";
        [SerializeField] private string _chase = "Chase";
        [SerializeField] private string _telegraph = "Telegraph";
        [SerializeField] private string _attack = "Attack";
        [SerializeField] private string _hurt = "Hurt";
        [SerializeField] private string _death = "Death";
        [SerializeField, Min(0f)] private float _crossFade = 0.05f;

        private int _currentHash;

        private void Awake() => _animator ??= GetComponent<Animator>();

        public void Play(EnemyActionKind kind, float stateDuration = -1f)
        {
            string state = kind switch
            {
                EnemyActionKind.Patrol => _patrol,
                EnemyActionKind.Alert => _alert,
                EnemyActionKind.Chase => _chase,
                EnemyActionKind.Telegraph => _telegraph,
                EnemyActionKind.Attack => _attack,
                EnemyActionKind.Hurt => _hurt,
                EnemyActionKind.Death => _death,
                _ => _idle
            };

            PlayState(state, kind == EnemyActionKind.Attack, stateDuration);
        }

        public void PlayAttack(string animationState, float stateDuration)
        {
            PlayState(string.IsNullOrWhiteSpace(animationState) ? _attack : animationState, true, stateDuration);
        }

        private void PlayState(string state, bool isAttack, float stateDuration)
        {

            if (string.IsNullOrWhiteSpace(state) || _animator == null || !_animator.isActiveAndEnabled ||
                _animator.runtimeAnimatorController == null)
                return;

            int hash = Animator.StringToHash(state);
            if (hash == _currentHash) return;
            if (_animator.HasState(0, hash))
            {
                _animator.speed = 1f;
                _animator.CrossFadeInFixedTime(hash, _crossFade, 0, 0f);
                if (isAttack && stateDuration > 0f)
                {
                    foreach (AnimationClip clip in _animator.runtimeAnimatorController.animationClips)
                    {
                        if (clip != null && clip.name == state)
                        {
                            _animator.speed = clip.length / stateDuration;
                            break;
                        }
                    }
                }
                _currentHash = hash;
            }
            else
            {
                Debug.LogWarning($"EnemyAnimation: Animator state/tag '{state}' was not found.", this);
            }
        }
    }
}

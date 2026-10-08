/*
 * SpriteAnimator.cs
 * ------------------
 * Module:  Player / Visual
 * Purpose: Visual presentation layer for the player sprite. Reads logical state
 *          from IFacingProvider (MovementCore) and mirrors it visually:
 *          sprite flipping (X scale) and animation parameter updates.
 *          This component owns the SpriteRenderer and Animator on the "Sprite"
 *          child GameObject. No game logic lives here — it only reflects state.
 *          Separation: MovementCore = physics + logical state, SpriteAnimator = visuals.
 * Scene:    GameManager (attached to player prefab's "Sprite" child).
 * Ch.Ref:   Ch.6.7 Owner Binding, Ch.6.10 Presentation Layer.
 */
using UnityEngine;
using Zephyr.Core.Interfaces;
using Zephyr.Gameplay.Player.Core;

namespace Zephyr.Gameplay.Player.Visual
{
    /// <summary>
    /// Visual presenter for the player sprite. Reads IFacingProvider from the
    /// parent hierarchy and flips the sprite to match facing direction.
    /// Also exposes animation control (PlayAnimation, SetFloat, SetTrigger)
    /// for state actions to drive the Animator without depending on it directly.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class SpriteAnimator : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Facing provider to read direction from. Auto-resolved if null.")]
        [SerializeField] private MovementCore _facingProvider;

        [Header("Visual Settings")]
        [Tooltip("If true, flips sprite on X scale instead of using SpriteRenderer.flipX")]
        [SerializeField] private bool _useScaleFlipping = true;

        private SpriteRenderer _spriteRenderer;
        private Animator _animator;
        private RuntimeAnimatorController _baseController;
        private AnimatorOverrideController _overrideController;
        private Vector2 _lastFacing = Vector2.right;
        private string _currentAnim;
        private int _currentAnimHash;


        /// <summary>
        /// True when the current roll animation has finished playing.
        /// Driven by an Animation Event on the roll clip calling
        /// <see cref="MarkRollComplete"/>. Reset in <see cref="PlayAnimation"/>
        /// when a new animation starts. Read by RollCompleteConditionSO.
        /// </summary>
        public bool IsRollComplete { get; private set; }

        /// <summary>
        /// True when the current jump peak animation has finished playing.
        /// Driven by an Animation Event on the jump peak clip calling
        /// <see cref="MarkPeakComplete"/>. Reset in <see cref="PlayAnimation"/>
        /// when a new animation starts. Read by JumpPeakToFallConditionSO.
        /// </summary>
        public bool IsPeakComplete { get; private set; }

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _animator = GetComponent<Animator>();
            _baseController = _animator != null ? _animator.runtimeAnimatorController : null;

            if (_facingProvider == null)
                _facingProvider = GetComponentInParent<MovementCore>();
        }

        private void LateUpdate()
        {
            if (_facingProvider == null) return;

            var currentFacing = _facingProvider.Facing;

            if (currentFacing != _lastFacing)
            {
                _lastFacing = currentFacing;
                ApplyFacingVisual(currentFacing);
            }
        }

        /// <summary>
        /// Plays a named animation state on the Animator with optional speed.
        /// Called by AnimatorParameterAction (state machine).
        /// Resets IsRollComplete flag so animation-completion conditions
        /// (e.g., roll) can detect the end via Animation Events.
        /// </summary>
        public void PlayAnimation(string animName, float speed = 1f, bool restart = false)
        {
            if (_animator == null || string.IsNullOrEmpty(animName)) return;
            if (!restart && _currentAnim == animName) return;

            _animator.speed = speed;
            _animator.Play(animName, 0, 0f);
            _currentAnim = animName;
            _currentAnimHash = Animator.StringToHash(animName);
            IsRollComplete = false;
            IsPeakComplete = false;
        }

        /// <summary>
        /// Plays a state from the player controller while replacing the
        /// authored placeholder clip for that state with a weapon-specific
        /// body animation. The weapon Animator remains independent and is
        /// still driven by WeaponRuntime.
        /// </summary>
        public bool PlayAnimationOverride(
            string animName,
            AnimationClip placeholderClip,
            AnimationClip replacementClip,
            float speed = 1f,
            bool restart = true)
        {
            if (_animator == null || string.IsNullOrEmpty(animName)
                || placeholderClip == null || replacementClip == null)
                return false;

            EnsureOverrideController();
            if (_overrideController == null) return false;

            _overrideController[placeholderClip] = replacementClip;
            PlayAnimation(animName, speed, restart);
            return true;
        }

        /// <summary>
        /// Restores the player's base controller after a weapon attack is
        /// cancelled or completed. The normal state machine then owns the
        /// next locomotion animation as usual.
        /// </summary>
        public void RestoreBaseController()
        {
            if (_animator == null || _baseController == null) return;
            if (_animator.runtimeAnimatorController == _baseController) return;

            _animator.runtimeAnimatorController = _baseController;
            _overrideController = null;
            _currentAnim = null;
            _currentAnimHash = 0;
        }

        /// <summary>
        /// Returns true once the currently requested animation reaches its end.
        /// Normalized time follows the clip length and Animator playback speed,
        /// so gameplay completion stays synchronized with animation playback.
        /// </summary>
        public bool IsCurrentAnimationComplete()
        {
            if (_animator == null || _currentAnimHash == 0 || _animator.IsInTransition(0))
            {
                return false;
            }

            var stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
            var isCurrentAnimation = stateInfo.shortNameHash == _currentAnimHash
                || stateInfo.fullPathHash == _currentAnimHash;

            return isCurrentAnimation && stateInfo.normalizedTime >= 1f;
        }

        /// <summary>
        /// Sets a float parameter on the Animator. For state actions that
        /// drive blend trees or speed-based animation parameters.
        /// </summary>
        public void SetFloat(string paramName, float value)
        {
            if (_animator == null) return;
            _animator.SetFloat(paramName, value);
        }

        /// <summary>
        /// Sets a trigger parameter on the Animator. For one-shot animations
        /// (attack, hit, etc.) triggered by state transitions.
        /// </summary>
        public void SetTrigger(string paramName)
        {
            if (_animator == null) return;
            _animator.SetTrigger(paramName);
        }

        /// <summary>
        /// Called by an Animation Event on the roll clip to signal that
        /// the roll animation has finished playing. Sets <see cref="IsRollComplete"/>
        /// to true, which RollCompleteConditionSO reads to exit the roll state.
        /// Place an Animation Event at the end of the roll animation clip and
        /// bind it to this method.
        /// </summary>
        public void MarkRollComplete()
        {
            IsRollComplete = true;
        }

        /// <summary>
        /// Called by an Animation Event on the jump peak clip to signal that
        /// the peak transition animation has finished playing. Sets
        /// <see cref="IsPeakComplete"/> to true, which JumpPeakToFallConditionSO
        /// reads to transition from JumpPeak to JumpFall state.
        /// Place an Animation Event at the end of the jump peak animation clip and
        /// bind it to this method.
        /// </summary>
        public void MarkPeakComplete()
        {
            IsPeakComplete = true;
        }

        /// <summary>
        /// Applies the visual flip to match the logical facing direction.
        /// Supports two modes: scale flipping (scale.x sign) or SpriteRenderer.flipX.
        /// </summary>
        private void ApplyFacingVisual(Vector2 facing)
        {
            var facingRight = facing.x >= 0;

            if (_useScaleFlipping)
            {
                var scale = transform.localScale;
                scale.x = facingRight ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
                transform.localScale = scale;
            }
            else
            {
                _spriteRenderer.flipX = !facingRight;
            }
        }

        private void EnsureOverrideController()
        {
            if (_animator == null) return;
            if (_baseController == null)
                _baseController = _animator.runtimeAnimatorController;

            if (_baseController == null) return;
            if (_overrideController != null
                && _animator.runtimeAnimatorController == _overrideController)
                return;

            _overrideController = new AnimatorOverrideController(_baseController);
            _animator.runtimeAnimatorController = _overrideController;
        }
    }
}

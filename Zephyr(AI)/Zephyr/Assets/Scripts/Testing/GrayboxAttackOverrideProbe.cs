using UnityEngine;
using Zephyr.Gameplay.Player.Visual;

namespace Zephyr.Testing
{
    /// <summary>
    /// Isolated MCP graybox probe for the player animation override path.
    /// H plays the horizontal attack state and V plays the vertical attack
    /// state on the same SpriteAnimator used by the player state machine.
    /// This component is test-only and is not part of the gameplay prefab.
    /// </summary>
    public sealed class GrayboxAttackOverrideProbe : MonoBehaviour
    {
        [SerializeField] private SpriteAnimator m_spriteAnimator;
        [SerializeField] private string m_horizontalAnimation = "GrayboxAttackHorizontal";
        [SerializeField] private string m_verticalAnimation = "GrayboxAttackVertical";
        [SerializeField] private AnimationClip m_horizontalPlaceholder;
        [SerializeField] private AnimationClip m_horizontalReplacement;
        [SerializeField] private AnimationClip m_verticalPlaceholder;
        [SerializeField] private AnimationClip m_verticalReplacement;
        [SerializeField] private float m_playSpeed = 1f;
        [SerializeField] private bool m_autoCycle;
        [SerializeField] private float m_autoCycleInterval = 1.2f;

        private float m_nextCycleAt;
        private bool m_nextIsHorizontal = true;

        private void Awake()
        {
            if (m_spriteAnimator == null)
                m_spriteAnimator = GetComponentInChildren<SpriteAnimator>(true);
        }

        private void Update()
        {
            if (m_spriteAnimator == null) return;

            if (m_autoCycle && Time.time >= m_nextCycleAt)
            {
                PlayOverride(m_nextIsHorizontal);
                m_nextIsHorizontal = !m_nextIsHorizontal;
                m_nextCycleAt = Time.time + Mathf.Max(0.1f, m_autoCycleInterval);
            }

        }

        private void PlayOverride(bool horizontal)
        {
            AnimationClip replacement = horizontal ? m_horizontalReplacement : m_verticalReplacement;
            Debug.Log($"[MCPGraybox] Playing {(horizontal ? "Horizontal" : "Vertical")} override: {(replacement != null ? replacement.name : "<none>")}");
            m_spriteAnimator.PlayAnimationOverride(
                horizontal ? m_horizontalAnimation : m_verticalAnimation,
                horizontal ? m_horizontalPlaceholder : m_verticalPlaceholder,
                replacement,
                m_playSpeed,
                true);
        }
    }
}

/*
 * JumpFallActionSO.cs
 * -------------------
 * Module:  Player / StateMachine / Actions
 * Purpose: Applies fall-phase physics when entering the JumpFall state.
 *          Reads fall gravity multiplier and max fall speed from JumpDataSO.
 *          On enter: scales gravity by gravityScale * fallGravityMultiplier
 *          and sets the max fall speed cap on MovementCore.
 *          On exit: restores original gravity and clears the speed cap.
 * Scene:    GameManager (attached to player prefab).
 * Ch.Ref:   Ch.6.4 State Actions, Ch.6.8 Physics / Jump Mechanics.
 */
using UnityEngine;
using Zephyr.Core.StateMachine;
using CoreSM = Zephyr.Core.StateMachine.StateMachine;
using Zephyr.Core.StateMachine.ScriptableObjects;
using Zephyr.Gameplay.Player.Core;
using Zephyr.Gameplay.Player.Data;

namespace Zephyr.Gameplay.Player.StateMachine
{
    [CreateAssetMenu(fileName = "JumpFallAction", menuName = "State Machines/Actions/Player/Jump Fall")]
    public class JumpFallActionSO : StateActionSO
    {
        [Tooltip("Reference to the jump physics configuration SO.")]
        public JumpDataSO jumpData;

        protected override StateAction CreateAction() => new JumpFallAction(jumpData);
    }

    public class JumpFallAction : StateAction
    {
        private MovementCore _movementCore;
        private readonly JumpDataSO _jumpData;
        private bool _ownsGravity;
        private float _ownedGravityScale;
        private float _previousGravityScale;

        public JumpFallAction(JumpDataSO jumpData)
        {
            _jumpData = jumpData;
        }

        public override void Awake(CoreSM stateMachine)
        {
            _movementCore = stateMachine.GetCachedComponent<MovementCore>();
        }

        public override void OnStateEnter()
        {
            if (_jumpData == null || _movementCore == null)
            {
                Debug.LogWarning($"[JumpFallAction] Missing refs: jumpData={_jumpData != null}, movementCore={_movementCore != null}");
                return;
            }

            var fallGravity = _jumpData.gravityScale * _jumpData.fallGravityMultiplier;
            Debug.Log($"[JumpFallAction] Enter: gravityScale={_jumpData.gravityScale} * fallMultiplier={_jumpData.fallGravityMultiplier} = {fallGravity:F2}, maxFallSpeed={_jumpData.maxFallSpeed}");
            _ownedGravityScale = fallGravity;
            _previousGravityScale = _movementCore.SetGravityScale(_ownedGravityScale);
            _ownsGravity = true;
            _movementCore.SetMaxFallSpeed(_jumpData.maxFallSpeed);
        }

        public override void OnStateExit()
        {
            if (_movementCore == null) return;

            if (_ownsGravity)
            {
                _movementCore.RestoreGravityScale(_ownedGravityScale, _previousGravityScale);
                _ownsGravity = false;
            }

            _movementCore.SetMaxFallSpeed(0f);
        }

        public override void OnUpdate() { }
    }
}
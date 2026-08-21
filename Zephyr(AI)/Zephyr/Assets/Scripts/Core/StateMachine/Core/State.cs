/*
 * State.cs
 * --------
 * Module:  Core / StateMachine / Core
 * Purpose: Runtime state container created from a StateSO ScriptableObject. Holds arrays
 *          of StateTransition and StateAction, and a StateTag enum value for identification.
 *          Manages lifecycle: OnStateEnter (calls transitions' and actions' OnStateEnter),
 *          OnUpdate (calls actions' OnUpdate), OnStateExit (calls transitions' and actions'
 *          OnStateExit). Delegates transition evaluation to StateTransition.TryGetTransition.
 *          Also defines the StateTag enum (Idle, Walk, Run, Jump, DoubleJump, Dash, AirDash,
 *          Roll, Climb, Crouch, Channeling, Attack, Hurt, Death).
 * Dependencies: StateSO (origin SO reference), StateMachine (owning machine),
 *               StateTransition[], StateAction[].
 * Scene:    GameManager (runtime; created per entity from TransitionTableSO data).
 * Ch.Ref:   Ch.6 State Machine Architecture, Ch.6.2 State Lifecycle.
 */
using Zephyr.Core.StateMachine.ScriptableObjects;

namespace Zephyr.Core.StateMachine
{
    public class State
    {
        internal StateSO _originSO;
        internal StateMachine _stateMachine;
        internal StateTransition[] _transitions;
        internal StateAction[] _actions;

        public StateTag stateTag;

        internal State() { }

        public void OnStateEnter()
        {
            void OnStateEnter(IStateComponent[] comps)
            {
                for (int i = 0; i < comps.Length; i++)
                    comps[i].OnStateEnter();
            }
            OnStateEnter(_transitions);
            OnStateEnter(_actions);
        }

        public void OnUpdate()
        {
            for (int i = 0; i < _actions.Length; i++)
                _actions[i].OnUpdate();
        }

        public void OnStateExit()
        {
            void OnStateExit(IStateComponent[] comps)
            {
                for (int i = 0; i < comps.Length; i++)
                    comps[i].OnStateExit();
            }
            OnStateExit(_transitions);
            OnStateExit(_actions);
        }

        public bool TryGetTransition(out State state)
        {
            state = null;

            for (int i = 0; i < _transitions.Length; i++)
                if (_transitions[i].TryGetTransition(out state))
                    break;

            for (int i = 0; i < _transitions.Length; i++)
                _transitions[i].ClearConditionsCache();

            return state != null;
        }
    }

    public enum StateTag
    {
        Idle, Walk, Run, Jump, DoubleJump, Dash, AirDash, Roll,
        Climb, Crouch, Channeling, Attack, Hurt, Death
    }
}

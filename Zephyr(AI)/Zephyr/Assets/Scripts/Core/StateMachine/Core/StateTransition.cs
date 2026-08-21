/*
 * StateTransition.cs
 * ------------------
 * Module:  Core / StateMachine / Core
 * Purpose: Runtime transition between states. Implements IStateComponent. Evaluates whether
 *          the transition should fire based on its StateCondition[] array with AND/OR logic
 *          grouped by resultGroups. Transitions with zero conditions are unconditional
 *          (always fire). Provides TryGetTransition(out State) for the StateMachine to query
 *          each frame. Clears condition caches after evaluation to prevent stale results.
 *          Supports editor debugging via StateMachineDebugger.
 * Dependencies: State (target), StateCondition[] (condition set), IStateComponent interface.
 * Scene:    GameManager (runtime; created from TransitionTableSO data).
 * Ch.Ref:   Ch.6 State Machine Architecture, Ch.6.3 Transition Logic.
 */
using System.Collections.Generic;

namespace Zephyr.Core.StateMachine
{
    public class StateTransition : IStateComponent
    {
        private State _targetState;
        private StateCondition[] _conditions;
        private int[] _resultGroups;
        private bool[] _results;

        public StateTransition(State targetState, StateCondition[] conditions, int[] resultGroups = null)
        {
            Init(targetState, conditions, resultGroups);
        }

        internal void Init(State targetState, StateCondition[] conditions, int[] resultGroups = null)
        {
            _targetState = targetState;
            _conditions = conditions;
            _resultGroups = resultGroups != null && resultGroups.Length > 0 ? resultGroups : new int[1];
            _results = new bool[_resultGroups.Length];
        }

        /// <summary>
        /// Checks whether the conditions to transition to the target state are met.
        /// </summary>
        /// <param name="state">Returns the state to transition to. Null if the conditions aren't met.</param>
        /// <returns>True if the conditions are met.</returns>
        public bool TryGetTransition(out State state)
        {
            state = ShouldTransition() ? _targetState : null;
            return state != null;
        }

        public void OnStateEnter()
        {
            for (int i = 0; i < _conditions.Length; i++)
                _conditions[i]._condition.OnStateEnter();
        }

        public void OnStateExit()
        {
            for (int i = 0; i < _conditions.Length; i++)
                _conditions[i]._condition.OnStateExit();
        }

        private bool ShouldTransition()
        {
#if UNITY_EDITOR
            _targetState._stateMachine._debugger?.TransitionEvaluationBegin(_targetState._originSO.name);
#endif

            // A transition with no conditions is unconditional and always fires.
            bool ret = _conditions.Length == 0;

            if (!ret)
            {
                int count = _resultGroups.Length;
                for (int i = 0, idx = 0; i < count && idx < _conditions.Length; i++)
                {
                    for (int j = 0; j < _resultGroups[i]; j++, idx++)
                    {
                        _results[i] = j == 0 ? _conditions[idx].IsMet() : _results[i] && _conditions[idx].IsMet();
                    }
                }

                ret = false;
                for (int i = 0; i < count && !ret; i++)
                    ret = ret || _results[i];
            }

#if UNITY_EDITOR
            _targetState._stateMachine._debugger?.TransitionEvaluationEnd(ret, _targetState._actions);
#endif

            return ret;
        }

        internal void ClearConditionsCache()
        {
            for (int i = 0; i < _conditions.Length; i++)
                _conditions[i]._condition.ClearStatementCache();
        }
    }
}

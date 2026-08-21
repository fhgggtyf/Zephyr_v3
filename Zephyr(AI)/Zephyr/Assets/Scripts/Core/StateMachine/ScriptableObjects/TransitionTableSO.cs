/*
 * TransitionTableSO.cs
 * --------------------
 * Module:  Core / StateMachine / ScriptableObjects
 * Purpose: The main data container for a state machine's transition table. Authored in the
 *          Unity editor via the custom TransitionTableEditorWindow. Contains an array of
 *          TransitionItem structs (FromState → ToState + Conditions) and an optional
 *          _initialState override. At runtime, GetInitialState(StateMachine) instantiates
 *          all States, Transitions, Actions, and Conditions from the authored data using
 *          a createdStates dictionary for state graph deduplication. Handles AND/OR logic grouping
 *          via resultGroups. Created via CreateAssetMenu at "State Machines/Transition Table".
 * Dependencies: StateSO (states), StateConditionSO (conditions), TransitionItem/ConditionUsage
 *               (serialization structs).
 * Scene:    N/A (authored in Project window; referenced by StateMachine at runtime).
 * Ch.Ref:   Ch.6 State Machine Architecture, Ch.6.3 Transition Logic.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Zephyr.Core.StateMachine.ScriptableObjects
{
    [CreateAssetMenu(fileName = "NewTransitionTable", menuName = "State Machines/Transition Table")]
    public class TransitionTableSO : ScriptableObject
    {
        [SerializeField] private TransitionItem[] _transitions = default;

        [Tooltip("State to enter on Awake. If unset, the first FromState in the list is used.")]
        [SerializeField] private StateSO _initialState = default;

        /// <summary>
        /// Will get the initial state and instantiate all subsequent states, transitions, actions and conditions.
        /// </summary>
        internal State GetInitialState(StateMachine stateMachine)
        {
            Validate();

            var states = new List<State>();
            var transitions = new List<StateTransition>();
            var createdStates = new Dictionary<StateSO, State>();

            var fromStates = _transitions.GroupBy(transition => transition.FromState);

            foreach (var fromState in fromStates)
            {
                if (fromState.Key == null)
                    throw new ArgumentNullException(nameof(fromState.Key), $"TransitionTable: {name}");

                var state = fromState.Key.GetState(stateMachine, createdStates);
                states.Add(state);

                transitions.Clear();
                foreach (var transitionItem in fromState)
                {
                    if (transitionItem.ToState == null)
                        throw new ArgumentNullException(nameof(transitionItem.ToState), $"TransitionTable: {name}, From State: {fromState.Key.name}");

                    var toState = transitionItem.ToState.GetState(stateMachine, createdStates);
                    ProcessConditionUsages(stateMachine, transitionItem.Conditions, out var conditions, out var resultGroups);
                    transitions.Add(new StateTransition(toState, conditions, resultGroups));
                }

                state._transitions = transitions.ToArray();
            }

            if (states.Count == 0)
                throw new InvalidOperationException($"TransitionTable {name} is empty.");

            return _initialState != null
                ? _initialState.GetState(stateMachine, createdStates)
                : states[0];
        }

        private void Validate()
        {
            string context = $"TransitionTable '{name}'";
            if (_transitions == null || _transitions.Length == 0)
                throw new InvalidOperationException($"{context}: transition list is empty.");

            _initialState?.Validate(context);

            for (int i = 0; i < _transitions.Length; i++)
            {
                var transition = _transitions[i];
                if (transition.FromState == null)
                    throw new InvalidOperationException($"{context}, transition {i}: FromState is null.");
                if (transition.ToState == null)
                    throw new InvalidOperationException($"{context}, transition {i}: ToState is null.");
                if (transition.Conditions == null)
                    throw new InvalidOperationException($"{context}, transition {i}: conditions array is null.");

                transition.FromState.Validate(context);
                transition.ToState.Validate(context);

                for (int conditionIndex = 0; conditionIndex < transition.Conditions.Length; conditionIndex++)
                {
                    if (transition.Conditions[conditionIndex].Condition == null)
                    {
                        throw new InvalidOperationException(
                            $"{context}, transition {i} '{transition.FromState.name}' to "
                            + $"'{transition.ToState.name}': condition at index {conditionIndex} is null.");
                    }
                }
            }
        }

        private static void ProcessConditionUsages(
            StateMachine stateMachine,
            ConditionUsage[] conditionUsages,
            out StateCondition[] conditions,
            out int[] resultGroups)
        {
            int count = conditionUsages.Length;
            conditions = new StateCondition[count];
            for (int i = 0; i < count; i++)
                conditions[i] = conditionUsages[i].Condition.GetCondition(
                    stateMachine, conditionUsages[i].ExpectedResult == Result.True);


            List<int> resultGroupsList = new List<int>();
            for (int i = 0; i < count; i++)
            {
                int idx = resultGroupsList.Count;
                resultGroupsList.Add(1);
                while (i < count - 1 && conditionUsages[i].Operator == Operator.And)
                {
                    i++;
                    resultGroupsList[idx]++;
                }
            }

            resultGroups = resultGroupsList.ToArray();
        }

        [Serializable]
        public struct TransitionItem
        {
            public StateSO FromState;
            public StateSO ToState;
            public ConditionUsage[] Conditions;
        }

        [Serializable]
        public struct ConditionUsage
        {
            public Result ExpectedResult;
            public StateConditionSO Condition;
            public Operator Operator;
        }

        public enum Result { True, False }
        public enum Operator { And, Or }
    }
}

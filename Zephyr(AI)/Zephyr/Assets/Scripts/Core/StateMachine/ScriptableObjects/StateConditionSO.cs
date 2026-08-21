/*
 * StateConditionSO.cs
 * -------------------
 * Module:  Core / StateMachine / ScriptableObjects
 * Purpose: Abstract ScriptableObject base for state conditions. Defines the factory method
 *          CreateCondition() that subclasses implement to create their runtime Condition
 *          counterpart. The generic StateConditionSO<T> provides a default implementation
 *          using the new() constraint. GetCondition(StateMachine, expectedResult) creates
 *          an independent runtime condition for each transition usage and wraps it with
 *          the authored expected result.
 * Dependencies: Condition (runtime type), StateCondition (runtime struct).
 * Scene:    N/A (authored in Project window; instantiated at runtime by TransitionTableSO).
 * Ch.Ref:   Ch.6 State Machine Architecture, Ch.6.5 State Conditions.
 */
using UnityEngine;

namespace Zephyr.Core.StateMachine.ScriptableObjects
{
    public abstract class StateConditionSO : ScriptableObject
    {
        /// <summary>
        /// Creates and initializes a new custom <see cref="Condition"/>.
        /// </summary>
        internal StateCondition GetCondition(StateMachine stateMachine, bool expectedResult)
        {
            var condition = CreateCondition();
            condition._originSO = this;
            condition.Awake(stateMachine);
            return new StateCondition(stateMachine, condition, expectedResult);
        }
        protected abstract Condition CreateCondition();
    }


    public abstract class StateConditionSO<T> : StateConditionSO where T : Condition, new()
    {
        protected override Condition CreateCondition() => new T();
    }
}

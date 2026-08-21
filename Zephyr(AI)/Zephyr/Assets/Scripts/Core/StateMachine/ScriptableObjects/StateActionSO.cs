/*
 * StateActionSO.cs
 * ----------------
 * Module:  Core / StateMachine / ScriptableObjects
 * Purpose: Abstract ScriptableObject base for state actions. Defines the factory method
 *          CreateAction() that subclasses implement to create their runtime StateAction
 *          counterpart. The generic StateActionSO<T> provides a default implementation using
 *          the new() constraint. GetAction(StateMachine) creates an independent runtime
 *          action for each state usage so mutable action state is never shared.
 *          Extends DescriptionSMActionBaseSO for the description field.
 * Dependencies: DescriptionSMActionBaseSO, StateAction (runtime type).
 * Scene:    N/A (authored in Project window; instantiated at runtime by StateSO).
 * Ch.Ref:   Ch.6 State Machine Architecture, Ch.6.4 State Actions.
 */
using UnityEngine;

namespace Zephyr.Core.StateMachine.ScriptableObjects
{
	public abstract class StateActionSO : DescriptionSMActionBaseSO
	{
		/// <summary>
		/// Creates and initializes a new custom <see cref="StateAction"/>.
		/// </summary>
		internal StateAction GetAction(StateMachine stateMachine)
		{
			var action = CreateAction();
			action._originSO = this;
			action.Awake(stateMachine);
			return action;
		}
		protected abstract StateAction CreateAction();
	}

	public abstract class StateActionSO<T> : StateActionSO where T : StateAction, new()
	{
		protected override StateAction CreateAction() => new T();
	}
}

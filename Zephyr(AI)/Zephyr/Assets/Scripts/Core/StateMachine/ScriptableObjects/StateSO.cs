/*
 * StateSO.cs
 * ----------
 * Module:  Core / StateMachine / ScriptableObjects
 * Purpose: ScriptableObject that defines a state in the state machine. Authored in the
 *          Unity editor with an array of StateActionSO references and a StateTag enum.
 *          At runtime, GetState(StateMachine, createdStates) creates or retrieves the
 *          corresponding State runtime instance and resolves its StateAction objects.
 *          Created via CreateAssetMenu at "State Machines/State".
 * Dependencies: StateActionSO[], State (runtime type), TransitionTableSO (creator).
 * Scene:    N/A (authored in Project window; instantiated at runtime by TransitionTableSO).
 * Ch.Ref:   Ch.6 State Machine Architecture.
 */
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Zephyr.Core.StateMachine.ScriptableObjects
{
	[CreateAssetMenu(fileName = "New State", menuName = "State Machines/State")]
	public class StateSO : ScriptableObject
	{
		[SerializeField] private StateActionSO[] _actions = null;

		[SerializeField] public StateTag stateTag;

		/// <summary>
		/// Will create a new state or return an existing one inside <paramref name="createdStates"/>.
		/// </summary>
		internal State GetState(StateMachine stateMachine, Dictionary<StateSO, State> createdStates)
		{
			if (createdStates.TryGetValue(this, out var state))
				return state;

			state = new State();
			createdStates.Add(this, state);

			state._originSO = this;
			state._stateMachine = stateMachine;
			state._transitions = new StateTransition[0];
			state._actions = GetActions(_actions, stateMachine);
			state.stateTag = stateTag;

			return state;
		}

        internal void Validate(string context)
        {
            if (_actions == null)
                throw new InvalidOperationException($"{context}, state '{name}': actions array is null.");

            for (int i = 0; i < _actions.Length; i++)
            {
                if (_actions[i] == null)
                    throw new InvalidOperationException($"{context}, state '{name}': action at index {i} is null.");
            }
        }

		internal StateTag GetStateTag()
        {
			return stateTag;
        }

		private static StateAction[] GetActions(StateActionSO[] scriptableActions,
			StateMachine stateMachine)
		{
			int count = scriptableActions.Length;
			var actions = new StateAction[count];
			for (int i = 0; i < count; i++)
				actions[i] = scriptableActions[i].GetAction(stateMachine);

			return actions;
		}
	}
}

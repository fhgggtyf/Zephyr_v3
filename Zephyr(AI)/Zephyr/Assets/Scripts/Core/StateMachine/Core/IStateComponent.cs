/*
 * IStateComponent.cs
 * ------------------
 * Module:  Core / StateMachine / Core
 * Purpose: Core interface implemented by StateTransition, StateAction, and Condition.
 *          Defines the OnStateEnter() and OnStateExit() lifecycle callbacks that are
 *          invoked when the owning State is entered or exited. All runtime state machine
 *          components (transitions, actions, conditions) share this common lifecycle
 *          contract, enabling the State class to iterate over arrays of IStateComponent
 *          polymorphically.
 * Dependencies: None (pure interface).
 * Scene:    GameManager (implemented by all runtime state machine components).
 * Ch.Ref:   Ch.6 State Machine Architecture.
 */
namespace Zephyr.Core.StateMachine
{
    interface IStateComponent
    {
		/// <summary>
		/// Called when entering the state.
		/// </summary>
		void OnStateEnter();

		/// <summary>
		/// Called when leaving the state.
		/// </summary>
		void OnStateExit();
	}
}

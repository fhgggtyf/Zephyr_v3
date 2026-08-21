/*
 * StateAction.cs
 * --------------
 * Module:  Core / StateMachine / Core
 * Purpose: Abstract base for runtime state actions — behaviors executed during a state's
 *          lifecycle (OnStateEnter, OnUpdate, OnStateExit). Implements IStateComponent.
 *          Created from StateActionSO ScriptableObjects via the factory pattern. Provides
 *          the OriginSO property for subclasses to access shared SO data. The SpecificMoment
 *          enum allows actions to selectively execute in specific lifecycle phases.
 *          Subclasses must implement OnUpdate and can override Awake, OnStateEnter, OnStateExit.
 * Dependencies: StateActionSO (origin SO reference), StateMachine (for Awake initialization).
 * Scene:    GameManager (runtime; one instance per SO per StateMachine).
 * Ch.Ref:   Ch.6 State Machine Architecture, Ch.6.4 State Actions.
 */
using Zephyr.Core.StateMachine.ScriptableObjects;

namespace Zephyr.Core.StateMachine
{
	/// <summary>
	/// An object representing an action.
	/// </summary>
	public abstract class StateAction : IStateComponent
	{
		internal StateActionSO _originSO;

		/// <summary>
		/// Use this property to access shared data from the <see cref="StateActionSO"/> that corresponds to this <see cref="StateAction"/>
		/// </summary>
		protected StateActionSO OriginSO => _originSO;

		/// <summary>
		/// Called every frame the <see cref="StateMachine"/> is in a <see cref="State"/> with this <see cref="StateAction"/>.
		/// </summary>
		public abstract void OnUpdate();

		/// <summary>
		/// Awake is called when creating a new instance. Use this method to cache the components needed for the action.
		/// </summary>
		/// <param name="stateMachine">The <see cref="StateMachine"/> this instance belongs to.</param>
		public virtual void Awake(StateMachine stateMachine) { }

		public virtual void OnStateEnter() { }
		public virtual void OnStateExit() { }

		/// <summary>
		/// This enum is used to create flexible <c>StateActions</c> which can execute in any of the 3 available "moments".
		/// The StateAction in this case would have to implement all the relative functions, and use an if statement with this enum as a condition to decide whether to act or not in each moment.
		/// </summary>
		public enum SpecificMoment
		{
			OnStateEnter, OnStateExit, OnUpdate,
		}
	}
}


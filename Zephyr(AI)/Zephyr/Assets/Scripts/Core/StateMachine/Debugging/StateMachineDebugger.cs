/*
 * StateMachineDebugger.cs
 * -----------------------
 * Module:  Core / StateMachine / Debugging
 * Purpose: Editor-only debugging tool for tracking state machine transitions. Logs detailed
 *          information about each transition evaluation: which conditions were evaluated,
 *          their pass/fail results, and which actions are activated by the new state.
 *          Provides configurable toggles: debugTransitions (enable/disable logging),
 *          appendConditionsInfo (log each condition result), appendActionsInfo (log new
 *          state actions). Uses Unicode symbols (✓, ✗, ⟼, ➤) for readable log output.
 *          Active only in UNITY_EDITOR builds.
 * Dependencies: System, System.Text, UnityEngine (MonoBehaviour, Debug).
 * Scene:    N/A (editor-only; not included in runtime builds, guarded by #if UNITY_EDITOR).
 * Ch.Ref:   Ch.6 State Machine Architecture (debugging tool).
 */
#if UNITY_EDITOR

using System;
using System.Text;
using UnityEngine;

namespace Zephyr.Core.StateMachine.Debugging
{
	/// <summary>
	/// Class specialized in debugging the state transitions, should only be used while in editor mode.
	/// </summary>
	[Serializable]
	internal class StateMachineDebugger
	{
		[SerializeField]
		[Tooltip("Issues a debug log when a state transition is triggered")]
		internal bool debugTransitions = false;

		[SerializeField]
		[Tooltip("List all conditions evaluated, the result is read: ConditionName == BooleanResult [PassedTest]")]
		internal bool appendConditionsInfo = true;

		[SerializeField]
		[Tooltip("List all actions activated by the new State")]
		internal bool appendActionsInfo = true;

		[SerializeField]
		[Tooltip("The current State name [Readonly]")]
		internal string currentState;
		
		[SerializeField]
		[Tooltip("The previous State name [Readonly]")]
		internal string previousState;

		private StateMachine _stateMachine;
		private StringBuilder _logBuilder;
		private string _targetState = string.Empty;

		private const string CHECK_MARK = "\u2714";
		private const string UNCHECK_MARK = "\u2718";
		private const string THICK_ARROW = "\u279C";
		private const string SHARP_ARROW = "\u27A4";

		/// <summary>
		/// Must be called together with <c>StateMachine.Awake()</c>
		/// </summary>
		internal void Awake(StateMachine stateMachine)
		{
			_stateMachine = stateMachine;
			_logBuilder = new StringBuilder();

			if (stateMachine == null || stateMachine._currentState == null || stateMachine._previousState == null)
			{
				currentState = string.Empty;
				previousState = string.Empty;
				return;
			}

			currentState = stateMachine._currentState._originSO != null
				? stateMachine._currentState._originSO.name : string.Empty;
			previousState = stateMachine._previousState._originSO != null
				? stateMachine._previousState._originSO.name : string.Empty;
		}

		internal void TransitionEvaluationBegin(string targetState)
		{
			_targetState = targetState;

			if (!debugTransitions || _stateMachine == null || _logBuilder == null)
				return;

			_logBuilder.Clear();
			_logBuilder.AppendLine($"{_stateMachine.name} state changed");
			_logBuilder.AppendLine($"{currentState}  {SHARP_ARROW}  {_targetState}");

			if (appendConditionsInfo)
			{
				_logBuilder.AppendLine();
				_logBuilder.AppendLine($"Transition Conditions:");
			}
		}

		internal void TransitionConditionResult(string conditionName, bool result, bool isMet)
		{
			if (!debugTransitions || _logBuilder == null || _logBuilder.Length == 0 || !appendConditionsInfo)
				return;

			_logBuilder.Append($"    {THICK_ARROW} {conditionName} == {result}");

			if (isMet)
				_logBuilder.AppendLine($" [{CHECK_MARK}]");
			else
				_logBuilder.AppendLine($" [{UNCHECK_MARK}]");
		}

		internal void TransitionEvaluationEnd(bool passed, StateAction[] actions)
		{
			if (passed)
				currentState = _targetState;

			if (!debugTransitions || _logBuilder == null || _logBuilder.Length == 0)
				return;

			if (passed)
			{
				LogActions(actions);
				PrintDebugLog();
			}

			_logBuilder.Clear();
		}

		private void LogActions(StateAction[] actions)
		{
			if (!appendActionsInfo)
				return;

			_logBuilder.AppendLine();
			_logBuilder.AppendLine("State Actions:");

			foreach (StateAction action in actions)
			{
				_logBuilder.AppendLine($"    {THICK_ARROW} {action._originSO.name}");
			}
		}

		private void PrintDebugLog()
		{
			_logBuilder.AppendLine();
			_logBuilder.Append("--------------------------------");

			Debug.Log(_logBuilder.ToString());
		}
	}
}

#endif

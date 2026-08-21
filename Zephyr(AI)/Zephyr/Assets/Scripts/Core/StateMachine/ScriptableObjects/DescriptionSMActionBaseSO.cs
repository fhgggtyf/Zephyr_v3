/*
 * DescriptionSMActionBaseSO.cs
 * ----------------------------
 * Module:  Core / StateMachine / ScriptableObjects
 * Purpose: Base ScriptableObject class for state machine ScriptableObjects that require a
 *          public description field. Provides a [TextArea] description string that appears
 *          in the editor for documentation purposes. Inherited by StateActionSO.
 * Dependencies: UnityEngine.ScriptableObject.
 * Scene:    N/A (editor-only; provides metadata for the TransitionTableEditor display).
 * Ch.Ref:   Ch.6 State Machine Architecture.
 */
using UnityEngine;

namespace Zephyr.Core.StateMachine.ScriptableObjects
{
	/// <summary>
	/// Base class for StateMachine ScriptableObjects that need a public description field.
	/// </summary>
	public class DescriptionSMActionBaseSO : ScriptableObject
	{
		[TextArea] public string description;
	}

}
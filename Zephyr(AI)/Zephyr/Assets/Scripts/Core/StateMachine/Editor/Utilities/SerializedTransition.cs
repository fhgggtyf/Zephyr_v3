/*
 * SerializedTransition.cs
 * -----------------------
 * Module:  Core / StateMachine / Editor / Utilities
 * Purpose: Readonly struct that wraps SerializedProperty references for a TransitionItem
 *          in the TransitionTableSO. Provides convenient access to FromState, ToState,
 *          Conditions, and the transition's array index. Three constructors support different
 *          access patterns: from a direct SerializedProperty, from a SerializedObject + index,
 *          or from a parent SerializedProperty + index. Includes ClearProperties() for
 *          resetting all fields to null/default.
 * Dependencies: UnityEditor (SerializedProperty).
 * Scene:    N/A (editor-only; not included in runtime builds).
 * Ch.Ref:   Ch.6 State Machine Architecture (editor authoring tool).
 */
using UnityEditor;

namespace Zephyr.Core.StateMachine.Editor
{
	internal readonly struct SerializedTransition
	{
		internal readonly SerializedProperty Transition;
		internal readonly SerializedProperty FromState;
		internal readonly SerializedProperty ToState;
		internal readonly SerializedProperty Conditions;
		internal readonly int Index;

		internal SerializedTransition(SerializedProperty transition)
		{
			Transition = transition;
			FromState = Transition.FindPropertyRelative("FromState");
			ToState = Transition.FindPropertyRelative("ToState");
			Conditions = Transition.FindPropertyRelative("Conditions");
			Index = -1;
		}

		internal SerializedTransition(SerializedObject transitionTable, int index)
		{
			Transition = transitionTable.FindProperty("_transitions").GetArrayElementAtIndex(index);
			FromState = Transition.FindPropertyRelative("FromState");
			ToState = Transition.FindPropertyRelative("ToState");
			Conditions = Transition.FindPropertyRelative("Conditions");
			Index = index;
		}

		internal SerializedTransition(SerializedProperty transition, int index)
		{
			Transition = transition.GetArrayElementAtIndex(index);
			FromState = Transition.FindPropertyRelative("FromState");
			ToState = Transition.FindPropertyRelative("ToState");
			Conditions = Transition.FindPropertyRelative("Conditions");
			Index = index;
		}

		internal void ClearProperties()
		{
			FromState.objectReferenceValue = null;
			ToState.objectReferenceValue = null;
			Conditions.ClearArray();
		}
	}
}

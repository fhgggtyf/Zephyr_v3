/*
 * AddTransitionHelper.cs
 * ----------------------
 * Module:  Core / StateMachine / Editor / Utilities
 * Purpose: Editor helper that provides the "Add Transition" UI for the TransitionTableEditor.
 *          Supports batch transition creation: multiple FromStates × multiple ToStates
 *          with the same Conditions, creating a cartesian product of transitions.
 *          Uses ReorderableLists for FromStates, ToStates, and Conditions.
 *          Implements IDisposable for cleanup of the internal BatchTransitionItemSO asset.
 * Dependencies: System, UnityEditor, UnityEngine, TransitionTableEditor, TransitionTableSO,
 *               StateSO, ContentStyle.
 * Scene:    N/A (editor-only; not included in runtime builds).
 * Ch.Ref:   Ch.6 State Machine Architecture (editor authoring tool).
 */
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Zephyr.Core.StateMachine.ScriptableObjects;
using static UnityEditor.EditorGUI;

namespace Zephyr.Core.StateMachine.Editor
{
	internal class AddTransitionHelper : IDisposable
	{
		private readonly SerializedObject _transition;
		private readonly ReorderableList _fromStatesList;
		private readonly ReorderableList _toStatesList;
		private readonly ReorderableList _conditionsList;
		private readonly TransitionTableEditor _editor;
		private bool _toggle = false;

		internal AddTransitionHelper(TransitionTableEditor editor)
		{
			_editor = editor;
			_transition = new SerializedObject(ScriptableObject.CreateInstance<BatchTransitionItemSO>());
			_fromStatesList = new ReorderableList(_transition, _transition.FindProperty("FromStates"));
			_toStatesList = new ReorderableList(_transition, _transition.FindProperty("ToStates"));
			_conditionsList = new ReorderableList(_transition, _transition.FindProperty("Conditions"));
			SetupStateList(_fromStatesList, "From States");
			SetupStateList(_toStatesList, "To States");
			SetupConditionsList(_conditionsList);
		}

		internal void Display(Rect position)
		{
			position.x += 8;
			position.width -= 16;
			var rect = position;
			float fromStatesHeight = _fromStatesList.GetHeight();
			float toStatesHeight = _toStatesList.GetHeight();
			float conditionsHeight = _conditionsList.GetHeight();
			float singleLineHeight = EditorGUIUtility.singleLineHeight;

			// Display add button only if not already adding a transition
			if (!_toggle)
			{
				position.height = singleLineHeight;

				// Reserve space
				GUILayoutUtility.GetRect(position.width, position.height);

				if (GUI.Button(position, "Add Transition"))
				{
					_toggle = true;
					_transition.Update();
					ClearArrays();
				}

				return;
			}

			// Background
			{
				position.height = fromStatesHeight + toStatesHeight + conditionsHeight + singleLineHeight * 6;
				DrawRect(position, ContentStyle.LightGray);
			}

			// Reserve space
			GUILayoutUtility.GetRect(position.width, position.height);

			float y = rect.y + 10;

			// From States
			{
				var listRect = new Rect(rect.x + 5, y, rect.width - 10, fromStatesHeight);
				_fromStatesList.DoList(listRect);
				y += fromStatesHeight + 5;
			}

			// To States
			{
				var listRect = new Rect(rect.x + 5, y, rect.width - 10, toStatesHeight);
				_toStatesList.DoList(listRect);
				y += toStatesHeight + 5;
			}

			// Conditions
			{
				var listRect = new Rect(rect.x + 5, y, rect.width - 10, conditionsHeight);
				_conditionsList.DoList(listRect);
				y += conditionsHeight + 10;
			}

			// Add and cancel buttons
			{
				var buttonRect = new Rect(rect.x + 5, y, rect.width / 2 - 15, singleLineHeight);
				if (GUI.Button(buttonRect, "Add Transitions"))
				{
					var fromStates = GetStates(_transition.FindProperty("FromStates"));
					var toStates = GetStates(_transition.FindProperty("ToStates"));

					if (fromStates.Count == 0)
						Debug.LogException(new ArgumentException("At least one From State is required."));
					else if (toStates.Count == 0)
						Debug.LogException(new ArgumentException("At least one To State is required."));
					else
					{
						var conditions = GetConditions(_transition);
						_editor.AddTransitions(fromStates, toStates, conditions);
						_toggle = false;
						ClearArrays();
					}
				}
				var cancelRect = new Rect(rect.x + rect.width / 2 + 10, y, rect.width / 2 - 15, singleLineHeight);
				if (GUI.Button(cancelRect, "Cancel"))
				{
					_toggle = false;
					ClearArrays();
				}
			}
		}

		private void ClearArrays()
		{
			_transition.Update();
			_transition.FindProperty("FromStates").ClearArray();
			_transition.FindProperty("ToStates").ClearArray();
			_transition.FindProperty("Conditions").ClearArray();
			_transition.ApplyModifiedProperties();
		}

		private static List<StateSO> GetStates(SerializedProperty arrayProp)
		{
			var result = new List<StateSO>();
			for (int i = 0; i < arrayProp.arraySize; i++)
			{
				var state = arrayProp.GetArrayElementAtIndex(i).objectReferenceValue as StateSO;
				if (state != null) result.Add(state);
			}
			return result;
		}

		private static TransitionTableSO.ConditionUsage[] GetConditions(SerializedObject transition)
		{
			var conditionsProp = transition.FindProperty("Conditions");
			int count = conditionsProp.arraySize;
			var result = new TransitionTableSO.ConditionUsage[count];
			for (int i = 0; i < count; i++)
			{
				var prop = conditionsProp.GetArrayElementAtIndex(i);
				result[i] = new TransitionTableSO.ConditionUsage
				{
					ExpectedResult = (TransitionTableSO.Result)prop.FindPropertyRelative("ExpectedResult").enumValueIndex,
					Operator = (TransitionTableSO.Operator)prop.FindPropertyRelative("Operator").enumValueIndex,
					Condition = prop.FindPropertyRelative("Condition").objectReferenceValue as StateConditionSO
				};
			}
			return result;
		}

		public void Dispose()
		{
			UnityEngine.Object.DestroyImmediate(_transition.targetObject);
			_transition.Dispose();
			GC.SuppressFinalize(this);
		}

		private static void SetupStateList(ReorderableList reorderableList, string header)
		{
			reorderableList.elementHeight = EditorGUIUtility.singleLineHeight;
			reorderableList.headerHeight = 18;
			reorderableList.drawHeaderCallback += rect => GUI.Label(rect, header);
			reorderableList.onAddCallback += list =>
			{
				int count = list.count;
				list.serializedProperty.InsertArrayElementAtIndex(count);
				var prop = list.serializedProperty.GetArrayElementAtIndex(count);
				prop.objectReferenceValue = null;
			};
			reorderableList.drawElementCallback += (Rect rect, int index, bool isActive, bool isFocused) =>
			{
				var prop = reorderableList.serializedProperty.GetArrayElementAtIndex(index);
				EditorGUI.PropertyField(rect, prop, GUIContent.none);
			};
			reorderableList.drawElementBackgroundCallback += (Rect rect, int index, bool isActive, bool isFocused) =>
			{
				if (isFocused)
					EditorGUI.DrawRect(rect, ContentStyle.Focused);
				if (index % 2 != 0)
					EditorGUI.DrawRect(rect, ContentStyle.ZebraDark);
				else
					EditorGUI.DrawRect(rect, ContentStyle.ZebraLight);
			};
		}

		private static void SetupConditionsList(ReorderableList reorderableList)
		{
			reorderableList.elementHeight *= 2.3f;
			reorderableList.headerHeight = 18;
			reorderableList.drawHeaderCallback += rect => GUI.Label(rect, "Conditions");
			reorderableList.onAddCallback += list =>
			{
				int count = list.count;
				list.serializedProperty.InsertArrayElementAtIndex(count);
				var prop = list.serializedProperty.GetArrayElementAtIndex(count);
				prop.FindPropertyRelative("Condition").objectReferenceValue = null;
				prop.FindPropertyRelative("ExpectedResult").enumValueIndex = 0;
				prop.FindPropertyRelative("Operator").enumValueIndex = 0;
			};

			reorderableList.drawElementCallback += (Rect rect, int index, bool isActive, bool isFocused) =>
			{
				var prop = reorderableList.serializedProperty.GetArrayElementAtIndex(index);
				rect = new Rect(rect.x, rect.y + 2.5f, rect.width, EditorGUIUtility.singleLineHeight);
				var condition = prop.FindPropertyRelative("Condition");
				if (condition.objectReferenceValue != null)
				{
					string label = condition.objectReferenceValue.name;
					GUI.Label(rect, "If");
					GUI.Label(new Rect(rect.x + 20, rect.y, rect.width, rect.height), label, EditorStyles.boldLabel);
					EditorGUI.PropertyField(new Rect(rect.x + rect.width - 180, rect.y, 20, rect.height), condition, GUIContent.none);
				}
				else
				{
					EditorGUI.PropertyField(new Rect(rect.x, rect.y, 150, rect.height), condition, GUIContent.none);
				}
				EditorGUI.LabelField(new Rect(rect.x + rect.width - 120, rect.y, 20, rect.height), "Is");
				EditorGUI.PropertyField(new Rect(rect.x + rect.width - 60, rect.y, 60, rect.height), prop.FindPropertyRelative("ExpectedResult"), GUIContent.none);
				EditorGUI.PropertyField(new Rect(rect.x + 20, rect.y + EditorGUIUtility.singleLineHeight + 5, 60, rect.height), prop.FindPropertyRelative("Operator"), GUIContent.none);
			};

			reorderableList.onChangedCallback += list => list.serializedProperty.serializedObject.ApplyModifiedProperties();
			reorderableList.drawElementBackgroundCallback += (Rect rect, int index, bool isActive, bool isFocused) =>
			{
				if (isFocused)
					EditorGUI.DrawRect(rect, ContentStyle.Focused);

				if (index % 2 != 0)
					EditorGUI.DrawRect(rect, ContentStyle.ZebraDark);
				else
					EditorGUI.DrawRect(rect, ContentStyle.ZebraLight);
			};
		}

		// SO to serialize a batch of transitions being authored
		internal class BatchTransitionItemSO : ScriptableObject
		{
			public StateSO[] FromStates = default;
			public StateSO[] ToStates = default;
			public TransitionTableSO.ConditionUsage[] Conditions = default;
		}
	}
}

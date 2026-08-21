/*
 * StateEditor.cs
 * --------------
 * Module:  Core / StateMachine / Editor
 * Purpose: Custom UnityEditor for StateSO. Provides a ReorderableList-based interface for
 *          editing the StateAction array on a State, with drag-and-drop reordering, zebra
 *          striping, and description display. Also exposes the stateTag enum for editing.
 *          Supports Undo/Redo via the DoUndo callback. Used both standalone (inspector) and
 *          embedded within TransitionTableEditor for the "pencil icon" state actions view.
 * Dependencies: UnityEditor, UnityEditorInternal, StateSO, DescriptionSMActionBaseSO.
 * Scene:    N/A (editor-only; not included in runtime builds).
 * Ch.Ref:   Ch.6 State Machine Architecture (editor authoring tool).
 */
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Zephyr.Core.StateMachine.ScriptableObjects;

namespace Zephyr.Core.StateMachine.Editor
{
    [CustomEditor(typeof(StateSO))]
    public class StateEditor : UnityEditor.Editor
	{
		private ReorderableList _list;
		private SerializedProperty _actions;
		private SerializedProperty _types;

		private void OnEnable()
		{
			Undo.undoRedoPerformed += DoUndo;
			_actions = serializedObject.FindProperty("_actions");

			_list = new ReorderableList(serializedObject, _actions, true, true, true, true); 
			_types = serializedObject.FindProperty("stateTag");
			SetupActionsList(_list);
								    

		}

		private void OnDisable()
		{
			Undo.undoRedoPerformed -= DoUndo;
		}

		public override void OnInspectorGUI()
		{
			_list.DoLayoutList();
			EditorGUILayout.PropertyField(_types);
			serializedObject.ApplyModifiedProperties();

		}

		private void DoUndo()
		{
			serializedObject.UpdateIfRequiredOrScript();
		}

		private static void SetupActionsList(ReorderableList reorderableList)
		{
			reorderableList.elementHeight *= 1.5f;
			reorderableList.drawHeaderCallback += rect => GUI.Label(rect, "Actions");
			reorderableList.onAddCallback += list =>
			{
				int count = list.count;
				list.serializedProperty.InsertArrayElementAtIndex(count);
				var prop = list.serializedProperty.GetArrayElementAtIndex(count);
				prop.objectReferenceValue = null;
			};

			reorderableList.drawElementCallback += (Rect rect, int index, bool isActive, bool isFocused) =>
			{
				var r = rect;
				r.height = EditorGUIUtility.singleLineHeight;
				r.y += 5;
				r.x += 5;

				var prop = reorderableList.serializedProperty.GetArrayElementAtIndex(index);
				if (prop.objectReferenceValue != null)
				{
					//The icon of the asset SO (basically an object field, cut to show just the icon)
					r.width = 35;
					EditorGUI.PropertyField(r, prop, GUIContent.none);
					r.width = rect.width - 50;
					r.x += 42;

					//The name of the StateAction
					string label = prop.objectReferenceValue.name;
					GUI.Label(r, label, EditorStyles.boldLabel);

					//The description
					r.x += 180;
					r.width = rect.width - 50 - 180;
					string description = (prop.objectReferenceValue as DescriptionSMActionBaseSO).description;
					GUI.Label(r, description);
				}
				else
					EditorGUI.PropertyField(r, prop, GUIContent.none);
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
	}
}

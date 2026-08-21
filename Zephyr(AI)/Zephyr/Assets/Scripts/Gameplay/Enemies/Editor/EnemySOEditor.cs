using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Zephyr.Gameplay.Enemies.Editor
{
    [CustomEditor(typeof(EnemySO))]
    public sealed class EnemySOEditor : UnityEditor.Editor
    {
        private SerializedProperty _attacksProperty;
        private SerializedProperty _componentDataProperty;
        private SerializedProperty _legacyAttackRange;
        private SerializedProperty _legacyAttackCooldown;
        private SerializedProperty _legacyAttackWindup;
        private SerializedProperty _legacyAttackDuration;
        private SerializedProperty _legacyAttackActiveDuration;
        private List<Type> _componentDataTypes;

        private void OnEnable()
        {
            _attacksProperty = serializedObject.FindProperty("_attacks");
            _componentDataProperty = serializedObject.FindProperty("_componentData");
            _legacyAttackRange = serializedObject.FindProperty("_attackRange");
            _legacyAttackCooldown = serializedObject.FindProperty("_attackCooldown");
            _legacyAttackWindup = serializedObject.FindProperty("_attackWindup");
            _legacyAttackDuration = serializedObject.FindProperty("_attackDuration");
            _legacyAttackActiveDuration = serializedObject.FindProperty("_attackActiveDuration");
            _componentDataTypes = TypeCache.GetTypesDerivedFrom<EnemyComponentData>()
                .Where(type => !type.IsAbstract && type.GetConstructor(Type.EmptyTypes) != null)
                .OrderBy(type => type.Name)
                .ToList();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, "m_Script", "_attacks", "_componentData",
                "_attackRange", "_attackCooldown", "_attackWindup", "_attackDuration",
                "_attackActiveDuration");

            EditorGUILayout.Space();
            if (_attacksProperty.arraySize == 0)
                DrawLegacyAttackFallback();
            EditorGUILayout.PropertyField(_attacksProperty, true);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Enemy Component Data", EditorStyles.boldLabel);
            DrawComponentData();

            if (serializedObject.ApplyModifiedProperties())
            {
                SynchronizeAttackData();
                EditorUtility.SetDirty(target);
            }
        }

        private void DrawLegacyAttackFallback()
        {
            EditorGUILayout.HelpBox(
                "This asset has no built-in attack definition. These legacy values are used only as a compatibility fallback.",
                MessageType.Info);
            EditorGUILayout.PropertyField(_legacyAttackRange);
            EditorGUILayout.PropertyField(_legacyAttackCooldown);
            EditorGUILayout.PropertyField(_legacyAttackWindup);
            EditorGUILayout.PropertyField(_legacyAttackDuration);
            EditorGUILayout.PropertyField(_legacyAttackActiveDuration);
            EditorGUILayout.Space();
        }

        private void DrawComponentData()
        {
            for (int i = 0; i < _componentDataProperty.arraySize; i++)
            {
                SerializedProperty element = _componentDataProperty.GetArrayElementAtIndex(i);
                string typeName = element.managedReferenceValue?.GetType().Name ?? "Missing Component Data";

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(typeName, EditorStyles.boldLabel);
                if (GUILayout.Button("Remove", GUILayout.Width(70f)))
                {
                    RemoveComponentData(i);
                    GUIUtility.ExitGUI();
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.PropertyField(element, GUIContent.none, true);
                EditorGUILayout.EndVertical();
            }

            if (!GUILayout.Button("Add Enemy Component Data")) return;
            var menu = new GenericMenu();
            foreach (Type type in _componentDataTypes)
            {
                Type selectedType = type;
                if (ContainsComponentData(type))
                    menu.AddDisabledItem(new GUIContent(type.Name));
                else
                    menu.AddItem(new GUIContent(type.Name), false, () => AddComponentData(selectedType));
            }
            menu.ShowAsContext();
        }

        private bool ContainsComponentData(Type type)
        {
            for (int i = 0; i < _componentDataProperty.arraySize; i++)
            {
                object value = _componentDataProperty.GetArrayElementAtIndex(i).managedReferenceValue;
                if (value != null && value.GetType() == type) return true;
            }
            return false;
        }

        private void AddComponentData(Type type)
        {
            Undo.RecordObject(target, "Add enemy component data");
            serializedObject.Update();
            _componentDataProperty.arraySize++;
            SerializedProperty element = _componentDataProperty.GetArrayElementAtIndex(
                _componentDataProperty.arraySize - 1);
            element.managedReferenceValue = Activator.CreateInstance(type);
            serializedObject.ApplyModifiedProperties();
            SynchronizeAttackData();
            EditorUtility.SetDirty(target);
        }

        private void RemoveComponentData(int index)
        {
            Undo.RecordObject(target, "Remove enemy component data");
            serializedObject.Update();
            int previousSize = _componentDataProperty.arraySize;
            _componentDataProperty.DeleteArrayElementAtIndex(index);
            if (_componentDataProperty.arraySize == previousSize)
                _componentDataProperty.DeleteArrayElementAtIndex(index);
            serializedObject.ApplyModifiedProperties();
            SynchronizeAttackData();
            EditorUtility.SetDirty(target);
        }

        private void SynchronizeAttackData()
        {
            var enemy = (EnemySO)target;
            foreach (EnemyComponentData data in enemy.Components)
                data?.SynchronizeAttackData(enemy.Attacks.Count);
            EditorUtility.SetDirty(enemy);
        }
    }
}

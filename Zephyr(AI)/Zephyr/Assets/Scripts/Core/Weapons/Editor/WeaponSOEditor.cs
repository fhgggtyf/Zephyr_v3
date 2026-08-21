using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Zephyr.Core.Weapons.Editor
{
    [CustomEditor(typeof(WeaponSO))]
    public class WeaponSOEditor : UnityEditor.Editor
    {
        private List<Type> m_componentDataTypes;
        private SerializedProperty m_componentDataProperty;

        private void OnEnable()
        {
            m_componentDataProperty = serializedObject.FindProperty("m_componentData");
            m_componentDataTypes = TypeCache.GetTypesDerivedFrom<ComponentData>()
                .Where(type => !type.IsAbstract && type.GetConstructor(Type.EmptyTypes) != null)
                .OrderBy(type => type.Name)
                .ToList();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, "m_Script", "m_componentData");

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Component Data", EditorStyles.boldLabel);
            DrawComponentData();

            if (serializedObject.ApplyModifiedProperties())
            {
                SynchronizeAttackData();
                EditorUtility.SetDirty(target);
            }
        }

        private void DrawComponentData()
        {
            for (int i = 0; i < m_componentDataProperty.arraySize; i++)
            {
                SerializedProperty element = m_componentDataProperty.GetArrayElementAtIndex(i);
                string name = element.managedReferenceValue?.GetType().Name ?? "Missing Component Data";

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(name, EditorStyles.boldLabel);
                if (GUILayout.Button("Remove", GUILayout.Width(70f)))
                {
                    RemoveComponentData(i);
                    GUIUtility.ExitGUI();
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.PropertyField(element, GUIContent.none, true);
                EditorGUILayout.EndVertical();
            }

            DrawAddComponentMenu();
        }

        private void RemoveComponentData(int index)
        {
            Undo.RecordObject(target, "Remove weapon component data");
            serializedObject.Update();

            int previousSize = m_componentDataProperty.arraySize;
            m_componentDataProperty.DeleteArrayElementAtIndex(index);

            // Some Unity versions clear a managed-reference element on the first
            // delete and only remove the array slot on the second delete.
            if (m_componentDataProperty.arraySize == previousSize)
            {
                m_componentDataProperty.DeleteArrayElementAtIndex(index);
            }

            serializedObject.ApplyModifiedProperties();
            SynchronizeAttackData();
            EditorUtility.SetDirty(target);
        }

        private void DrawAddComponentMenu()
        {
            if (!GUILayout.Button("Add Component Data")) return;

            var menu = new GenericMenu();
            foreach (Type type in m_componentDataTypes)
            {
                if (ContainsComponentData(type))
                {
                    menu.AddDisabledItem(new GUIContent(type.Name));
                    continue;
                }

                Type selectedType = type;
                menu.AddItem(new GUIContent(type.Name), false, () => AddComponentData(selectedType));
            }

            menu.ShowAsContext();
        }

        private bool ContainsComponentData(Type type)
        {
            for (int i = 0; i < m_componentDataProperty.arraySize; i++)
            {
                object value = m_componentDataProperty.GetArrayElementAtIndex(i).managedReferenceValue;
                if (value != null && value.GetType() == type) return true;
            }

            return false;
        }

        private void AddComponentData(object userData)
        {
            Type type = (Type)userData;
            Undo.RecordObject(target, "Add weapon component data");
            serializedObject.Update();
            m_componentDataProperty.arraySize++;
            SerializedProperty element = m_componentDataProperty.GetArrayElementAtIndex(
                m_componentDataProperty.arraySize - 1);
            element.managedReferenceValue = Activator.CreateInstance(type);
            serializedObject.ApplyModifiedProperties();
            SynchronizeAttackData();
            EditorUtility.SetDirty(target);
        }

        private void SynchronizeAttackData()
        {
            var weapon = (WeaponSO)target;
            int attackCount = weapon.Combo?.StepCount ?? 0;
            foreach (ComponentData data in weapon.Components)
            {
                data?.SynchronizeAttackData(attackCount);
            }

            EditorUtility.SetDirty(weapon);
        }
    }
}

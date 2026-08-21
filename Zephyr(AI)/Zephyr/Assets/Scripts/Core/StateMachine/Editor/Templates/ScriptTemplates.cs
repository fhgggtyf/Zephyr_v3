/*
 * ScriptTemplates.cs
 * ------------------
 * Module:  Core / StateMachine / Editor / Templates
 * Purpose: Editor utility for creating new state machine action/condition scripts from
 *          text templates. Provides menu items "Assets/Create/State Machines/Action Script"
 *          and "Assets/Create/State Machines/Condition Script" that launch the name editing
 *          workflow in the Project window. The DoCreateStateMachineScriptAsset inner class
 *          handles post-creation processing: renaming (ensuring SO suffix), replacing
 *          template placeholders (#SCRIPTNAME#, #RUNTIMENAME#, #RUNTIMENAME_WITH_SPACES#),
 *          and importing the new asset. Uses Unity 6's AssetCreationEndAction API.
 * Dependencies: System.IO, System.Text, UnityEditor, UnityEngine.
 * Scene:    N/A (editor-only; not included in runtime builds).
 * Ch.Ref:   Ch.6 State Machine Architecture (developer tooling).
 */
using System.IO;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.ProjectWindowCallback;

namespace Zephyr.Core.StateMachine.Editor
{
    internal class ScriptTemplates
    {
        private static readonly string _path = "Assets/Scripts/Core/StateMachine/Editor/Templates";

        [MenuItem("Assets/Create/State Machines/Action Script", false, 0)]
        public static void CreateActionScript() =>
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(EntityId.None,
                ScriptableObject.CreateInstance<DoCreateStateMachineScriptAsset>(),
                "NewActionSO.cs",
                (Texture2D)EditorGUIUtility.IconContent("cs Script Icon").image,
                $"{_path}/StateAction.txt");

        [MenuItem("Assets/Create/State Machines/Condition Script", false, 0)]
        public static void CreateConditionScript() =>
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(EntityId.None,
                ScriptableObject.CreateInstance<DoCreateStateMachineScriptAsset>(),
                "NewConditionSO.cs",
                (Texture2D)EditorGUIUtility.IconContent("cs Script Icon").image,
                $"{_path}/StateCondition.txt");

        private class DoCreateStateMachineScriptAsset : AssetCreationEndAction
        {
            public override void Action(EntityId entityId, string pathName, string resourceFile)
            {
                string text = File.ReadAllText(resourceFile);

                string fileName = Path.GetFileName(pathName);
                {
                    string newName = fileName.Replace(" ", "");
                    if (!newName.Contains("SO"))
                        newName = newName.Insert(fileName.Length - 3, "SO");

                    pathName = pathName.Replace(fileName, newName);
                    fileName = newName;
                }

                string fileNameWithoutExtension = fileName.Substring(0, fileName.Length - 3);
                text = text.Replace("#SCRIPTNAME#", fileNameWithoutExtension);

                string runtimeName = fileNameWithoutExtension.Replace("SO", "");
                text = text.Replace("#RUNTIMENAME#", runtimeName);

                for (int i = runtimeName.Length - 1; i > 0; i--)
                    if (char.IsUpper(runtimeName[i]) && char.IsLower(runtimeName[i - 1]))
                        runtimeName = runtimeName.Insert(i, " ");

                text = text.Replace("#RUNTIMENAME_WITH_SPACES#", runtimeName);

                string fullPath = Path.GetFullPath(pathName);
                var encoding = new UTF8Encoding(true);
                File.WriteAllText(fullPath, text, encoding);
                AssetDatabase.ImportAsset(pathName);
                ProjectWindowUtil.ShowCreatedAsset(AssetDatabase.LoadAssetAtPath(pathName, typeof(Object)));
            }
        }
    }
}

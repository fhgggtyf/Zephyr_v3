using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Zephyr.Gameplay.Player.Editor
{
    /// <summary>Keeps the code-driven player Animator populated with all authored clips.</summary>
    [InitializeOnLoad]
    internal static class PlayerAnimatorControllerSync
    {
        private const string ControllerPath = "Assets/Animations/Player/AlothaiPlayerBaseAnimations.controller";
        private const string AnimationFolder = "Assets/Animations/Player";

        static PlayerAnimatorControllerSync()
        {
            EditorApplication.delayCall += Sync;
        }

        [MenuItem("Zephyr/Player/Sync Animator States")]
        private static void Sync()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null) return;

            if (controller.layers.Length == 0)
                controller.AddLayer("Base Layer");

            var stateMachine = controller.layers[0].stateMachine;
            var existingNames = stateMachine.states.Select(item => item.state.name).ToHashSet();
            var clipGuids = AssetDatabase.FindAssets("t:AnimationClip", new[] { AnimationFolder });
            bool changed = false;

            foreach (string guid in clipGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                if (clip == null || existingNames.Contains(clip.name)) continue;

                var state = stateMachine.AddState(clip.name);
                state.motion = clip;
                existingNames.Add(clip.name);
                changed = true;
            }

            var idle = stateMachine.states.FirstOrDefault(item => item.state.name == "Idle").state;
            if (idle != null && stateMachine.defaultState != idle)
            {
                stateMachine.defaultState = idle;
                changed = true;
            }

            if (!changed) return;
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            Debug.Log("[PlayerAnimatorControllerSync] Player animation states synchronized.");
        }
    }
}

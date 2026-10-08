#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zephyr.Core.Weapons;

namespace Zephyr.EditorTools
{
    internal static class ConfigureSampleSceneExcalibur
    {
        private const string ScenePath = "Assets/Scenes/Test/SampleScene.unity";
        private const string WeaponPath = "Assets/ScriptableObjects/WeaponConfigs/Excalibur.asset";

        [MenuItem("Zephyr/MCP/Set SampleScene Primary Weapon To Excalibur")]
        private static void Configure()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            GameObject player = GameObject.FindWithTag("Player");
            if (player == null) player = GameObject.Find("Player");
            if (player == null) throw new InvalidOperationException("SampleScene Player was not found.");

            WeaponSO excalibur = AssetDatabase.LoadAssetAtPath<WeaponSO>(WeaponPath);
            if (excalibur == null) throw new InvalidOperationException("Excalibur WeaponSO was not found.");

            // The scene instance keeps WeaponController on Player/WeaponSocket.
            // Search the instance hierarchy so this tool never requires changing Player.prefab.
            WeaponController controller = player.GetComponentInChildren<WeaponController>(true);
            if (controller == null) throw new InvalidOperationException("SampleScene Player has no WeaponController.");

            SerializedObject serialized = new SerializedObject(controller);
            SerializedProperty weaponSet = serialized.FindProperty("m_weaponSet");
            SerializedProperty primary = weaponSet?.FindPropertyRelative("m_primary");
            if (primary == null) throw new InvalidOperationException("WeaponController.m_weaponSet.m_primary was not found.");

            primary.objectReferenceValue = excalibur;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(controller);
            EditorUtility.SetDirty(player);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[Excalibur] SampleScene Player Primary Weapon set to Excalibur. Player prefab was not modified.");
        }
    }
}
#endif

#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using Zephyr.Core.Weapons;

namespace Zephyr.EditorTools
{
    internal static class ImportExcaliburPlayerAnimations
    {
        private const string HorizontalImage = "Assets/ArtResources/Weapons/Excalibur/PlayerAnimations/Excalibur_Player_Horizontal_Slash.png";
        private const string VerticalImage = "Assets/ArtResources/Weapons/Excalibur/PlayerAnimations/Excalibur_Player_Vertical_Slash.png";
        private const string AnimationFolder = "Assets/Animations/Player/Weapons/Excalibur";
        private const string HorizontalClipPath = AnimationFolder + "/Excalibur_Player_Horizontal_Slash.anim";
        private const string VerticalClipPath = AnimationFolder + "/Excalibur_Player_Vertical_Slash.anim";
        private const string PlaceholderPath = "Assets/Animations/Player/Idle.anim";
        private const string WeaponPath = "Assets/ScriptableObjects/WeaponConfigs/Excalibur.asset";

        [MenuItem("Zephyr/MCP/Import Excalibur Player Animations")]
        private static void Import()
        {
            EnsureFolder("Assets/Animations/Player");
            EnsureFolder(AnimationFolder);
            ConfigureSpriteImporter(HorizontalImage);
            ConfigureSpriteImporter(VerticalImage);
            AssetDatabase.Refresh();

            Sprite horizontalSprite = AssetDatabase.LoadAssetAtPath<Sprite>(HorizontalImage);
            Sprite verticalSprite = AssetDatabase.LoadAssetAtPath<Sprite>(VerticalImage);
            AnimationClip placeholder = AssetDatabase.LoadAssetAtPath<AnimationClip>(PlaceholderPath);
            WeaponSO weapon = AssetDatabase.LoadAssetAtPath<WeaponSO>(WeaponPath);

            if (horizontalSprite == null || verticalSprite == null)
                throw new InvalidOperationException("Generated Excalibur images were not imported as Sprite assets.");
            if (placeholder == null)
                throw new InvalidOperationException("Player Idle placeholder clip was not found.");
            if (weapon == null)
                throw new InvalidOperationException("Excalibur WeaponSO was not found.");

            AnimationClip horizontalClip = CreateSpriteClip(HorizontalClipPath, "Excalibur_Player_Horizontal_Slash", horizontalSprite, 0.5f);
            AnimationClip verticalClip = CreateSpriteClip(VerticalClipPath, "Excalibur_Player_Vertical_Slash", verticalSprite, 0.7f);

            SerializedObject serializedWeapon = new SerializedObject(weapon);
            SerializedProperty combo = serializedWeapon.FindProperty("m_combo");
            SerializedProperty steps = combo?.FindPropertyRelative("m_steps");
            if (steps == null || steps.arraySize < 2)
                throw new InvalidOperationException("Excalibur must contain two combo steps before binding player animations.");

            BindStep(steps.GetArrayElementAtIndex(0), "Idle", placeholder, horizontalClip);
            BindStep(steps.GetArrayElementAtIndex(1), "Idle", placeholder, verticalClip);
            serializedWeapon.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(weapon);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Excalibur] Player animation clips imported and bound to both combo steps.");
        }

        private static void BindStep(SerializedProperty step, string animationName, AnimationClip placeholder, AnimationClip replacement)
        {
            step.FindPropertyRelative("m_playerAnimationName").stringValue = animationName;
            step.FindPropertyRelative("m_playerAnimationPlaceholder").objectReferenceValue = placeholder;
            step.FindPropertyRelative("m_playerAnimationClip").objectReferenceValue = replacement;
        }

        private static void ConfigureSpriteImporter(string path)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new InvalidOperationException("Texture importer not found: " + path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 16f;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        private static AnimationClip CreateSpriteClip(string path, string name, Sprite sprite, float duration)
        {
            AssetDatabase.DeleteAsset(path);
            AnimationClip clip = new AnimationClip { name = name, frameRate = 12f };
            EditorCurveBinding binding = EditorCurveBinding.PPtrCurve(string.Empty, typeof(SpriteRenderer), "m_Sprite");
            ObjectReferenceKeyframe[] keys =
            {
                new ObjectReferenceKeyframe { time = 0f, value = sprite },
                new ObjectReferenceKeyframe { time = Mathf.Max(0.01f, duration - 0.01f), value = sprite },
                new ObjectReferenceKeyframe { time = duration, value = sprite }
            };
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = false;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            AssetDatabase.CreateAsset(clip, path);
            return clip;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            string name = System.IO.Path.GetFileName(path);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
#endif

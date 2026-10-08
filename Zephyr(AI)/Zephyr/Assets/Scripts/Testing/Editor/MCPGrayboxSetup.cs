#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using Zephyr.Gameplay.Player.Visual;
using Zephyr.Testing;

namespace Zephyr.EditorTools
{
    internal static class MCPGrayboxSetup
    {
        private const string Root = "Assets/MCPTest/Character";
        private const string SpriteDir = Root + "/Sprites";
        private const string AnimDir = Root + "/Animations";
        private const string PrefabPath = Root + "/Player_Graybox.prefab";
        private const string ScenePath = Root + "/GrayboxAttackScene.unity";
        private const string IdleClipPath = "Assets/Animations/Player/Idle.anim";
        private const string FloorTilePath = AnimDir + "/GrayboxFloorTile.asset";

        [MenuItem("Zephyr/MCP/Build Graybox Excalibur Override Test")]
        private static void Build()
        {
            EnsureFolder("Assets/MCPTest");
            EnsureFolder(Root);
            EnsureFolder(SpriteDir);
            EnsureFolder(AnimDir);

            string bluePath = SpriteDir + "/Player_Base_Blue_32x32.png";
            string redPath = SpriteDir + "/Player_Attack_Horizontal_Red_32x32.png";
            string greenPath = SpriteDir + "/Player_Attack_Vertical_Green_32x32.png";
            ConfigureImporter(bluePath);
            ConfigureImporter(redPath);
            ConfigureImporter(greenPath);
            AssetDatabase.Refresh();

            Sprite blue = AssetDatabase.LoadAssetAtPath<Sprite>(bluePath);
            Sprite red = AssetDatabase.LoadAssetAtPath<Sprite>(redPath);
            Sprite green = AssetDatabase.LoadAssetAtPath<Sprite>(greenPath);
            if (blue == null || red == null || green == null)
                throw new InvalidOperationException("MCP graybox sprites were not imported as Sprite assets.");

            AnimationClip horizontal = CreateSpriteClip(AnimDir + "/GrayboxAttackHorizontal.anim", "GrayboxAttackHorizontal", red);
            AnimationClip vertical = CreateSpriteClip(AnimDir + "/GrayboxAttackVertical.anim", "GrayboxAttackVertical", green);
            AnimationClip placeholder = AssetDatabase.LoadAssetAtPath<AnimationClip>(IdleClipPath);
            if (placeholder == null)
                throw new InvalidOperationException("Player Idle animation clip was not found.");

            ConfigurePrefab(blue, placeholder, horizontal, vertical);
            TileBase floorTile = CreateFloorTile(FloorTilePath, SpriteForPath(SpriteDir + "/Graybox_Floor_Dark_32x16.png"));
            BuildScene(floorTile);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[MCPGrayboxSetup] Graybox override test built at " + ScenePath);
        }

        private static Sprite SpriteForPath(string path)
        {
            ConfigureImporter(path);
            AssetDatabase.Refresh();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static TileBase CreateFloorTile(string path, Sprite sprite)
        {
            if (sprite == null) throw new InvalidOperationException("Graybox floor sprite was not imported as a Sprite.");
            AssetDatabase.DeleteAsset(path);
            Tile tile = ScriptableObject.CreateInstance<Tile>();
            tile.name = "GrayboxFloorTile";
            tile.sprite = sprite;
            tile.colliderType = Tile.ColliderType.Grid;
            AssetDatabase.CreateAsset(tile, path);
            return tile;
        }

        private static void ConfigureImporter(string path)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 16f;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        private static AnimationClip CreateSpriteClip(string path, string name, Sprite sprite)
        {
            AssetDatabase.DeleteAsset(path);
            AnimationClip clip = new AnimationClip { name = name, frameRate = 12f };
            EditorCurveBinding binding = EditorCurveBinding.PPtrCurve(string.Empty, typeof(SpriteRenderer), "m_Sprite");
            ObjectReferenceKeyframe[] keys =
            {
                new ObjectReferenceKeyframe { time = 0f, value = sprite },
                new ObjectReferenceKeyframe { time = 0.45f, value = sprite }
            };
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = false;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            AssetDatabase.CreateAsset(clip, path);
            return clip;
        }

        private static void ConfigurePrefab(Sprite baseSprite, AnimationClip placeholder, AnimationClip horizontal, AnimationClip vertical)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            if (root == null) throw new InvalidOperationException("Could not open graybox player prefab.");

            SpriteRenderer renderer = root.GetComponentInChildren<SpriteRenderer>(true);
            if (renderer == null) throw new InvalidOperationException("Graybox player has no SpriteRenderer.");
            renderer.sprite = baseSprite;
            renderer.color = Color.white;

            GrayboxAttackOverrideProbe probe = root.GetComponent<GrayboxAttackOverrideProbe>();
            if (probe == null) probe = root.AddComponent<GrayboxAttackOverrideProbe>();
            SerializedObject serialized = new SerializedObject(probe);
            serialized.FindProperty("m_spriteAnimator").objectReferenceValue = root.GetComponentInChildren<SpriteAnimator>(true);
            serialized.FindProperty("m_horizontalAnimation").stringValue = "Idle";
            serialized.FindProperty("m_verticalAnimation").stringValue = "Idle";
            serialized.FindProperty("m_horizontalPlaceholder").objectReferenceValue = placeholder;
            serialized.FindProperty("m_horizontalReplacement").objectReferenceValue = horizontal;
            serialized.FindProperty("m_verticalPlaceholder").objectReferenceValue = placeholder;
            serialized.FindProperty("m_verticalReplacement").objectReferenceValue = vertical;
            serialized.FindProperty("m_autoCycle").boolValue = true;
            serialized.FindProperty("m_autoCycleInterval").floatValue = 0.9f;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            PrefabUtility.UnloadPrefabContents(root);
        }

        private static void BuildScene(TileBase floorTile)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 4f;
            camera.backgroundColor = new Color(0.05f, 0.05f, 0.08f, 1f);
            camera.transform.position = new Vector3(0f, 0f, -10f);

            GameObject gridObject = new GameObject("Grid");
            Grid grid = gridObject.AddComponent<Grid>();
            grid.cellSize = Vector3.one;

            GameObject tilemapObject = new GameObject("GrayboxFloor");
            tilemapObject.transform.SetParent(gridObject.transform, false);
            Tilemap tilemap = tilemapObject.AddComponent<Tilemap>();
            TilemapRenderer renderer = tilemapObject.AddComponent<TilemapRenderer>();
            renderer.sortingOrder = -1;
            TilemapCollider2D collider = tilemapObject.AddComponent<TilemapCollider2D>();
            CompositeCollider2D composite = tilemapObject.AddComponent<CompositeCollider2D>();
            Rigidbody2D body = tilemapObject.GetComponent<Rigidbody2D>();
            if (body == null) body = tilemapObject.AddComponent<Rigidbody2D>();
            if (body != null) body.bodyType = RigidbodyType2D.Static;
            collider.compositeOperation = Collider2D.CompositeOperation.Merge;
            composite.geometryType = CompositeCollider2D.GeometryType.Outlines;

            for (int x = -6; x <= 6; x++)
                tilemap.SetTile(new Vector3Int(x, -2, 0), floorTile);

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            GameObject player = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (player == null) throw new InvalidOperationException("Could not instantiate graybox player prefab.");
            player.name = "GrayboxPlayer";
            player.transform.position = new Vector3(0f, -0.4f, 0f);

            EditorSceneManager.SaveScene(scene, ScenePath);
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

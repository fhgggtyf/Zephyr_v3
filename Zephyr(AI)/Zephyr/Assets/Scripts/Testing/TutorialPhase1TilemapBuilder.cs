#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Zephyr.Testing
{
    public static class TutorialPhase1TilemapBuilder
    {
        private const string ScenePath = "Assets/Scenes/Tutorial/Tutorial_Phase_1.unity";
        private const string TilemapObjectName = "Tilemap_GroundAndPlatforms";
        private const string ExistingTilePath = "Assets/ArtResources/Tiles/PalletAssets/StoneGround/Mossy_stone_floor_0.asset";

        [MenuItem("Zephyr/Testing/Build Tutorial Phase 1 Tilemap")]
        public static void Build()
        {
            if (EditorSceneManager.GetActiveScene().path != ScenePath)
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            GameObject tilemapObject = GameObject.Find(TilemapObjectName);
            if (tilemapObject == null)
            {
                Debug.LogError($"Tutorial tilemap build failed: '{TilemapObjectName}' was not found.");
                return;
            }

            Tilemap tilemap = tilemapObject.GetComponent<Tilemap>();
            if (tilemap == null)
            {
                Debug.LogError("Tutorial tilemap build failed: Tilemap component is missing.");
                return;
            }

            Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(ExistingTilePath);
            if (tile == null)
            {
                Debug.LogError($"Tutorial tilemap build failed: existing project tile not found at '{ExistingTilePath}'.");
                return;
            }

            tilemap.ClearAllTiles();
            // Ground: x=0..100 at y=-3.
            FillRow(tilemap, tile, 0, 100, -3);
            // Four-tile floating platforms.
            FillRow(tilemap, tile, 8, 11, 0);
            FillRow(tilemap, tile, 15, 18, 2);
            // Closed left/right walls, open top.
            FillColumn(tilemap, tile, 0, -2, 4);
            FillColumn(tilemap, tile, 100, -2, 4);

            tilemap.RefreshAllTiles();
            tilemap.CompressBounds();
            Physics2D.SyncTransforms();

            DisableLegacyTerrain("Terrain_Ground");
            DisableLegacyTerrain("Terrain_Platform_A");
            DisableLegacyTerrain("Terrain_Platform_B");
            DisableLegacyTerrain("Terrain_LeftWall");
            DisableLegacyTerrain("Terrain_RightWall");

            EditorUtility.SetDirty(tilemap);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            AssetDatabase.Refresh();
            Debug.Log($"Tutorial Phase 1 Tilemap built from existing tile '{ExistingTilePath}': ground, platforms, side walls, and grid collision populated.");
        }

        private static void DisableLegacyTerrain(string objectName)
        {
            GameObject legacy = GameObject.Find(objectName);
            if (legacy != null)
                legacy.SetActive(false);
        }

        private static void FillRow(Tilemap tilemap, Tile tile, int startX, int endX, int y)
        {
            for (int x = startX; x <= endX; x++)
                tilemap.SetTile(new Vector3Int(x, y, 0), tile);
        }

        private static void FillColumn(Tilemap tilemap, Tile tile, int x, int startY, int endY)
        {
            for (int y = startY; y <= endY; y++)
                tilemap.SetTile(new Vector3Int(x, y, 0), tile);
        }
    }
}
#endif

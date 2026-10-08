#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Zephyr.EditorTools
{
    internal static class MCPGrayboxCaptureExporter
    {
        private const string OutputDirectory = "Assets/MCPTest/Screenshots";

        [MenuItem("Zephyr/MCP/Export Graybox Scene Capture")]
        private static void Export()
        {
            if (SceneView.lastActiveSceneView == null || SceneView.lastActiveSceneView.camera == null)
                throw new InvalidOperationException("No active SceneView camera is available.");

            Directory.CreateDirectory(Path.Combine(Application.dataPath, "MCPTest/Screenshots"));
            Camera camera = SceneView.lastActiveSceneView.camera;
            const int width = 896;
            const int height = 512;
            RenderTexture target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            Texture2D image = new Texture2D(width, height, TextureFormat.RGBA32, false);
            RenderTexture previous = camera.targetTexture;
            RenderTexture.active = target;
            camera.targetTexture = target;
            camera.Render();
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            image.Apply();
            camera.targetTexture = previous;
            RenderTexture.active = null;

            string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string absolutePath = Path.Combine(Application.dataPath, "MCPTest/Screenshots", timestamp + "_" + sceneName + ".png");
            File.WriteAllBytes(absolutePath, image.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(image);
            target.Release();
            UnityEngine.Object.DestroyImmediate(target);
            AssetDatabase.Refresh();
            Debug.Log("[MCPGrayboxCaptureExporter] Exported " + absolutePath);
        }
    }
}
#endif

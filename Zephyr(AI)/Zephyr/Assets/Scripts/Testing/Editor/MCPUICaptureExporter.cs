#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Zephyr.EditorTools
{
    /// <summary>
    /// Renders Screen Space Overlay canvases into PNG files so UI text can be verified
    /// without entering Play Mode. Used by the MCP verification workflow.
    /// </summary>
    internal static class MCPUICaptureExporter
    {
        private const string OutputDirectory = "Assets/MCPTest/Screenshots";
        private const int Width = 960;
        private const int Height = 540;

        [MenuItem("Zephyr/MCP/Export UI Capture")]
        private static void ExportActiveScene()
        {
            string scenePath = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
            if (string.IsNullOrEmpty(scenePath))
                throw new InvalidOperationException("Save the scene before exporting a UI capture.");

            Canvas[] canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            if (canvases.Length == 0)
                throw new InvalidOperationException("No Canvas found in the active scene.");

            Directory.CreateDirectory(Path.Combine(Application.dataPath, "MCPTest/Screenshots"));

            foreach (Canvas canvas in canvases)
            {
                if (!canvas.gameObject.activeInHierarchy) continue;

                RenderTexture target = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
                GameObject cameraObject = new GameObject("MCP_UICaptureCamera", typeof(Camera));
                Camera camera = cameraObject.GetComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.05f, 0.06f, 0.06f, 1f);
                // Screen Space - Camera canvases are culled by the camera mask, so layer 5 (UI)
                // must be included or the capture only contains the clear color.
                camera.cullingMask = 1 << 5;
                camera.orthographic = true;
                camera.targetTexture = target;

                RenderMode previousMode = canvas.renderMode;
                Camera previousCamera = canvas.worldCamera;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 10f;
                Canvas.ForceUpdateCanvases();

                camera.Render();
                Canvas.ForceUpdateCanvases();

                Texture2D image = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
                image.Apply();
                RenderTexture.active = null;

                canvas.renderMode = previousMode;
                canvas.worldCamera = previousCamera;
                UnityEngine.Object.DestroyImmediate(cameraObject);

                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                string absolutePath = Path.Combine(Application.dataPath, "MCPTest/Screenshots",
                    timestamp + "_" + sceneName + "_" + canvas.name + ".png");
                File.WriteAllBytes(absolutePath, image.EncodeToPNG());

                UnityEngine.Object.DestroyImmediate(image);
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                Debug.Log("[MCPUICaptureExporter] Exported " + absolutePath);
            }

            AssetDatabase.Refresh();
        }
    }
}
#endif

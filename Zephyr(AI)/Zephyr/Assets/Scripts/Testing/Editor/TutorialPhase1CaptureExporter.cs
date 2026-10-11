#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Zephyr.Testing
{
    public static class TutorialPhase1CaptureExporter
    {
        [MenuItem("Zephyr/Testing/Export Tutorial Phase 1 Capture")]
        public static void Export()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                Debug.LogError("Tutorial Phase 1 capture failed: no MainCamera found.");
                return;
            }

            const int width = 1664;
            const int height = 192;
            string directory = Path.Combine(Application.dataPath, "MCPTest/Screenshots/Tutorial_Phase_1");
            Directory.CreateDirectory(directory);
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string outputPath = Path.Combine(directory, timestamp + "_Tutorial_Phase_1.png");

            Transform transform = camera.transform;
            Vector3 oldPosition = transform.position;
            float oldOrthoSize = camera.orthographicSize;
            float oldAspect = camera.aspect;
            RenderTexture oldTarget = camera.targetTexture;
            RenderTexture renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);

            try
            {
                transform.position = new Vector3(50f, 0f, -10f);
                camera.orthographic = true;
                camera.orthographicSize = 6f;
                camera.aspect = (float)width / height;
                camera.targetTexture = renderTexture;
                camera.Render();
                RenderTexture.active = renderTexture;
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                texture.Apply();
                File.WriteAllBytes(outputPath, texture.EncodeToPNG());
                AssetDatabase.Refresh();
                Debug.Log($"Tutorial Phase 1 capture exported: {outputPath}");
            }
            finally
            {
                camera.targetTexture = oldTarget;
                camera.aspect = oldAspect;
                camera.orthographicSize = oldOrthoSize;
                transform.position = oldPosition;
                RenderTexture.active = null;
                UnityEngine.Object.DestroyImmediate(texture);
                renderTexture.Release();
                UnityEngine.Object.DestroyImmediate(renderTexture);
            }
        }
    }
}
#endif

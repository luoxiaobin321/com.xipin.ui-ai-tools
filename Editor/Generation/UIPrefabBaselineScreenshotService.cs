using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Xipin.UIAITools
{
    public static class UIPrefabBaselineScreenshotService
    {
        const int Width = 1080;
        const int Height = 1920;

        public static string Capture(UIAIToolsProfile profile, string prefabPath)
        {
            UIRedesignRequestValidation.ValidateSourcePrefab(profile, prefabPath, "baseline screenshot");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
                throw new Exception("Missing UI prefab asset: " + prefabPath);

            Directory.CreateDirectory(profile.logRoot);
            var outputPath = UIReportFiles.GetPath(profile.logRoot, $"UIPrefabBaseline_{SafeName(prefabPath)}.png");
            var previousActive = RenderTexture.active;
            var stage = new GameObject("UI Prefab Baseline Screenshot Stage");
            stage.hideFlags = HideFlags.HideAndDontSave;
            var renderTexture = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
            var texture = new Texture2D(Width, Height, TextureFormat.RGBA32, false);

            try
            {
                var camera = CreateCamera(stage.transform, renderTexture);
                var canvas = CreateCanvas(stage.transform, camera);
                var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                instance.transform.SetParent(stage.transform, false);
                var rect = instance.GetComponent<RectTransform>();
                if (rect == null)
                    throw new Exception("UI prefab root must have RectTransform: " + prefabPath);
                rect.SetParent(canvas.transform, false);
                rect.anchoredPosition = Vector2.zero;
                rect.localScale = Vector3.one;

                Canvas.ForceUpdateCanvases();
                RenderTexture.active = renderTexture;
                GL.Clear(true, true, new Color(0, 0, 0, 0));
                camera.Render();
                texture.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
                texture.Apply();
                File.WriteAllBytes(outputPath, texture.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previousActive;
                renderTexture.Release();
                UnityEngine.Object.DestroyImmediate(texture);
                UnityEngine.Object.DestroyImmediate(renderTexture);
                UnityEngine.Object.DestroyImmediate(stage);
            }

            Debug.Log($"UI prefab baseline screenshot generated: {outputPath}");
            if (!Application.isBatchMode)
                EditorUtility.RevealInFinder(Path.GetFullPath(outputPath));
            return outputPath;
        }

        static Camera CreateCamera(Transform parent, RenderTexture renderTexture)
        {
            var cameraObject = new GameObject("Camera");
            cameraObject.hideFlags = HideFlags.HideAndDontSave;
            cameraObject.transform.SetParent(parent, false);
            cameraObject.transform.position = new Vector3(0, 0, -1000);
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = Height * 0.5f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 2000;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0, 0, 0, 0);
            camera.allowHDR = false;
            camera.allowMSAA = false;
            camera.targetTexture = renderTexture;
            return camera;
        }

        static RectTransform CreateCanvas(Transform parent, Camera camera)
        {
            var canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.hideFlags = HideFlags.HideAndDontSave;
            canvasObject.transform.SetParent(parent, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 100;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(Width, Height);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            return canvasObject.GetComponent<RectTransform>();
        }

        static string SafeName(string path)
        {
            return path.Replace("Assets/", "").Replace(".prefab", "").Replace('/', '_').Replace('\\', '_');
        }
    }
}

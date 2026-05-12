using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Xipin.UIAITools
{
    public sealed class UISkinRuntimePreviewWindow : EditorWindow
    {
        string manifestPath;
        Vector2 scrollPosition;

        [MenuItem("Tools/UIAITools/新版换皮/运行时预览窗口")]
        public static void Open()
        {
            var window = GetWindow<UISkinRuntimePreviewWindow>("换皮运行时预览");
            window.Show();
        }

        [MenuItem("Assets/UIAITools/运行时预览 skin.json", true)]
        static bool ValidatePreviewSelectedManifest()
        {
            return SelectedManifestPath().Length > 0;
        }

        [MenuItem("Assets/UIAITools/运行时预览 skin.json")]
        static void PreviewSelectedManifest()
        {
            var window = GetWindow<UISkinRuntimePreviewWindow>("换皮运行时预览");
            window.manifestPath = SelectedManifestPath();
            window.Show();
            if (EditorApplication.isPlaying)
                window.ShowPreview();
        }

        void OnEnable()
        {
            if (string.IsNullOrEmpty(manifestPath))
                manifestPath = SelectedManifestPath();
            if (string.IsNullOrEmpty(manifestPath))
                manifestPath = UISkinRuntimePreviewService.FindManifestPaths().FirstOrDefault() ?? "";
        }

        void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            EditorGUILayout.HelpBox("仅用于 Unity Editor Play Mode。预览实例会挂到包内临时 Canvas，不替换宿主 prefab。", MessageType.Info);
            DrawManifestPicker();
            EditorGUILayout.Space();
            DrawActions();
            DrawStatus();
            EditorGUILayout.EndScrollView();
        }

        void DrawManifestPicker()
        {
            var manifests = UISkinRuntimePreviewService.FindManifestPaths();
            using (new EditorGUILayout.HorizontalScope())
            {
                var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(manifestPath);
                var next = (TextAsset)EditorGUILayout.ObjectField("Skin Manifest", asset, typeof(TextAsset), false);
                if (next != asset)
                    manifestPath = AssetDatabase.GetAssetPath(next);
                if (GUILayout.Button("选择", GUILayout.Width(52)))
                    SelectManifestFile();
            }

            if (manifests.Count == 0)
                return;

            var index = Mathf.Max(0, manifests.IndexOf(manifestPath));
            var labels = manifests.Select(Path.GetDirectoryName).ToArray();
            var nextIndex = EditorGUILayout.Popup("已有工作包", index, labels);
            manifestPath = manifests[nextIndex];
        }

        void DrawActions()
        {
            using (new EditorGUI.DisabledScope(!EditorApplication.isPlaying))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("打开预览"))
                        ShowPreview();
                    using (new EditorGUI.DisabledScope(!UISkinRuntimePreviewService.IsPreviewing))
                    {
                        if (GUILayout.Button("关闭预览"))
                            UISkinRuntimePreviewService.Close();
                        if (GUILayout.Button("选中实例"))
                            Selection.activeGameObject = UISkinRuntimePreviewService.PreviewInstance;
                    }
                }
            }

            if (!EditorApplication.isPlaying)
                EditorGUILayout.HelpBox("先进入 Play Mode，再打开预览。", MessageType.Warning);
        }

        void DrawStatus()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("当前 Manifest", manifestPath);
            EditorGUILayout.LabelField("预览状态", UISkinRuntimePreviewService.IsPreviewing ? "已打开" : "未打开");
            if (!string.IsNullOrEmpty(UISkinRuntimePreviewService.LastMessage))
                EditorGUILayout.HelpBox(UISkinRuntimePreviewService.LastMessage, MessageType.None);
        }

        void ShowPreview()
        {
            try
            {
                UISkinRuntimePreviewService.Show(manifestPath);
                EditorApplication.ExecuteMenuItem("Window/General/Game");
            }
            catch (Exception ex)
            {
                UISkinRuntimePreviewService.LastMessage = ex.Message;
                Debug.LogError(ex);
            }
        }

        void SelectManifestFile()
        {
            var fullPath = EditorUtility.OpenFilePanel("选择 skin.json", Application.dataPath, "json");
            if (string.IsNullOrEmpty(fullPath))
                return;
            manifestPath = UISkinRuntimePreviewService.ToAssetPath(fullPath);
        }

        static string SelectedManifestPath()
        {
            var path = AssetDatabase.GetAssetPath(Selection.activeObject);
            return path.EndsWith("/skin.json", StringComparison.OrdinalIgnoreCase) ? path : "";
        }
    }

    [InitializeOnLoad]
    public static class UISkinRuntimePreviewService
    {
        const string PreviewRootName = "__UIAIToolsSkinRuntimePreview";
        const int SortingOrder = 32760;
        static readonly Vector2 ReferenceResolution = new Vector2(1080, 1920);

        static GameObject previewRoot;
        static GameObject previewInstance;

        public static string LastMessage { get; set; }
        public static GameObject PreviewInstance => previewInstance;
        public static bool IsPreviewing => previewInstance != null;

        static UISkinRuntimePreviewService()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        public static List<string> FindManifestPaths()
        {
            var root = UIAIToolsHostWorkspaceInitializer.Root + "/Skinning";
            if (!Directory.Exists(root))
                return new List<string>();
            return Directory.GetFiles(root, "skin.json", SearchOption.AllDirectories)
                .Select(NormalizePath)
                .OrderBy(path => path)
                .ToList();
        }

        public static void Show(string manifestPath)
        {
            if (!EditorApplication.isPlaying)
                throw new Exception("Skin runtime preview requires Unity Play Mode.");

            var normalizedManifestPath = NormalizeManifestPath(manifestPath);
            var manifest = UISkinContractService.LoadManifest(normalizedManifestPath);
            var prefabPath = NormalizePrefabPath(manifest.generated.outputPrefabPath);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
                throw new Exception("Skin runtime preview prefab not found: " + prefabPath);

            Close();
            previewRoot = CreatePreviewRoot();
            previewInstance = UnityEngine.Object.Instantiate(prefab, previewRoot.transform, false);
            previewInstance.name = prefab.name + " (UIAITools Preview)";
            PreparePreviewInstance(previewInstance);
            UnityEngine.Object.DontDestroyOnLoad(previewRoot);
            Selection.activeGameObject = previewInstance;
            LastMessage = "预览已打开：" + prefabPath + "。按 Esc 或窗口按钮关闭。";
        }

        public static void Close()
        {
            DestroyPreviewObject(previewRoot);
            previewRoot = null;
            previewInstance = null;
            LastMessage = "预览已关闭。";
        }

        public static string ToAssetPath(string fullPath)
        {
            var projectAssets = NormalizePath(Application.dataPath);
            var normalized = NormalizePath(fullPath);
            if (!normalized.StartsWith(projectAssets + "/", StringComparison.Ordinal))
                throw new Exception("Selected manifest must be under Assets: " + fullPath);
            return "Assets" + normalized.Substring(projectAssets.Length);
        }

        static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode || state == PlayModeStateChange.EnteredEditMode)
                Close();
        }

        static GameObject CreatePreviewRoot()
        {
            var root = new GameObject(PreviewRootName, typeof(RectTransform));
            root.hideFlags = HideFlags.DontSave;
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = SortingOrder;
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            root.AddComponent<GraphicRaycaster>();
            root.AddComponent<UISkinRuntimePreviewHotkey>();
            Stretch(root.GetComponent<RectTransform>());
            return root;
        }

        static void PreparePreviewInstance(GameObject instance)
        {
            var rect = instance.GetComponent<RectTransform>();
            if (rect != null)
                Stretch(rect);

            foreach (var canvas in instance.GetComponentsInChildren<Canvas>(true))
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.worldCamera = null;
                canvas.overrideSorting = true;
                canvas.sortingOrder = SortingOrder + 1;
                var scaler = canvas.GetComponent<CanvasScaler>();
                if (scaler != null)
                    scaler.referenceResolution = ReferenceResolution;
            }
        }

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
        }

        static string NormalizeManifestPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new Exception("Select a skin.json before opening skin runtime preview.");
            var normalized = NormalizePath(path);
            if (!normalized.EndsWith("/skin.json", StringComparison.OrdinalIgnoreCase))
                throw new Exception("Skin runtime preview requires a skin.json asset path.");
            if (!File.Exists(normalized))
                throw new Exception("Skin manifest not found: " + normalized);
            return normalized;
        }

        static string NormalizePrefabPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new Exception("Skin manifest generated.outputPrefabPath is empty.");
            var normalized = NormalizePath(path);
            if (!normalized.StartsWith("Assets/", StringComparison.Ordinal) || !normalized.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                throw new Exception("Skin runtime preview requires an Assets prefab path: " + path);
            return normalized;
        }

        static string NormalizePath(string path)
        {
            return path.Replace('\\', '/').Trim();
        }

        static void DestroyPreviewObject(UnityEngine.Object target)
        {
            if (target == null)
                return;
            if (EditorApplication.isPlaying)
                UnityEngine.Object.Destroy(target);
            else
                UnityEngine.Object.DestroyImmediate(target);
        }
    }

    public sealed class UISkinRuntimePreviewHotkey : MonoBehaviour
    {
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
                UISkinRuntimePreviewService.Close();
        }
    }
}

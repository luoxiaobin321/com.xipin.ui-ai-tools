using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Xipin.UIAITools
{
    public class UIRedesignBriefWindow : EditorWindow
    {
        UIAIToolsProfile profile;
        Object prefabAsset;
        Object previewAsset;
        Object inputFolderAsset;
        Object outputFolderAsset;
        string stylePrompt = "";
        string sourcePreviewPath = "";
        string inputImageFolder = "";
        string outputFolder = "";
        string referenceImagePaths = "";
        Vector2 scroll;

        public static void ShowWindow(UIAIToolsProfile profile)
        {
            var window = GetWindow<UIRedesignBriefWindow>("UI改版Brief");
            window.profile = profile;
            window.UseSelection();
            window.Show();
        }

        void OnGUI()
        {
            if (profile == null)
            {
                EditorGUILayout.HelpBox("缺少 UIAIToolsProfile。", MessageType.Error);
                return;
            }

            scroll = EditorGUILayout.BeginScrollView(scroll);
            DrawPrefab();
            DrawPathObject("原界面预览", ref previewAsset, ref sourcePreviewPath);
            stylePrompt = EditorGUILayout.TextField("目标风格", stylePrompt);
            DrawPathObject("新切图目录", ref inputFolderAsset, ref inputImageFolder);
            DrawPathObject("输出目录", ref outputFolderAsset, ref outputFolder);
            referenceImagePaths = EditorGUILayout.TextField("参考图路径", referenceImagePaths);
            using (new EditorGUI.DisabledScope(PrefabPath().Length == 0))
            {
                if (GUILayout.Button("生成改版 Brief"))
                    UIRedesignBriefService.GenerateBrief(profile, Request());
            }
            EditorGUILayout.EndScrollView();
        }

        void DrawPrefab()
        {
            EditorGUILayout.BeginHorizontal();
            prefabAsset = EditorGUILayout.ObjectField("Prefab", prefabAsset, typeof(GameObject), false);
            if (GUILayout.Button("选中", GUILayout.Width(56)))
                UseSelection();
            EditorGUILayout.EndHorizontal();
        }

        void DrawPathObject(string label, ref Object asset, ref string path)
        {
            asset = EditorGUILayout.ObjectField(label, asset, typeof(Object), false);
            var assetPath = AssetDatabase.GetAssetPath(asset);
            if (!string.IsNullOrEmpty(assetPath))
                path = assetPath;
            path = EditorGUILayout.TextField(label + "路径", path);
        }

        void UseSelection()
        {
            var path = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (!string.IsNullOrEmpty(path) && path.EndsWith(".prefab"))
            {
                prefabAsset = Selection.activeObject;
                outputFolder = "Assets/Art/UI/AI/" + path.Replace("Assets/Bundle/Prefab/", "").Replace(".prefab", "").Replace('/', '_');
            }
        }

        UIRedesignRequest Request()
        {
            return new UIRedesignRequest
            {
                sourcePrefabPath = PrefabPath(),
                sourcePreviewPath = sourcePreviewPath,
                stylePrompt = stylePrompt,
                inputImageFolder = inputImageFolder,
                reuseCandidateReportPath = UIReportFiles.GetPath(profile.logRoot, UIReportFiles.ReuseIndex),
                outputFolder = outputFolder,
                referenceImagePaths = referenceImagePaths.Split(';').Where(p => p.Length > 0).ToList()
            };
        }

        string PrefabPath()
        {
            return AssetDatabase.GetAssetPath(prefabAsset);
        }
    }
}

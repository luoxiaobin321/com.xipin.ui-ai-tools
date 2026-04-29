using System.IO;
using UnityEditor;
using UnityEngine;

namespace Xipin.UIAITools
{
    public class UIRedesignDraftWindow : EditorWindow
    {
        UIRedesignDraft draft;
        Vector2 scroll;

        public static void ShowDraft(UIRedesignDraft draft)
        {
            var window = GetWindow<UIRedesignDraftWindow>("UI改版草稿");
            window.draft = draft;
            window.Show();
        }

        void OnGUI()
        {
            if (draft == null)
            {
                EditorGUILayout.HelpBox("没有可展示的改版草稿。", MessageType.Info);
                return;
            }

            scroll = EditorGUILayout.BeginScrollView(scroll);
            DrawPath("新版预览", draft.draftPreviewPath);
            DrawPath("生成图片目录", draft.generatedImageFolder);
            DrawReadOnlyToggle("需要人工确认", draft.requiresConfirmation);
            DrawRisks();
            DrawReplacementPlan();
            EditorGUILayout.EndScrollView();
        }

        static void DrawPath(string label, string path)
        {
            EditorGUILayout.BeginHorizontal();
            DrawReadOnlyText(label, path);
            using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(path) || !File.Exists(path) && !Directory.Exists(path)))
            {
                if (GUILayout.Button("定位", GUILayout.Width(56)))
                    EditorUtility.RevealInFinder(Path.GetFullPath(path));
            }
            EditorGUILayout.EndHorizontal();
        }

        static void DrawReadOnlyText(string label, string value)
        {
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.TextField(label, value);
        }

        static void DrawReadOnlyToggle(string label, bool value)
        {
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.Toggle(label, value);
        }

        void DrawRisks()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("风险", EditorStyles.boldLabel);
            if (draft.risks.Count == 0)
            {
                EditorGUILayout.LabelField("无");
                return;
            }

            foreach (var risk in draft.risks)
                EditorGUILayout.HelpBox(risk, MessageType.Warning);
        }

        void DrawReplacementPlan()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("替换计划", EditorStyles.boldLabel);
            foreach (var item in draft.replacementPlan.items)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                DrawPath("旧资源", item.oldAssetPath);
                DrawPath("新资源", item.newAssetPath);
                DrawPath("目标图集", item.targetAtlasPath);
                DrawReadOnlyToggle("保持 GUID", item.preserveGuid);
                DrawReadOnlyToggle("需要确认", item.requiresConfirmation);
                DrawReadOnlyText("原因", item.reason);
                EditorGUILayout.EndVertical();
            }
        }
    }
}

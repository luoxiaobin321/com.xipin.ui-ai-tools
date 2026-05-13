using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Xipin.UIAITools
{
    public sealed class UIAIToolsWorkbenchWindow : EditorWindow
    {
        readonly string[] tabs = { "自动整理", "复用反查", "自动制作", "新版换皮" };
        int tab;
        Vector2 scroll;
        string queryImagePath = "";
        string featureName = "DemoPanel";
        string uiType = "Panel";
        string stylePrompt = "";
        string referenceImages = "";
        string requiredInteractions = "";
        string dataBindings = "";
        string constraints = "";
        string briefJsonPath = "";
        string layoutDraftJsonPath = "";
        string skinManifestPath = "";
        string lastMessage = "";

        [MenuItem("Tools/UIAITools/打开工作台", false, 1)]
        public static void Open()
        {
            GetWindow<UIAIToolsWorkbenchWindow>("UIAITools").Show();
        }

        void OnEnable()
        {
            UIAIToolsHostWorkspaceInitializer.Ensure();
            if (string.IsNullOrEmpty(skinManifestPath))
                skinManifestPath = UISkinRuntimePreviewService.FindManifestPaths().FirstOrDefault() ?? "";
        }

        void OnGUI()
        {
            DrawHeader();
            tab = GUILayout.Toolbar(tab, tabs);
            scroll = EditorGUILayout.BeginScrollView(scroll);
            if (tab == 0)
                DrawScanning();
            else if (tab == 1)
                DrawReuseSearch();
            else if (tab == 2)
                DrawCreation();
            else
                DrawSkinning();
            EditorGUILayout.EndScrollView();
            if (!string.IsNullOrEmpty(lastMessage))
                EditorGUILayout.HelpBox(lastMessage, MessageType.None);
        }

        void DrawHeader()
        {
            EditorGUILayout.LabelField("Profile", UIAIToolsHostWorkspaceInitializer.ProfilePath);
            EditorGUILayout.LabelField("Catalog", UIAIToolsHostWorkspaceInitializer.CatalogPath);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("初始化工作区", GUILayout.Width(120)))
                    RunAction(() => UIAIToolsHostWorkspaceInitializer.Ensure().Summary);
                if (GUILayout.Button("打开工作区", GUILayout.Width(120)))
                    OpenFolder(Profile().workspaceRoot);
                if (GUILayout.Button("打开报告根", GUILayout.Width(120)))
                    OpenFolder(Profile().logRoot);
            }
            EditorGUILayout.Space();
        }

        void DrawScanning()
        {
            if (GUILayout.Button("生成扫描报告"))
                RunAction(() => { UIAssetScanService.Run(ReportProfile("Scanning"), Catalog()); return "扫描报告已生成。"; });
            if (GUILayout.Button("验证扫描报告"))
                RunAction(() => { UIReportValidationService.Validate(ReportProfile("Scanning")); return "扫描报告验证通过。"; });
            if (GUILayout.Button("生成扫描摘要"))
                RunAction(() => { UIScanSummaryService.Generate(ReportProfile("Scanning")); return "扫描摘要已生成。"; });
            if (GUILayout.Button("生成面板实测清单"))
                RunAction(() => { UIScanSummaryService.GeneratePanelFocus(ReportProfile("Scanning")); return "面板实测清单已生成。"; });
            if (GUILayout.Button("生成组件候选索引"))
                RunAction(() => { UIComponentCandidateIndexService.Generate(ReportProfile("Scanning"), ReportProfile("Creation"), Catalog()); return "组件候选索引已生成。"; });
            if (GUILayout.Button("验证组件候选索引"))
                RunAction(() => { UIComponentCandidateIndexService.Validate(ReportProfile("Creation")); return "组件候选索引验证通过。"; });
            if (GUILayout.Button("打开整理报告"))
                OpenFolder(ReportProfile("Scanning").logRoot);
        }

        void DrawReuseSearch()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                queryImagePath = EditorGUILayout.TextField("裁图路径", queryImagePath);
                if (GUILayout.Button("选择", GUILayout.Width(56)))
                    queryImagePath = EditorUtility.OpenFilePanel("选择缺图裁剪图", "", "png,jpg,jpeg");
            }
            using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(queryImagePath)))
            {
                if (GUILayout.Button("按裁图查找已有资源"))
                    RunAction(() => { UIAssetScanService.SearchReuseByImagePath(ReportProfile("ReuseSearch"), Catalog(), queryImagePath); return "复用反查已完成。"; });
                if (GUILayout.Button("生成反查摘要"))
                    RunAction(() => UIReuseSearchResultService.GenerateSummary(ReportProfile("ReuseSearch"), queryImagePath));
            }
            if (GUILayout.Button("打开反查报告"))
                OpenFolder(ReportProfile("ReuseSearch").logRoot);
        }

        void DrawCreation()
        {
            featureName = EditorGUILayout.TextField("功能名", featureName);
            uiType = EditorGUILayout.TextField("UI 类型", uiType);
            stylePrompt = EditorGUILayout.TextField("风格提示", stylePrompt);
            referenceImages = EditorGUILayout.TextField("参考图 ; 分隔", referenceImages);
            requiredInteractions = EditorGUILayout.TextField("交互 ; 分隔", requiredInteractions);
            dataBindings = EditorGUILayout.TextField("数据绑定 ; 分隔", dataBindings);
            constraints = EditorGUILayout.TextField("约束 ; 分隔", constraints);

            if (GUILayout.Button("生成组件候选索引"))
                RunAction(() => { UIComponentCandidateIndexService.Generate(ReportProfile("Scanning"), ReportProfile("Creation"), Catalog()); return "组件候选索引已生成。"; });
            if (GUILayout.Button("生成 Brief 模板"))
                RunAction(() =>
                {
                    UIComponentCandidateIndexService.Generate(ReportProfile("Scanning"), ReportProfile("Creation"), Catalog());
                    briefJsonPath = UICreationBriefTemplateService.Generate(ReportProfile("Creation"), CreationBrief());
                    return briefJsonPath;
                });

            using (new EditorGUILayout.HorizontalScope())
            {
                briefJsonPath = EditorGUILayout.TextField("Brief JSON", briefJsonPath);
                if (GUILayout.Button("选择", GUILayout.Width(56)))
                    briefJsonPath = EditorUtility.OpenFilePanel("选择 Brief JSON", "", "json");
            }
            using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(briefJsonPath)))
            {
                if (GUILayout.Button("生成 Layout Draft 模板"))
                    RunAction(() =>
                    {
                        layoutDraftJsonPath = UILayoutDraftTemplateService.Generate(ReportProfile("Creation"), briefJsonPath);
                        return layoutDraftJsonPath;
                    });
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                layoutDraftJsonPath = EditorGUILayout.TextField("Layout Draft", layoutDraftJsonPath);
                if (GUILayout.Button("选择", GUILayout.Width(56)))
                    layoutDraftJsonPath = EditorUtility.OpenFilePanel("选择 Layout Draft JSON", "", "json");
            }
            using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(layoutDraftJsonPath)))
            {
                if (GUILayout.Button("Dry Run"))
                    RunAction(() => UICreationLayoutDryRunService.Run(ReportProfile("Creation"), layoutDraftJsonPath));
                if (GUILayout.Button("验证 Dry Run"))
                    RunAction(() => { UICreationLayoutDryRunService.ValidateNoErrors(ReportProfile("Creation")); return "Dry Run 验证通过。"; });
                if (GUILayout.Button("生成宿主生成清单"))
                    RunAction(() => UICreationHostGenerateChecklistService.Generate(ReportProfile("Creation"), layoutDraftJsonPath));
                if (GUILayout.Button("验证宿主生成清单"))
                    RunAction(() => { UICreationHostGenerateChecklistService.ValidateNoBlockingSteps(ReportProfile("Creation")); return "宿主生成清单验证通过。"; });
            }
            if (GUILayout.Button("打开制作报告"))
                OpenFolder(ReportProfile("Creation").logRoot);
        }

        void DrawSkinning()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(skinManifestPath);
                var next = (TextAsset)EditorGUILayout.ObjectField("Skin Manifest", asset, typeof(TextAsset), false);
                if (next != asset)
                    skinManifestPath = AssetDatabase.GetAssetPath(next);
                if (GUILayout.Button("选择", GUILayout.Width(56)))
                    skinManifestPath = SelectAssetJson("选择 skin.json");
            }

            using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(skinManifestPath)))
            {
                if (GUILayout.Button("打开运行时预览窗口"))
                    UISkinRuntimePreviewWindow.OpenForManifest(skinManifestPath);
                if (GUILayout.Button("打开换皮工作包"))
                    OpenFolder(Path.GetDirectoryName(skinManifestPath));
                if (GUILayout.Button("打开换皮报告"))
                    OpenFolder(UISkinContractService.ReportFolder(Path.GetDirectoryName(skinManifestPath)));
            }
        }

        UICreationBrief CreationBrief()
        {
            return new UICreationBrief
            {
                featureName = featureName,
                uiType = uiType,
                targetFolder = Profile().workspaceRoot.TrimEnd('/', '\\') + "/Creation/" + featureName,
                stylePrompt = stylePrompt,
                referenceImagePaths = SplitList(referenceImages),
                requiredInteractions = SplitList(requiredInteractions),
                dataBindings = SplitList(dataBindings),
                constraints = SplitList(constraints),
                requiresConfirmation = true
            };
        }

        UIAIToolsProfile Profile()
        {
            var profile = AssetDatabase.LoadAssetAtPath<UIAIToolsProfile>(UIAIToolsHostWorkspaceInitializer.ProfilePath);
            if (profile == null)
                UIAIToolsHostWorkspaceInitializer.Ensure();
            return AssetDatabase.LoadAssetAtPath<UIAIToolsProfile>(UIAIToolsHostWorkspaceInitializer.ProfilePath);
        }

        UIControlCatalog Catalog()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<UIControlCatalog>(UIAIToolsHostWorkspaceInitializer.CatalogPath);
            if (catalog == null)
                UIAIToolsHostWorkspaceInitializer.Ensure();
            return AssetDatabase.LoadAssetAtPath<UIControlCatalog>(UIAIToolsHostWorkspaceInitializer.CatalogPath);
        }

        UIAIToolsProfile ReportProfile(string name)
        {
            var source = Profile();
            var profile = CreateInstance<UIAIToolsProfile>();
            profile.workspaceRoot = source.workspaceRoot;
            profile.prefabRoot = source.prefabRoot;
            profile.artUIRoot = source.artUIRoot;
            profile.uiAtlasRoot = source.uiAtlasRoot;
            profile.uiTextureRoot = source.uiTextureRoot;
            profile.mapTextureRoot = source.mapTextureRoot;
            profile.yooAssetAddressRule = source.yooAssetAddressRule;
            profile.projectStylePrompt = source.projectStylePrompt;
            profile.excludeMapFromUITriage = source.excludeMapFromUITriage;
            profile.textSearchRoots = new List<string>(source.textSearchRoots);
            profile.logRoot = source.logRoot.TrimEnd('/', '\\') + "/" + name;
            return profile;
        }

        void RunAction(Func<string> action)
        {
            try
            {
                lastMessage = action();
            }
            catch (Exception exception)
            {
                lastMessage = exception.Message;
                Debug.LogError(exception);
            }
        }

        static List<string> SplitList(string value)
        {
            return value.Split(new[] { ';', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(item => item.Trim())
                .Where(item => item.Length > 0)
                .ToList();
        }

        static string SelectAssetJson(string title)
        {
            var path = EditorUtility.OpenFilePanel(title, Application.dataPath, "json");
            if (string.IsNullOrEmpty(path))
                return "";
            var normalized = path.Replace('\\', '/');
            var assets = Application.dataPath.Replace('\\', '/');
            return normalized.StartsWith(assets + "/", StringComparison.Ordinal) ? "Assets" + normalized.Substring(assets.Length) : normalized;
        }

        static void OpenFolder(string path)
        {
            if (string.IsNullOrEmpty(path))
                return;
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            EditorUtility.RevealInFinder(Path.GetFullPath(path));
        }
    }
}

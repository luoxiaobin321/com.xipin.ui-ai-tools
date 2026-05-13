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
        readonly string[] tabs = { "自动整理", "复用反查", "自动制作", "新版换皮", "训练沉淀" };
        readonly string[] trainingScopes = { "宿主专项", "包内通用候选" };
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
        string aiApiKey = "";
        string aiResponsesUrl = "";
        string aiModel = "";
        bool showAISettings;
        int trainingScope;
        string trainingTitle = "";
        string trainingBody = "";
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
            LoadAISettings();
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
            else if (tab == 3)
                DrawSkinning();
            else
                DrawTraining();
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
            DrawAISettings();
            EditorGUILayout.Space();
        }

        void DrawAISettings()
        {
            EditorGUILayout.LabelField("AI 配置", (string.IsNullOrEmpty(aiApiKey) ? "未配置" : "已配置") + " / " + aiModel);
            showAISettings = EditorGUILayout.Foldout(showAISettings, "全局 AI 配置", true);
            if (!showAISettings)
                return;

            EditorGUI.indentLevel++;
            aiResponsesUrl = EditorGUILayout.TextField("AI 接口地址", aiResponsesUrl);
            aiModel = EditorGUILayout.TextField("AI 模型", aiModel);
            aiApiKey = EditorGUILayout.PasswordField("AI API Key", aiApiKey);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("从 Codex 配置读取 AI 地址"))
                {
                    var settings = UIAIToolsAISettingsService.LoadCodexDefaults();
                    aiResponsesUrl = settings.ResponsesUrl;
                    aiModel = settings.Model;
                }
                if (GUILayout.Button("保存 AI 配置"))
                    RunAction(() =>
                    {
                        UIAIToolsAISettingsService.Save(aiApiKey, aiResponsesUrl, aiModel);
                        LoadAISettings();
                        return "AI 配置已保存。";
                    });
            }
            EditorGUI.indentLevel--;
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
                if (GUILayout.Button("AI 映射美术包"))
                    RunAction(() => UISkinAIMappingService.Run(skinManifestPath));
                if (GUILayout.Button("打开运行时预览窗口"))
                    UISkinRuntimePreviewWindow.OpenForManifest(skinManifestPath);
                if (GUILayout.Button("打开换皮工作包"))
                    OpenFolder(Path.GetDirectoryName(skinManifestPath));
                if (GUILayout.Button("打开换皮报告"))
                    OpenFolder(UISkinContractService.ReportFolder(Path.GetDirectoryName(skinManifestPath)));
            }
        }

        void DrawTraining()
        {
            EditorGUILayout.HelpBox("训练沉淀默认不写 Packages。宿主专项记录当前项目经验；包内通用候选先写到宿主工作区，后续由包维护流程提升到包仓库。", MessageType.Info);
            trainingScope = EditorGUILayout.Popup("记录类型", trainingScope, trainingScopes);
            trainingTitle = EditorGUILayout.TextField("标题", trainingTitle);
            EditorGUILayout.LabelField("内容");
            trainingBody = EditorGUILayout.TextArea(trainingBody, GUILayout.MinHeight(120));
            using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(trainingTitle) || string.IsNullOrWhiteSpace(trainingBody)))
            {
                if (GUILayout.Button("记录训练沉淀"))
                    RunAction(() => trainingScope == 0
                        ? UIAIToolsTrainingLogService.RecordHost(trainingTitle, trainingBody)
                        : UIAIToolsTrainingLogService.RecordPackageCandidate(trainingTitle, trainingBody));
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("打开宿主专项记录"))
                    OpenFolder(UIAIToolsTrainingLogService.HostRoot);
                if (GUILayout.Button("打开包内候选记录"))
                    OpenFolder(UIAIToolsTrainingLogService.PackageCandidateRoot);
            }
            if (GUILayout.Button("自动维护旧沉淀"))
                RunAction(UIAIToolsTrainingLogService.GenerateLegacyTriageReport);
        }

        void LoadAISettings()
        {
            var settings = UIAIToolsAISettingsService.Load();
            aiApiKey = settings.ApiKey;
            aiResponsesUrl = settings.ResponsesUrl;
            aiModel = settings.Model;
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

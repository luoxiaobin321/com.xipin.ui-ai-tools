using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Xipin.UIAITools
{
    [InitializeOnLoad]
    public static class UIAIToolsHostWorkspaceInitializer
    {
        public const string Root = "Assets/UIAITools";
        public const string ReportsRoot = Root + "/Reports";
        public const string ProfilePath = Root + "/Settings/UIAIToolsProfile.asset";
        public const string CatalogPath = Root + "/Settings/UIControlCatalog.asset";

        static readonly string[] RequiredFolders =
        {
            Root,
            Root + "/Settings",
            Root + "/Skinning",
            Root + "/Creation",
            Root + "/GlobalRuntimeUpdates",
            ReportsRoot,
            ReportsRoot + "/Scanning",
            ReportsRoot + "/ReuseSearch",
            ReportsRoot + "/Creation",
            ReportsRoot + "/Skinning",
            ReportsRoot + "/GlobalRuntimeUpdates",
            Root + "/Docs",
            Root + "/Docs/UIControlContracts",
            Root + "/Docs/UIControlContracts/Controls",
            Root + "/Docs/UIControlContracts/Variants",
            Root + "/Docs/ReusableCandidates"
        };

        static UIAIToolsHostWorkspaceInitializer()
        {
            if (!Application.isBatchMode)
                EditorApplication.delayCall += AutoEnsure;
        }

        [MenuItem("Tools/UIAITools/初始化宿主工作区")]
        public static void EnsureFromMenu()
        {
            var report = Ensure();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<DefaultAsset>(Root);
            EditorUtility.DisplayDialog("UIAITools", report.Summary, "OK");
        }

        public static void EnsureBatch()
        {
            var report = Ensure();
            Debug.Log(report.Summary);
        }

        public static UIAIToolsHostWorkspaceReport Ensure()
        {
            var report = new UIAIToolsHostWorkspaceReport(Root);
            foreach (var folder in RequiredFolders)
                EnsureFolder(folder, report);

            if (report.NeedsRefresh)
                AssetDatabase.Refresh();
            EnsureProfile(report);
            EnsureCatalog(report);
            EnsureDocs(report);
            if (report.HasChanges)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            return report;
        }

        public static void ValidateContract()
        {
            Expect("Root", Root, "Assets/UIAITools");
            Expect("ReportsRoot", ReportsRoot, "Assets/UIAITools/Reports");
            ExpectFolder(Root + "/Settings");
            ExpectFolder(Root + "/Skinning");
            ExpectFolder(Root + "/Creation");
            ExpectFolder(ReportsRoot + "/Skinning");
            ExpectFolder(Root + "/Docs/UIControlContracts/Controls");
            ExpectFolder(Root + "/Docs/UIControlContracts/Variants");
            var profile = CreateHostProfile();
            try
            {
                Expect("workspaceRoot", profile.workspaceRoot, Root);
                Expect("logRoot", profile.logRoot, ReportsRoot);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(profile);
            }
            ExpectContains(HostReadme(), "删除这个文件夹");
            ExpectContains(ControlContractsReadme(), "按需读取");
            Debug.Log("UI AI Tools host workspace initializer contract validation passed.");
        }

        static void AutoEnsure()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            Ensure();
        }

        static void EnsureFolder(string path, UIAIToolsHostWorkspaceReport report)
        {
            if (Directory.Exists(path))
            {
                if (!AssetDatabase.IsValidFolder(path))
                    report.NeedsRefresh = true;
                report.Existing.Add(path);
                return;
            }
            Directory.CreateDirectory(path);
            report.NeedsRefresh = true;
            report.Created.Add(path);
        }

        static void EnsureProfile(UIAIToolsHostWorkspaceReport report)
        {
            var profile = AssetDatabase.LoadAssetAtPath<UIAIToolsProfile>(ProfilePath);
            if (profile == null)
            {
                profile = CreateHostProfile();
                AssetDatabase.CreateAsset(profile, ProfilePath);
                report.Created.Add(ProfilePath);
                return;
            }

            var changed = false;
            if (string.IsNullOrWhiteSpace(profile.workspaceRoot))
            {
                profile.workspaceRoot = Root;
                changed = true;
            }
            if (string.IsNullOrWhiteSpace(profile.logRoot) || profile.logRoot == "UIAIToolsReports")
            {
                profile.logRoot = ReportsRoot;
                changed = true;
            }
            if (changed)
            {
                EditorUtility.SetDirty(profile);
                report.Updated.Add(ProfilePath);
            }
        }

        static UIAIToolsProfile CreateHostProfile()
        {
            var profile = ScriptableObject.CreateInstance<UIAIToolsProfile>();
            profile.workspaceRoot = Root;
            profile.logRoot = ReportsRoot;
            return profile;
        }

        static void EnsureCatalog(UIAIToolsHostWorkspaceReport report)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<UIControlCatalog>(CatalogPath);
            if (catalog != null)
                return;
            catalog = ScriptableObject.CreateInstance<UIControlCatalog>();
            AssetDatabase.CreateAsset(catalog, CatalogPath);
            report.Created.Add(CatalogPath);
        }

        static void EnsureDocs(UIAIToolsHostWorkspaceReport report)
        {
            WriteIfMissing(Root + "/README.md", HostReadme(), report);
            WriteIfMissing(Root + "/Docs/README.md", DocsReadme(), report);
            WriteIfMissing(Root + "/Docs/UIControlContracts/README.md", ControlContractsReadme(), report);
            WriteIfMissing(Root + "/Docs/UIControlContracts/Controls/README.md", ControlContractsFolderReadme(), report);
            WriteIfMissing(Root + "/Docs/UIControlContracts/Variants/README.md", VariantContractsFolderReadme(), report);
            WriteIfMissing(Root + "/Docs/ReusableCandidates/README.md", ReusableCandidatesReadme(), report);
        }

        static void WriteIfMissing(string path, string content, UIAIToolsHostWorkspaceReport report)
        {
            if (File.Exists(path))
                return;
            File.WriteAllText(path, content, new UTF8Encoding(false));
            report.Created.Add(path);
        }

        static string HostReadme()
        {
            return "# UIAITools 宿主工作区\n\n"
                   + "这个目录由 `com.xipin.ui-ai-tools` 包创建和维护，用来集中放置配置、工作包、报告、项目内契约和训练沉淀。\n\n"
                   + "- `Settings`：默认 `UIAIToolsProfile` 和 `UIControlCatalog`。\n"
                   + "- `Skinning`、`Creation`、`GlobalRuntimeUpdates`：工具生成的项目内工作包。\n"
                   + "- `Reports`：扫描、复用反查、自动制作、换皮和全局资源更新报告。\n"
                   + "- `Docs`：只放 UIAITools 相关的宿主契约和可复用候选，不混入项目通用文档。\n\n"
                   + "如果要从项目中移除 UIAITools，先移除 UPM 包依赖，再删除这个文件夹即可清理干净。\n";
        }

        static string DocsReadme()
        {
            return "# UIAITools 宿主文档\n\n"
                   + "这里记录当前项目对 UIAITools 的接入规则。入口保持轻量，具体控件契约按需读取，避免每次任务把无关文档拉进上下文。\n\n"
                   + "- `UIControlContracts`：通用控件、项目控件和界面局部变体的渐进式契约。\n"
                   + "- `ReusableCandidates`：后续可能抽成通用预设、插件或组件的候选记录。\n";
        }

        static string ControlContractsReadme()
        {
            return "# UI 控件契约索引\n\n"
                   + "控件契约用于说明运行时数据所有权、可换皮范围、布局写入者和生成策略。默认只读这个索引；只有遇到对应控件、通用预设或界面变体时，再按需读取细节文件。\n\n"
                   + "| 目录 | Load When |\n"
                   + "| --- | --- |\n"
                   + "| `Controls` | 遇到项目通用控件或基础包控件时读取。 |\n"
                   + "| `Variants` | 遇到某个界面里的局部变体或特殊摆放规则时读取。 |\n";
        }

        static string ControlContractsFolderReadme()
        {
            return "# 通用控件契约\n\n"
                   + "每个文件描述一个可复用控件或通用预设。只有控件逻辑、运行时赋值、布局所有权或可换皮边界能复用到多个界面时，才在这里沉淀。\n";
        }

        static string VariantContractsFolderReadme()
        {
            return "# 界面变体契约\n\n"
                   + "这里记录某个 UI 内部的局部变体。优先沉淀通用判断方法，只有项目框架控件或真实通用预设才写成独立控件规则。\n";
        }

        static string ReusableCandidatesReadme()
        {
            return "# 可复用候选\n\n"
                   + "这里记录后续可能抽成通用预设、插件或独立组件的候选项。还没有复用价值证据前，只记录候选和触发场景，不提前设计接口。\n";
        }

        static void ExpectFolder(string path)
        {
            if (Array.IndexOf(RequiredFolders, path) < 0)
                throw new Exception("UI AI Tools host workspace initializer contract failed: missing folder " + path);
        }

        static void Expect(string field, string actual, string expected)
        {
            if (actual != expected)
                throw new Exception("UI AI Tools host workspace initializer contract failed: " + field);
        }

        static void ExpectContains(string content, string expected)
        {
            if (!content.Contains(expected))
                throw new Exception("UI AI Tools host workspace initializer contract failed: " + expected);
        }
    }

    public sealed class UIAIToolsHostWorkspaceReport
    {
        public readonly string Root;
        public readonly List<string> Created = new List<string>();
        public readonly List<string> Updated = new List<string>();
        public readonly List<string> Existing = new List<string>();
        public bool NeedsRefresh;

        public bool HasChanges => Created.Count > 0 || Updated.Count > 0;

        public UIAIToolsHostWorkspaceReport(string root)
        {
            Root = root;
        }

        public string Summary
        {
            get
            {
                return "宿主工作区已就绪："
                       + Root
                       + "\n新建："
                       + Created.Count
                       + "\n修复："
                       + Updated.Count
                       + "\n已有："
                       + Existing.Count;
            }
        }
    }
}

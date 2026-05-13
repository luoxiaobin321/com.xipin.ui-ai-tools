using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Xipin.UIAITools
{
    public static class UIAIToolsTrainingLogService
    {
        public const string TrainingRoot = UIAIToolsHostWorkspaceInitializer.Root + "/Docs/Training";
        public const string HostRoot = TrainingRoot + "/Host";
        public const string PackageCandidateRoot = TrainingRoot + "/PackageCandidates";
        const string LegacyTriagePath = TrainingRoot + "/legacy-triage.md";
        const string LegacyHostPath = HostRoot + "/legacy-auto-maintained.md";
        const string LegacyPackageCandidatePath = PackageCandidateRoot + "/legacy-auto-maintained.md";
        const string PackageMaintenancePath = PackageCandidateRoot + "/package-maintenance-items.md";
        const string PackageRoot = "Packages/com.xipin.ui-ai-tools";

        static readonly string[] HostSpecificKeywords =
        {
            "UIVipcard",
            "Vipcard",
            "會員卡",
            "会员卡",
            "GameApp",
            "MotionFramework",
            "YooAsset",
            "com.xipin.lframework",
            "Assets/Scripts/GameApp",
            "Assets/Bundle",
            "正式客户端",
            "正式工程",
            "E:\\Work\\Er",
            "E:/Work/Er"
        };

        static readonly string[] PackageCandidateKeywords =
        {
            "通用机制",
            "通用规则",
            "跨项目",
            "包内通用",
            "不依赖业务",
            "com.xipin.ui-ai-tools",
            "PackageCandidates"
        };

        static readonly string[] PackageHostLeakKeywords =
        {
            "UIVipcard",
            "Vipcard",
            "會員卡",
            "会员卡",
            "GameApp",
            "MotionFramework",
            "com.xipin.lframework",
            "Assets/Scripts/GameApp",
            "正式客户端",
            "正式工程",
            "E:\\Work\\Er",
            "E:/Work/Er"
        };

        public static string RecordHost(string title, string body)
        {
            return Write(HostRoot, "Host", title, body, "宿主专项沉淀，只适用于当前项目。");
        }

        public static string RecordPackageCandidate(string title, string body)
        {
            return Write(PackageCandidateRoot, "PackageCandidate", title, body, "包内通用候选，不直接修改 Packages；后续由包维护流程提升到包仓库。");
        }

        public static string GenerateLegacyTriageReport()
        {
            Directory.CreateDirectory(TrainingRoot);
            Directory.CreateDirectory(HostRoot);
            Directory.CreateDirectory(PackageCandidateRoot);
            var entries = new List<LegacyTrainingEntry>();
            AddMarkdownEntries(entries, UIAIToolsHostWorkspaceInitializer.Root + "/Docs", "HostWorkspaceDocs", true);
            AddMarkdownEntries(entries, UIAIToolsHostWorkspaceInitializer.ReportsRoot, "HostWorkspaceReports", false);
            AddPackageMarkdownEntries(entries);

            var builder = new StringBuilder();
            builder.AppendLine("# 旧沉淀分拣清单");
            builder.AppendLine();
            builder.AppendLine("- Created At: `" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "`");
            builder.AppendLine("- Policy: 自动维护旧沉淀，不移动旧文件，不修改 `Packages/com.xipin.ui-ai-tools` 内源码、文档或文件名。");
            builder.AppendLine();
            AppendSummary(builder, entries);
            AppendGroup(builder, "宿主专项", entries.Where(item => item.Scope == "Host"));
            AppendGroup(builder, "包内通用候选", entries.Where(item => item.Scope == "PackageCandidate"));
            AppendGroup(builder, "包维护处理项", entries.Where(item => item.Scope == "PackageMixed"));
            AppendGroup(builder, "自动归入宿主观察", entries.Where(item => item.Scope == "Review"));

            File.WriteAllText(LegacyTriagePath, builder.ToString(), new UTF8Encoding(true));
            WriteScopeFile(LegacyHostPath, "宿主专项自动归档", "这些内容默认只服务当前宿主项目。", entries.Where(item => item.Scope == "Host" || item.Scope == "Review"));
            WriteScopeFile(LegacyPackageCandidatePath, "包内通用候选自动归档", "这些内容可作为后续包能力沉淀的候选来源。", entries.Where(item => item.Scope == "PackageCandidate"));
            WriteScopeFile(PackageMaintenancePath, "包维护处理项", "这些包内文档命中了宿主专项关键词，后续由包维护流程收敛，不从正式工程直接改包文件。", entries.Where(item => item.Scope == "PackageMixed"));
            AssetDatabase.Refresh();
            return LegacyTriagePath + "\n" + LegacyHostPath + "\n" + LegacyPackageCandidatePath + "\n" + PackageMaintenancePath;
        }

        public static void GenerateLegacyTriageReportBatch()
        {
            Debug.Log(GenerateLegacyTriageReport());
        }

        static string Write(string root, string scope, string title, string body, string policy)
        {
            Directory.CreateDirectory(root);
            var path = root + "/" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + "_" + SafeName(title) + ".md";
            File.WriteAllText(path, string.Join("\n", new[]
            {
                "# " + title.Trim(),
                "",
                "- Scope: `" + scope + "`",
                "- Created At: `" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "`",
                "- Policy: " + policy,
                "",
                "## Content",
                "",
                body.Trim()
            }), new UTF8Encoding(true));
            AssetDatabase.Refresh();
            return path;
        }

        static void AddPackageMarkdownEntries(List<LegacyTrainingEntry> entries)
        {
            AddMarkdownEntries(entries, PackageRoot + "/Documentation~", "PackageDocumentation", false);
            AddMarkdownEntries(entries, PackageRoot + "/Development~", "PackageDevelopment", false);
            AddMarkdownEntries(entries, PackageRoot + "/Skills~", "PackageSkill", false);
            AddFileEntry(entries, PackageRoot + "/HANDOFF.md", "PackageHandoff");
        }

        static void AddMarkdownEntries(List<LegacyTrainingEntry> entries, string root, string source, bool skipTraining)
        {
            if (!Directory.Exists(root))
                return;

            foreach (var path in Directory.GetFiles(root, "*.md", SearchOption.AllDirectories).OrderBy(item => item))
            {
                var normalized = Normalize(path);
                if (skipTraining && normalized.StartsWith(TrainingRoot + "/", StringComparison.Ordinal))
                    continue;
                AddFileEntry(entries, normalized, source);
            }
        }

        static void AddFileEntry(List<LegacyTrainingEntry> entries, string path, string source)
        {
            if (!File.Exists(path))
                return;

            var text = File.ReadAllText(path);
            var hostMatches = Matches(text, path, HostSpecificKeywords);
            var packageMatches = Matches(text, path, PackageCandidateKeywords);
            var inPackage = Normalize(path).StartsWith(PackageRoot + "/", StringComparison.Ordinal);
            var packageHostLeakMatches = inPackage ? Matches(text, path, PackageHostLeakKeywords) : new List<string>();
            var scope = inPackage
                ? packageHostLeakMatches.Count > 0 ? "PackageMixed" : packageMatches.Count > 0 ? "PackageCandidate" : "Review"
                : hostMatches.Count > 0 ? "Host" : packageMatches.Count > 0 ? "PackageCandidate" : "Review";
            var reasons = inPackage ? packageHostLeakMatches.Concat(packageMatches) : hostMatches.Concat(packageMatches);
            entries.Add(new LegacyTrainingEntry(Normalize(path), source, scope, reasons.Distinct().ToList()));
        }

        static List<string> Matches(string text, string path, string[] keywords)
        {
            return keywords.Where(keyword => text.Contains(keyword) || path.Contains(keyword)).ToList();
        }

        static string Normalize(string path)
        {
            return path.Replace('\\', '/');
        }

        static void AppendSummary(StringBuilder builder, List<LegacyTrainingEntry> entries)
        {
            builder.AppendLine("## Summary");
            builder.AppendLine();
            builder.AppendLine("| Scope | Count |");
            builder.AppendLine("| --- | ---: |");
            foreach (var group in entries.GroupBy(item => item.Scope).OrderBy(item => item.Key))
                builder.AppendLine("| `" + group.Key + "` | " + group.Count() + " |");
            builder.AppendLine();
        }

        static void AppendGroup(StringBuilder builder, string title, IEnumerable<LegacyTrainingEntry> entries)
        {
            var list = entries.OrderBy(item => item.Path).ToList();
            if (list.Count == 0)
                return;

            builder.AppendLine("## " + title);
            builder.AppendLine();
            builder.AppendLine("| 文件 | 来源 | 命中 |");
            builder.AppendLine("| --- | --- | --- |");
            foreach (var entry in list)
                builder.AppendLine("| `" + entry.Path + "` | `" + entry.Source + "` | " + string.Join(", ", entry.Reasons.Select(item => "`" + item + "`")) + " |");
            builder.AppendLine();
        }

        static void WriteScopeFile(string path, string title, string policy, IEnumerable<LegacyTrainingEntry> entries)
        {
            var builder = new StringBuilder();
            builder.AppendLine("# " + title);
            builder.AppendLine();
            builder.AppendLine("- Updated At: `" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "`");
            builder.AppendLine("- Policy: " + policy);
            builder.AppendLine();
            builder.AppendLine("| 文件 | 来源 | 命中 |");
            builder.AppendLine("| --- | --- | --- |");
            foreach (var entry in entries.OrderBy(item => item.Path))
                builder.AppendLine("| `" + entry.Path + "` | `" + entry.Source + "` | " + string.Join(", ", entry.Reasons.Select(item => "`" + item + "`")) + " |");
            File.WriteAllText(path, builder.ToString(), new UTF8Encoding(true));
        }

        static string SafeName(string value)
        {
            var name = string.IsNullOrWhiteSpace(value) ? "training-note" : value.Trim();
            foreach (var c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name.Length > 40 ? name.Substring(0, 40) : name;
        }

        sealed class LegacyTrainingEntry
        {
            public readonly string Path;
            public readonly string Source;
            public readonly string Scope;
            public readonly List<string> Reasons;

            public LegacyTrainingEntry(string path, string source, string scope, List<string> reasons)
            {
                Path = path;
                Source = source;
                Scope = scope;
                Reasons = reasons;
            }
        }
    }
}

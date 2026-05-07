using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Xipin.UIAITools
{
    public static class UIReuseSearchResultService
    {
        static readonly string[] SummarySections =
        {
            "## 输入",
            "## 匹配建议",
            "## Top 结果",
            "## 下一步"
        };

        public static List<Dictionary<string, string>> ReadRows(UIAIToolsProfile profile)
        {
            UIReportValidationService.ValidateReport(profile, UIReportFiles.ReuseSearchResults, UIReportFiles.ReuseSearchResultsHeader);
            var rows = UIReportCsv.ReadRows(profile.logRoot, UIReportFiles.ReuseSearchResults);
            foreach (var row in rows)
                ValidateRow(row);
            ValidateNoDuplicateRows(rows);
            return rows;
        }

        public static string GenerateSummary(UIAIToolsProfile profile)
        {
            return GenerateSummary(profile, "");
        }

        public static string GenerateSummary(UIAIToolsProfile profile, string queryImagePath)
        {
            var rows = ReadRows(profile);
            var query = queryImagePath.Length > 0 ? queryImagePath : rows.Select(row => row["Query"]).FirstOrDefault() ?? "";
            var path = UIReportFiles.GetPath(profile.logRoot, UIReportFiles.ReuseSearchSummary);
            var lines = new List<string>
            {
                "# UI 复用图片反查摘要",
                "",
                $"生成时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                "",
                "本文件只总结复用反查结果，不移动图片、不改 prefab、不改图集或 YooAsset 配置。",
                "",
                "## 输入",
                $"- Query Image: `{query}`",
                $"- Reuse Search CSV: `{UIReportFiles.GetPath(profile.logRoot, UIReportFiles.ReuseSearchResults)}`",
                $"- Re-run Search: `UIAssetTriageScanner.SearchReuseByImageBatch -uiQueryImage \"{query}\"`",
                ""
            };

            AddAdviceSummary(lines, rows);
            AddTopResults(lines, rows);
            AddNextSteps(lines, rows);

            Directory.CreateDirectory(profile.logRoot);
            File.WriteAllLines(path, lines, new UTF8Encoding(true));
            ValidateSummarySections(lines.ToArray());
            Debug.Log($"UI reuse search summary generated: {path}, {rows.Count} rows.");
            return path;
        }

        public static void ValidateContract()
        {
            ValidateSummarySections(SummarySections);
            ExpectSectionFailure("reuse_summary_missing_section", "UI reuse search summary is missing section: ## Top 结果", () =>
                ValidateSummarySections(SummarySections.Where(section => section != "## Top 结果").ToArray()));
            ExpectSectionFailure("reuse_summary_out_of_order", "UI reuse search summary section is out of order", () =>
                ValidateSummarySections(new[] { SummarySections[1], SummarySections[0] }.Concat(SummarySections.Skip(2)).ToArray()));

            var root = Path.Combine(Path.GetTempPath(), "UIAIToolsReuseSearchResultContract_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                var profile = ScriptableObject.CreateInstance<UIAIToolsProfile>();
                profile.logRoot = root;
                WriteCsv(profile, new[]
                {
                    Row("Assets/UIAITools/Skinning/Demo/Source/Query.png", "Assets/Art/UI/Old.png", "Old", "guid", "128", "128", "0.95", "Small", "Image", "Assets/Atlas.spriteatlasv2", "UI", "1", "1", "Owner", "Assets/Prefab/A.prefab;Assets/Prefab/B.prefab;...", "", "Reuse", "same")
                });
                ReadRows(profile);
                var summaryPath = GenerateSummary(profile, "Assets/UIAITools/Skinning/Demo/Source/Query.png");
                RequireLine(summaryPath, "- Reuse Search CSV: `" + UIReportFiles.GetPath(profile.logRoot, UIReportFiles.ReuseSearchResults) + "`");
                RequireLine(summaryPath, "- Re-run Search: `UIAssetTriageScanner.SearchReuseByImageBatch -uiQueryImage \"Assets/UIAITools/Skinning/Demo/Source/Query.png\"`");
                ExpectRowsFailure(profile, "duplicate_reuse_search_row", new[]
                {
                    Row("Assets/UIAITools/Skinning/Demo/Source/Query.png", "Assets/Art/UI/Old.png", "Old", "guid", "128", "128", "0.95", "Small", "Image", "", "UI", "1", "1", "Owner", "Assets/Prefab/A.prefab", "", "Reuse", "same"),
                    Row("Assets/UIAITools/Skinning/Demo/Source/Query.png", "Assets/Art/UI/Old.png", "Old", "guid", "128", "128", "0.95", "Small", "Image", "", "UI", "1", "1", "Owner", "Assets/Prefab/A.prefab", "", "Reuse", "same")
                }, "duplicate reuse search result row");
                ExpectRowsFailure(profile, "query_extension", new[]
                {
                    Row("Assets/UIAITools/Skinning/Demo/Source/Query.gif", "Assets/Art/UI/Old.png", "Old", "guid", "128", "128", "0.95", "Small", "Image", "", "UI", "1", "1", "Owner", "Assets/Prefab/A.prefab", "", "Reuse", "same")
                }, "Query must be png/jpg/jpeg");
                ExpectRowsFailure(profile, "result_path", new[]
                {
                    Row("Assets/UIAITools/Skinning/Demo/Source/Query.png", "Art/UI/Old.png", "Old", "guid", "128", "128", "0.95", "Small", "Image", "", "UI", "1", "1", "Owner", "Assets/Prefab/A.prefab", "", "Reuse", "same")
                }, "Path path is invalid");
                ExpectRowsFailure(profile, "result_extension", new[]
                {
                    Row("Assets/UIAITools/Skinning/Demo/Source/Query.png", "Assets/Art/UI/Old.psd", "Old", "guid", "128", "128", "0.95", "Small", "Image", "", "UI", "1", "1", "Owner", "Assets/Prefab/A.prefab", "", "Reuse", "same")
                }, "Path must be png/jpg/jpeg");
                ExpectRowsFailure(profile, "atlas_extension", new[]
                {
                    Row("Assets/UIAITools/Skinning/Demo/Source/Query.png", "Assets/Art/UI/Old.png", "Old", "guid", "128", "128", "0.95", "Small", "Image", "Assets/Atlas.png", "UI", "1", "1", "Owner", "Assets/Prefab/A.prefab", "", "Reuse", "same")
                }, "Atlas must be .spriteatlasv2");
                ExpectRowsFailure(profile, "prefab_refs_path", new[]
                {
                    Row("Assets/UIAITools/Skinning/Demo/Source/Query.png", "Assets/Art/UI/Old.png", "Old", "guid", "128", "128", "0.95", "Small", "Image", "", "UI", "1", "1", "Owner", "Assets/Prefab/A.prefab;Prefab.prefab", "", "Reuse", "same")
                }, "PrefabRefs path is invalid");
            }
            finally
            {
                Directory.Delete(root, true);
            }
            Debug.Log("UI reuse search result contract validation passed.");
        }

        static void AddAdviceSummary(List<string> lines, List<Dictionary<string, string>> rows)
        {
            lines.Add("## 匹配建议");
            if (rows.Count == 0)
            {
                lines.Add("- 无候选结果。");
                lines.Add("");
                return;
            }
            foreach (var group in rows.GroupBy(row => row["Advice"]).OrderByDescending(group => group.Count()).ThenBy(group => group.Key))
                lines.Add($"- {group.Key}：{group.Count()}");
            lines.Add("");
        }

        static void AddTopResults(List<string> lines, List<Dictionary<string, string>> rows)
        {
            lines.Add("## Top 结果");
            if (rows.Count == 0)
            {
                lines.Add("- 无候选结果。");
                lines.Add("");
                return;
            }
            lines.Add("| Score | Path | Size | Kind | Owner | Advice | Reason |");
            lines.Add("| --- | --- | --- | --- | --- | --- | --- |");
            foreach (var row in rows.OrderBy(row => float.Parse(row["Score"], CultureInfo.InvariantCulture)).ThenBy(row => row["Path"]).Take(10))
                lines.Add($"| {row["Score"]} | `{row["Path"]}` | {row["Width"]}x{row["Height"]} {row["SizeClass"]} | {Cell(row["Kind"])} | {Cell(row["AssetOwner"])} | {Cell(row["Advice"])} | {Cell(row["Reason"])} |");
            lines.Add("");
        }

        static void AddNextSteps(List<string> lines, List<Dictionary<string, string>> rows)
        {
            lines.Add("## 下一步");
            lines.Add("- 优先看 Score 最低且 Advice 为可复用或同内容重复的候选。");
            lines.Add("- 跨功能或大图候选需要人工确认归属，不从本报告自动迁移资源。");
            lines.Add("- 如果候选都不合适，重新裁更干净的缺图区域后重跑 `Re-run Search`。");
            if (rows.Any(row => row["Advice"].Contains("按名加载风险")))
                lines.Add("- 报告存在按名加载风险候选，复用前先确认脚本或配置是否依赖文件名。");
            lines.Add("");
        }

        static void ValidateSummarySections(string[] lines)
        {
            UIReportMarkdown.RequireExactSectionOrder("UI reuse search summary", lines, SummarySections);
        }

        static void ValidateRow(Dictionary<string, string> row)
        {
            if (string.IsNullOrEmpty(row["Query"]) || string.IsNullOrEmpty(row["Path"]) || string.IsNullOrEmpty(row["Name"]))
                throw new Exception("Invalid UI reuse search result row: Query, Path and Name are required");
            RequireImageExtension(row["Query"], "Query");
            RequireAssetImagePath(row["Path"], "Path");
            if (!string.IsNullOrEmpty(row["Atlas"]))
                RequireAssetPath(row["Atlas"], "Atlas", ".spriteatlasv2");
            foreach (var prefab in row["PrefabRefs"].Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (prefab != "...")
                    RequireAssetPath(prefab, "PrefabRefs", ".prefab");
            }
            if (!int.TryParse(row["Width"], out _) || !int.TryParse(row["Height"], out _))
                throw new Exception("Invalid UI reuse search result row: Width and Height must be integers");
            if (!float.TryParse(row["Score"], NumberStyles.Float, CultureInfo.InvariantCulture, out _))
                throw new Exception("Invalid UI reuse search result row: Score must be a number");
            if (!int.TryParse(row["PrefabCount"], out _) || !int.TryParse(row["OwnerCount"], out _))
                throw new Exception("Invalid UI reuse search result row: PrefabCount and OwnerCount must be integers");
            if (string.IsNullOrEmpty(row["Advice"]) || string.IsNullOrEmpty(row["Reason"]))
                throw new Exception("Invalid UI reuse search result row: Advice and Reason are required");
        }

        static void RequireAssetImagePath(string path, string label)
        {
            RequireAssetPath(path, label, "");
            RequireImageExtension(path, label);
        }

        static void RequireAssetPath(string path, string label, string extension)
        {
            if (!path.StartsWith("Assets/", StringComparison.Ordinal) || path.Contains("\\") || path.Contains("/../") || path.EndsWith("/..", StringComparison.Ordinal))
                throw new Exception($"Invalid UI reuse search result row: {label} path is invalid");
            if (!string.IsNullOrEmpty(extension) && !path.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
                throw new Exception($"Invalid UI reuse search result row: {label} must be {extension}");
        }

        static void RequireImageExtension(string path, string label)
        {
            var extension = Path.GetExtension(path).ToLowerInvariant();
            if (extension != ".png" && extension != ".jpg" && extension != ".jpeg")
                throw new Exception($"Invalid UI reuse search result row: {label} must be png/jpg/jpeg");
        }

        static void ValidateNoDuplicateRows(List<Dictionary<string, string>> rows)
        {
            var duplicate = rows.GroupBy(row => new { Query = row["Query"], Path = row["Path"] }).FirstOrDefault(group => group.Count() > 1);
            if (duplicate != null)
                throw new Exception($"Invalid UI reuse search result row: duplicate reuse search result row for {duplicate.Key.Query} {duplicate.Key.Path}");
        }

        static void WriteCsv(UIAIToolsProfile profile, IEnumerable<string> rows)
        {
            var path = UIReportFiles.GetPath(profile.logRoot, UIReportFiles.ReuseSearchResults);
            File.WriteAllLines(path, new[] { UIReportFiles.ReuseSearchResultsHeader }.Concat(rows), new UTF8Encoding(true));
        }

        static string Row(string query, string path, string name, string guid, string width, string height, string score, string sizeClass, string kind, string atlas, string assetOwner, string prefabCount, string ownerCount, string owners, string prefabRefs, string textRefs, string advice, string reason)
        {
            return string.Join(",", new[] { query, path, name, guid, width, height, score, sizeClass, kind, atlas, assetOwner, prefabCount, ownerCount, owners, prefabRefs, textRefs, advice, reason }.Select(Csv));
        }

        static void ExpectRowsFailure(UIAIToolsProfile profile, string name, IEnumerable<string> rows, string expectedMessage)
        {
            WriteCsv(profile, rows);
            try
            {
                ReadRows(profile);
            }
            catch (Exception exception)
            {
                if (exception.Message.Contains(expectedMessage))
                    return;
                throw new Exception($"Unexpected UI reuse search result contract failure for {name}: {exception.Message}");
            }
            throw new Exception("UI reuse search result contract sample did not fail: " + name);
        }

        static void ExpectSectionFailure(string name, string expectedMessage, Action validate)
        {
            try
            {
                validate();
            }
            catch (Exception exception)
            {
                if (exception.Message.Contains(expectedMessage))
                    return;
                throw new Exception($"Unexpected UI reuse search summary contract failure for {name}: {exception.Message}");
            }
            throw new Exception("UI reuse search summary contract sample did not fail: " + name);
        }

        static void RequireLine(string path, string expected)
        {
            if (!File.ReadAllLines(path).Contains(expected))
                throw new Exception("UI reuse search summary is missing: " + expected);
        }

        static string Csv(string value)
        {
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        static string Cell(string value)
        {
            return value.Replace("|", "\\|");
        }
    }
}

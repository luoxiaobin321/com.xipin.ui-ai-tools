using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Xipin.UIAITools
{
    public static class UICreationHostGenerateResultService
    {
        static readonly HashSet<string> AllowedStatuses = new HashSet<string> { "Applied", "Skipped", "Failed", "Verified" };

        public static List<Dictionary<string, string>> ReadRows(UIAIToolsProfile profile)
        {
            UIReportValidationService.ValidateReport(profile, UIReportFiles.CreationHostGenerateResult, UIReportFiles.CreationHostGenerateResultHeader);
            var rows = UIReportCsv.ReadRows(profile.logRoot, UIReportFiles.CreationHostGenerateResult);
            if (rows.Count == 0)
                throw new Exception("Invalid UI creation host generate result: result rows are required");
            foreach (var row in rows)
                ValidateRow(row);
            TargetPrefab(rows);
            return rows;
        }

        public static void Validate(UIAIToolsProfile profile)
        {
            var rows = ReadRows(profile);
            ValidateSummary(profile, rows);
            Debug.Log($"UI creation host generate result validation passed: {rows.Count} rows.");
        }

        public static string GenerateSummary(UIAIToolsProfile profile)
        {
            return GenerateSummary(profile, "", -1);
        }

        public static string GenerateSummary(UIAIToolsProfile profile, string uiName, int nodeCount)
        {
            var rows = ReadRows(profile);
            var targetPrefab = TargetPrefab(rows);
            var path = UIReportFiles.GetPath(profile.logRoot, UIReportFiles.CreationHostGenerateResultSummary);
            var lines = new List<string>
            {
                "# UI 生成宿主结果",
                "",
                $"生成时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                "",
                "本文件记录宿主侧 prefab 草稿生成结果，不移动图片、不修改图集或 YooAsset 配置。",
                "",
                "## 目标"
            };
            if (!string.IsNullOrEmpty(uiName))
                lines.Add($"- UI：`{uiName}`");
            lines.Add($"- prefab：`{targetPrefab}`");
            if (nodeCount >= 0)
                lines.Add($"- 节点：{nodeCount}");
            lines.Add($"- CSV：`{UIReportFiles.GetPath(profile.logRoot, UIReportFiles.CreationHostGenerateResult)}`");
            lines.Add("");
            lines.Add("## 状态分布");
            foreach (var group in rows.GroupBy(r => r["Status"]).OrderBy(g => g.Key))
                lines.Add($"- {group.Key}：{group.Count()}");
            lines.Add("");
            lines.Add("## 下一步");
            lines.Add("- 打开生成 prefab 做视觉、交互和运行时数据绑定人工检查。");
            lines.Add("- 重新运行 UI 扫描或面板实测清单，确认资源引用和 DrawCall 风险。");
            File.WriteAllLines(path, lines, new UTF8Encoding(true));
            ValidateSummary(profile, rows);
            Debug.Log($"UI creation host generate result summary generated: {path}, {rows.Count} rows.");
            return path;
        }

        public static void ValidateContract()
        {
            var root = Path.Combine(Path.GetTempPath(), "UIAIToolsCreationHostGenerateResultContract_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                var profile = ScriptableObject.CreateInstance<UIAIToolsProfile>();
                profile.logRoot = root;
                WriteCsv(profile, new[]
                {
                    Row("0", "CreatePrefab", "Applied", "", "", "Assets/Art/UI/AI/Demo/Demo.prefab", "", "", "QA-1", "created"),
                    Row("1", "ApplyLayout", "Applied", "Root", "builtin:Panel", "Assets/Art/UI/AI/Demo/Demo.prefab", "", "", "QA-1", "layout"),
                    Row("2", "ApplyText", "Skipped", "Title", "builtin:Text", "Assets/Art/UI/AI/Demo/Demo.prefab", "", "", "", "empty"),
                    Row("3", "VerifyAfterGenerate", "Verified", "", "", "Assets/Art/UI/AI/Demo/Demo.prefab", "", "", "QA-1", "verified")
                });
                GenerateSummary(profile, "Demo", 2);
                Validate(profile);
                ExpectSummaryFailure(profile, "bad_title", "# Bad", "unexpected title");
                ExpectFailure(profile, "empty_rows", null, "result rows are required");
                ExpectFailure(profile, "bad_item_index", Row("x", "CreatePrefab", "Applied", "", "", "Assets/Demo.prefab", "", "", "QA-1", "bad"), "ItemIndex must be an integer");
                ExpectFailure(profile, "missing_action", Row("0", "", "Applied", "", "", "Assets/Demo.prefab", "", "", "QA-1", "bad"), "Action is required");
                ExpectFailure(profile, "bad_status", Row("0", "CreatePrefab", "Done", "", "", "Assets/Demo.prefab", "", "", "QA-1", "bad"), "invalid status");
                ExpectFailure(profile, "missing_target_prefab", Row("0", "CreatePrefab", "Applied", "", "", "", "", "", "QA-1", "bad"), "TargetPrefab is required");
                ExpectRowsFailure(profile, "inconsistent_target_prefab", new[]
                {
                    Row("0", "CreatePrefab", "Applied", "", "", "Assets/Demo.prefab", "", "", "QA-1", "created"),
                    Row("1", "VerifyAfterGenerate", "Verified", "", "", "Assets/Other.prefab", "", "", "QA-1", "verified")
                }, "TargetPrefab must be consistent");
                ExpectFailure(profile, "missing_confirmation", Row("0", "CreatePrefab", "Applied", "", "", "Assets/Demo.prefab", "", "", "", "bad"), "confirmation is required");
                ExpectFailure(profile, "missing_failed_message", Row("0", "CreatePrefab", "Failed", "", "", "Assets/Demo.prefab", "", "", "QA-1", ""), "failed message is required");
            }
            finally
            {
                if (Directory.Exists(root))
                    Directory.Delete(root, true);
            }
            Debug.Log("UI creation host generate result contract validation passed.");
        }

        static void ValidateRow(Dictionary<string, string> row)
        {
            if (!int.TryParse(row["ItemIndex"], out _))
                throw new Exception("Invalid UI creation host generate result: ItemIndex must be an integer");
            if (string.IsNullOrEmpty(row["Action"]))
                throw new Exception("Invalid UI creation host generate result: Action is required");
            if (!AllowedStatuses.Contains(row["Status"]))
                throw new Exception("Invalid UI creation host generate result: invalid status " + row["Status"]);
            if (string.IsNullOrEmpty(row["TargetPrefab"]))
                throw new Exception("Invalid UI creation host generate result: TargetPrefab is required");
            if ((row["Status"] == "Applied" || row["Status"] == "Verified") && string.IsNullOrEmpty(row["Confirmation"]))
                throw new Exception("Invalid UI creation host generate result: confirmation is required");
            if (row["Status"] == "Failed" && string.IsNullOrEmpty(row["Message"]))
                throw new Exception("Invalid UI creation host generate result: failed message is required");
        }

        static string TargetPrefab(List<Dictionary<string, string>> rows)
        {
            var targetPrefab = rows.First()["TargetPrefab"];
            if (rows.Any(r => r["TargetPrefab"] != targetPrefab))
                throw new Exception("Invalid UI creation host generate result: TargetPrefab must be consistent");
            return targetPrefab;
        }

        static void ValidateSummary(UIAIToolsProfile profile, List<Dictionary<string, string>> rows)
        {
            var path = UIReportFiles.GetPath(profile.logRoot, UIReportFiles.CreationHostGenerateResultSummary);
            if (!File.Exists(path))
                throw new Exception("Missing UI creation host generate result summary: " + path);
            var lines = File.ReadAllLines(path);
            RequireTitle(lines, "# UI 生成宿主结果");
            UIReportMarkdown.RequireExactSectionOrder("UI creation host generate result", lines, "## 目标", "## 状态分布", "## 下一步");
            RequireLine(lines, "本文件记录宿主侧 prefab 草稿生成结果，不移动图片、不修改图集或 YooAsset 配置。");
            RequireLine(lines, $"- prefab：`{TargetPrefab(rows)}`");
            RequireLine(lines, $"- CSV：`{UIReportFiles.GetPath(profile.logRoot, UIReportFiles.CreationHostGenerateResult)}`");
            foreach (var group in rows.GroupBy(r => r["Status"]).OrderBy(g => g.Key))
                RequireLine(lines, $"- {group.Key}：{group.Count()}");
        }

        static void RequireLine(string[] lines, string line)
        {
            if (!lines.Contains(line))
                throw new Exception("UI creation host generate result summary is missing: " + line);
        }

        static void RequireTitle(string[] lines, string title)
        {
            if (lines.Length == 0 || lines[0] != title)
                throw new Exception("UI creation host generate result summary has unexpected title");
        }

        static void WriteCsv(UIAIToolsProfile profile, IEnumerable<string> rows)
        {
            var path = UIReportFiles.GetPath(profile.logRoot, UIReportFiles.CreationHostGenerateResult);
            File.WriteAllLines(path, new[] { UIReportFiles.CreationHostGenerateResultHeader }.Concat(rows), new UTF8Encoding(true));
        }

        static string Row(string itemIndex, string action, string status, string nodeId, string componentId, string targetPrefab, string assetPath, string binding, string confirmation, string message)
        {
            return string.Join(",", new[] { itemIndex, action, status, nodeId, componentId, targetPrefab, assetPath, binding, confirmation, message }.Select(Csv));
        }

        static string Csv(string value)
        {
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        static void ExpectFailure(UIAIToolsProfile profile, string name, string row, string expectedMessage)
        {
            ExpectRowsFailure(profile, name, row == null ? new string[0] : new[] { row }, expectedMessage);
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
                throw new Exception($"Unexpected UI creation host generate result contract failure for {name}: {exception.Message}");
            }
            throw new Exception("UI creation host generate result contract sample did not fail: " + name);
        }

        static void ExpectSummaryFailure(UIAIToolsProfile profile, string name, string title, string expectedMessage)
        {
            var path = UIReportFiles.GetPath(profile.logRoot, UIReportFiles.CreationHostGenerateResultSummary);
            var original = File.ReadAllLines(path);
            var changed = original.ToArray();
            changed[0] = title;
            File.WriteAllLines(path, changed, new UTF8Encoding(true));
            try
            {
                Validate(profile);
            }
            catch (Exception exception)
            {
                File.WriteAllLines(path, original, new UTF8Encoding(true));
                if (exception.Message.Contains(expectedMessage))
                    return;
                throw new Exception($"Unexpected UI creation host generate result summary contract failure for {name}: {exception.Message}");
            }
            File.WriteAllLines(path, original, new UTF8Encoding(true));
            throw new Exception("UI creation host generate result summary contract sample did not fail: " + name);
        }
    }
}

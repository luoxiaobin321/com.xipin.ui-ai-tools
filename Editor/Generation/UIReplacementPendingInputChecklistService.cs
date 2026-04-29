using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Xipin.UIAITools
{
    public static class UIReplacementPendingInputChecklistService
    {
        public static string Generate(UIAIToolsProfile profile)
        {
            UIReportValidationService.ValidateReport(profile, UIReportFiles.ReplacementExecutionPlan, UIReportFiles.ReplacementExecutionPlanHeader);
            var planRows = UIReportCsv.ReadRows(profile.logRoot, UIReportFiles.ReplacementExecutionPlan);
            var rows = PendingRows(planRows);
            var lines = new List<string> { UIReportFiles.ReplacementPendingInputsHeader };
            foreach (var row in rows)
                lines.Add(string.Join(",", UIReportFiles.ReplacementPendingInputsHeader.Split(',').Select(column => Csv(row[column]))));
            var path = UIReportFiles.GetPath(profile.logRoot, UIReportFiles.ReplacementPendingInputs);
            Directory.CreateDirectory(profile.logRoot);
            File.WriteAllLines(path, lines, new UTF8Encoding(true));
            UIReportValidationService.ValidateReport(profile, UIReportFiles.ReplacementPendingInputs, UIReportFiles.ReplacementPendingInputsHeader);
            var summary = GenerateSummary(profile, path, rows);
            Debug.Log($"UI replacement pending input checklist generated: {path}, summary: {summary}, {rows.Count} rows.");
            return path;
        }

        public static void Validate(UIAIToolsProfile profile)
        {
            UIReportValidationService.ValidateReport(profile, UIReportFiles.ReplacementExecutionPlan, UIReportFiles.ReplacementExecutionPlanHeader);
            UIReportValidationService.ValidateReport(profile, UIReportFiles.ReplacementPendingInputs, UIReportFiles.ReplacementPendingInputsHeader);
            var rows = UIReportCsv.ReadRows(profile.logRoot, UIReportFiles.ReplacementPendingInputs);
            var expectedRows = PendingRows(UIReportCsv.ReadRows(profile.logRoot, UIReportFiles.ReplacementExecutionPlan));
            if (rows.Count != expectedRows.Count)
                throw new Exception($"UI replacement pending input checklist row count mismatch: {rows.Count}->{expectedRows.Count}");
            foreach (var expected in expectedRows)
                RequireRow(rows, expected);
            foreach (var row in rows)
                ValidatePathContract(row);
            ValidateSummary(profile, rows);
            Debug.Log($"UI replacement pending input checklist validation passed: {rows.Count} rows.");
        }

        static string GenerateSummary(UIAIToolsProfile profile, string csvPath, List<Dictionary<string, string>> rows)
        {
            var path = UIReportFiles.GetPath(profile.logRoot, UIReportFiles.ReplacementPendingInputsSummary);
            var lines = new List<string>
            {
                "# UI 替换待补输入",
                "",
                $"生成时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                "",
                "本文件聚焦新版预览、新图和目标图集待补输入；只写报告，不生成图片、不创建图集。",
                "",
                "## 总览",
                $"- CSV：`{csvPath}`",
                $"- 待补总数：{rows.Count}"
            };
            foreach (var group in rows.GroupBy(r => r["InputKind"]).OrderBy(g => g.Key))
                lines.Add($"- {group.Key}：{group.Count()}");
            lines.Add("");
            AddRows(lines, "新版预览", rows.Where(r => r["InputKind"] == "Preview").ToList());
            AddRows(lines, "新图", rows.Where(r => r["InputKind"] == "NewAsset").ToList());
            AddRows(lines, "目标图集", rows.Where(r => r["InputKind"] == "TargetAtlas").ToList());
            File.WriteAllLines(path, lines, new UTF8Encoding(true));
            ValidateSummarySections(lines.ToArray());
            return path;
        }

        static void AddRows(List<string> lines, string title, List<Dictionary<string, string>> rows)
        {
            lines.Add("## " + title);
            if (rows.Count == 0)
            {
                lines.Add("- 无");
                lines.Add("");
                return;
            }
            foreach (var row in rows.Take(30))
                lines.Add(RowSummary(row));
            if (rows.Count > 30)
                lines.Add(RowLimitSummary(rows.Count));
            lines.Add("");
        }

        static void ValidateSummary(UIAIToolsProfile profile, List<Dictionary<string, string>> rows)
        {
            var path = UIReportFiles.GetPath(profile.logRoot, UIReportFiles.ReplacementPendingInputsSummary);
            if (!File.Exists(path))
                throw new Exception("Missing UI replacement pending input summary: " + path);
            var lines = File.ReadAllLines(path);
            ValidateSummarySections(lines);
            RequireLine(lines, $"- CSV：`{UIReportFiles.GetPath(profile.logRoot, UIReportFiles.ReplacementPendingInputs)}`");
            RequireLine(lines, $"- 待补总数：{rows.Count}");
            foreach (var group in rows.GroupBy(r => r["InputKind"]).OrderBy(g => g.Key))
                RequireLine(lines, $"- {group.Key}：{group.Count()}");
            ValidateSummaryRows(lines, "新版预览", rows.Where(r => r["InputKind"] == "Preview").ToList());
            ValidateSummaryRows(lines, "新图", rows.Where(r => r["InputKind"] == "NewAsset").ToList());
            ValidateSummaryRows(lines, "目标图集", rows.Where(r => r["InputKind"] == "TargetAtlas").ToList());
        }

        static List<Dictionary<string, string>> PendingRows(List<Dictionary<string, string>> planRows)
        {
            var rows = new List<Dictionary<string, string>>();
            foreach (var row in planRows.Where(r => r["Status"] == "PendingPreview"))
                rows.Add(Row("Preview", row["Status"], row["NewAsset"], "", "", row["ItemIndex"], "1", row["Action"], row["Note"]));
            foreach (var row in planRows.Where(r => r["Action"] == "ConfirmNewAsset" && r["Status"] == "PendingAsset").OrderBy(r => int.Parse(r["ItemIndex"])))
                rows.Add(Row("NewAsset", row["Status"], row["NewAsset"], row["OldAsset"], row["TargetAtlas"], row["ItemIndex"], "1", row["Action"], row["Note"]));
            foreach (var group in planRows.Where(r => r["Action"] == "ConfirmTargetAtlas" && r["Status"] == "PendingAtlas").GroupBy(r => r["TargetAtlas"]).OrderBy(g => g.Key))
                rows.Add(Row("TargetAtlas", "PendingAtlas", group.Key, "", group.Key, string.Join(";", group.Select(r => r["ItemIndex"]).OrderBy(i => int.Parse(i))), group.Count().ToString(), "ConfirmTargetAtlas", "目标图集缺失或待确认"));
            return rows;
        }

        static Dictionary<string, string> Row(string kind, string status, string path, string referencePath, string targetAtlas, string itemIndices, string count, string action, string note)
        {
            return new Dictionary<string, string>
            {
                { "InputKind", kind },
                { "Status", status },
                { "Path", path },
                { "ReferencePath", referencePath },
                { "TargetAtlas", targetAtlas },
                { "ItemIndices", itemIndices },
                { "Count", count },
                { "SourceAction", action },
                { "Note", note }
            };
        }

        static void RequireRow(List<Dictionary<string, string>> rows, Dictionary<string, string> expected)
        {
            var columns = UIReportFiles.ReplacementPendingInputsHeader.Split(',');
            if (!rows.Any(row => columns.All(column => row[column] == expected[column])))
                throw new Exception($"UI replacement pending input checklist is missing: {expected["InputKind"]} {expected["Path"]}");
        }

        static void RequireLine(string[] lines, string line)
        {
            if (!lines.Contains(line))
                throw new Exception("UI replacement pending input summary is missing: " + line);
        }

        static void ValidateSummaryRows(string[] lines, string title, List<Dictionary<string, string>> rows)
        {
            RequireLine(lines, "## " + title);
            if (rows.Count == 0)
            {
                RequireLine(lines, "- 无");
                return;
            }
            foreach (var row in rows.Take(30))
                RequireLine(lines, RowSummary(row));
            if (rows.Count > 30)
                RequireLine(lines, RowLimitSummary(rows.Count));
        }

        static void ValidateSummarySections(string[] lines)
        {
            UIReportMarkdown.RequireExactSectionOrder("UI replacement pending input summary", lines, "## 总览", "## 新版预览", "## 新图", "## 目标图集");
        }

        static string RowSummary(Dictionary<string, string> row)
        {
            return $"- `{row["Path"]}`：{row["Status"]}，参考 `{row["ReferencePath"]}`，图集 `{row["TargetAtlas"]}`，Item `{row["ItemIndices"]}`，数量 {row["Count"]}";
        }

        static string RowLimitSummary(int count)
        {
            return $"- 仅显示前 30 项，共 {count} 项。";
        }

        static void ValidatePathContract(Dictionary<string, string> row)
        {
            RequireAssetPath(row["Path"], row["InputKind"]);
            if (row["InputKind"] == "Preview")
                RequireExtension(row["Path"], ".png", row["InputKind"]);
            if (row["InputKind"] == "NewAsset")
            {
                RequireExtension(row["Path"], ".png", row["InputKind"]);
                RequireAssetPath(row["ReferencePath"], "ReferencePath");
                RequireAssetPath(row["TargetAtlas"], "TargetAtlas");
                RequireExtension(row["TargetAtlas"], ".spriteatlasv2", "TargetAtlas");
            }
            if (row["InputKind"] == "TargetAtlas")
                RequireExtension(row["Path"], ".spriteatlasv2", row["InputKind"]);
            if (int.Parse(row["Count"]) <= 0)
                throw new Exception("UI replacement pending input checklist count must be positive: " + row["Path"]);
            foreach (var itemIndex in row["ItemIndices"].Split(';'))
                int.Parse(itemIndex);
        }

        static void RequireAssetPath(string path, string label)
        {
            if (!path.StartsWith("Assets/", StringComparison.Ordinal) || path.Contains("\\") || path.Contains("/../") || path.EndsWith("/..", StringComparison.Ordinal))
                throw new Exception($"UI replacement pending input checklist {label} path is invalid: {path}");
        }

        static void RequireExtension(string path, string extension, string label)
        {
            if (!path.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
                throw new Exception($"UI replacement pending input checklist {label} must be {extension}: {path}");
        }

        static string Csv(string value)
        {
            return "\"" + (value ?? "").Replace("\"", "\"\"") + "\"";
        }
    }
}

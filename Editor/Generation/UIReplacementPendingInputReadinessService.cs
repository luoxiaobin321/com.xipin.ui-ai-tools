using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Xipin.UIAITools
{
    public static class UIReplacementPendingInputReadinessService
    {
        public static string Generate(UIAIToolsProfile profile)
        {
            UIReplacementPendingInputChecklistService.Validate(profile);
            var rows = ReadinessRows(UIReportCsv.ReadRows(profile.logRoot, UIReportFiles.ReplacementPendingInputs));
            var columns = UIReportFiles.ReplacementPendingInputReadinessHeader.Split(',');
            var lines = new List<string> { UIReportFiles.ReplacementPendingInputReadinessHeader };
            foreach (var row in rows)
                lines.Add(string.Join(",", columns.Select(column => Csv(row[column]))));
            var path = UIReportFiles.GetPath(profile.logRoot, UIReportFiles.ReplacementPendingInputReadiness);
            File.WriteAllLines(path, lines, new UTF8Encoding(true));
            UIReportValidationService.ValidateReport(profile, UIReportFiles.ReplacementPendingInputReadiness, UIReportFiles.ReplacementPendingInputReadinessHeader);
            var summary = GenerateSummary(profile, path, rows);
            Debug.Log($"UI replacement pending input readiness generated: {path}, summary: {summary}, {MissingRows(rows).Count} missing inputs.");
            return path;
        }

        public static void Validate(UIAIToolsProfile profile)
        {
            UIReplacementPendingInputChecklistService.Validate(profile);
            UIReportValidationService.ValidateReport(profile, UIReportFiles.ReplacementPendingInputReadiness, UIReportFiles.ReplacementPendingInputReadinessHeader);
            var rows = UIReportCsv.ReadRows(profile.logRoot, UIReportFiles.ReplacementPendingInputReadiness);
            var expectedRows = ReadinessRows(UIReportCsv.ReadRows(profile.logRoot, UIReportFiles.ReplacementPendingInputs));
            if (rows.Count != expectedRows.Count)
                throw new Exception($"UI replacement pending input readiness row count mismatch: {rows.Count}->{expectedRows.Count}");
            foreach (var expected in expectedRows)
                RequireRow(rows, expected);
            ValidateSummary(profile, rows);
            Debug.Log($"UI replacement pending input readiness validation passed: {rows.Count} rows, {MissingRows(rows).Count} missing inputs.");
        }

        public static void ValidateNoMissing(UIAIToolsProfile profile)
        {
            var path = Generate(profile);
            var rows = UIReportCsv.ReadRows(profile.logRoot, UIReportFiles.ReplacementPendingInputReadiness);
            var notReady = NotReadyRows(rows);
            if (notReady.Count > 0)
            {
                var missing = MissingRows(rows);
                var invalid = InvalidRows(rows);
                if (invalid.Count == 0)
                    throw new Exception($"UI replacement pending input readiness has {missing.Count} missing inputs. See {path}");
                throw new Exception($"UI replacement pending input readiness has {notReady.Count} not ready inputs: {missing.Count} missing, {invalid.Count} invalid. See {path}");
            }
            Debug.Log($"UI replacement pending input readiness gate passed: {rows.Count} inputs ready.");
        }

        static List<Dictionary<string, string>> ReadinessRows(List<Dictionary<string, string>> pendingRows)
        {
            return pendingRows.Select(row =>
            {
                var info = ReadinessInfo(row);
                return new Dictionary<string, string>
                {
                    { "InputKind", row["InputKind"] },
                    { "PendingStatus", row["Status"] },
                    { "Readiness", info[0] },
                    { "Path", row["Path"] },
                    { "ActualWidth", info[1] },
                    { "ActualHeight", info[2] },
                    { "ReferencePath", row["ReferencePath"] },
                    { "TargetAtlas", row["TargetAtlas"] },
                    { "ItemIndices", row["ItemIndices"] },
                    { "Count", row["Count"] },
                    { "SourceAction", row["SourceAction"] },
                    { "Note", row["Note"] }
                };
            }).ToList();
        }

        static string GenerateSummary(UIAIToolsProfile profile, string csvPath, List<Dictionary<string, string>> rows)
        {
            var path = UIReportFiles.GetPath(profile.logRoot, UIReportFiles.ReplacementPendingInputReadinessSummary);
            var missing = MissingRows(rows);
            var invalid = InvalidRows(rows);
            var lines = new List<string>
            {
                "# UI 替换待补输入就绪检查",
                "",
                $"生成时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                "",
                "本文件只检查待补输入的当前路径状态和 PNG 可读性，不生成图片、不创建图集、不导入资源。",
                "",
                "## Gate 状态",
                $"- CSV：`{csvPath}`",
                $"- 待补总数：{rows.Count}",
                $"- 已就绪：{ReadyRows(rows).Count}",
                $"- 仍缺失：{missing.Count}",
                $"- 格式异常：{invalid.Count}",
                $"- 待补输入 Gate：{(NotReadyRows(rows).Count == 0 ? "通过" : "阻断")}",
                ""
            };
            AddReadinessSummary(lines, rows);
            AddAcceptanceChecklist(lines, rows);
            AddRows(lines, "缺失项", missing);
            AddRows(lines, "格式异常项", invalid);
            AddRows(lines, "已就绪项", rows.Where(r => r["Readiness"] == "Ready").ToList());
            File.WriteAllLines(path, lines, new UTF8Encoding(true));
            ValidateSummarySections(lines.ToArray());
            return path;
        }

        static void AddReadinessSummary(List<string> lines, List<Dictionary<string, string>> rows)
        {
            lines.Add("## 类型分布");
            if (rows.Count == 0)
            {
                lines.Add("- 无");
                lines.Add("");
                return;
            }
            foreach (var group in rows.GroupBy(r => new { Kind = r["InputKind"], Readiness = r["Readiness"] })
                         .OrderBy(g => g.Key.Kind)
                         .ThenBy(g => g.Key.Readiness))
                lines.Add($"- {group.Key.Kind} / {group.Key.Readiness}：{group.Count()}");
            lines.Add("");
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
                lines.Add($"- 仅显示前 30 项，共 {rows.Count} 项。");
            lines.Add("");
        }

        static void ValidateSummary(UIAIToolsProfile profile, List<Dictionary<string, string>> rows)
        {
            var path = UIReportFiles.GetPath(profile.logRoot, UIReportFiles.ReplacementPendingInputReadinessSummary);
            if (!File.Exists(path))
                throw new Exception("Missing UI replacement pending input readiness summary: " + path);
            var lines = File.ReadAllLines(path);
            var missing = MissingRows(rows);
            var invalid = InvalidRows(rows);
            ValidateSummarySections(lines);
            RequireLine(lines, "本文件只检查待补输入的当前路径状态和 PNG 可读性，不生成图片、不创建图集、不导入资源。");
            RequireLine(lines, $"- CSV：`{UIReportFiles.GetPath(profile.logRoot, UIReportFiles.ReplacementPendingInputReadiness)}`");
            RequireLine(lines, $"- 待补总数：{rows.Count}");
            RequireLine(lines, $"- 已就绪：{ReadyRows(rows).Count}");
            RequireLine(lines, $"- 仍缺失：{missing.Count}");
            RequireLine(lines, $"- 格式异常：{invalid.Count}");
            RequireLine(lines, $"- 待补输入 Gate：{(NotReadyRows(rows).Count == 0 ? "通过" : "阻断")}");
            foreach (var group in rows.GroupBy(r => new { Kind = r["InputKind"], Readiness = r["Readiness"] }).OrderBy(g => g.Key.Kind).ThenBy(g => g.Key.Readiness))
                RequireLine(lines, $"- {group.Key.Kind} / {group.Key.Readiness}：{group.Count()}");
            ValidateAcceptanceChecklist(lines, rows);
            ValidateSummaryRows(lines, "缺失项", missing);
            ValidateSummaryRows(lines, "格式异常项", invalid);
            ValidateSummaryRows(lines, "已就绪项", rows.Where(r => r["Readiness"] == "Ready").ToList());
        }

        static void AddAcceptanceChecklist(List<string> lines, List<Dictionary<string, string>> rows)
        {
            lines.Add("## 外部落位验收");
            AddAcceptanceLine(lines, rows, "Preview", "验收要求：新版预览 PNG 可解码，尺寸和视觉效果需人工确认。");
            AddAcceptanceLine(lines, rows, "NewAsset", "验收要求：替换 PNG 可解码，尺寸、透明通道、命名和目标图集归属需人工确认。");
            AddAcceptanceLine(lines, rows, "TargetAtlas", "验收要求：目标 SpriteAtlas 路径存在，并由宿主确认纳入对应替换图。");
            lines.Add("");
        }

        static void ValidateAcceptanceChecklist(string[] lines, List<Dictionary<string, string>> rows)
        {
            RequireLine(lines, "## 外部落位验收");
            RequireLine(lines, AcceptanceLine(rows, "Preview", "验收要求：新版预览 PNG 可解码，尺寸和视觉效果需人工确认。"));
            RequireLine(lines, AcceptanceLine(rows, "NewAsset", "验收要求：替换 PNG 可解码，尺寸、透明通道、命名和目标图集归属需人工确认。"));
            RequireLine(lines, AcceptanceLine(rows, "TargetAtlas", "验收要求：目标 SpriteAtlas 路径存在，并由宿主确认纳入对应替换图。"));
        }

        static void AddAcceptanceLine(List<string> lines, List<Dictionary<string, string>> rows, string kind, string text)
        {
            lines.Add(AcceptanceLine(rows, kind, text));
        }

        static string AcceptanceLine(List<Dictionary<string, string>> rows, string kind, string text)
        {
            var count = rows.Count(r => r["InputKind"] == kind);
            var missing = rows.Count(r => r["InputKind"] == kind && r["Readiness"] == "Missing");
            var invalid = rows.Count(r => r["InputKind"] == kind && r["Readiness"] == "Invalid");
            return $"- {kind}：{count - missing - invalid}/{count} Ready，{missing} Missing，{invalid} Invalid；{text}";
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
                RequireLine(lines, $"- 仅显示前 30 项，共 {rows.Count} 项。");
        }

        static void ValidateSummarySections(string[] lines)
        {
            UIReportMarkdown.RequireExactSectionOrder("UI replacement pending input readiness summary", lines, "## Gate 状态", "## 类型分布", "## 外部落位验收", "## 缺失项", "## 格式异常项", "## 已就绪项");
        }

        static List<Dictionary<string, string>> MissingRows(List<Dictionary<string, string>> rows)
        {
            return rows.Where(r => r["Readiness"] == "Missing").ToList();
        }

        static List<Dictionary<string, string>> InvalidRows(List<Dictionary<string, string>> rows)
        {
            return rows.Where(r => r["Readiness"] == "Invalid").ToList();
        }

        static List<Dictionary<string, string>> ReadyRows(List<Dictionary<string, string>> rows)
        {
            return rows.Where(r => r["Readiness"] == "Ready").ToList();
        }

        static List<Dictionary<string, string>> NotReadyRows(List<Dictionary<string, string>> rows)
        {
            return rows.Where(r => r["Readiness"] != "Ready").ToList();
        }

        static string[] ReadinessInfo(Dictionary<string, string> row)
        {
            if (!File.Exists(row["Path"]))
                return new[] { "Missing", "", "" };
            if (row["InputKind"] != "Preview" && row["InputKind"] != "NewAsset")
                return new[] { "Ready", "", "" };
            var texture = new Texture2D(2, 2);
            var readable = texture.LoadImage(File.ReadAllBytes(row["Path"]));
            var info = readable
                ? new[] { "Ready", texture.width.ToString(), texture.height.ToString() }
                : new[] { "Invalid", "", "" };
            UnityEngine.Object.DestroyImmediate(texture);
            return info;
        }

        static void RequireRow(List<Dictionary<string, string>> rows, Dictionary<string, string> expected)
        {
            var columns = UIReportFiles.ReplacementPendingInputReadinessHeader.Split(',');
            if (!rows.Any(row => columns.All(column => row[column] == expected[column])))
                throw new Exception($"UI replacement pending input readiness is missing: {expected["InputKind"]} {expected["Path"]}");
        }

        static void RequireLine(string[] lines, string line)
        {
            if (!lines.Contains(line))
                throw new Exception("UI replacement pending input readiness summary is missing: " + line);
        }

        static string RowSummary(Dictionary<string, string> row)
        {
            var actualSize = string.IsNullOrEmpty(row["ActualWidth"]) || string.IsNullOrEmpty(row["ActualHeight"]) ? "Unknown" : $"{row["ActualWidth"]}x{row["ActualHeight"]}";
            return $"- `{row["Path"]}`：{row["Readiness"]}，实际尺寸 {actualSize}，原状态 {row["PendingStatus"]}，参考 `{row["ReferencePath"]}`，图集 `{row["TargetAtlas"]}`，Item `{row["ItemIndices"]}`，数量 {row["Count"]}";
        }

        static string Csv(string value)
        {
            return "\"" + (value ?? "").Replace("\"", "\"\"") + "\"";
        }
    }
}

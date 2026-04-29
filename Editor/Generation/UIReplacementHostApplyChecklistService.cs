using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Xipin.UIAITools
{
    public static class UIReplacementHostApplyChecklistService
    {
        public static string Generate(UIAIToolsProfile profile)
        {
            UIReportValidationService.ValidateReport(profile, UIReportFiles.ReplacementExecutionPlan, UIReportFiles.ReplacementExecutionPlanHeader);
            var rows = UIReportCsv.ReadRows(profile.logRoot, UIReportFiles.ReplacementExecutionPlan);
            var path = UIReportFiles.GetPath(profile.logRoot, UIReportFiles.ReplacementHostApplyChecklist);
            var blockers = rows.Where(IsBlocking).ToList();
            var applyRows = rows.Where(r => r["Action"] == "ApplyPrefabReference").ToList();
            var reviewRows = rows.Where(r => r["Status"] == "NeedsReview").ToList();
            var lines = new List<string>
            {
                "# UI 替换宿主执行清单",
                "",
                $"生成时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                "",
                "本文件只把待确认执行计划整理成宿主执行前清单，不移动资源、不覆盖 prefab、不修改图集或 YooAsset 配置。",
                "",
                "## Gate 状态",
                $"- 阻断步骤：{blockers.Count}",
                $"- 复核步骤：{reviewRows.Count}",
                $"- Prefab 替换步骤：{applyRows.Count}",
                $"- 宿主执行前置：{(blockers.Count == 0 ? "通过" : "阻断")}",
                "- 前置规则：阻断步骤清零后再进入非阻断人工确认；NeedsReview 仍需人工确认。"
            };

            if (blockers.Count > 0)
                lines.Add($"- 阻断状态：{UIReplacementPlanStatus.Summary(blockers)}");
            lines.Add("");

            AddMissingInputs(lines, rows);
            UIReportMarkdown.AddNotePrefixSummary(lines, "复核项分布", reviewRows);
            AddRows(lines, "阻断项", blockers);
            AddRows(lines, "执行前人工确认", rows.Where(IsManualConfirmation).ToList());
            AddRows(lines, "Prefab 替换候选", applyRows);
            AddRows(lines, "执行后验证", rows.Where(r => r["Action"] == "VerifyAfterApply").ToList());

            File.WriteAllLines(path, lines, new UTF8Encoding(true));
            ValidateSections(lines.ToArray());
            Debug.Log($"UI replacement host apply checklist generated: {path}, {applyRows.Count} prefab apply steps, {blockers.Count} blocking steps.");
            return path;
        }

        public static void ValidateNoBlockingSteps(UIAIToolsProfile profile)
        {
            var path = Generate(profile);
            var rows = UIReportCsv.ReadRows(profile.logRoot, UIReportFiles.ReplacementExecutionPlan);
            var blockers = rows.Where(IsBlocking).ToList();
            if (blockers.Count > 0)
                throw new Exception($"UI replacement host apply checklist has {blockers.Count} blocking steps ({UIReplacementPlanStatus.Summary(blockers)}). See {path}");
            Debug.Log($"UI replacement host apply checklist gate passed: {rows.Count} steps, 0 blocking steps.");
        }

        static bool IsManualConfirmation(Dictionary<string, string> row)
        {
            return !UIReplacementPlanStatus.IsBlocking(row["Status"])
                && (row["Action"].StartsWith("Confirm", StringComparison.Ordinal) || row["Status"] == "NeedsReview");
        }

        static bool IsBlocking(Dictionary<string, string> row)
        {
            return UIReplacementPlanStatus.IsBlocking(row["Status"]);
        }

        static void AddMissingInputs(List<string> lines, List<Dictionary<string, string>> rows)
        {
            lines.Add("## 待补输入");
            var preview = rows.FirstOrDefault(r => r["Status"] == "PendingPreview");
            lines.Add(preview == null ? "- 新版预览：无" : $"- 新版预览：`{preview["NewAsset"]}`");
            var assets = rows.Where(r => r["Action"] == "ConfirmNewAsset" && r["Status"] == "PendingAsset").ToList();
            lines.Add($"- 新图：{assets.Count}");
            foreach (var row in assets.Take(30))
                lines.Add($"- `{row["NewAsset"]}`：参考 `{row["OldAsset"]}`，图集 `{row["TargetAtlas"]}`");
            if (assets.Count > 30)
                lines.Add($"- 仅显示前 30 张新图，共 {assets.Count} 张。");
            var atlases = rows.Where(r => r["Action"] == "ConfirmTargetAtlas" && r["Status"] == "PendingAtlas").GroupBy(r => r["TargetAtlas"]).OrderBy(g => g.Key).ToList();
            lines.Add($"- 目标图集：{atlases.Count}");
            foreach (var atlas in atlases)
                lines.Add($"- `{atlas.Key}`：{atlas.Count()} 张新图");
            lines.Add("");
        }

        static void AddRows(List<string> lines, string title, List<Dictionary<string, string>> rows)
        {
            lines.Add($"## {title}");
            if (rows.Count == 0)
            {
                lines.Add("- 无");
                lines.Add("");
                return;
            }

            foreach (var row in rows.Take(50))
                lines.Add($"- Item {row["ItemIndex"]} / {row["Action"]}：{row["Status"]}，旧 `{row["OldAsset"]}`，新 `{row["NewAsset"]}`，图集 `{row["TargetAtlas"]}`，prefab `{row["PrefabRefs"]}`，说明 `{row["Note"]}`");
            if (rows.Count > 50)
                lines.Add($"- 仅显示前 50 项，共 {rows.Count} 项。");
            lines.Add("");
        }

        static void ValidateSections(string[] lines)
        {
            UIReportMarkdown.RequireExactSectionOrder("UI replacement host apply checklist", lines, "## Gate 状态", "## 待补输入", "## 复核项分布", "## 阻断项", "## 执行前人工确认", "## Prefab 替换候选", "## 执行后验证");
        }

    }
}

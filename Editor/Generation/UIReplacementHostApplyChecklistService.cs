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
            var rows = UIReplacementExecutionPlanService.ReadRows(profile);
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
            Validate(profile);
            Debug.Log($"UI replacement host apply checklist generated: {path}, {applyRows.Count} prefab apply steps, {blockers.Count} blocking steps.");
            return path;
        }

        public static void Validate(UIAIToolsProfile profile)
        {
            var path = UIReportFiles.GetPath(profile.logRoot, UIReportFiles.ReplacementHostApplyChecklist);
            if (!File.Exists(path))
                throw new Exception("Missing UI replacement host apply checklist: " + path);
            var rows = UIReplacementExecutionPlanService.ReadRows(profile);
            var blockers = rows.Where(IsBlocking).ToList();
            var applyRows = rows.Where(r => r["Action"] == "ApplyPrefabReference").ToList();
            var reviewRows = rows.Where(r => r["Status"] == "NeedsReview").ToList();
            var lines = File.ReadAllLines(path);
            ValidateSections(lines);
            RequireLine(lines, "本文件只把待确认执行计划整理成宿主执行前清单，不移动资源、不覆盖 prefab、不修改图集或 YooAsset 配置。");
            RequireLine(lines, $"- 阻断步骤：{blockers.Count}");
            RequireLine(lines, $"- 复核步骤：{reviewRows.Count}");
            RequireLine(lines, $"- Prefab 替换步骤：{applyRows.Count}");
            RequireLine(lines, $"- 宿主执行前置：{(blockers.Count == 0 ? "通过" : "阻断")}");
            RequireLine(lines, "- 前置规则：阻断步骤清零后再进入非阻断人工确认；NeedsReview 仍需人工确认。");
            if (blockers.Count > 0)
                RequireLine(lines, $"- 阻断状态：{UIReplacementPlanStatus.Summary(blockers)}");
            ValidateMissingInputs(lines, rows);
            ValidateNotePrefixSummary(lines, "复核项分布", reviewRows);
            ValidateRows(lines, "阻断项", blockers);
            ValidateRows(lines, "执行前人工确认", rows.Where(IsManualConfirmation).ToList());
            ValidateRows(lines, "Prefab 替换候选", applyRows);
            ValidateRows(lines, "执行后验证", rows.Where(r => r["Action"] == "VerifyAfterApply").ToList());
            Debug.Log("UI replacement host apply checklist validation passed.");
        }

        public static void ValidateNoBlockingSteps(UIAIToolsProfile profile)
        {
            var path = Generate(profile);
            var rows = UIReplacementExecutionPlanService.ReadRows(profile);
            var blockers = rows.Where(IsBlocking).ToList();
            if (blockers.Count > 0)
                throw new Exception($"UI replacement host apply checklist has {blockers.Count} blocking steps ({UIReplacementPlanStatus.Summary(blockers)}). See {path}");
            Debug.Log($"UI replacement host apply checklist gate passed: {rows.Count} steps, 0 blocking steps.");
        }

        public static void ValidateContract()
        {
            var root = Path.Combine(Path.GetTempPath(), "UIAIToolsHostApplyChecklistContract_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                var profile = ScriptableObject.CreateInstance<UIAIToolsProfile>();
                profile.logRoot = root;
                WriteExecutionPlan(profile, new[]
                {
                    PlanRow("-1", "ConfirmDraftPreview", "PendingPreview", "", "Assets/Preview.png", "", "", "true", "人工确认新版预览", ""),
                    PlanRow("0", "ConfirmNewAsset", "PendingAsset", "Assets/Old.png", "Assets/New.png", "Assets/Atlas.spriteatlasv2", "Assets/Prefab/A.prefab", "true", "人工确认新图", ""),
                    PlanRow("0", "ConfirmTargetAtlas", "PendingAtlas", "Assets/Old.png", "Assets/New.png", "Assets/Atlas.spriteatlasv2", "Assets/Prefab/A.prefab", "true", "人工确认目标图集", ""),
                    PlanRow("0", "ConfirmRiskChecks", "NeedsReview", "Assets/Old.png", "Assets/New.png", "Assets/Atlas.spriteatlasv2", "Assets/Prefab/A.prefab", "true", "复用风险：人工确认", "risk"),
                    PlanRow("0", "ApplyPrefabReference", "PendingConfirmation", "Assets/Old.png", "Assets/New.png", "Assets/Atlas.spriteatlasv2", "Assets/Prefab/A.prefab", "true", "人工确认后替换 prefab", ""),
                    PlanRow("0", "VerifyAfterApply", "PendingConfirmation", "Assets/Old.png", "Assets/New.png", "Assets/Atlas.spriteatlasv2", "Assets/Prefab/A.prefab", "true", "执行后复验", "")
                });
                var path = Generate(profile);
                var lines = File.ReadAllLines(path).Where(line => line != "- 宿主执行前置：阻断").ToArray();
                File.WriteAllLines(path, lines, new UTF8Encoding(true));
                ExpectFailure("missing_gate_line", "宿主执行前置", () => Validate(profile));
            }
            finally
            {
                if (Directory.Exists(root))
                    Directory.Delete(root, true);
            }
            Debug.Log("UI replacement host apply checklist contract validation passed.");
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

        static void ValidateMissingInputs(string[] lines, List<Dictionary<string, string>> rows)
        {
            RequireLine(lines, "## 待补输入");
            var preview = rows.FirstOrDefault(r => r["Status"] == "PendingPreview");
            RequireLine(lines, preview == null ? "- 新版预览：无" : $"- 新版预览：`{preview["NewAsset"]}`");
            var assets = rows.Where(r => r["Action"] == "ConfirmNewAsset" && r["Status"] == "PendingAsset").ToList();
            RequireLine(lines, $"- 新图：{assets.Count}");
            foreach (var row in assets.Take(30))
                RequireLine(lines, $"- `{row["NewAsset"]}`：参考 `{row["OldAsset"]}`，图集 `{row["TargetAtlas"]}`");
            if (assets.Count > 30)
                RequireLine(lines, $"- 仅显示前 30 张新图，共 {assets.Count} 张。");
            var atlases = rows.Where(r => r["Action"] == "ConfirmTargetAtlas" && r["Status"] == "PendingAtlas").GroupBy(r => r["TargetAtlas"]).OrderBy(g => g.Key).ToList();
            RequireLine(lines, $"- 目标图集：{atlases.Count}");
            foreach (var atlas in atlases)
                RequireLine(lines, $"- `{atlas.Key}`：{atlas.Count()} 张新图");
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
                lines.Add(RowSummary(row));
            if (rows.Count > 50)
                lines.Add($"- 仅显示前 50 项，共 {rows.Count} 项。");
            lines.Add("");
        }

        static void ValidateRows(string[] lines, string title, List<Dictionary<string, string>> rows)
        {
            RequireLine(lines, "## " + title);
            if (rows.Count == 0)
            {
                RequireLine(lines, "- 无");
                return;
            }
            foreach (var row in rows.Take(50))
                RequireLine(lines, RowSummary(row));
            if (rows.Count > 50)
                RequireLine(lines, $"- 仅显示前 50 项，共 {rows.Count} 项。");
        }

        static string RowSummary(Dictionary<string, string> row)
        {
            return $"- Item {row["ItemIndex"]} / {row["Action"]}：{row["Status"]}，旧 `{row["OldAsset"]}`，新 `{row["NewAsset"]}`，图集 `{row["TargetAtlas"]}`，prefab `{row["PrefabRefs"]}`，说明 `{row["Note"]}`";
        }

        static void ValidateNotePrefixSummary(string[] lines, string title, List<Dictionary<string, string>> rows)
        {
            RequireLine(lines, "## " + title);
            if (rows.Count == 0)
            {
                RequireLine(lines, "- 无");
                return;
            }
            foreach (var group in rows.GroupBy(r => NotePrefix(r["Note"])).OrderByDescending(g => g.Count()).ThenBy(g => g.Key))
                RequireLine(lines, $"- {group.Key}：{group.Count()}");
        }

        static string NotePrefix(string note)
        {
            var index = note.IndexOf('：');
            return index > 0 ? note.Substring(0, index) : note;
        }

        static void RequireLine(string[] lines, string line)
        {
            if (!lines.Contains(line))
                throw new Exception("UI replacement host apply checklist is missing: " + line);
        }

        static void ValidateSections(string[] lines)
        {
            UIReportMarkdown.RequireExactSectionOrder("UI replacement host apply checklist", lines, "## Gate 状态", "## 待补输入", "## 复核项分布", "## 阻断项", "## 执行前人工确认", "## Prefab 替换候选", "## 执行后验证");
        }

        static void WriteExecutionPlan(UIAIToolsProfile profile, IEnumerable<string> rows)
        {
            var path = UIReportFiles.GetPath(profile.logRoot, UIReportFiles.ReplacementExecutionPlan);
            File.WriteAllLines(path, new[] { UIReportFiles.ReplacementExecutionPlanHeader }.Concat(rows), new UTF8Encoding(true));
        }

        static string PlanRow(string itemIndex, string action, string status, string oldAsset, string newAsset, string targetAtlas, string prefabRefs, string requiresManualConfirmation, string note, string risk)
        {
            return string.Join(",", new[] { itemIndex, action, status, oldAsset, newAsset, targetAtlas, prefabRefs, requiresManualConfirmation, note, risk }.Select(Csv));
        }

        static string Csv(string value)
        {
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        static void ExpectFailure(string name, string expectedMessage, Action action)
        {
            try
            {
                action();
            }
            catch (Exception exception)
            {
                if (exception.Message.Contains(expectedMessage))
                    return;
                throw new Exception($"Unexpected UI replacement host apply checklist contract failure for {name}: {exception.Message}");
            }
            throw new Exception("UI replacement host apply checklist contract sample did not fail: " + name);
        }
    }
}

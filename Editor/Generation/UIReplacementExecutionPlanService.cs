using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Xipin.UIAITools
{
    public static class UIReplacementExecutionPlanService
    {
        static readonly HashSet<string> AllowedActions = new HashSet<string> { "ConfirmDraftPreview", "ConfirmNewAsset", "ConfirmTargetAtlas", "ConfirmRiskChecks", "MoveNewAsset", "ApplyPrefabReference", "UpdateAtlas", "UpdateAddress", "VerifyAfterApply" };
        static readonly HashSet<string> AllowedStatuses = new HashSet<string> { "Blocked", "PendingPreview", "PendingAsset", "PendingAtlas", "NeedsReview", "PendingConfirmation" };

        public static string Generate(UIAIToolsProfile profile, string draftJsonPath)
        {
            UIReplacementPlanDryRunService.Run(profile, draftJsonPath);
            return GenerateFromCurrentDryRun(profile, draftJsonPath);
        }

        public static string GenerateFromCurrentDryRun(UIAIToolsProfile profile, string draftJsonPath)
        {
            var draft = UIRedesignDraftService.LoadDraft(draftJsonPath);
            var dryRunRows = UIReplacementPlanDryRunService.ReadRows(profile);
            var lines = new List<string> { UIReportFiles.ReplacementExecutionPlanHeader };

            AddDraftPreview(lines, draft);
            for (int i = 0; i < draft.replacementPlan.items.Count; i++)
                AddItem(lines, i, draft.replacementPlan.items[i], dryRunRows.Where(r => r["ItemIndex"] == i.ToString()).ToList());

            var path = UIReportFiles.GetPath(profile.logRoot, UIReportFiles.ReplacementExecutionPlan);
            File.WriteAllLines(path, lines, new UTF8Encoding(true));
            UIReportValidationService.ValidateReport(profile, UIReportFiles.ReplacementExecutionPlan, UIReportFiles.ReplacementExecutionPlanHeader);
            var summaryPath = GenerateSummary(profile, draft);
            Debug.Log($"UI replacement execution plan generated: {path}, summary: {summaryPath}, {draft.replacementPlan.items.Count} items.");
            return path;
        }

        public static List<Dictionary<string, string>> ReadRows(UIAIToolsProfile profile)
        {
            UIReportValidationService.ValidateReport(profile, UIReportFiles.ReplacementExecutionPlan, UIReportFiles.ReplacementExecutionPlanHeader);
            var rows = UIReportCsv.ReadRows(profile.logRoot, UIReportFiles.ReplacementExecutionPlan);
            if (rows.Count == 0)
                throw new Exception("Invalid UI replacement execution plan: plan rows are required");
            foreach (var row in rows)
                ValidateRow(row);
            ValidateNoDuplicateRows(rows);
            return rows;
        }

        public static void ValidateNoBlockingStatuses(UIAIToolsProfile profile)
        {
            var rows = ReadRows(profile);
            ValidateSummary(profile);
            var blocking = rows.Where(IsBlocking).ToList();
            if (blocking.Count > 0)
                throw new Exception($"UI replacement execution plan has {blocking.Count} blocking steps ({UIReplacementPlanStatus.Summary(blocking)}). See {UIReportFiles.GetPath(profile.logRoot, UIReportFiles.ReplacementExecutionPlanSummary)}");
            Debug.Log($"UI replacement execution plan gate passed: {rows.Count} steps, 0 blocking steps.");
        }

        public static void ValidateContract()
        {
            var root = Path.Combine(Path.GetTempPath(), "UIAIToolsReplacementExecutionPlanContract_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                var profile = ScriptableObject.CreateInstance<UIAIToolsProfile>();
                profile.logRoot = root;
                var row = Row("0", "ConfirmNewAsset", "PendingConfirmation", "Assets/Old.png", "Assets/New.png", "Assets/Atlas.spriteatlasv2", "Assets/Prefab/A.prefab", "true", "人工确认新图", "reason");
                WriteCsv(profile, new[] { row });
                ReadRows(profile);
                ExpectRowsFailure(profile, "duplicate_plan_row", new[] { row, row }, "duplicate plan row");
            }
            finally
            {
                if (Directory.Exists(root))
                    Directory.Delete(root, true);
            }
            Debug.Log("UI replacement execution plan contract validation passed.");
        }

        static void AddItem(List<string> lines, int index, UIReplacementItem item, List<Dictionary<string, string>> checks)
        {
            var prefabs = Evidence(checks, "PrefabReference");
            AddLine(lines, index, "ConfirmNewAsset", Status(checks, "NewAssetExists", "PendingConfirmation", "PendingAsset"), item, prefabs, "人工确认新图已生成、命名和视觉验收通过");
            AddLine(lines, index, "ConfirmTargetAtlas", Status(checks, "TargetAtlasExists", "PendingConfirmation", "PendingAtlas"), item, prefabs, "人工确认目标图集存在且归属正确");
            AddLine(lines, index, "ConfirmRiskChecks", RiskStatus(checks), item, prefabs, RiskNote(checks));
            AddLine(lines, index, "ApplyPrefabReference", Status(checks, "PrefabReference", "PendingConfirmation", "NeedsReview"), item, prefabs, "人工确认后再替换 prefab 内旧图引用");
            AddLine(lines, index, "VerifyAfterApply", BlockedByErrors(checks) ? "Blocked" : "PendingConfirmation", item, prefabs, "执行后重跑扫描、dry-run 和人工验收");
        }

        static void AddDraftPreview(List<string> lines, UIRedesignDraft draft)
        {
            lines.Add(string.Join(",", new[]
            {
                "-1",
                Csv("ConfirmDraftPreview"),
                Csv(File.Exists(draft.draftPreviewPath) ? "PendingConfirmation" : "PendingPreview"),
                Csv(""),
                Csv(draft.draftPreviewPath),
                Csv(""),
                Csv(""),
                Csv("true"),
                Csv("人工确认新版预览图已生成并通过视觉验收"),
                Csv("")
            }));
        }

        static bool IsBlocking(Dictionary<string, string> row)
        {
            return UIReplacementPlanStatus.IsBlocking(row["Status"]);
        }

        static void ValidateRow(Dictionary<string, string> row)
        {
            if (!int.TryParse(row["ItemIndex"], out _))
                throw new Exception("Invalid UI replacement execution plan: ItemIndex must be an integer");
            if (string.IsNullOrEmpty(row["Action"]))
                throw new Exception("Invalid UI replacement execution plan: Action is required");
            if (!AllowedActions.Contains(row["Action"]))
                throw new Exception("Invalid UI replacement execution plan: invalid action " + row["Action"]);
            if (!AllowedStatuses.Contains(row["Status"]))
                throw new Exception("Invalid UI replacement execution plan: invalid status " + row["Status"]);
            if (row["RequiresManualConfirmation"] != "true")
                throw new Exception("Invalid UI replacement execution plan: RequiresManualConfirmation must be true");
            if (string.IsNullOrEmpty(row["Note"]))
                throw new Exception("Invalid UI replacement execution plan: Note is required");
        }

        static void ValidateNoDuplicateRows(List<Dictionary<string, string>> rows)
        {
            var duplicate = rows.GroupBy(row => new
            {
                ItemIndex = row["ItemIndex"],
                Action = row["Action"],
                OldAsset = row["OldAsset"],
                NewAsset = row["NewAsset"],
                TargetAtlas = row["TargetAtlas"],
                PrefabRefs = row["PrefabRefs"]
            }).FirstOrDefault(group => group.Count() > 1);
            if (duplicate != null)
                throw new Exception($"Invalid UI replacement execution plan: duplicate plan row for Item {duplicate.Key.ItemIndex} / {duplicate.Key.Action}");
        }

        static string Status(List<Dictionary<string, string>> checks, string check, string ok, string missing)
        {
            var row = checks.First(r => r["Check"] == check);
            if (row["Severity"] == "Error")
                return "Blocked";
            return row["Status"] == "OK" ? ok : missing;
        }

        static string RiskStatus(List<Dictionary<string, string>> checks)
        {
            if (BlockedByErrors(checks))
                return "Blocked";
            return checks.Any(r => r["Severity"] == "Review") ? "NeedsReview" : "PendingConfirmation";
        }

        static string RiskNote(List<Dictionary<string, string>> checks)
        {
            var risks = checks.Where(r => r["Severity"] == "Error" || r["Severity"] == "Review").Select(RiskText).Take(6).ToList();
            return risks.Count == 0 ? "人工确认复用、按名加载、GUID 和输出目录风险" : string.Join("；", risks);
        }

        static string RiskText(Dictionary<string, string> row)
        {
            return row["Check"] + "：" + row["Message"] + EvidenceSuffix(row["Evidence"]);
        }

        static string EvidenceSuffix(string evidence)
        {
            return string.IsNullOrEmpty(evidence) ? "" : "，证据 " + evidence;
        }

        static bool BlockedByErrors(List<Dictionary<string, string>> checks)
        {
            return checks.Any(r => r["Severity"] == "Error");
        }

        static string Evidence(List<Dictionary<string, string>> checks, string check)
        {
            return checks.First(r => r["Check"] == check)["Evidence"];
        }

        static void AddLine(List<string> lines, int index, string action, string status, UIReplacementItem item, string prefabs, string note)
        {
            lines.Add(string.Join(",", new[]
            {
                index.ToString(),
                Csv(action),
                Csv(status),
                Csv(item.oldAssetPath),
                Csv(item.newAssetPath),
                Csv(item.targetAtlasPath),
                Csv(prefabs),
                Csv("true"),
                Csv(note),
                Csv(item.reason)
            }));
        }

        static string GenerateSummary(UIAIToolsProfile profile, UIRedesignDraft draft)
        {
            var dryRunRows = UIReplacementPlanDryRunService.ReadRows(profile);
            var planRows = ReadRows(profile);
            var reuseRows = UIScanReportRows.ReadReuseIndex(profile);
            var path = UIReportFiles.GetPath(profile.logRoot, UIReportFiles.ReplacementExecutionPlanSummary);
            var lines = new List<string>
            {
                "# UI 替换待确认执行计划",
                "",
                $"生成时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                "",
                "本文件只展开人工确认前的执行步骤，不移动资源、不覆盖 prefab、不修改图集或 YooAsset 配置。",
                "",
                "## 范围",
                $"- 替换项：{draft.replacementPlan.items.Count}",
                $"- 风险项：{draft.risks.Count}",
                $"- 草稿需要人工确认：{draft.requiresConfirmation}",
                "",
                "## DryRun 分布"
            };

            foreach (var group in dryRunRows.GroupBy(r => r["Severity"]).OrderBy(g => UIReplacementPlanStatus.SeverityOrder(g.Key)).ThenBy(g => g.Key))
                lines.Add($"- {group.Key}：{group.Count()}");
            lines.Add("");
            UIReportMarkdown.AddSeverityCheckSummary(lines, "DryRun 检查分布", dryRunRows.Where(r => r["Severity"] == "Warning" || r["Severity"] == "Review").ToList());

            lines.Add("## Gate 状态");
            var dryRunErrors = dryRunRows.Count(r => r["Severity"] == "Error");
            lines.Add($"- DryRun Error：{dryRunErrors}");
            lines.Add($"- DryRun Gate：{(dryRunErrors == 0 ? "通过" : "阻断")}");
            var blockingRows = planRows.Where(IsBlocking).ToList();
            lines.Add($"- 执行计划阻断步骤：{blockingRows.Count}");
            lines.Add($"- 执行计划 Gate：{(blockingRows.Count == 0 ? "通过" : "阻断")}");
            lines.Add("- 执行计划 Gate 规则：Blocked、PendingPreview、PendingAsset 和 PendingAtlas 阻断；NeedsReview 留给人工确认。");
            if (blockingRows.Count > 0)
                lines.Add($"- 阻断状态：{UIReplacementPlanStatus.Summary(blockingRows)}");
            lines.Add("");

            lines.Add("## 执行步骤状态");
            foreach (var group in planRows.GroupBy(r => r["Status"]).OrderBy(g => UIReplacementPlanStatus.StatusOrder(g.Key)).ThenBy(g => g.Key))
                lines.Add($"- {group.Key}：{group.Count()}");
            lines.Add("");

            AddDryRunTop(lines, "DryRun 阻断项", dryRunRows.Where(r => r["Severity"] == "Error").ToList());
            AddTop(lines, "待补预览和资源", planRows.Where(r => UIReplacementPlanStatus.IsBlocking(r["Status"]) && r["Status"] != "Blocked").ToList());
            AddMissingAssets(lines, planRows, reuseRows);
            AddMissingAtlases(lines, planRows);
            AddReviewStatus(lines, planRows);
            AddTop(lines, "待复核步骤", planRows.Where(r => r["Status"] == "NeedsReview").ToList());
            lines.Add("## 人工确认后");
            lines.Add("- 确认新版预览、新图、图集归属、prefab 引用范围、动态加载和 GUID 策略。");
            lines.Add("- 由宿主项目执行资源改动后，重新运行扫描、dry-run 和本执行计划。");
            lines.Add("");

            File.WriteAllLines(path, lines, new UTF8Encoding(true));
            ValidateSummarySections(lines.ToArray());
            return path;
        }

        static void AddDryRunTop(List<string> lines, string title, List<Dictionary<string, string>> rows)
        {
            lines.Add($"## {title}");
            if (rows.Count == 0)
            {
                lines.Add("- 无");
                lines.Add("");
                return;
            }

            foreach (var row in rows.Take(20))
                lines.Add($"- Item {row["ItemIndex"]} / {row["Check"]}：{row["Message"]}，旧 `{row["OldAsset"]}`，新 `{row["NewAsset"]}`，证据 `{row["Evidence"]}`");
            AddMore(lines, rows.Count, 20);
            lines.Add("");
        }

        static void AddTop(List<string> lines, string title, List<Dictionary<string, string>> rows)
        {
            lines.Add($"## {title}");
            if (rows.Count == 0)
            {
                lines.Add("- 无");
                lines.Add("");
                return;
            }

            foreach (var row in rows.Take(20))
                lines.Add($"- Item {row["ItemIndex"]} / {row["Action"]}：{row["Status"]}，旧 `{row["OldAsset"]}`，新 `{row["NewAsset"]}`，说明 `{row["Note"]}`");
            AddMore(lines, rows.Count, 20);
            lines.Add("");
        }

        static void AddReviewStatus(List<string> lines, List<Dictionary<string, string>> planRows)
        {
            UIReportMarkdown.AddNotePrefixSummary(lines, "复核项分布", planRows.Where(r => r["Status"] == "NeedsReview").ToList());
        }

        static void AddMissingAssets(List<string> lines, List<Dictionary<string, string>> planRows, List<Dictionary<string, string>> reuseRows)
        {
            var rows = planRows.Where(r => r["Action"] == "ConfirmNewAsset" && r["Status"] == "PendingAsset").ToList();
            lines.Add("## 待生成新图清单");
            if (rows.Count == 0)
            {
                lines.Add("- 无");
                lines.Add("");
                return;
            }

            foreach (var row in rows.Take(50))
                lines.Add($"- `{row["NewAsset"]}`：参考旧图 `{row["OldAsset"]}`，{AssetInfo(reuseRows, row["OldAsset"])}，目标图集 `{row["TargetAtlas"]}`");
            AddMore(lines, rows.Count, 50);
            lines.Add("");
        }

        static void AddMore(List<string> lines, int count, int shown)
        {
            if (count > shown)
                lines.Add($"- 仅显示前 {shown} 项，共 {count} 项。");
        }

        static void AddMissingAtlases(List<string> lines, List<Dictionary<string, string>> planRows)
        {
            var atlases = planRows.Where(r => r["Action"] == "ConfirmTargetAtlas" && r["Status"] == "PendingAtlas").GroupBy(r => r["TargetAtlas"]).OrderBy(g => g.Key).ToList();
            lines.Add("## 待确认目标图集");
            if (atlases.Count == 0)
            {
                lines.Add("- 无");
                lines.Add("");
                return;
            }

            foreach (var atlas in atlases)
                lines.Add($"- `{atlas.Key}`：{atlas.Count()} 张新图");
            lines.Add("");
        }

        static string AssetInfo(List<Dictionary<string, string>> reuseRows, string oldAsset)
        {
            var row = reuseRows.FirstOrDefault(r => r["Path"] == oldAsset);
            return row == null ? "旧图不在复用索引中" : $"{row["Width"]}x{row["Height"]}，{row["Kind"]}，{row["SizeClass"]}";
        }

        static void ValidateSummarySections(string[] lines)
        {
            UIReportMarkdown.RequireExactSectionOrder("UI replacement execution plan summary", lines, "## 范围", "## DryRun 分布", "## DryRun 检查分布", "## Gate 状态", "## 执行步骤状态", "## DryRun 阻断项", "## 待补预览和资源", "## 待生成新图清单", "## 待确认目标图集", "## 复核项分布", "## 待复核步骤", "## 人工确认后");
        }

        static void ValidateSummary(UIAIToolsProfile profile)
        {
            var path = UIReportFiles.GetPath(profile.logRoot, UIReportFiles.ReplacementExecutionPlanSummary);
            if (!File.Exists(path))
                throw new Exception("Missing UI replacement execution plan summary: " + path);
            ValidateSummarySections(File.ReadAllLines(path));
        }

        static string Csv(string value)
        {
            value = value ?? "";
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        static void WriteCsv(UIAIToolsProfile profile, IEnumerable<string> rows)
        {
            var path = UIReportFiles.GetPath(profile.logRoot, UIReportFiles.ReplacementExecutionPlan);
            File.WriteAllLines(path, new[] { UIReportFiles.ReplacementExecutionPlanHeader }.Concat(rows), new UTF8Encoding(true));
        }

        static string Row(string itemIndex, string action, string status, string oldAsset, string newAsset, string targetAtlas, string prefabRefs, string requiresManualConfirmation, string note, string risk)
        {
            return string.Join(",", new[] { itemIndex, action, status, oldAsset, newAsset, targetAtlas, prefabRefs, requiresManualConfirmation, note, risk }.Select(Csv));
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
                throw new Exception($"Unexpected UI replacement execution plan contract failure for {name}: {exception.Message}");
            }
            throw new Exception("UI replacement execution plan contract sample did not fail: " + name);
        }
    }
}

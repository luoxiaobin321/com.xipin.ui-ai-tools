using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Xipin.UIAITools
{
    public static class UIReplacementHostApplyResultService
    {
        static readonly HashSet<string> AllowedActions = new HashSet<string> { "MoveNewAsset", "ApplyPrefabReference", "UpdateAtlas", "UpdateAddress", "VerifyAfterApply" };
        static readonly HashSet<string> AllowedStatuses = new HashSet<string> { "Applied", "Skipped", "Failed", "Verified" };

        public static string GenerateSummary(UIAIToolsProfile profile)
        {
            var rows = ReadRows(profile);
            var path = UIReportFiles.GetPath(profile.logRoot, UIReportFiles.ReplacementHostApplyResultSummary);
            var failed = rows.Where(r => r["Status"] == "Failed").ToList();
            var lines = new List<string>
            {
                "# UI 替换宿主执行结果",
                "",
                $"生成时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                "",
                "本文件记录宿主确认后执行器的结果，只读校验，不代表包内执行了资源改动。",
                "",
                "## 总览",
                $"- CSV：`{UIReportFiles.GetPath(profile.logRoot, UIReportFiles.ReplacementHostApplyResult)}`",
                $"- 结果总数：{rows.Count}",
                $"- 成功或已验证：{rows.Count(r => r["Status"] == "Applied" || r["Status"] == "Verified")}",
                $"- 跳过：{rows.Count(r => r["Status"] == "Skipped")}",
                $"- 失败：{failed.Count}",
                ""
            };
            AddStatusSummary(lines, rows);
            AddRows(lines, "失败项", failed);
            AddRows(lines, "执行项", rows);
            lines.Add("## 执行后复验");
            lines.Add("- [ ] 重新运行核心扫描和报告验证。");
            lines.Add("- [ ] 重新运行 dry-run、执行计划和宿主执行前 gate。");
            lines.Add("- [ ] 宿主项目完成图集、资源加载和 UI 回归验证。");
            lines.Add("");
            File.WriteAllLines(path, lines, new UTF8Encoding(true));
            ValidateSummary(profile, rows);
            Debug.Log($"UI replacement host apply result summary generated: {path}, {rows.Count} rows.");
            return path;
        }

        public static void Validate(UIAIToolsProfile profile)
        {
            var rows = ReadRows(profile);
            ValidateSummary(profile, rows);
            Debug.Log($"UI replacement host apply result validation passed: {rows.Count} rows.");
        }

        public static void ValidateAgainstExecutionPlan(UIAIToolsProfile profile)
        {
            var rows = ReadRows(profile);
            ValidateSummary(profile, rows);
            var planRows = UIReplacementExecutionPlanService.ReadRows(profile);
            foreach (var row in rows)
            {
                if (!planRows.Any(plan => SamePlanRow(plan, row)))
                    throw new Exception($"Invalid UI replacement host apply result: execution plan row missing for Item {row["ItemIndex"]} / {row["Action"]}");
            }
            ValidatePlanCoverage(planRows, rows);
            ValidateBlockingPlanDidNotExecute(planRows, rows);
            Debug.Log($"UI replacement host apply result execution plan validation passed: {rows.Count} rows.");
        }

        public static void ValidateContract()
        {
            var root = Path.Combine(Path.GetTempPath(), "UIAIToolsHostApplyResultContract_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                var profile = ScriptableObject.CreateInstance<UIAIToolsProfile>();
                profile.logRoot = root;
                WriteCsv(profile, new[]
                {
                    Row("0", "MoveNewAsset", "Applied", "Assets/Old.png", "Assets/New.png", "", "", "QA-1", "moved"),
                    Row("0", "ApplyPrefabReference", "Applied", "Assets/Old.png", "Assets/New.png", "Assets/Atlas.spriteatlasv2", "Assets/UI.prefab", "QA-1", "updated"),
                    Row("0", "VerifyAfterApply", "Verified", "Assets/Old.png", "Assets/New.png", "Assets/Atlas.spriteatlasv2", "Assets/UI.prefab", "QA-1", "verified"),
                    Row("1", "UpdateAtlas", "Skipped", "Assets/Old2.png", "Assets/New2.png", "Assets/Atlas.spriteatlasv2", "", "", "no atlas change"),
                    Row("1", "UpdateAddress", "Skipped", "Assets/Old2.png", "Assets/New2.png", "Assets/Atlas.spriteatlasv2", "", "", "no address change"),
                    Row("2", "ApplyPrefabReference", "Failed", "Assets/Old3.png", "Assets/New3.png", "Assets/Atlas.spriteatlasv2", "Assets/UI.prefab", "QA-2", "missing sprite")
                });
                GenerateSummary(profile);
                Validate(profile);
                WritePlanCsv(profile, new[]
                {
                    PlanRow("0", "MoveNewAsset", "PendingConfirmation", "Assets/Old.png", "Assets/New.png", "", ""),
                    PlanRow("0", "ApplyPrefabReference", "PendingConfirmation", "Assets/Old.png", "Assets/New.png", "Assets/Atlas.spriteatlasv2", "Assets/UI.prefab"),
                    PlanRow("0", "VerifyAfterApply", "PendingConfirmation", "Assets/Old.png", "Assets/New.png", "Assets/Atlas.spriteatlasv2", "Assets/UI.prefab"),
                    PlanRow("1", "UpdateAtlas", "PendingConfirmation", "Assets/Old2.png", "Assets/New2.png", "Assets/Atlas.spriteatlasv2", ""),
                    PlanRow("1", "UpdateAddress", "PendingConfirmation", "Assets/Old2.png", "Assets/New2.png", "Assets/Atlas.spriteatlasv2", ""),
                    PlanRow("2", "ApplyPrefabReference", "PendingConfirmation", "Assets/Old3.png", "Assets/New3.png", "Assets/Atlas.spriteatlasv2", "Assets/UI.prefab")
                });
                ValidateAgainstExecutionPlan(profile);
                ExpectPlanCoverageFailure(profile, "result row missing for plan Item 0 / VerifyAfterApply");
                ExpectBlockingPlanFailure(profile, "blocking steps present");
                ExpectPlanFailure(profile, Row("3", "ApplyPrefabReference", "Skipped", "Assets/Old4.png", "Assets/New4.png", "", "", "", "stale"), "execution plan row missing");
                ExpectSummaryFailure(profile, "bad_title", "# Bad", "unexpected title");
                ExpectFailure(profile, "empty_rows", null, "result rows are required");
                ExpectFailure(profile, "bad_item_index", Row("x", "ApplyPrefabReference", "Applied", "Assets/Old.png", "Assets/New.png", "", "", "QA-1", "bad"), "ItemIndex must be an integer");
                ExpectFailure(profile, "missing_action", Row("0", "", "Applied", "Assets/Old.png", "Assets/New.png", "", "", "QA-1", "bad"), "Action is required");
                ExpectFailure(profile, "unknown_action", Row("0", "ApplyTypo", "Applied", "Assets/Old.png", "Assets/New.png", "", "", "QA-1", "bad"), "invalid action");
                ExpectFailure(profile, "bad_status", Row("0", "ApplyPrefabReference", "Done", "Assets/Old.png", "Assets/New.png", "", "", "QA-1", "bad"), "invalid status");
                ExpectFailure(profile, "missing_confirmation", Row("0", "ApplyPrefabReference", "Applied", "Assets/Old.png", "Assets/New.png", "", "", "", "bad"), "confirmation is required");
                ExpectFailure(profile, "missing_skipped_message", Row("0", "ApplyPrefabReference", "Skipped", "Assets/Old.png", "Assets/New.png", "", "", "", ""), "skipped message is required");
                ExpectFailure(profile, "missing_failed_message", Row("0", "ApplyPrefabReference", "Failed", "Assets/Old.png", "Assets/New.png", "", "", "QA-1", ""), "failed message is required");
            }
            finally
            {
                if (Directory.Exists(root))
                    Directory.Delete(root, true);
            }
            Debug.Log("UI replacement host apply result contract validation passed.");
        }

        public static List<Dictionary<string, string>> ReadRows(UIAIToolsProfile profile)
        {
            UIReportValidationService.ValidateReport(profile, UIReportFiles.ReplacementHostApplyResult, UIReportFiles.ReplacementHostApplyResultHeader);
            var rows = UIReportCsv.ReadRows(profile.logRoot, UIReportFiles.ReplacementHostApplyResult);
            if (rows.Count == 0)
                throw new Exception("Invalid UI replacement host apply result: result rows are required");
            foreach (var row in rows)
                ValidateRow(row);
            return rows;
        }

        static void ValidateRow(Dictionary<string, string> row)
        {
            if (!int.TryParse(row["ItemIndex"], out _))
                throw new Exception("Invalid UI replacement host apply result: ItemIndex must be an integer");
            if (string.IsNullOrEmpty(row["Action"]))
                throw new Exception("Invalid UI replacement host apply result: Action is required");
            if (!AllowedActions.Contains(row["Action"]))
                throw new Exception("Invalid UI replacement host apply result: invalid action " + row["Action"]);
            if (!AllowedStatuses.Contains(row["Status"]))
                throw new Exception("Invalid UI replacement host apply result: invalid status " + row["Status"]);
            if ((row["Status"] == "Applied" || row["Status"] == "Verified") && string.IsNullOrEmpty(row["Confirmation"]))
                throw new Exception("Invalid UI replacement host apply result: confirmation is required");
            if (row["Status"] == "Skipped" && string.IsNullOrEmpty(row["Message"]))
                throw new Exception("Invalid UI replacement host apply result: skipped message is required");
            if (row["Status"] == "Failed" && string.IsNullOrEmpty(row["Message"]))
                throw new Exception("Invalid UI replacement host apply result: failed message is required");
        }

        static bool SamePlanRow(Dictionary<string, string> plan, Dictionary<string, string> row)
        {
            return plan["ItemIndex"] == row["ItemIndex"]
                && plan["Action"] == row["Action"]
                && plan["OldAsset"] == row["OldAsset"]
                && plan["NewAsset"] == row["NewAsset"]
                && plan["TargetAtlas"] == row["TargetAtlas"]
                && plan["PrefabRefs"] == row["PrefabRefs"];
        }

        static void ValidatePlanCoverage(List<Dictionary<string, string>> planRows, List<Dictionary<string, string>> rows)
        {
            foreach (var plan in planRows.Where(RequiresResultRow))
            {
                if (!rows.Any(row => SamePlanRow(plan, row)))
                    throw new Exception($"Invalid UI replacement host apply result: result row missing for plan Item {plan["ItemIndex"]} / {plan["Action"]}");
            }
        }

        static bool RequiresResultRow(Dictionary<string, string> plan)
        {
            return plan["Action"] == "ApplyPrefabReference" || plan["Action"] == "VerifyAfterApply";
        }

        static void ValidateBlockingPlanDidNotExecute(List<Dictionary<string, string>> planRows, List<Dictionary<string, string>> rows)
        {
            if (!planRows.Any(row => UIReplacementPlanStatus.IsBlocking(row["Status"])))
                return;
            var executed = rows.FirstOrDefault(row => row["Status"] != "Skipped");
            if (executed != null)
                throw new Exception($"Invalid UI replacement host apply result: blocking steps present, result must be Skipped only. Item {executed["ItemIndex"]} / {executed["Action"]} is {executed["Status"]}");
        }

        static void AddStatusSummary(List<string> lines, List<Dictionary<string, string>> rows)
        {
            lines.Add("## 状态分布");
            if (rows.Count == 0)
            {
                lines.Add("- 无");
                lines.Add("");
                return;
            }
            foreach (var group in rows.GroupBy(r => r["Status"]).OrderBy(g => StatusOrder(g.Key)).ThenBy(g => g.Key))
                lines.Add($"- {group.Key}：{group.Count()}");
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
            foreach (var row in rows.Take(50))
                lines.Add(RowSummary(row));
            if (rows.Count > 50)
                lines.Add($"- 仅显示前 50 项，共 {rows.Count} 项。");
            lines.Add("");
        }

        static void ValidateSummary(UIAIToolsProfile profile, List<Dictionary<string, string>> rows)
        {
            var path = UIReportFiles.GetPath(profile.logRoot, UIReportFiles.ReplacementHostApplyResultSummary);
            if (!File.Exists(path))
                throw new Exception("Missing UI replacement host apply result summary: " + path);
            var lines = File.ReadAllLines(path);
            RequireTitle(lines, "# UI 替换宿主执行结果");
            ValidateSections(lines);
            var failed = rows.Where(r => r["Status"] == "Failed").ToList();
            RequireLine(lines, "本文件记录宿主确认后执行器的结果，只读校验，不代表包内执行了资源改动。");
            RequireLine(lines, $"- CSV：`{UIReportFiles.GetPath(profile.logRoot, UIReportFiles.ReplacementHostApplyResult)}`");
            RequireLine(lines, $"- 结果总数：{rows.Count}");
            RequireLine(lines, $"- 成功或已验证：{rows.Count(r => r["Status"] == "Applied" || r["Status"] == "Verified")}");
            RequireLine(lines, $"- 跳过：{rows.Count(r => r["Status"] == "Skipped")}");
            RequireLine(lines, $"- 失败：{failed.Count}");
            ValidateStatusSummary(lines, rows);
            ValidateRows(lines, "失败项", failed);
            ValidateRows(lines, "执行项", rows);
            RequireLine(lines, "- [ ] 重新运行核心扫描和报告验证。");
            RequireLine(lines, "- [ ] 重新运行 dry-run、执行计划和宿主执行前 gate。");
            RequireLine(lines, "- [ ] 宿主项目完成图集、资源加载和 UI 回归验证。");
        }

        static void ValidateStatusSummary(string[] lines, List<Dictionary<string, string>> rows)
        {
            RequireLine(lines, "## 状态分布");
            if (rows.Count == 0)
            {
                RequireLine(lines, "- 无");
                return;
            }
            foreach (var group in rows.GroupBy(r => r["Status"]).OrderBy(g => StatusOrder(g.Key)).ThenBy(g => g.Key))
                RequireLine(lines, $"- {group.Key}：{group.Count()}");
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
            return $"- Item {row["ItemIndex"]} / {row["Action"]}：{row["Status"]}，旧 `{row["OldAsset"]}`，新 `{row["NewAsset"]}`，图集 `{row["TargetAtlas"]}`，prefab `{row["PrefabRefs"]}`，确认 `{row["Confirmation"]}`，说明 `{row["Message"]}`";
        }

        static int StatusOrder(string status)
        {
            if (status == "Failed")
                return 0;
            if (status == "Skipped")
                return 1;
            if (status == "Applied")
                return 2;
            if (status == "Verified")
                return 3;
            return 9;
        }

        static void ValidateSections(string[] lines)
        {
            UIReportMarkdown.RequireExactSectionOrder("UI replacement host apply result summary", lines, "## 总览", "## 状态分布", "## 失败项", "## 执行项", "## 执行后复验");
        }

        static void RequireTitle(string[] lines, string title)
        {
            if (lines.Length == 0 || lines[0] != title)
                throw new Exception("UI replacement host apply result summary has unexpected title");
        }

        static void RequireLine(string[] lines, string line)
        {
            if (!lines.Contains(line))
                throw new Exception("UI replacement host apply result summary is missing: " + line);
        }

        static void WriteCsv(UIAIToolsProfile profile, IEnumerable<string> rows)
        {
            var path = UIReportFiles.GetPath(profile.logRoot, UIReportFiles.ReplacementHostApplyResult);
            File.WriteAllLines(path, new[] { UIReportFiles.ReplacementHostApplyResultHeader }.Concat(rows), new UTF8Encoding(true));
        }

        static void WritePlanCsv(UIAIToolsProfile profile, IEnumerable<string> rows)
        {
            var path = UIReportFiles.GetPath(profile.logRoot, UIReportFiles.ReplacementExecutionPlan);
            File.WriteAllLines(path, new[] { UIReportFiles.ReplacementExecutionPlanHeader }.Concat(rows), new UTF8Encoding(true));
        }

        static string Row(string itemIndex, string action, string status, string oldAsset, string newAsset, string targetAtlas, string prefabRefs, string confirmation, string message)
        {
            return string.Join(",", new[] { itemIndex, action, status, oldAsset, newAsset, targetAtlas, prefabRefs, confirmation, message }.Select(Csv));
        }

        static string PlanRow(string itemIndex, string action, string status, string oldAsset, string newAsset, string targetAtlas, string prefabRefs)
        {
            return string.Join(",", new[] { itemIndex, action, status, oldAsset, newAsset, targetAtlas, prefabRefs, "true", "manual", "" }.Select(Csv));
        }

        static string Csv(string value)
        {
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        static void ExpectFailure(UIAIToolsProfile profile, string name, string row, string expectedMessage)
        {
            WriteCsv(profile, row == null ? new string[0] : new[] { row });
            try
            {
                Validate(profile);
            }
            catch (Exception exception)
            {
                if (exception.Message.Contains(expectedMessage))
                    return;
                throw new Exception($"Unexpected host apply result contract failure for {name}: {exception.Message}");
            }
            throw new Exception("Host apply result contract sample did not fail: " + name);
        }

        static void ExpectSummaryFailure(UIAIToolsProfile profile, string name, string title, string expectedMessage)
        {
            var path = UIReportFiles.GetPath(profile.logRoot, UIReportFiles.ReplacementHostApplyResultSummary);
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
                throw new Exception($"Unexpected host apply result summary contract failure for {name}: {exception.Message}");
            }
            File.WriteAllLines(path, original, new UTF8Encoding(true));
            throw new Exception("Host apply result summary contract sample did not fail: " + name);
        }

        static void ExpectPlanFailure(UIAIToolsProfile profile, string row, string expectedMessage)
        {
            WriteCsv(profile, new[] { row });
            GenerateSummary(profile);
            try
            {
                ValidateAgainstExecutionPlan(profile);
            }
            catch (Exception exception)
            {
                if (exception.Message.Contains(expectedMessage))
                    return;
                throw new Exception($"Unexpected host apply result execution plan failure: {exception.Message}");
            }
            throw new Exception("Host apply result execution plan sample did not fail");
        }

        static void ExpectPlanCoverageFailure(UIAIToolsProfile profile, string expectedMessage)
        {
            WriteCsv(profile, new[]
            {
                Row("0", "ApplyPrefabReference", "Applied", "Assets/Old.png", "Assets/New.png", "Assets/Atlas.spriteatlasv2", "Assets/UI.prefab", "QA-1", "updated")
            });
            GenerateSummary(profile);
            try
            {
                ValidateAgainstExecutionPlan(profile);
            }
            catch (Exception exception)
            {
                if (exception.Message.Contains(expectedMessage))
                    return;
                throw new Exception($"Unexpected host apply result execution plan coverage failure: {exception.Message}");
            }
            throw new Exception("Host apply result execution plan coverage sample did not fail");
        }

        static void ExpectBlockingPlanFailure(UIAIToolsProfile profile, string expectedMessage)
        {
            WritePlanCsv(profile, new[]
            {
                PlanRow("0", "ConfirmNewAsset", "PendingAsset", "Assets/Old.png", "Assets/New.png", "Assets/Atlas.spriteatlasv2", "Assets/UI.prefab"),
                PlanRow("0", "ApplyPrefabReference", "PendingConfirmation", "Assets/Old.png", "Assets/New.png", "Assets/Atlas.spriteatlasv2", "Assets/UI.prefab"),
                PlanRow("0", "VerifyAfterApply", "PendingConfirmation", "Assets/Old.png", "Assets/New.png", "Assets/Atlas.spriteatlasv2", "Assets/UI.prefab")
            });
            WriteCsv(profile, new[]
            {
                Row("0", "ApplyPrefabReference", "Applied", "Assets/Old.png", "Assets/New.png", "Assets/Atlas.spriteatlasv2", "Assets/UI.prefab", "QA-1", "updated"),
                Row("0", "VerifyAfterApply", "Verified", "Assets/Old.png", "Assets/New.png", "Assets/Atlas.spriteatlasv2", "Assets/UI.prefab", "QA-1", "verified")
            });
            GenerateSummary(profile);
            try
            {
                ValidateAgainstExecutionPlan(profile);
            }
            catch (Exception exception)
            {
                if (exception.Message.Contains(expectedMessage))
                    return;
                throw new Exception($"Unexpected host apply result blocking plan failure: {exception.Message}");
            }
            throw new Exception("Host apply result blocking plan sample did not fail");
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Xipin.UIAITools
{
    public static class UIReplacementPlanDryRunService
    {
        static readonly HashSet<string> AllowedSeverities = new HashSet<string> { "Info", "Warning", "Review", "Error" };
        static readonly HashSet<string> AllowedStatuses = new HashSet<string> { "OK", "Missing", "Risk", "Review", "DuplicateName", "Skipped" };

        public static string Run(UIAIToolsProfile profile, string draftJsonPath)
        {
            UIReportValidationService.Validate(profile);
            var draft = UIRedesignDraftService.LoadDraft(draftJsonPath);
            var reuseRows = UIScanReportRows.ReadReuseIndex(profile);
            var detailRows = UIScanReportRows.ReadPrefabImageDetails(profile);
            var lines = new List<string> { UIReportFiles.ReplacementPlanDryRunHeader };
            for (int i = 0; i < draft.replacementPlan.items.Count; i++)
                AddItem(lines, profile, draft.replacementPlan.items[i], i, reuseRows, detailRows);

            var path = UIReportFiles.GetPath(profile.logRoot, UIReportFiles.ReplacementPlanDryRun);
            File.WriteAllLines(path, lines, new UTF8Encoding(true));
            ReadRows(profile);
            var summaryPath = GenerateSummary(profile);
            Debug.Log($"UI replacement plan dry-run generated: {path}, summary: {summaryPath}, {draft.replacementPlan.items.Count} items.");
            return path;
        }

        public static List<Dictionary<string, string>> ReadRows(UIAIToolsProfile profile)
        {
            UIReportValidationService.ValidateReport(profile, UIReportFiles.ReplacementPlanDryRun, UIReportFiles.ReplacementPlanDryRunHeader);
            var rows = UIReportCsv.ReadRows(profile.logRoot, UIReportFiles.ReplacementPlanDryRun);
            if (rows.Count == 0)
                throw new Exception("Invalid UI replacement plan dry-run: rows are required");
            foreach (var row in rows)
                ValidateRow(row);
            ValidateNoDuplicateRows(rows);
            return rows;
        }

        public static void ValidateContract()
        {
            var root = Path.Combine(Path.GetTempPath(), "UIAIToolsReplacementPlanDryRunContract_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                var profile = ScriptableObject.CreateInstance<UIAIToolsProfile>();
                profile.logRoot = root;
                WriteCsv(profile, new[]
                {
                    Row("0", "OldAssetExists", "Info", "OK", "Assets/Old.png", "Assets/New.png", "Assets/Atlas.spriteatlasv2", "通过", "Assets/Old.png")
                });
                ReadRows(profile);
                ExpectRowsFailure(profile, "duplicate_dry_run_row", new[]
                {
                    Row("0", "OldAssetExists", "Info", "OK", "Assets/Old.png", "Assets/New.png", "Assets/Atlas.spriteatlasv2", "通过", "Assets/Old.png"),
                    Row("0", "OldAssetExists", "Info", "OK", "Assets/Old.png", "Assets/New.png", "Assets/Atlas.spriteatlasv2", "通过", "Assets/Old.png")
                }, "duplicate dry-run row");
                ExpectRowsFailure(profile, "old_asset_path", new[]
                {
                    Row("0", "OldAssetExists", "Info", "OK", "Old.png", "Assets/New.png", "Assets/Atlas.spriteatlasv2", "通过", "Old.png")
                }, "OldAsset path is invalid");
                ExpectRowsFailure(profile, "new_asset_extension", new[]
                {
                    Row("0", "NewAssetExists", "Warning", "Missing", "Assets/Old.png", "Assets/New.jpg", "Assets/Atlas.spriteatlasv2", "新资源当前还不存在或未导入", "Assets/New.jpg")
                }, "NewAsset must be .png");
                ExpectRowsFailure(profile, "target_atlas_extension", new[]
                {
                    Row("0", "TargetAtlasExists", "Warning", "Missing", "Assets/Old.png", "Assets/New.png", "Assets/Atlas.png", "目标图集当前不存在", "Assets/Atlas.png")
                }, "TargetAtlas must be .spriteatlasv2");
            }
            finally
            {
                if (Directory.Exists(root))
                    Directory.Delete(root, true);
            }
            Debug.Log("UI replacement plan dry-run contract validation passed.");
        }

        public static string GenerateSummary(UIAIToolsProfile profile)
        {
            var rows = ReadRows(profile);
            var path = UIReportFiles.GetPath(profile.logRoot, UIReportFiles.ReplacementPlanDryRunSummary);
            var lines = new List<string>
            {
                "# UI 替换计划 DryRun 汇总",
                "",
                $"生成时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                "",
                "本文件由 dry-run 显式生成，只汇总 AI 替换计划检查结果，不移动资源、不覆盖 prefab、不修改图集或 YooAsset 配置。",
                "",
                "## 结果分布"
            };

            foreach (var group in rows.GroupBy(r => r["Severity"]).OrderBy(g => UIReplacementPlanStatus.SeverityOrder(g.Key)).ThenBy(g => g.Key))
                lines.Add($"- {group.Key}：{group.Count()}");
            lines.Add("");

            var errors = rows.Count(r => r["Severity"] == "Error");
            lines.Add("## Gate 状态");
            lines.Add($"- DryRun Error：{errors}");
            lines.Add($"- Gate：{(errors == 0 ? "通过" : "阻断")}");
            lines.Add("- Gate 规则：仅 Error 阻断；Warning 和 Review 进入人工确认。");
            lines.Add("");

            UIReportMarkdown.AddCheckSummary(lines, "警告项分布", rows.Where(r => r["Severity"] == "Warning").ToList());
            UIReportMarkdown.AddCheckSummary(lines, "复核项分布", rows.Where(r => r["Severity"] == "Review").ToList());
            AddTop(lines, "阻断项", rows.Where(r => r["Severity"] == "Error").ToList());
            AddTop(lines, "警告项", rows.Where(r => r["Severity"] == "Warning").ToList());
            AddTop(lines, "复核项", rows.Where(r => r["Severity"] == "Review").ToList());
            AddNextStep(lines, rows);

            File.WriteAllLines(path, lines, new UTF8Encoding(true));
            ValidateSummarySections(lines.ToArray());
            return path;
        }

        public static void ValidateNoErrors(UIAIToolsProfile profile)
        {
            var rows = ReadRows(profile);
            ValidateSummary(profile);
            var errors = rows.Where(r => r["Severity"] == "Error").ToList();
            if (errors.Count > 0)
                throw new Exception($"UI replacement plan dry-run has {errors.Count} error checks. See {UIReportFiles.GetPath(profile.logRoot, UIReportFiles.ReplacementPlanDryRunSummary)}");
            Debug.Log($"UI replacement plan dry-run gate passed: {rows.Count} checks, 0 errors.");
        }

        static void ValidateRow(Dictionary<string, string> row)
        {
            if (!int.TryParse(row["ItemIndex"], out _))
                throw new Exception("Invalid UI replacement plan dry-run: ItemIndex must be an integer");
            if (string.IsNullOrEmpty(row["Check"]))
                throw new Exception("Invalid UI replacement plan dry-run: Check is required");
            if (!AllowedSeverities.Contains(row["Severity"]))
                throw new Exception("Invalid UI replacement plan dry-run: invalid severity " + row["Severity"]);
            if (!AllowedStatuses.Contains(row["Status"]))
                throw new Exception("Invalid UI replacement plan dry-run: invalid status " + row["Status"]);
            if (string.IsNullOrEmpty(row["OldAsset"]) || string.IsNullOrEmpty(row["NewAsset"]) || string.IsNullOrEmpty(row["TargetAtlas"]))
                throw new Exception("Invalid UI replacement plan dry-run: asset paths are required");
            RequirePath(row["OldAsset"], "OldAsset", ".png");
            RequirePath(row["NewAsset"], "NewAsset", ".png");
            RequirePath(row["TargetAtlas"], "TargetAtlas", ".spriteatlasv2");
            if (string.IsNullOrEmpty(row["Message"]))
                throw new Exception("Invalid UI replacement plan dry-run: Message is required");
        }

        static void RequirePath(string path, string label, string extension)
        {
            if (!path.StartsWith("Assets/", StringComparison.Ordinal) || path.Contains("\\") || path.Contains("/../") || path.EndsWith("/..", StringComparison.Ordinal))
                throw new Exception($"Invalid UI replacement plan dry-run: {label} path is invalid");
            if (!path.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
                throw new Exception($"Invalid UI replacement plan dry-run: {label} must be {extension}");
        }

        static void ValidateNoDuplicateRows(List<Dictionary<string, string>> rows)
        {
            var duplicate = rows.GroupBy(row => new
            {
                ItemIndex = row["ItemIndex"],
                Check = row["Check"],
                OldAsset = row["OldAsset"],
                NewAsset = row["NewAsset"],
                TargetAtlas = row["TargetAtlas"]
            }).FirstOrDefault(group => group.Count() > 1);
            if (duplicate != null)
                throw new Exception($"Invalid UI replacement plan dry-run: duplicate dry-run row for Item {duplicate.Key.ItemIndex} / {duplicate.Key.Check}");
        }

        static void AddItem(List<string> lines, UIAIToolsProfile profile, UIReplacementItem item, int index, List<Dictionary<string, string>> reuseRows, List<Dictionary<string, string>> detailRows)
        {
            var reuse = reuseRows.FirstOrDefault(r => r["Path"] == item.oldAssetPath);
            AddPathCheck(lines, index, item, "OldAssetExists", AssetExists(item.oldAssetPath), "Error", "旧资源必须存在", item.oldAssetPath);
            AddPathCheck(lines, index, item, "NewAssetExists", AssetExists(item.newAssetPath), "Warning", "新资源当前还不存在或未导入", item.newAssetPath);
            AddPathCheck(lines, index, item, "TargetAtlasExists", AssetExists(item.targetAtlasPath), "Warning", "目标图集当前不存在", item.targetAtlasPath);
            AddStatus(lines, index, item, "TargetAtlasScope", StartsWithRoot(item.targetAtlasPath, profile.uiAtlasRoot) && item.targetAtlasPath.EndsWith(".spriteatlasv2", StringComparison.OrdinalIgnoreCase), "Review", "目标图集应位于 UIAtlas 且为 .spriteatlasv2", item.targetAtlasPath);
            AddStatus(lines, index, item, "NewAssetStaging", !StartsWithRoot(item.newAssetPath, "Assets/Bundle"), "Review", "AI 新图应先在设计或 AI 输出目录中确认", item.newAssetPath);
            AddReuseRisk(lines, index, item, reuse);
            AddPrefabReference(lines, index, item, detailRows);
            AddAddressRisk(lines, profile, index, item, reuseRows);
            AddStatus(lines, index, item, "RequiresConfirmation", item.requiresConfirmation, "Error", "替换项必须保持人工确认", "");
            AddStatus(lines, index, item, "PreserveGuid", !item.preserveGuid, "Review", "保持 GUID 需要单独人工确认", item.preserveGuid.ToString());
        }

        static void AddReuseRisk(List<string> lines, int index, UIReplacementItem item, Dictionary<string, string> reuse)
        {
            if (reuse == null)
            {
                AddLine(lines, index, item, "ReuseIndex", "Review", "Missing", "旧资源不在复用索引中", "");
                return;
            }

            var advice = reuse["Advice"];
            var risky = reuse["TextRefs"].Length > 0 || advice.Contains("按名加载") || advice.Contains("跨功能") || advice.Contains("多界面") || advice.Contains("同内容重复");
            AddLine(lines, index, item, "ReuseRisk", risky ? "Review" : "Info", risky ? "Risk" : "OK", advice, $"Owners={reuse["Owners"]};TextRefs={reuse["TextRefs"]};Reason={reuse["Reason"]}");
        }

        static void AddPrefabReference(List<string> lines, int index, UIReplacementItem item, List<Dictionary<string, string>> detailRows)
        {
            var prefabs = detailRows.Where(r => r["Image"] == item.oldAssetPath).Select(r => r["Prefab"]).Distinct().OrderBy(p => p).ToList();
            AddLine(lines, index, item, "PrefabReference", prefabs.Count == 0 ? "Review" : "Info", prefabs.Count == 0 ? "Missing" : "OK", prefabs.Count == 0 ? "旧资源当前没有 prefab 明细引用" : "旧资源存在 prefab 明细引用", Join(prefabs));
        }

        static void AddAddressRisk(List<string> lines, UIAIToolsProfile profile, int index, UIReplacementItem item, List<Dictionary<string, string>> reuseRows)
        {
            if (profile.yooAssetAddressRule != "AddressByFileName")
            {
                AddLine(lines, index, item, "YooAssetAddress", "Info", "Skipped", profile.yooAssetAddressRule, "");
                return;
            }

            var name = Path.GetFileNameWithoutExtension(item.newAssetPath);
            var duplicates = reuseRows.Where(r => r["Name"] == name && r["Path"] != item.oldAssetPath).Select(r => r["Path"]).OrderBy(p => p).ToList();
            AddLine(lines, index, item, "YooAssetAddress", duplicates.Count == 0 ? "Info" : "Review", duplicates.Count == 0 ? "OK" : "DuplicateName", duplicates.Count == 0 ? "AddressByFileName 未发现同名运行时资源" : "AddressByFileName 存在同名运行时资源", Join(duplicates));
        }

        static void AddPathCheck(List<string> lines, int index, UIReplacementItem item, string check, bool ok, string severity, string message, string evidence)
        {
            AddLine(lines, index, item, check, ok ? "Info" : severity, ok ? "OK" : "Missing", ok ? "通过" : message, evidence);
        }

        static void AddStatus(List<string> lines, int index, UIReplacementItem item, string check, bool ok, string severity, string message, string evidence)
        {
            AddLine(lines, index, item, check, ok ? "Info" : severity, ok ? "OK" : "Review", ok ? "通过" : message, evidence);
        }

        static void AddLine(List<string> lines, int index, UIReplacementItem item, string check, string severity, string status, string message, string evidence)
        {
            lines.Add(string.Join(",", new[]
            {
                index.ToString(),
                Csv(check),
                Csv(severity),
                Csv(status),
                Csv(item.oldAssetPath),
                Csv(item.newAssetPath),
                Csv(item.targetAtlasPath),
                Csv(message),
                Csv(evidence)
            }));
        }

        static bool AssetExists(string path)
        {
            return AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path) != null || File.Exists(path);
        }

        static bool StartsWithRoot(string path, string root)
        {
            path = Root(path);
            root = Root(root);
            return path.Equals(root, StringComparison.OrdinalIgnoreCase) || path.StartsWith(root + "/", StringComparison.OrdinalIgnoreCase);
        }

        static string Root(string path)
        {
            return (path ?? "").Replace('\\', '/').TrimEnd('/');
        }

        static string Csv(string value)
        {
            value = value ?? "";
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        static void WriteCsv(UIAIToolsProfile profile, IEnumerable<string> rows)
        {
            var path = UIReportFiles.GetPath(profile.logRoot, UIReportFiles.ReplacementPlanDryRun);
            File.WriteAllLines(path, new[] { UIReportFiles.ReplacementPlanDryRunHeader }.Concat(rows), new UTF8Encoding(true));
        }

        static string Row(string itemIndex, string check, string severity, string status, string oldAsset, string newAsset, string targetAtlas, string message, string evidence)
        {
            return string.Join(",", new[] { itemIndex, check, severity, status, oldAsset, newAsset, targetAtlas, message, evidence }.Select(Csv));
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
                throw new Exception($"Unexpected UI replacement plan dry-run contract failure for {name}: {exception.Message}");
            }
            throw new Exception("UI replacement plan dry-run contract sample did not fail: " + name);
        }

        static string Join(List<string> values)
        {
            return string.Join(";", values.Take(6)) + (values.Count > 6 ? ";..." : "");
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
                lines.Add($"- Item {row["ItemIndex"]} / {row["Check"]}：{row["Message"]}，旧 `{row["OldAsset"]}`，新 `{row["NewAsset"]}`，证据 `{row["Evidence"]}`");
            if (rows.Count > 20)
                lines.Add($"- 仅显示前 20 项，共 {rows.Count} 项。");
            lines.Add("");
        }

        static void AddNextStep(List<string> lines, List<Dictionary<string, string>> rows)
        {
            lines.Add("## 建议处理");
            if (rows.Any(r => r["Severity"] == "Error"))
                lines.Add("- 先修正 Error；存在阻断项时不要进入替换执行。");
            if (rows.Any(r => r["Severity"] == "Warning"))
                lines.Add("- Warning 通常表示新图或目标图集还未准备好，先补齐资源，再复跑 dry-run 和执行计划。");
            if (rows.Any(r => r["Severity"] == "Review"))
                lines.Add("- Review 需要人工确认复用归属、按名加载、跨功能借图、GUID 或目标图集策略。");
            if (rows.All(r => r["Severity"] == "Info"))
                lines.Add("- 当前 dry-run 没有发现阻断、警告或复核项，仍需人工确认视觉效果和最终执行范围。");
            lines.Add("");
        }

        static void ValidateSummary(UIAIToolsProfile profile)
        {
            var path = UIReportFiles.GetPath(profile.logRoot, UIReportFiles.ReplacementPlanDryRunSummary);
            if (!File.Exists(path))
                throw new Exception("Missing UI replacement plan dry-run summary: " + path);
            ValidateSummarySections(File.ReadAllLines(path));
        }

        static void ValidateSummarySections(string[] lines)
        {
            UIReportMarkdown.RequireExactSectionOrder("UI replacement plan dry-run summary", lines, "## 结果分布", "## Gate 状态", "## 警告项分布", "## 复核项分布", "## 阻断项", "## 警告项", "## 复核项", "## 建议处理");
        }

    }
}

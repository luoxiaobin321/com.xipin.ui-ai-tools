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

        public static void ValidateTargetPrefab(UIAIToolsProfile profile, string expectedTargetPrefab)
        {
            var rows = ReadRows(profile);
            ValidateSummary(profile, rows);
            var actualTargetPrefab = TargetPrefab(rows);
            if (actualTargetPrefab != expectedTargetPrefab)
                throw new Exception($"Invalid UI creation host generate result: TargetPrefab mismatch {actualTargetPrefab}->{expectedTargetPrefab}");
            Debug.Log($"UI creation host generate result target validation passed: {expectedTargetPrefab}, {rows.Count} rows.");
        }

        public static void ValidateAgainstLayoutDraft(UIAIToolsProfile profile, string layoutDraftJsonPath)
        {
            var draft = UILayoutDraftTemplateService.LoadDraft(layoutDraftJsonPath);
            var rows = ReadRows(profile);
            ValidateSummary(profile, rows);
            var expectedTargetPrefab = TargetPrefabPath(draft);
            var actualTargetPrefab = TargetPrefab(rows);
            if (actualTargetPrefab != expectedTargetPrefab)
                throw new Exception($"Invalid UI creation host generate result: TargetPrefab mismatch {actualTargetPrefab}->{expectedTargetPrefab}");
            ValidateDraftNodeRows(rows, draft);
            ValidateDraftLayoutRows(rows, draft);
            ValidateGenerateVerification(rows, expectedTargetPrefab);
            ValidatePrefabCreation(rows, expectedTargetPrefab);
            Debug.Log($"UI creation host generate result layout draft validation passed: {expectedTargetPrefab}, {draft.nodes.Count} nodes, {rows.Count} rows.");
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
                    Row("2", "ApplyLayout", "Applied", "Title", "builtin:Text", "Assets/Art/UI/AI/Demo/Demo.prefab", "", "", "QA-1", "layout"),
                    Row("3", "ApplyText", "Skipped", "Title", "builtin:Text", "Assets/Art/UI/AI/Demo/Demo.prefab", "", "", "", "empty"),
                    Row("4", "VerifyAfterGenerate", "Verified", "", "", "Assets/Art/UI/AI/Demo/Demo.prefab", "", "", "QA-1", "verified")
                });
                GenerateSummary(profile, "Demo", 2);
                Validate(profile);
                ValidateTargetPrefab(profile, "Assets/Art/UI/AI/Demo/Demo.prefab");
                var draftJsonPath = WriteDraftJson(profile, "Demo", "Assets/Art/UI/AI/Demo");
                ValidateAgainstLayoutDraft(profile, draftJsonPath);
                ExpectTargetFailure(profile, "Assets/Art/UI/AI/Other/Other.prefab", "TargetPrefab mismatch");
                ExpectDraftFailure(profile, WriteDraftJson(profile, "Other", "Assets/Art/UI/AI/Other"), "TargetPrefab mismatch");
                WriteCsv(profile, new[] { Row("0", "ApplyLayout", "Applied", "MissingNode", "builtin:Panel", "Assets/Art/UI/AI/Demo/Demo.prefab", "", "", "QA-1", "layout") });
                GenerateSummary(profile, "Demo", 1);
                ExpectDraftFailure(profile, draftJsonPath, "unknown NodeId");
                WriteCsv(profile, new[] { Row("0", "ApplyLayout", "Applied", "Root", "builtin:Text", "Assets/Art/UI/AI/Demo/Demo.prefab", "", "", "QA-1", "layout") });
                GenerateSummary(profile, "Demo", 1);
                ExpectDraftFailure(profile, draftJsonPath, "ComponentId mismatch");
                WriteCsv(profile, new[] { Row("0", "ApplyLayout", "Applied", "Root", "builtin:Panel", "Assets/Art/UI/AI/Demo/Demo.prefab", "", "", "QA-1", "layout") });
                GenerateSummary(profile, "Demo", 1);
                ExpectDraftFailure(profile, draftJsonPath, "missing NodeId");
                WriteCsv(profile, new[]
                {
                    Row("0", "ApplyLayout", "Applied", "Root", "builtin:Panel", "Assets/Art/UI/AI/Demo/Demo.prefab", "", "", "QA-1", "layout"),
                    Row("1", "ApplyText", "Applied", "Title", "builtin:Text", "Assets/Art/UI/AI/Demo/Demo.prefab", "", "", "QA-1", "title"),
                    Row("2", "VerifyAfterGenerate", "Verified", "", "", "Assets/Art/UI/AI/Demo/Demo.prefab", "", "", "QA-1", "verified")
                });
                GenerateSummary(profile, "Demo", 2);
                ExpectDraftFailure(profile, draftJsonPath, "missing ApplyLayout Title");
                WriteCsv(profile, new[]
                {
                    Row("0", "ApplyLayout", "Applied", "Root", "builtin:Panel", "Assets/Art/UI/AI/Demo/Demo.prefab", "", "", "QA-1", "layout"),
                    Row("1", "ApplyLayout", "Applied", "Title", "builtin:Text", "Assets/Art/UI/AI/Demo/Demo.prefab", "", "", "QA-1", "layout")
                });
                GenerateSummary(profile, "Demo", 2);
                ExpectDraftFailure(profile, draftJsonPath, "missing VerifyAfterGenerate");
                WriteCsv(profile, new[]
                {
                    Row("0", "ApplyLayout", "Applied", "Root", "builtin:Panel", "Assets/Art/UI/AI/Demo/Demo.prefab", "", "", "QA-1", "layout"),
                    Row("1", "ApplyLayout", "Applied", "Title", "builtin:Text", "Assets/Art/UI/AI/Demo/Demo.prefab", "", "", "QA-1", "layout"),
                    Row("2", "VerifyAfterGenerate", "Verified", "", "", "Assets/Art/UI/AI/Demo/Demo.prefab", "", "", "QA-1", "verified")
                });
                GenerateSummary(profile, "Demo", 2);
                ExpectDraftFailure(profile, draftJsonPath, "missing CreatePrefab");
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
                ExpectFailure(profile, "missing_skipped_message", Row("0", "ApplyText", "Skipped", "Title", "builtin:Text", "Assets/Demo.prefab", "", "", "", ""), "skipped message is required");
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
            if (row["Status"] == "Skipped" && string.IsNullOrEmpty(row["Message"]))
                throw new Exception("Invalid UI creation host generate result: skipped message is required");
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

        static string TargetPrefabPath(UILayoutDraft draft)
        {
            return (draft.root.targetFolder.TrimEnd('/', '\\') + "/" + draft.root.name + ".prefab").Replace('\\', '/');
        }

        static void ValidateDraftNodeRows(List<Dictionary<string, string>> rows, UILayoutDraft draft)
        {
            var nodes = draft.nodes.ToDictionary(n => n.nodeId);
            foreach (var row in rows.Where(r => !string.IsNullOrEmpty(r["NodeId"])))
            {
                if (!nodes.TryGetValue(row["NodeId"], out var node))
                    throw new Exception("Invalid UI creation host generate result: unknown NodeId " + row["NodeId"]);
                if (row["ComponentId"] != node.componentId)
                    throw new Exception($"Invalid UI creation host generate result: ComponentId mismatch {row["NodeId"]} {row["ComponentId"]}->{node.componentId}");
            }
            var resultNodeIds = rows.Where(r => !string.IsNullOrEmpty(r["NodeId"])).Select(r => r["NodeId"]).ToHashSet();
            foreach (var node in draft.nodes)
            {
                if (!resultNodeIds.Contains(node.nodeId))
                    throw new Exception("Invalid UI creation host generate result: missing NodeId " + node.nodeId);
            }
        }

        static void ValidateDraftLayoutRows(List<Dictionary<string, string>> rows, UILayoutDraft draft)
        {
            foreach (var node in draft.nodes)
            {
                var hasLayout = rows.Any(r => r["Action"] == "ApplyLayout" && r["Status"] == "Applied" && r["NodeId"] == node.nodeId && r["ComponentId"] == node.componentId);
                if (!hasLayout)
                    throw new Exception("Invalid UI creation host generate result: missing ApplyLayout " + node.nodeId);
            }
        }

        static void ValidateGenerateVerification(List<Dictionary<string, string>> rows, string targetPrefab)
        {
            var verified = rows.Any(r => r["Action"] == "VerifyAfterGenerate" && r["Status"] == "Verified" && r["TargetPrefab"] == targetPrefab);
            if (!verified)
                throw new Exception("Invalid UI creation host generate result: missing VerifyAfterGenerate");
        }

        static void ValidatePrefabCreation(List<Dictionary<string, string>> rows, string targetPrefab)
        {
            var created = rows.Any(r => r["Action"] == "CreatePrefab" && r["Status"] == "Applied" && r["TargetPrefab"] == targetPrefab);
            if (!created)
                throw new Exception("Invalid UI creation host generate result: missing CreatePrefab");
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

        static string WriteDraftJson(UIAIToolsProfile profile, string name, string targetFolder)
        {
            var path = UIReportFiles.GetPath(profile.logRoot, "UICreationHostGenerateResultContractDraft_" + name + ".json");
            var json = "{\"root\":{\"name\":\"" + name + "\",\"uiType\":\"Dialog\",\"targetFolder\":\"" + targetFolder + "\",\"referenceResolution\":\"1080x1920\",\"safeAreaPolicy\":\"\"},\"nodes\":[{\"nodeId\":\"Root\",\"parentId\":\"\",\"name\":\"Root\",\"componentRole\":\"Panel\",\"componentId\":\"builtin:Panel\",\"anchor\":\"stretch_full\",\"position\":\"0,0\",\"size\":\"1080x1920\"},{\"nodeId\":\"Title\",\"parentId\":\"Root\",\"name\":\"Title\",\"componentRole\":\"Text\",\"componentId\":\"builtin:Text\",\"anchor\":\"top_center\",\"position\":\"0,-80\",\"size\":\"520x80\"}],\"assets\":[],\"interactions\":[],\"risks\":[],\"requiresConfirmation\":true}";
            File.WriteAllText(path, json, new UTF8Encoding(true));
            return path;
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

        static void ExpectTargetFailure(UIAIToolsProfile profile, string targetPrefab, string expectedMessage)
        {
            try
            {
                ValidateTargetPrefab(profile, targetPrefab);
            }
            catch (Exception exception)
            {
                if (exception.Message.Contains(expectedMessage))
                    return;
                throw new Exception($"Unexpected UI creation host generate result target contract failure: {exception.Message}");
            }
            throw new Exception("UI creation host generate result target contract sample did not fail");
        }

        static void ExpectDraftFailure(UIAIToolsProfile profile, string draftJsonPath, string expectedMessage)
        {
            try
            {
                ValidateAgainstLayoutDraft(profile, draftJsonPath);
            }
            catch (Exception exception)
            {
                if (exception.Message.Contains(expectedMessage))
                    return;
                throw new Exception($"Unexpected UI creation host generate result layout draft contract failure: {exception.Message}");
            }
            throw new Exception("UI creation host generate result layout draft contract sample did not fail");
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

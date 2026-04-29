using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Xipin.UIAITools
{
    public static class UICreationHostGenerateChecklistService
    {
        public static string Generate(UIAIToolsProfile profile, string layoutDraftJsonPath)
        {
            UIComponentCandidateIndexService.Validate(profile);
            var draft = UILayoutDraftTemplateService.LoadDraft(layoutDraftJsonPath);
            var targetPrefab = TargetPrefabPath(draft);
            var rows = UICreationLayoutDryRunService.ReadRows(profile);
            var dryRunErrors = rows.Where(r => r["Severity"] == "Error").ToList();
            var errors = new List<Dictionary<string, string>>(dryRunErrors);
            var componentReviews = ComponentReviewRows(profile, draft);
            var componentReviewErrors = ComponentReviewErrors(componentReviews);
            errors.AddRange(componentReviewErrors);
            var warnings = rows.Where(r => r["Severity"] == "Warning").ToList();
            var assetNeedsReady = rows.Where(r => r["Check"] == "AssetNeed").All(r => r["Status"] == "Ready");
            var dryRunTargetRows = rows.Where(r => r["Check"] == "TargetPrefab").ToList();
            var dryRunTargetMatches = dryRunTargetRows.Any(r => r["Evidence"] == targetPrefab);
            if (!dryRunTargetMatches)
                errors.Add(DryRunTargetMismatchRow(dryRunTargetRows, targetPrefab));
            var targetPrefabClear = dryRunTargetRows.Any(r => r["Status"] == "OK" && r["Evidence"] == targetPrefab);
            var draftComponentIds = DraftComponentIds(draft);
            var dryRunComponentIds = CurrentDryRunComponentIds(rows);
            var dryRunComponentsMatch = SameValues(draftComponentIds, dryRunComponentIds);
            if (!dryRunComponentsMatch)
                errors.Add(DryRunComponentMismatchRow(dryRunComponentIds, draftComponentIds));
            var path = UIReportFiles.GetPath(profile.logRoot, UIReportFiles.CreationHostGenerateChecklist);
            var lines = new List<string>
            {
                "# UI 生成宿主确认清单",
                "",
                $"生成时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                "",
                "本文件只整理宿主 prefab 生成前确认项，不创建 prefab、不复制图片、不修改图集。",
                "",
                $"Gate：{(errors.Count == 0 ? "Passed" : "Blocked")}",
                "",
                "## 目标",
                $"- UI：`{draft.root.name}`",
                $"- 类型：`{draft.root.uiType}`",
                $"- 目录：`{draft.root.targetFolder}`",
                $"- 建议 prefab：`{targetPrefab}`",
                $"- 节点：{draft.nodes.Count}",
                $"- 资源需求：{draft.assets.Count}",
                $"- 交互：{Join(draft.interactions)}",
                "",
                "## 阻断项"
            };

            AddRows(lines, errors);
            lines.Add("");
            lines.Add("## 人工复核项");
            AddRows(lines, warnings);
            lines.Add("");
            lines.Add("## 组件候选确认");
            AddComponentReviews(lines, componentReviews);
            lines.Add("");
            lines.Add("## 宿主生成前确认");
            lines.Add(dryRunErrors.Count == 0 ? "- 布局 dry-run gate 已通过。" : "- 布局 dry-run gate 未通过，宿主生成器不得运行。");
            lines.Add(dryRunTargetMatches ? "- 当前 dry-run 目标与本次草稿一致。" : "- 当前 dry-run 目标与本次草稿不一致，需先重跑布局 dry-run。");
            lines.Add(dryRunComponentsMatch ? "- 当前 dry-run 组件列表与本次草稿一致。" : "- 当前 dry-run 组件列表与本次草稿不一致，需先重跑布局 dry-run。");
            lines.Add(componentReviewErrors.Count == 0 ? "- 布局引用的组件候选均已 `Approved`。" : "- 存在未 `Approved` 的组件候选，需先填写 `UIComponentCandidateReview.csv`。");
            lines.Add(assetNeedsReady ? "- 资源需求全部为 `Ready`。" : "- 资源需求未全部 `Ready`，需先补齐。");
            lines.Add(targetPrefabClear ? "- 目标 prefab 路径不会覆盖现有资源。" : "- 目标 prefab 路径存在阻断或未通过确认。");
            lines.Add("- 生成动作由宿主项目执行，并记录生成结果报告。");
            lines.Add("");
            lines.Add("## 宿主生成器允许动作");
            lines.Add("- 在确认后的目标目录创建 prefab 草稿。");
            lines.Add("- 实例化已确认组件，写入布局、文本、资源引用和数据绑定占位。");
            lines.Add("- 输出宿主侧生成结果报告和后续验收项。");
            lines.Add("");
            lines.Add("## 宿主生成器禁止动作");
            lines.Add("- 覆盖已有 prefab。");
            lines.Add("- 移动、删除或覆盖图片资源。");
            lines.Add("- 修改 SpriteAtlas、YooAsset 或业务运行时配置。");
            lines.Add("");
            lines.Add("## 生成后验证");
            lines.Add("- 打开 prefab 人工检查层级、锚点、尺寸和文本长度。");
            lines.Add("- 重新跑扫描或面板实测清单，确认 DrawCall 和资源引用风险。");
            lines.Add("- 保存宿主生成结果报告，供后续人工确认或回滚。");
            File.WriteAllLines(path, lines, new UTF8Encoding(true));
            ValidateSections(lines.ToArray());
            Debug.Log($"UI creation host generate checklist generated: {path}");
            return path;
        }

        public static void ValidateNoBlockingSteps(UIAIToolsProfile profile)
        {
            UIComponentCandidateIndexService.Validate(profile);
            var rows = UICreationLayoutDryRunService.ReadRows(profile);
            var errors = rows.Count(r => r["Severity"] == "Error");
            if (errors > 0)
                throw new Exception($"UI creation host generate checklist has blocking steps: {errors}");
            var checklistPath = UIReportFiles.GetPath(profile.logRoot, UIReportFiles.CreationHostGenerateChecklist);
            if (!File.Exists(checklistPath))
                throw new Exception($"Missing UI creation host generate checklist: {checklistPath}");
            var checklistLines = File.ReadAllLines(checklistPath);
            ValidateSections(checklistLines);
            var checklist = string.Join("\n", checklistLines);
            if (checklist.Contains("Gate：Blocked"))
                throw new Exception("UI creation host generate checklist gate is blocked.");
            if (!checklist.Contains("Gate：Passed"))
                throw new Exception("UI creation host generate checklist gate is missing or invalid.");
            ValidateCurrentTarget(rows, checklist);
            var componentIds = CurrentDryRunComponentIds(rows);
            ValidateChecklistComponents(checklist, componentIds);
            ValidateCurrentComponentReviews(profile, componentIds);
            Debug.Log("UI creation host generate checklist validation passed.");
        }

        static void ValidateCurrentTarget(List<Dictionary<string, string>> dryRunRows, string checklist)
        {
            var target = dryRunRows.FirstOrDefault(r => r["Check"] == "TargetPrefab" && r["Status"] == "OK");
            if (target == null)
                throw new Exception("UI creation host generate checklist target is missing from current dry-run.");
            if (ChecklistTarget(checklist) != target["Evidence"])
                throw new Exception("UI creation host generate checklist target does not match current dry-run.");
        }

        static string ChecklistTarget(string checklist)
        {
            const string prefix = "- 建议 prefab：`";
            foreach (var line in checklist.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None))
            {
                if (!line.StartsWith(prefix, StringComparison.Ordinal) || !line.EndsWith("`", StringComparison.Ordinal))
                    continue;
                return line.Substring(prefix.Length, line.Length - prefix.Length - 1);
            }
            return "";
        }

        static List<string> CurrentDryRunComponentIds(List<Dictionary<string, string>> dryRunRows)
        {
            return dryRunRows.Where(r => r["Check"] == "NodeComponentId" && r["Status"] == "OK")
                .Select(DryRunComponentId)
                .Distinct()
                .OrderBy(id => id)
                .ToList();
        }

        static string DryRunComponentId(Dictionary<string, string> row)
        {
            var evidence = row["Evidence"];
            var end = evidence.IndexOf(' ');
            return end > 0 ? evidence.Substring(0, end) : evidence;
        }

        static List<string> DraftComponentIds(UILayoutDraft draft)
        {
            return draft.nodes.Where(n => !string.IsNullOrEmpty(n.componentId))
                .Select(n => n.componentId)
                .Distinct()
                .OrderBy(id => id)
                .ToList();
        }

        static bool SameValues(List<string> left, List<string> right)
        {
            return left.Count == right.Count && !left.Where((value, index) => value != right[index]).Any();
        }

        static void ValidateChecklistComponents(string checklist, List<string> componentIds)
        {
            var checklistIds = checklist.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
                .Select(ChecklistComponentId)
                .Where(id => !string.IsNullOrEmpty(id))
                .Distinct()
                .OrderBy(id => id)
                .ToList();
            var missing = componentIds.Where(id => !checklistIds.Contains(id)).ToList();
            var stale = checklistIds.Where(id => !componentIds.Contains(id)).ToList();
            if (missing.Count > 0 || stale.Count > 0)
                throw new Exception($"UI creation host generate checklist component list does not match current dry-run: missing {Join(missing)} stale {Join(stale)}");
        }

        static string ChecklistComponentId(string line)
        {
            if (!line.StartsWith("- Component", StringComparison.Ordinal))
                return "";
            var end = line.IndexOf(" /", StringComparison.Ordinal);
            if (end <= 2)
                return "";
            var id = line.Substring(2, end - 2);
            return id.Length == 17 && id.StartsWith("Component", StringComparison.Ordinal) ? id : "";
        }

        static void ValidateCurrentComponentReviews(UIAIToolsProfile profile, List<string> componentIds)
        {
            var reviews = UIReportCsv.ReadRows(profile.logRoot, UIReportFiles.ComponentCandidateReview).ToDictionary(r => r["ComponentId"]);
            var unapproved = new List<string>();
            foreach (var id in componentIds)
            {
                if (!reviews.TryGetValue(id, out var review) || review["SuggestedDecision"] != "Approved")
                    unapproved.Add(id);
            }

            if (unapproved.Count > 0)
                throw new Exception($"UI creation host generate checklist has unapproved component candidates: {string.Join(";", unapproved)}");
        }

        static List<Dictionary<string, string>> ComponentReviewRows(UIAIToolsProfile profile, UILayoutDraft draft)
        {
            var reviews = UIReportCsv.ReadRows(profile.logRoot, UIReportFiles.ComponentCandidateReview).ToDictionary(r => r["ComponentId"]);
            var rows = new List<Dictionary<string, string>>();
            foreach (var group in draft.nodes.Where(n => !string.IsNullOrEmpty(n.componentId)).GroupBy(n => n.componentId))
            {
                if (!reviews.TryGetValue(group.Key, out var review))
                {
                    rows.Add(ComponentReviewRow(group.Key, "Missing", "", "", "", "", group.Select(n => n.name).ToList()));
                    continue;
                }

                rows.Add(ComponentReviewRow(group.Key, review["SuggestedDecision"], review["Role"], review["ReviewTier"], review["ImageAsset"], review["ComponentPrefabPath"], group.Select(n => n.name).ToList()));
            }
            return rows;
        }

        static List<Dictionary<string, string>> ComponentReviewErrors(List<Dictionary<string, string>> reviews)
        {
            var errors = new List<Dictionary<string, string>>();
            foreach (var review in reviews.Where(r => r["Status"] != "Approved"))
                errors.Add(ComponentReviewError(review["Status"], review["Status"] == "Missing" ? "组件候选缺少人工确认记录" : "组件候选未 Approved，宿主生成器不得实例化", review["ComponentId"], review["Nodes"]));
            return errors;
        }

        static Dictionary<string, string> ComponentReviewRow(string componentId, string status, string role, string reviewTier, string imageAsset, string componentPrefabPath, List<string> nodes)
        {
            return new Dictionary<string, string>
            {
                { "ComponentId", componentId },
                { "Status", status },
                { "Role", role },
                { "ReviewTier", reviewTier },
                { "ImageAsset", imageAsset },
                { "ComponentPrefabPath", componentPrefabPath },
                { "Nodes", Join(nodes) }
            };
        }

        static Dictionary<string, string> ComponentReviewError(string status, string message, string componentId, string nodes)
        {
            return new Dictionary<string, string>
            {
                { "Check", "ComponentCandidateReview" },
                { "Severity", "Error" },
                { "Status", status },
                { "Message", message },
                { "Evidence", $"{componentId} {nodes}" }
            };
        }

        static void AddComponentReviews(List<string> lines, List<Dictionary<string, string>> reviews)
        {
            if (reviews.Count == 0)
            {
                lines.Add("- 无");
                return;
            }

            foreach (var row in reviews)
                lines.Add($"- {row["ComponentId"]} / {row["Status"]} / {DisplayValue(row["Role"])} / {DisplayValue(row["ReviewTier"])}：节点 `{row["Nodes"]}`，资源 `{DisplayValue(row["ImageAsset"])}`，组件 prefab `{DisplayValue(row["ComponentPrefabPath"])}`");
        }

        static string DisplayValue(string value)
        {
            return string.IsNullOrEmpty(value) ? "未填写" : value;
        }

        static Dictionary<string, string> DryRunTargetMismatchRow(List<Dictionary<string, string>> rows, string targetPrefab)
        {
            var dryRunTarget = rows.Count == 0 ? "Missing" : string.Join(";", rows.Select(r => r["Evidence"]));
            return new Dictionary<string, string>
            {
                { "Check", "DryRunTarget" },
                { "Severity", "Error" },
                { "Status", "Mismatch" },
                { "Message", "当前 dry-run 目标与本次草稿目标不一致，需重跑布局 dry-run" },
                { "Evidence", $"{dryRunTarget}->{targetPrefab}" }
            };
        }

        static Dictionary<string, string> DryRunComponentMismatchRow(List<string> dryRunComponentIds, List<string> draftComponentIds)
        {
            return new Dictionary<string, string>
            {
                { "Check", "DryRunComponents" },
                { "Severity", "Error" },
                { "Status", "Mismatch" },
                { "Message", "当前 dry-run 组件列表与本次草稿不一致，需重跑布局 dry-run" },
                { "Evidence", $"{Join(dryRunComponentIds)}->{Join(draftComponentIds)}" }
            };
        }

        static void AddRows(List<string> lines, List<Dictionary<string, string>> rows)
        {
            if (rows.Count == 0)
            {
                lines.Add("- 无");
                return;
            }

            foreach (var row in rows.Take(30))
                lines.Add($"- {row["Check"]} / {row["Status"]}：{row["Message"]}，`{row["Evidence"]}`");
        }

        static string Join(List<string> values)
        {
            return values.Count == 0 ? "" : string.Join(";", values);
        }

        static string TargetPrefabPath(UILayoutDraft draft)
        {
            var targetFolder = draft.root.targetFolder ?? "";
            return $"{targetFolder.TrimEnd('/')}/{draft.root.name}.prefab";
        }

        static void ValidateSections(string[] lines)
        {
            UIReportMarkdown.RequireExactSectionOrder("UI creation host generate checklist", lines, "## 目标", "## 阻断项", "## 人工复核项", "## 组件候选确认", "## 宿主生成前确认", "## 宿主生成器允许动作", "## 宿主生成器禁止动作", "## 生成后验证");
        }
    }
}

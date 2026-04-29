using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Xipin.UIAITools
{
    public static class UIRedesignPackageService
    {
        public static string Prepare(UIAIToolsProfile profile, UIRedesignRequest request)
        {
            var brief = UIRedesignBriefService.GenerateBrief(profile, request);
            var template = UIRedesignDraftTemplateService.Generate(profile, request);
            return PrepareFromDraft(profile, request, brief, template, "");
        }

        public static string Prepare(UIAIToolsProfile profile, UIRedesignRequest request, UIRedesignDraft draft)
        {
            var brief = UIRedesignBriefService.GenerateBrief(profile, request);
            var draftJson = UIRedesignDraftService.SaveDraft(profile, request, draft);
            return PrepareFromDraft(profile, request, brief, draftJson, "");
        }

        public static string PrepareFromDraftJson(UIAIToolsProfile profile, UIRedesignRequest request, string draftJsonPath)
        {
            var brief = UIRedesignBriefService.GenerateBrief(profile, request);
            var draft = UIRedesignDraftService.LoadDraft(draftJsonPath);
            var draftJson = UIRedesignDraftService.SaveDraftSnapshot(profile, request, draft, draftJsonPath);
            return PrepareFromDraft(profile, request, brief, draftJson, draftJsonPath);
        }

        static string PrepareFromDraft(UIAIToolsProfile profile, UIRedesignRequest request, string brief, string draftJson, string sourceDraftJson)
        {
            UIReplacementPlanDryRunService.Run(profile, draftJson);
            UIReplacementPlanDryRunService.ValidateNoErrors(profile);
            var executionPlan = UIReplacementExecutionPlanService.GenerateFromCurrentDryRun(profile, draftJson);
            var pendingInputs = UIReplacementPendingInputChecklistService.Generate(profile);
            var pendingInputReadiness = UIReplacementPendingInputReadinessService.Generate(profile);
            var externalInputPackage = UIReplacementExternalInputPackageService.Generate(profile, request, brief, draftJson);
            var hostApplyChecklist = UIReplacementHostApplyChecklistService.Generate(profile);
            var manifest = GenerateManifest(profile, request, brief, draftJson, sourceDraftJson, executionPlan, pendingInputs, pendingInputReadiness, externalInputPackage, hostApplyChecklist);
            ValidateManifest(profile, request);
            Debug.Log($"UI redesign package prepared: manifest {manifest}, brief {brief}, draft {draftJson}, execution plan {executionPlan}, pending inputs {pendingInputs}, pending input readiness {pendingInputReadiness}, external input package {externalInputPackage}, host apply checklist {hostApplyChecklist}");
            return manifest;
        }

        public static void ValidateManifest(UIAIToolsProfile profile, UIRedesignRequest request)
        {
            var path = UIReportFiles.GetPath(profile.logRoot, $"UIRedesignPackage_{SafeName(request.sourcePrefabPath)}.md");
            var lines = File.ReadAllLines(path);
            ValidateManifestSections(lines);
            ValidateManifestSourceReports(profile);
            RequireLine(lines, $"- sourcePrefabPath：`{request.sourcePrefabPath}`");
            RequireExistingFile(ManifestPath(lines, "- sourcePreviewPath："), "source preview");
            RequireExistingFile(ManifestPath(lines, "- Brief："), "brief");
            RequireExistingFile(ManifestPath(lines, "- 草稿 JSON："), "draft JSON");
            RequireManifestPath(lines, "- DryRun 明细：", UIReportFiles.GetPath(profile.logRoot, UIReportFiles.ReplacementPlanDryRun), "dry-run CSV");
            RequireManifestPath(lines, "- DryRun 汇总：", UIReportFiles.GetPath(profile.logRoot, UIReportFiles.ReplacementPlanDryRunSummary), "dry-run summary");
            RequireManifestPath(lines, "- 待确认执行计划：", UIReportFiles.GetPath(profile.logRoot, UIReportFiles.ReplacementExecutionPlan), "execution plan");
            RequireManifestPath(lines, "- 执行计划汇总：", UIReportFiles.GetPath(profile.logRoot, UIReportFiles.ReplacementExecutionPlanSummary), "execution plan summary");
            RequireManifestPath(lines, "- 待补输入 CSV：", UIReportFiles.GetPath(profile.logRoot, UIReportFiles.ReplacementPendingInputs), "pending inputs");
            RequireManifestPath(lines, "- 待补输入汇总：", UIReportFiles.GetPath(profile.logRoot, UIReportFiles.ReplacementPendingInputsSummary), "pending inputs summary");
            RequireManifestPath(lines, "- 待补输入就绪 CSV：", UIReportFiles.GetPath(profile.logRoot, UIReportFiles.ReplacementPendingInputReadiness), "pending input readiness");
            RequireManifestPath(lines, "- 待补输入就绪汇总：", UIReportFiles.GetPath(profile.logRoot, UIReportFiles.ReplacementPendingInputReadinessSummary), "pending input readiness summary");
            RequireManifestPath(lines, "- 外部生成输入包：", UIReportFiles.GetPath(profile.logRoot, UIReportFiles.ReplacementExternalInputPackage), "external input package");
            RequireManifestPath(lines, "- 外部生成输入包汇总：", UIReportFiles.GetPath(profile.logRoot, UIReportFiles.ReplacementExternalInputPackageSummary), "external input package summary");
            RequireManifestPath(lines, "- 外部生成任务清单：", UIReportFiles.GetPath(profile.logRoot, UIReportFiles.ReplacementExternalGenerationTasks), "external generation tasks");
            RequireManifestPath(lines, "- 外部生成 Prompt Pack：", UIReportFiles.GetPath(profile.logRoot, UIReportFiles.ReplacementExternalPromptPack), "external prompt pack");
            RequireManifestPath(lines, "- 外部生成单项 Prompt 清单：", UIReportFiles.GetPath(profile.logRoot, UIReportFiles.ReplacementExternalPromptItemList), "external prompt item list");
            RequireManifestPath(lines, "- 外部生成引用素材清单：", UIReportFiles.GetPath(profile.logRoot, UIReportFiles.ReplacementExternalReferenceCopyList), "external reference copy list");
            RequireManifestPath(lines, "- 宿主执行清单：", UIReportFiles.GetPath(profile.logRoot, UIReportFiles.ReplacementHostApplyChecklist), "host apply checklist");
            UIReplacementPendingInputChecklistService.Validate(profile);
            UIReplacementPendingInputReadinessService.Validate(profile);
            UIReplacementHostApplyChecklistService.Validate(profile);
            UIReplacementExternalInputPackageService.Validate(profile, request, ManifestPath(lines, "- Brief："), ManifestPath(lines, "- 草稿 JSON："));

            var dryRunRows = UIReplacementPlanDryRunService.ReadRows(profile);
            var executionRows = UIReplacementExecutionPlanService.ReadRows(profile);
            var readinessRows = UIReplacementPendingInputReadinessService.ReadRows(profile);
            RequireDistribution(lines, dryRunRows, "Severity", UIReplacementPlanStatus.SeverityOrder);
            RequireDistribution(lines, executionRows, "Status", UIReplacementPlanStatus.StatusOrder);
            var dryRunErrors = dryRunRows.Count(r => r["Severity"] == "Error");
            var blockingRows = executionRows.Where(r => UIReplacementPlanStatus.IsBlocking(r["Status"])).ToList();
            var missingInputs = readinessRows.Count(r => r["Readiness"] == "Missing");
            var invalidInputs = readinessRows.Count(r => r["Readiness"] == "Invalid");
            var notReadyInputs = readinessRows.Count(r => r["Readiness"] != "Ready");
            RequireLine(lines, $"- DryRun Error：{dryRunErrors}");
            RequireLine(lines, $"- DryRun Gate：{(dryRunErrors == 0 ? "通过" : "阻断")}");
            RequireLine(lines, $"- 执行计划阻断步骤：{blockingRows.Count}");
            RequireLine(lines, $"- 执行计划 Gate：{(blockingRows.Count == 0 ? "通过" : "阻断")}");
            RequireLine(lines, $"- 待补输入缺失：{missingInputs}");
            RequireLine(lines, $"- 待补输入异常：{invalidInputs}");
            RequireLine(lines, $"- 待补输入未就绪：{notReadyInputs}");
            RequireLine(lines, $"- 待补输入 Gate：{(notReadyInputs == 0 ? "通过" : "阻断")}");
            if (blockingRows.Count > 0)
                RequireLine(lines, $"- 阻断状态：{UIReplacementPlanStatus.Summary(blockingRows)}");
            var pendingPreview = executionRows.FirstOrDefault(r => r["Status"] == "PendingPreview");
            RequireLine(lines, pendingPreview == null ? "- 新版预览：无" : $"- 新版预览：`{pendingPreview["NewAsset"]}`");
            RequireLine(lines, $"- 新图：{executionRows.Count(r => r["Action"] == "ConfirmNewAsset" && r["Status"] == "PendingAsset")}");
            RequireLine(lines, $"- 目标图集：{executionRows.Where(r => r["Action"] == "ConfirmTargetAtlas" && r["Status"] == "PendingAtlas").Select(r => r["TargetAtlas"]).Distinct().Count()}");
            Debug.Log($"UI redesign package manifest validation passed: {path}");
        }

        public static void ValidateContract()
        {
            UIRedesignRequestValidation.ValidateOutputFolder("");
            UIRedesignRequestValidation.ValidateOutputFolder("Assets/Art/UI/AI/Demo");
            ExpectFailure("output_folder_relative", "Assets/ path", () => UIRedesignRequestValidation.ValidateOutputFolder("Art/UI/AI/Demo"));
            ExpectFailure("output_folder_backslash", "Assets/ path", () => UIRedesignRequestValidation.ValidateOutputFolder("Assets\\Art\\UI"));
            ExpectFailure("output_folder_parent_segment", "cannot contain ..", () => UIRedesignRequestValidation.ValidateOutputFolder("Assets/Art/../UI"));
            ExpectFailure("output_folder_trailing_parent", "cannot contain ..", () => UIRedesignRequestValidation.ValidateOutputFolder("Assets/Art/UI/.."));
            var root = Path.Combine(Path.GetTempPath(), "UIAIToolsRedesignRequestContract_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                var profile = ScriptableObject.CreateInstance<UIAIToolsProfile>();
                profile.logRoot = root;
                WritePrefabOptimizationTargets(profile, "Assets/Bundle/Prefab/Valid.prefab");
                UIRedesignRequestValidation.ValidateSourcePrefab(profile, "Assets/Bundle/Prefab/Valid.prefab", "contract");
                ExpectFailure("source_prefab_missing", "Missing source prefab path", () => UIRedesignRequestValidation.ValidateSourcePrefab(profile, "", "contract"));
                ExpectFailure("source_prefab_relative", "Assets/ prefab path", () => UIRedesignRequestValidation.ValidateSourcePrefab(profile, "Bundle/Prefab/Valid.prefab", "contract"));
                ExpectFailure("source_prefab_backslash", "Assets/ prefab path", () => UIRedesignRequestValidation.ValidateSourcePrefab(profile, "Assets\\Bundle\\Prefab\\Valid.prefab", "contract"));
                ExpectFailure("source_prefab_extension", "Assets/ prefab path", () => UIRedesignRequestValidation.ValidateSourcePrefab(profile, "Assets/Bundle/Prefab/Valid.png", "contract"));
                ExpectFailure("source_prefab_parent_segment", "cannot contain ..", () => UIRedesignRequestValidation.ValidateSourcePrefab(profile, "Assets/Bundle/../Prefab/Valid.prefab", "contract"));
                ExpectFailure("source_prefab_not_scanned", "not present in scan reports", () => UIRedesignRequestValidation.ValidateSourcePrefab(profile, "Assets/Bundle/Prefab/Missing.prefab", "contract"));
            }
            finally
            {
                Directory.Delete(root, true);
            }
            Debug.Log("UI redesign package contract validation passed.");
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
                throw new Exception($"Unexpected UI redesign package contract failure for {name}: {exception.Message}");
            }
            throw new Exception("UI redesign package contract sample did not fail: " + name);
        }

        static void WritePrefabOptimizationTargets(UIAIToolsProfile profile, string prefab)
        {
            var row = string.Join(",", new[]
            {
                prefab,
                "Owner",
                "1",
                "Issue",
                "Next",
                "1",
                "1",
                "0",
                "0",
                "0",
                "0",
                "0",
                "0",
                "1",
                "0",
                "0"
            });
            File.WriteAllLines(UIReportFiles.GetPath(profile.logRoot, UIReportFiles.PrefabOptimizationTargets), new[] { UIReportFiles.PrefabOptimizationTargetsHeader, row }, new UTF8Encoding(true));
        }

        static string GenerateManifest(UIAIToolsProfile profile, UIRedesignRequest request, string brief, string draftJson, string sourceDraftJson, string executionPlan, string pendingInputs, string pendingInputReadiness, string externalInputPackage, string hostApplyChecklist)
        {
            ValidateManifestSourceReports(profile);
            var dryRun = UIReportFiles.GetPath(profile.logRoot, UIReportFiles.ReplacementPlanDryRun);
            var dryRunSummary = UIReportFiles.GetPath(profile.logRoot, UIReportFiles.ReplacementPlanDryRunSummary);
            var executionPlanSummary = UIReportFiles.GetPath(profile.logRoot, UIReportFiles.ReplacementExecutionPlanSummary);
            var rows = UIReplacementPlanDryRunService.ReadRows(profile);
            var executionRows = UIReplacementExecutionPlanService.ReadRows(profile);
            var readinessRows = UIReplacementPendingInputReadinessService.ReadRows(profile);
            var path = UIReportFiles.GetPath(profile.logRoot, $"UIRedesignPackage_{SafeName(request.sourcePrefabPath)}.md");
            var lines = new List<string>
            {
                "# UI AI 改版包",
                "",
                $"生成时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                "",
                "本文件只记录改版输入、草稿 JSON、dry-run 和待确认执行计划，不调用 AI、不生成图片、不修改资源。",
                "",
                "## Request",
                $"- sourcePrefabPath：`{request.sourcePrefabPath}`",
                $"- sourcePreviewPath：`{request.sourcePreviewPath}`",
                $"- stylePrompt：{request.stylePrompt}",
                $"- inputImageFolder：`{request.inputImageFolder}`",
                $"- outputFolder：`{request.outputFolder}`",
                $"- referenceImagePaths：{Join(request.referenceImagePaths)}",
                "",
                "## 产物",
                $"- Brief：`{brief}`",
                DraftJsonLine(draftJson, sourceDraftJson),
                $"- DryRun 明细：`{dryRun}`",
                $"- DryRun 汇总：`{dryRunSummary}`",
                $"- 待确认执行计划：`{executionPlan}`",
                $"- 执行计划汇总：`{executionPlanSummary}`",
                $"- 待补输入 CSV：`{pendingInputs}`",
                $"- 待补输入汇总：`{UIReportFiles.GetPath(profile.logRoot, UIReportFiles.ReplacementPendingInputsSummary)}`",
                $"- 待补输入就绪 CSV：`{pendingInputReadiness}`",
                $"- 待补输入就绪汇总：`{UIReportFiles.GetPath(profile.logRoot, UIReportFiles.ReplacementPendingInputReadinessSummary)}`",
                $"- 外部生成输入包：`{externalInputPackage}`",
                $"- 外部生成输入包汇总：`{UIReportFiles.GetPath(profile.logRoot, UIReportFiles.ReplacementExternalInputPackageSummary)}`",
                $"- 外部生成任务清单：`{UIReportFiles.GetPath(profile.logRoot, UIReportFiles.ReplacementExternalGenerationTasks)}`",
                $"- 外部生成 Prompt Pack：`{UIReportFiles.GetPath(profile.logRoot, UIReportFiles.ReplacementExternalPromptPack)}`",
                $"- 外部生成单项 Prompt 清单：`{UIReportFiles.GetPath(profile.logRoot, UIReportFiles.ReplacementExternalPromptItemList)}`",
                $"- 外部生成引用素材清单：`{UIReportFiles.GetPath(profile.logRoot, UIReportFiles.ReplacementExternalReferenceCopyList)}`",
                $"- 宿主执行清单：`{hostApplyChecklist}`",
                "",
                "## DryRun 分布"
            };

            foreach (var group in rows.GroupBy(r => r["Severity"]).OrderBy(g => UIReplacementPlanStatus.SeverityOrder(g.Key)).ThenBy(g => g.Key))
                lines.Add($"- {group.Key}：{group.Count()}");
            lines.Add("");
            UIReportMarkdown.AddSeverityCheckSummary(lines, "DryRun 检查分布", rows.Where(r => r["Severity"] == "Warning" || r["Severity"] == "Review").ToList());
            lines.Add("## 执行计划状态");
            foreach (var group in executionRows.GroupBy(r => r["Status"]).OrderBy(g => UIReplacementPlanStatus.StatusOrder(g.Key)).ThenBy(g => g.Key))
                lines.Add($"- {group.Key}：{group.Count()}");
            lines.Add("");
            lines.Add("## Gate 状态");
            var dryRunErrors = rows.Count(r => r["Severity"] == "Error");
            lines.Add($"- DryRun Error：{dryRunErrors}");
            lines.Add($"- DryRun Gate：{(dryRunErrors == 0 ? "通过" : "阻断")}");
            var blockingRows = executionRows.Where(r => UIReplacementPlanStatus.IsBlocking(r["Status"])).ToList();
            lines.Add($"- 执行计划阻断步骤：{blockingRows.Count}");
            lines.Add($"- 执行计划 Gate：{(blockingRows.Count == 0 ? "通过" : "阻断")}");
            lines.Add("- 执行计划 Gate 规则：Blocked、PendingPreview、PendingAsset 和 PendingAtlas 阻断；NeedsReview 留给人工确认。");
            var missingInputs = readinessRows.Count(r => r["Readiness"] == "Missing");
            var invalidInputs = readinessRows.Count(r => r["Readiness"] == "Invalid");
            var notReadyInputs = readinessRows.Count(r => r["Readiness"] != "Ready");
            lines.Add($"- 待补输入缺失：{missingInputs}");
            lines.Add($"- 待补输入异常：{invalidInputs}");
            lines.Add($"- 待补输入未就绪：{notReadyInputs}");
            lines.Add($"- 待补输入 Gate：{(notReadyInputs == 0 ? "通过" : "阻断")}");
            if (blockingRows.Count > 0)
                lines.Add($"- 阻断状态：{UIReplacementPlanStatus.Summary(blockingRows)}");
            lines.Add("");
            AddMissingInputs(lines, executionRows);
            UIReportMarkdown.AddNotePrefixSummary(lines, "复核项分布", executionRows.Where(r => r["Status"] == "NeedsReview").ToList());
            lines.Add("## 下一步");
            lines.Add("- 根据 Brief 和草稿 JSON 补齐新版预览、生成图片目录和替换计划。");
            lines.Add("- 新版预览和新图落到输出目录后重新运行 dry-run 和执行计划。");
            lines.Add("- dry-run Error 以及执行计划的 PendingPreview、PendingAsset、PendingAtlas 清零后，再进入宿主确认流程。");
            lines.Add("- 宿主确认前先查看宿主执行清单，确认阻断步骤为 0。");
            lines.Add("- Warning 和 Review 需要人工确认后，才能按待确认执行计划进入宿主执行流程。");
            lines.Add("");

            File.WriteAllLines(path, lines, new UTF8Encoding(true));
            ValidateManifestSections(lines.ToArray());
            return path;
        }

        static string DraftJsonLine(string draftJson, string sourceDraftJson)
        {
            return string.IsNullOrEmpty(sourceDraftJson) ? $"- 草稿 JSON：`{draftJson}`" : $"- 草稿 JSON：`{draftJson}`（输入 `{sourceDraftJson}` 的安全快照）";
        }

        static void AddMissingInputs(List<string> lines, List<Dictionary<string, string>> rows)
        {
            var preview = rows.FirstOrDefault(r => r["Status"] == "PendingPreview");
            var assets = rows.Count(r => r["Action"] == "ConfirmNewAsset" && r["Status"] == "PendingAsset");
            var atlasGroups = rows.Where(r => r["Action"] == "ConfirmTargetAtlas" && r["Status"] == "PendingAtlas").GroupBy(r => r["TargetAtlas"]).OrderBy(g => g.Key).ToList();
            lines.Add("## 待补输入");
            lines.Add(preview == null ? "- 新版预览：无" : $"- 新版预览：`{preview["NewAsset"]}`");
            lines.Add($"- 新图：{assets}");
            lines.Add($"- 目标图集：{atlasGroups.Count}");
            foreach (var group in atlasGroups)
                lines.Add($"- `{group.Key}`：{group.Count()} 张新图");
            lines.Add("");
        }

        static string SafeName(string path)
        {
            var name = path.Replace("Assets/Bundle/Prefab/", "").Replace(".prefab", "").Replace('/', '_');
            foreach (var c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name;
        }

        static string Join(List<string> values)
        {
            return values.Count == 0 ? "" : string.Join(";", values);
        }

        static string ManifestPath(string[] lines, string prefix)
        {
            var line = lines.First(l => l.StartsWith(prefix, StringComparison.Ordinal));
            var start = line.IndexOf('`');
            var end = line.IndexOf('`', start + 1);
            return line.Substring(start + 1, end - start - 1);
        }

        static void RequireLine(string[] lines, string line)
        {
            if (!lines.Contains(line))
                throw new Exception("UI redesign package manifest is missing: " + line);
        }

        static void RequireExistingFile(string path, string label)
        {
            if (!File.Exists(path))
                throw new Exception($"UI redesign package {label} file is missing: {path}");
        }

        static void RequireManifestPath(string[] lines, string prefix, string expectedPath, string label)
        {
            var actualPath = ManifestPath(lines, prefix);
            if (actualPath != expectedPath)
                throw new Exception($"UI redesign package {label} path mismatch: {actualPath} -> {expectedPath}");
            RequireExistingFile(expectedPath, label);
        }

        static void RequireDistribution(string[] lines, List<Dictionary<string, string>> rows, string column, Func<string, int> order)
        {
            foreach (var group in rows.GroupBy(r => r[column]).OrderBy(g => order(g.Key)).ThenBy(g => g.Key))
                RequireLine(lines, $"- {group.Key}：{group.Count()}");
        }

        static void ValidateManifestSections(string[] lines)
        {
            UIReportMarkdown.RequireExactSectionOrder("UI redesign package manifest", lines, "## Request", "## 产物", "## DryRun 分布", "## DryRun 检查分布", "## 执行计划状态", "## Gate 状态", "## 待补输入", "## 复核项分布", "## 下一步");
        }

        static void ValidateManifestSourceReports(UIAIToolsProfile profile)
        {
            UIReplacementPlanDryRunService.ReadRows(profile);
            UIReplacementExecutionPlanService.ReadRows(profile);
            UIReplacementPendingInputReadinessService.ReadRows(profile);
        }

    }
}

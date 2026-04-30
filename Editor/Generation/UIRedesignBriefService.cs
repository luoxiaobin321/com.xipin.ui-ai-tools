using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Xipin.UIAITools
{
    public static class UIRedesignBriefService
    {
        const string DuplicateAssetRule = "- `oldAssetPath` 和 `newAssetPath` 不允许重复，`targetAtlasPath` 必须是 `.spriteatlasv2`。";

        public static string GenerateBrief(UIAIToolsProfile profile, UIRedesignRequest request)
        {
            UIReportValidationService.Validate(profile);
            var prefab = request.sourcePrefabPath;
            UIRedesignRequestValidation.ValidateSourcePrefab(profile, prefab, "brief");
            UIRedesignRequestValidation.ValidateOutputFolder(request.outputFolder);
            UIRedesignRequestValidation.ValidateSourcePreviewPath(request.sourcePreviewPath);
            UIRedesignRequestValidation.ValidateInputImageFolder(request.inputImageFolder);
            UIRedesignRequestValidation.ValidateReferenceImagePaths(request.referenceImagePaths);
            if (string.IsNullOrEmpty(request.sourcePreviewPath))
                request.sourcePreviewPath = UIPrefabBaselineScreenshotService.Capture(profile, prefab);

            var targets = UIScanReportRows.ReadPrefabOptimizationTargets(profile);
            var risks = UIScanReportRows.ReadPrefabDrawCallRisk(profile);
            var summaries = UIScanReportRows.ReadPrefabBatchBreakSummary(profile);
            var target = RequiredPrefabRow(targets, prefab);
            var risk = RequiredPrefabRow(risks, prefab);
            var summary = RequiredPrefabRow(summaries, prefab);
            var details = UIScanReportRows.ReadPrefabImageDetails(profile).Where(r => r["Prefab"] == prefab).ToList();
            var atlases = UIScanReportRows.ReadPrefabAtlasBreakdown(profile).Where(r => r["Prefab"] == prefab).ToList();
            var loose = UIScanReportRows.ReadLooseTextureCandidates(profile).Where(r => r["Prefabs"].Contains(prefab)).ToList();
            var nullSprites = UIScanReportRows.ReadPrefabNullSpriteImages(profile).Where(r => r["Prefab"] == prefab).ToList();
            var detailImages = new HashSet<string>(details.Select(r => r["Image"]));
            var reuseRows = UIScanReportRows.ReadReuseIndex(profile).Where(r => detailImages.Contains(r["Path"]) && r["Advice"] != "功能内使用，保留当前归属").ToList();
            var path = UIReportFiles.GetPath(profile.logRoot, $"UIRedesignBrief_{SafeName(prefab)}.md");
            var lines = new List<string>
            {
                "# UI 改版 Brief",
                "",
                $"生成时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                "",
                "本文件整理改版输入上下文，必要时引用自动生成的旧版基准图；不调用 AI，不生成新版图片，不移动资源，不覆盖 prefab。",
                "",
                "## Request Seed",
                $"- sourcePrefabPath：`{prefab}`",
                $"- sourcePreviewPath：`{request.sourcePreviewPath}`",
                $"- stylePrompt：{request.stylePrompt}",
                $"- inputImageFolder：`{request.inputImageFolder}`",
                $"- reuseCandidateReportPath：`{request.reuseCandidateReportPath}`",
                $"- outputFolder：`{request.outputFolder}`",
                $"- referenceImagePaths：{JoinList(request.referenceImagePaths)}",
                "",
                "## 当前扫描结论",
                $"- 优先级：{target["PriorityScore"]}",
                $"- 主问题：{target["MainIssue"]}",
                $"- 建议：{target["NextStep"]}",
                $"- 静态 batch：Image {target["ImageBatchGroups"]} / Estimated {target["EstimatedBatchGroups"]}，Break {target["BreakCount"]}，CrossAtlas {target["CrossAtlasBreaks"]}，LooseTexture {target["LooseTextureBreaks"]}，Text {target["TextBreaks"]}",
                $"- 资源概况：Atlas {target["AtlasCount"]}，UITexture {target["UITextureCount"]}，LargeTexture {target["LargeTextureCount"]}，Graphic {risk["GraphicCount"]}，NestedCanvas {risk["NestedCanvasCount"]}，Mask {risk["MaskCount"]}，RectMask2D {risk["RectMask2DCount"]}",
                ""
            };

            AddSplitItems(lines, "断批建议", summary["TopAdvice"], 8);
            AddSplitItems(lines, "纹理切换", summary["TopTexturePairs"], 8);
            AddImageGroups(lines, details);
            AddAtlasGroups(lines, atlases);
            AddLooseTextures(lines, loose);
            AddReuseRows(lines, reuseRows);
            AddNullSprites(lines, nullSprites);
            AddOutputRules(lines);

            Directory.CreateDirectory(profile.logRoot);
            File.WriteAllLines(path, lines, new UTF8Encoding(true));
            ValidateSections(lines.ToArray());
            Debug.Log($"UI redesign brief generated: {path}");
            if (!Application.isBatchMode)
                EditorUtility.RevealInFinder(Path.GetFullPath(path));
            return path;
        }

        public static void ValidateContract()
        {
            var lines = ContractLines();
            ValidateSections(lines.ToArray());
            ValidateOutputRules(lines);

            var missingSection = ContractLines();
            missingSection.Remove("## 图片归属");
            ExpectFailure("missing_image_section", "missing section", () => ValidateSections(missingSection.ToArray()));

            var missingRule = ContractLines();
            missingRule.Remove(DuplicateAssetRule);
            ExpectFailure("missing_duplicate_asset_rule", "output constraint is missing", () => ValidateOutputRules(missingRule));

            Debug.Log("UI redesign brief contract validation passed.");
        }

        static void AddSplitItems(List<string> lines, string title, string value, int count)
        {
            lines.Add($"## {title}");
            foreach (var item in value.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Take(count))
                lines.Add($"- {item}");
            lines.Add("");
        }

        static void AddImageGroups(List<string> lines, List<Dictionary<string, string>> details)
        {
            lines.Add("## 图片归属");
            foreach (var group in details.GroupBy(r => r["Match"]).OrderByDescending(g => g.Count()).ThenBy(g => g.Key))
                lines.Add($"- {group.Key}：{group.Count()}");
            lines.Add("");
            foreach (var row in details.Where(r => r["Match"] != "Match" && r["Match"] != "Shared").Take(20))
                lines.Add($"- {row["Image"]}：{row["Kind"]}，{row["Match"]}，TextRefs `{row["TextRefs"]}`");
            lines.Add("");
        }

        static void AddAtlasGroups(List<string> lines, List<Dictionary<string, string>> atlases)
        {
            lines.Add("## 图集拆解");
            foreach (var row in atlases.OrderByDescending(r => int.Parse(r["ImageCount"])).ThenBy(r => r["Atlas"]))
                lines.Add($"- {row["Atlas"]}：{row["ImageCount"]}，{row["Match"]}");
            lines.Add("");
        }

        static void AddLooseTextures(List<string> lines, List<Dictionary<string, string>> loose)
        {
            lines.Add("## 直挂 UITexture");
            foreach (var row in loose.Take(20))
                lines.Add($"- {row["Path"]}：{row["SizeClass"]}，{row["Advice"]}，{row["Reason"]}");
            lines.Add("");
        }

        static void AddNullSprites(List<string> lines, List<Dictionary<string, string>> nullSprites)
        {
            lines.Add("## 空 Sprite Image");
            foreach (var row in nullSprites.Where(r => !r["Advice"].Contains("保留")).Take(20))
                lines.Add($"- {row["Path"]}：{row["Advice"]}，{row["Reason"]}");
            lines.Add("");
        }

        static void AddReuseRows(List<string> lines, List<Dictionary<string, string>> rows)
        {
            lines.Add("## 复用与归属风险");
            foreach (var group in rows.GroupBy(r => r["Advice"]).OrderByDescending(g => g.Count()).ThenBy(g => g.Key))
                lines.Add($"- {group.Key}：{group.Count()}");
            lines.Add("");
            foreach (var row in rows.OrderBy(r => r["Advice"]).ThenBy(r => r["Path"]).Take(20))
                lines.Add($"- {row["Path"]}：{row["Advice"]}，Owner `{row["AssetOwner"]}`，Owners `{row["Owners"]}`，SameHash {row["SameHashCount"]}，{row["Reason"]}");
            lines.Add("");
        }

        static void AddOutputRules(List<string> lines)
        {
            lines.Add("## AI 输出约束");
            lines.Add("- 只输出 `UIRedesignDraft`、新版预览、新切图目录、替换计划和风险项。");
            lines.Add("- 草稿 JSON 必须是单一根对象，不允许根对象后追加文本或第二段 JSON。");
            lines.Add("- 不覆盖原 prefab，不移动旧资源，不修改 SpriteAtlas，不修改 YooAsset 配置。");
            lines.Add("- 大图、按名加载风险、跨功能借图和空 Sprite 交互节点必须进入风险项。");
            lines.Add("- `draftPreviewPath`、`generatedImageFolder` 和替换项路径必须使用 `Assets/...`，不得包含 `..` 路径段，新图必须位于 `generatedImageFolder` 下。");
            lines.Add("- `draftPreviewPath` 和 `newAssetPath` 必须是 `.png`。");
            lines.Add("- `replacementPlan.items` 必须存在，可为空数组。");
            lines.Add(DuplicateAssetRule);
            lines.Add("- `risks` 必须存在，可为空数组。");
            lines.Add("- 每个 `UIReplacementItem` 都必须保持 `requiresConfirmation = true`。");
            lines.Add("- 草稿 JSON 返回后先保存安全快照，再运行替换计划 dry-run，查看 `Logs/UIReplacementPlanDryRun.csv` 和 `Logs/UIReplacementPlanDryRunSummary.md`。");
            lines.Add("- dry-run 后生成待确认执行计划，查看 `Logs/UIReplacementExecutionPlan.csv` 和 `Logs/UIReplacementExecutionPlanSummary.md`。");
            lines.Add("- 一键改版包会生成 `Logs/UIRedesignPackage_*.md` 和 `Logs/UIReplacementHostApplyChecklist.md`，manifest 会记录输入草稿 JSON 和快照 JSON。");
            lines.Add("- 宿主确认前先查看待补输入、复核项和阻断状态。");
            lines.Add("- dry-run Error 以及执行计划的 PendingPreview、PendingAsset、PendingAtlas 清零后，再进入宿主确认流程。");
            lines.Add("");
            lines.Add("## AI 输出格式");
            lines.Add("```json");
            lines.Add("{");
            lines.Add("  \"draftPreviewPath\": \"Assets/Art/UI/AI/<Feature>/preview.png\",");
            lines.Add("  \"generatedImageFolder\": \"Assets/Art/UI/AI/<Feature>/Images\",");
            lines.Add("  \"replacementPlan\": {");
            lines.Add("    \"items\": [");
            lines.Add("      {");
            lines.Add("        \"oldAssetPath\": \"Assets/Bundle/UIAtlas/.../old.png\",");
            lines.Add("        \"newAssetPath\": \"Assets/Art/UI/AI/<Feature>/Images/new.png\",");
            lines.Add("        \"targetAtlasPath\": \"Assets/Bundle/UIAtlas/<Feature>/atlas_<feature>.spriteatlasv2\",");
            lines.Add("        \"preserveGuid\": false,");
            lines.Add("        \"requiresConfirmation\": true,");
            lines.Add("        \"reason\": \"替换依据\"");
            lines.Add("      }");
            lines.Add("    ]");
            lines.Add("  },");
            lines.Add("  \"requiresConfirmation\": true,");
            lines.Add("  \"risks\": [\"按名加载、跨功能借图、大图或空 Sprite 风险\"]");
            lines.Add("}");
            lines.Add("```");
            lines.Add("");
        }

        static Dictionary<string, string> RequiredPrefabRow(List<Dictionary<string, string>> rows, string prefab)
        {
            var row = rows.FirstOrDefault(r => r["Prefab"] == prefab);
            if (row == null)
                throw new Exception("UI prefab is not present in scan reports: " + prefab);
            return row;
        }

        static string JoinList(List<string> values)
        {
            return values.Count == 0 ? "" : string.Join(";", values);
        }

        static string SafeName(string path)
        {
            var name = path.Replace("Assets/Bundle/Prefab/", "").Replace(".prefab", "").Replace('/', '_');
            foreach (var c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name;
        }

        static void ValidateSections(string[] lines)
        {
            UIReportMarkdown.RequireExactSectionOrder("UI redesign brief", lines, "## Request Seed", "## 当前扫描结论", "## 断批建议", "## 纹理切换", "## 图片归属", "## 图集拆解", "## 直挂 UITexture", "## 复用与归属风险", "## 空 Sprite Image", "## AI 输出约束", "## AI 输出格式");
        }

        static void ValidateOutputRules(List<string> lines)
        {
            if (!lines.Contains(DuplicateAssetRule))
                throw new Exception("UI redesign brief output constraint is missing: duplicate old/new asset rule");
        }

        static List<string> ContractLines()
        {
            var lines = new List<string>();
            AddBriefSection(lines, "Request Seed");
            AddBriefSection(lines, "当前扫描结论");
            AddBriefSection(lines, "断批建议");
            AddBriefSection(lines, "纹理切换");
            AddBriefSection(lines, "图片归属");
            AddBriefSection(lines, "图集拆解");
            AddBriefSection(lines, "直挂 UITexture");
            AddBriefSection(lines, "复用与归属风险");
            AddBriefSection(lines, "空 Sprite Image");
            AddOutputRules(lines);
            return lines;
        }

        static void AddBriefSection(List<string> lines, string title)
        {
            lines.Add("## " + title);
            lines.Add("");
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
                throw new Exception($"Unexpected UI redesign brief contract failure for {name}: {exception.Message}");
            }
            throw new Exception("UI redesign brief contract sample did not fail: " + name);
        }
    }
}

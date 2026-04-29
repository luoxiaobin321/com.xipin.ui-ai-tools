using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Xipin.UIAITools
{
    public static class UIScanSummaryService
    {
        static readonly string[] SummarySections =
        {
            "## 图片归类分布",
            "## UITexture 尺寸分布",
            "## 直挂散图候选",
            "## 复用索引建议",
            "## 优化目标 Top",
            "## 相邻断批原因",
            "## 相邻断批建议",
            "## 空 Sprite Image",
            "## 空 Sprite 待确认样例",
            "## 建议下一步"
        };

        public static void Generate(UIAIToolsProfile profile)
        {
            UIReportValidationService.Validate(profile);
            var path = UIReportFiles.GetPath(profile.logRoot, UIReportFiles.Summary);
            var triage = UIScanReportRows.ReadAssetTriage(profile);
            var reuse = UIScanReportRows.ReadReuseIndex(profile);
            var loose = UIScanReportRows.ReadLooseTextureCandidates(profile);
            var targets = UIScanReportRows.ReadPrefabOptimizationTargets(profile);
            var breaks = UIScanReportRows.ReadPrefabBatchBreaks(profile);
            var nullSprites = UIScanReportRows.ReadPrefabNullSpriteImages(profile);
            var textureSizes = UIScanReportRows.ReadTextureSizes(profile);
            var lines = new List<string>
            {
                "# UI AI Tools 扫描摘要",
                "",
                $"生成时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                "",
                "本文件只由菜单或 batchmode 命令显式生成，不会定时运行，也不会修改资源、prefab、图集或 YooAsset 配置。",
                ""
            };

            AddGroup(lines, "图片归类分布", triage, "Advice", 12);
            AddGroup(lines, "UITexture 尺寸分布", textureSizes, "SizeClass", 8);
            AddGroup(lines, "直挂散图候选", loose, "Advice", 8);
            AddGroup(lines, "复用索引建议", reuse, "Advice", 12);
            AddTopTargets(lines, targets);
            AddBreaks(lines, breaks);
            AddNullSprites(lines, nullSprites);
            AddNextSteps(lines, loose);

            Directory.CreateDirectory(profile.logRoot);
            File.WriteAllLines(path, lines, new UTF8Encoding(true));
            ValidateSummarySections(lines.ToArray());
            Debug.Log($"UI AI Tools summary generated: {path}");
            if (!Application.isBatchMode)
                EditorUtility.RevealInFinder(Path.GetFullPath(path));
        }

        public static void GeneratePanelFocus(UIAIToolsProfile profile)
        {
            UIReportValidationService.Validate(profile);
            var path = UIReportFiles.GetPath(profile.logRoot, UIReportFiles.PanelFocus);
            var targets = UIScanReportRows.ReadPrefabOptimizationTargets(profile)
                .OrderByDescending(r => Int(r, "PriorityScore"))
                .ThenBy(r => r["Prefab"])
                .Take(10)
                .ToList();
            var risks = UIScanReportRows.ReadPrefabDrawCallRisk(profile).ToDictionary(r => r["Prefab"]);
            var summaries = UIScanReportRows.ReadPrefabBatchBreakSummary(profile).ToDictionary(r => r["Prefab"]);
            var loose = UIScanReportRows.ReadLooseTextureCandidates(profile);
            var nullSprites = UIScanReportRows.ReadPrefabNullSpriteImages(profile);
            var lines = new List<string>
            {
                "# UI 面板实测聚焦清单",
                "",
                $"生成时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                "",
                "本文件只由菜单或 batchmode 命令显式生成，用于人工打开 Frame Debugger/Profiler 前定位检查点，不会定时运行，也不会修改资源。",
                ""
            };

            foreach (var target in targets)
                AddPanelFocus(lines, target, risks[target["Prefab"]], summaries[target["Prefab"]], loose, nullSprites);

            Directory.CreateDirectory(profile.logRoot);
            File.WriteAllLines(path, lines, new UTF8Encoding(true));
            ValidatePanelFocusSections(lines.ToArray(), targets);
            Debug.Log($"UI AI Tools panel focus generated: {path}");
            if (!Application.isBatchMode)
                EditorUtility.RevealInFinder(Path.GetFullPath(path));
        }

        public static void ValidateContract()
        {
            ValidateSummarySections(SummarySections);
            ExpectSectionFailure("summary_missing_section", "UI AI tools summary is missing section: ## UITexture 尺寸分布", () =>
                ValidateSummarySections(SummarySections.Where(section => section != "## UITexture 尺寸分布").ToArray()));
            ExpectSectionFailure("summary_out_of_order", "UI AI tools summary section is out of order", () =>
                ValidateSummarySections(new[] { SummarySections[1], SummarySections[0] }.Concat(SummarySections.Skip(2)).ToArray()));

            var targets = new List<Dictionary<string, string>>
            {
                new Dictionary<string, string> { { "Prefab", "Assets/Bundle/Prefab/Home.prefab" } },
                new Dictionary<string, string> { { "Prefab", "Assets/Bundle/Prefab/Shop.prefab" } }
            };
            ValidatePanelFocusSections(new[] { "## Home", "## Shop" }, targets);
            ExpectSectionFailure("panel_focus_out_of_order", "UI AI tools panel focus section is out of order", () =>
                ValidatePanelFocusSections(new[] { "## Shop", "## Home" }, targets));
            Debug.Log("UI scan summary contract validation passed.");
        }

        static void AddGroup(List<string> lines, string title, List<Dictionary<string, string>> rows, string field, int count)
        {
            lines.Add($"## {title}");
            foreach (var group in rows.GroupBy(r => r[field]).OrderByDescending(g => g.Count()).ThenBy(g => g.Key).Take(count))
                lines.Add($"- {group.Key}：{group.Count()}");
            lines.Add("");
        }

        static void AddTopTargets(List<string> lines, List<Dictionary<string, string>> rows)
        {
            lines.Add("## 优化目标 Top");
            foreach (var row in rows.OrderByDescending(r => Int(r, "PriorityScore")).ThenBy(r => r["Prefab"]).Take(10))
                lines.Add($"- {ShortPrefab(row["Prefab"])}：Score {row["PriorityScore"]}，{row["MainIssue"]}，{row["NextStep"]}");
            lines.Add("");
        }

        static void AddBreaks(List<string> lines, List<Dictionary<string, string>> rows)
        {
            AddGroup(lines, "相邻断批原因", rows, "Reason", 8);
            AddGroup(lines, "相邻断批建议", rows, "Advice", 8);
        }

        static void AddNullSprites(List<string> lines, List<Dictionary<string, string>> rows)
        {
            AddGroup(lines, "空 Sprite Image", rows, "Advice", 8);
            var samples = rows.Where(r => !r["Advice"].Contains("保留")).Take(10).ToList();
            lines.Add("## 空 Sprite 待确认样例");
            if (samples.Count == 0)
            {
                lines.Add("- 无");
                lines.Add("");
                return;
            }
            foreach (var row in samples)
                lines.Add($"- {ShortPrefab(row["Prefab"])}#{row["Path"]}：{row["Advice"]}，{row["Reason"]}");
            lines.Add("");
        }

        static void AddNextSteps(List<string> lines, List<Dictionary<string, string>> loose)
        {
            lines.Add("## 建议下一步");
            lines.Add("- 先打开 `UIPrefabOptimizationTargets.csv` Top 面板做 Frame Debugger/Profiler 实测，优先确认文本图片交错和跨图集相邻是否真的影响 DrawCall。");
            lines.Add("- 资源迁移仍走人工确认；AI 和扫描摘要只给建议，不自动移动图片、不覆盖 prefab。");
            if (loose.Any(r => r["Advice"] == "审计迁入功能图集"))
                lines.Add("- `UILooseTextureCandidates.csv` 存在可审计迁入功能图集的散图，先确认没有按名加载风险和目标图集归属。");
            else
                lines.Add("- 当前直挂散图没有可直接迁入功能图集的自动候选，继续保留确认流程。");
            lines.Add("- 新 UI 或改版资源进来后，手动跑扫描、验证、生成摘要，再按摘要决定是否处理资源归属。");
            lines.Add("");
        }

        static void AddPanelFocus(List<string> lines, Dictionary<string, string> target, Dictionary<string, string> risk, Dictionary<string, string> summary, List<Dictionary<string, string>> loose, List<Dictionary<string, string>> nullSprites)
        {
            var prefab = target["Prefab"];
            lines.Add($"## {ShortPrefab(prefab)}");
            lines.Add($"- 优先级：{target["PriorityScore"]}，主问题：{target["MainIssue"]}，建议：{target["NextStep"]}");
            lines.Add($"- 静态 batch：Image {target["ImageBatchGroups"]} / Estimated {target["EstimatedBatchGroups"]}，Break {target["BreakCount"]}，CrossAtlas {target["CrossAtlasBreaks"]}，LooseTexture {target["LooseTextureBreaks"]}，Text {target["TextBreaks"]}");
            lines.Add($"- 资源概况：Atlas {target["AtlasCount"]}，UITexture {target["UITextureCount"]}，LargeTexture {target["LargeTextureCount"]}，Graphic {risk["GraphicCount"]}，NestedCanvas {risk["NestedCanvasCount"]}，Mask {risk["MaskCount"]}，RectMask2D {risk["RectMask2DCount"]}");
            AddSplitItems(lines, "Top 断批建议", summary["TopAdvice"], 6);
            AddSplitItems(lines, "Top 纹理切换", summary["TopTexturePairs"], 6);
            AddSplitItems(lines, "样例节点", summary["Samples"], 6);
            AddLooseSamples(lines, prefab, loose);
            AddNullSpriteSamples(lines, prefab, nullSprites);
            lines.Add("");
        }

        static void AddSplitItems(List<string> lines, string label, string value, int count)
        {
            lines.Add($"- {label}：");
            foreach (var item in value.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Take(count))
                lines.Add($"  - {item}");
        }

        static void AddLooseSamples(List<string> lines, string prefab, List<Dictionary<string, string>> rows)
        {
            var samples = rows.Where(r => r["Prefabs"].Contains(prefab)).Take(5).ToList();
            if (samples.Count == 0)
                return;
            lines.Add("- 直挂散图样例：");
            foreach (var row in samples)
                lines.Add($"  - {row["Path"]}：{row["Advice"]}，{row["Reason"]}");
        }

        static void AddNullSpriteSamples(List<string> lines, string prefab, List<Dictionary<string, string>> rows)
        {
            var samples = rows.Where(r => r["Prefab"] == prefab && !r["Advice"].Contains("保留")).Take(5).ToList();
            if (samples.Count == 0)
                return;
            lines.Add("- 空 Sprite 待确认：");
            foreach (var row in samples)
                lines.Add($"  - {row["Path"]}：{row["Advice"]}，{row["Reason"]}");
        }

        static int Int(Dictionary<string, string> row, string field)
        {
            return int.Parse(row[field]);
        }

        static string ShortPrefab(string path)
        {
            return path.Replace("Assets/Bundle/Prefab/", "").Replace(".prefab", "");
        }

        static void ValidateSummarySections(string[] lines)
        {
            UIReportMarkdown.RequireExactSectionOrder("UI AI tools summary", lines, SummarySections);
        }

        static void ValidatePanelFocusSections(string[] lines, List<Dictionary<string, string>> targets)
        {
            UIReportMarkdown.RequireExactSectionOrder("UI AI tools panel focus", lines, targets.Select(target => "## " + ShortPrefab(target["Prefab"])).ToArray());
        }

        static void ExpectSectionFailure(string name, string expectedMessage, Action validate)
        {
            try
            {
                validate();
            }
            catch (Exception exception)
            {
                if (exception.Message.Contains(expectedMessage))
                    return;
                throw new Exception($"Unexpected UI scan summary contract failure for {name}: {exception.Message}");
            }
            throw new Exception("UI scan summary contract sample did not fail: " + name);
        }
    }
}

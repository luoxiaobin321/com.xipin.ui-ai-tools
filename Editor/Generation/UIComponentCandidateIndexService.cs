using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Xipin.UIAITools
{
    public static class UIComponentCandidateIndexService
    {
        public static string Generate(UIAIToolsProfile profile, UIControlCatalog catalog)
        {
            UIReportValidationService.Validate(profile);
            var rows = UIScanReportRows.ReadPrefabBatchSequence(profile);
            var lines = new List<string> { UIReportFiles.ComponentCandidateIndexHeader };
            var count = 0;
            foreach (var group in rows.GroupBy(r => BuildCandidateKey(catalog, r))
                         .OrderBy(g => RoleOrder(g.Key.Role))
                         .ThenByDescending(g => g.Count())
                         .ThenBy(g => g.Key.Source)
                         .ThenBy(g => g.Key.ImageAsset))
            {
                var prefabs = group.Select(r => r["Prefab"]).Distinct().OrderBy(p => p).ToList();
                var nodes = group.Select(r => r["Prefab"] + "#" + r["Path"]).Distinct().OrderBy(p => p).ToList();
                lines.Add(string.Join(",", new[]
                {
                    Csv(StableComponentId(group.Key)),
                    Csv(group.Key.Role),
                    Csv(group.Key.Source),
                    Csv(group.Key.Type),
                    Csv(group.Key.Kind),
                    Csv(group.Key.ImageAsset),
                    Csv(group.Key.Atlas),
                    group.Count().ToString(),
                    prefabs.Count.ToString(),
                    Csv(Join(prefabs, 8)),
                    Csv(Join(nodes, 8)),
                    Csv("候选来自 UIPrefabBatchSequence，升为组件库项前需宿主确认")
                }));
                count++;
            }

            var path = UIReportFiles.GetPath(profile.logRoot, UIReportFiles.ComponentCandidateIndex);
            File.WriteAllLines(path, lines, new UTF8Encoding(true));
            UIReportValidationService.ValidateReport(profile, UIReportFiles.ComponentCandidateIndex, UIReportFiles.ComponentCandidateIndexHeader);
            var summaryPath = GenerateSummary(profile);
            var reviewPath = GenerateReviewChecklist(profile);
            Validate(profile);
            Debug.Log($"UI component candidate index generated: {path}, summary: {summaryPath}, review: {reviewPath}, {count} candidates.");
            return path;
        }

        public static void Validate(UIAIToolsProfile profile)
        {
            var rows = ReadIndexRows(profile);
            var summaryPath = UIReportFiles.GetPath(profile.logRoot, UIReportFiles.ComponentCandidateIndexSummary);
            if (!File.Exists(summaryPath))
                throw new Exception($"Missing UI component candidate index summary: {summaryPath}");
            ValidateSummarySections(File.ReadAllLines(summaryPath));
            ValidateReviewChecklist(profile, rows);
            Debug.Log($"UI component candidate index validation passed: {rows.Count} candidates.");
        }

        public static List<Dictionary<string, string>> ReadIndexRows(UIAIToolsProfile profile)
        {
            UIReportValidationService.ValidateReport(profile, UIReportFiles.ComponentCandidateIndex, UIReportFiles.ComponentCandidateIndexHeader);
            var rows = UIReportCsv.ReadRows(profile.logRoot, UIReportFiles.ComponentCandidateIndex);
            if (rows.Count == 0)
                throw new Exception("Empty UI component candidate index.");
            foreach (var row in rows)
                ValidateIndexRow(row);
            var duplicateId = rows.GroupBy(r => r["ComponentId"]).FirstOrDefault(g => g.Count() > 1);
            if (duplicateId != null)
                throw new Exception($"Duplicate UI component candidate id: {duplicateId.Key}");
            return rows;
        }

        public static List<Dictionary<string, string>> ReadReviewRows(UIAIToolsProfile profile)
        {
            UIReportValidationService.ValidateReport(profile, UIReportFiles.ComponentCandidateReview, UIReportFiles.ComponentCandidateReviewHeader);
            var rows = UIReportCsv.ReadRows(profile.logRoot, UIReportFiles.ComponentCandidateReview);
            foreach (var row in rows)
                ValidateReviewRow(row);
            var duplicateId = rows.GroupBy(r => r["ComponentId"]).FirstOrDefault(g => g.Count() > 1);
            if (duplicateId != null)
                throw new Exception($"Duplicate UI component candidate review id: {duplicateId.Key}");
            return rows;
        }

        static string GenerateSummary(UIAIToolsProfile profile)
        {
            var rows = ReadIndexRows(profile);
            var path = UIReportFiles.GetPath(profile.logRoot, UIReportFiles.ComponentCandidateIndexSummary);
            var lines = new List<string>
            {
                "# UI 组件候选索引",
                "",
                $"生成时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                "",
                "本文件只汇总扫描报告中的组件候选，不创建 prefab、不复制图片、不修改图集。",
                "",
                "## 角色分布"
            };

            foreach (var group in rows.GroupBy(r => r["Role"]).OrderBy(g => RoleOrder(g.Key)).ThenBy(g => g.Key))
                lines.Add($"- {group.Key}：候选 {group.Count()}，引用 {group.Sum(r => int.Parse(r["UseCount"]))}");
            lines.Add("");
            lines.Add("## 高频候选");
            foreach (var row in rows.OrderByDescending(r => int.Parse(r["UseCount"])).ThenBy(r => r["Role"]).ThenBy(r => r["Source"]).Take(30))
                lines.Add($"- {row["ComponentId"]} / {row["Role"]}：{row["UseCount"]} 次，prefab {row["PrefabCount"]}，资源 `{row["ImageAsset"]}`，样例 `{row["SampleNodes"]}`");
            lines.Add("");
            AddButtonReviewSections(lines, rows);
            lines.Add("");
            lines.Add("## 使用方式");
            lines.Add($"- `UIComponentCandidateReview.csv` 是给宿主填写的确认清单，`SuggestedDecision` 默认为 `NeedsReview`。");
            lines.Add("- 重新生成时会按 `ComponentId` 保留确认清单里的人工填写列，并刷新扫描派生列。");
            lines.Add("- 先由宿主确认候选是否能升级为组件库项，再补充稳定 prefab、预览图、状态和使用说明。");
            lines.Add("- 自动制作 UI 的布局草稿只能引用已确认组件，不应凭空生成组件路径。");
            lines.Add("");
            File.WriteAllLines(path, lines, new UTF8Encoding(true));
            ValidateSummarySections(lines.ToArray());
            return path;
        }

        static string GenerateReviewChecklist(UIAIToolsProfile profile)
        {
            var rows = ReadIndexRows(profile);
            var path = UIReportFiles.GetPath(profile.logRoot, UIReportFiles.ComponentCandidateReview);
            var existingRows = ReadExistingReviewRows(profile, path);
            var lines = new List<string> { UIReportFiles.ComponentCandidateReviewHeader };
            foreach (var row in rows.OrderBy(r => ReviewTierOrder(ReviewTier(r))).ThenBy(r => RoleOrder(r["Role"])).ThenByDescending(UseCount).ThenBy(r => r["ImageAsset"]))
            {
                existingRows.TryGetValue(row["ComponentId"], out var existingRow);
                lines.Add(string.Join(",", new[]
                {
                    Csv(row["ComponentId"]),
                    Csv(row["Role"]),
                    Csv(ReviewTier(row)),
                    Csv(ReviewValue(existingRow, "SuggestedDecision", "NeedsReview")),
                    row["UseCount"],
                    row["PrefabCount"],
                    Csv(row["ImageAsset"]),
                    Csv(row["Atlas"]),
                    Csv(row["SamplePrefabs"]),
                    Csv(row["SampleNodes"]),
                    Csv(ReviewValue(existingRow, "ComponentPrefabPath", "")),
                    Csv(ReviewValue(existingRow, "PreviewPath", "")),
                    Csv(ReviewValue(existingRow, "States", row["Role"] == "Button" ? "normal;disabled;selected;pressed" : "normal")),
                    Csv(ReviewValue(existingRow, "UsageNotes", "")),
                    Csv(ReviewValue(existingRow, "Reviewer", "")),
                    Csv(ReviewValue(existingRow, "ReviewNotes", ""))
                }));
            }

            File.WriteAllLines(path, lines, new UTF8Encoding(true));
            ReadReviewRows(profile);
            return path;
        }

        static Dictionary<string, Dictionary<string, string>> ReadExistingReviewRows(UIAIToolsProfile profile, string path)
        {
            if (!File.Exists(path))
                return new Dictionary<string, Dictionary<string, string>>();
            return ReadReviewRows(profile).ToDictionary(r => r["ComponentId"]);
        }

        static string ReviewValue(Dictionary<string, string> row, string field, string defaultValue)
        {
            return row == null ? defaultValue : row[field];
        }

        static void ValidateReviewChecklist(UIAIToolsProfile profile, List<Dictionary<string, string>> indexRows)
        {
            var reviewRows = ReadReviewRows(profile);
            if (reviewRows.Count != indexRows.Count)
                throw new Exception($"UI component candidate review row count mismatch: {reviewRows.Count}->{indexRows.Count}");

            var indexById = indexRows.ToDictionary(r => r["ComponentId"]);
            foreach (var row in reviewRows)
            {
                if (!indexById.TryGetValue(row["ComponentId"], out var indexRow))
                    throw new Exception($"Unknown UI component candidate review id: {row["ComponentId"]}");
                foreach (var field in new[] { "Role", "UseCount", "PrefabCount", "ImageAsset", "Atlas", "SamplePrefabs", "SampleNodes" })
                {
                    if (row[field] != indexRow[field])
                        throw new Exception($"Stale UI component candidate review field: {row["ComponentId"]} {field}");
                }
                if (row["ReviewTier"] != ReviewTier(indexRow))
                    throw new Exception($"Stale UI component candidate review tier: {row["ComponentId"]}");
            }
        }

        static void ValidateIndexRow(Dictionary<string, string> row)
        {
            if (!ValidComponentId(row["ComponentId"]))
                throw new Exception($"Invalid UI component candidate id: {row["ComponentId"]}");
            if (string.IsNullOrEmpty(row["Role"]))
                throw new Exception("Invalid UI component candidate row: Role is required");
            if (!PositiveInt(row["UseCount"]) || !PositiveInt(row["PrefabCount"]))
                throw new Exception("Invalid UI component candidate row count: " + row["ComponentId"]);
            if (string.IsNullOrEmpty(row["SamplePrefabs"]) || string.IsNullOrEmpty(row["SampleNodes"]))
                throw new Exception("Invalid UI component candidate row samples: " + row["ComponentId"]);
        }

        static void ValidateReviewRow(Dictionary<string, string> row)
        {
            if (!ValidComponentId(row["ComponentId"]))
                throw new Exception($"Invalid UI component candidate review id: {row["ComponentId"]}");
            if (string.IsNullOrEmpty(row["Role"]) || string.IsNullOrEmpty(row["ReviewTier"]))
                throw new Exception("Invalid UI component candidate review row: Role and ReviewTier are required");
            if (!ValidDecision(row["SuggestedDecision"]))
                throw new Exception($"Invalid UI component candidate review decision: {row["ComponentId"]} {row["SuggestedDecision"]}");
            if (!PositiveInt(row["UseCount"]) || !PositiveInt(row["PrefabCount"]))
                throw new Exception("Invalid UI component candidate review row count: " + row["ComponentId"]);
            if (row["SuggestedDecision"] == "Approved" && string.IsNullOrEmpty(row["ComponentPrefabPath"]))
                throw new Exception($"Approved UI component candidate requires ComponentPrefabPath: {row["ComponentId"]}");
            if (string.IsNullOrEmpty(row["States"]))
                throw new Exception("Invalid UI component candidate review row: States is required " + row["ComponentId"]);
        }

        static void AddButtonReviewSections(List<string> lines, List<Dictionary<string, string>> rows)
        {
            var buttons = rows.Where(r => r["Role"] == "Button").ToList();
            lines.Add("## Button 复核队列");
            AddCandidateSection(lines, "跨 prefab Button 候选", buttons.Where(HighReuseButton)
                .OrderByDescending(UseCount).ThenBy(r => r["ImageAsset"]).Take(20));
            AddCandidateSection(lines, "单 prefab 或低复用 Button 候选", buttons.Where(r => !HighReuseButton(r))
                .OrderByDescending(UseCount).ThenBy(r => r["ImageAsset"]).Take(20));
        }

        static void AddCandidateSection(List<string> lines, string title, IEnumerable<Dictionary<string, string>> rows)
        {
            lines.Add("");
            lines.Add($"### {title}");
            var count = 0;
            foreach (var row in rows)
            {
                lines.Add($"- {row["ComponentId"]}：{row["UseCount"]} 次，prefab {row["PrefabCount"]}，资源 `{row["ImageAsset"]}`，样例 `{row["SampleNodes"]}`");
                count++;
            }

            if (count == 0)
                lines.Add("- 无");
        }

        static void ValidateSummarySections(string[] lines)
        {
            UIReportMarkdown.RequireExactSectionOrder("UI component candidate index summary", lines, "## 角色分布", "## 高频候选", "## Button 复核队列", "## 使用方式");
        }

        static int UseCount(Dictionary<string, string> row)
        {
            return int.Parse(row["UseCount"]);
        }

        static int PrefabCount(Dictionary<string, string> row)
        {
            return int.Parse(row["PrefabCount"]);
        }

        static bool HighReuseButton(Dictionary<string, string> row)
        {
            return PrefabCount(row) > 1 && UseCount(row) > 2;
        }

        static string ReviewTier(Dictionary<string, string> row)
        {
            if (row["Role"] == "Button")
                return HighReuseButton(row) ? "HighReuseButton" : "LowReuseButton";
            return "Candidate";
        }

        static int ReviewTierOrder(string tier)
        {
            if (tier == "HighReuseButton")
                return 0;
            if (tier == "LowReuseButton")
                return 1;
            return 2;
        }

        static bool ValidDecision(string decision)
        {
            return decision == "NeedsReview" || decision == "Approved" || decision == "Rejected";
        }

        static bool PositiveInt(string value)
        {
            return int.TryParse(value, out var count) && count > 0;
        }

        static CandidateKey BuildCandidateKey(UIControlCatalog catalog, Dictionary<string, string> row)
        {
            return new CandidateKey
            {
                Role = Role(catalog, row),
                Source = row["Source"],
                Type = row["Type"],
                Kind = row["Kind"],
                ImageAsset = row["ImageAsset"],
                Atlas = row["Atlas"]
            };
        }

        static string Role(UIControlCatalog catalog, Dictionary<string, string> row)
        {
            var source = row["Source"];
            var path = row["Path"];
            if (catalog.HasRole(source, UIControlRole.DynamicImage))
                return "DynamicImage";
            if (catalog.HasRole(source, UIControlRole.Text) || row["Kind"] == "TextGraphic" || source.Contains("Text"))
                return "Text";
            if (row["Type"] == "Image" && row["Kind"] == "NullSpriteImage")
                return "EmptyImage";
            if (catalog.HasRole(source, UIControlRole.Button) || LooksLikeButton(path))
                return "Button";
            if (catalog.HasRole(source, UIControlRole.Scroll) || LooksLikeScroll(path) || (row["Type"] == "Image" && row["ImageAsset"] == "Resources/unity_builtin_extra" && LooksLikeScrollContainer(path)))
                return "Scroll";
            if (catalog.HasRole(source, UIControlRole.Panel) || LooksLikePanel(path))
                return "Panel";
            if (catalog.HasRole(source, UIControlRole.FunctionalEmptyImage))
                return "FunctionalEmptyImage";
            if (row["Type"] == "Image" && row["ImageAsset"] == "Resources/unity_builtin_extra")
                return "Image";
            if (row["Type"] == "Image")
                return "Image";
            if (row["Type"] == "RawImage")
                return "RawImage";
            return "Graphic";
        }

        static bool LooksLikeButton(string path)
        {
            var name = LeafName(path).ToLowerInvariant();
            return name == "btn" || name == "button" || name.StartsWith("btn") || name.EndsWith("btn") || EndsWithNumberedToken(name, "btn") ||
                name.Contains("_btn") || name.Contains("btn_") || name.Contains("button") || name.Contains("toggle") ||
                name == "tab" || name.Contains("_tab") || name.Contains("tab_") || name.Contains("tabbtn");
        }

        static bool LooksLikeScroll(string path)
        {
            return LooksLikeScrollName(LeafName(path).ToLowerInvariant());
        }

        static bool LooksLikeScrollContainer(string path)
        {
            var name = LeafName(path).ToLowerInvariant();
            if (LooksLikeScroll(path))
                return true;
            return name == "viewport" && LooksLikeScrollName(ParentName(path));
        }

        static bool LooksLikePanel(string path)
        {
            var name = LeafName(path).ToLowerInvariant();
            return name == "panel" || name.EndsWith("panel");
        }

        static string LeafName(string path)
        {
            var index = path.LastIndexOf('/');
            var name = index >= 0 ? path.Substring(index + 1) : path;
            return StripCloneSuffix(name);
        }

        static string ParentName(string path)
        {
            var index = path.LastIndexOf('/');
            if (index <= 0)
                return "";
            var parentEnd = index - 1;
            var parentStart = path.LastIndexOf('/', parentEnd);
            var name = parentStart >= 0 ? path.Substring(parentStart + 1, parentEnd - parentStart) : path.Substring(0, index);
            return StripCloneSuffix(name).ToLowerInvariant();
        }

        static string StripCloneSuffix(string name)
        {
            var suffix = name.LastIndexOf(" (", StringComparison.Ordinal);
            return suffix > 0 && name.EndsWith(")", StringComparison.Ordinal) ? name.Substring(0, suffix) : name;
        }

        static bool LooksLikeScrollName(string name)
        {
            return name.Contains("scrollview") || name.Contains("scroll view") || name.Contains("scrollrect");
        }

        static bool EndsWithNumberedToken(string name, string token)
        {
            var index = name.LastIndexOf(token, StringComparison.Ordinal);
            return index >= 0 && index + token.Length < name.Length && name.Skip(index + token.Length).All(char.IsDigit);
        }

        static int RoleOrder(string role)
        {
            if (role == "Button")
                return 0;
            if (role == "Text")
                return 1;
            if (role == "Image" || role == "DynamicImage")
                return 2;
            if (role == "RawImage")
                return 3;
            if (role == "Scroll")
                return 4;
            if (role == "Panel")
                return 5;
            if (role == "EmptyImage" || role == "FunctionalEmptyImage")
                return 6;
            return 7;
        }

        static string Join(List<string> values, int count)
        {
            return string.Join(";", values.Take(count)) + (values.Count > count ? ";..." : "");
        }

        static string StableComponentId(CandidateKey key)
        {
            var value = string.Join("|", new[] { key.Role, key.Source, key.Type, key.Kind, key.ImageAsset, key.Atlas });
            return "Component" + StableHash(value).ToString("X8");
        }

        static uint StableHash(string value)
        {
            unchecked
            {
                uint hash = 2166136261;
                foreach (var c in value)
                {
                    hash ^= c;
                    hash *= 16777619;
                }
                return hash;
            }
        }

        static bool ValidComponentId(string value)
        {
            return value.Length == 17 && value.StartsWith("Component", StringComparison.Ordinal) && value.Skip(9).All(IsHex);
        }

        static bool IsHex(char value)
        {
            return value >= '0' && value <= '9' || value >= 'A' && value <= 'F';
        }

        static string Csv(string value)
        {
            value = value ?? "";
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        struct CandidateKey
        {
            public string Role;
            public string Source;
            public string Type;
            public string Kind;
            public string ImageAsset;
            public string Atlas;
        }
    }
}

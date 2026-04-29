using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Xipin.UIAITools
{
    public static class UIScanReportRows
    {
        public static void Validate(UIAIToolsProfile profile)
        {
            foreach (var report in UIReportFiles.CoreReports)
                ReadCore(profile, report);
        }

        public static void ValidateContract()
        {
            var root = Path.Combine(Path.GetTempPath(), "UIAIToolsScanReportRowsContract_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                var profile = ScriptableObject.CreateInstance<UIAIToolsProfile>();
                profile.logRoot = root;
                var row = ReuseIndexRow("Assets/Art/UI/Icon.png");
                WriteCsv(profile, UIReportFiles.ReuseIndex, UIReportFiles.ReuseIndexHeader, new[] { row });
                ReadReuseIndex(profile);
                ExpectFailure(profile, "duplicate_reuse_index_row", UIReportFiles.ReuseIndex, UIReportFiles.ReuseIndexHeader, new[] { row, row }, () => ReadReuseIndex(profile), "duplicate reuse index row");

                row = AssetTriageRow("Assets/Art/UI/Icon.png");
                WriteCsv(profile, UIReportFiles.AssetTriageReport, UIReportFiles.AssetTriageReportHeader, new[] { row });
                ReadAssetTriage(profile);
                ExpectFailure(profile, "duplicate_asset_triage_row", UIReportFiles.AssetTriageReport, UIReportFiles.AssetTriageReportHeader, new[] { row, row }, () => ReadAssetTriage(profile), "duplicate asset triage row");
            }
            finally
            {
                Directory.Delete(root, true);
            }
            Debug.Log("UI scan report rows contract validation passed.");
        }

        public static List<Dictionary<string, string>> ReadAssetTriage(UIAIToolsProfile profile)
        {
            return ReadCore(profile, UIReportFiles.AssetTriageReport);
        }

        public static List<Dictionary<string, string>> ReadTextureSizes(UIAIToolsProfile profile)
        {
            return ReadCore(profile, UIReportFiles.TextureSizeReport);
        }

        public static List<Dictionary<string, string>> ReadReuseIndex(UIAIToolsProfile profile)
        {
            return ReadCore(profile, UIReportFiles.ReuseIndex);
        }

        public static List<Dictionary<string, string>> ReadPrefabImageDetails(UIAIToolsProfile profile)
        {
            return ReadCore(profile, UIReportFiles.PrefabImageDetails);
        }

        public static List<Dictionary<string, string>> ReadPrefabAtlasBreakdown(UIAIToolsProfile profile)
        {
            return ReadCore(profile, UIReportFiles.PrefabAtlasBreakdown);
        }

        public static List<Dictionary<string, string>> ReadPrefabDrawCallRisk(UIAIToolsProfile profile)
        {
            return ReadCore(profile, UIReportFiles.PrefabDrawCallRisk);
        }

        public static List<Dictionary<string, string>> ReadPrefabBatchSequence(UIAIToolsProfile profile)
        {
            return ReadCore(profile, UIReportFiles.PrefabBatchSequence);
        }

        public static List<Dictionary<string, string>> ReadPrefabBatchBreaks(UIAIToolsProfile profile)
        {
            return ReadCore(profile, UIReportFiles.PrefabBatchBreaks);
        }

        public static List<Dictionary<string, string>> ReadPrefabBatchBreakSummary(UIAIToolsProfile profile)
        {
            return ReadCore(profile, UIReportFiles.PrefabBatchBreakSummary);
        }

        public static List<Dictionary<string, string>> ReadPrefabOptimizationTargets(UIAIToolsProfile profile)
        {
            return ReadCore(profile, UIReportFiles.PrefabOptimizationTargets);
        }

        public static List<Dictionary<string, string>> ReadPrefabNullSpriteImages(UIAIToolsProfile profile)
        {
            return ReadCore(profile, UIReportFiles.PrefabNullSpriteImages);
        }

        public static List<Dictionary<string, string>> ReadLooseTextureCandidates(UIAIToolsProfile profile)
        {
            return ReadCore(profile, UIReportFiles.LooseTextureCandidates);
        }

        static List<Dictionary<string, string>> ReadCore(UIAIToolsProfile profile, string report)
        {
            UIReportValidationService.ValidateReport(profile, report, UIReportFiles.CoreReportHeaders[report]);
            var rows = UIReportCsv.ReadRows(profile.logRoot, report);
            foreach (var row in rows)
                ValidateRow(report, row);
            ValidateNoDuplicateRows(report, rows);
            return rows;
        }

        static void ValidateNoDuplicateRows(string report, List<Dictionary<string, string>> rows)
        {
            if (report != UIReportFiles.ReuseIndex && report != UIReportFiles.AssetTriageReport)
                return;
            var duplicate = rows.GroupBy(row => row["Path"]).FirstOrDefault(group => group.Count() > 1);
            if (duplicate != null)
                throw new Exception($"Invalid {report}: duplicate {DuplicateRowName(report)} for {duplicate.Key}");
        }

        static string DuplicateRowName(string report)
        {
            return report == UIReportFiles.ReuseIndex ? "reuse index row" : "asset triage row";
        }

        static void ValidateRow(string report, Dictionary<string, string> row)
        {
            if (report == UIReportFiles.ReuseIndex)
            {
                Require(report, row, "Path", "Name", "Guid", "SizeClass", "Kind", "Hash", "Advice", "Reason");
                RequireInts(report, row, "Width", "Height", "PrefabCount", "OwnerCount", "SameHashCount");
            }
            else if (report == UIReportFiles.AssetTriagePlan)
            {
                Require(report, row, "Action", "Risk", "Source", "Target", "Advice");
                RequireInts(report, row, "Width", "Height", "PrefabCount");
            }
            else if (report == UIReportFiles.PrefabAtlasStats)
            {
                Require(report, row, "Prefab", "Owner");
                RequireInts(report, row, "AtlasCount", "UITextureCount", "LargeTextureCount", "ImageCount");
            }
            else if (report == UIReportFiles.ACommonUsage)
            {
                Require(report, row, "Path", "Name", "Guid", "Hash", "Advice");
                RequireInts(report, row, "Width", "Height", "PrefabCount", "OwnerCount");
            }
            else if (report == UIReportFiles.DuplicateImageReport)
            {
                Require(report, row, "Hash", "Paths", "Guids", "Advice");
                RequireInts(report, row, "Count", "Width", "Height", "Bytes");
            }
            else if (report == UIReportFiles.PrefabImageDetails)
            {
                Require(report, row, "Prefab", "Image", "Kind", "Name", "Guid", "SizeClass", "Match");
                RequireInts(report, row, "Width", "Height");
            }
            else if (report == UIReportFiles.PrefabAtlasBreakdown)
            {
                Require(report, row, "Prefab", "Atlas", "Match", "Images");
                RequireInts(report, row, "ImageCount");
            }
            else if (report == UIReportFiles.PrefabDrawCallRisk)
            {
                Require(report, row, "Prefab", "Owner");
                RequireInts(report, row, "GraphicCount", "ImageCount", "RawImageCount", "OtherGraphicCount", "ImagePlusCount", "NullSpriteImageCount", "AtlasImageCount", "LooseTextureImageCount", "UniqueAtlasCount", "UniqueLooseTextureCount", "UniqueTextureCount", "UniqueMaterialCount", "EstimatedBatchGroups", "ImageBatchGroups", "TextureSwitches", "ImageTextureSwitches", "MaterialSwitches", "NestedCanvasCount", "MaskCount", "RectMask2DCount");
            }
            else if (report == UIReportFiles.PrefabBatchSequence)
            {
                Require(report, row, "Prefab", "Owner", "Path", "Type", "Source", "Kind", "Canvas", "BatchKey");
                RequireInts(report, row, "Index");
            }
            else if (report == UIReportFiles.PrefabBatchBreaks)
            {
                Require(report, row, "Prefab", "Owner", "Path", "Reason", "Advice");
                RequireInts(report, row, "Index");
            }
            else if (report == UIReportFiles.PrefabBatchBreakSummary)
            {
                Require(report, row, "Prefab", "Owner");
                RequireInts(report, row, "BreakCount", "TextureBreaks", "TextBreaks", "MaterialBreaks", "CanvasBreaks", "CrossAtlasBreaks", "LooseTextureBreaks");
            }
            else if (report == UIReportFiles.PrefabOptimizationTargets)
            {
                Require(report, row, "Prefab", "Owner", "MainIssue", "NextStep");
                RequireInts(report, row, "PriorityScore", "ImageBatchGroups", "EstimatedBatchGroups", "BreakCount", "CrossAtlasBreaks", "LooseTextureBreaks", "TextBreaks", "MaterialBreaks", "CanvasBreaks", "AtlasCount", "UITextureCount", "LargeTextureCount");
            }
            else if (report == UIReportFiles.PrefabTextureSwitchPairs)
            {
                Require(report, row, "Prefab", "Owner", "Advice", "Reason", "PrevTexture", "Texture");
                RequireInts(report, row, "Count", "FirstIndex");
            }
            else if (report == UIReportFiles.PrefabWhiteTextureBreaks)
            {
                Require(report, row, "Prefab", "Owner", "WhiteSide", "WhiteKind", "Reason", "Advice");
                RequireInts(report, row, "Count", "FirstIndex");
            }
            else if (report == UIReportFiles.PrefabNullSpriteImages)
            {
                Require(report, row, "Prefab", "Owner", "Path", "Node", "Advice", "Reason");
                RequireInts(report, row, "Width", "Height");
            }
            else if (report == UIReportFiles.LooseTextureCandidates)
            {
                Require(report, row, "Path", "Name", "Guid", "SizeClass", "Advice", "Reason");
                RequireInts(report, row, "Width", "Height", "UseCount", "PrefabCount", "OwnerCount");
            }
            else if (report == UIReportFiles.TextureSizeReport)
            {
                Require(report, row, "Path", "Name", "Guid", "Hash", "SizeClass", "Advice");
                RequireInts(report, row, "Width", "Height", "Area", "Bytes", "Memory", "PrefabCount");
            }
            else if (report == UIReportFiles.AssetTriageReport)
            {
                Require(report, row, "Path", "Name", "Guid", "Hash", "Advice", "Reason");
                RequireInts(report, row, "Width", "Height", "Bytes", "Memory", "NameDup");
            }
        }

        static void Require(string report, Dictionary<string, string> row, params string[] fields)
        {
            foreach (var field in fields)
            {
                if (string.IsNullOrEmpty(row[field]))
                    throw new Exception($"Invalid {report}: {field} is required");
            }
        }

        static void RequireInts(string report, Dictionary<string, string> row, params string[] fields)
        {
            foreach (var field in fields)
            {
                if (!long.TryParse(row[field], out _))
                    throw new Exception($"Invalid {report}: {field} must be an integer");
            }
        }

        static void WriteCsv(UIAIToolsProfile profile, string report, string header, IEnumerable<string> rows)
        {
            File.WriteAllLines(UIReportFiles.GetPath(profile.logRoot, report), new[] { header }.Concat(rows), new UTF8Encoding(true));
        }

        static string ReuseIndexRow(string path)
        {
            return string.Join(",", new[]
            {
                Csv(path),
                Csv("Icon"),
                Csv("guid"),
                "64",
                "64",
                Csv("Small"),
                Csv("Image"),
                Csv("Assets/Art/UI/UI.spriteatlasv2"),
                Csv("UI"),
                "1",
                "1",
                Csv("Owner"),
                Csv("Assets/Prefab/A.prefab"),
                Csv(""),
                Csv("hash"),
                "1",
                Csv(""),
                Csv("Reuse"),
                Csv("same")
            });
        }

        static string AssetTriageRow(string path)
        {
            return string.Join(",", new[]
            {
                Csv(path),
                Csv("Icon"),
                Csv("guid"),
                "64",
                "64",
                "1024",
                "2048",
                Csv("TextureImporter"),
                Csv("Assets/Art/UI/UI.spriteatlasv2"),
                Csv("Assets/Prefab/A.prefab"),
                Csv(""),
                "1",
                Csv("hash"),
                Csv("Review"),
                Csv("same")
            });
        }

        static void ExpectFailure(UIAIToolsProfile profile, string name, string report, string header, IEnumerable<string> rows, Action readRows, string expectedMessage)
        {
            WriteCsv(profile, report, header, rows);
            try
            {
                readRows();
            }
            catch (Exception exception)
            {
                if (exception.Message.Contains(expectedMessage))
                    return;
                throw new Exception($"Unexpected UI scan report rows contract failure for {name}: {exception.Message}");
            }
            throw new Exception("UI scan report rows contract sample did not fail: " + name);
        }

        static string Csv(string value)
        {
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }
    }
}

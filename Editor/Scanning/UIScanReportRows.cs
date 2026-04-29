using System;
using System.Collections.Generic;

namespace Xipin.UIAITools
{
    public static class UIScanReportRows
    {
        public static void Validate(UIAIToolsProfile profile)
        {
            foreach (var report in UIReportFiles.CoreReports)
                ReadCore(profile, report);
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
            return rows;
        }

        static void ValidateRow(string report, Dictionary<string, string> row)
        {
            if (report == UIReportFiles.ReuseIndex)
            {
                Require(report, row, "Path", "Name", "Guid", "SizeClass", "Kind", "Hash", "Advice", "Reason");
                RequireInts(report, row, "Width", "Height", "PrefabCount", "OwnerCount", "SameHashCount");
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
    }
}

using System.Collections.Generic;

namespace Xipin.UIAITools
{
    public static class UIScanReportRows
    {
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
            return UIReportCsv.ReadRows(profile.logRoot, report);
        }
    }
}

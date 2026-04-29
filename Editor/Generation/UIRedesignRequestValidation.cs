using System;
using System.Linq;

namespace Xipin.UIAITools
{
    internal static class UIRedesignRequestValidation
    {
        public static void ValidateSourcePrefab(UIAIToolsProfile profile, string sourcePrefabPath, string context)
        {
            if (string.IsNullOrEmpty(sourcePrefabPath))
                throw new Exception($"Missing source prefab path for UI redesign {context}.");
            UIReportValidationService.ValidateReport(profile, UIReportFiles.PrefabOptimizationTargets, UIReportFiles.PrefabOptimizationTargetsHeader);
            var targets = UIReportCsv.ReadRows(profile.logRoot, UIReportFiles.PrefabOptimizationTargets);
            if (!targets.Any(r => r["Prefab"] == sourcePrefabPath))
                throw new Exception("UI prefab is not present in scan reports: " + sourcePrefabPath);
        }

        public static void ValidateOutputFolder(string outputFolder)
        {
            if (string.IsNullOrEmpty(outputFolder))
                return;
            if (outputFolder.Contains("\\") || !outputFolder.StartsWith("Assets/", StringComparison.Ordinal))
                throw new Exception("UI redesign outputFolder must be an Assets/ path.");
            if (outputFolder.Contains("/../") || outputFolder.EndsWith("/..", StringComparison.Ordinal))
                throw new Exception("UI redesign outputFolder cannot contain .. path segments.");
        }
    }
}

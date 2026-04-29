using System;
using System.Collections.Generic;
using System.Globalization;

namespace Xipin.UIAITools
{
    public static class UIReuseSearchResultService
    {
        public static List<Dictionary<string, string>> ReadRows(UIAIToolsProfile profile)
        {
            UIReportValidationService.ValidateReport(profile, UIReportFiles.ReuseSearchResults, UIReportFiles.ReuseSearchResultsHeader);
            var rows = UIReportCsv.ReadRows(profile.logRoot, UIReportFiles.ReuseSearchResults);
            foreach (var row in rows)
                ValidateRow(row);
            return rows;
        }

        static void ValidateRow(Dictionary<string, string> row)
        {
            if (string.IsNullOrEmpty(row["Query"]) || string.IsNullOrEmpty(row["Path"]) || string.IsNullOrEmpty(row["Name"]))
                throw new Exception("Invalid UI reuse search result row: Query, Path and Name are required");
            if (!int.TryParse(row["Width"], out _) || !int.TryParse(row["Height"], out _))
                throw new Exception("Invalid UI reuse search result row: Width and Height must be integers");
            if (!float.TryParse(row["Score"], NumberStyles.Float, CultureInfo.InvariantCulture, out _))
                throw new Exception("Invalid UI reuse search result row: Score must be a number");
            if (!int.TryParse(row["PrefabCount"], out _) || !int.TryParse(row["OwnerCount"], out _))
                throw new Exception("Invalid UI reuse search result row: PrefabCount and OwnerCount must be integers");
            if (string.IsNullOrEmpty(row["Advice"]) || string.IsNullOrEmpty(row["Reason"]))
                throw new Exception("Invalid UI reuse search result row: Advice and Reason are required");
        }
    }
}

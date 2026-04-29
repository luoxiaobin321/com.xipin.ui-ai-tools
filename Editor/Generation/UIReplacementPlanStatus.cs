using System.Collections.Generic;
using System.Linq;

namespace Xipin.UIAITools
{
    static class UIReplacementPlanStatus
    {
        public static bool IsBlocking(string status)
        {
            return status == "Blocked" || status == "PendingPreview" || status == "PendingAsset" || status == "PendingAtlas";
        }

        public static string Summary(List<Dictionary<string, string>> rows)
        {
            return string.Join("，", rows.GroupBy(r => r["Status"])
                .OrderBy(g => StatusOrder(g.Key))
                .ThenBy(g => g.Key)
                .Select(g => $"{g.Key}：{g.Count()}"));
        }

        public static int StatusOrder(string status)
        {
            if (status == "Blocked")
                return 0;
            if (status == "PendingPreview")
                return 1;
            if (status == "PendingAsset")
                return 2;
            if (status == "PendingAtlas")
                return 3;
            if (status == "NeedsReview")
                return 4;
            if (status == "PendingConfirmation")
                return 5;
            return 6;
        }

        public static int SeverityOrder(string severity)
        {
            if (severity == "Error")
                return 0;
            if (severity == "Warning")
                return 1;
            if (severity == "Review")
                return 2;
            return 3;
        }
    }
}

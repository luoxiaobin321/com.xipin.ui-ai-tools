using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Xipin.UIAITools
{
    public static class UIReplacementPlanStatus
    {
        public static void ValidateContract()
        {
            if (!IsBlocking("Blocked") || !IsBlocking("PendingPreview") || !IsBlocking("PendingAsset") || !IsBlocking("PendingAtlas") || IsBlocking("NeedsReview"))
                throw new Exception("UI replacement plan status blocking contract failed.");

            var summary = Summary(new List<Dictionary<string, string>>
            {
                Row("PendingAsset"),
                Row("Skipped"),
                Row("Blocked"),
                Row("PendingPreview"),
                Row("NeedsReview"),
                Row("PendingAtlas"),
                Row("PendingConfirmation"),
                Row("PendingAsset")
            });
            if (summary != "Blocked：1，PendingPreview：1，PendingAsset：2，PendingAtlas：1，NeedsReview：1，PendingConfirmation：1，Skipped：1")
                throw new Exception("UI replacement plan status summary contract failed: " + summary);
            if (SeverityOrder("Error") >= SeverityOrder("Warning") || SeverityOrder("Warning") >= SeverityOrder("Review") || SeverityOrder("Review") >= SeverityOrder("Info"))
                throw new Exception("UI replacement plan severity order contract failed.");
            Debug.Log("UI replacement plan status contract validation passed.");
        }

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

        static Dictionary<string, string> Row(string status)
        {
            return new Dictionary<string, string> { { "Status", status } };
        }
    }
}

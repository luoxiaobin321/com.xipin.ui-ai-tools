using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

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
            ValidateNoDuplicateRows(rows);
            return rows;
        }

        public static void ValidateContract()
        {
            var root = Path.Combine(Path.GetTempPath(), "UIAIToolsReuseSearchResultContract_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                var profile = ScriptableObject.CreateInstance<UIAIToolsProfile>();
                profile.logRoot = root;
                WriteCsv(profile, new[]
                {
                    Row("Assets/Query.png", "Assets/Art/UI/Old.png", "Old", "guid", "128", "128", "0.95", "Small", "Image", "", "UI", "1", "1", "Owner", "Prefab#Node", "", "Reuse", "same")
                });
                ReadRows(profile);
                ExpectRowsFailure(profile, "duplicate_reuse_search_row", new[]
                {
                    Row("Assets/Query.png", "Assets/Art/UI/Old.png", "Old", "guid", "128", "128", "0.95", "Small", "Image", "", "UI", "1", "1", "Owner", "Prefab#Node", "", "Reuse", "same"),
                    Row("Assets/Query.png", "Assets/Art/UI/Old.png", "Old", "guid", "128", "128", "0.95", "Small", "Image", "", "UI", "1", "1", "Owner", "Prefab#Node", "", "Reuse", "same")
                }, "duplicate reuse search result row");
            }
            finally
            {
                Directory.Delete(root, true);
            }
            Debug.Log("UI reuse search result contract validation passed.");
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

        static void ValidateNoDuplicateRows(List<Dictionary<string, string>> rows)
        {
            var duplicate = rows.GroupBy(row => new { Query = row["Query"], Path = row["Path"] }).FirstOrDefault(group => group.Count() > 1);
            if (duplicate != null)
                throw new Exception($"Invalid UI reuse search result row: duplicate reuse search result row for {duplicate.Key.Query} {duplicate.Key.Path}");
        }

        static void WriteCsv(UIAIToolsProfile profile, IEnumerable<string> rows)
        {
            var path = UIReportFiles.GetPath(profile.logRoot, UIReportFiles.ReuseSearchResults);
            File.WriteAllLines(path, new[] { UIReportFiles.ReuseSearchResultsHeader }.Concat(rows), new UTF8Encoding(true));
        }

        static string Row(string query, string path, string name, string guid, string width, string height, string score, string sizeClass, string kind, string atlas, string assetOwner, string prefabCount, string ownerCount, string owners, string prefabRefs, string textRefs, string advice, string reason)
        {
            return string.Join(",", new[] { query, path, name, guid, width, height, score, sizeClass, kind, atlas, assetOwner, prefabCount, ownerCount, owners, prefabRefs, textRefs, advice, reason }.Select(Csv));
        }

        static void ExpectRowsFailure(UIAIToolsProfile profile, string name, IEnumerable<string> rows, string expectedMessage)
        {
            WriteCsv(profile, rows);
            try
            {
                ReadRows(profile);
            }
            catch (Exception exception)
            {
                if (exception.Message.Contains(expectedMessage))
                    return;
                throw new Exception($"Unexpected UI reuse search result contract failure for {name}: {exception.Message}");
            }
            throw new Exception("UI reuse search result contract sample did not fail: " + name);
        }

        static string Csv(string value)
        {
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }
    }
}

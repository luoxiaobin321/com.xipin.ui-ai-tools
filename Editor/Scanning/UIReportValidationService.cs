using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Xipin.UIAITools
{
    public static class UIReportValidationService
    {
        public static void Validate(UIAIToolsProfile profile)
        {
            foreach (var report in UIReportFiles.CoreReports)
                ValidateReport(profile, report, UIReportFiles.CoreReportHeaders[report]);
            UIScanReportRows.Validate(profile);
            Debug.Log($"UI AI Tools report validation passed: {UIReportFiles.CoreReports.Length} reports.");
        }

        public static void ValidateReport(UIAIToolsProfile profile, string report, string expectedHeader)
        {
            ValidateReport(UIReportFiles.GetPath(profile.logRoot, report), expectedHeader);
        }

        public static void ValidateCsvContract()
        {
            var root = Path.Combine(Path.GetTempPath(), "UIAIToolsCsvContract_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                var validPath = Path.Combine(root, "valid.csv");
                File.WriteAllLines(validPath, new[] { "A,B", "1,2", "\"x,y\",z", "\"x\"\"y\",z" }, new UTF8Encoding(true));
                ValidateReport(validPath, "A,B");

                ExpectCsvFailure(root, "duplicate_header.csv", new[] { "A,A", "1,2" }, "Duplicate UI AI Tools CSV header");
                ExpectCsvFailure(root, "empty_header.csv", new[] { "A,", "1,2" }, "Invalid UI AI Tools CSV empty header");
                ExpectCsvFailure(root, "column_count.csv", new[] { "A,B", "1" }, "Invalid UI AI Tools CSV column count");
                ExpectCsvFailure(root, "bad_quote.csv", new[] { "A,B", "\"1,2" }, "Invalid UI AI Tools CSV quote");
                ExpectCsvFailure(root, "multiline_quoted_value.csv", new[] { "A,B", "\"1", "2\",3" }, "Invalid UI AI Tools CSV quote");
                ExpectCsvFailure(root, "quote_in_unquoted_value.csv", new[] { "A,B", "1\"2,3" }, "Invalid UI AI Tools CSV quote");
                ExpectCsvFailure(root, "text_after_quote.csv", new[] { "A,B", "\"1\"2,3" }, "Invalid UI AI Tools CSV quote");
                ExpectCsvFailure(root, "space_after_quote.csv", new[] { "A,B", "\"1\" ,3" }, "Invalid UI AI Tools CSV quote");
            }
            finally
            {
                if (Directory.Exists(root))
                    Directory.Delete(root, true);
            }
            Debug.Log("UI AI Tools CSV contract validation passed.");
        }

        public static void ValidateJsonContract()
        {
            var root = Path.Combine(Path.GetTempPath(), "UIAIToolsJsonContract_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                const string targetFolder = "Assets/UIAITools/Creation/JsonContract";
                var briefPath = Path.Combine(root, "brief.json");
                File.WriteAllText(briefPath, UICreationBriefTemplateService.ToJsonWithRootArrays(new UICreationBrief
                {
                    featureName = "JsonContract",
                    uiType = "Dialog",
                    targetFolder = targetFolder,
                    stylePrompt = ""
                }, "referenceImagePaths", "requiredInteractions", "dataBindings", "constraints"), new UTF8Encoding(true));
                UICreationBriefTemplateService.LoadBrief(briefPath);

                var layoutPath = Path.Combine(root, "layout.json");
                File.WriteAllText(layoutPath, UICreationBriefTemplateService.ToJsonWithRootArrays(new UILayoutDraft
                {
                    root = new UILayoutRoot
                    {
                        name = "JsonContract",
                        uiType = "Dialog",
                        targetFolder = targetFolder,
                        referenceResolution = "1080x1920",
                        safeAreaPolicy = ""
                    },
                    nodes = new System.Collections.Generic.List<UILayoutNode>
                    {
                        new UILayoutNode
                        {
                            nodeId = "Root",
                            parentId = "",
                            name = "Root",
                            componentRole = "Panel",
                            componentId = "builtin:Panel",
                            anchor = "stretch_full",
                            position = "0,0",
                            size = "1080x1920",
                            state = "normal",
                            text = "",
                            dataBinding = "",
                            assetPath = ""
                        }
                    },
                    assets = new System.Collections.Generic.List<UICreationAssetNeed>
                    {
                        new UICreationAssetNeed { needId = "BindingTitle", kind = "DataBinding", path = "", source = "Brief", status = "NeedsReview", reason = "title" }
                    }
                }, "nodes", "assets", "interactions", "risks"), new UTF8Encoding(true));
                UILayoutDraftTemplateService.LoadDraft(layoutPath);

                ExpectJsonFailure(root, "brief_missing_array.json", "{\"featureName\":\"Bad\",\"uiType\":\"Dialog\",\"targetFolder\":\"Assets/UIAITools/Creation/Bad\",\"stylePrompt\":\"\",\"requiredInteractions\":[],\"dataBindings\":[],\"constraints\":[]}", UICreationBriefTemplateService.LoadBrief, "referenceImagePaths is required");
                ExpectJsonFailure(root, "brief_feature_name_not_string.json", "{\"featureName\":{},\"uiType\":\"Dialog\",\"targetFolder\":\"Assets/UIAITools/Creation/Bad\",\"stylePrompt\":\"\",\"referenceImagePaths\":[],\"requiredInteractions\":[],\"dataBindings\":[],\"constraints\":[]}", UICreationBriefTemplateService.LoadBrief, "featureName must be a string");
                ExpectJsonFailure(root, "brief_feature_name_bad_escape.json", "{\"featureName\":\"Bad\\q\",\"uiType\":\"Dialog\",\"targetFolder\":\"Assets/UIAITools/Creation/Bad\",\"stylePrompt\":\"\",\"referenceImagePaths\":[],\"requiredInteractions\":[],\"dataBindings\":[],\"constraints\":[]}", UICreationBriefTemplateService.LoadBrief, "string escape is invalid");
                ExpectJsonFailure(root, "brief_feature_name_trailing_token.json", "{\"featureName\":\"Bad\" x,\"uiType\":\"Dialog\",\"targetFolder\":\"Assets/UIAITools/Creation/Bad\",\"stylePrompt\":\"\",\"referenceImagePaths\":[],\"requiredInteractions\":[],\"dataBindings\":[],\"constraints\":[]}", UICreationBriefTemplateService.LoadBrief, "featureName must be a string");
                ExpectJsonFailure(root, "brief_feature_name_duplicated.json", "{\"featureName\":\"Bad\",\"featureName\":\"Other\",\"uiType\":\"Dialog\",\"targetFolder\":\"Assets/UIAITools/Creation/Bad\",\"stylePrompt\":\"\",\"referenceImagePaths\":[],\"requiredInteractions\":[],\"dataBindings\":[],\"constraints\":[]}", UICreationBriefTemplateService.LoadBrief, "featureName is duplicated");
                ExpectJsonFailure(root, "brief_root_leading_comma.json", "{,\"featureName\":\"Bad\",\"uiType\":\"Dialog\",\"targetFolder\":\"Assets/UIAITools/Creation/Bad\",\"stylePrompt\":\"\",\"referenceImagePaths\":[],\"requiredInteractions\":[],\"dataBindings\":[],\"constraints\":[]}", UICreationBriefTemplateService.LoadBrief, "brief field is missing");
                ExpectJsonFailure(root, "brief_root_double_comma.json", "{\"featureName\":\"Bad\",,\"uiType\":\"Dialog\",\"targetFolder\":\"Assets/UIAITools/Creation/Bad\",\"stylePrompt\":\"\",\"referenceImagePaths\":[],\"requiredInteractions\":[],\"dataBindings\":[],\"constraints\":[]}", UICreationBriefTemplateService.LoadBrief, "brief field is missing");
                ExpectJsonFailure(root, "brief_root_trailing_comma.json", "{\"featureName\":\"Bad\",\"uiType\":\"Dialog\",\"targetFolder\":\"Assets/UIAITools/Creation/Bad\",\"stylePrompt\":\"\",\"referenceImagePaths\":[],\"requiredInteractions\":[],\"dataBindings\":[],\"constraints\":[],}", UICreationBriefTemplateService.LoadBrief, "brief field is missing");
                ExpectJsonFailure(root, "brief_style_prompt_not_string.json", "{\"featureName\":\"Bad\",\"uiType\":\"Dialog\",\"targetFolder\":\"Assets/UIAITools/Creation/Bad\",\"stylePrompt\":{},\"referenceImagePaths\":[],\"requiredInteractions\":[],\"dataBindings\":[],\"constraints\":[]}", UICreationBriefTemplateService.LoadBrief, "stylePrompt must be a string");
                ExpectJsonFailure(root, "brief_requires_confirmation_not_boolean.json", "{\"featureName\":\"Bad\",\"uiType\":\"Dialog\",\"targetFolder\":\"Assets/UIAITools/Creation/Bad\",\"stylePrompt\":\"\",\"requiresConfirmation\":{},\"referenceImagePaths\":[],\"requiredInteractions\":[],\"dataBindings\":[],\"constraints\":[]}", UICreationBriefTemplateService.LoadBrief, "requiresConfirmation must be a boolean");
                ExpectJsonFailure(root, "brief_requires_confirmation_bad_literal.json", "{\"featureName\":\"Bad\",\"uiType\":\"Dialog\",\"targetFolder\":\"Assets/UIAITools/Creation/Bad\",\"stylePrompt\":\"\",\"requiresConfirmation\":truex,\"referenceImagePaths\":[],\"requiredInteractions\":[],\"dataBindings\":[],\"constraints\":[]}", UICreationBriefTemplateService.LoadBrief, "requiresConfirmation must be a boolean");
                ExpectJsonFailure(root, "brief_requires_confirmation_trailing_token.json", "{\"featureName\":\"Bad\",\"uiType\":\"Dialog\",\"targetFolder\":\"Assets/UIAITools/Creation/Bad\",\"stylePrompt\":\"\",\"requiresConfirmation\":true x,\"referenceImagePaths\":[],\"requiredInteractions\":[],\"dataBindings\":[],\"constraints\":[]}", UICreationBriefTemplateService.LoadBrief, "requiresConfirmation must be a boolean");
                ExpectJsonFailure(root, "brief_reference_image_item_not_string.json", "{\"featureName\":\"Bad\",\"uiType\":\"Dialog\",\"targetFolder\":\"Assets/UIAITools/Creation/Bad\",\"stylePrompt\":\"\",\"referenceImagePaths\":[{}],\"requiredInteractions\":[],\"dataBindings\":[],\"constraints\":[]}", UICreationBriefTemplateService.LoadBrief, "referenceImagePaths item must be a string");
                ExpectJsonFailure(root, "brief_reference_image_missing_comma.json", "{\"featureName\":\"Bad\",\"uiType\":\"Dialog\",\"targetFolder\":\"Assets/UIAITools/Creation/Bad\",\"stylePrompt\":\"\",\"referenceImagePaths\":[\"Assets/A.png\" \"Assets/B.png\"],\"requiredInteractions\":[],\"dataBindings\":[],\"constraints\":[]}", UICreationBriefTemplateService.LoadBrief, "referenceImagePaths items must be separated by commas");
                ExpectJsonFailure(root, "brief_reference_image_field_missing_comma.json", "{\"featureName\":\"Bad\",\"uiType\":\"Dialog\",\"targetFolder\":\"Assets/UIAITools/Creation/Bad\",\"stylePrompt\":\"\",\"referenceImagePaths\":[] \"requiredInteractions\":[],\"dataBindings\":[],\"constraints\":[]}", UICreationBriefTemplateService.LoadBrief, "referenceImagePaths must be an array");
                ExpectJsonFailure(root, "brief_trailing_content.json", "{\"featureName\":\"Bad\",\"uiType\":\"Dialog\",\"targetFolder\":\"Assets/UIAITools/Creation/Bad\",\"stylePrompt\":\"\",\"referenceImagePaths\":[],\"requiredInteractions\":[],\"dataBindings\":[],\"constraints\":[]}{}", UICreationBriefTemplateService.LoadBrief, "trailing content");
                ExpectJsonFailure(root, "layout_missing_confirmation.json", "{\"root\":{\"name\":\"Bad\",\"uiType\":\"Dialog\",\"targetFolder\":\"Assets/UIAITools/Creation/Bad\",\"referenceResolution\":\"1080x1920\",\"safeAreaPolicy\":\"\"},\"nodes\":[],\"assets\":[],\"interactions\":[],\"risks\":[]}", UILayoutDraftTemplateService.LoadDraft, "requiresConfirmation is required");
                ExpectJsonFailure(root, "layout_root_duplicated.json", "{\"root\":{\"name\":\"Bad\",\"uiType\":\"Dialog\",\"targetFolder\":\"Assets/UIAITools/Creation/Bad\",\"referenceResolution\":\"1080x1920\",\"safeAreaPolicy\":\"\"},\"root\":{\"name\":\"Other\",\"uiType\":\"Dialog\",\"targetFolder\":\"Assets/UIAITools/Creation/Bad\",\"referenceResolution\":\"1080x1920\",\"safeAreaPolicy\":\"\"},\"nodes\":[],\"assets\":[],\"interactions\":[],\"risks\":[],\"requiresConfirmation\":true}", UILayoutDraftTemplateService.LoadDraft, "root is duplicated");
                ExpectJsonFailure(root, "layout_root_name_trailing_token.json", "{\"root\":{\"name\":\"Bad\" x,\"uiType\":\"Dialog\",\"targetFolder\":\"Assets/UIAITools/Creation/Bad\",\"referenceResolution\":\"1080x1920\",\"safeAreaPolicy\":\"\"},\"nodes\":[],\"assets\":[],\"interactions\":[],\"risks\":[],\"requiresConfirmation\":true}", UILayoutDraftTemplateService.LoadDraft, "root.name must be a string");
                ExpectJsonFailure(root, "layout_root_trailing_comma.json", "{\"root\":{\"name\":\"Bad\",\"uiType\":\"Dialog\",\"targetFolder\":\"Assets/UIAITools/Creation/Bad\",\"referenceResolution\":\"1080x1920\",\"safeAreaPolicy\":\"\",},\"nodes\":[],\"assets\":[],\"interactions\":[],\"risks\":[],\"requiresConfirmation\":true}", UILayoutDraftTemplateService.LoadDraft, "root field is missing");
                ExpectJsonFailure(root, "layout_root_field_missing_comma.json", "{\"root\":{\"name\":\"Bad\",\"uiType\":\"Dialog\",\"targetFolder\":\"Assets/UIAITools/Creation/Bad\",\"referenceResolution\":\"1080x1920\",\"safeAreaPolicy\":\"\"} \"nodes\":[],\"assets\":[],\"interactions\":[],\"risks\":[],\"requiresConfirmation\":true}", UILayoutDraftTemplateService.LoadDraft, "root must be an object");
                ExpectJsonFailure(root, "layout_root_target_folder_not_string.json", "{\"root\":{\"name\":\"Bad\",\"uiType\":\"Dialog\",\"targetFolder\":{},\"referenceResolution\":\"1080x1920\",\"safeAreaPolicy\":\"\"},\"nodes\":[],\"assets\":[],\"interactions\":[],\"risks\":[],\"requiresConfirmation\":true}", UILayoutDraftTemplateService.LoadDraft, "root.targetFolder must be a string");
                ExpectJsonFailure(root, "layout_requires_confirmation_not_boolean.json", "{\"root\":{\"name\":\"Bad\",\"uiType\":\"Dialog\",\"targetFolder\":\"Assets/UIAITools/Creation/Bad\",\"referenceResolution\":\"1080x1920\",\"safeAreaPolicy\":\"\"},\"nodes\":[],\"assets\":[],\"interactions\":[],\"risks\":[],\"requiresConfirmation\":{}}", UILayoutDraftTemplateService.LoadDraft, "requiresConfirmation must be a boolean");
                ExpectJsonFailure(root, "layout_requires_confirmation_bad_literal.json", "{\"root\":{\"name\":\"Bad\",\"uiType\":\"Dialog\",\"targetFolder\":\"Assets/UIAITools/Creation/Bad\",\"referenceResolution\":\"1080x1920\",\"safeAreaPolicy\":\"\"},\"nodes\":[],\"assets\":[],\"interactions\":[],\"risks\":[],\"requiresConfirmation\":truex}", UILayoutDraftTemplateService.LoadDraft, "requiresConfirmation must be a boolean");
                ExpectJsonFailure(root, "layout_assets_not_array.json", "{\"root\":{\"name\":\"Bad\",\"uiType\":\"Dialog\",\"targetFolder\":\"Assets/UIAITools/Creation/Bad\",\"referenceResolution\":\"1080x1920\",\"safeAreaPolicy\":\"\"},\"nodes\":[],\"assets\":{},\"interactions\":[],\"risks\":[],\"requiresConfirmation\":true}", UILayoutDraftTemplateService.LoadDraft, "assets must be an array");
                ExpectJsonFailure(root, "layout_node_name_not_string.json", LayoutDraftJson("[{\"nodeId\":\"Root\",\"name\":{},\"componentRole\":\"Panel\",\"componentId\":\"builtin:Panel\",\"anchor\":\"stretch_full\",\"position\":\"0,0\",\"size\":\"1080x1920\"}]", "[]"), UILayoutDraftTemplateService.LoadDraft, "nodes.name must be a string");
                ExpectJsonFailure(root, "layout_node_id_duplicated.json", LayoutDraftJson("[{\"nodeId\":\"Root\",\"nodeId\":\"Other\",\"name\":\"Root\",\"componentRole\":\"Panel\",\"componentId\":\"builtin:Panel\",\"anchor\":\"stretch_full\",\"position\":\"0,0\",\"size\":\"1080x1920\"}]", "[]"), UILayoutDraftTemplateService.LoadDraft, "nodeId is duplicated");
                ExpectJsonFailure(root, "layout_node_parent_id_not_string.json", LayoutDraftJson("[{\"nodeId\":\"Root\",\"parentId\":{},\"name\":\"Root\",\"componentRole\":\"Panel\",\"componentId\":\"builtin:Panel\",\"anchor\":\"stretch_full\",\"position\":\"0,0\",\"size\":\"1080x1920\"}]", "[]"), UILayoutDraftTemplateService.LoadDraft, "nodes.parentId must be a string");
                ExpectJsonFailure(root, "layout_node_missing_comma.json", LayoutDraftJson("[" + LayoutNodeJson("Root") + " " + LayoutNodeJson("Child") + "]", "[]"), UILayoutDraftTemplateService.LoadDraft, "nodes items must be separated by commas");
                ExpectJsonFailure(root, "layout_asset_status_not_string.json", LayoutDraftJson("[]", "[{\"needId\":\"BindingTitle\",\"kind\":\"DataBinding\",\"path\":\"\",\"source\":\"Brief\",\"status\":{},\"reason\":\"title\"}]"), UILayoutDraftTemplateService.LoadDraft, "assets.status must be a string");
                ExpectJsonFailure(root, "layout_asset_need_id_duplicated.json", LayoutDraftJson("[]", "[{\"needId\":\"BindingTitle\",\"needId\":\"Other\",\"kind\":\"DataBinding\",\"path\":\"\",\"source\":\"Brief\",\"status\":\"NeedsReview\",\"reason\":\"title\"}]"), UILayoutDraftTemplateService.LoadDraft, "needId is duplicated");
                ExpectJsonFailure(root, "layout_interaction_item_not_string.json", LayoutDraftJson("[]", "[]", "[{}]", "[]"), UILayoutDraftTemplateService.LoadDraft, "interactions item must be a string");
            }
            finally
            {
                if (Directory.Exists(root))
                    Directory.Delete(root, true);
            }
            Debug.Log("UI AI Tools JSON contract validation passed.");
        }

        public static void ValidateReportFilesContract()
        {
            var duplicateReport = UIReportFiles.CoreReports.GroupBy(report => report).FirstOrDefault(group => group.Count() > 1);
            if (duplicateReport != null)
                throw new Exception("Duplicate UI AI Tools core report: " + duplicateReport.Key);
            var staleHeader = UIReportFiles.CoreReportHeaders.Keys.Except(UIReportFiles.CoreReports).FirstOrDefault();
            if (staleHeader != null)
                throw new Exception("Stale UI AI Tools core report header: " + staleHeader);

            foreach (var report in UIReportFiles.CoreReports)
            {
                if (!UIReportFiles.CoreReportHeaders.ContainsKey(report))
                    throw new Exception("Missing UI AI Tools core report header: " + report);
                ValidateHeaderContract(report, UIReportFiles.CoreReportHeaders[report]);
            }
            if (UIReportFiles.GetPath(UIAIToolsHostWorkspaceInitializer.ReportsRoot + "/", "Report.csv") != UIAIToolsHostWorkspaceInitializer.ReportsRoot + "/Report.csv" || UIReportFiles.GetPath(UIAIToolsHostWorkspaceInitializer.ReportsRoot + "\\", "Report.csv") != UIAIToolsHostWorkspaceInitializer.ReportsRoot + "/Report.csv")
                throw new Exception("UI AI Tools report path contract failed.");
            Debug.Log("UI AI Tools report file contract validation passed.");
        }

        static void ValidateReport(string path, string expectedHeader)
        {
            if (!File.Exists(path))
                throw new Exception($"Missing UI AI Tools report: {path}");
            using (var reader = new StreamReader(path))
            {
                var header = reader.ReadLine();
                if (string.IsNullOrEmpty(header))
                    throw new Exception($"Empty UI AI Tools report: {path}");
                if (header != expectedHeader)
                    throw new Exception($"Unexpected UI AI Tools report header: {path}");
            }
            UIReportCsv.ReadRows(path);
        }

        static void ValidateHeaderContract(string report, string header)
        {
            var columns = header.Split(',');
            var emptyColumn = columns.FirstOrDefault(string.IsNullOrEmpty);
            if (emptyColumn != null)
                throw new Exception("Invalid UI AI Tools report header empty column: " + report);
            var duplicateColumn = columns.GroupBy(column => column).FirstOrDefault(group => group.Count() > 1);
            if (duplicateColumn != null)
                throw new Exception($"Duplicate UI AI Tools report header column: {report} {duplicateColumn.Key}");
        }

        static void ExpectCsvFailure(string root, string fileName, string[] lines, string expectedMessage)
        {
            var path = Path.Combine(root, fileName);
            File.WriteAllLines(path, lines, new UTF8Encoding(true));
            try
            {
                ValidateReport(path, lines[0]);
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains(expectedMessage))
                    return;
                throw new Exception($"Unexpected CSV contract failure for {fileName}: {ex.Message}");
            }
            throw new Exception("CSV contract sample did not fail: " + fileName);
        }

        static string LayoutNodeJson(string nodeId)
        {
            return "{\"nodeId\":\"" + nodeId + "\",\"name\":\"" + nodeId + "\",\"componentRole\":\"Panel\",\"componentId\":\"builtin:Panel\",\"anchor\":\"stretch_full\",\"position\":\"0,0\",\"size\":\"1080x1920\"}";
        }

        static string LayoutDraftJson(string nodes, string assets)
        {
            return LayoutDraftJson(nodes, assets, "[]", "[]");
        }

        static string LayoutDraftJson(string nodes, string assets, string interactions, string risks)
        {
            return "{\"root\":{\"name\":\"Bad\",\"uiType\":\"Dialog\",\"targetFolder\":\"Assets/UIAITools/Creation/Bad\",\"referenceResolution\":\"1080x1920\",\"safeAreaPolicy\":\"\"},\"nodes\":" + nodes + ",\"assets\":" + assets + ",\"interactions\":" + interactions + ",\"risks\":" + risks + ",\"requiresConfirmation\":true}";
        }

        static void ExpectJsonFailure(string root, string fileName, string json, Func<string, object> load, string expectedMessage)
        {
            var path = Path.Combine(root, fileName);
            File.WriteAllText(path, json, new UTF8Encoding(true));
            try
            {
                load(path);
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains(expectedMessage))
                    return;
                throw new Exception($"Unexpected JSON contract failure for {fileName}: {ex.Message}");
            }
            throw new Exception("JSON contract sample did not fail: " + fileName);
        }
    }
}

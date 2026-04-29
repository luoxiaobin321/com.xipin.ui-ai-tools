using System;
using System.IO;
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
                var briefPath = Path.Combine(root, "brief.json");
                File.WriteAllText(briefPath, UICreationBriefTemplateService.ToJsonWithRootArrays(new UICreationBrief
                {
                    featureName = "JsonContract",
                    uiType = "Dialog",
                    targetFolder = "Assets/Art/UI/AI/JsonContract",
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
                        targetFolder = "Assets/Art/UI/AI/JsonContract",
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

                var redesignPath = Path.Combine(root, "redesign.json");
                File.WriteAllText(redesignPath, UIRedesignDraftService.ToJsonWithRequiredArrays(new UIRedesignDraft
                {
                    draftPreviewPath = "Assets/Art/UI/AI/JsonContract/preview.png",
                    generatedImageFolder = "Assets/Art/UI/AI/JsonContract/Images"
                }), new UTF8Encoding(true));
                UIRedesignDraftService.LoadDraft(redesignPath);

                var externalPath = Path.Combine(root, "external.json");
                File.WriteAllText(externalPath, UICreationBriefTemplateService.ToJsonWithRootArrays(new UIReplacementExternalInputPackageService.ExternalInputPackage
                {
                    generatedAt = "2026-04-29 00:00:00",
                    sourcePrefabPath = "Assets/Bundle/Prefab/JsonContract/JsonContract.prefab",
                    sourcePreviewPath = "",
                    sourcePreviewReadiness = "Missing",
                    stylePrompt = "",
                    outputFolder = "Assets/Art/UI/AI/JsonContract",
                    briefPath = "Logs/brief.md",
                    draftJsonPath = "Logs/draft.json",
                    pendingInputsCsvPath = "Logs/pending.csv",
                    pendingInputReadinessCsvPath = "Logs/readiness.csv",
                    outputDirectories = new System.Collections.Generic.List<UIReplacementExternalInputPackageService.ExternalOutputDirectory>
                    {
                        new UIReplacementExternalInputPackageService.ExternalOutputDirectory { path = "Assets/Art/UI/AI/JsonContract", readiness = "Missing", inputKinds = "Preview", inputCount = 1 }
                    },
                    referenceInputs = new System.Collections.Generic.List<UIReplacementExternalInputPackageService.ExternalReferenceInput>
                    {
                        new UIReplacementExternalInputPackageService.ExternalReferenceInput { referenceKind = "Preview", path = "Logs/old.png", readiness = "Missing", width = "", height = "", copyFileName = "preview.png", itemIndices = "1", inputKinds = "Preview" }
                    },
                    inputs = new System.Collections.Generic.List<UIReplacementExternalInputPackageService.ExternalInput>
                    {
                        new UIReplacementExternalInputPackageService.ExternalInput
                        {
                            inputKind = "Preview",
                            pendingStatus = "PendingPreview",
                            readiness = "Missing",
                            outputPath = "Assets/Art/UI/AI/JsonContract/preview.png",
                            outputDirectory = "Assets/Art/UI/AI/JsonContract",
                            outputFileName = "preview.png",
                            outputWidth = "1080",
                            outputHeight = "1920",
                            actualWidth = "",
                            actualHeight = "",
                            sizeStatus = "Pending",
                            outputDirectoryReadiness = "Missing",
                            referencePath = "Logs/old.png",
                            targetAtlasPath = "",
                            referenceReadiness = "Missing",
                            referenceWidth = "",
                            referenceHeight = "",
                            referenceCopyFileName = "preview.png",
                            itemIndices = "1",
                            sourceAction = "Preview",
                            note = "",
                            taskPrompt = "Generate preview",
                            acceptanceCheck = "PNG"
                        }
                    }
                }, "outputDirectories", "referenceInputs", "inputs"), new UTF8Encoding(true));
                UIReplacementExternalInputPackageService.LoadPackageJson(externalPath);

                ExpectJsonFailure(root, "brief_missing_array.json", "{\"featureName\":\"Bad\",\"uiType\":\"Dialog\",\"targetFolder\":\"Assets/Art/UI/AI/Bad\",\"stylePrompt\":\"\",\"requiredInteractions\":[],\"dataBindings\":[],\"constraints\":[]}", UICreationBriefTemplateService.LoadBrief, "referenceImagePaths is required");
                ExpectJsonFailure(root, "brief_feature_name_not_string.json", "{\"featureName\":{},\"uiType\":\"Dialog\",\"targetFolder\":\"Assets/Art/UI/AI/Bad\",\"stylePrompt\":\"\",\"referenceImagePaths\":[],\"requiredInteractions\":[],\"dataBindings\":[],\"constraints\":[]}", UICreationBriefTemplateService.LoadBrief, "featureName must be a string");
                ExpectJsonFailure(root, "brief_feature_name_bad_escape.json", "{\"featureName\":\"Bad\\q\",\"uiType\":\"Dialog\",\"targetFolder\":\"Assets/Art/UI/AI/Bad\",\"stylePrompt\":\"\",\"referenceImagePaths\":[],\"requiredInteractions\":[],\"dataBindings\":[],\"constraints\":[]}", UICreationBriefTemplateService.LoadBrief, "string escape is invalid");
                ExpectJsonFailure(root, "brief_feature_name_trailing_token.json", "{\"featureName\":\"Bad\" x,\"uiType\":\"Dialog\",\"targetFolder\":\"Assets/Art/UI/AI/Bad\",\"stylePrompt\":\"\",\"referenceImagePaths\":[],\"requiredInteractions\":[],\"dataBindings\":[],\"constraints\":[]}", UICreationBriefTemplateService.LoadBrief, "featureName must be a string");
                ExpectJsonFailure(root, "brief_feature_name_duplicated.json", "{\"featureName\":\"Bad\",\"featureName\":\"Other\",\"uiType\":\"Dialog\",\"targetFolder\":\"Assets/Art/UI/AI/Bad\",\"stylePrompt\":\"\",\"referenceImagePaths\":[],\"requiredInteractions\":[],\"dataBindings\":[],\"constraints\":[]}", UICreationBriefTemplateService.LoadBrief, "featureName is duplicated");
                ExpectJsonFailure(root, "brief_root_leading_comma.json", "{,\"featureName\":\"Bad\",\"uiType\":\"Dialog\",\"targetFolder\":\"Assets/Art/UI/AI/Bad\",\"stylePrompt\":\"\",\"referenceImagePaths\":[],\"requiredInteractions\":[],\"dataBindings\":[],\"constraints\":[]}", UICreationBriefTemplateService.LoadBrief, "brief field is missing");
                ExpectJsonFailure(root, "brief_root_double_comma.json", "{\"featureName\":\"Bad\",,\"uiType\":\"Dialog\",\"targetFolder\":\"Assets/Art/UI/AI/Bad\",\"stylePrompt\":\"\",\"referenceImagePaths\":[],\"requiredInteractions\":[],\"dataBindings\":[],\"constraints\":[]}", UICreationBriefTemplateService.LoadBrief, "brief field is missing");
                ExpectJsonFailure(root, "brief_root_trailing_comma.json", "{\"featureName\":\"Bad\",\"uiType\":\"Dialog\",\"targetFolder\":\"Assets/Art/UI/AI/Bad\",\"stylePrompt\":\"\",\"referenceImagePaths\":[],\"requiredInteractions\":[],\"dataBindings\":[],\"constraints\":[],}", UICreationBriefTemplateService.LoadBrief, "brief field is missing");
                ExpectJsonFailure(root, "brief_style_prompt_not_string.json", "{\"featureName\":\"Bad\",\"uiType\":\"Dialog\",\"targetFolder\":\"Assets/Art/UI/AI/Bad\",\"stylePrompt\":{},\"referenceImagePaths\":[],\"requiredInteractions\":[],\"dataBindings\":[],\"constraints\":[]}", UICreationBriefTemplateService.LoadBrief, "stylePrompt must be a string");
                ExpectJsonFailure(root, "brief_requires_confirmation_not_boolean.json", "{\"featureName\":\"Bad\",\"uiType\":\"Dialog\",\"targetFolder\":\"Assets/Art/UI/AI/Bad\",\"stylePrompt\":\"\",\"requiresConfirmation\":{},\"referenceImagePaths\":[],\"requiredInteractions\":[],\"dataBindings\":[],\"constraints\":[]}", UICreationBriefTemplateService.LoadBrief, "requiresConfirmation must be a boolean");
                ExpectJsonFailure(root, "brief_requires_confirmation_bad_literal.json", "{\"featureName\":\"Bad\",\"uiType\":\"Dialog\",\"targetFolder\":\"Assets/Art/UI/AI/Bad\",\"stylePrompt\":\"\",\"requiresConfirmation\":truex,\"referenceImagePaths\":[],\"requiredInteractions\":[],\"dataBindings\":[],\"constraints\":[]}", UICreationBriefTemplateService.LoadBrief, "requiresConfirmation must be a boolean");
                ExpectJsonFailure(root, "brief_requires_confirmation_trailing_token.json", "{\"featureName\":\"Bad\",\"uiType\":\"Dialog\",\"targetFolder\":\"Assets/Art/UI/AI/Bad\",\"stylePrompt\":\"\",\"requiresConfirmation\":true x,\"referenceImagePaths\":[],\"requiredInteractions\":[],\"dataBindings\":[],\"constraints\":[]}", UICreationBriefTemplateService.LoadBrief, "requiresConfirmation must be a boolean");
                ExpectJsonFailure(root, "brief_reference_image_item_not_string.json", "{\"featureName\":\"Bad\",\"uiType\":\"Dialog\",\"targetFolder\":\"Assets/Art/UI/AI/Bad\",\"stylePrompt\":\"\",\"referenceImagePaths\":[{}],\"requiredInteractions\":[],\"dataBindings\":[],\"constraints\":[]}", UICreationBriefTemplateService.LoadBrief, "referenceImagePaths item must be a string");
                ExpectJsonFailure(root, "brief_reference_image_missing_comma.json", "{\"featureName\":\"Bad\",\"uiType\":\"Dialog\",\"targetFolder\":\"Assets/Art/UI/AI/Bad\",\"stylePrompt\":\"\",\"referenceImagePaths\":[\"Assets/A.png\" \"Assets/B.png\"],\"requiredInteractions\":[],\"dataBindings\":[],\"constraints\":[]}", UICreationBriefTemplateService.LoadBrief, "referenceImagePaths items must be separated by commas");
                ExpectJsonFailure(root, "brief_reference_image_field_missing_comma.json", "{\"featureName\":\"Bad\",\"uiType\":\"Dialog\",\"targetFolder\":\"Assets/Art/UI/AI/Bad\",\"stylePrompt\":\"\",\"referenceImagePaths\":[] \"requiredInteractions\":[],\"dataBindings\":[],\"constraints\":[]}", UICreationBriefTemplateService.LoadBrief, "referenceImagePaths must be an array");
                ExpectJsonFailure(root, "brief_trailing_content.json", "{\"featureName\":\"Bad\",\"uiType\":\"Dialog\",\"targetFolder\":\"Assets/Art/UI/AI/Bad\",\"stylePrompt\":\"\",\"referenceImagePaths\":[],\"requiredInteractions\":[],\"dataBindings\":[],\"constraints\":[]}{}", UICreationBriefTemplateService.LoadBrief, "trailing content");
                ExpectJsonFailure(root, "layout_missing_confirmation.json", "{\"root\":{\"name\":\"Bad\",\"uiType\":\"Dialog\",\"targetFolder\":\"Assets/Art/UI/AI/Bad\",\"referenceResolution\":\"1080x1920\",\"safeAreaPolicy\":\"\"},\"nodes\":[],\"assets\":[],\"interactions\":[],\"risks\":[]}", UILayoutDraftTemplateService.LoadDraft, "requiresConfirmation is required");
                ExpectJsonFailure(root, "layout_root_duplicated.json", "{\"root\":{\"name\":\"Bad\",\"uiType\":\"Dialog\",\"targetFolder\":\"Assets/Art/UI/AI/Bad\",\"referenceResolution\":\"1080x1920\",\"safeAreaPolicy\":\"\"},\"root\":{\"name\":\"Other\",\"uiType\":\"Dialog\",\"targetFolder\":\"Assets/Art/UI/AI/Bad\",\"referenceResolution\":\"1080x1920\",\"safeAreaPolicy\":\"\"},\"nodes\":[],\"assets\":[],\"interactions\":[],\"risks\":[],\"requiresConfirmation\":true}", UILayoutDraftTemplateService.LoadDraft, "root is duplicated");
                ExpectJsonFailure(root, "layout_root_name_trailing_token.json", "{\"root\":{\"name\":\"Bad\" x,\"uiType\":\"Dialog\",\"targetFolder\":\"Assets/Art/UI/AI/Bad\",\"referenceResolution\":\"1080x1920\",\"safeAreaPolicy\":\"\"},\"nodes\":[],\"assets\":[],\"interactions\":[],\"risks\":[],\"requiresConfirmation\":true}", UILayoutDraftTemplateService.LoadDraft, "root.name must be a string");
                ExpectJsonFailure(root, "layout_root_trailing_comma.json", "{\"root\":{\"name\":\"Bad\",\"uiType\":\"Dialog\",\"targetFolder\":\"Assets/Art/UI/AI/Bad\",\"referenceResolution\":\"1080x1920\",\"safeAreaPolicy\":\"\",},\"nodes\":[],\"assets\":[],\"interactions\":[],\"risks\":[],\"requiresConfirmation\":true}", UILayoutDraftTemplateService.LoadDraft, "root field is missing");
                ExpectJsonFailure(root, "layout_root_field_missing_comma.json", "{\"root\":{\"name\":\"Bad\",\"uiType\":\"Dialog\",\"targetFolder\":\"Assets/Art/UI/AI/Bad\",\"referenceResolution\":\"1080x1920\",\"safeAreaPolicy\":\"\"} \"nodes\":[],\"assets\":[],\"interactions\":[],\"risks\":[],\"requiresConfirmation\":true}", UILayoutDraftTemplateService.LoadDraft, "root must be an object");
                ExpectJsonFailure(root, "layout_root_target_folder_not_string.json", "{\"root\":{\"name\":\"Bad\",\"uiType\":\"Dialog\",\"targetFolder\":{},\"referenceResolution\":\"1080x1920\",\"safeAreaPolicy\":\"\"},\"nodes\":[],\"assets\":[],\"interactions\":[],\"risks\":[],\"requiresConfirmation\":true}", UILayoutDraftTemplateService.LoadDraft, "root.targetFolder must be a string");
                ExpectJsonFailure(root, "layout_requires_confirmation_not_boolean.json", "{\"root\":{\"name\":\"Bad\",\"uiType\":\"Dialog\",\"targetFolder\":\"Assets/Art/UI/AI/Bad\",\"referenceResolution\":\"1080x1920\",\"safeAreaPolicy\":\"\"},\"nodes\":[],\"assets\":[],\"interactions\":[],\"risks\":[],\"requiresConfirmation\":{}}", UILayoutDraftTemplateService.LoadDraft, "requiresConfirmation must be a boolean");
                ExpectJsonFailure(root, "layout_requires_confirmation_bad_literal.json", "{\"root\":{\"name\":\"Bad\",\"uiType\":\"Dialog\",\"targetFolder\":\"Assets/Art/UI/AI/Bad\",\"referenceResolution\":\"1080x1920\",\"safeAreaPolicy\":\"\"},\"nodes\":[],\"assets\":[],\"interactions\":[],\"risks\":[],\"requiresConfirmation\":truex}", UILayoutDraftTemplateService.LoadDraft, "requiresConfirmation must be a boolean");
                ExpectJsonFailure(root, "layout_assets_not_array.json", "{\"root\":{\"name\":\"Bad\",\"uiType\":\"Dialog\",\"targetFolder\":\"Assets/Art/UI/AI/Bad\",\"referenceResolution\":\"1080x1920\",\"safeAreaPolicy\":\"\"},\"nodes\":[],\"assets\":{},\"interactions\":[],\"risks\":[],\"requiresConfirmation\":true}", UILayoutDraftTemplateService.LoadDraft, "assets must be an array");
                ExpectJsonFailure(root, "layout_node_name_not_string.json", LayoutDraftJson("[{\"nodeId\":\"Root\",\"name\":{},\"componentRole\":\"Panel\",\"componentId\":\"builtin:Panel\",\"anchor\":\"stretch_full\",\"position\":\"0,0\",\"size\":\"1080x1920\"}]", "[]"), UILayoutDraftTemplateService.LoadDraft, "nodes.name must be a string");
                ExpectJsonFailure(root, "layout_node_id_duplicated.json", LayoutDraftJson("[{\"nodeId\":\"Root\",\"nodeId\":\"Other\",\"name\":\"Root\",\"componentRole\":\"Panel\",\"componentId\":\"builtin:Panel\",\"anchor\":\"stretch_full\",\"position\":\"0,0\",\"size\":\"1080x1920\"}]", "[]"), UILayoutDraftTemplateService.LoadDraft, "nodeId is duplicated");
                ExpectJsonFailure(root, "layout_node_parent_id_not_string.json", LayoutDraftJson("[{\"nodeId\":\"Root\",\"parentId\":{},\"name\":\"Root\",\"componentRole\":\"Panel\",\"componentId\":\"builtin:Panel\",\"anchor\":\"stretch_full\",\"position\":\"0,0\",\"size\":\"1080x1920\"}]", "[]"), UILayoutDraftTemplateService.LoadDraft, "nodes.parentId must be a string");
                ExpectJsonFailure(root, "layout_node_missing_comma.json", LayoutDraftJson("[" + LayoutNodeJson("Root") + " " + LayoutNodeJson("Child") + "]", "[]"), UILayoutDraftTemplateService.LoadDraft, "nodes items must be separated by commas");
                ExpectJsonFailure(root, "layout_asset_status_not_string.json", LayoutDraftJson("[]", "[{\"needId\":\"BindingTitle\",\"kind\":\"DataBinding\",\"path\":\"\",\"source\":\"Brief\",\"status\":{},\"reason\":\"title\"}]"), UILayoutDraftTemplateService.LoadDraft, "assets.status must be a string");
                ExpectJsonFailure(root, "layout_asset_need_id_duplicated.json", LayoutDraftJson("[]", "[{\"needId\":\"BindingTitle\",\"needId\":\"Other\",\"kind\":\"DataBinding\",\"path\":\"\",\"source\":\"Brief\",\"status\":\"NeedsReview\",\"reason\":\"title\"}]"), UILayoutDraftTemplateService.LoadDraft, "needId is duplicated");
                ExpectJsonFailure(root, "layout_interaction_item_not_string.json", LayoutDraftJson("[]", "[]", "[{}]", "[]"), UILayoutDraftTemplateService.LoadDraft, "interactions item must be a string");
                ExpectJsonFailure(root, "redesign_draft_preview_not_string.json", "{\"draftPreviewPath\":{},\"generatedImageFolder\":\"Assets/Art/UI/AI/Bad/Images\",\"replacementPlan\":{\"items\":[]},\"risks\":[]}", UIRedesignDraftService.LoadDraft, "draftPreviewPath must be a string");
                ExpectJsonFailure(root, "redesign_draft_preview_trailing_token.json", "{\"draftPreviewPath\":\"Assets/Art/UI/AI/Bad/preview.png\" x,\"generatedImageFolder\":\"Assets/Art/UI/AI/Bad/Images\",\"replacementPlan\":{\"items\":[]},\"risks\":[]}", UIRedesignDraftService.LoadDraft, "draftPreviewPath must be a string");
                ExpectJsonFailure(root, "redesign_missing_items.json", "{\"draftPreviewPath\":\"Assets/Art/UI/AI/Bad/preview.png\",\"generatedImageFolder\":\"Assets/Art/UI/AI/Bad/Images\",\"replacementPlan\":{},\"risks\":[]}", UIRedesignDraftService.LoadDraft, "replacementPlan.items is required");
                ExpectJsonFailure(root, "redesign_replacement_plan_not_object.json", "{\"draftPreviewPath\":\"Assets/Art/UI/AI/Bad/preview.png\",\"generatedImageFolder\":\"Assets/Art/UI/AI/Bad/Images\",\"replacementPlan\":[],\"risks\":[]}", UIRedesignDraftService.LoadDraft, "replacementPlan must be an object");
                ExpectJsonFailure(root, "redesign_replacement_plan_duplicated.json", "{\"draftPreviewPath\":\"Assets/Art/UI/AI/Bad/preview.png\",\"generatedImageFolder\":\"Assets/Art/UI/AI/Bad/Images\",\"replacementPlan\":{\"items\":[]},\"replacementPlan\":{\"items\":[]},\"risks\":[]}", UIRedesignDraftService.LoadDraft, "replacementPlan is duplicated");
                ExpectJsonFailure(root, "redesign_replacement_plan_field_missing_comma.json", "{\"draftPreviewPath\":\"Assets/Art/UI/AI/Bad/preview.png\",\"generatedImageFolder\":\"Assets/Art/UI/AI/Bad/Images\",\"replacementPlan\":{\"items\":[]} \"risks\":[]}", UIRedesignDraftService.LoadDraft, "replacementPlan must be an object");
                ExpectJsonFailure(root, "redesign_replacement_plan_trailing_comma.json", "{\"draftPreviewPath\":\"Assets/Art/UI/AI/Bad/preview.png\",\"generatedImageFolder\":\"Assets/Art/UI/AI/Bad/Images\",\"replacementPlan\":{\"items\":[],},\"risks\":[]}", UIRedesignDraftService.LoadDraft, "replacementPlan field is missing");
                ExpectJsonFailure(root, "redesign_item_old_asset_not_string.json", RedesignDraftJson("[{\"oldAssetPath\":{},\"newAssetPath\":\"Assets/Art/UI/AI/Bad/Images/new.png\",\"targetAtlasPath\":\"Assets/Art/UI/AI/Bad/Bad.spriteatlasv2\"}]"), UIRedesignDraftService.LoadDraft, "replacementPlan.items[0].oldAssetPath must be a string");
                ExpectJsonFailure(root, "redesign_item_old_asset_duplicated.json", RedesignDraftJson("[{\"oldAssetPath\":\"Assets/Art/UI/Old.png\",\"oldAssetPath\":\"Assets/Art/UI/Other.png\",\"newAssetPath\":\"Assets/Art/UI/AI/Bad/Images/new.png\",\"targetAtlasPath\":\"Assets/Art/UI/AI/Bad/Bad.spriteatlasv2\"}]"), UIRedesignDraftService.LoadDraft, "oldAssetPath is duplicated");
                ExpectJsonFailure(root, "redesign_item_new_asset_not_string.json", RedesignDraftJson("[{\"oldAssetPath\":\"Assets/Art/UI/Old.png\",\"newAssetPath\":{},\"targetAtlasPath\":\"Assets/Art/UI/AI/Bad/Bad.spriteatlasv2\"}]"), UIRedesignDraftService.LoadDraft, "replacementPlan.items[0].newAssetPath must be a string");
                ExpectJsonFailure(root, "redesign_item_new_asset_duplicate.json", RedesignDraftJson("[{\"oldAssetPath\":\"Assets/Art/UI/OldA.png\",\"newAssetPath\":\"Assets/Art/UI/AI/Bad/Images/New.png\",\"targetAtlasPath\":\"Assets/Art/UI/AI/Bad/Bad.spriteatlasv2\"},{\"oldAssetPath\":\"Assets/Art/UI/OldB.png\",\"newAssetPath\":\"Assets/Art/UI/AI/Bad/Images/New.png\",\"targetAtlasPath\":\"Assets/Art/UI/AI/Bad/Bad.spriteatlasv2\"}]"), UIRedesignDraftService.LoadDraft, "newAssetPath duplicates");
                ExpectJsonFailure(root, "redesign_item_target_atlas_not_string.json", RedesignDraftJson("[{\"oldAssetPath\":\"Assets/Art/UI/Old.png\",\"newAssetPath\":\"Assets/Art/UI/AI/Bad/Images/new.png\",\"targetAtlasPath\":{}}]"), UIRedesignDraftService.LoadDraft, "replacementPlan.items[0].targetAtlasPath must be a string");
                ExpectJsonFailure(root, "redesign_item_trailing_comma.json", RedesignDraftJson("[{\"oldAssetPath\":\"Assets/Art/UI/Old.png\",\"newAssetPath\":\"Assets/Art/UI/AI/Bad/Images/new.png\",\"targetAtlasPath\":\"Assets/Art/UI/AI/Bad/Bad.spriteatlasv2\",}]"), UIRedesignDraftService.LoadDraft, "replacementPlan.items[0] field is missing");
                ExpectJsonFailure(root, "redesign_items_missing_comma.json", RedesignDraftJson("[" + RedesignDraftItemJson("OldA") + " " + RedesignDraftItemJson("OldB") + "]"), UIRedesignDraftService.LoadDraft, "replacementPlan.items items must be separated by commas");
                ExpectJsonFailure(root, "redesign_requires_confirmation_not_boolean.json", "{\"draftPreviewPath\":\"Assets/Art/UI/AI/Bad/preview.png\",\"generatedImageFolder\":\"Assets/Art/UI/AI/Bad/Images\",\"replacementPlan\":{\"items\":[]},\"requiresConfirmation\":{},\"risks\":[]}", UIRedesignDraftService.LoadDraft, "requiresConfirmation must be a boolean");
                ExpectJsonFailure(root, "redesign_requires_confirmation_bad_literal.json", "{\"draftPreviewPath\":\"Assets/Art/UI/AI/Bad/preview.png\",\"generatedImageFolder\":\"Assets/Art/UI/AI/Bad/Images\",\"replacementPlan\":{\"items\":[]},\"requiresConfirmation\":truex,\"risks\":[]}", UIRedesignDraftService.LoadDraft, "requiresConfirmation must be a boolean");
                ExpectJsonFailure(root, "redesign_requires_confirmation_trailing_token.json", "{\"draftPreviewPath\":\"Assets/Art/UI/AI/Bad/preview.png\",\"generatedImageFolder\":\"Assets/Art/UI/AI/Bad/Images\",\"replacementPlan\":{\"items\":[]},\"requiresConfirmation\":true x,\"risks\":[]}", UIRedesignDraftService.LoadDraft, "requiresConfirmation must be a boolean");
                ExpectJsonFailure(root, "redesign_item_requires_confirmation_not_boolean.json", RedesignDraftJson("[{\"oldAssetPath\":\"Assets/Art/UI/Old.png\",\"newAssetPath\":\"Assets/Art/UI/AI/Bad/Images/new.png\",\"targetAtlasPath\":\"Assets/Art/UI/AI/Bad/Bad.spriteatlasv2\",\"requiresConfirmation\":{}}]"), UIRedesignDraftService.LoadDraft, "replacementPlan.items[0].requiresConfirmation must be a boolean");
                ExpectJsonFailure(root, "redesign_item_requires_confirmation_bad_literal.json", RedesignDraftJson("[{\"oldAssetPath\":\"Assets/Art/UI/Old.png\",\"newAssetPath\":\"Assets/Art/UI/AI/Bad/Images/new.png\",\"targetAtlasPath\":\"Assets/Art/UI/AI/Bad/Bad.spriteatlasv2\",\"requiresConfirmation\":truex}]"), UIRedesignDraftService.LoadDraft, "replacementPlan.items[0].requiresConfirmation must be a boolean");
                ExpectJsonFailure(root, "redesign_item_preserve_guid_not_boolean.json", RedesignDraftJson("[{\"oldAssetPath\":\"Assets/Art/UI/Old.png\",\"newAssetPath\":\"Assets/Art/UI/AI/Bad/Images/new.png\",\"targetAtlasPath\":\"Assets/Art/UI/AI/Bad/Bad.spriteatlasv2\",\"preserveGuid\":{}}]"), UIRedesignDraftService.LoadDraft, "replacementPlan.items[0].preserveGuid must be a boolean");
                ExpectJsonFailure(root, "redesign_item_reason_not_string.json", RedesignDraftJson("[{\"oldAssetPath\":\"Assets/Art/UI/Old.png\",\"newAssetPath\":\"Assets/Art/UI/AI/Bad/Images/new.png\",\"targetAtlasPath\":\"Assets/Art/UI/AI/Bad/Bad.spriteatlasv2\",\"reason\":{}}]"), UIRedesignDraftService.LoadDraft, "replacementPlan.items[0].reason must be a string");
                ExpectJsonFailure(root, "redesign_risks_not_array.json", "{\"draftPreviewPath\":\"Assets/Art/UI/AI/Bad/preview.png\",\"generatedImageFolder\":\"Assets/Art/UI/AI/Bad/Images\",\"replacementPlan\":{\"items\":[]},\"risks\":{}}", UIRedesignDraftService.LoadDraft, "risks must be an array");
                ExpectJsonFailure(root, "redesign_risk_item_not_string.json", RedesignDraftJson("[]", "[{}]"), UIRedesignDraftService.LoadDraft, "risks item must be a string");
                ExpectJsonFailure(root, "redesign_risk_bad_unicode_escape.json", RedesignDraftJson("[]", "[\"risk\\u12G4\"]"), UIRedesignDraftService.LoadDraft, "unicode escape is invalid");
                ExpectJsonFailure(root, "redesign_risk_trailing_comma.json", RedesignDraftJson("[]", "[\"risk\",]"), UIRedesignDraftService.LoadDraft, "risks item is missing");
                ExpectJsonFailure(root, "external_missing_generated_at.json", "{\"sourcePrefabPath\":\"Assets/Bundle/Prefab/Bad/Bad.prefab\",\"sourcePreviewPath\":\"\",\"sourcePreviewReadiness\":\"Missing\",\"stylePrompt\":\"\",\"outputFolder\":\"Assets/Art/UI/AI/Bad\",\"briefPath\":\"Logs/brief.md\",\"draftJsonPath\":\"Logs/draft.json\",\"pendingInputsCsvPath\":\"Logs/pending.csv\",\"pendingInputReadinessCsvPath\":\"Logs/readiness.csv\",\"outputDirectories\":[],\"referenceInputs\":[],\"inputs\":[]}", UIReplacementExternalInputPackageService.LoadPackageJson, "generatedAt is required");
                ExpectJsonFailure(root, "external_generated_at_not_string.json", "{\"generatedAt\":[],\"sourcePrefabPath\":\"Assets/Bundle/Prefab/Bad/Bad.prefab\",\"sourcePreviewPath\":\"\",\"sourcePreviewReadiness\":\"Missing\",\"stylePrompt\":\"\",\"outputFolder\":\"Assets/Art/UI/AI/Bad\",\"briefPath\":\"Logs/brief.md\",\"draftJsonPath\":\"Logs/draft.json\",\"pendingInputsCsvPath\":\"Logs/pending.csv\",\"pendingInputReadinessCsvPath\":\"Logs/readiness.csv\",\"outputDirectories\":[],\"referenceInputs\":[],\"inputs\":[]}", UIReplacementExternalInputPackageService.LoadPackageJson, "generatedAt must be a string");
                ExpectJsonFailure(root, "external_generated_at_trailing_token.json", "{\"generatedAt\":\"2026-04-29 00:00:00\" x,\"sourcePrefabPath\":\"Assets/Bundle/Prefab/Bad/Bad.prefab\",\"sourcePreviewPath\":\"\",\"sourcePreviewReadiness\":\"Missing\",\"stylePrompt\":\"\",\"outputFolder\":\"Assets/Art/UI/AI/Bad\",\"briefPath\":\"Logs/brief.md\",\"draftJsonPath\":\"Logs/draft.json\",\"pendingInputsCsvPath\":\"Logs/pending.csv\",\"pendingInputReadinessCsvPath\":\"Logs/readiness.csv\",\"outputDirectories\":[],\"referenceInputs\":[],\"inputs\":[]}", UIReplacementExternalInputPackageService.LoadPackageJson, "generatedAt must be a string");
                ExpectJsonFailure(root, "external_missing_inputs.json", "{\"generatedAt\":\"2026-04-29 00:00:00\",\"sourcePrefabPath\":\"Assets/Bundle/Prefab/Bad/Bad.prefab\",\"sourcePreviewPath\":\"\",\"sourcePreviewReadiness\":\"Missing\",\"stylePrompt\":\"\",\"outputFolder\":\"Assets/Art/UI/AI/Bad\",\"briefPath\":\"Logs/brief.md\",\"draftJsonPath\":\"Logs/draft.json\",\"pendingInputsCsvPath\":\"Logs/pending.csv\",\"pendingInputReadinessCsvPath\":\"Logs/readiness.csv\",\"outputDirectories\":[],\"referenceInputs\":[]}", UIReplacementExternalInputPackageService.LoadPackageJson, "inputs is required");
                ExpectJsonFailure(root, "external_inputs_duplicated.json", "{\"generatedAt\":\"2026-04-29 00:00:00\",\"sourcePrefabPath\":\"Assets/Bundle/Prefab/Bad/Bad.prefab\",\"sourcePreviewPath\":\"\",\"sourcePreviewReadiness\":\"Missing\",\"stylePrompt\":\"\",\"outputFolder\":\"Assets/Art/UI/AI/Bad\",\"briefPath\":\"Logs/brief.md\",\"draftJsonPath\":\"Logs/draft.json\",\"pendingInputsCsvPath\":\"Logs/pending.csv\",\"pendingInputReadinessCsvPath\":\"Logs/readiness.csv\",\"outputDirectories\":[],\"referenceInputs\":[],\"inputs\":[],\"inputs\":[]}", UIReplacementExternalInputPackageService.LoadPackageJson, "inputs is duplicated");
                ExpectJsonFailure(root, "external_output_directories_not_array.json", "{\"generatedAt\":\"2026-04-29 00:00:00\",\"sourcePrefabPath\":\"Assets/Bundle/Prefab/Bad/Bad.prefab\",\"sourcePreviewPath\":\"\",\"sourcePreviewReadiness\":\"Missing\",\"stylePrompt\":\"\",\"outputFolder\":\"Assets/Art/UI/AI/Bad\",\"briefPath\":\"Logs/brief.md\",\"draftJsonPath\":\"Logs/draft.json\",\"pendingInputsCsvPath\":\"Logs/pending.csv\",\"pendingInputReadinessCsvPath\":\"Logs/readiness.csv\",\"outputDirectories\":{},\"referenceInputs\":[],\"inputs\":[]}", UIReplacementExternalInputPackageService.LoadPackageJson, "outputDirectories must be an array");
                ExpectJsonFailure(root, "external_output_directories_field_missing_comma.json", "{\"generatedAt\":\"2026-04-29 00:00:00\",\"sourcePrefabPath\":\"Assets/Bundle/Prefab/Bad/Bad.prefab\",\"sourcePreviewPath\":\"\",\"sourcePreviewReadiness\":\"Missing\",\"stylePrompt\":\"\",\"outputFolder\":\"Assets/Art/UI/AI/Bad\",\"briefPath\":\"Logs/brief.md\",\"draftJsonPath\":\"Logs/draft.json\",\"pendingInputsCsvPath\":\"Logs/pending.csv\",\"pendingInputReadinessCsvPath\":\"Logs/readiness.csv\",\"outputDirectories\":[] \"referenceInputs\":[],\"inputs\":[]}", UIReplacementExternalInputPackageService.LoadPackageJson, "outputDirectories must be an array");
                ExpectJsonFailure(root, "external_reference_inputs_not_array.json", "{\"generatedAt\":\"2026-04-29 00:00:00\",\"sourcePrefabPath\":\"Assets/Bundle/Prefab/Bad/Bad.prefab\",\"sourcePreviewPath\":\"\",\"sourcePreviewReadiness\":\"Missing\",\"stylePrompt\":\"\",\"outputFolder\":\"Assets/Art/UI/AI/Bad\",\"briefPath\":\"Logs/brief.md\",\"draftJsonPath\":\"Logs/draft.json\",\"pendingInputsCsvPath\":\"Logs/pending.csv\",\"pendingInputReadinessCsvPath\":\"Logs/readiness.csv\",\"outputDirectories\":[],\"referenceInputs\":{},\"inputs\":[]}", UIReplacementExternalInputPackageService.LoadPackageJson, "referenceInputs must be an array");
                ExpectJsonFailure(root, "external_inputs_not_array.json", "{\"generatedAt\":\"2026-04-29 00:00:00\",\"sourcePrefabPath\":\"Assets/Bundle/Prefab/Bad/Bad.prefab\",\"sourcePreviewPath\":\"\",\"sourcePreviewReadiness\":\"Missing\",\"stylePrompt\":\"\",\"outputFolder\":\"Assets/Art/UI/AI/Bad\",\"briefPath\":\"Logs/brief.md\",\"draftJsonPath\":\"Logs/draft.json\",\"pendingInputsCsvPath\":\"Logs/pending.csv\",\"pendingInputReadinessCsvPath\":\"Logs/readiness.csv\",\"outputDirectories\":[],\"referenceInputs\":[],\"inputs\":{}}", UIReplacementExternalInputPackageService.LoadPackageJson, "inputs must be an array");
                ExpectJsonFailure(root, "external_output_directory_path_not_string.json", ExternalPackageJson("[" + ExternalOutputDirectoryItem("{}", "0") + "]", "[]", "[]"), UIReplacementExternalInputPackageService.LoadPackageJson, "outputDirectories.path must be a string");
                ExpectJsonFailure(root, "external_output_directory_path_duplicated.json", ExternalPackageJson("[{\"path\":\"Assets/Art/UI/AI/Bad\",\"path\":\"Assets/Art/UI/AI/Other\",\"readiness\":\"Missing\",\"inputCount\":0,\"notReadyCount\":0,\"missingCount\":0,\"invalidCount\":0,\"inputKinds\":\"Preview\"}]", "[]", "[]"), UIReplacementExternalInputPackageService.LoadPackageJson, "path is duplicated");
                ExpectJsonFailure(root, "external_output_directory_item_duplicated.json", ExternalPackageJson("[" + ExternalOutputDirectoryItem("\"Assets/Art/UI/AI/Bad\"", "0") + "," + ExternalOutputDirectoryItem("\"Assets/Art/UI/AI/Bad\"", "0") + "]", "[]", "[]"), UIReplacementExternalInputPackageService.LoadPackageJson, "duplicate output directory");
                ExpectJsonFailure(root, "external_output_directory_input_count_not_integer.json", ExternalPackageJson("[" + ExternalOutputDirectoryItem("\"Assets/Art/UI/AI/Bad\"", "{}") + "]", "[]", "[]"), UIReplacementExternalInputPackageService.LoadPackageJson, "outputDirectories.inputCount must be an integer");
                ExpectJsonFailure(root, "external_reference_input_path_not_string.json", ExternalPackageJson("[]", "[" + ExternalReferenceInputItem("{}", "1") + "]", "[]"), UIReplacementExternalInputPackageService.LoadPackageJson, "referenceInputs.path must be a string");
                ExpectJsonFailure(root, "external_reference_input_path_duplicated.json", ExternalPackageJson("[]", "[{\"referenceKind\":\"Preview\",\"path\":\"Logs/old.png\",\"path\":\"Logs/other.png\",\"readiness\":\"Missing\",\"width\":\"\",\"height\":\"\",\"copyFileName\":\"preview.png\",\"useCount\":1,\"itemIndices\":\"1\",\"inputKinds\":\"Preview\"}]", "[]"), UIReplacementExternalInputPackageService.LoadPackageJson, "path is duplicated");
                ExpectJsonFailure(root, "external_reference_input_item_duplicated.json", ExternalPackageJson("[]", "[" + ExternalReferenceInputItem("\"Logs/old.png\"", "1") + "," + ExternalReferenceInputItem("\"Logs/old.png\"", "1") + "]", "[]"), UIReplacementExternalInputPackageService.LoadPackageJson, "duplicate reference input");
                ExpectJsonFailure(root, "external_reference_input_use_count_not_integer.json", ExternalPackageJson("[]", "[" + ExternalReferenceInputItem("\"Logs/old.png\"", "{}") + "]", "[]"), UIReplacementExternalInputPackageService.LoadPackageJson, "referenceInputs.useCount must be an integer");
                ExpectJsonFailure(root, "external_input_output_path_not_string.json", ExternalPackageJson("[]", "[]", "[" + ExternalInputItem("{}", "1") + "]"), UIReplacementExternalInputPackageService.LoadPackageJson, "inputs.outputPath must be a string");
                ExpectJsonFailure(root, "external_input_output_path_duplicated.json", ExternalPackageJson("[]", "[]", "[" + ExternalInputItem("\"Assets/Art/UI/AI/Bad/preview.png\"", "1").Replace("\"outputDirectory\"", "\"outputPath\":\"Assets/Art/UI/AI/Bad/other.png\",\"outputDirectory\"") + "]"), UIReplacementExternalInputPackageService.LoadPackageJson, "outputPath is duplicated");
                ExpectJsonFailure(root, "external_input_item_duplicated.json", ExternalPackageJson("[]", "[]", "[" + ExternalInputItem("\"Assets/Art/UI/AI/Bad/preview.png\"", "1") + "," + ExternalInputItem("\"Assets/Art/UI/AI/Bad/preview.png\"", "1") + "]"), UIReplacementExternalInputPackageService.LoadPackageJson, "duplicate input");
                ExpectJsonFailure(root, "external_input_count_not_integer.json", ExternalPackageJson("[]", "[]", "[" + ExternalInputItem("\"Assets/Art/UI/AI/Bad/preview.png\"", "{}") + "]"), UIReplacementExternalInputPackageService.LoadPackageJson, "inputs.count must be an integer");
                ExpectJsonFailure(root, "external_input_count_trailing_token.json", ExternalPackageJson("[]", "[]", "[" + ExternalInputItem("\"Assets/Art/UI/AI/Bad/preview.png\"", "1 x") + "]"), UIReplacementExternalInputPackageService.LoadPackageJson, "inputs.count must be an integer");
                ExpectJsonFailure(root, "external_input_count_leading_zero.json", ExternalPackageJson("[]", "[]", "[" + ExternalInputItem("\"Assets/Art/UI/AI/Bad/preview.png\"", "01") + "]"), UIReplacementExternalInputPackageService.LoadPackageJson, "inputs.count must be an integer");
                ExpectJsonFailure(root, "external_input_count_non_ascii_digit.json", ExternalPackageJson("[]", "[]", "[" + ExternalInputItem("\"Assets/Art/UI/AI/Bad/preview.png\"", "\u0661") + "]"), UIReplacementExternalInputPackageService.LoadPackageJson, "inputs.count must be an integer");
                ExpectJsonFailure(root, "external_input_trailing_comma.json", ExternalPackageJson("[]", "[]", "[" + ExternalInputItem("\"Assets/Art/UI/AI/Bad/preview.png\"", "1").Replace("}", ",}") + "]"), UIReplacementExternalInputPackageService.LoadPackageJson, "inputs item field is missing");
                ExpectJsonFailure(root, "external_trailing_content.json", "{\"generatedAt\":\"2026-04-29 00:00:00\",\"sourcePrefabPath\":\"Assets/Bundle/Prefab/Bad/Bad.prefab\",\"sourcePreviewPath\":\"\",\"sourcePreviewReadiness\":\"Missing\",\"stylePrompt\":\"\",\"outputFolder\":\"Assets/Art/UI/AI/Bad\",\"briefPath\":\"Logs/brief.md\",\"draftJsonPath\":\"Logs/draft.json\",\"pendingInputsCsvPath\":\"Logs/pending.csv\",\"pendingInputReadinessCsvPath\":\"Logs/readiness.csv\",\"outputDirectories\":[],\"referenceInputs\":[],\"inputs\":[]}{}", UIReplacementExternalInputPackageService.LoadPackageJson, "trailing content");
            }
            finally
            {
                if (Directory.Exists(root))
                    Directory.Delete(root, true);
            }
            Debug.Log("UI AI Tools JSON contract validation passed.");
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

        static string ExternalPackageJson(string outputDirectories, string referenceInputs, string inputs)
        {
            return "{\"generatedAt\":\"2026-04-29 00:00:00\",\"sourcePrefabPath\":\"Assets/Bundle/Prefab/Bad/Bad.prefab\",\"sourcePreviewPath\":\"\",\"sourcePreviewReadiness\":\"Missing\",\"stylePrompt\":\"\",\"outputFolder\":\"Assets/Art/UI/AI/Bad\",\"briefPath\":\"Logs/brief.md\",\"draftJsonPath\":\"Logs/draft.json\",\"pendingInputsCsvPath\":\"Logs/pending.csv\",\"pendingInputReadinessCsvPath\":\"Logs/readiness.csv\",\"outputDirectories\":" + outputDirectories + ",\"referenceInputs\":" + referenceInputs + ",\"inputs\":" + inputs + "}";
        }

        static string ExternalOutputDirectoryItem(string path, string inputCount)
        {
            return "{\"path\":" + path + ",\"readiness\":\"Missing\",\"inputCount\":" + inputCount + ",\"notReadyCount\":0,\"missingCount\":0,\"invalidCount\":0,\"inputKinds\":\"Preview\"}";
        }

        static string ExternalReferenceInputItem(string path, string useCount)
        {
            return "{\"referenceKind\":\"Preview\",\"path\":" + path + ",\"readiness\":\"Missing\",\"width\":\"\",\"height\":\"\",\"copyFileName\":\"preview.png\",\"useCount\":" + useCount + ",\"itemIndices\":\"1\",\"inputKinds\":\"Preview\"}";
        }

        static string ExternalInputItem(string outputPath, string count)
        {
            return "{\"inputKind\":\"Preview\",\"pendingStatus\":\"PendingPreview\",\"readiness\":\"Missing\",\"outputPath\":" + outputPath + ",\"outputDirectory\":\"Assets/Art/UI/AI/Bad\",\"outputFileName\":\"preview.png\",\"outputWidth\":\"1080\",\"outputHeight\":\"1920\",\"actualWidth\":\"\",\"actualHeight\":\"\",\"sizeStatus\":\"Pending\",\"outputDirectoryReadiness\":\"Missing\",\"referencePath\":\"Logs/old.png\",\"targetAtlasPath\":\"\",\"referenceReadiness\":\"Missing\",\"referenceWidth\":\"\",\"referenceHeight\":\"\",\"referenceCopyFileName\":\"preview.png\",\"itemIndices\":\"1\",\"count\":" + count + ",\"sourceAction\":\"Preview\",\"note\":\"\",\"taskPrompt\":\"Generate preview\",\"acceptanceCheck\":\"PNG\"}";
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
            return "{\"root\":{\"name\":\"Bad\",\"uiType\":\"Dialog\",\"targetFolder\":\"Assets/Art/UI/AI/Bad\",\"referenceResolution\":\"1080x1920\",\"safeAreaPolicy\":\"\"},\"nodes\":" + nodes + ",\"assets\":" + assets + ",\"interactions\":" + interactions + ",\"risks\":" + risks + ",\"requiresConfirmation\":true}";
        }

        static string RedesignDraftJson(string items)
        {
            return RedesignDraftJson(items, "[]");
        }

        static string RedesignDraftJson(string items, string risks)
        {
            return "{\"draftPreviewPath\":\"Assets/Art/UI/AI/Bad/preview.png\",\"generatedImageFolder\":\"Assets/Art/UI/AI/Bad/Images\",\"replacementPlan\":{\"items\":" + items + "},\"risks\":" + risks + "}";
        }

        static string RedesignDraftItemJson(string oldName)
        {
            return "{\"oldAssetPath\":\"Assets/Art/UI/" + oldName + ".png\",\"newAssetPath\":\"Assets/Art/UI/AI/Bad/Images/" + oldName + ".png\",\"targetAtlasPath\":\"Assets/Art/UI/AI/Bad/Bad.spriteatlasv2\"}";
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

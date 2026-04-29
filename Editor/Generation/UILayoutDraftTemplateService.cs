using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Xipin.UIAITools
{
    public static class UILayoutDraftTemplateService
    {
        public static string Generate(UIAIToolsProfile profile, string briefJsonPath)
        {
            return Generate(profile, UICreationBriefTemplateService.LoadBrief(briefJsonPath));
        }

        public static UILayoutDraft LoadDraft(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                throw new Exception("Missing UI layout draft JSON: " + path);
            var json = File.ReadAllText(path);
            UICreationBriefTemplateService.ValidateLayoutDraftJson(json);
            var draft = UICreationBriefTemplateService.FromJson<UILayoutDraft>(json, "layout draft");
            ValidateDraft(draft);
            return draft;
        }

        public static string Generate(UIAIToolsProfile profile, UICreationBrief brief)
        {
            UIComponentCandidateIndexService.Validate(profile);
            UICreationBriefTemplateService.ValidateBrief(brief);
            var draft = CreateDraft(brief);
            ValidateDraft(draft);
            var path = UIReportFiles.GetPath(profile.logRoot, $"UILayoutDraftTemplate_{SafeName(brief.featureName)}.json");
            Directory.CreateDirectory(profile.logRoot);
            File.WriteAllText(path, UICreationBriefTemplateService.ToJsonWithRootArrays(draft, "nodes", "assets", "interactions", "risks"));
            LoadDraft(path);
            Debug.Log($"UI layout draft template generated: {path}");
            return path;
        }

        public static void ValidateDraft(UILayoutDraft draft)
        {
            if (draft == null)
                throw new Exception("Invalid UI layout draft: missing draft root");
            if (draft.root == null)
                throw new Exception("Invalid UI layout draft: root is required");
            UICreationBriefTemplateService.RequireValue(draft.root.name, "root.name");
            UICreationBriefTemplateService.RequireValue(draft.root.uiType, "root.uiType");
            if (draft.nodes == null || draft.assets == null || draft.interactions == null || draft.risks == null)
                throw new Exception("Invalid UI layout draft: list fields are required");
        }

        public static void ValidateContract()
        {
            var root = Path.Combine(Path.GetTempPath(), "UIAIToolsLayoutDraftContract_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                WriteContractDraft(root, "valid", JsonWithNode(NodeJson("\"100x80\""), "\"Open\""));
                LoadDraft(ContractPath(root, "valid"));

                ExpectRawDraftFailure(root, "missing_node_size", JsonWithNode(NodeJson(null), "\"Open\""), "nodes.size is required");
                ExpectRawDraftFailure(root, "interaction_item_type", JsonWithNode(NodeJson("\"100x80\""), "1"), "interactions item must be a string");
            }
            finally
            {
                if (Directory.Exists(root))
                    Directory.Delete(root, true);
            }
            Debug.Log("UI layout draft contract validation passed.");
        }

        static UILayoutDraft CreateDraft(UICreationBrief brief)
        {
            return new UILayoutDraft
            {
                root = new UILayoutRoot
                {
                    name = brief.featureName,
                    uiType = brief.uiType,
                    targetFolder = brief.targetFolder,
                    referenceResolution = "1080x1920",
                    safeAreaPolicy = brief.constraints.FirstOrDefault(c => c.IndexOf("safe", StringComparison.OrdinalIgnoreCase) >= 0) ?? ""
                },
                assets = AssetNeeds(brief),
                interactions = new List<string>(brief.requiredInteractions),
                risks = new List<string>
                {
                    "布局节点需由 AI 或人工补齐",
                    "资源需求需补齐为 Ready 后才能进入 prefab 生成前 dry-run"
                },
                requiresConfirmation = true
            };
        }

        static List<UICreationAssetNeed> AssetNeeds(UICreationBrief brief)
        {
            var needs = new List<UICreationAssetNeed>();
            for (int i = 0; i < brief.dataBindings.Count; i++)
            {
                needs.Add(new UICreationAssetNeed
                {
                    needId = "Need" + (i + 1).ToString("0000"),
                    kind = "DataBinding",
                    path = "",
                    source = "Brief",
                    status = "NeedsReview",
                    reason = brief.dataBindings[i]
                });
            }
            return needs;
        }

        static string SafeName(string value)
        {
            var name = value;
            foreach (var c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name;
        }

        static void WriteContractDraft(string root, string name, string json)
        {
            File.WriteAllText(ContractPath(root, name), json);
        }

        static string ContractPath(string root, string name)
        {
            return Path.Combine(root, name + ".json");
        }

        static void ExpectRawDraftFailure(string root, string name, string json, string expectedMessage)
        {
            WriteContractDraft(root, name, json);
            try
            {
                LoadDraft(ContractPath(root, name));
            }
            catch (Exception exception)
            {
                if (exception.Message.Contains(expectedMessage))
                    return;
                throw new Exception($"Unexpected UI layout draft contract failure for {name}: {exception.Message}");
            }
            throw new Exception("UI layout draft contract sample did not fail: " + name);
        }

        static string JsonWithNode(string nodeJson, string interactionItem)
        {
            return string.Join("\n", new[]
            {
                "{",
                "    \"root\": {",
                "        \"name\": \"DemoPanel\",",
                "        \"uiType\": \"Panel\",",
                "        \"targetFolder\": \"Assets/Art/UI/AI/DemoPanel\",",
                "        \"referenceResolution\": \"1080x1920\",",
                "        \"safeAreaPolicy\": \"\"",
                "    },",
                "    \"nodes\": [" + nodeJson + "],",
                "    \"assets\": [],",
                "    \"interactions\": [" + interactionItem + "],",
                "    \"risks\": [],",
                "    \"requiresConfirmation\": true",
                "}"
            });
        }

        static string NodeJson(string sizeValue)
        {
            var lines = new List<string>
            {
                "{",
                "        \"nodeId\": \"Node0001\",",
                "        \"name\": \"Root\",",
                "        \"componentRole\": \"Panel\",",
                "        \"componentId\": \"Builtin.Panel\",",
                "        \"anchor\": \"stretch_full\",",
                "        \"position\": \"0,0\""
            };
            if (sizeValue != null)
                lines.Add("        ,\"size\": " + sizeValue);
            lines.Add("    }");
            return string.Join("\n", lines);
        }
    }
}

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
    }
}

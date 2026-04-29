using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Xipin.UIAITools
{
    public static class UIRedesignDraftTemplateService
    {
        public static string Generate(UIAIToolsProfile profile, UIRedesignRequest request)
        {
            UIReportValidationService.Validate(profile);
            var prefab = request.sourcePrefabPath;
            UIRedesignRequestValidation.ValidateSourcePrefab(profile, prefab, "draft template");
            UIRedesignRequestValidation.ValidateOutputFolder(request.outputFolder);

            var details = UIReportCsv.ReadRows(profile.logRoot, UIReportFiles.PrefabImageDetails).Where(r => r["Prefab"] == prefab).ToList();
            var loose = UIReportCsv.ReadRows(profile.logRoot, UIReportFiles.LooseTextureCandidates).Where(r => r["Prefabs"].Contains(prefab)).ToList();
            var outputFolder = OutputFolder(request, prefab);
            var draft = new UIRedesignDraft
            {
                draftPreviewPath = $"{outputFolder}/preview.png",
                generatedImageFolder = $"{outputFolder}/Images",
                requiresConfirmation = true
            };

            foreach (var row in details.Where(IsReplacementCandidate).Take(30))
                draft.replacementPlan.items.Add(ReplacementItem(profile, prefab, outputFolder, row));
            foreach (var row in details.Where(IsRiskOnly).Take(30))
                draft.risks.Add($"跨功能大图或散图：{row["Image"]}，{row["Kind"]}，{row["SizeClass"]}");
            foreach (var row in details.Where(r => r["Match"] == "DynamicRisk").Take(20))
                draft.risks.Add($"按名加载风险：{row["Image"]}，TextRefs {row["TextRefs"]}");
            foreach (var row in loose.Take(20))
                draft.risks.Add($"直挂 UITexture：{row["Path"]}，{row["Advice"]}，{row["Reason"]}");

            UIRedesignDraftService.ValidateDraft(draft);
            var path = UIReportFiles.GetPath(profile.logRoot, $"UIRedesignDraftTemplate_{SafeName(prefab)}.json");
            Directory.CreateDirectory(profile.logRoot);
            File.WriteAllText(path, UIRedesignDraftService.ToJsonWithRequiredArrays(draft));
            UIRedesignDraftService.LoadDraft(path);
            Debug.Log($"UI redesign draft template generated: {path}, {draft.replacementPlan.items.Count} candidate items.");
            if (!Application.isBatchMode)
                EditorUtility.RevealInFinder(Path.GetFullPath(path));
            return path;
        }

        static UIReplacementItem ReplacementItem(UIAIToolsProfile profile, string prefab, string outputFolder, Dictionary<string, string> row)
        {
            var oldPath = row["Image"];
            return new UIReplacementItem
            {
                oldAssetPath = oldPath,
                newAssetPath = $"{outputFolder}/Images/{Path.GetFileName(oldPath)}",
                targetAtlasPath = TargetAtlas(profile, prefab),
                preserveGuid = false,
                requiresConfirmation = true,
                reason = $"模板候选：{row["Match"]}，Owner {row["ImageOwner"]}"
            };
        }

        static bool IsReplacementCandidate(Dictionary<string, string> row)
        {
            return row["Match"] == "CrossFeature" && row["Kind"] == "AtlasSprite" && row["SizeClass"] != "Large";
        }

        static bool IsRiskOnly(Dictionary<string, string> row)
        {
            return row["Match"] == "CrossFeature" && (row["Kind"] != "AtlasSprite" || row["SizeClass"] == "Large");
        }

        static string TargetAtlas(UIAIToolsProfile profile, string prefab)
        {
            var owner = Owner(profile, prefab);
            var dir = $"{Root(profile.uiAtlasRoot)}/{owner}";
            if (Directory.Exists(dir))
            {
                var atlas = Directory.GetFiles(dir, "*.spriteatlasv2", SearchOption.AllDirectories).OrderBy(p => p).FirstOrDefault();
                if (!string.IsNullOrEmpty(atlas))
                    return Root(atlas);
            }
            return $"{dir}/atlas_{owner.ToLowerInvariant()}.spriteatlasv2";
        }

        static string OutputFolder(UIRedesignRequest request, string prefab)
        {
            return string.IsNullOrEmpty(request.outputFolder) ? "Assets/Art/UI/AI/" + SafeName(prefab) : Root(request.outputFolder);
        }

        static string Owner(UIAIToolsProfile profile, string prefab)
        {
            var path = Root(prefab);
            var root = Root(profile.prefabRoot);
            var value = path.StartsWith(root + "/", StringComparison.OrdinalIgnoreCase) ? path.Substring(root.Length + 1) : Path.GetFileNameWithoutExtension(prefab);
            var parts = value.Split('/');
            return parts.Length > 1 && parts[0] == "Activity" ? parts[1] : Path.GetFileNameWithoutExtension(parts[parts.Length - 1]);
        }

        static string SafeName(string path)
        {
            var name = path.Replace("Assets/Bundle/Prefab/", "").Replace(".prefab", "").Replace('/', '_');
            foreach (var c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name;
        }

        static string Root(string path)
        {
            return (path ?? "").Replace('\\', '/').TrimEnd('/');
        }
    }
}

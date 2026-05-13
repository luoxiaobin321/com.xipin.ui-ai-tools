using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Xipin.UIAITools
{
    public static class UISkinAIMappingService
    {
        const int MaxCandidateImages = 40;

        public static string LastMessage { get; private set; }

        public static string Run(string manifestPath)
        {
            var manifest = UISkinContractService.LoadManifest(NormalizeManifestPath(manifestPath));
            var skinFolder = NormalizeFolder(manifest.skinFolder);
            var generatedFolder = skinFolder + "/Generated";
            var reportFolder = UISkinContractService.ReportFolder(skinFolder);
            Directory.CreateDirectory(generatedFolder);
            Directory.CreateDirectory(reportFolder);

            var slots = LoadSlots(manifest);
            var candidates = LoadCandidateImages(skinFolder);
            var images = LoadRequestImages(manifest.conceptPath, candidates);
            var prompt = BuildPrompt(manifest, slots, candidates);
            File.WriteAllText(generatedFolder + "/ai-mapping-request.md", prompt, new UTF8Encoding(true));

            var settings = UIAIToolsAISettingsService.Load();
            var response = UIAIToolsAIClient.RequestText(prompt, images);
            var outputJson = NormalizeOutputJson(response.Text);
            if (!outputJson.Contains("\"slots\""))
                throw new Exception("UIAITools AI mapping response must contain a slots array.");

            var outputPath = generatedFolder + "/ai-mapping.json";
            var rawPath = generatedFolder + "/ai-mapping-response.json";
            var reportPath = reportFolder + "/ai-mapping.md";
            File.WriteAllText(outputPath, outputJson, new UTF8Encoding(true));
            File.WriteAllText(rawPath, response.RawResponse, new UTF8Encoding(true));
            WriteReport(reportPath, manifestPath, settings, outputPath, rawPath, slots, candidates);
            AssetDatabase.Refresh();
            LastMessage = "AI 映射建议已生成：" + outputPath;
            return outputPath;
        }

        static string BuildPrompt(UISkinManifest manifest, List<SkinAISlot> slots, List<SkinAIImageInfo> candidates)
        {
            var lines = new List<string>
            {
                "你是 Unity UI 换皮映射助手。请根据效果图和候选切图，为现有 skin.json 生成结构化槽位映射建议。",
                "",
                "规则：",
                "- 只输出 JSON，不要输出 Markdown 或解释性段落。",
                "- 不要建议从效果图自动裁切缺失素材；缺图时标记 action=Missing。",
                "- sourcePath 必须来自 Candidate Images 中列出的路径，不能编造资源路径。",
                "- AI 结果只是候选建议，不直接代表最终 prefab 写入。",
                "- 对重复、对称、同组 UI，优先保持同组中心线、间距、卡片内相对位置和语义一致性。",
                "",
                "JSON schema：",
                "{",
                "  \"summary\": \"string\",",
                "  \"slots\": [",
                "    {\"slotId\":\"string\",\"sourcePath\":\"string\",\"action\":\"UseCrop|Missing|Review\",\"confidence\":0.0,\"reason\":\"string\"}",
                "  ],",
                "  \"layoutNotes\": [\"string\"],",
                "  \"missingAssets\": [\"string\"]",
                "}",
                "",
                "Manifest:",
                "- sourcePrefabPath: " + manifest.sourcePrefabPath,
                "- skinName: " + manifest.skinName,
                "- conceptPath: " + manifest.conceptPath,
                "",
                "Expected Slots:"
            };
            foreach (var slot in slots)
                lines.Add("- " + slot.Id + " | " + slot.Usage + " | current: " + slot.CurrentOutputPath);
            lines.Add("");
            lines.Add("Candidate Images:");
            foreach (var image in candidates)
                lines.Add("- " + image.Path + " | " + image.Width + "x" + image.Height);
            return string.Join("\n", lines);
        }

        static List<SkinAISlot> LoadSlots(UISkinManifest manifest)
        {
            var result = new List<SkinAISlot>();
            if (!string.IsNullOrEmpty(manifest.generated.assetCropsPath) && File.Exists(manifest.generated.assetCropsPath))
            {
                var crops = JsonUtility.FromJson<UISkinAssetCrops>(File.ReadAllText(manifest.generated.assetCropsPath));
                foreach (var crop in crops.crops)
                    result.Add(new SkinAISlot(crop.id, crop.usage, crop.outputPath));
            }
            if (result.Count == 0 && !string.IsNullOrEmpty(manifest.generated.skinLayoutPath) && File.Exists(manifest.generated.skinLayoutPath))
            {
                var layout = JsonUtility.FromJson<UISkinLayout>(File.ReadAllText(manifest.generated.skinLayoutPath));
                foreach (var node in layout.createNodes)
                    if (!string.IsNullOrEmpty(node.name))
                        result.Add(new SkinAISlot(node.name, node.type, node.spritePath));
            }
            return result.GroupBy(slot => slot.Id).Select(group => group.First()).OrderBy(slot => slot.Id).ToList();
        }

        static List<SkinAIImageInfo> LoadCandidateImages(string skinFolder)
        {
            var texturesFolder = skinFolder.TrimEnd('/', '\\') + "/Textures";
            if (!Directory.Exists(texturesFolder))
                return new List<SkinAIImageInfo>();
            return Directory.GetFiles(texturesFolder, "*.*", SearchOption.TopDirectoryOnly)
                .Where(IsImagePath)
                .OrderBy(path => path)
                .Take(MaxCandidateImages)
                .Select(ImageInfo)
                .ToList();
        }

        static List<UIAIToolsAIImage> LoadRequestImages(string conceptPath, List<SkinAIImageInfo> candidates)
        {
            var images = new List<UIAIToolsAIImage>();
            if (!string.IsNullOrEmpty(conceptPath) && File.Exists(conceptPath))
                images.Add(LoadImage(conceptPath));
            images.AddRange(candidates.Select(candidate => LoadImage(candidate.Path)));
            return images;
        }

        static UIAIToolsAIImage LoadImage(string path)
        {
            var bytes = File.ReadAllBytes(path);
            var mime = path.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ? "image/jpeg" : "image/png";
            return new UIAIToolsAIImage(path.Replace('\\', '/'), "data:" + mime + ";base64," + Convert.ToBase64String(bytes));
        }

        static SkinAIImageInfo ImageInfo(string path)
        {
            var tex = new Texture2D(2, 2);
            tex.LoadImage(File.ReadAllBytes(path));
            var result = new SkinAIImageInfo(path.Replace('\\', '/'), tex.width, tex.height);
            UnityEngine.Object.DestroyImmediate(tex);
            return result;
        }

        static void WriteReport(string reportPath, string manifestPath, UIAIToolsAISettings settings, string outputPath, string rawPath, List<SkinAISlot> slots, List<SkinAIImageInfo> candidates)
        {
            var lines = new List<string>
            {
                "# AI 换皮映射建议",
                "",
                "## Summary",
                "- Manifest: `" + manifestPath + "`",
                "- Output JSON: `" + outputPath + "`",
                "- Raw Response: `" + rawPath + "`",
                "- Model: `" + settings.Model + "`",
                "- Slot Count: " + slots.Count,
                "- Candidate Image Count: " + candidates.Count,
                "",
                "## Next Step",
                "- 工具保留 `ai-mapping.json` 的 `action`、`confidence` 和 `reason` 作为候选输入。",
                "- 宿主 adapter 后续接入现有切图登记、prefab 生成、绑定验证和真实运行时预览。",
                "",
                "## Inputs",
                "| Type | Path |",
                "| --- | --- |"
            };
            lines.Add("| Mapping JSON | `" + outputPath + "` |");
            lines.Add("| Raw Response | `" + rawPath + "` |");
            File.WriteAllLines(reportPath, lines, new UTF8Encoding(true));
        }

        static string NormalizeOutputJson(string text)
        {
            var value = text.Trim();
            if (value.StartsWith("```", StringComparison.Ordinal))
            {
                var firstLineEnd = value.IndexOf('\n');
                var lastFence = value.LastIndexOf("```", StringComparison.Ordinal);
                if (firstLineEnd >= 0 && lastFence > firstLineEnd)
                    value = value.Substring(firstLineEnd + 1, lastFence - firstLineEnd - 1).Trim();
            }
            if (!value.StartsWith("{", StringComparison.Ordinal) || !value.EndsWith("}", StringComparison.Ordinal))
                throw new Exception("UIAITools AI mapping response must be a JSON object.");
            return value;
        }

        static string NormalizeManifestPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new Exception("Select a skin.json before running AI mapping.");
            var normalized = path.Replace('\\', '/').Trim();
            if (!normalized.EndsWith("/skin.json", StringComparison.OrdinalIgnoreCase))
                throw new Exception("AI mapping requires a skin.json path.");
            return normalized;
        }

        static string NormalizeFolder(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new Exception("Skin manifest skinFolder is empty.");
            return path.Replace('\\', '/').TrimEnd('/');
        }

        static bool IsImagePath(string path)
        {
            return path.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
                || path.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
                || path.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase);
        }

        sealed class SkinAISlot
        {
            public readonly string Id;
            public readonly string Usage;
            public readonly string CurrentOutputPath;

            public SkinAISlot(string id, string usage, string currentOutputPath)
            {
                Id = id ?? "";
                Usage = usage ?? "";
                CurrentOutputPath = currentOutputPath ?? "";
            }
        }

        sealed class SkinAIImageInfo
        {
            public readonly string Path;
            public readonly int Width;
            public readonly int Height;

            public SkinAIImageInfo(string path, int width, int height)
            {
                Path = path;
                Width = width;
                Height = height;
            }
        }
    }
}

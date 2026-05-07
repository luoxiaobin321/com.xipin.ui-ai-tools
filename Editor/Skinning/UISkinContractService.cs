using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Xipin.UIAITools
{
    public static class UISkinContractService
    {
        public const string StatusPendingConcept = "PendingConcept";
        public const string StatusManifestReady = "ManifestReady";
        public const string StatusPromptReady = "PromptReady";
        public const string StatusLayoutReady = "LayoutReady";
        public const string StatusPrefabGenerated = "PrefabGenerated";
        public const string StatusValidated = "Validated";
        static readonly string[] ManifestSummarySections =
        {
            "## Generated Files"
        };
        static readonly string[] DetectedLayoutSummarySections =
        {
            "## Regions"
        };
        static readonly string[] AssetCropsSummarySections =
        {
            "## Crops"
        };
        static readonly string[] SkinLayoutSummarySections =
        {
            "## Counts",
            "## Layers",
            "## Validation Slots"
        };

        public static UISkinManifest CreateManifest(string sourcePrefabPath, string skinName, string skinFolder, string conceptPath)
        {
            var reportFolder = ReportFolder(skinFolder);
            var generated = new UISkinGeneratedPaths
            {
                finalPromptPath = skinFolder + "/Generated/final-prompt.md",
                detectedLayoutPath = skinFolder + "/Generated/detected-layout.json",
                assetCropsPath = skinFolder + "/Generated/asset-crops.json",
                skinLayoutPath = skinFolder + "/Generated/skin-layout.json",
                outputPrefabPath = OutputPrefabPath(skinFolder, sourcePrefabPath),
                finalPreviewPath = skinFolder + "/Preview/final.png",
                comparisonPreviewPath = skinFolder + "/Preview/comparison.png",
                bindingReportPath = reportFolder + "/binding-check.md",
                visualReportPath = reportFolder + "/visual-check.md",
                applyChecklistPath = reportFolder + "/apply-checklist.md",
                autoBuildNotesPath = reportFolder + "/auto-build-notes.md"
            };

            return new UISkinManifest
            {
                sourcePrefabPath = sourcePrefabPath,
                skinName = skinName,
                skinFolder = skinFolder,
                conceptPath = conceptPath,
                userPromptPath = skinFolder + "/user-prompt.md",
                status = string.IsNullOrEmpty(conceptPath) ? StatusPendingConcept : StatusManifestReady,
                generated = generated
            };
        }

        public static void SaveManifest(UISkinManifest manifest)
        {
            ValidateManifest(manifest);
            Directory.CreateDirectory(manifest.skinFolder);
            WriteJson(ManifestPath(manifest), manifest);
        }

        public static UISkinManifest LoadManifest(string path)
        {
            var manifest = LoadJson<UISkinManifest>(path);
            ValidateManifest(manifest);
            return manifest;
        }

        public static void SaveDetectedLayout(UISkinManifest manifest, UISkinDetectedLayout layout)
        {
            ValidateManifest(manifest);
            ValidateDetectedLayout(layout);
            WriteJson(manifest.generated.detectedLayoutPath, layout);
        }

        public static UISkinDetectedLayout LoadDetectedLayout(UISkinManifest manifest)
        {
            var layout = LoadJson<UISkinDetectedLayout>(manifest.generated.detectedLayoutPath);
            ValidateDetectedLayout(layout);
            return layout;
        }

        public static void SaveAssetCrops(UISkinManifest manifest, UISkinAssetCrops crops)
        {
            ValidateManifest(manifest);
            var layout = File.Exists(manifest.generated.detectedLayoutPath) ? LoadDetectedLayout(manifest) : null;
            ValidateAssetCrops(crops, layout);
            WriteJson(manifest.generated.assetCropsPath, crops);
        }

        public static UISkinAssetCrops LoadAssetCrops(UISkinManifest manifest)
        {
            var crops = LoadJson<UISkinAssetCrops>(manifest.generated.assetCropsPath);
            var layout = File.Exists(manifest.generated.detectedLayoutPath) ? LoadDetectedLayout(manifest) : null;
            ValidateAssetCrops(crops, layout);
            return crops;
        }

        public static void SaveSkinLayout(UISkinManifest manifest, UISkinLayout layout)
        {
            ValidateManifest(manifest);
            var detectedLayout = File.Exists(manifest.generated.detectedLayoutPath) ? LoadDetectedLayout(manifest) : null;
            var crops = File.Exists(manifest.generated.assetCropsPath) ? LoadAssetCrops(manifest) : null;
            ValidateSkinLayout(layout, detectedLayout, crops);
            WriteJson(manifest.generated.skinLayoutPath, layout);
        }

        public static UISkinLayout LoadSkinLayout(UISkinManifest manifest)
        {
            var layout = LoadJson<UISkinLayout>(manifest.generated.skinLayoutPath);
            var detectedLayout = File.Exists(manifest.generated.detectedLayoutPath) ? LoadDetectedLayout(manifest) : null;
            var crops = File.Exists(manifest.generated.assetCropsPath) ? LoadAssetCrops(manifest) : null;
            ValidateSkinLayout(layout, detectedLayout, crops);
            return layout;
        }

        public static void WriteManifestSummary(UISkinManifest manifest, string path)
        {
            ValidateManifest(manifest);
            var lines = new List<string>
            {
                "# UI Skin Manifest",
                "",
                "- Manifest: `" + ManifestPath(manifest) + "`",
                "- Source Prefab: `" + manifest.sourcePrefabPath + "`",
                "- Skin: `" + manifest.skinName + "`",
                "- Status: `" + manifest.status + "`",
                "- Skin Folder: `" + manifest.skinFolder + "`",
                "- Concept: `" + manifest.conceptPath + "`",
                "",
                "## Generated Files",
                "- Prompt: `" + manifest.generated.finalPromptPath + "`",
                "- Detected Layout: `" + manifest.generated.detectedLayoutPath + "`",
                "- Asset Crops: `" + manifest.generated.assetCropsPath + "`",
                "- Skin Layout: `" + manifest.generated.skinLayoutPath + "`",
                "- Prefab: `" + manifest.generated.outputPrefabPath + "`",
                "- Preview: `" + manifest.generated.finalPreviewPath + "`"
            };
            ValidateManifestSummarySections(lines.ToArray());
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllLines(path, lines, new UTF8Encoding(true));
        }

        public static void WriteDetectedLayoutSummary(UISkinManifest manifest, UISkinDetectedLayout layout, string path)
        {
            ValidateManifest(manifest);
            ValidateDetectedLayout(layout);
            var lines = new List<string>
            {
                "# UI Skin Detected Layout",
                "",
                "- Manifest: `" + ManifestPath(manifest) + "`",
                "- Detected Layout JSON: `" + manifest.generated.detectedLayoutPath + "`",
                "- Source Image: `" + layout.sourceImagePath + "`",
                "- Size: `" + layout.imageWidth + "x" + layout.imageHeight + "`",
                "- Region Count: `" + layout.regions.Count + "`",
                "",
                "## Regions",
                "| Id | Type | Rect | Confidence | Note |",
                "| --- | --- | --- | --- | --- |"
            };
            foreach (var region in layout.regions)
                lines.Add("| " + region.id + " | " + region.type + " | " + RectText(region.rect) + " | " + region.confidence.ToString("0.##") + " | " + region.note + " |");
            ValidateDetectedLayoutSummarySections(lines.ToArray());
            WriteLines(path, lines);
        }

        public static void WriteAssetCropsSummary(UISkinManifest manifest, UISkinAssetCrops crops, UISkinDetectedLayout layout, string path)
        {
            ValidateManifest(manifest);
            ValidateAssetCrops(crops, layout);
            var lines = new List<string>
            {
                "# UI Skin Asset Crops",
                "",
                "- Manifest: `" + ManifestPath(manifest) + "`",
                "- Asset Crops JSON: `" + manifest.generated.assetCropsPath + "`",
                "- Source Image: `" + crops.sourceImagePath + "`",
                "- Crop Count: `" + crops.crops.Count + "`",
                "",
                "## Crops",
                "| Id | Source Region | Rect | Size | Output | Usage | Note |",
                "| --- | --- | --- | --- | --- | --- | --- |"
            };
            foreach (var crop in crops.crops)
                lines.Add("| " + crop.id + " | " + crop.sourceRegionId + " | " + RectText(crop.rect) + " | " + SizeText(crop.rect) + " | `" + crop.outputPath + "` | " + crop.usage + " | " + crop.note + " |");
            ValidateAssetCropsSummarySections(lines.ToArray());
            WriteLines(path, lines);
        }

        public static void WriteSkinLayoutSummary(UISkinManifest manifest, UISkinLayout layout, UISkinDetectedLayout detectedLayout, UISkinAssetCrops crops, string path)
        {
            ValidateManifest(manifest);
            ValidateSkinLayout(layout, detectedLayout, crops);
            var lines = new List<string>
            {
                "# UI Skin Layout",
                "",
                "- Manifest: `" + ManifestPath(manifest) + "`",
                "- Skin Layout JSON: `" + manifest.generated.skinLayoutPath + "`",
                "- Asset Crops JSON: `" + manifest.generated.assetCropsPath + "`",
                "- Source Prefab: `" + layout.sourcePrefabPath + "`",
                "- Output Prefab: `" + layout.outputPrefabPath + "`",
                "- Root: `" + layout.generatedVisualRoot + "`",
                "- Reference Resolution: `" + layout.referenceResolution + "`",
                "",
                "## Counts",
                "- Layers: `" + layout.layers.Count + "`",
                "- Hide Nodes: `" + layout.hideNodes.Count + "`",
                "- Move Nodes: `" + layout.moveNodes.Count + "`",
                "- Create Nodes: `" + layout.createNodes.Count + "`",
                "- Text Styles: `" + layout.styleTextNodes.Count + "`",
                "- Preserve Nodes: `" + layout.preserveNodes.Count + "`",
                "- Validation Slots: `" + layout.validationSlots.Count + "`",
                "",
                "## Layers"
            };
            foreach (var layer in layout.layers)
                lines.Add("- `" + layer.name + "`：" + layer.purpose);
            lines.Add("");
            lines.Add("## Validation Slots");
            foreach (var slot in layout.validationSlots)
                lines.Add("- `" + slot.id + "`：" + slot.check + " / " + slot.target + " / " + slot.expected);
            ValidateSkinLayoutSummarySections(lines.ToArray());
            WriteLines(path, lines);
        }

        public static string ManifestPath(UISkinManifest manifest)
        {
            return manifest.skinFolder.TrimEnd('/', '\\') + "/skin.json";
        }

        public static string ReportFolder(UISkinManifest manifest)
        {
            return ReportFolder(manifest.skinFolder);
        }

        public static string ReportFolder(string skinFolder)
        {
            return "UIAIToolsReports/Skinning/" + Path.GetFileName(skinFolder.TrimEnd('/', '\\').Replace('\\', '/'));
        }

        public static void ValidateContract()
        {
            var root = "Assets/UIAITools/Skinning/ContractDemo";
            var manifest = CreateManifest("Assets/Bundle/Prefab/Demo/Demo.prefab", "DemoSkin", root, root + "/Source/concept.png");
            ValidateManifest(manifest);
            ValidateManifestSummarySections(ManifestSummarySections);
            ValidateDetectedLayoutSummarySections(DetectedLayoutSummarySections);
            ValidateAssetCropsSummarySections(AssetCropsSummarySections);
            ValidateSkinLayoutSummarySections(SkinLayoutSummarySections);
            ExpectContractFailure("manifest_summary_missing_section", () =>
                ValidateManifestSummarySections(Array.Empty<string>()), "UI skin manifest summary is missing section: ## Generated Files");
            ExpectContractFailure("detected_layout_summary_missing_section", () =>
                ValidateDetectedLayoutSummarySections(Array.Empty<string>()), "UI skin detected layout summary is missing section: ## Regions");
            ExpectContractFailure("asset_crops_summary_missing_section", () =>
                ValidateAssetCropsSummarySections(Array.Empty<string>()), "UI skin asset crops summary is missing section: ## Crops");
            ExpectContractFailure("skin_layout_summary_missing_section", () =>
                ValidateSkinLayoutSummarySections(SkinLayoutSummarySections.Where(section => section != "## Layers").ToArray()), "UI skin layout summary is missing section: ## Layers");
            ExpectContractFailure("skin_layout_summary_out_of_order", () =>
                ValidateSkinLayoutSummarySections(new[] { SkinLayoutSummarySections[1], SkinLayoutSummarySections[0] }.Concat(SkinLayoutSummarySections.Skip(2)).ToArray()), "UI skin layout summary section is out of order");
            var badOutputManifest = CreateManifest("Assets/Bundle/Prefab/Demo/Demo.prefab", "DemoSkin", root, root + "/Source/concept.png");
            badOutputManifest.generated.outputPrefabPath = root + "/Prefabs/Demo.prefab";
            ExpectContractFailure("manifest_output_prefab_name", () => ValidateManifest(badOutputManifest), "UISkin outputPrefabPath must be " + OutputPrefabPath(root, badOutputManifest.sourcePrefabPath));
            var manifestSummaryPath = Path.Combine(Path.GetTempPath(), "UISkinManifestSummaryContract_" + Guid.NewGuid().ToString("N") + ".md");
            try
            {
                WriteManifestSummary(manifest, manifestSummaryPath);
                ValidateManifestSummarySections(File.ReadAllLines(manifestSummaryPath));
                ExpectReportContains("manifest_summary_manifest_path", manifestSummaryPath, "- Manifest: `" + ManifestPath(manifest) + "`");
                ExpectReportContains("manifest_summary_generated_files", manifestSummaryPath, "## Generated Files");
            }
            finally
            {
                if (File.Exists(manifestSummaryPath))
                    File.Delete(manifestSummaryPath);
            }
            ValidateDetectedLayout(new UISkinDetectedLayout
            {
                sourceImagePath = manifest.conceptPath,
                imageWidth = 1080,
                imageHeight = 1920,
                regions = new List<UISkinRegion>
                {
                    new UISkinRegion { id = "card", type = "Card", rect = Rect(0, 0, 100, 100), confidence = 0.8f }
                }
            });
            var detected = new UISkinDetectedLayout
            {
                sourceImagePath = manifest.conceptPath,
                imageWidth = 1080,
                imageHeight = 1920,
                regions = new List<UISkinRegion>
                {
                    new UISkinRegion { id = "card", type = "Card", rect = Rect(0, 0, 100, 100), confidence = 0.8f }
                }
            };
            ValidateDetectedLayout(detected);
            var detectedLayoutSummaryPath = Path.Combine(Path.GetTempPath(), "UISkinDetectedLayoutSummaryContract_" + Guid.NewGuid().ToString("N") + ".md");
            try
            {
                WriteDetectedLayoutSummary(manifest, detected, detectedLayoutSummaryPath);
                ValidateDetectedLayoutSummarySections(File.ReadAllLines(detectedLayoutSummaryPath));
                ExpectReportContains("detected_layout_summary_manifest", detectedLayoutSummaryPath, "- Manifest: `" + ManifestPath(manifest) + "`");
                ExpectReportContains("detected_layout_summary_json", detectedLayoutSummaryPath, "- Detected Layout JSON: `" + manifest.generated.detectedLayoutPath + "`");
                ExpectReportContains("detected_layout_summary_regions", detectedLayoutSummaryPath, "## Regions");
            }
            finally
            {
                if (File.Exists(detectedLayoutSummaryPath))
                    File.Delete(detectedLayoutSummaryPath);
            }
            var crops = new UISkinAssetCrops
            {
                sourceImagePath = manifest.conceptPath,
                crops = new List<UISkinCrop>
                {
                    new UISkinCrop { id = "card_bg", sourceRegionId = "card", rect = Rect(0, 0, 100, 100), outputPath = root + "/Textures/card_bg.png", usage = "Image" }
                }
            };
            ValidateAssetCrops(crops, detected);
            var assetCropsSummaryPath = Path.Combine(Path.GetTempPath(), "UISkinAssetCropsSummaryContract_" + Guid.NewGuid().ToString("N") + ".md");
            try
            {
                WriteAssetCropsSummary(manifest, crops, detected, assetCropsSummaryPath);
                ValidateAssetCropsSummarySections(File.ReadAllLines(assetCropsSummaryPath));
                ExpectReportContains("asset_crops_summary_manifest", assetCropsSummaryPath, "- Manifest: `" + ManifestPath(manifest) + "`");
                ExpectReportContains("asset_crops_summary_json", assetCropsSummaryPath, "- Asset Crops JSON: `" + manifest.generated.assetCropsPath + "`");
                ExpectReportContains("asset_crops_summary_columns", assetCropsSummaryPath, "| Id | Source Region | Rect | Size | Output | Usage | Note |");
                ExpectReportContains("asset_crops_summary_size", assetCropsSummaryPath, "| card_bg | card | 0,0,100x100 | 100x100 |");
            }
            finally
            {
                if (File.Exists(assetCropsSummaryPath))
                    File.Delete(assetCropsSummaryPath);
            }
            var skinLayout = new UISkinLayout
            {
                sourcePrefabPath = manifest.sourcePrefabPath,
                outputPrefabPath = manifest.generated.outputPrefabPath,
                generatedVisualRoot = "SkinRoot",
                referenceResolution = "1080x1920",
                layers = new List<UISkinLayer> { new UISkinLayer { name = "Background", purpose = "static" } },
                createNodes = new List<UISkinCreateNode> { new UISkinCreateNode { name = "Card", type = "Image", targetLayer = "Background", targetRegionId = "card", spritePath = root + "/Textures/card_bg.png", rect = UnityRect(0, 0, 100, 100) } }
            };
            ValidateSkinLayout(skinLayout, detected, crops);
            var skinLayoutSummaryPath = Path.Combine(Path.GetTempPath(), "UISkinLayoutSummaryContract_" + Guid.NewGuid().ToString("N") + ".md");
            try
            {
                WriteSkinLayoutSummary(manifest, skinLayout, detected, crops, skinLayoutSummaryPath);
                ValidateSkinLayoutSummarySections(File.ReadAllLines(skinLayoutSummaryPath));
                ExpectReportContains("skin_layout_summary_manifest", skinLayoutSummaryPath, "- Manifest: `" + ManifestPath(manifest) + "`");
                ExpectReportContains("skin_layout_summary_json", skinLayoutSummaryPath, "- Skin Layout JSON: `" + manifest.generated.skinLayoutPath + "`");
                ExpectReportContains("skin_layout_summary_asset_crops_json", skinLayoutSummaryPath, "- Asset Crops JSON: `" + manifest.generated.assetCropsPath + "`");
            }
            finally
            {
                if (File.Exists(skinLayoutSummaryPath))
                    File.Delete(skinLayoutSummaryPath);
            }
        }

        static void ValidateManifestSummarySections(string[] lines)
        {
            UIReportMarkdown.RequireExactSectionOrder("UI skin manifest summary", lines, ManifestSummarySections);
        }

        static void ValidateDetectedLayoutSummarySections(string[] lines)
        {
            UIReportMarkdown.RequireExactSectionOrder("UI skin detected layout summary", lines, DetectedLayoutSummarySections);
        }

        static void ValidateAssetCropsSummarySections(string[] lines)
        {
            UIReportMarkdown.RequireExactSectionOrder("UI skin asset crops summary", lines, AssetCropsSummarySections);
        }

        static void ValidateSkinLayoutSummarySections(string[] lines)
        {
            UIReportMarkdown.RequireExactSectionOrder("UI skin layout summary", lines, SkinLayoutSummarySections);
        }

        static void ValidateManifest(UISkinManifest manifest)
        {
            if (manifest == null)
                throw new Exception("UISkin manifest is required");
            RequireAssetPath(manifest.sourcePrefabPath, "sourcePrefabPath");
            if (!manifest.sourcePrefabPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                throw new Exception("UISkin sourcePrefabPath must be .prefab");
            RequireNonEmpty(manifest.skinName, "skinName");
            RequireAssetPath(manifest.skinFolder, "skinFolder");
            if (!string.IsNullOrEmpty(manifest.conceptPath))
                RequireImagePath(manifest.conceptPath, "conceptPath");
            RequireAssetPath(manifest.userPromptPath, "userPromptPath");
            RequireNonEmpty(manifest.status, "status");
            ValidateGeneratedPaths(manifest);
        }

        static void ValidateGeneratedPaths(UISkinManifest manifest)
        {
            var paths = manifest.generated;
            if (paths == null)
                throw new Exception("UISkin generated paths are required");
            RequireAssetPath(paths.finalPromptPath, "finalPromptPath");
            RequireAssetPath(paths.detectedLayoutPath, "detectedLayoutPath");
            RequireAssetPath(paths.assetCropsPath, "assetCropsPath");
            RequireAssetPath(paths.skinLayoutPath, "skinLayoutPath");
            RequireAssetPath(paths.outputPrefabPath, "outputPrefabPath");
            RequireImagePath(paths.finalPreviewPath, "finalPreviewPath");
            RequireImagePath(paths.comparisonPreviewPath, "comparisonPreviewPath");
            RequireReportPath(paths.bindingReportPath, "bindingReportPath");
            RequireReportPath(paths.visualReportPath, "visualReportPath");
            RequireReportPath(paths.applyChecklistPath, "applyChecklistPath");
            RequireReportPath(paths.autoBuildNotesPath, "autoBuildNotesPath");
            if (!paths.outputPrefabPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                throw new Exception("UISkin outputPrefabPath must be .prefab");
            var expectedOutputPrefabPath = OutputPrefabPath(manifest.skinFolder, manifest.sourcePrefabPath);
            if (paths.outputPrefabPath != expectedOutputPrefabPath)
                throw new Exception("UISkin outputPrefabPath must be " + expectedOutputPrefabPath);
            foreach (var path in new[]
            {
                paths.finalPromptPath,
                paths.detectedLayoutPath,
                paths.assetCropsPath,
                paths.skinLayoutPath,
                paths.outputPrefabPath,
                paths.finalPreviewPath,
                paths.comparisonPreviewPath
            })
                if (!path.StartsWith(manifest.skinFolder.TrimEnd('/', '\\') + "/", StringComparison.Ordinal))
                    throw new Exception("UISkin generated path must be under skinFolder: " + path);
            foreach (var path in new[]
            {
                paths.bindingReportPath,
                paths.visualReportPath,
                paths.applyChecklistPath,
                paths.autoBuildNotesPath
            })
                if (!path.StartsWith(ReportFolder(manifest) + "/", StringComparison.Ordinal))
                    throw new Exception("UISkin report path must be under report folder: " + path);
        }

        static void ValidateDetectedLayout(UISkinDetectedLayout layout)
        {
            if (layout == null)
                throw new Exception("UISkin detected layout is required");
            RequireImagePath(layout.sourceImagePath, "sourceImagePath");
            if (layout.imageWidth <= 0 || layout.imageHeight <= 0)
                throw new Exception("UISkin detected layout image size must be positive");
            if (layout.regions == null)
                throw new Exception("UISkin detected layout regions are required");
            RequireUnique(layout.regions.Select(r => r.id), "region id");
            foreach (var region in layout.regions)
            {
                RequireNonEmpty(region.id, "region.id");
                RequireNonEmpty(region.type, "region.type");
                ValidateRect(region.rect, layout.imageWidth, layout.imageHeight, region.id);
                if (region.confidence < 0f || region.confidence > 1f)
                    throw new Exception("UISkin region confidence must be 0..1: " + region.id);
            }
        }

        static void ValidateAssetCrops(UISkinAssetCrops crops, UISkinDetectedLayout layout)
        {
            if (crops == null)
                throw new Exception("UISkin asset crops are required");
            RequireImagePath(crops.sourceImagePath, "sourceImagePath");
            if (crops.crops == null)
                throw new Exception("UISkin crops list is required");
            RequireUnique(crops.crops.Select(c => c.id), "crop id");
            var regionIds = layout != null ? new HashSet<string>(layout.regions.Select(r => r.id)) : null;
            foreach (var crop in crops.crops)
            {
                RequireNonEmpty(crop.id, "crop.id");
                RequireNonEmpty(crop.sourceRegionId, "crop.sourceRegionId");
                if (regionIds != null && !regionIds.Contains(crop.sourceRegionId))
                    throw new Exception("UISkin crop references missing region: " + crop.id + " -> " + crop.sourceRegionId);
                RequireImagePath(crop.outputPath, "crop.outputPath");
                RequireNonEmpty(crop.usage, "crop.usage");
                ValidateRect(crop.rect, int.MaxValue, int.MaxValue, crop.id);
            }
        }

        static void ValidateSkinLayout(UISkinLayout layout, UISkinDetectedLayout detectedLayout, UISkinAssetCrops crops)
        {
            if (layout == null)
                throw new Exception("UISkin layout is required");
            RequireAssetPath(layout.sourcePrefabPath, "sourcePrefabPath");
            RequireAssetPath(layout.outputPrefabPath, "outputPrefabPath");
            if (Path.GetFileName(layout.outputPrefabPath) != OutputPrefabFileName(layout.sourcePrefabPath))
                throw new Exception("UISkin layout outputPrefabPath must end with " + OutputPrefabFileName(layout.sourcePrefabPath));
            RequireNonEmpty(layout.generatedVisualRoot, "generatedVisualRoot");
            RequireNonEmpty(layout.referenceResolution, "referenceResolution");
            if (layout.layers == null || layout.hideNodes == null || layout.moveNodes == null || layout.createNodes == null ||
                layout.styleTextNodes == null || layout.preserveNodes == null || layout.validationSlots == null)
                throw new Exception("UISkin layout list fields are required");
            RequireUnique(layout.layers.Select(l => l.name), "layer name");
            foreach (var layer in layout.layers)
                RequireNonEmpty(layer.name, "layer.name");
            var layers = new HashSet<string>(layout.layers.Select(l => l.name));
            var regionIds = detectedLayout != null ? new HashSet<string>(detectedLayout.regions.Select(r => r.id)) : null;
            var cropPaths = crops != null ? new HashSet<string>(crops.crops.Select(c => c.outputPath)) : null;
            foreach (var node in layout.hideNodes)
            {
                RequireNonEmpty(node.path, "hideNode.path");
                RequireNonEmpty(node.reason, "hideNode.reason");
            }
            foreach (var node in layout.createNodes)
            {
                RequireNonEmpty(node.name, "createNode.name");
                RequireNonEmpty(node.type, "createNode.type");
                RequireNonEmpty(node.targetLayer, "createNode.targetLayer");
                RequireLayer(layers, node.targetLayer, "createNode.targetLayer");
                RequireNonEmpty(node.targetRegionId, "createNode.targetRegionId");
                if (regionIds != null && !regionIds.Contains(node.targetRegionId))
                    throw new Exception("UISkin create node references missing region: " + node.name + " -> " + node.targetRegionId);
                RequireImagePath(node.spritePath, "createNode.spritePath");
                if (cropPaths != null && !cropPaths.Contains(node.spritePath))
                    throw new Exception("UISkin create node references missing crop output: " + node.name + " -> " + node.spritePath);
                ValidateUnityRect(node.rect, node.name);
            }
            foreach (var node in layout.moveNodes)
            {
                RequireNonEmpty(node.binding, "moveNode.binding");
                RequireNonEmpty(node.targetLayer, "moveNode.targetLayer");
                RequireLayer(layers, node.targetLayer, "moveNode.targetLayer");
                if (!string.IsNullOrEmpty(node.targetRegionId) && regionIds != null && !regionIds.Contains(node.targetRegionId))
                    throw new Exception("UISkin move node references missing region: " + node.binding + " -> " + node.targetRegionId);
                ValidateUnityRect(node.rect, node.binding);
            }
            foreach (var node in layout.styleTextNodes)
            {
                RequireNonEmpty(node.binding, "styleTextNode.binding");
                ValidateUnityRect(node.rect, node.binding);
                if (node.fontSize <= 0)
                    throw new Exception("UISkin styleTextNode fontSize must be positive: " + node.binding);
                RequireNonEmpty(node.color, "styleTextNode.color");
                RequireNonEmpty(node.alignment, "styleTextNode.alignment");
            }
            foreach (var node in layout.preserveNodes)
            {
                RequireNonEmpty(node.binding, "preserveNode.binding");
                RequireNonEmpty(node.policy, "preserveNode.policy");
            }
            foreach (var slot in layout.validationSlots)
            {
                RequireNonEmpty(slot.id, "validationSlot.id");
                RequireNonEmpty(slot.check, "validationSlot.check");
                RequireNonEmpty(slot.target, "validationSlot.target");
                RequireNonEmpty(slot.expected, "validationSlot.expected");
            }
        }

        static T LoadJson<T>(string path)
        {
            if (!File.Exists(path))
                throw new Exception("Missing UI skin JSON: " + path);
            var value = JsonUtility.FromJson<T>(File.ReadAllText(path));
            if (value == null)
                throw new Exception("Invalid UI skin JSON: " + path);
            return value;
        }

        static void WriteJson<T>(string path, T value)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, JsonUtility.ToJson(value, true), new UTF8Encoding(true));
        }

        static void WriteLines(string path, IEnumerable<string> lines)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllLines(path, lines, new UTF8Encoding(true));
        }

        static void RequireNonEmpty(string value, string field)
        {
            if (string.IsNullOrEmpty(value))
                throw new Exception("UISkin field is required: " + field);
        }

        static void RequireAssetPath(string path, string field)
        {
            RequireNonEmpty(path, field);
            if (!path.StartsWith("Assets/", StringComparison.Ordinal) || path.Contains("\\") || path.Contains("/../") || path.EndsWith("/..", StringComparison.Ordinal))
                throw new Exception("UISkin " + field + " path is invalid: " + path);
        }

        static void RequireReportPath(string path, string field)
        {
            RequireNonEmpty(path, field);
            if (!path.StartsWith("UIAIToolsReports/", StringComparison.Ordinal) || path.Contains("\\") || path.Contains("/../") || path.EndsWith("/..", StringComparison.Ordinal))
                throw new Exception("UISkin " + field + " report path is invalid: " + path);
        }

        static void RequireImagePath(string path, string field)
        {
            RequireAssetPath(path, field);
            var extension = Path.GetExtension(path).ToLowerInvariant();
            if (extension != ".png" && extension != ".jpg" && extension != ".jpeg")
                throw new Exception("UISkin " + field + " must be an image path: " + path);
        }

        static void RequireUnique(IEnumerable<string> values, string label)
        {
            var duplicate = values.Where(v => !string.IsNullOrEmpty(v)).GroupBy(v => v).FirstOrDefault(g => g.Count() > 1);
            if (duplicate != null)
                throw new Exception("Duplicate UI skin " + label + ": " + duplicate.Key);
        }

        static void RequireLayer(HashSet<string> layers, string layer, string field)
        {
            if (!layers.Contains(layer))
                throw new Exception("UISkin " + field + " references missing layer: " + layer);
        }

        static void ValidateRect(UISkinPixelRect rect, int maxWidth, int maxHeight, string id)
        {
            if (rect.width <= 0 || rect.height <= 0 || rect.x < 0 || rect.y < 0 || rect.x > maxWidth - rect.width || rect.y > maxHeight - rect.height)
                throw new Exception("Invalid UI skin pixel rect: " + id);
        }

        static void ValidateUnityRect(UISkinRectTransform rect, string id)
        {
            RequireNonEmpty(rect.anchor, "rect.anchor");
            if (rect.width <= 0 || rect.height <= 0)
                throw new Exception("Invalid UI skin rect size: " + id);
        }

        static UISkinPixelRect Rect(int x, int y, int width, int height)
        {
            return new UISkinPixelRect { x = x, y = y, width = width, height = height };
        }

        static UISkinRectTransform UnityRect(float x, float y, float width, float height)
        {
            return new UISkinRectTransform { anchor = "middle_center", x = x, y = y, width = width, height = height };
        }

        static string OutputPrefabPath(string skinFolder, string sourcePrefabPath)
        {
            return skinFolder.TrimEnd('/', '\\') + "/Prefabs/" + OutputPrefabFileName(sourcePrefabPath);
        }

        static string OutputPrefabFileName(string sourcePrefabPath)
        {
            return Path.GetFileNameWithoutExtension(sourcePrefabPath) + "_v2.prefab";
        }

        static void ExpectContractFailure(string label, Action action, string expectedMessage)
        {
            try
            {
                action();
            }
            catch (Exception exception)
            {
                if (exception.Message.Contains(expectedMessage))
                    return;
                throw new Exception("Unexpected UI skin contract failure for " + label + ": " + exception.Message);
            }
            throw new Exception("UI skin contract sample did not fail: " + label);
        }

        static void ExpectReportContains(string label, string path, string expectedText)
        {
            if (!File.ReadAllText(path).Contains(expectedText))
                throw new Exception("UI skin contract report missing " + label + ": " + expectedText);
        }

        static string RectText(UISkinPixelRect rect)
        {
            return rect.x + "," + rect.y + "," + rect.width + "x" + rect.height;
        }

        static string SizeText(UISkinPixelRect rect)
        {
            return rect.width + "x" + rect.height;
        }
    }
}

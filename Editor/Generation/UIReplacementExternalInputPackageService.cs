using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Xipin.UIAITools
{
    public static class UIReplacementExternalInputPackageService
    {
        public static string Generate(UIAIToolsProfile profile, UIRedesignRequest request, string briefPath, string draftJsonPath)
        {
            ValidateSourceReports(profile);
            var package = Package(profile, request, briefPath, draftJsonPath);
            var path = UIReportFiles.GetPath(profile.logRoot, UIReportFiles.ReplacementExternalInputPackage);
            File.WriteAllText(path, UICreationBriefTemplateService.ToJsonWithRootArrays(package, "outputDirectories", "referenceInputs", "inputs"), new UTF8Encoding(true));
            RequireGeneratedPackageJson(path, package);
            var summary = GenerateSummary(profile, path, package);
            var tasks = GenerateTasks(profile, path, package);
            var promptPack = GeneratePromptPack(profile, path, package);
            var promptItems = GeneratePromptItemFiles(profile, path, package);
            var referenceCopyList = GenerateReferenceCopyList(profile, path, package);
            Validate(profile, request, briefPath, draftJsonPath);
            Debug.Log($"UI replacement external input package generated: {path}, summary: {summary}, tasks: {tasks}, prompt pack: {promptPack}, prompt items: {promptItems}, reference copy list: {referenceCopyList}, {package.inputs.Count} inputs.");
            return path;
        }

        public static string GenerateFromManifest(UIAIToolsProfile profile, UIRedesignRequest request)
        {
            var lines = ManifestLines(profile, request);
            if (string.IsNullOrEmpty(request.sourcePreviewPath))
                request.sourcePreviewPath = ManifestPath(lines, "- sourcePreviewPath：");
            if (string.IsNullOrEmpty(request.stylePrompt))
                request.stylePrompt = ManifestValue(lines, "- stylePrompt：");
            return Generate(profile, request, ManifestPath(lines, "- Brief："), ManifestPath(lines, "- 草稿 JSON："));
        }

        public static void Validate(UIAIToolsProfile profile, UIRedesignRequest request, string briefPath, string draftJsonPath)
        {
            ValidateSourceReports(profile);
            var path = UIReportFiles.GetPath(profile.logRoot, UIReportFiles.ReplacementExternalInputPackage);
            if (!File.Exists(path))
                throw new Exception("Missing UI replacement external input package: " + path);
            var package = LoadPackageJson(path);
            Require(package.sourcePrefabPath == request.sourcePrefabPath, "sourcePrefabPath");
            if (!string.IsNullOrEmpty(request.sourcePreviewPath))
                Require(package.sourcePreviewPath == request.sourcePreviewPath, "sourcePreviewPath");
            Require(package.sourcePreviewReadiness == SourcePreviewReadiness(package.sourcePreviewPath), "sourcePreviewReadiness");
            Require(package.stylePrompt == request.stylePrompt, "stylePrompt");
            Require(package.outputFolder == request.outputFolder, "outputFolder");
            Require(package.briefPath == briefPath, "briefPath");
            Require(package.draftJsonPath == draftJsonPath, "draftJsonPath");
            Require(package.pendingInputsCsvPath == UIReportFiles.GetPath(profile.logRoot, UIReportFiles.ReplacementPendingInputs), "pendingInputsCsvPath");
            Require(package.pendingInputReadinessCsvPath == UIReportFiles.GetPath(profile.logRoot, UIReportFiles.ReplacementPendingInputReadiness), "pendingInputReadinessCsvPath");
            var expectedInputs = Inputs(profile);
            ApplyOutputSizes(new ExternalInputPackage { sourcePreviewPath = package.sourcePreviewPath, inputs = expectedInputs });
            if (package.inputs.Count != expectedInputs.Count)
                throw new Exception($"UI replacement external input package item count mismatch: {package.inputs.Count}->{expectedInputs.Count}");
            foreach (var expected in expectedInputs)
                RequireInput(package.inputs, expected);
            var reuseRows = UIScanReportRows.ReadReuseIndex(profile);
            foreach (var input in package.inputs)
                RequireInputDerivedFields(input, reuseRows);
            RequireOutputDirectories(package.outputDirectories, OutputDirectories(package.inputs));
            RequireReferenceInputs(package.referenceInputs, ReferenceInputs(package));
            ValidateSummary(profile, package);
            ValidateTasks(profile, package);
            ValidatePromptPack(profile, package);
            ValidateReferenceCopyList(profile, package);
            Debug.Log($"UI replacement external input package validation passed: {package.inputs.Count} inputs.");
        }

        public static void ValidateFromManifest(UIAIToolsProfile profile, UIRedesignRequest request)
        {
            var lines = ManifestLines(profile, request);
            if (string.IsNullOrEmpty(request.sourcePreviewPath))
                request.sourcePreviewPath = ManifestPath(lines, "- sourcePreviewPath：");
            if (string.IsNullOrEmpty(request.stylePrompt))
                request.stylePrompt = ManifestValue(lines, "- stylePrompt：");
            Validate(profile, request, ManifestPath(lines, "- Brief："), ManifestPath(lines, "- 草稿 JSON："));
        }

        public static void ValidateContract()
        {
            var root = Path.Combine(Path.GetTempPath(), "UIAIToolsExternalInputPackageContract_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                WritePackageJson(root, "valid.json", ContractPackage());
                LoadPackageJson(Path.Combine(root, "valid.json"));

                var duplicateDirectory = ContractPackage();
                duplicateDirectory.outputDirectories.Add(ContractOutputDirectory());
                ExpectFailure(root, "duplicate_directory.json", duplicateDirectory, "duplicate output directory");

                var duplicateReference = ContractPackage();
                duplicateReference.referenceInputs.Add(ContractReferenceInput());
                ExpectFailure(root, "duplicate_reference.json", duplicateReference, "duplicate reference input");

                var duplicateInput = ContractPackage();
                duplicateInput.inputs.Add(ContractInput());
                ExpectFailure(root, "duplicate_input.json", duplicateInput, "duplicate input");
            }
            finally
            {
                if (Directory.Exists(root))
                    Directory.Delete(root, true);
            }
            Debug.Log("UI replacement external input package contract validation passed.");
        }

        static ExternalInputPackage Package(UIAIToolsProfile profile, UIRedesignRequest request, string briefPath, string draftJsonPath)
        {
            var package = new ExternalInputPackage
            {
                generatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                sourcePrefabPath = request.sourcePrefabPath,
                sourcePreviewPath = request.sourcePreviewPath,
                sourcePreviewReadiness = SourcePreviewReadiness(request.sourcePreviewPath),
                stylePrompt = request.stylePrompt,
                outputFolder = request.outputFolder,
                briefPath = briefPath,
                draftJsonPath = draftJsonPath,
                pendingInputsCsvPath = UIReportFiles.GetPath(profile.logRoot, UIReportFiles.ReplacementPendingInputs),
                pendingInputReadinessCsvPath = UIReportFiles.GetPath(profile.logRoot, UIReportFiles.ReplacementPendingInputReadiness),
                inputs = Inputs(profile)
            };
            ApplyOutputSizes(package);
            package.outputDirectories = OutputDirectories(package.inputs);
            package.referenceInputs = ReferenceInputs(package);
            return package;
        }

        static List<ExternalInput> Inputs(UIAIToolsProfile profile)
        {
            var rows = UIReplacementPendingInputReadinessService.ReadRows(profile);
            var reuseRows = UIScanReportRows.ReadReuseIndex(profile);
            return rows.Select(row => new ExternalInput
            {
                inputKind = row["InputKind"],
                pendingStatus = row["PendingStatus"],
                readiness = row["Readiness"],
                outputPath = row["Path"],
                outputDirectory = DirectoryPath(row["Path"]),
                outputFileName = Path.GetFileName(row["Path"]),
                outputWidth = row["InputKind"] == "NewAsset" ? ReferenceValue(reuseRows, row["ReferencePath"], "Width") : "",
                outputHeight = row["InputKind"] == "NewAsset" ? ReferenceValue(reuseRows, row["ReferencePath"], "Height") : "",
                actualWidth = row["ActualWidth"],
                actualHeight = row["ActualHeight"],
                outputDirectoryReadiness = Directory.Exists(DirectoryPath(row["Path"])) ? "Ready" : "Missing",
                referencePath = row["ReferencePath"],
                targetAtlasPath = row["TargetAtlas"],
                referenceReadiness = ReferenceReadiness(row["ReferencePath"]),
                referenceWidth = ReferenceValue(reuseRows, row["ReferencePath"], "Width"),
                referenceHeight = ReferenceValue(reuseRows, row["ReferencePath"], "Height"),
                referenceCopyFileName = InputReferenceCopyFileName(row["ReferencePath"], row["ItemIndices"]),
                itemIndices = row["ItemIndices"],
                count = int.Parse(row["Count"]),
                sourceAction = row["SourceAction"],
                note = row["Note"],
                taskPrompt = TaskPrompt(row, ReferenceValue(reuseRows, row["ReferencePath"], "Width"), ReferenceValue(reuseRows, row["ReferencePath"], "Height")),
                acceptanceCheck = AcceptanceCheck(row["InputKind"], row["Path"], "", "")
            }).ToList();
        }

        static string GenerateSummary(UIAIToolsProfile profile, string jsonPath, ExternalInputPackage package)
        {
            var path = UIReportFiles.GetPath(profile.logRoot, UIReportFiles.ReplacementExternalInputPackageSummary);
            var missing = MissingInputs(package).Count;
            var invalid = InvalidInputs(package).Count;
            var notReady = NotReadyInputs(package).Count;
            var lines = new List<string>
            {
                "# UI 替换外部生成输入包",
                "",
                $"生成时间：{package.generatedAt}",
                "",
                "本文件整理外部生成工具需要读取和落位的输入，不调用 AI、不生成图片、不创建图集。",
                "",
                "## 总览",
                $"- JSON：`{jsonPath}`",
                $"- sourcePrefabPath：`{package.sourcePrefabPath}`",
                $"- sourcePreviewPath：`{package.sourcePreviewPath}`",
                $"- sourcePreviewReadiness：{package.sourcePreviewReadiness}",
                $"- stylePrompt：{package.stylePrompt}",
                $"- Brief：`{package.briefPath}`",
                $"- 草稿 JSON：`{package.draftJsonPath}`",
                $"- 输入总数：{package.inputs.Count}",
                $"- 未就绪：{notReady}",
                $"- 仍缺失：{missing}",
                $"- 格式异常：{invalid}",
                ""
            };
            AddSizeStatusSummary(lines, package);
            AddOutputDirectorySummary(lines, package);
            AddDirectoryPreparation(lines, package);
            AddPlacementDirectoryGroups(lines, package);
            AddReferenceSummary(lines, package);
            AddReferenceInputs(lines, package);
            AddPromptItemSummary(lines, profile, package);
            AddPlacementRerunSteps(lines, package);
            AddInputs(lines, "新版预览", package.inputs.Where(i => i.inputKind == "Preview").ToList());
            AddInputs(lines, "新图", package.inputs.Where(i => i.inputKind == "NewAsset").ToList());
            AddInputs(lines, "目标图集", package.inputs.Where(i => i.inputKind == "TargetAtlas").ToList());
            File.WriteAllLines(path, lines, new UTF8Encoding(true));
            RequireSummarySections(lines.ToArray());
            return path;
        }

        static string GeneratePromptPack(UIAIToolsProfile profile, string jsonPath, ExternalInputPackage package)
        {
            var path = UIReportFiles.GetPath(profile.logRoot, UIReportFiles.ReplacementExternalPromptPack);
            var notReadyInputs = NotReadyInputs(package);
            var lines = new List<string>
            {
                "# UI 替换外部生成 Prompt Pack",
                "",
                $"生成时间：{package.generatedAt}",
                "",
                "本文件只整理外部生成提示词，不调用 AI、不生成图片、不创建图集。",
                "",
                "## 全局上下文",
                $"- JSON：`{jsonPath}`",
                $"- sourcePrefabPath：`{package.sourcePrefabPath}`",
                $"- sourcePreview：{package.sourcePreviewReadiness}，`{package.sourcePreviewPath}`",
                $"- stylePrompt：{package.stylePrompt}",
                $"- Brief：`{package.briefPath}`",
                $"- 草稿 JSON：`{package.draftJsonPath}`",
                $"- outputFolder：`{package.outputFolder}`",
                $"- 单项 Prompt 清单：`{UIReportFiles.GetPath(profile.logRoot, UIReportFiles.ReplacementExternalPromptItemList)}`",
                $"- 未就绪任务：{notReadyInputs.Count}",
                $"- 缺失任务：{MissingInputs(package).Count}",
                $"- 格式异常：{InvalidInputs(package).Count}",
                "",
                "## 全局约束",
                "- 只生成外部图片或确认目标图集输入，不修改原 prefab、旧图片、SpriteAtlas 或 YooAsset 配置。",
                "- 输出路径必须严格使用 prompt 块中的 outputPath。",
                "- 新版预览和新图必须是 PNG。",
                ""
            };
            AddSizeStatusSummary(lines, package);
            AddOutputDirectories(lines, package);
            AddDirectoryPreparation(lines, package);
            AddReferenceInputs(lines, package);
            AddPromptBlocks(lines, notReadyInputs);
            File.WriteAllLines(path, lines, new UTF8Encoding(true));
            RequirePromptPackSections(lines.ToArray());
            return path;
        }

        static string GeneratePromptItemFiles(UIAIToolsProfile profile, string jsonPath, ExternalInputPackage package)
        {
            var path = UIReportFiles.GetPath(profile.logRoot, UIReportFiles.ReplacementExternalPromptItemList);
            var inputs = NotReadyInputs(package);
            var lines = new List<string>
            {
                "# UI 替换外部生成单项 Prompt",
                "",
                $"生成时间：{package.generatedAt}",
                "",
                "本文件列出每个未就绪输入对应的单项 prompt 文件；不调用 AI、不生成图片、不创建图集。",
                "",
                "## 总览",
                $"- JSON：`{jsonPath}`",
                $"- Prompt Pack：`{UIReportFiles.GetPath(profile.logRoot, UIReportFiles.ReplacementExternalPromptPack)}`",
                $"- 单项任务：{inputs.Count}",
                "",
                "## 目录索引"
            };

            AddPromptItemDirectoryIndex(lines, profile, inputs);
            lines.Add("## 文件清单");

            if (inputs.Count == 0)
            {
                lines.Add("- 无");
            }
            else
            {
                foreach (var input in inputs)
                {
                    var itemPath = PromptItemPath(profile, input);
                    lines.Add(PromptItemListSummary(input, itemPath));
                    var itemLines = PromptItemLines(jsonPath, package, input);
                    File.WriteAllLines(itemPath, itemLines, new UTF8Encoding(true));
                    RequirePromptItemSections("external prompt item " + itemPath, itemLines.ToArray());
                }
            }

            lines.Add("");
            File.WriteAllLines(path, lines, new UTF8Encoding(true));
            RequirePromptItemListSections(lines.ToArray());
            return path;
        }

        static void AddPromptItemDirectoryIndex(List<string> lines, UIAIToolsProfile profile, List<ExternalInput> inputs)
        {
            if (inputs.Count == 0)
            {
                lines.Add("- 无");
                lines.Add("");
                return;
            }

            foreach (var group in inputs.GroupBy(i => i.outputDirectory).OrderBy(g => g.Key))
                lines.Add(PromptItemDirectoryIndexSummary(profile, group));
            lines.Add("");
        }

        static string GenerateTasks(UIAIToolsProfile profile, string jsonPath, ExternalInputPackage package)
        {
            var path = UIReportFiles.GetPath(profile.logRoot, UIReportFiles.ReplacementExternalGenerationTasks);
            var notReadyInputs = NotReadyInputs(package);
            var lines = new List<string>
            {
                "# UI 替换外部生成任务",
                "",
                $"生成时间：{package.generatedAt}",
                "",
                "本文件把缺失的新版预览、新图和目标图集整理成任务清单；不调用 AI、不生成图片、不创建图集。",
                "",
                "## 总览",
                $"- JSON：`{jsonPath}`",
                $"- sourcePreview：{package.sourcePreviewReadiness}，`{package.sourcePreviewPath}`",
                $"- stylePrompt：{package.stylePrompt}",
                $"- 未就绪任务：{notReadyInputs.Count}",
                $"- 缺失任务：{MissingInputs(package).Count}",
                $"- 格式异常：{InvalidInputs(package).Count}",
                $"- 参考素材缺失：{package.inputs.Count(i => i.referenceReadiness == "Missing")}",
                ""
            };
            AddSizeStatusSummary(lines, package);
            AddOutputDirectories(lines, package);
            AddDirectoryPreparation(lines, package);
            AddPlacementDirectoryGroups(lines, package);
            AddReferenceInputs(lines, package);
            AddPlacementRerunSteps(lines, package);
            AddTasks(lines, "未就绪任务", notReadyInputs, profile, true);
            AddTasks(lines, "已就绪输入", ReadyInputs(package), profile, false);
            File.WriteAllLines(path, lines, new UTF8Encoding(true));
            RequireTaskSections(lines.ToArray());
            return path;
        }

        static string GenerateReferenceCopyList(UIAIToolsProfile profile, string jsonPath, ExternalInputPackage package)
        {
            var path = UIReportFiles.GetPath(profile.logRoot, UIReportFiles.ReplacementExternalReferenceCopyList);
            var missingReferences = package.referenceInputs.Count(i => i.readiness == "Missing");
            var lines = new List<string>
            {
                "# UI 替换外部生成引用素材清单",
                "",
                $"生成时间：{package.generatedAt}",
                "",
                "本文件只列出外部生成工具需要准备的引用素材，不复制文件、不创建目录。",
                "",
                "## 总览",
                $"- JSON：`{jsonPath}`",
                $"- 引用素材：{package.referenceInputs.Count}",
                $"- 引用缺失：{missingReferences}",
                ""
            };
            AddReferenceImportSteps(lines, package);
            AddReferenceSourceDirectoryGroups(lines, package);
            lines.Add("## 复制清单");
            foreach (var reference in package.referenceInputs)
                lines.Add(ReferenceCopySummary(reference));
            lines.Add("");
            File.WriteAllLines(path, lines, new UTF8Encoding(true));
            RequireReferenceCopyListSections(lines.ToArray());
            return path;
        }

        static void AddReferenceSummary(List<string> lines, ExternalInputPackage package)
        {
            lines.Add("## 参考素材");
            lines.Add($"- sourcePreview：{package.sourcePreviewReadiness}");
            foreach (var group in package.inputs.GroupBy(i => i.referenceReadiness).OrderBy(g => g.Key))
                lines.Add($"- {group.Key}：{group.Count()}");
            lines.Add("");
        }

        static void AddInputs(List<string> lines, string title, List<ExternalInput> inputs)
        {
            lines.Add("## " + title);
            if (inputs.Count == 0)
            {
                lines.Add("- 无");
                lines.Add("");
                return;
            }
            foreach (var input in inputs.Take(30))
                lines.Add(InputSummary(input));
            if (inputs.Count > 30)
                lines.Add($"- 仅显示前 30 项，共 {inputs.Count} 项。");
            lines.Add("");
        }

        static void ValidateSummary(UIAIToolsProfile profile, ExternalInputPackage package)
        {
            var path = UIReportFiles.GetPath(profile.logRoot, UIReportFiles.ReplacementExternalInputPackageSummary);
            if (!File.Exists(path))
                throw new Exception("Missing UI replacement external input package summary: " + path);
            var lines = File.ReadAllLines(path);
            RequireSummarySections(lines);
            RequireLine(lines, $"- JSON：`{UIReportFiles.GetPath(profile.logRoot, UIReportFiles.ReplacementExternalInputPackage)}`");
            RequireLine(lines, $"- sourcePrefabPath：`{package.sourcePrefabPath}`");
            RequireLine(lines, $"- sourcePreviewPath：`{package.sourcePreviewPath}`");
            RequireLine(lines, $"- sourcePreviewReadiness：{package.sourcePreviewReadiness}");
            RequireLine(lines, $"- stylePrompt：{package.stylePrompt}");
            RequireLine(lines, $"- Brief：`{package.briefPath}`");
            RequireLine(lines, $"- 草稿 JSON：`{package.draftJsonPath}`");
            RequireLine(lines, $"- 输入总数：{package.inputs.Count}");
            RequireLine(lines, $"- 未就绪：{NotReadyInputs(package).Count}");
            RequireLine(lines, $"- 仍缺失：{MissingInputs(package).Count}");
            RequireLine(lines, $"- 格式异常：{InvalidInputs(package).Count}");
            ValidateSizeStatusSummary(lines, package);
            ValidateOutputDirectorySummary(lines, package);
            ValidateDirectoryPreparation(lines, package);
            ValidatePlacementDirectoryGroups(lines, package);
            RequireLine(lines, $"- sourcePreview：{package.sourcePreviewReadiness}");
            foreach (var group in package.inputs.GroupBy(i => i.referenceReadiness).OrderBy(g => g.Key))
                RequireLine(lines, $"- {group.Key}：{group.Count()}");
            ValidateReferenceInputs(lines, package);
            ValidatePromptItemSummary(lines, profile, package);
            ValidatePlacementRerunSteps(lines, package);
            ValidateSummaryRows(lines, "新版预览", package.inputs.Where(i => i.inputKind == "Preview").ToList());
            ValidateSummaryRows(lines, "新图", package.inputs.Where(i => i.inputKind == "NewAsset").ToList());
            ValidateSummaryRows(lines, "目标图集", package.inputs.Where(i => i.inputKind == "TargetAtlas").ToList());
        }

        static void ValidateSummaryRows(string[] lines, string title, List<ExternalInput> inputs)
        {
            RequireLine(lines, "## " + title);
            if (inputs.Count == 0)
            {
                RequireLine(lines, "- 无");
                return;
            }
            foreach (var input in inputs.Take(30))
                RequireLine(lines, InputSummary(input));
            if (inputs.Count > 30)
                RequireLine(lines, $"- 仅显示前 30 项，共 {inputs.Count} 项。");
        }

        static void ValidateTasks(UIAIToolsProfile profile, ExternalInputPackage package)
        {
            var path = UIReportFiles.GetPath(profile.logRoot, UIReportFiles.ReplacementExternalGenerationTasks);
            if (!File.Exists(path))
                throw new Exception("Missing UI replacement external generation tasks: " + path);
            var lines = File.ReadAllLines(path);
            var notReadyInputs = NotReadyInputs(package);
            RequireTaskSections(lines);
            RequireLine(lines, $"- JSON：`{UIReportFiles.GetPath(profile.logRoot, UIReportFiles.ReplacementExternalInputPackage)}`");
            RequireLine(lines, $"- sourcePreview：{package.sourcePreviewReadiness}，`{package.sourcePreviewPath}`");
            RequireLine(lines, $"- stylePrompt：{package.stylePrompt}");
            RequireLine(lines, $"- 未就绪任务：{notReadyInputs.Count}");
            RequireLine(lines, $"- 缺失任务：{MissingInputs(package).Count}");
            RequireLine(lines, $"- 格式异常：{InvalidInputs(package).Count}");
            RequireLine(lines, $"- 参考素材缺失：{package.inputs.Count(i => i.referenceReadiness == "Missing")}");
            ValidateSizeStatusSummary(lines, package);
            ValidateOutputDirectories(lines, package);
            ValidateDirectoryPreparation(lines, package);
            ValidatePlacementDirectoryGroups(lines, package);
            ValidateReferenceInputs(lines, package);
            ValidatePlacementRerunSteps(lines, package);
            ValidateTaskRows(lines, "未就绪任务", notReadyInputs, profile, true);
            ValidateTaskRows(lines, "已就绪输入", ReadyInputs(package), profile, false);
        }

        static void ValidatePromptPack(UIAIToolsProfile profile, ExternalInputPackage package)
        {
            var path = UIReportFiles.GetPath(profile.logRoot, UIReportFiles.ReplacementExternalPromptPack);
            if (!File.Exists(path))
                throw new Exception("Missing UI replacement external prompt pack: " + path);
            var lines = File.ReadAllLines(path);
            var notReadyInputs = NotReadyInputs(package);
            RequirePromptPackSections(lines);
            RequireLine(lines, $"- JSON：`{UIReportFiles.GetPath(profile.logRoot, UIReportFiles.ReplacementExternalInputPackage)}`");
            RequireLine(lines, $"- sourcePrefabPath：`{package.sourcePrefabPath}`");
            RequireLine(lines, $"- sourcePreview：{package.sourcePreviewReadiness}，`{package.sourcePreviewPath}`");
            RequireLine(lines, $"- stylePrompt：{package.stylePrompt}");
            RequireLine(lines, $"- Brief：`{package.briefPath}`");
            RequireLine(lines, $"- 草稿 JSON：`{package.draftJsonPath}`");
            RequireLine(lines, $"- outputFolder：`{package.outputFolder}`");
            RequireLine(lines, $"- 单项 Prompt 清单：`{UIReportFiles.GetPath(profile.logRoot, UIReportFiles.ReplacementExternalPromptItemList)}`");
            RequireLine(lines, $"- 未就绪任务：{notReadyInputs.Count}");
            RequireLine(lines, $"- 缺失任务：{MissingInputs(package).Count}");
            RequireLine(lines, $"- 格式异常：{InvalidInputs(package).Count}");
            ValidateSizeStatusSummary(lines, package);
            ValidateOutputDirectories(lines, package);
            ValidateDirectoryPreparation(lines, package);
            ValidateReferenceInputs(lines, package);
            ValidatePromptBlocks(lines, notReadyInputs);
            ValidatePromptItemFiles(profile, package);
        }

        static void ValidateReferenceCopyList(UIAIToolsProfile profile, ExternalInputPackage package)
        {
            var path = UIReportFiles.GetPath(profile.logRoot, UIReportFiles.ReplacementExternalReferenceCopyList);
            if (!File.Exists(path))
                throw new Exception("Missing UI replacement external reference copy list: " + path);
            var lines = File.ReadAllLines(path);
            RequireReferenceCopyListSections(lines);
            RequireLine(lines, $"- JSON：`{UIReportFiles.GetPath(profile.logRoot, UIReportFiles.ReplacementExternalInputPackage)}`");
            RequireLine(lines, $"- 引用素材：{package.referenceInputs.Count}");
            RequireLine(lines, $"- 引用缺失：{package.referenceInputs.Count(i => i.readiness == "Missing")}");
            ValidateReferenceImportSteps(lines, package);
            ValidateReferenceSourceDirectoryGroups(lines, package);
            RequireLine(lines, "## 复制清单");
            foreach (var reference in package.referenceInputs)
                RequireLine(lines, ReferenceCopySummary(reference));
        }

        static List<ExternalOutputDirectory> OutputDirectories(List<ExternalInput> inputs)
        {
            return inputs.GroupBy(i => i.outputDirectory).OrderBy(g => g.Key).Select(g => new ExternalOutputDirectory
            {
                path = g.Key,
                readiness = g.First().outputDirectoryReadiness,
                inputCount = g.Count(),
                notReadyCount = g.Count(i => i.readiness != "Ready"),
                missingCount = g.Count(i => i.readiness == "Missing"),
                invalidCount = g.Count(i => i.readiness == "Invalid"),
                inputKinds = string.Join(";", g.Select(i => i.inputKind).Distinct().OrderBy(kind => kind))
            }).ToList();
        }

        static List<ExternalReferenceInput> ReferenceInputs(ExternalInputPackage package)
        {
            var sourcePreviewSize = ImageSize(package.sourcePreviewPath);
            var references = new List<ExternalReferenceInput>
            {
                new ExternalReferenceInput
                {
                    referenceKind = "SourcePreview",
                    path = package.sourcePreviewPath,
                    readiness = package.sourcePreviewReadiness,
                    width = sourcePreviewSize[0],
                    height = sourcePreviewSize[1],
                    copyFileName = ReferenceCopyFileName("SourcePreview", package.sourcePreviewPath, ""),
                    useCount = 1,
                    itemIndices = "",
                    inputKinds = "Preview"
                }
            };
            references.AddRange(package.inputs.Where(i => !string.IsNullOrEmpty(i.referencePath)).GroupBy(i => i.referencePath).OrderBy(g => g.Key).Select(g =>
            {
                var first = g.First();
                var itemIndices = string.Join(";", g.Select(i => i.itemIndices).OrderBy(i => int.Parse(i)));
                return new ExternalReferenceInput
                {
                    referenceKind = "OldAsset",
                    path = g.Key,
                    readiness = first.referenceReadiness,
                    width = first.referenceWidth,
                    height = first.referenceHeight,
                    copyFileName = ReferenceCopyFileName("OldAsset", g.Key, itemIndices),
                    useCount = g.Count(),
                    itemIndices = itemIndices,
                    inputKinds = string.Join(";", g.Select(i => i.inputKind).Distinct().OrderBy(kind => kind))
                };
            }));
            return references;
        }

        static List<ExternalInput> MissingInputs(ExternalInputPackage package)
        {
            return package.inputs.Where(i => i.readiness == "Missing").ToList();
        }

        static List<ExternalInput> InvalidInputs(ExternalInputPackage package)
        {
            return package.inputs.Where(i => i.readiness == "Invalid").ToList();
        }

        static List<ExternalInput> NotReadyInputs(ExternalInputPackage package)
        {
            return package.inputs.Where(i => i.readiness != "Ready").ToList();
        }

        static List<ExternalInput> ReadyInputs(ExternalInputPackage package)
        {
            return package.inputs.Where(i => i.readiness == "Ready").ToList();
        }

        static void AddSizeStatusSummary(List<string> lines, ExternalInputPackage package)
        {
            lines.Add("## 尺寸状态分布");
            foreach (var group in package.inputs.GroupBy(i => i.sizeStatus).OrderBy(g => g.Key))
                lines.Add(SizeStatusSummary(group.Key, group.Count()));
            lines.Add("");
        }

        static void ValidateSizeStatusSummary(string[] lines, ExternalInputPackage package)
        {
            RequireLine(lines, "## 尺寸状态分布");
            foreach (var group in package.inputs.GroupBy(i => i.sizeStatus).OrderBy(g => g.Key))
                RequireLine(lines, SizeStatusSummary(group.Key, group.Count()));
        }

        static string SizeStatusSummary(string sizeStatus, int count)
        {
            return $"- {sizeStatus}：{count}";
        }

        static void AddOutputDirectorySummary(List<string> lines, ExternalInputPackage package)
        {
            lines.Add("## 输出目录");
            foreach (var directory in package.outputDirectories)
                lines.Add(OutputDirectorySummary(directory));
            lines.Add("");
        }

        static void ValidateOutputDirectorySummary(string[] lines, ExternalInputPackage package)
        {
            RequireLine(lines, "## 输出目录");
            foreach (var directory in package.outputDirectories)
                RequireLine(lines, OutputDirectorySummary(directory));
        }

        static void AddOutputDirectories(List<string> lines, ExternalInputPackage package)
        {
            lines.Add("## 输出目录");
            foreach (var directory in package.outputDirectories)
                lines.Add($"- [ ] `{directory.path}`：{directory.readiness}，输入 {directory.inputCount}，未就绪 {directory.notReadyCount}，缺失 {directory.missingCount}，异常 {directory.invalidCount}，类型 `{directory.inputKinds}`");
            lines.Add("");
        }

        static void ValidateOutputDirectories(string[] lines, ExternalInputPackage package)
        {
            RequireLine(lines, "## 输出目录");
            foreach (var directory in package.outputDirectories)
                RequireLine(lines, $"- [ ] `{directory.path}`：{directory.readiness}，输入 {directory.inputCount}，未就绪 {directory.notReadyCount}，缺失 {directory.missingCount}，异常 {directory.invalidCount}，类型 `{directory.inputKinds}`");
        }

        static void AddDirectoryPreparation(List<string> lines, ExternalInputPackage package)
        {
            lines.Add("## 目录准备");
            foreach (var directory in package.outputDirectories)
                lines.Add(DirectoryPreparationSummary(directory));
            lines.Add("");
        }

        static void ValidateDirectoryPreparation(string[] lines, ExternalInputPackage package)
        {
            RequireLine(lines, "## 目录准备");
            foreach (var directory in package.outputDirectories)
                RequireLine(lines, DirectoryPreparationSummary(directory));
        }

        static void AddPlacementDirectoryGroups(List<string> lines, ExternalInputPackage package)
        {
            lines.Add("## 待落位目录分组");
            var inputs = NotReadyInputs(package);
            if (inputs.Count == 0)
            {
                lines.Add("- 无");
                lines.Add("");
                return;
            }

            foreach (var group in inputs.GroupBy(i => i.outputDirectory).OrderBy(g => g.Key))
                lines.Add(PlacementDirectoryGroupSummary(group));
            lines.Add("");
        }

        static void ValidatePlacementDirectoryGroups(string[] lines, ExternalInputPackage package)
        {
            RequireLine(lines, "## 待落位目录分组");
            var inputs = NotReadyInputs(package);
            if (inputs.Count == 0)
            {
                RequireLine(lines, "- 无");
                return;
            }

            foreach (var group in inputs.GroupBy(i => i.outputDirectory).OrderBy(g => g.Key))
                RequireLine(lines, PlacementDirectoryGroupSummary(group));
        }

        static void AddReferenceInputs(List<string> lines, ExternalInputPackage package)
        {
            lines.Add("## 参考输入");
            foreach (var reference in package.referenceInputs)
                lines.Add(ReferenceInputSummary(reference));
            lines.Add("");
        }

        static void ValidateReferenceInputs(string[] lines, ExternalInputPackage package)
        {
            RequireLine(lines, "## 参考输入");
            foreach (var reference in package.referenceInputs)
                RequireLine(lines, ReferenceInputSummary(reference));
        }

        static void AddPromptItemSummary(List<string> lines, UIAIToolsProfile profile, ExternalInputPackage package)
        {
            lines.Add("## 单项 Prompt 文件");
            var inputs = NotReadyInputs(package);
            lines.Add($"- 清单：`{UIReportFiles.GetPath(profile.logRoot, UIReportFiles.ReplacementExternalPromptItemList)}`");
            lines.Add($"- 单项任务：{inputs.Count}");
            foreach (var group in inputs.GroupBy(i => i.inputKind).OrderBy(g => g.Key))
                lines.Add($"- {group.Key}：{group.Count()}");
            lines.Add("");
        }

        static void ValidatePromptItemSummary(string[] lines, UIAIToolsProfile profile, ExternalInputPackage package)
        {
            RequireLine(lines, "## 单项 Prompt 文件");
            var inputs = NotReadyInputs(package);
            RequireLine(lines, $"- 清单：`{UIReportFiles.GetPath(profile.logRoot, UIReportFiles.ReplacementExternalPromptItemList)}`");
            RequireLine(lines, $"- 单项任务：{inputs.Count}");
            foreach (var group in inputs.GroupBy(i => i.inputKind).OrderBy(g => g.Key))
                RequireLine(lines, $"- {group.Key}：{group.Count()}");
        }

        static void AddPlacementRerunSteps(List<string> lines, ExternalInputPackage package)
        {
            lines.Add("## 外部产物落位后复跑");
            foreach (var line in PlacementRerunStepLines(package))
                lines.Add(line);
            lines.Add("");
        }

        static void ValidatePlacementRerunSteps(string[] lines, ExternalInputPackage package)
        {
            RequireLine(lines, "## 外部产物落位后复跑");
            foreach (var line in PlacementRerunStepLines(package))
                RequireLine(lines, line);
        }

        static List<string> PlacementRerunStepLines(ExternalInputPackage package)
        {
            return new List<string>
            {
                "- [ ] 将新版预览、新图和目标图集按待补路径落位。",
                "- [ ] 运行 `UIAssetTriageScanner.GenerateReplacementPendingInputReadinessBatch`。",
                "- [ ] 运行 `UIAssetTriageScanner.ValidateReplacementPendingInputReadinessBatch`。",
                $"- [ ] 运行 `UIAssetTriageScanner.ValidateReplacementExternalInputPackageBatch -uiPrefabPath {package.sourcePrefabPath}`。",
                "- [ ] 运行 `UIAssetTriageScanner.ValidateReplacementPendingInputReadyBatch`，确认 Missing/Invalid 清零。",
                $"- [ ] 运行 `UIAssetTriageScanner.PrepareRedesignPackageBatch -uiPrefabPath {package.sourcePrefabPath}` 刷新 dry-run、执行计划、待补输入和宿主清单。"
            };
        }

        static void AddReferenceImportSteps(List<string> lines, ExternalInputPackage package)
        {
            lines.Add("## 导入步骤");
            lines.Add("- [ ] 准备外部生成工具的引用素材目录；本包不创建目录、不复制文件。");
            foreach (var reference in package.referenceInputs)
                lines.Add(ReferenceImportStepSummary(reference));
            lines.Add("");
        }

        static void ValidateReferenceImportSteps(string[] lines, ExternalInputPackage package)
        {
            RequireLine(lines, "## 导入步骤");
            RequireLine(lines, "- [ ] 准备外部生成工具的引用素材目录；本包不创建目录、不复制文件。");
            foreach (var reference in package.referenceInputs)
                RequireLine(lines, ReferenceImportStepSummary(reference));
        }

        static void AddReferenceSourceDirectoryGroups(List<string> lines, ExternalInputPackage package)
        {
            lines.Add("## 引用来源目录分组");
            foreach (var group in package.referenceInputs.GroupBy(i => DirectoryPath(i.path)).OrderBy(g => g.Key))
                lines.Add(ReferenceSourceDirectoryGroupSummary(group));
            lines.Add("");
        }

        static void ValidateReferenceSourceDirectoryGroups(string[] lines, ExternalInputPackage package)
        {
            RequireLine(lines, "## 引用来源目录分组");
            foreach (var group in package.referenceInputs.GroupBy(i => DirectoryPath(i.path)).OrderBy(g => g.Key))
                RequireLine(lines, ReferenceSourceDirectoryGroupSummary(group));
        }

        static void AddTasks(List<string> lines, string title, List<ExternalInput> inputs, UIAIToolsProfile profile, bool includePromptItem)
        {
            lines.Add("## " + title);
            if (inputs.Count == 0)
            {
                lines.Add("- 无");
                lines.Add("");
                return;
            }
            foreach (var input in inputs.Take(50))
                lines.Add(TaskSummary(input, profile, includePromptItem));
            if (inputs.Count > 50)
                lines.Add($"- 仅显示前 50 项，共 {inputs.Count} 项。");
            lines.Add("");
        }

        static void ValidateTaskRows(string[] lines, string title, List<ExternalInput> inputs, UIAIToolsProfile profile, bool includePromptItem)
        {
            RequireLine(lines, "## " + title);
            if (inputs.Count == 0)
            {
                RequireLine(lines, "- 无");
                return;
            }
            foreach (var input in inputs.Take(50))
                RequireLine(lines, TaskSummary(input, profile, includePromptItem));
            if (inputs.Count > 50)
                RequireLine(lines, $"- 仅显示前 50 项，共 {inputs.Count} 项。");
        }

        static void AddPromptBlocks(List<string> lines, List<ExternalInput> inputs)
        {
            lines.Add("## Prompt Blocks");
            if (inputs.Count == 0)
            {
                lines.Add("- 无");
                lines.Add("");
                return;
            }
            foreach (var input in inputs.Take(50))
            {
                lines.Add($"### {input.inputKind} Item {input.itemIndices}");
                lines.Add($"- outputPath：`{input.outputPath}`");
                lines.Add($"- outputDirectory：{input.outputDirectoryReadiness}，`{input.outputDirectory}`");
                lines.Add($"- outputFileName：`{input.outputFileName}`");
                lines.Add($"- outputSize：{OutputSize(input)}");
                lines.Add($"- actualSize：{ActualSize(input)}");
                lines.Add($"- sizeStatus：{input.sizeStatus}");
                lines.Add($"- referencePath：`{input.referencePath}`");
                lines.Add($"- referenceCopyFileName：`{input.referenceCopyFileName}`");
                lines.Add($"- referenceReadiness：{input.referenceReadiness}");
                lines.Add($"- referenceSize：{ReferenceSize(input)}");
                lines.Add($"- targetAtlasPath：`{input.targetAtlasPath}`");
                lines.Add($"- prompt：{input.taskPrompt}");
                lines.Add($"- acceptanceCheck：{input.acceptanceCheck}");
                lines.Add("");
            }
            if (inputs.Count > 50)
                lines.Add($"- 仅显示前 50 项，共 {inputs.Count} 项。");
        }

        static void ValidatePromptBlocks(string[] lines, List<ExternalInput> inputs)
        {
            RequireLine(lines, "## Prompt Blocks");
            if (inputs.Count == 0)
            {
                RequireLine(lines, "- 无");
                return;
            }
            foreach (var input in inputs.Take(50))
            {
                RequireLine(lines, $"### {input.inputKind} Item {input.itemIndices}");
                RequireLine(lines, $"- outputPath：`{input.outputPath}`");
                RequireLine(lines, $"- outputDirectory：{input.outputDirectoryReadiness}，`{input.outputDirectory}`");
                RequireLine(lines, $"- outputFileName：`{input.outputFileName}`");
                RequireLine(lines, $"- outputSize：{OutputSize(input)}");
                RequireLine(lines, $"- actualSize：{ActualSize(input)}");
                RequireLine(lines, $"- sizeStatus：{input.sizeStatus}");
                RequireLine(lines, $"- referencePath：`{input.referencePath}`");
                RequireLine(lines, $"- referenceCopyFileName：`{input.referenceCopyFileName}`");
                RequireLine(lines, $"- referenceReadiness：{input.referenceReadiness}");
                RequireLine(lines, $"- referenceSize：{ReferenceSize(input)}");
                RequireLine(lines, $"- targetAtlasPath：`{input.targetAtlasPath}`");
                RequireLine(lines, $"- prompt：{input.taskPrompt}");
                RequireLine(lines, $"- acceptanceCheck：{input.acceptanceCheck}");
            }
            if (inputs.Count > 50)
                RequireLine(lines, $"- 仅显示前 50 项，共 {inputs.Count} 项。");
        }

        static void ValidatePromptItemFiles(UIAIToolsProfile profile, ExternalInputPackage package)
        {
            var path = UIReportFiles.GetPath(profile.logRoot, UIReportFiles.ReplacementExternalPromptItemList);
            if (!File.Exists(path))
                throw new Exception("Missing UI replacement external prompt item list: " + path);
            var lines = File.ReadAllLines(path);
            var inputs = NotReadyInputs(package);
            var jsonPath = UIReportFiles.GetPath(profile.logRoot, UIReportFiles.ReplacementExternalInputPackage);
            RequirePromptItemListSections(lines);
            RequireLine(lines, $"- JSON：`{jsonPath}`");
            RequireLine(lines, $"- Prompt Pack：`{UIReportFiles.GetPath(profile.logRoot, UIReportFiles.ReplacementExternalPromptPack)}`");
            RequireLine(lines, $"- 单项任务：{inputs.Count}");
            ValidatePromptItemDirectoryIndex(lines, profile, inputs);
            RequireLine(lines, "## 文件清单");
            if (inputs.Count == 0)
            {
                RequireLine(lines, "- 无");
                return;
            }

            foreach (var input in inputs)
            {
                var itemPath = PromptItemPath(profile, input);
                RequireLine(lines, PromptItemListSummary(input, itemPath));
                if (!File.Exists(itemPath))
                    throw new Exception("Missing UI replacement external prompt item: " + itemPath);
                var itemLines = File.ReadAllLines(itemPath);
                RequirePromptItemSections("external prompt item " + itemPath, itemLines);
                foreach (var line in PromptItemLines(jsonPath, package, input))
                    RequireLine(itemLines, line);
            }
        }

        static void ValidatePromptItemDirectoryIndex(string[] lines, UIAIToolsProfile profile, List<ExternalInput> inputs)
        {
            RequireLine(lines, "## 目录索引");
            if (inputs.Count == 0)
            {
                RequireLine(lines, "- 无");
                return;
            }

            foreach (var group in inputs.GroupBy(i => i.outputDirectory).OrderBy(g => g.Key))
                RequireLine(lines, PromptItemDirectoryIndexSummary(profile, group));
        }

        static List<string> PromptItemLines(string jsonPath, ExternalInputPackage package, ExternalInput input)
        {
            var lines = new List<string>
            {
                "# UI 替换外部生成单项 Prompt",
                "",
                $"生成时间：{package.generatedAt}",
                "",
                "本文件只整理单项外部生成提示词，不调用 AI、不生成图片、不创建图集。",
                "",
                "## 全局上下文",
                $"- JSON：`{jsonPath}`",
                $"- sourcePrefabPath：`{package.sourcePrefabPath}`",
                $"- sourcePreview：{package.sourcePreviewReadiness}，`{package.sourcePreviewPath}`",
                $"- stylePrompt：{package.stylePrompt}",
                $"- Brief：`{package.briefPath}`",
                $"- 草稿 JSON：`{package.draftJsonPath}`",
                "",
                "## 任务",
                $"- inputKind：{input.inputKind}",
                $"- itemIndices：`{input.itemIndices}`",
                $"- outputPath：`{input.outputPath}`",
                $"- outputDirectory：{input.outputDirectoryReadiness}，`{input.outputDirectory}`",
                $"- outputFileName：`{input.outputFileName}`",
                $"- outputSize：{OutputSize(input)}",
                $"- actualSize：{ActualSize(input)}",
                $"- sizeStatus：{input.sizeStatus}",
                $"- referencePath：`{input.referencePath}`",
                $"- referenceCopyFileName：`{input.referenceCopyFileName}`",
                $"- referenceReadiness：{input.referenceReadiness}",
                $"- referenceSize：{ReferenceSize(input)}",
                $"- targetAtlasPath：`{input.targetAtlasPath}`",
                $"- prompt：{input.taskPrompt}",
                $"- acceptanceCheck：{input.acceptanceCheck}",
                ""
            };
            AddPromptItemAcceptanceLines(lines, input);
            AddPlacementRerunSteps(lines, package);
            return lines;
        }

        static void AddPromptItemAcceptanceLines(List<string> lines, ExternalInput input)
        {
            lines.Add("## 外部产物验收");
            lines.Add($"- [ ] 输出路径保持 `{input.outputPath}`。");
            lines.Add($"- [ ] 验收规则：{input.acceptanceCheck}");
            if (input.inputKind == "TargetAtlas")
            {
                lines.Add($"- [ ] 目标 SpriteAtlas 文件存在：`{input.outputPath}`。");
                lines.Add($"- [ ] 后续宿主确认 Item `{input.itemIndices}` 的替换 PNG 纳入该图集。");
            }
            else
            {
                lines.Add("- [ ] 输出文件为 PNG，且可被 Unity 解码。");
                lines.Add($"- [ ] 复跑 readiness 后实际尺寸应匹配 `{OutputSize(input)}`。");
            }
            if (input.inputKind == "NewAsset")
                lines.Add($"- [ ] 图集归属保持 `{input.targetAtlasPath}`。");
            lines.Add("");
        }

        static void RequireInput(List<ExternalInput> inputs, ExternalInput expected)
        {
            if (!inputs.Any(input =>
                    input.inputKind == expected.inputKind &&
                    input.pendingStatus == expected.pendingStatus &&
                    input.readiness == expected.readiness &&
                    input.outputPath == expected.outputPath &&
                    input.outputDirectory == expected.outputDirectory &&
                    input.outputFileName == expected.outputFileName &&
                    input.outputWidth == expected.outputWidth &&
                    input.outputHeight == expected.outputHeight &&
                    input.actualWidth == expected.actualWidth &&
                    input.actualHeight == expected.actualHeight &&
                    input.sizeStatus == expected.sizeStatus &&
                    input.outputDirectoryReadiness == expected.outputDirectoryReadiness &&
                    input.referencePath == expected.referencePath &&
                    input.targetAtlasPath == expected.targetAtlasPath &&
                    input.referenceReadiness == expected.referenceReadiness &&
                    input.referenceWidth == expected.referenceWidth &&
                    input.referenceHeight == expected.referenceHeight &&
                    input.referenceCopyFileName == expected.referenceCopyFileName &&
                    input.itemIndices == expected.itemIndices &&
                    input.count == expected.count &&
                    input.sourceAction == expected.sourceAction &&
                    input.note == expected.note &&
                    input.taskPrompt == expected.taskPrompt &&
                    input.acceptanceCheck == expected.acceptanceCheck))
                throw new Exception($"UI replacement external input package is missing: {expected.inputKind} {expected.outputPath}");
        }

        static void RequireOutputDirectories(List<ExternalOutputDirectory> directories, List<ExternalOutputDirectory> expectedDirectories)
        {
            if (directories == null || directories.Count != expectedDirectories.Count)
                throw new Exception($"UI replacement external input package output directory count mismatch: {directories?.Count ?? 0}->{expectedDirectories.Count}");
            foreach (var expected in expectedDirectories)
            {
                if (!directories.Any(directory =>
                        directory.path == expected.path &&
                        directory.readiness == expected.readiness &&
                        directory.inputCount == expected.inputCount &&
                        directory.notReadyCount == expected.notReadyCount &&
                        directory.missingCount == expected.missingCount &&
                        directory.invalidCount == expected.invalidCount &&
                        directory.inputKinds == expected.inputKinds))
                    throw new Exception("UI replacement external input package output directory is missing: " + expected.path);
            }
        }

        static void RequireReferenceInputs(List<ExternalReferenceInput> references, List<ExternalReferenceInput> expectedReferences)
        {
            if (references == null || references.Count != expectedReferences.Count)
                throw new Exception($"UI replacement external input package reference input count mismatch: {references?.Count ?? 0}->{expectedReferences.Count}");
            foreach (var expected in expectedReferences)
            {
                if (!references.Any(reference =>
                        reference.referenceKind == expected.referenceKind &&
                        reference.path == expected.path &&
                        reference.readiness == expected.readiness &&
                        reference.width == expected.width &&
                        reference.height == expected.height &&
                        reference.copyFileName == expected.copyFileName &&
                        reference.useCount == expected.useCount &&
                        reference.itemIndices == expected.itemIndices &&
                        reference.inputKinds == expected.inputKinds))
                    throw new Exception("UI replacement external input package reference input is missing: " + expected.path);
            }
        }

        static void RequireInputDerivedFields(ExternalInput input, List<Dictionary<string, string>> reuseRows)
        {
            Require(input.outputDirectory == DirectoryPath(input.outputPath), "outputDirectory: " + input.outputPath);
            Require(input.outputFileName == Path.GetFileName(input.outputPath), "outputFileName: " + input.outputPath);
            Require(input.outputWidth == OutputWidth(input), "outputWidth: " + input.outputPath);
            Require(input.outputHeight == OutputHeight(input), "outputHeight: " + input.outputPath);
            Require(input.actualWidth == ActualWidth(input), "actualWidth: " + input.outputPath);
            Require(input.actualHeight == ActualHeight(input), "actualHeight: " + input.outputPath);
            Require(input.sizeStatus == SizeStatus(input), "sizeStatus: " + input.outputPath);
            RequireOutputImageSize(input);
            Require(input.outputDirectoryReadiness == (Directory.Exists(input.outputDirectory) ? "Ready" : "Missing"), "outputDirectoryReadiness: " + input.outputPath);
            Require(input.referenceReadiness == ReferenceReadiness(input.referencePath), "referenceReadiness: " + input.outputPath);
            Require(input.referenceWidth == ReferenceValue(reuseRows, input.referencePath, "Width"), "referenceWidth: " + input.outputPath);
            Require(input.referenceHeight == ReferenceValue(reuseRows, input.referencePath, "Height"), "referenceHeight: " + input.outputPath);
            Require(input.referenceCopyFileName == InputReferenceCopyFileName(input.referencePath, input.itemIndices), "referenceCopyFileName: " + input.outputPath);
            Require(input.taskPrompt == TaskPrompt(input), "taskPrompt: " + input.outputPath);
            Require(input.acceptanceCheck == AcceptanceCheck(input), "acceptanceCheck: " + input.outputPath);
        }

        static string InputSummary(ExternalInput input)
        {
            return $"- `{input.outputPath}`：{input.readiness}，目录 {input.outputDirectoryReadiness}，输出尺寸 {OutputSize(input)}，实际尺寸 {ActualSize(input)}，尺寸状态 {input.sizeStatus}，参考 `{input.referencePath}`（{input.referenceReadiness}，{ReferenceSize(input)}，导入名 `{input.referenceCopyFileName}`），图集 `{input.targetAtlasPath}`，Item `{input.itemIndices}`，数量 {input.count}";
        }

        static string TaskSummary(ExternalInput input, UIAIToolsProfile profile, bool includePromptItem)
        {
            var promptItem = includePromptItem ? $"；单项 Prompt `{PromptItemPath(profile, input)}`" : "";
            return $"- [ ] {input.inputKind} `{input.outputPath}`：{input.taskPrompt}；输出尺寸 {OutputSize(input)}；实际尺寸 {ActualSize(input)}；尺寸状态 {input.sizeStatus}；目录 `{input.outputDirectory}`（{input.outputDirectoryReadiness}）；参考 `{input.referencePath}`（{input.referenceReadiness}，{ReferenceSize(input)}，导入名 `{input.referenceCopyFileName}`）；Item `{input.itemIndices}`；验收 `{input.acceptanceCheck}`{promptItem}；说明 `{input.note}`";
        }

        static string PromptItemListSummary(ExternalInput input, string itemPath)
        {
            return $"- [ ] `{itemPath}`：{input.inputKind} Item `{input.itemIndices}`，{input.readiness}，尺寸状态 {input.sizeStatus}，输出 `{input.outputPath}`";
        }

        static string PromptItemDirectoryIndexSummary(UIAIToolsProfile profile, IGrouping<string, ExternalInput> group)
        {
            var kinds = string.Join(";", group.Select(i => i.inputKind).Distinct().OrderBy(kind => kind));
            var prompts = string.Join(";", group.OrderBy(i => i.outputFileName).Select(i => PromptItemPath(profile, i)));
            return $"- `{group.Key}`：{group.Count()} 项，类型 `{kinds}`，单项 Prompt `{prompts}`";
        }

        static string ReferenceInputSummary(ExternalReferenceInput reference)
        {
            var itemText = string.IsNullOrEmpty(reference.itemIndices) ? "" : $"，Item `{reference.itemIndices}`";
            return $"- {reference.referenceKind} `{reference.path}`：{reference.readiness}，导入名 `{reference.copyFileName}`，尺寸 {ReferenceSize(reference.width, reference.height)}，使用 {reference.useCount} 项，类型 `{reference.inputKinds}`{itemText}";
        }

        static string ReferenceSourceDirectoryGroupSummary(IGrouping<string, ExternalReferenceInput> group)
        {
            var ordered = group.OrderBy(i => i.path).ToList();
            var kinds = string.Join(";", group.Select(i => i.referenceKind).Distinct().OrderBy(kind => kind));
            var files = string.Join(";", ordered.Select(i => Path.GetFileName(i.path)));
            var copyFiles = string.Join(";", ordered.Select(i => i.copyFileName));
            return $"- `{group.Key}`：{group.Count()} 项，类型 `{kinds}`，源文件 `{files}`，导入名 `{copyFiles}`";
        }

        static string ReferenceCopySummary(ExternalReferenceInput reference)
        {
            var itemText = string.IsNullOrEmpty(reference.itemIndices) ? "" : $"，Item `{reference.itemIndices}`";
            return $"- [ ] {reference.referenceKind} `{reference.path}` -> `{reference.copyFileName}`：{reference.readiness}，尺寸 {ReferenceSize(reference.width, reference.height)}，使用 {reference.useCount} 项，类型 `{reference.inputKinds}`{itemText}";
        }

        static string ReferenceImportStepSummary(ExternalReferenceInput reference)
        {
            var itemText = string.IsNullOrEmpty(reference.itemIndices) ? "" : $"，Item `{reference.itemIndices}`";
            return $"- [ ] 导入 {reference.referenceKind}：从 `{reference.path}` 读取为 `{reference.copyFileName}`；状态 {reference.readiness}；尺寸 {ReferenceSize(reference.width, reference.height)}；用于 `{reference.inputKinds}`{itemText}";
        }

        static string OutputDirectorySummary(ExternalOutputDirectory directory)
        {
            return $"- `{directory.path}`：{directory.readiness}，输入 {directory.inputCount}，未就绪 {directory.notReadyCount}，缺失 {directory.missingCount}，异常 {directory.invalidCount}，类型 `{directory.inputKinds}`";
        }

        static string DirectoryPreparationSummary(ExternalOutputDirectory directory)
        {
            return $"- `{directory.path}`：{directory.readiness}；外部生成前确认目录存在，包内不创建目录。";
        }

        static string PlacementDirectoryGroupSummary(IGrouping<string, ExternalInput> group)
        {
            var kinds = string.Join(";", group.Select(i => i.inputKind).Distinct().OrderBy(kind => kind));
            var files = string.Join(";", group.Select(i => i.outputFileName).OrderBy(name => name));
            return $"- `{group.Key}`：{group.Count()} 项，类型 `{kinds}`，文件 `{files}`";
        }

        static string ReferenceCopyFileName(string kind, string path, string itemIndices)
        {
            if (kind == "SourcePreview")
                return "source_preview" + ReferenceCopyExtension(path);
            var prefix = string.IsNullOrEmpty(itemIndices) ? "old" : "old_" + itemIndices.Replace(';', '_');
            return prefix + "_" + Path.GetFileName(path);
        }

        static string InputReferenceCopyFileName(string path, string itemIndices)
        {
            return string.IsNullOrEmpty(path) ? "" : ReferenceCopyFileName("OldAsset", path, itemIndices);
        }

        static string PromptItemPath(UIAIToolsProfile profile, ExternalInput input)
        {
            return UIReportFiles.GetPath(profile.logRoot, PromptItemFileName(input));
        }

        static string PromptItemFileName(ExternalInput input)
        {
            var item = string.IsNullOrEmpty(input.itemIndices) ? "root" : input.itemIndices.Replace(';', '_');
            return $"UIReplacementExternalPrompt_{input.inputKind}_{SafeReportName(item)}.md";
        }

        static string SafeReportName(string value)
        {
            var name = value;
            foreach (var c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name;
        }

        static void ApplyOutputSizes(ExternalInputPackage package)
        {
            var sourcePreviewSize = ImageSize(package.sourcePreviewPath);
            foreach (var input in package.inputs)
            {
                input.outputWidth = OutputWidth(input, sourcePreviewSize);
                input.outputHeight = OutputHeight(input, sourcePreviewSize);
                input.acceptanceCheck = AcceptanceCheck(input);
                input.sizeStatus = SizeStatus(input);
            }
        }

        static string OutputWidth(ExternalInput input)
        {
            return input.inputKind == "NewAsset" ? input.referenceWidth : input.outputWidth;
        }

        static string OutputHeight(ExternalInput input)
        {
            return input.inputKind == "NewAsset" ? input.referenceHeight : input.outputHeight;
        }

        static string OutputWidth(ExternalInput input, string[] sourcePreviewSize)
        {
            if (input.inputKind == "Preview")
                return sourcePreviewSize[0];
            if (input.inputKind == "NewAsset")
                return input.referenceWidth;
            return "";
        }

        static string OutputHeight(ExternalInput input, string[] sourcePreviewSize)
        {
            if (input.inputKind == "Preview")
                return sourcePreviewSize[1];
            if (input.inputKind == "NewAsset")
                return input.referenceHeight;
            return "";
        }

        static string OutputSize(ExternalInput input)
        {
            return ReferenceSize(input.outputWidth, input.outputHeight);
        }

        static string ActualWidth(ExternalInput input)
        {
            return ActualSizeValues(input)[0];
        }

        static string ActualHeight(ExternalInput input)
        {
            return ActualSizeValues(input)[1];
        }

        static string[] ActualSizeValues(ExternalInput input)
        {
            if (input.readiness != "Ready" || (input.inputKind != "Preview" && input.inputKind != "NewAsset"))
                return new[] { "", "" };
            return ImageSize(input.outputPath);
        }

        static string ActualSize(ExternalInput input)
        {
            return ReferenceSize(input.actualWidth, input.actualHeight);
        }

        static string SizeStatus(ExternalInput input)
        {
            if (input.inputKind == "TargetAtlas")
                return "NotApplicable";
            if (input.readiness != "Ready")
                return "Pending";
            if (OutputSize(input) == "Unknown" || ActualSize(input) == "Unknown")
                return "Unknown";
            return input.outputWidth == input.actualWidth && input.outputHeight == input.actualHeight ? "Match" : "Mismatch";
        }

        static void RequireOutputImageSize(ExternalInput input)
        {
            if (input.readiness != "Ready" || input.inputKind == "TargetAtlas" || OutputSize(input) == "Unknown")
                return;
            var size = ImageSize(input.outputPath);
            Require(size[0] == input.outputWidth && size[1] == input.outputHeight, "outputSize: " + input.outputPath);
        }

        static string AcceptanceCheck(ExternalInput input)
        {
            return AcceptanceCheck(input.inputKind, input.outputPath, input.outputWidth, input.outputHeight);
        }

        static string AcceptanceCheck(string inputKind, string outputPath, string outputWidth, string outputHeight)
        {
            if (inputKind == "TargetAtlas")
                return "目标 .spriteatlasv2 存在，路径保持 " + outputPath;
            var size = ReferenceSize(outputWidth, outputHeight);
            return size == "Unknown"
                ? "PNG 可解码，路径保持 " + outputPath
                : "PNG 可解码，尺寸 " + size + "，路径保持 " + outputPath;
        }

        static string ReferenceCopyExtension(string path)
        {
            var extension = Path.GetExtension(path);
            return string.IsNullOrEmpty(extension) ? ".png" : extension;
        }

        static string TaskPrompt(Dictionary<string, string> row, string referenceWidth, string referenceHeight)
        {
            if (row["InputKind"] == "Preview")
                return $"按 Brief 和旧版预览生成新版整体预览，输出到 {row["Path"]}";
            if (row["InputKind"] == "NewAsset")
                return $"参考 {row["ReferencePath"]} 生成替换 PNG，输出到 {row["Path"]}，参考尺寸 {ReferenceSize(referenceWidth, referenceHeight)}，目标图集 {row["TargetAtlas"]}";
            return $"确认或创建目标图集 {row["Path"]}，纳入 Item {row["ItemIndices"]} 的替换 PNG";
        }

        static string TaskPrompt(ExternalInput input)
        {
            if (input.inputKind == "Preview")
                return $"按 Brief 和旧版预览生成新版整体预览，输出到 {input.outputPath}";
            if (input.inputKind == "NewAsset")
                return $"参考 {input.referencePath} 生成替换 PNG，输出到 {input.outputPath}，参考尺寸 {ReferenceSize(input)}，目标图集 {input.targetAtlasPath}";
            return $"确认或创建目标图集 {input.outputPath}，纳入 Item {input.itemIndices} 的替换 PNG";
        }

        static string ReferenceReadiness(string path)
        {
            if (string.IsNullOrEmpty(path))
                return "None";
            return File.Exists(path) ? "Ready" : "Missing";
        }

        static string SourcePreviewReadiness(string path)
        {
            return File.Exists(path) ? "Ready" : "Missing";
        }

        static string ReferenceValue(List<Dictionary<string, string>> rows, string path, string column)
        {
            var row = rows.FirstOrDefault(r => r["Path"] == path);
            return row == null ? "" : row[column];
        }

        static string ReferenceSize(ExternalInput input)
        {
            return ReferenceSize(input.referenceWidth, input.referenceHeight);
        }

        static string ReferenceSize(string width, string height)
        {
            return string.IsNullOrEmpty(width) || string.IsNullOrEmpty(height) ? "Unknown" : $"{width}x{height}";
        }

        static string[] ImageSize(string path)
        {
            if (!File.Exists(path))
                return new[] { "", "" };
            var texture = new Texture2D(2, 2);
            texture.LoadImage(File.ReadAllBytes(path));
            var size = new[] { texture.width.ToString(), texture.height.ToString() };
            UnityEngine.Object.DestroyImmediate(texture);
            return size;
        }

        static string DirectoryPath(string path)
        {
            return Path.GetDirectoryName(path).Replace('\\', '/');
        }

        static string[] ManifestLines(UIAIToolsProfile profile, UIRedesignRequest request)
        {
            return File.ReadAllLines(UIReportFiles.GetPath(profile.logRoot, $"UIRedesignPackage_{SafeName(request.sourcePrefabPath)}.md"));
        }

        static string ManifestPath(string[] lines, string prefix)
        {
            var line = lines.First(l => l.StartsWith(prefix, StringComparison.Ordinal));
            var start = line.IndexOf('`');
            var end = line.IndexOf('`', start + 1);
            return line.Substring(start + 1, end - start - 1);
        }

        static string ManifestValue(string[] lines, string prefix)
        {
            var line = lines.First(l => l.StartsWith(prefix, StringComparison.Ordinal));
            return line.Substring(prefix.Length);
        }

        static string SafeName(string path)
        {
            var name = path.Replace("Assets/Bundle/Prefab/", "").Replace(".prefab", "").Replace('/', '_');
            foreach (var c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name;
        }

        static void Require(bool condition, string field)
        {
            if (!condition)
                throw new Exception("Invalid UI replacement external input package: " + field);
        }

        static void RequireLine(string[] lines, string line)
        {
            if (!lines.Contains(line))
                throw new Exception("UI replacement external input package summary is missing: " + line);
        }

        static void RequireSectionOrder(string report, string[] lines, params string[] sections)
        {
            UIReportMarkdown.RequireExactSectionOrder("UI replacement external report " + report, lines, sections);
        }

        static void RequireGeneratedPackageJson(string path, ExternalInputPackage expected)
        {
            var package = LoadPackageJson(path);
            Require(package.generatedAt == expected.generatedAt, "generatedAt");
            Require(package.sourcePrefabPath == expected.sourcePrefabPath, "sourcePrefabPath");
            Require(package.sourcePreviewPath == expected.sourcePreviewPath, "sourcePreviewPath");
            Require(package.stylePrompt == expected.stylePrompt, "stylePrompt");
            Require(package.outputFolder == expected.outputFolder, "outputFolder");
            Require(package.briefPath == expected.briefPath, "briefPath");
            Require(package.draftJsonPath == expected.draftJsonPath, "draftJsonPath");
            Require(package.pendingInputsCsvPath == expected.pendingInputsCsvPath, "pendingInputsCsvPath");
            Require(package.pendingInputReadinessCsvPath == expected.pendingInputReadinessCsvPath, "pendingInputReadinessCsvPath");
            Require(package.inputs.Count == expected.inputs.Count, "inputs");
            Require(package.outputDirectories.Count == expected.outputDirectories.Count, "outputDirectories");
            Require(package.referenceInputs.Count == expected.referenceInputs.Count, "referenceInputs");
        }

        internal static ExternalInputPackage LoadPackageJson(string path)
        {
            var json = File.ReadAllText(path);
            UICreationBriefTemplateService.ValidateRootJson(json, "replacement external input package", new[]
            {
                "generatedAt",
                "sourcePrefabPath",
                "sourcePreviewPath",
                "sourcePreviewReadiness",
                "stylePrompt",
                "outputFolder",
                "briefPath",
                "draftJsonPath",
                "pendingInputsCsvPath",
                "pendingInputReadinessCsvPath"
            }, new[] { "outputDirectories", "referenceInputs", "inputs" });
            UICreationBriefTemplateService.ValidateRootJsonObjectArrayItems(json, "replacement external input package", "outputDirectories", new[]
            {
                "path",
                "readiness",
                "inputKinds"
            }, new string[0], new[]
            {
                "inputCount",
                "notReadyCount",
                "missingCount",
                "invalidCount"
            });
            UICreationBriefTemplateService.ValidateRootJsonObjectArrayItems(json, "replacement external input package", "referenceInputs", new[]
            {
                "referenceKind",
                "path",
                "readiness",
                "width",
                "height",
                "copyFileName",
                "itemIndices",
                "inputKinds"
            }, new string[0], new[] { "useCount" });
            UICreationBriefTemplateService.ValidateRootJsonObjectArrayItems(json, "replacement external input package", "inputs", new[]
            {
                "inputKind",
                "pendingStatus",
                "readiness",
                "outputPath",
                "outputDirectory",
                "outputFileName",
                "outputWidth",
                "outputHeight",
                "actualWidth",
                "actualHeight",
                "sizeStatus",
                "outputDirectoryReadiness",
                "referencePath",
                "targetAtlasPath",
                "referenceReadiness",
                "referenceWidth",
                "referenceHeight",
                "referenceCopyFileName",
                "itemIndices",
                "sourceAction",
                "note",
                "taskPrompt",
                "acceptanceCheck"
            }, new string[0], new[] { "count" });
            var package = JsonUtility.FromJson<ExternalInputPackage>(json);
            if (package == null || package.inputs == null || package.outputDirectories == null || package.referenceInputs == null)
                throw new Exception("Invalid UI replacement external input package: " + path);
            ValidateNoDuplicatePackageRows(package);
            return package;
        }

        static void ValidateNoDuplicatePackageRows(ExternalInputPackage package)
        {
            var duplicateDirectory = package.outputDirectories.GroupBy(row => row.path).FirstOrDefault(group => group.Count() > 1);
            if (duplicateDirectory != null)
                throw new Exception("Invalid UI replacement external input package: duplicate output directory " + duplicateDirectory.Key);

            var duplicateReference = package.referenceInputs.GroupBy(row => new { row.referenceKind, row.path, row.itemIndices }).FirstOrDefault(group => group.Count() > 1);
            if (duplicateReference != null)
                throw new Exception($"Invalid UI replacement external input package: duplicate reference input {duplicateReference.Key.referenceKind} {duplicateReference.Key.path}");

            var duplicateInput = package.inputs.GroupBy(row => new { row.inputKind, row.outputPath, row.itemIndices, row.sourceAction }).FirstOrDefault(group => group.Count() > 1);
            if (duplicateInput != null)
                throw new Exception($"Invalid UI replacement external input package: duplicate input {duplicateInput.Key.inputKind} {duplicateInput.Key.outputPath}");
        }

        static void ValidateSourceReports(UIAIToolsProfile profile)
        {
            UIReplacementPendingInputReadinessService.Validate(profile);
            UIScanReportRows.ReadReuseIndex(profile);
        }

        static ExternalInputPackage ContractPackage()
        {
            return new ExternalInputPackage
            {
                generatedAt = "2026-04-29 00:00:00",
                sourcePrefabPath = "Assets/Bundle/Prefab/Demo.prefab",
                sourcePreviewPath = "",
                sourcePreviewReadiness = "Missing",
                stylePrompt = "",
                outputFolder = "Assets/Art/UI/AI/Demo",
                briefPath = "Logs/brief.md",
                draftJsonPath = "Logs/draft.json",
                pendingInputsCsvPath = "Logs/pending.csv",
                pendingInputReadinessCsvPath = "Logs/readiness.csv",
                outputDirectories = new List<ExternalOutputDirectory> { ContractOutputDirectory() },
                referenceInputs = new List<ExternalReferenceInput> { ContractReferenceInput() },
                inputs = new List<ExternalInput> { ContractInput() }
            };
        }

        static ExternalOutputDirectory ContractOutputDirectory()
        {
            return new ExternalOutputDirectory
            {
                path = "Assets/Art/UI/AI/Demo",
                readiness = "Missing",
                inputCount = 1,
                notReadyCount = 1,
                missingCount = 1,
                invalidCount = 0,
                inputKinds = "Preview"
            };
        }

        static ExternalReferenceInput ContractReferenceInput()
        {
            return new ExternalReferenceInput
            {
                referenceKind = "Preview",
                path = "Logs/old.png",
                readiness = "Missing",
                width = "",
                height = "",
                copyFileName = "preview.png",
                useCount = 1,
                itemIndices = "1",
                inputKinds = "Preview"
            };
        }

        static ExternalInput ContractInput()
        {
            return new ExternalInput
            {
                inputKind = "Preview",
                pendingStatus = "PendingPreview",
                readiness = "Missing",
                outputPath = "Assets/Art/UI/AI/Demo/preview.png",
                outputDirectory = "Assets/Art/UI/AI/Demo",
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
                count = 1,
                sourceAction = "Preview",
                note = "",
                taskPrompt = "Generate preview",
                acceptanceCheck = "PNG"
            };
        }

        static void WritePackageJson(string root, string fileName, ExternalInputPackage package)
        {
            var path = Path.Combine(root, fileName);
            File.WriteAllText(path, UICreationBriefTemplateService.ToJsonWithRootArrays(package, "outputDirectories", "referenceInputs", "inputs"), new UTF8Encoding(true));
        }

        static void ExpectFailure(string root, string fileName, ExternalInputPackage package, string expectedMessage)
        {
            var path = Path.Combine(root, fileName);
            WritePackageJson(root, fileName, package);
            try
            {
                LoadPackageJson(path);
            }
            catch (Exception exception)
            {
                if (exception.Message.Contains(expectedMessage))
                    return;
                throw new Exception($"Unexpected UI replacement external input package contract failure for {fileName}: {exception.Message}");
            }
            throw new Exception("UI replacement external input package contract sample did not fail: " + fileName);
        }

        static void RequireSummarySections(string[] lines)
        {
            RequireSectionOrder("external input package summary", lines, "## 总览", "## 尺寸状态分布", "## 输出目录", "## 目录准备", "## 待落位目录分组", "## 参考素材", "## 参考输入", "## 单项 Prompt 文件", "## 外部产物落位后复跑", "## 新版预览", "## 新图", "## 目标图集");
        }

        static void RequireTaskSections(string[] lines)
        {
            RequireSectionOrder("external generation tasks", lines, "## 总览", "## 尺寸状态分布", "## 输出目录", "## 目录准备", "## 待落位目录分组", "## 参考输入", "## 外部产物落位后复跑", "## 未就绪任务", "## 已就绪输入");
        }

        static void RequirePromptPackSections(string[] lines)
        {
            RequireSectionOrder("external prompt pack", lines, "## 全局上下文", "## 全局约束", "## 尺寸状态分布", "## 输出目录", "## 目录准备", "## 参考输入", "## Prompt Blocks");
        }

        static void RequireReferenceCopyListSections(string[] lines)
        {
            RequireSectionOrder("external reference copy list", lines, "## 总览", "## 导入步骤", "## 引用来源目录分组", "## 复制清单");
        }

        static void RequirePromptItemListSections(string[] lines)
        {
            RequireSectionOrder("external prompt item list", lines, "## 总览", "## 目录索引", "## 文件清单");
        }

        static void RequirePromptItemSections(string report, string[] lines)
        {
            RequireSectionOrder(report, lines, "## 全局上下文", "## 任务", "## 外部产物验收", "## 外部产物落位后复跑");
        }

        [Serializable]
        public class ExternalInputPackage
        {
            public string generatedAt;
            public string sourcePrefabPath;
            public string sourcePreviewPath;
            public string sourcePreviewReadiness;
            public string stylePrompt;
            public string outputFolder;
            public string briefPath;
            public string draftJsonPath;
            public string pendingInputsCsvPath;
            public string pendingInputReadinessCsvPath;
            public List<ExternalOutputDirectory> outputDirectories = new List<ExternalOutputDirectory>();
            public List<ExternalReferenceInput> referenceInputs = new List<ExternalReferenceInput>();
            public List<ExternalInput> inputs = new List<ExternalInput>();
        }

        [Serializable]
        public class ExternalOutputDirectory
        {
            public string path;
            public string readiness;
            public int inputCount;
            public int notReadyCount;
            public int missingCount;
            public int invalidCount;
            public string inputKinds;
        }

        [Serializable]
        public class ExternalReferenceInput
        {
            public string referenceKind;
            public string path;
            public string readiness;
            public string width;
            public string height;
            public string copyFileName;
            public int useCount;
            public string itemIndices;
            public string inputKinds;
        }

        [Serializable]
        public class ExternalInput
        {
            public string inputKind;
            public string pendingStatus;
            public string readiness;
            public string outputPath;
            public string outputDirectory;
            public string outputFileName;
            public string outputWidth;
            public string outputHeight;
            public string actualWidth;
            public string actualHeight;
            public string sizeStatus;
            public string outputDirectoryReadiness;
            public string referencePath;
            public string targetAtlasPath;
            public string referenceReadiness;
            public string referenceWidth;
            public string referenceHeight;
            public string referenceCopyFileName;
            public string itemIndices;
            public int count;
            public string sourceAction;
            public string note;
            public string taskPrompt;
            public string acceptanceCheck;
        }
    }
}

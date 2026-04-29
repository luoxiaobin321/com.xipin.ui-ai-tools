using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Xipin.UIAITools
{
    public static class UICreationLayoutDryRunService
    {
        static readonly HashSet<string> AnchorNames = new HashSet<string>
        {
            "top_left",
            "top_center",
            "top_right",
            "middle_left",
            "middle_center",
            "middle_right",
            "bottom_left",
            "bottom_center",
            "bottom_right",
            "stretch_full",
            "stretch_horizontal_top",
            "stretch_horizontal_middle",
            "stretch_horizontal_bottom",
            "stretch_vertical_left",
            "stretch_vertical_center",
            "stretch_vertical_right"
        };

        static readonly HashSet<string> BuiltInRoles = new HashSet<string>
        {
            "Button",
            "Text",
            "Image",
            "DynamicImage",
            "RawImage",
            "Scroll",
            "Panel",
            "EmptyImage",
            "FunctionalEmptyImage",
            "Graphic"
        };

        static readonly HashSet<string> AssetNeedKinds = new HashSet<string>
        {
            "DataBinding",
            "NewImage",
            "ReuseImage",
            "Font",
            "Effect",
            "ComponentPrefab",
            "ScriptBinding"
        };

        static readonly HashSet<string> AssetNeedStatuses = new HashSet<string>
        {
            "Ready",
            "Missing",
            "NeedsReview"
        };
        static readonly HashSet<string> AllowedSeverities = new HashSet<string> { "Info", "Warning", "Error" };
        static readonly HashSet<string> AllowedStatuses = new HashSet<string> { "OK", "Missing", "NeedsReview", "Exists", "Duplicate", "Unknown", "Invalid", "Ready" };

        public static string Run(UIAIToolsProfile profile, string layoutDraftJsonPath)
        {
            UIComponentCandidateIndexService.Validate(profile);
            var draft = UILayoutDraftTemplateService.LoadDraft(layoutDraftJsonPath);
            var components = UIComponentCandidateIndexService.ReadIndexRows(profile).ToDictionary(r => r["ComponentId"]);
            var roles = new HashSet<string>(BuiltInRoles.Concat(components.Values.Select(r => r["Role"])));
            var lines = new List<string> { UIReportFiles.CreationLayoutDryRunHeader };
            var index = 1;
            AddTargetFolderCheck(lines, ref index, draft);
            AddReferenceResolutionCheck(lines, ref index, draft);
            AddTargetPrefabCheck(lines, ref index, draft);
            AddLine(lines, index++, "RequiresConfirmation", draft.requiresConfirmation ? "Info" : "Error", draft.requiresConfirmation ? "OK" : "Missing", draft.requiresConfirmation ? "需要人工确认" : "requiresConfirmation 必须为 true", "");
            AddLine(lines, index++, "LayoutNodes", draft.nodes.Count == 0 ? "Error" : "Info", draft.nodes.Count == 0 ? "Missing" : "OK", draft.nodes.Count == 0 ? "布局节点为空" : "布局节点已提供", draft.nodes.Count.ToString());
            AddLine(lines, index++, "Interactions", draft.interactions.Count == 0 ? "Warning" : "Info", draft.interactions.Count == 0 ? "NeedsReview" : "OK", draft.interactions.Count == 0 ? "交互为空，需人工确认" : "交互已提供", Join(draft.interactions));

            AddNodeTreeChecks(lines, ref index, draft.nodes);
            foreach (var node in draft.nodes)
                AddNodeChecks(lines, ref index, node, components, roles);
            AddAssetNeedIdentityChecks(lines, ref index, draft.assets);
            AddDataBindingCoverage(lines, ref index, draft);
            foreach (var need in draft.assets)
                AddAssetNeedCheck(lines, ref index, need);

            var path = UIReportFiles.GetPath(profile.logRoot, UIReportFiles.CreationLayoutDryRun);
            File.WriteAllLines(path, lines, new UTF8Encoding(true));
            ReadRows(profile);
            var summaryPath = GenerateSummary(profile);
            Debug.Log($"UI creation layout dry-run generated: {path}, summary: {summaryPath}");
            return path;
        }

        public static List<Dictionary<string, string>> ReadRows(UIAIToolsProfile profile)
        {
            UIReportValidationService.ValidateReport(profile, UIReportFiles.CreationLayoutDryRun, UIReportFiles.CreationLayoutDryRunHeader);
            var rows = UIReportCsv.ReadRows(profile.logRoot, UIReportFiles.CreationLayoutDryRun);
            if (rows.Count == 0)
                throw new Exception("Invalid UI creation layout dry-run: rows are required");
            foreach (var row in rows)
                ValidateRow(row);
            ValidateNoDuplicateRows(rows);
            return rows;
        }

        public static void ValidateContract()
        {
            var root = Path.Combine(Path.GetTempPath(), "UIAIToolsCreationLayoutDryRunContract_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                var profile = ScriptableObject.CreateInstance<UIAIToolsProfile>();
                profile.logRoot = root;
                WriteCsv(profile, new[]
                {
                    Row("1", "TargetFolder", "Info", "OK", "目标目录格式合法", "Assets/Art/UI/AI/Demo"),
                    Row("2", "TargetPrefab", "Info", "OK", "目标 prefab 不存在", "Assets/Art/UI/AI/Demo/Demo.prefab"),
                    Row("3", "NodeAssetPath", "Info", "OK", "节点资源路径合法", "Assets/Art/UI/Icon.png"),
                    Row("4", "AssetNeedPath", "Info", "OK", "资源需求路径合法", "Assets/Art/UI/Need.png")
                });
                ReadRows(profile);
                ExpectRowsFailure(profile, "duplicate_dry_run_row", new[]
                {
                    Row("1", "TargetFolder", "Info", "OK", "目标目录格式合法", "Assets/Art/UI/AI/Demo"),
                    Row("1", "TargetFolder", "Info", "OK", "目标目录格式合法", "Assets/Art/UI/AI/Demo")
                }, "duplicate dry-run row");
                ExpectRowsFailure(profile, "bad_target_folder_path", new[]
                {
                    Row("1", "TargetFolder", "Info", "OK", "目标目录格式合法", "Generated/Demo")
                }, "TargetFolder path is invalid");
                ExpectRowsFailure(profile, "bad_target_prefab_path", new[]
                {
                    Row("1", "TargetPrefab", "Info", "OK", "目标 prefab 不存在", "Generated/Demo.prefab")
                }, "TargetPrefab path is invalid");
                ExpectRowsFailure(profile, "bad_target_prefab_extension", new[]
                {
                    Row("1", "TargetPrefab", "Info", "OK", "目标 prefab 不存在", "Assets/Art/UI/AI/Demo/Demo.png")
                }, "TargetPrefab must be .prefab");
                ExpectRowsFailure(profile, "bad_node_asset_path", new[]
                {
                    Row("1", "NodeAssetPath", "Info", "OK", "节点资源路径合法", "Art/UI/Icon.png")
                }, "NodeAssetPath path is invalid");
                ExpectRowsFailure(profile, "bad_asset_need_path", new[]
                {
                    Row("1", "AssetNeedPath", "Info", "OK", "资源需求路径合法", "Assets/Art/UI/../Need.png")
                }, "AssetNeedPath path is invalid");
            }
            finally
            {
                if (Directory.Exists(root))
                    Directory.Delete(root, true);
            }
            Debug.Log("UI creation layout dry-run contract validation passed.");
        }

        public static void ValidateNoErrors(UIAIToolsProfile profile)
        {
            var rows = ReadRows(profile);
            ValidateSummary(profile);
            var errors = rows.Count(r => r["Severity"] == "Error");
            if (errors > 0)
                throw new Exception($"UI creation layout dry-run has blocking errors: {errors}");
            Debug.Log("UI creation layout dry-run validation passed.");
        }

        static void ValidateRow(Dictionary<string, string> row)
        {
            if (!int.TryParse(row["ItemIndex"], out _))
                throw new Exception("Invalid UI creation layout dry-run: ItemIndex must be an integer");
            if (string.IsNullOrEmpty(row["Check"]))
                throw new Exception("Invalid UI creation layout dry-run: Check is required");
            if (!AllowedSeverities.Contains(row["Severity"]))
                throw new Exception("Invalid UI creation layout dry-run: invalid severity " + row["Severity"]);
            if (!AllowedStatuses.Contains(row["Status"]))
                throw new Exception("Invalid UI creation layout dry-run: invalid status " + row["Status"]);
            if (string.IsNullOrEmpty(row["Message"]))
                throw new Exception("Invalid UI creation layout dry-run: Message is required");
            ValidateEvidence(row);
        }

        static void ValidateEvidence(Dictionary<string, string> row)
        {
            var status = row["Status"];
            var evidence = row["Evidence"];
            if (row["Check"] == "TargetFolder" && status == "OK")
                RequireAssetPath(evidence, "TargetFolder");
            else if (row["Check"] == "TargetPrefab" && (status == "OK" || status == "Exists"))
                RequireTargetPrefabPath(evidence);
            else if ((row["Check"] == "NodeAssetPath" || row["Check"] == "AssetNeedPath") && status == "OK")
                RequireAssetPath(evidence, row["Check"]);
        }

        static void RequireTargetPrefabPath(string path)
        {
            RequireAssetPath(path, "TargetPrefab");
            if (!path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                throw new Exception("Invalid UI creation layout dry-run: TargetPrefab must be .prefab");
        }

        static void RequireAssetPath(string path, string label)
        {
            if (!ValidAssetPath(path))
                throw new Exception($"Invalid UI creation layout dry-run: {label} path is invalid");
        }

        static void ValidateNoDuplicateRows(List<Dictionary<string, string>> rows)
        {
            var duplicate = rows.GroupBy(row => new
            {
                ItemIndex = row["ItemIndex"],
                Check = row["Check"],
                Evidence = row["Evidence"]
            }).FirstOrDefault(group => group.Count() > 1);
            if (duplicate != null)
                throw new Exception($"Invalid UI creation layout dry-run: duplicate dry-run row for Item {duplicate.Key.ItemIndex} / {duplicate.Key.Check}");
        }

        static void AddNodeTreeChecks(List<string> lines, ref int index, List<UILayoutNode> nodes)
        {
            foreach (var group in nodes.Where(n => !string.IsNullOrEmpty(n.nodeId)).GroupBy(n => n.nodeId).Where(g => g.Count() > 1))
                AddLine(lines, index++, "DuplicateNodeId", "Error", "Duplicate", "节点 ID 重复", group.Key);

            foreach (var group in nodes.Where(n => !string.IsNullOrEmpty(n.name)).GroupBy(n => $"{n.parentId}/{n.name}").Where(g => g.Count() > 1))
                AddLine(lines, index++, "DuplicateNodeName", "Error", "Duplicate", "同父节点下节点名重复", group.Key);

            var nodeIds = new HashSet<string>(nodes.Where(n => !string.IsNullOrEmpty(n.nodeId)).Select(n => n.nodeId));
            foreach (var node in nodes.Where(n => !string.IsNullOrEmpty(n.parentId) && !nodeIds.Contains(n.parentId)))
                AddLine(lines, index++, "MissingParent", "Error", "Missing", "父节点不存在", $"{node.nodeId}->{node.parentId}");
        }

        static void AddTargetFolderCheck(List<string> lines, ref int index, UILayoutDraft draft)
        {
            if (string.IsNullOrEmpty(draft.root.targetFolder))
            {
                AddLine(lines, index++, "TargetFolder", "Error", "Missing", "目标目录为空", draft.root.name);
                return;
            }

            var ok = ValidAssetPath(draft.root.targetFolder);
            AddLine(lines, index++, "TargetFolder", ok ? "Info" : "Error", ok ? "OK" : "Invalid", ok ? "目标目录格式合法" : "目标目录必须是 Assets/... 且不能包含 ..", draft.root.targetFolder);
        }

        static void AddReferenceResolutionCheck(List<string> lines, ref int index, UILayoutDraft draft)
        {
            if (string.IsNullOrEmpty(draft.root.referenceResolution))
            {
                AddLine(lines, index++, "ReferenceResolution", "Error", "Missing", "参考分辨率为空", draft.root.name);
                return;
            }

            var ok = ValidSize(draft.root.referenceResolution);
            AddLine(lines, index++, "ReferenceResolution", ok ? "Info" : "Error", ok ? "OK" : "Invalid", ok ? "参考分辨率格式合法" : "参考分辨率必须是正数 宽x高", draft.root.referenceResolution);
        }

        static void AddTargetPrefabCheck(List<string> lines, ref int index, UILayoutDraft draft)
        {
            var targetFolder = draft.root.targetFolder ?? "";
            var prefabPath = $"{targetFolder.TrimEnd('/')}/{draft.root.name}.prefab";
            var exists = File.Exists(prefabPath);
            AddLine(lines, index++, "TargetPrefab", exists ? "Error" : "Info", exists ? "Exists" : "OK", exists ? "目标 prefab 已存在，需进入宿主覆盖流程" : "目标 prefab 不存在", prefabPath);
        }

        static void AddNodeChecks(List<string> lines, ref int index, UILayoutNode node, Dictionary<string, Dictionary<string, string>> components, HashSet<string> roles)
        {
            AddLine(lines, index++, "NodeId", string.IsNullOrEmpty(node.nodeId) ? "Error" : "Info", string.IsNullOrEmpty(node.nodeId) ? "Missing" : "OK", string.IsNullOrEmpty(node.nodeId) ? "节点 ID 为空" : "节点 ID 已提供", node.name);
            AddLine(lines, index++, "NodeName", string.IsNullOrEmpty(node.name) ? "Error" : "Info", string.IsNullOrEmpty(node.name) ? "Missing" : "OK", string.IsNullOrEmpty(node.name) ? "节点名为空" : "节点名已提供", node.nodeId);
            AddRoleCheck(lines, ref index, node, roles);
            AddComponentCheck(lines, ref index, node, components);
            AddAnchorCheck(lines, ref index, node);
            AddPositionCheck(lines, ref index, node);
            AddSizeCheck(lines, ref index, node);
            AddStateCheck(lines, ref index, node);
            AddTextCheck(lines, ref index, node);
            AddAssetPathCheck(lines, ref index, node);
        }

        static void AddRoleCheck(List<string> lines, ref int index, UILayoutNode node, HashSet<string> roles)
        {
            if (string.IsNullOrEmpty(node.componentRole))
            {
                AddLine(lines, index++, "NodeComponentRole", "Error", "Missing", "组件角色为空", node.name);
                return;
            }

            var known = roles.Contains(node.componentRole);
            AddLine(lines, index++, "NodeComponentRole", known ? "Info" : "Error", known ? "OK" : "Unknown", known ? "组件角色已识别" : "组件角色不在候选索引或内置角色中", $"{node.name} {node.componentRole}");
        }

        static void AddComponentCheck(List<string> lines, ref int index, UILayoutNode node, Dictionary<string, Dictionary<string, string>> components)
        {
            if (string.IsNullOrEmpty(node.componentId))
            {
                AddLine(lines, index++, "NodeComponentId", "Error", "Missing", "组件候选 ID 为空", node.name);
                return;
            }

            if (!components.ContainsKey(node.componentId))
            {
                AddLine(lines, index++, "NodeComponentId", "Error", "Unknown", "组件候选 ID 不在索引中", node.componentId);
                return;
            }

            var role = components[node.componentId]["Role"];
            AddLine(lines, index++, "NodeComponentId", "Info", "OK", "组件候选 ID 已在索引中", $"{node.componentId} {role}");
            if (!string.IsNullOrEmpty(node.componentRole) && role != node.componentRole)
                AddLine(lines, index++, "NodeComponentRoleMatch", "Warning", "NeedsReview", "节点角色与候选角色不一致", $"{node.componentRole}->{role} {node.componentId}");
        }

        static void AddAnchorCheck(List<string> lines, ref int index, UILayoutNode node)
        {
            if (string.IsNullOrEmpty(node.anchor))
            {
                AddLine(lines, index++, "NodeAnchor", "Error", "Missing", "锚点为空", node.name);
                return;
            }

            var ok = ValidAnchor(node.anchor);
            AddLine(lines, index++, "NodeAnchor", ok ? "Info" : "Error", ok ? "OK" : "Invalid", ok ? "锚点格式合法" : "锚点必须是预设方位或 minX,minY|maxX,maxY", $"{node.name} {node.anchor}");
        }

        static void AddPositionCheck(List<string> lines, ref int index, UILayoutNode node)
        {
            if (string.IsNullOrEmpty(node.position))
            {
                AddLine(lines, index++, "NodePosition", "Error", "Missing", "位置为空", node.name);
                return;
            }

            var ok = ValidPosition(node.position);
            AddLine(lines, index++, "NodePosition", ok ? "Info" : "Error", ok ? "OK" : "Invalid", ok ? "位置格式合法" : "位置必须是 x,y 数字格式", $"{node.name} {node.position}");
        }

        static void AddSizeCheck(List<string> lines, ref int index, UILayoutNode node)
        {
            if (string.IsNullOrEmpty(node.size))
            {
                AddLine(lines, index++, "NodeSize", "Error", "Missing", "尺寸为空", node.name);
                return;
            }

            var ok = ValidSize(node.size);
            AddLine(lines, index++, "NodeSize", ok ? "Info" : "Error", ok ? "OK" : "Invalid", ok ? "尺寸格式合法" : "尺寸必须是正数 宽x高", $"{node.name} {node.size}");
        }

        static void AddStateCheck(List<string> lines, ref int index, UILayoutNode node)
        {
            if (string.IsNullOrEmpty(node.state))
            {
                AddLine(lines, index++, "NodeState", "Warning", "NeedsReview", "组件状态未指定，需宿主确认默认状态", node.name);
                return;
            }

            var ok = node.state == "normal" || node.state == "disabled" || node.state == "selected" || node.state == "pressed" || node.state == "hidden";
            AddLine(lines, index++, "NodeState", ok ? "Info" : "Error", ok ? "OK" : "Invalid", ok ? "组件状态合法" : "组件状态必须是 normal/disabled/selected/pressed/hidden", $"{node.name} {node.state}");
            if (ok && !string.IsNullOrEmpty(node.componentRole) && !StateFitsRole(node.componentRole, node.state))
                AddLine(lines, index++, "NodeStateRoleCompatibility", "Warning", "NeedsReview", "组件状态与组件角色不常见，需宿主确认", $"{node.name} {node.componentRole} {node.state}");
        }

        static void AddTextCheck(List<string> lines, ref int index, UILayoutNode node)
        {
            if (node.componentRole != "Text")
                return;

            var hasText = !string.IsNullOrEmpty(node.text);
            var hasBinding = !string.IsNullOrEmpty(node.dataBinding);
            AddLine(lines, index++, "TextSource", hasText || hasBinding ? "Info" : "Error", hasText || hasBinding ? "OK" : "Missing", hasText || hasBinding ? "文本来源已提供" : "文本或数据绑定为空", node.name);
            if (hasText && node.text.Length > 40)
                AddLine(lines, index++, "TextLength", "Warning", "NeedsReview", "文本超过 40 字，需复核多语言长度", node.name);
        }

        static void AddAssetPathCheck(List<string> lines, ref int index, UILayoutNode node)
        {
            if (string.IsNullOrEmpty(node.assetPath))
                return;

            var ok = ValidAssetPath(node.assetPath);
            AddLine(lines, index++, "NodeAssetPath", ok ? "Info" : "Error", ok ? "OK" : "Invalid", ok ? "节点资源路径合法" : "节点资源路径必须是 Assets/... 且不能包含 ..", node.assetPath);
        }

        static void AddDataBindingCoverage(List<string> lines, ref int index, UILayoutDraft draft)
        {
            var nodeBindings = new HashSet<string>(draft.nodes.Where(n => !string.IsNullOrEmpty(n.dataBinding)).Select(n => n.dataBinding));
            var assetBindings = new HashSet<string>(draft.assets.Where(a => a.kind == "DataBinding" && !string.IsNullOrEmpty(a.reason)).Select(a => a.reason));
            foreach (var binding in nodeBindings.Where(b => !assetBindings.Contains(b)))
                AddLine(lines, index++, "DataBindingDeclaration", "Error", "Missing", "节点数据绑定未在资源需求中声明", binding);

            foreach (var need in draft.assets.Where(a => a.kind == "DataBinding"))
            {
                var covered = nodeBindings.Contains(need.reason);
                AddLine(lines, index++, "DataBindingCoverage", covered ? "Info" : "Warning", covered ? "OK" : "NeedsReview", covered ? "数据绑定已有节点引用" : "数据绑定未被节点引用，需宿主确认绑定位置", need.reason);
            }
        }

        static void AddAssetNeedIdentityChecks(List<string> lines, ref int index, List<UICreationAssetNeed> needs)
        {
            foreach (var group in needs.Where(n => !string.IsNullOrEmpty(n.needId)).GroupBy(n => n.needId).Where(g => g.Count() > 1))
                AddLine(lines, index++, "DuplicateAssetNeedId", "Error", "Duplicate", "资源需求 ID 重复", group.Key);
        }

        static void AddAssetNeedCheck(List<string> lines, ref int index, UICreationAssetNeed need)
        {
            var hasKind = !string.IsNullOrEmpty(need.kind);
            var knownKind = hasKind && AssetNeedKinds.Contains(need.kind);
            var hasStatus = !string.IsNullOrEmpty(need.status);
            var knownStatus = hasStatus && AssetNeedStatuses.Contains(need.status);
            var status = hasStatus ? need.status : "Missing";
            AddLine(lines, index++, "AssetNeedId", string.IsNullOrEmpty(need.needId) ? "Error" : "Info", string.IsNullOrEmpty(need.needId) ? "Missing" : "OK", string.IsNullOrEmpty(need.needId) ? "资源需求 ID 为空" : "资源需求 ID 已提供", need.reason);
            AddLine(lines, index++, "AssetNeedKind", knownKind ? "Info" : "Error", hasKind ? (knownKind ? "OK" : "Invalid") : "Missing", knownKind ? "资源需求类型合法" : "资源需求类型必须是 DataBinding/NewImage/ReuseImage/Font/Effect/ComponentPrefab/ScriptBinding", $"{need.needId} {need.kind}");
            AddLine(lines, index++, "AssetNeedSource", string.IsNullOrEmpty(need.source) ? "Error" : "Info", string.IsNullOrEmpty(need.source) ? "Missing" : "OK", string.IsNullOrEmpty(need.source) ? "资源来源为空" : "资源来源已提供", need.needId);
            AddAssetNeedPathCheck(lines, ref index, need);
            var ready = status == "Ready";
            AddLine(lines, index++, "AssetNeed", ready ? "Info" : "Error", knownStatus ? status : "Invalid", ready ? "资源需求已就绪" : knownStatus ? "资源需求未就绪" : "资源需求状态必须是 Ready/Missing/NeedsReview", $"{need.needId} {need.kind} {need.reason}");
        }

        static void AddAssetNeedPathCheck(List<string> lines, ref int index, UICreationAssetNeed need)
        {
            var requiresPath = need.status == "Ready" && need.kind != "DataBinding";
            if (string.IsNullOrEmpty(need.path))
            {
                if (requiresPath)
                    AddLine(lines, index++, "AssetNeedPath", "Error", "Missing", "Ready 的非 DataBinding 资源需求必须提供路径", need.needId);
                return;
            }

            var ok = ValidAssetPath(need.path);
            AddLine(lines, index++, "AssetNeedPath", ok ? "Info" : "Error", ok ? "OK" : "Invalid", ok ? "资源需求路径合法" : "资源需求路径必须是 Assets/... 且不能包含 ..", need.path);
        }

        static string GenerateSummary(UIAIToolsProfile profile)
        {
            var rows = ReadRows(profile);
            var path = UIReportFiles.GetPath(profile.logRoot, UIReportFiles.CreationLayoutDryRunSummary);
            var errors = rows.Count(r => r["Severity"] == "Error");
            var lines = new List<string>
            {
                "# UI 生成前布局 dry-run",
                "",
                $"生成时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                "",
                "本文件只检查布局草稿是否可进入 prefab 生成，不创建 prefab、不复制图片、不修改图集。",
                "",
                $"Gate：{(errors == 0 ? "Passed" : "Blocked")}",
                "",
                "## Severity 分布"
            };
            foreach (var group in rows.GroupBy(r => r["Severity"]).OrderBy(g => g.Key))
                lines.Add($"- {group.Key}：{group.Count()}");
            lines.Add("");
            lines.Add("## Status 分布");
            foreach (var group in rows.GroupBy(r => r["Status"]).OrderBy(g => g.Key))
                lines.Add($"- {group.Key}：{group.Count()}");
            lines.Add("");
            lines.Add("## 阻断项");
            foreach (var row in rows.Where(r => r["Severity"] == "Error").Take(30))
                lines.Add($"- {row["Check"]} / {row["Status"]}：{row["Message"]}，`{row["Evidence"]}`");
            File.WriteAllLines(path, lines, new UTF8Encoding(true));
            ValidateSummarySections(lines.ToArray());
            return path;
        }

        static void ValidateSummary(UIAIToolsProfile profile)
        {
            var path = UIReportFiles.GetPath(profile.logRoot, UIReportFiles.CreationLayoutDryRunSummary);
            if (!File.Exists(path))
                throw new Exception("Missing UI creation layout dry-run summary: " + path);
            ValidateSummarySections(File.ReadAllLines(path));
        }

        static void ValidateSummarySections(string[] lines)
        {
            UIReportMarkdown.RequireExactSectionOrder("UI creation layout dry-run summary", lines, "## Severity 分布", "## Status 分布", "## 阻断项");
        }

        static void AddLine(List<string> lines, int index, string check, string severity, string status, string message, string evidence)
        {
            lines.Add(string.Join(",", new[]
            {
                index.ToString(),
                Csv(check),
                Csv(severity),
                Csv(status),
                Csv(message),
                Csv(evidence)
            }));
        }

        static string Join(List<string> values)
        {
            return values.Count == 0 ? "" : string.Join(";", values);
        }

        static bool ValidSize(string value)
        {
            var parts = value.ToLowerInvariant().Split('x');
            return parts.Length == 2 && TryFloat(parts[0], out var width) && TryFloat(parts[1], out var height) && width > 0 && height > 0;
        }

        static bool ValidAnchor(string value)
        {
            if (AnchorNames.Contains(value))
                return true;

            var parts = value.Split('|');
            return parts.Length == 2 &&
                TryPair(parts[0], out var minX, out var minY) &&
                TryPair(parts[1], out var maxX, out var maxY) &&
                minX >= 0f && minY >= 0f && maxX <= 1f && maxY <= 1f && minX <= maxX && minY <= maxY;
        }

        static bool ValidPosition(string value)
        {
            return TryPair(value, out _, out _);
        }

        static bool TryPair(string value, out float x, out float y)
        {
            x = 0f;
            y = 0f;
            var parts = value.Split(',');
            if (parts.Length != 2)
                return false;

            var xOk = TryFloat(parts[0], out x);
            var yOk = TryFloat(parts[1], out y);
            return xOk && yOk;
        }

        static bool TryFloat(string value, out float result)
        {
            return float.TryParse(value.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out result);
        }

        static bool ValidAssetPath(string value)
        {
            return value.StartsWith("Assets/", StringComparison.Ordinal) && !value.Contains("\\") && !value.Contains("/../") && !value.EndsWith("/..", StringComparison.Ordinal);
        }

        static bool StateFitsRole(string role, string state)
        {
            return state == "normal" || state == "hidden" || role == "Button";
        }

        static string Csv(string value)
        {
            value = value ?? "";
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        static void WriteCsv(UIAIToolsProfile profile, IEnumerable<string> rows)
        {
            var path = UIReportFiles.GetPath(profile.logRoot, UIReportFiles.CreationLayoutDryRun);
            File.WriteAllLines(path, new[] { UIReportFiles.CreationLayoutDryRunHeader }.Concat(rows), new UTF8Encoding(true));
        }

        static string Row(string itemIndex, string check, string severity, string status, string message, string evidence)
        {
            return string.Join(",", new[] { itemIndex, check, severity, status, message, evidence }.Select(Csv));
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
                throw new Exception($"Unexpected UI creation layout dry-run contract failure for {name}: {exception.Message}");
            }
            throw new Exception("UI creation layout dry-run contract sample did not fail: " + name);
        }
    }
}

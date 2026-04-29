# Current Goal
把 `com.xipin.ui-ai-tools` 做成可审计的 Unity UI 自动化工具包，当前优先稳定改版和新 UI 生成链路的 gate 与宿主结果契约。

# Status
替换链路的 manifest、dry-run、pending input、external input package、execution plan、plan status、core scan rows、scan summary、reuse search result、component candidate、host apply checklist 和 host apply result 已串起前置复验；dry-run、pending input、readiness、external input package、execution plan、core scan rows、reuse search result 和 component candidate 都会拒绝重复逻辑行，reuse search result 还会复验查询图后缀、候选图片路径、图集路径和 prefab 引用路径，component candidate 还会复验样例 prefab/node 路径、确认清单 `ComponentPrefabPath` 和 `PreviewPath`，pending input/readiness contract 还覆盖路径、后缀、数量、item index 和 readiness 实际尺寸边界，plan status 有独立阻断状态、汇总顺序和 severity 顺序 contract，scan summary 和 panel focus 有独立 Markdown section 顺序 contract，external input package 有独立重复输出目录/参考输入/输入项 contract，execution plan 也有独立重复计划行和资源路径 contract，host apply checklist 有独立 gate 行契约，host apply result 要求所有可产生结果的 action 都有结果行、资源路径后缀有效，并拒绝同一计划行重复上报。Profile contract 覆盖默认路径、文本扫描根和地图排除开关；Control catalog contract 覆盖默认角色映射、大小写不敏感匹配和基础负例。核心报告注册表 contract 覆盖 CoreReports 不重复、表头字典无陈旧项、每个核心 CSV 都有表头、表头列不为空且不重复，并锁定 `UIReportFiles.GetPath` 的 `/` 输出。Redesign brief contract 覆盖 Markdown section 顺序和 duplicate old/new asset 输出约束；Redesign draft template 会给同名旧图生成唯一 newAssetPath，并覆盖 `_2` 后缀碰撞；Redesign draft contract 覆盖重复旧图、重复新图和 newAssetPath 越过 generatedImageFolder；Redesign package contract 覆盖 source prefab 必须是 `Assets/*.prefab` 且出现在扫描报告里，outputFolder 只能是 `Assets/` 且不能包含反斜杠或 `..`。Creation brief contract 覆盖 targetFolder 路径和数组项类型；Layout draft contract 覆盖必需节点字段和交互数组项类型；Creation layout dry-run 和 host generate result 会拒绝重复逻辑行，host generate result 还要求 TargetPrefab 是 `Assets/*.prefab` 且 AssetPath 匹配动作语义，Creation host generate checklist 也有独立 Ready gate 行契约，并复验生成前清单、目标 prefab、当前 dry-run 组件列表和 Ready gate 文案。CSV 底层契约补充覆盖 quoted 字段跨物理行和引号后夹空格的严格拒绝样本；JSON 底层契约补充覆盖改版草稿重复 newAssetPath；Markdown section 契约会用 duplicate section 明确报错重复合法标题，并忽略 fenced code block 内的 `## `，同时锁定共享 Markdown 汇总 helper 的分组顺序。UIVipcard 真实 apply 仍按外部输入缺失预期阻断：`PendingPreview：1，PendingAsset：13，PendingAtlas：13`。

# Key Files
- `Editor/Generation/UIReplacementPlanDryRunService.cs`：替换 dry-run 读取、校验和重复检查行 gate。
- `Editor/Generation/UIReplacementPendingInputChecklistService.cs`：待补输入清单读取、校验和重复行 gate。
- `Editor/Generation/UIReplacementPendingInputReadinessService.cs`：待补输入就绪检查读取、校验和重复行 gate。
- `Editor/Generation/UIReplacementExternalInputPackageService.cs`：外部输入包生成、JSON 读回和重复 item gate。
- `Editor/Generation/UIReplacementExecutionPlanService.cs`：替换执行计划读取、校验和重复计划行 gate。
- `Editor/Generation/UIReplacementPlanStatus.cs`：替换计划阻断状态、汇总和 severity 顺序契约。
- `Editor/Generation/UIReplacementHostApplyResultService.cs`：host apply 结果读回和执行计划覆盖契约。
- `Editor/Generation/UIReplacementHostApplyChecklistService.cs`：host apply 前置清单 gate。
- `Editor/Generation/UIRedesignBriefService.cs`：改版 Brief Markdown section 和 AI 输出约束 gate。
- `Editor/Generation/UIRedesignDraftTemplateService.cs`：改版草稿模板和同名旧图 newAssetPath 去重。
- `Editor/Generation/UIRedesignDraftService.cs`：改版草稿 JSON 读取、路径边界和重复新旧图 gate。
- `Editor/Generation/UIRedesignPackageService.cs`：改版包 manifest 生成与校验入口。
- `Editor/Scanning/UIScanReportRows.cs`：核心扫描报告读取和全部 CoreReports 重复逻辑行 gate。
- `Editor/Scanning/UIScanSummaryService.cs`：扫描摘要、面板实测聚焦清单和 section 顺序契约。
- `Editor/Scanning/UIReportValidationService.cs`：核心报告注册表、CSV 和 JSON 底层契约 batch 入口。
- `Editor/Scanning/UIReuseSearchResultService.cs`：复用搜索结果读取、路径校验和重复结果行 gate。
- `Editor/Generation/UIComponentCandidateIndexService.cs`：组件候选索引和 review 清单读取、校验和重复 ComponentId gate。
- `Editor/Generation/UICreationBriefTemplateService.cs`：Creation brief JSON 读取、targetFolder 和数组项类型 gate。
- `Editor/Generation/UILayoutDraftTemplateService.cs`：Creation layout draft JSON 读取和必需字段类型 gate。
- `Editor/Generation/UICreationLayoutDryRunService.cs`：Creation layout dry-run 读取、校验和重复检查行 gate。
- `Editor/Generation/UICreationHostGenerateChecklistService.cs`：Creation 生成前清单 gate 和 Ready 文案复验。
- `Editor/Generation/UICreationHostGenerateResultService.cs`：Creation 宿主生成结果、目标 prefab 和 AssetPath 契约。
- `Editor/Generation/UIReportMarkdownContractService.cs`：Markdown section 顺序契约 batch 入口。
- `Editor/Config/UIAIToolsProfile.cs`：项目路径配置和默认 profile 契约。
- `Editor/Config/UIControlCatalog.cs`：项目控件角色配置和默认角色映射契约。

# Next Steps
1. 不依赖外部图片时，继续补真实宿主执行器或生成器回归样例。
2. 外部产物落位后，按 readiness、外部输入包、Ready gate、执行计划 gate、host apply gate 顺序重跑 UIVipcard。
3. 发布前重跑核心 CSV/JSON/Markdown 契约和 UIVipcard 改版包校验。

# Run / Test
- `ValidateReplacementPlanDryRunContractBatch` -> `Logs/Verify_ReplacementPlanDryRunContract_DuplicateRows.log`，覆盖替换 dry-run 重复检查行复验，return code 0。
- `ValidateReplacementPendingInputContractsBatch` -> `Logs/Verify_ReplacementPendingInputContracts_InputBoundaries.log`，覆盖待补输入清单和就绪检查重复行、路径、后缀、数量、item index 与实际尺寸复验，return code 0。
- `ValidateJsonContractBatch` -> `Logs/Verify_JsonContract_ExternalPackageDuplicateItems.log`，覆盖外部输入包 JSON 重复 list item 复验，return code 0。
- `ValidateJsonContractBatch` -> `Logs/Verify_JsonContract_RedesignDuplicateNewAsset.log`，覆盖改版草稿重复 newAssetPath 复验，return code 0。
- `ValidateReplacementExternalInputPackageContractBatch` -> `Logs/Verify_ReplacementExternalInputPackageContract_DuplicateRows.log`，覆盖外部输入包重复目录、参考输入和输入项复验，return code 0。
- `ValidateReplacementExecutionPlanContractBatch` -> `Logs/Verify_ReplacementExecutionPlanContract_Paths.log`，覆盖执行计划重复 plan row 和资源路径复验，return code 0。
- `ValidateReplacementPlanStatusContractBatch` -> `Logs/Verify_ReplacementPlanStatusContract_Order.log`，覆盖替换计划阻断状态、汇总顺序和 severity 顺序复验，return code 0。
- `ValidateRedesignBriefContractBatch` -> `Logs/Verify_RedesignBriefContract_OutputRules.log`，覆盖改版 Brief section 顺序和重复新旧图输出约束复验，return code 0。
- `ValidateRedesignDraftTemplateContractBatch` -> `Logs/Verify_RedesignDraftTemplateContract_SuffixCollision.log`，覆盖草稿模板同名旧图和 `_2` 后缀碰撞 newAssetPath 去重复验，return code 0。
- `ValidateRedesignDraftContractBatch` -> `Logs/Verify_RedesignDraftContract_DuplicateAssets.log`，覆盖改版草稿重复旧图、重复新图和新图目录越界复验，return code 0。
- `ValidateRedesignPackageContractBatch` -> `Logs/Verify_RedesignPackageContract_SourcePrefabPath.log`，覆盖改版包 source prefab 资源路径、扫描报告边界和 outputFolder 边界复验，return code 0。
- `ValidateUICreationBriefContractBatch` -> `Logs/Verify_UICreationBriefContract_InputBoundaries.log`，覆盖 Creation brief targetFolder 和数组项类型复验，return code 0。
- `ValidateUILayoutDraftContractBatch` -> `Logs/Verify_UILayoutDraftContract_InputBoundaries.log`，覆盖 Layout draft 必需节点字段和交互数组项类型复验，return code 0。
- `ValidateHostApplyChecklistContractBatch` -> `Logs/Verify_HostApplyChecklistContract_GateLines.log`，覆盖宿主执行清单 gate 行复验，return code 0。
- `ValidateHostApplyResultContractBatch` -> `Logs/Verify_HostApplyResultContract_ResultPaths.log`，覆盖缺失结果、阻断 plan、重复 plan 行、资源路径和 result 契约，return code 0。
- `ValidateScanReportRowsContractBatch` -> `Logs/Verify_ScanReportRowsContract_DuplicateAllCoreRows.log`，覆盖全部 CoreReports 重复逻辑行复验，return code 0。
- `ValidateScanSummaryContractBatch` -> `Logs/Verify_ScanSummaryContract_Sections.log`，覆盖扫描摘要和面板实测聚焦清单 Markdown section 顺序复验，return code 0。
- `ValidateProfileContractBatch` -> `Logs/Verify_ProfileContract_Defaults.log`，覆盖默认 profile 路径和文本扫描根复验，return code 0。
- `ValidateControlCatalogContractBatch` -> `Logs/Verify_ControlCatalogContract_DefaultRoles.log`，覆盖默认控件角色映射和大小写不敏感匹配复验，return code 0。
- `ValidateReportFilesContractBatch` -> `Logs/Verify_ReportFilesContract_HeaderRegistryAndPaths.log`，覆盖核心 CSV 注册表、表头列和路径规范化契约复验，return code 0。
- `ValidateCsvContractBatch` -> `Logs/Verify_CsvContract_MultilineAndStrictQuotes.log`，覆盖 CSV multiline quoted value 和 quote 后夹空格严格拒绝复验，return code 0。
- `ValidateMarkdownSectionContractBatch` -> `Logs/Verify_MarkdownSectionContract_SummaryHelpers.log`，覆盖重复合法 Markdown section、fenced code block 内标题忽略和共享汇总 helper 分组顺序复验，return code 0。
- `ValidateReuseSearchResultContractBatch` -> `Logs/Verify_ReuseSearchResultContract_PathBoundaries.log`，覆盖复用搜索结果重复结果行和路径边界复验，return code 0。
- `SearchReuseByImageBatch` -> `Logs/Verify_ReuseSearchResult_PathBoundaries.log`，用 UIVipcard 基准图复验真实复用反查输出，return code 0。
- `ValidateComponentCandidateContractBatch` -> `Logs/Verify_ComponentCandidateContract_PathBoundaries.log`，覆盖组件候选索引和 review 清单重复 ComponentId、样例路径、组件 prefab 与预览路径复验，return code 0。
- `ValidateComponentCandidateIndexBatch` -> `Logs/Verify_ComponentCandidateIndex_PathBoundaries.log`，覆盖当前真实组件候选索引和 review 清单路径边界复验，return code 0。
- `ValidateUICreationLayoutDryRunContractBatch` -> `Logs/Verify_UICreationLayoutDryRunContract_DuplicateRows.log`，覆盖 Creation layout dry-run 重复检查行复验，return code 0。
- `ValidateHostApplyBlockedResultSampleBatch` -> `Logs/Verify_HostApplyBlockedResultSample_PlanStatusHelper.log`，日志显示 26 行 skipped 阻断样例通过，return code 0。
- `ValidateUICreationHostGenerateChecklistContractBatch` -> `Logs/Verify_UICreationHostGenerateChecklistContract_GateLines.log`，覆盖 Creation 生成前清单 Ready gate 行复验，return code 0。
- `ValidateUICreationHostGenerateResultContractBatch` -> `Logs/Verify_UICreationHostGenerateResultContract_AssetPathBoundaries.log`，覆盖 Creation 清单目标、组件列表、Ready gate 文案、TargetPrefab 路径、AssetPath 动作级路径和重复结果行复验，return code 0。

# Constraints
- 包不得编译引用 `GameApp`、`MotionFramework`、`com.xipin.lframework` 或 YooAsset。
- 包内不得修改 prefab、图片、SpriteAtlas 或 YooAsset 配置。
- 宿主侧可改 wrapper、profile、catalog 和测试样本，但不提交到包仓库。
- Unity batch 前读宿主 `Docs/UnityBatchModeGuide.md`，一次只跑一个 Unity 进程。

# Known Issues
- 测试宿主当前没有 `.csproj` 可用，验证以 Unity batch 为准。
- 组件候选仍是扫描候选，不是已确认组件库。
- Editor 静态基准图不能覆盖运行时数据、动画、滚动状态等 UI 状态。
- UIVipcard 外部输入仍缺新版预览、13 张新图和目标图集。

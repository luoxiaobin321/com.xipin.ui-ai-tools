# Current Goal
把 `com.xipin.ui-ai-tools` 做成可审计的 Unity UI 自动化工具包，当前优先稳定改版和新 UI 生成链路的 gate 与宿主结果契约。

# Status
替换链路的 manifest、dry-run、pending input、external input package、execution plan、core scan rows、reuse search result、component candidate、host apply checklist 和 host apply result 已串起前置复验；dry-run、pending input、readiness、external input package、execution plan、core scan rows、reuse search result 和 component candidate 都会拒绝重复逻辑行，external input package 有独立重复输出目录/参考输入/输入项 contract，execution plan 也有独立重复计划行 contract，host apply checklist 有独立 gate 行契约，host apply result 要求所有可产生结果的 action 都有结果行，并拒绝同一计划行重复上报。Redesign draft contract 覆盖重复旧图、重复新图和 newAssetPath 越过 generatedImageFolder；Redesign package contract 覆盖 outputFolder 只能是 `Assets/` 且不能包含反斜杠或 `..`。Creation brief contract 覆盖 targetFolder 路径和数组项类型；Layout draft contract 覆盖必需节点字段和交互数组项类型；Creation layout dry-run 和 host generate result 会拒绝重复逻辑行，Creation host generate checklist 也有独立 Ready gate 行契约，并复验生成前清单、目标 prefab、当前 dry-run 组件列表和 Ready gate 文案。CSV 底层契约补充覆盖 quoted 字段跨物理行和引号后夹空格的严格拒绝样本；JSON 底层契约补充覆盖改版草稿重复 newAssetPath；Markdown section 契约会用 duplicate section 明确报错重复合法标题，并忽略 fenced code block 内的 `## `。UIVipcard 真实 apply 仍按外部输入缺失预期阻断：`PendingPreview：1，PendingAsset：13，PendingAtlas：13`。

# Key Files
- `Editor/Generation/UIReplacementPlanDryRunService.cs`：替换 dry-run 读取、校验和重复检查行 gate。
- `Editor/Generation/UIReplacementPendingInputChecklistService.cs`：待补输入清单读取、校验和重复行 gate。
- `Editor/Generation/UIReplacementPendingInputReadinessService.cs`：待补输入就绪检查读取、校验和重复行 gate。
- `Editor/Generation/UIReplacementExternalInputPackageService.cs`：外部输入包生成、JSON 读回和重复 item gate。
- `Editor/Generation/UIReplacementExecutionPlanService.cs`：替换执行计划读取、校验和重复计划行 gate。
- `Editor/Generation/UIReplacementHostApplyResultService.cs`：host apply 结果读回和执行计划覆盖契约。
- `Editor/Generation/UIReplacementHostApplyChecklistService.cs`：host apply 前置清单 gate。
- `Editor/Generation/UIRedesignDraftService.cs`：改版草稿 JSON 读取、路径边界和重复新旧图 gate。
- `Editor/Generation/UIRedesignPackageService.cs`：改版包 manifest 生成与校验入口。
- `Editor/Scanning/UIScanReportRows.cs`：核心扫描报告读取和全部 CoreReports 重复逻辑行 gate。
- `Editor/Scanning/UIReportValidationService.cs`：CSV/JSON 底层契约 batch 入口。
- `Editor/Scanning/UIReuseSearchResultService.cs`：复用搜索结果读取、校验和重复结果行 gate。
- `Editor/Generation/UIComponentCandidateIndexService.cs`：组件候选索引和 review 清单读取、校验和重复 ComponentId gate。
- `Editor/Generation/UICreationBriefTemplateService.cs`：Creation brief JSON 读取、targetFolder 和数组项类型 gate。
- `Editor/Generation/UILayoutDraftTemplateService.cs`：Creation layout draft JSON 读取和必需字段类型 gate。
- `Editor/Generation/UICreationLayoutDryRunService.cs`：Creation layout dry-run 读取、校验和重复检查行 gate。
- `Editor/Generation/UICreationHostGenerateChecklistService.cs`：Creation 生成前清单 gate 和 Ready 文案复验。
- `Editor/Generation/UICreationHostGenerateResultService.cs`：Creation 宿主生成结果契约。
- `Editor/Generation/UIReportMarkdownContractService.cs`：Markdown section 顺序契约 batch 入口。

# Next Steps
1. 不依赖外部图片时，继续补真实宿主执行器或生成器回归样例。
2. 外部产物落位后，按 readiness、外部输入包、Ready gate、执行计划 gate、host apply gate 顺序重跑 UIVipcard。
3. 发布前重跑核心 CSV/JSON/Markdown 契约和 UIVipcard 改版包校验。

# Run / Test
- `ValidateReplacementPlanDryRunContractBatch` -> `Logs/Verify_ReplacementPlanDryRunContract_DuplicateRows.log`，覆盖替换 dry-run 重复检查行复验，return code 0。
- `ValidateReplacementPendingInputContractsBatch` -> `Logs/Verify_ReplacementPendingInputContracts_DuplicateRows.log`，覆盖待补输入清单和就绪检查重复行复验，return code 0。
- `ValidateJsonContractBatch` -> `Logs/Verify_JsonContract_ExternalPackageDuplicateItems.log`，覆盖外部输入包 JSON 重复 list item 复验，return code 0。
- `ValidateJsonContractBatch` -> `Logs/Verify_JsonContract_RedesignDuplicateNewAsset.log`，覆盖改版草稿重复 newAssetPath 复验，return code 0。
- `ValidateReplacementExternalInputPackageContractBatch` -> `Logs/Verify_ReplacementExternalInputPackageContract_DuplicateRows.log`，覆盖外部输入包重复目录、参考输入和输入项复验，return code 0。
- `ValidateReplacementExecutionPlanContractBatch` -> `Logs/Verify_ReplacementExecutionPlanContract_DuplicateRows.log`，覆盖执行计划重复 plan row 复验，return code 0。
- `ValidateRedesignDraftContractBatch` -> `Logs/Verify_RedesignDraftContract_DuplicateAssets.log`，覆盖改版草稿重复旧图、重复新图和新图目录越界复验，return code 0。
- `ValidateRedesignPackageContractBatch` -> `Logs/Verify_RedesignPackageContract_OutputFolder.log`，覆盖改版包 outputFolder 边界复验，return code 0。
- `ValidateUICreationBriefContractBatch` -> `Logs/Verify_UICreationBriefContract_InputBoundaries.log`，覆盖 Creation brief targetFolder 和数组项类型复验，return code 0。
- `ValidateUILayoutDraftContractBatch` -> `Logs/Verify_UILayoutDraftContract_InputBoundaries.log`，覆盖 Layout draft 必需节点字段和交互数组项类型复验，return code 0。
- `ValidateHostApplyChecklistContractBatch` -> `Logs/Verify_HostApplyChecklistContract_GateLines.log`，覆盖宿主执行清单 gate 行复验，return code 0。
- `ValidateHostApplyResultContractBatch` -> `Logs/Verify_HostApplyResultContract_DuplicatePlanRows.log`，覆盖缺失结果、阻断 plan、重复 plan 行和 result 契约，return code 0。
- `ValidateScanReportRowsContractBatch` -> `Logs/Verify_ScanReportRowsContract_DuplicateAllCoreRows.log`，覆盖全部 CoreReports 重复逻辑行复验，return code 0。
- `ValidateCsvContractBatch` -> `Logs/Verify_CsvContract_MultilineAndStrictQuotes.log`，覆盖 CSV multiline quoted value 和 quote 后夹空格严格拒绝复验，return code 0。
- `ValidateMarkdownSectionContractBatch` -> `Logs/Verify_MarkdownSectionContract_FencedHeadings.log`，覆盖重复合法 Markdown section 的 duplicate section 报错和 fenced code block 内标题忽略复验，return code 0。
- `ValidateReuseSearchResultContractBatch` -> `Logs/Verify_ReuseSearchResultContract_DuplicateRows.log`，覆盖复用搜索结果重复结果行复验，return code 0。
- `ValidateComponentCandidateContractBatch` -> `Logs/Verify_ComponentCandidateContract_DuplicateRows.log`，覆盖组件候选索引和 review 清单重复 ComponentId 复验，return code 0。
- `ValidateUICreationLayoutDryRunContractBatch` -> `Logs/Verify_UICreationLayoutDryRunContract_DuplicateRows.log`，覆盖 Creation layout dry-run 重复检查行复验，return code 0。
- `ValidateHostApplyBlockedResultSampleBatch` -> `Logs/Verify_HostApplyBlockedResultSample_PlanStatusHelper.log`，日志显示 26 行 skipped 阻断样例通过，return code 0。
- `ValidateUICreationHostGenerateChecklistContractBatch` -> `Logs/Verify_UICreationHostGenerateChecklistContract_GateLines.log`，覆盖 Creation 生成前清单 Ready gate 行复验，return code 0。
- `ValidateUICreationHostGenerateResultContractBatch` -> `Logs/Verify_UICreationHostGenerateResultContract_ChecklistGateLines.log`，覆盖 Creation 清单目标、组件列表、Ready gate 文案和重复结果行复验，return code 0。

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

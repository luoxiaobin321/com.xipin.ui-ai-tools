# Current Goal
把 `com.xipin.ui-ai-tools` 做成可审计的 Unity UI 自动化工具包，当前优先稳定 AI 改版和新 UI 制作的输入、gate 与宿主结果报告契约。

# Status
宿主结果报告契约、Creation 生成结果契约、替换 dry-run/执行计划/待补输入读回契约已落地；替换链路报告、host apply 结果、Creation layout dry-run、组件候选索引、组件确认表和截图复用搜索结果都有 `ReadRows(profile)` 行契约。核心扫描报告新增 `UIScanReportRows` 读入口，并对生成链路依赖的关键路径字段和计数字段做行级校验；扫描摘要、改版 brief/template、替换 dry-run/执行计划、组件候选索引和外部输入包已复用。改版包 manifest 会复验宿主执行清单，host apply 结果对执行计划校验前也会复验宿主执行清单；Creation 结果对布局草稿校验前也会复验生成前清单，并用临时组件候选/dry-run/checklist 样例覆盖契约。UIVipcard 仍因外部产物缺失预期阻断：`PendingPreview：1，PendingAsset：13，PendingAtlas：13`。

# Key Files
- `Editor/Generation/UIReplacementExecutionPlanService.cs`：替换执行计划生成与读回契约。
- `Editor/Generation/UIReplacementPlanDryRunService.cs`：替换 dry-run 与读回契约。
- `Editor/Generation/UIReplacementPendingInputReadinessService.cs`：待补输入就绪检查与读回契约。
- `Editor/Generation/UIReplacementHostApplyResultService.cs`：宿主 apply 结果报告读回契约，并在对执行计划校验前复验宿主执行清单。
- `Editor/Generation/UIRedesignPackageService.cs`：改版包 manifest 生成与校验入口。
- `Editor/Generation/UIComponentCandidateIndexService.cs`：组件候选索引和人工确认表读回契约。
- `Editor/Generation/UICreationLayoutDryRunService.cs`：新 UI 布局 dry-run 与读回契约。
- `Editor/Generation/UICreationHostGenerateResultService.cs`：Creation 宿主生成结果契约，结果对布局草稿校验前复验生成前清单。
- `Editor/Scanning/UIScanReportRows.cs`：核心扫描 CSV 报告校验后读行入口和轻量行契约。
- `Editor/Scanning/UIReuseSearchResultService.cs`：截图复用搜索结果报告读回契约。

# Next Steps
1. 不依赖外部图片时，继续补宿主生成器回归数据或真实宿主执行器样例。
2. 外部产物落位后，按 readiness、外部输入包、Ready gate、执行计划 gate、host apply gate 顺序重跑。
3. 发布前重跑核心 CSV/JSON/Markdown 契约和 UIVipcard 改版包校验。

# Run / Test
- 最近已验证：`GenerateSummary` -> `Logs/Generate_Summary_ScanReadRows.log`，exit code 0。
- 最近已验证：`GeneratePanelFocus` -> `Logs/Generate_PanelFocus_ScanReadRows.log`，exit code 0。
- 最近已验证：`GenerateRedesignBriefBatch -uiPrefabPath Assets/Bundle/Prefab/UIVipcard/UIVipcard.prefab -uiPreviewPath Logs/UIPrefabBaseline_Bundle_Prefab_UIVipcard_UIVipcard.png` -> `Logs/Generate_RedesignBrief_ScanReadRows.log`，exit code 0。
- 最近已验证：`GenerateRedesignDraftTemplateBatch -uiPrefabPath Assets/Bundle/Prefab/UIVipcard/UIVipcard.prefab -uiPreviewPath Logs/UIPrefabBaseline_Bundle_Prefab_UIVipcard_UIVipcard.png` -> `Logs/Generate_RedesignDraftTemplate_ScanReadRows.log`，exit code 0。
- 最近已验证：`DryRunRedesignDraftJsonBatch -uiDraftJsonPath Logs/UIRedesignDraftTemplate_UIVipcard_UIVipcard.json` -> `Logs/Generate_ReplacementDryRun_ScanReadRows.log`，exit code 0。
- 最近已验证：`GenerateReplacementExecutionPlanBatch -uiDraftJsonPath Logs/UIRedesignDraftTemplate_UIVipcard_UIVipcard.json` -> `Logs/Generate_ReplacementExecutionPlan_ScanReadRows.log`，exit code 0。
- 最近已验证：`GenerateComponentCandidateIndexBatch` -> `Logs/Generate_ComponentCandidateIndex_ScanReadRows.log`，exit code 0。
- 最近已验证：`GenerateReplacementExternalInputPackageBatch -uiPrefabPath Assets/Bundle/Prefab/UIVipcard/UIVipcard.prefab` -> `Logs/Generate_ReplacementExternalInputPackage_ScanReadRows.log`，exit code 0。
- 最近已验证：`ValidateReplacementExternalInputPackageBatch -uiPrefabPath Assets/Bundle/Prefab/UIVipcard/UIVipcard.prefab` -> `Logs/Verify_ReplacementExternalInputPackage_ScanReadRows.log`，exit code 0。
- 最近已验证：`SearchReuseByImageBatch -uiQueryImage Logs/UIPrefabBaseline_Bundle_Prefab_UIVipcard_UIVipcard.png` -> `Logs/Verify_ReuseSearchResult_ReadRows.log`，exit code 0。
- 最近已验证：`GenerateSummary` -> `Logs/Generate_Summary_CoreRowValidation.log`，exit code 0。
- 最近已验证：`GeneratePanelFocus` -> `Logs/Generate_PanelFocus_CoreRowValidation.log`，exit code 0。
- 最近已验证：`GenerateComponentCandidateIndexBatch` -> `Logs/Generate_ComponentCandidateIndex_CoreRowValidation.log`，exit code 0。
- 最近已验证：`GenerateRedesignBriefBatch -uiPrefabPath Assets/Bundle/Prefab/UIVipcard/UIVipcard.prefab -uiPreviewPath Logs/UIPrefabBaseline_Bundle_Prefab_UIVipcard_UIVipcard.png` -> `Logs/Generate_RedesignBrief_CoreRowValidation.log`，exit code 0。
- 最近已验证：`ValidateReports` -> `Logs/Verify_Reports_AllCoreRowValidation.log`，exit code 0。
- 最近已验证：`ValidateMarkdownSectionContractBatch` -> `Logs/Verify_MarkdownContract_PostCoreRows.log`，exit code 0。
- 最近已验证：`ValidateCsvContractBatch` -> `Logs/Verify_CsvContract_PostCoreRows.log`，exit code 0。
- 最近已验证：`ValidateJsonContractBatch` -> `Logs/Verify_JsonContract_PostCoreRows.log`，exit code 0。
- 最近已验证：`ValidateUICreationHostGenerateResultContractBatch` -> `Logs/Verify_UICreationHostGenerateResultContract_PostCoreRows.log`，exit code 0。
- 最近已验证：`ValidateHostApplyResultContractBatch` -> `Logs/Verify_HostApplyResultContract_PostCoreRows.log`，exit code 0。
- 最近已验证：`ValidateRedesignPackageBatch -uiPrefabPath Assets/Bundle/Prefab/UIVipcard/UIVipcard.prefab` -> `Logs/Verify_RedesignPackage_PostCoreRows.log`，exit code 0。
- 最近已验证：`ValidateRedesignPackageBatch -uiPrefabPath Assets/Bundle/Prefab/UIVipcard/UIVipcard.prefab` -> `Logs/Verify_RedesignPackage_HostApplyChecklistValidation.log`，exit code 0。
- 最近已验证：`ValidateHostApplyResultContractBatch` -> `Logs/Verify_HostApplyResultContract_ChecklistGate.log`，exit code 0。
- 最近已验证：`ValidateHostApplyBlockedResultSampleBatch` -> `Logs/Verify_HostApplyBlockedResultSample_ChecklistGate.log`，exit code 0。
- 最近已验证：`ValidateUICreationHostGenerateResultContractBatch` -> `Logs/Verify_UICreationHostGenerateResultContract_ChecklistGate.log`，exit code 0。

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

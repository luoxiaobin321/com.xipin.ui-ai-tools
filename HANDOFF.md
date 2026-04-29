# Current Goal
把 `com.xipin.ui-ai-tools` 做成可审计的 Unity UI 自动化工具包，当前先稳定 AI 改版和新 UI 制作的输入、gate 与宿主结果报告契约。

# Status
总纲进度约 69%；“宿主结果报告契约回归”已推送到 `origin/main`（最新功能提交 `4613968`）。包侧已提供 `UIReplacementHostApplyResult.csv/md` 与 `UICreationHostGenerateResult.csv/md` 只读契约，host apply checklist 增强为内容级校验；新 UI 生成结果可通过 `ValidateAgainstLayoutDraft` 校验当前草稿目标、NodeId/ComponentId 归属、Action 白名单、节点动作字段形状、每个草稿节点至少一条结果行、每个节点的 `Applied ApplyLayout` 行、目标 prefab 的 `Applied CreatePrefab` 行和 `Verified VerifyAfterGenerate` 行，宿主替换结果可通过 `ValidateAgainstExecutionPlan` 匹配当前执行计划，要求计划内 `ApplyPrefabReference` / `VerifyAfterApply` 都有结果行覆盖，并在当前执行计划仍有阻断步骤时只允许 `Skipped` 结果。测试宿主已有阻断结果样例，可从 UIVipcard 执行计划生成 26 行 `Skipped` 并读回校验。UIVipcard 仍缺新版预览、13 张新图和目标图集，host apply gate 预期阻断。

# Key Files
- `Editor/Generation/UIReplacementHostApplyResultService.cs`：AI 改版宿主执行结果报告契约，包含 Action 白名单、当前执行计划反查、apply/verify 覆盖校验、阻断计划只允许跳过结果和 Skipped/Failed 说明必填校验。
- `Editor/Generation/UICreationHostGenerateResultService.cs`：新 UI 宿主生成结果报告契约和基础汇总生成，包含 Action 白名单、节点动作字段形状、prefab 创建、草稿节点布局行、生成后复验覆盖和 Skipped/Failed 说明必填校验。
- `Editor/Generation/UIReplacementHostApplyChecklistService.cs`：宿主替换前清单和 gate。
- `Editor/Scanning/UIReportFiles.cs`：核心文件名和 CSV 表头常量。
- `Documentation~/modules/ui-creation-host-generator.md`：宿主 prefab 草稿生成器接入说明。

# Next Steps
1. 不依赖外部图片时，继续补更多宿主生成器回归数据或真实宿主执行器样例。
2. 外部产物落位后，按 readiness、外部输入包、Ready gate、执行计划 gate、host apply gate 顺序重跑。
3. 发布前，再跑一轮核心扫描和项目 UI 回归。

# Run / Test
- `UIAssetTriageScanner.ValidateHostApplyResultContractBatch`；`UIAssetTriageScanner.ValidateUICreationHostGenerateResultContractBatch`
- 最近推送：宿主结果报告覆盖、Skipped 说明、Creation Action 白名单与正样例覆盖。
- 最近已跑：`ValidateHostApplyResultContractBatch` -> `Logs/Verify_HostApplyResultContract_Coverage.log`；`ValidateHostApplyBlockedResultSampleBatch` -> `Logs/Verify_HostApplyBlockedResultSample_Coverage.log`；`ValidateUICreationHostGenerateResultContractBatch` -> `Logs/Verify_UICreationHostGenerateResultContract_CreatePrefabCoverage.log`；`ValidateUICreationHostGenerateResultBatch -uiLayoutDraftJsonPath Logs/UILayoutDraftReadySample_ShopDialogTemplate.json` -> `Logs/Verify_UICreationHostGenerateResult_CreatePrefabCoverage.log`；均 exit code 0。
- 覆盖收紧后已跑：`ValidateMarkdownSectionContractBatch` -> `Logs/Verify_MarkdownSectionContract_AfterCoverageTightening.log`；`ValidateCsvContractBatch` -> `Logs/Verify_CsvContract_AfterCoverageTightening.log`；`ValidateJsonContractBatch` -> `Logs/Verify_JsonContract_AfterCoverageTightening.log`；均 exit code 0。
- Action 样例覆盖后已跑：`ValidateCsvContractBatch` -> `Logs/Verify_CsvContract_AfterActionSamples.log`；`ValidateJsonContractBatch` -> `Logs/Verify_JsonContract_AfterActionSamples.log`；`ValidateMarkdownSectionContractBatch` -> `Logs/Verify_MarkdownSectionContract_AfterActionSamples.log`；均 exit code 0。
- 阻断计划结果规则已跑：`ValidateHostApplyResultContractBatch` -> `Logs/Verify_HostApplyResultContract_BlockingPlan.log`；`ValidateHostApplyBlockedResultSampleBatch` -> `Logs/Verify_HostApplyBlockedResultSample_BlockingPlan.log`；均 exit code 0。
- Skipped 说明规则已跑：`ValidateHostApplyResultContractBatch` -> `Logs/Verify_HostApplyResultContract_SkippedMessage.log`；`ValidateHostApplyBlockedResultSampleBatch` -> `Logs/Verify_HostApplyBlockedResultSample_SkippedMessage.log`；均 exit code 0。
- Host apply Action 白名单已跑：`ValidateHostApplyResultContractBatch` -> `Logs/Verify_HostApplyResultContract_ActionWhitelist.log`；`ValidateHostApplyBlockedResultSampleBatch` -> `Logs/Verify_HostApplyBlockedResultSample_ActionWhitelist.log`；`ValidateHostApplyResultBatch` -> `Logs/Verify_HostApplyResult_ActionWhitelist.log`；均 exit code 0。
- Host apply Action 正样例覆盖已跑：`ValidateHostApplyResultContractBatch` -> `Logs/Verify_HostApplyResultContract_ActionSampleCoverage.log`；exit code 0，正样例覆盖 6 行。
- Creation Action 白名单已跑：`ValidateUICreationHostGenerateResultContractBatch` -> `Logs/Verify_UICreationHostGenerateResultContract_ActionWhitelist.log`；`ValidateUICreationHostGenerateResultBatch -uiLayoutDraftJsonPath Logs/UILayoutDraftReadySample_ShopDialogTemplate.json` -> `Logs/Verify_UICreationHostGenerateResult_ActionWhitelist.log`；均 exit code 0。
- Creation Action 正样例覆盖已跑：`ValidateUICreationHostGenerateResultContractBatch` -> `Logs/Verify_UICreationHostGenerateResultContract_ActionSampleCoverage.log`；exit code 0。
- Creation 节点动作字段形状已跑：`ValidateUICreationHostGenerateResultContractBatch` -> `Logs/Verify_UICreationHostGenerateResultContract_NodeActionShape.log`；`ValidateUICreationHostGenerateResultBatch -uiLayoutDraftJsonPath Logs/UILayoutDraftReadySample_ShopDialogTemplate.json` -> `Logs/Verify_UICreationHostGenerateResult_NodeActionShape.log`；均 exit code 0。
- 预期阻断已确认：`ValidateHostApplyChecklistBatch` -> `Logs/Verify_HostApplyChecklist_ExpectedBlocked_AfterBlockingResultRule.log`；Unity exit code 1，命中 `PendingPreview：1，PendingAsset：13，PendingAtlas：13`。
- `UIAssetTriageScanner.ValidateCsvContractBatch`；`UIAssetTriageScanner.ValidateJsonContractBatch`；`UIAssetTriageScanner.ValidateMarkdownSectionContractBatch`
- `UIAssetTriageScanner.ValidateUICreationHostGenerateResultBatch -uiLayoutDraftJsonPath Logs/UILayoutDraftReadySample_ShopDialogTemplate.json`
- `UIAssetTriageScanner.ValidateRedesignPackageBatch -uiPrefabPath Assets/Bundle/Prefab/UIVipcard/UIVipcard.prefab`；`UIAssetTriageScanner.ValidateHostApplyChecklistBatch` 预期因 `PendingPreview：1，PendingAsset：13，PendingAtlas：13` 阻断。
- `UIAssetTriageScanner.GenerateHostApplyBlockedResultSampleBatch`；`UIAssetTriageScanner.ValidateHostApplyBlockedResultSampleBatch`；`UIAssetTriageScanner.GenerateHostApplyResultSummaryBatch`；`UIAssetTriageScanner.ValidateHostApplyResultBatch`

# Constraints
- 包不得编译引用 `GameApp`、`MotionFramework`、`com.xipin.lframework` 或 YooAsset。
- 包内不得修改 prefab、图片、SpriteAtlas 或 YooAsset 配置。
- 宿主侧可以改 wrapper、profile、catalog 和测试样本，但不提交到包仓库。
- Unity batch 前读宿主 `Docs/UnityBatchModeGuide.md`，一次只跑一个 Unity 进程。

# Known Issues
- 测试宿主当前没有 `.csproj` 可用，验证以 Unity batch 为准。
- 组件候选仍是扫描候选，不是已确认组件库。
- Editor 静态基准图不能覆盖运行时数据、动画、滚动状态等 UI 状态。
- UIVipcard 外部输入仍缺新版预览、13 张新图和目标图集。

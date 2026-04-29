# Current Goal
把 `com.xipin.ui-ai-tools` 做成可审计的 Unity UI 自动化工具包，当前优先稳定 AI 改版和新 UI 制作的输入、gate 与宿主结果报告契约。

# Status
宿主结果报告契约、Creation 生成结果契约、替换执行计划/替换 dry-run 读回契约已落地；执行计划统一通过 `UIReplacementExecutionPlanService.ReadRows(profile)` 校验，替换 dry-run 统一通过 `UIReplacementPlanDryRunService.ReadRows(profile)` 校验，Creation layout dry-run 也有 `ReadRows(profile)` 行契约并被宿主生成清单复用。UIVipcard 仍因外部产物缺失预期阻断：`PendingPreview：1，PendingAsset：13，PendingAtlas：13`。

# Key Files
- `Editor/Generation/UIReplacementExecutionPlanService.cs`：替换执行计划生成与读回契约。
- `Editor/Generation/UIReplacementPlanDryRunService.cs`：替换 dry-run 与读回契约。
- `Editor/Generation/UIRedesignPackageService.cs`：改版包 manifest 生成与校验入口。
- `Editor/Generation/UICreationLayoutDryRunService.cs`：新 UI 布局 dry-run 与读回契约。
- `Editor/Generation/UICreationHostGenerateResultService.cs`：新 UI 宿主生成结果报告契约。
- `Editor/Scanning/UIReportFiles.cs`：报告文件名与 CSV 表头常量。

# Next Steps
1. 不依赖外部图片时，继续补宿主生成器回归数据或真实宿主执行器样例。
2. 外部产物落位后，按 readiness、外部输入包、Ready gate、执行计划 gate、host apply gate 顺序重跑。
3. 发布前重跑核心 CSV/JSON/Markdown 契约和 UIVipcard 改版包校验。

# Run / Test
- 最近已验证：`ValidateReplacementPlanDryRunBatch` -> `Logs/Verify_ReplacementPlanDryRun_ReadRows.log`，exit code 0。
- 最近已验证：`ValidateRedesignPackageBatch -uiPrefabPath Assets/Bundle/Prefab/UIVipcard/UIVipcard.prefab` -> `Logs/Verify_RedesignPackage_DryRunReadRows.log`，exit code 0。
- 最近已验证：`GenerateUICreationHostGenerateChecklistBatch -uiLayoutDraftJsonPath Logs/UILayoutDraftReadySample_ShopDialogTemplate.json` -> `Logs/Generate_UICreationHostChecklist_LayoutDryRunReadRows.log`，exit code 0。
- 最近已验证：`ValidateUICreationLayoutDryRunBatch` -> `Logs/Verify_UICreationLayoutDryRun_ExpectedBlocked_ReadRows.log`，预期 exit code 1，命中 `UI creation layout dry-run has blocking errors: 1`。

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

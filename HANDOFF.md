# Current Goal
把 `com.xipin.ui-ai-tools` 做成可审计的 Unity UI 自动化工具包，当前优先稳定改版和新 UI 生成链路的 gate 与宿主结果契约。

# Status
替换链路的 manifest、host apply checklist 和 host apply result 已串起前置复验；host apply result 现在要求执行计划中所有可产生结果的 action 都有结果行，并拒绝同一计划行重复上报。`RequiresResultAction` 和 `UIReplacementPlanStatus` 已公开给宿主样例复用。Creation host generate result 会复验生成前清单，校验目标 prefab 与当前 dry-run 组件列表，并拒绝同一生成结果行重复上报。UIVipcard 真实 apply 仍按外部输入缺失预期阻断：`PendingPreview：1，PendingAsset：13，PendingAtlas：13`。

# Key Files
- `Editor/Generation/UIReplacementHostApplyResultService.cs`：host apply 结果读回和执行计划覆盖契约。
- `Editor/Generation/UIReplacementPlanStatus.cs`：执行计划状态排序、阻断判断和摘要 helper。
- `Editor/Generation/UIReplacementHostApplyChecklistService.cs`：host apply 前置清单 gate。
- `Editor/Generation/UIRedesignPackageService.cs`：改版包 manifest 生成与校验入口。
- `Editor/Generation/UICreationHostGenerateResultService.cs`：Creation 宿主生成结果契约。
- `Editor/Generation/UICreationHostGenerateChecklistService.cs`：Creation 生成前清单 gate。
- `Editor/Generation/UIReplacementPendingInputReadinessService.cs`：UIVipcard 外部输入就绪检查。

# Next Steps
1. 不依赖外部图片时，继续补真实宿主执行器或生成器回归样例。
2. 外部产物落位后，按 readiness、外部输入包、Ready gate、执行计划 gate、host apply gate 顺序重跑 UIVipcard。
3. 发布前重跑核心 CSV/JSON/Markdown 契约和 UIVipcard 改版包校验。

# Run / Test
- `ValidateHostApplyResultContractBatch` -> `Logs/Verify_HostApplyResultContract_DuplicateRows.log`，日志显示 checklist、execution plan 和 result contract validation passed，return code 0。
- `ValidateHostApplyBlockedResultSampleBatch` -> `Logs/Verify_HostApplyBlockedResultSample_PlanStatusHelper.log`，日志显示 26 行 skipped 阻断样例通过，return code 0。
- `ValidateUICreationHostGenerateResultContractBatch` -> `Logs/Verify_UICreationHostGenerateResultContract_DuplicateRows.log`，用于覆盖 Creation 清单目标、组件列表和重复结果行复验，return code 0。

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

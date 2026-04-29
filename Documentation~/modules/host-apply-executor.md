# 宿主确认后执行器

宿主确认后执行器属于接入项目，不属于 `com.xipin.ui-ai-tools` 包。包内只提供扫描事实、AI 草稿协议、dry-run、待确认执行计划、宿主执行清单和 gate；真正移动资源、更新 prefab、调整 SpriteAtlas 或 YooAsset 配置的代码必须放在宿主项目。

## 输入

执行器读取当前 `UIAIToolsProfile.logRoot` 下的产物：

- `UIRedesignPackage_*.md`：改版包 manifest，记录 request、输入草稿 JSON、快照草稿 JSON 和产物路径。
- `UIReplacementPlanDryRun.csv`：静态检查明细。
- `UIReplacementExecutionPlan.csv`：待确认执行步骤。
- `UIReplacementPendingInputReadiness.csv`：新版预览、新图和目标图集的 Ready/Missing/Invalid 状态。
- `UIReplacementExternalInputPackage.md`：外部产物目录、验收、Prompt、引用素材和落位后复跑顺序。
- `UIReplacementHostApplyChecklist.md`：宿主执行前人工清单。

宿主侧还需要读取自己的人工确认记录，至少覆盖新版预览、新图验收、目标图集归属、复用风险、GUID 策略、YooAsset 地址策略和回归验收人。

## 前置 gate

执行器开始前必须先通过：

```csharp
UIReplacementPlanDryRunService.ValidateNoErrors(profile);
UIReplacementExecutionPlanService.ValidateNoBlockingStatuses(profile);
UIReplacementPendingInputReadinessService.ValidateNoMissing(profile);
UIReplacementHostApplyChecklistService.ValidateNoBlockingSteps(profile);
```

这些 gate 只说明 dry-run、执行计划、外部待补输入和宿主清单的阻断项已经补齐，不代表 `NeedsReview` 已经被批准。宿主执行器必须拒绝没有人工确认记录的 `NeedsReview` 和 `PendingConfirmation` 步骤。

## 允许动作

宿主执行器可以按项目规则实现这些动作：

- 导入或移动已确认的新图，并保持 Unity `.meta` 和 GUID 策略可追踪。
- 更新 prefab 内确认范围内的旧图引用。
- 把新图加入确认过的目标 SpriteAtlas 或宿主图集配置。
- 按宿主项目规则更新 YooAsset 地址或 collector 配置。
- 输出执行结果报告，记录每个 `ApplyPrefabReference` 的执行状态、旧资源、新资源、prefab 引用和人工确认记录。

执行器不得跳过 dry-run、执行计划 gate 或宿主清单 gate；不得把 `NeedsReview` 自动视为通过；不得在没有人工确认时删除旧资源或覆盖 prefab。

## 输出

建议宿主输出独立报告：`UIReplacementHostApplyResult.csv` 和 `UIReplacementHostApplyResult.md`。包侧提供只读校验和 Markdown 汇总生成入口；它只校验报告，不执行资源改动。CSV 字段为：

| 字段 | 含义 |
| --- | --- |
| `ItemIndex` | 对应 `UIReplacementExecutionPlan.csv` 的替换项。 |
| `Action` | 宿主执行动作，例如 `MoveNewAsset`、`ApplyPrefabReference`、`UpdateAtlas`、`UpdateAddress`、`VerifyAfterApply`。 |
| `Status` | `Applied`、`Skipped`、`Failed` 或 `Verified`。 |
| `OldAsset` | 旧资源路径。 |
| `NewAsset` | 新资源路径。 |
| `TargetAtlas` | 目标图集。 |
| `PrefabRefs` | 实际处理的 prefab。 |
| `Confirmation` | 宿主人工确认记录标识。 |
| `Message` | 执行结果说明。 |

包侧会校验 CSV 精确表头、非空结果行、`ItemIndex`、`Action`、状态、Applied/Verified 确认记录、Failed 说明、Markdown 顶层标题、状态分布和执行后复验清单。

测试宿主可用 `GenerateHostApplyBlockedResultSampleBatch` 从当前执行计划生成阻断样例结果，并用 `ValidateHostApplyBlockedResultSampleBatch` 读回逐行比对执行计划。它只写 `Skipped` 报告行并复用包侧汇总和校验，不移动资源、不覆盖 prefab、不修改图集。

## 执行后复验

宿主执行完成后应重新运行：

1. 核心扫描。
2. `UIReportValidationService.Validate(profile)`。
3. `UIReplacementPlanDryRunService.Run(profile, snapshotJsonPath)`。
4. `UIReplacementExecutionPlanService.Generate(profile, snapshotJsonPath)`。
5. 宿主项目自己的 UI 回归、图集构建和资源加载验证。

复验通过前，不应删除旧资源，也不应把替换结果当成已发布状态。

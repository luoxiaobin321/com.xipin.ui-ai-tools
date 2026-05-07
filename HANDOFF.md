# com.xipin.ui-ai-tools Handoff

## Current Goal
把 UI 扫描、复用反查、自动制作 UI 和新版换皮的报告契约收紧到可复跑、可审计、可接力。

## Status
包内通用报告契约已覆盖扫描、复用反查、自动制作 UI、global runtime、换皮 summary/visual/binding/apply、generated summaries、host-adapter checklist 和宿主 UIVipcard 专项报告。宿主 master plan 会校验现有真实 CSV 全覆盖、Creation JSON 本体复读、skin generated JSON 覆盖、skin 子报告实物字段、非 skin Markdown 实物章节顺序、host-adapter 实物字段、UIVipcard Sprite Backfill 与标题绑定实物字段。最新验证：`Logs/Verify_CodexSkinSubreports_MasterPlan_20260507.log` 通过。

## Key Files
- `Editor/Generation/UIReportMarkdown.cs`：Markdown 章节顺序契约核心。
- `Editor/Generation/UIReportMarkdownContractService.cs`：契约 self-test 包装。
- `Editor/Skinning/UISkinContractService.cs`：包内换皮报告契约。
- `Development~/modules/report-contracts.md`：报告契约说明。
- `../Assets/Scripts/GameApp/Editor/GameApp/UIAssetTriageScanner.cs`：宿主总体验证入口。

## Next Steps
1. 继续只扫真实用户可读报告是否还有契约缺口。
2. prompt 型 Markdown 不为形式感加报告契约，除非它变成用户审计报告。
3. 若改章节或 CSV 结构，同步生成逻辑、契约验证、实际报告和文档说明。
4. 收口跑宿主 `UIAssetTriageScanner.ValidateUIToolsMasterPlanBatch`。

## Run / Test
- `UIAssetTriageScanner.ValidateUIToolsMasterPlanBatch`：通过，`Logs/Verify_CodexSkinSubreports_MasterPlan_20260507.log`。
- `git -C Packages/com.xipin.ui-ai-tools diff --check`：仅既有 LF/CRLF warning。
- 卫生检查：无 Unity/CrashHandler、`Logs/` 仅 `.log`、无 `__Contract_*`。

## Constraints
- 包内不得依赖 `GameApp`、`MotionFramework`、`com.xipin.lframework` 或 YooAsset。
- 包内默认只生成报告、JSON、Markdown 和 gate，不直接写正式 prefab、图片、图集或 YooAsset 配置。
- 旧实验链路不要恢复为菜单、batch、文档入口或公共 API。
- 宿主侧测试资源可以改，但包提交不要混入宿主专用实现。

## Known Issues
- 当前 provided-crops 可直接生成 `_v2.prefab` 的是宿主 UIVipcard 分支；其它 UI 要先补宿主映射。
- 包仓库仍是 dirty 状态，继续工作时只处理当前任务相关 diff。

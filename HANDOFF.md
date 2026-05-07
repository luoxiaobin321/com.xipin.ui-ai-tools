# com.xipin.ui-ai-tools Handoff

## Current Goal
把 UI 扫描、复用反查、自动制作 UI 和新版换皮的报告契约收紧到可复跑、可审计、可接力。

## Status
包内报告契约代码已按 5 个提交收口并 push 到 `origin/main`，代码基线为 `a4290d2589e05a744b75ad217dc25c182699dc54`。旧 redesign/replacement 链路已移除；skinning 通用契约、报告摘要契约和 package dependency boundary gate 已落地。宿主已生成非 UIVipcard 样本 `UICatPassInfo` 的 host-adapter checklist，最新 skin contract 与 master plan 验证均通过。

## Key Files
- `Editor/Generation/UIReportMarkdown.cs`：Markdown 章节顺序契约核心。
- `Editor/Generation/UIReportMarkdownContractService.cs`：契约 self-test 包装。
- `Editor/Skinning/UISkinContractService.cs`：包内换皮报告契约。
- `Development~/modules/report-contracts.md`：报告契约说明。
- `../Assets/Scripts/GameApp/Editor/GameApp/UIAssetTriageScanner.cs`：宿主总体验证入口。
- `../UIAIToolsReports/Skinning/UICatPassInfo/host-adapter-checklist.md`：非 UIVipcard 宿主映射样本。

## Next Steps
1. 若继续换皮泛化，先在宿主为 `UICatPassInfo` 补 adapter，生成 `provided-crops-spec.md`。
2. adapter 成形后跑 provided-crops spec、crops validation 和 review package。
3. 若改章节或 CSV/JSON 结构，同步生成逻辑、契约验证、实际报告和文档说明。
4. prompt 型 Markdown 不为形式感加报告契约，除非它变成用户审计报告。

## Run / Test
- `UIAssetTriageScanner.GenerateSkinHostAdapterChecklistBatch`：通过，`Logs/Generate_UICatPassInfoHostAdapterChecklist_20260507.log`。
- `UIAssetTriageScanner.ValidateSkinContractBatch`：通过，`Logs/ValidateSkinContractBatch_20260507.log`。
- `UIAssetTriageScanner.ValidateUIToolsMasterPlanBatch`：通过，`Logs/ValidateUIToolsMasterPlanBatch_20260507.log`。

## Constraints
- 包内不得依赖 `GameApp`、`MotionFramework`、`com.xipin.lframework` 或 YooAsset。
- 包内默认只生成报告、JSON、Markdown 和 gate，不直接写正式 prefab、图片、图集或 YooAsset 配置。
- 旧实验链路不要恢复为菜单、batch、文档入口或公共 API。
- 宿主侧测试资源可以改，但包提交不要混入宿主专用实现。

## Known Issues
- 当前 provided-crops 可直接生成 `_v2.prefab` 的是宿主 UIVipcard 分支；其它 UI 要先补宿主映射。

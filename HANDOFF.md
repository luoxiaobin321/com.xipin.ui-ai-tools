# Current Goal
维护独立 UPM 包 `com.xipin.ui-ai-tools`，用 `E:\Work\UIAIToolsClient` 作为 Unity 测试宿主验证扫描、AI 改版输入链路和新 UI 生成前 gate。

# Status
包已从原游戏项目迁出为独立仓库形态，远端目标是 `https://github.com/luoxiaobin321/com.xipin.ui-ai-tools.git`。当前最新能力包括 CSV/JSON/Markdown 契约校验、外部输入包读回校验、字符串转义校验、ASCII 整数校验、对象字段分隔校验；UIVipcard redesign package 最近验证仍是预期 `15 inputs / 15 missing`。宿主 wrapper 和真实资源只用于测试，不进入包仓库。

# Key Files
- `Editor/Generation/UICreationBriefTemplateService.cs`：Brief/layout JSON 边界校验。
- `Editor/Generation/UIRedesignDraftService.cs`：redesign draft JSON 边界校验。
- `Editor/Generation/UIReplacementExternalInputPackageService.cs`：外部输入包生成、读回和 validate gate。
- `Editor/Scanning/UIReportValidationService.cs`：CSV/JSON/Markdown 契约自检样例。
- `Development~/modules/*contracts.md`、`Documentation~/modules/*.md`：契约说明。

# Next Steps
1. 初始化并推送包目录 Git 仓库。
2. 在测试宿主跑 `UIAssetTriageScanner.ValidateJsonContractBatch` 做轻量回归。
3. 继续只读推进时，优先排查其它 JSON 读回/外部输入边界，补最小自检样例和文档。
4. 若 UIVipcard 外部产物已落位，按 readiness、外部输入包验证、Ready gate、执行计划 gate、prepare/validate redesign package 顺序重跑。

# Run / Test
- 宿主 batch：`UIAssetTriageScanner.ValidateJsonContractBatch`
- 宿主 batch：`UIAssetTriageScanner.ValidateRedesignPackageBatch -uiPrefabPath Assets/Bundle/Prefab/UIVipcard/UIVipcard.prefab`
- 包目录：`git status --short --branch`

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

# 报告契约

CSV、JSON 和 Markdown 报告是包与宿主流程之间的契约。改文件名、字段名、字段含义或 Markdown 顶层标题顺序时，要同步更新生成逻辑、验证逻辑和文档。

## 修改规则

- 核心 CSV 文件名和表头由 `UIReportFiles` 维护。
- `UIReportValidationService` 负责核心 CSV batch 验证和 `UIReportFiles` 注册表契约自检。
- 写出 CSV 后优先立即复验表头和行结构。
- 皮肤 JSON、自动制作 dry-run 和宿主生成结果报告必须有可验收的 Markdown 汇总。
- 写出固定结构 Markdown 后立即校验顶层 `##` 标题顺序。
- JSON 入口在系统边界校验根对象、必需字段、字符串转义、字段分隔、重复字段和尾随内容。

## 核心扫描 CSV

| 文件 | 用途 |
| --- | --- |
| `UIAssetTriageReport.csv` | 每张图片的事实、当前归属、建议和原因。 |
| `UIAssetTriagePlan.csv` | 迁移计划草稿，只给建议，不执行。 |
| `UIReuseIndex.csv` | 复用图片索引。 |
| `UIPrefabOptimizationTargets.csv` | prefab 优化优先级排序。 |
| `UIPrefabAtlasStats.csv` | prefab 依赖图集、散图和大图数量。 |
| `UIPrefabImageDetails.csv` | prefab 依赖图片明细。 |
| `UIPrefabAtlasBreakdown.csv` | prefab 依赖图集拆解。 |
| `UIPrefabDrawCallRisk.csv` | prefab 级静态风险汇总。 |
| `UIPrefabBatchSequence.csv` | Graphic 顺序、纹理、材质和 batch key。 |
| `UIPrefabBatchBreaks.csv` | 相邻 batch key 变化点。 |
| `UIPrefabBatchBreakSummary.csv` | prefab 级断批汇总。 |
| `UIPrefabTextureSwitchPairs.csv` | 重复纹理切换 pair。 |
| `UIPrefabWhiteTextureBreaks.csv` | 白纹理相关断批汇总。 |
| `UIACommonUsage.csv` | 通用图集使用频次和建议。 |
| `UITextureSizeReport.csv` | 散图尺寸、引用和建议。 |
| `UIDuplicateImageReport.csv` | 同 SHA1 重复图。 |
| `UIPrefabNullSpriteImages.csv` | 可见启用空 Sprite Image。 |
| `UILooseTextureCandidates.csv` | prefab 直接引用 UITexture 散图候选。 |

## 按需报告

| 流程 | 产物 |
| --- | --- |
| 扫描摘要 | `UIAIToolsSummary.md`、`UIAIToolsPanelFocus.md` |
| 复用反查 | `UIReuseSearchResults.csv`、`UIReuseSearchSummary.md` |
| 新版换皮 | `Assets/UIAITools/Skinning/<UIName>/skin.json`、`Generated/detected-layout.json`、`Generated/asset-crops.json`、`Generated/skin-layout.json`、`Prefabs/<SourcePrefabName>_v2.prefab`、`UIAIToolsReports/Skinning/<UIName>/host-adapter-checklist.md`、`provided-crops-spec.md`、`provided-crops-check.md`、`binding-check.md`、`visual-check.md`、`review-package.md`、`apply-checklist.md`、`auto-build-notes.md` |
| 组件候选 | `UIComponentCandidateIndex.csv`、`UIComponentCandidateIndexSummary.md`、`UIComponentCandidateReview.csv` |
| 自动制作 UI | `UICreationLayoutDryRun.csv`、`UICreationLayoutDryRunSummary.md`、`UICreationHostGenerateChecklist.md` |
| 宿主草稿生成 | `UICreationHostGenerateResult.csv`、`UICreationHostGenerateResult.md` |
| 全局动态资源更新 | `Assets/UIAITools/GlobalRuntimeUpdates/<BatchName>/replace-map.csv`、`UIAIToolsReports/GlobalRuntimeUpdates/<BatchName>/checklist.md` |
| 宿主专项维护 | `UIAIToolsReports/GlobalRuntimeUpdates/UIVipcardSpriteBackfillReport.md`、`UIAIToolsReports/Skinning/UIVipcardTitleTextBindingCheck.md` |

这些按需报告不属于核心扫描 CSV 契约，但各自的生成和 validate 入口仍会校验结构。宿主 master plan 还会检查已存在真实 CSV 的表头和可读性，覆盖扫描核心 CSV、`UIReuseSearchResults.csv`、组件候选 CSV、layout dry-run、host generate result、skin `generate-result.csv` 和 global runtime `replace-map.csv`；新增真实 CSV 必须纳入该校验，防止旧实物报告在生成逻辑变化后静默漂移。

宿主 master plan 还会复读 `UIAIToolsReports/Creation` 下已存在的 Creation brief/layout template JSON；新增 Creation JSON 必须登记对应加载和校验逻辑，不能只写出文件不纳入契约。

扫描摘要不能成为孤岛：`UIAIToolsSummary.md` 和 `UIAIToolsPanelFocus.md` 应包含 `输入` 区，列出来源 CSV、`ValidateReports` 和可直接复用的重跑入口。

宿主 master plan 会校验现有非 skin Markdown 报告的章节顺序，覆盖扫描摘要、panel focus、复用反查摘要、组件候选摘要、layout dry-run 摘要、host generate checklist/result、global runtime checklist 和 UIVipcard 宿主专项报告。

复用反查应在 `UIReuseSearchResults.csv` 旁写出 `UIReuseSearchSummary.md`；摘要应包含查询图、结果 CSV、带引号的 `Re-run Search` 命令、建议分布、Top 结果和下一步，不移动图片、不改 prefab 或图集。

`provided-crops-spec.md` 是“目标效果图 + 已提供切图”换皮分支的交付清单，应按 `Inputs`、`Required Crops` 的顺序分段，并包含 `Id`、文件路径、源区域、参考矩形、`Expected Size` 和带引号的 `Validate Command`。`-uiInputImageFolder` 必须是 `Assets` 下的文件夹；切图交付清单入口允许目录尚未创建，方便先出交付规格；“切图交付清单”“验证已提供切图”“一键验收包”三条入口遇到误选 PNG 文件或 `Assets` 外路径时都必须阻断。

`provided-crops-check.md` 是同一批切图的体检报告，应按 `Inputs`、`Gate Summary`、`Crop Rows` 的顺序分段，并包含通过/阻断数量、带引号的 `Re-run Check` 命令、实际尺寸、`Expected Size`、状态和阻断原因；缺少 PNG、解码失败、尺寸过小或尺寸不等于 `Expected Size` 都必须阻断。验证、登记、草稿和一键验收入口遇到切图目录不存在、误选 PNG 文件或 `Assets` 外路径时，也要写出 `provided-crops-spec.md` 和 `Gate：Blocked` 的 `provided-crops-check.md`，列出必需文件名，方便补齐后重跑；输入路径本身非法时下一步应提示先修正 `-uiInputImageFolder`。

`host-adapter-checklist.md` 是新 UI 或非 UIVipcard 换皮前的宿主接入清单，应包含 `Gate`、源 prefab、建议工作包、报告目录、现有适配器、带引号的 `Re-run Checklist` 命令、prefab 结构快照、宿主适配工作项、切图命名建议和验证顺序。它只读源 prefab 并生成 Markdown，不生成 prefab、图片或正式资源。现有 `host-adapter-checklist.md` 会被换皮验证复查章节顺序和可追溯字段，避免接入清单在生成逻辑变化后静默漂移。

`review-package.md` 是 provided-crops 换皮分支的一键验收入口，应汇总 `Gate`、源 prefab、皮肤名、目标图、工作包、当前 `Input Folder`、带引号的重跑命令、切图交付清单、切图体检、视觉报告、绑定检查、package gate、草稿 prefab、最终预览、对比预览和人工验收顺序；成功态只有在 package gate 通过后写出，并应标记 `Package Gate: Passed`。一键验收包遇到缺目录、误选文件、`Assets` 外路径、切图体检阻断、草稿 prefab 已存在、复制源 prefab 失败、清理旧草稿失败、拒绝清理非生成 prefab、绑定阻断、目标图或预览图片损坏/解码失败、预览缺失、预览空白、预览过小或预览纯色等 package gate 问题时，仍要写出 `Gate：Blocked` 的 `review-package.md`，保留当次输入目录、`Skin Prefab` / `Final Preview` / `Comparison Preview` 路径和带引号的重跑命令，同时保留 batch 失败信号；误选文件或 `Assets` 外路径的修复顺序应先提示修正 `-uiInputImageFolder`，缺目录或切图体检阻断才引导查看 `provided-crops-check.md`，package gate 阻断则引导打开阻断原因指向的报告或资源。

`binding-check.md` 应包含 `Manifest`、`Skin Prefab` 和带引号的 `Re-run Binding` 命令；provided-crops 分支还应包含带引号的 `Review Package Command`。`visual-check.md` 的 provided-crops gate 应包含输入目录、交付清单、体检报告和带引号的 `Review Package Command`，方便从子报告回到一键验收入口。现有 provided-crops、visual、binding 和 apply 子报告会被换皮验证复查章节顺序、重跑入口与来源字段。

`auto-build-notes.md` 应包含 `Manifest` 路径和关键生成文件路径；宿主 `apply-checklist.md` 应包含 `Manifest`、草稿 prefab 路径和 provided-crops 分支回到一键验收入口的 `Review Package Command`。

`Generated/detected-layout.md` 是 `detected-layout.json` 的可读摘要，必须包含 `Manifest`、`Detected Layout JSON`、目标图尺寸和区域表。`Generated/asset-crops.md` 是 `asset-crops.json` 的可读摘要，必须包含 `Manifest`、`Asset Crops JSON`、`Rect` 和 `Size`，方便从自动裁剪或 provided-crops 分支登记后直接核对来源和实际宽高。`Generated/skin-layout.md` 是 `skin-layout.json` 的可读摘要，必须包含 `Manifest`、`Skin Layout JSON`、`Asset Crops JSON`、输出 prefab、布局计数和验证槽位。现有 generated summaries 被校验时也会复读对应 generated JSON 本体。

`Assets/UIAITools/Skinning` 下的 skin JSON 必须被 `skin.json` 或 manifest.generated 路径覆盖；新增 skin JSON 必须登记加载和校验逻辑，不能作为孤儿 JSON 留在工作包里。

运行时清理辅助报告也不能成为孤岛：`runtime-cleanup-mask-check.md` 应包含 `Manifest`、mask、overlay、prompt 路径和带引号的 `Re-run Check` / `Post Cleanup Checklist` 命令；`post-cleanup-rerun-checklist.md` 应包含 `Manifest` 和带引号的 `Re-run Checklist` / `Re-run Readiness` 命令；`post-cleanup-readiness.md` 应包含 `Manifest` 和带引号的 `Re-run Readiness` / `Re-run Checklist` 命令。

自动制作 UI 的 Markdown 汇总也要可接力：`UIComponentCandidateIndexSummary.md` 应包含 `Component Candidate Index CSV`、`Component Candidate Review CSV`、`Source Batch Sequence CSV` 和重跑/校验入口；`UICreationLayoutDryRunSummary.md` 应包含 `Layout Draft JSON`、dry-run CSV、组件候选 CSV、review CSV 和重跑/校验入口；`UICreationHostGenerateChecklist.md` 应包含 `Layout Draft JSON`、dry-run CSV、组件候选 review CSV，以及重跑 checklist / dry-run 的入口。

宿主草稿生成结果也要可接力：`UICreationHostGenerateResult.md` 应包含 `Layout Draft JSON`、目标 prefab、结果 CSV、host checklist、layout dry-run CSV、组件候选 review CSV，以及带引号的 `Re-run Generate` / `Re-run Validate` 命令。

全局动态资源更新 checklist 也不能成为孤岛：`checklist.md` 应包含 batch 名、工作包、`replace-map.csv`、`Images` 目录，以及带引号的 `Re-run Template` / `Re-run Validate` 命令；该流程只准备清单和输入，不直接写正式 `Assets/Bundle`、SpriteAtlas 或 YooAsset。

宿主专项维护报告也要可接力：`UIVipcardSpriteBackfillReport.md` 应包含目标 prefab、相关 prefab、报告路径、带引号的 `Re-run Report` / `Re-run Validate` 命令、空 Sprite 列表和人工确认后的 apply 提示；报告和校验入口不应修改 prefab。

`UIVipcardTitleTextBindingCheck.md` 是 UIVipcard 标题文字绑定布局专项报告，应按 `Inputs`、`Title Rows` 的顺序分段，并包含源 prefab、报告路径、带引号的 `Re-run Check` 命令、检查/阻断数量、标题文本节点、父节点、锚点、位置、尺寸、背景 Image 状态和每行状态；报告契约必须覆盖缺章和乱序负例。

## JSON 规则

- 根对象后不允许尾随内容。
- 契约字段不允许重复。
- 字符串必须是完整字面量，转义合法，不包含未转义控制字符。
- 布尔和整数必须是完整字面量；整数只接受 ASCII 数字且不允许前导零。
- 对象字段和数组项必须用逗号分隔。
- 必需数组字段必须存在，即使为空数组。

覆盖对象包括 `UISkinManifest`、`UISkinDetectedLayout`、`UISkinAssetCrops`、`UISkinLayout`、`UICreationBrief` 和 `UILayoutDraft`。

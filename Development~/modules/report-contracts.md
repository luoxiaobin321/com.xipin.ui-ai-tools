# 报告契约

CSV、JSON 和 Markdown 报告是包与宿主流程之间的契约。改文件名、字段名、字段含义或 Markdown 顶层标题顺序时，要同步更新生成逻辑、验证逻辑和相关文档。

## 修改规则

- 核心 CSV 文件名和表头由 `UIReportFiles` 维护。
- `UIReportValidationService` 负责核心 CSV batch 验证和 `UIReportFiles` 注册表契约自检。
- 写出 CSV 后优先立即复验表头和行结构。
- 宿主执行结果和宿主生成结果报告必须至少包含一行结果。
- 宿主执行结果报告应能和当前 `UIReplacementExecutionPlan.csv` 按 `ItemIndex`、`Action`、资源路径、图集和 prefab 引用匹配。
- 宿主生成结果报告必须指向同一个目标 prefab；基础 Markdown 汇总可从结果 CSV 生成。
- 写出固定结构 Markdown 后立即校验顶层 `##` 标题顺序。
- JSON 入口在系统边界校验根对象、必需字段、字符串转义、字段分隔、重复字段和尾随内容。

## 核心扫描 CSV

| 文件 | 用途 |
| --- | --- |
| `UIAssetTriageReport.csv` | 每张图片的事实、当前归属、建议和原因。 |
| `UIAssetTriagePlan.csv` | 迁移计划草稿，只给建议，不执行。 |
| `UIReuseIndex.csv` | 复用图片索引，供人工和 AI 查询。 |
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
| 复用反查 | `UIReuseSearchResults.csv` |
| AI 改版 Brief | `UIRedesignBrief_*.md` |
| AI 草稿与 dry-run | `UIRedesignDraft_*.json`、`UIReplacementPlanDryRun.csv`、`UIReplacementPlanDryRunSummary.md` |
| 执行计划 | `UIReplacementExecutionPlan.csv`、`UIReplacementExecutionPlanSummary.md` |
| 待补输入 | `UIReplacementPendingInputs.csv`、`UIReplacementPendingInputsSummary.md`、`UIReplacementPendingInputReadiness.csv`、`UIReplacementPendingInputReadinessSummary.md` |
| 外部生成输入包 | `UIReplacementExternalInputPackage.json`、`UIReplacementExternalInputPackage.md`、`UIReplacementExternalGenerationTasks.md`、`UIReplacementExternalPromptPack.md`、`UIReplacementExternalPromptItems.md`、`UIReplacementExternalPrompt_*.md`、`UIReplacementExternalReferenceCopyList.md` |
| 宿主执行 | `UIReplacementHostApplyChecklist.md`、`UIReplacementHostApplyResult.csv`、`UIReplacementHostApplyResult.md` |
| 改版包 manifest | `UIRedesignPackage_*.md` |
| 组件候选 | `UIComponentCandidateIndex.csv`、`UIComponentCandidateIndexSummary.md`、`UIComponentCandidateReview.csv` |
| 新 UI 制作 | `UICreationLayoutDryRun.csv`、`UICreationLayoutDryRunSummary.md`、`UICreationHostGenerateChecklist.md` |
| 宿主生成器样例 | `UICreationHostGenerateResult.csv`、`UICreationHostGenerateResult.md` |

这些按需报告不属于核心扫描 CSV 契约，但各自的生成和 validate 入口仍会校验结构。

## Markdown 标题契约

固定结构 Markdown 只校验顶层 `##` 标题。缺失、错序或额外标题都会阻断，并在错误里指出报告名；额外或错序标题会带行号。共享汇总 helper 也用 contract 锁定检查项、severity/check 和备注前缀分布顺序。

当前覆盖：

- 扫描摘要、面板实测清单。
- 改版 Brief、dry-run 汇总、执行计划汇总。
- 待补输入汇总、待补输入就绪汇总。
- 外部输入包汇总、任务清单、Prompt Pack、单项 Prompt 索引、单项 Prompt、引用素材清单。
- 宿主执行前清单、改版包 manifest。
- 宿主执行结果汇总。
- 组件候选汇总。
- 新 UI layout dry-run 汇总、宿主生成前清单、宿主生成结果汇总。

## CSV 规则

- CSV 使用 UTF-8 BOM，兼容 Excel。
- 路径统一使用 Unity 资产路径格式 `/`。
- 表头不能为空，列名不能为空且不能重复。
- 每行列数必须和表头一致。
- 未闭合引号、非字段开头引号和引号后追加文本都直接报带文件行号的异常。
- `ValidateReportFilesContractBatch` 覆盖 CoreReports 不重复、表头字典无陈旧项、每个核心 CSV 都有表头、表头列不为空且不重复，并锁定 `UIReportFiles.GetPath` 的 `/` 输出。
- `ValidateCsvContractBatch` 覆盖正向样例、重复表头、空表头、列数不一致和引号错误。

## JSON 规则

JSON 入口只在系统边界做严格校验：

- 根对象后不允许尾随内容。
- 契约字段不允许重复；根对象、嵌套对象和数组对象项都按同一规则检查。
- 字符串必须是完整字面量，转义合法，不包含未转义控制字符。
- 布尔和整数必须是完整字面量；整数只接受 ASCII 数字且不允许前导零。
- 对象字段和数组项必须用逗号分隔，不能前置逗号、重复逗号或尾逗号。
- 必需数组字段必须存在，即使为空数组。

覆盖对象包括 `UICreationBrief`、`UILayoutDraft`、`UIRedesignDraft` 和 `UIReplacementExternalInputPackage.json`。

## 字段约定

- `Advice` 写行动建议，保持短句。
- `Reason` 写建议依据，便于人工判断。
- `Owner` 表示依据路径推断的功能归属。
- `Score` 在复用反查里越低越相似。
- `Status`、`Severity`、`Action` 等枚举字段新增值时，同步更新排序、分布统计、gate 和文档。

## 关键依赖

- 源 prefab 校验读取 `UIPrefabOptimizationTargets.csv` 前先复验优化目标报告。
- 执行计划汇总读取 `UIReuseIndex.csv` 前先复验复用索引。
- 从执行计划派生的待补输入和宿主执行清单先复验执行计划。
- 外部输入包生成前先复验待补输入就绪链路和 `UIReuseIndex.csv`。
- 改版包 manifest 生成或验证前先复验 dry-run、执行计划和待补输入就绪 CSV。

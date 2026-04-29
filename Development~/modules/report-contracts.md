# CSV 报告契约

CSV 报告是包和外部分析流程之间的主要契约。改文件名、字段名或字段含义时，要同步更新对外文档和使用这些报告的脚本。

`UIReportFiles` 维护核心报告文件名和表头清单，`UIReportValidationService` 用它做 batchmode 检查，检查精确表头后会复用 `UIReportCsv` 解析所有数据行。新增、移除或改字段时要同步更新 `UIReportFiles.CoreReports` 和 `UIReportFiles.CoreReportHeaders`。

`UIAIToolsSummary.md`、`UIAIToolsPanelFocus.md`、`UIRedesignBrief_*.md`、`UIReplacementPlanDryRun.csv`、`UIReplacementPlanDryRunSummary.md`、`UIReplacementExecutionPlan.csv`、`UIReplacementExecutionPlanSummary.md`、`UIReplacementPendingInputs.csv`、`UIReplacementPendingInputsSummary.md`、`UIReplacementPendingInputReadiness.csv`、`UIReplacementPendingInputReadinessSummary.md`、`UIReplacementExternalInputPackage.json`、`UIReplacementExternalInputPackage.md`、`UIReplacementExternalGenerationTasks.md`、`UIReplacementExternalPromptPack.md`、`UIReplacementExternalPromptItems.md`、`UIReplacementExternalPrompt_*.md`、`UIReplacementExternalReferenceCopyList.md`、`UIReplacementHostApplyChecklist.md`、`UIRedesignPackage_*.md`、`UIComponentCandidateIndexSummary.md`、`UICreationLayoutDryRunSummary.md`、`UICreationHostGenerateChecklist.md` 和宿主生成器样例的 `UICreationHostGenerateResult.md` 是按需生成报告，不属于核心扫描 CSV 契约。

## 核心报告

| 文件 | 用途 |
| --- | --- |
| `UIAssetTriageReport.csv` | 每张图片的事实、当前归属、建议和原因。 |
| `UIAssetTriagePlan.csv` | 迁移计划草稿，只给建议，不执行。 |
| `UIReuseIndex.csv` | 复用图片索引，供人工和 AI 查询。 |
| `UIPrefabOptimizationTargets.csv` | prefab 优化优先级排序。 |

## prefab 与图集报告

| 文件 | 用途 |
| --- | --- |
| `UIPrefabAtlasStats.csv` | prefab 依赖图集、散图和大图数量。 |
| `UIPrefabImageDetails.csv` | prefab 依赖图片明细。 |
| `UIPrefabAtlasBreakdown.csv` | prefab 依赖图集拆解。 |

## DrawCall 风险报告

| 文件 | 用途 |
| --- | --- |
| `UIPrefabDrawCallRisk.csv` | prefab 级静态风险汇总。 |
| `UIPrefabBatchSequence.csv` | Graphic 顺序、纹理、材质和 batch key。 |
| `UIPrefabBatchBreaks.csv` | 相邻 batch key 变化点。 |
| `UIPrefabBatchBreakSummary.csv` | prefab 级断批汇总。 |
| `UIPrefabTextureSwitchPairs.csv` | 重复纹理切换 pair。 |
| `UIPrefabWhiteTextureBreaks.csv` | 白纹理相关断批汇总。 |

## 专项报告

| 文件 | 用途 |
| --- | --- |
| `UIACommonUsage.csv` | 通用图集使用频次和建议。 |
| `UITextureSizeReport.csv` | 散图尺寸、引用和建议。 |
| `UIDuplicateImageReport.csv` | 同 SHA1 重复图。 |
| `UIPrefabNullSpriteImages.csv` | 可见启用空 Sprite Image。 |
| `UILooseTextureCandidates.csv` | prefab 直接引用 UITexture 散图候选。 |
| `UIReuseSearchResults.csv` | 裁剪图反查已有图片结果。 |
| `UIRedesignBrief_*.md` | AI 改版 Brief，整理 request、当前扫描结论、图片/图集/复用风险和输出约束。 |
| `UIReplacementPlanDryRun.csv` | AI 替换计划 dry-run 检查结果，只报告不执行。 |
| `UIReplacementPlanDryRunSummary.md` | dry-run 人工阅读汇总，含 gate、警告项分布和复核项分布。 |
| `UIReplacementExecutionPlan.csv` | AI 替换计划的待确认执行步骤，只报告不执行。 |
| `UIReplacementExecutionPlanSummary.md` | 待确认执行计划人工阅读汇总，含 dry-run 检查分布、gate、待补输入、目标图集新图数量和复核项分布。 |
| `UIReplacementPendingInputs.csv` | 从执行计划导出的待补新版预览、新图和目标图集清单，只报告不补图。 |
| `UIReplacementPendingInputsSummary.md` | 待补输入人工阅读汇总，含输入类型和来源动作分布。 |
| `UIReplacementPendingInputReadiness.csv` | 待补输入按当前文件系统和 PNG 可读性检查后的 Ready/Missing/Invalid 清单，Preview/NewAsset Ready 时记录实际宽高。 |
| `UIReplacementPendingInputReadinessSummary.md` | 待补输入就绪汇总，含 Ready/Missing/Invalid 分布、外部落位验收清单、缺失项、格式异常项、已就绪项和实际尺寸。 |
| `UIReplacementExternalInputPackage.json` | 外部生成工具输入包，整理旧版预览、输出目录及未就绪/缺失/异常计数、期望输出尺寸、实际尺寸、尺寸状态、任务级验收规则、去重参考输入、引用素材导入名、任务级引用导入名、输出路径、参考图、尺寸和 prompt。 |
| `UIReplacementExternalInputPackage.md` | 外部生成输入包人工阅读汇总，含尺寸状态分布、输出目录未就绪/缺失/异常计数、待落位目录分组、单项 Prompt 汇总和落位后复跑步骤。 |
| `UIReplacementExternalGenerationTasks.md` | 外部生成任务清单，按未就绪项列出可执行任务、尺寸状态分布、输出目录、待落位目录分组、参考输入和单项 Prompt 链接。 |
| `UIReplacementExternalPromptPack.md` | 可投喂外部生成器的 prompt 块集合，含尺寸状态分布、输出目录、目录准备清单和参考输入。 |
| `UIReplacementExternalPromptItems.md` | 未就绪输入到单项 Prompt 文件的索引，含按输出目录分组的索引。 |
| `UIReplacementExternalPrompt_*.md` | 单个未就绪输入的外部生成 prompt 上下文，含验收规则和落位后复跑步骤。 |
| `UIReplacementExternalReferenceCopyList.md` | 外部生成引用素材清单，列出去重后的旧版预览、旧图引用、稳定导入名、导入步骤和引用来源目录分组。 |
| `UIReplacementHostApplyChecklist.md` | 宿主执行前清单，把待补输入、复核项分布、阻断项、非阻断人工确认项、prefab 替换候选和执行后验证拆开。 |
| `UIComponentCandidateIndex.csv` | UI 组件候选索引，从 `UIPrefabBatchSequence.csv` 和 `UIControlCatalog` 推导。 |
| `UIComponentCandidateIndexSummary.md` | UI 组件候选索引汇总，含 Button 复核队列，给自动制作 UI 的组件库建设使用。 |
| `UIComponentCandidateReview.csv` | UI 组件候选人工确认清单，给宿主填写组件 prefab、预览图、状态和备注；重新生成时按 `ComponentId` 保留人工填写列。 |
| `UICreationLayoutDryRun.csv` | 自动制作 UI 的 prefab 生成前布局 dry-run，只报告不生成 prefab。 |
| `UICreationLayoutDryRunSummary.md` | 布局 dry-run 人工阅读汇总，含 gate、severity 分布、status 分布和阻断项。 |
| `UICreationHostGenerateChecklist.md` | 宿主 UI prefab 草稿生成前确认清单，只整理 gate、当前 dry-run 目标和组件列表匹配、布局引用组件确认状态、人工确认和宿主执行边界。 |
| `UICreationHostGenerateResult.md` | 宿主 UI prefab 草稿生成结果汇总，记录目标 prefab、节点数、CSV、预览检查、深度验证、状态分布和下一步；该文件由宿主样例生成，不属于包内输出。 |
| `UIRedesignPackage_*.md` | AI 改版包 manifest，记录请求、草稿 JSON 快照、外部输入草稿 JSON、产物、dry-run 检查分布、状态、gate、待补输入、目标图集新图数量、复核项分布和下一步。 |

## 按需 Markdown 结构

- `UIReplacementPlanDryRunSummary.md`：结果分布、Gate 状态、警告项分布、复核项分布、阻断项、警告项、复核项和建议处理。
- `UIAIToolsSummary.md`：图片归类分布、UITexture 尺寸分布、直挂散图候选、复用索引建议、优化目标 Top、相邻断批原因、相邻断批建议、空 Sprite Image、空 Sprite 待确认样例和建议下一步。
- `UIAIToolsPanelFocus.md`：当前 `UIPrefabOptimizationTargets.csv` Top 面板短路径，按优先级顺序动态生成。
- `UIRedesignBrief_*.md`：Request Seed、当前扫描结论、断批建议、纹理切换、图片归属、图集拆解、直挂 UITexture、复用与归属风险、空 Sprite Image、AI 输出约束和 AI 输出格式。
- `UIReplacementExecutionPlanSummary.md`：范围、DryRun 分布、DryRun 检查分布、Gate 状态、执行步骤状态、DryRun 阻断项、待补预览和资源、待生成新图清单、待确认目标图集、复核项分布、待复核步骤和人工确认后。
- `UIReplacementPendingInputsSummary.md`：总览、新版预览、新图和目标图集。
- `UIReplacementPendingInputReadinessSummary.md`：Gate 状态、类型分布、外部落位验收、缺失项、格式异常项和已就绪项。
- `UIReplacementExternalInputPackage.md`：总览、尺寸状态分布、输出目录、目录准备清单、待落位目录分组、参考素材、参考输入、单项 Prompt 文件、外部产物落位后复跑、按类型列出的输入。
- `UIReplacementExternalGenerationTasks.md`：总览、尺寸状态分布、输出目录、目录准备清单、待落位目录分组、参考输入、外部产物落位后复跑、未就绪任务和已就绪输入。
- `UIReplacementExternalPromptPack.md`：全局上下文、全局约束、尺寸状态分布、输出目录、目录准备清单、参考输入和 Prompt Blocks。
- `UIReplacementExternalPromptItems.md`：总览、按输出目录分组的目录索引和单项 Prompt 文件清单。
- `UIReplacementExternalPrompt_*.md`：全局上下文、任务、外部产物验收和外部产物落位后复跑。
- `UIReplacementExternalReferenceCopyList.md`：总览、导入步骤、引用来源目录分组和复制清单。
- `UIReplacementHostApplyChecklist.md`：Gate 状态、待补输入、复核项分布、阻断项、执行前人工确认、Prefab 替换候选和执行后验证。
- `UIRedesignPackage_*.md`：Request、产物、DryRun 分布、DryRun 检查分布、执行计划状态、Gate 状态、待补输入、复核项分布和下一步。
- `UIComponentCandidateIndexSummary.md`：角色分布、高频候选、Button 复核队列和使用方式。
- `UICreationLayoutDryRunSummary.md`：Severity 分布、Status 分布和阻断项。
- `UICreationHostGenerateChecklist.md`：目标、阻断项、人工复核项、组件候选确认、宿主生成前确认、宿主生成器允许动作、宿主生成器禁止动作和生成后验证。
- `UICreationHostGenerateResult.md`：目标、状态分布和下一步。

上述 Markdown 会按对应顺序精确检查顶层 `##` 标题；生成服务会在写出后立即校验，validate 入口会复验。缺失、错序或额外标题都会阻断，并在错误里指出具体报告名，额外或错序标题会带行号。

`UIComponentCandidateIndexService.Generate` 写出候选索引 CSV 后会立即复验，再生成汇总和人工确认清单；确认清单写出后也会立即复验，最后复用 `Validate`。`Validate` 是自动制作 UI 的按需 gate，会检查候选索引 CSV 表头、候选数量、`ComponentId` 格式与唯一性、汇总文件、人工确认清单表头、确认清单行数、ID、扫描派生列、复核分层和决策值；`Approved` 行必须填写 `ComponentPrefabPath`。生成确认清单时只保留 `SuggestedDecision`、`ComponentPrefabPath`、`PreviewPath`、`States`、`UsageNotes`、`Reviewer` 和 `ReviewNotes` 这些人工填写列，其余字段来自当前候选索引。它不属于核心扫描 CSV 验证。

## 字段约定

- `Advice` 写行动建议，保持短句。
- `Reason` 写建议依据，便于人工判断。
- `Owner` 表示依据路径推断的功能归属。
- `Score` 在复用反查里越低越相似。
- 路径统一使用 Unity 资产路径格式 `/`。
- CSV 使用 UTF-8 BOM，兼容 Excel。
- CSV 读取和 `ValidateReport` 会检查文件存在、表头非空、表头列名非空且不重复、每行列数必须和表头一致；未闭合引号、非字段开头引号和引号后追加文本都会直接报带文件行号的异常。`ValidateCsvContractBatch` 覆盖正向样例、重复表头、空表头、列数不一致、未闭合引号、非字段开头引号和引号后追加文本。`UIReplacementPlanDryRun.csv`、`UIReplacementExecutionPlan.csv`、`UIReplacementPendingInputs.csv`、`UIReplacementPendingInputReadiness.csv`、`UICreationLayoutDryRun.csv` 和 `UIReuseSearchResults.csv` 写出后会立即复验表头和行结构。
- JSON 读取会先检查根对象、尾随内容和必需数组字段。`UICreationBrief` 的根字符串字段、可选 `requiresConfirmation` 布尔字段和字符串数组项会先校验。`UILayoutDraft` 的 `root` 内部字段、`interactions`/`risks` 字符串数组项、`nodes`/`assets` 数组项关键字段会先校验。`UIRedesignDraft` 的 `risks` 字符串数组项、`replacementPlan.items` 路径字段、确认字段和 `preserveGuid` 会分别按字符串或完整布尔字面量校验。`UIReplacementExternalInputPackage.json` 的生成后读回和 validate 入口也会检查根字符串字段、`outputDirectories`、`referenceInputs`、`inputs` 数组字段、数组项字符串/整数字段和尾随内容；对象字段里的字符串、布尔、整数、对象和数组值后面只能接逗号或对象结束，字符串转义必须合法且不能包含未转义控制字符，整数只接受 ASCII 数字且不允许前导零，已声明的契约字段不允许重复，对象字段和数组项必须用逗号分隔且不能前置、重复或尾逗号。`ValidateJsonContractBatch` 覆盖空字符串字段后的扫描、creation brief/layout draft/redesign draft/external input package 空数组保留、尾随内容阻断、根字段缺失阻断、字符串字段类型、非法转义和尾随 token 阻断、布尔字段类型阻断、整数字段类型、非 ASCII 数字与前导零阻断、重复契约字段阻断、缺失数组字段阻断、对象字段类型、对象字段分隔和尾随 token 阻断、数组字段类型和尾随 token 阻断、数组项字段类型阻断、数组分隔阻断和 `replacementPlan.items` 阻断。
- 扫描入口写完核心 CSV 后会立即复用 batchmode 验证；验证会检查核心报告存在、非空、第一行表头完全匹配且数据行能按 CSV 契约解析。
- 源 prefab 校验读取 `UIPrefabOptimizationTargets.csv` 前会先复验优化目标报告精确表头和行结构，覆盖基准图、Brief、草稿模板和草稿保存入口。
- `UIReplacementPlanDryRunService.GenerateSummary` 和 `ValidateNoErrors` 会先复验 dry-run 精确表头；`ValidateNoErrors` 还会复验汇总标题结构，再判断 Error gate。
- `UIReplacementExecutionPlanService.GenerateFromCurrentDryRun` 读取 `UIReplacementPlanDryRun.csv` 前会先复验 dry-run 精确表头和行结构；生成执行计划汇总读取 `UIReuseIndex.csv` 前会先复验复用索引精确表头和行结构。
- `UIReplacementExecutionPlanService.ValidateNoBlockingStatuses` 会先复验执行计划精确表头和执行计划汇总标题结构，再判断阻断状态。
- 从 `UIReplacementExecutionPlan.csv` 派生的待补输入清单和宿主执行清单会先复验执行计划精确表头和行结构，再生成或验证自身报告。
- `UICreationLayoutDryRunService.ValidateNoErrors` 会先复验 layout dry-run 精确表头和行结构，再判断 Error gate。
- `UICreationHostGenerateChecklistService` 读取 `UICreationLayoutDryRun.csv` 前会先复验 layout dry-run 精确表头和行结构，再生成或验证宿主生成前确认清单。
- `UIReplacementExternalInputPackageService` 读取 `UIReuseIndex.csv` 前会先复验复用索引精确表头和行结构；`Generate` 写出外部输入 JSON、汇总、任务清单、Prompt Pack、单项 Prompt 索引、单项 Prompt 和引用素材清单后会立即复用完整验证。
- `UIRedesignPackageService` 生成或验证 manifest 读取 `UIReplacementPlanDryRun.csv`、`UIReplacementExecutionPlan.csv` 和 `UIReplacementPendingInputReadiness.csv` 前会先复验精确表头和行结构；`Prepare` 写出 manifest 后会立即复用完整 manifest 验证，覆盖固定产物路径、外部输入包验证、分布和 gate 计数。

# AI 扩展契约

AI 相关代码目前是协议层，目标是把“生成建议”和“执行资源改动”分开。

## Provider 边界

`IUIAIGenerationProvider` 只负责根据 `UIRedesignRequest` 返回 `UIRedesignDraft`。它不应该直接修改 prefab、移动资源、覆盖图片或改 SpriteAtlas。

`UIRedesignPackageService` 只串联 Brief、草稿模板或 provider 草稿 JSON、dry-run、Error gate、待确认执行计划、待补输入、就绪检查、外部生成输入包、宿主执行清单和 manifest，不调用 AI，不执行替换；manifest 生成和验证读取 dry-run、执行计划和待补输入就绪 CSV 前会先复验精确表头和行结构，manifest 生成后会立即复用完整 manifest 验证。manifest 验证会复用外部输入包验证，覆盖目录分组、Prompt、引用素材清单和外部输入包 Markdown 标题顺序，并检查 manifest 顶层 Markdown 标题结构与固定产物路径。
`UIPrefabBaselineScreenshotService` 只把已扫描的旧 prefab 临时实例化到标准 Canvas/Camera 环境并输出 `Logs/UIPrefabBaseline_*.png`，不保存场景、不修改 prefab 或资源；源 prefab 校验读取 `UIPrefabOptimizationTargets.csv` 前会先复验精确表头和行结构。
`UIRedesignBriefService` 负责把 request 和已有扫描报告整理成 Markdown Brief，并在生成时校验顶层标题结构；`sourcePreviewPath` 为空时会先生成旧版基准图。它是 provider 前置输入，不调用 AI，也不产生资源改动。
`UIRedesignBriefWindow` 只负责手动填写 request 字段并调用 Brief 服务，不保存配置、不执行替换。
Brief 可以包含复用风险、`UIRedesignDraft` 输出格式和执行前 gate 提示，但这些内容仍是草稿输入，不是执行结果。
`UIRedesignDraftTemplateService` 可以根据扫描报告生成草稿 JSON 模板，模板中的替换项只是候选项；同名旧图会生成唯一 `newAssetPath`，包含 `_2` 后缀碰撞场景；模板写盘会保留 `replacementPlan.items` 和 `risks` 数组字段，并立即复用 `LoadDraft` 读回校验。
provider 返回实际 `UIRedesignDraft` 后，`UIRedesignDraftService.SaveDraft` 只负责落盘经过 request 与草稿校验且强制人工确认的草稿 JSON；写盘会保留必需数组字段并立即读回校验。`UIRedesignPackageService.Prepare(profile, request, draft)` 只用该草稿继续生成 dry-run、执行计划、待补输入、就绪检查、外部生成输入包、宿主执行清单和 manifest。AI 或人工已经写好草稿 JSON 时，用 `UIRedesignPackageService.PrepareFromDraftJson` 走同一套报告闭环，并在 manifest 记录原输入 JSON 与包内快照 JSON。
外部 JSON 的包内快照使用 `UIRedesignDraft_*_Snapshot.json`，如果输入路径已经是该快照名，会继续使用 `Snapshot2` 等后缀，避免覆盖输入草稿。

## Request 稳定字段

- `sourcePrefabPath`：必须已经出现在当前扫描报告里。
- `sourcePreviewPath`：为空时 Brief 入口会生成旧版基准图，非空时必须是图片路径。
- `stylePrompt`
- `inputImageFolder`：非空时必须是 `Assets/...` 路径。
- `reuseCandidateReportPath`
- `outputFolder`：非空时必须是 `Assets/...` 路径。
- `referenceImagePaths`：参考图列表，必须是 `Assets/...` 下的图片源文件。

新增字段前先确认已有 provider 不能从这些字段推导。
宿主 batch 入口也应复用这组字段，不另建一套命令行专用 DTO。
`UIRedesignRequestValidation` 负责 request 边界字段校验；当前 Brief、草稿模板和 `SaveDraft` 都会用它阻断不是 `Assets/*.prefab` 或未出现在当前扫描报告里的 `sourcePrefabPath`，Brief 和草稿模板还会用它阻断非法 `sourcePreviewPath`、`outputFolder`、`inputImageFolder` 和参考图路径。`ValidateRedesignPackageContractBatch` 覆盖 source prefab 资源路径、扫描报告边界、sourcePreviewPath 图片路径边界、outputFolder/inputImageFolder 路径边界和 referenceImagePaths 路径边界。

## Draft 稳定字段

- `draftPreviewPath`
- `generatedImageFolder`
- `replacementPlan`：必须包含 `items`。
- `requiresConfirmation`
- `risks`：必须存在，可为空数组。

`UIRedesignDraftService` 会把 `requiresConfirmation` 固定为 `true`，因为 AI 草稿不能跳过人工验收。
`UIRedesignDraftService.SaveDraft` 写出的 JSON 也会先强制 `requiresConfirmation = true`，避免 provider 通过文件绕过确认。

## 确认窗口

`UIRedesignDraftWindow` 只读展示 `UIRedesignDraft`，用于人工确认新版预览、生成目录、风险项和 `UIReplacementPlan`。窗口不移动资源、不覆盖 prefab、不修改图集。
`UIRedesignDraftService.SaveDraft` 和 `UIRedesignDraftService.LoadDraft` 复用同一套草稿校验。`LoadDraft` 会先检查原始 JSON 根对象后没有尾随内容，`draftPreviewPath`、`generatedImageFolder` 必须是完整字符串字面量且转义合法，`requiresConfirmation` 如果存在必须是完整布尔字面量，`risks` 必须是字符串数组，`replacementPlan` 对象内的 `items` 必须是数组，草稿契约字段不允许重复，替换项的 `oldAssetPath`、`newAssetPath`、`targetAtlasPath` 必须是完整字符串字面量且转义合法，`preserveGuid` 和 `requiresConfirmation` 如果存在必须是完整布尔字面量，`reason` 如果存在必须是字符串，再校验替换项必填 `Assets/...` 路径、不得包含 `..` 路径段、预览和新图为 `.png`、新图位于生成目录下、`.spriteatlasv2` 目标图集以及重复旧资源或新资源路径，最后强制保持人工确认。
`UIReplacementPlanDryRunService` 用于读取草稿 JSON 并输出检查报告，不执行替换；读回 dry-run CSV 时会复验旧图、新图和目标图集路径。
`UIReplacementPlanDryRunService.ValidateNoErrors` 会先复验 dry-run CSV 精确表头和汇总标题结构，再只把 Error 作为 batch 阻断，Warning 和 Review 保留给人工确认。
`UIReplacementExecutionPlanService` 用于把草稿和当前 dry-run 结果展开成待确认执行计划；读取 dry-run 前会先复验 dry-run CSV 精确表头，生成汇总读取 `UIReuseIndex.csv` 前会先复验复用索引表头和行结构，读回执行计划时会校验资源路径后缀。它只输出 CSV 和 Markdown 汇总，不执行资源迁移、prefab 覆盖、图集修改或 YooAsset 配置修改。
`UIReplacementExecutionPlanService.ValidateNoBlockingStatuses` 会先复验执行计划 CSV 精确表头和汇总 Markdown 顶层标题结构，再阻断 `Blocked`、`PendingPreview`、`PendingAsset` 和 `PendingAtlas`，用于新版预览、新图和目标图集补齐后的 batch gate；`NeedsReview` 仍由人工确认流程处理。
`UIReplacementPendingInputChecklistService` 把执行计划里的待补新版预览、新图和目标图集导出成外部可消费的 CSV/Markdown；读取执行计划前会先复验表头和行结构，待补输入 CSV 写出后会立即复验，并在 contract 中覆盖路径、后缀、数量和 item index 边界，只写报告。
`UIReplacementPendingInputReadinessService` 只按当前文件系统检查待补输入路径状态，并对 Preview/NewAsset 做 PNG 可读性检查；`ValidateNoMissing` 会阻断 Missing 或 Invalid，contract 覆盖路径、后缀、数量、item index 和实际尺寸字段边界，不生成图片、不创建图集。
`UIReplacementExternalInputPackageService` 只整理外部生成工具需要的 JSON、汇总、任务清单、Prompt Pack、单项 Prompt 文件和引用素材清单；生成和验证入口会先复验待补输入就绪链路和 `UIReuseIndex.csv` 精确表头与行结构。JSON 写盘会保留 `outputDirectories`、`referenceInputs` 和 `inputs` 数组字段并立即读回校验，读回时也会检查根字符串字段、字符串转义、数组项字符串/整数字段、字段值字面量结束、对象/数组字段值结束、对象字段分隔、整数 ASCII 数字和前导零、重复契约字段和尾随内容；生成和验证时会校验外部 Markdown 顶层标题结构，同时校验旧版预览、旧版预览尺寸、`stylePrompt`、输出目录、期望输出尺寸、readiness 实际尺寸、尺寸状态、Ready 输出 PNG 尺寸、任务级验收规则、目录准备清单、待落位目录分组、去重参考输入、任务级引用导入名、参考图、参考尺寸、引用素材导入名、引用来源目录分组、单项 Prompt 目录索引、落位后复跑步骤和 prompt 派生字段，不调用 AI、不写资源、不创建目录。
`UIReplacementHostApplyChecklistService` 用于把当前执行计划整理成宿主执行前 Markdown 清单，并提供阻断步骤 gate；生成和验证会复验执行计划表头、Markdown 标题、gate 计数、待补输入、复核分布、阻断项、人工确认项、prefab 替换候选和执行后验证行。它不执行资源迁移、prefab 覆盖、图集修改或 YooAsset 配置修改。
`UIReplacementHostApplyResultService` 用于校验宿主执行器输出的 `UIReplacementHostApplyResult.csv/md`，并可从 CSV 生成结果 Markdown；它只检查精确表头、非空结果行、基础字段、状态、资源路径后缀、确认记录、失败说明、Markdown 顶层标题、状态分布、执行后复验清单和当前执行计划匹配，不执行资源改动。
执行计划状态排序、阻断状态和 dry-run severity 排序统一放在 `UIReplacementPlanStatus`；`ValidateReplacementPlanStatusContractBatch` 覆盖这些状态口径，`UIReportMarkdown` 只保留 Markdown 汇总输出。

## ReplacementPlan 语义

`UIReplacementItem` 描述“建议替换”，不是“已经替换”。

- `oldAssetPath` 是被替换的旧资源。
- `newAssetPath` 是候选新资源。
- `targetAtlasPath` 是建议进入的目标图集。
- `preserveGuid` 表示是否建议保持旧 GUID。
- `requiresConfirmation` 表示该项是否需要人工确认，服务层会固定为 `true`。
- `reason` 记录替换依据。

## 执行前检查

宿主项目执行替换前，应检查新版预览、YooAsset 地址重名、动态按名加载风险、目标图集 pack 关系、prefab 引用、GUID 策略和人工确认结果。
dry-run 只覆盖这些检查的可静态判断部分，不能替代人工确认和最终执行前检查。
执行计划只是把这些检查后的候选动作列清楚，仍必须由宿主确认流程决定是否真正执行。
宿主执行清单只是把执行计划拆成待补输入、复核项分布、阻断项、非阻断人工确认项、prefab 替换候选和执行后验证，仍不代表资源改动已获批准。
宿主确认后执行器可以按 `Documentation~/modules/host-apply-executor.md` 实现，但必须留在宿主项目，不能移进 UPM 包。

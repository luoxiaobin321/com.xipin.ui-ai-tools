# AI 改版草稿模块

AI 改版模块目前只定义协议和草稿服务，不内置具体 AI provider，也不自动覆盖项目资源。

## 核心入口

```csharp
var draft = UIRedesignDraftService.CreateDraft(request, provider);
var jsonPath = UIRedesignDraftService.SaveDraft(profile, request, draft);
UIRedesignDraftWindow.ShowDraft(draft);
```

准备完整 AI 改版包：

```csharp
UIRedesignPackageService.Prepare(profile, request);
UIRedesignPackageService.Prepare(profile, request, draft);
UIRedesignPackageService.PrepareFromDraftJson(profile, request, jsonPath);
UIRedesignPackageService.ValidateManifest(profile, request);
```

无草稿参数的入口会生成 Brief、草稿 JSON 模板、dry-run CSV、dry-run Markdown 汇总，并运行 Error gate。传入 provider 返回的 `UIRedesignDraft`，或传入 AI/人工编辑后的草稿 JSON 路径时，会先保存经过 request 与草稿校验且强制人工确认的草稿 JSON，再运行同一套 dry-run、gate、执行计划、宿主执行清单和 manifest 流程。
最后还会生成待确认执行计划、待补输入 CSV 与汇总、待补输入就绪检查、外部生成输入包、外部生成任务清单、外部生成 Prompt Pack、外部生成单项 Prompt 清单、外部生成引用素材清单、宿主执行清单和 `UIRedesignPackage_*.md` manifest，记录请求参数、草稿 JSON 快照路径、外部输入草稿路径、产物路径、dry-run 分布、dry-run 检查分布、执行计划状态、gate 状态、待补输入、目标图集新图数量、复核项分布和下一步；manifest 生成前会先复验 dry-run、执行计划和待补输入就绪 CSV，写出后会立即复用完整 manifest 验证。
`ValidateManifest` 只读验证 manifest 顶层标题、固定产物路径、引用的旧版预览、Brief、草稿、dry-run、执行计划、待补输入 CSV 与汇总、待补输入就绪检查、外部生成输入包、外部生成任务清单、外部生成 Prompt Pack、外部生成单项 Prompt 清单、引用素材清单和宿主执行清单存在，并先复验 dry-run、执行计划和待补输入就绪 CSV，再复用外部输入包验证来检查目录分组、Prompt、引用素材清单和 Markdown 标题顺序；dry-run 汇总、执行计划汇总、待补输入汇总、待补输入就绪汇总和宿主执行清单生成时也会检查自身 Markdown 标题顺序，再比对 dry-run 分布、执行计划状态分布、gate、阻断状态分布、待补新版预览、新图数和目标图集数。
从外部草稿 JSON 进入时，安全快照会写入独立的 `UIRedesignDraft_*_Snapshot.json`；如果输入路径已经占用该快照名，会自动使用 `Snapshot2` 等后缀，避免覆盖输入草稿。

生成 AI 输入上下文：

```csharp
UIRedesignBriefService.GenerateBrief(profile, request);
UIRedesignBriefWindow.ShowWindow(profile);
```

`sourcePreviewPath` 为空时，Brief 入口会先生成旧版基准图到 `Logs/UIPrefabBaseline_*.png`，只临时实例化 prefab 并用 Editor Camera 渲染，不保存场景、不写 prefab。源 prefab 校验会先复验 `UIPrefabOptimizationTargets.csv` 精确表头和行结构，确认目标 prefab 已在扫描报告中。Brief 会整理目标 prefab 的扫描结论、断批建议、图集拆解、直挂 `UITexture`、复用与归属风险、空 Sprite 风险，并附带 `UIRedesignDraft` 输出格式、dry-run、执行计划 gate、manifest 和宿主执行清单提示；生成时会检查自身 Markdown 顶层标题顺序。

生成 AI 草稿 JSON 模板：

```csharp
UIRedesignDraftTemplateService.Generate(profile, request);
```

模板会把跨功能非大图 `AtlasSprite` 放入候选替换计划，把大图、`UITexture`、按名加载和直挂散图写入风险。模板落盘前会复用草稿 JSON 校验，确保输出路径和替换项契约合规。它是给 AI 或人工编辑的起点，不代表批准执行。

读取 AI 返回的草稿 JSON 并打开只读确认窗口：

```csharp
UIRedesignDraftService.LoadDraftAndShow(jsonPath);
```

batchmode 只需要校验 JSON 时，可以调用 `UIRedesignDraftService.LoadDraft(jsonPath)`。
草稿 JSON 读取时会先检查根对象后没有尾随内容，`draftPreviewPath`、`generatedImageFolder` 必须是完整字符串字面量且转义合法，`requiresConfirmation` 如果存在必须是完整布尔字面量，契约字段不允许重复，`risks` 必须是字符串数组，以及 `replacementPlan` 对象内存在 `items` 数组；每个替换项的 `oldAssetPath`、`newAssetPath`、`targetAtlasPath` 会先按完整字符串字段校验并阻断非法转义，`preserveGuid` 和 `requiresConfirmation` 如果存在必须是完整布尔字面量，`reason` 如果存在必须是字符串，再进入路径规则校验。这些路径必须使用 `Assets/...` 格式，不能包含 `..` 路径段；预览和新图必须为 `.png`，新图必须位于 `generatedImageFolder` 下，目标图集必须为 `.spriteatlasv2`，旧资源不能重复，同时强制保持人工确认。`risks` 必须存在，可为空数组。

替换计划 dry-run：

```csharp
UIReplacementPlanDryRunService.Run(profile, jsonPath);
```

dry-run 会读取草稿 JSON 和现有扫描报告，输出 `UIReplacementPlanDryRun.csv` 与 `UIReplacementPlanDryRunSummary.md`，检查旧资源、新资源、目标图集、prefab 引用、复用归属、按名加载和 `AddressByFileName` 同名风险；CSV 写出后会立即复验表头和行结构，汇总里会列 gate 状态、警告项分布和复核项分布。它只写报告，不执行替换。
需要 batch 关口时，宿主可在 dry-run 后调用 `UIReplacementPlanDryRunService.ValidateNoErrors(profile)`；该 gate 会先复验 dry-run CSV 和汇总标题结构，再只在存在 `Error` 检查项时失败。

生成待确认执行计划：

```csharp
UIReplacementExecutionPlanService.Generate(profile, jsonPath);
```

执行计划会先重新跑 dry-run，读取当前 dry-run 前会复验 dry-run CSV 精确表头，再输出 `UIReplacementExecutionPlan.csv` 与 `UIReplacementExecutionPlanSummary.md`。它会先确认新版预览图，再把每个替换项展开为确认新图、确认目标图集、确认风险、替换 prefab 引用和执行后复验等步骤，并标记 `PendingPreview`、`PendingAsset`、`PendingAtlas`、`NeedsReview`、`Blocked` 或 `PendingConfirmation`。确认风险步骤会带上 dry-run 的 Review/Error 检查摘要和证据；汇总读取 `UIReuseIndex.csv` 前会先复验复用索引表头和行结构，用于补充待生成新图尺寸和归属信息，并列出 dry-run 检查分布、gate 状态、待补新版预览、待生成新图、待确认目标图集新图数量和复核项分布。这些步骤只是给人工确认和宿主执行流程使用，不会改资源。

新图和目标图集补齐后，可以用 gate 检查执行计划是否仍有阻断状态：

```csharp
UIReplacementExecutionPlanService.ValidateNoBlockingStatuses(profile);
```

该 gate 会先复验执行计划 CSV 精确表头和汇总 Markdown 顶层标题结构，再阻断 `Blocked`、`PendingPreview`、`PendingAsset` 和 `PendingAtlas`，失败时会输出阻断状态分布。`NeedsReview` 仍表示人工确认项，不会被自动当成通过或失败。

导出待补输入：

```csharp
UIReplacementPendingInputChecklistService.Generate(profile);
UIReplacementPendingInputChecklistService.Validate(profile);
```

待补输入 CSV 来自当前 `UIReplacementExecutionPlan.csv`，读取前会先校验执行计划表头和行结构，CSV 写出后会立即复验表头和行结构。它把新版预览、新图和目标图集拆成机器可读行，方便外部生成工具或人工补图按路径落位；Markdown 汇总用于人工快速查看总数、分布和前 30 项。验证入口会检查 CSV 与当前执行计划一致，确认汇总计数、分布和前 30 项明细一致，并确认预览/新图为 `.png`、目标图集为 `.spriteatlasv2`、路径都是 `Assets/...` 且不含 `..`。它只写报告，不生成图片、不创建图集。

检查待补输入是否已落位：

```csharp
UIReplacementPendingInputReadinessService.Generate(profile);
UIReplacementPendingInputReadinessService.Validate(profile);
UIReplacementPendingInputReadinessService.ValidateNoMissing(profile);
```

就绪检查读取当前待补输入 CSV，输出 `UIReplacementPendingInputReadiness.csv` 和 Markdown 汇总，标记每个新版预览、新图和目标图集为 `Ready`、`Missing` 或 `Invalid`；CSV 写出后会立即复验表头和行结构。Preview/NewAsset 已存在时会尝试解码 PNG，解码失败标记为 `Invalid`，解码成功时记录实际宽高；TargetAtlas 只检查文件存在。Markdown 汇总还会列出外部落位验收清单，并分开列出缺失项、格式异常项和已就绪项。`Validate` 只校验报告与当前文件状态一致；`ValidateNoMissing` 是补图前 gate，仍有 Missing 或 Invalid 时失败。它只读文件存在性和 PNG 可读性，不生成图片、不创建图集。

生成外部生成输入包：

```csharp
UIReplacementExternalInputPackageService.Generate(profile, request, briefPath, draftJsonPath);
UIReplacementExternalInputPackageService.Validate(profile, request, briefPath, draftJsonPath);
```

外部生成输入包读取待补输入就绪检查，输出 `UIReplacementExternalInputPackage.json`、Markdown 汇总、`UIReplacementExternalGenerationTasks.md`、`UIReplacementExternalPromptPack.md`、`UIReplacementExternalPromptItems.md`、`UIReplacementExternalPrompt_*.md` 和 `UIReplacementExternalReferenceCopyList.md`，把旧版预览、旧版预览就绪状态与尺寸、`stylePrompt`、Brief、草稿 JSON、待补 CSV、readiness CSV、输出目录及其未就绪/缺失/异常计数、目录准备清单、待落位目录分组、去重参考输入 `referenceInputs`、每个输出路径、输出目录和文件名、期望输出尺寸、当前实际尺寸、尺寸状态 `sizeStatus`、尺寸状态分布、任务级验收规则 `acceptanceCheck`、输出目录就绪状态、参考旧图、参考图就绪状态、参考图尺寸、任务级 `referenceCopyFileName`、引用素材导入名、目标图集、Item、就绪状态和 `taskPrompt` 整理在一起。生成和验证入口会先复验待补输入就绪链路和 `UIReuseIndex.csv`，再用复用索引补参考旧图尺寸。Preview 期望输出尺寸来自旧版基准图，NewAsset 期望输出尺寸来自参考旧图；Preview/NewAsset 的验收规则会要求 PNG 可解码并匹配期望尺寸，TargetAtlas 要求目标 `.spriteatlasv2` 存在。`sizeStatus` 为 `Pending`、`Match`、`Mismatch`、`Unknown` 或 `NotApplicable`。生成入口写出全部 JSON 和 Markdown 后会立即复用完整验证，并检查外部输入 JSON 根字符串字段、字符串转义、数组字段、数组项字符串/整数字段、字段值字面量结束、对象/数组字段值结束、对象字段分隔、整数 ASCII 数字和前导零、重复契约字段和尾随内容；草稿 JSON 也会阻断字符串字段尾随 token、非法转义、非字符串 `risks` 项和不完整布尔字面量。若 Preview/NewAsset 已经 Ready，会复算当前 PNG 尺寸并比对 readiness 记录的实际尺寸、期望输出尺寸和 `sizeStatus`。任务清单按未就绪项列出可执行任务，Missing 和 Invalid 都会进入任务；任务清单和汇总都会列待落位目录分组与外部产物落位后复跑顺序；Prompt Pack 也带 `stylePrompt`、尺寸状态分布、目录准备清单和去重参考输入，并把未就绪项拆成上下文块；单项 Prompt 清单会把每个未就绪项拆成独立 `UIReplacementExternalPrompt_*.md`，并按输出目录建立索引，单项文件包含验收规则和落位后复跑步骤，方便外部生成器逐项投喂；引用素材清单把旧版预览和旧图引用拆成带稳定导入名的外部准备 checklist，并列出导入步骤、引用来源目录分组和复制清单。生成这些外部 Markdown 时会立即校验顶层标题结构。它只整理给外部生成工具消费的输入，不调用 AI、不生成图片、不创建图集、不创建目录。

整理宿主执行前清单：

```csharp
UIReplacementHostApplyChecklistService.Generate(profile);
```

宿主执行清单会先校验当前 `UIReplacementExecutionPlan.csv` 表头和行结构，再输出 `UIReplacementHostApplyChecklist.md`，把待补新版预览、新图、目标图集、复核项分布、阻断项、非阻断的执行前人工确认、prefab 替换候选和执行后验证拆开给宿主流程使用。需要 batch 关口时，可以调用 `UIReplacementHostApplyChecklistService.ValidateNoBlockingSteps(profile)`，只检查执行计划阻断步骤是否为 0，不自动批准 `NeedsReview`。

真正执行替换应由宿主确认后执行器负责。执行器契约见 `host-apply-executor.md`，它必须在 dry-run、执行计划 gate、宿主清单 gate 和人工确认记录都通过后运行。

编辑器内也可以直接创建并打开确认窗口：

```csharp
var draft = UIRedesignDraftService.CreateDraftAndShow(request, provider);
var jsonPath = UIRedesignDraftService.SaveDraft(profile, request, draft);
```

`provider` 实现 `IUIAIGenerationProvider`，`UIRedesignDraftService` 会把草稿和替换项的 `requiresConfirmation` 固定为 `true`：

```csharp
public interface IUIAIGenerationProvider
{
    string Name { get; }
    UIRedesignDraft CreateDraft(UIRedesignRequest request);
}
```

## 输入

`UIRedesignRequest` 包含：

- `sourcePrefabPath`：原始 prefab，必须已经出现在当前扫描报告里。
- `sourcePreviewPath`：原界面预览图；为空时 Brief 入口会生成旧版基准图。
- `stylePrompt`：目标风格描述。
- `inputImageFolder`：新切图输入目录。
- `reuseCandidateReportPath`：复用候选报告。
- `outputFolder`：草稿输出目录，非空时必须是 `Assets/...` 路径。
- `referenceImagePaths`：参考图列表。

宿主项目的 batch 包装可以把命令行参数直接映射到同一份 request，例如 `-uiPrefabPath`、`-uiPreviewPath`、`-uiStylePrompt`、`-uiInputImageFolder`、`-uiOutputFolder` 和分号分隔的 `-uiReferenceImages`。命令行解析属于宿主边界，包内服务只接收 `UIRedesignRequest`。
AI/人工编辑后的草稿 JSON 可以额外通过 `-uiDraftJsonPath` 传给宿主包装，再调用 `UIRedesignPackageService.PrepareFromDraftJson`。

## 输出

`UIRedesignDraft` 包含：

- `draftPreviewPath`：新版预览图。
- `generatedImageFolder`：生成图片目录。
- `replacementPlan`：图片替换计划，必须包含 `items`。
- `requiresConfirmation`：是否需要人工确认，默认需要。
- `risks`：风险说明，必须存在，可为空数组。

`UIReplacementItem` 描述单个替换项：

- `oldAssetPath`
- `newAssetPath`
- `targetAtlasPath`
- `preserveGuid`
- `requiresConfirmation`
- `reason`

`UIReplacementExecutionPlan.csv` 描述执行前人工确认步骤：

- `ItemIndex`
- `Action`
- `Status`
- `OldAsset`
- `NewAsset`
- `TargetAtlas`
- `PrefabRefs`
- `RequiresManualConfirmation`
- `Note`
- `Reason`

## 安全边界

`UIRedesignPackageService` 只串联只读和写报告步骤。`UIPrefabBaselineScreenshotService` 只把旧 prefab 临时渲染成 PNG 基准图，不保存场景、不写 prefab。`UIRedesignBriefService` 只读取扫描报告并写出改版 Brief；当 `sourcePreviewPath` 为空时，它会补旧版基准图，不调用 AI、不生成新版图片、不修改资源。Brief 里的输出格式只是草稿契约，不代表已经执行替换。`UIRedesignDraftTemplateService` 只写经过 request 与草稿校验的 JSON 模板，写盘会保留必需数组字段并立即读回。`UIRedesignDraftService.SaveDraft` 只保存经过 request 与草稿校验且强制人工确认的草稿 JSON，写盘后立即读回校验。`UIRedesignDraftService.LoadDraft` 只读取 JSON 并强制保持人工确认。`UIRedesignPackageService.PrepareFromDraftJson` 只把现有草稿 JSON 快照进报告目录、避开输入路径、在 manifest 记录原输入路径，并继续生成 dry-run、执行计划、待补输入、就绪检查、外部生成输入包、宿主执行清单和 manifest。`UIReplacementPlanDryRunService` 只写检查报告。`UIReplacementExecutionPlanService` 只写待确认执行计划。`UIReplacementPendingInputReadinessService` 只检查待补输入文件是否已存在。`UIReplacementExternalInputPackageService` 只整理外部生成工具输入，JSON 和 Markdown 写盘后会立即校验结构。`UIReplacementHostApplyChecklistService` 只把执行计划整理成宿主执行前清单。`UIRedesignBriefWindow` 只收集 request 字段并生成 Brief。provider 只能返回 `UIRedesignDraft`。`UIRedesignDraftWindow` 只读展示新版预览、生成目录、风险和替换计划，不执行资源替换。真正执行替换前，宿主项目需要确认新版预览、GUID、YooAsset 地址、图集归属、prefab 引用、动态加载风险和人工验收结果。

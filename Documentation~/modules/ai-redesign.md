# AI 改版草稿模块

AI 改版模块只定义协议、报告和 gate，不内置具体 AI provider，也不自动覆盖项目资源。

## 工作流

1. `UIRedesignBriefService.GenerateBrief` 整理旧 prefab、旧版预览、扫描结论、风险和输出约束。
2. `UIRedesignDraftTemplateService.Generate` 生成可编辑草稿 JSON 模板。
3. AI provider 或人工返回 `UIRedesignDraft`，再由 `UIRedesignDraftService.SaveDraft` 或 `LoadDraft` 校验并强制人工确认。
4. `UIReplacementPlanDryRunService.Run` 输出替换计划 dry-run，只报告旧资源、新资源、目标图集、复用归属和按名加载风险。
5. `UIReplacementExecutionPlanService.Generate` 把 dry-run 结果展开成待确认执行计划。
6. `UIReplacementPendingInputChecklistService` 和 `UIReplacementPendingInputReadinessService` 导出并检查新版预览、新图和目标图集是否落位。
7. `UIReplacementExternalInputPackageService` 生成外部生成工具可消费的 JSON、Prompt 和引用素材清单。
8. `UIReplacementHostApplyChecklistService` 输出宿主执行前清单。
9. 宿主执行器完成确认后的资源改动时，可输出 `UIReplacementHostApplyResult.csv/md`，包侧只读校验并生成汇总。
10. `UIRedesignPackageService` 串联执行前产物并写 manifest。

## 核心 API

```csharp
UIRedesignBriefService.GenerateBrief(profile, request);
UIRedesignDraftTemplateService.Generate(profile, request);

var draft = UIRedesignDraftService.CreateDraft(request, provider);
var jsonPath = UIRedesignDraftService.SaveDraft(profile, request, draft);
UIRedesignDraftService.LoadDraftAndShow(jsonPath);

UIRedesignPackageService.Prepare(profile, request);
UIRedesignPackageService.Prepare(profile, request, draft);
UIRedesignPackageService.PrepareFromDraftJson(profile, request, jsonPath);
UIRedesignPackageService.ValidateManifest(profile, request);
```

## Request 字段

- `sourcePrefabPath`：原始 prefab，必须已经出现在当前扫描报告里。
- `sourcePreviewPath`：原界面预览图；为空时 Brief 入口会生成旧版基准图。
- `stylePrompt`：目标风格描述。
- `inputImageFolder`：新切图输入目录。
- `reuseCandidateReportPath`：复用候选报告。
- `outputFolder`：草稿输出目录，非空时必须是 `Assets/...`。
- `referenceImagePaths`：参考图列表。

宿主 batch 包装通常把 `-uiPrefabPath`、`-uiPreviewPath`、`-uiStylePrompt`、`-uiInputImageFolder`、`-uiOutputFolder` 和 `-uiReferenceImages` 映射到这组字段。

## Draft 字段

`UIRedesignDraft` 包含：

- `draftPreviewPath`：新版预览 PNG。
- `generatedImageFolder`：新图目录。
- `replacementPlan.items`：图片替换项数组。
- `requiresConfirmation`：必须保持人工确认。
- `risks`：风险说明数组，可为空但必须存在。

`UIReplacementItem` 包含 `oldAssetPath`、`newAssetPath`、`targetAtlasPath`、`preserveGuid`、`requiresConfirmation` 和 `reason`。路径必须是 `Assets/...`，新图必须在 `generatedImageFolder` 下，目标图集必须是 `.spriteatlasv2`，旧资源不能重复。

## 生成产物

| 产物 | 用途 |
| --- | --- |
| `UIRedesignBrief_*.md` | 给 AI 或人工的旧界面上下文与输出约束。 |
| `UIRedesignDraftTemplate_*.json` | 可编辑草稿起点。 |
| `UIReplacementPlanDryRun.csv/md` | 替换计划检查结果和人工汇总。 |
| `UIReplacementExecutionPlan.csv/md` | 待确认执行步骤。 |
| `UIReplacementPendingInputs.csv/md` | 待补新版预览、新图和目标图集清单。 |
| `UIReplacementPendingInputReadiness.csv/md` | 当前文件状态下的 Ready/Missing/Invalid 检查。 |
| `UIReplacementExternalInputPackage.json/md` | 外部生成工具输入包和人工汇总。 |
| `UIReplacementExternalGenerationTasks.md` | 未就绪项任务清单。 |
| `UIReplacementExternalPromptPack.md`、`UIReplacementExternalPromptItems.md`、`UIReplacementExternalPrompt_*.md` | 可拆分投喂外部生成器的 prompt。 |
| `UIReplacementExternalReferenceCopyList.md` | 旧版预览和旧图引用素材准备清单。 |
| `UIReplacementHostApplyChecklist.md` | 宿主执行前清单。 |
| `UIReplacementHostApplyResult.csv/md` | 宿主确认后执行器的只读结果报告和汇总。 |
| `UIRedesignPackage_*.md` | 改版包 manifest。 |

## Gate 规则

- dry-run gate 只阻断 `Error`。
- 执行计划 gate 会先校验 CSV 精确表头、非空步骤、`ItemIndex`、`Action` 白名单、状态白名单、`RequiresManualConfirmation=true` 和非空说明，再阻断 `Blocked`、`PendingPreview`、`PendingAsset` 和 `PendingAtlas`。
- 待补输入 ready gate 阻断 `Missing` 和 `Invalid`。
- 宿主执行清单 gate 只检查阻断步骤是否清零。
- 宿主执行结果 gate 校验宿主已输出的状态、确认记录、Skipped/Failed 说明、复验清单，并确认结果行覆盖当前执行计划内的 prefab 替换和执行后验证步骤；当前执行计划仍有阻断步骤时，结果只能是 `Skipped`。
- `NeedsReview` 始终留给人工确认，不会被自动视为通过。

## 外部输入包

外部输入包会整理旧版预览、期望输出尺寸、实际尺寸、尺寸状态、输出目录、待落位目录、参考旧图、引用素材导入名、验收规则和落位后复跑步骤。它只生成 JSON/Markdown/Prompt，不调用 AI、不生成图片、不创建目录、不创建图集。

## 安全边界

- `UIPrefabBaselineScreenshotService` 只临时渲染旧 prefab 预览，不保存场景、不写 prefab。
- Brief、草稿模板、dry-run、执行计划、待补输入、外部输入包、宿主执行清单和 manifest 都只写报告或 JSON。
- Provider 只能返回 `UIRedesignDraft`。
- `UIRedesignDraftWindow` 只读展示草稿。
- 宿主执行结果报告只读校验，不代表包内执行了资源改动。
- 真正移动资源、覆盖 prefab、调整 SpriteAtlas 或修改 YooAsset 配置，必须由宿主确认后的宿主执行器完成。

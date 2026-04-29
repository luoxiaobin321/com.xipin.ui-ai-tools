# 项目适配模块

本包不直接提供业务菜单。宿主项目应写薄包装，把项目默认配置和菜单接到本包 API。

## 菜单包装示例

```csharp
using System;
using Xipin.UIAITools;
using UnityEditor;

public static class UIAssetTriageScanner
{
    const string ProfilePath = "Assets/Settings/UIAITools/UIAIToolsProfile.asset";
    const string CatalogPath = "Assets/Settings/UIAITools/UIControlCatalog.asset";

    [MenuItem("Tools/UI资源/图片归类扫描")]
    public static void Run()
    {
        UIAssetScanService.Run(Profile(), Catalog());
    }

    [MenuItem("Tools/UI资源/按截图查找已有图片")]
    public static void SearchReuseByImage()
    {
        UIImageSearchService.SearchReuseByImage(Profile(), Catalog());
    }

    public static void SearchReuseByImageBatch()
    {
        UIImageSearchService.SearchReuseByImageBatch(Profile(), Catalog());
    }

    [MenuItem("Tools/UI资源/验证扫描报告")]
    public static void ValidateReports()
    {
        UIReportValidationService.Validate(Profile());
    }

    [MenuItem("Tools/UI资源/生成扫描摘要")]
    public static void GenerateSummary()
    {
        UIScanSummaryService.Generate(Profile());
    }

    [MenuItem("Tools/UI资源/生成面板实测清单")]
    public static void GeneratePanelFocus()
    {
        UIScanSummaryService.GeneratePanelFocus(Profile());
    }

    [MenuItem("Tools/UI资源/生成UI组件候选索引")]
    public static void GenerateComponentCandidateIndex()
    {
        UIComponentCandidateIndexService.Generate(Profile(), Catalog());
    }

    [MenuItem("Tools/UI资源/验证UI组件候选索引")]
    public static void ValidateComponentCandidateIndex()
    {
        UIComponentCandidateIndexService.Validate(Profile());
    }

    public static void GenerateComponentCandidateIndexBatch()
    {
        UIComponentCandidateIndexService.Generate(Profile(), Catalog());
    }

    public static void ValidateComponentCandidateIndexBatch()
    {
        UIComponentCandidateIndexService.Validate(Profile());
    }

    [MenuItem("Tools/UI资源/为选中Prefab生成改版Brief")]
    public static void GenerateRedesignBriefForSelection()
    {
        var request = new UIRedesignRequest
        {
            sourcePrefabPath = SelectedPrefab()
        };
        UIRedesignBriefService.GenerateBrief(Profile(), request);
    }

    [MenuItem("Tools/UI资源/为选中Prefab生成旧版基准图")]
    public static void CapturePrefabBaselineScreenshotForSelection()
    {
        UIPrefabBaselineScreenshotService.Capture(Profile(), SelectedPrefab());
    }

    [MenuItem("Tools/UI资源/打开改版Brief生成器")]
    public static void OpenRedesignBriefWindow()
    {
        UIRedesignBriefWindow.ShowWindow(Profile());
    }

    [MenuItem("Tools/UI资源/打开AI草稿JSON")]
    public static void OpenRedesignDraftJson()
    {
        var path = EditorUtility.OpenFilePanel("选择 UI 改版草稿 JSON", "", "json");
        if (string.IsNullOrEmpty(path))
            return;
        UIRedesignDraftService.LoadDraftAndShow(path);
    }

    [MenuItem("Tools/UI资源/为选中Prefab生成AI草稿模板")]
    public static void GenerateRedesignDraftTemplateForSelection()
    {
        var request = new UIRedesignRequest
        {
            sourcePrefabPath = SelectedPrefab()
        };
        UIRedesignDraftTemplateService.Generate(Profile(), request);
    }

    [MenuItem("Tools/UI资源/为选中Prefab准备AI改版包")]
    public static void PrepareRedesignPackageForSelection()
    {
        var request = new UIRedesignRequest
        {
            sourcePrefabPath = SelectedPrefab()
        };
        UIRedesignPackageService.Prepare(Profile(), request);
    }

    [MenuItem("Tools/UI资源/使用AI草稿JSON准备改版包")]
    public static void PrepareRedesignPackageFromDraftJson()
    {
        var prefab = SelectedPrefab();
        var path = EditorUtility.OpenFilePanel("选择 UI 改版草稿 JSON", "", "json");
        if (string.IsNullOrEmpty(path))
            return;
        var request = new UIRedesignRequest
        {
            sourcePrefabPath = prefab
        };
        UIRedesignPackageService.PrepareFromDraftJson(Profile(), request, path);
    }

    [MenuItem("Tools/UI资源/检查AI替换计划DryRun")]
    public static void DryRunRedesignDraftJson()
    {
        var path = EditorUtility.OpenFilePanel("选择 UI 改版草稿 JSON", "", "json");
        if (string.IsNullOrEmpty(path))
            return;
        UIReplacementPlanDryRunService.Run(Profile(), path);
    }

    [MenuItem("Tools/UI资源/验证AI替换计划DryRun")]
    public static void ValidateReplacementPlanDryRun()
    {
        UIReplacementPlanDryRunService.ValidateNoErrors(Profile());
    }

    [MenuItem("Tools/UI资源/生成AI待确认执行计划")]
    public static void GenerateReplacementExecutionPlan()
    {
        var path = EditorUtility.OpenFilePanel("选择 UI 改版草稿 JSON", "", "json");
        if (string.IsNullOrEmpty(path))
            return;
        UIReplacementExecutionPlanService.Generate(Profile(), path);
    }

    [MenuItem("Tools/UI资源/验证AI待确认执行计划")]
    public static void ValidateReplacementExecutionPlan()
    {
        UIReplacementExecutionPlanService.ValidateNoBlockingStatuses(Profile());
    }

    [MenuItem("Tools/UI资源/生成宿主替换执行清单")]
    public static void GenerateHostApplyChecklist()
    {
        UIReplacementHostApplyChecklistService.Generate(Profile());
    }

    [MenuItem("Tools/UI资源/验证宿主替换执行前置")]
    public static void ValidateHostApplyChecklist()
    {
        UIReplacementHostApplyChecklistService.ValidateNoBlockingSteps(Profile());
    }

    static string SelectedPrefab()
    {
        var prefab = AssetDatabase.GetAssetPath(Selection.activeObject);
        if (string.IsNullOrEmpty(prefab) || !prefab.EndsWith(".prefab"))
            throw new Exception("请选择一个 UI prefab。");
        return prefab;
    }

    static UIAIToolsProfile Profile()
    {
        return AssetDatabase.LoadAssetAtPath<UIAIToolsProfile>(ProfilePath);
    }

    static UIControlCatalog Catalog()
    {
        return AssetDatabase.LoadAssetAtPath<UIControlCatalog>(CatalogPath);
    }
}
```

## Batch 包装入口

宿主可以按需暴露下列 `-executeMethod`。参数名后必须跟随值，不能把下一个已知参数名当作值；包内 `SearchReuseByImageBatch` 也按已知 Unity/工具参数名校验 `-uiQueryImage` 缺值。资源参数使用 Unity 资产路径，`-uiPrefabPath` 必须已经出现在当前扫描报告里，`-uiOutputFolder` 必须是 `Assets/...` 路径；`-uiQueryImage` 和 `-uiDraftJsonPath` 可以指向 `Logs` 下文件或绝对文件路径：

| executeMethod | 参数 | 用途 |
| --- | --- | --- |
| `UIAssetTriageScanner.Run` | 无 | 生成核心扫描 CSV。 |
| `UIAssetTriageScanner.ValidateReports` | 无 | 验证核心扫描 CSV 表头和非空状态。 |
| `UIAssetTriageScanner.GenerateSummary` | 无 | 生成扫描摘要并校验 Markdown 标题结构。 |
| `UIAssetTriageScanner.GeneratePanelFocus` | 无 | 生成面板实测清单，并按当前 Top 面板顺序校验动态 Markdown 标题结构。 |
| `UIAssetTriageScanner.GenerateComponentCandidateIndexBatch` | 无 | 从扫描报告生成 UI 组件候选索引、汇总和人工确认清单，候选 CSV 和确认清单写出后会立即复验。 |
| `UIAssetTriageScanner.ValidateComponentCandidateIndexBatch` | 无 | 验证 UI 组件候选索引表头、候选数量、`ComponentId` 格式与唯一性、汇总文件和 Markdown 标题结构、人工确认清单表头、ID、扫描派生列、分层和决策值。 |
| `UIAssetTriageScanner.ValidateMarkdownSectionContractBatch` | 无 | 自检 Markdown 顶层标题精确校验的正向、缺失、额外和错序样例，不读取或写入资源。 |
| `UIAssetTriageScanner.GenerateUICreationBriefTemplateBatch` | `-uiFeatureName`、`-uiType`，可选 `-uiOutputFolder`、`-uiStylePrompt`、`-uiReferenceImages`、`-uiRequiredInteractions`、`-uiDataBindings`、`-uiConstraints` | 生成新 UI 需求 Brief JSON 模板。 |
| `UIAssetTriageScanner.GenerateUILayoutDraftTemplateBatch` | `-uiCreationBriefJsonPath` | 从新 UI 需求 Brief JSON 生成布局草稿 JSON 模板。 |
| `UIAssetTriageScanner.DryRunUICreationLayoutDraftBatch` | `-uiLayoutDraftJsonPath` | 生成新 UI prefab 生成前布局 dry-run。 |
| `UIAssetTriageScanner.ValidateUICreationLayoutDryRunBatch` | 无 | 先校验新 UI 布局 dry-run CSV，再检查汇总 Markdown 结构和 Error 是否为 0。 |
| `UIAssetTriageScanner.GenerateUICreationHostGenerateChecklistBatch` | `-uiLayoutDraftJsonPath` | 先校验当前 layout dry-run CSV，再生成宿主 UI prefab 草稿生成前确认清单，并比对当前 dry-run 目标和组件列表。 |
| `UIAssetTriageScanner.ValidateUICreationHostGenerateChecklistBatch` | 无 | 先校验当前 layout dry-run CSV，再检查宿主 UI prefab 草稿生成前确认清单 Markdown 结构、阻断步骤、`Gate：Passed`、当前 dry-run 目标、清单组件列表和当前组件确认状态。 |
| `UIAssetTriageScanner.GenerateUICreationHostPrefabDraftBatch` | `-uiLayoutDraftJsonPath` | 宿主侧在 gate 通过后创建 UI prefab 草稿，并输出 `UICreationHostGenerateResult.csv/md`；目标已存在时拒绝覆盖。 |
| `UIAssetTriageScanner.ValidateUICreationHostPrefabDraftBatch` | `-uiLayoutDraftJsonPath` | 验证宿主生成的 prefab 草稿存在，草稿节点、锚点、位置、尺寸、静态文本、静态图片和绑定占位与布局草稿一致，结果报告包含生成后验证行。 |
| `UIAssetTriageScanner.CaptureUICreationHostPrefabPreviewBatch` | `-uiLayoutDraftJsonPath` | 渲染宿主生成的 prefab 草稿预览图到 `Logs/UICreationHostGeneratePreview_*.png`，检查不是空白图，并把尺寸、可见像素、覆盖率和包围盒写入结果报告。 |
| `UIAssetTriageScanner.ValidateUICreationHostGenerateResultBatch` | `-uiLayoutDraftJsonPath` | 只读验证宿主生成结果 CSV、Markdown 顶层标题结构、每个草稿节点的结果行、状态分布、生成后验证行和预览检查行齐全，并按现有 PNG 复算预览统计。 |
| `UIAssetTriageScanner.SearchReuseByImageBatch` | `-uiQueryImage` | 按截图裁剪图反查已有图片。 |
| `UIAssetTriageScanner.CapturePrefabBaselineScreenshotBatch` | `-uiPrefabPath` | 先校验目标 prefab 已在扫描报告中，再生成旧版视觉基准 PNG。 |
| `UIAssetTriageScanner.GenerateRedesignBriefBatch` | `-uiPrefabPath`，可选 `-uiPreviewPath`、`-uiStylePrompt`、`-uiInputImageFolder`、`-uiOutputFolder`、`-uiReferenceImages` | 生成 Brief 并校验 Markdown 标题结构；未传预览图时会先生成旧版基准图。 |
| `UIAssetTriageScanner.GenerateRedesignDraftTemplateBatch` | 同上 | 生成草稿 JSON 模板。 |
| `UIAssetTriageScanner.PrepareRedesignPackageBatch` | 同上 | 串联 Brief、模板、dry-run、执行计划、待补输入、就绪检查、外部生成输入包、宿主执行清单和 manifest；manifest 生成前复验源 CSV，写出后立即复用完整验证。 |
| `UIAssetTriageScanner.ValidateRedesignPackageBatch` | `-uiPrefabPath` | 先复验 manifest 源 CSV，再只读验证改版包 manifest 引用的 Brief、草稿、dry-run、执行计划、待补输入、就绪检查、外部生成输入包、任务清单、Prompt Pack、单项 Prompt 清单、引用素材清单、宿主执行清单和旧版预览图存在，并比对分布、gate 与待补输入计数。 |
| `UIAssetTriageScanner.PrepareRedesignPackageFromDraftJsonBatch` | 同上，另加 `-uiDraftJsonPath` | 用 AI 或人工编辑后的草稿 JSON 串联 Brief、dry-run、执行计划、待补输入、就绪检查、外部生成输入包、宿主执行清单和 manifest。 |
| `UIAssetTriageScanner.ValidateRedesignDraftJsonBatch` | `-uiDraftJsonPath` | 只校验草稿 JSON。 |
| `UIAssetTriageScanner.DryRunRedesignDraftJsonBatch` | `-uiDraftJsonPath` | 生成替换计划 dry-run。 |
| `UIAssetTriageScanner.ValidateReplacementPlanDryRunBatch` | 无 | 先校验 dry-run CSV 和汇总结构，再检查 Error 是否为 0。 |
| `UIAssetTriageScanner.GenerateReplacementExecutionPlanBatch` | `-uiDraftJsonPath` | 生成待确认执行计划，读取 dry-run 前会先校验 dry-run CSV。 |
| `UIAssetTriageScanner.ValidateReplacementExecutionPlanBatch` | 无 | 先校验执行计划 CSV 和汇总结构，再检查阻断状态是否为 0。 |
| `UIAssetTriageScanner.GenerateReplacementPendingInputsBatch` | 无 | 先校验当前执行计划 CSV，再导出待补预览、新图和目标图集 CSV 并立即复验。 |
| `UIAssetTriageScanner.ValidateReplacementPendingInputsBatch` | 无 | 验证待补输入 CSV 与当前执行计划一致，并检查 PNG、图集和 `Assets/...` 路径契约。 |
| `UIAssetTriageScanner.GenerateReplacementPendingInputReadinessBatch` | 无 | 检查待补预览、新图和目标图集是否已按路径落位。 |
| `UIAssetTriageScanner.ValidateReplacementPendingInputReadinessBatch` | 无 | 验证待补输入就绪报告与当前文件状态一致。 |
| `UIAssetTriageScanner.ValidateReplacementPendingInputReadyBatch` | 无 | gate：仍有待补输入 Missing 或 Invalid 时阻断。 |
| `UIAssetTriageScanner.GenerateReplacementExternalInputPackageBatch` | `-uiPrefabPath` | 先校验待补输入就绪链路和 `UIReuseIndex.csv`，再从 manifest 生成外部生成输入 JSON、汇总、任务清单、Prompt Pack、单项 Prompt 文件和引用素材清单，含目录分组、验收和落位后复跑步骤。 |
| `UIAssetTriageScanner.ValidateReplacementExternalInputPackageBatch` | `-uiPrefabPath` | 先校验待补输入就绪链路和 `UIReuseIndex.csv`，再验证外部生成输入包、汇总、任务清单、Prompt Pack、单项 Prompt 文件和引用素材清单，并校验旧版预览、输出目录、参考图、目录分组、验收、复跑步骤和 prompt 派生字段。 |
| `UIAssetTriageScanner.GenerateHostApplyChecklistBatch` | 无 | 生成宿主执行前清单。 |
| `UIAssetTriageScanner.ValidateHostApplyChecklistBatch` | 无 | 检查宿主执行前阻断步骤是否为 0。 |

## 适配职责

宿主项目负责：

- 决定菜单路径和命令行入口。
- 提供默认 `UIAIToolsProfile`。
- 提供默认 `UIControlCatalog`。
- 按需提供组件候选索引入口，给自动制作 UI 的组件库建设和人工确认清单使用。
- 按需提供组件候选索引验证入口，作为自动制作 UI 的组件库候选 gate，并确认 `ComponentId` 格式合法且唯一、人工确认清单表头可读、ID、扫描派生列和决策值合法。
- 按需提供新 UI 需求 Brief 模板入口，只生成 JSON 输入，不创建 prefab。
- 按需提供布局草稿模板入口，只生成 JSON 草稿，不创建 prefab。
- 按需提供新 UI 布局 dry-run 和 Error gate，只输出检查报告。
- 按需提供宿主 UI prefab 草稿生成前确认清单和阻断 gate，验证时重新检查当前 dry-run 目标、清单组件列表和当前组件确认状态。
- 若实现宿主 UI prefab 草稿生成器，必须放在宿主项目，先通过组件候选、布局 dry-run 和宿主确认清单 gate，创建前拒绝覆盖已有目标，并输出结果报告、预览检查结果和可验证的 Markdown 顶层标题结构。
- 把 batchmode 参数映射成 `UIRedesignRequest`，例如 prefab、预览图、目标风格、新切图目录、输出目录和参考图列表。
- 按需提供旧版 prefab 基准图入口，把 `UIPrefabBaselineScreenshotService.Capture` 的输出作为 `sourcePreviewPath`。
- 提供一键准备 AI 改版包入口，串联 Brief、模板、dry-run、执行计划、待补输入、就绪检查、外部生成输入包、引用素材清单、宿主执行清单和 manifest。
- 提供从已有草稿 JSON 准备 AI 改版包入口，给真实 AI provider 或人工编辑后的草稿复跑完整 gate。
- 提供草稿 JSON 模板生成入口，给 AI 或人工编辑。
- 提供草稿 JSON 的手动打开入口，交给只读确认窗口查看。
- 提供替换计划 dry-run 入口和 Error gate，只输出检查报告。
- 提供待确认执行计划入口，只输出人工确认步骤。
- 提供待确认执行计划 gate，阻断缺新版预览、缺新图、缺目标图集或 dry-run Error 展开的步骤。
- 提供待补输入、就绪检查和外部生成输入包入口，只整理报告、Prompt Pack、单项 Prompt、目录索引、引用素材清单和外部工具上下文，不生成图片、不创建图集。
- 提供宿主执行清单入口，把阻断项、非阻断人工确认项、prefab 替换候选和执行后验证拆给宿主流程。
- 若项目要自动执行替换，提供宿主确认后执行器，并让它只在 dry-run、执行计划 gate、待补输入 Ready gate、宿主清单 gate 和人工确认记录都通过后运行。
- 把扫描结果接入项目自己的资源迁移、构建和验收流程。
- 在业务文档里记录本项目的资源分层规则。

## 不建议放进包内的内容

- 业务程序集引用。
- 具体项目菜单路径。
- 具体项目的 YooAsset Collector 修改逻辑。
- 自动迁移资源和覆盖 prefab 的命令。
- 只服务单个项目历史问题的一次性修复脚本。

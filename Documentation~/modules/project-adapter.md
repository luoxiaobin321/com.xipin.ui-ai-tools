# 项目适配模块

本包不直接提供业务菜单。宿主项目只写薄包装：加载项目默认 `UIAIToolsProfile`、`UIControlCatalog`，把菜单或 batch 参数转成包 API 调用。

## 最小菜单包装

```csharp
using System;
using UnityEditor;
using Xipin.UIAITools;

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

    [MenuItem("Tools/UI资源/为选中Prefab准备AI改版包")]
    public static void PrepareRedesignPackageForSelection()
    {
        UIRedesignPackageService.Prepare(Profile(), new UIRedesignRequest
        {
            sourcePrefabPath = SelectedPrefab()
        });
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

测试宿主的完整 wrapper 位于 `Assets/Scripts/GameApp/Editor/GameApp/UIAssetTriageScanner.cs`。新项目照这个边界接入即可，不需要把宿主业务类型放进包内。

## Batch 参数约定

- 资源路径使用 Unity 资产路径，例如 `Assets/...`。
- `-uiPrefabPath` 必须已经出现在当前扫描报告里。
- `-uiOutputFolder` 必须是 `Assets/...`。
- `-uiQueryImage` 和 `-uiDraftJsonPath` 可指向 `Logs` 下文件或绝对路径。
- 参数名后必须跟随值，不能把下一个已知参数名当作值。
- 宿主负责命令行解析，包服务只接收结构化 request 或明确路径。

## 常用 executeMethod

| 场景 | 入口 |
| --- | --- |
| 核心扫描 | `Run`、`ValidateReports`、`GenerateSummary`、`GeneratePanelFocus` |
| 复用反查 | `SearchReuseByImageBatch -uiQueryImage` |
| 组件候选 | `GenerateComponentCandidateIndexBatch`、`ValidateComponentCandidateIndexBatch` |
| 新 UI Brief/布局 | `GenerateUICreationBriefTemplateBatch`、`GenerateUILayoutDraftTemplateBatch` |
| 新 UI dry-run/gate | `DryRunUICreationLayoutDraftBatch`、`ValidateUICreationLayoutDryRunBatch` |
| 宿主生成前清单 | `GenerateUICreationHostGenerateChecklistBatch`、`ValidateUICreationHostGenerateChecklistBatch` |
| 宿主 prefab 草稿样例 | `GenerateUICreationHostPrefabDraftBatch`、`ValidateUICreationHostPrefabDraftBatch`、`CaptureUICreationHostPrefabPreviewBatch`、`ValidateUICreationHostGenerateResultBatch`、`ValidateUICreationHostGenerateResultContractBatch` |
| 旧界面改版输入 | `CapturePrefabBaselineScreenshotBatch`、`GenerateRedesignBriefBatch`、`GenerateRedesignDraftTemplateBatch` |
| 改版包闭环 | `PrepareRedesignPackageBatch`、`PrepareRedesignPackageFromDraftJsonBatch`、`ValidateRedesignPackageBatch` |
| 草稿与 dry-run | `ValidateRedesignDraftJsonBatch`、`DryRunRedesignDraftJsonBatch`、`ValidateReplacementPlanDryRunBatch` |
| 执行计划 | `GenerateReplacementExecutionPlanBatch`、`ValidateReplacementExecutionPlanBatch` |
| 待补输入 | `GenerateReplacementPendingInputsBatch`、`ValidateReplacementPendingInputsBatch`、`GenerateReplacementPendingInputReadinessBatch`、`ValidateReplacementPendingInputReadinessBatch`、`ValidateReplacementPendingInputReadyBatch` |
| 外部生成输入包 | `GenerateReplacementExternalInputPackageBatch`、`ValidateReplacementExternalInputPackageBatch` |
| 宿主替换前清单 | `GenerateHostApplyChecklistBatch`、`ValidateHostApplyChecklistBatch` |
| 宿主替换结果 | `GenerateHostApplyBlockedResultSampleBatch`、`ValidateHostApplyBlockedResultSampleBatch`、`GenerateHostApplyResultSummaryBatch`、`ValidateHostApplyResultBatch`、`ValidateHostApplyResultContractBatch` |
| 基础契约自检 | `ValidateReportFilesContractBatch`、`ValidateMarkdownSectionContractBatch`、`ValidateJsonContractBatch`、`ValidateCsvContractBatch` |
| 链路契约自检 | `ValidateScanSummaryContractBatch`、`ValidateReplacementPlanStatusContractBatch`、`ValidateReplacementPlanDryRunContractBatch`、`ValidateReplacementExecutionPlanContractBatch`、`ValidateReplacementExternalInputPackageContractBatch`、`ValidateHostApplyChecklistContractBatch`、`ValidateHostApplyResultContractBatch` |

## 宿主职责

- 决定菜单路径、命令行入口和默认配置资产。
- 把 batch 参数映射成 `UIRedesignRequest` 或新 UI brief/layout 路径。
- 提供组件候选人工确认流程和组件 prefab。
- 在项目侧实现可选的 prefab 草稿生成器或替换执行器。
- 把扫描结果接入项目自己的资源迁移、构建和验收流程。
- 在业务文档里记录项目资源分层规则。

## 不放进包内

- 业务程序集引用。
- 具体项目菜单路径。
- YooAsset Collector 修改逻辑。
- 自动迁移资源、覆盖 prefab、创建图集或删除旧资源的命令。
- 只服务单个项目历史问题的一次性脚本。

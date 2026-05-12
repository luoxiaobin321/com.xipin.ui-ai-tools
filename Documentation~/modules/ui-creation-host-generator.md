# 宿主 UI prefab 草稿生成器

宿主 UI prefab 草稿生成器属于接入项目，不属于 `com.xipin.ui-ai-tools` 包。包内只提供 brief、组件候选、layout draft、dry-run、宿主确认清单和 gate；真正创建 prefab 的代码必须放宿主项目。

自动皮肤 prefab 生成复用同一类边界，但输入不同：皮肤从已有 prefab 复制改造，自动制作 UI 从新需求和 layout draft 生成新 prefab。

## 输入

默认读取 `UIAIToolsProfile.logRoot/Creation` 下的这些产物：

- `UICreationBriefTemplate_*.json`
- `UILayoutDraftTemplate_*.json` 或补齐后的 layout draft JSON
- `UIComponentCandidateIndex.csv`
- `UIComponentCandidateReview.csv`
- `UICreationLayoutDryRun.csv`
- `UICreationHostGenerateChecklist.md`

## 前置 Gate

生成器开始前必须通过：

```csharp
UIComponentCandidateIndexService.Validate(profile);
UICreationLayoutDryRunService.ValidateNoErrors(profile);
UICreationHostGenerateChecklistService.ValidateNoBlockingSteps(profile);
```

当前宿主样例入口：

```text
UIAssetTriageScanner.GenerateUICreationHostPrefabDraftBatch -uiLayoutDraftJsonPath <UILayoutDraft.json>
UIAssetTriageScanner.ValidateUICreationHostPrefabDraftBatch -uiLayoutDraftJsonPath <UILayoutDraft.json>
UIAssetTriageScanner.CaptureUICreationHostPrefabPreviewBatch -uiLayoutDraftJsonPath <UILayoutDraft.json>
UIAssetTriageScanner.ValidateUICreationHostGenerateResultBatch -uiLayoutDraftJsonPath <UILayoutDraft.json>
```

## 允许动作

- 创建新的 prefab 草稿；目标 prefab 已存在时拒绝覆盖。
- 实例化人工确认过的组件 prefab 或宿主模板节点。
- 写入锚点、位置、尺寸、层级、状态、静态文本、数据绑定占位和资源引用。
- 输出 `UICreationHostGenerateResult.csv` 和 `UICreationHostGenerateResult.md`。

## 禁止动作

- 覆盖已有 prefab。
- 移动、删除或覆盖图片资源。
- 修改 SpriteAtlas、YooAsset、脚本绑定或业务运行时配置。
- 绕过 dry-run 写入未知组件角色、非法布局或未确认状态。

## 结果契约

`UICreationHostGenerateResult.csv` 精确字段：

```text
ItemIndex,Action,Status,NodeId,ComponentId,TargetPrefab,AssetPath,Binding,Confirmation,Message
```

`Action` 只允许 `CreatePrefab`、`CreateTemplateNode`、`InstantiateComponent`、`ApplyLayout`、`ApplyText`、`ApplyAssetReference`、`ApplyBindingPlaceholder`、`VerifyAfterGenerate`。`Status` 只允许 `Applied`、`Skipped`、`Failed`、`Verified`。

`UICreationHostGenerateResult.md` 应保留 `Layout Draft JSON`、目标 prefab、结果 CSV、host checklist、layout dry-run CSV、组件候选 review CSV，以及带引号的 `Re-run Generate` / `Re-run Validate` 命令，方便从结果报告直接回到生成或复验入口。

生成后至少跑 `ValidateUICreationHostGenerateResultBatch`，再做人工视觉、交互、运行时数据绑定和宿主项目回归。复验通过前，不应把草稿当成可发布 UI。

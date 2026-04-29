# 宿主 UI prefab 草稿生成器

宿主 UI prefab 草稿生成器属于接入项目，不属于 `com.xipin.ui-ai-tools` 包。包内只提供需求 Brief、组件候选索引、布局草稿、资源需求、dry-run、宿主确认清单和 gate；真正创建 prefab 的代码必须放在宿主项目，并且只能在确认流程通过后运行。

## 输入

生成器读取当前 `UIAIToolsProfile.logRoot` 下的产物：

- `UICreationBriefTemplate_*.json`：新 UI 需求 Brief。
- `UILayoutDraftTemplate_*.json` 或人工/AI 补齐后的 `UILayoutDraft` JSON。
- `UIComponentCandidateIndex.csv`：组件候选索引，`ComponentId` 为稳定哈希 ID。
- `UIComponentCandidateReview.csv`：组件候选人工确认清单，宿主可据此确认组件 prefab、预览图、状态和备注；重新生成索引会按 `ComponentId` 保留人工填写列。
- `UICreationLayoutDryRun.csv`：生成前检查明细。
- `UICreationHostGenerateChecklist.md`：宿主生成前确认清单。

宿主侧还需要读取自己的人工确认记录，至少覆盖组件候选升级、目标 prefab 路径、资源需求 Ready 状态、交互绑定、数据绑定、文本长度和 UI 验收人。

## 前置 gate

生成器开始前必须先通过：

```csharp
UIComponentCandidateIndexService.Validate(profile);
UICreationLayoutDryRunService.ValidateNoErrors(profile);
UICreationHostGenerateChecklistService.ValidateNoBlockingSteps(profile);
```

当前宿主样例入口：

```csharp
UICreationHostPrefabDraftGenerator.Generate(profile, layoutDraftJsonPath);
```

batch 包装：

```text
UIAssetTriageScanner.GenerateUICreationHostPrefabDraftBatch -uiLayoutDraftJsonPath <UILayoutDraft.json>
UIAssetTriageScanner.ValidateUICreationHostPrefabDraftBatch -uiLayoutDraftJsonPath <UILayoutDraft.json>
UIAssetTriageScanner.CaptureUICreationHostPrefabPreviewBatch -uiLayoutDraftJsonPath <UILayoutDraft.json>
UIAssetTriageScanner.ValidateUICreationHostGenerateResultBatch -uiLayoutDraftJsonPath <UILayoutDraft.json>
UIAssetTriageScanner.ValidateUICreationHostGenerateResultContractBatch
```

生成入口会先跑上述 gate，再创建目标 prefab 草稿并输出 `UICreationHostGenerateResult.csv` 和 `UICreationHostGenerateResult.md`；目标 prefab 已存在时直接拒绝，不覆盖。当前宿主样例中，`Image` 和 `Text` 节点使用宿主模板节点生成，避免把整屏旧 prefab 当作组件塞入新 UI；`Button` 等复用组件继续实例化确认后的组件 prefab。验证入口会检查生成的 prefab 草稿存在，草稿节点父子层级、锚点、位置、尺寸、静态文本、静态图片和绑定占位与布局草稿一致，结果报告包含生成后验证行。预览入口会把生成后的 prefab 草稿渲染到 `Logs/UICreationHostGeneratePreview_*.png`，检查不是空白图，并把尺寸、可见像素、覆盖率和包围盒写入结果报告。结果验证入口会先复用包内 `UICreationHostGenerateResultService.ValidateAgainstLayoutDraft` 检查 CSV 精确表头、非空结果行、`ItemIndex`、`Action`、状态、目标 prefab 一致性、当前草稿目标、NodeId/ComponentId 归属、每个草稿节点至少一条结果行、每个草稿节点的 `Applied ApplyLayout` 行、目标 prefab 的 `Applied CreatePrefab` 行和 `Verified VerifyAfterGenerate` 行、Applied/Verified 确认记录、Skipped/Failed 说明、Markdown 顶层标题、目标 prefab 和状态分布，再只读检查每个草稿节点的结果行及行内布局说明/资源路径/绑定值/消息、生成后验证行和预览检查行是否齐全，并按现有 PNG 复算预览统计。

这些 gate 只说明阻断输入已经补齐，不代表所有非布局人工复核都已完成。宿主生成器必须拒绝没有人工确认记录的人工复核项。
`UICreationHostGenerateChecklistService` 还会确认本次布局草稿目标 prefab 和组件列表与当前 `UICreationLayoutDryRun.csv` 一致；不一致代表 dry-run 已过期或来自另一份草稿，宿主生成器不得运行。
它还会读取 `UIComponentCandidateReview.csv`，在清单中列出布局引用组件的状态、角色、分层、资源和组件 prefab；引用组件未 `Approved` 时阻断生成清单 gate。验证入口会重新检查当前 dry-run 目标、清单组件列表和当前组件确认清单，避免旧的 `Gate：Passed` 清单在 review 或 dry-run 变化后继续放行。
验证入口会要求 `UICreationHostGenerateChecklist.md` 顶层标题结构有效，并明确包含 `Gate：Passed`；缺失、过期或格式异常的清单不得放行。

## 允许动作

宿主生成器可以按项目规则实现这些动作：

- 在确认后的目标目录创建新的 prefab 草稿。
- 实例化宿主确认过的组件 prefab 或模板节点。
- 写入锚点、位置、尺寸、层级、状态、静态文本、数据绑定占位和资源引用；状态值必须来自 dry-run 允许集合。
- 拒绝绕过 dry-run 写入未知组件角色、非法锚点、非法位置或与组件角色不兼容且未确认的状态。
- 生成宿主侧结果报告，记录每个节点、资源需求和绑定的执行状态。

生成器不得覆盖已有 prefab，不得移动、删除或覆盖图片资源，不得修改 SpriteAtlas、YooAsset、脚本绑定或业务运行时配置。

## 输出

建议宿主输出独立报告，例如 `UICreationHostGenerateResult.csv` 和 `UICreationHostGenerateResult.md`。CSV 字段至少包含：

| 字段 | 含义 |
| --- | --- |
| `ItemIndex` | 执行项序号。 |
| `Action` | 宿主执行动作，只允许 `CreatePrefab`、`CreateTemplateNode`、`InstantiateComponent`、`ApplyLayout`、`ApplyText`、`ApplyAssetReference`、`ApplyBindingPlaceholder`、`VerifyAfterGenerate`。 |
| `Status` | `Applied`、`Skipped`、`Failed` 或 `Verified`。 |
| `NodeId` | 对应 `UILayoutDraft.nodes` 的节点 ID。 |
| `ComponentId` | 对应 `UIComponentCandidateIndex.csv` 的稳定组件候选 ID。 |
| `TargetPrefab` | 生成的 prefab 路径。 |
| `AssetPath` | 实际写入的资源路径。 |
| `Binding` | 数据绑定或交互绑定说明。 |
| `Confirmation` | 宿主人工确认记录标识。 |
| `Message` | 执行结果说明。 |

Markdown 汇总建议列出目标 prefab、节点数量、失败项、跳过项、人工确认记录、生成后验证结果、预览图检查结果和下一步。包侧 `UICreationHostGenerateResultService.GenerateSummary` 可从 CSV 生成基础汇总，宿主样例再追加预览和深度验证。当前宿主样例固定顶层标题为 `目标`、`状态分布` 和 `下一步`；写入、追加预览、追加深度验证、通用契约验证和结果验证都会精确校验该结构。

## 生成后复验

宿主生成完成后应重新运行：

1. `ValidateUICreationHostGenerateResultBatch`。
2. 打开生成 prefab 做视觉、交互和运行时数据绑定人工检查。
3. 核心扫描。
4. `UIReportValidationService.Validate(profile)`。
5. `UIScanSummaryService.GeneratePanelFocus(profile)` 或宿主自己的面板实测流程。
6. 宿主项目自己的 UI 回归、图集构建、资源加载和数据绑定验证。

复验通过前，不应把生成结果当成可发布 UI，也不应覆盖已有界面。

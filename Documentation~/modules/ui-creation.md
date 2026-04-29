# 自动制作 UI 契约

自动制作 UI 是第三阶段能力，目前只定义输入和安全边界，不生成 prefab，不导入资源，不覆盖现有界面。

## 目标

自动制作 UI 的目标是把需求、参考图和组件库事实整理成可审阅的 prefab 草稿计划。它应复用本包已有模式：先产出 Brief、JSON 草稿、资源需求清单、dry-run 和人工确认清单，再由宿主项目决定是否生成或应用 prefab。

## 输入契约

### UI 需求 Brief

`UICreationBrief` 应描述：

| 字段 | 含义 |
| --- | --- |
| `featureName` | 新界面或弹窗的功能名。 |
| `uiType` | 页面、弹窗、列表项、页签、浮层等类型。 |
| `targetFolder` | 建议输出目录，必须是 `Assets/...`。 |
| `stylePrompt` | 风格、情绪、尺寸和主题描述。 |
| `referenceImagePaths` | 参考图路径。 |
| `requiredInteractions` | 必要交互，例如按钮、关闭、分页、列表滚动。 |
| `dataBindings` | 文本、图标、货币、头像、进度条等数据位。 |
| `constraints` | 安全区、分辨率、语言长度、性能或动效约束。 |

生成需求 Brief 模板：

```csharp
UICreationBriefTemplateService.Generate(profile, brief);
```

模板入口会先验证组件候选索引存在且非空，再保存 `UICreationBriefTemplate_<Feature>.json`。写盘会保留空数组字段并立即读回；读取 Brief JSON 时会检查根对象、必填字符串根字段及其字面量结束、字符串转义、数组字段值结束、重复契约字段和尾随内容。它只写输入 JSON，不创建 prefab。

### 组件库索引

组件库索引用于告诉 AI 和宿主生成器有哪些可复用构件：

| 字段 | 含义 |
| --- | --- |
| `componentId` | 稳定组件标识。 |
| `prefabPath` | 组件 prefab。 |
| `role` | Button、Text、Image、Scroll、Panel 等角色。 |
| `size` | 推荐尺寸。 |
| `states` | normal、disabled、selected、pressed 等状态。 |
| `previewPath` | 组件预览图。 |
| `usageNotes` | 适用场景。 |

组件库索引应来自宿主项目扫描或人工维护，不应在 AI 输出里凭空捏造。
第一版组件候选可以从现有 `UIPrefabBatchSequence.csv` 的 `Source`、`Type`、`Path`、`ImageAsset`、`Atlas` 字段和 `UIControlCatalog` 的角色配置推导；按钮、滚动容器和面板会额外参考明确的节点命名 token 做候选角色归类，例如 `btn`、`btn1`、`button`、`toggle`、`tab`、`scrollview`、`scrollrect`、`panel`。滚动容器只从自身节点或 `Scroll View`、`ScrollViewPlus`、`ScrollRect` 下的直接 `Viewport` 内置白图行推导，普通 `Viewport` 和滚动内容子图不升级为 Scroll。需要更稳定的组件库时，再由宿主项目人工维护组件 prefab 和预览图。
候选 `componentId` 由角色、来源类型、资源路径和图集字段生成，避免因候选排序变化导致已补齐的布局草稿失效。
空 Sprite 和 Unity 内置白图不会仅凭按钮节点名升级为 Button 候选，避免把低信号占位图当作可复用按钮组件。

生成组件候选索引：

```csharp
UIComponentCandidateIndexService.Generate(profile, catalog);
UIComponentCandidateIndexService.Validate(profile);
```

该入口输出 `UIComponentCandidateIndex.csv`、`UIComponentCandidateIndexSummary.md` 和 `UIComponentCandidateReview.csv`，写出后会立即复用验证入口。汇总会列出 Button 复核队列，把跨 prefab 的候选和单 prefab 或低复用候选分开；确认清单给宿主填写 `ComponentPrefabPath`、`PreviewPath`、`States`、`UsageNotes`、`Reviewer` 和 `ReviewNotes`，`SuggestedDecision` 默认是 `NeedsReview`，允许 `NeedsReview`、`Approved` 或 `Rejected`，`Approved` 必须填写 `ComponentPrefabPath`。重新生成确认清单时，会按 `ComponentId` 保留 `SuggestedDecision`、`ComponentPrefabPath`、`PreviewPath`、`States`、`UsageNotes`、`Reviewer` 和 `ReviewNotes`，并刷新角色、分层、引用次数、样例节点等扫描派生字段。验证入口检查候选索引表头、候选数量、`ComponentId` 格式与唯一性、汇总文件和 Markdown 标题结构、确认清单表头、确认清单行数、ID、扫描派生列、复核分层和决策值。它只汇总已有扫描报告，不创建组件 prefab。

### 布局草稿 JSON

`UILayoutDraft` 描述拟生成的界面结构：

- `root`：界面根节点、画布参考尺寸和安全区策略。
- `nodes`：节点树，包含节点名、组件角色、组件候选 ID、锚点、位置、尺寸、状态、文本、数据绑定和资源引用。
- `assets`：所需新图或复用图。
- `interactions`：按钮、列表、页签和关闭行为。
- `risks`：缺组件、缺素材、文本过长、动态数据不明确或性能风险。
- `requiresConfirmation`：必须为 `true`。

生成布局草稿模板：

```csharp
UILayoutDraftTemplateService.Generate(profile, briefJsonPath);
```

该入口会读取并校验 `UICreationBrief`，再输出 `UILayoutDraftTemplate_<Feature>.json`。写盘会保留空数组字段并立即读回；读取 Brief 和布局草稿 JSON 时会检查根对象、Brief 可选布尔 `requiresConfirmation`、字符串数组项、字符串转义、`root` 对象及其字符串字段字面量结束、对象/数组字段值结束、重复契约字段、`nodes`/`assets` 数组项关键字符串字段、布尔 `requiresConfirmation` 字段和尾随内容。首版模板只填 root、交互、由数据位推导的资源需求和风险提示，布局节点仍由 AI 或人工补齐。

## 资源需求清单

生成 prefab 前应先输出资源需求清单：

| 字段 | 含义 |
| --- | --- |
| `NeedId` | 需求项编号。 |
| `Kind` | `DataBinding`、`NewImage`、`ReuseImage`、`Font`、`Effect`、`ComponentPrefab` 或 `ScriptBinding`。 |
| `Path` | 期望资源路径。 |
| `Source` | AI 生成、工程复用、组件库或人工提供。 |
| `Status` | `Ready`、`Missing`、`NeedsReview`。 |
| `Reason` | 需求来源。 |

`Missing` 和 `NeedsReview` 不清零时，不应进入 prefab 生成。

## 生成前 dry-run

prefab 生成前 dry-run 至少检查：

- `targetFolder` 是合法 `Assets/...` 路径。
- `referenceResolution` 必须是正数 `宽x高`。
- 建议目标 prefab 不存在；若已存在，必须进入宿主单独覆盖流程。
- `requiresConfirmation` 必须为 `true`，布局草稿加载时不会替宿主或 AI 改写该字段。
- 所有组件候选 ID 已存在于 `UIComponentCandidateIndex.csv`。
- 所有资源需求为 `Ready`。
- 资源需求 `needId` 不能重复。
- 资源需求 `kind` 必须来自允许集合，`status` 必须是 `Ready`、`Missing` 或 `NeedsReview`。
- `Ready` 的非 `DataBinding` 资源需求必须提供合法 `Assets/...` 路径；非空资源需求路径不能包含反斜杠或 `..`。
- 文本、列表、按钮和数据绑定都有明确来源。
- 组件角色必须来自组件候选索引角色或包内内置角色；角色不一致会进入人工复核，未知角色会阻断。
- 布局节点没有重复 ID、同父节点重名、缺父节点、缺锚点、缺位置或缺尺寸。
- `anchor` 必须是预设方位，例如 `top_left`、`middle_center`、`bottom_right`、`stretch_full`，或 `minX,minY|maxX,maxY` 的归一化锚点范围。
- `position` 必须是 `x,y` 数字格式。
- Text 节点必须有静态文本或数据绑定，超过 40 字会进入人工复核。
- `state` 必须是 `normal`、`disabled`、`selected`、`pressed` 或 `hidden`；缺失会进入人工复核，非法值会阻断；非 Button 角色使用 `disabled`、`selected` 或 `pressed` 会进入人工复核。
- `size` 必须是正数 `宽x高`。
- 节点 `dataBinding` 必须在 `assets` 的 `DataBinding` 资源需求中声明；未声明会阻断。
- `dataBindings` 中的每个数据位应被节点 `dataBinding` 引用；未覆盖会进入人工复核。
- 节点资源路径如果存在，必须是合法 `Assets/...` 路径。

dry-run 只输出报告，不创建 prefab。

首版入口：

```csharp
UICreationLayoutDryRunService.Run(profile, layoutDraftJsonPath);
UICreationLayoutDryRunService.ValidateNoErrors(profile);
```

入口输出 `UICreationLayoutDryRun.csv` 和 `UICreationLayoutDryRunSummary.md`。验证入口会先复验 dry-run CSV 精确表头和行结构，再检查汇总 Markdown 的顶层标题结构；模板草稿通常会因为布局节点为空、资源需求仍是 `NeedsReview` 被 gate 阻断，补齐后再复跑。

## 宿主生成前确认清单

```csharp
UICreationHostGenerateChecklistService.Generate(profile, layoutDraftJsonPath);
UICreationHostGenerateChecklistService.ValidateNoBlockingSteps(profile);
```

确认清单输出 `UICreationHostGenerateChecklist.md`，把目标 prefab 建议路径、dry-run gate、阻断项、人工复核项、布局引用组件确认状态、宿主生成器允许动作、禁止动作和生成后验证列出来。它读取当前 dry-run 前会先复验 `UICreationLayoutDryRun.csv` 精确表头和行结构，再比对本次布局草稿目标 prefab 与当前 dry-run 的 `TargetPrefab` 行，也会比对本次草稿组件列表与当前 dry-run 的组件列表；不一致时阻断，要求先用同一份草稿重跑布局 dry-run。它还会读取 `UIComponentCandidateReview.csv`，布局引用的组件候选未 `Approved` 时阻断宿主生成。验证入口会重新验证当前组件候选确认清单和 layout dry-run CSV，要求清单标题结构有效、明确包含 `Gate：Passed`，且目标 prefab 与当前 dry-run 一致、清单组件列表与当前 dry-run 一致、当前 dry-run 引用组件仍为 `Approved`，缺失或格式异常不会放行。它不创建 prefab，只给宿主确认后的生成器使用。

## 安全边界

自动制作 UI 的包内能力应止步于 Brief、组件库索引、布局草稿、资源需求清单、dry-run 和确认清单。真正 prefab 生成器必须放宿主项目，并且只能在 dry-run 和人工确认通过后运行。

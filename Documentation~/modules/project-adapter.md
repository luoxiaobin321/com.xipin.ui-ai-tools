# 项目适配模块

本包不直接绑定业务菜单。宿主项目只写薄包装：加载 `UIAIToolsProfile`、`UIControlCatalog`，把菜单、工作台按钮或 batch 参数转成包 API 调用。

## 默认路径

```csharp
const string ProfilePath = "Assets/UIAITools/Settings/UIAIToolsProfile.asset";
const string CatalogPath = "Assets/UIAITools/Settings/UIControlCatalog.asset";
```

```text
Assets/UIAITools/Skinning/<UIName>
Assets/UIAITools/Creation/<FeatureName>
Assets/UIAITools/GlobalRuntimeUpdates/<BatchName>

UIAIToolsReports/Scanning
UIAIToolsReports/ReuseSearch
UIAIToolsReports/Creation
UIAIToolsReports/Skinning/<UIName>
UIAIToolsReports/GlobalRuntimeUpdates
```

新版换皮宿主生成的独立 prefab 草稿固定为：

```text
Assets/UIAITools/Skinning/<UIName>/Prefabs/<SourcePrefabName>_v2.prefab
```

## 当前入口

宿主推荐只暴露一个菜单：

```text
Tools/UIAITools/打开工作台
```

工作台组织四个页签：自动整理、复用反查、自动制作、新版换皮。旧自动设计效果图和资源替换实验链路不再作为入口。

## Batch 参数

- 资源路径使用 Unity 资产路径，例如 `Assets/...`。
- 纯报告写到 `UIAIToolsReports/...`，不写入 `Assets`。
- `-uiPrefabPath`：现有 UI prefab。
- `-uiOutputFolder`：可选；不传时宿主按 `UIAIToolsProfile.workspaceRoot` 推导；自动制作 UI 必须在 `<workspaceRoot>/Creation/` 下，新版换皮必须在 `<workspaceRoot>/Skinning/` 下。
- `-uiQueryImage`：PNG/JPG 裁图。
- `-uiCreationBriefJsonPath`、`-uiLayoutDraftJsonPath`：自动制作 UI JSON。
- `-uiSkinManifestPath`：新版换皮 `skin.json`。
- `-uiConceptPath`、`-uiInputImageFolder`、`-uiCleanupSourcePath`：换皮目标图、已提供切图目录、运行时清理源图。
- `-uiBatchName`：全局动态资源更新批次。
- 参数名后必须跟随值，不能把下一个已知参数名当作值。

## 当前换皮主线

新 UI 或非 UIVipcard 起步：

```text
GenerateSkinHostAdapterChecklistBatch -uiPrefabPath <Prefab>
```

它只读源 prefab，写出 `UIAIToolsReports/Skinning/<UIName>/host-adapter-checklist.md`，不生成 prefab 或图片。
报告应包含带引号的 `Re-run Checklist` 命令，方便从接入清单复跑同一 prefab 和输出目录。
现有 `host-adapter-checklist.md` 会被换皮验证复查章节顺序和可追溯字段，避免新 UI 接入清单变成旁路 Markdown。

“目标效果图 + 已提供切图”推荐主线：

```text
GenerateProvidedSkinAssetCropSpecBatch
ValidateProvidedSkinAssetCropsBatch
GenerateProvidedSkinReviewPackageBatch
```

`GenerateProvidedSkinReviewPackageBatch` 体检通过后会登记切图、生成布局、清理旧 `_v2.prefab` 草稿、生成新草稿、验证绑定、截图、执行 package gate，并写出 `review-package.md`。成功态 `review-package.md` 应标记 `Package Gate: Passed`，并保留可直接复用的 `Re-run` 命令。截图预览入口命令行运行时不要加 `-nographics`。

没有真实目标图或切图时，宿主可提供 synthetic 冒烟入口，例如 `GenerateSyntheticProvidedSkinReviewPackageBatch`，由宿主生成临时目标图和匹配尺寸切图后复用同一条 provided-crops review package 链路；自定义 synthetic 输出目录仍必须在 `Assets` 下。

已提供切图目录必须是 `Assets` 下的文件夹。交付清单入口允许目录尚未创建，用于先出美术交付规格；交付清单、体检、一键验收包和调试拆步入口都应阻断误选 PNG 文件或 `Assets` 外路径。体检、登记、草稿和一键验收入口遇到目录不存在、误选 PNG 文件或 `Assets` 外路径时，仍要写出交付清单和 `Gate：Blocked` 的切图体检，列出必需文件名；误选文件或 `Assets` 外路径的下一步先提示修正 `-uiInputImageFolder`。交付清单和体检报告都应列出文件、源区域、参考矩形和 `Expected Size`；交付清单还应保留可直接复用的 `Validate Command`，体检报告还应保留可直接复用的 `Re-run Check` 命令；detected-layout、asset-crops 和 skin-layout 摘要应保留 `Manifest` 及对应 JSON 路径，校验时也会复读 generated JSON 本体；runtime cleanup 和 post-cleanup 报告应保留 `Manifest` 以及带引号的 `Re-run Check`、`Re-run Checklist` 或 `Re-run Readiness` 命令；binding 报告应保留可直接复用的 `Re-run Binding` 命令，binding/visual 子报告应保留可回到一键验收入口的 `Review Package Command`；现有 provided-crops、visual、binding 和 apply 子报告会被换皮验证复查章节顺序、重跑入口与来源字段；缺图、解码失败、尺寸过小或尺寸不等必须阻断。一键验收包遇到输入、切图 gate 或 package gate 阻断时仍要写 `Gate：Blocked` 的 `review-package.md`；prefab 类 package gate 至少覆盖草稿 prefab 已存在、复制源 prefab 失败、清理旧草稿失败和拒绝清理非生成 prefab，预览类 package gate 至少覆盖预览图片解码失败、缺失、空白、过小和纯色，并保留带引号的重跑命令。

`Assets/UIAITools/Skinning` 下的 skin JSON 必须由 `skin.json` 或 manifest.generated 路径覆盖；新增 skin JSON 要登记加载和校验逻辑，不能只写入工作包。

## 常用 executeMethod

| 功能 | 常用入口 |
| --- | --- |
| 自动整理 | `Run`、`ValidateReports`、`GenerateSummary`、`GeneratePanelFocus` |
| 复用反查 | `SearchReuseByImageBatch -uiQueryImage`、`GenerateReuseSearchSummaryBatch` |
| 自动制作 | `GenerateComponentCandidateIndexBatch`、`GenerateUICreationBriefTemplateBatch`、`GenerateUILayoutDraftTemplateBatch`、`DryRunUICreationLayoutDraftBatch`、`GenerateUICreationHostGenerateChecklistBatch` |
| 新版换皮 | `GenerateSkinHostAdapterChecklistBatch`、`GenerateSkinManifestBatch`、`GenerateProvidedSkinAssetCropSpecBatch`、`ValidateProvidedSkinAssetCropsBatch`、`GenerateProvidedSkinReviewPackageBatch`、`GenerateSyntheticProvidedSkinReviewPackageBatch`、`UseProvidedSkinAssetCropsBatch`、`GenerateProvidedSkinPrefabDraftBatch`、`ValidateSkinBindingBatch`、`CaptureSkinPreviewBatch`、`ValidateSkinPackageBatch` |
| 全局动态资源更新 | `GenerateGlobalRuntimeUpdateTemplateBatch -uiBatchName`、`ValidateGlobalRuntimeUpdateBatch -uiBatchName` |
| 宿主专项维护 | `ValidateVipcardTitleTextBindingBatch`、`ValidateVipcardSpriteBackfillBatch`、`ValidateVipcardSpriteBackfillReportContractBatch` |
| 契约自检 | `ValidateUIToolsMasterPlanBatch`、`ValidateProfileContractBatch`、`ValidateReportFilesContractBatch`、`ValidateSkinContractBatch`、`ValidateWorkbenchContractBatch` |

其它调试入口以 `UIAssetTriageScanner` 源码为准。

`UIAIToolsReports/Scanning` 下的 `UIAIToolsSummary.md` 和 `UIAIToolsPanelFocus.md` 应保留来源 CSV、校验入口和重跑入口；`UIAIToolsReports/ReuseSearch` 下的 `UIReuseSearchSummary.md` 应保留查询图、`UIReuseSearchResults.csv` 和带引号的 `Re-run Search` 命令。

宿主 master plan 会校验现有非 skin Markdown 报告的章节顺序，覆盖扫描摘要、panel focus、复用反查摘要、组件候选摘要、layout dry-run 摘要、host generate checklist/result、global runtime checklist 和 UIVipcard 宿主专项报告。

宿主草稿生成后的 `UICreationHostGenerateResult.md` 应保留 `Layout Draft JSON`、目标 prefab、结果 CSV、host checklist、layout dry-run、组件候选 review，以及带引号的 `Re-run Generate` / `Re-run Validate` 命令。`UIAIToolsReports/GlobalRuntimeUpdates/<BatchName>/checklist.md` 应保留 batch 名、工作包、`replace-map.csv`、`Images` 目录，以及带引号的 `Re-run Template` / `Re-run Validate` 命令。

宿主 master plan 会复查已存在真实 CSV 的表头和可读性，覆盖扫描核心 CSV、`UIReuseSearchResults.csv`、组件候选 CSV、layout dry-run、host generate result、skin `generate-result.csv` 和 global runtime `replace-map.csv`；新增真实 CSV 必须纳入该校验，改 CSV 字段时要同步生成逻辑、验证逻辑和实物报告。

`UIAIToolsReports/Creation` 下已存在的 Creation brief/layout template JSON 也会被 master plan 复读；新增 Creation JSON 必须登记对应加载和校验逻辑。

宿主专项维护报告也要避免旁路孤岛；例如 `UIVipcardSpriteBackfillReport.md` 应保留目标 prefab、相关 prefab、报告路径、带引号的 `Re-run Report` / `Re-run Validate` 命令和空 Sprite 列表。报告和校验入口只读 prefab，不应修改正式资源；真正 apply 必须单独人工确认。

UIVipcard 标题文字绑定专项报告 `UIVipcardTitleTextBindingCheck.md` 应保留源 prefab、报告路径、可直接复用的 `Re-run Check` 命令、标题文本节点和每行状态，用于检查标题背景和 TMP 文本绑定没有被 prefab 结构调整破坏。

## 宿主职责

- 决定菜单路径、工作台 UI、命令行入口和默认配置资产。
- 按功能给 `profile.logRoot` 加子目录。
- 提供组件候选人工确认流程和组件 prefab。
- 为具体界面实现皮肤槽位、规则识别、切图登记和独立新版 prefab 生成器；新 UI 先生成 `host-adapter-checklist.md`。
- 任何正式资源替换、SpriteAtlas 或 YooAsset 修改都必须在宿主项目独立确认后执行。

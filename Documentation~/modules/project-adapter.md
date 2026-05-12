# 项目适配模块

本包不直接绑定业务菜单。包内只提供通用宿主工作区初始化菜单和不依赖业务框架的换皮 Play Mode 预览窗口；宿主项目仍只写薄包装：加载 `UIAIToolsProfile`、`UIControlCatalog`，把工作台按钮或 batch 参数转成包 API 调用。

## 默认路径

```csharp
const string ProfilePath = "Assets/UIAITools/Settings/UIAIToolsProfile.asset";
const string CatalogPath = "Assets/UIAITools/Settings/UIControlCatalog.asset";
```

```text
Assets/UIAITools/Skinning/<UIName>
Assets/UIAITools/Creation/<FeatureName>
Assets/UIAITools/GlobalRuntimeUpdates/<BatchName>

Assets/UIAITools/Reports/Scanning
Assets/UIAITools/Reports/ReuseSearch
Assets/UIAITools/Reports/Creation
Assets/UIAITools/Reports/Skinning/<UIName>
Assets/UIAITools/Reports/GlobalRuntimeUpdates
```

新版换皮宿主生成的独立 prefab 草稿固定为：

```text
Assets/UIAITools/Skinning/<UIName>/Prefabs/<SourcePrefabName>_v2.prefab
```

## 当前入口

包会提供一个通用初始化菜单：

```text
Tools/UIAITools/初始化宿主工作区
```

包会提供一个通用换皮运行时预览窗口：

```text
Tools/UIAITools/新版换皮/运行时预览窗口
```

这个窗口只在 Unity Editor Play Mode 内工作。它读取 `skin.json` 的 `generated.outputPrefabPath`，用 `AssetDatabase` 加载 `_v2.prefab`，挂到包内临时 Screen Space Overlay Canvas；不调用宿主 `OpenPanel`、不注册宿主 UI 管理器、不替换正式 prefab。选中 `skin.json` 时也可以从 Project 右键菜单 `Assets/UIAITools/运行时预览 skin.json` 打开。退出 Play Mode、按 Esc 或点击窗口里的关闭按钮会销毁预览实例。

宿主推荐另行只暴露一个业务工作台菜单：

```text
Tools/UIAITools/打开工作台
```

工作台组织四个页签：自动整理、复用反查、自动制作、新版换皮。旧自动设计效果图和资源替换实验链路不再作为入口。

## Batch 参数

- 资源路径使用 Unity 资产路径，例如 `Assets/...`。
- 纯报告写到 `UIAIToolsProfile.logRoot`；新接入默认是 `Assets/UIAITools/Reports/...`。
- `-uiPrefabPath`：现有 UI prefab。
- `-uiOutputFolder`：可选；不传时宿主按 `UIAIToolsProfile.workspaceRoot` 推导；自动制作 UI 必须在 `<workspaceRoot>/Creation/` 下，新版换皮必须在 `<workspaceRoot>/Skinning/` 下。
- `-uiQueryImage`：PNG/JPG 裁图。
- `-uiCreationBriefJsonPath`、`-uiLayoutDraftJsonPath`：自动制作 UI JSON。
- `-uiSkinManifestPath`：新版换皮 `skin.json`。
- `-uiArtPackageFolder`：可视化换皮主流程使用的项目外美术资源目录；宿主复制到 `<workspaceRoot>/Skinning/<UIName>/Source` 和 `Textures` 后再进入 provided-crops 验收。
- `-uiConceptPath`、`-uiInputImageFolder`、`-uiCleanupSourcePath`：换皮目标图、已提供切图目录、运行时清理源图。
- `-uiBatchName`：全局动态资源更新批次。
- 参数名后必须跟随值，不能把下一个已知参数名当作值。

## 当前换皮主线

新 UI 或非 UIVipcard 起步：

```text
GenerateSkinHostAdapterChecklistBatch -uiPrefabPath <Prefab>
```

它只读源 prefab，写出 `<logRoot>/Skinning/<UIName>/host-adapter-checklist.md`，不生成 prefab 或图片。
报告应包含带引号的 `Re-run Checklist` 命令，方便从接入清单复跑同一 prefab 和输出目录。
现有 `host-adapter-checklist.md` 会被换皮验证复查章节顺序和可追溯字段，避免新 UI 接入清单变成旁路 Markdown。

可视化换皮推荐主线只暴露源 prefab 和项目外美术资源目录，按钮为 `一键导入并生成换皮验收包`。宿主应优先选择美术目录根部文件名包含 `效果图`、`效果圖`、`preview`、`concept` 或 `target` 的 PNG/JPG/JPEG 作为目标效果图，没有命名命中时再选择目录内面积最大图片；优先从 `slices`、`Slices`、`切图`、`textures`、`Textures` 子目录读取可选切图，并用 AI/语义映射把任意命名、任意尺寸、任意拆法的美术交付归一化成内部槽位文件。美术按运行时画面和功能设计即可，不需要照旧工程切图结构；旧工程两张图拼成的背景，新美术可以是一张整图或新的拆图方式。宿主不要从效果图自动裁切缺失素材；缺少可匹配切图时生成透明占位图并在报告标记缺失，方便让美术重新导出。AI 超时或不可用时，重跑不得覆盖上一轮可见工作区素材，应先标记低置信兜底或保留旧图并提示重新 AI 映射。宿主输出 `art-package-import.md` 和 `art-package-map.md` 后调用 provided-crops 验收链路。外部美术目录只读，重跑只覆盖 `Assets/UIAITools/Skinning/<UIName>` 下的复制件。

宿主生成 prefab 前必须先判定 RectTransform 所有权。目标效果图负责新静态视觉坐标；源 prefab 的 `LayoutGroup`、`ContentSizeFitter`、`AspectRatioFitter`、动画、脚本或框架自定义布局组件，凡是会在编辑器或运行时改写子节点位置、尺寸、缩放的，都视为布局写入者。如果这些组件服务的是旧视觉排版，生成器应把新视觉放到不受它们驱动的节点下，或在复制草稿里禁用对应布局写入；如果它们服务的是真实运行时数据列表或框架通用控件，则可以保留，但新生成的子节点必须主动服从该布局，而不是同时手写坐标。这个判断是通用规则，不应按某个 UI 的奖励区、资源栏或按钮区逐条沉淀；只有游戏框架或基础包里的通用控件、通用预设，才单独写控件级规则。

AI/识图结果只作为槽位识别和初始参考，不直接当最终布局真相。能被美术切图在效果图中稳定匹配的位置优先信切图匹配；AI 只补匹配不到、语义映射需要判断、或模板匹配已知会误吸附的槽位。卡片底、奖励底、资源底等容器背景不要用整块像素硬匹配内部内容；内部常被运行时文字、图标或道具遮挡，应优先用外框、边缘、角标和稳定装饰定位。AI 返回的矩形不能直接当最终显示尺寸，宿主 adapter 应按真实切图尺寸、九宫/等比策略和画布边界收敛，避免 AI 把角色、图标或底板估大导致 prefab 越界或漂移。重复结构、对称结构、资源组、页签组、卡片标题和文字组应再按语义约束归一化，例如共享基线、同组中心线、固定间距、卡片中心、卡片内顶部偏移或容器边界；顶部锚点等边缘锚点必须把图片坐标换算为 RectTransform 中心点，图标按所属底板内容带等比留边后再对齐组中心线；具体偏移常量留在宿主 adapter，通用文档只记录判断方法。

宿主 adapter 生成运行时文本前必须校验 TMP 字体和材质是否能渲染目标预览文案。不能假设正式项目里的静态 TMP 字体资产已经包含美术目标语言的全部字形；如果缺字形或材质 atlas 不匹配，应在 `Assets/UIAITools/Skinning/<UIName>/Generated` 下生成皮肤专用动态 TMP 字体和对应普通/描边材质，并把缺失字形写入生成结果或 gate。描边要按文字颜色和底色极性选择：白字在深色按钮/徽章上使用深色描边，深色字只有落在花纹或较深装饰底上才使用浅色描边，浅色底上的深色信息字保持无描边。资源数、价格、底部标签、标题等运行时文字框要同时跟随效果图槽位和最小可读尺寸；binding gate 的宽高阈值必须与生成器的最小尺寸一致。

内部调试或 batch 可继续使用“目标效果图 + 已提供切图”拆步主线：

```text
GenerateProvidedSkinAssetCropSpecBatch
ValidateProvidedSkinAssetCropsBatch
GenerateProvidedSkinReviewPackageBatch
```

`GenerateProvidedSkinReviewPackageBatch` 体检通过后会登记切图、生成布局、清理旧 `_v2.prefab` 草稿、生成新草稿、验证绑定、截图、执行 package gate，并写出 `review-package.md`。成功态 `review-package.md` 应标记 `Package Gate: Passed`，并保留可直接复用的 `Re-run` 命令。截图预览入口命令行运行时不要加 `-nographics`。

没有真实目标图或切图时，宿主可提供 synthetic 冒烟入口，例如 `GenerateSyntheticProvidedSkinReviewPackageBatch`，由宿主生成临时目标图和匹配尺寸切图后复用同一条 provided-crops review package 链路；自定义 synthetic 输出目录仍必须在 `Assets` 下。

已提供切图目录必须是 `Assets` 下的文件夹。交付清单入口允许目录尚未创建，用于先出内部槽位规格；交付清单、体检、一键验收包和调试拆步入口都应阻断误选 PNG 文件或 `Assets` 外路径。体检、登记、草稿和一键验收入口遇到目录不存在、误选 PNG 文件或 `Assets` 外路径时，仍要写出交付清单和 `Gate：Blocked` 的切图体检，列出必需文件名；误选文件或 `Assets` 外路径的下一步先提示修正 `-uiInputImageFolder`。交付清单和体检报告都应列出文件、源区域、参考矩形和 `Expected Size`；交付清单还应保留可直接复用的 `Validate Command`，体检报告还应保留可直接复用的 `Re-run Check` 命令；detected-layout、asset-crops 和 skin-layout 摘要应保留 `Manifest` 及对应 JSON 路径，校验时也会复读 generated JSON 本体；runtime cleanup 和 post-cleanup 报告应保留 `Manifest` 以及带引号的 `Re-run Check`、`Re-run Checklist` 或 `Re-run Readiness` 命令；binding 报告应保留可直接复用的 `Re-run Binding` 命令，binding/visual 子报告应保留可回到一键验收入口的 `Review Package Command`；现有 provided-crops、visual、binding 和 apply 子报告会被换皮验证复查章节顺序、重跑入口与来源字段；缺图、解码失败或尺寸过小必须阻断，尺寸不等于参考槽位时提示按效果图槽位适配但不阻断。宿主 adapter 应按新制作生成视觉骨架：目标效果图和美术切图决定静态视觉，源 Prefab 只提供功能绑定、热区、运行时文案和数据节点；图标、角色和徽章按槽位等比缩放，允许放大或缩小；按钮、底板、资源条和页签等框体按槽位九宫适配；会写 RectTransform 的 Unity 或框架布局组件必须先完成所有权判定，避免旧布局和新视觉坐标互相抢占。一键验收包遇到输入、切图 gate 或 package gate 阻断时仍要写 `Gate：Blocked` 的 `review-package.md`；prefab 类 package gate 至少覆盖草稿 prefab 已存在、复制源 prefab 失败、清理旧草稿失败和拒绝清理非生成 prefab，预览类 package gate 至少覆盖预览图片解码失败、缺失、空白、过小和纯色，并保留带引号的重跑命令。

`Assets/UIAITools/Skinning` 下的 skin JSON 必须由 `skin.json` 或 manifest.generated 路径覆盖；新增 skin JSON 要登记加载和校验逻辑，不能只写入工作包。

## 常用 executeMethod

| 功能 | 常用入口 |
| --- | --- |
| 自动整理 | `Run`、`ValidateReports`、`GenerateSummary`、`GeneratePanelFocus` |
| 复用反查 | `SearchReuseByImageBatch -uiQueryImage`、`GenerateReuseSearchSummaryBatch` |
| 自动制作 | `GenerateComponentCandidateIndexBatch`、`GenerateUICreationBriefTemplateBatch`、`GenerateUILayoutDraftTemplateBatch`、`DryRunUICreationLayoutDraftBatch`、`GenerateUICreationHostGenerateChecklistBatch` |
| 新版换皮 | `GenerateSkinReviewPackageFromArtPackageBatch -uiArtPackageFolder`、`GenerateSkinHostAdapterChecklistBatch`、`GenerateSkinManifestBatch`、`GenerateProvidedSkinAssetCropSpecBatch`、`ValidateProvidedSkinAssetCropsBatch`、`GenerateProvidedSkinReviewPackageBatch`、`GenerateSyntheticProvidedSkinReviewPackageBatch`、`UseProvidedSkinAssetCropsBatch`、`GenerateProvidedSkinPrefabDraftBatch`、`ValidateSkinBindingBatch`、`CaptureSkinPreviewBatch`、`ValidateSkinPackageBatch` |
| 全局动态资源更新 | `GenerateGlobalRuntimeUpdateTemplateBatch -uiBatchName`、`ValidateGlobalRuntimeUpdateBatch -uiBatchName` |
| 宿主工作区 | `Xipin.UIAITools.UIAIToolsHostWorkspaceInitializer.EnsureBatch` |
| 宿主专项维护 | `ValidateVipcardTitleTextBindingBatch`、`ValidateVipcardSpriteBackfillBatch`、`ValidateVipcardSpriteBackfillReportContractBatch` |
| 契约自检 | `Xipin.UIAITools.UIAIToolsHostWorkspaceInitializer.ValidateContract`、`ValidateUIToolsMasterPlanBatch`、`ValidateProfileContractBatch`、`ValidateReportFilesContractBatch`、`ValidateSkinContractBatch`、`ValidateWorkbenchContractBatch` |

其它调试入口以 `UIAssetTriageScanner` 源码为准。

`<logRoot>/Scanning` 下的 `UIAIToolsSummary.md` 和 `UIAIToolsPanelFocus.md` 应保留来源 CSV、校验入口和重跑入口；`<logRoot>/ReuseSearch` 下的 `UIReuseSearchSummary.md` 应保留查询图、`UIReuseSearchResults.csv` 和带引号的 `Re-run Search` 命令。

宿主 master plan 会校验现有非 skin Markdown 报告的章节顺序，覆盖扫描摘要、panel focus、复用反查摘要、组件候选摘要、layout dry-run 摘要、host generate checklist/result、global runtime checklist 和 UIVipcard 宿主专项报告。

宿主草稿生成后的 `UICreationHostGenerateResult.md` 应保留 `Layout Draft JSON`、目标 prefab、结果 CSV、host checklist、layout dry-run、组件候选 review，以及带引号的 `Re-run Generate` / `Re-run Validate` 命令。`<logRoot>/GlobalRuntimeUpdates/<BatchName>/checklist.md` 应保留 batch 名、工作包、`replace-map.csv`、`Images` 目录，以及带引号的 `Re-run Template` / `Re-run Validate` 命令。

宿主 master plan 会复查已存在真实 CSV 的表头和可读性，覆盖扫描核心 CSV、`UIReuseSearchResults.csv`、组件候选 CSV、layout dry-run、host generate result、skin `generate-result.csv` 和 global runtime `replace-map.csv`；新增真实 CSV 必须纳入该校验，改 CSV 字段时要同步生成逻辑、验证逻辑和实物报告。

`<logRoot>/Creation` 下已存在的 Creation brief/layout template JSON 也会被 master plan 复读；新增 Creation JSON 必须登记对应加载和校验逻辑。

宿主专项维护报告也要避免旁路孤岛；例如 `UIVipcardSpriteBackfillReport.md` 应保留目标 prefab、相关 prefab、报告路径、带引号的 `Re-run Report` / `Re-run Validate` 命令和空 Sprite 列表。报告和校验入口只读 prefab，不应修改正式资源；真正 apply 必须单独人工确认。

UIVipcard 标题文字绑定专项报告 `UIVipcardTitleTextBindingCheck.md` 应保留源 prefab、报告路径、可直接复用的 `Re-run Check` 命令、标题文本节点和每行状态，用于检查标题背景和 TMP 文本绑定没有被 prefab 结构调整破坏。

## 宿主职责

- 决定业务工作台 UI、命令行入口和项目专属默认配置资产。
- 按功能给 `profile.logRoot` 加子目录。
- 提供组件候选人工确认流程和组件 prefab。
- 为具体界面实现皮肤槽位、规则识别、切图登记和独立新版 prefab 生成器；新 UI 先生成 `host-adapter-checklist.md`。
- 任何正式资源替换、SpriteAtlas 或 YooAsset 修改都必须在宿主项目独立确认后执行。

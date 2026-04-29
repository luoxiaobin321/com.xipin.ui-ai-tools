# 扫描模块

扫描模块入口是 `UIAssetScanService`，用于生成 UI 图片归类、图集依赖、复用索引和静态 DrawCall 风险报告。

## 入口

```csharp
UIAssetScanService.Run();
UIAssetScanService.Run(profile);
UIReportValidationService.Validate(profile);
UIScanSummaryService.Generate(profile);
UIScanSummaryService.GeneratePanelFocus(profile);
```

默认 profile 会扫描：

- `Assets/Art/UI`
- `Assets/Bundle/UIAtlas`
- `Assets/Bundle/UITexture`
- `Assets/Bundle/Prefab`

扫描只写 CSV 报告，不移动资源、不改 prefab、不改 SpriteAtlas。扫描入口写完核心 CSV 后会立即调用 `UIReportValidationService.Validate`，让表头或行结构错误在扫描 batch 内失败。
`UIReportValidationService.Validate` 用于检查核心 CSV 是否都已生成、不是空文件、表头匹配且数据行能按契约解析，适合接到宿主项目 batchmode 验证入口。
`UIScanSummaryService.Generate` 用于按需生成 `UIAIToolsSummary.md`，汇总现有 CSV 的分布、Top 面板、断批、空 Sprite 和下一步建议，并校验固定 Markdown 标题结构；它只在菜单或 batchmode 显式调用时运行。
`UIScanSummaryService.GeneratePanelFocus` 用于按需生成 `UIAIToolsPanelFocus.md`，把 Top 面板的静态 batch 指标、断批摘要、直挂散图和空 Sprite 样例整理成实测前清单，并按当前 Top 面板顺序校验动态 Markdown 标题结构。

## 推荐阅读顺序

先看结论类报告：

- `UIAssetTriageReport.csv`：每张图片的归类建议。
- `UIAssetTriagePlan.csv`：低风险迁移计划草稿。
- `UIPrefabOptimizationTargets.csv`：prefab 优化优先级。
- `UIReuseIndex.csv`：已有图片复用索引。

定位具体问题时再看明细：

- `UIPrefabAtlasStats.csv`：每个 prefab 依赖的图集和散图数量。
- `UIPrefabImageDetails.csv`：prefab 到图片的明细关系。
- `UIPrefabAtlasBreakdown.csv`：prefab 按图集拆解。
- `UIPrefabDrawCallRisk.csv`：静态 DrawCall 风险汇总。
- `UIPrefabBatchSequence.csv`：Graphic 顺序和 batch key。
- `UIPrefabBatchBreaks.csv`：相邻断批点。
- `UIPrefabBatchBreakSummary.csv`：prefab 级断批汇总。
- `UIAIToolsSummary.md`：手动生成的扫描摘要和下一步建议。
- `UIAIToolsPanelFocus.md`：手动生成的 Top 面板实测清单。

专项报告按需读取：

- `UIACommonUsage.csv`：通用图集使用频次。
- `UITextureSizeReport.csv`：散图尺寸分布。
- `UIDuplicateImageReport.csv`：同内容重复图。
- `UIPrefabTextureSwitchPairs.csv`：重复纹理切换 pair。
- `UIPrefabWhiteTextureBreaks.csv`：白纹理和空 Sprite 相关断批。
- `UIPrefabNullSpriteImages.csv`：可见启用空 Sprite Image。
- `UILooseTextureCandidates.csv`：prefab 直接引用 UITexture 散图候选。

## 判断口径

- `Art/UI` 默认视为设计目录，不进入运行时 Bundle。
- 已被 SpriteAtlas 收集的 `UIAtlas` 图片建议保留图集。
- 未被 SpriteAtlas 收集的 `UIAtlas` 图片会提示补进图集。
- 大背景、大 banner、插画、按名加载风险图优先保留 `UITexture`。
- 多界面稳定复用的小图才建议进入 `ACommon`。
- 静态 DrawCall 报告只看 prefab 结构、Graphic 顺序、材质、纹理和 Canvas，不替代 Frame Debugger 实测。

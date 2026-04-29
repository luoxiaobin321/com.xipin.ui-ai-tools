# 扫描管线内部结构

`UIAssetScanService.Run` 是当前扫描管线入口。它按固定顺序收集事实并写出 CSV。

## 主流程

1. 创建 `Profile.logRoot`。
2. `FindTextures` 收集 `Art/UI`、`UIAtlas`、`UITexture` 下的图片。
3. `BuildAtlasMap` 建立图片到 SpriteAtlas 的映射。
4. `BuildPrefabMap` 建立图片到 prefab 引用的映射。
5. `BuildTextMap` 建立文件文本到图片名的弱引用映射。
6. 计算图片名重复和 SHA1 内容重复。
7. 写图片归类报告和迁移计划草稿。
8. 写 prefab、图集、复用、重复图和静态 DrawCall 风险报告。
9. 立即复用 `UIReportValidationService.Validate` 校验 18 份核心 CSV 的表头和行结构。

## 模块职责

- 资源发现：只找 Unity AssetDatabase 能识别的图片、prefab 和 SpriteAtlas。
- 归属判断：从路径 owner、atlas owner、prefab owner、文本引用和尺寸推断。
- DrawCall 风险：读取 prefab 下启用且可见的 `Graphic`，按 Canvas、纹理和材质构造静态 batch key。
- 空 Sprite 分析：只记录可见启用、alpha 大于 0 且 sprite 为空的 `Image`。
- 复用反查：把查询图和候选图采样后算相似度，分数越低越相近。

## 文件拆分

- `UIAssetScanService.cs` 保留主扫描流程。
- `UIAssetScanService.AssetReports.cs` 保留图片、prefab、图集、重复图和复用索引 CSV 写出。
- `UIAssetScanService.Discovery.cs` 保留资源发现、SpriteAtlas 映射、prefab 依赖映射和文本弱引用映射。
- `UIAssetScanService.Classification.cs` 保留路径归属、尺寸分级、资源类型和建议短句判断。
- `UIAssetScanService.MigrationPlan.cs` 保留 L1 迁移计划草稿生成，不执行资源改动。
- `UIAssetScanService.ReuseSearch.cs` 保留按截图反查已有图片、图片采样评分和复用建议判断。
- `UIAssetScanService.DrawCallReports.cs` 保留静态 DrawCall、断批、空 Sprite、直挂散图候选报告写出。
- `UIAssetScanService.DrawCallAnalysis.cs` 保留 batch key、断批原因、白纹理、文本材质和空 Sprite 分类辅助函数。
- `UIAssetScanService.Csv.cs` 保留 CSV 转义和列表展示格式。
- `UIAssetScanService.Models.cs` 保留扫描内部临时数据结构。
- `UIReportFiles.cs` 保留 CSV 文件名和表头契约，`UIReportValidationService.cs` 保留报告完整性检查；扫描入口写完核心 CSV 后会立即复用该验证。
- `UIReportCsv.cs` 保留 CSV 读取工具，供摘要和 AI Brief 复用。
- `UIScanSummaryService.cs` 读取已生成 CSV，按需写出 `UIAIToolsSummary.md` 和 `UIAIToolsPanelFocus.md`；扫描摘要会校验固定 Markdown 标题结构，面板实测清单会按当前 Top 面板顺序校验动态标题结构，不参与扫描写表流程。

## 项目差异入口

`TextFiles` 从 `UIAIToolsProfile.textSearchRoots` 读取文本引用扫描目录，默认保持当前项目的 `Assets/Scripts`、`Assets/Bundle/Config`、`Assets/Bundle/Setting`。动态图片、按钮热区和功能性空 Image 通过 `UIControlCatalog` 的角色配置识别。

## 修改原则

- 新报告优先复用已有事实映射，不重复遍历全项目。
- 改字段名会影响外部表格和 AI 阅读流程，先看 `report-contracts.md`。
- 静态 DrawCall 结果是排序和定位依据，不写成运行时真实性能结论。
- 扫描服务不做资源迁移，迁移只能由宿主项目确认流程执行。

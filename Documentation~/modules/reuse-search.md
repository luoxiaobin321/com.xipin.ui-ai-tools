# 复用图片反查模块

复用反查用于从效果图裁剪图查找工程里相似的已有 UI 图片，适合美术没有单独交付某个旧元素时使用。

## 入口

编辑器交互入口：

```csharp
UIImageSearchService.SearchReuseByImage();
UIImageSearchService.SearchReuseByImage(profile, catalog);
```

批处理入口：

```csharp
UIImageSearchService.SearchReuseByImageBatch();
UIImageSearchService.SearchReuseByImageBatch(profile, catalog);
```

批处理需要命令行参数：

```text
-uiQueryImage <png-or-jpg-path>
```

`-uiQueryImage` 后必须跟图片路径，不能把下一个已知 Unity 或 UI 工具参数名当作路径。

## 输出

结果写入 `UIReuseSearchResults.csv`，并同步生成 `UIReuseSearchSummary.md`。写出后会立即复验表头、CSV 行结构、候选资源路径、图集路径、prefab 引用路径和 Markdown 顶层标题顺序。核心字段：

| 字段 | 含义 |
| --- | --- |
| `Query` | 查询图片路径。 |
| `Path` | 候选图片路径。 |
| `Score` | 相似度分数，越低越接近。 |
| `Kind` | 候选图片属于图集、散图或其他类型。 |
| `Atlas` | 候选图片所属 SpriteAtlas。 |
| `AssetOwner` | 依据路径推断的资源归属。 |
| `PrefabRefs` | 直接引用候选图片的 prefab。 |
| `Advice` | 复用建议。 |
| `Reason` | 建议原因。 |

`UIReuseSearchSummary.md` 会保留查询图、结果 CSV 和带引号的 `Re-run Search` 命令，并汇总建议分布和 Score 最低的候选。

## 使用建议

- 先裁出尽量干净的缺图区域，避免把背景或文字一起裁进去。
- 优先复用 `ACommon` 小图。
- 其他功能图集里的专属图不要直接跨功能借用；确认为通用后再升到公共图集。
- 大图、banner、立绘和按名加载图片优先保留在 `UITexture`。

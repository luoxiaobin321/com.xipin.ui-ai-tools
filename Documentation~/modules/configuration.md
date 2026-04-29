# 配置模块

配置模块负责把项目差异收进 ScriptableObject，避免包代码直接依赖业务工程。

## UIAIToolsProfile

通过 `Create > Xipin > UI AI Tools > Profile` 创建。
默认路径、文本扫描根和地图排除开关可用 `ValidateProfileContractBatch` 复验。

| 字段 | 默认值 | 用途 |
| --- | --- | --- |
| `prefabRoot` | `Assets/Bundle/Prefab` | 扫描 UI prefab 的根目录。 |
| `artUIRoot` | `Assets/Art/UI` | 设计图、源图和参考图目录。 |
| `uiAtlasRoot` | `Assets/Bundle/UIAtlas` | SpriteAtlas 和图集图片目录。 |
| `uiTextureRoot` | `Assets/Bundle/UITexture` | 大图、散图和按名加载图片目录。 |
| `mapTextureRoot` | `Assets/Bundle/MapTexture` | 地图图片目录。 |
| `logRoot` | `Logs` | CSV 报告输出目录。 |
| `yooAssetAddressRule` | `AddressByFileName` | 记录当前 YooAsset 地址规则。 |
| `textSearchRoots` | `Assets/Scripts`、`Assets/Bundle/Config`、`Assets/Bundle/Setting` | 按文件名弱匹配图片引用的文本目录。 |
| `excludeMapFromUITriage` | `true` | UI 审计时排除地图图片。 |

调用时可以直接传入 profile：

```csharp
UIAssetScanService.Run(profile);
```

需要同时覆盖控件角色时传入 catalog：

```csharp
UIAssetScanService.Run(profile, catalog);
```

## UIControlCatalog

通过 `Create > Xipin > UI AI Tools > Control Catalog` 创建。它按类型名识别控件角色，不要求包编译引用业务控件。扫描服务会用它识别动态图片组件、按钮热区和功能性空 Image。
默认角色映射可用 `ValidateControlCatalogContractBatch` 复验，覆盖大小写不敏感匹配和基础负例。

| 角色 | 默认类型名 |
| --- | --- |
| 动态图片 | `ImagePlus`, `RawImagePlus`, `ResView`, `ItemView`, `TopResView` |
| 文本 | `TextPlus`, `TMP_Text` |
| 按钮 | `Button`, `ButtonPlus`, `UIActionButton`, `TogglePlus` |
| 滚动 | `ScrollViewPlus`, `ScrollRectPlus` |
| 面板 | `UIPanel`, `CanvasPanelBase`, `CanvasPanelItemBase` |
| 功能性空 Image | `BackgroundPlus`, `GuideMask`, `HighlightStencil`, `SpriteAtlasAnimator`, `Mask`, `CanvasGroup` |

## 配置边界

项目专属路径、控件名和地址规则应进入 profile 或 catalog。包内代码不要直接依赖宿主项目程序集，也不要把业务控件写成强类型引用。

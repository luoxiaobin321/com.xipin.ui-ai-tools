# 配置模块

配置模块负责把项目差异收进 ScriptableObject，避免包代码直接依赖业务工程。

## UIAIToolsProfile

通过 `Create > Xipin > UI AI Tools > Profile` 创建。
默认路径、文本扫描根和地图排除开关可用 `ValidateProfileContractBatch` 复验。
新接入项目也可以直接使用菜单 `Tools/UIAITools/初始化宿主工作区`，包会创建 `Assets/UIAITools/Settings/UIAIToolsProfile.asset` 和 `Assets/UIAITools/Settings/UIControlCatalog.asset`，并把新 profile 的报告根设为 `Assets/UIAITools/Reports`。

| 字段 | 默认值 | 用途 |
| --- | --- | --- |
| `workspaceRoot` | `Assets/UIAITools` | 工具工作区，保存配置、制作/换皮工作包和需要 Unity 导入的工具资产。 |
| `prefabRoot` | `Assets/Bundle/Prefab` | 扫描 UI prefab 的根目录。 |
| `artUIRoot` | `Assets/Art/UI` | 设计图、源图和参考图目录。 |
| `uiAtlasRoot` | `Assets/Bundle/UIAtlas` | SpriteAtlas 和图集图片目录。 |
| `uiTextureRoot` | `Assets/Bundle/UITexture` | 大图、散图和按名加载图片目录。 |
| `mapTextureRoot` | `Assets/Bundle/MapTexture` | 地图图片目录。 |
| `logRoot` | 类默认 `UIAIToolsReports`；初始化器创建值为 `Assets/UIAITools/Reports` | CSV/Markdown/JSON 报告输出根目录。新接入推荐放进 `Assets/UIAITools/Reports`，方便删除包时一并清理。 |
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

Profile 和 Catalog 推荐放在 `Assets/UIAITools/Settings`。`Assets/Art/UI` 只作为项目美术扫描根，不作为工具输出目录。

## 全局 AI 配置

通用工作台顶部提供 `全局 AI 配置`，用于整个 UIAITools，而不是某个换皮页签的局部设置。

| 配置 | 用途 |
| --- | --- |
| `AI API Key` | OpenAI Responses API Key；保存到当前机器 EditorPrefs，不写入项目资产。 |
| `AI 接口地址` | Responses API 地址，默认 `https://api.openai.com/v1/responses`；也支持填 base url，工具会补齐 `/v1/responses`。 |
| `AI 模型` | 默认 `gpt-5.5`，可按项目需要改成当前可用视觉模型。 |

读取优先级：全局 `UIAITools.AI.*` EditorPrefs、旧换皮 `UIAITools.Skinning.OpenAI.*` EditorPrefs、环境变量、Codex `config.toml`、默认值。保存全局配置时也会同步写旧换皮 key，方便仍保留旧宿主换皮窗口的项目继续读取同一份配置。

## 宿主工作区初始化

`UIAIToolsHostWorkspaceInitializer` 会在交互式 Unity Editor 加载包后自动补齐宿主工作区，也可以通过菜单手动重跑。
命令行或 CI 可直接执行 `Xipin.UIAITools.UIAIToolsHostWorkspaceInitializer.EnsureBatch`。

```text
Assets/UIAITools
Assets/UIAITools/Settings
Assets/UIAITools/Skinning
Assets/UIAITools/Creation
Assets/UIAITools/GlobalRuntimeUpdates
Assets/UIAITools/Reports
Assets/UIAITools/Docs
Assets/UIAITools/Docs/Training/Host
Assets/UIAITools/Docs/Training/PackageCandidates
```

初始化器只写包专用目录，不移动业务资源，不修改正式 prefab、SpriteAtlas 或 YooAsset 配置。移除包时，删除 UPM 依赖和 `Assets/UIAITools` 即可清理干净。

## 训练沉淀路由

工作台 `训练沉淀` 页签把经验分成两类：

- `宿主专项`：写到 `Assets/UIAITools/Docs/Training/Host`，用于当前项目路径、业务控件、UI 专项规则和临时经验。
- `包内通用候选`：写到 `Assets/UIAITools/Docs/Training/PackageCandidates`，表示可能进入 `com.xipin.ui-ai-tools` 的跨项目规则。训练记录不会直接改 `Packages/com.xipin.ui-ai-tools` 内的源码、文档或文件名；后续由包维护流程提升到包仓库。

历史训练和旧文档用 `自动维护旧沉淀` 处理。它会读取 `Assets/UIAITools/Docs`、`Assets/UIAITools/Reports` 和当前包内 Markdown，输出 `Assets/UIAITools/Docs/Training/legacy-triage.md`，并自动维护 `Host/legacy-auto-maintained.md`、`PackageCandidates/legacy-auto-maintained.md` 和 `PackageCandidates/package-maintenance-items.md`；这个过程不移动旧文件，也不修改包内文件名。

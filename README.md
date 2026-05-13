# 自动化界面工具

`com.xipin.ui-ai-tools` 是独立 Unity Editor UPM 包，GitHub 仓库为 `https://github.com/luoxiaobin321/com.xipin.ui-ai-tools.git`。它用于 UI 图片归类、图集审计、复用图片反查、prefab 静态 DrawCall 风险分析、自动制作 UI 输入链路，以及新版换皮 prefab 生成的通用契约和报告。

继续开发本包时，先读 `HANDOFF.md`。当前 `E:\Work\UIAIToolsClient` 只是测试宿主，提供真实 prefab、图集、图片、profile、catalog 和 batch wrapper；宿主根目录不作为包仓库提交。

## 包边界

- 只包含 Unity Editor 工具，不包含 player 运行时代码；Play Mode 预览也由 Editor 程序集提供。
- 不编译引用 `GameApp`、`MotionFramework`、`com.xipin.lframework` 或 YooAsset。
- 项目差异通过 `UIAIToolsProfile`、`UIControlCatalog` 和宿主薄包装接入。
- 包会初始化 `Assets/UIAITools` 宿主工作区，把配置、工作包、报告和宿主契约集中到一个可删除目录。
- 包内提供 `Tools/UIAITools/打开工作台` 通用 Editor 工作台。
- 包内默认只生成 CSV、Markdown、JSON、Prompt、manifest 和 gate 报告，不直接修改 prefab、图片、SpriteAtlas 或 YooAsset 配置。

## 能力范围

- 资源治理：图片归类、图集审计、复用反查、prefab 依赖和 DrawCall 静态风险报告。
- 通用工作台：自动整理、复用反查、自动制作和新版换皮的包内入口。
- 新版换皮 prefab 生成：定义 `skin.json`、`detected-layout.json`、`asset-crops.json`、`skin-layout.json` 等通用契约；宿主负责具体 prefab 生成和业务 gate。
- 新版换皮运行时预览：在 Unity Editor Play Mode 中读取 `skin.json`；可用临时 Canvas 查看静态运行效果，也可临时热替换 `sourcePrefabPath` 让游戏原 UI 入口真实加载 `_v2.prefab`，退出 Play 后自动恢复。
- 自动制作 UI 输入链路：需求 Brief、组件候选索引、布局草稿、资源需求清单、prefab 生成前 dry-run、宿主确认清单和宿主生成结果只读校验。

## 文档入口

- `Documentation~/README.md`：接入者入口和能力地图。
- `Development~/README.md`：开发维护入口。
- `Development~/modules/package-boundary.md`：独立包边界。
- `Documentation~/modules/project-adapter.md`：宿主包装方式。

## Codex Skill

包内提供 `Skills~/ui-ai-tools`，用于让 Codex 按模块读取本包文档并辅助处理自动整理、复用反查、自动制作 UI、新版换皮和包代码维护。需要启用时，把该目录安装或复制到当前 Codex 的 skills 目录。

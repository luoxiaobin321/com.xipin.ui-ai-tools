# Xipin UI AI Tools 使用文档

这是独立 UPM 包 `com.xipin.ui-ai-tools` 的对外接入文档，面向要在 Unity 项目里使用本包的人。当前测试宿主是 `E:\Work\UIAIToolsClient`；正式接入项目应通过 UPM git 引用或嵌入式包引用本仓库。

## 能力地图

- 配置项目路径、日志目录和控件角色：读 `modules/configuration.md`。
- 配置整个工具共用的大模型能力：在 `Tools/UIAITools/打开工作台` 顶部展开 `全局 AI 配置`，细节读 `modules/configuration.md`。
- 区分宿主专项与包内通用候选训练记录：在 `Tools/UIAITools/打开工作台` 的 `训练沉淀` 页签记录，细节读 `modules/configuration.md`。
- 安装或修复宿主工作区：使用菜单 `Tools/UIAITools/初始化宿主工作区`，细节读 `modules/configuration.md`。
- 打开通用工作台：使用菜单 `Tools/UIAITools/打开工作台`，细节读 `modules/project-adapter.md`。
- 生成 UI 图片、图集、prefab 和 DrawCall 静态审计报告：读 `modules/scanning.md`。
- 从效果图裁剪图反查工程已有图片：读 `modules/reuse-search.md`。
- 规划自动制作 UI 的输入契约、布局草稿和 dry-run：读 `modules/ui-creation.md`。
- 设计宿主 UI prefab 草稿生成器：读 `modules/ui-creation-host-generator.md`。
- 在 Unity Editor Play Mode 里预览新版换皮 prefab：菜单 `Tools/UIAITools/新版换皮/运行时预览窗口`，细节读 `modules/project-adapter.md`。
- 给项目加专项菜单、默认配置或命令行入口：读 `modules/project-adapter.md`。
- 已有 UI 新版换皮走 `Editor/Skinning` 契约、包内通用工作台和宿主专项适配；旧自动设计效果图和资源替换实验链路已删除。

## 包定位

本包只提供 Unity Editor 侧工具，不包含 player 运行时代码。它负责收集事实、输出 CSV/Markdown/JSON 报告、提供皮肤契约、自动制作 UI 契约、dry-run、gate 和 Editor Play Mode 预览；是否生成 prefab、移动资源、替换正式图集或修改 YooAsset 配置，由宿主项目的确认流程负责。

## 接入要求

- Unity 2022.3 或兼容版本。
- 依赖 `com.unity.ugui` 和 `com.unity.textmeshpro`。
- 宿主项目需要提供 UI prefab、UIAtlas、UITexture、Art/UI 和工具工作区等路径约定；包会在交互式 Unity Editor 中自动补齐 `Assets/UIAITools` 宿主工作区。
- 宿主项目应写薄包装调用本包 API，避免把业务程序集依赖引入包内。

## 默认输出

扫描报告默认写到 `UIAIToolsProfile.logRoot`。类内置默认值仍保持 `UIAIToolsReports` 以兼容旧接入；包初始化器创建的新宿主 profile 使用 `Assets/UIAITools/Reports`，让配置、工作包、报告和宿主契约都集中在 `Assets/UIAITools`。核心明细报告为 UTF-8 BOM CSV；摘要、dry-run、确认清单和生成结果汇总为 Markdown，模板和机器契约另有 JSON。

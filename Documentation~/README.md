# Xipin UI AI Tools 使用文档

这是独立 UPM 包 `com.xipin.ui-ai-tools` 的对外接入文档，面向要在 Unity 项目里使用本包的人。当前测试宿主是 `E:\Work\UIAIToolsClient`；正式接入项目应通过 UPM git 引用或嵌入式包引用本仓库。

## 能力地图

- 配置项目路径、日志目录和控件角色：读 `modules/configuration.md`。
- 生成 UI 图片、图集、prefab 和 DrawCall 静态审计报告：读 `modules/scanning.md`。
- 从效果图裁剪图反查工程已有图片：读 `modules/reuse-search.md`。
- 接 AI 改版 provider，生成改版草稿、dry-run、待确认执行计划、外部生成输入包、单项 Prompt、引用素材清单、宿主执行清单、宿主执行结果汇总和 manifest：读 `modules/ai-redesign.md`。
- 理解换皮、自动制作 UI、prefab 自动截图和 AI 生成图的最终工作流：读 `modules/visual-generation-workflow.md`。
- 设计宿主确认后执行器：读 `modules/host-apply-executor.md`。
- 规划自动制作 UI 的输入契约、布局草稿和 dry-run：读 `modules/ui-creation.md`。
- 设计宿主 UI prefab 草稿生成器：读 `modules/ui-creation-host-generator.md`。
- 给项目加菜单、默认配置或命令行入口：读 `modules/project-adapter.md`。

## 包定位

本包只提供 Unity Editor 侧工具，不包含运行时代码。它负责收集事实、输出 CSV/Markdown/JSON 报告、提供 AI 草稿协议、dry-run、待确认执行计划、外部生成输入包、引用素材清单、宿主执行清单、宿主执行结果只读校验和 manifest；是否移动资源、替换 prefab、调整图集和修改 YooAsset 配置，由宿主项目的确认流程或宿主执行器负责。

## 接入要求

- Unity 2022.3 或兼容版本。
- 依赖 `com.unity.ugui` 和 `com.unity.textmeshpro`。
- 宿主项目需要提供 UI prefab、UIAtlas、UITexture、Art/UI 等路径约定。
- 宿主项目应写薄包装调用本包 API，避免把业务程序集依赖引入包内。

## 默认输出

扫描报告默认写到 `UIAIToolsProfile.logRoot`，内置默认值是 `Logs`。核心明细报告为 UTF-8 BOM CSV；摘要、改版包、外部输入包、Prompt 清单、引用素材清单、宿主执行清单和宿主执行结果汇总为 Markdown，外部生成输入包另有 JSON。

# com.xipin.ui-ai-tools 开发规则

本文件约束独立 UPM 包 `com.xipin.ui-ai-tools`。当前源码仓库只包含本目录，远端为 `https://github.com/luoxiaobin321/com.xipin.ui-ai-tools.git`。

## 中文名与交接

- 本包中文名是“自动化界面工具”。
- 包内 `HANDOFF.md` 是继续开发本包的权威交接入口。
- `E:\Work\UIAIToolsClient` 是测试宿主，宿主根文档只说明测试环境，不代表包状态。

## 包边界

- 包保持可复用，不编译引用 `GameApp`、`MotionFramework`、`com.xipin.lframework`、YooAsset 或其他项目业务程序集。
- 项目路径、日志目录、YooAsset 地址规则通过 `UIAIToolsProfile` 表达。
- 项目控件类型差异通过 `UIControlCatalog` 或字符串角色识别表达，不在包内直接依赖业务控件类型。
- 菜单、batch 参数适配、最终资源写入和 prefab 生成执行器放在宿主项目。

## 代码原则

- 默认只做确定性扫描、报告、草稿和 gate，不自动执行资源迁移或 prefab 覆盖。
- AI 改版能力只能输出 `UIRedesignDraft`、`UIReplacementPlan`、外部输入包和风险项；真正替换必须由宿主确认流程执行。
- 新增公共 API 要有明确调用方，不添加占位扩展点。
- 内部函数之间信任契约，不补防御性 null 检查；只在文件选择、命令行参数、外部 provider 返回等边界做验证。

## 文档原则

- 对外接入文档放 `Documentation~`，面向使用者和接入项目。
- 开发维护文档放 `Development~`，面向继续改包的人。
- 改代码影响 CSV 字段、JSON 契约、Markdown 段落、调用入口或包边界时，同步更新对应模块文档。

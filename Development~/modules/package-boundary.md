# 包边界

`com.xipin.ui-ai-tools` 已作为独立 UPM 包维护。当前测试宿主是 `E:\Work\UIAIToolsClient`，但包仓库只包含 `Packages/com.xipin.ui-ai-tools` 下的文件。

## 可以进入包内

- 纯 Editor 工具、扫描服务、报告生成、JSON/CSV/Markdown 契约验证。
- 与项目无关的数据结构、dry-run、gate、manifest、外部生成输入包和 Prompt 报告。
- 通过 `UIAIToolsProfile`、`UIControlCatalog` 表达的路径、控件角色和项目规则。
- 不依赖业务程序集的 Unity API、UGUI、TextMeshPro 能力。

## 必须留在宿主

- 菜单和 batch 薄包装。
- 业务程序集、业务控件类型、MotionFramework、GameApp、YooAsset 直接调用。
- 真正移动资源、写 prefab、改 SpriteAtlas、改 YooAsset 配置的执行器。
- 测试样本、真实 prefab、profile、catalog、截图和日志。

## 维护规则

- 新公共 API 必须有明确宿主调用场景。
- 包内不得为了某个宿主硬编码业务路径以外的业务语义；路径差异进入 profile，控件差异进入 catalog。
- 变更报告字段、JSON 契约、Markdown 段落或 gate 语义时，同步更新 `Documentation~` 与 `Development~` 对应模块。

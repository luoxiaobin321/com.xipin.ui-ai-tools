# 自动制作 UI 契约边界

自动制作 UI 当前先稳定输入、草稿、dry-run 和宿主生成结果契约。包内不创建或覆盖 prefab；测试宿主可在确认 gate 后生成 prefab 草稿样例，并用结果报告回归验证。

## 包内可以做

- 根据扫描报告生成组件候选索引、人工确认清单，并生成 UI 需求 Brief 模板。
- 定义 `UICreationBrief`、组件库索引、`UILayoutDraft` 和资源需求清单的 JSON/CSV 契约。
- 生成 prefab 前 dry-run 和人工确认清单。
- 复用 `UIAIToolsProfile`、`UIControlCatalog`、扫描报告和 Markdown 汇总模式。

## 包内不能做

- 创建、覆盖或保存 prefab。
- 复制、移动或删除图片资源。
- 修改 SpriteAtlas、YooAsset、脚本绑定或业务配置。
- 假设宿主项目的运行时框架、数据绑定框架或组件基类。

## 推荐推进顺序

1. 固化 `Documentation~/modules/ui-creation.md` 中的输入和输出契约。
2. 先用 `UIComponentCandidateIndexService` 从 `UIPrefabBatchSequence.csv`、`UIControlCatalog` 和明确节点命名 token 推导组件候选索引；滚动容器只从自身节点或直接 `Viewport` 行推导；汇总里把 Button 候选按跨 prefab 和单 prefab 或低复用拆成复核队列，并输出 `UIComponentCandidateReview.csv` 给宿主填写确认结果，重新生成时按 `ComponentId` 保留人工填写列，稳定后再由宿主人工维护组件 prefab 和预览图。
3. 用 `UICreationBriefTemplateService` 生成只读需求 Brief JSON 模板，写盘时保留必需空数组字段并立即读回，读取时校验根对象、Brief 字符串根字段及其字面量结束、字符串转义、数组字段值结束、重复契约字段、可选布尔 `requiresConfirmation`、字符串数组项和尾随内容。
4. 用 `UILayoutDraftTemplateService` 生成只读 `UILayoutDraft` 示例和资源需求清单初稿，写盘时保留必需空数组字段并立即读回，读取时校验根对象、`root` 对象与内部字符串字段及其字面量结束、字符串转义、对象/数组字段值结束、重复契约字段、字符串数组项、`nodes`/`assets` 数组项关键字符串字段、布尔 `requiresConfirmation` 和尾随内容。
5. 用 `UICreationLayoutDryRunService` 实现 prefab 生成前 dry-run，检查目标目录、目标 prefab、参考分辨率、`requiresConfirmation`、组件候选 ID、组件角色、节点树、文本来源、锚点/位置格式、组件状态和角色兼容性、尺寸格式、节点数据绑定声明、数据绑定覆盖率、节点资源路径、资源需求 ID 唯一性、资源需求 kind/status、资源需求路径、资源需求 Ready 状态、Error gate 和汇总 Markdown 标题结构；验证入口先复验 dry-run CSV 精确表头和行结构。
6. 用 `UICreationHostGenerateChecklistService` 输出宿主生成前确认清单，读取当前 dry-run 前先复验 `UICreationLayoutDryRun.csv` 精确表头和行结构，并确认当前 dry-run 的 `TargetPrefab` 和组件列表与本次布局草稿一致，布局引用的组件候选已在 `UIComponentCandidateReview.csv` 中 `Approved`；验证入口必须看到 `Gate：Passed`，并重新检查 Markdown 标题结构、当前 dry-run 目标、清单组件列表和当前组件确认状态才放行。
7. 由宿主项目实现确认后的 prefab 草稿生成器；宿主生成结果 CSV 应复验精确表头、状态、目标 prefab 一致性、当前草稿目标、NodeId/ComponentId 归属、每个草稿节点至少一条结果行、确认记录、失败说明、节点动作行和行内派生字段，基础 Markdown 汇总可由包内 `UICreationHostGenerateResultService.GenerateSummary` 生成，当前样例固定为目标、状态分布和下一步，生成后复验会比对父子层级、布局、文本、静态图片、绑定占位和预览统计。

## 验证边界

每次新增自动制作 UI 能力时，先确认它只写报告或草稿文件。出现 prefab、图片、图集或 YooAsset 改动时，代码必须在宿主项目里，并经过单独的人工确认入口。

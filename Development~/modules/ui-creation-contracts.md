# 自动制作 UI 契约边界

自动制作 UI 当前先稳定输入、草稿、dry-run 和宿主生成结果契约。包内不创建或覆盖 prefab；测试宿主可在确认 gate 后生成 prefab 草稿样例，并用结果报告回归验证。

## 包内可以做

- 生成组件候选索引、人工确认清单、需求 brief 模板和 layout draft 模板。
- 定义 `UICreationBrief`、`UILayoutDraft`、组件候选、资源需求、dry-run 和生成结果报告契约。
- 生成 prefab 前 dry-run 和宿主生成前确认清单。
- 复用 `UIAIToolsProfile`、`UIControlCatalog`、扫描报告和 Markdown 汇总模式。

## 包内不能做

- 创建、覆盖或保存 prefab。
- 复制、移动或删除图片资源。
- 修改 SpriteAtlas、YooAsset、脚本绑定或业务配置。
- 假设宿主项目运行时框架、数据绑定框架或组件基类。

## 维护锚点

- 使用文档：`Documentation~/modules/ui-creation.md`
- 宿主生成器契约：`Documentation~/modules/ui-creation-host-generator.md`
- 报告字段和 Markdown 结构：`Development~/modules/report-contracts.md`
- 主要服务：`UIComponentCandidateIndexService`、`UICreationBriefTemplateService`、`UILayoutDraftTemplateService`、`UICreationLayoutDryRunService`、`UICreationHostGenerateChecklistService`、`UICreationHostGenerateResultService`

## 推进顺序

1. 保持 brief、layout draft 和 dry-run 只写 JSON/CSV/Markdown。
2. 新增字段时同步读取校验、写出模板、报告契约和验证入口。
3. 宿主生成前必须确认 `UICreationLayoutDryRun.csv` 与当前 layout draft 一致，引用组件在 `UIComponentCandidateReview.csv` 中为 `Approved`，且 `UICreationHostGenerateChecklist.md` 明确 `Gate：Passed`。
4. 自动制作 UI 的 Markdown 汇总必须保留输入 CSV/JSON 路径和重跑/校验入口，避免报告脱离生成上下文后无法接力。
5. 宿主生成结果必须复验 CSV 精确表头、状态、目标 prefab、一致的 NodeId/ComponentId、每个草稿节点的执行行、确认记录、失败说明、Markdown 标题和预览统计。

出现 prefab、图片、图集或 YooAsset 改动时，代码必须在宿主项目里，并经过单独人工确认入口。

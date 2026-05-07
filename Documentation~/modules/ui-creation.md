# 自动制作 UI 契约

自动制作 UI 是后续能力线，当前只稳定输入、dry-run、报告和安全边界。包内不创建 prefab、不导入资源、不覆盖现有界面；真正 prefab 生成器放宿主项目。

## 流程

1. 生成或维护组件候选索引：`UIComponentCandidateIndex.csv`、`UIComponentCandidateReview.csv`。
2. 生成需求 brief 模板：`UICreationBriefTemplateService.Generate`。
3. 生成 layout draft 模板：`UILayoutDraftTemplateService.Generate`。
4. 跑生成前 dry-run：`UICreationLayoutDryRunService.Run`、`ValidateNoErrors`。
5. 生成宿主确认清单：`UICreationHostGenerateChecklistService.Generate`、`ValidateNoBlockingSteps`。
6. 宿主在确认 gate 通过后生成草稿 prefab；生成器契约见 `ui-creation-host-generator.md`。

## 关键契约

- `UICreationBrief` 描述功能名、UI 类型、目标目录、参考图、交互、数据绑定和约束。
- `UILayoutDraft` 描述 root、nodes、assets、interactions、risks，并且 `requiresConfirmation` 必须为 `true`。
- 组件候选来自扫描结果和人工 review，不能由 AI 凭空捏造。
- 资源需求必须清到 `Ready` 才能进入宿主生成。
- dry-run 只输出 `UICreationLayoutDryRun.csv` 和 `UICreationLayoutDryRunSummary.md`，不创建 prefab；汇总报告要保留 `Layout Draft JSON`、dry-run CSV、组件候选 CSV、review CSV 和重跑/校验入口。
- 组件候选汇总 `UIComponentCandidateIndexSummary.md` 要保留候选 CSV、review CSV、来源 batch sequence CSV 和重跑/校验入口。
- 宿主确认清单必须明确 `Gate：Passed`，并且当前 dry-run、layout draft 和组件 review 状态一致；清单要保留 `Layout Draft JSON`、dry-run CSV、组件候选 review CSV 和重跑 checklist / dry-run 的入口。

## 阻断边界

dry-run 至少阻断这些问题：目标目录越界、目标 prefab 已存在、组件候选缺失、资源未 Ready、节点 ID 或同父节点重名、父节点缺失、锚点/位置/尺寸格式非法、未知组件角色、非法状态、文本或数据绑定来源不明确、资源路径不是合法 `Assets/...`。

## 安全边界

包内能力止步于 brief、组件候选、layout draft、资源需求、dry-run 和确认清单。任何 prefab、图片、SpriteAtlas、YooAsset、脚本绑定或业务配置写入都必须在宿主项目中实现，并经过单独确认入口。

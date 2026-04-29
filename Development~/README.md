# Xipin UI AI Tools 开发文档

这是独立 UPM 包 `com.xipin.ui-ai-tools` 的开发维护文档。入口页只做路由，处理哪个模块就读哪个模块。

## 按需加载

- 判断哪些依赖能进包、哪些必须留在宿主：读 `modules/package-boundary.md`。
- 改 `UIAssetScanService` 扫描流程：读 `modules/scanning-internals.md`。
- 改 CSV 文件名、字段、Markdown 摘要或含义：读 `modules/report-contracts.md`。
- 改 AI provider、request、draft、replacement plan、外部输入包或 Prompt 报告：读 `modules/ai-contracts.md`，报告字段和 Markdown 段落同步读 `modules/report-contracts.md`。
- 设计宿主确认后执行器时，先读 `Documentation~/modules/host-apply-executor.md`，执行器实现仍放宿主项目。
- 推进自动制作 UI 前，先读 `modules/ui-creation-contracts.md`。
- 设计宿主 UI prefab 草稿生成器时，先读 `Documentation~/modules/ui-creation-host-generator.md`，生成器实现仍放宿主项目。

## 当前结论

包已经迁为独立 Git 仓库开发，测试宿主是 `E:\Work\UIAIToolsClient`。包仓库只提交 `Packages/com.xipin.ui-ai-tools` 下的文件；宿主 wrapper、真实 UI 资源、profile、catalog 和 batch 日志只用于验证。

## 文档分层

- `Documentation~`：对外接入文档，写给使用者。
- `Development~`：开发维护文档，写给改包的人。
- 包根 `README.md`：只写定位、边界和文档索引。
- 包根 `AGENTS.md`：写 Codex 和开发者必须遵守的包级规则。

# com.xipin.ui-ai-tools Handoff

## Current Goal
把宿主集成能力收进包内：新项目安装 `com.xipin.ui-ai-tools` 后，由包自动/手动初始化 `Assets/UIAITools`，让配置、工作包、报告、控件契约和可复用候选沉淀都集中在一个可删除目录。

## Status
当前测试宿主已替换为正式客户端镜像，旧宿主完整备份在 `E:\Work\UIAIToolsClient_backup_20260509_115240`。宿主锁定的 `com.xipin.lframework` 已从旧提交 `345552334bf319d8dd2074a4370de450b4c23864` 更新到正式客户端锁定的 `56f8e93d3ecfa4a27dc01919c1cf5b5f734b676c`。`Packages/com.xipin.ui-ai-tools` 已从备份恢复到新宿主中，作为当前工具开发包继续使用；它在正式客户端根仓库里是本地未跟踪目录，不代表正式项目依赖已提交。

真实 `UIVipcard` 美术包 `E:\Work\Er\Art\UI\會員卡界面` 已在旧宿主中完成冷启动验证：清理 `Assets/UIAITools/Skinning/UIVipcard` 和旧报告目录后可重新生成，31/31 provided-crops 通过，skin contract 和 master plan 通过。正式客户端宿主也已冷启动重跑同一美术包，生成物位于 `Assets/UIAITools/Skinning/UIVipcard`，旧轮次报告位于历史 `UIAIToolsReports/Skinning/UIVipcard`；新接入应通过包初始化器使用 `Assets/UIAITools/Reports/Skinning/<UIName>`。

包内已新增 `UIAIToolsHostWorkspaceInitializer`。交互式 Unity Editor 加载包时会自动补齐 `Assets/UIAITools`；菜单 `Tools/UIAITools/初始化宿主工作区` 和 batch 入口 `Xipin.UIAITools.UIAIToolsHostWorkspaceInitializer.EnsureBatch` 可手动重跑。初始化器只创建包专用目录、默认 `UIAIToolsProfile`、`UIControlCatalog`、`Reports` 和 `Docs` 索引；已有 profile 只在 `workspaceRoot` 为空或 `logRoot` 为空/旧 `UIAIToolsReports` 时修复，不重置项目自定义字段。

沉淀规则已重新分层：通用机制写进 `Documentation~/modules/project-adapter.md` 和 `Skills~/ui-ai-tools/SKILL.md`；具体 UI 个案只保留为验证证据，不再把某个奖励区、资源栏或按钮区的问题直接写成长期规则。换皮 prefab 生成前必须先判断 RectTransform 所有权：目标效果图负责新静态视觉坐标；`LayoutGroup`、`ContentSizeFitter`、`AspectRatioFitter`、动画、脚本或框架自定义布局组件凡是会改写 RectTransform，都视为布局写入者。旧视觉排版写入者应隔离或在草稿中禁用；真实运行时数据列表或框架通用控件可以保留，但新生成节点必须服从该布局，不能同时手写坐标。只有游戏框架或基础包里的通用控件、通用预设才单独沉淀控件级规则。

新增通用机制沉淀：宿主 adapter 不能信任项目内静态 TMP 字体已包含目标语言字形。正式客户端的 `FontOtf SDF.asset` 缺少部分繁中文字形，导致标题、价格符号和底部标签预览断字；生成器现在应在换皮工作包 `Generated` 下生成皮肤专用动态 TMP 字体和匹配普通/描边材质，并在重跑发现字体 atlas 丢失时重建字体资产。文字描边要按文字颜色和底色极性选择材质：白字深色底用深色描边，深色字只有落在花纹或较深装饰底上才用浅色描边，浅色底信息字保持无描边。运行时文本框还要同时满足效果图槽位和最小可读尺寸，生成规则与 binding gate 阈值必须一致。

新增通用机制沉淀：AI 坐标不是最终布局真相。能被美术切图在效果图中稳定匹配的位置优先信切图匹配；AI 只补奖励底板、免广告说明等匹配不到或模板容易误吸附的槽位。AI 返回的矩形尺寸必须按真实切图尺寸、九宫/等比策略和画布边界收敛，避免角色或图标被估大后越界。当前正式宿主 `UIVipcard` 已按该规则重跑，最新生成日志为 `Logs/GenerateRealArt_UIVipcard_ai_sparse_20260509_170011.log`。

新增通用机制沉淀：顶部锚点等边缘锚点不能把图片左上角直接当 RectTransform 坐标。宿主 adapter 应先换算中心点，再让同组图标按底板内容带等比留边并对齐中心线；正式宿主 `UIVipcard` 顶部资源图标已按 `resource_count_bg` 内容带收敛并重跑预览。

新增通用机制沉淀：重复卡片标题要共享卡片内顶部偏移或标题中心线，不能只按各自 AI/切图识别框独立摆放。正式宿主 `UIVipcard` 终身会员标题已从贴近卡片顶边改为跟月度会员标题使用一致的卡片内纵向关系。

## Key Files
- `Documentation~/modules/project-adapter.md`：宿主 adapter 接入、外部美术包主线、RectTransform 所有权规则。
- `Editor/Config/UIAIToolsHostWorkspaceInitializer.cs`：包内宿主工作区初始化器，自动/菜单创建 `Assets/UIAITools`、默认 profile、catalog、Reports 和按需契约文档入口。
- `Skills~/ui-ai-tools/SKILL.md`：工具使用入口和当前换皮优先级。
- `Development~/modules/report-contracts.md`：报告字段和章节约束。
- `../Docs/AI/Modules/UI.md`：正式客户端 UI 开发与换皮规则入口。
- `../Docs/UIToolsMasterPlan.md`、`../Docs/SkinningProvidedCropsQuickStart.md`：正式客户端宿主缺失后已从旧宿主恢复的工具契约文档。
- `../Assets/Scripts/GameApp/Editor/GameApp/Skinning/UIVipcardSkinWorkflow.cs`：宿主 UIVipcard 专项生成器，仍只属于测试宿主/正式客户端侧。

## Next Steps
1. 提交包改动时只在 `Packages/com.xipin.ui-ai-tools` 仓库内 stage/commit，不要把测试宿主根目录的脏改动混入包提交。
2. 正式工程接入包后先跑 `Xipin.UIAITools.UIAIToolsHostWorkspaceInitializer.EnsureBatch` 或打开 Editor，让 `Assets/UIAITools` 自动补齐。
3. 后续 UI 换皮、新功能制作或通用预设抽取时，把宿主专属控件契约和候选记录写到 `Assets/UIAITools/Docs`，不要散到项目根文档。

## Run / Test
- 旧宿主冷启动生成通过：`Logs/GenerateRealArt_UIVipcard_20260509_cold_restart_stable_title.log`。
- 旧宿主 `UIAssetTriageScanner.ValidateSkinContractBatch` 冷启动回归通过：`Logs/ValidateSkinContract_20260509_cold_restart_stable_title.log`。
- 旧宿主 `UIAssetTriageScanner.ValidateUIToolsMasterPlanBatch` 冷启动回归通过：`Logs/ValidateMasterPlan_20260509_cold_restart_stable_title.log`。
- 正式客户端宿主 UIVipcard 外部美术包冷启动生成通过：`Logs/GenerateRealArt_UIVipcard_fresh_fontfix_20260509_130544.log`。
- 正式客户端宿主 `UIAssetTriageScanner.ValidateSkinContractBatch` 通过：`Logs/ValidateSkinContract_finalhost_fontfix_20260509_131024.log`。
- 正式客户端宿主 `UIAssetTriageScanner.ValidateUIToolsMasterPlanBatch` 通过：`Logs/ValidateMasterPlan_finalhost_fontfix_20260509_140721.log`。
- 正式客户端宿主顶部资源图标中心线和留边规则生成通过：`Logs/GenerateRealArt_UIVipcard_top_resource_icon_fit_20260509_182457.log`；回归通过：`Logs/ValidateSkinContract_contract_ignore_20260509_185325.log`、`Logs/ValidateMasterPlan_top_resource_icon_fit_retry_20260509_185622.log`。
- 正式客户端宿主终身会员标题组顶部偏移归一化生成通过：`Logs/GenerateRealArt_UIVipcard_title_group_offset_20260509_191940.log`；回归通过：`Logs/ValidateSkinContract_title_group_offset_20260509_192856.log`、`Logs/ValidateMasterPlan_title_group_offset_retry_20260509_193904.log`。
- 为 master plan 收口删除了正式客户端内的旧工具残留路径 `Assets/Settings/UIAITools` 和 `Assets/Art/UI/AI`，并恢复缺失的 `Docs/UIToolsMasterPlan.md` 与 `Docs/SkinningProvidedCropsQuickStart.md`。
- 包内宿主工作区初始化契约通过：`Logs/UnityBatch_UIAIToolsHostWorkspaceInitializer_20260510_retry.log`。
- 包内宿主工作区实跑通过：`Logs/UnityBatch_UIAIToolsHostWorkspaceEnsure_20260510.log`，测试宿主创建 `Assets/UIAITools/Reports` 和 `Assets/UIAITools/Docs`，并把 profile `logRoot` 从旧 `UIAIToolsReports` 修复为 `Assets/UIAITools/Reports`。
- 包内宿主工作区幂等验证通过：`Logs/UnityBatch_UIAIToolsHostWorkspaceEnsure_Idempotent_20260510.log`，第二次执行 `新建：0`、`修复：0`。
- 包内宿主工作区最终契约通过：`Logs/UnityBatch_UIAIToolsHostWorkspaceInitializer_Final_20260510.log`。
- 包依赖边界通过：`Logs/UnityBatch_UIPackageDependencyBoundary_20260510.log`。
- 报告路径契约通过：`Logs/UnityBatch_UIReportFilesContract_ReportsRoot_20260510.log`。
- `UISkinContractService.ValidateContract` 在新报告根改造后退出码 0：`Logs/UnityBatch_UISkinContract_ReportsRoot_20260510_retry.log`。

## Constraints
- 包内不得依赖 `GameApp`、`MotionFramework`、`com.xipin.lframework` 或 YooAsset。
- 包内默认只生成报告、JSON、Markdown 和 gate，不直接写正式 prefab、图片、图集或 YooAsset 配置。
- 外部美术目录只读；重跑只覆盖 `Assets/UIAITools/Skinning/<UIName>` 工作区复制件。
- 不运行 `UIVipcardPrefabSpriteBackfill.ApplyBatch`，除非人工明确批准。

## Known Issues
- 当前正式客户端镜像的根仓库已有大量既有脏改动，`Packages/com.xipin.ui-ai-tools` 是额外恢复的本地未跟踪目录。
- 本轮为了验证包初始化器，测试宿主 `Assets/UIAITools` 下新增了 `Reports`、`Docs` 和 README/meta，并修复了测试宿主 profile 的 `logRoot`；这些属于宿主验证产物，不属于包仓库提交。
- 当前正式客户端预览的文本已完整恢复；本轮已按切图匹配优先、AI 稀疏补位和 AI 尺寸收敛重跑 `UIVipcard`，生成日志 `Logs/GenerateRealArt_UIVipcard_ai_sparse_20260509_170011.log`，skin contract `Logs/ValidateSkinContract_ai_sparse_20260509_172639.log` 和 master plan `Logs/ValidateMasterPlan_ai_sparse_20260509_173441.log` 通过。卡片内奖励组、徽章文字风格和部分图标位置仍可继续按通用机制优化。
- 其它 UI 仍需先补宿主 adapter，不能把 UIVipcard 专项逻辑泛化成包内通用实现。
- Unity batch 会触发正式客户端资产刷新；本轮看到 `Assets/Plugins/Easy Save 3/Resources/ES3/ES3Defaults.asset` 被 Unity 标记为修改，未人工处理。

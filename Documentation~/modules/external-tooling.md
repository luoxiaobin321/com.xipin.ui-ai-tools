# 外部工具清单

核心流程不强制安装额外商业软件。Unity、当前包和宿主包装入口足够完成扫描、dry-run、确认清单和 prefab 自动截图。下面是后续提高生成效果和开发效率时建议安装或评估的工具。

## 可复制清单

```powershell
# 图像处理基础依赖
python -m pip install --upgrade pip
python -m pip install pillow opencv-python

# 可选：Node 图像处理替代方案
npm install sharp
```

Unity Package Manager 按需添加：

```text
com.unity.2d.psdimporter
```

Codex 当前会话已有系统 `imagegen` skill，可用于生成风格板、参考图和图片候选；它不是项目依赖，不需要写入 Unity 工程。换机器或新 Codex 环境时，确认 `E:\Codex\.codex\skills\.system\imagegen\SKILL.md` 或对应系统 skill 可用即可。

## 换皮报告消费顺序

外部图片生成工具先读 `Logs/UIReplacementExternalPromptPack.md` 获取整体任务块和输出路径；逐项投喂时读 `Logs/UIReplacementExternalPromptItems.md` 的输出目录索引，再打开对应的 `Logs/UIReplacementExternalPrompt_*.md`。随后读 `Logs/UIReplacementExternalReferenceCopyList.md` 的“导入步骤”和“引用来源目录分组”准备旧版预览与旧图引用素材；清单中的导入名用于外部工具本地素材池命名，“复制清单”保留源路径到导入名的逐项映射。目录是否存在、新版预览、新图和目标图集是否落位，以 `Logs/UIReplacementPendingInputReadiness.csv` 和 `Logs/UIReplacementExternalInputPackage.md` 为准；单项 Prompt 内的验收规则和“外部产物落位后复跑”是交付后的复核顺序。所有这些文件都只是输入和验收清单，不会自动复制引用图、创建目录或改工程资源。

## 先装

### Python 图像处理

用途：自动切图、透明边界裁剪、尺寸检查、简单视觉 diff、九宫格候选分析。

```powershell
python -m pip install --upgrade pip
python -m pip install pillow opencv-python
```

参考：

- Pillow installation: https://pillow.readthedocs.io/en/stable/installation.html
- OpenCV pip install: https://docs.opencv.org/4.x/db/dd1/tutorial_py_pip_install.html

### Node sharp

用途：作为 Python 图像处理的替代或补充，适合批量 resize、trim、格式转换和简单资源管线脚本。

```powershell
npm install sharp
```

参考：

- sharp install: https://sharp.pixelplumbing.com/install/

### Unity prefab 自动截图

用途：用户只有 prefab、没有旧版参考图时，由 Editor 自动生成老 UI 视觉基准图。

不需要额外软件，后续在宿主/包入口实现。技术路线是 Camera 渲染到 RenderTexture 后导出 PNG。

参考：

- Camera.targetTexture: https://docs.unity3d.com/ScriptReference/Camera-targetTexture.html
- Render Texture: https://docs.unity3d.com/Manual/class-RenderTexture.html

## 按需装

### Unity 2D PSD Importer

用途：当美术提供 PSD/PSB 分层设计稿时，保留图层信息并辅助后续切图、层级和骨架/多 Sprite 工作流。

安装方式：Unity Package Manager 里添加 `com.unity.2d.psdimporter`。

参考：

- Unity 2D PSD Importer: https://docs.unity3d.com/Packages/com.unity.2d.psdimporter@latest

### TexturePacker

用途：批量散图处理、trim、pivot、spritesheet 输出和外部图集实验。

注意：当前项目已有 SpriteAtlas/YooAsset 规则，TexturePacker 只能用于离线分析或候选资源整理，不能绕过宿主确认流程直接改工程图集。

参考：

- TexturePacker documentation: https://www.codeandweb.com/texturepacker/documentation
- TexturePacker Unity support: https://www.codeandweb.com/unity-support

## 暂缓评估

### Unity MCP

用途：让 AI 通过 MCP 连接 Unity Editor，执行截图、读取对象、运行命令等操作。

当前建议暂缓接入。现阶段 Unity batch 已能稳定验证，MCP 会增加权限面、稳定性风险和供应链风险。后续若接入，只开放只读或低风险能力，例如 prefab 截图、读取层级、运行验证入口。

可评估项目：

- mcp-unity: https://github.com/codergamester/mcp-unity
- Unity-MCP: https://github.com/TruthZY/Unity-MCP

## 不作为硬依赖

- Figma/Photoshop/Aseprite 等设计软件：可以作为美术来源，但工具链不能依赖它们才能运行。
- 商业图集工具：可以提升素材整理效率，但最终导入、图集、YooAsset 变更仍必须走宿主确认。

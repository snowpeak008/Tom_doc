# Tom

Tom 是重新设计后的 Windows 便携版本地文档工程工作台，用于辅助编写玩法文档、需求文档、设计说明和说明文档。

## 打开方式

便携版输出目录：

```text
D:\workwork\cehua_doc\Tom\portable
```

打开方式：

```text
双击 Tom.exe
```

可以把 `portable` 目录压缩成 `Tom.zip`，用户解压后仍然通过 `Tom.exe` 打开。安装版输出在 `D:\workwork\cehua_doc\Tom_gre\0.1.0`，用于后续安装测试。

## 当前能力

- 独立 Windows 桌面界面，不要求用户安装 Node.js、npm 或前端开发环境。
- 默认在 `Tom.exe` 所在目录创建 `tom-docs/`，也支持在界面中手动选择项目目录。
- 多文档工作区：新建、打开、保存、另存、删除、刷新文档列表。
- 富文本编辑：撤销、恢复、标题 1、标题 2、正文、加粗、斜体、下划线、左/中/右对齐、项目符号、编号。
- 剪贴板能力：保留复制、剪切、粘贴快捷键，不在工具栏显示按钮。
- 查找与替换：逐项替换并尽量保留未替换部分的富文本格式。
- 快照：创建快照、查看快照列表、恢复快照；恢复前会自动创建当前状态快照。
- 大纲：按真实文档顺序提取 Markdown 标题、数字编号标题、中文章/节标题和富文本标题样式，并支持跳转。
- 导入：TXT、Markdown、HTML、RTF、DOCX。
- 导出：TXT、Markdown、HTML、RTF、DOCX。
- AI：Codex CLI、Claude CLI、自由指令；自由指令输入框为固定高度并带滚动条，执行按钮为主按钮。
- 选区右键 AI：选中文字后，在选区内右键可直接执行扩写、美化、总结、需求四个快捷功能。
- AI 加载状态：AI 运行中自由指令按钮会显示加载并暂时禁用，输入框仍可继续编辑；选区右键 AI 动作允许并发执行。
- AI 文体模板：工程设计说明、玩法文档、需求规格、用户手册、简洁说明。
- AI 防护：CLI 输出必须解析成固定 JSON，且通过 operation、before 和危险内容校验后才会应用。
- AI 自动应用：开关位于 AI 页签，默认开启；关闭后会先弹出修改预览。
- AI 历史：最近记录以表格显示状态、范围和摘要；双击可查看摘要、修改前、修改后和 Diff；支持一键清理，并持久化到 `tom-docs/ai-runs/`。
- 环境检测：检测 Codex CLI 与 Claude CLI；缺失时只提示，不内置安装器。
- 诊断包：一键生成 zip，包含状态、当前文档、最近 AI 记录和快照索引文件。

## 工作空间结构

```text
tom-docs/
  documents/   文档 RTF/TXT 数据
  assets/      预留的素材目录
  exports/     导出文件建议目录
  snapshots/   快照文件
  ai-runs/     AI 运行 JSON 记录
  logs/        诊断包和本地日志
  README.txt
```

## 环境边界

- Tom.exe 为 Windows 便携版。
- macOS 接口方向保留在设计层，但当前不构建 macOS 包。
- Codex CLI 和 Claude CLI 不随 Tom 内置；Tom 只检测并提示缺少环境。
- 离线可打开、编辑、保存、导入导出；使用 AI 必须联网并安装对应 CLI。
- DOCX 已支持基础文本、段落、表格文本和图片占位导入，以及基础段落导出；复杂图片、复杂表格和 Word 高级样式不承诺完整保真。
- Token/计费底层记录暂时保留，但界面入口暂不显示，等待后续重新设计。

## 构建命令

```powershell
dotnet restore .\Tom\src\Tom.App\Tom.App.csproj -r win-x64
dotnet publish .\Tom\src\Tom.App\Tom.App.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:PublishReadyToRun=false -o .\Tom\portable --no-restore
```

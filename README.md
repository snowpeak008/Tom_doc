# Tom 0.1.0

Tom 是一个 Windows 本地 AI 文档工作台，用于编写、整理和保存玩法文档、需求文档、设计说明和普通说明文档。

## 直接使用

便携版：

```text
Tom_lite/0.1.0/Tom.exe
```

解压或拉取仓库后，双击 `Tom.exe` 即可打开。Tom 会在程序所在目录创建 `tom-docs`，用于保存文档、快照、导出文件和 AI 历史。

安装版：

```text
Tom_gre/0.1.0/Tom_Setup_0.1.0.exe
```

安装后会写入 `%LOCALAPPDATA%\Tom`，并创建桌面快捷方式和开始菜单快捷方式。

## 主要功能

- 本地文档编辑、保存、新建、打开、另存和删除。
- 标题、正文、加粗、斜体、下划线、对齐、编号、项目符号。
- 查找、替换、导入 TXT/Markdown/HTML/RTF/DOCX，导出 TXT/Markdown/HTML/RTF/DOCX。
- 快照创建和恢复，大纲跳转。
- Codex CLI 和 Claude CLI 执行自由指令。
- 选中文字后，在选区内右键可直接执行扩写、美化、总结、需求。
- AI 运行中自由指令按钮会显示加载并暂时禁用，输入框仍可继续编辑；选区右键 AI 动作允许并发执行。
- AI 历史支持查看摘要、修改前、修改后和 Diff，也可以一键清理历史。

## AI 环境

Tom 不内置 Codex 或 Claude。

如果电脑已安装并登录 Codex CLI 或 Claude CLI，Tom 会自动检测并使用。没有 AI CLI 时，Tom 仍可离线编辑、保存、导入和导出文档，只是 AI 功能不可用。

## 仓库结构

```text
Tom/
  src/                         源码
  README.md                    开发和功能说明
  需求完成情况检查.md

Tom_lite/0.1.0/                0.1.0 便携版
  Tom.exe
  README.txt
  VERSION.txt
  tom-docs/
  debug-symbols/

Tom_gre/0.1.0/                 0.1.0 安装版
  Tom_Setup_0.1.0.exe
  README.txt
  VERSION.txt
  debug-symbols/
```

## 当前边界

- 当前只构建 Windows 版本。
- macOS 方向保留在设计层，暂未发布 macOS 包。
- Token/计费底层记录仍保留，但界面入口和 Usage 汇总暂时隐藏，等待后续重新设计。
- DOCX 支持基础导入导出，不承诺完整 Word 级复杂排版保真。

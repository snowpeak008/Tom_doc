Tom 用户数据目录

这个文件夹由 Tom 自动创建，用来保存你在 Tom 中产生的数据。

目录内容

documents
保存 Tom 的可编辑文档数据。通常包含 RTF 和 TXT 两种格式。

assets
预留的素材目录，用于后续保存图片、附件或导入资源。

exports
建议的导出文件目录。你导出的 TXT、HTML、DOCX 等文件可以放在这里。

snapshots
保存文档快照。恢复快照时，Tom 会从这里读取历史版本。

ai-runs
保存 AI 运行记录，包括使用 Codex 或 Claude 处理文档时的记录。

logs
保存诊断包和本地日志，用于排查问题。

使用建议

- 不要直接删除 documents、snapshots、ai-runs 目录，除非你确认不再需要这些数据。
- 迁移 Tom 时，可以把整个 tom-docs 文件夹一起复制走。
- 如果你只想备份文档，优先备份 documents 和 snapshots。
- 如果你要反馈问题，可以一起提供 logs 中的诊断包。

namespace Tom.App;

internal static class TomAiPromptBuilder
{
    private static readonly IReadOnlyDictionary<TomAiAction, string> ActionLabels = new Dictionary<TomAiAction, string>
    {
        [TomAiAction.Expand] = "扩写",
        [TomAiAction.Beautify] = "美化",
        [TomAiAction.Summarize] = "总结",
        [TomAiAction.Requirements] = "需求",
        [TomAiAction.Review] = "检查",
        [TomAiAction.Custom] = "自由指令"
    };

    public static string Build(TomAiRequest request)
    {
        var label = ActionLabels.TryGetValue(request.Action, out var value) ? value : "自由指令";
        return string.Join('\n', new[]
        {
            "你是 Tom 的本地中文文档工程助手，负责处理玩法文档、需求文档和说明文档。",
            "禁止调用工具，禁止修改文件，禁止输出 Markdown 代码块，禁止输出解释性前后缀。",
            "你必须只输出一个合法 JSON 对象。",
            "",
            "JSON 结构必须完全符合：",
            "{\"operation\":\"replace-selection|insert-at-cursor|replace-document\",\"before\":\"原始内容\",\"after\":\"修改后内容\",\"summary\":\"一句话修改说明\"}",
            "",
            "通用规则：",
            "1. 有目标文本时，operation 必须为 replace-selection，before 必须等于目标文本，after 只包含替换目标文本的新内容。",
            "2. 没有目标文本时，operation 使用 insert-at-cursor，before 为空字符串。",
            "3. 只有用户明确要求全文重写时，才能使用 replace-document。",
            "4. 总结和需求只能基于原文已有信息，不得加入原文外的推测、建议、行业常识或未出现的功能点。",
            "5. 美化是文档规范化：优化表达、层级、编号和段落，不得虚构新系统、新功能、新数值或新章节。",
            "6. 检查是工程审阅：输出问题清单、风险、缺失项和建议修改位置，但不得直接虚构解决方案。",
            "7. 不要输出 HTML、表格、代码块、引用块或“以下是”等说明性文字。",
            "",
            "本次请求：",
            $"当前功能：{label}",
            $"用户指令：{request.Instruction}",
            $"首选操作：{request.PreferredOperation}",
            "",
            "目标文本：",
            request.TargetText,
            "",
            "全文纯文本：",
            request.FullText
        });
    }
}

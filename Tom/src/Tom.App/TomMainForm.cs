using System.Drawing.Drawing2D;

namespace Tom.App;

internal sealed class TomMainForm : Form
{
    private readonly RichTextBox _editor = new();
    private readonly ListView _environmentList = new();
    private readonly TextBox _workspacePath = new();
    private readonly CheckBox _autoApplyAi = new();
    private readonly Label _statusLabel = new();
    private readonly ComboBox _providerCombo = new();
    private readonly ComboBox _styleCombo = new();
    private readonly TextBox _instructionBox = new();
    private readonly ListView _historyList = new();
    private readonly ContextMenuStrip _selectionAiMenu = new();
    private readonly ListBox _documentList = new();
    private readonly ListBox _snapshotList = new();
    private readonly ListBox _outlineList = new();
    private readonly Label _tokenSummaryLabel = new();
    private readonly ListView _tokenUsageList = new();
    private readonly Button _saveButton = new();
    private readonly Button _loadButton = new();
    private readonly Button _chooseProjectButton = new();
    private readonly Button _detectCliButton = new();
    private readonly Button _customAiButton = new();
    private readonly System.Windows.Forms.Timer _autoSaveTimer = new();
    private readonly TomAiService _aiService = new();
    private readonly List<TomAiRunRecord> _aiHistory = new();
    private readonly List<TomDocumentRecord> _documentRecords = new();
    private readonly List<TomSnapshotRecord> _snapshotRecords = new();
    private readonly List<TomOutlineItem> _outlineItems = new();
    private int _activeAiTasks;
    private TomWorkspace _workspace;
    private TomDocumentStore _store;
    private string _currentDocumentId = TomDocumentStore.DefaultDocumentId;

    public TomMainForm()
    {
        Text = "Tom";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1120, 720);
        Size = new Size(1280, 820);
        BackColor = TomColors.Surface;
        Font = new Font("Microsoft YaHei UI", 9F);

        _workspace = TomWorkspaceService.CreateDefault();
        _store = new TomDocumentStore(_workspace);

        BuildLayout();
        LoadDocumentIfPresent();
        LoadAiHistory();
        RefreshDocumentList();
        RefreshSnapshotList();
        RefreshOutline();
        ConfigureAutoSave();

        Shown += async (_, _) => await DetectCliAsync();
        FormClosing += (_, _) => SaveDocument(silent: true);
    }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(0),
            BackColor = TomColors.Surface
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        Controls.Add(root);

        root.Controls.Add(BuildHeader(), 0, 0);
        root.Controls.Add(BuildBody(), 0, 1);
        root.Controls.Add(BuildStatusBar(), 0, 2);
    }

    private Control BuildHeader()
    {
        var header = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = TomColors.Header,
            Padding = new Padding(20, 12, 20, 10)
        };

        var title = new Label
        {
            Text = "Tom",
            AutoSize = true,
            ForeColor = Color.White,
            Font = new Font("Microsoft YaHei UI", 22F, FontStyle.Bold),
            Location = new Point(20, 12)
        };
        header.Controls.Add(title);

        var subtitle = new Label
        {
            Text = "本地AI文档工作台",
            AutoSize = true,
            ForeColor = Color.FromArgb(226, 232, 240),
            Font = new Font("Microsoft YaHei UI", 9.5F),
            Location = new Point(104, 22)
        };
        header.Controls.Add(subtitle);

        return header;
    }

    private Control BuildBody()
    {
        var body = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            Padding = new Padding(14),
            BackColor = TomColors.Surface
        };
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 68));
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32));

        body.Controls.Add(BuildEditorPanel(), 0, 0);
        body.Controls.Add(BuildSidePanel(), 1, 0);
        return body;
    }

    private Control BuildEditorPanel()
    {
        var panel = new TomCardPanel { Dock = DockStyle.Fill, Padding = new Padding(14) };
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 4,
            ColumnCount = 1,
            BackColor = Color.Transparent
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 82));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
        panel.Controls.Add(layout);

        var label = new Label
        {
            Dock = DockStyle.Fill,
            Text = "Tom 文档编辑器",
            ForeColor = TomColors.Text,
            Font = new Font("Microsoft YaHei UI", 13F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };
        layout.Controls.Add(label, 0, 0);
        layout.Controls.Add(BuildEditorToolbar(), 0, 1);

        _editor.Dock = DockStyle.Fill;
        _editor.BorderStyle = BorderStyle.FixedSingle;
        _editor.Font = new Font("Microsoft YaHei UI", 11F);
        _editor.BackColor = Color.White;
        _editor.ForeColor = TomColors.Text;
        _editor.AcceptsTab = true;
        _editor.DetectUrls = false;
        _editor.Text = CreateDefaultDocumentText();
        ConfigureSelectionAiContextMenu();
        layout.Controls.Add(_editor, 0, 2);

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(0, 8, 0, 0)
        };
        _saveButton.Text = "保存 Tom 文档";
        _saveButton.Width = 132;
        _saveButton.Height = 30;
        _saveButton.Click += (_, _) => SaveDocument();
        actions.Controls.Add(_saveButton);

        _loadButton.Text = "载入已保存";
        _loadButton.Width = 112;
        _loadButton.Height = 30;
        _loadButton.Click += (_, _) => LoadDocumentIfPresent(showMissingMessage: true);
        actions.Controls.Add(_loadButton);

        layout.Controls.Add(actions, 0, 3);
        return panel;
    }

    private Control BuildEditorToolbar()
    {
        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Padding = new Padding(0, 2, 0, 2),
            BackColor = Color.Transparent
        };

        AddToolButton(toolbar, "撤销", () => { if (_editor.CanUndo) _editor.Undo(); }, 56);
        AddToolButton(toolbar, "恢复", () => { if (_editor.CanRedo) _editor.Redo(); }, 56);
        AddToolButton(toolbar, "标题1", () => ApplyHeading(18, true), 62);
        AddToolButton(toolbar, "标题2", () => ApplyHeading(14, true), 62);
        AddToolButton(toolbar, "正文", ApplyBodyStyle, 56);
        AddToolButton(toolbar, "B", () => ToggleFontStyle(FontStyle.Bold), 36);
        AddToolButton(toolbar, "I", () => ToggleFontStyle(FontStyle.Italic), 36);
        AddToolButton(toolbar, "U", () => ToggleFontStyle(FontStyle.Underline), 36);
        AddToolButton(toolbar, "左", () => _editor.SelectionAlignment = HorizontalAlignment.Left, 36);
        AddToolButton(toolbar, "中", () => _editor.SelectionAlignment = HorizontalAlignment.Center, 36);
        AddToolButton(toolbar, "右", () => _editor.SelectionAlignment = HorizontalAlignment.Right, 36);
        AddToolButton(toolbar, "项目符号", ApplyBulletList, 78);
        AddToolButton(toolbar, "编号", ApplyOrderedList, 54);
        AddToolButton(toolbar, "查找", FindText, 54);
        AddToolButton(toolbar, "替换", ReplaceText, 54);
        AddToolButton(toolbar, "导入", ImportDocument, 54);
        AddToolButton(toolbar, "导出", ExportDocument, 54);

        return toolbar;
    }

    private void ConfigureSelectionAiContextMenu()
    {
        _selectionAiMenu.Items.Clear();
        AddSelectionAiMenuItem("扩写", TomAiAction.Expand);
        AddSelectionAiMenuItem("美化", TomAiAction.Beautify);
        AddSelectionAiMenuItem("总结", TomAiAction.Summarize);
        AddSelectionAiMenuItem("需求", TomAiAction.Requirements);

        _selectionAiMenu.KeyDown += (_, args) =>
        {
            _selectionAiMenu.Close(ToolStripDropDownCloseReason.Keyboard);
            args.Handled = true;
        };

        _editor.MouseDown += (_, args) =>
        {
            if (args.Button != MouseButtons.Right)
            {
                _selectionAiMenu.Close(ToolStripDropDownCloseReason.AppClicked);
                return;
            }

            if (!ShouldShowSelectionAiMenu(args.Location))
            {
                _selectionAiMenu.Close(ToolStripDropDownCloseReason.AppClicked);
                return;
            }

            _editor.Focus();
            _selectionAiMenu.Show(_editor, args.Location);
        };
    }

    private void AddSelectionAiMenuItem(string text, TomAiAction action)
    {
        var item = new ToolStripMenuItem(text);
        item.Click += async (_, _) =>
        {
            _selectionAiMenu.Close(ToolStripDropDownCloseReason.ItemClicked);
            _editor.Focus();
            await RunAiActionAsync(action);
        };
        _selectionAiMenu.Items.Add(item);
    }

    private bool ShouldShowSelectionAiMenu(Point location)
    {
        if (string.IsNullOrWhiteSpace(_editor.SelectedText) || _editor.SelectionLength <= 0) return false;

        var index = _editor.GetCharIndexFromPosition(location);
        var selectionStart = _editor.SelectionStart;
        var selectionEnd = selectionStart + _editor.SelectionLength;
        if (index < selectionStart || index > selectionEnd) return false;

        var clickLine = _editor.GetLineFromCharIndex(index);
        var startLine = _editor.GetLineFromCharIndex(selectionStart);
        var endLine = _editor.GetLineFromCharIndex(Math.Max(selectionStart, selectionEnd - 1));
        return clickLine >= startLine && clickLine <= endLine;
    }

    private Control BuildSidePanel()
    {
        var panel = new TomCardPanel { Dock = DockStyle.Fill, Padding = new Padding(14) };
        var tabs = new TabControl
        {
            Dock = DockStyle.Fill
        };
        panel.Controls.Add(tabs);

        tabs.TabPages.Add(CreateTab("输入", BuildAiWorkspaceTab()));
        tabs.TabPages.Add(CreateTab("文档", BuildDocumentsTab()));
        tabs.TabPages.Add(CreateTab("大纲/快照", BuildOutlineSnapshotTab()));
        tabs.TabPages.Add(CreateTab("环境", BuildEnvironmentTab()));

        return panel;
    }

    private static TabPage CreateTab(string title, Control content)
    {
        var page = new TabPage(title)
        {
            BackColor = Color.White,
            Padding = new Padding(8)
        };
        page.Controls.Add(content);
        return page;
    }

    private Control BuildAiWorkspaceTab()
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 5,
            ColumnCount = 1,
            BackColor = Color.Transparent
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 60));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 40));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));

        layout.Controls.Add(SectionTitle("Tom AI 辅助"), 0, 0);
        layout.Controls.Add(BuildAiPanel(), 0, 1);
        layout.Controls.Add(BuildAiHistoryHeader(), 0, 2);

        _historyList.Dock = DockStyle.Fill;
        _historyList.View = View.Details;
        _historyList.FullRowSelect = true;
        _historyList.GridLines = false;
        _historyList.HeaderStyle = ColumnHeaderStyle.Nonclickable;
        _historyList.Columns.Add("时间", 82);
        _historyList.Columns.Add("动作", 58);
        _historyList.Columns.Add("AI", 56);
        _historyList.Columns.Add("状态", 70);
        _historyList.Columns.Add("范围", 64);
        _historyList.Columns.Add("摘要", 260);
        _historyList.DoubleClick += (_, _) => ShowSelectedAiHistory();
        layout.Controls.Add(_historyList, 0, 3);

        layout.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = "AI 输出必须经过 JSON 解析和安全防护。双击历史记录可查看修改前、修改后和 Diff。",
            ForeColor = TomColors.Muted,
            Font = new Font("Microsoft YaHei UI", 9F),
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 4);

        return layout;
    }

    private Control BuildAiHistoryHeader()
    {
        var header = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 1,
            ColumnCount = 2,
            BackColor = Color.Transparent
        };
        header.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 72));
        header.Controls.Add(SectionTitle("AI 历史"), 0, 0);

        var clear = new Button
        {
            Text = "清理",
            Width = 64,
            Height = 28,
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Margin = new Padding(0, 2, 0, 0)
        };
        clear.Click += (_, _) => ClearAiHistory();
        header.Controls.Add(clear, 1, 0);

        return header;
    }

    private Control BuildTokenUsageTab()
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 4,
            ColumnCount = 1,
            BackColor = Color.Transparent
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));

        layout.Controls.Add(SectionTitle("Token 计费查看"), 0, 0);

        _tokenSummaryLabel.Dock = DockStyle.Fill;
        _tokenSummaryLabel.ForeColor = TomColors.Text;
        _tokenSummaryLabel.TextAlign = ContentAlignment.MiddleLeft;
        layout.Controls.Add(_tokenSummaryLabel, 0, 1);

        _tokenUsageList.Dock = DockStyle.Fill;
        _tokenUsageList.View = View.Details;
        _tokenUsageList.FullRowSelect = true;
        _tokenUsageList.GridLines = false;
        _tokenUsageList.HeaderStyle = ColumnHeaderStyle.Nonclickable;
        _tokenUsageList.Columns.Add("时间", 88);
        _tokenUsageList.Columns.Add("AI", 60);
        _tokenUsageList.Columns.Add("动作", 60);
        _tokenUsageList.Columns.Add("Token", 78);
        _tokenUsageList.Columns.Add("缓存", 58);
        _tokenUsageList.Columns.Add("命中率", 64);
        _tokenUsageList.Columns.Add("来源", 60);
        _tokenUsageList.DoubleClick += (_, _) => ShowSelectedTokenUsage();
        layout.Controls.Add(_tokenUsageList, 0, 2);

        var note = new Label
        {
            Dock = DockStyle.Fill,
            Text = "CLI 返回 usage 时显示真实 token；未返回时显示估算 token，缓存命中率显示未知。",
            ForeColor = TomColors.Muted,
            Font = new Font("Microsoft YaHei UI", 9F)
        };
        layout.Controls.Add(note, 0, 3);

        return layout;
    }

    private Control BuildDocumentsTab()
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 4,
            ColumnCount = 1,
            BackColor = Color.Transparent
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));

        layout.Controls.Add(SectionTitle("文档工作区"), 0, 0);

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true
        };
        AddToolButton(actions, "新建", NewDocument, 54);
        AddToolButton(actions, "打开", OpenSelectedDocument, 54);
        AddToolButton(actions, "保存", () => SaveDocument(), 54);
        AddToolButton(actions, "另存", SaveAsDocument, 54);
        AddToolButton(actions, "删除", DeleteSelectedDocument, 54);
        AddToolButton(actions, "刷新", RefreshDocumentList, 54);
        layout.Controls.Add(actions, 0, 1);

        _documentList.Dock = DockStyle.Fill;
        _documentList.HorizontalScrollbar = true;
        _documentList.DoubleClick += (_, _) => OpenSelectedDocument();
        layout.Controls.Add(_documentList, 0, 2);

        var note = new Label
        {
            Dock = DockStyle.Fill,
            Text = "文档保存到 tom-docs/documents。RTF 保留编辑格式，TXT 用于搜索、诊断和 AI 上下文。",
            ForeColor = TomColors.Muted,
            Font = new Font("Microsoft YaHei UI", 9F)
        };
        layout.Controls.Add(note, 0, 3);
        return layout;
    }

    private Control BuildOutlineSnapshotTab()
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 6,
            ColumnCount = 1,
            BackColor = Color.Transparent
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 48));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 52));

        layout.Controls.Add(SectionTitle("大纲"), 0, 0);
        var outlineActions = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight };
        AddToolButton(outlineActions, "刷新大纲", RefreshOutline, 80);
        AddToolButton(outlineActions, "跳转", JumpToSelectedOutline, 54);
        layout.Controls.Add(outlineActions, 0, 1);

        _outlineList.Dock = DockStyle.Fill;
        _outlineList.HorizontalScrollbar = true;
        _outlineList.DoubleClick += (_, _) => JumpToSelectedOutline();
        layout.Controls.Add(_outlineList, 0, 2);

        layout.Controls.Add(SectionTitle("快照"), 0, 3);
        var snapshotActions = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight };
        AddToolButton(snapshotActions, "创建", CreateSnapshot, 54);
        AddToolButton(snapshotActions, "恢复", RestoreSelectedSnapshot, 54);
        AddToolButton(snapshotActions, "刷新", RefreshSnapshotList, 54);
        layout.Controls.Add(snapshotActions, 0, 4);

        _snapshotList.Dock = DockStyle.Fill;
        _snapshotList.HorizontalScrollbar = true;
        _snapshotList.DoubleClick += (_, _) => RestoreSelectedSnapshot();
        layout.Controls.Add(_snapshotList, 0, 5);
        return layout;
    }

    private Control BuildEnvironmentTab()
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 6,
            ColumnCount = 1,
            BackColor = Color.Transparent
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 86));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));

        layout.Controls.Add(SectionTitle("Tom 工作空间"), 0, 0);
        layout.Controls.Add(BuildWorkspaceBox(), 0, 1);

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true
        };
        _detectCliButton.Text = "重新检测 CLI";
        _detectCliButton.Width = 110;
        _detectCliButton.Height = 30;
        _detectCliButton.Click += async (_, _) => await DetectCliAsync();
        actions.Controls.Add(_detectCliButton);

        var diagnostics = new Button
        {
            Text = "生成诊断包",
            Width = 110,
            Height = 30
        };
        diagnostics.Click += (_, _) => CreateDiagnosticsPackage();
        actions.Controls.Add(diagnostics);
        layout.Controls.Add(actions, 0, 2);

        layout.Controls.Add(SectionTitle("Tom 环境状态"), 0, 3);

        _environmentList.Dock = DockStyle.Fill;
        _environmentList.View = View.Details;
        _environmentList.FullRowSelect = true;
        _environmentList.GridLines = false;
        _environmentList.HeaderStyle = ColumnHeaderStyle.Nonclickable;
        _environmentList.Columns.Add("项目", 112);
        _environmentList.Columns.Add("状态", 76);
        _environmentList.Columns.Add("说明", 300);
        layout.Controls.Add(_environmentList, 0, 4);

        var note = new Label
        {
            Dock = DockStyle.Fill,
            Text = "缺少 Codex 或 Claude 时，Tom 只提示环境缺失，不内置安装器。",
            ForeColor = TomColors.Muted,
            Font = new Font("Microsoft YaHei UI", 9F)
        };
        layout.Controls.Add(note, 0, 5);
        return layout;
    }

    private Control BuildWorkspaceBox()
    {
        var workspaceBox = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            BackColor = Color.Transparent
        };
        workspaceBox.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        workspaceBox.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 94));
        workspaceBox.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        workspaceBox.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));

        _workspacePath.Dock = DockStyle.Fill;
        _workspacePath.ReadOnly = true;
        _workspacePath.Text = _workspace.RootPath;
        workspaceBox.Controls.Add(_workspacePath, 0, 0);

        _chooseProjectButton.Text = "选择项目";
        _chooseProjectButton.Dock = DockStyle.Fill;
        _chooseProjectButton.Click += (_, _) => ChooseProjectFolder();
        workspaceBox.Controls.Add(_chooseProjectButton, 1, 0);

        var note = new Label
        {
            Dock = DockStyle.Fill,
            Text = "Tom 默认在项目目录下创建 tom-docs，用于保存文档、日志、AI 记录和导出文件。",
            ForeColor = TomColors.Muted,
            Font = new Font("Microsoft YaHei UI", 8.5F)
        };
        workspaceBox.Controls.Add(note, 0, 1);
        workspaceBox.SetColumnSpan(note, 2);
        return workspaceBox;
    }

    private Control BuildAiPanel()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 5,
            ColumnCount = 1,
            BackColor = Color.Transparent
        };
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));

        _providerCombo.Dock = DockStyle.Fill;
        _providerCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _providerCombo.Items.Add(new ProviderItem("codex", "Codex CLI"));
        _providerCombo.Items.Add(new ProviderItem("claude", "Claude CLI"));
        _providerCombo.SelectedIndex = 0;
        panel.Controls.Add(_providerCombo, 0, 0);

        _styleCombo.Dock = DockStyle.Fill;
        _styleCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _styleCombo.Items.Add("工程设计说明");
        _styleCombo.Items.Add("玩法文档");
        _styleCombo.Items.Add("需求规格");
        _styleCombo.Items.Add("用户手册");
        _styleCombo.Items.Add("简洁说明");
        _styleCombo.SelectedIndex = 0;
        panel.Controls.Add(_styleCombo, 0, 1);

        var instructionHeader = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Color.Transparent
        };
        instructionHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        instructionHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 104));

        instructionHeader.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = "自由指令",
            ForeColor = TomColors.Text,
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 0);

        _autoApplyAi.Text = "自动应用";
        _autoApplyAi.Checked = true;
        _autoApplyAi.AutoSize = true;
        _autoApplyAi.ForeColor = TomColors.Text;
        _autoApplyAi.Dock = DockStyle.None;
        _autoApplyAi.Margin = new Padding(0, 3, 0, 0);

        var autoApplyBox = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            BackColor = Color.Transparent,
            Margin = new Padding(0)
        };
        autoApplyBox.Controls.Add(_autoApplyAi);
        instructionHeader.Controls.Add(autoApplyBox, 1, 0);
        panel.Controls.Add(instructionHeader, 0, 2);

        _instructionBox.Dock = DockStyle.Fill;
        _instructionBox.Multiline = true;
        _instructionBox.ScrollBars = ScrollBars.Vertical;
        _instructionBox.WordWrap = true;
        _instructionBox.AcceptsReturn = true;
        _instructionBox.PlaceholderText = "自由指令或补充要求";
        panel.Controls.Add(_instructionBox, 0, 3);

        _customAiButton.Text = "执行自由指令";
        _customAiButton.Dock = DockStyle.Fill;
        _customAiButton.Height = 44;
        _customAiButton.Margin = new Padding(0, 8, 0, 0);
        _customAiButton.Font = new Font("Microsoft YaHei UI", 10.5F, FontStyle.Bold);
        _customAiButton.Click += async (_, _) => await RunAiActionAsync(TomAiAction.Custom);
        panel.Controls.Add(_customAiButton, 0, 4);

        return panel;
    }

    private static void AddToolButton(FlowLayoutPanel toolbar, string text, Action action, int width)
    {
        var button = new Button
        {
            Text = text,
            Width = width,
            Height = 28,
            Margin = new Padding(2)
        };
        button.Click += (_, _) => action();
        toolbar.Controls.Add(button);
    }

    private Control BuildStatusBar()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = TomColors.Status,
            Padding = new Padding(14, 0, 14, 0)
        };

        _statusLabel.Dock = DockStyle.Fill;
        _statusLabel.ForeColor = TomColors.Text;
        _statusLabel.TextAlign = ContentAlignment.MiddleLeft;
        _statusLabel.Text = "Tom 已启动。";
        panel.Controls.Add(_statusLabel);
        return panel;
    }

    private static Label SectionTitle(string text)
    {
        return new Label
        {
            Dock = DockStyle.Fill,
            Text = text,
            ForeColor = TomColors.Text,
            Font = new Font("Microsoft YaHei UI", 11F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };
    }

    private void ApplyHeading(float size, bool bold)
    {
        var current = _editor.SelectionFont ?? _editor.Font;
        var style = bold ? FontStyle.Bold : FontStyle.Regular;
        _editor.SelectionFont = new Font(current.FontFamily, size, style);
    }

    private void ApplyBodyStyle()
    {
        _editor.SelectionFont = new Font("Microsoft YaHei UI", 11F, FontStyle.Regular);
        _editor.SelectionAlignment = HorizontalAlignment.Left;
    }

    private void ToggleFontStyle(FontStyle style)
    {
        var current = _editor.SelectionFont ?? _editor.Font;
        var nextStyle = current.Style.HasFlag(style) ? current.Style & ~style : current.Style | style;
        _editor.SelectionFont = new Font(current.FontFamily, current.Size, nextStyle);
    }

    private void ApplyBulletList()
    {
        if (string.IsNullOrWhiteSpace(_editor.SelectedText))
        {
            _editor.SelectedText = "- ";
            return;
        }

        ReplaceSelectedLines((line, _) => string.IsNullOrWhiteSpace(line) ? line : "- " + StripListPrefix(line));
    }

    private void ApplyOrderedList()
    {
        if (string.IsNullOrWhiteSpace(_editor.SelectedText))
        {
            _editor.SelectedText = "1. ";
            return;
        }

        ReplaceSelectedLines((line, index) => string.IsNullOrWhiteSpace(line) ? line : $"{index + 1}. {StripListPrefix(line)}");
    }

    private void ReplaceSelectedLines(Func<string, int, string> transform)
    {
        var selected = _editor.SelectedText;
        if (string.IsNullOrEmpty(selected)) return;

        var separator = selected.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        var lines = selected.Replace("\r\n", "\n").Split('\n');
        _editor.SelectedText = string.Join(separator, lines.Select(transform));
    }

    private static string StripListPrefix(string line)
    {
        var trimmed = line.TrimStart();
        if (trimmed.StartsWith("- ")) return trimmed[2..];
        var dotIndex = trimmed.IndexOf('.');
        if (dotIndex > 0 && trimmed[..dotIndex].All(char.IsDigit))
        {
            return trimmed[(dotIndex + 1)..].TrimStart();
        }

        return trimmed;
    }

    private void ImportDocument()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "导入 Tom 文档",
            Filter = "Supported files|*.txt;*.md;*.markdown;*.html;*.htm;*.rtf;*.docx|Text|*.txt|Markdown|*.md;*.markdown|HTML|*.html;*.htm|RTF|*.rtf|DOCX|*.docx|All files|*.*"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            var extension = Path.GetExtension(dialog.FileName).ToLowerInvariant();
            switch (extension)
            {
                case ".rtf":
                    _editor.LoadFile(dialog.FileName, RichTextBoxStreamType.RichText);
                    break;
                case ".docx":
                    _editor.Text = TomDocxService.ImportText(dialog.FileName);
                    break;
                case ".html":
                case ".htm":
                    _editor.Text = TomDocumentConversion.HtmlToText(File.ReadAllText(dialog.FileName));
                    break;
                case ".md":
                case ".markdown":
                    _editor.Text = TomDocumentConversion.MarkdownToText(File.ReadAllText(dialog.FileName));
                    break;
                default:
                    _editor.Text = File.ReadAllText(dialog.FileName);
                    break;
            }

            var title = Path.GetFileNameWithoutExtension(dialog.FileName);
            _currentDocumentId = _store.CreateDocumentId(title);
            SaveDocument(silent: true);
            RefreshOutline();
            SetStatus($"已导入：{dialog.FileName}");
        }
        catch (Exception error)
        {
            SetStatus($"导入失败：{error.Message}");
        }
    }

    private void ExportDocument()
    {
        using var dialog = new SaveFileDialog
        {
            Title = "导出 Tom 文档",
            Filter = "Text|*.txt|RTF|*.rtf|Markdown|*.md|HTML|*.html|DOCX|*.docx",
            FileName = $"{SafeFileNameStem(FirstDocumentLineOrDefault())}.txt",
            InitialDirectory = Directory.Exists(_workspace.ExportsPath) ? _workspace.ExportsPath : _workspace.RootPath
        };

        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            var extension = Path.GetExtension(dialog.FileName).ToLowerInvariant();
            switch (extension)
            {
                case ".rtf":
                    _editor.SaveFile(dialog.FileName, RichTextBoxStreamType.RichText);
                    break;
                case ".md":
                    File.WriteAllText(dialog.FileName, TomDocumentConversion.TextToMarkdown(_editor.Text));
                    break;
                case ".html":
                    File.WriteAllText(dialog.FileName, TomDocumentConversion.TextToHtml(_editor.Text));
                    break;
                case ".docx":
                    TomDocxService.ExportText(dialog.FileName, FirstDocumentLineOrDefault(), _editor.Text);
                    break;
                default:
                    File.WriteAllText(dialog.FileName, _editor.Text);
                    break;
            }

            SetStatus($"已导出：{dialog.FileName}");
        }
        catch (Exception error)
        {
            SetStatus($"导出失败：{error.Message}");
        }
    }

    private async Task RunAiActionAsync(TomAiAction action)
    {
        if (action == TomAiAction.Custom && _activeAiTasks > 0)
        {
            SetStatus("AI 正在运行，请等待当前任务完成后再执行自由指令。");
            return;
        }

        var provider = (_providerCombo.SelectedItem as ProviderItem)?.Id ?? "codex";
        var (targetText, targetStart, targetLength, preferredOperation) = BuildAiTarget(action);
        var instruction = BuildInstruction(action);

        if (action == TomAiAction.Custom && string.IsNullOrWhiteSpace(instruction))
        {
            SetStatus("请输入自由指令。");
            return;
        }

        var request = new TomAiRequest(action, provider, instruction, targetText, _editor.Text, preferredOperation);
        BeginAiTask();
        SetStatus($"{provider} 正在执行 {ActionLabel(action)}...");

        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(3));
        try
        {
            var (result, raw) = await _aiService.RunAsync(request, timeout.Token);
            var applied = ApplyAiResult(result, targetStart, targetLength);
            var status = applied ? "applied" : "cancelled";
            AddAiHistory(CreateAiRunRecord(action, provider, result.Summary, result.Before, result.After, raw, string.Empty, request, status, ScopeLabel(preferredOperation, targetLength)));

            if (applied)
            {
                SaveDocument(silent: true);
                RefreshOutline();
                SetStatus($"AI {ActionLabel(action)} 已应用：{result.Summary}");
            }
            else
            {
                SetStatus($"AI {ActionLabel(action)} 已取消应用：{result.Summary}");
            }
        }
        catch (Exception error)
        {
            AddAiHistory(CreateAiRunRecord(action, provider, "执行失败", targetText, string.Empty, string.Empty, error.Message, request, "failed", ScopeLabel(preferredOperation, targetLength)));
            SetStatus($"AI 执行失败：{error.Message}");
        }
        finally
        {
            EndAiTask();
        }
    }

    private (string Text, int Start, int Length, string PreferredOperation) BuildAiTarget(TomAiAction action)
    {
        if (!string.IsNullOrWhiteSpace(_editor.SelectedText))
        {
            return (_editor.SelectedText, _editor.SelectionStart, _editor.SelectionLength, "replace-selection");
        }

        if (action == TomAiAction.Beautify)
        {
            var range = CurrentParagraphRange();
            if (range.Length > 0)
            {
                return (_editor.Text.Substring(range.Start, range.Length), range.Start, range.Length, "replace-selection");
            }
        }

        return (string.Empty, _editor.SelectionStart, 0, "insert-at-cursor");
    }

    private (int Start, int Length) CurrentParagraphRange()
    {
        if (string.IsNullOrEmpty(_editor.Text)) return (0, 0);

        var cursor = Math.Min(_editor.SelectionStart, _editor.Text.Length);
        var start = _editor.Text.LastIndexOf('\n', Math.Max(0, cursor - 1));
        start = start < 0 ? 0 : start + 1;
        var end = _editor.Text.IndexOf('\n', cursor);
        end = end < 0 ? _editor.Text.Length : end;
        return (start, Math.Max(0, end - start));
    }

    private string BuildInstruction(TomAiAction action)
    {
        var extra = _instructionBox.Text.Trim();
        var style = _styleCombo.SelectedItem?.ToString() ?? "工程设计说明";
        var baseInstruction = action switch
        {
            TomAiAction.Expand => "根据上下文扩写目标内容，保持文档风格，不覆盖已有观点。",
            TomAiAction.Beautify => "按文档规范美化目标内容，整理层级、编号、段落和表达，不新增无依据内容。",
            TomAiAction.Summarize => "只基于当前文档已有内容总结核心信息，结构清晰。",
            TomAiAction.Requirements => "只基于当前文档已有内容整理需求清单，不新增未写明的功能、规则或建议。",
            TomAiAction.Review => "检查当前文档的一致性、缺失项、重复内容、结构问题和表述风险，输出可执行的审阅清单，不直接改写全文。",
            TomAiAction.Custom => extra,
            _ => extra
        };

        if (action == TomAiAction.Custom)
        {
            if (string.IsNullOrWhiteSpace(baseInstruction)) return string.Empty;
            return string.IsNullOrWhiteSpace(style) ? baseInstruction : $"{baseInstruction}\n文体：{style}";
        }

        var styleInstruction = string.IsNullOrWhiteSpace(style) ? baseInstruction : $"{baseInstruction}\n文体：{style}";
        if (string.IsNullOrWhiteSpace(extra)) return styleInstruction;
        return $"{styleInstruction}\n补充要求：{extra}";
    }

    private bool ApplyAiResult(TomAiResult result, int targetStart, int targetLength)
    {
        if (!_autoApplyAi.Checked)
        {
            using var dialog = new TomAiPreviewForm(result);
            if (dialog.ShowDialog(this) != DialogResult.OK) return false;
        }

        if (result.Operation == "replace-document")
        {
            _editor.Text = result.After;
            return true;
        }

        _editor.Select(targetStart, Math.Min(targetLength, Math.Max(0, _editor.TextLength - targetStart)));
        _editor.SelectedText = result.After;
        return true;
    }

    private TomAiRunRecord CreateAiRunRecord(
        TomAiAction action,
        string provider,
        string summary,
        string before,
        string after,
        string raw,
        string error,
        TomAiRequest request,
        string status,
        string scope)
    {
        var inputCharacters = request.FullText.Length + request.TargetText.Length + request.Instruction.Length;
        var outputCharacters = after.Length + raw.Length + error.Length;
        var estimatedTokens = EstimateTokens(inputCharacters, outputCharacters);
        var usage = TomTokenUsageParser.Parse($"{raw}\n{error}", estimatedTokens);
        return new TomAiRunRecord(
            DateTime.Now,
            action,
            provider,
            summary,
            before,
            after,
            raw,
            error,
            inputCharacters,
            outputCharacters,
            estimatedTokens,
            usage.InputTokens,
            usage.OutputTokens,
            usage.CacheReadTokens,
            usage.CacheWriteTokens,
            usage.TotalTokens,
            usage.CacheHitRate,
            usage.Source,
            status,
            scope,
            request.Instruction,
            _currentDocumentId);
    }

    private void AddAiHistory(TomAiRunRecord record, bool persist = true)
    {
        _aiHistory.Insert(0, record);
        if (_aiHistory.Count > 50) _aiHistory.RemoveAt(_aiHistory.Count - 1);

        if (persist) TomAiRunStore.Save(_workspace, record);
        RefreshAiHistoryView();
    }

    private void LoadAiHistory()
    {
        _aiHistory.Clear();
        _aiHistory.AddRange(TomAiRunStore.LoadRecent(_workspace, 50));
        RefreshAiHistoryView();
    }

    private void RefreshAiHistoryView()
    {
        if (_historyList.IsDisposed) return;

        _historyList.Items.Clear();
        foreach (var record in _aiHistory)
        {
            var item = new ListViewItem(record.CreatedAt.ToString("MM-dd HH:mm"));
            item.SubItems.Add(ActionLabel(record.Action));
            item.SubItems.Add(record.Provider);
            item.SubItems.Add(StatusText(record));
            item.SubItems.Add(ScopeText(record));
            item.SubItems.Add(SummaryText(record));
            _historyList.Items.Add(item);
        }

        RefreshTokenUsageView();
    }

    private void ClearAiHistory()
    {
        if (_aiHistory.Count == 0 && !Directory.Exists(_workspace.AiRunsPath))
        {
            SetStatus("AI 历史为空。");
            return;
        }

        var confirm = MessageBox.Show(
            this,
            "确认清理 AI 历史？这会删除当前工作空间 ai-runs 记录，不会删除正文文档、快照或导出文件。",
            "清理 AI 历史",
            MessageBoxButtons.OKCancel,
            MessageBoxIcon.Warning);

        if (confirm != DialogResult.OK) return;

        try
        {
            _aiHistory.Clear();

            if (Directory.Exists(_workspace.AiRunsPath))
            {
                foreach (var file in Directory.EnumerateFiles(_workspace.AiRunsPath, "*.json"))
                {
                    File.Delete(file);
                }
            }

            RefreshAiHistoryView();
            SetStatus("AI 历史已清理。");
        }
        catch (Exception error)
        {
            SetStatus($"AI 历史清理失败：{error.Message}");
        }
    }

    private void RefreshTokenUsageView()
    {
        if (_tokenUsageList.IsDisposed) return;

        _tokenUsageList.Items.Clear();
        foreach (var record in _aiHistory)
        {
            var item = new ListViewItem(record.CreatedAt.ToString("MM-dd HH:mm"));
            item.SubItems.Add(record.Provider);
            item.SubItems.Add(ActionLabel(record.Action));
            item.SubItems.Add(EffectiveTotalTokens(record).ToString("N0"));
            item.SubItems.Add(CacheHitLevel(record));
            item.SubItems.Add(CacheHitRateText(record));
            item.SubItems.Add(TokenSourceText(record));
            _tokenUsageList.Items.Add(item);
        }

        UpdateTokenSummary();
    }

    private void UpdateTokenSummary()
    {
        if (_tokenSummaryLabel.IsDisposed) return;

        var runs = _aiHistory.Count;
        var totalTokens = _aiHistory.Sum(EffectiveTotalTokens);
        var reportedRuns = _aiHistory.Count(item => TokenSourceText(item) != "估算");
        var cacheKnown = _aiHistory.Where(item => item.CacheHitRate >= 0).ToList();
        var cacheRate = cacheKnown.Count == 0 ? -1D : cacheKnown.Average(item => item.CacheHitRate);
        var cacheText = cacheRate < 0 ? "未知" : $"{cacheRate:P0}（{CacheHitLevel(cacheRate)}）";

        _tokenSummaryLabel.Text =
            $"对话记录：{runs} 次\r\nToken 合计：{totalTokens:N0}；真实 usage：{reportedRuns} 次；缓存命中率：{cacheText}";
    }

    private void ShowSelectedTokenUsage()
    {
        var index = _tokenUsageList.SelectedIndices.Count > 0 ? _tokenUsageList.SelectedIndices[0] : -1;
        if (index < 0 || index >= _aiHistory.Count)
        {
            SetStatus("请先选择一条 Token 记录。");
            return;
        }

        var record = _aiHistory[index];
        using var viewer = new TomTextViewerForm(
            "Tom Token 计费明细",
            $"时间：{record.CreatedAt:yyyy-MM-dd HH:mm:ss}\r\nProvider：{record.Provider}\r\n动作：{ActionLabel(record.Action)}\r\n来源：{TokenSourceText(record)}\r\n\r\n总 Token：{EffectiveTotalTokens(record):N0}\r\n输入 Token：{record.InputTokens:N0}\r\n输出 Token：{record.OutputTokens:N0}\r\n缓存读取 Token：{record.CacheReadTokens:N0}\r\n缓存写入 Token：{record.CacheWriteTokens:N0}\r\n缓存命中率：{CacheHitRateText(record)}\r\n缓存命中程度：{CacheHitLevel(record)}\r\n\r\n说明：来源为“估算”时，代表 CLI 未返回真实 usage，Tom 根据字符数估算，不作为精确账单。");
        viewer.ShowDialog(this);
    }

    private void ShowSelectedAiHistory()
    {
        var index = _historyList.SelectedIndices.Count > 0 ? _historyList.SelectedIndices[0] : -1;
        if (index < 0 || index >= _aiHistory.Count)
        {
            SetStatus("请先选择一条 AI 历史。");
            return;
        }

        var record = _aiHistory[index];
        using var viewer = new TomAiHistoryDetailForm(record, ActionLabel(record.Action), StatusText(record), ScopeText(record));
        viewer.ShowDialog(this);
    }

    private static string StatusText(TomAiRunRecord record)
    {
        if (!string.IsNullOrWhiteSpace(record.Error)) return "失败";

        return record.Status switch
        {
            "applied" => "已应用",
            "cancelled" => "已取消",
            "previewed" => "已预览",
            "failed" => "失败",
            _ => string.IsNullOrWhiteSpace(record.After) ? "未应用" : "已应用"
        };
    }

    private static string ScopeLabel(string preferredOperation, int targetLength)
    {
        if (preferredOperation == "replace-selection" && targetLength > 0) return "selection";
        if (preferredOperation == "replace-document") return "document";
        return "cursor";
    }

    private static string ScopeText(TomAiRunRecord record)
    {
        return record.Scope switch
        {
            "selection" => "选区",
            "document" => "全文",
            "paragraph" => "段落",
            "cursor" => "光标",
            _ => "未知"
        };
    }

    private static string SummaryText(TomAiRunRecord record)
    {
        if (!string.IsNullOrWhiteSpace(record.Error)) return record.Error;
        return string.IsNullOrWhiteSpace(record.Summary) ? "无摘要" : record.Summary;
    }

    private static int EffectiveTotalTokens(TomAiRunRecord record)
    {
        if (record.TotalTokens > 0) return record.TotalTokens;
        return Math.Max(1, record.EstimatedTokens);
    }

    private static string DisplayToken(TomAiRunRecord record)
    {
        return TokenSourceText(record) == "估算" ? $"~{EffectiveTotalTokens(record)}t" : $"{EffectiveTotalTokens(record)}t";
    }

    private static string TokenSourceText(TomAiRunRecord record)
    {
        return string.IsNullOrWhiteSpace(record.TokenSource) ? "估算" : record.TokenSource;
    }

    private static string CacheHitRateText(TomAiRunRecord record)
    {
        return record.CacheHitRate < 0 ? "未知" : $"{record.CacheHitRate:P0}";
    }

    private static string CacheHitLevel(TomAiRunRecord record)
    {
        return CacheHitLevel(record.CacheHitRate);
    }

    private static string CacheHitLevel(double cacheHitRate)
    {
        if (cacheHitRate < 0) return "未知";
        if (cacheHitRate >= 0.6D) return "高";
        if (cacheHitRate >= 0.25D) return "中";
        return "低";
    }

    private static int EstimateTokens(int inputCharacters, int outputCharacters)
    {
        var characters = Math.Max(0, inputCharacters) + Math.Max(0, outputCharacters);
        return Math.Max(1, (int)Math.Ceiling(characters / 2.2D));
    }

    private void BeginAiTask()
    {
        _activeAiTasks++;
        UpdateAiLoadingState();
    }

    private void EndAiTask()
    {
        _activeAiTasks = Math.Max(0, _activeAiTasks - 1);
        UpdateAiLoadingState();
    }

    private void UpdateAiLoadingState()
    {
        if (_customAiButton.IsDisposed) return;

        var isLoading = _activeAiTasks > 0;
        _customAiButton.Enabled = !isLoading;
        _customAiButton.Text = isLoading ? $"AI 加载中（{_activeAiTasks}）" : "执行自由指令";
    }

    private static string ActionLabel(TomAiAction action)
    {
        return action switch
        {
            TomAiAction.Expand => "扩写",
            TomAiAction.Beautify => "美化",
            TomAiAction.Summarize => "总结",
            TomAiAction.Requirements => "需求",
            TomAiAction.Review => "检查",
            TomAiAction.Custom => "自由指令",
            _ => "AI"
        };
    }

    private void ConfigureAutoSave()
    {
        _autoSaveTimer.Interval = 30000;
        _autoSaveTimer.Tick += (_, _) => SaveDocument(silent: true);
        _autoSaveTimer.Start();
    }

    private void SaveDocument(bool silent = false)
    {
        try
        {
            _store.Save(_currentDocumentId, _editor.Rtf ?? string.Empty, _editor.Text);
            RefreshDocumentList();
            if (!silent) SetStatus($"Tom 文档已保存：{_store.GetRtfPath(_currentDocumentId)}");
        }
        catch (Exception error)
        {
            if (!silent) SetStatus($"保存失败：{error.Message}");
        }
    }

    private void CreateSnapshot()
    {
        try
        {
            Directory.CreateDirectory(_workspace.SnapshotsPath);
            var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            var rtfPath = Path.Combine(_workspace.SnapshotsPath, $"snapshot-{timestamp}.rtf");
            var txtPath = Path.Combine(_workspace.SnapshotsPath, $"snapshot-{timestamp}.txt");
            File.WriteAllText(rtfPath, _editor.Rtf ?? string.Empty);
            File.WriteAllText(txtPath, _editor.Text);
            RefreshSnapshotList();
            SetStatus($"已创建快照：{rtfPath}");
        }
        catch (Exception error)
        {
            SetStatus($"快照失败：{error.Message}");
        }
    }

    private void FindText()
    {
        using var dialog = new TomInputDialog("查找", "输入要查找的文本：");
        if (dialog.ShowDialog(this) != DialogResult.OK || string.IsNullOrWhiteSpace(dialog.Value)) return;

        var start = Math.Min(_editor.SelectionStart + _editor.SelectionLength, _editor.TextLength);
        var index = _editor.Text.IndexOf(dialog.Value, start, StringComparison.CurrentCultureIgnoreCase);
        if (index < 0 && start > 0)
        {
            index = _editor.Text.IndexOf(dialog.Value, 0, StringComparison.CurrentCultureIgnoreCase);
        }

        if (index < 0)
        {
            SetStatus($"未找到：{dialog.Value}");
            return;
        }

        _editor.Select(index, dialog.Value.Length);
        _editor.Focus();
        SetStatus($"已找到：{dialog.Value}");
    }

    private void ReplaceText()
    {
        using var findDialog = new TomInputDialog("替换", "输入要替换的文本：");
        if (findDialog.ShowDialog(this) != DialogResult.OK || string.IsNullOrWhiteSpace(findDialog.Value)) return;

        using var replaceDialog = new TomInputDialog("替换", "输入替换后的文本：");
        if (replaceDialog.ShowDialog(this) != DialogResult.OK) return;

        var find = findDialog.Value;
        var replacement = replaceDialog.Value;
        var count = 0;
        var cursor = 0;

        _editor.SuspendLayout();
        try
        {
            while (cursor <= _editor.TextLength)
            {
                var index = _editor.Find(find, cursor, RichTextBoxFinds.None);
                if (index < 0) break;

                _editor.Select(index, find.Length);
                _editor.SelectedText = replacement;
                count++;
                cursor = index + replacement.Length;
            }
        }
        finally
        {
            _editor.ResumeLayout();
        }

        SetStatus(count == 0 ? $"未找到：{find}" : $"已替换 {count} 处：{find}");
        RefreshOutline();
    }

    private void NewDocument()
    {
        SaveDocument(silent: true);

        using var dialog = new TomInputDialog("新建文档", "输入文档标题：");
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        var title = string.IsNullOrWhiteSpace(dialog.Value) ? "Tom 文档" : dialog.Value.Trim();
        _currentDocumentId = _store.CreateDocumentId(title);
        _editor.Text = $"{title}\n\n";
        SaveDocument(silent: true);
        RefreshOutline();
        SetStatus($"已新建文档：{title}");
    }

    private void SaveAsDocument()
    {
        using var dialog = new TomInputDialog("另存为", "输入新文档标题：");
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        var title = string.IsNullOrWhiteSpace(dialog.Value) ? FirstDocumentLineOrDefault() : dialog.Value.Trim();
        _currentDocumentId = _store.CreateDocumentId(title);
        SaveDocument(silent: true);
        SetStatus($"已另存为：{title}");
    }

    private void OpenSelectedDocument()
    {
        var record = SelectedDocumentRecord();
        if (record is null)
        {
            SetStatus("请先在文档列表中选择一个文档。");
            return;
        }

        SaveDocument(silent: true);
        _currentDocumentId = record.Id;
        LoadDocumentById(record.Id, showStatus: true);
    }

    private void DeleteSelectedDocument()
    {
        var record = SelectedDocumentRecord();
        if (record is null)
        {
            SetStatus("请先在文档列表中选择一个文档。");
            return;
        }

        if (MessageBox.Show(this, $"确认删除文档“{record.Title}”？", "删除文档", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) != DialogResult.OK)
        {
            return;
        }

        _store.Delete(record.Id);
        if (string.Equals(_currentDocumentId, record.Id, StringComparison.OrdinalIgnoreCase))
        {
            _currentDocumentId = TomDocumentStore.DefaultDocumentId;
            _editor.Text = CreateDefaultDocumentText();
            SaveDocument(silent: true);
        }

        RefreshDocumentList();
        RefreshOutline();
        SetStatus($"已删除文档：{record.Title}");
    }

    private TomDocumentRecord? SelectedDocumentRecord()
    {
        var index = _documentList.SelectedIndex;
        return index >= 0 && index < _documentRecords.Count ? _documentRecords[index] : null;
    }

    private void RefreshDocumentList()
    {
        if (_documentList.IsDisposed) return;

        _documentRecords.Clear();
        _documentRecords.AddRange(_store.ListDocuments());
        _documentList.Items.Clear();

        foreach (var record in _documentRecords)
        {
            _documentList.Items.Add(record);
        }

        var selectedIndex = _documentRecords.FindIndex(record => string.Equals(record.Id, _currentDocumentId, StringComparison.OrdinalIgnoreCase));
        if (selectedIndex >= 0) _documentList.SelectedIndex = selectedIndex;
    }

    private void RefreshSnapshotList()
    {
        if (_snapshotList.IsDisposed) return;

        _snapshotRecords.Clear();
        if (Directory.Exists(_workspace.SnapshotsPath))
        {
            var ids = Directory.EnumerateFiles(_workspace.SnapshotsPath)
                .Where(path => string.Equals(Path.GetExtension(path), ".rtf", StringComparison.OrdinalIgnoreCase) ||
                               string.Equals(Path.GetExtension(path), ".txt", StringComparison.OrdinalIgnoreCase))
                .Select(path => Path.GetFileNameWithoutExtension(path))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(id => SnapshotUpdatedAt(id))
                .ToList();

            _snapshotRecords.AddRange(ids.Select(id => new TomSnapshotRecord(
                id,
                Path.Combine(_workspace.SnapshotsPath, $"{id}.rtf"),
                Path.Combine(_workspace.SnapshotsPath, $"{id}.txt"),
                SnapshotUpdatedAt(id))));
        }

        _snapshotList.Items.Clear();
        foreach (var record in _snapshotRecords)
        {
            _snapshotList.Items.Add(record);
        }
    }

    private DateTime SnapshotUpdatedAt(string id)
    {
        var rtfPath = Path.Combine(_workspace.SnapshotsPath, $"{id}.rtf");
        var txtPath = Path.Combine(_workspace.SnapshotsPath, $"{id}.txt");
        var rtfTime = File.Exists(rtfPath) ? File.GetLastWriteTime(rtfPath) : DateTime.MinValue;
        var txtTime = File.Exists(txtPath) ? File.GetLastWriteTime(txtPath) : DateTime.MinValue;
        return rtfTime > txtTime ? rtfTime : txtTime;
    }

    private void RestoreSelectedSnapshot()
    {
        var index = _snapshotList.SelectedIndex;
        if (index < 0 || index >= _snapshotRecords.Count)
        {
            SetStatus("请先在快照列表中选择一个快照。");
            return;
        }

        var record = _snapshotRecords[index];
        if (MessageBox.Show(this, "恢复快照会覆盖当前编辑区。是否继续？", "恢复快照", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) != DialogResult.OK)
        {
            return;
        }

        CreateSnapshot();
        if (File.Exists(record.RtfPath))
        {
            _editor.Rtf = File.ReadAllText(record.RtfPath);
        }
        else if (File.Exists(record.TextPath))
        {
            _editor.Text = File.ReadAllText(record.TextPath);
        }

        SaveDocument(silent: true);
        RefreshOutline();
        SetStatus($"已恢复快照：{record.Id}");
    }

    private void RefreshOutline()
    {
        if (_outlineList.IsDisposed) return;

        _outlineItems.Clear();
        _outlineList.Items.Clear();

        var originalStart = _editor.SelectionStart;
        var originalLength = _editor.SelectionLength;
        try
        {
            var charIndex = 0;
            var lines = _editor.Text.Replace("\r\n", "\n").Split('\n');
            for (var lineIndex = 0; lineIndex < lines.Length; lineIndex++)
            {
                var line = lines[lineIndex];
                var trimmed = line.Trim();
                if (trimmed.Length > 0 && IsOutlineLine(trimmed, charIndex))
                {
                    _outlineItems.Add(new TomOutlineItem(charIndex, $"{lineIndex + 1}. {trimmed}"));
                }

                charIndex += line.Length + 1;
            }
        }
        finally
        {
            _editor.Select(Math.Min(originalStart, _editor.TextLength), Math.Min(originalLength, Math.Max(0, _editor.TextLength - originalStart)));
        }

        foreach (var item in _outlineItems)
        {
            _outlineList.Items.Add(item);
        }
    }

    private bool IsOutlineLine(string trimmed, int charIndex)
    {
        if (trimmed.StartsWith('#')) return true;
        if (trimmed.StartsWith("第", StringComparison.Ordinal) && (trimmed.Contains('章') || trimmed.Contains('节'))) return true;
        if (trimmed.Length <= 32 && (trimmed.EndsWith("：", StringComparison.Ordinal) || trimmed.EndsWith(":", StringComparison.Ordinal))) return true;

        try
        {
            if (charIndex >= 0 && charIndex < _editor.TextLength)
            {
                _editor.Select(charIndex, 1);
                var font = _editor.SelectionFont;
                if (font is not null && (font.Bold || font.Size >= 13F)) return true;
            }
        }
        catch
        {
            // Outline detection is best-effort and should never interrupt editing.
        }

        return false;
    }

    private void JumpToSelectedOutline()
    {
        var index = _outlineList.SelectedIndex;
        if (index < 0 || index >= _outlineItems.Count)
        {
            SetStatus("请先在大纲列表中选择一个条目。");
            return;
        }

        var item = _outlineItems[index];
        _editor.Select(Math.Min(item.Start, _editor.TextLength), 0);
        _editor.Focus();
        _editor.ScrollToCaret();
        SetStatus($"已跳转：{item.Text}");
    }

    private void LoadDocumentIfPresent(bool showMissingMessage = false, bool resetWhenMissing = false)
    {
        try
        {
            if (!_store.HasSavedDocument)
            {
                if (resetWhenMissing) _editor.Text = CreateDefaultDocumentText();
                if (showMissingMessage) SetStatus("还没有已保存的 Tom 文档。");
                return;
            }

            _currentDocumentId = TomDocumentStore.DefaultDocumentId;
            LoadDocumentById(_currentDocumentId, showStatus: true);
        }
        catch (Exception error)
        {
            SetStatus($"载入失败：{error.Message}");
        }
    }

    private void LoadDocumentById(string documentId, bool showStatus)
    {
        var snapshot = _store.Load(documentId);
        if (snapshot.IsRtf)
        {
            _editor.Rtf = snapshot.Rtf;
        }
        else if (!string.IsNullOrWhiteSpace(snapshot.Text))
        {
            _editor.Text = snapshot.Text;
        }

        RefreshDocumentList();
        RefreshOutline();
        if (showStatus) SetStatus($"已载入 Tom 文档：{_store.GetRtfPath(documentId)}");
    }

    private string FirstDocumentLineOrDefault()
    {
        return _editor.Text
            .Replace("\r\n", "\n")
            .Split('\n')
            .Select(line => line.Trim())
            .FirstOrDefault(line => !string.IsNullOrWhiteSpace(line)) ?? "Tom 文档";
    }

    private static string SafeFileNameStem(string value)
    {
        var invalid = Path.GetInvalidFileNameChars().ToHashSet();
        var cleaned = new string(value.Select(ch => invalid.Contains(ch) ? '-' : ch).ToArray()).Trim('-', ' ');
        if (string.IsNullOrWhiteSpace(cleaned)) return "tom-document";
        return cleaned.Length > 48 ? cleaned[..48] : cleaned;
    }

    private void ChooseProjectFolder()
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "选择 Tom 项目目录，Tom 会在该目录下创建 tom-docs",
            UseDescriptionForTitle = true,
            InitialDirectory = Directory.Exists(_workspace.ProjectPath) ? _workspace.ProjectPath : AppContext.BaseDirectory
        };

        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        _workspace = TomWorkspaceService.CreateForProject(dialog.SelectedPath);
        _store = new TomDocumentStore(_workspace);
        _currentDocumentId = TomDocumentStore.DefaultDocumentId;
        _workspacePath.Text = _workspace.RootPath;
        SetStatus($"Tom 工作空间已切换：{_workspace.RootPath}");
        LoadAiHistory();
        RefreshSnapshotList();
        LoadDocumentIfPresent(resetWhenMissing: true);
        RefreshDocumentList();
        RefreshOutline();
    }

    private void CreateDiagnosticsPackage()
    {
        try
        {
            SaveDocument(silent: true);
            var packagePath = TomDiagnosticsService.CreatePackage(
                _workspace,
                _store,
                _currentDocumentId,
                _statusLabel.Text,
                _aiHistory);
            SetStatus($"已生成诊断包：{packagePath}");
        }
        catch (Exception error)
        {
            SetStatus($"诊断包生成失败：{error.Message}");
        }
    }

    private async Task DetectCliAsync()
    {
        _detectCliButton.Enabled = false;
        _environmentList.Items.Clear();
        AddEnvironmentItem("Tom", true, "本地桌面界面已运行。");
        AddEnvironmentItem("Workspace", Directory.Exists(_workspace.RootPath), _workspace.RootPath);
        SetStatus("正在检测 Tom AI CLI 环境...");

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var statuses = await TomCliDetector.DetectAsync(timeout.Token);
        foreach (var status in statuses)
        {
            AddEnvironmentItem(status.Label, status.Available, status.Message);
        }

        _detectCliButton.Enabled = true;
        SetStatus("Tom 环境检测完成。");
    }

    private void AddEnvironmentItem(string name, bool ok, string message)
    {
        var item = new ListViewItem(name);
        item.SubItems.Add(ok ? "可用" : "缺失");
        item.SubItems.Add(message);
        item.ForeColor = ok ? TomColors.Success : TomColors.Warning;
        _environmentList.Items.Add(item);
    }

    private void SetStatus(string message)
    {
        _statusLabel.Text = message;
    }

    private static string CreateDefaultDocumentText()
    {
        return """
Tom 文档

在这里编写玩法文档、需求文档或说明文档。

Tom 首版便携应用已内置本地工作空间结构：
- documents
- assets
- exports
- snapshots
- ai-runs
- logs

AI 功能会检测 Codex CLI 与 Claude CLI。缺少 CLI 时，Tom 只提示环境缺失，不会内置安装器。
""";
    }
}

internal static class TomColors
{
    public static Color Header { get; } = Color.FromArgb(35, 54, 69);
    public static Color Surface { get; } = Color.FromArgb(239, 243, 246);
    public static Color Status { get; } = Color.FromArgb(225, 233, 239);
    public static Color Text { get; } = Color.FromArgb(31, 41, 51);
    public static Color Muted { get; } = Color.FromArgb(91, 105, 120);
    public static Color Border { get; } = Color.FromArgb(199, 211, 220);
    public static Color Success { get; } = Color.FromArgb(34, 111, 84);
    public static Color Warning { get; } = Color.FromArgb(153, 83, 24);
}

internal sealed class TomCardPanel : Panel
{
    public TomCardPanel()
    {
        BackColor = Color.White;
        DoubleBuffered = true;
        Margin = new Padding(8);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        using var path = RoundedRectangle(new Rectangle(0, 0, Width - 1, Height - 1), 8);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var fill = new SolidBrush(BackColor);
        using var border = new Pen(TomColors.Border);
        e.Graphics.FillPath(fill, path);
        e.Graphics.DrawPath(border, path);
    }

    private static GraphicsPath RoundedRectangle(Rectangle bounds, int radius)
    {
        var diameter = radius * 2;
        var path = new GraphicsPath();
        path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}

internal sealed record ProviderItem(string Id, string Label)
{
    public override string ToString()
    {
        return Label;
    }
}

internal sealed record TomSnapshotRecord(string Id, string RtfPath, string TextPath, DateTime UpdatedAt)
{
    public override string ToString()
    {
        return $"{UpdatedAt:MM-dd HH:mm}  {Id}";
    }
}

internal sealed record TomOutlineItem(int Start, string Text)
{
    public override string ToString()
    {
        return Text;
    }
}

internal sealed class TomTextViewerForm : Form
{
    public TomTextViewerForm(string title, string text)
    {
        Text = title;
        StartPosition = FormStartPosition.CenterParent;
        Size = new Size(760, 620);
        MinimumSize = new Size(620, 420);
        Font = new Font("Microsoft YaHei UI", 9F);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            ColumnCount = 1,
            Padding = new Padding(12)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        Controls.Add(root);

        root.Controls.Add(new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Both,
            WordWrap = false,
            Text = text
        }, 0, 0);

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft
        };
        var close = new Button { Text = "关闭", DialogResult = DialogResult.OK, Width = 88, Height = 30 };
        actions.Controls.Add(close);
        root.Controls.Add(actions, 0, 1);
        AcceptButton = close;
    }
}

internal sealed class TomAiHistoryDetailForm : Form
{
    public TomAiHistoryDetailForm(TomAiRunRecord record, string actionLabel, string status, string scope)
    {
        Text = "Tom AI 历史详情";
        StartPosition = FormStartPosition.CenterParent;
        Size = new Size(860, 680);
        MinimumSize = new Size(720, 520);
        Font = new Font("Microsoft YaHei UI", 9F);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            ColumnCount = 1,
            Padding = new Padding(12)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        Controls.Add(root);

        var tabs = new TabControl { Dock = DockStyle.Fill };
        tabs.TabPages.Add(CreatePage("摘要", BuildSummary(record, actionLabel, status, scope)));
        tabs.TabPages.Add(CreatePage("修改前", CreateTextBox(record.Before)));
        tabs.TabPages.Add(CreatePage("修改后", CreateTextBox(record.After)));
        tabs.TabPages.Add(CreatePage("Diff", CreateTextBox(TomDiffBuilder.Build(record.Before, record.After))));
        root.Controls.Add(tabs, 0, 0);

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft
        };
        var close = new Button { Text = "关闭", DialogResult = DialogResult.OK, Width = 88, Height = 30 };
        actions.Controls.Add(close);
        root.Controls.Add(actions, 0, 1);
        AcceptButton = close;
    }

    private static TabPage CreatePage(string title, Control content)
    {
        var page = new TabPage(title) { Padding = new Padding(8) };
        page.Controls.Add(content);
        return page;
    }

    private static Control BuildSummary(TomAiRunRecord record, string actionLabel, string status, string scope)
    {
        return CreateTextBox(
            $"时间：{record.CreatedAt:yyyy-MM-dd HH:mm:ss}\r\n" +
            $"动作：{actionLabel}\r\n" +
            $"Provider：{record.Provider}\r\n" +
            $"状态：{status}\r\n" +
            $"范围：{scope}\r\n" +
            $"文档：{record.DocumentId}\r\n" +
            $"摘要：{record.Summary}\r\n" +
            $"错误：{record.Error}\r\n\r\n" +
            $"指令：\r\n{record.Instruction}");
    }

    private static TextBox CreateTextBox(string text)
    {
        return new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            WordWrap = true,
            Text = string.IsNullOrEmpty(text) ? "(无内容)" : text
        };
    }
}

internal static class TomDiffBuilder
{
    public static string Build(string before, string after)
    {
        var beforeLines = NormalizeLines(before);
        var afterLines = NormalizeLines(after);
        if (beforeLines.Length == 0 && afterLines.Length == 0) return "(无差异内容)";

        var table = BuildLcsTable(beforeLines, afterLines);
        var lines = new List<string>();
        BuildDiffLines(beforeLines, afterLines, table, beforeLines.Length, afterLines.Length, lines);
        return string.Join(Environment.NewLine, lines);
    }

    private static string[] NormalizeLines(string value)
    {
        if (string.IsNullOrEmpty(value)) return Array.Empty<string>();
        return value.Replace("\r\n", "\n").Split('\n');
    }

    private static int[,] BuildLcsTable(string[] before, string[] after)
    {
        var table = new int[before.Length + 1, after.Length + 1];
        for (var i = 1; i <= before.Length; i++)
        {
            for (var j = 1; j <= after.Length; j++)
            {
                table[i, j] = before[i - 1] == after[j - 1]
                    ? table[i - 1, j - 1] + 1
                    : Math.Max(table[i - 1, j], table[i, j - 1]);
            }
        }

        return table;
    }

    private static void BuildDiffLines(string[] before, string[] after, int[,] table, int i, int j, List<string> lines)
    {
        if (i > 0 && j > 0 && before[i - 1] == after[j - 1])
        {
            BuildDiffLines(before, after, table, i - 1, j - 1, lines);
            lines.Add("  " + before[i - 1]);
            return;
        }

        if (j > 0 && (i == 0 || table[i, j - 1] >= table[i - 1, j]))
        {
            BuildDiffLines(before, after, table, i, j - 1, lines);
            lines.Add("+ " + after[j - 1]);
            return;
        }

        if (i > 0)
        {
            BuildDiffLines(before, after, table, i - 1, j, lines);
            lines.Add("- " + before[i - 1]);
        }
    }
}

internal sealed class TomAiPreviewForm : Form
{
    public TomAiPreviewForm(TomAiResult result)
    {
        Text = "Tom AI 修改预览";
        StartPosition = FormStartPosition.CenterParent;
        Size = new Size(720, 560);
        MinimumSize = new Size(640, 480);
        Font = new Font("Microsoft YaHei UI", 9F);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 4,
            ColumnCount = 1,
            Padding = new Padding(12)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 45));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 55));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        Controls.Add(root);

        var summary = new Label
        {
            Dock = DockStyle.Fill,
            Text = result.Summary,
            Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };
        root.Controls.Add(summary, 0, 0);

        root.Controls.Add(CreateTextBox("修改前", result.Before), 0, 1);
        root.Controls.Add(CreateTextBox("修改后", result.After), 0, 2);

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft
        };
        var apply = new Button { Text = "应用", DialogResult = DialogResult.OK, Width = 88, Height = 30 };
        var cancel = new Button { Text = "取消", DialogResult = DialogResult.Cancel, Width = 88, Height = 30 };
        actions.Controls.Add(apply);
        actions.Controls.Add(cancel);
        root.Controls.Add(actions, 0, 3);

        AcceptButton = apply;
        CancelButton = cancel;
    }

    private static Control CreateTextBox(string title, string text)
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            ColumnCount = 1
        };
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        panel.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = title,
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 0);

        panel.Controls.Add(new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            Text = text
        }, 0, 1);
        return panel;
    }
}

internal sealed class TomInputDialog : Form
{
    private readonly TextBox _input = new();

    public TomInputDialog(string title, string prompt)
    {
        Text = title;
        StartPosition = FormStartPosition.CenterParent;
        Size = new Size(420, 150);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Font = new Font("Microsoft YaHei UI", 9F);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 3,
            ColumnCount = 1,
            Padding = new Padding(12)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        Controls.Add(root);

        root.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = prompt,
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 0);

        _input.Dock = DockStyle.Fill;
        root.Controls.Add(_input, 0, 1);

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft
        };
        var ok = new Button { Text = "确定", DialogResult = DialogResult.OK, Width = 80 };
        var cancel = new Button { Text = "取消", DialogResult = DialogResult.Cancel, Width = 80 };
        actions.Controls.Add(ok);
        actions.Controls.Add(cancel);
        root.Controls.Add(actions, 0, 2);

        AcceptButton = ok;
        CancelButton = cancel;
    }

    public string Value => _input.Text;
}

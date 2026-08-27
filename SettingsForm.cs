namespace DesktopScroll;

public sealed class SettingsForm : Form
{
    private readonly CheckBox _enabledCheckBox;
    private readonly CheckBox _startupCheckBox;
    private readonly NumericUpDown _rowsInput;
    private readonly NumericUpDown _columnsInput;
    private readonly NumericUpDown _verticalStepInput;
    private readonly NumericUpDown _horizontalStepInput;
    private readonly NumericUpDown _repeatDelayInput;
    private readonly NumericUpDown _repeatIntervalInput;
    private readonly CheckBox _showDotCheckBox;
    private readonly NumericUpDown _dotSizeInput;
    private readonly NumericUpDown _dotOpacityInput;
    private readonly TextBox _activateHotkeyInput;
    private readonly TextBox _resumeHotkeyInput;
    private readonly TextBox _scrollUpKeyInput;
    private readonly TextBox _scrollDownKeyInput;
    private readonly TextBox _scrollLeftKeyInput;
    private readonly TextBox _scrollRightKeyInput;

    public Settings EditedSettings { get; }

    public SettingsForm(Settings source)
    {
        EditedSettings = SettingsService.Clone(source);

        Text = "DesktopScroll Settings";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        Width = 520;
        Height = 620;

        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 18,
            Padding = new Padding(12),
            AutoSize = true
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

        _enabledCheckBox = new CheckBox { Text = "Enabled", Checked = EditedSettings.Enabled, Dock = DockStyle.Fill };
        _startupCheckBox = new CheckBox { Text = "Start with Windows", Checked = EditedSettings.StartWithWindows, Dock = DockStyle.Fill };
        _rowsInput = CreateNumeric(EditedSettings.Grid.Rows, 1, 40);
        _columnsInput = CreateNumeric(EditedSettings.Grid.Columns, 1, 60);
        _verticalStepInput = CreateNumeric(EditedSettings.Scrolling.VerticalStep, 1, 1200);
        _horizontalStepInput = CreateNumeric(EditedSettings.Scrolling.HorizontalStep, 1, 1200);
        _repeatDelayInput = CreateNumeric(EditedSettings.Scrolling.RepeatDelayMs, 0, 2000);
        _repeatIntervalInput = CreateNumeric(EditedSettings.Scrolling.RepeatIntervalMs, 1, 2000);
        _showDotCheckBox = new CheckBox { Text = "Show Cursor Dot", Checked = EditedSettings.Visuals.ShowCursorDot, Dock = DockStyle.Fill };
        _dotSizeInput = CreateNumeric(EditedSettings.Visuals.CursorDotSize, 4, 40);
        _dotOpacityInput = CreateNumeric((decimal)EditedSettings.Visuals.CursorDotOpacity * 100m, 10, 100);

        _activateHotkeyInput = new TextBox { Text = EditedSettings.Hotkeys.Activate, Dock = DockStyle.Fill };
        _resumeHotkeyInput = new TextBox { Text = EditedSettings.Hotkeys.Resume, Dock = DockStyle.Fill };
        _scrollUpKeyInput = new TextBox { Text = EditedSettings.ScrollKeys.Up, Dock = DockStyle.Fill };
        _scrollDownKeyInput = new TextBox { Text = EditedSettings.ScrollKeys.Down, Dock = DockStyle.Fill };
        _scrollLeftKeyInput = new TextBox { Text = EditedSettings.ScrollKeys.Left, Dock = DockStyle.Fill };
        _scrollRightKeyInput = new TextBox { Text = EditedSettings.ScrollKeys.Right, Dock = DockStyle.Fill };

        AddRow(panel, "", _enabledCheckBox);
        AddRow(panel, "", _startupCheckBox);
        AddRow(panel, "Activation Hotkey", _activateHotkeyInput);
        AddRow(panel, "Resume Hotkey", _resumeHotkeyInput);
        AddRow(panel, "Scroll Up Key", _scrollUpKeyInput);
        AddRow(panel, "Scroll Down Key", _scrollDownKeyInput);
        AddRow(panel, "Scroll Left Key", _scrollLeftKeyInput);
        AddRow(panel, "Scroll Right Key", _scrollRightKeyInput);
        AddRow(panel, "Grid Rows", _rowsInput);
        AddRow(panel, "Grid Columns", _columnsInput);
        AddRow(panel, "Vertical Step", _verticalStepInput);
        AddRow(panel, "Horizontal Step", _horizontalStepInput);
        AddRow(panel, "Repeat Delay (ms)", _repeatDelayInput);
        AddRow(panel, "Repeat Interval (ms)", _repeatIntervalInput);
        AddRow(panel, "", _showDotCheckBox);
        AddRow(panel, "Dot Size", _dotSizeInput);
        AddRow(panel, "Dot Opacity (%)", _dotOpacityInput);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(12),
            AutoSize = true
        };

        var saveButton = new Button { Text = "Save", Width = 90 };
        var cancelButton = new Button { Text = "Cancel", Width = 90 };

        saveButton.Click += (_, _) =>
        {
            ApplyValues();
            DialogResult = DialogResult.OK;
            Close();
        };

        cancelButton.Click += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };

        buttons.Controls.Add(saveButton);
        buttons.Controls.Add(cancelButton);

        Controls.Add(panel);
        Controls.Add(buttons);
    }

    private static NumericUpDown CreateNumeric(decimal value, decimal min, decimal max)
    {
        return new NumericUpDown
        {
            Minimum = min,
            Maximum = max,
            Value = Math.Clamp(value, min, max),
            Dock = DockStyle.Fill
        };
    }

    private static void AddRow(TableLayoutPanel panel, string label, Control editor)
    {
        var row = panel.RowCount++;
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var labelControl = new Label
        {
            Text = label,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoSize = true
        };

        panel.Controls.Add(labelControl, 0, row);
        panel.Controls.Add(editor, 1, row);
    }

    private void ApplyValues()
    {
        EditedSettings.Enabled = _enabledCheckBox.Checked;
        EditedSettings.StartWithWindows = _startupCheckBox.Checked;

        EditedSettings.Hotkeys.Activate = _activateHotkeyInput.Text.Trim();
        EditedSettings.Hotkeys.Resume = _resumeHotkeyInput.Text.Trim();

        EditedSettings.ScrollKeys.Up = _scrollUpKeyInput.Text.Trim();
        EditedSettings.ScrollKeys.Down = _scrollDownKeyInput.Text.Trim();
        EditedSettings.ScrollKeys.Left = _scrollLeftKeyInput.Text.Trim();
        EditedSettings.ScrollKeys.Right = _scrollRightKeyInput.Text.Trim();

        EditedSettings.Grid.Rows = (int)_rowsInput.Value;
        EditedSettings.Grid.Columns = (int)_columnsInput.Value;

        EditedSettings.Scrolling.VerticalStep = (int)_verticalStepInput.Value;
        EditedSettings.Scrolling.HorizontalStep = (int)_horizontalStepInput.Value;
        EditedSettings.Scrolling.RepeatDelayMs = (int)_repeatDelayInput.Value;
        EditedSettings.Scrolling.RepeatIntervalMs = (int)_repeatIntervalInput.Value;

        EditedSettings.Visuals.ShowCursorDot = _showDotCheckBox.Checked;
        EditedSettings.Visuals.CursorDotSize = (int)_dotSizeInput.Value;
        EditedSettings.Visuals.CursorDotOpacity = (double)(_dotOpacityInput.Value / 100m);
    }
}

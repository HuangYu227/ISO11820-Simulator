using Guna.UI2.WinForms;
using ISO11820Simulator.Models;

namespace ISO11820Simulator.UI;

public sealed class PhenomenonForm : Form
{
    private readonly Guna2CheckBox _hasFlame = new() { Text = "出现持续火焰", ForeColor = Theme.Text, AutoSize = true, Font = Theme.Body };
    private readonly Guna2NumericUpDown _flameTime = Theme.GunaNumber(0, 0, 100000, 0);
    private readonly Guna2NumericUpDown _flameDuration = Theme.GunaNumber(0, 0, 100000, 0);
    private readonly Guna2NumericUpDown _postWeight;
    private readonly Guna2TextBox _memo = Theme.GunaTextBox(placeholder: "备注信息");

    public ResultSaveRequest? Request { get; private set; }

    public PhenomenonForm(double preWeight)
    {
        _postWeight = Theme.GunaNumber((decimal)(preWeight * 0.96), 0.01m, (decimal)(preWeight * 1.2), 2);
        Text = "试验现象记录";
        ClientSize = new Size(560, 480);
        MinimumSize = new Size(500, 500);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Theme.Background;
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = true;
        BuildUi(preWeight);
        _hasFlame.CheckedChanged += (_, _) => ToggleFlameInputs();
        ToggleFlameInputs();
        Theme.FadeIn(this, 300);
    }

    private void BuildUi(double preWeight)
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1, Padding = new Padding(16), BackColor = Theme.Background };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        Controls.Add(root);

        var card = new Guna2Panel { Dock = DockStyle.Fill, FillColor = Theme.Surface, BorderRadius = 8, BorderColor = Theme.Border, BorderThickness = 1, Padding = new Padding(16) };
        var grid = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 0, BackColor = Theme.Surface };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        card.Controls.Add(grid);
        root.Controls.Add(card);

        AddRow(grid, "试验前质量", Theme.Label($"{preWeight:F2} g", 10, true, Theme.Accent));
        AddRow(grid, "试验后质量(g)", _postWeight);
        AddRow(grid, "火焰现象", _hasFlame);
        AddRow(grid, "火焰发生时刻(s)", _flameTime);
        AddRow(grid, "火焰持续时间(s)", _flameDuration);
        _memo.Multiline = true;
        _memo.Height = 80;
        AddRow(grid, "备注", _memo, 100);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, BackColor = Theme.Background, Padding = new Padding(0, 8, 0, 0) };
        var ok = Theme.GunaButton("保存并生成报告", Theme.Success); ok.Width = 160; ok.Click += (_, _) => Submit();
        var cancel = Theme.GunaButton("取消"); cancel.Width = 100; cancel.Click += (_, _) => Close();
        buttons.Controls.Add(ok); buttons.Controls.Add(cancel);
        root.Controls.Add(buttons);
    }

    private void ToggleFlameInputs()
    {
        _flameTime.Enabled = _hasFlame.Checked;
        _flameDuration.Enabled = _hasFlame.Checked;
    }

    private static void AddRow(TableLayoutPanel grid, string label, Control control, int height = 44)
    {
        var row = grid.RowCount++;
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, height));
        var lbl = Theme.Label(label, 10, true, Theme.Muted);
        lbl.Dock = DockStyle.Fill;
        lbl.TextAlign = ContentAlignment.MiddleLeft;
        grid.Controls.Add(lbl, 0, row);
        control.Dock = DockStyle.Fill;
        grid.Controls.Add(control, 1, row);
    }

    private void Submit()
    {
        Request = new ResultSaveRequest
        {
            HasFlame = _hasFlame.Checked,
            FlameTime = (int)_flameTime.Value,
            FlameDuration = (int)_flameDuration.Value,
            PostWeight = (double)_postWeight.Value,
            Memo = _memo.Text.Trim()
        };
        DialogResult = DialogResult.OK;
        Close();
    }
}

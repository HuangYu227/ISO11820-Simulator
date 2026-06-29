using Guna.UI2.WinForms;
using ISO11820Simulator.Config;

namespace ISO11820Simulator.UI;

public sealed class SettingsForm : Form
{
    private readonly AppSettings _settings;
    private readonly Guna2NumericUpDown _target;
    private readonly Guna2NumericUpDown _rate;
    private readonly Guna2NumericUpDown _noise;
    private readonly Guna2NumericUpDown _stable;
    private readonly Guna2NumericUpDown _demoDuration;
    private readonly Guna2TextBox _baseDir;

    public SettingsForm(AppSettings settings)
    {
        _settings = settings;
        _target = Theme.GunaNumber((decimal)settings.Simulation.TargetFurnaceTemp, 100, 1000, 1);
        _rate = Theme.GunaNumber((decimal)settings.Simulation.HeatingRatePerSecond, 1, 100, 1);
        _noise = Theme.GunaNumber((decimal)settings.Simulation.TempFluctuation, 0, 10, 2);
        _stable = Theme.GunaNumber((decimal)settings.Simulation.StableThreshold, 0, 20, 1);
        _demoDuration = Theme.GunaNumber(settings.Experiment.DemoDurationSeconds, 10, 3600, 0);
        _baseDir = Theme.GunaTextBox(settings.FileStorage.BaseDirectory, "基础目录");
        Text = "参数设置";
        ClientSize = new Size(580, 460);
        MinimumSize = new Size(520, 480);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Theme.Background;
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = true;
        BuildUi();
        Theme.FadeIn(this, 300);
    }

    private void BuildUi()
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1, Padding = new Padding(16), BackColor = Theme.Background };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        Controls.Add(root);

        var card = new Guna2Panel { Dock = DockStyle.Fill, FillColor = Theme.Surface, BorderRadius = 8, BorderColor = Theme.Border, BorderThickness = 1, Padding = new Padding(16) };
        var grid = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 0, BackColor = Theme.Surface };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        card.Controls.Add(grid);
        root.Controls.Add(card);

        AddRow(grid, "目标炉温(℃)", _target);
        AddRow(grid, "升温速度(℃/s)", _rate);
        AddRow(grid, "随机波动(℃)", _noise);
        AddRow(grid, "稳定阈值(℃)", _stable);
        AddRow(grid, "演示时长(s)", _demoDuration);
        AddRow(grid, "基础目录", _baseDir);
        AddRow(grid, "提示", Theme.Label("保存后立即影响后续仿真", 9, false, Theme.Muted), 50);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, BackColor = Theme.Background, Padding = new Padding(0, 8, 0, 0) };
        var ok = Theme.GunaButton("保存", Theme.Success); ok.Width = 110; ok.Click += (_, _) => Save();
        var cancel = Theme.GunaButton("取消"); cancel.Width = 100; cancel.Click += (_, _) => Close();
        buttons.Controls.Add(ok); buttons.Controls.Add(cancel);
        root.Controls.Add(buttons);
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

    private void Save()
    {
        _settings.Simulation.TargetFurnaceTemp = (double)_target.Value;
        _settings.Simulation.HeatingRatePerSecond = (double)_rate.Value;
        _settings.Simulation.TempFluctuation = (double)_noise.Value;
        _settings.Simulation.StableThreshold = (double)_stable.Value;
        _settings.Experiment.DemoDurationSeconds = (int)_demoDuration.Value;
        _settings.FileStorage.BaseDirectory = _baseDir.Text.Trim();
        AppConfig.Save(_settings);
        DialogResult = DialogResult.OK;
        Close();
    }
}

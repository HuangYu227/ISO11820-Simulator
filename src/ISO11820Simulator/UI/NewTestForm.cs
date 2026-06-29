using Guna.UI2.WinForms;
using ISO11820Simulator.App;
using ISO11820Simulator.Models;

namespace ISO11820Simulator.UI;

public sealed class NewTestForm : Form
{
    private readonly Guna2TextBox _productId = Theme.GunaTextBox(DateTime.Now.ToString("yyyyMMdd-001"), "样品编号");
    private readonly Guna2TextBox _testId = Theme.GunaTextBox(DateTime.Now.ToString("yyyyMMdd-HHmmss"), "试验ID");
    private readonly Guna2TextBox _productName = Theme.GunaTextBox("岩棉隔热板", "样品名称");
    private readonly Guna2TextBox _specific = Theme.GunaTextBox("100×50×25mm", "规格型号");
    private readonly Guna2NumericUpDown _diameter = Theme.GunaNumber(50, 1, 500, 1);
    private readonly Guna2NumericUpDown _height = Theme.GunaNumber(25, 1, 500, 1);
    private readonly Guna2NumericUpDown _ambientTemp = Theme.GunaNumber(25, -20, 80, 1);
    private readonly Guna2NumericUpDown _ambientHumi = Theme.GunaNumber(50, 0, 100, 1);
    private readonly Guna2NumericUpDown _preWeight = Theme.GunaNumber(100, 0.01m, 100000, 2);
    private readonly Guna2CheckBox _standard = new() { Text = "标准 60 分钟模式", ForeColor = Theme.Text, Checked = !GlobalApp.Settings.Experiment.UseDemoDurationByDefault, AutoSize = true, Font = Theme.Body };
    private readonly Guna2NumericUpDown _durationMin = Theme.GunaNumber(GlobalApp.Settings.Experiment.UseDemoDurationByDefault ? GlobalApp.Settings.Experiment.DemoDurationSeconds / 60m : 60, 1, 240, 0);

    public TestSession? Test { get; private set; }

    public NewTestForm()
    {
        Text = "新建试验";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(640, 680);
        MinimumSize = new Size(580, 680);
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
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        card.Controls.Add(grid);
        root.Controls.Add(card);

        AddRow(grid, "样品编号", _productId);
        AddRow(grid, "试验ID", _testId);
        AddRow(grid, "样品名称", _productName);
        AddRow(grid, "规格型号", _specific);
        AddRow(grid, "直径(mm)", _diameter);
        AddRow(grid, "高度(mm)", _height);
        AddRow(grid, "环境温度(℃)", _ambientTemp);
        AddRow(grid, "环境湿度(%)", _ambientHumi);
        AddRow(grid, "试验前质量(g)", _preWeight);
        AddRow(grid, "时长模式", _standard);
        AddRow(grid, "自定义分钟", _durationMin);
        AddRow(grid, "操作员", Theme.Label(GlobalApp.Session.Username, 10, true, Theme.Accent));
        var app = GlobalApp.Db.GetDefaultApparatus();
        AddRow(grid, "设备信息", Theme.Label($"{app.InnerNumber} / {app.Name}", 10, false, Theme.Muted));
        AddRow(grid, "恒功率", Theme.Label(app.ConstPower.ToString(), 10, true, Theme.Muted));

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, BackColor = Theme.Background, Padding = new Padding(0, 8, 0, 0) };
        var ok = Theme.GunaButton("创建试验", Theme.Success); ok.Width = 130; ok.Click += (_, _) => Submit(app);
        var cancel = Theme.GunaButton("取消"); cancel.Width = 100; cancel.Click += (_, _) => Close();
        buttons.Controls.Add(ok); buttons.Controls.Add(cancel);
        root.Controls.Add(buttons);
    }

    private static void AddRow(TableLayoutPanel grid, string label, Control control)
    {
        var row = grid.RowCount++;
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        var lbl = Theme.Label(label, 10, true, Theme.Muted);
        lbl.Dock = DockStyle.Fill;
        lbl.TextAlign = ContentAlignment.MiddleLeft;
        grid.Controls.Add(lbl, 0, row);
        control.Dock = DockStyle.Fill;
        grid.Controls.Add(control, 1, row);
    }

    private void Submit((string InnerNumber, string Name, DateTime CheckDateTo, int ConstPower) app)
    {
        if (string.IsNullOrWhiteSpace(_productId.Text) || string.IsNullOrWhiteSpace(_productName.Text))
        {
            MessageBox.Show("样品编号和样品名称不能为空");
            return;
        }
        var useStandard = _standard.Checked;
        var duration = useStandard ? GlobalApp.Settings.Experiment.StandardDurationSeconds : (int)_durationMin.Value * 60;
        Test = new TestSession
        {
            ProductId = _productId.Text.Trim(),
            TestId = _testId.Text.Trim(),
            ProductName = _productName.Text.Trim(),
            Specific = _specific.Text.Trim(),
            Diameter = (double)_diameter.Value,
            Height = (double)_height.Value,
            AmbientTemperature = (double)_ambientTemp.Value,
            AmbientHumidity = (double)_ambientHumi.Value,
            Operator = GlobalApp.Session.Username,
            ApparatusId = app.InnerNumber,
            ApparatusName = app.Name,
            ApparatusCheckDate = app.CheckDateTo,
            ReportNo = _productId.Text.Trim(),
            PreWeight = (double)_preWeight.Value,
            TargetDurationSeconds = duration,
            UseStandardDuration = useStandard,
            ConstPower = app.ConstPower
        };
        DialogResult = DialogResult.OK;
        Close();
    }
}

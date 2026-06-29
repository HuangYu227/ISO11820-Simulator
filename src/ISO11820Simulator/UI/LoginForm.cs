using Guna.UI2.WinForms;
using ISO11820Simulator.Data;
using ISO11820Simulator.Models;

namespace ISO11820Simulator.UI;

public sealed class LoginForm : Form
{
    private readonly DbHelper _db;
    private readonly Guna2RadioButton _admin = Theme.GunaRadio("管理员", true);
    private readonly Guna2RadioButton _operator = Theme.GunaRadio("试验员");
    private readonly Guna2TextBox _pwd = Theme.GunaTextBox(placeholder: "请输入访问口令");
    private readonly Label _error = Theme.Label("", 9, false, Theme.Danger);
    private readonly Guna2Button _loginBtn = Theme.GunaButton("登录系统", Theme.Accent);
    private readonly Guna2Panel _decorLine = new()
    {
        FillColor = Theme.Accent,
        BorderRadius = 2,
    };

    public UserSession? Session { get; private set; }

    public LoginForm(DbHelper db)
    {
        _db = db;
        Text = "ISO 11820 登录";
        ClientSize = new Size(520, 460);
        MinimumSize = new Size(480, 420);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = true;
        BackColor = Theme.Background;
        Font = Theme.Body;
        BuildUi();
        WireEvents();
        Theme.FadeIn(this, 400);
    }

    private void BuildUi()
    {
        // 外层居中容器
        var wrap = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            BackColor = Theme.Background
        };
        wrap.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));
        wrap.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 380));
        wrap.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));
        Controls.Add(wrap);

        // 主卡片
        var card = Theme.GunaCard(10);
        card.Dock = DockStyle.Fill;
        card.Margin = new Padding(0, 40, 0, 40);
        wrap.Controls.Add(card, 1, 0);

        // 卡片内部布局
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 10,
            ColumnCount = 1,
            BackColor = Theme.Surface
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 8));   // 顶部间距
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 3));   // 装饰线
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 16));  // 间距
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));  // 标题
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));  // 副标题
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 20));  // 间距
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));  // 角色选择
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));  // 密码框
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));  // 登录按钮
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));  // 底部弹性
        card.Controls.Add(layout);

        // 装饰渐变线
        _decorLine.Dock = DockStyle.Fill;
        layout.Controls.Add(_decorLine, 0, 1);

        // 标题
        var title = Theme.Label("ISO 11820", 22, true, Theme.Accent);
        title.Dock = DockStyle.Fill;
        title.TextAlign = ContentAlignment.MiddleCenter;
        layout.Controls.Add(title, 0, 3);

        // 副标题
        var subtitle = Theme.Label("建筑材料不燃性试验仿真系统", 11, false, Theme.Muted);
        subtitle.Dock = DockStyle.Fill;
        subtitle.TextAlign = ContentAlignment.MiddleCenter;
        layout.Controls.Add(subtitle, 0, 4);

        // 角色选择
        var rolePanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.Surface,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(20, 0, 20, 0),
            WrapContents = false
        };
        rolePanel.Controls.Add(_admin);
        rolePanel.Controls.Add(Theme.Label("    "));
        rolePanel.Controls.Add(_operator);
        layout.Controls.Add(rolePanel, 0, 6);

        // 密码框
        _pwd.UseSystemPasswordChar = true;
        _pwd.Text = "123456";
        _pwd.Dock = DockStyle.Fill;
        _pwd.Margin = new Padding(20, 4, 20, 4);
        layout.Controls.Add(_pwd, 0, 7);

        // 错误提示
        _error.Dock = DockStyle.Fill;
        _error.TextAlign = ContentAlignment.MiddleCenter;
        layout.Controls.Add(_error, 0, 8);

        // 登录按钮
        _loginBtn.Dock = DockStyle.Fill;
        _loginBtn.Margin = new Padding(20, 4, 20, 4);
        _loginBtn.FillColor = Theme.Accent;
        _loginBtn.ForeColor = Color.FromArgb(3, 7, 18);
        _loginBtn.Font = new Font("Microsoft YaHei UI", 11, FontStyle.Bold);
        layout.Controls.Add(_loginBtn, 0, 9);

        // 底部版本信息（嵌入 wrap 底部行）
        var versionPanel = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Background, Height = 28 };
        var version = Theme.Label("v1.0 · 本地仿真 · 无需硬件", 8, false, Theme.Muted);
        version.Dock = DockStyle.Fill;
        version.TextAlign = ContentAlignment.MiddleCenter;
        versionPanel.Controls.Add(version);
        // 在 wrap 的 1 列布局中追加一行
        wrap.RowCount = 2;
        wrap.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        wrap.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        wrap.Controls.Add(versionPanel, 0, 1);
    }

    private void WireEvents()
    {
        _loginBtn.Click += (_, _) => TryLogin();
        _pwd.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) TryLogin(); };
    }

    private void TryLogin()
    {
        if (string.IsNullOrWhiteSpace(_pwd.Text))
        {
            _error.Text = "请输入访问口令";
            _pwd.Focus();
            return;
        }
        var username = _admin.Checked ? "admin" : "experimenter";
        if (_db.Login(username, _pwd.Text.Trim(), out var session) && session is not null)
        {
            Session = session;
            DialogResult = DialogResult.OK;
            Close();
            return;
        }
        _error.Text = "密码错误，请重新输入";
        _pwd.Focus();
        _pwd.SelectAll();
    }
}

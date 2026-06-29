using Guna.UI2.WinForms;

namespace ISO11820Simulator.UI;

/// <summary>
/// 深空工业风设计系统 — 9 个设计 Token + Guna2 控件工厂 + 动效辅助。
/// </summary>
public static class Theme
{
    // ───────────── 色彩 Token ─────────────
    public static readonly Color Background   = Color.FromArgb(15, 23, 42);       // #0F172A 深海蓝
    public static readonly Color Surface      = Color.FromArgb(30, 41, 59);       // #1E293B 石板灰
    public static readonly Color SurfaceAlt   = Color.FromArgb(51, 65, 85);       // #334155 暖灰
    public static readonly Color Border       = Color.FromArgb(71, 85, 105);      // #475569
    public static readonly Color Text         = Color.FromArgb(248, 250, 252);    // #F8FAFC 月光白
    public static readonly Color Muted        = Color.FromArgb(148, 163, 184);    // #94A3B8 银灰
    public static readonly Color Accent       = Color.FromArgb(6, 182, 212);      // #06B6D4 霓虹青
    public static readonly Color AccentGlow   = Color.FromArgb(34, 211, 238);     // #22D3EE 高亮青
    public static readonly Color Success      = Color.FromArgb(16, 185, 129);     // #10B981 翡翠绿
    public static readonly Color Warning      = Color.FromArgb(245, 158, 11);     // #F59E0B 琥珀黄
    public static readonly Color Danger       = Color.FromArgb(239, 68, 68);      // #EF4444 珊瑚红
    public static readonly Color Info         = Color.FromArgb(59, 130, 246);     // #3B82F6 天蓝

    // 兼容旧代码的别名
    public static readonly Color Panel  = Surface;
    public static readonly Color Panel2 = SurfaceAlt;

    // ───────────── 字体 ─────────────
    public static readonly Font H1   = new("Microsoft YaHei UI", 20, FontStyle.Bold);
    public static readonly Font H2   = new("Microsoft YaHei UI", 13, FontStyle.Bold);
    public static readonly Font Body = new("Microsoft YaHei UI", 10);
    public static readonly Font Mono = new("Consolas", 18, FontStyle.Bold);

    // ═══════════════ Guna2 控件工厂 ═══════════════

    /// <summary>
    /// 创建深空工业风按钮 — 圆角 + hover 渐变 + press 缩放。
    /// </summary>
    public static Guna2Button GunaButton(string text, Color? fillColor = null, int radius = 6)
    {
        var btn = new Guna2Button
        {
            Text = text,
            Height = 40,
            MinimumSize = new Size(100, 40),
            FillColor = fillColor ?? SurfaceAlt,
            ForeColor = Text,
            Font = new Font("Microsoft YaHei UI", 10, FontStyle.Bold),
            TextAlign = HorizontalAlignment.Center,
            Cursor = Cursors.Hand,
            Margin = new Padding(4, 6, 4, 6),
            BorderRadius = radius,
            BorderColor = Border,
            BorderThickness = 1,
            Animated = true,
            DisabledState = { FillColor = Surface, ForeColor = Muted },
            HoverState = { FillColor = AccentGlow, ForeColor = Color.FromArgb(3, 7, 18) },
            PressedColor = Color.FromArgb(8, 145, 178),
        };
        if (fillColor.HasValue)
        {
            btn.HoverState.FillColor = ControlPaint.Light(fillColor.Value, 0.2f);
            btn.HoverState.ForeColor = Color.White;
        }
        return btn;
    }

    /// <summary>
    /// 创建深空工业风文本框 — 圆角 + focus 发光边框。
    /// </summary>
    public static Guna2TextBox GunaTextBox(string? text = null, string? placeholder = null, int radius = 6)
    {
        return new Guna2TextBox
        {
            Text = text ?? string.Empty,
            PlaceholderText = placeholder ?? string.Empty,
            FillColor = Background,
            ForeColor = Text,
            PlaceholderForeColor = Muted,
            Font = Body,
            Height = 36,
            MinimumSize = new Size(60, 36),
            BorderRadius = radius,
            BorderColor = Border,
            BorderThickness = 1,
            FocusedState = { BorderColor = Accent, FillColor = Color.FromArgb(12, 20, 38) },
            HoverState = { BorderColor = AccentGlow },
        };
    }

    /// <summary>
    /// 创建深空工业风数字输入框 — 圆角 + 深色主题。
    /// </summary>
    public static Guna2NumericUpDown GunaNumber(decimal value, decimal min, decimal max, int decimals = 1, int radius = 6)
    {
        return new Guna2NumericUpDown
        {
            Value = Math.Min(max, Math.Max(min, value)),
            Minimum = min,
            Maximum = max,
            DecimalPlaces = decimals,
            Increment = decimals == 0 ? 1 : 0.1m,
            FillColor = Background,
            ForeColor = Text,
            Font = Body,
            Height = 36,
            MinimumSize = new Size(60, 36),
            BorderRadius = radius,
            BorderColor = Border,
            BorderThickness = 1,
            FocusedState = { BorderColor = Accent },
        };
    }

    /// <summary>
    /// 创建深空工业风面板（卡片）— 圆角 + 边框。
    /// </summary>
    public static Guna2Panel GunaCard(int radius = 8)
    {
        return new Guna2Panel
        {
            FillColor = Surface,
            Padding = new Padding(14),
            Margin = new Padding(8),
            BorderRadius = radius,
            BorderColor = Border,
            BorderThickness = 1,
        };
    }

    /// <summary>
    /// 创建深空工业风单选按钮 — 自定义颜色 + 动画。
    /// </summary>
    public static Guna2RadioButton GunaRadio(string text, bool isChecked = false)
    {
        return new Guna2RadioButton
        {
            Text = text,
            Checked = isChecked,
            ForeColor = Text,
            Font = Body,
            AutoSize = true,
            CheckedState = { FillColor = Accent, BorderColor = Accent, InnerColor = Background },
            UncheckedState = { BorderColor = Border, FillColor = Surface },
        };
    }

    /// <summary>
    /// 创建深空工业风下拉框 — 圆角 + 深色主题。
    /// </summary>
    public static Guna2ComboBox GunaCombo(int radius = 6)
    {
        return new Guna2ComboBox
        {
            FillColor = Background,
            ForeColor = Text,
            Font = Body,
            BorderRadius = radius,
            BorderColor = Border,
            BorderThickness = 1,
            DropDownStyle = ComboBoxStyle.DropDownList,
            FocusedState = { BorderColor = Accent },
        };
    }

    // ═══════════════ 兼容旧代码的工厂 ═══════════════

    /// <summary>
    /// 创建标准 Label（保留兼容）。
    /// </summary>
    public static Label Label(string text, int size = 10, bool bold = false, Color? color = null)
    {
        return new Label
        {
            Text = text,
            AutoSize = true,
            ForeColor = color ?? Text,
            Font = new Font("Microsoft YaHei UI", size, bold ? FontStyle.Bold : FontStyle.Regular),
            Margin = new Padding(4)
        };
    }

    /// <summary>
    /// 创建标准 Button（保留兼容）。
    /// </summary>
    public static Button Button(string text, Color? color = null)
    {
        var btn = new Button
        {
            Text = text,
            Height = 42,
            MinimumSize = new Size(100, 42),
            FlatStyle = FlatStyle.Flat,
            ForeColor = Text,
            BackColor = color ?? SurfaceAlt,
            Font = new Font("Microsoft YaHei UI", 10, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter,
            Cursor = Cursors.Hand,
            Margin = new Padding(4, 6, 4, 6)
        };
        btn.FlatAppearance.BorderColor = Border;
        btn.FlatAppearance.MouseOverBackColor = Accent;
        return btn;
    }

    /// <summary>
    /// 创建标准 Panel 卡片（保留兼容）。
    /// </summary>
    public static Panel Card()
    {
        return new Panel
        {
            BackColor = Surface,
            Padding = new Padding(14),
            Margin = new Padding(8)
        };
    }

    /// <summary>
    /// 创建标准 TextBox（保留兼容）。
    /// </summary>
    public static TextBox TextBox(string? text = null)
    {
        return new TextBox
        {
            Text = text ?? string.Empty,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Background,
            ForeColor = Text,
            Font = Body,
            Height = 30,
            MinimumSize = new Size(60, 30)
        };
    }

    /// <summary>
    /// 创建标准 NumericUpDown（保留兼容）。
    /// </summary>
    public static NumericUpDown Number(decimal value, decimal min, decimal max, int decimals = 1)
    {
        return new NumericUpDown
        {
            Value = Math.Min(max, Math.Max(min, value)),
            Minimum = min,
            Maximum = max,
            DecimalPlaces = decimals,
            Increment = decimals == 0 ? 1 : 0.1m,
            BackColor = Background,
            ForeColor = Text,
            BorderStyle = BorderStyle.FixedSingle,
            Font = Body,
            Height = 30,
            MinimumSize = new Size(60, 30)
        };
    }

    /// <summary>
    /// 样式化 DataGridView（保留兼容）。
    /// </summary>
    public static void StyleGrid(DataGridView grid)
    {
        grid.BackgroundColor = Surface;
        grid.GridColor = Border;
        grid.BorderStyle = BorderStyle.None;
        grid.EnableHeadersVisualStyles = false;
        grid.ColumnHeadersDefaultCellStyle.BackColor = SurfaceAlt;
        grid.ColumnHeadersDefaultCellStyle.ForeColor = Text;
        grid.ColumnHeadersDefaultCellStyle.Font = new Font("Microsoft YaHei UI", 10, FontStyle.Bold);
        grid.DefaultCellStyle.BackColor = Background;
        grid.DefaultCellStyle.ForeColor = Text;
        grid.DefaultCellStyle.SelectionBackColor = Accent;
        grid.DefaultCellStyle.SelectionForeColor = Color.White;
        grid.RowHeadersVisible = false;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.MultiSelect = false;
        grid.ReadOnly = true;
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
    }

    /// <summary>
    /// 样式化 Guna2DataGridView — 圆角行 + hover 高亮 + 选中渐变。
    /// </summary>
    public static void StyleGunaGrid(Guna2DataGridView grid)
    {
        grid.BackgroundColor = Surface;
        grid.GridColor = Border;
        grid.BorderStyle = BorderStyle.None;
        grid.EnableHeadersVisualStyles = false;
        grid.ColumnHeadersDefaultCellStyle.BackColor = SurfaceAlt;
        grid.ColumnHeadersDefaultCellStyle.ForeColor = Text;
        grid.ColumnHeadersDefaultCellStyle.Font = new Font("Microsoft YaHei UI", 10, FontStyle.Bold);
        grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = SurfaceAlt;
        grid.DefaultCellStyle.BackColor = Background;
        grid.DefaultCellStyle.ForeColor = Text;
        grid.DefaultCellStyle.SelectionBackColor = Accent;
        grid.DefaultCellStyle.SelectionForeColor = Color.White;
        grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(18, 28, 48);
        grid.RowHeadersVisible = false;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.MultiSelect = false;
        grid.ReadOnly = true;
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.ThemeStyle.AlternatingRowsStyle.BackColor = Color.FromArgb(18, 28, 48);
        grid.ThemeStyle.BackColor = Surface;
        grid.ThemeStyle.GridColor = Border;
        grid.ThemeStyle.HeaderStyle.BackColor = SurfaceAlt;
        grid.ThemeStyle.HeaderStyle.ForeColor = Text;
        grid.ThemeStyle.HeaderStyle.Font = new Font("Microsoft YaHei UI", 10, FontStyle.Bold);
        grid.ThemeStyle.RowsStyle.BackColor = Background;
        grid.ThemeStyle.RowsStyle.ForeColor = Text;
        grid.ThemeStyle.RowsStyle.SelectionBackColor = Accent;
        grid.ThemeStyle.RowsStyle.SelectionForeColor = Color.White;
    }

    // ═══════════════ 动效辅助 ═══════════════

    /// <summary>
    /// 淡入动画 — 窗体打开时 Opacity 0→1。
    /// </summary>
    public static void FadeIn(Form form, int durationMs = 400)
    {
        form.Opacity = 0;
        var timer = new System.Windows.Forms.Timer { Interval = 16 }; // ~60fps
        var steps = durationMs / 16;
        var step = 0;
        timer.Tick += (_, _) =>
        {
            step++;
            form.Opacity = Math.Min(1.0, (double)step / steps);
            if (step >= steps) timer.Stop();
        };
        timer.Start();
    }

    /// <summary>
    /// 颜色插值过渡 — 从 from 到 to，ratio 0.0~1.0。
    /// </summary>
    public static Color Lerp(Color from, Color to, double ratio)
    {
        ratio = Math.Clamp(ratio, 0, 1);
        return Color.FromArgb(
            (int)(from.A + (to.A - from.A) * ratio),
            (int)(from.R + (to.R - from.R) * ratio),
            (int)(from.G + (to.G - from.G) * ratio),
            (int)(from.B + (to.B - from.B) * ratio));
    }
}

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using ISO11820Simulator.UI;

namespace ISO11820Simulator.UI.Controls;

/// <summary>
/// 炉膛热场主模块：不是静态图片，而是"低频数据 + 高频动画"的自绘控件。
/// DataBroadcast 可以 800ms 来一次，控件内部用 30FPS 线性插值和脉冲动画保证画面连续。
/// </summary>
public sealed class FurnaceThermalViewPro : UserControl
{
    private ThermalSnapshot _target = new(25, 25, 25, 25, 25, 0, 0, 0, 0, "未选择样品", "空闲", ThermalVisualState.Idle);
    private ThermalSnapshot _display = new(25, 25, 25, 25, 25, 0, 0, 0, 0, "未选择样品", "空闲", ThermalVisualState.Idle);
    private readonly System.Windows.Forms.Timer _timer;
    private float _phase;
    private bool _first = true;

    public FurnaceThermalViewPro()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        DoubleBuffered = true;
        BackColor = Color.FromArgb(7, 17, 31);
        MinimumSize = new Size(320, 360);

        _timer = new System.Windows.Forms.Timer { Interval = 33 };
        _timer.Tick += (_, _) =>
        {
            _phase += 0.075f;
            if (_phase > MathF.PI * 2) _phase -= MathF.PI * 2;
            SmoothToTarget();
            Invalidate();
        };
        _timer.Start();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _timer.Dispose();
        base.Dispose(disposing);
    }

    public void PushSnapshot(ThermalSnapshot snapshot)
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action(() => PushSnapshot(snapshot)));
            return;
        }

        _target = snapshot;
        if (_first)
        {
            _display = snapshot;
            _first = false;
        }
    }

    private void SmoothToTarget()
    {
        const double k = 0.12;
        _display = _display with
        {
            Tf1 = Lerp(_display.Tf1, _target.Tf1, k),
            Tf2 = Lerp(_display.Tf2, _target.Tf2, k),
            Surface = Lerp(_display.Surface, _target.Surface, k),
            Center = Lerp(_display.Center, _target.Center, k),
            Calibration = Lerp(_display.Calibration, _target.Calibration, k),
            DriftTf1Per10Min = Lerp(_display.DriftTf1Per10Min, _target.DriftTf1Per10Min, k),
            DriftTf2Per10Min = Lerp(_display.DriftTf2Per10Min, _target.DriftTf2Per10Min, k),
            StableTicks = _target.StableTicks,
            RecordSeconds = _target.RecordSeconds,
            SampleNo = _target.SampleNo,
            StateText = _target.StateText,
            State = _target.State
        };
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        g.Clear(BackColor);

        var card = RectangleF.Inflate(ClientRectangle, -6, -6);
        DrawCard(g, card);
        DrawHeader(g, card);

        float footer = 86;
        var field = new RectangleF(card.X + 14, card.Y + 48, card.Width - 28, card.Height - 48 - footer);
        float size = Math.Min(field.Width, field.Height);
        var furnace = new RectangleF(field.X + (field.Width - size) / 2f, field.Y + (field.Height - size) / 2f, size, size);
        DrawFurnace(g, furnace);
        DrawFooter(g, card);
    }

    private static void DrawCard(Graphics g, RectangleF r)
    {
        using var bg = new LinearGradientBrush(r, Color.FromArgb(20, 34, 55), Color.FromArgb(8, 18, 33), LinearGradientMode.Vertical);
        using var border = new Pen(Color.FromArgb(95, Theme.Border));
        ControlPaintUtil.FillRounded(g, bg, r, 16);
        ControlPaintUtil.DrawRounded(g, border, r, 16);
    }

    private void DrawHeader(Graphics g, RectangleF r)
    {
        using var titleFont = new Font("Microsoft YaHei UI", 12, FontStyle.Bold);
        using var subFont = new Font("Segoe UI", 8.2f);
        using var textBrush = new SolidBrush(Theme.Text);
        using var mutedBrush = new SolidBrush(Theme.Muted);
        g.DrawString("炉膛热场", titleFont, textBrush, r.X + 16, r.Y + 14);
        g.DrawString("Furnace Thermal Field", subFont, mutedBrush, r.X + 96, r.Y + 19);

        var color = StateColor(_target.State);
        DrawBadge(g, new RectangleF(r.Right - 104, r.Y + 13, 86, 26), _target.StateText, color);
    }

    private void DrawFurnace(Graphics g, RectangleF rect)
    {
        var c = new PointF(rect.X + rect.Width / 2, rect.Y + rect.Height / 2);
        float r = rect.Width / 2f;
        double wallTemp = (_display.Tf1 + _display.Tf2 + _display.Calibration) / 3.0;
        float heat = (float)Math.Clamp((wallTemp - 20) / 780.0, 0, 1);

        DrawOuterShell(g, c, r);
        DrawBrickLayer(g, c, r * 0.86f);
        DrawChamber(g, c, r * 0.70f, wallTemp);
        DrawHeatingCoils(g, c, r * 0.62f, heat);
        DrawSample(g, c, r);
        DrawSensors(g, c, r);
        DrawStabilityHalo(g, c, r);
    }

    private void DrawOuterShell(Graphics g, PointF c, float r)
    {
        using var shadow = new SolidBrush(Color.FromArgb(100, 0, 0, 0));
        g.FillEllipse(shadow, c.X - r * 0.99f, c.Y - r * 0.96f + 8, r * 1.98f, r * 1.92f);

        using var shell = new Pen(Color.FromArgb(140, 82, 105, 130), r * 0.070f);
        using var shell2 = new Pen(Color.FromArgb(110, 28, 45, 66), r * 0.030f);
        g.DrawEllipse(shell, c.X - r * 0.92f, c.Y - r * 0.92f, r * 1.84f, r * 1.84f);
        g.DrawEllipse(shell2, c.X - r * 0.80f, c.Y - r * 0.80f, r * 1.60f, r * 1.60f);

        using var segPen = new Pen(Color.FromArgb(70, 170, 200, 230), 1);
        for (int i = 0; i < 24; i++)
        {
            var p1 = ControlPaintUtil.Polar(c, r * 0.86f, i * 15 - 90);
            var p2 = ControlPaintUtil.Polar(c, r * 0.98f, i * 15 - 90);
            g.DrawLine(segPen, p1, p2);
        }

        // 右侧观察口/接线端
        var port = new RectangleF(c.X + r * 0.88f, c.Y - r * 0.08f, r * 0.17f, r * 0.16f);
        using var portBrush = new SolidBrush(Color.FromArgb(75, 40, 60, 82));
        using var portPen = new Pen(Color.FromArgb(120, 130, 160, 190));
        ControlPaintUtil.FillRounded(g, portBrush, port, 6);
        ControlPaintUtil.DrawRounded(g, portPen, port, 6);
    }

    private static void DrawBrickLayer(Graphics g, PointF c, float r)
    {
        int blocks = 34;
        float sweep = 360f / blocks;
        for (int i = 0; i < blocks; i++)
        {
            var color = i % 2 == 0 ? Color.FromArgb(120, 120, 112, 98) : Color.FromArgb(96, 94, 86, 76);
            using var pen = new Pen(color, r * 0.11f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            g.DrawArc(pen, c.X - r, c.Y - r, r * 2, r * 2, i * sweep + 2, sweep - 4);
        }
    }

    private void DrawChamber(Graphics g, PointF c, float r, double wallTemp)
    {
        var ellipse = new RectangleF(c.X - r, c.Y - r, r * 2, r * 2);
        using var path = new GraphicsPath();
        path.AddEllipse(ellipse);
        using var heat = new PathGradientBrush(path)
        {
            CenterColor = ControlPaintUtil.HeatColor(_display.Center + 65, 185),
            SurroundColors = new[] { ControlPaintUtil.HeatColor(wallTemp, 165) },
            CenterPoint = new PointF(c.X, c.Y)
        };
        g.FillPath(heat, path);

        // 8 个加热热点云
        foreach (float deg in new[] { -75f, -30f, 22f, 66f, 112f, 158f, 204f, 250f })
        {
            var p = ControlPaintUtil.Polar(c, r * 0.80f, deg);
            float rr = r * 0.35f;
            using var pth = new GraphicsPath();
            pth.AddEllipse(p.X - rr, p.Y - rr, rr * 2, rr * 2);
            using var glow = new PathGradientBrush(pth)
            {
                CenterColor = Color.FromArgb(95, 255, 122, 25),
                SurroundColors = new[] { Color.FromArgb(0, 255, 122, 25) }
            };
            g.FillPath(glow, pth);
        }

        using var ring = new Pen(Color.FromArgb(120, 255, 166, 55), 2f);
        g.DrawEllipse(ring, ellipse);

        using var isoPen = new Pen(Color.FromArgb(18, 255, 255, 255), 1);
        for (float k = 0.25f; k <= 0.75f; k += 0.17f)
            g.DrawEllipse(isoPen, c.X - r * k, c.Y - r * k, r * k * 2, r * k * 2);
    }

    private void DrawHeatingCoils(Graphics g, PointF c, float r, float heat)
    {
        var arcRect = new RectangleF(c.X - r, c.Y - r, r * 2, r * 2);
        var segs = new[] { (-82f, 28f), (-34f, 28f), (14f, 28f), (62f, 28f), (110f, 28f), (158f, 28f), (206f, 28f), (254f, 28f) };

        int i = 0;
        foreach (var (start, sweep) in segs)
        {
            float pulse = 0.55f + 0.45f * MathF.Sin(_phase * 2.2f + i * 0.75f);
            int alpha = (int)(70 + 185 * heat * pulse);
            Color coil = ControlPaintUtil.Blend(Color.FromArgb(255, 90, 28), Color.FromArgb(255, 235, 92), pulse);
            using var glow = new Pen(Color.FromArgb((int)(alpha * 0.30f), coil), r * 0.09f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            using var core = new Pen(Color.FromArgb(alpha, coil), r * 0.038f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            g.DrawArc(glow, arcRect, start, sweep);
            g.DrawArc(core, arcRect, start, sweep);
            i++;
        }
    }

    private void DrawSample(Graphics g, PointF c, float r)
    {
        var sample = new RectangleF(c.X - r * 0.13f, c.Y - r * 0.34f, r * 0.26f, r * 0.68f);
        using var shadow = new SolidBrush(Color.FromArgb(90, 0, 0, 0));
        g.FillRectangle(shadow, sample.X + 4, sample.Y + 5, sample.Width, sample.Height);

        using var path = ControlPaintUtil.RoundedRect(sample, 5);
        using var fill = new LinearGradientBrush(sample,
            ControlPaintUtil.HeatColor(_display.Surface, 230),
            ControlPaintUtil.HeatColor(_display.Center, 210),
            LinearGradientMode.Vertical);
        g.FillPath(fill, path);

        using var pen = new Pen(Color.FromArgb(210, 220, 238, 255), 1.1f);
        g.DrawPath(pen, path);

        using var highlight = new Pen(Color.FromArgb(70, 255, 255, 255));
        g.DrawLine(highlight, sample.X + sample.Width * 0.55f, sample.Y + 5, sample.X + sample.Width * 0.55f, sample.Bottom - 5);
    }

    private void DrawSensors(Graphics g, PointF c, float r)
    {
        DrawSensor(g, "TF1", _display.Tf1, ControlPaintUtil.Polar(c, r * 0.44f, 220), new PointF(-86, -34), Theme.Accent);
        DrawSensor(g, "TF2", _display.Tf2, ControlPaintUtil.Polar(c, r * 0.44f, 320), new PointF(18, -34), Theme.Info);
        DrawSensor(g, "TCal", _display.Calibration, ControlPaintUtil.Polar(c, r * 0.52f, 180), new PointF(-100, -10), Theme.Warning);
        DrawSensor(g, "TS", _display.Surface, ControlPaintUtil.Polar(c, r * 0.24f, 0), new PointF(20, -10), Theme.Success);
        DrawSensor(g, "TC", _display.Center, ControlPaintUtil.Polar(c, r * 0.22f, 90), new PointF(-38, 18), Theme.Warning);
    }

    private void DrawSensor(Graphics g, string name, double temp, PointF p, PointF offset, Color color)
    {
        float pulse = 0.55f + 0.45f * MathF.Sin(_phase * 3.0f);
        using var glow = new SolidBrush(Color.FromArgb((int)(55 + 85 * pulse), color));
        g.FillEllipse(glow, p.X - 11, p.Y - 11, 22, 22);
        using var ring = new Pen(color, 2f);
        using var core = new SolidBrush(Color.White);
        g.DrawEllipse(ring, p.X - 7, p.Y - 7, 14, 14);
        g.FillEllipse(core, p.X - 3, p.Y - 3, 6, 6);

        string text = $"{name} {temp:0.0}";
        using var font = new Font("Segoe UI", 7.4f, FontStyle.Bold);
        var sz = g.MeasureString(text, font);
        var label = new RectangleF(p.X + offset.X, p.Y + offset.Y, sz.Width + 12, sz.Height + 7);
        using var bg = new SolidBrush(Color.FromArgb(185, 7, 17, 31));
        using var pen = new Pen(Color.FromArgb(135, color));
        ControlPaintUtil.FillRounded(g, bg, label, 7);
        ControlPaintUtil.DrawRounded(g, pen, label, 7);
        using var textBrush = new SolidBrush(Theme.Text);
        g.DrawString(text, font, textBrush, label.X + 6, label.Y + 3);
    }

    private void DrawStabilityHalo(Graphics g, PointF c, float r)
    {
        if (_target.State is not (ThermalVisualState.Ready or ThermalVisualState.Recording or ThermalVisualState.Complete)) return;
        Color color = _target.State == ThermalVisualState.Complete ? Theme.Success : Theme.Accent;
        float pulse = 0.55f + 0.45f * MathF.Sin(_phase * 1.7f);
        using var halo = new Pen(Color.FromArgb((int)(70 + 110 * pulse), color), 2.6f);
        g.DrawEllipse(halo, c.X - r * 0.98f, c.Y - r * 0.98f, r * 1.96f, r * 1.96f);
    }

    private void DrawFooter(Graphics g, RectangleF r)
    {
        float x = r.X + 18;
        float y = r.Bottom - 66;
        float w = r.Width - 36;
        var bar = new RectangleF(x, y, w, 12);
        using var brush = new LinearGradientBrush(bar, Color.Blue, Color.Red, LinearGradientMode.Horizontal)
        {
            InterpolationColors = new ColorBlend
            {
                Positions = new[] { 0f, 0.35f, 0.58f, 0.78f, 1f },
                Colors = new[]
                {
                    Color.FromArgb(47, 140, 255),
                    Color.FromArgb(24, 215, 255),
                    Color.FromArgb(255, 220, 70),
                    Color.FromArgb(255, 139, 35),
                    Color.FromArgb(255, 77, 79)
                }
            }
        };
        ControlPaintUtil.FillRounded(g, brush, bar, 6);
        using var line = new Pen(Color.FromArgb(110, 220, 235, 255));
        ControlPaintUtil.DrawRounded(g, line, bar, 6);

        using var small = new Font("Microsoft YaHei UI", 8f);
        using var value = new Font("Consolas", 9f, FontStyle.Bold);
        using var mutedBrush = new SolidBrush(Theme.Muted);
        using var textBrush = new SolidBrush(Theme.Text);
        g.DrawString("冷", small, mutedBrush, x, y - 22);
        g.DrawString("热", small, mutedBrush, x + w - 18, y - 22);
        g.DrawString($"样品：{_display.SampleNo}", small, mutedBrush, x, y + 22);
        g.DrawString($"记录 {_display.RecordSeconds}s    稳定 {_display.StableTicks}/4", value, textBrush, x + w * 0.48f, y + 20);
    }

    private static void DrawBadge(Graphics g, RectangleF r, string text, Color color)
    {
        using var bg = new SolidBrush(Color.FromArgb(35, color));
        using var pen = new Pen(Color.FromArgb(150, color));
        ControlPaintUtil.FillRounded(g, bg, r, 13);
        ControlPaintUtil.DrawRounded(g, pen, r, 13);
        using var dot = new SolidBrush(color);
        g.FillEllipse(dot, r.X + 10, r.Y + 9, 8, 8);
        using var font = new Font("Microsoft YaHei UI", 8.2f, FontStyle.Bold);
        using var textBrush = new SolidBrush(Theme.Text);
        g.DrawString(text, font, textBrush, r.X + 25, r.Y + 5);
    }

    private static Color StateColor(ThermalVisualState state) => state switch
    {
        ThermalVisualState.Preparing => Theme.Warning,
        ThermalVisualState.Ready => Theme.Success,
        ThermalVisualState.Recording => Theme.Accent,
        ThermalVisualState.Complete => Theme.Success,
        _ => Theme.Muted
    };

    private static double Lerp(double a, double b, double k) => a + (b - a) * k;
}

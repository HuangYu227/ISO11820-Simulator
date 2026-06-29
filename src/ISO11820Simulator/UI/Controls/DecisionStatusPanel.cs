using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using ISO11820Simulator.UI;

namespace ISO11820Simulator.UI.Controls;

/// <summary>
/// 右下判定卡：用条形状态代替三个同级圆形仪表，让"能否开始记录/是否达标"更直观。
/// </summary>
public sealed class DecisionStatusPanel : UserControl
{
    private ThermalSnapshot _snapshot = new(25, 25, 25, 25, 25, 0, 0, 0, 0, "未选择样品", "空闲", ThermalVisualState.Idle);
    private readonly System.Windows.Forms.Timer _timer;
    private float _phase;

    public DecisionStatusPanel()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        DoubleBuffered = true;
        BackColor = Theme.Background;
        MinimumSize = new Size(300, 170);
        _timer = new System.Windows.Forms.Timer { Interval = 33 };
        _timer.Tick += (_, _) => { _phase += 0.055f; Invalidate(); };
        _timer.Start();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _timer.Dispose();
        base.Dispose(disposing);
    }

    public void UpdateSnapshot(ThermalSnapshot snapshot)
    {
        _snapshot = snapshot;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        g.Clear(Theme.Background);

        var card = RectangleF.Inflate(ClientRectangle, -6, -6);
        using var bg = new LinearGradientBrush(card, Color.FromArgb(30, Theme.Surface), Theme.Surface, LinearGradientMode.Vertical);
        using var border = new Pen(Color.FromArgb(90, Theme.Border));
        ControlPaintUtil.FillRounded(g, bg, card, 14);
        ControlPaintUtil.DrawRounded(g, border, card, 14);

        using var title = new Font("Microsoft YaHei UI", 11, FontStyle.Bold);
        using var sub = new Font("Microsoft YaHei UI", 8.3f);
        using var textBrush = new SolidBrush(Theme.Text);
        using var mutedBrush = new SolidBrush(Theme.Muted);
        g.DrawString("稳定性判定", title, textBrush, card.X + 16, card.Y + 12);
        g.DrawString("ISO 11820 Ready Gate", sub, mutedBrush, card.X + 108, card.Y + 16);

        double temp = (_snapshot.Tf1 + _snapshot.Tf2) / 2.0;
        bool tempOk = temp >= 745 && temp <= 755;
        bool driftOk = Math.Abs(_snapshot.DriftTf1Per10Min) <= 2.0 && Math.Abs(_snapshot.DriftTf2Per10Min) <= 2.0;
        bool ticksOk = _snapshot.StableTicks >= 4;

        var y = card.Y + 48;
        float rowH = Math.Max(28, (card.Height - 66) / 4f);
        DrawRow(g, card.X + 16, y, card.Width - 32, rowH, "炉温范围", $"{temp:0.0} °C", "745–755 °C", tempOk, ProgressRange(temp, 700, 760));
        DrawRow(g, card.X + 16, y + rowH, card.Width - 32, rowH, "漂移速率", $"TF1 {_snapshot.DriftTf1Per10Min:+0.0;-0.0;0.0} / TF2 {_snapshot.DriftTf2Per10Min:+0.0;-0.0;0.0}", "±2 °C/10min", driftOk, 1f - (float)Math.Clamp(Math.Max(Math.Abs(_snapshot.DriftTf1Per10Min), Math.Abs(_snapshot.DriftTf2Per10Min)) / 4.0, 0, 1));
        DrawRow(g, card.X + 16, y + rowH * 2, card.Width - 32, rowH, "稳定计数", $"{Math.Clamp(_snapshot.StableTicks, 0, 4)}/4", "连续稳定", ticksOk, Math.Clamp(_snapshot.StableTicks / 4f, 0, 1));

        string final = (tempOk && driftOk && ticksOk) ? "允许开始记录" : _snapshot.State == ThermalVisualState.Recording ? "记录中" : "等待稳定";
        Color finalColor = final == "允许开始记录" || final == "记录中" ? Theme.Success : Theme.Warning;
        DrawFinalBadge(g, new RectangleF(card.X + 16, card.Bottom - 34, card.Width - 32, 22), final, finalColor);
    }

    private static void DrawRow(Graphics g, float x, float y, float w, float h, string name, string value, string hint, bool ok, float progress)
    {
        Color color = ok ? Theme.Success : Theme.Warning;
        using var labelFont = new Font("Microsoft YaHei UI", 8.4f, FontStyle.Bold);
        using var valueFont = new Font("Consolas", 9f, FontStyle.Bold);
        using var hintFont = new Font("Microsoft YaHei UI", 7.5f);
        using var textBrush = new SolidBrush(Theme.Text);
        using var colorBrush = new SolidBrush(color);
        using var mutedBrush = new SolidBrush(Theme.Muted);
        g.DrawString(name, labelFont, textBrush, x, y + 2);
        g.DrawString(value, valueFont, colorBrush, x + w * 0.30f, y + 2);
        g.DrawString(hint, hintFont, mutedBrush, x + w * 0.72f, y + 4);

        var bar = new RectangleF(x, y + h - 8, w, 5);
        using var barBg = new SolidBrush(Color.FromArgb(65, Theme.SurfaceAlt));
        ControlPaintUtil.FillRounded(g, barBg, bar, 3);
        var fill = new RectangleF(bar.X, bar.Y, bar.Width * Math.Clamp(progress, 0f, 1f), bar.Height);
        using var barFill = new SolidBrush(color);
        ControlPaintUtil.FillRounded(g, barFill, fill, 3);
    }

    private void DrawFinalBadge(Graphics g, RectangleF r, string text, Color color)
    {
        float pulse = 0.5f + 0.5f * MathF.Sin(_phase * 2.4f);
        using var bg = new SolidBrush(Color.FromArgb((int)(28 + 22 * pulse), color));
        using var pen = new Pen(Color.FromArgb(150, color));
        ControlPaintUtil.FillRounded(g, bg, r, 11);
        ControlPaintUtil.DrawRounded(g, pen, r, 11);
        using var font = new Font("Microsoft YaHei UI", 8.3f, FontStyle.Bold);
        var sz = g.MeasureString(text, font);
        using var textBrush = new SolidBrush(Theme.Text);
        g.DrawString(text, font, textBrush, r.X + (r.Width - sz.Width) / 2, r.Y + 3);
    }

    private static float ProgressRange(double value, double min, double max)
        => (float)Math.Clamp((value - min) / (max - min), 0, 1);
}

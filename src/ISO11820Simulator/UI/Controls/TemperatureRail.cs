using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using ISO11820Simulator.UI;

namespace ISO11820Simulator.UI.Controls;

/// <summary>
/// 顶部温度总览：把 TF1/TF2 合并成"炉膛温度"主卡，其他通道作为辅助卡。
/// 目的：第一眼先看炉膛是否接近 750°C，而不是被 5 个同级数字淹没。
/// </summary>
public sealed class TemperatureRail : UserControl
{
    private ThermalSnapshot _snapshot = new(25, 25, 25, 25, 25, 0, 0, 0, 0, "未选择样品", "空闲", ThermalVisualState.Idle);
    private readonly System.Windows.Forms.Timer _timer;
    private float _phase;

    public TemperatureRail()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        DoubleBuffered = true;
        BackColor = Theme.Background;
        MinimumSize = new Size(900, 105);
        _timer = new System.Windows.Forms.Timer { Interval = 33 };
        _timer.Tick += (_, _) => { _phase += 0.05f; Invalidate(); };
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

        var gap = 10f;
        var rect = new RectangleF(4, 4, Width - 8, Height - 8);
        if (rect.Width < 600 || rect.Height < 70) return;

        float mainW = Math.Max(290, rect.Width * 0.28f);
        var main = new RectangleF(rect.X, rect.Y, mainW, rect.Height);
        float smallW = (rect.Width - mainW - gap * 4) / 4f;

        DrawMainCard(g, main);
        DrawSmallCard(g, new RectangleF(main.Right + gap, rect.Y, smallW, rect.Height), "表面温 TS", _snapshot.Surface, Theme.Success);
        DrawSmallCard(g, new RectangleF(main.Right + gap * 2 + smallW, rect.Y, smallW, rect.Height), "中心温 TC", _snapshot.Center, Theme.Warning);
        DrawSmallCard(g, new RectangleF(main.Right + gap * 3 + smallW * 2, rect.Y, smallW, rect.Height), "校准温 TCal", _snapshot.Calibration, Theme.Warning);
        DrawSmallCard(g, new RectangleF(main.Right + gap * 4 + smallW * 3, rect.Y, smallW, rect.Height), "稳定 / 漂移", _snapshot.StableTicks, Theme.Accent, isStatus: true);
    }

    private void DrawMainCard(Graphics g, RectangleF r)
    {
        var furnace = (_snapshot.Tf1 + _snapshot.Tf2) / 2.0;
        Color accent = _snapshot.State switch
        {
            ThermalVisualState.Ready => Theme.Success,
            ThermalVisualState.Recording => Theme.Accent,
            ThermalVisualState.Complete => Theme.Warning,
            ThermalVisualState.Preparing => Theme.Warning,
            _ => Theme.Muted
        };

        DrawCardShell(g, r, accent, emphasized: true);
        using var titleFont = new Font("Microsoft YaHei UI", 10, FontStyle.Bold);
        using var subFont = new Font("Microsoft YaHei UI", 8.5f);
        using var bigFont = new Font("Consolas", Math.Max(22, r.Height * 0.34f), FontStyle.Bold);
        using var unitFont = new Font("Microsoft YaHei UI", 10, FontStyle.Bold);

        using var textBrush = new SolidBrush(Theme.Text);
        using var accentBrush = new SolidBrush(accent);
        using var mutedBrush = new SolidBrush(Theme.Muted);
        g.DrawString("炉膛温度", titleFont, textBrush, r.X + 18, r.Y + 12);
        DrawStateBadge(g, new RectangleF(r.Right - 92, r.Y + 10, 74, 24), _snapshot.StateText, accent);

        var value = $"{furnace:0.0}";
        var sz = g.MeasureString(value, bigFont);
        g.DrawString(value, bigFont, accentBrush, r.X + 18, r.Y + 37);
        g.DrawString("°C", unitFont, mutedBrush, r.X + 20 + sz.Width, r.Y + 50);

        string details = $"TF1 {_snapshot.Tf1:0.0}   TF2 {_snapshot.Tf2:0.0}   Δ {Math.Abs(_snapshot.Tf1 - _snapshot.Tf2):0.0}°C";
        g.DrawString(details, subFont, mutedBrush, r.X + 18, r.Bottom - 24);
    }

    private void DrawSmallCard(Graphics g, RectangleF r, string title, double value, Color accent, bool isStatus = false)
    {
        DrawCardShell(g, r, accent, emphasized: false);
        using var titleFont = new Font("Microsoft YaHei UI", 8.8f, FontStyle.Bold);
        using var valueFont = new Font("Consolas", Math.Max(16, r.Height * 0.23f), FontStyle.Bold);
        using var subFont = new Font("Microsoft YaHei UI", 7.8f);
        using var mutedBrush = new SolidBrush(Theme.Muted);
        g.DrawString(title, titleFont, mutedBrush, r.X + 13, r.Y + 12);

        if (!isStatus)
        {
            using var accentBrush = new SolidBrush(accent);
            g.DrawString($"{value:0.0} °C", valueFont, accentBrush, r.X + 13, r.Y + 40);
            return;
        }

        var stable = Math.Clamp(_snapshot.StableTicks, 0, 4);
        Color statusColor = stable >= 4 ? Theme.Success : stable >= 2 ? Theme.Warning : Theme.Danger;
        using var statusBrush = new SolidBrush(statusColor);
        g.DrawString($"{stable}/4", valueFont, statusBrush, r.X + 13, r.Y + 38);
        g.DrawString($"TF1 {_snapshot.DriftTf1Per10Min:+0.0;-0.0;0.0}  TF2 {_snapshot.DriftTf2Per10Min:+0.0;-0.0;0.0} °C/10min", subFont, mutedBrush, r.X + 13, r.Bottom - 23);
    }

    private void DrawCardShell(Graphics g, RectangleF r, Color accent, bool emphasized)
    {
        using var path = ControlPaintUtil.RoundedRect(r, 14);
        using var fill = new LinearGradientBrush(r, Color.FromArgb(34, Theme.Surface), Theme.Surface, LinearGradientMode.Vertical);
        using var border = new Pen(Color.FromArgb(emphasized ? 130 : 85, accent), emphasized ? 1.4f : 1f);
        g.FillPath(fill, path);
        g.DrawPath(border, path);

        float pulse = 0.5f + 0.5f * MathF.Sin(_phase * 2.0f);
        using var accentBrush = new SolidBrush(Color.FromArgb((int)(130 + 60 * pulse), accent));
        ControlPaintUtil.FillRounded(g, accentBrush, new RectangleF(r.X + 1, r.Y + 14, 4, r.Height - 28), 2);
    }

    private static void DrawStateBadge(Graphics g, RectangleF r, string text, Color color)
    {
        using var bg = new SolidBrush(Color.FromArgb(36, color));
        using var pen = new Pen(Color.FromArgb(150, color));
        ControlPaintUtil.FillRounded(g, bg, r, 12);
        ControlPaintUtil.DrawRounded(g, pen, r, 12);
        using var font = new Font("Microsoft YaHei UI", 8.2f, FontStyle.Bold);
        var sz = g.MeasureString(text, font);
        using var textBrush = new SolidBrush(Theme.Text);
        g.DrawString(text, font, textBrush, r.X + (r.Width - sz.Width) / 2, r.Y + (r.Height - sz.Height) / 2 - 1);
    }
}

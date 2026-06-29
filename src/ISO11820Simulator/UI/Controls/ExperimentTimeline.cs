using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using ISO11820Simulator.UI;

namespace ISO11820Simulator.UI.Controls;

/// <summary>
/// 系统日志时间线：替代 RichTextBox 黑框。保留最新 80 条，按事件级别显示颜色和时间点。
/// </summary>
public sealed class ExperimentTimeline : UserControl
{
    private readonly List<TimelineEntry> _entries = new();
    private const int MaxEntries = 80;

    public ExperimentTimeline()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        DoubleBuffered = true;
        BackColor = Theme.Background;
        MinimumSize = new Size(420, 170);
    }

    public void AddEntry(string time, string message, Color color)
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action(() => AddEntry(time, message, color)));
            return;
        }
        _entries.Add(new TimelineEntry(time, message, color));
        while (_entries.Count > MaxEntries) _entries.RemoveAt(0);
        Invalidate();
    }

    public void ClearEntries()
    {
        _entries.Clear();
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
        using var bg = new SolidBrush(Theme.Surface);
        using var border = new Pen(Color.FromArgb(90, Theme.Border));
        ControlPaintUtil.FillRounded(g, bg, card, 14);
        ControlPaintUtil.DrawRounded(g, border, card, 14);

        using var titleFont = new Font("Microsoft YaHei UI", 10.5f, FontStyle.Bold);
        using var subFont = new Font("Microsoft YaHei UI", 8.2f);
        using var textBrush = new SolidBrush(Theme.Text);
        using var mutedBrush = new SolidBrush(Theme.Muted);
        g.DrawString("实验事件时间线", titleFont, textBrush, card.X + 16, card.Y + 12);
        g.DrawString("System Event Stream", subFont, mutedBrush, card.X + 126, card.Y + 15);

        var area = new RectangleF(card.X + 16, card.Y + 42, card.Width - 32, card.Height - 50);
        if (_entries.Count == 0)
        {
            using var f = new Font("Microsoft YaHei UI", 9f);
            g.DrawString("暂无事件", f, mutedBrush, area.X, area.Y + 10);
            return;
        }

        using var timeFont = new Font("Consolas", 8f, FontStyle.Bold);
        using var msgFont = new Font("Microsoft YaHei UI", 8.6f);
        float rowH = 24;
        int visible = Math.Max(1, (int)(area.Height / rowH));
        var list = _entries.Skip(Math.Max(0, _entries.Count - visible)).ToList();
        float y = area.Y;

        using var linePen = new Pen(Color.FromArgb(55, Theme.Border), 1);
        g.DrawLine(linePen, area.X + 5, y + 5, area.X + 5, area.Bottom - 8);

        foreach (var item in list)
        {
            using var dot = new SolidBrush(item.Color);
            using var dotGlow = new SolidBrush(Color.FromArgb(45, item.Color));
            g.FillEllipse(dotGlow, area.X - 2, y + 5, 14, 14);
            g.FillEllipse(dot, area.X + 2, y + 9, 6, 6);
            using var timeBrush = new SolidBrush(item.Color);
            g.DrawString(item.Time, timeFont, timeBrush, area.X + 20, y + 3);
            g.DrawString(TrimToWidth(g, item.Message, msgFont, area.Width - 105), msgFont, textBrush, area.X + 92, y + 3);
            y += rowH;
        }
    }

    private static string TrimToWidth(Graphics g, string text, Font font, float maxWidth)
    {
        if (g.MeasureString(text, font).Width <= maxWidth) return text;
        string ellipsis = "…";
        int len = text.Length;
        while (len > 0 && g.MeasureString(text[..len] + ellipsis, font).Width > maxWidth) len--;
        return len <= 0 ? ellipsis : text[..len] + ellipsis;
    }

    private sealed record TimelineEntry(string Time, string Message, Color Color);
}
